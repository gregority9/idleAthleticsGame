using System;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Domain;
using TrackDynasty.Mvp03.Systems;
using TrackDynasty.Mvp03.UI;
using UnityEngine;

namespace TrackDynasty.Mvp03.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; }
        public Athlete ActiveAthlete { get; private set; }
        public CompetitionMeet ActiveCompetition { get; private set; }
        public DistanceType ActiveDistance { get; private set; } = DistanceType.M100;
        public RaceStrategy ActiveStrategy { get; set; } = RaceStrategy.Balanced;
        public RaceResult CurrentRaceResult { get; private set; }
        public MainUIController UI { get; private set; }

        public event Action StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOrCreate();
        }

        private void Start()
        {
            UI = gameObject.AddComponent<MainUIController>();
            UI.Initialize(this);
        }

        public void LoadOrCreate()
        {
            State = SaveSystem.Load() ?? CreateNewState();
            ActiveAthlete = GetSelectedAthlete();
        }

        public GameState CreateNewState()
        {
            return new GameState
            {
                SaveVersion = 4,
                SetupCompleted = false,
                CurrentWeek = new GameWeek(2026, 1),
                Cash = 5000,
                Management = new ManagementProfile { Name = "My Athletics Management", Reputation = 0 },
                StarterChoices = AthleteGenerator.CreateStarterChoices(),
                CompetitionCalendar = CompetitionSystem.GenerateSeason(2026)
            };
        }

        public bool CompleteSetup(string managementName, AthleteCandidate candidate)
        {
            if (State.SetupCompleted || candidate == null) return false;
            string cleanName = string.IsNullOrWhiteSpace(managementName) ? "My Athletics Management" : managementName.Trim();
            if (cleanName.Length > 32) cleanName = cleanName.Substring(0, 32);

            Athlete athlete = candidate.ToAthlete();
            State.Management.Name = cleanName;
            State.Roster.Add(athlete);
            State.SelectedAthleteId = athlete.Id;
            State.StarterChoices.Clear();
            State.SetupCompleted = true;
            ActiveAthlete = athlete;
            SaveAndNotify();
            return true;
        }

        public Athlete GetSelectedAthlete()
        {
            if (State == null || State.Roster == null || State.Roster.Count == 0) return null;
            Athlete athlete = State.Roster.Find(a => a.Id == State.SelectedAthleteId);
            return athlete ?? State.Roster[0];
        }

        public void SelectAthlete(Athlete athlete)
        {
            if (athlete == null) return;
            ActiveAthlete = athlete;
            State.SelectedAthleteId = athlete.Id;
            Notify();
        }

        public void SetTrainingDistance(Athlete athlete, DistanceType distance)
        {
            if (athlete == null) return;
            athlete.TrainingDistance = distance;
            SaveAndNotify();
        }

        public void SetTrainingFocus(Athlete athlete, TrainingFocus focus)
        {
            if (athlete == null) return;
            athlete.TrainingFocus = focus;
            SaveAndNotify();
        }

        public CoachProfile GetCoach(Athlete athlete)
        {
            if (athlete == null || string.IsNullOrEmpty(athlete.AssignedCoachId) || State.Coaches == null) return null;
            return State.Coaches.Find(c => c.Id == athlete.AssignedCoachId);
        }

        public bool ScheduleCompetition(Athlete athlete, CompetitionMeet meet, DistanceType distance)
        {
            if (athlete == null || meet == null || meet.Week == null) return false;
            if (athlete.ScheduledCompetition != null) return false;
            if (meet.Week.CompareTo(State.CurrentWeek) < 0) return false;
            if (!CompetitionSystem.CanEnter(athlete, meet, distance)) return false;
            if (State.Cash < meet.EntryFee) return false;

            State.Cash -= meet.EntryFee;
            athlete.ScheduledCompetition = new ScheduledCompetition { MeetId = meet.Id, Distance = distance };
            SaveAndNotify();
            return true;
        }

        public void CancelScheduledCompetition(Athlete athlete)
        {
            if (athlete == null || athlete.ScheduledCompetition == null) return;
            athlete.ScheduledCompetition = null;
            SaveAndNotify();
        }

        public CompetitionMeet ScheduledMeet(Athlete athlete)
        {
            if (athlete == null || athlete.ScheduledCompetition == null) return null;
            return CompetitionSystem.FindMeet(State, athlete.ScheduledCompetition.MeetId);
        }

        public List<Athlete> AthletesRacingThisWeek()
        {
            List<Athlete> result = new List<Athlete>();
            if (State.Roster == null) return result;
            for (int i = 0; i < State.Roster.Count; i++)
            {
                Athlete athlete = State.Roster[i];
                CompetitionMeet meet = ScheduledMeet(athlete);
                if (meet != null && meet.Week.IsSame(State.CurrentWeek)) result.Add(athlete);
            }
            return result;
        }

        public bool CanAdvanceWeek() => AthletesRacingThisWeek().Count == 0;

        public void AdvanceOneWeek()
        {
            if (!State.SetupCompleted || !CanAdvanceWeek()) return;

            for (int i = 0; i < State.Roster.Count; i++)
                TrainingSystem.ApplyTrainingWeek(State.Roster[i], GetCoach(State.Roster[i]));

            ApplyWeeklyFinances();
            ClubApplication weeklyApplication = ApplicationSystem.MaybeGenerateWeekly(State);
            if (weeklyApplication != null) State.Applications.Add(weeklyApplication);

            int previousYear = State.CurrentWeek.Year;
            State.CurrentWeek = State.CurrentWeek.AddWeeks(1);
            if (State.CurrentWeek.Year != previousYear) ApplyYearRollover();

            ApplicationSystem.RemoveExpired(State);
            RemoveExpiredScheduledCompetitions();
            SaveAndNotify();
        }

        private void ApplyWeeklyFinances()
        {
            int staffCost = 0;
            for (int i = 0; i < State.Coaches.Count; i++) staffCost += State.Coaches[i].WeeklySalary;
            for (int i = 0; i < State.Staff.Count; i++) staffCost += State.Staff[i].WeeklySalary;
            State.Cash -= staffCost;

            for (int i = State.SponsorContracts.Count - 1; i >= 0; i--)
            {
                SponsorContract contract = State.SponsorContracts[i];
                State.Cash += contract.WeeklyPayment;
                contract.WeeksRemaining--;
                if (contract.WeeksRemaining <= 0) State.SponsorContracts.RemoveAt(i);
            }
        }

        private void ApplyYearRollover()
        {
            for (int i = 0; i < State.Roster.Count; i++) TrainingSystem.ApplyYearRollover(State.Roster[i]);
            State.CompetitionCalendar = CompetitionSystem.GenerateSeason(State.CurrentWeek.Year);
        }

        private void RemoveExpiredScheduledCompetitions()
        {
            for (int i = 0; i < State.Roster.Count; i++)
            {
                Athlete athlete = State.Roster[i];
                CompetitionMeet meet = ScheduledMeet(athlete);
                if (meet != null && meet.Week.CompareTo(State.CurrentWeek) < 0) athlete.ScheduledCompetition = null;
            }
        }

        public List<CompetitionMeet> UpcomingMeets(Athlete athlete, int maxCount = 8)
        {
            List<CompetitionMeet> result = new List<CompetitionMeet>();
            if (athlete == null || State.CompetitionCalendar == null) return result;
            for (int i = 0; i < State.CompetitionCalendar.Count; i++)
            {
                CompetitionMeet meet = State.CompetitionCalendar[i];
                if (meet.Week.CompareTo(State.CurrentWeek) < 0) continue;
                if (meet.AllowedCategories != null && meet.AllowedCategories.Contains(athlete.Category)) result.Add(meet);
                if (result.Count >= maxCount) break;
            }
            return result;
        }

        public void PrepareRace(Athlete athlete)
        {
            if (athlete == null || athlete.ScheduledCompetition == null) return;
            CompetitionMeet meet = ScheduledMeet(athlete);
            if (meet == null || !meet.Week.IsSame(State.CurrentWeek)) return;
            ActiveAthlete = athlete;
            ActiveCompetition = meet;
            ActiveDistance = athlete.ScheduledCompetition.Distance;
            ActiveStrategy = RaceStrategy.Balanced;
            CurrentRaceResult = null;
            State.SelectedAthleteId = athlete.Id;
            Notify();
        }

        public void StartRace()
        {
            if (ActiveAthlete == null || ActiveCompetition == null) return;
            CurrentRaceResult = RaceSimulator.Simulate(State, ActiveAthlete, ActiveCompetition, ActiveDistance, ActiveStrategy);
            Notify();
        }

        public void ClaimRaceResult()
        {
            if (CurrentRaceResult == null || ActiveAthlete == null || ActiveCompetition == null) return;
            RaceResult result = CurrentRaceResult;
            Athlete athlete = ActiveAthlete;
            DistanceRecord record = athlete.GetRecord(result.Distance);

            athlete.CareerRaces++;
            record.Races++;
            if (result.PlayerPlace == 1)
            {
                athlete.CareerWins++;
                record.Wins++;
            }
            if (result.NewPersonalBest) record.PersonalBest = result.PlayerTime;
            if (result.NewClubRecord) State.SetClubRecord(result.Distance, result.PlayerTime, athlete.DisplayName);

            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + (result.Distance >= DistanceType.M800 ? 0.16f : 0.10f));
            athlete.Form = Mathf.Clamp(athlete.Form + UnityEngine.Random.Range(-0.012f, 0.016f), 0.78f, 1.08f);
            athlete.SponsorInterest += result.SponsorInterestGain;
            State.Cash += result.CashReward;
            State.Management.Reputation += result.ReputationReward;

            SponsorContract contract = State.SponsorContracts.Find(c => c.AthleteId == athlete.Id);
            if (contract != null && result.PlayerPlace == 1) State.Cash += contract.WinBonus;

            athlete.RaceHistory.Add(new RaceHistoryEntry
            {
                Year = State.CurrentWeek.Year,
                Week = State.CurrentWeek.Week,
                EventName = result.EventName,
                Range = result.Range,
                Distance = result.Distance,
                Place = result.PlayerPlace,
                Time = result.PlayerTime,
                PersonalBest = result.NewPersonalBest,
                ClubRecord = result.NewClubRecord
            });

            ClubApplication application = ApplicationSystem.MaybeGenerateAfterRace(State, athlete, result);
            if (application != null) State.Applications.Add(application);
            SponsorOffer offer = SponsorSystem.MaybeCreateOffer(State, athlete);
            if (offer != null) State.SponsorOffers.Add(offer);

            athlete.ScheduledCompetition = null;
            CurrentRaceResult = null;
            ActiveCompetition = null;
            SaveAndNotify();
        }

        public bool SignApplication(ClubApplication application)
        {
            if (application == null || application.Candidate == null) return false;
            if (State.Cash < application.Candidate.SigningFee) return false;
            if (State.Roster.Count >= 25) return false;

            State.Cash -= application.Candidate.SigningFee;
            Athlete athlete = application.Candidate.ToAthlete();
            State.Roster.Add(athlete);
            State.Applications.Remove(application);
            SaveAndNotify();
            return true;
        }

        public void RejectApplication(ClubApplication application)
        {
            if (application == null) return;
            State.Applications.Remove(application);
            SaveAndNotify();
        }

        public bool AcceptSponsorOffer(SponsorOffer offer)
        {
            if (offer == null || State.SponsorOffers == null || !State.SponsorOffers.Contains(offer)) return false;
            if (State.SponsorContracts.Exists(c => c.AthleteId == offer.AthleteId)) return false;

            State.Cash += offer.SigningBonus;
            State.SponsorContracts.Add(new SponsorContract
            {
                Id = offer.Id,
                AthleteId = offer.AthleteId,
                BrandName = offer.BrandName,
                WeeklyPayment = offer.WeeklyPayment,
                WinBonus = offer.WinBonus,
                WeeksRemaining = offer.DurationWeeks
            });
            State.SponsorOffers.Remove(offer);
            SaveAndNotify();
            return true;
        }

        public void SaveGame()
        {
            SaveSystem.Save(State);
            Notify();
        }

        public void LoadGame()
        {
            GameState loaded = SaveSystem.Load();
            if (loaded == null) return;
            State = loaded;
            ActiveAthlete = GetSelectedAthlete();
            ActiveCompetition = null;
            CurrentRaceResult = null;
            Notify();
        }

        public void ResetGame()
        {
            SaveSystem.Delete();
            State = CreateNewState();
            ActiveAthlete = null;
            ActiveCompetition = null;
            CurrentRaceResult = null;
            SaveSystem.Save(State);
            Notify();
        }

        private void SaveAndNotify()
        {
            SaveSystem.Save(State);
            Notify();
        }

        private void Notify() => StateChanged?.Invoke();
    }
}
