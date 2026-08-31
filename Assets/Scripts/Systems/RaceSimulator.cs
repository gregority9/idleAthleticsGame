using System;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Domain;
using UnityEngine;

namespace TrackDynasty.Mvp03.Systems
{
    public static class RaceSimulator
    {
        private static readonly string[] FirstNames = { "Jamal", "Tyrese", "Elijah", "Marcus", "Devonte", "Isaiah", "Caleb", "Noah", "Luca", "Kofi", "Milan", "Mateo", "Joshua", "Liam" };
        private static readonly string[] LastNames = { "Reid", "Walker", "Brooks", "Lee", "Smith", "Johnson", "Brown", "Schneider", "Romano", "Mensah", "Costa", "Pierre", "Turner", "Cole" };
        private static readonly string[] Countries = { "USA", "GBR", "CAN", "NGR", "FRA", "GER", "ITA", "JAM", "POL", "BRA" };

        public static RaceResult Simulate(GameState state, Athlete athlete, CompetitionOffer offer, RaceStrategy strategy)
        {
            RaceResult result = new RaceResult
            {
                EventName = offer.Name,
                City = offer.City,
                Date = new GameDate(offer.Date.Year, offer.Date.Month, offer.Date.Day),
                Tier = offer.Tier,
                IsChampionship = offer.IsChampionship,
                PreviousPersonalBest = athlete.PersonalBest,
                PreviousClubRecord = state.ClubRecord100m
            };

            List<RaceRunner> runners = new List<RaceRunner>();
            runners.Add(BuildPlayer(athlete, offer, strategy));

            float tierRating = TierRating(offer.Tier);
            for (int i = 0; i < 7; i++)
                runners.Add(BuildOpponent(tierRating, offer.IsChampionship));

            Shuffle(runners);
            for (int i = 0; i < runners.Count; i++)
                runners[i].Lane = i + 1;

            result.Runners = runners;
            result.Standings = new List<RaceRunner>(runners);
            result.Standings.Sort((a, b) => a.FinishTime.CompareTo(b.FinishTime));

            RaceRunner player = result.Standings.Find(r => r.IsPlayer);
            result.PlayerPlace = result.Standings.IndexOf(player) + 1;
            result.PlayerTime = player.FinishTime;
            result.NewPersonalBest = player.FinishTime < athlete.PersonalBest;
            result.NewClubRecord = player.FinishTime < state.ClubRecord100m;
            result.NewWorldRecord = player.FinishTime < state.WorldRecord100m;
            if (result.Standings.Count >= 2)
                result.PhotoFinish = Mathf.Abs(result.Standings[0].FinishTime - result.Standings[1].FinishTime) <= 0.03f;

            int baseCash = offer.CashReward;
            int baseRep = offer.ReputationReward;
            float placeMultiplier = result.PlayerPlace == 1 ? 1f : result.PlayerPlace == 2 ? 0.65f : result.PlayerPlace == 3 ? 0.45f : result.PlayerPlace <= 5 ? 0.25f : 0.12f;
            result.CashReward = Mathf.RoundToInt(baseCash * placeMultiplier);
            result.ReputationReward = Mathf.RoundToInt(baseRep * placeMultiplier);
            if (result.NewPersonalBest) result.ReputationReward += 2;
            if (result.NewClubRecord) result.ReputationReward += 4;
            if (result.NewWorldRecord) result.ReputationReward += 25;

            return result;
        }

        private static RaceRunner BuildPlayer(Athlete athlete, CompetitionOffer offer, RaceStrategy strategy)
        {
            float rating = athlete.BaseRating;
            float form = Mathf.Lerp(0.95f, 1.05f, Mathf.InverseLerp(0.78f, 1.08f, athlete.Form));
            float fatigue = TrainingSystem.RacePerformanceMultiplier(athlete.Fatigue);
            rating *= form * fatigue;

            float variance = athlete.HasTrait(TraitType.Consistent) ? 0.025f : athlete.HasTrait(TraitType.Volatile) ? 0.085f : 0.045f;
            rating += UnityEngine.Random.Range(-variance * 20f, variance * 20f);

            if (athlete.HasTrait(TraitType.BigStagePerformer) && offer.IsChampionship)
                rating += 1.5f;

            float finishTime = Mathf.Clamp(13.72f - rating * 0.0445f, 9.40f, 13.30f);
            float[] weights = { 0.286f, 0.188f, 0.177f, 0.174f, 0.175f };

            if (strategy == RaceStrategy.ExplosiveStart)
            {
                weights = new[] { 0.278f, 0.182f, 0.179f, 0.179f, 0.182f };
                finishTime -= Mathf.Clamp((athlete.Acceleration - athlete.Speed) * 0.0028f, -0.025f, 0.045f);
            }
            else if (strategy == RaceStrategy.LatePush)
            {
                weights = new[] { 0.295f, 0.193f, 0.178f, 0.170f, 0.164f };
                finishTime -= Mathf.Clamp((athlete.Speed - athlete.Acceleration) * 0.0028f, -0.025f, 0.045f);
            }

            if (athlete.HasTrait(TraitType.ExplosiveStarter))
            {
                weights[0] -= 0.006f;
                weights[1] -= 0.002f;
                weights[4] += 0.008f;
                finishTime -= 0.015f;
            }
            if (athlete.HasTrait(TraitType.StrongFinisher))
            {
                weights[0] += 0.006f;
                weights[4] -= 0.006f;
                finishTime -= 0.015f;
            }

            finishTime += UnityEngine.Random.Range(-variance, variance);
            finishTime = Mathf.Round(finishTime * 100f) / 100f;

            return new RaceRunner
            {
                Name = athlete.DisplayName,
                CountryCode = athlete.CountryCode,
                IsPlayer = true,
                FinishTime = finishTime,
                SplitTimes = BuildSplits(finishTime, weights)
            };
        }

