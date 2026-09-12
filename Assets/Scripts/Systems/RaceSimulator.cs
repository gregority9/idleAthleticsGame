using System.Collections.Generic;
using TrackDynasty.Mvp03.Domain;
using UnityEngine;

namespace TrackDynasty.Mvp03.Systems
{
    public static class RaceSimulator
    {
        private static readonly string[] FirstNames = { "Jakub", "Michał", "Antoni", "Noah", "Liam", "Mateo", "Elias", "Lucas", "Kofi", "Sofia", "Maja", "Julia" };
        private static readonly string[] LastNames = { "Nowak", "Kowalski", "Smith", "Johnson", "Becker", "Martin", "Costa", "Mensah", "Rossi", "Brown" };
        private static readonly string[] Countries = { "POL", "GER", "FRA", "ITA", "GBR", "USA", "CAN", "NGR", "BRA" };

        public static RaceResult Simulate(GameState state, Athlete athlete, CompetitionMeet meet, DistanceType distance, RaceStrategy strategy)
        {
            DistanceRecord record = athlete.GetRecord(distance);
            float previousClub = state.GetClubRecord(distance);
            RaceResult result = new RaceResult
            {
                EventName = meet.Name,
                City = meet.City,
                Week = new GameWeek(meet.Week.Year, meet.Week.Week),
                Range = meet.Range,
                Distance = distance,
                Category = athlete.Category,
                IsChampionship = meet.IsChampionship,
                PreviousPersonalBest = record.PersonalBest,
                PreviousClubRecord = previousClub
            };

            List<RaceRunner> runners = new List<RaceRunner> { BuildPlayer(athlete, meet, distance, strategy) };
            for (int i = 0; i < 7; i++) runners.Add(BuildOpponent(athlete, meet, distance));
            Shuffle(runners);
            for (int i = 0; i < runners.Count; i++) runners[i].Lane = i + 1;

            result.Runners = runners;
            result.Standings = new List<RaceRunner>(runners);
            result.Standings.Sort((a, b) => a.FinishTime.CompareTo(b.FinishTime));

            RaceRunner player = result.Standings.Find(r => r.IsPlayer);
            result.PlayerPlace = result.Standings.IndexOf(player) + 1;
            result.PlayerTime = player.FinishTime;
            result.NewPersonalBest = !record.HasPersonalBest || player.FinishTime < record.PersonalBest;
            result.NewClubRecord = previousClub <= 0f || player.FinishTime < previousClub;

            float photoWindow = distance <= DistanceType.M200 ? 0.03f : distance == DistanceType.M400 ? 0.06f : distance == DistanceType.M800 ? 0.12f : 0.20f;
            if (result.Standings.Count >= 2)
                result.PhotoFinish = Mathf.Abs(result.Standings[0].FinishTime - result.Standings[1].FinishTime) <= photoWindow;

            float placeMultiplier = result.PlayerPlace == 1 ? 1f : result.PlayerPlace == 2 ? 0.65f : result.PlayerPlace == 3 ? 0.45f : result.PlayerPlace <= 5 ? 0.18f : 0f;
            result.CashReward = Mathf.RoundToInt(meet.BaseCashReward * placeMultiplier);
            result.ReputationReward = Mathf.RoundToInt(meet.BaseReputationReward * placeMultiplier);
            if (result.NewPersonalBest) result.ReputationReward += Mathf.Max(1, (int)meet.Range);
            if (result.NewClubRecord) result.ReputationReward += Mathf.Max(1, (int)meet.Range + 1);
            result.SponsorInterestGain = SponsorSystem.InterestGain(result);
            return result;
        }

