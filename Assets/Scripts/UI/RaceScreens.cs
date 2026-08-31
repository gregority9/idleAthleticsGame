using TrackDynasty.Mvp03.Domain;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Systems;
using System;
using System.Text;

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class RacePrepScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            Athlete athlete = Manager.ActiveAthlete;
            CompetitionOffer competition = Manager.ActiveCompetition;
            if (athlete == null || competition == null)
            {
                UIFactory.Text(Content, "No race selected.", 20, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 60f);
                UIFactory.Button(Content, "BACK", () => Controller.Navigate(ScreenId.HQ), UITheme.PanelAlt, 42f);
                return;
            }

            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 20);
            UIFactory.Button(stack, "‹ BACK", () => Controller.Navigate(ScreenId.HQ), UITheme.PanelAlt, 38f);
            UIFactory.Text(stack, "RACE PREP", 30, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 48f);
            UIFactory.Text(stack, competition.Name.ToUpperInvariant(), 22, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 38f);
            UIFactory.Text(stack, competition.Date.LongLabel + " · " + competition.City + " · " + competition.Tier, 14, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Normal, 30f);

            Image athleteCard = UIFactory.FixedPanel(stack, UITheme.Panel, 100f, "AthleteCard");
            Transform row = UIFactory.Horizontal(athleteCard.transform, 10f, 100f);
            UIFactory.Stretch(row.GetComponent<RectTransform>(), 12, 12, 0, 0);
            Image flag = UIFactory.Panel(row, Color.white, "Flag");
            flag.sprite = FlagSpriteFactory.Get(athlete.CountryCode);
            flag.preserveAspect = true;
            UIFactory.SetPreferredWidth(flag, 64f);
            Text info = UIFactory.Text(row, athlete.DisplayName + "\nPB " + athlete.PersonalBest.ToString("0.00") + "s · Form " + Mathf.RoundToInt(athlete.Form * 100f) + "% · Fatigue " + Mathf.RoundToInt(athlete.Fatigue * 100f) + "%", 15, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 82f);
            UIFactory.SetFlexibleWidth(info);

            UIFactory.Text(stack, "STRATEGY", 16, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 28f);
            AddStrategy(stack, RaceStrategy.ExplosiveStart, "Explosive Start", "Attack the first 40m. Best for acceleration-heavy athletes.");
            AddStrategy(stack, RaceStrategy.Balanced, "Balanced", "Stable race profile with the lowest tactical risk.");
            AddStrategy(stack, RaceStrategy.LatePush, "Late Push", "Save more for the final 40m. Best for strong finishers.");

            UIFactory.Button(stack, "START 100M", () => { Manager.StartRace(); Controller.OpenRace(); }, UITheme.Green, 58f);
        }

        private void AddStrategy(Transform parent, RaceStrategy strategy, string title, string description)
        {
            bool active = Manager.ActiveStrategy == strategy;
            Image card = UIFactory.FixedPanel(parent, active ? UITheme.GreenDark : UITheme.Panel, 86f, "StrategyCard");
            Transform inner = UIFactory.Vertical(card.transform, 2f, 10, "Inner");
            UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(inner, title.ToUpperInvariant() + (active ? "  ✓" : ""), 16, TextAnchor.MiddleLeft, active ? UITheme.Green : UITheme.Text, FontStyle.Bold, 26f);
            UIFactory.Text(inner, description, 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 30f);
            UIFactory.Button(inner, active ? "SELECTED" : "SELECT", () => { Manager.ActiveStrategy = strategy; Refresh(); }, active ? UITheme.Green : UITheme.PanelAlt, 30f);
        }
    }
}

namespace TrackDynasty.Mvp03.UI.Screens
{
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
        private float _elapsed;
        private bool _completed;

        public override void Show()
        {
            _elapsed = 0f;
            _completed = false;
            base.Show();
        }

        public override void Refresh()
        {
            Rebuild();
        }

