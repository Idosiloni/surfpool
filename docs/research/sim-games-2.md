# More sim games: running costs, UI, props, progression
Date: 2026-10-20

Sourcing note: web search was thin again. Facts with a link come from a fan guide or Steam thread (numbers change between patches). **(guess)** = my own idea. **(general knowledge)** = I know it from playing/reading but did not fetch a source. This file adds to [sim-games.md](sim-games.md) and [sim-ui.md](sim-ui.md); it does not repeat them.

Games covered: TCG Card Shop Simulator, Supermarket Simulator, Waterpark Simulator, Planet Coaster 2, Two Point Campus, Parkitect, Internet Cafe Simulator 2, Schedule I, Car Wash / Gas Station sims (thin info).

## 1. Running costs: how the games charge you
| Game | What costs money | When you pay | Notes |
|---|---|---|---|
| TCG Card Shop | Rent, electricity, employee salaries | Piles up daily in a **Pay Bills app** on your computer. "Due" for 7 days, "past due" 7 more, then at day 14 the whole amount is taken **even if you can't afford it** ([guide](https://gameplay.tips/guides/tcg-card-shop-simulator-employee-stats-leveling-and-usage-guide.html)) | Electricity is **live**: about $1 per real minute the shop is open at the start, +$0.25/min per shop expansion, plus $4/day per checkout counter ([guide](https://gameplay.tips/guides/tcg-card-shop-simulator-electricity-bills-guide.html)). Turning lights off while restocking after close saves money. |
| TCG Card Shop (player scale) | All of the above | Daily | Players report about $900/day bills in a small shop, $4,400/day with about $5k profit in a big one, and "30-40% margins". Some said early rent felt too high; others said money gets too easy later ([Steam thread](https://steamcommunity.com/app/3070070/discussions/0/6757179594729379718)). Soft-lock when in debt was reported. |
| Supermarket Simulator | Rent, electricity, stock, licenses, store-size unlocks | Electricity only counts while the clock runs (fixed in patch 1.0.4); lights all day was about "$50+" a day for one big store, turning them off saved about $15 ([thread](https://steamcommunity.com/app/2670630/discussions/0/4289188517338282679)). One player saw a rent notice with a late fee ([search result](https://steamcommunity.com/app/2670630/discussions/0/4342103705848090922)) | Cash bottleneck: licenses (~$30k) and the next store size (~$52k) compete for the same money ([search summary of player posts](https://gameplay.tips/guides/supermarket-simulator-product-licenses-guide-with-tips-and-tricks.html)). |
| Two Point Campus | Staff wages, loans (5%/10%/15% per year), room upkeep | Monthly | Loans can be repaid early with no penalty. Guides say keep about $30k in reserve ([guide](https://www.gamerguides.com/two-point-campus/guide/getting-started/basics/how-to-make-money-in-two-point-campus)). Bankruptcy is a common new-player complaint. |
| Planet Coaster 2 water parks | Lifeguards, shops, pool filtration/cleaning | Live, per staff/shop | Pools need filtration; a cleaning tool shows how much capacity is left before maintenance ([preview](https://gamingtrend.com/previews/planet-coaster-2-hands-on-preview-lets-take-a-dip-in-the-pool)). Guests can be charged to change into swimwear. |
| Waterpark Simulator | Staff (lifeguards, janitors, vendors, security), repairs | Live | 1.0 added 4 difficulty modes (Sandbox to Hard) ([press release](https://www.gamedeveloper.com/press-release/from-50m-subscriber-creator-to-1m-downloads-waterpark-simulator-splashes-into-1-0-and-consoles-on-july-31)). Exact bill system: not found. |

### How big is it vs income?
- TCG Card Shop: bills roughly **20-60% of daily revenue** in the player reports above (my rough math from two anecdotes, **guess**). Margins of 30-40% are called healthy.
- Our own rule in BALANCE.md (wages under about 10% of what staff earn) is much gentler than these games. That is fine for a first game.

### Why it does not feel punishing (patterns)
1. **Grace period.** TCG gives 14 days before force-pay. (Soft-lock risk is the downside: our game should never let money go below zero.)
2. **Costs scale with what you built.** Bills rise with expansions, so the player feels "I chose this" (TCG expansions raise electricity).
3. **Player control.** Lights off = save money. Fire a worker. Low wages = worse workers (Two Point Campus).
4. **One clear place to see it.** A Pay Bills app / end-of-day report with each line, not hidden drains.
5. **Cost-cutting upgrades.** I found **no** confirmed cost-reduction upgrade in TCG (the electricity guide says expansions only raise cost). So solar panels / efficient pumps are an **open gap** we can fill (general knowledge: tycoons like Planet Coaster and Two Point do have efficiency research).

## 2. UI and HUD patterns players like
(Mostly general knowledge; checked against the product pages where noted.)
- **Pay Bills / Finance app** as one of the icons on the in-world computer: rows of Rent / Power / Wages with "due in X days" chips (TCG does this).
- **Store level + XP bar** near money. Level unlocks products (Supermarket: level 1 shelf goods, level 3 fridge goods, freezers at level 13, alcohol at levels 30/56/90 per [license guide](https://gameplay.tips/guides/supermarket-simulator-product-licenses-guide-with-tips-and-tricks.html)). Big, loud level-up banner with "New: ..." list.
- **Star rating of the place.** Waterpark Simulator's cap was raised to "Diamond Stars" in 1.0 (press release above). Stars are easy to read on stream.
- **Daily summary**: income lines, cost lines, profit, tips earned, best/worst moment (e.g. "Longest wipeout"). Ends with a big "Start next day" button.
- **Perk picks.** Waterpark Simulator unlocks perks as you play that boost profit or give tools (e.g. a plunger gun), which is a pick-one-of-three style reward.
- **Coverage overlays.** Planet Coaster 2 shows a lifeguard "vision cone" so you see what is covered. A similar "who can see the pool" overlay would help our lifeguard placement.
- **Difficulty selector** (Sandbox / Easy / Normal / Hard) at new game. Cheap to add and helps both kids and tryhards.

Concrete layout idea for our HUD **(guess)**: top-left money + tiny "per minute" rate; top-center day clock; top-right stars + level bar; left side 3-line goal checklist; bottom-center prompt. (Already close to what sim-ui.md suggests.)

## 3. Props on counters and how you use them
| Prop | What it does | Interaction |
|---|---|---|
| Register / checkout counter | Core scan-and-pay station | Scan each item, take cash or card; newer Supermarket-style listings mention scanning barcodes, taking cards and **counting out change** ([store listing](https://store.playstation.com/en-cz/concept/10020001), a different title "My Supermarket Simulator" but same idea) |
| Price tags | Set the price per product | Interact with the shelf tag, then type or click a number |
| Shop computer | Order stock, pay bills, upgrades | Walk up, press E, cursor appears (see sim-ui.md) |
| Scent/spray machines | Keep shop smell/cleanliness (TCG has refillable scent machines, per [guide](https://gameplay.tips/guides/tcg-card-shop-simulator-employee-stats-leveling-and-usage-guide.html)) | Refill chore that employees can also learn |
| Pressure washer | PowerWash core | Hold to spray, progress bar |
| Phone / bell **(general knowledge)** | Call a customer, ring for service | Press to ring |
| Cash drawer **(general knowledge)** | Hold the till money | Open on sale, pops "ka-ching" |

Our equivalents **(guess)**: ticket desk register with a bell, a clipboard price board, a radio for "call a surfer", a whistle for the lifeguard chair, a squeegee/hose for puddles, a wave console (already built).

## 4. Progression features we lack
| Feature | Seen in | Our version **(guess)** |
|---|---|---|
| Unlock by level | Supermarket licenses; levels gate products | Park level unlocks upgrades/boards. Gate by *level + money* so players choose when to buy. |
| Staff skill levels | TCG: each task levels separately to 10 stars; each level cuts action time by 7% of base; "pay boosts" (up to 3 a day) temporarily raise levels and double XP ([guide](https://gameplay.tips/guides/tcg-card-shop-simulator-employee-stats-leveling-and-usage-guide.html)) | Staff gain stars by working. Coffee/"pep talk" button = daily boost for tips. |
| Cleanliness / mess | Waterpark Simulator: janitors, puddles that make guests slip; Planet Coaster 2 pool cleaning; Parkitect handymen clean vomit before guests get upset ([coasterbuzz forum, RCT2-era advice](https://coasterbuzz.com/Forums/PostLink/312859)) | Trash, sand, puddles on deck, algae/filter level on pools. Janitor staff. |
| Breakdowns / repair | Waterpark Simulator (fix broken slides); Parkitect mechanics | Wave machine overheats or a flap jams: mini-game repair, mechanic staff. |
| Random events | Internet Cafe 2 (rainy days bring customers, power cuts need generators, gang threats) ([store text](https://apps.apple.com/us/app/internet-cafe-simulator-2/id6474156844)); Car Wash sim (surprise inspections) | Contest day, shark sighting, cloud front, VIP visit, power outage. |
| Weather / seasons | Waterpark Simulator 1.0: sun, rainbows, tornadoes, night mode | Swell season, rainy week (fewer guests), heat wave (more snack sales). |
| Decorations raise rating | Parkitect scenery feeds park rating ([devlog](https://texelraptor.com/blog/page:19/tag:Parkitect)); Waterpark Sim themed decor | Palm trees, tiki lights, murals give a rating bonus with diminishing returns. |
| Customer types | Schedule I (many customer personalities, hiring dealers/cooks/janitors) ([Wikipedia](https://en.wikipedia.org/wiki/Schedule_I_(video_game))) | Kooks, groms, locals, influencers, instructors with different tipping and patience. |
| Difficulty modes | Waterpark Simulator | Easy / Normal / Hard changes bill size and patience. |

## Ideas for our game (ranked: impact vs effort)
1. **Add a small, visible running cost, shown in the end-of-day report (high impact, low effort).** Wages already exist. Add Pump Power (per minute the wave machine runs, scaled by wave size) and Pool Water/Chemicals. Keep total around 15-25% of revenue early. Never let money go negative; push the rest to a "due tomorrow" line (TCG's grace-period idea).
2. **Cost-cutting upgrades (high, low).** Solar Panels (-X% power), Efficient Pump, Water Recycler. Fills the gap TCG leaves and gives mid-game money a new sink (see incremental-economy.md).
3. **Park level + XP bar with "New unlocks" banner (high, medium).** Gate boards, wave types and decorations by level; show next-level preview.
4. **Mess and cleaning loop (high, medium).** Puddles/trash on deck and pool filter level; a hose/squeegee chore; Janitor staff. Matches the run-down to shiny fantasy and Waterpark Simulator.
5. **Staff stars (medium, medium).** Staff speed rises with use; a daily "pay boost" button as a money sink.
6. **Random event of the day (medium, low-medium).** One per day, shown as a morning card: heat wave, contest, rainy day. Reuses ParkEffects.
7. **Breakdown mini-game on the wave machine (medium, medium).** Risk grows with use; maintenance upgrade or mechanic reduces it. Add a clear warning first so it never feels unfair.
8. **Rating from decoration (medium, low).** Each placed prop adds a small stars bonus, capped.
9. **Difficulty selector (medium, low).** Scales bill size, patience and surfer wallet.
10. **Coverage overlay for lifeguards (low, medium).** Do after lifeguards need positioning.
Warning from the research: bill-based soft-locks and "bills too high early" are real complaints (TCG thread). Test with a new player before shipping any bill.
