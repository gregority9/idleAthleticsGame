using System;
using System.Collections.Generic;
using System.IO;
using TrackDynasty.Mvp03.Domain;
using UnityEngine;

namespace TrackDynasty.Mvp03.Systems
{
    public static class Geography
    {
        public static readonly string[] SupportedCountries = { "POL", "GER", "FRA", "ITA", "GBR", "USA", "CAN", "NGR", "BRA" };
        public static readonly string[] SupportedContinents = { "Europe", "North America", "South America", "Africa" };

        public static string ContinentForCountry(string countryCode)
        {
            switch ((countryCode ?? "POL").ToUpperInvariant())
            {
                case "USA":
                case "CAN": return "North America";
                case "BRA": return "South America";
                case "NGR": return "Africa";
                default: return "Europe";
            }
        }

        public static string CountryName(string code)
        {
            switch ((code ?? "POL").ToUpperInvariant())
            {
                case "GER": return "German";
                case "FRA": return "French";
                case "ITA": return "Italian";
                case "GBR": return "British";
                case "USA": return "US";
                case "CAN": return "Canadian";
                case "NGR": return "Nigerian";
                case "BRA": return "Brazilian";
                default: return "Polish";
            }
        }

        public static string CountryCity(string code)
        {
            switch ((code ?? "POL").ToUpperInvariant())
            {
                case "GER": return "Berlin";
                case "FRA": return "Paris";
                case "ITA": return "Rome";
                case "GBR": return "London";
                case "USA": return "New York";
                case "CAN": return "Toronto";
                case "NGR": return "Lagos";
                case "BRA": return "Rio de Janeiro";
                default: return "Warsaw";
            }
        }

        public static string ContinentCity(string continent)
        {
            switch (continent)
            {
                case "North America": return "Toronto";
                case "South America": return "Rio de Janeiro";
                case "Africa": return "Nairobi";
                default: return "Berlin";
            }
        }
    }

    public static class PerformanceModel
    {
        public static float EstimateTime(DistanceType distance, int age, float rating)
        {
            float slow;
            float elite;
            switch (distance)
            {
                case DistanceType.M100: slow = 14.50f; elite = 9.65f; break;
                case DistanceType.M200: slow = 30.00f; elite = 19.45f; break;
                case DistanceType.M400: slow = 68.00f; elite = 43.80f; break;
                case DistanceType.M800: slow = 165.0f; elite = 101.0f; break;
                default: slow = 340.0f; elite = 207.0f; break;
            }

            float normalized = Mathf.Clamp01(rating / 100f);
            float baseTime = Mathf.Lerp(slow, elite, Mathf.Pow(normalized, 0.92f));
            return baseTime * AgeTimeMultiplier(age);
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 60f) return seconds.ToString("0.00") + "s";
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainder = seconds - minutes * 60f;
            return minutes + ":" + remainder.ToString("00.00");
        }

        public static int RepresentativeAge(AgeCategory category)
        {
            switch (category)
            {
                case AgeCategory.U10: return 9;
                case AgeCategory.U12: return 12;
                case AgeCategory.U14: return 14;
                case AgeCategory.U16: return 16;
                case AgeCategory.U18: return 18;
                case AgeCategory.Junior: return 21;
                case AgeCategory.Adult: return 27;
                default: return 35;
            }
        }

