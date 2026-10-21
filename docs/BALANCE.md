# Money balance (2026-10-20)

Goal: the incremental feeling. You start at about **$30 a minute** with a tiny pool. You end at about **$8,000 a minute** with three pools, and the Big Wave Lagoon is the last big goal (around day 25, about 2.5 hours).

## How money works now
- **Session price = base price × skill × pool.** You set the base price (a beginner session in the main pool) with the +/- buttons at the desk.
  - Skill: Beginner ×1, Intermediate ×2.5, Pro ×6.
  - Pool: Main ×1, Reef ×2.5, Lagoon ×5.
  - So a Pro in the Lagoon pays 30× the base price ($600 at a $20 base).
- **Each surfer will pay up to 80–150% of the "fair" price**, times the same multipliers. The fair price starts at $25. Tiki Garden (+$10), Reef Shaper (+$5) and VIP (+$25) raise it. If your base price is above what a surfer will pay, they leave.
- **Surfers arrive about every 20 s at first.** A better rating, the Billboard, Social Ads and the Grandstand make them come faster.
- **How many surfers can be in the park at once** is the other limit. It grows with Bigger Lineup, the rental stand, Huge Lineup, the pool expansions and new pools.
- **One surfer's visit takes about 2 minutes** (walk in, pay, 3 waves, walk out). Measured in-game: with 2 surfers allowed, the park serves about 1 surfer a minute.

## The curve (from the model in this file's numbers)
| When | Buy | Income after |
|---|---|---|
| Day 1 | Bigger Lineup $50, Turbo Pump $75, Rental Stand $100 | ~$95/min |
| Day 2–3 | Billboard $150, Auto Waves $200, Lifeguard $250 | ~$95/min |
| Day 3–5 | **Pool Expansion I $400**, **Intermediate waves $600**, Desk Clerk $500 | ~$210/min |
| Day 5–10 | Rental staff, Wave Sets, Huge Lineup $1.2k, Social Ads $1.5k, Tiki Garden $2k | ~$430/min |
| Day 11–15 | **Pool Expansion II $3k**, **Pro waves $4.5k**, Coach, Surf Shop | ~$880/min |
| Day 16–21 | Turbo II, Wave Operator $6k, VIP $8k, **Reef Pool $18k** | ~$2,700/min |
| Day 22–27 | Grandstand $22k, Season Passes $35k, **Big Wave Lagoon $80k** | ~$8,200/min |

Mission rewards (about $60k in total, $50k of it for finishing chapter 4) speed this up a bit.

## Measured in the game (4x speed, staff doing the chores, base price left at $20)
| Park | Model | Measured |
|---|---|---|
| Starter pool, 2 surfers | ~$30/min | $32/min (1 surfer/min, 0 lost) |
| Expanded pool, Intermediate waves, 7 surfers | ~$210/min | $202/min |
| Full pool, Pro waves, VIP, 12 surfers | ~$1,300/min with the price raised | $584/min at a $20 base price |

The last row shows why the price buttons matter: the upgrades raise what surfers will pay (fair price $65 by then),
but you only earn it if you raise your base price. The price buttons now say what surfers expect.

## Running costs (paid at closing time, listed in the day report)
- **Electricity:** every wave uses power: Beginner $1, Intermediate $3, Pro $6, times the pool multiplier (Reef x2.5, Lagoon x5).
  The console and HUD show the cost per press. **Solar Panels** ($1,500) halve it.
- **Water:** each open pool, per day: main pool $10 (starter) -> $25 (full size), Reef $60, Lagoon $150. **Water Recycling** ($5,000) halves it.
- **Business tax:** 6% of the day's income.
- The HUD's "Today's bills" grows during the day. Together with wages, costs aim at 15–25% of income (what other sims do, see docs/research/sim-games-2.md).

## Park level (XP)
- XP: happy surfer 10 / 20 / 35 (Beginner / Intermediate / Pro), mission 50, upgrade bought 10, your own ride 15.
- Your own ride (sponsor bonus): $1 per 120 points, max $40. A good ride with flow and combos scores about 2,500 (about $20); a plain one about 1,000 (about $8). Carves only count when held 0.6 s+ at speed, so tapping can't farm it.
- Park perks (ParkPerks): 1 pick every 2 park levels (8 perks max), pick 1 of 3, permanent and stacking: tips +15%, arrivals +10%, power+water bills -12%, patience +15%, wipeout mood +0.2, machine wear -15%, litter -20%, own-ride sponsor pay +25%. No XP perk (it would speed up its own picks). All are decisions for Ido to tune.
- Level L needs 60 x (L-1)^1.8 XP in total (L2 60, L3 209, L5 727, L10 3,175, L15 7,148, L20 13,000).
- Each upgrade has a level gate set about one level below where the money model reaches it, so levels unlock
  things without making you wait (checked in the model: no purchase is ever blocked by its level).

## Breakdowns, staff stars, trash, daily events (first guesses, tune after playing)
- Machine wear per wave: 0.4% / 0.6% / 0.9% (B / I / P). Warning at 70%, then a growing chance to break per wave.
  Roughly one service every 1-3 days per busy pool. Repair is free (hold E 3 s). Mechanic $1,800 + $10/min,
  Heavy-Duty Pumps $3,500 halve wear.
