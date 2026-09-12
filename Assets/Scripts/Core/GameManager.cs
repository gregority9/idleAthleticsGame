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
        public CompetitionOffer ActiveCompetition { get; private set; }
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
            State = SaveSystem.Load();
            if (State == null || State.Roster == null || State.Roster.Count == 0)
                State = CreateNewState();
            NormalizeState();
            ActiveAthlete = GetSelectedAthlete();
        }

        public GameState CreateNewState()
        {
            GameState state = new GameState
            {
                CurrentDate = new GameDate(2027, 1, 1),
                Cash = 6200,
                Reputation = 120,
                ClubRecord100m = 10.72f,
                ClubRecordHolder = "Andre Campbell",
                WorldRecord100m = 9.58f,
                StartingScoutChoices = ScoutSystem.CreateStartingChoices()
            };
            Athlete starter = AthleteGenerator.CreateStarterAthlete();
            state.Roster.Add(starter);
            state.SelectedAthleteId = starter.Id;
            CompetitionSystem.EnsureOffers(starter, state.CurrentDate, state.Reputation);
            return state;
        }

        private void NormalizeState()
        {
            if (State.CurrentDate == null) State.CurrentDate = new GameDate(2027, 1, 1);
            if (State.Roster == null) State.Roster = new List<Athlete>();
            if (State.ScoutedProspects == null) State.ScoutedProspects = new List<Prospect>();
            if (State.Applications == null) State.Applications = new List<ClubApplication>();
            if (State.HallOfFame == null) State.HallOfFame = new List<HallOfFameEntry>();
            if (State.StartingScoutChoices == null || State.StartingScoutChoices.Count == 0)
                State.StartingScoutChoices = ScoutSystem.CreateStartingChoices();

            State.Roster.RemoveAll(a => a == null);

            // MVP 0.3.5 migration: some early 0.3 saves could reach the main UI without
            // a usable starter athlete. Restore Andre exactly once for pre-0.3.5 saves.
            if (State.SaveVersion < 5)
            {
                bool hasStarter = State.Roster.Exists(a => a.Id == "andre-campbell");
                bool starterRetired = State.HallOfFame.Exists(h => h != null && h.Name == "Andre Campbell");
                if (!hasStarter && !starterRetired)
                    State.Roster.Insert(0, AthleteGenerator.CreateStarterAthlete());
                State.SaveVersion = 5;
            }

            if (State.Roster.Count == 0)
                State.Roster.Add(AthleteGenerator.CreateStarterAthlete());

            for (int i = 0; i < State.Roster.Count; i++)
            {
                Athlete athlete = State.Roster[i];
                if (string.IsNullOrEmpty(athlete.Id)) athlete.Id = Guid.NewGuid().ToString("N");
                if (athlete.CompetitionOffers == null) athlete.CompetitionOffers = new List<CompetitionOffer>();
                if (athlete.Traits == null) athlete.Traits = new List<TraitType>();
                if (athlete.RaceHistory == null) athlete.RaceHistory = new List<RaceHistoryEntry>();
                if (athlete.SeasonHistory == null) athlete.SeasonHistory = new List<SeasonHistoryEntry>();

                // Recovery used to be a focus. In 0.3.5 recovery is controlled by load/actions.
                if (athlete.TrainingFocus == TrainingFocus.Recovery)
                {
                    athlete.TrainingFocus = TrainingFocus.Sprint;
                    athlete.TrainingIntensity = TrainingIntensity.Rest;
                }

                CompetitionSystem.EnsureOffers(athlete, State.CurrentDate, State.Reputation);
            }

            Athlete selected = State.Roster.Find(a => a.Id == State.SelectedAthleteId);
            if (selected == null)
            {
                Athlete starter = State.Roster.Find(a => a.Id == "andre-campbell");
                State.SelectedAthleteId = (starter ?? State.Roster[0]).Id;
            }
        }

        public void ChooseScout(ScoutProfile scout)
        {
            if (scout == null) return;
            State.ChosenScout = scout;
            State.StartingScoutChoices.Clear();

            Athlete starter = State.Roster.Find(a => a != null && a.Id == "andre-campbell");
            if (starter == null)
            {
                starter = AthleteGenerator.CreateStarterAthlete();
                State.Roster.Insert(0, starter);
            }
            State.SelectedAthleteId = starter.Id;
            ActiveAthlete = starter;
            CompetitionSystem.EnsureOffers(starter, State.CurrentDate, State.Reputation);

            RefreshScouting(true);
            SaveAndNotify();
        }

        public void SelectAthlete(Athlete athlete)
        {
            if (athlete == null) return;
            ActiveAthlete = athlete;
            State.SelectedAthleteId = athlete.Id;
            CompetitionSystem.EnsureOffers(athlete, State.CurrentDate, State.Reputation);
            Notify();
        }

        public Athlete GetSelectedAthlete()
        {
            if (State == null || State.Roster == null || State.Roster.Count == 0) return null;
            Athlete athlete = State.Roster.Find(a => a.Id == State.SelectedAthleteId);
            return athlete ?? State.Roster[0];
        }

        public void SetTraining(Athlete athlete, TrainingFocus focus)
        {
            if (athlete == null) return;
            if (focus == TrainingFocus.Recovery)
            {
                athlete.TrainingIntensity = TrainingIntensity.Rest;
                focus = TrainingFocus.Sprint;
            }
            athlete.TrainingFocus = focus;
            SaveAndNotify();
        }

        public void SetTrainingIntensity(Athlete athlete, TrainingIntensity intensity)
        {
            if (athlete == null) return;
            athlete.TrainingIntensity = intensity;
            SaveAndNotify();
        }

        public bool ScheduleCompetition(Athlete athlete, CompetitionOffer offer)
        {
            if (athlete == null || offer == null) return false;
            if (IsOnCompetitionBreak(athlete)) return false;
            if (!CompetitionSystem.CanEnter(athlete, offer)) return false;
            if (offer.Date == null || offer.Date.CompareTo(State.CurrentDate) <= 0) return false;
            athlete.ScheduledCompetition = offer;
            athlete.CompetitionOffers.Clear();
            SaveAndNotify();
            return true;
        }

        public bool IsOnCompetitionBreak(Athlete athlete)
        {
            return athlete != null && athlete.CompetitionBreakUntil != null &&
                   State.CurrentDate.CompareTo(athlete.CompetitionBreakUntil) < 0;
        }

        public void TakeCompetitionBreak(Athlete athlete, int days)
        {
            if (athlete == null) return;
            days = Mathf.Clamp(days, 7, 90);
            athlete.ScheduledCompetition = null;
            athlete.CompetitionOffers.Clear();
            athlete.CompetitionBreakUntil = State.CurrentDate.AddDays(days);
            SaveAndNotify();
        }

        public void ResumeCompetitions(Athlete athlete)
        {
            if (athlete == null) return;
            athlete.CompetitionBreakUntil = null;
            athlete.ScheduledCompetition = null;
            athlete.CompetitionOffers.Clear();
            CompetitionSystem.EnsureOffers(athlete, State.CurrentDate, State.Reputation);
            SaveAndNotify();
        }

        public bool CanUsePhysio(Athlete athlete)
        {
            if (athlete == null) return false;
            if (athlete.LastPhysioDate == null) return true;
            return (State.CurrentDate.ToDateTime() - athlete.LastPhysioDate.ToDateTime()).TotalDays >= 7;
        }

        public int PhysioCooldownDays(Athlete athlete)
        {
            if (athlete == null || athlete.LastPhysioDate == null) return 0;
            int elapsed = Mathf.Max(0, (int)(State.CurrentDate.ToDateTime() - athlete.LastPhysioDate.ToDateTime()).TotalDays);
            return Mathf.Max(0, 7 - elapsed);
        }

        public bool UsePhysio(Athlete athlete)
        {
            const int cost = 250;
            if (athlete == null || State.Cash < cost || !CanUsePhysio(athlete)) return false;
            State.Cash -= cost;
            TrainingSystem.ApplyPhysio(athlete);
            athlete.LastPhysioDate = new GameDate(State.CurrentDate.Year, State.CurrentDate.Month, State.CurrentDate.Day);
            SaveAndNotify();
            return true;
        }

        public bool CanStartCamp(Athlete athlete, CampType camp, out string reason)
        {
            reason = "";
            if (athlete == null) { reason = "No athlete selected."; return false; }

            int days = camp == CampType.Recovery ? 5 : 7;
            int cost = camp == CampType.Recovery ? 900 : 1500;
            if (State.Cash < cost) { reason = "Not enough cash."; return false; }

            GameDate end = State.CurrentDate.AddDays(days);
            for (int i = 0; i < State.Roster.Count; i++)
            {
                CompetitionOffer scheduled = State.Roster[i].ScheduledCompetition;
                if (scheduled == null || scheduled.Date == null) continue;
                if (scheduled.Date.CompareTo(State.CurrentDate) >= 0 && scheduled.Date.CompareTo(end) <= 0)
                {
                    reason = "A club athlete has a race during the camp window.";
                    return false;
                }
            }
            return true;
        }

        public bool StartCamp(Athlete athlete, CampType camp)
        {
            if (!CanStartCamp(athlete, camp, out _)) return false;

            int days = camp == CampType.Recovery ? 5 : 7;
            State.Cash -= camp == CampType.Recovery ? 900 : 1500;
            AdvanceDaysInternal(days, athlete, camp);
            SaveAndNotify();
            return true;
        }

        public List<Athlete> AthletesRacingToday()
        {
            List<Athlete> list = new List<Athlete>();
            for (int i = 0; i < State.Roster.Count; i++)
            {
                Athlete athlete = State.Roster[i];
                if (athlete.ScheduledCompetition != null && athlete.ScheduledCompetition.Date != null && athlete.ScheduledCompetition.Date.IsSameDay(State.CurrentDate))
                    list.Add(athlete);
            }
            return list;
        }

        public bool CanAdvanceDate()
        {
            return AthletesRacingToday().Count == 0;
        }

        public void AdvanceOneDay()
        {
            if (!CanAdvanceDate()) return;
            AdvanceDaysInternal(1);
            SaveAndNotify();
        }

        public void AdvanceSevenDays()
        {
            if (!CanAdvanceDate()) return;
            int advanced = 0;
            while (advanced < 7 && CanAdvanceDate())
            {
                AdvanceDaysInternal(1);
                advanced++;
            }
            SaveAndNotify();
        }

        public void AdvanceToNextCompetition()
        {
            if (!CanAdvanceDate()) return;
            GameDate next = FindNextScheduledDate();
            if (next == null)
            {
                AdvanceDaysInternal(1);
                SaveAndNotify();
                return;
            }

            int safety = 0;
            while (State.CurrentDate.CompareTo(next) < 0 && safety < 400)
            {
                AdvanceDaysInternal(1);
                safety++;
            }
            SaveAndNotify();
        }

        private GameDate FindNextScheduledDate()
        {
            GameDate best = null;
            for (int i = 0; i < State.Roster.Count; i++)
            {
                CompetitionOffer offer = State.Roster[i].ScheduledCompetition;
                if (offer == null || offer.Date == null) continue;
                if (offer.Date.CompareTo(State.CurrentDate) < 0) continue;
                if (best == null || offer.Date.CompareTo(best) < 0)
                    best = offer.Date;
            }
            return best;
        }

        private void AdvanceDaysInternal(int days, Athlete campAthlete = null, CampType? camp = null)
        {
            for (int d = 0; d < days; d++)
            {
                DateTime before = State.CurrentDate.ToDateTime();
                for (int i = 0; i < State.Roster.Count; i++)
                {
                    Athlete athlete = State.Roster[i];
                    if (campAthlete != null && athlete.Id == campAthlete.Id && camp.HasValue)
                    {
                        if (camp.Value == CampType.Recovery)
                            TrainingSystem.ApplyRecoveryCampDay(athlete);
                        else
                            TrainingSystem.ApplyFocusedCampDay(athlete);
                    }
                    else
                    {
                        TrainingSystem.ApplyTrainingDay(athlete);
                    }
                }

                State.CurrentDate = State.CurrentDate.AddDays(1);
                DateTime after = State.CurrentDate.ToDateTime();

                if (before.Month != after.Month)
                    ApplyMonthlyCosts();
                if (before.Year != after.Year)
                    ApplyYearRollover(before.Year);

                ApplicationSystem.RemoveExpired(State);

                for (int i = 0; i < State.Roster.Count; i++)
                    CompetitionSystem.EnsureOffers(State.Roster[i], State.CurrentDate, State.Reputation);
            }
        }

        private void ApplyMonthlyCosts()
        {
            if (State.ChosenScout != null)
                State.Cash -= State.ChosenScout.MonthlySalary;
        }

        private void ApplyYearRollover(int completedYear)
        {
            for (int i = State.Roster.Count - 1; i >= 0; i--)
            {
                Athlete athlete = State.Roster[i];
                int yearRaces = 0;
                int yearWins = 0;
                int yearTitles = 0;
                for (int r = 0; r < athlete.RaceHistory.Count; r++)
                {
                    RaceHistoryEntry entry = athlete.RaceHistory[r];
                    if (entry.Year != completedYear) continue;
                    yearRaces++;
                    if (entry.Place == 1) yearWins++;
                    if (entry.Place == 1 && entry.Tier >= CompetitionTier.National) yearTitles++;
                }

                athlete.SeasonHistory.Add(new SeasonHistoryEntry
                {
                    Year = completedYear,
                    StartAge = athlete.Age,
                    EndAge = athlete.Age + 1,
                    PbAtStart = athlete.YearStartPersonalBest,
                    PbAtEnd = athlete.PersonalBest,
                    Races = yearRaces,
                    Wins = yearWins,
                    Championships = yearTitles
                });

                athlete.YearStartPersonalBest = athlete.PersonalBest;
                TrainingSystem.ApplyYearRollover(athlete);
                bool retires = athlete.Age >= 36 || (athlete.Age >= 33 && UnityEngine.Random.value < 0.20f);
                if (retires)
                {
                    State.HallOfFame.Insert(0, new HallOfFameEntry
                    {
                        Name = athlete.DisplayName,
                        CountryCode = athlete.CountryCode,
                        RetireAge = athlete.Age,
                        Races = athlete.Races,
                        Wins = athlete.Wins,
                        Championships = athlete.Championships,
                        PersonalBest = athlete.PersonalBest
                    });
                    State.Roster.RemoveAt(i);
                }
                else
                {
                    athlete.CompetitionOffers.Clear();
                    if (athlete.ScheduledCompetition != null && athlete.ScheduledCompetition.Date != null && athlete.ScheduledCompetition.Date.CompareTo(State.CurrentDate) < 0)
                        athlete.ScheduledCompetition = null;
                    CompetitionSystem.EnsureOffers(athlete, State.CurrentDate, State.Reputation);
                }
            }

            if (State.Roster.Count == 0)
            {
                Prospect emergency = AthleteGenerator.GenerateApplicant(8, State.Reputation, State.ChosenScout);
                Athlete athlete = AthleteGenerator.FromProspect(emergency);
                State.Roster.Add(athlete);
                State.SelectedAthleteId = athlete.Id;
            }
            ActiveAthlete = GetSelectedAthlete();
        }

        public bool RefreshScouting(bool free = false)
        {
            if (State.ChosenScout == null) return false;
            int cost = ScoutSystem.RefreshCost(State.ChosenScout);
            if (!free && State.Cash < cost) return false;
            if (!free) State.Cash -= cost;
            State.ScoutedProspects.Clear();
            for (int i = 0; i < 3; i++)
                State.ScoutedProspects.Add(AthleteGenerator.GenerateScoutedProspect(State.ChosenScout, State.Reputation));
            SaveAndNotify();
            return true;
        }

        public bool SignProspect(Prospect prospect, bool fromApplication = false)
        {
            if (prospect == null || State.Roster.Count >= 8) return false;
            if (State.Cash < prospect.SigningFee) return false;
            State.Cash -= prospect.SigningFee;
            Athlete athlete = AthleteGenerator.FromProspect(prospect);
            State.Roster.Add(athlete);
            CompetitionSystem.EnsureOffers(athlete, State.CurrentDate, State.Reputation);
            if (fromApplication)
                State.Applications.RemoveAll(a => a.Prospect != null && a.Prospect.Id == prospect.Id);
            else
                State.ScoutedProspects.RemoveAll(p => p.Id == prospect.Id);
            SaveAndNotify();
            return true;
        }

        public void RejectApplication(ClubApplication application)
        {
            if (application == null) return;
            State.Applications.Remove(application);
            SaveAndNotify();
        }

        public void PrepareRace(Athlete athlete)
        {
            if (athlete == null || athlete.ScheduledCompetition == null) return;
            if (athlete.ScheduledCompetition.Date == null || !athlete.ScheduledCompetition.Date.IsSameDay(State.CurrentDate)) return;
            ActiveAthlete = athlete;
            ActiveCompetition = athlete.ScheduledCompetition;
            ActiveStrategy = RaceStrategy.Balanced;
            CurrentRaceResult = null;
            State.SelectedAthleteId = athlete.Id;
            Notify();
        }

        public void StartRace()
        {
            if (ActiveAthlete == null || ActiveCompetition == null) return;
            CurrentRaceResult = RaceSimulator.Simulate(State, ActiveAthlete, ActiveCompetition, ActiveStrategy);
            Notify();
        }

        public void ClaimRaceResult()
        {
            if (CurrentRaceResult == null || ActiveAthlete == null || ActiveCompetition == null) return;

            RaceResult result = CurrentRaceResult;
            Athlete athlete = ActiveAthlete;
            athlete.Races++;
            if (result.PlayerPlace == 1) athlete.Wins++;
            if (result.PlayerPlace == 1 && result.IsChampionship) athlete.Championships++;
            if (result.NewPersonalBest) athlete.PersonalBest = result.PlayerTime;
            if (result.NewClubRecord)
            {
                State.ClubRecord100m = result.PlayerTime;
                State.ClubRecordHolder = athlete.DisplayName;
            }
            if (result.NewWorldRecord)
                State.WorldRecord100m = result.PlayerTime;

            athlete.Fatigue = Mathf.Clamp01(athlete.Fatigue + 0.10f);
            athlete.Form = Mathf.Clamp(athlete.Form + UnityEngine.Random.Range(-0.015f, 0.018f), 0.78f, 1.08f);
            State.Cash += result.CashReward;
            State.Reputation += result.ReputationReward;

            athlete.RaceHistory.Add(new RaceHistoryEntry
            {
                Year = State.CurrentDate.Year,
                Month = State.CurrentDate.Month,
                Day = State.CurrentDate.Day,
                EventName = result.EventName,
                Tier = result.Tier,
                Place = result.PlayerPlace,
                Time = result.PlayerTime,
                PersonalBest = result.NewPersonalBest,
                ClubRecord = result.NewClubRecord,
                WorldRecord = result.NewWorldRecord
            });

            ClubApplication application = ApplicationSystem.MaybeGenerate(State, athlete, result);
            if (application != null)
                State.Applications.Add(application);

            athlete.ScheduledCompetition = null;
            athlete.CompetitionOffers = CompetitionSystem.GenerateOffers(athlete, State.CurrentDate, State.Reputation);

            CurrentRaceResult = null;
            ActiveCompetition = null;
            SaveAndNotify();
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
            NormalizeState();
            ActiveAthlete = GetSelectedAthlete();
            ActiveCompetition = null;
            CurrentRaceResult = null;
            Notify();
        }

        public void ResetGame()
        {
            SaveSystem.Delete();
            State = CreateNewState();
            NormalizeState();
            ActiveAthlete = GetSelectedAthlete();
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

        private void Notify()
        {
            StateChanged?.Invoke();
        }
    }
}
