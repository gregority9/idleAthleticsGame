using System;
using TrackDynasty.Mvp03.Domain;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace TrackDynasty.Mvp03.Systems
{
    public static class TrainingSystem
    {
        public static void ApplyTrainingDay(Athlete athlete)
        {
            if (athlete == null) return;

            float ageModifier = AgeModifier(athlete);
            float learner = athlete.HasTrait(TraitType.FastLearner) ? 1.10f : 1f;
            float fatigueModifier = Mathf.Lerp(1f, 0.50f, athlete.Fatigue);
            float mult = ageModifier * learner * athlete.DevelopmentRate * fatigueModifier;

            switch (athlete.TrainingFocus)
            {
                case TrainingFocus.Sprint:
                    AddGrowth(ref athlete.Speed, ref athlete.SpeedProgress, athlete.Potential, 0.075f * mult);
                    AddGrowth(ref athlete.Acceleration, ref athlete.AccelerationProgress, athlete.Potential, 0.095f * mult);
                    athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + 0.018f);
                    break;
                case TrainingFocus.Strength:
                    AddGrowth(ref athlete.Strength, ref athlete.StrengthProgress, athlete.Potential - 2, 0.085f * mult);
                    AddGrowth(ref athlete.Acceleration, ref athlete.AccelerationProgress, athlete.Potential - 4, 0.035f * mult);
                    athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + 0.020f);
                    break;
                case TrainingFocus.Technique:
                    AddGrowth(ref athlete.Technique, ref athlete.TechniqueProgress, athlete.Potential - 1, 0.082f * mult);
                    AddGrowth(ref athlete.Mental, ref athlete.MentalProgress, athlete.Potential - 1, 0.048f * mult);
                    athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + 0.014f);
                    break;
                case TrainingFocus.Recovery:
                    athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue - 0.065f);
                    athlete.Form = Mathf.Clamp(athlete.Form + 0.006f, 0.78f, 1.08f);
                    return;
            }

            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue - 0.006f);
            if (athlete.HasTrait(TraitType.InjuryProne)) athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + 0.003f);
            athlete.Form = Mathf.Clamp(athlete.Form + UnityEngine.Random.Range(-0.0025f, 0.0025f), 0.78f, 1.08f);
            if (athlete.Fatigue > 0.62f) athlete.Form = Mathf.Max(0.78f, athlete.Form - 0.004f);
        }

        public static void ApplyYearRollover(Athlete athlete)
        {
            if (athlete == null) return;
            NarrowPotentialEstimate(athlete);
            athlete.Age++;
            athlete.YearsCompleted++;

            if (athlete.Age >= 30 && UnityEngine.Random.value < 0.45f) athlete.Speed = Mathf.Max(40, athlete.Speed - 1);
            if (athlete.Age >= 31 && UnityEngine.Random.value < 0.40f) athlete.Acceleration = Mathf.Max(40, athlete.Acceleration - 1);
            if (athlete.Age >= 33 && UnityEngine.Random.value < 0.32f) athlete.Strength = Mathf.Max(40, athlete.Strength - 1);

            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue * 0.45f);
            athlete.Form = Mathf.Clamp(0.95f + UnityEngine.Random.Range(-0.03f, 0.03f), 0.86f, 1.04f);
        }

        private static float AgeModifier(Athlete athlete)
        {
            if (athlete.HasTrait(TraitType.LateBloomer))
            {
                if (athlete.Age <= 19) return 0.82f;
                if (athlete.Age <= 24) return 1.16f;
                if (athlete.Age <= 28) return 0.94f;
                return 0.45f;
            }
            if (athlete.Age <= 20) return 1.15f;
            if (athlete.Age <= 24) return 1.00f;
            if (athlete.Age <= 28) return 0.70f;
            return 0.34f;
        }

        private static void AddGrowth(ref int stat, ref float progress, int cap, float amount)
        {
            cap = Mathf.Clamp(cap, 1, 99);
            if (stat >= cap) return;
            float gap = cap - stat;
            float difficulty = Mathf.Lerp(0.18f, 1f, Mathf.Clamp01(gap / 20f));
            progress += amount * difficulty * UnityEngine.Random.Range(0.92f, 1.08f);
            while (progress >= 1f && stat < cap)
            {
                progress -= 1f;
                stat++;
            }
        }

        private static void NarrowPotentialEstimate(Athlete athlete)
        {
            if (athlete.PotentialMin < athlete.Potential)
                athlete.PotentialMin = Mathf.Min(athlete.Potential, athlete.PotentialMin + UnityEngine.Random.Range(1, 4));
            if (athlete.PotentialMax > athlete.Potential)
                athlete.PotentialMax = Mathf.Max(athlete.Potential, athlete.PotentialMax - UnityEngine.Random.Range(1, 4));
        }
    }
}
