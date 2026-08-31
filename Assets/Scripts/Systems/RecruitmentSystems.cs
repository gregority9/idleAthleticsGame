using System;
using TrackDynasty.Mvp03.Domain;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace TrackDynasty.Mvp03.Systems
{
    public static class ApplicationSystem
    {
        public static ClubApplication MaybeGenerate(GameState state, Athlete athlete, RaceResult result)
        {
            if (state == null || athlete == null || result == null) return null;

            int performanceScore = 0;
            if (result.PlayerPlace == 1) performanceScore += 8;
            else if (result.PlayerPlace <= 3) performanceScore += 4;
            if (result.NewPersonalBest) performanceScore += 3;
            if (result.NewClubRecord) performanceScore += 4;
            if (result.NewWorldRecord) performanceScore += 12;
            performanceScore += (int)result.Tier * 2;

            if (performanceScore < 5) return null;

            float chance = Mathf.Clamp01(0.10f + performanceScore * 0.035f + state.Reputation / 10000f);
            if (result.PlayerPlace == 1 && result.Tier >= CompetitionTier.National) chance = Mathf.Max(chance, 0.70f);
            if (UnityEngine.Random.value > chance) return null;

            Prospect prospect = AthleteGenerator.GenerateApplicant(performanceScore, state.Reputation, state.ChosenScout);
            string reason = prospect.DisplayName + " noticed the club after " + athlete.DisplayName + " finished " + Ordinal(result.PlayerPlace) + " at " + result.EventName + ".";
            return new ClubApplication
            {
                Id = Guid.NewGuid().ToString("N"),
                Prospect = prospect,
                AppliedDate = new GameDate(state.CurrentDate.Year, state.CurrentDate.Month, state.CurrentDate.Day),
                ExpiresDate = state.CurrentDate.AddDays(30),
                Reason = reason
            };
        }

        public static void RemoveExpired(GameState state)
        {
            if (state == null || state.Applications == null) return;
            for (int i = state.Applications.Count - 1; i >= 0; i--)
            {
                ClubApplication app = state.Applications[i];
                if (app.ExpiresDate != null && app.ExpiresDate.CompareTo(state.CurrentDate) < 0)
                    state.Applications.RemoveAt(i);
            }
        }

        private static string Ordinal(int place)
        {
            if (place == 1) return "1st";
            if (place == 2) return "2nd";
            if (place == 3) return "3rd";
            return place + "th";
        }
    }
}

namespace TrackDynasty.Mvp03.Systems
{
    public static class AthleteGenerator
    {
        private static readonly string[] FirstNames = { "Samuel", "Elias", "Marcus", "Jaden", "Kofi", "Mateo", "Aiden", "Noel", "Omar", "Tyrique", "Milan", "Darius", "Andre", "Lucas", "Jakub", "Rafael", "Mateusz", "Daniel" };
        private static readonly string[] LastNames = { "Okafor", "Becker", "Pierre", "Mensah", "Costa", "Johnson", "Smith", "Cole", "Reyes", "Walker", "Mendes", "Taylor", "Campbell", "Martin", "Zielinski", "Silva", "Nowak", "King" };
        private static readonly string[] Countries = { "NGR", "GER", "USA", "CAN", "FRA", "ITA", "JAM", "GBR", "BRA", "POL" };

        public static Athlete CreateStarterAthlete()
        {
            return new Athlete
            {
                Id = "andre-campbell",
                FirstName = "Andre",
                LastName = "Campbell",
                CountryCode = "JAM",
                Age = 17,
                Speed = 72,
                Acceleration = 82,
                Strength = 66,
                Technique = 68,
                Mental = 71,
                Form = 0.92f,
                Fatigue = 0.14f,
                PersonalBest = 10.72f,
                YearStartPersonalBest = 10.72f,
                Potential = 94,
                PotentialMin = 82,
                PotentialMax = 97,
                DevelopmentRate = 1.08f,
                TrainingFocus = TrainingFocus.Sprint,
                Traits = new List<TraitType> { TraitType.ExplosiveStarter, TraitType.FastLearner }
            };
        }

        public static Prospect GenerateScoutedProspect(ScoutProfile scout, int clubReputation)
        {
            int network = scout != null ? scout.Network : 2;
            int qualityBonus = Mathf.Clamp(network - 2, 0, 3) * 2 + Mathf.Clamp(clubReputation / 1000, 0, 5);
            return GenerateProspectInternal(qualityBonus, scout != null ? scout.Evaluation : 2, scout, false);
        }

        public static Prospect GenerateApplicant(int performanceScore, int clubReputation, ScoutProfile scout)
        {
            int repBonus = Mathf.Clamp(clubReputation / 700, 0, 8);
            int qualityBonus = Mathf.Clamp(performanceScore / 3, 0, 12) + repBonus;
            int evaluation = scout != null ? scout.Evaluation : 2;
            return GenerateProspectInternal(qualityBonus, evaluation, scout, true);
        }

