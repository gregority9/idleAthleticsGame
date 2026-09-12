using System.Collections.Generic;
using System.Text;
using TrackDynasty.Mvp03.Domain;
using TrackDynasty.Mvp03.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class AthleteScreen : GameScreen
    {
        private static readonly DistanceType[] Distances = { DistanceType.M100, DistanceType.M200, DistanceType.M400, DistanceType.M800, DistanceType.M1500 };

        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            Athlete athlete = Manager.GetSelectedAthlete();
            if (athlete == null)
            {
                UIFactory.Text(Content, "No athlete selected.", 20, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 60f);
                return;
            }

            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            Transform hero = UIFactory.Horizontal(stack, 10f, 64f);
            Image flag = UIFactory.Panel(hero, Color.white, "Flag");
            flag.sprite = FlagSpriteFactory.Get(athlete.CountryCode);
            flag.preserveAspect = true;
            UIFactory.SetPreferredWidth(flag, 56f);
            Transform heroText = UIFactory.Vertical(hero, 0f, 0, "HeroText");
            UIFactory.SetFlexibleWidth(heroText.GetComponent<RectTransform>());
            UIFactory.Text(heroText, athlete.DisplayName, 23, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 32f);
            UIFactory.Text(heroText, athlete.CountryCode + " · Age " + athlete.Age + " · " + athlete.Category + " · OVR " + athlete.Overall, 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);

            Transform metrics = UIFactory.Horizontal(stack, 7f, 62f);
            AddMetric(metrics, "FORM", Mathf.RoundToInt(athlete.Form * 100f) + "%", UITheme.Green);
            AddMetric(metrics, "FATIGUE", Mathf.RoundToInt(athlete.Fatigue * 100f) + "%", UITheme.Text);
            AddMetric(metrics, "POTENTIAL", athlete.PotentialMin + "–" + athlete.PotentialMax, UITheme.Gold);

            if (athlete.InjuryWeeks > 0)
                UIFactory.Text(stack, "INJURED · " + athlete.InjuryWeeks + " week(s) remaining. Training is replaced by recovery.", 13, TextAnchor.MiddleLeft, UITheme.Red, FontStyle.Bold, 34f);

            UIFactory.Text(stack, "DISTANCE RATINGS & PERSONAL BESTS", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            for (int i = 0; i < Distances.Length; i++) AddDistanceRow(stack, athlete, Distances[i]);

            UIFactory.Text(stack, "ATTRIBUTES", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            AddAttribute(stack, "Speed", athlete.Speed);
            AddAttribute(stack, "Acceleration", athlete.Acceleration);
            AddAttribute(stack, "Strength", athlete.Strength);
            AddAttribute(stack, "Endurance", athlete.Endurance);
            AddAttribute(stack, "Technique", athlete.Technique);
            AddAttribute(stack, "Mental", athlete.Mental);

            UIFactory.Text(stack, "TRAINING DISTANCE", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            Transform distanceButtons = UIFactory.Horizontal(stack, 5f, 40f);
            for (int i = 0; i < Distances.Length; i++) AddDistanceButton(distanceButtons, athlete, Distances[i]);

            UIFactory.Text(stack, "TRAINING FOCUS", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            Transform focusA = UIFactory.Horizontal(stack, 5f, 38f);
            AddFocusButton(focusA, athlete, TrainingFocus.Speed, "SPD");
            AddFocusButton(focusA, athlete, TrainingFocus.Acceleration, "ACC");
            AddFocusButton(focusA, athlete, TrainingFocus.Strength, "STR");
            AddFocusButton(focusA, athlete, TrainingFocus.Endurance, "END");
            Transform focusB = UIFactory.Horizontal(stack, 5f, 38f);
            AddFocusButton(focusB, athlete, TrainingFocus.Technique, "TECH");
            AddFocusButton(focusB, athlete, TrainingFocus.Mental, "MENT");
            AddFocusButton(focusB, athlete, TrainingFocus.Recovery, "RECOVERY");

            CoachProfile coach = Manager.GetCoach(athlete);
            UIFactory.Text(stack, "COACH", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            UIFactory.Text(stack, coach == null ? "Manual training · no coach assigned yet. Coach hiring/assignment is prepared in the data model for the next progression layer." : coach.Name + " · primary " + DomainLabels.Distance(coach.PrimaryDistance) + " · capacity " + coach.Capacity, 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 40f);

            UIFactory.Text(stack, "COMPETITION PLAN", 15, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 24f);
            CompetitionMeet scheduled = Manager.ScheduledMeet(athlete);
            if (scheduled != null)
            {
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 126f, "ScheduledMeet");
                Transform inner = UIFactory.Vertical(card.transform, 3f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                DistanceType distance = athlete.ScheduledCompetition.Distance;
                UIFactory.Text(inner, scheduled.Week.ShortLabel + " · " + scheduled.Name, 16, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 25f);
                UIFactory.Text(inner, scheduled.City + " · " + DomainLabels.Range(scheduled.Range) + " · " + DomainLabels.Distance(distance) + " · entry already paid", 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 21f);
                UIFactory.Text(inner, "Expected winner " + PerformanceModel.FormatTime(CompetitionSystem.ExpectedWinningTime(scheduled, athlete.Category, distance)) + " · average " + PerformanceModel.FormatTime(CompetitionSystem.ExpectedAverageTime(scheduled, athlete.Category, distance)), 12, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 22f);
                if (scheduled.Week.IsSame(Manager.State.CurrentWeek))
                    UIFactory.Button(inner, "ENTER RACE", () => Controller.OpenRacePrep(athlete), UITheme.Gold, 34f);
                else
                    UIFactory.Button(inner, "CANCEL ENTRY", () => Manager.CancelScheduledCompetition(athlete), UITheme.PanelAlt, 34f);
            }
            else
            {
                List<CompetitionMeet> meets = Manager.UpcomingMeets(athlete, 8);
                for (int i = 0; i < meets.Count; i++) AddMeetCard(stack, athlete, meets[i]);
            }

            UIFactory.Text(stack, "CAREER", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            UIFactory.Text(stack, "Races " + athlete.CareerRaces + " · Wins " + athlete.CareerWins + " · Sponsor interest " + Mathf.RoundToInt(athlete.SponsorInterest), 13, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 24f);
            int historyStart = Mathf.Max(0, athlete.RaceHistory.Count - 8);
            for (int i = athlete.RaceHistory.Count - 1; i >= historyStart; i--)
            {
                RaceHistoryEntry entry = athlete.RaceHistory[i];
                string badges = (entry.PersonalBest ? " PB" : "") + (entry.ClubRecord ? " CR" : "");
                UIFactory.Text(stack, "W" + entry.Week.ToString("00") + " " + entry.Year + " · " + DomainLabels.Distance(entry.Distance) + " · " + entry.EventName + " · P" + entry.Place + " · " + PerformanceModel.FormatTime(entry.Time) + badges, 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
            }
        }

        private void AddDistanceRow(Transform parent, Athlete athlete, DistanceType distance)
        {
            Transform row = UIFactory.Horizontal(parent, 7f, 30f);
            Text name = UIFactory.Text(row, DomainLabels.Distance(distance), 14, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 30f);
            UIFactory.SetPreferredWidth(name, 58f);
            Text rating = UIFactory.Text(row, "RATING " + athlete.DistanceRating(distance), 13, TextAnchor.MiddleLeft, UITheme.Green, FontStyle.Bold, 30f);
            UIFactory.SetFlexibleWidth(rating);
            DistanceRecord record = athlete.GetRecord(distance);
            Text pb = UIFactory.Text(row, record.HasPersonalBest ? "PB " + PerformanceModel.FormatTime(record.PersonalBest) : "PB —", 13, TextAnchor.MiddleRight, record.HasPersonalBest ? UITheme.Gold : UITheme.Muted, FontStyle.Bold, 30f);
            UIFactory.SetPreferredWidth(pb, 110f);
        }

        private void AddAttribute(Transform parent, string label, int value)
        {
            Transform row = UIFactory.Horizontal(parent, 8f, 26f);
            Text name = UIFactory.Text(row, label, 13, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 26f);
            UIFactory.SetFlexibleWidth(name);
            Text score = UIFactory.Text(row, value.ToString(), 13, TextAnchor.MiddleRight, UITheme.Green, FontStyle.Bold, 26f);
            UIFactory.SetPreferredWidth(score, 44f);
        }

        private void AddMetric(Transform parent, string label, string value, Color color)
        {
            Image card = UIFactory.Panel(parent, UITheme.Panel, "Metric");
            Transform stack = UIFactory.Vertical(card.transform, 0f, 4, "MetricStack");
            UIFactory.Stretch(stack.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(stack, label, 10, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 18f);
            UIFactory.Text(stack, value, 16, TextAnchor.MiddleCenter, color, FontStyle.Bold, 30f);
        }

        private void AddDistanceButton(Transform parent, Athlete athlete, DistanceType distance)
        {
            bool active = athlete.TrainingDistance == distance;
            Button button = UIFactory.Button(parent, ((int)distance).ToString(), () => Manager.SetTrainingDistance(athlete, distance), active ? UITheme.Green : UITheme.PanelAlt, 40f);
            UIFactory.SetFlexibleWidth(button);
        }

        private void AddFocusButton(Transform parent, Athlete athlete, TrainingFocus focus, string label)
        {
            bool active = athlete.TrainingFocus == focus;
            Button button = UIFactory.Button(parent, label, () => Manager.SetTrainingFocus(athlete, focus), active ? UITheme.Green : UITheme.PanelAlt, 38f);
            UIFactory.SetFlexibleWidth(button);
        }

        private void AddMeetCard(Transform parent, Athlete athlete, CompetitionMeet meet)
        {
            Image card = UIFactory.FixedPanel(parent, UITheme.Panel, 176f, "MeetCard");
            Transform inner = UIFactory.Vertical(card.transform, 2f, 9, "MeetInner");
            UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(inner, meet.Week.ShortLabel + " · " + meet.Name, 15, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 24f);
            UIFactory.Text(inner, meet.City + " · " + DomainLabels.Range(meet.Range) + (meet.IsChampionship ? " · CHAMPIONSHIP" : "") + " · fee $" + meet.EntryFee, 12, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 20f);
            UIFactory.Text(inner, "Field rating approx. " + Mathf.Max(1, meet.FieldStrength - meet.FieldSpread) + "–" + Mathf.Min(99, meet.FieldStrength + meet.FieldSpread) + " · required rating " + CompetitionSystem.RequiredRating(meet.Range), 11, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 20f);
            UIFactory.Text(inner, ExpectedTimes(meet, athlete.Category), 10, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 38f);
            Transform buttons = UIFactory.Horizontal(inner, 4f, 34f);
            for (int i = 0; i < Distances.Length; i++)
            {
                DistanceType distance = Distances[i];
                bool canEnter = CompetitionSystem.CanEnter(athlete, meet, distance) && Manager.State.Cash >= meet.EntryFee;
                Button button = UIFactory.Button(buttons, ((int)distance).ToString(), () => Manager.ScheduleCompetition(athlete, meet, distance), canEnter ? UITheme.Green : UITheme.PanelAlt, 34f, canEnter);
                UIFactory.SetFlexibleWidth(button);
            }
        }

        private string ExpectedTimes(CompetitionMeet meet, AgeCategory category)
        {
            StringBuilder sb = new StringBuilder("AVG ");
            for (int i = 0; i < Distances.Length; i++)
            {
                if (i > 0) sb.Append(" · ");
                DistanceType d = Distances[i];
                sb.Append((int)d).Append(" ").Append(PerformanceModel.FormatTime(CompetitionSystem.ExpectedAverageTime(meet, category, d)));
            }
            return sb.ToString();
        }
    }

    public class CalendarScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 12);
            UIFactory.Text(stack, "COMPETITION CALENDAR " + Manager.State.CurrentWeek.Year, 26, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 40f);
            UIFactory.Text(stack, "Meets are global. You choose which athlete enters which distance; age category, geography and required rating determine eligibility.", 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 44f);

            for (int week = Manager.State.CurrentWeek.Week; week <= 52; week++)
            {
                List<CompetitionMeet> meets = Manager.State.CompetitionCalendar.FindAll(m => m.Week != null && m.Week.Year == Manager.State.CurrentWeek.Year && m.Week.Week == week);
                if (meets.Count == 0) continue;
                UIFactory.Text(stack, "WEEK " + week.ToString("00") + (week == Manager.State.CurrentWeek.Week ? " · CURRENT" : ""), 14, TextAnchor.MiddleLeft, week == Manager.State.CurrentWeek.Week ? UITheme.Green : UITheme.Muted, FontStyle.Bold, 24f);
                for (int i = 0; i < meets.Count; i++)
                {
                    CompetitionMeet meet = meets[i];
                    Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 82f, "CalendarMeet");
                    Transform inner = UIFactory.Vertical(card.transform, 2f, 9, "Inner");
                    UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                    UIFactory.Text(inner, meet.Name + " · " + meet.City, 14, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 22f);
                    UIFactory.Text(inner, DomainLabels.Range(meet.Range) + " · categories " + Categories(meet.AllowedCategories) + " · field " + Mathf.Max(1, meet.FieldStrength - meet.FieldSpread) + "–" + Mathf.Min(99, meet.FieldStrength + meet.FieldSpread), 11, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
                    UIFactory.Text(inner, "100 / 200 / 400 / 800 / 1500m · entry $" + meet.EntryFee, 11, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 20f);
                }
            }
        }

        private string Categories(List<AgeCategory> categories)
        {
            if (categories == null || categories.Count == 0) return "—";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < categories.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(categories[i]);
            }
            return sb.ToString();
        }
    }
}
