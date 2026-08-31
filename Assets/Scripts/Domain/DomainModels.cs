using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrackDynasty.Mvp03.Domain
{
    public enum TrainingFocus { Sprint, Strength, Technique, Recovery }
    public enum CompetitionTier { Local, Regional, National, International, Elite }
    public enum RaceStrategy { ExplosiveStart, Balanced, LatePush }
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

    public enum ScoutSpecialty
    {
        Evaluation,
        TalentNetwork,
        BargainHunter
    }
}

namespace TrackDynasty.Mvp03.Domain
{
    [Serializable]
    public class GameDate
    {
        public int Year;
        public int Month;
        public int Day;

        public GameDate() { }

        public GameDate(int year, int month, int day)
        {
            Year = year;
            Month = month;
            Day = day;
        }

        public DateTime ToDateTime()
        {
            int year = Math.Max(1, Year);
            int month = Math.Min(12, Math.Max(1, Month));
            int day = Math.Min(DateTime.DaysInMonth(year, month), Math.Max(1, Day));
            return new DateTime(year, month, day);
        }

        public static GameDate FromDateTime(DateTime value)
        {
            return new GameDate(value.Year, value.Month, value.Day);
        }

        public GameDate AddDays(int days)
        {
            return FromDateTime(ToDateTime().AddDays(days));
        }

        public int CompareTo(GameDate other)
        {
            if (other == null) return 1;
            return DateTime.Compare(ToDateTime(), other.ToDateTime());
        }

        public bool IsSameDay(GameDate other)
        {
            return other != null && Year == other.Year && Month == other.Month && Day == other.Day;
        }

        public string LongLabel => ToDateTime().ToString("dd MMMM yyyy");
        public string ShortLabel => ToDateTime().ToString("dd MMM yyyy");
    }

    [Serializable]
    public class CompetitionOffer
    {
        public string Id;
        public string AthleteId;
        public string Name;
        public string City;
        public CompetitionTier Tier;
        public GameDate Date;
        public float EntryStandard;
        public int CashReward;
        public int ReputationReward;
        public bool IsChampionship;
    }

    [Serializable]
    public class ScoutProfile
    {
        public string Id;
        public string Name;
        public string CountryCode;
        public ScoutSpecialty Specialty;
        public int Evaluation;
        public int Network;
        public int MonthlySalary;
        public string Description;
    }

    [Serializable]
    public class ClubApplication
    {
        public string Id;
        public Prospect Prospect;
        public GameDate AppliedDate;
        public GameDate ExpiresDate;
        public string Reason;
    }

    [Serializable]
    public class GameState
    {
        public int SaveVersion = 3;
        public GameDate CurrentDate = new GameDate(2027, 1, 1);
        public int Cash = 6200;
        public int Reputation = 120;
        public float ClubRecord100m = 10.72f;
        public float WorldRecord100m = 9.58f;
        public string ClubRecordHolder = "Andre Campbell";
        public string SelectedAthleteId;
        public ScoutProfile ChosenScout;
        public List<ScoutProfile> StartingScoutChoices = new List<ScoutProfile>();
        public List<Athlete> Roster = new List<Athlete>();
        public List<Prospect> ScoutedProspects = new List<Prospect>();
        public List<ClubApplication> Applications = new List<ClubApplication>();
        public List<HallOfFameEntry> HallOfFame = new List<HallOfFameEntry>();
    }
}

namespace TrackDynasty.Mvp03.Domain
{
    [Serializable]
    public class Prospect
    {
        public string Id;
        public string FirstName;
        public string LastName;
        public string CountryCode;
        public int Age;
        public int Speed;
        public int Acceleration;
        public int Strength;
        public int Technique;
        public int Mental;
        public int Potential;
        public int PotentialMin;
        public int PotentialMax;
        public float DevelopmentRate;
        public int SigningFee;
        public List<TraitType> Traits = new List<TraitType>();
        public string DisplayName => FirstName + " " + LastName;
        public float BaseRating => Speed * 0.40f + Acceleration * 0.30f + Technique * 0.12f + Strength * 0.08f + Mental * 0.10f;
    }