        private static Prospect GenerateProspectInternal(int qualityBonus, int evaluation, ScoutProfile scout, bool applicant)
        {
            int age = UnityEngine.Random.Range(16, 23);
            int potential = Mathf.Clamp(UnityEngine.Random.Range(76, 94) + qualityBonus / 2, 74, 99);
            int baseStat = Mathf.Clamp(UnityEngine.Random.Range(58, 72) + qualityBonus, 55, 88);

            int uncertainty = Mathf.Clamp(14 - evaluation * 2, 3, 12);
            int estimateMin = Mathf.Max(55, potential - UnityEngine.Random.Range(Mathf.Max(2, uncertainty / 2), uncertainty + 1));
            int estimateMax = Mathf.Min(99, potential + UnityEngine.Random.Range(1, Mathf.Max(2, uncertainty / 2 + 1)));
            int rawFee = UnityEngine.Random.Range(applicant ? 1200 : 1800, applicant ? 3300 : 4500) + qualityBonus * 90;

            Prospect p = new Prospect
            {
                Id = Guid.NewGuid().ToString("N"),
                FirstName = FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)],
                LastName = LastNames[UnityEngine.Random.Range(0, LastNames.Length)],
                CountryCode = Countries[UnityEngine.Random.Range(0, Countries.Length)],
                Age = age,
                Speed = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-3, 6), 52, 94),
                Acceleration = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-3, 7), 52, 94),
                Strength = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-7, 4), 48, 90),
                Technique = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-5, 5), 48, 90),
                Mental = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-5, 6), 48, 90),
                Potential = potential,
                PotentialMin = estimateMin,
                PotentialMax = estimateMax,
                DevelopmentRate = UnityEngine.Random.Range(0.86f, 1.16f),
                SigningFee = ScoutSystem.SigningFee(scout, rawFee)
            };

            p.Traits.Add(RandomTrait());
            if (UnityEngine.Random.value < 0.20f)
            {
                TraitType second = RandomTrait();
                if (!p.Traits.Contains(second)) p.Traits.Add(second);
            }
            return p;
        }

        public static Athlete FromProspect(Prospect p)
        {
            Athlete athlete = new Athlete
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                CountryCode = p.CountryCode,
                Age = p.Age,
                Speed = p.Speed,
                Acceleration = p.Acceleration,
                Strength = p.Strength,
                Technique = p.Technique,
                Mental = p.Mental,
                Potential = p.Potential,
                PotentialMin = p.PotentialMin,
                PotentialMax = p.PotentialMax,
                DevelopmentRate = p.DevelopmentRate,
                Traits = new List<TraitType>(p.Traits),
                Form = 0.94f,
                Fatigue = 0.10f,
                PersonalBest = EstimateInitialPb(p),
                TrainingFocus = TrainingFocus.Sprint
            };
            athlete.YearStartPersonalBest = athlete.PersonalBest;
            return athlete;
        }

        private static float EstimateInitialPb(Prospect p)
        {
            float rating = p.BaseRating;
            return Mathf.Round(Mathf.Clamp(13.70f - rating * 0.0445f + UnityEngine.Random.Range(0.02f, 0.12f), 9.75f, 13.10f) * 100f) / 100f;
        }

        private static TraitType RandomTrait()
        {
            Array values = Enum.GetValues(typeof(TraitType));
            return (TraitType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }
    }
}

namespace TrackDynasty.Mvp03.Systems
{
    public static class ScoutSystem
    {
        public static List<ScoutProfile> CreateStartingChoices()
        {
            return new List<ScoutProfile>
            {
                new ScoutProfile
                {
                    Id = "sofia-novak", Name = "Sofia Novak", CountryCode = "POL",
                    Specialty = ScoutSpecialty.Evaluation, Evaluation = 5, Network = 2, MonthlySalary = 550,
                    Description = "Elite evaluator. Gives the narrowest potential estimates."
                },
                new ScoutProfile
                {
                    Id = "malik-johnson", Name = "Malik Johnson", CountryCode = "USA",
                    Specialty = ScoutSpecialty.TalentNetwork, Evaluation = 3, Network = 5, MonthlySalary = 750,
                    Description = "Strong global network. Finds better prospects more often."
                },
                new ScoutProfile
                {
                    Id = "elena-rossi", Name = "Elena Rossi", CountryCode = "ITA",
                    Specialty = ScoutSpecialty.BargainHunter, Evaluation = 3, Network = 3, MonthlySalary = 350,
                    Description = "Balanced and cheap. Prospects ask for lower signing fees."
                }
            };
        }

        public static int RefreshCost(ScoutProfile scout)
        {
            if (scout != null && scout.Specialty == ScoutSpecialty.BargainHunter) return 1100;
            return 1500;
        }

        public static int SigningFee(ScoutProfile scout, int rawFee)
        {
            if (scout != null && scout.Specialty == ScoutSpecialty.BargainHunter)
                return (int)(rawFee * 0.78f);
            return rawFee;
        }
    }
}
