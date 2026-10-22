# Park layout, pool shapes, one growing pool, guest flow, luxury tiers
Date: 2026-10-22

Why: Ido said pools should be shaped by level/cost, that separate far-apart pools are too much to handle ("make ONE pool that expands and divides"), that richer = more luxurious, and that the guests' way to check-in and pool feels weird.
Tags: **[S]** = from a linked source, **[K]** = my background knowledge (not checked), **[G]** = my guess/design idea. Company numbers are marketing. Sizes in older notes: [wave-pools-and-ocean-waves.md](wave-pools-and-ocean-waves.md), [surf-park-design.md](surf-park-design.md), [wave-pool-look.md](wave-pool-look.md).
Words: **lineup** = where surfers wait on the water. **Peel** = break point sliding along the wave. **Pier** = the walkway in the middle of a Cove that carries the machines. **Dip pool** = tiny private pool.

## 1. Real parks: outline, machine spot, zones

| Park / tech | Outline from above | Machine sits | Zones | Size |
|---|---|---|---|---|
| **Wavegarden Cove** (Bristol, URBNSURF Melbourne, Lost Shore, Alaia Bay) | Fat diamond / hexagon / "fan" | On a **center pier**, waves run out to both sides, left on one side, right on the other [S: [Raised Water](https://raisedwaterresearch.com/wavegarden-cove/)] | Reef = advanced at the far end; Peak = middle, open face; Bay = beginner whitewater near the beach, 0.5-1.2 m deep. Bay waves are the "leftovers" of the Reef waves [S: [Wave Pool Mag](https://wavepoolmag.com/?p=3107)] | Min ~120x120 m, standard ~150x150 m (2.25 ha); Melbourne ~2 ha; Alaia Bay ~8,500 m2 (46 modules) [S]. Melbourne advanced takeoff holds max 18 surfers per side [S] |
| **Wavegarden Lagoon** (Surf Snowdonia, closed 2023) | Long straight rectangle | Foil on a center pier | Waves peel both ways | ~300 m long, 18-20 s rides [S, see older note] |
| **Kelly Slater Surf Ranch** | Skinny long runway | Foil carriage on a rail down one side | None. One perfect wave, left or right | ~700 m (sources say 610-700; uncertain) x ~150 m [S]; 45 s rides |
| **AWM PerfectSwell** (Waco BSR) | Square-ish pond | Air chambers along **one long side** (~73 m, 24 chambers) | Different wave shapes by firing order; beginners and advanced take turns | ~2 acres [S] |
| **Surf Lakes** | Round-ish lake (~200x150 m, one report) | **Plunger in the middle**, rings roll out to reef arcs at the rim | Several reef peaks around the edge, each with a left and right | Prototype far below the marketing numbers [S] |
| **Endless Surf** | Heart-shaped pool, 6 zones | Row of air chambers | Color-coded: red foamies, green longboards, orange shortboards; expert wave reforms into a mellow inside wave [S: [Wave Pool Mag](https://wavepoolmag.com/how-endless-surf-works-with-engineer-clement-ginestet/)] | Not found |
| **Palm Springs Surf Club** (pneumatic Surf Loch) | Converted old waterpark, 21 acres | Air chambers | "Five slabs of fury" barrels; I found **no** published lagoon size or zone map [S: [search](https://wavepoolmag.com/?p=7348)] | ~3 million gallons [S] |

### Patterns worth copying
- **Skill zones go by distance from the machine.** Hard/steep at the outside (far from the beach), medium in the middle, easy whitewater close to the sand. Beginners ride the *leftovers* of the pro wave. [S] This means one machine feeds three skill groups at once, which is perfect for a "single pool that gets divided".
- **Lanes:** Real parks put a deep **return channel** along the wall so paddlers get back without crossing the break (also in [surf-park-design.md](surf-park-design.md)) [K]. Waiting is on the water (lineup), spread along the peel line, about **1 surfer per 8-10 m** [G, from older note].
- **Skill levels don't mix in the same hour** at Alaia Bay and Beast Mode [S]. Our game can show "zone closed to this level" instead.
- **Most pools are one plain shape** (diamond, rectangle, strip); "kidney" and "teardrop" are not real park outlines I could verify. For game feel a **soft curved edge + sand beach** is [G] a good cheap way to look "designed" and still keep water math simple (see section 2 notes).

## 2. One park that grows

### How real parks phase it [S]
- DSRT Surf (Palm Desert): **Phase 1 = lagoon + surf center + utilities; Phase 2 = 139-key hotel + 57 villas**. Plan shrank from 350 rooms to 139 over time [S: [Surf Park Central](https://surfparkcentral.com/?p=28250), [PMA](https://pmainc.com/project/dsrt-surf)].
- Palm Springs Surf Club: opened Jan 2024 with the pool, cabanas with private dip pools, 3 bars/restaurants; hotel + villas come in phase 2 [S: [Bisnow](https://www.bisnow.com/news/los-angeles/hospitality-entertainment/surf-parks-growth-mixed-use-135604)].
- Cannon Beach (AZ): finish the surf part as a complete product first, add retail/hotel after [S: [KTAR](https://ktar.com/arizona-news/cannon-beach-surf-park-eyes-soft-opening-next-spring-adds-new-tenants-to-retail-lineup)].
- Real lesson: **the water comes first, then lodging/retail**; roads and utilities are laid early so later phases plug in. [S + G]
- Real pools are usually **not** enlarged later (concrete). Our game can cheat; that is fine and fun [G].

### Our design: ONE lagoon, five stages [G]
Keep one fixed "home" (the machine end + beach never move). Each stage re-draws the outline/zones in the *same* place and adds a new area on the shore next to it. All distances are from the beach deck centre, so the player can walk it in under ~30 s (player walk ~4-5 m/s [E]).

| Stage | Name | Outline | Water zones | Rides | Walk: deck to farthest lineup | Maps onto what we have now [G] |
|---|---|---|---|---|---|---|
| 1 | **Puddle** | Small soft kidney (curved beach side), ~40x30 | One beginner whitewater zone, simple paddle machine on the short wall | 5-6 s, 1 direction | ~25 m | Today's main pool stage 1 (40x30) |
| 2 | **Bay** | Wider kidney, ~50x40, a second shallower lobe on one side | Beginner "Bay" next to a new **Intermediate** shoulder on the side | 8 s | ~35 m | Pool Expansion 1 (50x40) |
| 3 | **Cove** | Fan/diamond, ~60x50 body, a **pier** from the machine end splitting water into a right half and a left half | Rights + lefts at once, each with Bay near beach | 10 s | pier end ~45 m (walkable pier) | Pool Expansion 2 (60x50) + the Cove (`pool_cove`) merged into the same basin |
| 4 | **Reef** | Cove outline plus a raised **reef block** at the far end of one half (outer corner) | Adds Reef (advanced/barrel) in the outer area; Peak in the middle; Bay stays | 12-15 s | ~60 m (pier + side jetty) | Reef pool (75x72) becomes *a zone* of the main lagoon, not a separate pool |
| 5 | **Strip** | Cove plus a long lane (~45x200) along one side with a visible carriage | Legend lane parallel to the beach (private/pro) | 30-45 s | lane is viewed from a ~200 m boardwalk; surfers ride in via a short gate, not a long walk | Surf Ranch (`pool_ranch`) attached to the side; Lagoon (75x90) = the wide Peak/Bay area of stages 3-4 |

Rules to keep it manageable (the "too far, too big" complaint):
- **Total footprint cap ~150 x 150 m** for stages 1-4 (real standard Cove size [S]). Only the strip (stage 5) is longer, and it hugs one edge.
- **Everything the player needs sits on one central deck** (check-in, rental, shop, console, snack within ~40 m). The machine console is on the **pier/deck**, never across the water.
- **Zones as flags, not new maps:** one `PoolArea` per half is already built for the Cove [CLAUDE.md]; zones can be *sub-rectangles* of the same water with a different `priceMultiplier`/wave level (Bay x1, Peak x1.6, Reef x2-3; ladder from [older note](wave-pools-and-ocean-waves.md)).
- **Shape trick for cheap code [G]:** keep the water mesh rectangular but hide corners with **beach/sand and rock "skirts"** that curve (rendered meshes, not water). A kidney or fan look then costs art, not wave math. Pools already work "in the water's own frame" [CLAUDE.md].
- **Stage change = visible event:** the old pool is drained for one night; next morning the new shape is there (you can reuse `Celebration`).
- Upgrades that stay stage-independent: wave size, set size, machine upgrades, staff, shops. Only shape/zones/walkways change with stage.

### Stage blueprints (DECIDED by Ido: one pool grows; Reef, Lagoon, Cove become stages) [G]
Frame (matches the water's own frame in CLAUDE.md): **x = from machine toward beach, z = across**. Beach deck is at the x-max end. "Deck" = the one central plaza (desk, lockers, shop, snack, console) that never moves, so the walk to any lineup stays short. Meters are my design numbers, not real-park data.

| Stage | Water outline (x by z) | Machine(s) | Zones | Desk / lockers / paths | Current upgrade it replaces |
|---|---|---|---|---|---|
| **1 Puddle** | Soft kidney, 40 x 30 (beach side curved, sand skirt) | One paddle machine on the x-min wall, centre | Beginner whitewater only; lineup 6 m from the beach | Gate street 60 m long runs straight to the deck; desk 8 m from gate, rental/lockers right after; deck at beach; ~25 m deck to lineup | Main pool 40x30 |
| **2 Bay + Shoulder** | 50 x 40, kidney with a second lobe on the north side (a shallow shelf) | Same machine, now wave starts 3 m from the south wall and peels north | Beach strip = Bay (beginner); north lobe = Intermediate shoulder (8 s) | Return channel along the south wall; entry steps at deck, exit steps 10 m east; ~35 m to farthest lineup | Pool Expansion 1 (50x40) |
| **3 Cove** | 60 x 50 core widened to a fan/diamond ~90 wide at the beach (rounded), **pier** along the middle from machine to x = 40 | Machine moves onto the pier (east `WaveMachine` leads, west follows, as the Cove already does) | Rights on one half, lefts on the other, Bay on both beaches, Peak in the middle | Pier is the player's walkway to the console (3 m wide); two entry ramps from the deck, one per half; ~45 m max | Pool Expansion 2 (60x50) + the separate Cove (pool_cove) merged into the main basin |
| **4 Reef** | Cove grows to ~110 wide x 75 long; raised reef block (hex) at the outer end of one half | Add a second machine bank at the pier end (bigger waves) | Reef = advanced/barrel at the outer end; Peak; Bay | A side jetty from the deck out to the reef lineup (~60 m, walkable, player rides a boat/cart later); rinse showers at jetty root | Reef (75x72) and Lagoon (75x90) become Peak/Reef/Bay zones inside this basin; their upgrades unlock the zone, not a new map |
| **5 Legend lane** | Cove unchanged plus a 45 x ~150 strip along the **east edge**, beach side open to the deck | Carriage on a rail along the strip (the Ranch foil) | One perfect long wave, left or right; private/pro tickets | Boardwalk viewing deck all along; surfers enter from a short pier at the deck (<= 30 m), ride the strip, exit at the far end onto a path back (a golf-cart shuttle later) | Surf Ranch |

Where machines sit, in one line: **S1-S2 on the x-min wall, S3 on the pier (east leads, west follows), S4 adds a second bank at the pier end, S5 adds a carriage on a rail.** Wave size/direction/shape settings stay on the console and apply to every machine (`ApplyWaveUpgrade` already fans out to every pool).

Desk, lockers and paths that never change (so growth never breaks the walk):
- **Gate street** (straight, 4-5 m wide) -> **check-in desk** (queue lane at its side) -> **rental + lockers** -> **shop/snack** -> **deck**. Arrival lane on one side, exit lane on the other (exit passes snack bar and photo wall).
- Each stage only adds **water, ramps and decor on the water side of the deck**. The deck is the fixed hub, ~25-45 m from any lineup in stages 1-4.
- Surfers walk: deck -> entry steps -> paddle to lineup; after the ride they paddle to the **exit steps** (different spot), rinse, then snack.

**Surf Ranch verdict: keep it, as the final stage attached to the pool, but shorter and closer.** Reasons [G]: it is the one truly different wave (45 s ride, private tickets) and a great endgame goal, and real parks do run a single long pool on its own (so it is plausible). But a 200 m strip placed far away breaks Ido's rule, so: attach along one edge, cut it to ~150 m (30-35 s rides, still the longest in the game), make the carriage and the boardwalk the show, and give it a 30 m entry pier from the deck. If time is tight, drop it before dropping the Reef zone: Stages 1-4 already deliver the core idea. Surf Lakes ring pool: skip.

## 3. Guest flow

### What real parks do [S where linked, else K]
Arrive -> park -> **check-in/ticket desk (waiver, level check)** -> **hub building: changing rooms, lockers, rental (board/wetsuit)** -> **walk to the lagoon edge** -> **briefing/lineup on the water** -> ride -> **paddle back along the channel** -> shower -> cafe/shop/deck. Cafe and viewing deck face the water so friends and parents stay and spend [S: [surf-park-design.md](surf-park-design.md)]. Bristol: clubhouse with cafe-bar, rooftop terrace, shop, changing, gardens [S: [Avison Young](https://www.avisonyoung.co.uk/owner-investor/planning-consultancy/case-study/-/article/2021/09/02/the-wave), search results]. Architects want about 23 m (75 ft) next to the beach to feel connected to the action [S: [Wave Pool Mag](https://wavepoolmag.com/how-does-technology-footprint-affect-layout-surf-park-design-insights-from-two-architects/)].

### What feels natural in a game [G]
- **One main street (spine)** from gate to pool, 4-5 m wide, straight, with everything on its sides in this order: Gate -> Check-in desk -> Rental/locker -> Shop -> (T-junction) -> Beach deck -> Water. The order matches the guest's thinking, so the line never doubles back.
- **Walk time budget:** gate to water **<= 40 s** (~150-180 m max at ~4 m/s); desk to water **<= 20 s**.
- **One-way loops:** arrival path (left side) and exit path (right side) so inbound and outbound guests never cross. Exit passes the snack bar and photo wall (the "exit through the gift shop" trick).
- **Queue space:** a marked queue lane 2 m wide at the check-in desk; guests wait in a line, not a blob.
- **Return path from the water:** surfers leave the water at a **different spot** (exit steps at the beach end of the return channel), walk along the deck's edge to the rinse shower, then the snack bar. They never walk back across the lineup path.
- **Show the path:** worn sand-colored or painted floor line + arrows/signs; a decal path is also a free hint for the player (the HUD can draw it).

### Common sim mistakes (and fixes) [G, from sim-genre experience]
| Mistake | Fix |
|---|---|
| Guests path straight across the map, cutting corners through shops/walls | Put **waypoint rails** along streets; use `WalkRouter` with only street nodes; add invisible walls |
| Two streams crossing at the desk | Separate in/out lanes; desk has one front side and one exit gap |
| Walking around a corner for the pool entry | Place the pool entrance **in line** with the street, no 90-degree turn near doors |
| Guests clump at a door | Door width 2+ m, or open gap; stagger spawn times |
| The player cannot tell where to go | Light path + a sign above each building |
| Big park, long walks = boring | Cap the park to the "one central deck" rule; add a golf cart/ shuttle later |
| Queue blocks the main street | Give the queue its own side lane |

## 4. Luxury tiers: cheap pool to resort [G unless linked]
Tie to **Park level** (the `ParkGlowUp`/`GlowItem` `fromLevel`) *and* money spent, so rich = fancy. Real references: Palm Springs Surf Club has cabanas with private dip pools, 3 bars [S: Bisnow link above]; Bristol has landscaped gardens, clubhouse with rooftop terrace and safari-tent glamping [S: Avison Young link]; DSRT Surf has a boutique hotel and villas [S]; Alaia Bay has chalet, dining, retail [S].

| Tier | Feel | Ground / walls | Seating & shade | Lighting | Staff / extras |
|---|---|---|---|---|---|
| 1. **Rough** | Backyard, shabby | Cracked concrete, chain-link fence, plain painted lines, worn paint | Plastic chairs, bare cooler, one beach umbrella | Bare bulb on a pole | Cash box, hand-written sign |
| 2. **Tidy** | Local club | Clean concrete, low wooden fence, painted lines, sand beach | Benches, picnic table, striped umbrellas | Pole lights, string lights | Uniform T-shirt staff, tip jar |
| 3. **Beach club** | Nice surf shop | Wood boardwalk deck, rope rails, palms, planter boxes | Loungers, shade sails, bamboo hut bar | Lanterns, bar neon sign | Rental rack, rinse showers, speaker music |
| 4. **Resort** | Premium | Stone/terrazzo tiles, glass pool fence, tile waterline, planted gardens, water feature | **Cabanas** (some with tiny dip pool), pergola, outdoor lounge sofas | Uplights, **LED lights under the water** (Wavegarden tested this [S: [Surfer Today](https://www.surfertoday.com/surfing/wavegarden-tests-a-night-surfing-formula)]), light columns (Bristol [S]) | Towel service, coach lounge, matching uniforms |
| 5. **Luxury** | Boutique hotel | **Infinity-edge** look on the beach side, marble/white stone, fountains, flower arches, fire pits, rooftop terrace | VIP cabanas, pavilion/clubhouse, rooftop bar, glamping tents (Bristol has safari tents [S]) | Night shows, lit palms, drone/ firework show | VIP lane, butler NPC, hotel and villas on the hill (DSRT [S]) |

Concrete ways to apply: every tier is one extra `GlowItem` set (`fromLevel`) and a **material swap** (concrete -> wood -> stone) already supported by `ParkGlowUp` paint swapping. Lower tiers *disappear* when a better tier covers them (chain-link removed when glass fence appears) so it feels like a glow-up, not clutter. Guest-side: higher tier = higher price multiplier and mood bonus; shabby item near the pool = small mood penalty (cheap to add).

## 5. Ranked recommendations (build in this order)

1. **Fix the walking spine first (Step 3-4, small, no new art).** Straight Gate -> Desk -> Rental -> Deck street with a separate exit path via the snack bar; remove corners/doubling-back. Cheapest fix for Ido's "weird way". Check with `WalkRouter` nodes only on streets.
2. **Cap and centre the park (Step 4).** Rule: everything the player operates is within ~40 m of the central deck; pool footprint max ~150x150 m (real Cove size [S]).
3. **Make Reef and Lagoon zones of the home pool, not remote pools (Step 4+).** Same water, sub-areas with price/wave-level multipliers (Bay 1, Peak 1.6, Reef 2-3). Fixes "too far and too big".
4. **Soft shaped beach edges (Step 4).** Rounded sand/rock/plant skirts around the rectangular water give kidney/fan looks without new wave math. Different silhouette per stage.
5. **Stage ladder with visible stage-up night (Step 4-5).** Stages 1-3 first (kidney -> wider -> Cove pier merged into main basin). Reuse `Celebration`.
6. **Luxury tiers by Park level (Step 4+).** Start with tier 1 -> 3 as new `GlowItem` sets (chain-link/plastic -> boardwalk/palms/cabanas); add 4-5 later. Include "shabby item near pool" mood penalty.
7. **Pool-edge guest spots and lineup flow (Step 3-4).** Entry ramp + exit steps at different places; a return channel strip along one wall, so paddle-back does not cross the break.
8. **Cove pier merged in (Step 5).** Move the machine to the pier and let the main basin become both rights and lefts; pier doubles as player walkway to the console. Mostly layout, since `CoveBuilder` already exists.
9. **Reef block + barrel zone (Step 5-6).** Raised block at the outer corner, only high level surfers allowed (zone sign "ADVANCED ONLY").
10. **Ranch strip attached to the side (later).** Visible carriage lane along one edge of the park; expensive pro tickets, event days. Viewed from a boardwalk.
11. **Surf Lakes ring pool (very late / maybe skip).** Needs radial water math [E]; a single novelty at the end.
12. **Lodging phase (late).** Hotel/villas on a hill behind the beach as a pure money sink, like DSRT's phase 2 [S]; gives income per night and a luxury-endgame look.

## Open questions for Ido
- (Answered by Ido: one growing pool; old pools become stages.) Open: OK to shorten the Ranch strip to ~150 m and attach it along the east edge?
- Should luxury come only from **Park level**, or be **bought** with money on a "Style" panel? (Both options are cheap; level-based is simpler.)

## Ideas for our game
- **Step 3 (money loop):** the guest-flow spine fix + queue lane; price multipliers per zone (Bay/Peak/Reef) as a "wave menu" row.
- **Step 4 (growth):** stage 1 -> 3 of the home pool (Puddle, Bay, Cove), soft sand skirts, "central deck" rule, luxury tiers 1-3 via `GlowItem.fromLevel`.
- **Step 5+ (later):** stage 4-5 (Reef zone, Ranch strip), luxury tiers 4-5, hotel/villa phase.
- **Quick wins any time:** worn-path arrows on the street, a different silhouette per stage, uplights/under-water LEDs for night sessions, "ADVANCED ONLY" zone signs, a stage-up night celebration.
