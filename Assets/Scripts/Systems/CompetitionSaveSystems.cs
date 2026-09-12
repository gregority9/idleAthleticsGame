using System;
using TrackDynasty.Mvp03.Domain;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace TrackDynasty.Mvp03.Systems
{
    public static class CompetitionSystem
    {
        private static readonly string[] LocalNames = { "City Sprint Meet", "Open Track Night", "County 100", "Summer Sprint Series" };
        private static readonly string[] RegionalNames = { "Regional Cup", "Coastal Sprint Classic", "Northern Grand Prix", "Regional Challenge" };
        private static readonly string[] NationalNames = { "National Challenge", "National Grand Prix", "Championship Qualifier", "National Sprint Cup" };
        private static readonly string[] InternationalNames = { "Continental Classic", "International Sprint Night", "European Grand Prix", "Global Challenge" };
        private static readonly string[] EliteNames = { "Diamond Sprint Final", "World Elite 100", "Champions Gala", "Global Finals" };
        private static readonly string[] Cities = { "Gdansk", "Warsaw", "Berlin", "Paris", "Rome", "London", "Madrid", "Amsterdam", "Prague", "Vienna", "Stockholm", "Lisbon" };

        public static void EnsureOffers(Athlete athlete, GameDate currentDate, int reputation)
        {
            if (athlete == null) return;
            if (athlete.CompetitionOffers == null) athlete.CompetitionOffers = new List<CompetitionOffer>();

            if (athlete.CompetitionBreakUntil != null)
            {
                if (currentDate.CompareTo(athlete.CompetitionBreakUntil) < 0)
                {
                    athlete.CompetitionOffers.Clear();
                    return;
                }
                athlete.CompetitionBreakUntil = null;
            }

            if (athlete.ScheduledCompetition != null || athlete.CompetitionOffers.Count > 0) return;
            athlete.CompetitionOffers = GenerateOffers(athlete, currentDate, reputation);
        }

        public static List<CompetitionOffer> GenerateOffers(Athlete athlete, GameDate currentDate, int reputation)
        {
            List<CompetitionOffer> offers = new List<CompetitionOffer>();
            CompetitionTier maxTier = HighestQualifiedTier(athlete);
            List<CompetitionTier> pool = BuildTierPool(maxTier);
            DateTime cursor = currentDate.ToDateTime();

            for (int i = 0; i < 3; i++)
            {
                CompetitionTier tier = pool[Mathf.Min(i, pool.Count - 1)];
                int minDays = 7 + i * 4;
                int maxDays = 18 + i * 9;
                DateTime date = cursor.AddDays(UnityEngine.Random.Range(minDays, maxDays + 1));
                bool championship = (tier == CompetitionTier.National || tier == CompetitionTier.International || tier == CompetitionTier.Elite) && UnityEngine.Random.value < 0.22f;
                offers.Add(new CompetitionOffer
                {
                    Id = Guid.NewGuid().ToString("N"),
                    AthleteId = athlete.Id,
                    Name = PickName(tier, championship),
                    City = Cities[UnityEngine.Random.Range(0, Cities.Length)],
                    Tier = tier,
                    Date = GameDate.FromDateTime(date),
                    EntryStandard = EntryStandard(tier),
                    CashReward = BaseCash(tier, championship),
                    ReputationReward = BaseRep(tier, championship),
                    IsChampionship = championship
                });
            }

            offers.Sort((a, b) => a.Date.CompareTo(b.Date));
            return offers;
        }

        public static bool CanEnter(Athlete athlete, CompetitionOffer offer)
        {
            if (athlete == null || offer == null) return false;
            return athlete.PersonalBest <= offer.EntryStandard || offer.EntryStandard >= 90f;
        }

        public static CompetitionTier HighestQualifiedTier(Athlete athlete)
        {
            if (athlete == null) return CompetitionTier.Local;
            if (athlete.PersonalBest <= EntryStandard(CompetitionTier.Elite)) return CompetitionTier.Elite;
            if (athlete.PersonalBest <= EntryStandard(CompetitionTier.International)) return CompetitionTier.International;
            if (athlete.PersonalBest <= EntryStandard(CompetitionTier.National)) return CompetitionTier.National;
            if (athlete.PersonalBest <= EntryStandard(CompetitionTier.Regional)) return CompetitionTier.Regional;
            return CompetitionTier.Local;
        }

        public static float EntryStandard(CompetitionTier tier)
        {
            switch (tier)
            {
                case CompetitionTier.Local: return 99f;
                case CompetitionTier.Regional: return 11.20f;
                case CompetitionTier.National: return 10.80f;
                case CompetitionTier.International: return 10.45f;
                case CompetitionTier.Elite: return 10.10f;
                default: return 99f;
            }
        }

        public static string QualificationText(CompetitionTier tier)
        {
            float standard = EntryStandard(tier);
            return standard >= 90f ? "Open entry" : "Entry standard: " + standard.ToString("0.00") + "s";
        }

        private static List<CompetitionTier> BuildTierPool(CompetitionTier maxTier)
        {
            int max = (int)maxTier;
            List<CompetitionTier> pool = new List<CompetitionTier>();
            pool.Add((CompetitionTier)Mathf.Max(0, max - 1));
            pool.Add(maxTier);
            pool.Add(maxTier);
            if (max >= 2) pool[0] = (CompetitionTier)(max - 1);
            if (max == 0) pool[0] = CompetitionTier.Local;
            if (max == 4) pool[2] = CompetitionTier.Elite;
            return pool;
        }

        private static string PickName(CompetitionTier tier, bool championship)
        {
            if (championship)
            {
                if (tier == CompetitionTier.National) return "National Championships";
                if (tier == CompetitionTier.International) return "Continental Championships";
                if (tier == CompetitionTier.Elite) return "World Championships";
            }
            string[] source = tier == CompetitionTier.Local ? LocalNames : tier == CompetitionTier.Regional ? RegionalNames : tier == CompetitionTier.National ? NationalNames : tier == CompetitionTier.International ? InternationalNames : EliteNames;
            return source[UnityEngine.Random.Range(0, source.Length)];
        }

        private static int BaseCash(CompetitionTier tier, bool championship)
        {
            int value = tier == CompetitionTier.Local ? 700 : tier == CompetitionTier.Regional ? 1400 : tier == CompetitionTier.National ? 2600 : tier == CompetitionTier.International ? 4800 : 8000;
            return championship ? Mathf.RoundToInt(value * 1.4f) : value;
        }

        private static int BaseRep(CompetitionTier tier, bool championship)
        {
            int value = tier == CompetitionTier.Local ? 6 : tier == CompetitionTier.Regional ? 10 : tier == CompetitionTier.National ? 18 : tier == CompetitionTier.International ? 30 : 48;
            return championship ? Mathf.RoundToInt(value * 1.5f) : value;
        }
    }
}

namespace TrackDynasty.Mvp03.Systems
{
    public static class SaveSystem
    {
        private const string FileName = "track_dynasty_mvp03.json";
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists() => File.Exists(SavePath);

        public static void Save(GameState state)
        {
            if (state == null) return;
            File.WriteAllText(SavePath, JsonUtility.ToJson(state, true));
        }

        public static GameState Load()
        {
            if (!Exists()) return null;
            try { return JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath)); }
            catch { return null; }
        }

        public static void Delete()
        {
            if (Exists()) File.Delete(SavePath);
        }
    }
}
