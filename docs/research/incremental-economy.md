# Incremental economy for Surf Pool Sim
Date: 2026-10-20

Goal: pacing and concrete money curves for a 6-minute in-game day, ~15 missions in 4 chapters, a ~$20 starting session ticket, pools at $3k (Reef) and $12k (Lagoon), and a much more expensive endgame pool. Numbers marked (guess) are my own tuning proposals, not measured from other games.

## 1. How other games do it (short)
| Game type | Money pattern | Takeaway |
|---|---|---|
| Supermarket Simulator / TCG Card Shop Simulator | Small margin per sale early, buy stock, unlock shelves/licenses/space with growing price tags, then hire staff. See [sim-games.md](sim-games.md). Early TCG reviews complained the economy was shallow / stalled. | Always keep something affordable to buy; money piling up with nothing to buy is the #1 complaint. |
| Idle/incremental games (Cookie Clicker-style) | Upgrade cost grows geometrically; income grows linearly per unit. Cost multiplier per purchase typically about 1.07-1.15 (guess within the range discussed in [The Math of Idle Games, Kongregate](https://blog.kongregate.com/the-math-of-idle-games-part-i/)). New tiers cost ~10-20x the previous but earn far more. | Two layers: small repeating upgrades (ratio ~1.15) + big tier unlocks (x4 to x10). |
| RollerCoaster Tycoon / Planet Coaster | Park rating + guest count drive income; big rides are expensive but payback in a few in-game months; staff wages are a constant drain; loans accelerate growth. See [genre overview](https://en.wikipedia.org/wiki/RollerCoaster_Tycoon_(video_game)). | Visible guest crowd = visible progress; running costs create tension. |
| Idle tycoon mobiles | Prestige/reset layers, offline income, boosts. | We do not need prestige; skip. Offline income not needed for a single-player PC game. |

## 2. Pacing rules of thumb
- **First purchase in 60-120 s of play.** Make it cheap (about 3-5 tickets worth) and obvious. (Matches the "within 1-2 min" target; guess.)
- **First 15 min: a purchase every 1-3 min.** Then every 3-6 min later in a chapter. Never longer than ~8 min without something to buy or a goal to chase (guess).
- **Always show 2-3 goals at once:** one cheap (30 s away), one medium (2-4 min), one big (the next pool). The mission list does the medium one.
- **Payback time of each purchase: 1-3 days of play early (6-18 min), up to 3-5 days late.** Shorter than that feels free, longer feels pointless (guess).
- **Tier unlock cost = about 3-5 days of current daily income** (6 min x 3-5 = 18-30 min of play at the current rate) so each tier feels earned but reachable.
- **Late unlock cost can climb to 5-10 days of income** because the player has many ways to spend and big numbers feel good (guess). Do not go past ~10 or it feels like grind.
- **Upgrade repeat cost:** `cost(n) = base * 1.15^n` for small upgrades (wave quality, queue size, marketing). Each level +10-20 % of the relevant income (so each buy is worth it).
- **Income per tier:** each new pool earns about 3-4x the previous one at the point you buy it, and the total economy about 2-3x each chapter.
- **Staff wage vs income:** one staff member should replace a chore worth ~20-30 min of play per day *and* cost 8-15 % of the extra income they enable. Total wages should stay below ~35 % of gross so net always rises (guess; RCT-style wages are a notable drag, but we want "number go up").
- **Avoid dead time:** while the player waits for money, give them chores (rack boards, queue), a mission, a choice (ticket price), or a visible event (surfer arrival, trick). If a gap is unavoidable, a fast-forward (2x/3x speed) key is a standard fix in sims (guess).
- **Number go up:** show a +$ pop on every sale, a rolling money counter, daily profit with a "best day" record, and use k/M formatting.

## 3. Number formatting
| Value | Show as |
|---|---|
| < 1,000 | `$840` |
| 1,000-9,999 | `$3,420` (full, with comma; feels richer than `3.4k`) |
| 10k-999k | `$48.2k` (one decimal, drop `.0`) |
| 1M-999M | `$1.24M` (two decimals) |
| 1B+ | `$1.2B` |
Rules: round down for cost, round to nearest for income (so "can I afford it" never lies). Animate the counter (0.3 s count-up), flash green on gain / red on spend. Use a stable-width font or tabular numbers so the HUD does not jitter. Show `+$` rate, e.g. `+$42/min`.

## 4. Target curves (6-minute days, ~40 days total, ~4 hours) (all guesses to tune)
Assumptions: ticket $20 at start; first surfer arrives every ~25 s; ~15 sessions/day at start, so about $300 gross on Day 1. Income/day below is gross (before wages).

| Chapter | Days | Missions | Income/day | Cumulative earned | Key purchases (price) | Pace |
|---|---|---|---|---|---|---|
| 1. First waves | 1-3 | 1-4 | $300 -> $800 | ~$1.5k | Wave upgrade L1 ($60), ticket desk upgrade ($120), board rack ($200), 1st cheap staff ($400) | buy every 1-3 min |
| 2. Growing crowd | 4-10 | 5-8 | $800 -> $3k | ~$12k | **Reef Pool $3k (day ~5)**, rentals, marketing ($300-1.2k, ratio 1.2), Wave Operator ($1.5k) | buy every 2-4 min |
| 3. Big time | 11-24 | 9-12 | $3k -> $15k | ~$120k | **Big Wave Lagoon $12k (day ~11)** (about 4 days of income at the time), Surf Coach, Surf Shop, VIP, Season Passes ($5k-40k) | every 3-5 min |
| 4. Surf resort | 25-40 | 13-15 | $15k -> $100k | ~$900k | Staff automation ($20k-80k), pool-wide wave upgrades ($50k-250k), **endgame pool $400k-600k** (~5-6 days of income at $80-100k/day), cosmetics | every 4-6 min, bigger ones every 15-25 min |

Notes:
- The $3k and $12k price tags already in the game fit: Reef Pool at ~4 days of income, Lagoon at ~4 days. Check against what income the game actually produces (guess; measure with the end-of-day report).
- Each tier should visibly out-earn the previous: Main ~$300-800/day, Reef ~$1.5-3k, Lagoon ~$5-15k, endgame pool ~$50-100k/day once upgraded.
- If income jumps 100x from start to endgame, ticket price alone cannot do it. Sources: higher ticket tier (Intermediate/Pro $40-150), VIP, lessons, snack bar, season passes, more simultaneous surfers per pool (bigger pools), more pools, tips.
- Cost multipliers to use: repeating upgrades x1.15 per level (x1.25 for strong ones); tiered upgrade sets x3-x5 between tiers; pool unlocks x4 (Main -> Reef: 3k), x4 (Reef -> Lagoon: 12k), x35-50 (Lagoon -> endgame: ~400-600k). That last jump is large on purpose; chapter 4 gives the income to reach it (guess).

### Example upgrade ladder (one line, repeatable "Wave Quality")
Level 1 $60, 2 $70, 3 $80, 4 $92, 5 $106, 6 $122, 7 $140, 8 $161, 9 $185, 10 $213 (x1.15). Past level 10, require a unlock or switch to the next set at x3 base.

### Staff example (wages per in-game day)
| Staff | Hire cost | Wage/day | Enabled gain | Wage as % of gain |
|---|---|---|---|---|
| Desk clerk (frees the ticket chore) | $400 | $20 | +$150-250/day (desk never idle) | ~10 % |
| Wave Operator | $1.5k | $60 | wave machine always running, +$400-800/day | ~10 % |
| Surf Coach | $5k | $150 | lessons +$1.5-2.5k/day | ~7 % |
| Shop staff | $12k | $400 | retail +$3-5k/day | ~10 % |
| Pool manager (automation) | $60k | $1.5k | runs a pool with no player input | ~8-10 % of that pool |
All values are placeholders (guess); the point is hire cost = 3-5 days of the gain, wage = ~10 % of gain.

### Late-game "money sinks" (so cash never piles up)
- Cosmetic decoration packs ($5k-100k) that raise park rating a bit.
- Prestige-like "Season" goals (win a competition, hit 5-star rating) with a fixed cash entry fee.
- Maintenance/repairs at a % of income (a gentle drain to give meaning to saving).
- Big vanity buy (statue, lighthouse, sponsored banner).

## 5. Telemetry to add (so Ido can tune)
- End-of-day report already exists: also log gross, wages, net, "money at day end", and "cheapest thing I could afford".
- Playtest rule: if the player can afford nothing for >4 min, lower the next cost; if they have >2 days of income unspent, add a sink or raise cost (guess).

## Ideas for our game
1. **Day-1 hook:** first upgrade ("Wave +1" or "Better ticket desk") costs $60 and is affordable in ~90 s; tie it to mission 1.
2. **Two-layer prices:** repeat upgrades at x1.15 per level, tier unlocks at x3-x5; pools at $3k -> $12k -> ~$500k.
3. **Set the pool price to about 4 days of the income you have then** (Reef at ~day 5, Lagoon at ~day 11, endgame at ~day 35); read it off the end-of-day report.
4. **Staff rule:** wage ~10 % of the extra income they unlock; hire cost 3-5 days of that gain; cap total wages at ~35 % of gross.
5. **Add a "goal strip" to the HUD** with next cheap goal, next mission, and next pool, each with a progress bar toward the money needed.
6. **Number formatting util** (`$3,420`, `$48.2k`, `$1.24M`) with count-up animation and green/red flash; show a `+$/min` rate.
7. **Add income multipliers over time** so numbers feel big: VIP x2-x5 tickets, season passes, Pro waves x3, tips by trick; this makes late numbers reach $100k/day without silly ticket prices.
8. **2x/3x speed key** for waiting phases late in the game, and offer chores/choices during any wait.
9. **Late money sinks:** decoration packs, competitions with entry fees, maintenance; so late cash is never stuck.
10. **Tuning loop:** run a scripted agent playtest that fast-forwards days with a simple bot and prints income/day and time-to-next-purchase vs the table in section 4.
