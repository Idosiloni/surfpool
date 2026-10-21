# Polish, shop depth, surf-only ideas, and what to build next
Date: 2026-10-21

Sourcing note: search was thin again (mostly review summaries, some AI-written). Tags: **[S]** = from a linked source, **[K]** = general game-design knowledge, **[G]** = my guess. Does not repeat sim-games.md, sim-games-2.md, sim-ui.md, shops-and-machines.md, progression.md, fun-surfers.md.

## 1. What makes a sim feel polished in the first 30 minutes

"Juice" = small sound/motion/visual replies to every action, so the game feels alive.

| Detail | Seen in | Cheap version for us |
|---|---|---|
| Instant visible payoff the second you act | PowerWash: you see the clean the moment you spray [S: [Games Asylum](https://www.gamesasylum.com/2022/07/22/powerwash-simulator/), [Geeks Under Grace](https://www.geeksundergrace.com/gaming/review-powerwash-simulator/)] | Planing a board: wood chips + shavings fall, blank visibly thins. Hose/clean: splash decals. |
| Per-part progress meter + "ding" when a part is done | PowerWash [S: same + [Kotaku](https://kotaku.com/powerwash-sim-devs-on-making-cleaning-fun-advanced-cro-1847319502)] | Every shop step shows a small ring that fills, then a ding and a sparkle. |
| Small cash reward + cha-ching per finished piece | PowerWash [S] | Floating "+$12" over the counter, coin sound pitch rises on combos [K]. |
| End-of-job montage / recap | PowerWash [S] | End-of-day "best moment" card: longest ride, best order, biggest tip [G]. |
| Layered difficulty so the same action stays interesting | PowerWash dirt layers [S] | Shop orders with 1 to 4 steps, picky customers (see section 2). |
| Per-item beep, drawer pop, receipt | Supermarket / TCG checkout [S: [wiki](https://tcg-card-shop-sim.upcomer.com/wiki/Checkout_Counter_Functionality)] | Already built in Gear Shop: add pitch rise per item, drawer slide, receipt prints. |
| Tidy-fantasy: items snap to shelves, stack neatly [K] | Supermarket / House Flipper | Ghost preview + soft "thunk" + tiny squash when a box lands. |
| Run-down to shiny before/after [K] | House Flipper 2: "turn a garbage heap into a home" [S: [OpenCritic](https://opencritic.com/game/15968/-/reviews)] | We have the park glow-up. Add "before" photo on the wall that the player can compare. |
| Camera feedback: tiny bob, FOV kick on sprint, head turn toward interactable [K] | most first-person sims | Look-at highlight outline on things you can use (cheap, big clarity win). |
| Customer reaction faces / emotes [K] | TCG, Two Point | Hearts, shaka, thumbs-up, sweat drop above head; match the order bubble. |
| One-line hint instead of a wall of tutorial [K] | all | Already have Kai's Plan; keep the prompt to 1 line. |

### Most common Steam complaints (and our fix)
- **Late-game repetition, grindy chores, little new** (Supermarket and TCG summaries) [S: [Gamepressure Supermarket](https://www.gamepressure.com/games/movie.asp?ID=51559), [Gamepressure TCG](https://www.gamepressure.com/games/tcg-card-shop-simulator/ze6aeb), [OpenCritic TCG](https://opencritic.com/game/21392/view)]. Fix: new twist every ~30 min of play (new shop type, event, mini-game), not just bigger numbers.
- **Management too shallow** (TCG) [S: same Gamepressure]. Fix: price-per-item, demand, reputation per shop (section 2).
- **Lag and crashes as the base grows** (Supermarket) [S: [search summary](https://vaporlens.app/app/2670630/supermarket_simulator.md)]. Fix: cap live NPCs, pool objects, test on Mac with 40 surfers + 3 shops.
- **NPCs stuck in queues / never reaching checkout** (Supermarket thread) [S: [Steam](https://steamcommunity.com/app/2670630/discussions/0/4352240417145840904)]. Fix: a "stuck for 30 s, then walk away unhappy" timeout for every NPC state.
- **Repetitive audio** (TCG, PowerWash) [S: Geeks Under Grace, OpenCritic TCG]. Fix: 3 to 5 variants per sound with random pitch +-5%.
- **Load times, sandbox without goals overwhelming** (House Flipper 2) [S: OpenCritic]. Fix: we already have Kai's Plan; keep it.
- **Cash stalls / soft-locks** (TCG) [S: sim-games-2.md]. Keep: never negative money.

## 2. Shop depth: restocking, upgrades, customers, variety

### Should our shops need restocking?
| For | Against |
|---|---|
| Gives a reason to walk around, makes shelves look "worked" [K] | TCG/Supermarket reviews already call chores grindy [S] |
| Makes staff useful (a "stocker") | Our player is also surfing, running waves, cleaning: time is tight |
| Out-of-stock creates choices | Early TCG: out-of-stock bad reviews annoyed players [S: [Steam](https://steamcommunity.com/app/3070070/discussions/0/4700161008287262340)] |

Recommendation **[G]**: soft restock. Stock is a *bar on the shelf/bin*, not a hard stop.
- Acai: 3 bins (fruit, granola, base). Bin empty = that topping greyed out and the order can still be served "plain" for less tip. Refill = hold E on bin, 2 seconds, one crate carried from a back fridge.
- Gear: shelf shows 0 to 4 items; empty slot shows a price tag and "SOLD OUT" sticker. One crate refills a whole rack.
- Shaper: blanks come in a rack; reorder = one click on the computer, arrives next morning.
- A Stocker staff hire fixes all three (copy TCG: staff arrive after you master the job [S: sim-games-2.md]).
- Rule: restock only needed about once per game-day per shop, and only if you sold a lot. Never block ticket sales.

### Shop upgrades (each should change a visible thing)
| Shop | Upgrade | Visible effect |
|---|---|---|
| Acai | Faster blender (2 levels) | Blender prop spins faster, step takes 1.5 s instead of 3 [G] |
| Acai | Bigger menu: Tropical Bowl, Green Shake, Coconut Cup | New bin + new icon on bubble |
| Acai | Sign + cups with logo | Sign changes, cups branded |
| Gear | More racks (2 to 4) | New rack props appear |
| Gear | Price tags / "set price" per category | Tag panel above rack; sweet-spot rules below |
| Gear | Card reader | Faster ring-up, no change counting |
| Shaper | 2nd shaping bay, dust extractor, CNC "Pro" blank | Second bench, cleaner air (less dust particles) |
| All | Shop level (shared) | Unlocks hire slots |

### Price setting per item (sweet spot) [K, from sim-games.md pattern]
- Each item has a "fair price". Customer buys if price <= fair x patience multiplier (random 0.85 to 1.25 per customer). Price too high = speech bubble "pricey!" and a lost sale; too low = a "deal!" bubble and a sold-out shelf.
- Show a green/yellow/red dot on the tag so kids understand without math.
- Optionally a once-per-day market price shift ("Sunscreen +30% today") so the setting stays alive.

### Customer variety and reactions
Add *order modifiers*, not new models first:
- Picky (allergic: "no granola"), In a hurry (short patience bar), Big spender (asks for premium), Group of 3 (three orders at once), Kid (wants tiny cup, tips a shell), Influencer (photographs the bowl; free marketing if perfect) [G, ties to fun-surfers.md personalities].
- Reactions: delighted (hearts + tip), ok, meh (no tip), angry (leaves, rating -). Always show *why* in the bubble ("too slow", "wrong topping").
- Same customer returns: regulars remember their favorite order and say "the usual?" - serve it with one click for a bonus [G].

### Queues
- Visible line markers on the floor (rope stanchions) so people queue neatly.
- Customer in line shows patience pie; going yellow = tiny foot-tap animation [K].
- A bell/ping when a new customer joins an empty counter (for when you are away).
- Staff takes the second counter; you take over any station on E (copy TCG: take over a register [S: shops-and-machines.md]).

### What keeps selling interesting after an hour **[G/K]**
1. Orders get longer (more steps) as shop level rises.
2. Rush hours tied to waves: a set of good waves = spectators = acai rush after.
3. Daily special on a chalkboard (e.g. "Mango Monday"): serve it, get a 2x tip.
4. Mistakes are funny not punishing (spilled bowl, glitter on the floor) but cost a tip.
5. Combo meter across customers: serve 5 perfect orders in a row = "Rush!" banner with a coin shower.
6. Collection goals: unlock all menu items, "Perfect Bowl" badge.
7. Staff you can train (stars already exist) and pay to specialize.

## 3. Surf-park ideas no other sim has

### What real parks sell beyond sessions
- Operators try to keep surf income at or below 50% of revenue and layer food and drink, retail, lessons, memberships, events, sponsorships; track spend per guest not per session [S: [Surf Park Central](https://surfparkcentral.com/executive-summary-key-operational-lessons-from-surf-park-operators/)].
- Events: sponsor-backed contests, community events, pre-contest training boosts bookings, and non-surf events (music, private parties) can beat surf revenue [S: same].
- Memberships can sell out before opening; members bring guests; tiered memberships sell premium perks [S: same].
- Lessons and progression paths turn beginners into repeat customers [S: same]. Lessons/camps/private rentals fill off-peak hours [S: [FlowSurf brochure](https://www.flowrider.com/wp-content/uploads/2025/03/FlowSurf_Brochure_2025.pdf), vendor marketing].
- Palm Springs Surf Club: lazy river, restaurant, private lessons, cabana rental, event venue (birthdays, corporate) [S: [Meetings Mags](https://meetingsmags.com/california/ca_blogs/palm-springs-surf-club-opens/), [Tripleseat](https://venues.tripleseat.com/venues/palm-springs/palm-springs-surf-club)].
- URBNSURF: lessons, events, corporate groups, group experiences [S: [CB Insights](https://www.cbinsights.com/company/urbnsurf)].
- Alaïa Bay: sessions by level (Malibu to Beast) and a separate restaurant next to the pool [S: [Inertia](https://www.theinertia.com/surf/wavegarden-alaia-bay-opens-in-sion-switzerland-2021/)].
- The Wave Bristol: directional lights planned for evening sessions; 90 min coached session about GBP 55-60 adult; has a "Surf Photos" link (details not found) [S: [Business Magazine](https://thebusinessmagazine.co.uk/lifestyle/surfing-after-dark-to-become-a-reality-in-bristol/), [Wave Pool Mag](https://wavepoolmag.com/complete-guide-to-surfing-the-wave-bristol/)].
- BSR Waco: hosted national contests and Red Bull Foam Wreckers (100+ surfers), USA Surfing training base, sessions often sold out weeks ahead [S: [KLTV](https://www.kltv.com/2021/05/24/surfers-descend-on-waco-for-national-surfing-contest/), [WWA](https://www.thewwa.com/?p=15597)].
- Not found: photo/video revenue numbers, night-session data. Treat those as [G].

### Ideas, mapped to what makes each a *game* (not just a menu item)
| Idea | The mechanic that makes it unique | Fits |
|---|---|---|
| **Photographer** (staff or you): stands on the pier with a camera; you aim a first-person camera at a surfer mid-trick, press click, the shot is graded (framing, trick, board visible) and printed at a **photo kiosk**; surfers buy prints | Real photo mode, graded; best shots hang on a wall of fame | Surfers, shops, UI |
| **Surf lessons**: beginner group on foam boards, you stand in the shallow area and call "paddle! pop up!" with timed prompts; students' skill levels up and they come back as paying regulars | Teaching mini-game with timing; funnel from grom to pro | Surfers, wave control |
| **Contest day**: heat bracket, judges' scorecards read live from `PlayerSurfing`-style scoring (0 to 10), crowd noise, prize money, sponsor banner | Judges score real rides, you can enter yourself | Surfers, events |
| **Night session**: pool lights, glowing foam (emissive), string lights, fire pit, reduced waves but premium price | Light-up shader, lights cost power (OperatingCosts) | World, costs, events |
| **Wave designer**: sliders for size, speed, peel, set length; a preview strip; save presets; surfers of different skill rate the wave afterwards | Direct control of the product, reuse existing presets | Wave machine, upgrades |
| **Weather/wind**: offshore wind = cleaner faces (+rating), onshore = choppy (lower rating), rain = fewer beach guests but cheaper tickets; forecast board at the entrance | Daily forecast to plan around | Daily events, water |
| **Water-level / tide-like control** (pool depth knob) | Deeper = softer waves for beginners, shallower = steeper/barrel; real pools tune this [S: wave-physics.md breaking-depth rule] | Wave machine |
| **Influencer visit**: arrives with a drone-camera guy; if their ride is great, a "viral" boost to arrivals for 2 days; if bad, small dip | Risk/reward event | Events, marketing |
| **Ding repair shop / board repair**: customer hands in a dinged board; you sand, patch with resin, cure under a lamp; fits shaper bench | Fun hands-on mini-game, cheap use of existing tools | Shops |
| **Wetsuit and rash-guard rental**: sized bins (S/M/L), hang them on a drying rack after use; cold-weather days increase demand | Chore with visible state (wet vs dry) [G] | Rentals, weather |
| **Membership card tiers**: Bronze/Silver/Gold perks (priority queue, free wax, cabana) | Park-wide perk; tie to Season Pass we have | Money loop |
| **Cabana/lounge rentals**, birthday-party packages (a group of kids arrives with balloons) | Group customers; cake mini-game [G] | Events, shops |
| **Lifeguard whistle + safety**: stop waves for a wipeout; a rescue throw | Gameplay reason for the lifeguard | Staff |

## Ideas for our game

### Ranked next steps (12 to 15). Size: S = under 1 session, M = 1 session, L = 2 to 3 sessions.
TOP 3 are marked with a star.

| # | Build | Why (source) | Size | Touches |
|---|---|---|---|---|
| 1 * | **Look-at outline + one-line prompt + "ding/ring" on every shop step**: progress ring per step, sparkle on done, floating +$ over counter, sound variants with pitch jitter | PowerWash's per-part meters, ding, cash pop [S: Games Asylum, Geeks Under Grace]; repeated-audio complaint [S] | S | Shops, UI, sound |
| 2 * | **Photographer + photo kiosk**: aim and click, graded shot, surfers buy prints, wall of fame | No other sim has it [G]; Bristol has a Surf Photos service [S: Wave Pool Mag]; a unique mechanic, matches "PC not Roblox" | L | Surfers, shops, UI |
| 3 * | **Surf contest day**: bracket of NPC heats, judge cards, crowd noise, prizes, player can enter; ties to Ideas parking lot | Events and contests drive bookings and non-surf events can beat surf income [S: Surf Park Central]; BSR contests [S: KLTV] | L | Surfers, events, UI |
| 4 | **Per-item price tags with sweet spot + green/yellow/red dot + "pricey!/deal!" bubbles** | Players love sweet spots; TCG "too simple management" complaint [S: Gamepressure] | M | Shops, UI |
| 5 | **Soft restock** (bins/racks with levels, "plain" fallback, 1-crate refill, Stocker hire) | Hands-on loop without grind; staff pattern [S: sim-games-2.md] | M | Shops, staff |
| 6 | **Customer modifiers**: picky, in-a-hurry, big spender, group, regulars "the usual?" | Fix repetition [S: Gamepressure Supermarket/TCG]; Two Point style variety [K] | M | Shops, surfers |
| 7 | **Visible shop upgrades**: blender speed, extra racks, 2nd shaping bay, new menu items, each changes a prop | "Visible props for every upgrade" matches Air Wall work | M | Upgrades, shops |
| 8 | **Stuck-NPC timeout + queue stanchions + patience pie** | Stuck NPC bug in Supermarket [S: Steam thread] | S | Surfers, shops |
| 9 | **Surf lessons area** (parking-lot item): timed "paddle / pop up" prompts, students level up | Lessons turn non-surfers into repeat customers [S: Surf Park Central]; roadmap already lists it | L | Surfers, UI |
| 10 | **Night sessions**: pool lights, glowing foam, power cost, premium price | Bristol lights for evening sessions [S: Business Magazine] | M | World, costs, events |
| 11 | **Forecast board + wind**: offshore/onshore changes wave look and rating, daily card | Weather feature in Waterpark Sim [S: sim-games-2.md]; reuses DailyEvents | M | Daily events, wave, UI |
| 12 | **Board repair (ding) bench** in the Shaper | Cheap hands-on mini-game, reuses planing/spray tools [G] | M | Shops |
| 13 | **Wave designer panel**: size / speed / peel / set length sliders + saved presets | Ideas parking lot; owning your product [G] | L | Wave machine, UI |
| 14 | **Membership tiers** (Bronze/Silver/Gold) with perks | Memberships and tiered premium perks [S: Surf Park Central] | M | Upgrades, money |
| 15 | **Influencer visit event** with viral boost or flop | Event variety; Schedule I-style customer types [S: sim-games-2.md] | S | Events, surfers |

### Why the top 3 **[G]**
- 1 is tiny and makes everything we already built feel better today.
- 2 is the single most unique mechanic: a camera job inside a surf sim, great for streamers, and gives shops a new product to sell.
- 3 is a big spectacle moment (crowd, scorecards, sponsor banner) that gives mid-game a goal beyond bigger numbers.

### Small polish list (do inside any session, all S)
- Random pitch +-5% on repeating sounds; 3 variants for beeps, pops, splashes.
- Squash-and-stretch on items when picked up and dropped.
- Receipt printer, cash drawer slide, coin spill on a big tip.
- Footstep sound by surface (sand, wood, tile, wet deck).
- Customers glance at you when you walk past and wave if they are regulars.
- Hold-to-do actions show a ring around the crosshair, not a bar at the screen edge.
- Add "Best moment" and "Funniest wipeout" to the end-of-day report.
