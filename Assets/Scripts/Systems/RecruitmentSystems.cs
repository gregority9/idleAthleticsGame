using System;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Domain;
using UnityEngine;

namespace TrackDynasty.Mvp03.Systems
{
    public static class AthleteGenerator
    {
        private static readonly string[] PolishFirstNames = { "Michał", "Jakub", "Antoni", "Kacper", "Jan", "Aleksander", "Filip", "Mateusz", "Zofia", "Maja", "Julia", "Oliwia", "Hanna", "Lena" };
        private static readonly string[] PolishLastNames = { "Nowak", "Kowalski", "Wiśniewski", "Wójcik", "Kamiński", "Lewandowski", "Zieliński", "Szymański", "Dąbrowski", "Woźniak" };
        private static readonly string[] InternationalFirstNames = { "Noah", "Liam", "Mateo", "Elias", "Amir", "Lucas", "Kofi", "Sofia", "Emma", "Mia", "Amina", "Lea" };
        private static readonly string[] InternationalLastNames = { "Smith", "Johnson", "Martin", "Costa", "Mensah", "Becker", "Silva", "Brown", "Rossi", "Meyer" };
        private static readonly string[] Countries = { "POL", "GER", "FRA", "ITA", "GBR", "USA", "CAN", "NGR", "BRA" };

        public static List<AthleteCandidate> CreateStarterChoices()
        {
            List<AthleteCandidate> result = new List<AthleteCandidate>();
            for (int i = 0; i < 5; i++) result.Add(CreateStarter(i));
            return result;
        }

        private static AthleteCandidate CreateStarter(int archetype)
        {
            AthleteCandidate candidate = new AthleteCandidate
            {
                Id = Guid.NewGuid().ToString("N"),
                FirstName = PolishFirstNames[UnityEngine.Random.Range(0, PolishFirstNames.Length)],
                LastName = PolishLastNames[UnityEngine.Random.Range(0, PolishLastNames.Length)],
                CountryCode = "POL",
                Age = 8,
                Speed = UnityEngine.Random.Range(5, 10),
                Acceleration = UnityEngine.Random.Range(5, 10),
                Strength = UnityEngine.Random.Range(4, 9),
                Endurance = UnityEngine.Random.Range(5, 10),
                Technique = UnityEngine.Random.Range(5, 10),
                Mental = UnityEngine.Random.Range(5, 10),
                Potential = UnityEngine.Random.Range(70, 94),
                DevelopmentRate = UnityEngine.Random.Range(0.94f, 1.12f)
            };

            int uncertainty = UnityEngine.Random.Range(10, 18);
            candidate.PotentialMin = Mathf.Max(55, candidate.Potential - uncertainty);
            candidate.PotentialMax = Mathf.Min(99, candidate.Potential + UnityEngine.Random.Range(4, 11));

            int[] bonuses = { 0, 0, 0, 0, 0 };
            switch (archetype)
            {
                case 0: bonuses = new[] { 3, 4, 1, -1, -2 }; break;
                case 1: bonuses = new[] { 1, 3, 4, 1, -1 }; break;
                case 2: bonuses = new[] { -1, 1, 4, 3, 0 }; break;
                case 3: bonuses = new[] { -2, -1, 1, 4, 3 }; break;
                default: bonuses = new[] { 1, 1, 1, 1, 1 }; break;
            }
            DistanceType[] distances = { DistanceType.M100, DistanceType.M200, DistanceType.M400, DistanceType.M800, DistanceType.M1500 };
            for (int i = 0; i < distances.Length; i++)
                candidate.Aptitudes.Add(new DistanceAptitude { Distance = distances[i], Bonus = bonuses[i] + UnityEngine.Random.Range(-1, 2) });

            if (UnityEngine.Random.value < 0.35f) candidate.Traits.Add(RandomTrait());
            return candidate;
        }

