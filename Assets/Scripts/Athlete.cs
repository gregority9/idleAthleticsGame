using System;
using UnityEngine;

namespace TrackDynasty.Mvp02
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

        [Range(0.7f, 1.1f)] public float Form = 1f;
        [Range(0f, 1f)] public float Fatigue = 0.15f;

        public int Potential = 90;
        public int PotentialMin = 80;
        public int PotentialMax = 94;
        public float PersonalBest = 10.72f;

        public int Races;
        public int Wins;
        public int Championships;
        public int SeasonsCompleted;
        public bool Retired;
        public TrainingFocus Training = TrainingFocus.Sprint;

        public float SpeedProgress;
        public float AccelerationProgress;
        public float StrengthProgress;
        public float TechniqueProgress;
        public float MentalProgress;

        public string DisplayName
        {
            get { return FirstName + " " + LastName; }
        }

        public float BaseRating
        {
            get
            {
                return Speed * 0.40f +
                       Acceleration * 0.30f +
                       Technique * 0.12f +
                       Strength * 0.08f +
                       Mental * 0.10f;
            }
        }

        public int Overall
        {
            get { return Mathf.RoundToInt(BaseRating); }
        }
    }
}
