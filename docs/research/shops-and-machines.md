# Wave machines you can SEE, and counter-shop selling
Date: 2026-10-21

Tags: [FACT] = from a source, [GUESS] = my guess / memory, not checked. Sources are mostly trade press (Wave Pool Mag) and
vendor pages; they rarely describe looks, so many "look" notes are [GUESS] from photos I remember.
Not repeated here: tech basics in `wave-pools.md`, wave shapes in `wave-pool-look.md`, rental chores in `rentals-and-chores.md`.

---
## Part 1. What wave machines look like from outside

### Quick table
| Tech | Visible parts | Moves when it fires | Best "level up" parts |
|---|---|---|---|
| Wavegarden Cove | Long central pier/footbridge with a row of modules; each module holds a lever/piston that pushes a paddle (foil) into the water [FACT: [Wave Pool Mag](https://wavepoolmag.com/how-the-wavegarden-cove-works/)] | Paddles fire one after another from the back of the pool toward the front; lefts on one side of the pier, rights on the other [FACT]. 28 modules (test site) to 56 (Korea) [FACT]; more modules = longer/stronger wave [FACT] | Module count, a visible paddle row, control cabin |
| American Wave Machines PerfectSwell | Chambers hidden in a central wall; BSR uses 24 chambers about 10 ft wide [FACT: [search summary of Wave Pool Mag / RWR](https://wavepoolmag.com/how-the-american-wave-machines-perfectswell-system-works/)]. Blowers (New York Blower fans) fill a closed air chamber with pressure, valves release it [FACT: [NYB case study](https://www.nyb.com/latest-news/2025/09/09/new-case-study-oem-partnerships/)] | Air pushes the water; software ("Phased Array Control System") sets firing order, timing, power; order can be reversed for lefts/rights [FACT]. About 120-160 waves/hour in 3-wave sets, ~1:10-1:30 apart [FACT: Wave Pool Mag]. Most sites run lefts for 30 min, then rights for 30 min [FACT] | Blowers, number of chambers, valves, control screens |
| Endless Surf / Okahina style | Row of pneumatic chambers ("caissons") at the deep end of a heart-shaped lagoon; ES34 has 34, ES48 has 48 [FACT: [Wave Pool Mag](https://wavepoolmag.com/how-the-endless-surf-wave-system-works)] | Air pushed through caissons in sequence; software can change shape per surfer, power drops when few surfers are in [FACT]. 400-700 waves/hour [FACT]. (I found no look/sound details for Okahina itself [GAP]) | Caisson count, "Wave Doctor"/"Swell Studio" screens |
| Surf Lakes | One big central plunger in the middle of a round lake [FACT: [Wave Pool Mag](https://wavepoolmag.com/how-the-surf-lakes-wave-pool-design-works/)] | Plunger is raised, then dropped; swells spread in all directions and break on a ring of reefs. Height and speed of the drop set wave size [FACT]. Sources disagree if it is air or hydraulic driven [GAP] | Plunger size, drop height |
| Kelly Slater Wave Co (Surf Ranch) | A huge submerged foil ("plow") on a vehicle riding a track beside a long pool; cable winches with electric motors at each end pull it [FACT: [Wave Pool Mag](https://wavepoolmag.com/how-the-kelly-slater-wave-pool-works/)] | Vehicle runs 10-18 mph along the pool; goes both ways so it makes a left or a right [FACT]. Foil weight is 100 or 200 tons depending on source [GAP]. One long wave per pass, then it resets | Foil size, winch houses, track |
| Webber Wave Pools | Boat-like hulls hanging from carriages on a steel rail loop around an island [FACT: [Wave Pool Mag](https://wavepoolmag.com/how-the-webber-wave-pool-system-works/)] | Electric motors drive carriages, hydraulics tilt the hulls; angles reversible for lefts/rights. Design target 120-180 waves/hour [FACT, developer claim]. Mostly "not built" [FACT] | Number of hulls (not for us) |

### Sound and "show" (mostly my guess; no source gave sound)
- Air machines (PerfectSwell, Endless Surf): [GUESS] a low blower hum that rises as pressure builds, a big "whump" or hiss at release, a rumble and bubbling boil on the water in front of the chamber, spray at the vents.
- Cove: [GUESS] a row of paddles slapping the water one by one, a "ripple" of sound traveling down the pier; motors and a dull thud.
- Surf Lakes: [GUESS] a huge crash and a circular splash ring.
- Surf Ranch: [GUESS] winch whine and a long water roar as the foil passes.
- Timing that is certain: waves come in sets with a pause between (see PerfectSwell numbers above), so a "charge up, fire, rest" beat is realistic.

### What reads well in low-poly (my analysis)
- Readable at a glance means: a row of repeated big shapes, one moving part, one clear glowing signal.
- Air chambers fit our rectangular pool best: a wall of boxes at the west end is simple to model and simple to upgrade (add more boxes).
- Surf Lakes plunger and Surf Ranch foil need a special pool shape; skip for the main pool (maybe a late special pool).
- Cove paddles need a pier in the middle of the water; our pool is rectangular with the machine at the end, so use the air-chamber look and borrow the Cove idea of "sequential firing" as a light ripple along the row.

### Recommendation: the "Air Wall" machine at the west end
A stylized PerfectSwell/Endless Surf mix. Everything below is art; the real waves stay faked.
- **Wall**: a wide concrete block along the whole west end, about the pool width. Top is a catwalk with a railing.
- **Chambers**: a row of tall rounded boxes (start with 4) facing the water, each with a dark "mouth" slit just above the waterline.
- **Blower house**: a small shed behind the wall with 1-2 big round fans; blue ducts (pipes) run along the top to each chamber.
- **Valves**: one red wheel/valve on each chamber duct (little props that can spin).
- **Control room**: a small glass cabin on one end with a screen showing a wave graph and a green/amber/red lamp.
- **Charge gauge**: a big round pressure dial on the wall, readable from the lineup (needle = recharge progress).
- **Fire animation (about 1.5 s)**: 1) lamp turns amber, fans spin up and pitch rises (charge); 2) dial hits full, lamp green; 3) chambers fire left-to-right in a quick 0.1 s stagger: each shakes (scale squish), puffs white spray from the mouth, water boils; 4) one low "whump" sound, camera shake very small; 5) fans spin down, dial drops to zero (recharge). A set = 3 fires with pauses.

### Visible upgrade stages (9) mapped to our upgrades
| # | Stage (what you see) | Upgrade it fits |
|---|---|---|
| 0 | Old rusty box, 2 chambers, 1 small fan, a hand lever, no screen | start |
| 1 | Bigger pump pipe and a second fan; dial fills faster (needle moves visibly quicker) | Faster recharge pump |
| 2 | Timer clock on the wall plus a green "AUTO" lamp; lever stops moving by itself | Auto wave timer |
| 3 | 2 more chambers (4 total), three little lights on top that blink 1-2-3 per set | Wave sets |
| 4 | Fresh blue paint, blower house gets a second, larger fan; chambers get yellow stripes (intermediate) | Intermediate wave program |
| 5 | Control cabin appears with a screen showing a wave profile; green graph line | Pro wave program |
| 6 | Two sets of chamber mouths: left half and right half have an arrow sign; a flip-switch swaps side | Reef shaper (lefts/rights) |
| 7 | Thicker ducts, steel pump housings, steam/vent stacks with puffs on fire; orange safety paint | Heavy-duty pumps |
| 8 | A "wave poster" panel and little location flags above chambers (Pipeline, Teahupoo...); screen shows a preset picker | Famous waves pack |
| 9 | Big wall screen with a shiny "smart" icon, scoreboard of queued waves, neon edge lights, antenna dish | Smart timer |
Why this order: early stages change things that move (fans, dial), later ones add things that glow or display. Players "see" power before they see software [GUESS].

---
## Part 2. Serving a customer at a counter

### What makes it satisfying (from sims)
- TCG Card Shop checkout: you hover over items to total them, then either run the card on the scanner or count change from the register [FACT: [True Achievements](https://www.trueachievements.com/game/TCG-Card-Shop-Simulator/walkthrough/2), [wiki](https://tcg-card-shop-sim.upcomer.com/wiki/Checkout_Counter_Functionality)].
- Supermarket Simulator: scan the items, take cash and give change, or use the card terminal [FACT: [Gamepressure](https://www.gamepressure.com/games/movie.asp?ID=51559)]. Steam players asked for open/close register controls and a receipt-handover minigame [FACT: [Steam](https://steamcommunity.com/app/2670630/discussions/0/4848778732124091914)]. I found no confirmed tips in that game [GAP].
- Bartending sims split into two camps: timed rush with tips as score vs relaxed no-timer play with story [FACT: [search summary](https://nodal.gg/game/open-bar-1634340)]. Cafe/ice cream sims all sell the "hire baristas/cleaners/servers" step [FACT: [Coffee Shop Simulator page](https://store.steampowered.com/app/2127130)].
- Cooking Simulator and the Fast Food sims: I found no usable review data [GAP].

### Satisfying parts (patterns, partly guess)
| Part | Why it feels good |
|---|---|
| Order bubble over customer's head showing the item icons | You know the goal in 1 second (also good for streamers) |
| Assembling in steps (scoop, pour, top) with a sound per step | Small wins stacked; looks like a mini craft |
| Scan beep per item, running total | Rhythm |
| Cash drawer pop-open + coin/cash sound, change counted | The "cha-ching" payoff |
| Tip jar / bigger tip for fast + correct | Skill gets paid; a clear reason to be good |
| Customer reaction (smile, thumbs up, bad face) | Instant feedback |

### What gets boring
- Counting change every time: players ask to turn the register off or round prices to 25 cents to skip cents [FACT: [Steam threads via search](https://steamcommunity.com/app/3070070/discussions/0/4849904631719164591)].
- Long queues from one till when you also want to restock [FACT, same threads].
- Same action 500 times with no new twist [GUESS].

### How they add staff
- TCG: cashiers get hired at a level; "very fast" ones at level 75 empty the queue; 2nd/3rd registers help at busy hours; you can take over a register from a worker [FACT: Steam threads above].
- Cafes: baristas + cleaners + servers [FACT]. Pattern: staff arrive after you master the job, cost wages, and are slower than you at first [FACT/GUESS, see `rentals-and-chores.md`].

### Real surf-park retail facts
| Item | What I found | Source |
|---|---|---|
| Acai bowl | Surf cafe example: "Whole Shebang" bowl with granola, peanut butter, coconut flakes, banana, strawberries, blueberries, honey drizzle; price listed as $8 in one snapshot and $17.50 in another (disagree). Smoothies $7.50. Another bar's smoothie (avocado, banana, pineapple, chia) $6.99 | [Groundswell Surf Cafe](https://order.toasttab.com/online/groundswell-surf-cafe) [FACT, shaky] |
| Bowl base | Frozen acai blended thick, topped with fruit, granola, nut butter, honey [FACT-ish, the menu above] |
| Custom board | Flow: consult (length, model, fins, color), estimate, 50% deposit (min $500, refundable until shaping starts), shape foam blank, glass, fins, finish; takes weeks to months. Prices: basic $700-1000, mid $1000-1500, high-end $1500+ (low-quality SEO guides) | [Gordon & Smith](https://gordonandsmith.com/blogs/news/custom-orders), [guide](https://node.albumsurf.com/?p=72231) |
| Fins | $20 low / $60 avg / $150 high per set | [price guide, low quality](https://new.risingsunartscentre.org/news/surfboard-price-guide-typical-costs-for-boards-fins-and-accessories-2026.html) |
| Leash | $15-60 (avg about $25) | [Windy](https://windy.app/blog/surf-gear-equipment.html) |
| Wax | $5-10 a bar | same |
| Rash guard | $25-100 | same |
| Boardshorts, sunscreen | No reliable prices found; [GUESS] boardshorts $40-70, sunscreen $12-20 | - |
| Wave pool shops sell | boards, fins, sunscreen, caps, sunglasses, towels (sold better at the counter), merch | [Wave Pool Mag](https://wavepoolmag.com/managing-gear-and-retail-wave-pool/) |

---
## Ideas for our game

### Step 3 / 4: Machine look (art, cheap first)
- Build the Air Wall as 3 pieces: wall + chamber row (add chambers as child objects shown by upgrade level) + blower shed. Show/hide per upgrade the same way `ParkGlowUp` shows `GlowItem`s (level or owned upgrade). [GUESS on wiring]
- Wire the fire animation to the existing wave timer: fans spin up during recharge, dial needle = recharge progress, chamber puffs on the wave spawn. This also gives players a readable "wave coming" cue.
- Use the 9 stages above as the visual list; start with stages 1-3 (pump, timer, sets) since those upgrades come first.

### Step 4+: Three counter shops (each plays differently)
**1. Acai & Shake Bar — fast, rhythm, many small orders**
- Order bubble shows a bowl icon with 3-4 layers (base, fruit, crunch, drizzle). Player clicks 3-4 bins in the matching order; each click = scoop sound; bowl shows layers.
- Time pressure: patience bar; speed bonus tip when served in under ~10 s [GUESS]. Price about 8-14 coins, many customers.
- Cash: customers pay by tapping card (no change) to keep it quick; fruit stock runs low (small restock chore, one crate at a time).
- Staff: a "blender kid" who makes shakes only; you keep the bowls.

**2. Custom Board Shaper — slow, premium, few customers**
- Not a queue. A surfer comes in with a wish card (length, rider skill, color). Player picks options on a screen (3 choices: shape, color, fins), pays a deposit up front.
- Board goes into the shaping bay and takes game-days (e.g. 2-3 days [GUESS]); a progress tag shows on the wall. When ready, the surfer comes back and picks it up for the balance; the price should match the real $700-1500 range, scaled to our economy.
- Reward: the customer then rides that board with a visible custom color, and a better match = a big tip and a reputation boost.
- Staff: a hired shaper speeds the bay; second bay is a later upgrade.

**3. Surf Gear Shop — shelf browsing, scan and bag, upsell**
- Customers walk the shelves and bring 1-4 items (trunks, rash guard, wax, fins) to a register; player scans each (beep), takes cash, and gives change by tapping bills (optional "round price" toggle like TCG players ask for).
- Stock shelves with the same box-carry loop as board rentals; keep the restock loop short (`rentals-and-chores.md`).
- Upsell mini-choice: customer buying a board gets a prompt "add wax?" (+2 coins) — a quick yes/no that rewards attention.
- Staff: a cashier (hire at a level) so you can go restock — copies the TCG pattern.

### Why they feel different
| Shop | Rhythm | Main skill | Money per customer |
|---|---|---|---|
| Acai | quick, constant | memory + speed | low |
| Shaper | slow, planned | choosing + waiting | very high |
| Gear | steady, scanning | counting + restocking | medium |

### Later
- A shared "Shop level" for all three; each unlocks its own staff type.
- Daily event hooks: hot day = acai rush; contest day = custom board orders.
