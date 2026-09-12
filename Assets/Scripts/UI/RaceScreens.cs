using System.Collections.Generic;
using System.Text;
using TrackDynasty.Mvp03.Domain;
using TrackDynasty.Mvp03.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class RacePrepScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            Athlete athlete = Manager.ActiveAthlete;
            CompetitionMeet competition = Manager.ActiveCompetition;
            if (athlete == null || competition == null)
            {
                UIFactory.Text(Content, "No race selected.", 20, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 60f);
                UIFactory.Button(Content, "BACK", () => Controller.Navigate(ScreenId.HQ), UITheme.PanelAlt, 42f);
                return;
            }

            DistanceType distance = Manager.ActiveDistance;
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 20);
            UIFactory.Button(stack, "‹ BACK", () => Controller.Navigate(ScreenId.HQ), UITheme.PanelAlt, 38f);
            UIFactory.Text(stack, DomainLabels.Distance(distance) + " · RACE PREP", 30, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 48f);
            UIFactory.Text(stack, competition.Name.ToUpperInvariant(), 21, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 36f);
            UIFactory.Text(stack, competition.Week.ShortLabel + " · " + competition.City + " · " + DomainLabels.Range(competition.Range) + " · " + athlete.Category, 13, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Normal, 28f);

            Image athleteCard = UIFactory.FixedPanel(stack, UITheme.Panel, 112f, "AthleteCard");
            Transform row = UIFactory.Horizontal(athleteCard.transform, 10f, 112f);
            UIFactory.Stretch(row.GetComponent<RectTransform>(), 12, 12, 0, 0);
            Image flag = UIFactory.Panel(row, Color.white, "Flag");
            flag.sprite = FlagSpriteFactory.Get(athlete.CountryCode);
            flag.preserveAspect = true;
            UIFactory.SetPreferredWidth(flag, 62f);
            DistanceRecord record = athlete.GetRecord(distance);
            string pb = record.HasPersonalBest ? PerformanceModel.FormatTime(record.PersonalBest) : "—";
            Text info = UIFactory.Text(row, athlete.DisplayName + "\nRating " + athlete.DistanceRating(distance) + " · PB " + pb + "\nForm " + Mathf.RoundToInt(athlete.Form * 100f) + "% · Fatigue " + Mathf.RoundToInt(athlete.Fatigue * 100f) + "%", 14, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 88f);
            UIFactory.SetFlexibleWidth(info);

            UIFactory.Text(stack, "FIELD EXPECTATION", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 25f);
            UIFactory.Text(stack, "Expected winner: " + PerformanceModel.FormatTime(CompetitionSystem.ExpectedWinningTime(competition, athlete.Category, distance)) + " · average field: " + PerformanceModel.FormatTime(CompetitionSystem.ExpectedAverageTime(competition, athlete.Category, distance)), 13, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 34f);

            UIFactory.Text(stack, "RACE STRATEGY", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 25f);
            AddStrategy(stack, RaceStrategy.FastStart, "Fast Start", (int)distance <= 400 ? "Attack early. Better for acceleration-heavy athletes." : "Push the opening phase harder, with a higher risk of fading later.");
            AddStrategy(stack, RaceStrategy.Balanced, "Balanced", "Lowest tactical risk. Uses the athlete's normal race profile.");
            AddStrategy(stack, RaceStrategy.LateKick, "Late Kick", (int)distance >= 400 ? "Save more for the finish. Better for endurance and mental strength." : "Hold slightly more back for the final phase.");

            UIFactory.Button(stack, "START " + DomainLabels.Distance(distance).ToUpperInvariant(), () => { Manager.StartRace(); Controller.OpenRace(); }, UITheme.Green, 56f);
        }

        private void AddStrategy(Transform parent, RaceStrategy strategy, string title, string description)
        {
            bool active = Manager.ActiveStrategy == strategy;
            Image card = UIFactory.FixedPanel(parent, active ? UITheme.GreenDark : UITheme.Panel, 82f, "StrategyCard");
            Transform inner = UIFactory.Vertical(card.transform, 2f, 10, "Inner");
            UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(inner, title.ToUpperInvariant() + (active ? "  ✓" : ""), 15, TextAnchor.MiddleLeft, active ? UITheme.Green : UITheme.Text, FontStyle.Bold, 24f);
            UIFactory.Text(inner, description, 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 28f);
            UIFactory.Button(inner, active ? "SELECTED" : "SELECT", () => { Manager.ActiveStrategy = strategy; Refresh(); }, active ? UITheme.Green : UITheme.PanelAlt, 28f);
        }
    }

    public class RaceScreen : GameScreen
    {
        private class Marker
        {
            public RaceRunner Runner;
            public RectTransform Rect;
        }

        private readonly List<Marker> _markers = new List<Marker>();
        private RectTransform _trackRect;
        private Text _clockText;
        private Text _leaderboardText;
        private Text _photoFinishText;
        private float _realElapsed;
        private float _simulationSpeed = 1f;
        private bool _completed;

        public override void Show()
        {
            _realElapsed = 0f;
            _completed = false;
            base.Show();
        }

        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            RaceResult result = Manager.CurrentRaceResult;
            if (result == null)
            {
                UIFactory.Text(Content, "Race result is not ready.", 20, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 60f);
                return;
            }

            float maxTime = 0f;
            for (int i = 0; i < result.Runners.Count; i++) maxTime = Mathf.Max(maxTime, result.Runners[i].FinishTime);
            _simulationSpeed = Mathf.Max(1f, maxTime / 14f);

            Transform root = UIFactory.Vertical(Content, 8f, 12, "RaceRoot");
            UIFactory.Stretch(root.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(root, DomainLabels.Distance(result.Distance) + " · LIVE", 28, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 40f);
            UIFactory.Text(root, result.EventName + " · " + result.City + " · " + result.Week.ShortLabel, 13, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Normal, 24f);
            _clockText = UIFactory.Text(root, "0.00s", 23, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 34f);

            Image track = UIFactory.FixedPanel(root, UITheme.Track, 500f, "Track");
            _trackRect = track.rectTransform;
            BuildTrack(track.transform, result);

            _photoFinishText = UIFactory.Text(root, "", 17, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 28f);
            _leaderboardText = UIFactory.Text(root, "LIVE TOP 3", 13, TextAnchor.UpperLeft, UITheme.Text, FontStyle.Normal, 92f);
            UIFactory.Button(root, "SKIP TO RESULTS", Controller.OpenResults, UITheme.PanelAlt, 38f);
        }

        private void BuildTrack(Transform track, RaceResult result)
        {
            _markers.Clear();
            float laneHeight = 500f / 8f;
            for (int lane = 0; lane < 8; lane++)
            {
                GameObject lineGo = UIFactory.CreateRect("LaneLine", track);
                Image line = lineGo.AddComponent<Image>();
                line.color = UITheme.LaneLine;
                RectTransform rt = line.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -lane * laneHeight);
                rt.sizeDelta = new Vector2(0f, 2f);
            }

            for (int s = 1; s <= 5; s++)
            {
                float ratio = s * 0.20f;
                GameObject splitGo = UIFactory.CreateRect("Split", track);
                Image split = splitGo.AddComponent<Image>();
                split.color = s == 5 ? UITheme.Gold : new Color(1f, 1f, 1f, 0.30f);
                RectTransform rt = split.rectTransform;
                rt.anchorMin = new Vector2(ratio, 0f);
                rt.anchorMax = new Vector2(ratio, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(s == 5 ? 3f : 1f, 0f);
            }

            for (int i = 0; i < result.Runners.Count; i++)
            {
                RaceRunner runner = result.Runners[i];
                GameObject markerGo = UIFactory.CreateRect("Runner_" + runner.Lane, track);
                Image marker = markerGo.AddComponent<Image>();
                marker.sprite = FlagSpriteFactory.Get(runner.CountryCode);
                marker.preserveAspect = true;
                if (runner.IsPlayer)
                {
                    Outline outline = markerGo.AddComponent<Outline>();
                    outline.effectColor = UITheme.Green;
                    outline.effectDistance = new Vector2(3f, -3f);
                }
                RectTransform rt = marker.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(runner.IsPlayer ? 46f : 40f, runner.IsPlayer ? 46f : 40f);
                rt.anchoredPosition = new Vector2(30f, -(runner.Lane - 0.5f) * laneHeight);
                _markers.Add(new Marker { Runner = runner, Rect = rt });
            }
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || Manager == null || Manager.CurrentRaceResult == null || _trackRect == null || _completed) return;
            _realElapsed += Time.deltaTime;
            RaceResult result = Manager.CurrentRaceResult;
            float raceTime = _realElapsed * _simulationSpeed;
            float width = Mathf.Max(200f, _trackRect.rect.width);
            float totalDistance = (float)(int)result.Distance;

            for (int i = 0; i < _markers.Count; i++)
            {
                Marker marker = _markers[i];
                float distance = RaceSimulator.DistanceAtTime(marker.Runner, result.Distance, raceTime);
                Vector2 pos = marker.Rect.anchoredPosition;
                pos.x = Mathf.Lerp(30f, width - 24f, distance / totalDistance);
                marker.Rect.anchoredPosition = pos;
            }

            float finalTime = result.Standings[result.Standings.Count - 1].FinishTime;
            _clockText.text = PerformanceModel.FormatTime(Mathf.Min(raceTime, finalTime));
            _leaderboardText.text = BuildLiveLeaderboard(result, raceTime);
            if (result.PhotoFinish && raceTime >= result.Standings[0].FinishTime - 0.05f) _photoFinishText.text = "PHOTO FINISH";
            else _photoFinishText.text = "";

            if (raceTime >= finalTime && _realElapsed >= finalTime / _simulationSpeed + 0.8f)
            {
                _completed = true;
                Controller.OpenResults();
            }
        }

        private string BuildLiveLeaderboard(RaceResult result, float raceTime)
        {
            List<RaceRunner> runners = new List<RaceRunner>(result.Runners);
            runners.Sort((a, b) =>
            {
                float da = RaceSimulator.DistanceAtTime(a, result.Distance, raceTime);
                float db = RaceSimulator.DistanceAtTime(b, result.Distance, raceTime);
                int compare = db.CompareTo(da);
                return compare != 0 ? compare : a.FinishTime.CompareTo(b.FinishTime);
            });

            StringBuilder sb = new StringBuilder("LIVE TOP 3\n");
            for (int i = 0; i < Mathf.Min(3, runners.Count); i++)
            {
                RaceRunner runner = runners[i];
                float distance = RaceSimulator.DistanceAtTime(runner, result.Distance, raceTime);
                sb.Append(i + 1).Append(". ").Append(runner.CountryCode).Append("  ").Append(runner.Name).Append("   ").Append(distance.ToString("0")).Append("m");
                if (i < 2) sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    public class ResultsScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            RaceResult result = Manager.CurrentRaceResult;
            Athlete athlete = Manager.ActiveAthlete;
            if (result == null || athlete == null)
            {
                UIFactory.Text(Content, "No race result available.", 20, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 60f);
                return;
            }

            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            UIFactory.Text(stack, result.EventName.ToUpperInvariant(), 18, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 30f);
            UIFactory.Text(stack, DomainLabels.Distance(result.Distance) + " · " + Ordinal(result.PlayerPlace) + " PLACE", 28, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 44f);
            UIFactory.Text(stack, PerformanceModel.FormatTime(result.PlayerTime), 32, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 48f);
            if (result.PhotoFinish) UIFactory.Text(stack, "PHOTO FINISH", 16, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 26f);

            string badges = "";
            if (result.NewPersonalBest) badges += "PB  ";
            if (result.NewClubRecord) badges += "CR";
            if (!string.IsNullOrEmpty(badges)) UIFactory.Text(stack, badges.Trim(), 16, TextAnchor.MiddleCenter, UITheme.Green, FontStyle.Bold, 26f);

            UIFactory.Text(stack, "FULL RESULTS", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            float winner = result.Standings[0].FinishTime;
            for (int i = 0; i < result.Standings.Count; i++)
            {
                RaceRunner runner = result.Standings[i];
                string gap = i == 0 ? "WINNER" : "+" + PerformanceModel.FormatTime(runner.FinishTime - winner);
                UIFactory.Text(stack, (i + 1) + ".  " + runner.CountryCode + "  " + runner.Name + "   " + PerformanceModel.FormatTime(runner.FinishTime) + "   " + gap, 12, TextAnchor.MiddleLeft, runner.IsPlayer ? UITheme.Green : UITheme.Text, runner.IsPlayer ? FontStyle.Bold : FontStyle.Normal, 24f);
            }

            Transform rewards = UIFactory.Horizontal(stack, 8f, 64f);
            AddReward(rewards, "CASH", "+$" + result.CashReward, UITheme.Gold);
            AddReward(rewards, "REPUTATION", "+" + result.ReputationReward, UITheme.Green);
            AddReward(rewards, "SPONSOR", "+" + Mathf.RoundToInt(result.SponsorInterestGain), UITheme.Text);

            UIFactory.Button(stack, "CLAIM RESULT", () =>
            {
                Athlete completed = Manager.ActiveAthlete;
                Manager.ClaimRaceResult();
                Controller.OpenAthlete(completed);
            }, UITheme.Green, 52f);
        }

        private void AddReward(Transform parent, string label, string value, Color color)
        {
            Image card = UIFactory.Panel(parent, UITheme.Panel, "Reward");
            Transform inner = UIFactory.Vertical(card.transform, 0f, 4, "Inner");
            UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(inner, label, 10, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 18f);
            UIFactory.Text(inner, value, 17, TextAnchor.MiddleCenter, color, FontStyle.Bold, 30f);
        }

        private string Ordinal(int place)
        {
            if (place == 1) return "1ST";
            if (place == 2) return "2ND";
            if (place == 3) return "3RD";
            return place + "TH";
        }
    }
}
