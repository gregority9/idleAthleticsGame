using System;
using System.Collections.Generic;

namespace TrackDynasty.Mvp02
{
    [Serializable]
    public class RaceRunner
    {
        public string Name;
        public string CountryCode;
        public bool IsPlayer;
        public float FinishTime;
        public float[] SplitTimes = new float[5];
    }

    [Serializable]
    public class RaceResult
    {
        public List<RaceRunner> Runners = new List<RaceRunner>();
        public List<RaceRunner> Standings = new List<RaceRunner>();
        public int PlayerPlace;
        public float PlayerTime;
        public float PreviousPersonalBest;
        public bool NewPersonalBest;
        public int CashReward;
        public int ReputationReward;
        public string EventName;
        public CompetitionTier Tier;
        public bool IsChampionship;
    }
}
