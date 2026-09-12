using TrackDynasty.Mvp03.Domain;
using UnityEngine;

namespace TrackDynasty.Mvp03.Systems
{
    public static class TrainingSystem
    {
        public static void ApplyTrainingWeek(Athlete athlete, CoachProfile coach)
        {
            if (athlete == null) return;

            if (athlete.InjuryWeeks > 0)
            {
                athlete.InjuryWeeks--;
                athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue - 0.13f);
                athlete.Form = Mathf.Clamp(athlete.Form - 0.01f, 0.78f, 1.08f);
                return;
            }

            if (athlete.TrainingFocus == TrainingFocus.Recovery)
            {
                athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue - 0.24f);
                athlete.Form = Mathf.Clamp(athlete.Form + 0.018f, 0.78f, 1.08f);
                return;
            }

            float age = AgeModifier(athlete);
            float learner = athlete.HasTrait(TraitType.FastLearner) ? 1.10f : 1f;
            float fatigue = Mathf.Lerp(1f, 0.50f, athlete.Fatigue);
            float coachDistance = coach != null ? coach.DistanceMultiplier(athlete.TrainingDistance) : 1f;
            float coachFocus = coach != null ? coach.FocusMultiplier(athlete.TrainingFocus) : 1f;
            float mult = age * learner * athlete.DevelopmentRate * fatigue * coachDistance * coachFocus;

            float speed = 0f;
            float acceleration = 0f;
            float strength = 0f;
            float endurance = 0f;
            float technique = 0f;
            float mental = 0f;
            DistanceWeights(athlete.TrainingDistance, ref speed, ref acceleration, ref strength, ref endurance, ref technique, ref mental);

            BoostFocus(athlete.TrainingFocus, ref speed, ref acceleration, ref strength, ref endurance, ref technique, ref mental);

            const float weeklyBase = 0.095f;
            AddGrowth(ref athlete.Speed, ref athlete.SpeedProgress, athlete.Potential, weeklyBase * speed * mult);
            AddGrowth(ref athlete.Acceleration, ref athlete.AccelerationProgress, athlete.Potential, weeklyBase * acceleration * mult);
            AddGrowth(ref athlete.Strength, ref athlete.StrengthProgress, athlete.Potential, weeklyBase * strength * mult);
            AddGrowth(ref athlete.Endurance, ref athlete.EnduranceProgress, athlete.Potential, weeklyBase * endurance * mult);
            AddGrowth(ref athlete.Technique, ref athlete.TechniqueProgress, athlete.Potential, weeklyBase * technique * mult);
            AddGrowth(ref athlete.Mental, ref athlete.MentalProgress, athlete.Potential, weeklyBase * mental * mult);

            float load = athlete.TrainingDistance == DistanceType.M100 || athlete.TrainingDistance == DistanceType.M200 ? 0.085f : athlete.TrainingDistance == DistanceType.M400 ? 0.075f : 0.068f;
            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + load - 0.018f);
            athlete.Form = Mathf.Clamp(athlete.Form + Random.Range(-0.010f, 0.012f), 0.78f, 1.08f);

            float injuryChance = 0.002f + athlete.Fatigue * 0.020f;
            if (athlete.HasTrait(TraitType.InjuryProne)) injuryChance += 0.008f;
            if (coach != null && coach.StrongCategories != null && coach.StrongCategories.Contains(athlete.TrainingFocus)) injuryChance *= 0.72f;
            if (Random.value < injuryChance)
                athlete.InjuryWeeks = Random.Range(1, athlete.Fatigue > 0.75f ? 5 : 3);
        }

        public static void ApplyYearRollover(Athlete athlete)
        {
            if (athlete == null) return;
            athlete.Age++;
            NarrowPotentialEstimate(athlete);

            if (athlete.Age >= 30)
            {
                if (Random.value < 0.34f) athlete.Speed = Mathf.Max(1, athlete.Speed - 1);
                if (Random.value < 0.30f) athlete.Acceleration = Mathf.Max(1, athlete.Acceleration - 1);
            }
            if (athlete.Age >= 33 && Random.value < 0.28f)
                athlete.Endurance = Mathf.Max(1, athlete.Endurance - 1);

            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue * 0.45f);
            athlete.Form = Mathf.Clamp(0.95f + Random.Range(-0.025f, 0.025f), 0.86f, 1.04f);
        }

        private static void DistanceWeights(DistanceType distance, ref float speed, ref float acceleration, ref float strength, ref float endurance, ref float technique, ref float mental)
        {
            switch (distance)
            {
                case DistanceType.M100:
                    speed = 1.00f; acceleration = 1.15f; strength = 0.52f; endurance = 0.10f; technique = 0.48f; mental = 0.20f;
                    break;
                case DistanceType.M200:
                    speed = 1.00f; acceleration = 0.88f; strength = 0.50f; endurance = 0.32f; technique = 0.52f; mental = 0.22f;
                    break;
                case DistanceType.M400:
                    speed = 0.72f; acceleration = 0.48f; strength = 0.58f; endurance = 0.92f; technique = 0.62f; mental = 0.36f;
                    break;
                case DistanceType.M800:
                    speed = 0.32f; acceleration = 0.15f; strength = 0.42f; endurance = 1.12f; technique = 0.72f; mental = 0.58f;
                    break;
                default:
                    speed = 0.18f; acceleration = 0.08f; strength = 0.30f; endurance = 1.20f; technique = 0.78f; mental = 0.72f;
                    break;
            }
        }

        private static void BoostFocus(TrainingFocus focus, ref float speed, ref float acceleration, ref float strength, ref float endurance, ref float technique, ref float mental)
        {
            switch (focus)
            {
                case TrainingFocus.Speed: speed *= 1.65f; break;
                case TrainingFocus.Acceleration: acceleration *= 1.65f; break;
                case TrainingFocus.Strength: strength *= 1.65f; break;
                case TrainingFocus.Endurance: endurance *= 1.65f; break;
                case TrainingFocus.Technique: technique *= 1.65f; break;
                case TrainingFocus.Mental: mental *= 1.65f; break;
            }
        }

        private static float AgeModifier(Athlete athlete)
        {
            int age = athlete.Age;
            if (athlete.HasTrait(TraitType.LateBloomer))
            {
                if (age <= 14) return 0.88f;
                if (age <= 18) return 1.02f;
                if (age <= 24) return 1.20f;
                if (age <= 29) return 0.86f;
                return 0.40f;
            }

            if (age <= 10) return 1.08f;
            if (age <= 14) return 1.25f;
            if (age <= 18) return 1.16f;
            if (age <= 23) return 1.00f;
            if (age <= 28) return 0.78f;
            if (age <= 32) return 0.55f;
            return 0.34f;
        }

        private static void AddGrowth(ref int stat, ref float progress, int cap, float amount)
        {
            cap = Mathf.Clamp(cap, 1, 99);
            if (stat >= cap || amount <= 0f) return;
            float gap = cap - stat;
            float difficulty = Mathf.Lerp(0.20f, 1f, Mathf.Clamp01(gap / 30f));
            progress += amount * difficulty * Random.Range(0.91f, 1.09f);
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
