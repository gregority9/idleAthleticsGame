# Track Dynasty — MVP 0.3

Target Unity Editor: **6000.5.10f1**

MVP 0.3 is the first architectural refactor of the prototype. It removes IMGUI entirely and uses Unity UI (uGUI) with separate domain, systems, core and screen modules.

## Career loop

- career starts on **1 January 2027**
- one starting athlete: **Andre Campbell**
- choose one of three scouts before entering the main game
- normal **12-month / 365-day calendar** with real day/month/year dates
- every athlete has an independent training focus, competition offers and scheduled race
- after each race that athlete receives several new competition choices
- competition tiers: `Local`, `Regional`, `National`, `International`, `Elite`
- higher tiers require faster qualifying PBs
- multiple athletes may race on the same date; the day does not advance until all scheduled races for that date are resolved
- training is applied on normal days between competitions
- scout salary is charged monthly
- yearly progression, aging, decline, retirement and Hall of Fame happen when the calendar rolls into a new year

## Recruitment

- three starter scout profiles with different strengths: evaluation accuracy, talent network, or lower scouting/signing costs
- scouting produces prospects with hidden exact potential and a visible estimate range
- stronger club reputation improves the general talent pool
- good race results can trigger **inbound applications** from athletes who want to join the club
- wins, podiums, PBs, club/world records and higher-tier results increase both application chance and applicant quality
- applications expire if ignored

## Athlete development

- Speed, Acceleration, Strength, Technique and Mental
- Form and Fatigue
- hidden exact Potential + visible estimated range
- individual Development Rate
- age curve and potential-gap based progression
- training focuses: Sprint, Strength, Technique, Recovery
- 8 traits with gameplay effects: Explosive Starter, Strong Finisher, Big Stage Performer, Fast Learner, Injury Prone, Late Bloomer, Consistent, Volatile
- race history and yearly career history
- PB progression mini-chart

## Racing

- 100 m only in MVP 0.3
- strategy selection: Explosive Start / Balanced / Late Push
- deterministic race result calculated before presentation
- continuous movement through 20/40/60/80/100 m split points using continuous interpolation
- no stopping at split markers
- photo finish when P1/P2 differ by <= 0.03 s
- club record and world record tracking

### Temporary race presentation

MVP 0.3 deliberately uses **circular country flags instead of runner sprites**.

Every lane has a circular flag marker moving continuously from start to finish. The player's athlete has an additional green outline. The same flag language is used in race prep and the results sheet.

## Full race results

After every competition the game displays the complete 1–8 results table:

- finishing position
- lane
- country flag
- athlete name
- finish time
- gap to winner
- PB / CR / WR badges when applicable
- cash and reputation rewards

After claiming the result, the game returns to that athlete so a new competition can be selected from the newly generated offers.

## UI screens

- Scout Choice
- HQ
- Team
- Athlete Detail
- 12-month Calendar
- Scout
- Applications / Inbox
- Hall of Fame
- Race Prep
- Live Race
- Full Results

## Project structure

```text
Assets/Scripts/
├── Core/
│   ├── GameManager.cs
│   └── MvpBootstrap.cs
├── Domain/
│   └── DomainModels.cs
├── Systems/
│   ├── CompetitionSaveSystems.cs
│   ├── RaceSimulator.cs
│   ├── RecruitmentSystems.cs
│   └── TrainingSystem.cs
└── UI/
    ├── AthleteCalendarScreens.cs
    ├── ClubScreens.cs
    ├── RaceScreens.cs
    ├── RecruitScreens.cs
    ├── UIController.cs
    └── UIPrimitives.cs
```

## Saving

- JSON save file in `Application.persistentDataPath`
- automatic saves after meaningful career actions
- development controls for Save / Load / Reset are available from HQ
- no backend or cloud save yet

## Running

1. Open the repository in Unity **6000.5.10f1**.
2. Open any scene or create a blank scene.
3. Press Play.
4. `MvpBootstrap` creates the game root and runtime UI automatically.

## Packages

The project uses Unity UI (`com.unity.ugui`) plus built-in Input, UI and JSON serialization modules. IMGUI is not used.
