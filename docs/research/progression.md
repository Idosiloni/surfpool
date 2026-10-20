# Leveling, unlocking and guiding the player
Date: 2026-10-20

Sourcing note: web search gave guides and player threads, almost no developer talks. Facts are marked "per guide" (a fan guide said it; I did not open the page). **(general knowledge)** = from memory, no source. **[Idea]** = my invention. Numbers can change between patches.

## 1. What we have today (read from the code)
- **Park level** (`ParkLevel.cs`): XP from happy surfers, missions (50), upgrades (10), daily challenges (30), your rides. Level L needs 60 x (L-1)^1.8 XP. On level-up: fireworks + banner "New: <upgrade names>" (`Celebration`), a toast, glow-up props (`ParkGlowUp`, stages at levels 1/3/5/8/11/15).
- **Level gates** (`UpgradeShop.cs`): every upgrade has `level` (1..20) and often `requires` (another upgrade). BALANCE.md says each gate sits about one level *below* where the money curve gets there, so **no purchase is ever blocked by level**.
- **Missions** (`MissionManager.BuildStory`): 15 linear missions, 4 chapters, marker in the world. Chapter 4 is ONE mission (4 objectives, $50k).
- **Daily challenges** (`DailyChallenges.cs`): 3 random small goals per morning, ~$100 + 15% of yesterday, 30 XP.
- **Goal strip** (`GameHud.UpdateGoal`): "cheapest upgrade you can buy next" with a money bar.
- **Journal (J)** already exists but only expands the mission card + challenge list.
- **Locked pools** already show a construction lot with fence + coral sign (`GreyboxBuilder` ~line 718), and the main pool shows an "expansion site" sign. So "see it before you can use it" is half done in the world.
- **SurfOS cards** (`UpgradeScreen.cs`): sorted buyable / too expensive / locked / owned; locked cards say "Unlocks at park level N" or "Needs X". Tab badges count buyable items.

