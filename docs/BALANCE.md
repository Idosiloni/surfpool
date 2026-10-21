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

## Rush hour (2026-10-21, proposal)
- From day 3, 45% of days get one surf-camp bus between 25% and 60% of the open day: 6-9 beginner campers at once.
- Camp leader tips $12 per camper who got a session, doubled if all were paid in within 150 s and nobody gave up
  (8 campers perfect = $192, plus their session prices).

## Sneak-ins and bad customers (2026-10-21, proposal)
- From day 2, one troublemaker every 2.5-5 minutes while the park is open (one at a time). Kicking one out: +20 XP.
  From day 3: 30% wave hogs (pay, want 6 waves, drop in on everyone: each victim's mood takes a 0.2 hit),
  25% litterbugs (pay, drop trash on the deck or junk in the pool every 6-11 s), the rest sneak-ins.
  With the gear shop open, 20% are shoplifters instead: stop one for +20 XP, or lose $35 if they get away.
  Missing one costs nothing directly: they take waves and a lineup spot from paying surfers, then brag.

## Kook rescue (2026-10-21, proposal)
- 12% of beginner wipeouts turn into "HELP!" (one at a time, at least 60 s apart). 25 s to throw a ring.
- Saved: +25 XP, they finish happy-ish and write a 4-5 star review. Not saved: they leave angry (counts as lost),
  a 0-star mark on the rating, and a 1-star review. A hired lifeguard throws after 6 s.

## Surf moves (2026-10-21, proposals)
- Whitewater: no tricks; "RODE IT IN" +80 at the end of a ride. Carving speed x0.55, flow capped at 0.3.
- Green (Intermediate): CARVE 50, BOTTOM TURN 120, TOP TURN 70 (flow < 0.4), SNAP 130 (flow 0.4+), TAIL SLIDE 140
  (SHIFT at the lip 0.35-1.3 s, longer = wipeout), CUTBACK 140
  (out on the shoulder, flow 0.45+), FLOATER 160, PARTY WAVE 100. Pumping: +0.07 flow per up-and-down.
- Pro: AIR 150; spins 180 = 330, 360 = 510, 540 = 690 (150 + degrees); grabs 120 + 100/s held. Land within 55 degrees
  of a half turn or wipe out. Airs last 0.8-1.35 s and go 1.1-2.1 m high with flow.
- Variety: each repeat of the same move in one ride is worth x0.65 of the last.
- Drop-in on an NPC: their mood drops, a 0.4 mark on the rating, no party wave.

## Snapped boards (2026-10-21, proposals)
- Chance per wipeout: whitewater 0, Intermediate 4%, Pro 10%, +12% if it came off an air. Legends never snap.
- An NPC with their own board buys a new one at your gear shop if it's open: $60 / $110 / $180 by skill.
  A snapped rental costs the park $30. Your own board: a spare for $40, and you keep surfing.

## Dirty pools (2026-10-21, proposals)
- Each open pool gets 0.5 pieces a minute plus 0.3 per surfer in it (litter events and perks scale it), up to 14.
  Each piece lowers that pool's surfers' happiness: up to -0.25 at 12+ pieces. 4% of pieces are rubber ducks (+10 XP).
- Pool Robot upgrade: $1,200, level 5. One robot per open pool, 0.7 m/s: keeps up on quiet days, not in a rush.

## Main pool wave speed (2026-10-21, proposal)
- Main pool waves travel at 75% speed (travel 0.75): a green-wave ride went from about 3.5 s to 5.3 s in the
  40 m starter pool, enough to link two or three moves. Reef (0.7) and Lagoon (0.65) were already slower.

## Judges' score (2026-10-21, proposal)
- Every finished ride (not wipeouts) gets a 0-10 score from 3 "judges" (+-0.25 apart), like a real heat:
  speed 1.5, power 2 (snap/cutback/turns/floater/airs points, full at 550), flow 1.5 (average flow, full at 0.7),
  variety 1.5 (5 different moves = full), combos 1.5 (x3 = full), commitment 2 (tube time, spins, perfect take-off).
- Ceiling by wave: whitewater 4.5 (+1.5 for riding it in), Intermediate 8, Pro 10. Best score is saved and shown
  in the pause menu stats.

## Wide main pool (2026-10-21)
- Main pool stages are now 40x30, 50x40, 60x50 m (was 40x14, 50x19, 60x24). Waves travel at 0.6x and peel at 1.9x
  (green: ~4.2 m/s toward the beach, ~4.9 m/s sideways) from 3 m off a side wall. Full size: an 11 s ride, 41 m
  sideways (measured). The daily water bill is unchanged at full size (it scales by the share of the full area).
- Replaces the earlier "main pool waves travel 0.75" note.
- Down the line: in the pocket (0-8 m in front of the break) flow +0.05/s; 2+ m behind it (whitewater) flow -0.2/s
  with a nudge. Rides with 20+ m sideways on green waves get DOWN THE LINE: 6 points per meter (41 m = +245).
- Reef Pool is now 75x72 (travel 0.5, peel 2.0: ~13 s rides, ~69 m sideways) and the Big Wave Lagoon 75x90
  (travel 0.5, peel 2.25: ~11 s, ~84 m sideways; its pocket moves ~7 m/s, so you need speed to stay in it).

## Wave shape and sets (2026-10-21, proposals)
- Mellow: height x0.85, face x1.35 wider, peel x0.85, never barrels. NPC wipeouts x0.6; learners' ride quality x1.1,
  pros x0.9 ("bit soft"). Hollow (needs Reef Shaper): height x1.12, face x0.75, barrels on green waves too. Pros' ride
  quality x1.15; everyone else wipes out x1.5. Power cost is the same for every shape (could charge more for hollow).
- Alternate L/R sends lefts and rights in turn. Set size can be chosen from 1 up to what Wave Sets allows.

## Sets and lulls (2026-10-22, proposals)
- A press charges 1.4 s ("SET INCOMING", the lineup calls "Outside!" and looks out, +10% patience), then the waves
  leave 4 s apart (was 2.5 s; real PerfectSwell sets: 5 s apart, ~30 s lull). Recharge = cooldown + 1.4 s + spacing.
- Leftover chop: every wave that hits the beach adds 0.22 chop (max 1), which halves every 8 s. Ride quality and your
  flow build x(1 - 0.2 x chop): back-to-back sets on a choppy pool score up to 20% less. A set of 3 leaves ~0.6 chop,
  glassy again (< 0.15) after ~16 s of lull. Ripples grow up to x3.5 when fully choppy.

## Wind (2026-10-21, proposal)
- From day 2: 50% calm, 25% offshore, 25% onshore (independent of the day's event). Offshore: glassy water (ripples
  x0.25), spray blowing off the lips, ride quality x1.1, wipeouts x0.85, your flow builds x1.1 faster. Onshore:
  chop x3.5, ride quality x0.85, wipeouts x1.25, flow builds x0.85.

## Your quiver (2026-10-21, proposals; since 2026-10-22 you start with the shortboard + foamie, and buy the fish
## $900 and longboard $1,400 at the boards standing in front of the Surf Gear Shop, L10)
- Shortboard: speed x1 (x1.05 on Pro waves), all moves. Fish: x1.12 on small/green waves (x0.95 on Pro), pumping adds
  up to 0.5 flow (shortboard 0.35). Foamie: x0.75, only carves/bottom turns, never pearls, PERFECT take-off window x1.6.
  Longboard: x0.9 (x0.75 on Pro), no airs/snaps/tail slides, never pearls, take-off window x1.3, HANG FIVE 100 after
  1.5 s at the nose, HANG TEN 200 after 3 s (not on Pro waves). Could later be sold at the gear shop instead.

## The Cove (2026-10-22, proposals)
- Upgrade `pool_cove`: L22, $150k, needs the Lagoon. A pier of 16 wave modules down the middle of a 2 x 50 x 80 m
  lagoon east of the main pool: every set makes a right (east side) and a left (west side) at once, from one console.
- Each side: travel 0.45, peel 2.0, size 1.1, sessions x4 the base price, daily water 70 each. +6 surfers (two
  lineups). Power is paid per side (a set costs twice). ~10 s rides, ~45 m down the line.

## The Surf Ranch (2026-10-22, proposals)
- Upgrade `pool_ranch`: L25, $300k, needs the Cove. A 45 x 200 m strip north of the Cove; a foil on a rail makes one
  right per pass. Travel 0.2, peel 2.6 (~7 m/s down the line), breaks right by the rail (breakAt x0.4, 5 s head
  start). Measured: an Intermediate wave breaks ~3 s after it leaves and gives a ~24 s ride over ~170 m.
- One wave at a time (no sets), at least 20 s between waves whatever the Turbo upgrades. Sessions x10, daily water
  200, +3 surfers. Everyone waits at the takeoff end; after a ride they paddle ~170 m back (~1 min): a return lane or
  a jet-ski tow back would be a nice upgrade later.
- Open question: ride scores on 24 s rides are much higher than on 10 s rides (points pile up). Cap or scale?