        private static RaceRunner BuildOpponent(float tierRating, bool championship)
        {
            float rating = tierRating + UnityEngine.Random.Range(-4.5f, 4.5f) + (championship ? 1.2f : 0f);
            float finishTime = Mathf.Clamp(13.72f - rating * 0.0445f + UnityEngine.Random.Range(-0.06f, 0.06f), 9.42f, 13.35f);
            finishTime = Mathf.Round(finishTime * 100f) / 100f;
            float bias = UnityEngine.Random.Range(-0.012f, 0.012f);
            float[] weights =
            {
                0.286f + bias,
                0.188f + bias * 0.35f,
                0.177f,
                0.174f - bias * 0.45f,
                0.175f - bias * 0.90f
            };

            return new RaceRunner
            {
                Name = FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)] + " " + LastNames[UnityEngine.Random.Range(0, LastNames.Length)],
                CountryCode = Countries[UnityEngine.Random.Range(0, Countries.Length)],
                IsPlayer = false,
                FinishTime = finishTime,
                SplitTimes = BuildSplits(finishTime, weights)
            };
        }

        private static float[] BuildSplits(float finishTime, float[] weights)
        {
            float sum = 0f;
            for (int i = 0; i < weights.Length; i++) sum += weights[i];
            float cumulative = 0f;
            float[] splits = new float[5];
            for (int i = 0; i < 5; i++)
            {
                cumulative += finishTime * (weights[i] / sum);
                splits[i] = cumulative;
            }
            splits[4] = finishTime;
            return splits;
        }

        private static float TierRating(CompetitionTier tier)
        {
            switch (tier)
            {
                case CompetitionTier.Local: return 62f;
                case CompetitionTier.Regional: return 69f;
                case CompetitionTier.National: return 76f;
                case CompetitionTier.International: return 83f;
                case CompetitionTier.Elite: return 89f;
                default: return 68f;
            }
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        public static float DistanceAtTime(RaceRunner runner, float time)
        {
            if (runner == null || runner.SplitTimes == null || runner.SplitTimes.Length < 5) return 0f;
            if (time <= 0f) return 0f;
            if (time >= runner.FinishTime) return 100f;

            float[] times = new float[6];
            float[] distances = { 0f, 20f, 40f, 60f, 80f, 100f };
            times[0] = 0f;
            for (int i = 0; i < 5; i++) times[i + 1] = runner.SplitTimes[i];

            float[] tangents = new float[6];
            tangents[0] = 20f / Mathf.Max(0.001f, times[1] - times[0]);
            tangents[5] = 20f / Mathf.Max(0.001f, times[5] - times[4]);
            for (int i = 1; i < 5; i++)
            {
                float before = 20f / Mathf.Max(0.001f, times[i] - times[i - 1]);
                float after = 20f / Mathf.Max(0.001f, times[i + 1] - times[i]);
                tangents[i] = (before + after) * 0.5f;
            }

            for (int segment = 0; segment < 5; segment++)
            {
                if (time <= times[segment + 1])
                {
                    float duration = Mathf.Max(0.001f, times[segment + 1] - times[segment]);
                    float t = Mathf.Clamp01((time - times[segment]) / duration);
                    return Hermite(distances[segment], distances[segment + 1], tangents[segment] * duration, tangents[segment + 1] * duration, t);
                }
            }
            return 100f;
        }

        private static float Hermite(float p0, float p1, float m0, float m1, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return (2f * t3 - 3f * t2 + 1f) * p0 + (t3 - 2f * t2 + t) * m0 + (-2f * t3 + 3f * t2) * p1 + (t3 - t2) * m1;
        }
    }
}
