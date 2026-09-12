using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrackDynasty.Mvp03.Domain
{
    public enum DistanceType { M100 = 100, M200 = 200, M400 = 400, M800 = 800, M1500 = 1500 }
    public enum AgeCategory { U10, U12, U14, U16, U18, Junior, Adult, Senior }
    public enum CompetitionRange { Local, CityWide, Regional, Country, Continent, World }
    public enum TrainingFocus { Speed, Acceleration, Strength, Endurance, Technique, Mental, Recovery }
    public enum RaceStrategy { FastStart, Balanced, LateKick }
    public enum StaffRole { Coach, StrengthCoach, Dietitian, Physiotherapist }
    public enum TraitType
    {
        ExplosiveStarter,
        StrongFinisher,
        BigStagePerformer,
        FastLearner,
        InjuryProne,
        LateBloomer,
        Consistent,
        Volatile
    }

    public static class DomainLabels
    {
        public static string Distance(DistanceType distance) => ((int)distance) + "m";

        public static string Range(CompetitionRange range)
        {
            switch (range)
            {
                case CompetitionRange.CityWide: return "CITY-WIDE";
                default: return range.ToString().ToUpperInvariant();
            }
        }
    }

    public static class AgeCategories
    {
        public static AgeCategory ForAge(int age)
        {
            if (age <= 10) return AgeCategory.U10;
            if (age <= 12) return AgeCategory.U12;
            if (age <= 14) return AgeCategory.U14;
            if (age <= 16) return AgeCategory.U16;
            if (age <= 18) return AgeCategory.U18;
            if (age <= 23) return AgeCategory.Junior;
            if (age <= 32) return AgeCategory.Adult;
            return AgeCategory.Senior;
        }
    }

    [Serializable]
    public class GameWeek
    {
        public int Year = 2026;
        public int Week = 1;

        public GameWeek() { }
        public GameWeek(int year, int week) { Year = year; Week = week; }

        public int AbsoluteIndex => Year * 52 + Mathf.Clamp(Week, 1, 52) - 1;
        public string Label => "WEEK " + Mathf.Clamp(Week, 1, 52).ToString("00") + "/52 • " + Year;
        public string ShortLabel => "W" + Mathf.Clamp(Week, 1, 52).ToString("00") + " " + Year;

        public GameWeek AddWeeks(int amount)
        {
            int zeroBased = (Year * 52 + Mathf.Clamp(Week, 1, 52) - 1) + amount;
            int year = Mathf.FloorToInt(zeroBased / 52f);
            int week = zeroBased - year * 52 + 1;
            return new GameWeek(year, week);
        }

        public int CompareTo(GameWeek other)
        {
            if (other == null) return 1;
            return AbsoluteIndex.CompareTo(other.AbsoluteIndex);
        }

        public bool IsSame(GameWeek other) => other != null && Year == other.Year && Week == other.Week;
    }

    [Serializable]
    public class DistanceAptitude
    {
        public DistanceType Distance;
        [Range(-8, 8)] public int Bonus;
    }

    [Serializable]
    public class DistanceRecord
    {
        public DistanceType Distance;
        public float PersonalBest;
        public int Races;
        public int Wins;

        public bool HasPersonalBest => PersonalBest > 0f;
    }

    [Serializable]
    public class ScheduledCompetition
    {
        public string MeetId;
        public DistanceType Distance;
    }

    [Serializable]
    public class Athlete
    {
        public string Id;
        public string FirstName;
        public string LastName;
        public string CountryCode = "POL";
        public int Age = 8;

        [Range(1, 99)] public int Speed;
        [Range(1, 99)] public int Acceleration;
        [Range(1, 99)] public int Strength;
        [Range(1, 99)] public int Endurance;
        [Range(1, 99)] public int Technique;
        [Range(1, 99)] public int Mental;

        [Range(0.75f, 1.10f)] public float Form = 0.95f;
        [Range(0f, 1f)] public float Fatigue = 0.08f;
        public int InjuryWeeks;

        public int Potential;
        public int PotentialMin;
        public int PotentialMax;
        public float DevelopmentRate = 1f;

        public float SpeedProgress;
        public float AccelerationProgress;
        public float StrengthProgress;
        public float EnduranceProgress;
        public float TechniqueProgress;
        public float MentalProgress;

        public DistanceType TrainingDistance = DistanceType.M200;
        public TrainingFocus TrainingFocus = TrainingFocus.Speed;
        public string AssignedCoachId;

        public float SponsorInterest;
        public int CareerRaces;
        public int CareerWins;

        public List<DistanceAptitude> Aptitudes = new List<DistanceAptitude>();
        public List<DistanceRecord> Records = new List<DistanceRecord>();
        public List<TraitType> Traits = new List<TraitType>();
        public List<RaceHistoryEntry> RaceHistory = new List<RaceHistoryEntry>();
        public ScheduledCompetition ScheduledCompetition;

        public string DisplayName => FirstName + " " + LastName;
        public AgeCategory Category => AgeCategories.ForAge(Age);

        public bool HasTrait(TraitType trait) => Traits != null && Traits.Contains(trait);

        public int GetAptitude(DistanceType distance)
        {
            if (Aptitudes == null) return 0;
            DistanceAptitude value = Aptitudes.Find(a => a.Distance == distance);
            return value != null ? value.Bonus : 0;
        }

        public DistanceRecord GetRecord(DistanceType distance)
        {
            if (Records == null) Records = new List<DistanceRecord>();
            DistanceRecord record = Records.Find(r => r.Distance == distance);
            if (record == null)
            {
                record = new DistanceRecord { Distance = distance };
                Records.Add(record);
            }
            return record;
        }

        public int DistanceRating(DistanceType distance)
        {
            float rating;
            switch (distance)
            {
                case DistanceType.M100:
                    rating = Speed * 0.35f + Acceleration * 0.35f + Strength * 0.10f + Technique * 0.12f + Mental * 0.08f;
                    break;
                case DistanceType.M200:
                    rating = Speed * 0.32f + Acceleration * 0.25f + Endurance * 0.12f + Strength * 0.10f + Technique * 0.13f + Mental * 0.08f;
                    break;
                case DistanceType.M400:
                    rating = Speed * 0.23f + Acceleration * 0.15f + Endurance * 0.28f + Strength * 0.12f + Technique * 0.14f + Mental * 0.08f;
                    break;
                case DistanceType.M800:
                    rating = Speed * 0.10f + Acceleration * 0.05f + Endurance * 0.45f + Strength * 0.10f + Technique * 0.18f + Mental * 0.12f;
                    break;
                default:
                    rating = Speed * 0.05f + Acceleration * 0.02f + Endurance * 0.52f + Strength * 0.06f + Technique * 0.18f + Mental * 0.17f;
                    break;
            }
            rating += GetAptitude(distance);
            return Mathf.Clamp(Mathf.RoundToInt(rating), 1, 99);
        }

        public int Overall
        {
            get
            {
                int best = 0;
                int second = 0;
                DistanceType[] distances = { DistanceType.M100, DistanceType.M200, DistanceType.M400, DistanceType.M800, DistanceType.M1500 };
                for (int i = 0; i < distances.Length; i++)
                {
                    int value = DistanceRating(distances[i]);
                    if (value > best) { second = best; best = value; }
                    else if (value > second) second = value;
                }
                return Mathf.RoundToInt(best * 0.65f + second * 0.35f);
            }
        }
    }

    [Serializable]
    public class AthleteCandidate
    {
        public string Id;
        public string FirstName;
        public string LastName;
        public string CountryCode = "POL";
        public int Age;
        public int Speed;
        public int Acceleration;
        public int Strength;
        public int Endurance;
        public int Technique;
        public int Mental;
        public int Potential;
        public int PotentialMin;
        public int PotentialMax;
        public float DevelopmentRate;
        public int SigningFee;
        public List<DistanceAptitude> Aptitudes = new List<DistanceAptitude>();
        public List<TraitType> Traits = new List<TraitType>();

        public string DisplayName => FirstName + " " + LastName;

        public Athlete ToAthlete()
        {
            Athlete athlete = new Athlete
            {
                Id = Id,
                FirstName = FirstName,
                LastName = LastName,
                CountryCode = CountryCode,
                Age = Age,
                Speed = Speed,
                Acceleration = Acceleration,
                Strength = Strength,
                Endurance = Endurance,
                Technique = Technique,
                Mental = Mental,
                Potential = Potential,
                PotentialMin = PotentialMin,
                PotentialMax = PotentialMax,
                DevelopmentRate = DevelopmentRate,
                Aptitudes = new List<DistanceAptitude>(Aptitudes),
                Traits = new List<TraitType>(Traits),
                Form = 0.95f,
                Fatigue = 0.08f
            };
            DistanceType[] distances = { DistanceType.M100, DistanceType.M200, DistanceType.M400, DistanceType.M800, DistanceType.M1500 };
            for (int i = 0; i < distances.Length; i++) athlete.Records.Add(new DistanceRecord { Distance = distances[i] });
            return athlete;
        }
    }

    [Serializable]
    public class CoachProfile
    {
        public string Id;
        public string Name;
        public DistanceType PrimaryDistance;
        public List<DistanceType> SecondaryDistances = new List<DistanceType>();
        public List<TrainingFocus> StrongCategories = new List<TrainingFocus>();
        public int Quality = 1;
        public int Capacity = 5;
        public int WeeklySalary;

        public float DistanceMultiplier(DistanceType distance)
        {
            if (distance == PrimaryDistance) return 1f + Quality * 0.03f;
            if (SecondaryDistances != null && SecondaryDistances.Contains(distance)) return 0.86f + Quality * 0.02f;
            return 0.66f + Quality * 0.01f;
        }

        public float FocusMultiplier(TrainingFocus focus)
        {
            return StrongCategories != null && StrongCategories.Contains(focus) ? 1.08f + Quality * 0.01f : 1f;
        }
    }

    [Serializable]
    public class StaffMember
    {
        public string Id;
        public string Name;
        public StaffRole Role;
        public int Quality;
        public int WeeklySalary;
    }

    [Serializable]
    public class CompetitionMeet
    {
        public string Id;
        public string Name;
        public string City;
        public CompetitionRange Range;
        public string CountryCode;
        public string Continent;
        public GameWeek Week;
        public List<AgeCategory> AllowedCategories = new List<AgeCategory>();
        public List<DistanceType> Distances = new List<DistanceType>();
        public int FieldStrength;
        public int FieldSpread;
        public bool IsChampionship;
        public int EntryFee;
        public int BaseCashReward;
        public int BaseReputationReward;
    }

    [Serializable]
    public class ClubApplication
    {
        public string Id;
        public AthleteCandidate Candidate;
        public GameWeek AppliedWeek;
        public GameWeek ExpiresWeek;
        public string Reason;
    }

    [Serializable]
    public class SponsorOffer
    {
        public string Id;
        public string AthleteId;
        public string BrandName;
        public int SigningBonus;
        public int WeeklyPayment;
        public int WinBonus;
        public int DurationWeeks;
    }

    [Serializable]
    public class SponsorContract
    {
        public string Id;
        public string AthleteId;
        public string BrandName;
        public int WeeklyPayment;
        public int WinBonus;
        public int WeeksRemaining;
    }

    [Serializable]
    public class ClubRecordEntry
    {
        public DistanceType Distance;
        public float Time;
        public string Holder;
    }

    [Serializable]
    public class RaceHistoryEntry
    {
        public int Year;
        public int Week;
        public string EventName;
        public CompetitionRange Range;
        public DistanceType Distance;
        public int Place;
        public float Time;
        public bool PersonalBest;
        public bool ClubRecord;
    }

    [Serializable]
    public class RaceRunner
    {
        public int Lane;
        public string Name;
        public string CountryCode;
        public bool IsPlayer;
        public float FinishTime;
        public float[] SplitTimes;
    }

    [Serializable]
    public class RaceResult
    {
        public string EventName;
        public string City;
        public GameWeek Week;
        public CompetitionRange Range;
        public DistanceType Distance;
        public AgeCategory Category;
        public bool IsChampionship;
        public List<RaceRunner> Runners = new List<RaceRunner>();
        public List<RaceRunner> Standings = new List<RaceRunner>();
        public int PlayerPlace;
        public float PlayerTime;
        public float PreviousPersonalBest;
        public float PreviousClubRecord;
        public bool NewPersonalBest;
        public bool NewClubRecord;
        public bool PhotoFinish;
        public int CashReward;
        public int ReputationReward;
        public float SponsorInterestGain;
    }

    [Serializable]
    public class ManagementProfile
    {
        public string Name = "My Athletics Management";
        public int Reputation;
    }

    [Serializable]
    public class GameState
    {
        public int SaveVersion = 4;
        public bool SetupCompleted;
        public GameWeek CurrentWeek = new GameWeek(2026, 1);
        public int Cash = 5000;
        public ManagementProfile Management = new ManagementProfile();
        public string SelectedAthleteId;

        public List<AthleteCandidate> StarterChoices = new List<AthleteCandidate>();
        public List<Athlete> Roster = new List<Athlete>();
        public List<CompetitionMeet> CompetitionCalendar = new List<CompetitionMeet>();
        public List<ClubApplication> Applications = new List<ClubApplication>();
        public List<CoachProfile> Coaches = new List<CoachProfile>();
        public List<StaffMember> Staff = new List<StaffMember>();
        public List<SponsorOffer> SponsorOffers = new List<SponsorOffer>();
        public List<SponsorContract> SponsorContracts = new List<SponsorContract>();
        public List<ClubRecordEntry> ClubRecords = new List<ClubRecordEntry>();

        public bool AutoManagementUnlocked => Roster != null && Roster.Count >= 10;

        public float GetClubRecord(DistanceType distance)
        {
            if (ClubRecords == null) return 0f;
            ClubRecordEntry record = ClubRecords.Find(r => r.Distance == distance);
            return record != null ? record.Time : 0f;
        }

        public void SetClubRecord(DistanceType distance, float time, string holder)
        {
            if (ClubRecords == null) ClubRecords = new List<ClubRecordEntry>();
            ClubRecordEntry record = ClubRecords.Find(r => r.Distance == distance);
            if (record == null)
            {
                record = new ClubRecordEntry { Distance = distance };
                ClubRecords.Add(record);
            }
            record.Time = time;
            record.Holder = holder;
        }
    }
}
