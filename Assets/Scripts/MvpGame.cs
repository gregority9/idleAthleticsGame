using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrackDynasty.Mvp02
{
    public class MvpGame : MonoBehaviour
    {
        private enum ScreenState { HQ, Team, Athlete, Season, Scout, Legends, PreRace, Race, Results }

        private ScreenState _screen = ScreenState.HQ;
        private readonly List<Athlete> _roster = new List<Athlete>();
        private readonly List<Prospect> _prospects = new List<Prospect>();
        private readonly List<SeasonEvent> _events = new List<SeasonEvent>();
        private readonly List<HallOfFameEntry> _legends = new List<HallOfFameEntry>();

        private Athlete _selected;
        private RaceSummary _pendingRace;
        private RaceStrategy _strategy = RaceStrategy.Balanced;
        private RaceResult _preview;
        private RaceResult _result;
        private float _raceElapsed;
        private int _season = 1;
        private int _day = 1;
        private int _cash = 6200;
        private int _reputation = 120;
        private int _raceSeed = 1;
        private bool _claimed;

        private GUIStyle _logo;
        private GUIStyle _title;
        private GUIStyle _h2;
        private GUIStyle _body;
        private GUIStyle _small;
        private GUIStyle _tiny;
        private GUIStyle _center;
        private GUIStyle _right;
        private GUIStyle _goldButton;
        private GUIStyle _greenButton;
        private GUIStyle _darkButton;
        private bool _stylesReady;

        private Texture2D _bg;
        private Texture2D _panel;
        private Texture2D _panel2;
        private Texture2D _gold;
        private Texture2D _green;
        private Texture2D _greenDim;
        private Texture2D _muted;
        private Texture2D _white;
        private Texture2D _track;
        private Texture2D _grass;
        private Texture2D _runnerA;
        private Texture2D _runnerB;
        private Texture2D _runnerAiA;
        private Texture2D _runnerAiB;
        private readonly Dictionary<string, Texture2D> _flags = new Dictionary<string, Texture2D>();

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            BuildTextures();
            SeedRoster();
            BuildSeason();
            RefreshProspects();
            _selected = _roster[0];
        }

        private void Update()
        {
            if (_screen != ScreenState.Race || _result == null) return;
            _raceElapsed += Time.deltaTime;
            float maxTime = 0f;
            for (int i = 0; i < _result.Runners.Count; i++) maxTime = Mathf.Max(maxTime, _result.Runners[i].FinishTime);
            if (_raceElapsed > maxTime + 0.75f) _screen = ScreenState.Results;
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Min(Screen.width / 430f, Screen.height / 930f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;
            GUI.DrawTexture(new Rect(0, 0, w, h), _bg);

            switch (_screen)
            {
                case ScreenState.HQ: DrawHQ(w, h); break;
                case ScreenState.Team: DrawTeam(w, h); break;
                case ScreenState.Athlete: DrawAthlete(w, h); break;
                case ScreenState.Season: DrawSeason(w, h); break;
                case ScreenState.Scout: DrawScout(w, h); break;
                case ScreenState.Legends: DrawLegends(w, h); break;
                case ScreenState.PreRace: DrawPreRace(w, h); break;
                case ScreenState.Race: DrawRace(w, h); break;
                case ScreenState.Results: DrawResults(w, h); break;
            }
        }

        private void DrawHQ(float w, float h)
        {
            Header(w, "CAREER HQ");
            GUI.Label(new Rect(24, 88, 300, 36), "SEASON " + _season + " · DAY " + _day, _title);
            GUI.Label(new Rect(24, 130, 180, 22), "$" + _cash + "   ★" + _reputation, _body);

            SeasonEvent current = CurrentEvent();
            Rect card = new Rect(18, 170, w - 36, 150);
            GUI.DrawTexture(card, _panel);
            GUI.Label(new Rect(34, 186, 260, 22), current != null ? "NEXT EVENT" : "TRAINING DAY", _small);
            GUI.Label(new Rect(34, 216, w - 68, 34), current != null ? current.Name.ToUpperInvariant() : "NO RACE TODAY", _title);
            GUI.Label(new Rect(34, 255, w - 68, 22), current != null ? current.Tier.ToString() : "Advance the day to train your roster.", _body);
            if (current != null)
            {
                if (GUI.Button(new Rect(w - 146, 270, 110, 34), "OPEN", _goldButton)) _screen = ScreenState.Season;
            }
            else if (GUI.Button(new Rect(w - 180, 270, 144, 34), "ADVANCE DAY", _greenButton)) AdvanceDay();

            GUI.Label(new Rect(24, 350, 200, 24), "TEAM", _h2);
            float y = 386;
            for (int i = 0; i < _roster.Count && i < 4; i++)
            {
                Athlete a = _roster[i];
                Rect row = new Rect(18, y, w - 36, 78);
                GUI.DrawTexture(row, _panel2);
                DrawFlag(a.CountryCode, new Rect(30, y + 28, 24, 16));
                GUI.Label(new Rect(62, y + 13, 190, 22), a.DisplayName, _body);
                GUI.Label(new Rect(62, y + 39, 210, 18), "Age " + a.Age + " · PB " + a.PersonalBest.ToString("0.00") + "s", _tiny);
                GUI.Label(new Rect(w - 160, y + 13, 120, 18), "OVR " + a.Overall, _right);
                GUI.Label(new Rect(w - 160, y + 39, 120, 18), "Fat " + Mathf.RoundToInt(a.Fatigue * 100f) + "%", _right);
                if (GUI.Button(new Rect(w - 96, y + 20, 60, 32), "VIEW", _darkButton)) { _selected = a; _screen = ScreenState.Athlete; }
                y += 88;
            }
            BottomNav(w, h, 0);
        }

        private void DrawTeam(float w, float h)
        {
            Header(w, "TEAM");
            GUI.Label(new Rect(24, 88, 260, 34), "ROSTER " + _roster.Count + "/5", _title);
            float y = 142;
            for (int i = 0; i < _roster.Count; i++)
            {
                Athlete a = _roster[i];
                Rect row = new Rect(18, y, w - 36, 105);
                GUI.DrawTexture(row, _panel);
                DrawFlag(a.CountryCode, new Rect(30, y + 18, 24, 16));
                GUI.Label(new Rect(62, y + 12, 210, 24), a.DisplayName, _h2);
                GUI.Label(new Rect(62, y + 42, 210, 18), "Age " + a.Age + " · PB " + a.PersonalBest.ToString("0.00") + "s", _tiny);
                GUI.Label(new Rect(62, y + 64, 210, 18), "Potential " + a.PotentialMin + "–" + a.PotentialMax, _tiny);
                GUI.Label(new Rect(w - 160, y + 16, 120, 18), "OVR " + a.Overall, _right);
                GUI.Label(new Rect(w - 160, y + 42, 120, 18), "Form " + Mathf.RoundToInt(a.Form * 100f) + "%", _right);
                if (GUI.Button(new Rect(w - 98, y + 65, 62, 28), "OPEN", _goldButton)) { _selected = a; _screen = ScreenState.Athlete; }
                y += 116;
            }
            BottomNav(w, h, 1);
        }

        private void DrawAthlete(float w, float h)
        {
            if (_selected == null) { _screen = ScreenState.Team; return; }
            Header(w, "ATHLETE");
            if (GUI.Button(new Rect(18, 82, 70, 30), "‹ TEAM", _darkButton)) _screen = ScreenState.Team;

            GUI.Label(new Rect(24, 130, 300, 34), _selected.DisplayName.ToUpperInvariant(), _title);
            DrawFlag(_selected.CountryCode, new Rect(24, 172, 30, 20));
            GUI.Label(new Rect(64, 170, 260, 22), "Age " + _selected.Age + " · PB " + _selected.PersonalBest.ToString("0.00") + "s", _body);
            GUI.Label(new Rect(24, 204, 320, 20), "Potential estimate: " + _selected.PotentialMin + "–" + _selected.PotentialMax, _small);

            float y = 250;
            DrawStat(w, ref y, "SPEED", _selected.Speed);
            DrawStat(w, ref y, "ACCELERATION", _selected.Acceleration);
            DrawStat(w, ref y, "STRENGTH", _selected.Strength);
            DrawStat(w, ref y, "TECHNIQUE", _selected.Technique);
            DrawStat(w, ref y, "MENTAL", _selected.Mental);

            GUI.Label(new Rect(24, 505, 220, 24), "TRAINING FOCUS", _h2);
            TrainingFocus[] focuses = { TrainingFocus.Sprint, TrainingFocus.Strength, TrainingFocus.Technique, TrainingFocus.Recovery };
            y = 540;
            for (int i = 0; i < focuses.Length; i++)
            {
                TrainingFocus focus = focuses[i];
                bool active = _selected.Training == focus;
                Rect r = new Rect(24, y, w - 48, 48);
                GUI.DrawTexture(r, active ? _greenDim : _panel2);
                GUI.Label(new Rect(38, y + 13, 170, 22), focus.ToString().ToUpperInvariant(), active ? _center : _body);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) _selected.Training = focus;
                y += 58;
            }

            GUI.Label(new Rect(24, y + 5, 260, 18), "Form " + Mathf.RoundToInt(_selected.Form * 100f) + "% · Fatigue " + Mathf.RoundToInt(_selected.Fatigue * 100f) + "%", _small);
            if (GUI.Button(new Rect(24, h - 112, w - 48, 38), "RELEASE ATHLETE", _darkButton)) ReleaseSelected();
            BottomNav(w, h, 1);
        }

        private void DrawSeason(float w, float h)
        {
            Header(w, "SEASON");
            GUI.Label(new Rect(24, 88, 300, 34), "SEASON " + _season + " · DAY " + _day, _title);
            SeasonEvent current = CurrentEvent();
            if (current == null)
            {
                GUI.Label(new Rect(24, 140, w - 48, 22), "Training day. No race scheduled.", _body);
                if (GUI.Button(new Rect(24, 178, w - 48, 44), "ADVANCE DAY", _greenButton)) AdvanceDay();
            }
            else
            {
                Rect card = new Rect(18, 138, w - 36, 118);
                GUI.DrawTexture(card, _panel);
                GUI.Label(new Rect(32, 152, w - 64, 26), current.Name.ToUpperInvariant(), _h2);
                GUI.Label(new Rect(32, 184, 160, 20), current.Tier.ToString(), _body);
                GUI.Label(new Rect(32, 212, 240, 18), "Reward up to $" + current.BaseCashReward, _tiny);
            }

            GUI.Label(new Rect(24, current == null ? 260 : 280, 220, 24), "CHOOSE ATHLETE", _h2);
            float y = current == null ? 300 : 320;
            for (int i = 0; i < _roster.Count; i++)
            {
                Athlete a = _roster[i];
                Rect row = new Rect(18, y, w - 36, 75);
                GUI.DrawTexture(row, _panel2);
                DrawFlag(a.CountryCode, new Rect(30, y + 28, 22, 14));
                GUI.Label(new Rect(60, y + 13, 190, 22), a.DisplayName, _body);
                GUI.Label(new Rect(60, y + 40, 190, 18), "PB " + a.PersonalBest.ToString("0.00") + " · OVR " + a.Overall, _tiny);
                if (current != null && GUI.Button(new Rect(w - 98, y + 20, 62, 34), "ENTER", _goldButton))
                {
                    _selected = a;
                    _pendingRace = new RaceSummary { Athlete = a, Event = current };
                    _preview = null;
                    _strategy = RaceStrategy.Balanced;
                    _screen = ScreenState.PreRace;
                }
                y += 84;
            }
            BottomNav(w, h, 2);
        }

        private void DrawScout(float w, float h)
        {
            Header(w, "SCOUT");
            GUI.Label(new Rect(24, 88, 300, 34), "DISCOVER TALENT", _title);
            if (GUI.Button(new Rect(w - 170, 86, 146, 34), "REFRESH $1500", _darkButton))
            {
                if (_cash >= 1500) { _cash -= 1500; RefreshProspects(); }
            }

            float y = 140;
            for (int i = 0; i < _prospects.Count; i++)
            {
                Prospect p = _prospects[i];
                Rect card = new Rect(18, y, w - 36, 156);
                GUI.DrawTexture(card, _panel);
                DrawFlag(p.CountryCode, new Rect(30, y + 18, 24, 16));
                GUI.Label(new Rect(62, y + 12, 230, 24), p.DisplayName, _h2);
                GUI.Label(new Rect(62, y + 42, 230, 18), "Age " + p.Age + " · Potential " + p.PotentialMin + "–" + p.PotentialMax, _tiny);
                GUI.Label(new Rect(62, y + 65, 240, 18), "Speed " + p.Speed + " · Acc " + p.Acceleration + " · Tech " + p.Technique, _tiny);
                GUI.Label(new Rect(62, y + 90, 180, 20), "$" + p.SigningFee, _body);
                if (GUI.Button(new Rect(w - 100, y + 50, 64, 34), "SIGN", _greenButton)) SignProspect(p);
                if (GUI.Button(new Rect(w - 100, y + 94, 64, 28), "PASS", _darkButton)) { _prospects[i] = MakeProspect(); }
                y += 168;
            }
            BottomNav(w, h, 3);
        }

        private void DrawLegends(float w, float h)
        {
            Header(w, "LEGENDS");
            GUI.Label(new Rect(24, 88, 300, 34), "HALL OF FAME", _title);
            if (_legends.Count == 0) GUI.Label(new Rect(24, 144, w - 48, 24), "No retired club legends yet.", _body);
            float y = 140;
            for (int i = 0; i < _legends.Count; i++)
            {
                HallOfFameEntry l = _legends[i];
                Rect card = new Rect(18, y, w - 36, 92);
                GUI.DrawTexture(card, _panel);
                DrawFlag(l.CountryCode, new Rect(30, y + 18, 24, 16));
                GUI.Label(new Rect(62, y + 12, 220, 24), l.Name, _h2);
                GUI.Label(new Rect(62, y + 42, 250, 18), "PB " + l.PersonalBest.ToString("0.00") + " · Wins " + l.Wins + " · Titles " + l.Championships, _tiny);
                y += 102;
            }
            BottomNav(w, h, 4);
        }

        private void DrawPreRace(float w, float h)
        {
            if (_pendingRace == null) { _screen = ScreenState.Season; return; }
            Header(w, "STRATEGY");
            if (GUI.Button(new Rect(18, 82, 74, 30), "‹ BACK", _darkButton)) _screen = ScreenState.Season;
            GUI.Label(new Rect(110, 84, 280, 30), _pendingRace.Event.Name.ToUpperInvariant(), _title);
            EnsurePreview();

            Rect card = new Rect(18, 130, w - 36, 260);
            GUI.DrawTexture(card, _panel);
            float y = 148;
            for (int i = 0; i < _preview.Runners.Count; i++)
            {
                RaceRunner r = _preview.Runners[i];
                if (r.IsPlayer) GUI.DrawTexture(new Rect(28, y - 2, w - 56, 24), _greenDim);
                GUI.Label(new Rect(36, y, 30, 20), (i + 1).ToString(), _body);
                DrawFlag(r.CountryCode, new Rect(70, y + 2, 22, 14));
                GUI.Label(new Rect(100, y, 220, 20), r.Name, _body);
                GUI.Label(new Rect(w - 92, y, 56, 20), r.FinishTime.ToString("0.00"), _right);
                y += 28;
            }

            GUI.Label(new Rect(24, 414, 220, 24), "RACE STRATEGY", _h2);
            RaceStrategy[] strategies = { RaceStrategy.ExplosiveStart, RaceStrategy.Balanced, RaceStrategy.LatePush };
            y = 450;
            for (int i = 0; i < strategies.Length; i++)
            {
                RaceStrategy strategy = strategies[i];
                bool active = _strategy == strategy;
                Rect r = new Rect(24, y, w - 48, 68);
                GUI.DrawTexture(r, active ? _greenDim : _panel2);
                GUI.Label(new Rect(38, y + 8, 250, 24), RaceStrategyInfo.Title(strategy), _h2);
                GUI.Label(new Rect(38, y + 36, 300, 18), RaceStrategyInfo.Description(strategy), _tiny);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { _strategy = strategy; _preview = null; }
                y += 80;
            }
            if (GUI.Button(new Rect(24, h - 80, w - 48, 52), "START RACE", _greenButton)) StartRace();
        }

        private void DrawRace(float w, float h)
        {
            if (_result == null) { _screen = ScreenState.PreRace; return; }
            GUI.Label(new Rect(20, 18, 240, 34), "100M LIVE", _title);
            Rect track = new Rect(18, 80, w - 36, 520);
            GUI.DrawTexture(track, _track);
            float laneH = track.height / 8f;
            float left = track.x + 28f;
            float right = track.xMax - 14f;
            for (int split = 20; split <= 100; split += 20)
            {
                float sx = Mathf.Lerp(left, right, split / 100f);
                GUI.DrawTexture(new Rect(sx, track.y, 1f, track.height), _white);
                GUI.Label(new Rect(sx - 18, track.y - 24, 40, 20), split + "m", _tiny);
            }
            bool alt = Mathf.FloorToInt(_raceElapsed * 10f) % 2 == 0;
            for (int i = 0; i < _result.Runners.Count; i++)
            {
                RaceRunner r = _result.Runners[i];
                float y = track.y + laneH * i;
                if (i > 0) GUI.DrawTexture(new Rect(track.x, y, track.width, 1f), _white);
                float distance = DistanceAtTime(r, _raceElapsed);
                float x = Mathf.Lerp(left, right, distance / 100f);
                GUI.Label(new Rect(track.x + 4, y + 15, 20, 20), (i + 1).ToString(), _tiny);
                Texture2D sprite = r.IsPlayer ? (alt ? _runnerA : _runnerB) : (alt ? _runnerAiA : _runnerAiB);
                GUI.DrawTexture(new Rect(x - 28, y + laneH * 0.5f - 20, 56, 40), sprite, ScaleMode.ScaleToFit, true);
            }
            GUI.DrawTexture(new Rect(track.x, track.yMax, track.width, 14), _grass);
            GUI.Label(new Rect(24, 628, w - 48, 24), "LIVE TOP 3", _h2);
            List<RaceRunner> live = new List<RaceRunner>(_result.Runners);
            live.Sort(delegate(RaceRunner a, RaceRunner b) { return DistanceAtTime(b, _raceElapsed).CompareTo(DistanceAtTime(a, _raceElapsed)); });
            for (int i = 0; i < Mathf.Min(3, live.Count); i++)
                GUI.Label(new Rect(34, 662 + i * 28, w - 68, 22), (i + 1) + ". " + live[i].Name + "   " + DistanceAtTime(live[i], _raceElapsed).ToString("0.0") + "m", _body);
            if (GUI.Button(new Rect(w - 92, h - 56, 68, 30), "SKIP", _darkButton)) _screen = ScreenState.Results;
        }

        private void DrawResults(float w, float h)
        {
            if (_result == null || _pendingRace == null) { _screen = ScreenState.Season; return; }
            GUI.Label(new Rect(24, 44, w - 48, 30), _result.EventName.ToUpperInvariant(), _center);
            GUI.Label(new Rect(24, 96, w - 48, 52), Ordinal(_result.PlayerPlace) + " PLACE", _title);
            GUI.Label(new Rect(24, 170, w - 48, 90), _result.PlayerTime.ToString("0.00") + "s", _title);
            if (_result.NewPersonalBest) GUI.Label(new Rect(24, 270, w - 48, 28), "NEW PERSONAL BEST", _center);
            GUI.Label(new Rect(24, 330, w - 48, 22), "+$" + _result.CashReward + "   +" + _result.ReputationReward + " reputation", _center);
            GUI.Label(new Rect(24, 382, w - 48, 22), _pendingRace.Athlete.DisplayName, _center);
            if (GUI.Button(new Rect(24, h - 82, w - 48, 50), "CLAIM RESULT", _goldButton)) ClaimResult();
        }

        private void StartRace()
        {
            _raceSeed++;
            _result = new RaceSimulator(Environment.TickCount ^ (_raceSeed * 7919)).Simulate(_pendingRace.Athlete, _strategy, _pendingRace.Event, true);
            _raceElapsed = 0f;
            _claimed = false;
            _screen = ScreenState.Race;
        }

        private void ClaimResult()
        {
            if (_claimed || _result == null || _pendingRace == null) return;
            Athlete a = _pendingRace.Athlete;
            a.Races++;
            if (_result.PlayerPlace == 1) a.Wins++;
            if (_pendingRace.Event.IsChampionship && _result.PlayerPlace == 1) a.Championships++;
            a.Fatigue = Mathf.Clamp01(a.Fatigue + 0.09f);
            a.Form = Mathf.Clamp(a.Form + UnityEngine.Random.Range(-0.02f, 0.02f), 0.82f, 1.08f);
            _cash += _result.CashReward;
            _reputation += _result.ReputationReward;
            _pendingRace.Event.Completed = true;
            _claimed = true;
            _pendingRace = null;
            _preview = null;
            _result = null;
            _day++;
            if (_day > 7) CompleteSeason();
            _screen = ScreenState.Season;
        }

        private void AdvanceDay()
        {
            if (CurrentEvent() != null) return;
            for (int i = 0; i < _roster.Count; i++) ApplyTraining(_roster[i]);
            _day++;
            if (_day > 7) CompleteSeason();
        }

        private void CompleteSeason()
        {
            for (int i = _roster.Count - 1; i >= 0; i--)
            {
                Athlete a = _roster[i];
                a.SeasonsCompleted++;
                a.Age++;
                if (a.Age >= 30 && UnityEngine.Random.value < 0.45f) a.Speed = Mathf.Max(40, a.Speed - 1);
                if (a.Age >= 31 && UnityEngine.Random.value < 0.40f) a.Acceleration = Mathf.Max(40, a.Acceleration - 1);
                bool retire = a.Age >= 35 || (a.Age >= 33 && UnityEngine.Random.value < 0.22f);
                if (retire)
                {
                    _legends.Insert(0, new HallOfFameEntry { Name = a.DisplayName, CountryCode = a.CountryCode, RetireAge = a.Age, Races = a.Races, Wins = a.Wins, Championships = a.Championships, PersonalBest = a.PersonalBest });
                    _roster.RemoveAt(i);
                }
                else
                {
                    a.Fatigue *= 0.55f;
                    a.Form = Mathf.Clamp(0.94f + UnityEngine.Random.Range(-0.03f, 0.03f), 0.86f, 1.04f);
                }
            }
            if (_roster.Count == 0) SignProspect(MakeProspect(), true);
            _season++;
            _day = 1;
            BuildSeason();
            RefreshProspects();
            _selected = _roster[0];
        }

        private void ApplyTraining(Athlete a)
        {
            float ageFactor = a.Age <= 20 ? 1.15f : a.Age <= 24 ? 1f : a.Age <= 28 ? 0.7f : 0.35f;
            if (a.Training == TrainingFocus.Recovery)
            {
                a.Fatigue = Mathf.Clamp01(a.Fatigue - 0.20f);
                a.Form = Mathf.Clamp(a.Form + 0.015f, 0.80f, 1.08f);
                return;
            }
            if (a.Training == TrainingFocus.Sprint)
            {
                Grow(ref a.Speed, ref a.SpeedProgress, a.Potential, 0.45f * ageFactor);
                Grow(ref a.Acceleration, ref a.AccelerationProgress, a.Potential, 0.60f * ageFactor);
                a.Fatigue = Mathf.Clamp01(a.Fatigue + 0.10f);
            }
            else if (a.Training == TrainingFocus.Strength)
            {
                Grow(ref a.Strength, ref a.StrengthProgress, a.Potential, 0.55f * ageFactor);
                Grow(ref a.Acceleration, ref a.AccelerationProgress, a.Potential - 3, 0.20f * ageFactor);
                a.Fatigue = Mathf.Clamp01(a.Fatigue + 0.11f);
            }
            else
            {
                Grow(ref a.Technique, ref a.TechniqueProgress, a.Potential, 0.52f * ageFactor);
                Grow(ref a.Mental, ref a.MentalProgress, a.Potential, 0.28f * ageFactor);
                a.Fatigue = Mathf.Clamp01(a.Fatigue + 0.08f);
            }
            a.Fatigue = Mathf.Clamp01(a.Fatigue - 0.03f);
        }

        private void Grow(ref int stat, ref float progress, int cap, float amount)
        {
            if (stat >= cap) return;
            float gap = Mathf.Clamp01((cap - stat) / 18f);
            progress += amount * Mathf.Lerp(0.35f, 1f, gap);
            if (progress >= 1f) { progress -= 1f; stat++; }
        }

        private void SignProspect(Prospect p, bool free = false)
        {
            if (_roster.Count >= 5) return;
            if (!free && _cash < p.SigningFee) return;
            if (!free) _cash -= p.SigningFee;
            Athlete a = new Athlete
            {
                Id = p.Id, FirstName = p.FirstName, LastName = p.LastName, CountryCode = p.CountryCode, Age = p.Age,
                Speed = p.Speed, Acceleration = p.Acceleration, Strength = p.Strength, Technique = p.Technique, Mental = p.Mental,
                Potential = p.Potential, PotentialMin = p.PotentialMin, PotentialMax = p.PotentialMax,
                Form = 0.94f, Fatigue = 0.10f, Training = TrainingFocus.Sprint
            };
            a.PersonalBest = Mathf.Round(Mathf.Clamp(13.70f - a.BaseRating * 0.0445f + UnityEngine.Random.Range(0.02f, 0.12f), 9.80f, 12.80f) * 100f) / 100f;
            _roster.Add(a);
            _selected = a;
            _prospects.Remove(p);
            while (_prospects.Count < 3) _prospects.Add(MakeProspect());
        }

        private void ReleaseSelected()
        {
            if (_selected == null || _roster.Count <= 1) return;
            _roster.Remove(_selected);
            _selected = _roster[0];
            _screen = ScreenState.Team;
        }

        private void SeedRoster()
        {
            _roster.Add(new Athlete { Id = "andre-campbell", FirstName = "Andre", LastName = "Campbell", CountryCode = "JAM", Age = 17, Speed = 72, Acceleration = 82, Strength = 66, Technique = 68, Mental = 71, Form = 0.92f, Fatigue = 0.14f, PersonalBest = 10.72f, Potential = 94, PotentialMin = 82, PotentialMax = 94, Training = TrainingFocus.Sprint });
            _roster.Add(new Athlete { Id = "jakub-zielinski", FirstName = "Jakub", LastName = "Zielinski", CountryCode = "POL", Age = 18, Speed = 69, Acceleration = 74, Strength = 63, Technique = 65, Mental = 70, Form = 0.95f, Fatigue = 0.10f, PersonalBest = 10.88f, Potential = 87, PotentialMin = 74, PotentialMax = 87, Training = TrainingFocus.Technique });
            _roster.Add(new Athlete { Id = "lucas-martin", FirstName = "Lucas", LastName = "Martin", CountryCode = "FRA", Age = 21, Speed = 76, Acceleration = 71, Strength = 70, Technique = 72, Mental = 64, Form = 0.97f, Fatigue = 0.16f, PersonalBest = 10.63f, Potential = 82, PotentialMin = 76, PotentialMax = 82, Training = TrainingFocus.Strength });
        }

        private void BuildSeason()
        {
            _events.Clear();
            _events.Add(new SeasonEvent { Day = 1, Name = "Local Meet", Tier = CompetitionTier.Local, BaseCashReward = 900, BaseReputationReward = 8 });
            _events.Add(new SeasonEvent { Day = 2, Name = "Regional Cup", Tier = CompetitionTier.Regional, BaseCashReward = 1400, BaseReputationReward = 11 });
            _events.Add(new SeasonEvent { Day = 4, Name = "National Qualifier", Tier = CompetitionTier.National, BaseCashReward = 2200, BaseReputationReward = 16 });
            _events.Add(new SeasonEvent { Day = 6, Name = "National Championship", Tier = CompetitionTier.National, IsChampionship = true, BaseCashReward = 3400, BaseReputationReward = 26 });
            _events.Add(new SeasonEvent { Day = 7, Name = "International Final", Tier = CompetitionTier.International, IsChampionship = true, BaseCashReward = 5000, BaseReputationReward = 34 });
        }

        private SeasonEvent CurrentEvent() { return _events.Find(delegate(SeasonEvent e) { return !e.Completed && e.Day == _day; }); }

        private void RefreshProspects() { _prospects.Clear(); for (int i = 0; i < 3; i++) _prospects.Add(MakeProspect()); }

        private Prospect MakeProspect()
        {
            string[] first = { "Samuel", "Elias", "Marcus", "Jaden", "Kofi", "Mateo", "Aiden", "Omar", "Tyrique" };
            string[] last = { "Okafor", "Becker", "Pierre", "Mensah", "Costa", "Johnson", "Smith", "Cole", "Walker" };
            string[] country = { "NGR", "GER", "USA", "CAN", "FRA", "ITA", "JAM", "GBR" };
            int potential = UnityEngine.Random.Range(78, 98);
            int baseStat = UnityEngine.Random.Range(60, Mathf.Min(80, potential));
            return new Prospect
            {
                Id = Guid.NewGuid().ToString("N"), FirstName = first[UnityEngine.Random.Range(0, first.Length)], LastName = last[UnityEngine.Random.Range(0, last.Length)], CountryCode = country[UnityEngine.Random.Range(0, country.Length)], Age = UnityEngine.Random.Range(16, 23),
                Speed = baseStat + UnityEngine.Random.Range(-2, 5), Acceleration = baseStat + UnityEngine.Random.Range(-3, 6), Strength = baseStat + UnityEngine.Random.Range(-5, 3), Technique = baseStat + UnityEngine.Random.Range(-4, 4), Mental = baseStat + UnityEngine.Random.Range(-4, 4),
                Potential = potential, PotentialMin = Mathf.Max(60, potential - UnityEngine.Random.Range(6, 13)), PotentialMax = Mathf.Min(99, potential + UnityEngine.Random.Range(0, 5)), SigningFee = UnityEngine.Random.Range(1800, 4200)
            };
        }

        private void EnsurePreview()
        {
            if (_preview != null) return;
            _preview = new RaceSimulator(_season * 1000 + _day * 73 + _selected.Overall).Simulate(_pendingRace.Athlete, _strategy, _pendingRace.Event, false);
        }

        private float DistanceAtTime(RaceRunner r, float t)
        {
            if (t <= 0f) return 0f;
            if (t >= r.FinishTime) return 100f;
            float[] times = { 0f, r.SplitTimes[0], r.SplitTimes[1], r.SplitTimes[2], r.SplitTimes[3], r.SplitTimes[4] };
            float[] distance = { 0f, 20f, 40f, 60f, 80f, 100f };
            float[] tangent = new float[6];
            tangent[0] = 20f / Mathf.Max(0.001f, times[1] - times[0]);
            tangent[5] = 20f / Mathf.Max(0.001f, times[5] - times[4]);
            for (int i = 1; i < 5; i++)
            {
                float a = 20f / Mathf.Max(0.001f, times[i] - times[i - 1]);
                float b = 20f / Mathf.Max(0.001f, times[i + 1] - times[i]);
                tangent[i] = (a + b) * 0.5f;
            }
            for (int s = 0; s < 5; s++)
            {
                if (t <= times[s + 1])
                {
                    float dt = Mathf.Max(0.001f, times[s + 1] - times[s]);
                    float u = Mathf.Clamp01((t - times[s]) / dt);
                    float u2 = u * u;
                    float u3 = u2 * u;
                    return (2f * u3 - 3f * u2 + 1f) * distance[s] + (u3 - 2f * u2 + u) * tangent[s] * dt + (-2f * u3 + 3f * u2) * distance[s + 1] + (u3 - u2) * tangent[s + 1] * dt;
                }
            }
            return 100f;
        }

        private void Header(float w, string section)
        {
            GUI.Label(new Rect(20, 16, 260, 30), "TRACK DYNASTY", _logo);
            GUI.Label(new Rect(w - 150, 20, 126, 22), section, _right);
        }

        private void BottomNav(float w, float h, int active)
        {
            string[] labels = { "HQ", "TEAM", "SEASON", "SCOUT", "LEGENDS" };
            float cell = w / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                Rect r = new Rect(i * cell, h - 58, cell, 58);
                GUI.DrawTexture(r, i == active ? _greenDim : _panel);
                GUI.Label(r, labels[i], _center);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    if (i == 0) _screen = ScreenState.HQ;
                    else if (i == 1) _screen = ScreenState.Team;
                    else if (i == 2) _screen = ScreenState.Season;
                    else if (i == 3) _screen = ScreenState.Scout;
                    else _screen = ScreenState.Legends;
                }
            }
        }

        private void DrawStat(float w, ref float y, string label, int value)
        {
            GUI.Label(new Rect(24, y, 130, 22), label, _body);
            Rect bar = new Rect(160, y + 7, w - 230, 9);
            GUI.DrawTexture(bar, _muted);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * value / 99f, bar.height), _green);
            GUI.Label(new Rect(w - 60, y, 36, 22), value.ToString(), _right);
            y += 42;
        }

        private void DrawFlag(string code, Rect rect)
        {
            Texture2D tex;
            if (!_flags.TryGetValue(code, out tex)) { tex = BuildFlag(code); _flags[code] = tex; }
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill);
        }

        private void BuildTextures()
        {
            _bg = Solid(new Color(0.02f, 0.035f, 0.055f));
            _panel = Solid(new Color(0.04f, 0.055f, 0.078f));
            _panel2 = Solid(new Color(0.06f, 0.075f, 0.10f));
            _gold = Solid(new Color(0.95f, 0.68f, 0.16f));
            _green = Solid(new Color(0.42f, 0.88f, 0.22f));
            _greenDim = Solid(new Color(0.08f, 0.18f, 0.10f));
            _muted = Solid(new Color(0.18f, 0.22f, 0.28f));
            _white = Solid(Color.white);
            _track = Solid(new Color(0.49f, 0.22f, 0.14f));
            _grass = Solid(new Color(0.05f, 0.20f, 0.11f));
            _runnerA = BuildRunner(new Color(0.42f, 0.88f, 0.22f), false);
            _runnerB = BuildRunner(new Color(0.42f, 0.88f, 0.22f), true);
            _runnerAiA = BuildRunner(new Color(0.74f, 0.77f, 0.82f), false);
            _runnerAiB = BuildRunner(new Color(0.74f, 0.77f, 0.82f), true);
        }

        private Texture2D Solid(Color c)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            for (int y = 0; y < 2; y++) for (int x = 0; x < 2; x++) t.SetPixel(x, y, c);
            t.Apply();
            return t;
        }

        private Texture2D BuildRunner(Color kit, bool alt)
        {
            Texture2D t = new Texture2D(92, 56, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < t.height; y++) for (int x = 0; x < t.width; x++) t.SetPixel(x, y, clear);
            Color skin = new Color(0.45f, 0.28f, 0.18f);
            FillEllipse(t, 60, 41, 6, 6, skin); // head on RIGHT = runner faces right
            DrawLine(t, 55, 36, 43, 25, kit, 7);
            if (!alt)
            {
                DrawLine(t, 52, 33, 72, 25, skin, 3);
                DrawLine(t, 41, 24, 60, 15, skin, 4);
                DrawLine(t, 60, 15, 82, 17, skin, 4);
                DrawLine(t, 41, 24, 28, 38, skin, 4);
                DrawLine(t, 28, 38, 11, 34, skin, 4);
            }
            else
            {
                DrawLine(t, 52, 33, 71, 42, skin, 3);
                DrawLine(t, 41, 24, 60, 36, skin, 4);
                DrawLine(t, 60, 36, 80, 32, skin, 4);
                DrawLine(t, 41, 24, 27, 15, skin, 4);
                DrawLine(t, 27, 15, 10, 18, skin, 4);
            }
            t.Apply();
            return t;
        }

        private Texture2D BuildFlag(string code)
        {
            Texture2D t = new Texture2D(36, 24, TextureFormat.RGBA32, false);
            Color a = new Color(0.05f, 0.25f, 0.50f), b = Color.white, c = new Color(0.85f, 0.08f, 0.10f);
            if (code == "JAM") { a = new Color(0.03f, 0.42f, 0.16f); b = new Color(0.98f, 0.80f, 0.08f); c = Color.black; }
            else if (code == "NGR") { a = new Color(0.03f, 0.45f, 0.18f); b = Color.white; c = a; }
            else if (code == "GER") { a = Color.black; b = new Color(0.8f, 0.05f, 0.08f); c = new Color(0.95f, 0.75f, 0.08f); }
            else if (code == "POL") { a = Color.white; b = Color.white; c = new Color(0.86f, 0.08f, 0.14f); }
            for (int y = 0; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                    t.SetPixel(x, y, y < t.height / 3 ? a : y < 2 * t.height / 3 ? b : c);
            t.Apply();
            return t;
        }

        private void FillEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color color)
        {
            for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) tex.SetPixel(x, y, color);
                }
        }

        private void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                for (int oy = -thickness / 2; oy <= thickness / 2; oy++) for (int ox = -thickness / 2; ox <= thickness / 2; ox++)
                {
                    int px = x0 + ox, py = y0 + oy;
                    if (px >= 0 && py >= 0 && px < tex.width && py < tex.height) tex.SetPixel(px, py, color);
                }
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _logo = Label(28, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            _title = Label(30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            _h2 = Label(20, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            _body = Label(17, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            _small = Label(15, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.8f, 0.84f, 0.9f));
            _tiny = Label(13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.75f, 0.80f, 0.86f));
            _center = Label(16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            _right = Label(14, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
            _goldButton = Button(_gold, new Color(0.08f, 0.06f, 0.02f));
            _greenButton = Button(_green, new Color(0.03f, 0.08f, 0.03f));
            _darkButton = Button(_panel2, Color.white);
            _stylesReady = true;
        }

        private GUIStyle Label(int size, FontStyle style, TextAnchor anchor, Color color)
        {
            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.fontSize = size; s.fontStyle = style; s.alignment = anchor; s.normal.textColor = color;
            return s;
        }

        private GUIStyle Button(Texture2D background, Color textColor)
        {
            GUIStyle s = new GUIStyle(GUI.skin.button);
            s.fontSize = 15; s.fontStyle = FontStyle.Bold; s.normal.background = background; s.active.background = background; s.normal.textColor = textColor; s.active.textColor = textColor;
            return s;
        }

        private string Ordinal(int n) { if (n == 1) return "1ST"; if (n == 2) return "2ND"; if (n == 3) return "3RD"; return n + "TH"; }
    }
}