## 2. How other games do it
| Game | What unlocks when | How the next goal is shown | What a level-up feels like | Source |
|---|---|---|---|---|
| Supermarket Simulator | Store level opens *license slots*; you still pay for the license. Licenses sit at levels 1/3/6/9/12 and on (per guide). 26 licenses to ~150 products. | Level bar; license list shows required level. | Level gives permission, **money gives the thing**, so level and cash compete for choices. Late unlocks (70+) cost so much that players call it grind. | [guide](https://gameplay.tips/guides/supermarket-simulator-product-licenses-guide-with-tips-and-tricks.html), [walkthrough](https://earlyguides.com/supermarket-simulator/walkthrough), [Steam thread](https://steamcommunity.com/app/2670630/discussions/0/677329154824225463/?ctp=2) |
| TCG Card Shop Simulator | Shop level gates card licenses (Ascension 32-count at 18, 64-count at 32, per guide), a workbench by about level 7; shop expansions bought in order. One run hit level 7 on day 3, level 13 on day 7 (per guide). | Level + license list. | Fast early levels, then slow. Same "level opens, cash buys" pattern. | [guide](https://www.whisperofthehouse.com/tcg-card-shop-simulator/progression-guide), [list](https://www.treyexgaming.com/?p=17335) |
| PowerWash Simulator | **Jobs** unlock by finishing earlier jobs (finishing Back Garden opened 3 more, per guide). **Stars** unlock better washers (15 / 40 / 90 stars, per guide). | A job list; completed jobs carry over to Free Play at 25% value. | Progress is a tangible clean surface. The tool upgrade is the "level-up". | [wiki](https://powerwashsimulator.wiki.gg/wiki/Career_Mode), [walkthrough](https://www.trueachievements.com/game/PowerWash-Simulator/walkthrough/3) |
| Two Point Hospital / Campus | Objectives give **stars**; stars open new campuses/levels; **Kudosh** (earned only from objectives) buys items. Star goals mix level, money, happiness (e.g. "campus level 9, earn $150k, 10 personal goals", per guide). | Three-star list per place. Rewards include busts and events. Players say later stars reward less ("patience, not skill"). | Stars = clear milestones with a mixed shopping list. | [guide](https://www.trueachievements.com/game/Two-Point-Campus/walkthrough/9), [Kudosh](https://www.thegamer.com/two-point-hospital-making-kudosh-guide/), [thread](https://steamcommunity.com/app/535930/discussions/0/2590022385665527144) |
| Planet Coaster 2 | Career: pre-built run-down parks, objectives rising in difficulty, Bronze to Platinum medals; medals open chapters; research points unlock rides. Park rating = your **weakest** area (per guide). | Objective list per park. One reviewer said objectives and design felt off balance. | Fix-up-a-broken-park is the same fantasy as ours. | [review](https://www.tomsguide.com/gaming/planet-coaster-2-review), [guide](https://www.gamerguides.com/planet-coaster-2/review) |
| Parkitect | Campaign map with fog that clears as you finish scenarios. Deadlines are optional; **medals pay to open map choke-points** (per guide). Sandbox toggles for money, research, objectives. | A world map. | Fog clearing is a reveal moment. Missing a deadline never fails you. | [moddb](https://www.moddb.com/games/parkitect), [devlog](https://www.texelraptor.com/blog/page:10) |
| Satisfactory | HUB tiers 0..8. Tier 0 is onboarding (upgrade the HUB in order); finishing it opens Tiers 1 and 2; later tiers open via Space Elevator phases (per wiki). You hand in materials, not XP. | Milestone screen at the HUB. | Each milestone is a *project you build toward*, and it changes how the factory looks. | [wiki](https://satisfactory.wiki.gg/wiki/Progression), [HUB](https://satisfactory.fandom.com/wiki/The_HUB) |
| Stardew Valley | Community Center: 30 bundles in 6 rooms. Each finished bundle gives a reward; finished room restores a part of the building (per guide). Bundles can be remixed. | Bundles sit as an open checklist you can fill in any order. | Gentle goals, a rebuilt place, no ticking clock. | [guide](https://www.playnews.gg/en/guides/stardew-valley-the-complete-community-center-guide-the-30-bundles-the-6-rooms-and-the-140-000g-joja-route) |
| Dave the Diver | Day dive feeds night restaurant; new systems (weapons, bracelets, research) arrive in small steps. Critics: late loop gets mechanical. | Story prompts + a new mechanic every few hours. | New *verb* beats new number. | [Digital Trends](https://uat4.www.digitaltrends.com/?p=3386157) |
| Hades | Mirror: spend keys to reveal talents, then crystals to upgrade; each talent has a mutually exclusive twin. Later patch added new talents (per guide). | The mirror is a room you walk to. | A choice (twin talents) + a physical place. | [guide](https://primagames.com/gaming/hades-guide-mirror-of-night-upgrades-fated-list-of-prophecies) |
| Vampire Survivors | Each unlock is tied to a specific odd condition (survive X min, find a rosary). An in-game Unlocks list acts as the guide. Power-ups fully refundable. | Unlocks menu. | Surprise: the *condition* is the fun. Refunds make trying things safe. | [Steam thread](https://steamcommunity.com/app/1794680/discussions/0/4631482569784832721), [guide](https://www.keengamer.com/articles/guides/vampire-survivors-ultimate-weapons-guide-all-unlocks-evolutions/) |
| Slime Rancher | Ranch areas start walled; bought with Newbucks; some also need a rancher's story tasks done (per wiki). Free to arrange the ranch. | Visible barriers you can see across. | Seeing the gate you cannot open yet. | [wiki](https://slimerancher.fandom.com/wiki/The_Ranch) |
| Car Mechanic Simulator 2021 | XP gives skill points spent in a Toolbox: tools, garage size, 36 skills in 6 groups. Players say everything is unlocked by about level 32 of 50 (opinion). | Toolbox menu. | A spend-your-points choice. Dead levels at the end feel empty. | [guide](https://www.magicgameworld.com/?p=126813), [thread](https://steamcommunity.com/app/1190000/discussions/0/3039354546886972322) |
| Gas Station Simulator | Story-tied renovations; a late "Popularity" meter the dev said was never meant to be maxed fast. Players call late grinding tedious. | Popularity bar. | Warning: slow meters at the end, no new verb. | [Steam thread](https://steamcommunity.com/app/1149620/discussions/0/3270183784611878537) |
| Cult of the Lamb, Rollercoaster Tycoon, Factorio, Internet Cafe 2, Kebab Chefs, Dinkum, House Flipper 2 | No sources found. **(general knowledge)** Cult: doctrine points choose a branch. RCT: scenario goals ("X guests, Y rating by year Z") plus research queue. Factorio: tech tree with *science packs you build*. | | | |

### Patterns that repeat
1. **Level opens a door, money buys the thing** (Supermarket, TCG). Works only if level and cash are *different* limits.
2. **Milestone = a project, not a number** (Satisfactory HUB, Stardew bundles, Planet Coaster objectives).
3. **A choice at the reward** (Hades twins, CMS skill points, Waterpark perks per [sim-games-2.md](sim-games-2.md)).
4. **You can see the door before it opens** (Slime Rancher barriers, Parkitect fog, locked cards).
5. **A new verb every few hours** beats a bigger number (Dave).
6. **Soft guidance:** objectives you can ignore (Parkitect optional deadlines, Stardew open checklist).
7. **Failure modes:** dead levels after everything is unlocked (CMS), huge late costs (Supermarket 70+), meters with no new verb (Gas Station), later rewards shrinking (Two Point).

### When guidance gets annoying (general UX, reasoned from the above)
- A marker you cannot hide, or a forced order. Ours says "missions guide, they never block" - keep that.
- Three trackers telling you three different things.
- Popups that stop the game on every level. Queue them, show them when the player is free (our `Celebration` already waits for menus to close).
- Goal-gradient research says a goal that *feels close* motivates most ([study](https://research.polyu.edu.hk/en/publications/drawing-goals-nearer-using-the-goal-gradient-perspective-to-incre/)); a pre-filled first step helps ([article](https://uxdesign.cc/the-power-of-perceived-progress-motivating-users-through-design-e91b1957266b)). The "show locked content" benefit is mostly practitioner opinion, not tested.

## 3. Surf-park ideas **[Idea]** (surf facts are general knowledge)
- **The Break Book (wave design as a tree):** each wave type is a "break" you learn: Beach break, Point break, Reef, Barrel. Real names (Waikiki, Malibu, Snapper, Pipeline, Teahupo'o) are already presets; make them *discoveries* with conditions like Vampire Survivors ("5 tube rides in a row", "ride 3 lefts").
- **Surf-culture factions:** Locals (love clean, uncrowded water), Groms (kids, love lessons/snacks), Pros (love big waves), Influencers (love decor/photo spots). Each has a reputation bar; filling one gives a signature unlock and a chooser reward. Maps to existing surfer types.
- **Visiting legends as milestones:** a famous surfer arrives at level N, rides your pool, the crowd goes wild, and they leave an endorsement (permanent perk) or their board on the wall. Fits the open roadmap item "celebrity surfer visits".
- **Sponsor contracts:** a brand offers "Serve 40 pros this week, keep 4.5 stars" for cash + a banner in the park. Pick 1 of 3 offers. Optional, with a deadline that never fails you (Parkitect style).
- **Contest calendar and seasons:** a visible calendar on the SurfOS computer (swell season, summer rush, contest weekend). A contest is the "boss" of each chapter: its entry rules are what the player needs to have built.
- **Construction as a process:** the construction lot exists; add an actual build: bulldozer, a sign with a progress bar, finished over 1-2 in-game days with a ribbon-cutting. Satisfactory-style: the project is the reward.
- **Kai's notebook as the guide:** guidance in the world, not a menu. Kai leaves notes pinned at places ("The Reef Pool: once the Lagoon lot is cleared..."). Diegetic, so it feels like story and not a quest log.

## 4. Gap list vs our systems (most important first)
1. **Level never changes a decision.** Gates sit below the money curve (`UpgradeShop.level`, BALANCE.md), so level-up = banner + toast + props. Fix: give each level-up something to *choose or do* (`ParkLevel.LeveledUp`, `Celebration.cs`).
2. **Next level is invisible.** `UpgradeShop.UnlockedAt(level)` is only used *after* a level-up (`Celebration`, `GameHud.ShowLevelUp`). The XP bar (`GameHud.UpdateLevel`, `Hud.uxml` xpLabel) and SurfOS (`UpgradeScreen.BuildCards`) never show what Level+1 brings.
3. **Three trackers that disagree.** Mission card (`MissionManager`), daily challenges (`DailyChallenges`) and the goal strip (`GameHud.UpdateGoal`, cheapest upgrade) each pick their own target. The strip can contradict the active mission.
4. **Chapter 4 is one mission** (`BuildStory`): the whole back half of the game ($40k to $500k) has no story steps.
5. **Glow-up ends at level 15** (`ParkGlowUp.StageLevels`) while the Lagoon gates at level 20 and the cap in practice is higher.
6. **Journal (J) is thin:** only shows the mission card + challenges. No unlock list, no "what's coming", no history of done chapters.
7. **Locked cards just sort to the bottom** (`UpgradeScreen.Rank`). No teaser art/name hint, no "level 7 shows 3 new things".
8. **One flat XP source list** (`ParkLevel`): no milestones that reward *style* (e.g. perfect days, contest wins), so rewards can't pull the player toward new play.
9. **Daily challenges are random and small**; no weekly/seasonal bigger goal and no streak.
10. **Open roadmap items that fit as milestones:** lessons area, celebrity visits, surf contests (DESIGN.md).

## 5. Proposal: "Kai's Plan" - one clear path

**One idea:** the park has ONE current **Plan** (the active chapter goal), and everything else feeds it. Level opens doors, money buys what is behind them, and each level-up gives a **pick**.

### What the player sees
- **HUD, left:** mission card stays the single "do this now". The goal strip (bottom) becomes "Plan step": if the mission needs an upgrade, the strip shows that upgrade (money bar); otherwise it shows the cheapest useful one as now. Daily challenges get a small collapsed line unless J is open. One target at a time.
- **HUD, XP bar:** under "60 XP to level 2" add one line: "Level 2 brings: Auto Wave Timer, Lifeguard" (from `UnlockedAt(Level+1)`). On hover/hold J, a full list for the next 3 levels.
- **SurfOS (computer):** a top strip "Next level: N - X XP to go - brings: ..." and a new tab or header "Plan" showing chapter milestones. Locked cards show the real name and icon, greyed, with "Level N" (done today) plus a "NEW at level N" tag the moment they open.
- **Journal (J):** pages: Story (chapters done, current), Unlocks (every upgrade by level, opened / coming), Break Book (waves discovered), Legends (visitors met). Fills in as you play (collection feel, Stardew bundles).

### What a level-up does
1. Banner + fireworks as now (`Celebration`).
2. **A pick card** (queued, shown when you are free): choose 1 of 3 *park perks* [Idea]. Examples: "Locals' Favorite: +10% tips"; "Crowd Pleaser: arrivals +8%"; "Green Park: running costs -5%"; "Fast Hands: staff +5% speed"; "Photo Spot: decor counts 1.5x". They stack, are small, and are permanent. Offer 1 pick every 2 levels (not every level, to avoid popup fatigue). Optional respec at the desk for a fee so trying is safe (Vampire Survivors refunds).
3. **New: one or two things to unlock** shown as before, now with a "go buy it" marker on the computer.
4. At milestone levels (5, 10, 15, 20, 25 [guess]) a **set piece**: glow-up stage + a new visitor/event (legend visit, contest invite, sponsor offer). Align with `ParkGlowUp.StageLevels` and extend the stages past 15.

### Make level matter (fix gap 1)
Move some gates to *above* the money curve for **features**, not stats: e.g. Surf Contest at 12, Sponsor deals at 9, Lessons Area at 7, Legend visits at 14 (all new). Keep today's cheap stat upgrades gated low so nothing stalls. This gives level its own job: **level = permission and variety; money = scale.**

### How the parts fit (one path)
| Layer | Role | Time scale | Reward |
|---|---|---|---|
| Daily challenges | tiny nudge toward the Plan (pick from what the mission needs) | 1 day | small cash + 30 XP |
| Mission / Plan steps | the guide, now with 6-8 steps in chapter 4 | 10-30 min | cash + XP + story note |
| Park level | permission + perk picks | 1-3 days | picks, set pieces |
| Chapter finale | a contest, a legend visit, a new pool opening | per chapter | big ceremony, new area/verb |
| Sponsor/Faction goals | optional side goals, no fail state | a week | perks, banners |

Rule: **at most one marker in the world, at most one bar in the strip, nothing blocks.** Missions in chapter 4 end with "ceremonies" (pool ribbon-cutting, contest day) so the player gets a real event, not just a toast.

## Ideas for our game (ranked, smallest first; each is about one session)
1. **Step 5 / tiny - "Level N+1 brings..." line under the XP bar.** Use `UpgradeShop.UnlockedAt(ParkLevel.Level + 1)` in `GameHud.UpdateLevel`; one extra label in `Hud.uxml` + USS. Fixes gap 2, no new systems.
2. **Step 5 / tiny - Goal strip follows the active mission.** In `GameHud.UpdateGoal`, if `MissionManager.Current` has an `Owns(...)` objective not yet owned and its level/requirement is met, show that upgrade first; else current cheapest logic. Fixes gap 3. Needs the objective to expose an upgrade id (small field on `Objective`).
3. **Step 5 / small - "Next level" strip at top of SurfOS** (`UpgradeScreen`): XP bar + names of upgrades at Level+1, and a "NEW" sticker on cards that unlocked at the current level. Gap 2 and 7.
4. **Step 5 / small - Flesh out chapter 4 in `BuildStory`.** Split it into 6-8 missions (sell out the Reef, 5 stars, 1,000 sessions, hire X staff, first $100k day, open Lagoon, host a famous wave, $500k). Gap 4. Reuses `Count`/`Flag`.
5. **Step 5 / small - Journal pages (J):** add an "Unlocks" list (all upgrades by level: opened / coming) in the existing journal (`journalOpen`). Collection feel, gap 6.
6. **Step 5 / medium - Level-up pick card.** New `PerkPicker` queued via `Celebration` after the banner; 3 small perks stored in `ParkEffects` and saved in `SaveSystem`. Every 2 levels. Gap 1 and 8.
7. **Step 5 / medium - Extend glow-up past 15** (stages at 20, 25) and tie the stage names to chapters (`ParkGlowUp.StageLevels/StageNames`, new `GlowItem`s from the builder). Gap 5.
8. **Step 5 / medium - Daily challenges follow the Plan:** pick challenges that match the active mission's topic first; add a 3-day streak bonus. Gap 3 and 9.
9. **Later / medium - Sponsor contracts:** a board in SurfOS with 3 offers, pick one, optional deadline, no fail state; reward = cash + perk + banner in park. Needs `SponsorBoard` + `GameStats` deltas (same pattern as `DailyChallenges`).
10. **Later / medium - Break Book:** wave presets and tricks as discoveries with Vampire-Survivors-style conditions; shown as a journal page. Builds on `famous_waves`.
11. **Later / large - Legend visits + Surf contest day** (roadmap items): a celebrity or contest event at set levels with a ceremony, crowd, and a permanent perk; gate them by level (the "level matters" fix).
12. **Later / large - Pool construction as a process:** turn the construction lot into a short build (bulldozer, progress sign, ribbon-cutting) when you buy a pool; Satisfactory-style project reward.

Smallest first order to build: 1, 2, 3, 4, 5, 6, then 7-12 as time allows.
