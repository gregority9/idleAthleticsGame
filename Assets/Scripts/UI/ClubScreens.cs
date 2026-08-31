using TrackDynasty.Mvp03.Domain;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Systems;
using System;
using System.Text;

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class ScoutChoiceScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            Image bg = UIFactory.Panel(Content, UITheme.Background, "SetupBackground");
            UIFactory.Stretch(bg.rectTransform, 0, 0, 0, 0);
            Transform stack = UIFactory.Vertical(Content, 12f, 20, "SetupStack");
            RectTransform rt = stack.GetComponent<RectTransform>();
            UIFactory.Stretch(rt, 0, 0, 30, 30);

            UIFactory.Text(stack, "TRACK DYNASTY", 32, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 50f);
            UIFactory.Text(stack, "MVP 0.3 · BUILD YOUR FIRST CLUB", 14, TextAnchor.MiddleCenter, UITheme.Green, FontStyle.Bold, 30f);
            UIFactory.Text(stack, "You start with one athlete: Andre Campbell. Choose the scout who will shape your recruitment pipeline.", 17, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Normal, 60f);

            for (int i = 0; i < Manager.State.StartingScoutChoices.Count; i++)
            {
                ScoutProfile scout = Manager.State.StartingScoutChoices[i];
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 150f, "ScoutChoiceCard");
                Transform cardStack = UIFactory.Vertical(card.transform, 4f, 12, "CardStack");
                UIFactory.Stretch(cardStack.GetComponent<RectTransform>(), 0, 0, 0, 0);
                Transform nameRow = UIFactory.Horizontal(cardStack, 8f, 34f);
                Image flag = UIFactory.Panel(nameRow, Color.white, "Flag");
                flag.sprite = FlagSpriteFactory.Get(scout.CountryCode);
                flag.preserveAspect = true;
                UIFactory.SetPreferredWidth(flag, 34f);
                Text name = UIFactory.Text(nameRow, scout.Name, 19, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 34f);
                UIFactory.SetFlexibleWidth(name);
                UIFactory.Text(cardStack, scout.Specialty.ToString().ToUpperInvariant() + " · $" + scout.MonthlySalary + "/month", 14, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 24f);
                UIFactory.Text(cardStack, "Evaluation " + scout.Evaluation + "/5 · Network " + scout.Network + "/5", 14, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
                UIFactory.Text(cardStack, scout.Description, 14, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 38f);
                UIFactory.Button(cardStack, "CHOOSE " + scout.Name.ToUpperInvariant(), () => { Manager.ChooseScout(scout); Controller.Navigate(ScreenId.HQ); }, UITheme.Green, 38f);
            }
        }
    }
}

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class HQScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            UIFactory.Text(stack, "CAREER HQ", 28, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 42f);
            UIFactory.Text(stack, Manager.State.CurrentDate.LongLabel, 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 26f);

            List<Athlete> today = Manager.AthletesRacingToday();
            if (today.Count > 0)
            {
                UIFactory.Text(stack, "RACE DAY", 16, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 26f);
                for (int i = 0; i < today.Count; i++)
                {
                    Athlete athlete = today[i];
                    Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 92f, "TodayRace");
                    Transform row = UIFactory.Horizontal(card.transform, 8f, 92f);
                    UIFactory.Stretch(row.GetComponent<RectTransform>(), 10, 10, 0, 0);
                    Image flag = UIFactory.Panel(row, Color.white, "Flag");
                    flag.sprite = FlagSpriteFactory.Get(athlete.CountryCode);
                    flag.preserveAspect = true;
                    UIFactory.SetPreferredWidth(flag, 48f);
                    Text info = UIFactory.Text(row, athlete.DisplayName + "\n" + athlete.ScheduledCompetition.Name + " · " + athlete.ScheduledCompetition.City, 15, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 74f);
                    UIFactory.SetFlexibleWidth(info);
                    Button enter = UIFactory.Button(row, "ENTER", () => Controller.OpenRacePrep(athlete), UITheme.Gold, 46f);
                    UIFactory.SetPreferredWidth(enter, 86f);
                }
            }
            else
            {
                UIFactory.Text(stack, "No competition today. Train or move the calendar forward.", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 38f);
                Transform row = UIFactory.Horizontal(stack, 8f, 46f);
                UIFactory.Button(row, "ADVANCE DAY", Manager.AdvanceOneDay, UITheme.PanelAlt, 46f);
                UIFactory.Button(row, "NEXT EVENT", Manager.AdvanceToNextCompetition, UITheme.Green, 46f);
            }

            Athlete selected = Manager.GetSelectedAthlete();
            if (selected != null)
            {
                UIFactory.Text(stack, "FOCUS ATHLETE", 16, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 26f);
                Image athleteCard = UIFactory.FixedPanel(stack, UITheme.Panel, 118f, "FocusAthlete");
                Transform inner = UIFactory.Vertical(athleteCard.transform, 3f, 12, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                UIFactory.Text(inner, selected.DisplayName + " · " + selected.CountryCode, 20, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 28f);
                UIFactory.Text(inner, "PB " + selected.PersonalBest.ToString("0.00") + "s · OVR " + selected.Overall + " · Potential " + selected.PotentialMin + "–" + selected.PotentialMax, 14, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 24f);
                string next = selected.ScheduledCompetition == null ? "No event scheduled" : selected.ScheduledCompetition.Date.ShortLabel + " · " + selected.ScheduledCompetition.Name;
                UIFactory.Text(inner, next, 14, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 22f);
                UIFactory.Button(inner, "OPEN ATHLETE", () => Controller.OpenAthlete(selected), UITheme.PanelAlt, 34f);
            }

            Transform metrics = UIFactory.Horizontal(stack, 8f, 72f);
            AddMetric(metrics, "ROSTER", Manager.State.Roster.Count.ToString());
            AddMetric(metrics, "CLUB RECORD", Manager.State.ClubRecord100m.ToString("0.00") + "s");
            AddMetric(metrics, "APPLICATIONS", Manager.State.Applications.Count.ToString());

            UIFactory.Text(stack, "RECORDS", 16, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 26f);
            UIFactory.Text(stack, "Club: " + Manager.State.ClubRecord100m.ToString("0.00") + "s — " + Manager.State.ClubRecordHolder + "\nWorld: " + Manager.State.WorldRecord100m.ToString("0.00") + "s", 15, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 48f);

            Transform utilities = UIFactory.Horizontal(stack, 8f, 40f);
            UIFactory.Button(utilities, "SAVE", Manager.SaveGame, UITheme.PanelAlt, 40f);
            UIFactory.Button(utilities, "LOAD", Manager.LoadGame, UITheme.PanelAlt, 40f);
            UIFactory.Button(utilities, "LEGENDS", Controller.OpenHallOfFame, UITheme.PanelAlt, 40f);
            UIFactory.Button(stack, "RESET SAVE", () => { Manager.ResetGame(); Controller.Navigate(ScreenId.ScoutChoice); }, UITheme.Red, 40f);
        }

        private void AddMetric(Transform parent, string label, string value)
        {
            Image card = UIFactory.Panel(parent, UITheme.Panel, "Metric");
            Transform stack = UIFactory.Vertical(card.transform, 0f, 6, "MetricStack");
            UIFactory.Stretch(stack.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(stack, label, 11, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 22f);
            UIFactory.Text(stack, value, 18, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 34f);
        }
    }
}

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class TeamScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            UIFactory.Text(stack, "TEAM", 28, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 42f);
            UIFactory.Text(stack, "Each athlete has an independent training focus and competition calendar.", 14, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 40f);

            for (int i = 0; i < Manager.State.Roster.Count; i++)
            {
                Athlete athlete = Manager.State.Roster[i];
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 126f, "AthleteCard");
                Transform inner = UIFactory.Vertical(card.transform, 4f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                Transform row = UIFactory.Horizontal(inner, 8f, 38f);
                Image flag = UIFactory.Panel(row, Color.white, "Flag");
                flag.sprite = FlagSpriteFactory.Get(athlete.CountryCode);
                flag.preserveAspect = true;
                UIFactory.SetPreferredWidth(flag, 36f);
                Text name = UIFactory.Text(row, athlete.DisplayName, 18, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 36f);
                UIFactory.SetFlexibleWidth(name);
                Text ovr = UIFactory.Text(row, "OVR " + athlete.Overall, 15, TextAnchor.MiddleRight, UITheme.Green, FontStyle.Bold, 36f);
                UIFactory.SetPreferredWidth(ovr, 70f);
                UIFactory.Text(inner, "Age " + athlete.Age + " · PB " + athlete.PersonalBest.ToString("0.00") + "s · Potential " + athlete.PotentialMin + "–" + athlete.PotentialMax, 14, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 24f);
                string scheduled = athlete.ScheduledCompetition == null ? "Needs next competition" : athlete.ScheduledCompetition.Date.ShortLabel + " · " + athlete.ScheduledCompetition.Name;
                UIFactory.Text(inner, scheduled, 13, TextAnchor.MiddleLeft, athlete.ScheduledCompetition == null ? UITheme.Gold : UITheme.Text, FontStyle.Bold, 22f);
                UIFactory.Button(inner, "MANAGE " + athlete.FirstName.ToUpperInvariant(), () => Controller.OpenAthlete(athlete), UITheme.PanelAlt, 34f);
            }
        }
    }
}
