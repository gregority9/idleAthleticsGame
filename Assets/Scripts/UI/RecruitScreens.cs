using TrackDynasty.Mvp03.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TrackDynasty.Mvp03.UI.Screens
{
    public class ApplicationsScreen : GameScreen
    {
        public override void Refresh() { Rebuild(); }

        protected override void Build()
        {
            ScrollRect scroll;
            Transform stack = UIFactory.ScrollContent(Content, out scroll, 14);
            UIFactory.Text(stack, "ATHLETE APPLICATIONS", 26, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 42f);
            UIFactory.Text(stack, "Better results and higher management reputation make applications more frequent and improve their quality. Applicants can arrive at many different ages.", 12, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 48f);

            if (Manager.State.Applications.Count == 0)
                UIFactory.Text(stack, "Nobody is currently asking to join your management.", 14, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 44f);

            for (int i = 0; i < Manager.State.Applications.Count; i++)
            {
                ClubApplication application = Manager.State.Applications[i];
                AthleteCandidate candidate = application.Candidate;
                Athlete preview = candidate.ToAthlete();
                Image card = UIFactory.FixedPanel(stack, UITheme.Panel, 188f, "ApplicationCard");
                Transform inner = UIFactory.Vertical(card.transform, 3f, 10, "Inner");
                UIFactory.Stretch(inner.GetComponent<RectTransform>(), 0, 0, 0, 0);

                UIFactory.Text(inner, candidate.DisplayName + " · " + candidate.CountryCode + " · AGE " + candidate.Age + " · OVR " + preview.Overall, 16, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 25f);
                UIFactory.Text(inner, "Potential " + candidate.PotentialMin + "–" + candidate.PotentialMax + " · signing fee $" + candidate.SigningFee, 12, TextAnchor.MiddleLeft, UITheme.Gold, FontStyle.Bold, 21f);
                UIFactory.Text(inner, "100 " + preview.DistanceRating(DistanceType.M100) + " · 200 " + preview.DistanceRating(DistanceType.M200) + " · 400 " + preview.DistanceRating(DistanceType.M400) + " · 800 " + preview.DistanceRating(DistanceType.M800) + " · 1500 " + preview.DistanceRating(DistanceType.M1500), 12, TextAnchor.MiddleLeft, UITheme.Green, FontStyle.Bold, 21f);
                UIFactory.Text(inner, "SPD " + candidate.Speed + " · ACC " + candidate.Acceleration + " · STR " + candidate.Strength + " · END " + candidate.Endurance + " · TECH " + candidate.Technique + " · MENT " + candidate.Mental, 11, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 25f);
                UIFactory.Text(inner, application.Reason, 11, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 34f);
                UIFactory.Text(inner, "Expires " + application.ExpiresWeek.ShortLabel, 11, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Bold, 18f);

                Transform buttons = UIFactory.Horizontal(inner, 6f, 32f);
                bool canSign = Manager.State.Cash >= candidate.SigningFee && Manager.State.Roster.Count < 25;
                UIFactory.Button(buttons, canSign ? "SIGN" : "CAN'T SIGN", () => Manager.SignApplication(application), canSign ? UITheme.Green : UITheme.PanelAlt, 32f, canSign);
                UIFactory.Button(buttons, "DECLINE", () => Manager.RejectApplication(application), UITheme.PanelAlt, 32f);
            }

            UIFactory.Text(stack, "Current temporary roster capacity: 25. Coach-based capacity is already represented in the data model and will replace this limit when staff hiring is enabled.", 11, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 40f);
        }
    }
}
