using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrackDynasty.Mvp02
{
    public class RaceSimulator
    {
        private readonly System.Random _random;

        private readonly string[] _names =
        {
            "Jamal Reid", "Tyrese Walker", "Elijah Brooks", "Marcus Lee",
            "Devonte Smith", "Isaiah Johnson", "Caleb Brown", "Noah Schneider",
            "Luca Romano", "Kofi Mensah", "Joshua Pierre", "Liam Turner"
        };

        private readonly string[] _countries =
        {
            "USA", "USA", "GBR", "CAN", "USA", "NGR",
            "FRA", "GER", "ITA", "NGR", "FRA", "GBR"
        };

        public RaceSimulator(int seed)
        {
            _random = new System.Random(seed);
        }

        public RaceResult Simulate(Athlete athlete, RaceStrategy strategy, SeasonEvent seasonEvent, bool commitPersonalBest)
        {
            RaceResult result = new RaceResult();
            result.PreviousPersonalBest = athlete.PersonalBest;
            result.EventName = seasonEvent != null ? seasonEvent.Name : "100m Exhibition";
            result.Tier = seasonEvent != null ? seasonEvent.Tier : CompetitionTier.Local;
            result.IsChampionship = seasonEvent != null && seasonEvent.IsChampionship;

            RaceRunner player = BuildPlayerRunner(athlete, strategy);
            result.Runners.Add(player);

            List<int> indexes = new List<int>();
            for (int i = 0; i < _names.Length; i++) indexes.Add(i);
            Shuffle(indexes);

            float tierBase = TierBaseRating(result.Tier);
            for (int i = 0; i < 7; i++)
            {
                int idx = indexes[i];
                float rating = tierBase + RandomRange(-4.5f, 4.5f);
                result.Runners.Add(BuildAiRunner(_names[idx], _countries[idx], rating));
            }

            Shuffle(result.Runners);

            result.Standings = new List<RaceRunner>(result.Runners);
            result.Standings.Sort(delegate(RaceRunner a, RaceRunner b)
            {
                return a.FinishTime.CompareTo(b.FinishTime);
            });

            for (int i = 0; i < result.Standings.Count; i++)
            {
                if (result.Standings[i].IsPlayer)
                {
                    result.PlayerPlace = i + 1;
                    break;
                }
            }

            result.PlayerTime = player.FinishTime;
            result.NewPersonalBest = player.FinishTime < athlete.PersonalBest;
            if (commitPersonalBest && result.NewPersonalBest)
                athlete.PersonalBest = player.FinishTime;

            int baseCash = seasonEvent != null ? seasonEvent.BaseCashReward : 800;
            int baseRep = seasonEvent != null ? seasonEvent.BaseReputationReward : 8;
            result.CashReward = RewardForPlace(baseCash, result.PlayerPlace);
            result.ReputationReward = RewardForPlace(baseRep, result.PlayerPlace);
            if (result.IsChampionship && result.PlayerPlace == 1)
                result.ReputationReward += Mathf.RoundToInt(baseRep * 0.8f);

            return result;
        }

        private RaceRunner BuildPlayerRunner(Athlete athlete, RaceStrategy strategy)
        {
            float rating = athlete.BaseRating;
            rating *= Mathf.Lerp(0.94f, 1.05f, Mathf.InverseLerp(0.70f, 1.08f, athlete.Form));
            rating *= 1f - athlete.Fatigue * 0.11f;
            rating += RandomRange(-1.1f, 1.1f);

            float finishTime = Mathf.Clamp(13.70f - rating * 0.0445f, 9.55f, 12.90f);
            float[] weights = new float[] { 0.286f, 0.188f, 0.177f, 0.174f, 0.175f };
            float variance = 0.035f;

            if (strategy == RaceStrategy.ExplosiveStart)
            {
                weights = new float[] { 0.278f, 0.182f, 0.179f, 0.179f, 0.182f };
                finishTime += 0.010f - Mathf.Clamp((athlete.Acceleration - athlete.Speed) * 0.0034f, -0.045f, 0.030f);
                variance = 0.055f;
            }
            else if (strategy == RaceStrategy.LatePush)
            {
                weights = new float[] { 0.295f, 0.193f, 0.178f, 0.170f, 0.164f };
                finishTime += 0.010f - Mathf.Clamp((athlete.Speed - athlete.Acceleration) * 0.0032f, -0.045f, 0.030f);
                variance = 0.050f;
            }

            finishTime += RandomRange(-variance, variance);
            finishTime = Mathf.Round(finishTime * 100f) / 100f;

            RaceRunner runner = new RaceRunner();
            runner.Name = athlete.DisplayName;
            runner.CountryCode = athlete.CountryCode;
            runner.IsPlayer = true;
            runner.FinishTime = finishTime;
            runner.SplitTimes = BuildSplits(finishTime, weights);
            return runner;
        }

        private RaceRunner BuildAiRunner(string name, string country, float rating)
        {
            float effective = rating + RandomRange(-3.0f, 3.0f);
            float finishTime = Mathf.Clamp(13.70f - effective * 0.0445f + RandomRange(-0.05f, 0.05f), 9.60f, 12.95f);
            finishTime = Mathf.Round(finishTime * 100f) / 100f;

            float startBias = RandomRange(-0.012f, 0.012f);
            float[] weights = new float[]
            {
                0.286f + startBias,
                0.188f + startBias * 0.35f,
                0.177f,
                0.174f - startBias * 0.45f,
                0.175f - startBias * 0.90f
            };

            RaceRunner runner = new RaceRunner();
            runner.Name = name;
            runner.CountryCode = country;
            runner.IsPlayer = false;
            runner.FinishTime = finishTime;
            runner.SplitTimes = BuildSplits(finishTime, weights);
            return runner;
        }

        private float[] BuildSplits(float finishTime, float[] weights)
        {
            float sum = 0f;
            for (int i = 0; i < weights.Length; i++) sum += weights[i];

            float cumulative = 0f;
            float[] splits = new float[5];
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += finishTime * (weights[i] / sum);
                splits[i] = cumulative;
            }
            splits[4] = finishTime;
            return splits;
        }

        private float TierBaseRating(CompetitionTier tier)
        {
            if (tier == CompetitionTier.Local) return 63f;
            if (tier == CompetitionTier.Regional) return 70f;
            if (tier == CompetitionTier.National) return 77f;
            return 84f;
        }

        private int RewardForPlace(int baseValue, int place)
        {
            if (place == 1) return Mathf.RoundToInt(baseValue * 1.00f);
            if (place == 2) return Mathf.RoundToInt(baseValue * 0.65f);
            if (place == 3) return Mathf.RoundToInt(baseValue * 0.45f);
            if (place == 4) return Mathf.RoundToInt(baseValue * 0.28f);
            return Mathf.RoundToInt(baseValue * 0.16f);
        }

        private float RandomRange(float min, float max)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
