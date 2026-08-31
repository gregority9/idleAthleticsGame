using System;

namespace TrackDynasty.Mvp02
{
    public enum TrainingFocus
    {
        Sprint,
        Strength,
        Technique,
        Recovery
    }

    public enum CompetitionTier
    {
        Local,
        Regional,
        National,
        International
    }

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
        public int SigningFee;

        public string DisplayName
        {
            get { return FirstName + " " + LastName; }
        }
    }

    [Serializable]
    public class SeasonEvent
    {
        public string Name;
        public CompetitionTier Tier;
        public int Day;
        public bool IsChampionship;
        public bool Completed;
        public int BaseCashReward;
        public int BaseReputationReward;
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
    [Serializable]
    public class RaceSummary
    {
        public SeasonEvent Event;
        public Athlete Athlete;
    }
}