        private static RaceRunner BuildPlayer(Athlete athlete, CompetitionMeet meet, DistanceType distance, RaceStrategy strategy)
        {
            float rating = athlete.DistanceRating(distance);
            float form = Mathf.Lerp(0.94f, 1.05f, Mathf.InverseLerp(0.78f, 1.08f, athlete.Form));
            float fatigue = 1f - athlete.Fatigue * 0.12f;
            rating *= form * fatigue;

            float variance = athlete.HasTrait(TraitType.Consistent) ? 0.55f : athlete.HasTrait(TraitType.Volatile) ? 2.0f : 1.05f;
            rating += Random.Range(-variance, variance);
            if (athlete.HasTrait(TraitType.BigStagePerformer) && meet.IsChampionship) rating += 1.5f;

            if (strategy == RaceStrategy.FastStart)
            {
                rating += distance <= DistanceType.M400 ? (athlete.Acceleration - athlete.Endurance) * 0.018f : (athlete.Acceleration - athlete.Endurance) * 0.008f;
            }
            else if (strategy == RaceStrategy.LateKick)
            {
                rating += distance >= DistanceType.M400 ? (athlete.Endurance + athlete.Mental - athlete.Acceleration * 1.4f) * 0.012f : (athlete.Speed - athlete.Acceleration) * 0.010f;
            }

            if (athlete.HasTrait(TraitType.ExplosiveStarter) && strategy == RaceStrategy.FastStart) rating += 0.8f;
            if (athlete.HasTrait(TraitType.StrongFinisher) && strategy == RaceStrategy.LateKick) rating += 0.8f;

            float finishTime = PerformanceModel.EstimateTime(distance, athlete.Age, rating);
            finishTime *= Random.Range(0.995f, 1.005f);
            finishTime = RoundTime(finishTime, distance);
            return new RaceRunner
            {
                Name = athlete.DisplayName,
                CountryCode = athlete.CountryCode,
                IsPlayer = true,
                FinishTime = finishTime,
                SplitTimes = BuildSplits(finishTime, strategy, true)
            };
        }

        private static RaceRunner BuildOpponent(Athlete athlete, CompetitionMeet meet, DistanceType distance)
        {
            float rating = meet.FieldStrength + Random.Range(-meet.FieldSpread, meet.FieldSpread + 1);
            if (meet.IsChampionship) rating += Random.Range(0f, 2.5f);
            float finishTime = PerformanceModel.EstimateTime(distance, athlete.Age, rating) * Random.Range(0.994f, 1.006f);
            finishTime = RoundTime(finishTime, distance);
            RaceStrategy style = (RaceStrategy)Random.Range(0, 3);
            return new RaceRunner
            {
                Name = FirstNames[Random.Range(0, FirstNames.Length)] + " " + LastNames[Random.Range(0, LastNames.Length)],
                CountryCode = meet.Range <= CompetitionRange.Country ? "POL" : Countries[Random.Range(0, Countries.Length)],
                IsPlayer = false,
                FinishTime = finishTime,
                SplitTimes = BuildSplits(finishTime, style, false)
            };
        }

        private static float RoundTime(float time, DistanceType distance)
        {
            float precision = distance <= DistanceType.M400 ? 100f : 100f;
            return Mathf.Round(time * precision) / precision;
        }

        private static float[] BuildSplits(float finishTime, RaceStrategy strategy, bool player)
        {
            float[] weights = { 0.22f, 0.20f, 0.195f, 0.192f, 0.193f };
            if (strategy == RaceStrategy.FastStart) weights = new[] { 0.205f, 0.195f, 0.198f, 0.200f, 0.202f };
            if (strategy == RaceStrategy.LateKick) weights = new[] { 0.225f, 0.205f, 0.198f, 0.190f, 0.182f };
            if (!player)
            {
                float bias = Random.Range(-0.008f, 0.008f);
                weights[0] += bias;
                weights[4] -= bias;
            }

            float total = 0f;
            for (int i = 0; i < weights.Length; i++) total += weights[i];
            float cumulative = 0f;
            float[] splits = new float[5];
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += finishTime * weights[i] / total;
                splits[i] = cumulative;
            }
            splits[4] = finishTime;
            return splits;
        }

        public static float DistanceAtTime(RaceRunner runner, DistanceType distance, float time)
        {
            if (runner == null || runner.SplitTimes == null || runner.SplitTimes.Length < 5) return 0f;
            float totalDistance = (float)(int)distance;
            if (time <= 0f) return 0f;
            if (time >= runner.FinishTime) return totalDistance;

            float segmentDistance = totalDistance / 5f;
            float previousTime = 0f;
            float previousDistance = 0f;
            for (int i = 0; i < 5; i++)
            {
                float nextTime = runner.SplitTimes[i];
                float nextDistance = segmentDistance * (i + 1);
                if (time <= nextTime)
                {
                    float t = Mathf.InverseLerp(previousTime, nextTime, time);
                    return Mathf.Lerp(previousDistance, nextDistance, Smooth(t));
                }
                previousTime = nextTime;
                previousDistance = nextDistance;
            }
            return totalDistance;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