        private static float AgeTimeMultiplier(int age)
        {
            if (age <= 8) return 1.18f;
            if (age <= 10) return 1.15f;
            if (age <= 12) return 1.12f;
            if (age <= 14) return 1.08f;
            if (age <= 16) return 1.045f;
            if (age <= 18) return 1.02f;
            if (age <= 32) return 1f;
            return 1f + Mathf.Clamp(age - 32, 0, 15) * 0.006f;
        }
    }

    public static class CompetitionSystem
    {
        private static readonly string[] PolishCities = { "Gdańsk", "Warsaw", "Poznań", "Wrocław", "Kraków", "Łódź", "Szczecin", "Lublin" };
        private static readonly string[] WorldCities = { "London", "Tokyo", "Doha", "New York", "Sydney", "Toronto", "Rio de Janeiro", "Nairobi" };

        public static List<CompetitionMeet> GenerateSeason(int year)
        {
            List<CompetitionMeet> result = new List<CompetitionMeet>();
            for (int week = 2; week <= 52; week += 2)
            {
                CompetitionRange range = week % 4 == 0 ? CompetitionRange.CityWide : CompetitionRange.Local;
                result.Add(CreateMeet(year, week, range, false, "POL", "Europe"));
            }

            for (int week = 6; week <= 48; week += 6)
                result.Add(CreateMeet(year, week, CompetitionRange.Regional, false, "POL", "Europe"));

            for (int i = 0; i < Geography.SupportedCountries.Length; i++)
            {
                string country = Geography.SupportedCountries[i];
                result.Add(CreateMeet(year, 18, CompetitionRange.Country, true, country, Geography.ContinentForCountry(country)));
                result.Add(CreateMeet(year, 34, CompetitionRange.Country, false, country, Geography.ContinentForCountry(country)));
            }

            for (int i = 0; i < Geography.SupportedContinents.Length; i++)
            {
                string continent = Geography.SupportedContinents[i];
                result.Add(CreateMeet(year, 28, CompetitionRange.Continent, true, "", continent));
            }

            result.Add(CreateMeet(year, 44, CompetitionRange.World, true, "", ""));
            result.Sort((a, b) => a.Week.CompareTo(b.Week));
            return result;
        }

        public static bool GeographyMatches(Athlete athlete, CompetitionMeet meet)
        {
            if (athlete == null || meet == null) return false;
            if (meet.Range == CompetitionRange.Country)
                return string.Equals(athlete.CountryCode, meet.CountryCode, StringComparison.OrdinalIgnoreCase);
            if (meet.Range == CompetitionRange.Continent)
                return Geography.ContinentForCountry(athlete.CountryCode) == meet.Continent;
            return true;
        }

        public static bool CanEnter(Athlete athlete, CompetitionMeet meet, DistanceType distance)
        {
            if (athlete == null || meet == null || meet.Week == null) return false;
            if (meet.Distances == null || !meet.Distances.Contains(distance)) return false;
            if (meet.AllowedCategories == null || !meet.AllowedCategories.Contains(athlete.Category)) return false;
            if (!GeographyMatches(athlete, meet)) return false;
            if (athlete.DistanceRating(distance) < RequiredRating(meet.Range)) return false;
            return true;
        }

        public static int RequiredRating(CompetitionRange range)
        {
            switch (range)
            {
                case CompetitionRange.Local: return 0;
                case CompetitionRange.CityWide: return 7;
                case CompetitionRange.Regional: return 15;
                case CompetitionRange.Country: return 28;
                case CompetitionRange.Continent: return 45;
                default: return 62;
            }
        }

        public static float ExpectedWinningTime(CompetitionMeet meet, AgeCategory category, DistanceType distance)
        {
            int rating = Mathf.Clamp(meet.FieldStrength + Mathf.Max(2, meet.FieldSpread / 2), 1, 99);
            return PerformanceModel.EstimateTime(distance, PerformanceModel.RepresentativeAge(category), rating);
        }

        public static float ExpectedAverageTime(CompetitionMeet meet, AgeCategory category, DistanceType distance)
        {
            return PerformanceModel.EstimateTime(distance, PerformanceModel.RepresentativeAge(category), meet.FieldStrength);
        }

        public static CompetitionMeet FindMeet(GameState state, string meetId)
        {
            if (state == null || state.CompetitionCalendar == null) return null;
            return state.CompetitionCalendar.Find(m => m.Id == meetId);
        }

        private static CompetitionMeet CreateMeet(int year, int week, CompetitionRange range, bool championship, string country, string continent)
        {
            int strength;
            int spread;
            int fee;
            int cash;
            int rep;
            string city;
            string name;

            switch (range)
            {
                case CompetitionRange.Local:
                    strength = UnityEngine.Random.Range(7, 16); spread = 8; fee = 10; cash = 40; rep = 2;
                    city = PolishCities[UnityEngine.Random.Range(0, PolishCities.Length)]; name = city + " Local Track Meet";
                    break;
                case CompetitionRange.CityWide:
                    strength = UnityEngine.Random.Range(11, 22); spread = 9; fee = 20; cash = 80; rep = 4;
                    city = PolishCities[UnityEngine.Random.Range(0, PolishCities.Length)]; name = city + " City Athletics Cup";
                    break;
                case CompetitionRange.Regional:
                    strength = UnityEngine.Random.Range(19, 34); spread = 10; fee = 45; cash = 180; rep = 8;
                    city = PolishCities[UnityEngine.Random.Range(0, PolishCities.Length)]; name = "Regional Athletics Challenge";
                    break;
                case CompetitionRange.Country:
                    strength = UnityEngine.Random.Range(34, 54); spread = 11; fee = 90; cash = 500; rep = 18;
                    city = Geography.CountryCity(country); name = Geography.CountryName(country) + (championship ? " Championships" : " Grand Prix");
                    break;
                case CompetitionRange.Continent:
                    strength = UnityEngine.Random.Range(53, 72); spread = 10; fee = 180; cash = 1500; rep = 45;
                    city = Geography.ContinentCity(continent); name = continent + (championship ? " Championships" : " Athletics Classic");
                    break;
                default:
                    strength = UnityEngine.Random.Range(72, 91); spread = 9; fee = 350; cash = 5000; rep = 100;
                    city = WorldCities[UnityEngine.Random.Range(0, WorldCities.Length)]; name = championship ? "World Championships" : "World Athletics Gala";
                    break;
            }

            return new CompetitionMeet
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                City = city,
                Range = range,
                CountryCode = country,
                Continent = continent,
                Week = new GameWeek(year, week),
                FieldStrength = strength,
                FieldSpread = spread,
                IsChampionship = championship,
                EntryFee = fee,
                BaseCashReward = cash,
                BaseReputationReward = rep,
                AllowedCategories = CategoriesForRange(range),
                Distances = new List<DistanceType> { DistanceType.M100, DistanceType.M200, DistanceType.M400, DistanceType.M800, DistanceType.M1500 }
            };
        }

        private static List<AgeCategory> CategoriesForRange(CompetitionRange range)
        {
            if (range == CompetitionRange.Local || range == CompetitionRange.CityWide || range == CompetitionRange.Regional)
                return new List<AgeCategory> { AgeCategory.U10, AgeCategory.U12, AgeCategory.U14, AgeCategory.U16, AgeCategory.U18, AgeCategory.Junior, AgeCategory.Adult, AgeCategory.Senior };
            if (range == CompetitionRange.Country)
                return new List<AgeCategory> { AgeCategory.U14, AgeCategory.U16, AgeCategory.U18, AgeCategory.Junior, AgeCategory.Adult, AgeCategory.Senior };
            if (range == CompetitionRange.Continent)
                return new List<AgeCategory> { AgeCategory.U16, AgeCategory.U18, AgeCategory.Junior, AgeCategory.Adult, AgeCategory.Senior };
            return new List<AgeCategory> { AgeCategory.U18, AgeCategory.Junior, AgeCategory.Adult, AgeCategory.Senior };
        }
    }

    public static class SaveSystem
    {
        private const string FileName = "track_dynasty_weekly_v1.json";
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
            try
            {
                GameState state = JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));
                return state != null && state.SaveVersion == 4 ? state : null;
            }
            catch { return null; }
        }

        public static void Delete()
        {
            if (Exists()) File.Delete(SavePath);
        }
    }
}