    [Serializable]
    public class HallOfFameEntry
    {
        public string Name;
        public string CountryCode;
        public int RetireAge;
        public int Races;
        public int Wins;
        public int Championships;
        public float PersonalBest;
    }
}

namespace TrackDynasty.Mvp03.Domain
{
    [Serializable]
    public class Athlete
    {
        public string Id;
        public string FirstName;
        public string LastName;
        public string CountryCode;
        public int Age;

        [Range(1, 99)] public int Speed;
        [Range(1, 99)] public int Acceleration;
        [Range(1, 99)] public int Strength;
        [Range(1, 99)] public int Technique;
        [Range(1, 99)] public int Mental;

        [Range(0.75f, 1.10f)] public float Form = 0.95f;
        [Range(0f, 1f)] public float Fatigue = 0.10f;

        public int Potential = 90;
        public int PotentialMin = 80;
        public int PotentialMax = 95;
        public float DevelopmentRate = 1f;

        public float PersonalBest = 99f;
        public float YearStartPersonalBest = 99f;
        public int Races;
        public int Wins;
        public int Championships;
        public int YearsCompleted;
        public TrainingFocus TrainingFocus = TrainingFocus.Sprint;

        public CompetitionOffer ScheduledCompetition;
        public List<CompetitionOffer> CompetitionOffers = new List<CompetitionOffer>();
        public List<TraitType> Traits = new List<TraitType>();
        public List<RaceHistoryEntry> RaceHistory = new List<RaceHistoryEntry>();
        public List<SeasonHistoryEntry> SeasonHistory = new List<SeasonHistoryEntry>();

        public float SpeedProgress;
        public float AccelerationProgress;
        public float StrengthProgress;
        public float TechniqueProgress;
        public float MentalProgress;

        public string DisplayName => FirstName + " " + LastName;

        public float BaseRating =>
            Speed * 0.40f +
            Acceleration * 0.30f +
            Technique * 0.12f +
            Strength * 0.08f +
            Mental * 0.10f;

        public int Overall => Mathf.RoundToInt(BaseRating);

        public bool HasTrait(TraitType trait)
        {
            return Traits != null && Traits.Contains(trait);
        }
    }

    [Serializable]
    public class RaceHistoryEntry
    {
        public int Year;
        public int Month;
        public int Day;
        public string EventName;
        public CompetitionTier Tier;
        public int Place;
        public float Time;
        public bool PersonalBest;
        public bool ClubRecord;
        public bool WorldRecord;
    }

    [Serializable]
    public class SeasonHistoryEntry
    {
        public int Year;
        public int StartAge;
        public int EndAge;
        public float PbAtStart;
        public float PbAtEnd;
        public int Races;
        public int Wins;
        public int Championships;
    }
}

namespace TrackDynasty.Mvp03.Domain
{
    [Serializable]
    public class RaceRunner
    {
        public int Lane;
        public string Name;
        public string CountryCode;
        public bool IsPlayer;
        public float FinishTime;
        public float[] SplitTimes = new float[5];
    }

    [Serializable]
    public class RaceResult
    {
        public string EventName;
        public string City;
        public GameDate Date;
        public CompetitionTier Tier;
        public bool IsChampionship;
        public List<RaceRunner> Runners = new List<RaceRunner>();
        public List<RaceRunner> Standings = new List<RaceRunner>();
        public int PlayerPlace;
        public float PlayerTime;
        public float PreviousPersonalBest;
        public float PreviousClubRecord;
        public bool NewPersonalBest;
        public bool NewClubRecord;
        public bool NewWorldRecord;
        public bool PhotoFinish;
        public int CashReward;
        public int ReputationReward;
    }
}
