# Surf Pool Sim — Design

## Pitch
You own a run-down surf wave pool. Walk around in first person, sell surf sessions, run the
wave machine, keep surfers happy, and grow it into the best surf park around.

## Core loop
Surfers arrive → buy a session at the desk → wait their turn → ride waves → leave happy (or not)
→ you earn money and reputation → spend on upgrades → more and better surfers arrive.

## Pillars (from Ido, 2026-10-20)
- **Incremental tycoon feel:** start tiny and hands-on (one small pool, you do every job), then scale up —
  more and bigger pools, staff and automation running everything, and money growing to big numbers.
  Each new pool/tier should earn clearly more than the last, and automation should free you to build more.
- **Premium beach-surf vibe UI:** warm sand/sunset colors, ocean teal, a beachy display font, polished cards —
  feels like a high-end surf resort, not a spreadsheet.

## Decisions so far
- View: first person
- Engine: Unity 6 (URP), single-player
- Art style: **low-poly cartoon** (like TCG Card Shop Simulator). Custom models built in Blender (via Blender MCP)
  plus free CC0 packs (Kenney, Quaternius). Gray placeholders until the "Better world" step.
- The wave is faked (animated mesh + shader), not simulated water.

## Roadmap (each step ends playable)
- [x] **Step 1 — Wave:** first-person pool, wave console, a wave rolls.
- [x] **Step 2 — First surfer:** NPC pays at the desk, rides, leaves.
- [x] **Step 3 — Money loop:** ticket price, patience, 3-wave sessions, tips, upgrade computer.
- [x] **Step 4 — Grow:**
  - [x] Board rentals (rack → renter, return bin → rack chore)
  - [x] More waves: Beginner / Intermediate / Pro, skills, tricks, wipeouts; start with beginners only
  - [x] More upgrades: tabbed upgrade tree, staff with wages, park rating, marketing
  - [x] Better UI: UI Toolkit HUD, speech bubbles, SurfOS, Wave Control panel
  - [x] Better world: Blender low-poly models, stylized water + foam, peeling breaks, barrels, lefts/rights, sky, post
  - [x] Premium UI restyle (Fredoka, cream/teal/coral)
  - [x] Story + missions + tutorial (Uncle Kai's letter, 15 missions in 4 chapters, marker, banner)
  - [x] Multiple pools: Main, Reef Pool ($3k), Big Wave Lagoon ($12k)
  - [x] Characters: low-poly people with code animation (walk, paddle, surf stance, sit)
  - [x] Player surfing (peg by every console)
  - [x] Day cycle + end-of-day report, save/load, pause menu
  - [x] More staff + income: Wave Operator, Surf Coach, Surf Shop, VIP Sessions, Season Passes
- [ ] **Step 5 — Polish & depth (next ideas):**
  - [x] Sound: waves, crowd, music, UI clicks (synthesized by `tools/audio/make_sounds.py`)
  - [x] Balance pass on prices, wages and pool costs (incremental curve to big numbers, see `docs/BALANCE.md`)
  - [x] Map-changing upgrades: main pool grows (Starter -> Expanded -> Full-Size), Tiki Garden, Grandstand
  - [x] NPC polish: board carried beside the arm, crawl-stroke paddling, surfers keep their distance
  - [x] Running costs (power per wave, water, tax) + Solar Panels / Water Recycling
  - [x] Real models for the register, computer, wave console, price stand; foam / fun / short boards by skill
  - [x] HUD: money counts up, +$/min, next-goal bar
  - [x] Park level + XP, mess & janitor, staff stars, random daily events, machine breakdowns
  - [x] Snack bar (line, grill, Snack Cook, Smoothie Bar)
  - [x] Build mode (place decorations, park beauty), beach guests (Beach Passes), 12 hidden treasures
  - [x] Fishing pier minigame, parking lot + bridge with cars, daily challenges
  - [ ] Lessons area, celebrity surfer visits, surf contests
  - [x] Famous wave presets (Waikiki, Malibu, Snapper Rocks, Pipeline, Teahupo'o in the Lagoon)
  - [ ] Characters: real rigs/animations (Quaternius / Mixamo, needs download OK)
  - [x] Main menu, settings (mouse sensitivity, volume, quality, fullscreen)
  - [x] Riding feel: smooth pose blending, real pop-up timing, board lies on the wave face and leans into turns, rail spray, distinct snaps/cutbacks/floaters/airs, head-covering wipeouts, forward crawl paddling, sitting up on boards in the lineup; first-person camera lean/FOV/pop-up rise; no dropping in or overlapping riders (see `docs/research/surfing-motion.md`)
  - [x] New people (knees, elbows, faces, 6 hair styles) and detailed boards; water look v2 (sunlit faces, shallow aqua, lingering lace foam, soft spray puffs); surfing game feel: pop-up grades, flow meter, carve/bottom-turn/air combos, speed shake + wind, wipeout hit, lens drops, score card (see `docs/research/surf-feel.md`, `wave-pool-look.md`, `wave-rendering.md`)
  - [x] Guided leveling ("Kai's Plan", see `docs/research/progression.md`): level preview under the XP bar and in SurfOS, goal strip follows the mission (or the level it needs), PLAN / NEW stickers on upgrade cards, pick-1-of-3 park perks every 2 levels; player tube rides; surfers stand/lie in the right spots on the board
  - [x] Wide pools, rides go left/right down the line (Ido, 2026-10-21; `docs/research/wave-pools-and-ocean-waves.md`): main pool 40x30 -> 50x40 -> 60x50 m, Reef 75x72, Lagoon 75x90; slow waves that peel fast from a side wall (main ~11 s / 41 m sideways, Reef ~13 s, Lagoon ~11 s); DOWN THE LINE bonus, pocket keeps flow, whitewater drains it. Next: wave sections, a Cove-style or Surf-Ranch-style pool (asked Ido)
  - [x] Fun surfers I (see `docs/research/fun-surfers.md`): 7 surf personalities with their own lines, celebrations at the end of good rides (claim, shaka, fist pump, soul arch), lineup fidgets, hoots for others' waves, kook drop-ins with yelling and a whistle, heads turning to big moments. Next: funny wipeout variants, accessories/outfits, bubble polish, party waves
  - [x] Ambience + growth: the park starts run-down (grimy paint, weeds, junk, a crooked hand-painted sign) and glows up by park level (fresh paint at 3, flags, then bunting/flower boxes/PA speakers at 5, string lights at 8, the KAI'S SURF PARK arch at 11, flag towers at 15); a rail crowd that grows each level, circling gulls, fishing boat, sailboats, jet skis, a blimp (14) and a news helicopter (18); fireworks + confetti + crowd cheer + fanfare + big banner on level-ups and new/bigger pools; wind, a crowd that gets louder and busier, music out of the park speakers (upbeat songs from level 8) and PA announcements
  - [x] UI restyle with a strong beach-surf art direction, "Sunset Surf Shop": Lilita One + Fredoka + Pacifico, ink-outlined chunky cards with dark sign strips, colored icon tiles, tilted price stickers + OWNED stamps, tab badges, key-cap buttons, toasts with kinds, pop-in windows over a dusk backdrop, retro sunset-poster main menu (see `docs/research/ui-powerwash.md`)
  - [x] Counter shops (see `docs/research/shops-and-machines.md`): Acai & Shake Shack (assemble bowls/shakes from topping tubs, hold to blend, speed tips), Surf Gear Shop (the old Surf Shop: grab gear from racks into a basket, ring it up item by item, upsell extras), Custom Board Shaper (deposit, foam blank, template, hold to plane, spray, fins, hand over). Shoppers line up with order bubbles + patience, an order ticket shows near the shop, each shop has a hireable worker. Polish: floating +$ over the counter on each sale, a hold ring at the crosshair (blend, shape, repair), customer moods (in a hurry / big tipper), rope lines at each shop. Shop upgrades you can see: Turbo Blender, Card Reader, Power Planer
  - [x] See the machine: the closed machine house is now an open "Air Wall" (air chambers along the pool wall, blowers, duct manifold, pressure dial, status lamp); chambers fire in the peel direction, blowers spin up while recharging; every wave upgrade adds a visible part. Every upgrade that only changed a number now has a prop (billboard, selfie wall, VIP lounge, season-pass turnstiles, ...)
  - [x] 2026-10-21 big day (all pushed; details in `docs/BALANCE.md` and the CLAUDE.md systems map):
    - People with character (chunky heads, faces with moods, 11 hair styles, outfits with prints), babble voices, 7 invented legends from level 15, closing time (8 PM, guests go home, flip the park sign), instant replay (R), surf photographer (photo tower, graded shots, prints, Wall of Fame)
    - Name your park, surfer names + written reviews in the day report, rush hour (surf camp bus), bad customers (sneak-ins, wave hogs, litterbugs, shoplifters; whistle from far), kook rescue (ring buoys, lifeguard)
    - Surfing 2.0: chase cam (V, default), moves follow real surfing by wave size (whitewater = ride it in; green = carves, top turns/snaps, cutbacks, floaters, tail slides, pumping; Pro = airs with spins and grabs), variety scoring, pearling, party waves/drop-ins, judges' 0-10 score per ride, NPC pumping/tail slides/air reverses, surf daily challenges
    - Boards snap, pools get dirty (skimmer net, Pool Robot), daily wind (offshore/onshore), wave shape (Mellow/Standard/Hollow) + Alternate L/R + set size, your quiver (shortboard / foamie / longboard with hang ten / fish)
  - [x] 2026-10-22 (all pushed): sets and lulls (SET INCOMING call with a pip per wave, the lineup shouts "Outside!", leftover chop settles in the lull, choppy water = worse rides); boards sold at the gear shop (fish $900, longboard $1,400); the Cove (pier of wave paddles, rights on one side and lefts on the other, L22 $150k) and the Surf Ranch (foil on a rail, one 24 s wave down a 200 m strip, L25 $300k), pools can now face any way; UI polish from `docs/research/game-ui-craft.md` (toast queue, readable text, focus/hover, stamp slam, window close animation, day report reveal, wax-sticker trick call-outs)
  - [x] 2026-10-22 waves (Ido: "too much whitewater, a blob moves"): breaking by age (foam ball -> bore -> soup), beginner spills with a green face, reform near the beach, face gradient dark->aqua->lip, barrels (thick lip, dark tube, landing splash, spit, chase-cam see-through), XL Big/Giant waves (Lagoon/Ranch), launch boil, steepening swell, feathering, board wakes, sections (takeoff/wall/barrel/closeout), slowing near the beach, ripple normals, face streaks, golden-hour glow, glints, backwash. Research: docs/research/wave-breaking.md, wave-look-2.md
  - [x] 2026-10-22 wave polish 2: spilling foam runs down the face in lines, each section sounds different (the wall whooshes up, the barrel roars hollow, the closeout booms), Big/Giant waves boom and shake the camera (`CameraShake`, Camera shake setting), Golden Hour day event (the sun hangs low and warm all afternoon: glowing waves, more surfers, +25% XP, photo prints x2)
  - [x] 2026-10-22 UI polish 2 (`docs/research/game-ui-craft.md` #4, #8-#11, #13-#15): water-droplet UI sounds, wave wipe between title/day report and the park (`UiWipe`), H hides the HUD and Park rows flash in when they change, Settings accessibility (UI size, camera shake, reduce motion, colorblind patience faces, key list; `UiAccess`), prompt v2 (pop-in, HOLD key cap, sandy + shake when it can't be used: `IInteractable.CanUse`), upgrade tooltips (`Tooltip`), Mail tab (`Notifications.Log`), wallet tide toward your best day (`GameStats.BestDayIncome`)
  - [x] 2026-10-22 performance: at park level 30 (6 pools, ~20 surfers) the water was 106 ms of a 120 ms frame in the Editor. Now the surface math runs on all CPU cores and far pools refresh every 2nd/3rd frame: water 16 ms, frame ~34 ms (profiler on). Test: `tools/test/perf_park.sh`
  - [x] 2026-10-22 quiet HUD (Ido: "the HUD is too full"; `docs/research/hud-less-is-more.md`): always on = money + level badge + clock + a one-line mission (the next step, its price); the rest pops in for a few seconds on rare events (level-up / XP quarter, new half star, lost customer, every 5 trash, the morning's event, challenge progress, bills from 6 PM) or while you hold Alt (peek) / J. Goal strip only for side goals and while you can afford one; wave panel = wave + charge bar. Screen covered: ~23% -> ~6-8%
  - [x] 2026-10-22 minimal HUD v2 (Ido: "minimal HUD, I want to see the game, more alive"): no cream cards in normal play. Money = a small ink sign (wave pattern, tide, level badge with a gold XP ring that pops on level-up, coin flips on income), clock as plain text, the mission as one ink pill (turns teal "Done!" for a second when you finish a step, then the next step slides in); pop-ins and the ready goal are ink pills; wave panel only at the console; bubbles only for surfers who need you + the 3 nearest; money popups merge into one number. Alt / J = full cards
  - [x] 2026-10-22 polish what we have (Ido: no new features): re-pricing so you do jobs by hand before automating them (`docs/research/pacing-and-upgrade-costs.md`: Auto Wave Timer L8 $3,000 after 40 hand-sent waves, Wave Sets $500 L3, chores hired first, Desk Clerk after 60 sessions sold; `Upgrade.handCount` gates; the upgrade list lives in code only), one-time tips + Uncle Kai's postcards (`TutorialTips`, Kai is retired and travelling), missions saved by id, floating junk drifts to the walls (net 9 m), Blender pass (bevels, smooth + weighted normals, Solidify; new palm, lifeguard tower, rocks, lounge chair, kiosk, bench, umbrella; `docs/research/blender-high-level.md`)
  - [x] 2026-10-22 growing pool, slice 1 (Ido: "pools so far and so big you can't handle everything"): every zone now sits in one connected basin you can walk around in a minute. Main pool 60x50 (grows from its south wall) with a 12 m grandstand pier to the north, the Big Wave zone (60x52) behind it, a 12 m sandbar east with separate in/out lanes (x 64.5 / 67.5), the Reef (60x52, turned 180 deg, machine on the east) across it, the Cove (two 33x50 halves around the paddle pier) north of the Reef, the Ranch a 150 m strip along the east edge. Surfers walk one spine (z -17) and the sandbar lanes. The sand island is cut from `GreyboxBuilder.WaterRects`; `Surf Pool/Check Layout` (`Editor/LayoutCheck.cs`) lists props in water and routes crossing water (0/0). Rides: main 9 s, Big Wave 13.5 s, Reef 12 s, Cove 8-9 s, Ranch 23 s; water 14.7 ms at level 30
  - [x] 2026-10-22 growing pool, slice 2 (pool shapes by level and cost): every beach end is now a sand crescent. The slope's top follows an ellipse at each corner (`PoolSpec.beachCorner`, per stage `stageCorners`), so the waterline sweeps round and the corners are dry sand. The main pool gets rounder as it grows (5x8 -> 7x12 -> 8x15 m corners; bare, then planters, then palms). Each zone has its own edge look (`PoolLook`): Reef = dark teal tiles, basalt coping, boulders along the edges; Big Wave = slate walls and big dark rocks; Cove = sandstone and palms (together the two halves read as a fan); Ranch = white walls, white planters. Riders and floating junk follow the curve (`WaveWater.HalfWidthAt`/`ShoreAt`); junk now washes up at the waterline instead of under the sand
  - [x] 2026-10-22 growing pool, slice 3: luxury tiers (Ido: "the richer you are the luxurier the place"). Tiers follow total money earned (never the wallet): Beach Club $25k, Resort $150k, Luxury Resort $500k (`ParkGlowUp.TierEarnings`). Deck paint concrete -> wooden boardwalk -> sandstone -> marble (plus coping and pool tiles); loungers under umbrellas along the sandbar and north decks (beach club), shade sails replace the umbrellas and cabanas take every other deck spot, pools glow turquoise after dark (resort), fire-pit corners and a fountain at the gate (luxury). New models in `tools/blender/build_luxury.py` (ShadeSail, Cabana, FirePit, Fountain). Props only go on free deck (`LuxuryBuilder.Props`, checked by Check Layout's new "luxury props on routes"). Tier-ups post a toast; loading a save snaps. The pause menu shows the park style. Visual only for now (a mood/price bonus is a possible follow-up)
  - [x] 2026-10-22 story chapters 4-6 and the ending (`docs/research/story-and-tutorial.md`): ch4 Legends (host a legend, become a resort), ch5 The Big One (rival Splash Valley's Chad Waverly mocks your waves: Big Wave zone + 30 barrels, then the Cove + 600 sessions), ch6 Kai Comes Home (the Surf Ranch, a judged-ride showdown: beat Chad's 7.20, then Uncle Kai arrives at the gate as a special legend, surfs, and his last letter ends the story; the game goes on). Missions can post a Mail letter when they start (`Mission.mail`) and run code while current (`whileActive`)
  - [ ] Steam build

## Ideas parking lot
- Waiting for Ido: balance calls listed in `docs/BALANCE.md` (Cove/Ranch prices and levels, long-ride scores, board prices)
- Ranch follow-ups: a return lane or jet-ski tow back after a ride, lefts on the way back (the foil goes both ways), sections (barrel / wall / air ramp)
- Checks for the Cove/Ranch (L22+): kook rescue posts/lifeguard don't reach them (keep beginners out of one-way pools?); a breakdown on the Cove's west side isn't shown on the HUD (it shows the east machine); frame time with both Cove halves + the Ranch in view not measured yet; the "Measured in the game" income table in BALANCE.md predates sets/lulls
- Next (Ido's direction, decided): ONE pool that grows in stages with real shapes (`docs/research/park-layout-and-pool-shapes.md`: Puddle -> Bay -> Cove -> Reef -> Legend lane; Reef/Lagoon/Cove become stages, the Ranch a ~150 m strip attached at the end), surfers walk one straight spine (gate -> desk -> rentals -> deck, separate in/out lanes), then luxury tiers by park wealth, then story chapters 5-6 to the ending (Kai comes home; `docs/research/story-and-tutorial.md`)
- Performance next: a Burst job for the water surface (most of the 16 ms left is the main pool, refreshed every frame). `Mathf.PerlinNoise` can't run in Burst, so the lumps/boil noise must change in `Sample` for both paths (mesh and `LocalHeight`, which surfers ride). Draw calls are ~8.7k but render CPU is ~4 ms, not the bottleneck yet
- UI follow-ups: world-anchored bubbles over shops/sign (prompt v2's optional part), controller navigation pass, a subtitles toggle for voices, UI size above 130% needs scrolling windows
- Surf contest day (judges' score exists now), session schedule ("beginner hour"), tide
- Per-item prices, soft restock, visible shop upgrades (ranked in `docs/research/polish-and-next.md`)
- Surfer wipeouts (funny ragdolls)
- Wave designer: size, speed, left/right breaks
- Surf competitions as events
