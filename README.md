# Track Dynasty — Weekly Career Redesign

Target Unity Editor: **6000.5.10f1**

This branch is a hard mechanical redesign. It does **not** preserve the old MVP 0.3 save format or its day-by-day / 100 m-only career loop.

## Career start

- career starts at **Week 01/52, 2026**
- starting cash: **$5,000**
- player names their management
- player chooses **1 of 5 Polish athletes**, all age 8
- starter athletes begin at a very low level, with visible distance ratings and a hidden exact potential
- visible potential is shown as an estimate range that narrows over time

## Time model

- one season = **52 weeks**
- the main action is **Advance to Next Week**
- weekly processing includes training, fatigue/recovery, staff/sponsor finances and possible inbound applications
- unresolved competitions in the current week must be completed before advancing
- after Week 52 the year rolls over, athletes age by one year and a new competition calendar is generated

## Distances

The game now supports:

- 100 m
- 200 m
- 400 m
- 800 m
- 1500 m

Athletes have base attributes:

- Speed
- Acceleration
- Strength
- Endurance
- Technique
- Mental

Each distance rating is derived from those attributes plus a hidden-ish athlete aptitude profile. This suggests specialization without forcing it.

Every distance has its own PB, race count and win count.

## Age categories

- U10: age <= 10
- U12: 11–12
- U14: 13–14
- U16: 15–16
- U18: 17–18
- Junior: 19–23
- Adult: 24–32
- Senior: 33+

Competition eligibility can restrict age categories independently of distance and competition range.

## Competition model

Competitions are now **global meets**, not per-athlete random offers.

Ranges:

- Local
- City-wide
- Regional
- Country
- Continent
- World

A meet stores:

- week
- city
- range
- geographic scope
- allowed age categories
- available distances
- estimated field strength and spread
- entry fee
- prize / reputation values
- championship flag

The athlete screen shows upcoming meets, approximate field level and expected average times. The player chooses which athlete enters which distance.

Country and continent competitions apply geographic eligibility.

## Training

Every athlete has a weekly plan with:

- training distance: 100 / 200 / 400 / 800 / 1500
- training focus: Speed / Acceleration / Strength / Endurance / Technique / Mental / Recovery

Distance training changes multiple base attributes rather than directly adding points to one distance rating.

Training is affected by:

- age
- development rate
- fatigue
- traits
- assigned coach, when available

Injuries are possible. Coach specialization can improve training efficiency and reduce injury risk in strong training categories.

## Staff foundation

The data model already contains hooks for:

- coaches
- strength coaches
- dietitians
- physiotherapists

A coach has:

- primary distance
- two or more secondary distances
- strong training categories
- quality
- athlete capacity
- weekly salary

Hiring and assignment UI is intentionally left for the next progression layer.

## Auto management

Auto management is prepared but not implemented yet.

It unlocks when the roster reaches **10 athletes**.

## Recruitment

The old starting scout flow has been removed.

Athletes can now apply to the management directly. Application frequency and candidate quality improve with:

- management reputation
- strong race results
- higher competition range
- wins, podiums, PBs and club records

Applicants can arrive at many different ages. Early in a career they are mostly Polish/local; stronger international applicants become possible as reputation grows.

## Reputation

Management reputation is the main meta-progression value.

Race results increase reputation, and reputation improves the quality/frequency of future athlete applications and sponsor opportunities.

## Sponsorship

Race performance builds per-athlete sponsor interest.

When interest is high enough, an athlete can receive a sponsor offer containing:

- signing bonus
- weekly payment
- win bonus
- contract duration

Accepted sponsorship contracts generate recurring income.

## Racing

The race simulator supports all five distances.

Strategies:

- Fast Start
- Balanced
- Late Kick

Longer races are visually accelerated so a 1500 m event does not take several real minutes to watch.

Results include:

- full 1–8 standings
- finish time
- PB / club record status
- cash reward
- reputation reward
- sponsor-interest gain

## UI

Main screens:

- Setup / starter choice
- HQ
- Team
- Athlete Detail
- Competition Calendar
- Applications / Inbox
- Race Prep
- Live Race
- Results

The persistent header shows:

- management name
- current week / year
- cash
- reputation
- athlete count

## Saving

The redesigned career uses a new save file:

`track_dynasty_weekly_v1.json`

Old MVP 0.3 saves are not migrated or loaded.

## Running

1. Open the repository in Unity **6000.5.10f1**.
2. Open any scene or create a blank scene.
3. Press Play.
4. `MvpBootstrap` creates the runtime game root and UI automatically.