- Staff stars at 15 / 50 / 120 / 250 jobs: each star 10% faster, wage +15%.
- Litter: a walking surfer drops something about every 2 minutes on deck (Trash Cans: 60% less, Heat Wave: 80% more).
  Each piece lowers surfer happiness; 20+ pieces = -0.3. Janitor $900 + $5/min.
- Daily events (from day 2, 65% of days): Heat Wave (+40% arrivals), Rainy Day (-40% arrivals, half water),
  Surf Contest (skilled sessions +50%, more pros), Influencer (rating x2 speed, XP +50%), Power Price Spike
  (power x2), Health Inspector at 3 PM (<= 3 trash: bonus 10% of the day's income, min $100; otherwise a fine of
  $50 + $15 per piece, never more than a fifth of your money).

## Rules of thumb used
- Early buys take 1–3 minutes of income, mid-game buys a few days, and each new pool about 4–5 days.
- Each wave tier needs the bigger pool first (Expansion I → Intermediate, Expansion II → Pro). That's the "bigger waves need a bigger pool" step.
- Staff wages stay under about 10% of what they help earn ($3–$25 a minute).
- Money shows as `$12,450`; pop-ups shorten big amounts (`+$48.2k`).

## Things to tune after Ido plays
- If day 2–3 feels slow (several buys that don't raise income), make Billboard/Lifeguard cheaper or give the "Fix up the place" mission a reward.
- If the end feels too fast or slow, change the pool costs (`UpgradeShop.cs`) and the pool multipliers (`price` in `GreyboxBuilder` pool specs).
- All numbers live in `UpgradeShop.cs` (costs, effects), `PriceBoard.cs` (skill multipliers), `GreyboxBuilder.cs` (wages, pool price multipliers) and `MissionManager.cs` (rewards).

## Counter shops (2026-10-21, first guesses: all are proposals for Ido)
| Upgrade | Cost | Level | What it earns |
|---|---|---|---|
| Acai & Shake Shack | $1,500 | 6 | Bowl $12 (+$2 per 3rd topping), shake $9 (+$2 per 2nd fruit), + half the snack bonus; tip $4 if served within 12 s of reaching the counter ($2 within 24 s). A customer every 14-24 s. |
| Surf Gear Shop (was "Surf Shop", same id) | $3,500 | 10 | Shorts $40, rash guard $35, fins $55, leash $25, sunscreen $12, wax $6; 1-3 items per customer; tip $6 within 25 s; extra item brought along sells 50% of the time. A customer every 18-30 s. Still gives the old $15 merch per happy surfer. |
| Custom Board Shaper | $12,000 | 13 | Funboard $750, shortboard $900, quad fins +$50; 40% deposit up front, rest on hand-over; tip $120 if done within 70 s. A customer every 70-110 s, one board at a time. |
| Turbo Blender | $900 | 7 | Blending 1.2 s -> 0.5 s, barista 30% faster. Visible: big red TURBO blender. |
| Card Reader | $2,200 | 11 | Ringing up 2.5x faster, +$3 tip on every gear sale. Visible: tap-to-pay terminal + sign. |
| Power Planer | $5,000 | 14 | Shaping 4 s -> 2 s, apprentice 25% faster. Visible: planer on a ceiling rail + dust extractor. |
| Acai Barista | $1,400 + $7/min | 7 | Serves acai orders (7 s each, half tip). |
| Shop Clerk | $3,000 + $10/min | 11 | Rings up gear orders (9 s each, half tip). |
| Apprentice Shaper | $9,000 + $30/min | 14 | Takes orders and builds boards (40 s each, third of the tip). |

Rough income if you work a shop yourself: acai ~ $40-60/min, gear ~ $80-150/min, shaper ~ $500-700/min (about 1 board a minute and a half).
Shops scale with arrivals (ads, events, perks) at the square root, so they stay a side hustle next to sessions. Smoothie Bar is now "Bigger Snack Menu" (same id and +$8).
Open questions for Ido: should the gear shop keep the passive $15 merch bonus on top of selling? Are shaper prices too high for level 13 (about 1 board = a few minutes of session income)?

## Legends (famous surfers, 2026-10-21, proposals)
- From park level 15 with Pro waves owned, one legend walks in every 4-7 minutes (faster on busy event days), one at a time.
  Each has a level: Leilani + Jett 15, June 16, Skip 17, Rio 18, Bruno 19, Ingrid 20.
- They ride the best open pool as pros and will pay up to 4x the fair Pro price (so a high price works on them).
- Leaving happy: appearance bonus = 8x what they paid (times happiness), +150 XP, and 3 perfect reviews for the rating.
  Leaving unhappy: nothing extra and a "left unimpressed" toast.

## Surf photographer (2026-10-21, proposals)
- Upgrade "Surf Photographer": $4000, park level 9. Climb the tower, zoom and shoot surfers.
- Grade = 45% moment (barrel 1, trick in the last 0.8 s 0.85, celebrating 0.6, just riding 0.4) + 20% centered
  + 20% size in frame + 15% surfer skill. S >= 0.82, A >= 0.62, B >= 0.45, else C (C prints don't sell).
- A surfer who leaves happy buys their best print: B $15, A $30, S $80, times skill (beginner 1, intermediate 1.5,
  pro 2.5, legend 3). So an S shot of a legend = $240.
- S shots go on the Wall of Fame by the kiosk (the last 6).

## Park name (2026-10-21, proposal)
- The first name is free (after Uncle Kai's letter, or once for older saves). Repainting the gate sign later costs $100.
