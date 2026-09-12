using TrackDynasty.Mvp03.Domain;
using UnityEngine;

namespace TrackDynasty.Mvp03.Systems
{
    public static class TrainingSystem
    {
        public static void ApplyTrainingDay(Athlete athlete)
        {
            if (athlete == null) return;

            // Old saves may still contain Recovery as the training focus.
            if (athlete.TrainingFocus == TrainingFocus.Recovery)
            {
                athlete.TrainingFocus = TrainingFocus.Sprint;
                athlete.TrainingIntensity = TrainingIntensity.Rest;
            }

            if (athlete.TrainingIntensity == TrainingIntensity.Rest)
            {
                ApplyRestDay(athlete);
                return;
            }

            float ageModifier = AgeModifier(athlete);
            float learner = athlete.HasTrait(TraitType.FastLearner) ? 1.10f : 1f;
            float fatigueModifier = TrainingEfficiencyFromFatigue(athlete.Fatigue);
            float intensityGrowth = GrowthMultiplier(athlete.TrainingIntensity);
            float mult = ageModifier * learner * athlete.DevelopmentRate * fatigueModifier * intensityGrowth;

            switch (athlete.TrainingFocus)
            {
                case TrainingFocus.Sprint:
                    AddGrowth(ref athlete.Speed, ref athlete.SpeedProgress, athlete.Potential, 0.075f * mult);
                    AddGrowth(ref athlete.Acceleration, ref athlete.AccelerationProgress, athlete.Potential, 0.095f * mult);
                    athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + TrainingLoad(0.018f, athlete.TrainingIntensity));
                    break;
                case TrainingFocus.Strength:
                    AddGrowth(ref athlete.Strength, ref athlete.StrengthProgress, athlete.Potential - 2, 0.085f * mult);
                    AddGrowth(ref athlete.Acceleration, ref athlete.AccelerationProgress, athlete.Potential - 4, 0.035f * mult);
                    athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + TrainingLoad(0.020f, athlete.TrainingIntensity));
                    break;
                case TrainingFocus.Technique:
                    AddGrowth(ref athlete.Technique, ref athlete.TechniqueProgress, athlete.Potential - 1, 0.082f * mult);
                    AddGrowth(ref athlete.Mental, ref athlete.MentalProgress, athlete.Potential - 1, 0.048f * mult);
                    athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + TrainingLoad(0.014f, athlete.TrainingIntensity));
                    break;
            }

            // Small natural overnight recovery.
            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue - 0.006f);

            if (athlete.HasTrait(TraitType.InjuryProne))
                athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + 0.003f);

            athlete.Form = Mathf.Clamp(athlete.Form + Random.Range(-0.0025f, 0.0025f), 0.78f, 1.08f);

            if (athlete.Fatigue > 0.65f)
                athlete.Form = Mathf.Max(0.78f, athlete.Form - 0.004f);
            if (athlete.Fatigue > 0.85f && athlete.TrainingIntensity == TrainingIntensity.Hard)
                athlete.Form = Mathf.Max(0.78f, athlete.Form - 0.006f);
        }

        public static void ApplyFocusedCampDay(Athlete athlete)
        {
            if (athlete == null) return;
            TrainingIntensity previous = athlete.TrainingIntensity;
            athlete.TrainingIntensity = TrainingIntensity.Hard;
            ApplyTrainingDay(athlete);

            // Camp coaching bonus: an extra small development pulse in the current focus.
            float bonus = AgeModifier(athlete) * athlete.DevelopmentRate * TrainingEfficiencyFromFatigue(athlete.Fatigue) * 0.30f;
            if (athlete.HasTrait(TraitType.FastLearner)) bonus *= 1.10f;
            if (athlete.TrainingFocus == TrainingFocus.Sprint)
            {
                AddGrowth(ref athlete.Speed, ref athlete.SpeedProgress, athlete.Potential, 0.075f * bonus);
                AddGrowth(ref athlete.Acceleration, ref athlete.AccelerationProgress, athlete.Potential, 0.095f * bonus);
            }
            else if (athlete.TrainingFocus == TrainingFocus.Strength)
            {
                AddGrowth(ref athlete.Strength, ref athlete.StrengthProgress, athlete.Potential - 2, 0.085f * bonus);
            }
            else if (athlete.TrainingFocus == TrainingFocus.Technique)
            {
                AddGrowth(ref athlete.Technique, ref athlete.TechniqueProgress, athlete.Potential - 1, 0.082f * bonus);
                AddGrowth(ref athlete.Mental, ref athlete.MentalProgress, athlete.Potential - 1, 0.048f * bonus);
            }
            athlete.TrainingIntensity = previous;
        }

        public static void ApplyRecoveryCampDay(Athlete athlete)
        {
            if (athlete == null) return;
            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue - 0.080f);
            athlete.Form = Mathf.Clamp(athlete.Form + 0.006f, 0.78f, 1.08f);
        }

        public static void ApplyPhysio(Athlete athlete)
        {
            if (athlete == null) return;
            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue - 0.12f);
            athlete.Form = Mathf.Clamp(athlete.Form + 0.008f, 0.78f, 1.08f);
        }

        private static void ApplyRestDay(Athlete athlete)
        {
            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue - 0.055f);
            athlete.Form = Mathf.Clamp(athlete.Form + 0.004f, 0.78f, 1.08f);
        }

        public static float RacePerformanceMultiplier(float fatigue)
        {
            fatigue = Mathf.Clamp01(fatigue);
            if (fatigue <= 0.20f) return 1f;
            if (fatigue <= 0.50f) return Mathf.Lerp(1f, 0.985f, Mathf.InverseLerp(0.20f, 0.50f, fatigue));
            if (fatigue <= 0.75f) return Mathf.Lerp(0.985f, 0.95f, Mathf.InverseLerp(0.50f, 0.75f, fatigue));
            return Mathf.Lerp(0.95f, 0.88f, Mathf.InverseLerp(0.75f, 1f, fatigue));
        }

        public static float TrainingEfficiencyFromFatigue(float fatigue)
        {
            fatigue = Mathf.Clamp01(fatigue);
            if (fatigue <= 0.20f) return 1f;
            if (fatigue <= 0.50f) return Mathf.Lerp(1f, 0.86f, Mathf.InverseLerp(0.20f, 0.50f, fatigue));
            if (fatigue <= 0.75f) return Mathf.Lerp(0.86f, 0.65f, Mathf.InverseLerp(0.50f, 0.75f, fatigue));
            return Mathf.Lerp(0.65f, 0.35f, Mathf.InverseLerp(0.75f, 1f, fatigue));
        }

        public static TrainingIntensity RecommendedIntensity(Athlete athlete, int daysUntilRace = -1)
        {
            if (athlete == null) return TrainingIntensity.Normal;
            float f = athlete.Fatigue;

            if (daysUntilRace >= 0 && daysUntilRace <= 2)
                return f >= 0.35f ? TrainingIntensity.Rest : TrainingIntensity.Light;

            if (f >= 0.72f) return TrainingIntensity.Rest;
            if (f >= 0.50f) return TrainingIntensity.Light;
            if (f >= 0.22f) return TrainingIntensity.Normal;
            return TrainingIntensity.Hard;
        }

        public static string FatigueLabel(float fatigue)
        {
            if (fatigue < 0.20f) return "FRESH";
            if (fatigue < 0.50f) return "NORMAL LOAD";
            if (fatigue < 0.70f) return "TIRED";
            if (fatigue < 0.85f) return "VERY TIRED";
            return "OVERLOADED";
        }

        public static string IntensityLabel(TrainingIntensity intensity)
        {
            if (intensity == TrainingIntensity.Light) return "LIGHT";
            if (intensity == TrainingIntensity.Hard) return "HARD";
            if (intensity == TrainingIntensity.Rest) return "REST";
            return "NORMAL";
        }

        public static float EstimatedDailyFatigueDelta(Athlete athlete, TrainingIntensity intensity)
        {
            if (athlete == null) return 0f;
            if (intensity == TrainingIntensity.Rest) return -0.055f;

            float raw = athlete.TrainingFocus == TrainingFocus.Strength ? 0.020f :
                        athlete.TrainingFocus == TrainingFocus.Technique ? 0.014f : 0.018f;
            float delta = TrainingLoad(raw, intensity) - 0.006f;
            if (athlete.HasTrait(TraitType.InjuryProne)) delta += 0.003f;
            return delta;
        }

        private static float GrowthMultiplier(TrainingIntensity intensity)
        {
            if (intensity == TrainingIntensity.Light) return 0.65f;
            if (intensity == TrainingIntensity.Hard) return 1.35f;
            return 1f;
        }

        private static float TrainingLoad(float normalLoad, TrainingIntensity intensity)
        {
            if (intensity == TrainingIntensity.Light) return normalLoad * 0.48f;
            if (intensity == TrainingIntensity.Hard) return normalLoad * 1.65f;
            return normalLoad;
        }

        public static void ApplyYearRollover(Athlete athlete)
        {
            if (athlete == null) return;
            NarrowPotentialEstimate(athlete);
            athlete.Age++;
            athlete.YearsCompleted++;

            if (athlete.Age >= 30 && Random.value < 0.45f) athlete.Speed = Mathf.Max(40, athlete.Speed - 1);
            if (athlete.Age >= 31 && Random.value < 0.40f) athlete.Acceleration = Mathf.Max(40, athlete.Acceleration - 1);
            if (athlete.Age >= 33 && Random.value < 0.32f) athlete.Strength = Mathf.Max(40, athlete.Strength - 1);

            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue * 0.45f);
            athlete.Form = Mathf.Clamp(0.95f + Random.Range(-0.03f, 0.03f), 0.86f, 1.04f);
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
            progress += amount * difficulty * Random.Range(0.92f, 1.08f);
            while (progress >= 1f && stat < cap)
            {
                progress -= 1f;
                stat++;
            }
        }

        private static void NarrowPotentialEstimate(Athlete athlete)
        {
            if (athlete.PotentialMin < athlete.Potential)
                athlete.PotentialMin = Mathf.Min(athlete.Potential, athlete.PotentialMin + Random.Range(1, 4));
            if (athlete.PotentialMax > athlete.Potential)
                athlete.PotentialMax = Mathf.Max(athlete.Potential, athlete.PotentialMax - Random.Range(1, 4));
        }
    }
}