        protected override void Build()
        {
            RaceResult result = Manager.CurrentRaceResult;
            if (result == null)
            {
                UIFactory.Text(Content, "Race result is not ready.", 20, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 60f);
                return;
            }

            Transform root = UIFactory.Vertical(Content, 8f, 12, "RaceRoot");
            UIFactory.Stretch(root.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(root, "100M · LIVE", 28, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 40f);
            UIFactory.Text(root, result.EventName + " · " + result.City + " · " + result.Date.ShortLabel, 14, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Normal, 26f);
            _clockText = UIFactory.Text(root, "0.00s", 24, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 34f);

            Image track = UIFactory.FixedPanel(root, UITheme.Track, 520f, "Track");
            _trackRect = track.rectTransform;
            BuildTrack(track.transform, result);

            _photoFinishText = UIFactory.Text(root, "", 18, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 30f);
            _leaderboardText = UIFactory.Text(root, "LIVE LEADERBOARD", 14, TextAnchor.UpperLeft, UITheme.Text, FontStyle.Normal, 98f);
            UIFactory.Button(root, "SKIP TO RESULTS", () => Controller.OpenResults(), UITheme.PanelAlt, 38f);
        }

        private void BuildTrack(Transform track, RaceResult result)
        {
            _markers.Clear();
            float laneHeight = 520f / 8f;

            for (int lane = 0; lane < 8; lane++)
            {
                GameObject lineGo = UIFactory.CreateRect("LaneLine", track);
                Image line = lineGo.AddComponent<Image>();
                line.color = UITheme.LaneLine;
                RectTransform lineRt = line.rectTransform;
                lineRt.anchorMin = new Vector2(0f, 1f);
                lineRt.anchorMax = new Vector2(1f, 1f);
                lineRt.pivot = new Vector2(0.5f, 1f);
                lineRt.anchoredPosition = new Vector2(0f, -lane * laneHeight);
                lineRt.sizeDelta = new Vector2(0f, 2f);

                Text laneLabel = UIFactory.Text(track, (lane + 1).ToString(), 12, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 22f);
                RectTransform labelRt = laneLabel.rectTransform;
                labelRt.anchorMin = new Vector2(0f, 1f);
                labelRt.anchorMax = new Vector2(0f, 1f);
                labelRt.pivot = new Vector2(0.5f, 0.5f);
                labelRt.anchoredPosition = new Vector2(14f, -(lane + 0.5f) * laneHeight);
                labelRt.sizeDelta = new Vector2(24f, 22f);
            }

            for (int s = 1; s <= 5; s++)
            {
                float ratio = s * 0.20f;
                GameObject splitGo = UIFactory.CreateRect("Split", track);
                Image split = splitGo.AddComponent<Image>();
                split.color = s == 5 ? UITheme.Gold : new Color(1f, 1f, 1f, 0.32f);
                RectTransform splitRt = split.rectTransform;
                splitRt.anchorMin = new Vector2(ratio, 0f);
                splitRt.anchorMax = new Vector2(ratio, 1f);
                splitRt.pivot = new Vector2(0.5f, 0.5f);
                splitRt.anchoredPosition = Vector2.zero;
                splitRt.sizeDelta = new Vector2(s == 5 ? 3f : 1f, 0f);
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
                rt.sizeDelta = new Vector2(runner.IsPlayer ? 48f : 42f, runner.IsPlayer ? 48f : 42f);
                rt.anchoredPosition = new Vector2(34f, -(runner.Lane - 0.5f) * laneHeight);
                _markers.Add(new Marker { Runner = runner, Rect = rt });
            }
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || Manager == null || Manager.CurrentRaceResult == null || _trackRect == null || _completed) return;
            _elapsed += Time.deltaTime;
            RaceResult result = Manager.CurrentRaceResult;
            float width = Mathf.Max(200f, _trackRect.rect.width);
            float startX = 34f;
            float finishX = width - 24f;

            for (int i = 0; i < _markers.Count; i++)
            {
                Marker marker = _markers[i];
                float distance = RaceSimulator.DistanceAtTime(marker.Runner, _elapsed);
                Vector2 pos = marker.Rect.anchoredPosition;
                pos.x = Mathf.Lerp(startX, finishX, distance / 100f);
                marker.Rect.anchoredPosition = pos;
            }

            _clockText.text = Mathf.Min(_elapsed, result.Standings[result.Standings.Count - 1].FinishTime).ToString("0.00") + "s";
            _leaderboardText.text = BuildLiveLeaderboard(result);

            float winnerTime = result.Standings[0].FinishTime;
            if (result.PhotoFinish && _elapsed >= winnerTime - 0.05f)
                _photoFinishText.text = "PHOTO FINISH · ≤ 0.03s";
            else
                _photoFinishText.text = "";

            float max = 0f;
            for (int i = 0; i < result.Runners.Count; i++) max = Mathf.Max(max, result.Runners[i].FinishTime);
            float hold = result.PhotoFinish ? 1.45f : 0.80f;
            if (_elapsed >= max + hold)
            {
                _completed = true;
                Controller.OpenResults();
            }
        }

