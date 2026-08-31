using TrackDynasty.Mvp03.Domain;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Systems;
using System;
using System.Text;

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class AthleteScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            Athlete athlete = Manager.GetSelectedAthlete();
            if (athlete == null)
            {
                UIFactory.Text(Content, "No athlete selected.", 20, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 60f);
                return;
            }

            CompetitionSystem.EnsureOffers(athlete, Manager.State.CurrentDate, Manager.State.Reputation);
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);

            Transform hero = UIFactory.Horizontal(stack, 10f, 72f);
            Image flag = UIFactory.Panel(hero, Color.white, "Flag");
            flag.sprite = FlagSpriteFactory.Get(athlete.CountryCode);
            flag.preserveAspect = true;
            UIFactory.SetPreferredWidth(flag, 64f);
            Transform heroText = UIFactory.Vertical(hero, 0f, 0, "HeroText");
            UIFactory.SetFlexibleWidth(heroText.GetComponent<RectTransform>());
            UIFactory.Text(heroText, athlete.DisplayName, 24, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 34f);
            UIFactory.Text(heroText, athlete.CountryCode + " · Age " + athlete.Age + " · OVR " + athlete.Overall, 14, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 24f);

            Transform metrics = UIFactory.Horizontal(stack, 8f, 64f);
            AddMetric(metrics, "PB", athlete.PersonalBest.ToString("0.00") + "s", UITheme.Gold);
            AddMetric(metrics, "FORM", Mathf.RoundToInt(athlete.Form * 100f) + "%", UITheme.Green);
            AddMetric(metrics, "FATIGUE", Mathf.RoundToInt(athlete.Fatigue * 100f) + "%", UITheme.Text);

            UIFactory.Text(stack, "ATTRIBUTES", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            AddAttribute(stack, "Speed", athlete.Speed);
            AddAttribute(stack, "Acceleration", athlete.Acceleration);
            AddAttribute(stack, "Strength", athlete.Strength);
            AddAttribute(stack, "Technique", athlete.Technique);
            AddAttribute(stack, "Mental", athlete.Mental);
            UIFactory.Text(stack, "Visible potential estimate: " + athlete.PotentialMin + "–" + athlete.PotentialMax + ". Exact potential remains hidden.", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 38f);

            UIFactory.Text(stack, "TRAITS", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            string traits = athlete.Traits == null || athlete.Traits.Count == 0 ? "No special traits" : string.Join(" · ", athlete.Traits.ConvertAll(t => TraitLabel(t)).ToArray());
            UIFactory.Text(stack, traits, 14, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 40f);

            UIFactory.Text(stack, "TRAINING FOCUS", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            Transform training = UIFactory.Horizontal(stack, 6f, 42f);
            AddTrainingButton(training, athlete, TrainingFocus.Sprint);
            AddTrainingButton(training, athlete, TrainingFocus.Strength);
            AddTrainingButton(training, athlete, TrainingFocus.Technique);
            AddTrainingButton(training, athlete, TrainingFocus.Recovery);

            UIFactory.Text(stack, "COMPETITION PLAN", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            if (athlete.ScheduledCompetition != null)
            {
                CompetitionOffer scheduled = athlete.ScheduledCompetition;
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 106f, "ScheduledCard");
                Transform inner = UIFactory.Vertical(card.transform, 3f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                UIFactory.Text(inner, scheduled.Date.LongLabel + " · " + scheduled.Name, 16, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 28f);
                UIFactory.Text(inner, scheduled.City + " · " + scheduled.Tier + " · " + CompetitionSystem.QualificationText(scheduled.Tier), 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 24f);
                if (scheduled.Date.IsSameDay(Manager.State.CurrentDate))
                    UIFactory.Button(inner, "ENTER RACE", () => Controller.OpenRacePrep(athlete), UITheme.Gold, 38f);
                else
                    UIFactory.Text(inner, "Scheduled in " + DaysUntil(scheduled.Date) + " day(s)", 13, TextAnchor.MiddleLeft, UITheme.Green, FontStyle.Bold, 22f);
            }
            else
            {
                UIFactory.Text(stack, "Choose where this athlete should race next:", 14, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 26f);
                for (int i = 0; i < athlete.CompetitionOffers.Count; i++)
                {
                    CompetitionOffer offer = athlete.CompetitionOffers[i];
                    bool canEnter = CompetitionSystem.CanEnter(athlete, offer);
                    Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 112f, "OfferCard");
                    Transform inner = UIFactory.Vertical(card.transform, 3f, 10, "OfferInner");
                    UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                    UIFactory.Text(inner, offer.Date.ShortLabel + " · " + offer.Name, 16, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 26f);
                    UIFactory.Text(inner, offer.City + " · " + offer.Tier + (offer.IsChampionship ? " · CHAMPIONSHIP" : ""), 13, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 22f);
                    UIFactory.Text(inner, CompetitionSystem.QualificationText(offer.Tier) + " · Reward up to $" + offer.CashReward, 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
                    UIFactory.Button(inner, canEnter ? "SCHEDULE" : "NOT QUALIFIED", () => Manager.ScheduleCompetition(athlete, offer), canEnter ? UITheme.Green : UITheme.PanelAlt, 34f, canEnter);
                }
            }

            UIFactory.Text(stack, "CAREER", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            UIFactory.Text(stack, "Races " + athlete.Races + " · Wins " + athlete.Wins + " · Championships " + athlete.Championships, 14, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 24f);
            UIFactory.Text(stack, "PB progression: " + BuildPbSparkline(athlete), 14, TextAnchor.MiddleLeft, UITheme.Green, FontStyle.Bold, 28f);

            int start = Mathf.Max(0, athlete.RaceHistory.Count - 8);
            for (int i = athlete.RaceHistory.Count - 1; i >= start; i--)
            {
                RaceHistoryEntry entry = athlete.RaceHistory[i];
                string badges = (entry.PersonalBest ? " PB" : "") + (entry.ClubRecord ? " CR" : "") + (entry.WorldRecord ? " WR" : "");
                UIFactory.Text(stack, entry.Day.ToString("00") + "/" + entry.Month.ToString("00") + "/" + entry.Year + " · " + entry.EventName + " · P" + entry.Place + " · " + entry.Time.ToString("0.00") + "s" + badges, 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 24f);
            }
        }

        private void AddMetric(Transform parent, string label, string value, Color color)
        {
            Image card = UIFactory.Panel(parent, UITheme.Panel, "Metric");
            Transform stack = UIFactory.Vertical(card.transform, 0f, 4, "Stack");
            UIFactory.Stretch(stack.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(stack, label, 11, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 20f);
            UIFactory.Text(stack, value, 18, TextAnchor.MiddleCenter, color, FontStyle.Bold, 30f);
        }

        private void AddAttribute(Transform parent, string label, int value)
        {
            Transform row = UIFactory.Horizontal(parent, 8f, 28f);
            Text name = UIFactory.Text(row, label, 14, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 28f);
            UIFactory.SetFlexibleWidth(name);
            Text score = UIFactory.Text(row, value.ToString(), 14, TextAnchor.MiddleRight, UITheme.Green, FontStyle.Bold, 28f);
            UIFactory.SetPreferredWidth(score, 50f);
        }

        private void AddTrainingButton(Transform parent, Athlete athlete, TrainingFocus focus)
        {
            bool active = athlete.TrainingFocus == focus;
            Button b = UIFactory.Button(parent, ShortTraining(focus), () => Manager.SetTraining(athlete, focus), active ? UITheme.Green : UITheme.PanelAlt, 42f);
            UIFactory.SetFlexibleWidth(b);
        }

        private string ShortTraining(TrainingFocus focus)
        {
            if (focus == TrainingFocus.Sprint) return "SPRINT";
            if (focus == TrainingFocus.Strength) return "STR";
            if (focus == TrainingFocus.Technique) return "TECH";
            return "REC";
        }

        private int DaysUntil(GameDate date)
        {
            return Mathf.Max(0, (int)(date.ToDateTime() - Manager.State.CurrentDate.ToDateTime()).TotalDays);
        }

        private string TraitLabel(TraitType trait)
        {
            switch (trait)
            {
                case TraitType.ExplosiveStarter: return "Explosive Starter";
                case TraitType.StrongFinisher: return "Strong Finisher";
                case TraitType.BigStagePerformer: return "Big Stage Performer";
                case TraitType.FastLearner: return "Fast Learner";
                case TraitType.InjuryProne: return "Injury Prone";
                case TraitType.LateBloomer: return "Late Bloomer";
                case TraitType.Consistent: return "Consistent";
                case TraitType.Volatile: return "Volatile";
                default: return trait.ToString();
            }
        }

        private string BuildPbSparkline(Athlete athlete)
        {
            List<float> values = new List<float>();
            if (athlete.SeasonHistory != null)
            {
                for (int i = 0; i < athlete.SeasonHistory.Count; i++)
                    if (athlete.SeasonHistory[i].PbAtEnd < 90f) values.Add(athlete.SeasonHistory[i].PbAtEnd);
            }
            if (athlete.PersonalBest < 90f) values.Add(athlete.PersonalBest);
            if (values.Count == 0) return "—";
            float min = values[0];
            float max = values[0];
            for (int i = 1; i < values.Count; i++) { min = Mathf.Min(min, values[i]); max = Mathf.Max(max, values[i]); }
            string[] blocks = { "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█" };
            string output = "";
            for (int i = 0; i < values.Count; i++)
            {
                float normalized = max - min < 0.001f ? 1f : 1f - Mathf.InverseLerp(min, max, values[i]);
                int index = Mathf.Clamp(Mathf.RoundToInt(normalized * 7f), 0, 7);
                output += blocks[index];
            }
            return output + "  " + athlete.PersonalBest.ToString("0.00") + "s";
        }
    }
}

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class CalendarScreen : GameScreen
    {
        private int _selectedMonth;

        public override void Initialize(TrackDynasty.Mvp03.Core.GameManager manager, MainUIController controller)
        {
            base.Initialize(manager, controller);
            _selectedMonth = manager.State.CurrentDate.Month;
        }

        public override void Refresh()
        {
            if (_selectedMonth < 1 || _selectedMonth > 12) _selectedMonth = Manager.State.CurrentDate.Month;
            Rebuild();
        }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 12);
            UIFactory.Text(stack, "CALENDAR " + Manager.State.CurrentDate.Year, 28, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 42f);
            UIFactory.Text(stack, "Every athlete schedules competitions independently. The calendar covers the full January–December season.", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 44f);

            GameObject gridGo = UIFactory.CreateRect("MonthGrid", stack);
            LayoutElement gridLe = gridGo.AddComponent<LayoutElement>();
            gridLe.preferredHeight = 174f;
            gridLe.minHeight = 174f;
            GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(91f, 36f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(0, 0, 4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            for (int month = 1; month <= 12; month++)
            {
                int captured = month;
                string label = new DateTime(Manager.State.CurrentDate.Year, month, 1).ToString("MMM").ToUpperInvariant();
                UIFactory.Button(gridGo.transform, label, () => { _selectedMonth = captured; Refresh(); }, month == _selectedMonth ? UITheme.Green : UITheme.PanelAlt, 36f);
            }

            string monthName = new DateTime(Manager.State.CurrentDate.Year, _selectedMonth, 1).ToString("MMMM").ToUpperInvariant();
            UIFactory.Text(stack, monthName, 20, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 32f);

            int days = DateTime.DaysInMonth(Manager.State.CurrentDate.Year, _selectedMonth);
            for (int day = 1; day <= days; day++)
            {
                GameDate date = new GameDate(Manager.State.CurrentDate.Year, _selectedMonth, day);
                List<Athlete> athletes = EventsOnDate(date);
                bool today = date.IsSameDay(Manager.State.CurrentDate);
                float height = Mathf.Max(46f, 34f + athletes.Count * 26f);
                Image card = UIFactory.FixedPanel(stack, today ? UITheme.GreenDark : UITheme.Panel, height, "CalendarDay");
                Transform inner = UIFactory.Vertical(card.transform, 1f, 8, "DayInner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                string weekday = date.ToDateTime().ToString("ddd");
                UIFactory.Text(inner, day.ToString("00") + " " + weekday + (today ? " · TODAY" : ""), 13, TextAnchor.MiddleLeft, today ? UITheme.Green : UITheme.Muted, FontStyle.Bold, 20f);
                if (athletes.Count == 0)
                {
                    UIFactory.Text(inner, "Training / recovery day", 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 18f);
                }
                else
                {
                    for (int i = 0; i < athletes.Count; i++)
                    {
                        Athlete athlete = athletes[i];
                        CompetitionOffer ev = athlete.ScheduledCompetition;
                        UIFactory.Text(inner, athlete.DisplayName + " — " + ev.Name + " · " + ev.City + " · " + ev.Tier, 12, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 22f);
                    }
                }
            }
        }

        private List<Athlete> EventsOnDate(GameDate date)
        {
            List<Athlete> list = new List<Athlete>();
            for (int i = 0; i < Manager.State.Roster.Count; i++)
            {
                Athlete athlete = Manager.State.Roster[i];
                if (athlete.ScheduledCompetition != null && athlete.ScheduledCompetition.Date != null && athlete.ScheduledCompetition.Date.IsSameDay(date))
                    list.Add(athlete);
            }
            return list;
        }
    }
}
