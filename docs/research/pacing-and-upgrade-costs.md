# Pacing and upgrade costs: what should be cheap, what should be expensive
Date: 2026-10-22

Why: Ido says "very fast you can get auto waves, which makes the game very easy". He is right. Auto Wave Timer is $200 at park level 2 (about 2 minutes of income at $95/min), so the player's main job (pressing the wave button) disappears in the first 20 minutes.

Tags: **[S]** = sourced (link), **[K]** = my general game knowledge, **[G]** = my judgment/guess for our game. Sources for specific numbers in other games are thin; I say so.

## 1. How good sims pace automation

**The rule: do the job by hand first, automate it as a reward.** [K]
- Factorio: you hand-mine and hand-craft for the first minutes; the first drill and the first belt feel great *because* your hands hurt. Satisfactory is the same (the HUB milestones make you hand-craft before the first machine). The automation is the prize for mastering the chore.
- Supermarket Simulator: the cashier hire is locked behind **200 checkouts you did yourself plus store level 10** (Cashier 2: 400 checkouts and level 20, Cashier 3: 900 and level 30). One player notes the count only grows if you actually work the register. Source: [commonsensegamer guide](https://commonsensegamer.com/supermarket-simulator-employee-guide-how-to-hire-workers-hcfe/) [S]. I found no reliable wage or hire-cost numbers, so do not copy prices from it.
- Order of staff in these games: first the *most boring* chore (restocking / checkout), last the *skilled* job. The skilled job is the fun, so you keep it. [K]
- Two Point Hospital: staff are hired from day one, but they are the *cost of growing* (wages are a constant drain), and better staff (higher skill) come from training and stars. Automation is never free. [K]
- Idle games: a payback time of 1-3 "sessions" for early upgrades; the first purchase in under ~2 minutes. See [The Math of Idle Games (Gamasutra/Game Developer)](https://www.gamedeveloper.com/design/the-math-of-idle-games-part-i) [S]. Our own earlier notes (incremental-economy.md) say the same.

**How long should a player do a chore before it can be automated?** [G, based on the above]
- Let the player do the chore **40-60 times, or about 15-25 minutes**, so they learn it and feel the pain.
- Then the automation should cost **about 6-10 minutes of the income they have at that moment** (a clear goal, not a trivial buy).
- Better than a timer: a **count gate**, like Supermarket's 200 checkouts. "Send 40 waves by hand" before the Auto Wave Timer can be bought. It also fixes the bug where a player just grinds money.
- Automate in this order: the *dull* verb first (carrying boards, picking trash), the *core* verb last (the wave button, selling tickets).

**Automation should be slightly worse than you.** [K] Our Auto timer sends one fixed wave every 12 s whether or not anyone is waiting (and every wave costs electricity). Smart Auto picks the type. The Wave Operator is the "good as you" one. So you still want to touch the console on a big day.

## 2. Cost-curve rules

### What is cheap
- **Anything that teaches a new verb or shows a new idea.** Turbo Pump, Bigger Lineup, Rental Stand, Trash Cans. Price: **1-2 minutes** of income. The player must always have one thing they can buy within a minute. [K, matches incremental-economy.md]
- **Quality of life that does not remove a verb.** A bench, a bigger lineup, a billboard. 2-4 minutes.
- **Cosmetics that make the place nicer early** (Tiki Garden, build-mode decor): cheap-ish, because they make the player proud and give the camera something to look at. 3-5 minutes. [G] (Our Tiki Garden also raises prices by $10, so it is really an income upgrade; keep it above cosmetics.)

### What is expensive
- **Automation of a core verb**: 6-10 minutes of current income, plus a count gate. (Auto Waves, Desk Clerk, Wave Operator.)
- **Big expansions** (bigger pool, a new pool): **15-40 minutes** of current income (3-6 in-game days of 6 minutes). Our pools already do this.
- **Prestige/status items** (Grandstand, Season Passes, the last pools): 30-60 minutes. They should be goals you talk about.

### Ratios between tiers [K/G]
| What | Ratio |
|---|---|
| Repeating small upgrade, each level | x1.15 to x1.25 |
| Next tier of the same line (Sets -> Smart Auto -> Operator) | x2.5 to x4 |
| Pool to next pool (we have x4.4, x1.9, x2) | x2 to x5, higher is fine if income also jumps x3+ |
| Wage per minute | about 3-10% of the extra income that worker frees (we use $3-$30/min) |

### Minutes of income per purchase, by stage [G]
| Stage | Typical buy | Minutes of income |
|---|---|---|
| First 10 min | any first upgrade | 1-3 |
| Day 2-5 | mid-size upgrade | 3-6 |
| Day 5-15 | automation / staff | 6-10 |
| Day 10-25 | a new pool | 15-35 |
| Late | status item / last pool | 30-60 |
Never go over ~10 minutes without something affordable (our existing rule). Gas Station and late Supermarket players call 60+ minutes between goals "grind". See progression.md.

### How many goals visible at once
- **Three**: one cheap (under a minute away), one medium (2-6 min), one big (the next pool). We already have goal strip + mission + daily challenges; progression.md says make them agree. Adding a 4th (a second bar) makes players ignore all of them. [K]

### Avoiding "everything unlocked = nothing to do"
How other sims do it, and what we can borrow:
- **Ratings/stars** (Two Point, Planet Coaster): a number you can improve forever. We have stars + reviews. [K]
- **Contracts and events** (RCT scenarios, Parkitect medals): optional jobs with a reward. We have DailyEvents, DailyChallenges, SurfRush, famous visits.
- **Bigger customers**: each tier of customer pays more AND wants something harder (pros want hollow waves, legends want XL).
- **Prestige / reset** is NOT right for a first-person sim (my judgment). Use perks + level picks instead (already built: ParkPerks).
- **Stagger the *kinds* of goals**: money, level, stars, "do X without Y". If every goal is "have $N", players only wait.
- Car Mechanic Sim players say everything is unlocked around level 32 of 50 and the last levels feel empty (progression.md). Keep unlocks spread to the end: our last unlock is the Ranch at L25. Keep levels beyond that for perks and cosmetics.

## 3. Income per minute at each stage (from BALANCE.md, mapped to levels by me [G])
Day = 6 minutes. These numbers assume the player raises the base price. BALANCE says the level gates sit about one level *below* where the money curve gets there.
| Park level | About | Income/min | One day (6 min) |
|---|---|---|---|
| 1-2 | day 1 | $30 -> $95 | $180-570 |
| 3-4 | day 2-5 | $95 -> $210 | $570-1.3k |
| 5-7 | day 5-10 | $210 -> $430 | $1.3-2.6k |
| 8-10 | day 8-12 | $430 -> $880 | $2.6-5.3k |
| 11-14 | day 11-15 | $880 -> $1.5k | $5-9k |
| 15-18 | day 16-21 | ~$2.7k | $16k |
| 19-22 | day 22-27 | ~$5k | $30k |
| 23+ | late | ~$8k | $49k |
Mission rewards (about $60k, $50k in chapter 4) go on top.

**The bug:** Auto Waves ($200, L2) costs 2 minutes of income, and Lifeguard $250, Desk Clerk $500 (L3) and Wave Operator ($6k, L13, about 7 min) are all cheap *and* early. The player owns Auto + Desk Clerk + Lifeguard by day 3, so the only verb left is carrying boards. The Wave Operator (the "full automation" upgrade) is 6 minutes of income at level 13, when Auto Wave has been running for 10 days anyway, so nobody needs it.

## 4. Re-pricing table (all upgrades in UpgradeShop.cs)
"Min" = proposed cost divided by income/min at the level it unlocks (using the table above, lower end of the range). Bold = a real change. Everything marked "keep" stays as it is.

### Waves (the big fix)
| Id | Now | Proposed | Min | Why |
|---|---|---|---|---|
| faster_machine (Turbo Pump) | $75 L1 | keep | 1 | Teaches "upgrades make waves faster". Cheap first buy. |
| **wave_sets** | $900 L5 | **$500 L3**, req Turbo | 3-4 | A fun manual upgrade (more rides per press). Play it by hand first. It becomes the "mastered the console" step. |
| **auto_waves** | $200 L2 | **$3,000 L8**, req Wave Sets, plus count gate "send 40 waves by hand" | ~7 | Removes the core verb. At L8 the player is at ~$430/min and has run the console for 5+ days. Make this the first big "Yes!" buy. |
| intermediate_waves | $600 L3 (req Exp I) | keep | 3-6 | A tier jump that raises income x2.5; stay cheap. |
| wave_shaper (Reef Shaper) | $2,500 L8 | keep | 3-6 | New verb (lefts/rights/hollow). |
| pro_waves | $4,500 L11 (req Exp II) | keep | 5 | Tier jump; the pair Exp II + Pro = $8.5k = ~10 min. |
| heavy_duty_pumps | $3,500 L9 | keep | 4-8 | Only matters once breakdowns bite. |
| **smart_auto** | $2,000 L8 | **$6,000 L11** | ~7 | Upgrade to automation; should come after Auto has run for a while. |
| turbo_2 | $4,000 L12 | keep | 4 | Faster recharge; fits Pro. |
| famous_waves | $6,000 L12 | keep | 6 | Content pack, a goal for Pro days. |

### Staff (who replaces which manual job)
Rule: the *dull* job first, the *core* job last. Add a count gate for jobs you do by hand (like Supermarket's 200 checkouts).
| Id | Replaces | Now | Proposed | Min | Why |
|---|---|---|---|---|---|
| **lifeguard** | your safety net (patience, rescue) | $250 L2 | **$800 L4** | 4 | Not a day-1 purchase. You first learn the kook rescue by hand. |
| **desk_clerk** | selling tickets (a core verb) | $500 L3 | **$2,000 L7**, count gate "sell 60 sessions by hand" | ~5 | Selling is the first thing you learn; keep it for ~day 5-6. |
| **board_runner** | carrying boards to the rack | $600 L4 | **$900 L5**, req Rental Stand + 20 boards carried | 4 | Dull chore; first hire for most players. This should be the *first* hire. |
| **rental_clerk** | handing out boards | $700 L5 | **$1,500 L7** | 4 | After the runner. |
| **janitor** | trash pick-up | $900 L5 | keep | 4 | Dull chore. |
| snack_cook | grill | $1,500 L6 | **$2,000 L7**, gate "serve 20 snacks" | 5 | A side job; hire when the grill steals time. |
| **mechanic** | repairing machines | $1,800 L6 | **$2,500 L8** | 5 | Repairs start to matter at L8. |
| surf_coach | lessons | $2,500 L9 | keep | 3-5 | A new income stream, not just a chore. |
| **acai_barista** | acai shack | $1,400 L7 | **$2,500 L8** | 5 | Cheaper than the shack's own income. |
| **gear_clerk** | gear shop | $3,000 L11 | **$5,000 L12** | 5 | |
| **shaper_apprentice** | board shaper (~$600/min!) | $9,000 L14 | **$20,000 L15** | 13 | Frees $500+/min of work. Priced on value. |
| **wave_operator** | the Auto timer and your console in every pool | $6,000 L13 | **$18,000 L16** | ~7 at $2.7k | The full-automation prestige hire. Idea: Auto Wave works in the main pool only; new pools need the Operator (see section 6). |

### Pools
| Id | Now | Proposed | Min | Why |
|---|---|---|---|---|
| pool_expand_1 | $400 L3 | **$500 L3** | ~4 | First "real" goal (mission pays $300). |
| pool_expand_2 | $3,000 L10 | **$4,000 L10** | 5-9 | Needs Intermediate; fine. |
| pool_reef | $18,000 L15 | keep | 7-20 | ~3-5 days of income. |
| pool_lagoon | $80,000 L20 | keep | 16-30 | Same. |
| pool_cove | $150,000 L22 | keep | ~18 | Same. |
| pool_ranch | $300,000 L25 | keep | ~37 | Final goal. |

### Park, rentals and shops
| Id | Now | Proposed | Why |
|---|---|---|---|
| more_surfers | $50 L1 | keep | First buy, 1 minute. |
| billboard | $150 L2 | keep | 2 minutes; first "more customers" lesson. |
| huge_lineup | $1,200 L6 | keep | |
| social_ads | $1,500 L7 | keep | |
| tiki_garden | $2,000 L8 | keep | Cosmetic *and* +$10 fair price. |
| beach_passes | $500 L3 | keep | New verb; side income. |
| fishing_pier | $700 L4 / pro_rod $2,000 L8 | keep | Side verb. |
| trash_cans | $300 L3 | keep | Cheap QoL. |
| solar_panels | $1,500 L4 | **$1,500 L6** | Power bill is tiny at L4. |
| water_recycling | $5,000 L12 | keep | |
| snack_bar $1,200 L5 / smoothie $2,500 L8 | | keep | |
| photo_tower | $4,000 L9 | keep | |
| pool_robot | $1,200 L5 | keep | Dull chore, cheap. |
| acai_bar $1,500 L6, turbo blender $900 L7 | | keep | |
| surf_shop $3,500 L10, card reader $2,200 L11 | | keep | |
| board_shaper $12,000 L13, power planer $5,000 L14 | | keep | |
| vip_sessions | $8,000 L14 | keep | |
| grandstand | $22,000 L17 | keep | |
| season_passes | $35,000 L18 | **$45,000 L19** | $1,000/min passive and "even while you sleep": the best payback in the game (a day at L19 is $30k). Pay for it. |
| rental_stand $100 L1, more_boards $300 L2, premium_boards $400 L4 | | keep | Cheap verb teaching. |

## 5. Goals by stage, and how missions line up
| Stage | Aim | What the player buys | Current mission |
|---|---|---|---|
| **Day 1** (first 10 min) | Learn sell, wave, upgrade; make the first $150 | Turbo ($75), Bigger Lineup ($50), Rental Stand ($100) | "Open the doors" ... "Fix up the place". Good. |
| **Day 2-3** | Fill the lineup, learn sets and rentals, ride a wave yourself | Billboard, Wave Sets, Pool Exp I | "Owner's privilege", "Bigger waves" ($300). Good. |
| **Day 4-8** | First help: board runner, then Lifeguard; Intermediate waves | Runner, Rental Clerk, Auto Wave | "Hire some help" should point at the **Board Runner**, not "any staff". |
| **Mid game** (L8-14) | Pro waves, shops, a side hustle, Desk Clerk | Auto, Exp II, Pro, shops | "Go pro" $1,500, "Get barreled" $2,000. |
| **Late** (L15-25) | Second pool -> Lagoon -> Cove -> Ranch | Wave Operator, pools | Chapter 4 is ONE mission ($50k). Split it (progression.md already says this). |

Mission changes (small, in `MissionManager.BuildStory`):
- "Fix up the place" (buy *any* upgrade) -> keep, but add "$75 Turbo Pump" as the pointed-at buy.
- "Hire some help" (any staff) -> "Hire a Board Runner". After this change the cheap hires are the dull chores.
- Add mission **"Wave master"** (ch2 end): "Send 40 waves and keep patience above half" -> reward = a coupon: "Auto Wave Timer now available". This is the "automation as a reward" step. [G]
- Add mission **"Desk duty"** (ch3): "Sell 60 sessions" -> Desk Clerk unlocked. [G]
- Chapter 4: split into pool-by-pool steps with a ceremony each.

## 6. Ranked recommendations
1. **Move Auto Wave Timer to L8, $3,000, require Wave Sets and a count gate of 40 hand-sent waves.** This is the direct answer to Ido. (UpgradeShop `auto_waves`; the gate can use `GameStats.WavesSent`.)
2. **Make Wave Sets cheap ($500, L3).** It is the fun manual upgrade; good players should enjoy the button before they lose it.
3. **Re-price the Wave Operator to $18,000, L16, and make it the only way to auto-run new pools.** Auto timer = main pool only; Reef/Lagoon/Cove/Ranch consoles stay manual until the Operator. Then automation is a reward and pools still need you. [G]
4. **Staff order: dull chores first** (runner, janitor, pool robot, rental clerk), **core jobs last** (desk clerk, operator). Add count gates ("carried 20 boards") like Supermarket's 200 checkouts.
5. **Push Lifeguard to $800 / L4.** At $250 it removes the kook rescue mini-game before the player learns it.
6. **Pay for passive income**: Season Passes $45k, apprentice shaper $20k. Anything that earns while you stand still should cost 30+ min of income.
7. **Keep the cheap first-hour buys exactly as they are** (Bigger Lineup $50, Turbo $75, Rental Stand $100, Billboard $150): the player always has something to buy in under a minute.
8. **Make "Hire some help" and "Bigger waves" name the specific upgrade** so the goal strip and the mission always agree (`Objective.upgradeId`, already supported).
9. **Show the player the *price in minutes*** on the upgrade card ("about 6 min of income") from the day report income. A designer's tool and a player's goal in one number. [G]
10. **Measure with a bot, not by feel.** Re-run the income model in BALANCE.md with the new prices; check that no stretch longer than ~8 minutes has nothing affordable, and that the first Auto Wave buy lands around day 5-7.
11. **Leave the pool curve alone** (Reef 3-5 days of income, Lagoon similar). It matches the 3-5 day rule and the 2.5-hour plan in BALANCE.md.
12. **Playtest question for Ido:** after the change, is days 2-5 too slow? If yes, give "Wave master" a $400 reward, not a cheaper Auto.

## Ideas for our game
- **Step 3 (money loop):** re-price the table in section 4; add count gates (`GameStats` counters for waves, tickets, boards carried) to Auto, Desk Clerk, Board Runner. Add the "price in minutes" line to cards.
- **Step 3/4:** two new missions, "Wave master" and "Desk duty", that unlock automation as a reward. Chapter 4 split into steps.
- **Step 4+:** Auto Wave works only at the main pool; Wave Operator needed to automate each new pool.
- **Later:** a "Pool manager" hire per pool at the very end ($60k+), the same shape as Two Point's hospital managers. Ranch return lane / jet-ski as late prestige (BALANCE.md already lists it).
- **Later:** sponsor contracts and a contest calendar (progression.md) as optional goals, so a rich late-game player has something besides "have $N".
