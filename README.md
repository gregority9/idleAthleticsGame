# Track Dynasty — MVP 0.3.3

Target Unity Editor: **6000.5.10f1**

Mobile-first athletics manager prototype.

## Career

- starts on 1 January 2027
- one starting athlete: Andre Campbell
- choose one of three scouts
- full January–December calendar with real dates
- each athlete has an independent training plan and competition schedule
- after each race, choose the athlete's next event from several offers
- Local / Regional / National / International / Elite competition tiers
- PB-based qualification standards
- multiple athletes can race on the same day
- daily training between competitions
- annual aging, decline, retirement and Hall of Fame

## Recruitment and development

- hidden exact potential with a visible scout estimate range
- scout-dependent evaluation/network/cost strengths
- scouting shortlist and athlete signing
- inbound applications after strong club results
- applicant quality scales with performance and club reputation
- applications expire after 30 days
- Speed, Acceleration, Strength, Technique, Mental
- Form and Fatigue
- age curve, development rate and potential-gap progression
- training focuses and athlete traits
- race history and yearly career history

## Racing

- 100 m in MVP 0.3.x
- Explosive Start / Balanced / Late Push strategies
- continuous movement through 20/40/60/80/100 m splits
- circular country flags are the temporary runner presentation
- photo finish at <= 0.03 s
- club and world records
- full 1–8 results table: place, lane, country, athlete, time and gap
- PB / CR / WR badges

## MVP 0.3.3 phone viewport fix

The game is now built around a fixed logical **430 × 930 portrait phone viewport**.

- Unity `CanvasScaler` uses `Scale With Screen Size`
- `Screen Match Mode` is `Expand`, so the whole phone reference area fits instead of being cropped on wide desktop windows
- on PC the phone is centered with dark letterboxing around it
- desktop preview controls are outside the phone viewport: `−`, `FIT`, `+`, plus the current zoom percentage
- manual zoom range is **50%–150%**
- `FIT` returns to the correctly fitted phone scale
- desktop standalone builds request a **516 × 1116 windowed** phone-shaped window
- mobile builds request portrait orientation
- long game screens use internal scroll views
- the zoom system no longer polls `UnityEngine.Input` directly

## Saving

- JSON save in `Application.persistentDataPath`
- autosave after meaningful career actions
- Save / Load / Reset development controls on HQ

## Running

1. Open the project in Unity **6000.5.10f1**.
2. Open or create any blank scene.
3. Press Play.
4. `MvpBootstrap` creates the runtime UI automatically.

## Packages

The project uses Unity UI (`com.unity.ugui`) and built-in UI / JSON serialization modules. IMGUI is not used.