        public static AthleteCandidate GenerateApplicant(int reputation, int performanceScore)
        {
            int age = RollApplicantAge(reputation);
            int quality = Mathf.Clamp(reputation / 80 + performanceScore / 3, 0, 24);
            int ageBase = AgeBaseStat(age);
            int baseStat = Mathf.Clamp(ageBase + quality + UnityEngine.Random.Range(-3, 4), 4, 82);
            int potentialFloor = Mathf.Clamp(58 + quality, 58, 90);
            int potential = Mathf.Clamp(UnityEngine.Random.Range(potentialFloor, Mathf.Min(100, potentialFloor + 22)), 55, 99);
            int uncertainty = Mathf.Clamp(18 - reputation / 250, 5, 18);
            bool international = reputation >= 220 && UnityEngine.Random.value < Mathf.Clamp01((reputation - 150) / 1200f);

            AthleteCandidate candidate = new AthleteCandidate
            {
                Id = Guid.NewGuid().ToString("N"),
                FirstName = international ? InternationalFirstNames[UnityEngine.Random.Range(0, InternationalFirstNames.Length)] : PolishFirstNames[UnityEngine.Random.Range(0, PolishFirstNames.Length)],
                LastName = international ? InternationalLastNames[UnityEngine.Random.Range(0, InternationalLastNames.Length)] : PolishLastNames[UnityEngine.Random.Range(0, PolishLastNames.Length)],
                CountryCode = international ? Countries[UnityEngine.Random.Range(0, Countries.Length)] : "POL",
                Age = age,
                Speed = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-4, 6), 3, 94),
                Acceleration = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-4, 6), 3, 94),
                Strength = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-5, 5), 3, 92),
                Endurance = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-5, 6), 3, 94),
                Technique = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-4, 5), 3, 94),
                Mental = Mathf.Clamp(baseStat + UnityEngine.Random.Range(-4, 5), 3, 94),
                Potential = potential,
                PotentialMin = Mathf.Max(45, potential - UnityEngine.Random.Range(Mathf.Max(4, uncertainty / 2), uncertainty + 1)),
                PotentialMax = Mathf.Min(99, potential + UnityEngine.Random.Range(2, Mathf.Max(3, uncertainty / 2 + 1))),
                DevelopmentRate = UnityEngine.Random.Range(0.84f, 1.18f),
                SigningFee = Mathf.Max(0, 150 + age * 18 + quality * 45 + UnityEngine.Random.Range(-100, 250))
            };

            DistanceType[] distances = { DistanceType.M100, DistanceType.M200, DistanceType.M400, DistanceType.M800, DistanceType.M1500 };
            int preferred = UnityEngine.Random.Range(0, distances.Length);
            for (int i = 0; i < distances.Length; i++)
            {
                int bonus = UnityEngine.Random.Range(-2, 3);
                if (i == preferred) bonus += UnityEngine.Random.Range(2, 6);
                if (Mathf.Abs(i - preferred) == 1) bonus += UnityEngine.Random.Range(0, 3);
                candidate.Aptitudes.Add(new DistanceAptitude { Distance = distances[i], Bonus = Mathf.Clamp(bonus, -6, 7) });
            }

            if (UnityEngine.Random.value < 0.48f) candidate.Traits.Add(RandomTrait());
            if (UnityEngine.Random.value < 0.12f)
            {
                TraitType second = RandomTrait();
                if (!candidate.Traits.Contains(second)) candidate.Traits.Add(second);
            }
            return candidate;
        }

        private static int RollApplicantAge(int reputation)
        {
            float roll = UnityEngine.Random.value;
            if (reputation < 150)
            {
                if (roll < 0.62f) return UnityEngine.Random.Range(8, 16);
                if (roll < 0.90f) return UnityEngine.Random.Range(16, 20);
                return UnityEngine.Random.Range(20, 28);
            }
            if (roll < 0.40f) return UnityEngine.Random.Range(8, 17);
            if (roll < 0.75f) return UnityEngine.Random.Range(17, 24);
            if (roll < 0.95f) return UnityEngine.Random.Range(24, 33);
            return UnityEngine.Random.Range(33, 36);
        }

        private static int AgeBaseStat(int age)
        {
            if (age <= 10) return 8;
            if (age <= 12) return 13;
            if (age <= 14) return 20;
            if (age <= 16) return 29;
            if (age <= 18) return 39;
            if (age <= 23) return 52;
            if (age <= 29) return 58;
            if (age <= 32) return 55;
            return 49;
        }

        private static TraitType RandomTrait()
        {
            Array values = Enum.GetValues(typeof(TraitType));
            return (TraitType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }
    }

    public static class ApplicationSystem
    {
        public static ClubApplication MaybeGenerateAfterRace(GameState state, Athlete athlete, RaceResult result)
        {
            if (state == null || athlete == null || result == null) return null;
            int score = PerformanceScore(result);
            if (score < 3) return null;

            float chance = Mathf.Clamp01(0.05f + score * 0.025f + state.Management.Reputation / 6000f);
            if (UnityEngine.Random.value > chance) return null;
            return BuildApplication(state, score, athlete.DisplayName + "'s result at " + result.EventName + " put your management on the radar.");
        }

        public static ClubApplication MaybeGenerateWeekly(GameState state)
        {
            if (state == null || !state.SetupCompleted) return null;
            float chance = Mathf.Clamp01(0.012f + state.Management.Reputation / 18000f);
            if (UnityEngine.Random.value > chance) return null;
            return BuildApplication(state, Mathf.Clamp(state.Management.Reputation / 100, 0, 14), "The athlete contacted your management after following your recent progress.");
        }

        public static void RemoveExpired(GameState state)
        {
            if (state == null || state.Applications == null) return;
            for (int i = state.Applications.Count - 1; i >= 0; i--)
            {
                ClubApplication application = state.Applications[i];
                if (application.ExpiresWeek != null && application.ExpiresWeek.CompareTo(state.CurrentWeek) < 0)
                    state.Applications.RemoveAt(i);
            }
        }

        private static ClubApplication BuildApplication(GameState state, int performanceScore, string reason)
        {
            AthleteCandidate candidate = AthleteGenerator.GenerateApplicant(state.Management.Reputation, performanceScore);
            return new ClubApplication
            {
                Id = Guid.NewGuid().ToString("N"),
                Candidate = candidate,
                AppliedWeek = new GameWeek(state.CurrentWeek.Year, state.CurrentWeek.Week),
                ExpiresWeek = state.CurrentWeek.AddWeeks(6),
                Reason = reason
            };
        }

        private static int PerformanceScore(RaceResult result)
        {
            int score = 0;
            if (result.PlayerPlace == 1) score += 8;
            else if (result.PlayerPlace <= 3) score += 4;
            else if (result.PlayerPlace <= 5) score += 1;
            if (result.NewPersonalBest) score += 2;
            if (result.NewClubRecord) score += 3;
            score += (int)result.Range * 2;
            if (result.IsChampionship) score += 2;
            return score;
        }
    }

    public static class SponsorSystem
    {
        private static readonly string[] SmallBrands = { "VeloRun", "NorthTrack", "Pulse Gear", "Stride Lab", "FastLane" };
        private static readonly string[] MajorBrands = { "Apex Athletics", "Momentum", "Velocity Sports", "Peak Performance" };

        public static float InterestGain(RaceResult result)
        {
            if (result == null) return 0f;
            float gain = result.PlayerPlace == 1 ? 8f : result.PlayerPlace <= 3 ? 4f : result.PlayerPlace <= 5 ? 1.5f : 0.5f;
            gain += (int)result.Range * 2.5f;
            if (result.NewPersonalBest) gain += 2f;
            if (result.NewClubRecord) gain += 4f;
            if (result.IsChampionship) gain += 3f;
            return gain;
        }

        public static SponsorOffer MaybeCreateOffer(GameState state, Athlete athlete)
        {
            if (state == null || athlete == null) return null;
            if (state.SponsorContracts.Exists(c => c.AthleteId == athlete.Id)) return null;
            if (state.SponsorOffers.Exists(o => o.AthleteId == athlete.Id)) return null;

            float threshold = 20f + Mathf.Max(0, 40f - state.Management.Reputation * 0.02f);
            if (athlete.SponsorInterest < threshold) return null;

            bool major = athlete.SponsorInterest >= 85f && state.Management.Reputation >= 350;
            string[] brands = major ? MajorBrands : SmallBrands;
            int level = Mathf.Clamp(Mathf.RoundToInt(athlete.SponsorInterest / 18f + state.Management.Reputation / 300f), 1, 12);
            athlete.SponsorInterest *= 0.42f;

            return new SponsorOffer
            {
                Id = Guid.NewGuid().ToString("N"),
                AthleteId = athlete.Id,
                BrandName = brands[UnityEngine.Random.Range(0, brands.Length)],
                SigningBonus = (major ? 1200 : 250) * level,
                WeeklyPayment = (major ? 90 : 20) * level,
                WinBonus = (major ? 350 : 65) * level,
                DurationWeeks = major ? 104 : 52
            };
        }
    }
}
