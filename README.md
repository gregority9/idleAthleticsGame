# Track Dynasty — MVP 0.3.5

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

## Phone viewport

The game is built around a fixed logical **430 × 930 portrait phone viewport**.

- Unity `CanvasScaler` uses `Scale With Screen Size`
- `Screen Match Mode` is `Expand`
- on PC the phone is centered with dark letterboxing
- desktop preview controls: `−`, `FIT`, `+`, plus current zoom percentage
- manual zoom range is **50%–150%**
- desktop standalone requests a **516 × 1116 windowed** phone-shaped window
- mobile builds request portrait orientation
- long game screens use internal scroll views

## Runtime UI safeguards

- shared ScrollRect hierarchy uses `RectMask2D`
- stale screen children are disabled before deferred destruction so they cannot intercept clicks
- screen construction is guarded: runtime UI exceptions display a visible `SCREEN ERROR` panel instead of a blank area

## MVP 0.3.5 — athlete management, training load and recovery

### Starter athlete migration

- Andre Campbell is guaranteed to exist for pre-0.3.5 saves unless he already retired into the Hall of Fame.
- invalid/missing selected-athlete IDs are repaired automatically.
- Andre is explicitly selected after the initial scout choice and receives competition offers.

### Fatigue and training load

Fatigue is stored as `0.0–1.0` and displayed as `0–100%`.

- 0–20%: Fresh
- 20–50%: Normal load
- 50–70%: Tired
- 70–85%: Very tired
- 85–100%: Overloaded

Training separates:

- focus: Sprint / Strength / Technique
- load: Rest / Light / Normal / Hard

The athlete screen shows:

- current fatigue status
- estimated race effectiveness
- recommended training load
- estimated daily fatigue change

Fatigue now has progressive penalties to both training efficiency and race performance instead of one simple linear modifier.

### Recovery

- Rest load: about -5.5 percentage points fatigue/day
- Physio: $250, -12 pp fatigue, 7-day cooldown
- Focused camp: 7 days, $1500, boosted training in the current focus
- Recovery camp: 5 days, $900, roughly -40 pp fatigue

Camps are blocked when any club athlete has a scheduled competition during the camp window.

### Competition breaks

Each athlete can deliberately pause racing for:

- 14 days
- 30 days
- 60 days

A scheduled competition can be cancelled into a 14-day break. During a break the athlete keeps training but receives no race offers. Competitions can be resumed early.

### Calendar controls

HQ includes:

- Advance Day
- Advance 7D
- Next Event

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
