using TrackDynasty.Mvp03.Domain;
using TrackDynasty.Mvp03.Systems;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class SetupScreen : GameScreen
    {
        private InputField _managementName;

        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 18);
            UIFactory.Text(stack, "TRACK DYNASTY", 34, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold, 52f);
            UIFactory.Text(stack, "START YOUR MANAGEMENT", 15, TextAnchor.MiddleCenter, UITheme.Green, FontStyle.Bold, 28f);
            UIFactory.Text(stack, "It is Week 01/52 of 2026. You have $5,000 and must choose one of five 8-year-old Polish athletes to build your career around.", 15, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Normal, 66f);

            UIFactory.Text(stack, "MANAGEMENT NAME", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            _managementName = BuildInput(stack, "e.g. Baltic Athletics Management");

            UIFactory.Text(stack, "CHOOSE YOUR FIRST ATHLETE", 16, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 28f);
            for (int i = 0; i < Manager.State.StarterChoices.Count; i++)
            {
                AthleteCandidate candidate = Manager.State.StarterChoices[i];
                Athlete preview = candidate.ToAthlete();
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 178f, "StarterCard");
                Transform inner = UIFactory.Vertical(card.transform, 3f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);

                Transform header = UIFactory.Horizontal(inner, 8f, 36f);
                Image flag = UIFactory.Panel(header, Color.white, "Flag");
                flag.sprite = FlagSpriteFactory.Get("POL");
                flag.preserveAspect = true;
                UIFactory.SetPreferredWidth(flag, 34f);
                Text name = UIFactory.Text(header, candidate.DisplayName + " · AGE 8", 17, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 34f);
                UIFactory.SetFlexibleWidth(name);
                Text overall = UIFactory.Text(header, "OVR " + preview.Overall, 15, TextAnchor.MiddleRight, UITheme.Green, FontStyle.Bold, 34f);
                UIFactory.SetPreferredWidth(overall, 68f);

                UIFactory.Text(inner, "Potential " + candidate.PotentialMin + "–" + candidate.PotentialMax + " · exact potential hidden", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
                UIFactory.Text(inner, Ratings(preview), 13, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 24f);
                UIFactory.Text(inner, "SPD " + candidate.Speed + " · ACC " + candidate.Acceleration + " · STR " + candidate.Strength + " · END " + candidate.Endurance + " · TECH " + candidate.Technique + " · MENT " + candidate.Mental, 11, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 28f);
                UIFactory.Button(inner, "START WITH " + candidate.FirstName.ToUpperInvariant(), () =>
                {
                    string managementName = _managementName != null ? _managementName.text : "";
                    if (Manager.CompleteSetup(managementName, candidate)) Controller.Navigate(ScreenId.HQ);
                }, UITheme.Green, 36f);
            }
        }

        private InputField BuildInput(Transform parent, string placeholder)
        {
            GameObject go = UIFactory.CreateRect("ManagementNameInput", parent);
            Image image = go.AddComponent<Image>();
            image.color = UITheme.PanelAlt;
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 48f;
            layout.minHeight = 48f;
            InputField input = go.AddComponent<InputField>();
            input.targetGraphic = image;
            input.characterLimit = 32;

            Text value = UIFactory.Text(go.transform, "", 15, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 48f);
            UIFactory.Stretch(value.rectTransform, 12, 12, 0, 0);
            Text hint = UIFactory.Text(go.transform, placeholder, 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Italic, 48f);
            UIFactory.Stretch(hint.rectTransform, 12, 12, 0, 0);
            input.textComponent = value;
            input.placeholder = hint;
            return input;
        }

        private string Ratings(Athlete athlete)
        {
            return "100m " + athlete.DistanceRating(DistanceType.M100) + "   200m " + athlete.DistanceRating(DistanceType.M200) + "   400m " + athlete.DistanceRating(DistanceType.M400) + "   800m " + athlete.DistanceRating(DistanceType.M800) + "   1500m " + athlete.DistanceRating(DistanceType.M1500);
        }
    }

    public class HQScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            UIFactory.Text(stack, "MANAGEMENT HQ", 28, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 42f);
            UIFactory.Text(stack, Manager.State.CurrentWeek.Label, 14, TextAnchor.MiddleLeft, UITheme.Green, FontStyle.Bold, 24f);

            List<Athlete> racing = Manager.AthletesRacingThisWeek();
            if (racing.Count > 0)
            {
                UIFactory.Text(stack, "RACE WEEK", 16, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 26f);
                for (int i = 0; i < racing.Count; i++)
                {
                    Athlete athlete = racing[i];
                    CompetitionMeet meet = Manager.ScheduledMeet(athlete);
                    Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 88f, "RaceWeekCard");
                    Transform inner = UIFactory.Vertical(card.transform, 2f, 10, "Inner");
                    UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                    UIFactory.Text(inner, athlete.DisplayName + " · " + DomainLabels.Distance(athlete.ScheduledCompetition.Distance), 16, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 24f);
                    UIFactory.Text(inner, meet.Name + " · " + meet.City + " · " + DomainLabels.Range(meet.Range), 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 20f);
                    UIFactory.Button(inner, "ENTER EVENT", () => Controller.OpenRacePrep(athlete), UITheme.Gold, 32f);
                }
                UIFactory.Text(stack, "Resolve every scheduled race this week before advancing.", 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 28f);
            }
            else
            {
                UIFactory.Text(stack, "No unresolved races this week. Advancing applies one week of training, recovery, finances and recruitment events.", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 46f);
                UIFactory.Button(stack, "ADVANCE TO NEXT WEEK", Manager.AdvanceOneWeek, UITheme.Green, 46f);
            }

            Athlete selected = Manager.GetSelectedAthlete();
            if (selected != null)
            {
                UIFactory.Text(stack, "FOCUS ATHLETE", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 128f, "FocusAthlete");
                Transform inner = UIFactory.Vertical(card.transform, 3f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                UIFactory.Text(inner, selected.DisplayName + " · " + selected.Category + " · OVR " + selected.Overall, 19, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 28f);
                UIFactory.Text(inner, "Training: " + DomainLabels.Distance(selected.TrainingDistance) + " / " + selected.TrainingFocus + " · Potential " + selected.PotentialMin + "–" + selected.PotentialMax, 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 24f);
                UIFactory.Text(inner, "Sponsor interest " + Mathf.RoundToInt(selected.SponsorInterest) + " · Form " + Mathf.RoundToInt(selected.Form * 100f) + "% · Fatigue " + Mathf.RoundToInt(selected.Fatigue * 100f) + "%", 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
                UIFactory.Button(inner, "MANAGE ATHLETE", () => Controller.OpenAthlete(selected), UITheme.PanelAlt, 34f);
            }

            Transform metrics = UIFactory.Horizontal(stack, 8f, 72f);
            AddMetric(metrics, "ATHLETES", Manager.State.Roster.Count.ToString());
            AddMetric(metrics, "REP", Manager.State.Management.Reputation.ToString());
            AddMetric(metrics, "APPLICATIONS", Manager.State.Applications.Count.ToString());

            UIFactory.Text(stack, "AUTO MANAGEMENT", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 24f);
            UIFactory.Text(stack, Manager.State.AutoManagementUnlocked ? "UNLOCKED · automation settings will be added in a later update." : "LOCKED · unlocks when your roster reaches 10 athletes (" + Manager.State.Roster.Count + "/10).", 13, TextAnchor.MiddleLeft, Manager.State.AutoManagementUnlocked ? UITheme.Green : UITheme.Muted, FontStyle.Bold, 32f);

            if (Manager.State.SponsorOffers.Count > 0)
            {
                UIFactory.Text(stack, "SPONSOR OFFERS", 15, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 24f);
                for (int i = 0; i < Manager.State.SponsorOffers.Count; i++)
                {
                    SponsorOffer offer = Manager.State.SponsorOffers[i];
                    Athlete athlete = Manager.State.Roster.Find(a => a.Id == offer.AthleteId);
                    Image sponsor = UIFactory.FixedPanel(stack, UITheme.Panel, 104f, "SponsorOffer");
                    Transform inner = UIFactory.Vertical(sponsor.transform, 2f, 10, "Inner");
                    UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                    UIFactory.Text(inner, offer.BrandName + " → " + (athlete != null ? athlete.DisplayName : "Athlete"), 16, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 24f);
                    UIFactory.Text(inner, "$" + offer.SigningBonus + " signing · $" + offer.WeeklyPayment + "/week · $" + offer.WinBonus + " per win · " + offer.DurationWeeks + " weeks", 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
                    UIFactory.Button(inner, "ACCEPT SPONSOR", () => Manager.AcceptSponsorOffer(offer), UITheme.Green, 32f);
                }
            }

            Transform utilities = UIFactory.Horizontal(stack, 8f, 38f);
            UIFactory.Button(utilities, "SAVE", Manager.SaveGame, UITheme.PanelAlt, 38f);
            UIFactory.Button(utilities, "LOAD", Manager.LoadGame, UITheme.PanelAlt, 38f);
            UIFactory.Button(stack, "RESET CAREER", () => { Manager.ResetGame(); Controller.Navigate(ScreenId.Setup); }, UITheme.Red, 40f);
        }

        private void AddMetric(Transform parent, string label, string value)
        {
            Image card = UIFactory.Panel(parent, UITheme.Panel, "Metric");
            Transform stack = UIFactory.Vertical(card.transform, 0f, 5, "MetricStack");
            UIFactory.Stretch(stack.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(stack, label, 10, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 20f);
            UIFactory.Text(stack, value, 18, TextAnchor.MiddleCenter, UITheme.Gold, FontStyle.Bold, 34f);
        }
    }

    public class TeamScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            UIFactory.Text(stack, "TEAM", 28, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 42f);
            UIFactory.Text(stack, "Every athlete has a separate distance training plan, focus, PB set and competition entry.", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 42f);

            for (int i = 0; i < Manager.State.Roster.Count; i++)
            {
                Athlete athlete = Manager.State.Roster[i];
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 142f, "AthleteCard");
                Transform inner = UIFactory.Vertical(card.transform, 3f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                UIFactory.Text(inner, athlete.DisplayName + " · AGE " + athlete.Age + " · " + athlete.Category + " · OVR " + athlete.Overall, 17, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 26f);
                UIFactory.Text(inner, Ratings(athlete), 12, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 22f);
                UIFactory.Text(inner, "Training " + DomainLabels.Distance(athlete.TrainingDistance) + " / " + athlete.TrainingFocus + (athlete.InjuryWeeks > 0 ? " · INJURED " + athlete.InjuryWeeks + "w" : ""), 12, TextAnchor.MiddleLeft, athlete.InjuryWeeks > 0 ? UITheme.Red : UITheme.Muted, FontStyle.Normal, 22f);
                CompetitionMeet meet = Manager.ScheduledMeet(athlete);
                UIFactory.Text(inner, meet == null ? "No competition scheduled" : meet.Week.ShortLabel + " · " + meet.Name + " · " + DomainLabels.Distance(athlete.ScheduledCompetition.Distance), 12, TextAnchor.MiddleLeft, meet == null ? UITheme.Muted : UITheme.Green, FontStyle.Bold, 22f);
                UIFactory.Button(inner, "OPEN " + athlete.FirstName.ToUpperInvariant(), () => Controller.OpenAthlete(athlete), UITheme.PanelAlt, 34f);
            }
        }

        private string Ratings(Athlete a)
        {
            return "100 " + a.DistanceRating(DistanceType.M100) + " · 200 " + a.DistanceRating(DistanceType.M200) + " · 400 " + a.DistanceRating(DistanceType.M400) + " · 800 " + a.DistanceRating(DistanceType.M800) + " · 1500 " + a.DistanceRating(DistanceType.M1500);
        }
    }
}