        private string BuildLiveLeaderboard(RaceResult result)
        {
            List<RaceRunner> runners = new List<RaceRunner>(result.Runners);
            runners.Sort((a, b) =>
            {
                float da = RaceSimulator.DistanceAtTime(a, _elapsed);
                float db = RaceSimulator.DistanceAtTime(b, _elapsed);
                int compare = db.CompareTo(da);
                return compare != 0 ? compare : a.FinishTime.CompareTo(b.FinishTime);
            });

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("LIVE TOP 3");
            for (int i = 0; i < Mathf.Min(3, runners.Count); i++)
            {
                RaceRunner r = runners[i];
                float distance = RaceSimulator.DistanceAtTime(r, _elapsed);
                sb.Append(i + 1).Append(". ").Append(r.CountryCode).Append("  ").Append(r.Name).Append("   ").Append(distance.ToString("0.0")).Append("m");
                if (i < 2) sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}

namespace TrackDynasty.Mvp03.UI.Screens
{
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
            UIFactory.Text(stack, Ordinal(result.PlayerPlace) + " PLACE · " + result.PlayerTime.ToString("0.00") + "s", 32, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 50f);
            if (result.PhotoFinish)
                UIFactory.Text(stack, "PHOTO FINISH", 17, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 28f);

            string badges = "";
            if (result.NewPersonalBest) badges += "PB  ";
            if (result.NewClubRecord) badges += "CR  ";
            if (result.NewWorldRecord) badges += "WR";
            if (!string.IsNullOrEmpty(badges))
                UIFactory.Text(stack, badges.Trim(), 16, TextAnchor.MiddleCenter, UITheme.Green, FontStyle.Bold, 26f);

            UIFactory.Text(stack, "FULL RESULTS", 16, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 26f);
            AddHeader(stack);
            float winner = result.Standings[0].FinishTime;
            for (int i = 0; i < result.Standings.Count; i++)
            {
                RaceRunner runner = result.Standings[i];
                string runnerBadges = "";
                if (runner.IsPlayer)
                {
                    if (result.NewPersonalBest) runnerBadges += " PB";
                    if (result.NewClubRecord) runnerBadges += " CR";
                    if (result.NewWorldRecord) runnerBadges += " WR";
                }
                AddResultRow(stack, i + 1, runner, winner, runnerBadges);
            }

            Transform rewards = UIFactory.Horizontal(stack, 8f, 64f);
            AddReward(rewards, "CASH", "+$" + result.CashReward, UITheme.Gold);
            AddReward(rewards, "REPUTATION", "+" + result.ReputationReward, UITheme.Green);

            UIFactory.Text(stack, "After claiming, " + athlete.FirstName + " will receive several new competition options to choose from.", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 42f);
            UIFactory.Button(stack, "CLAIM RESULT & CHOOSE NEXT EVENT", () =>
            {
                Athlete completedAthlete = Manager.ActiveAthlete;
                Manager.ClaimRaceResult();
                Controller.OpenAthlete(completedAthlete);
            }, UITheme.Green, 54f);
        }

        private void AddHeader(Transform parent)
        {
            Transform row = UIFactory.Horizontal(parent, 4f, 30f);
            AddCell(row, "POS", 38f, UITheme.Muted, true);
            AddCell(row, "LN", 30f, UITheme.Muted, true);
            AddCell(row, "NAT", 44f, UITheme.Muted, true);
            AddCell(row, "ATHLETE", -1f, UITheme.Muted, true);
            AddCell(row, "TIME", 62f, UITheme.Muted, true);
            AddCell(row, "GAP", 62f, UITheme.Muted, true);
        }

        private void AddResultRow(Transform parent, int place, RaceRunner runner, float winner, string badges)
        {
            Image background = UIFactory.FixedPanel(parent, runner.IsPlayer ? UITheme.GreenDark : UITheme.Panel, 42f, "ResultRow");
            Transform row = UIFactory.Horizontal(background.transform, 4f, 42f);
            UIFactory.Stretch(row.GetComponent<RectTransform>(), 4, 4, 0, 0);
            AddCell(row, place.ToString(), 38f, runner.IsPlayer ? UITheme.Green : UITheme.Text, true);
            AddCell(row, runner.Lane.ToString(), 30f, UITheme.Muted, false);

            Image flag = UIFactory.Panel(row, Color.white, "Flag");
            flag.sprite = FlagSpriteFactory.Get(runner.CountryCode);
            flag.preserveAspect = true;
            UIFactory.SetPreferredWidth(flag, 34f);

            Text athlete = UIFactory.Text(row, runner.Name + badges, 12, TextAnchor.MiddleLeft, runner.IsPlayer ? UITheme.Green : UITheme.Text, runner.IsPlayer ? FontStyle.Bold : FontStyle.Normal, 40f);
            UIFactory.SetFlexibleWidth(athlete);
            AddCell(row, runner.FinishTime.ToString("0.00"), 62f, UITheme.Text, true);
            string gap = place == 1 ? "—" : "+" + (runner.FinishTime - winner).ToString("0.00");
            AddCell(row, gap, 62f, UITheme.Muted, false);
        }

        private void AddCell(Transform parent, string text, float width, Color color, bool bold)
        {
            Text t = UIFactory.Text(parent, text, 12, TextAnchor.MiddleCenter, color, bold ? FontStyle.Bold : FontStyle.Normal, 30f);
            if (width > 0f) UIFactory.SetPreferredWidth(t, width); else UIFactory.SetFlexibleWidth(t);
        }

        private void AddReward(Transform parent, string label, string value, Color color)
        {
            Image card = UIFactory.Panel(parent, UITheme.Panel, "Reward");
            Transform inner = UIFactory.Vertical(card.transform, 0f, 4, "Inner");
            UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(inner, label, 11, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 20f);
            UIFactory.Text(inner, value, 20, TextAnchor.MiddleCenter, color, FontStyle.Bold, 34f);
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
