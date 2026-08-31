using TrackDynasty.Mvp03.Domain;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Systems;
using System;
using System.Text;

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class ScoutScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            UIFactory.Text(stack, "SCOUTING", 28, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 42f);

            ScoutProfile scout = Manager.State.ChosenScout;
            if (scout == null)
            {
                UIFactory.Text(stack, "Choose your starting scout first.", 16, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 40f);
                return;
            }

            Image scoutCard = UIFactory.FixedPanel(stack, UITheme.Panel, 112f, "ScoutProfile");
            Transform scoutStack = UIFactory.Vertical(scoutCard.transform, 3f, 10, "ScoutStack");
            UIFactory.Stretch(scoutStack.GetComponent<RectTransform>(), 0, 0, 0, 0);
            UIFactory.Text(scoutStack, scout.Name + " · " + scout.Specialty, 18, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 28f);
            UIFactory.Text(scoutStack, "Evaluation " + scout.Evaluation + "/5 · Network " + scout.Network + "/5 · Salary $" + scout.MonthlySalary + "/month", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 24f);
            UIFactory.Text(scoutStack, scout.Description, 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 34f);

            int cost = ScoutSystem.RefreshCost(scout);
            UIFactory.Button(stack, "REFRESH SHORTLIST  $" + cost, () => Manager.RefreshScouting(false), Manager.State.Cash >= cost ? UITheme.Green : UITheme.PanelAlt, 42f, Manager.State.Cash >= cost);

            if (Manager.State.ScoutedProspects.Count == 0)
                UIFactory.Text(stack, "No active shortlist. Refresh scouting to find athletes.", 14, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 40f);

            for (int i = 0; i < Manager.State.ScoutedProspects.Count; i++)
            {
                Prospect p = Manager.State.ScoutedProspects[i];
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 152f, "ProspectCard");
                Transform inner = UIFactory.Vertical(card.transform, 3f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                Transform header = UIFactory.Horizontal(inner, 8f, 34f);
                Image flag = UIFactory.Panel(header, Color.white, "Flag");
                flag.sprite = FlagSpriteFactory.Get(p.CountryCode);
                flag.preserveAspect = true;
                UIFactory.SetPreferredWidth(flag, 34f);
                Text name = UIFactory.Text(header, p.DisplayName, 17, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 34f);
                UIFactory.SetFlexibleWidth(name);
                UIFactory.Text(inner, "Age " + p.Age + " · OVR " + Mathf.RoundToInt(p.BaseRating) + " · Potential " + p.PotentialMin + "–" + p.PotentialMax, 13, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 22f);
                UIFactory.Text(inner, "SPD " + p.Speed + " · ACC " + p.Acceleration + " · STR " + p.Strength + " · TECH " + p.Technique + " · MENT " + p.Mental, 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
                string traits = p.Traits == null || p.Traits.Count == 0 ? "No traits known" : string.Join(", ", p.Traits.ConvertAll(t => t.ToString()).ToArray());
                UIFactory.Text(inner, traits, 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 20f);
                bool canSign = Manager.State.Cash >= p.SigningFee && Manager.State.Roster.Count < 8;
                UIFactory.Button(inner, "SIGN  $" + p.SigningFee, () => Manager.SignProspect(p, false), canSign ? UITheme.Green : UITheme.PanelAlt, 34f, canSign);
            }
        }
    }
}

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class ApplicationsScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            UIFactory.Text(stack, "INBOUND APPLICATIONS", 26, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 42f);
            UIFactory.Text(stack, "Better club results attract stronger athletes. Applications expire after 30 days.", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 42f);

            if (Manager.State.Applications.Count == 0)
                UIFactory.Text(stack, "No athletes are currently asking to join the club.", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 50f);

            for (int i = 0; i < Manager.State.Applications.Count; i++)
            {
                ClubApplication application = Manager.State.Applications[i];
                Prospect p = application.Prospect;
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 180f, "ApplicationCard");
                Transform inner = UIFactory.Vertical(card.transform, 3f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                Transform header = UIFactory.Horizontal(inner, 8f, 34f);
                Image flag = UIFactory.Panel(header, Color.white, "Flag");
                flag.sprite = FlagSpriteFactory.Get(p.CountryCode);
                flag.preserveAspect = true;
                UIFactory.SetPreferredWidth(flag, 34f);
                Text name = UIFactory.Text(header, p.DisplayName, 17, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 34f);
                UIFactory.SetFlexibleWidth(name);
                UIFactory.Text(inner, "Age " + p.Age + " · Potential " + p.PotentialMin + "–" + p.PotentialMax + " · Fee $" + p.SigningFee, 13, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 22f);
                UIFactory.Text(inner, application.Reason, 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 42f);
                UIFactory.Text(inner, "Expires " + application.ExpiresDate.ShortLabel, 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 20f);
                Transform buttons = UIFactory.Horizontal(inner, 8f, 36f);
                bool canSign = Manager.State.Cash >= p.SigningFee && Manager.State.Roster.Count < 8;
                UIFactory.Button(buttons, "SIGN", () => Manager.SignProspect(p, true), canSign ? UITheme.Green : UITheme.PanelAlt, 36f, canSign);
                UIFactory.Button(buttons, "DECLINE", () => Manager.RejectApplication(application), UITheme.PanelAlt, 36f);
            }

            UIFactory.Button(stack, "OPEN HALL OF FAME", Controller.OpenHallOfFame, UITheme.PanelAlt, 40f);
        }
    }
}

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class HallOfFameScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            UIFactory.Button(stack, "‹ BACK TO HQ", () => Controller.Navigate(ScreenId.HQ), UITheme.PanelAlt, 38f);
            UIFactory.Text(stack, "HALL OF FAME", 28, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 42f);
            if (Manager.State.HallOfFame.Count == 0)
                UIFactory.Text(stack, "No retired club legends yet.", 15, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 50f);

            for (int i = 0; i < Manager.State.HallOfFame.Count; i++)
            {
                HallOfFameEntry h = Manager.State.HallOfFame[i];
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 96f, "LegendCard");
                Transform inner = UIFactory.Vertical(card.transform, 2f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);
                UIFactory.Text(inner, (i + 1) + ". " + h.Name + " · " + h.CountryCode, 17, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 26f);
                UIFactory.Text(inner, "PB " + h.PersonalBest.ToString("0.00") + "s · Retired at " + h.RetireAge, 13, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Normal, 22f);
                UIFactory.Text(inner, "Races " + h.Races + " · Wins " + h.Wins + " · Titles " + h.Championships, 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
            }
        }
    }
}
