# Longer, wider rides: how real pools do it and how we can fake it
Date: 2026-10-20

Tags: **[S]** = from a linked source. **[E]** = my estimate or own math. Company numbers are marketing claims. Words: **celerity (c)** = how fast the wave crest travels; **peel speed (v_p)** = how fast the break point slides along the crest; **peel angle (alpha)** = angle between the crest line and the path of the break point; **section** = a stretch of the ride with its own shape (hollow, flat, ramp...).

## 1. Real long-ride pools
| Pool | Size [S] | Ride [S] | How the length is made |
|---|---|---|---|
| Kelly Slater Surf Ranch | Sources disagree: ~610 m (2,000 ft, [ABC30 via our surf-park-design.md](surf-park-design.md)), **700 x 150 m** ([Raised Water](https://raisedwaterresearch.com/kelly-slater-wave-company/)), wave ~3,200 ft (~975 m, WSL, via search). Deepest 9 ft (2.7 m); wave breaks in 3-4 ft | Up to **45 s**; first right-hand barrel section up to **14 s** ([Raised Water](https://raisedwaterresearch.com/kelly-slater-wave-company/)); about 12 wave profiles | A foil carriage on rails moves at **10-18 mph (4.5-8 m/s)**; the wave is made progressively, so the break point runs along the pool at carriage speed. Wave 2 m face; one wave per ~3-4 min. Foil length: not found |
| Endless Surf ES66 (Brazil, planned) | Footprint not found (an older 48-chamber pool is 282 x 84.5 m) | ~**40 s**, "5-6 sections"; split-peak mode makes two ~20 s waves ([Wave Pool Mag](https://wavepoolmag.com/largest-endless-surf-lagoon-sao-paulo/)) | Many air chambers firing in a pattern; more chambers = longer wave |
| Wavegarden Cove | Full size ~**150 x 150 m**, minimum ~120 x 120 m; R&D pool 50 x 80 m ([Raised Water](https://raisedwaterresearch.com/wavegarden-cove/)) | 15-20 s claimed; ~10 s (small) to ~15 s (large) Reef; 7-10 s Peak/Bay | A row of modules on a center pier; **more modules = longer ride** ([Wave Pool Mag](https://wavepoolmag.com/?p=3107)). Lefts and rights break away from the pier |
| Lost Shore (Edinburgh) | 52 modules, 250 m of beachfront ([Time Out](https://www.timeout.com/uk/news/scotlands-first-inland-surfing-resort-opens-next-week-110824) via search) | up to **22 s** (pre-opening claim) | Same Cove idea; each half has a Bay (whitewater) and a Reef (green wave) |
| Waco BSR / American Dream (PerfectSwell) | ~2 acre lake, 24 chambers (Waco) | 8-10 s Waco, 5-8 s New Jersey ([Raised Water](https://raisedwaterresearch.com/?p=168)) | Short on purpose: 120-160 waves/h |
| Surf Lakes | ~200 x 150 m (one report); 4, 5 or 8 reefs depending on source ([EAP](https://www.eap-magazin.de/Article/Australia%3A-New-Multi-Break-Wave-Pool-Promises-Surfing-Fun-with-up-to-2-400-Waves-per-Hour.html)) | **Not found** | Rings of swell hit curved reefs around the edge: many short breaks, not one long one |
| Palm Springs Surf Club | **Not found** | Barrel right off the takeoff, then turns ([wave-pool-look.md](wave-pool-look.md)) | Pneumatic "slabs" |

Pattern: **long ride = long distance along the break line.** Ranch 45 s at ~6.5 m/s average is about 300 m of ride [E]. Cove 15 s is about 90 m [E]. That is why the Ranch is a 700 m strip.

### The math real designers use
- A wave moves at c, and the break point slides along the crest at v_p. The surfer rides the break point, so surfer speed v_s = c / sin(alpha) and v_p = c / tan(alpha). Tiny angle = very fast peel. [S: formula quoted in patents, e.g. [US 9777494](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/9777494); I could not open the original Hutt, Black and Mead 2001 paper, so treat as secondary.]
- Practical peel angles: roughly **30-70 degrees** (patents); a Swellnet article says under ~27 degrees most waves peel too fast for anyone ([Swellnet](https://swellnet.com/news/coastal-creationism/2015/10/19/coastal-creationism-part-1-basic-reef-shapes)). Sources disagree on the low end. Bigger waves and better surfers can take smaller (faster) angles; beginners need wide angles (slow peel).
- The break point moves because the **reef line is diagonal** to the crest. Ranch: reef parallel to the track, crest at an angle to it.
- Shallow-water speed c = sqrt(g x depth): 1-1.5 m deep gives ~3-4 m/s ([wave-pool-look.md](wave-pool-look.md)).
- **Sections and reforms** [S]: Endless Surf 5-6 sections in 40 s; its expert wave's shoulder reforms into a mellow inside wave and whitewash reforms into a ~0.6 m beginner roller ([wave-pool-look.md](wave-pool-look.md)); Ranch has barrel, turn and air sections ([Raised Water](https://raisedwaterresearch.com/kelly-slater-wave-company/)); the Kelly Slater Wave Co also announced an added air section (headline only; the page was blocked for me).

## 2. The math for our game
### What our rides really are today (read from code)
A ride ends at whichever limit comes first:
| Limit | Rule in code | Intermediate (c 7, peel 2.6) | Pro (c 8.5, peel 3.2) |
|---|---|---|---|
| Down the pool (x) | NPC: crest >= 0.88 x length; player: > 0.9 x length | break at 15 m; 52.8 m end = **5.4 s** | break at 13 m; end = **4.7 s** |
| Along the crest (z), NPC only | break starts at `peakOffset` 4 m, ends at half-width - 1.5 = 10.5 m: 14.5 m of peel | 14.5 / 2.6 = **5.6 s** | 14.5 / 3.2 = **4.5 s** |
(60 x 24 pool; break time = (breakAt x length + backWidth) / speed.) Implied peel angle: atan(7/2.6) = **~70 degrees**, atan(8.5/3.2) = **~69**: at the "slow, mushy" edge of the real range. Our waves rush at the beach far faster than they peel.

Consequences [verified by arithmetic]:
- **Reef Pool (`peel` 0.8) and Lagoon (0.9) barely change ride time**: the x limit (5.4 s) hits first. The Malibu preset (`peel` 0.6) does not give a longer ride either.
- All three pools are the same size (global `PoolLength` 60 / `PoolWidth` 24); `size` only scales height.
- Beginner closeouts ride whitewater from catch to 0.88: (52.8 - 13) / 5 = ~8 s.

### Two ways to build a longer ride
| | A. Straight crest (what we have) | B. Angled crest, "Ranch style" |
|---|---|---|
| Idea | Whole crest moves in +x at c; break slides in z | Crest is a diagonal line (made progressively, like the foil); break sits at a fixed reef x and slides along z at v_p |
| Rider moves | diagonally: speed c / sin(alpha) | along z only: speed v_p |
| Pool length x | about c x T + 20 m | **about 40-50 m, whatever T is** |
| Pool width z | about v_p x T + 5 m | about v_p x T + 20 m |
| Area | grows with **T squared** | grows with **T** |

Sizes for a target ride (T), using A: c 4.5, v_p 3.3 (alpha 54, v_s 5.6 m/s) and B: v_p 6 [E, for Ranch-like 4.5-8 m/s carriage speeds]:
| Ride T | A: length x width | A: area | B: x by z | B: area |
|---|---|---|---|---|
| 5 s (today) | 60 x 24 | 1.4k m2 | n/a | n/a |
| 10 s | 70 x 38 | 2.7k | 45 x 80 | 3.6k |
| 20 s | 110 x 72 | 7.9k | 45 x 140 | 6.3k |
| 40 s | 200 x 137 | 27k | 50 x 260 | 13k |
A is fine up to ~10-12 s; **B wins from ~15 s.** (B's rider also faces down the line with the wave on one side, like real surfing; A's diagonal path is odd.)

### How B works in WaveWater [E]
- Replace the scalar crest `center` by a per-z crest: `crestX(z) = center - shear x (z - z0) x dir`, with `shear = c / peelSpeed` (tan alpha). Behind the carriage the wave is already far along, ahead it does not exist yet (the existing `rise` ramp gives amplitude 0 there).
- Proof the break stays at one x (the "reef"): break point z_b = z0 + v_p (t - t_b); crest there = X0 + c(t - t0) - shear x v_p (t - t_b) = constant. So the existing `breakDistance = (t - breakTime) x peelSpeed` stays valid.
- Uses: `LocalHeight`/`Foam` (`d = x - crestX(z)`), `Broken`, `beachBreak` (per z), `CurrentWaveCenter(id)` -> `CrestXAt(id, z)`, `TryGetPocket`, and the lip/spray code (already uses `LocalHeight(state.center, z)`). The visible wave is a diagonal ribbon about (pool x length) / shear = ~45 m long in z; the green shoulder ahead of the break is ~20 m (about 3 s of look-ahead at 6 m/s).

### Performance (CPU mesh, 3 verts/m, every vertex loops over every wave)
Today: 181 x 73 = **13.2k vertices** per open pool (closed pools should be inactive: check `buildVisuals`), plus `RecalculateNormals` on the whole mesh each frame.
| Pool | Verts @3/m | @2/m | Only the ~60 m active slice @2/m |
|---|---|---|---|
| Main 60 x 24 | 13k | 6k | n/a |
| Reef A 75 x 36 | 25k | 11k | n/a |
| Lagoon A 75 x 50 | 34k | 15k | n/a |
| Ranch B 50 x 200 | 91k | 41k | **~12k** |
Fixes, cheapest first [E]:
1. `verticesPerMeter` 2 on long pools (face width 1.4-3 m still gets 3+ samples at 0.5 m).
2. Cut the pool into ~25 m chunks along z, each its own mesh; only update chunks near an active wave (the wave ribbon is short). Idle chunks keep flat water.
3. When no wave is active, update ripples at ~15 Hz or move ripples into the shader.
4. Later: Burst job for the vertex loop, analytic normals. 1 and 2 are enough for the Ranch.
5. Slow waves live longer, so more are active at once; keep wave spacing >= ride time / 2.

## 3. Keeping a 15-40 s ride interesting
Little exists on how surf games pace long rides; the closest facts are in [surf-feel.md](surf-feel.md): random-feeling failure and bad cameras are the top complaints, simple inputs with positioning skill are praised, forgiving combos keep beginners happy. The rest below is general game-design practice [E].
- **A new thing every 4-6 s** (a section), a calm part after each hard one, a finale at the end.
- **Show it coming:** the green shoulder ahead of the break is the preview; add a banner "NEXT: THE PIT" 3 s early.
- **Failure must explain itself** and be recoverable (fall behind the break = "caught by the whitewash", get a second chance to catch up by pumping W/S).
- **Partial credit:** a fall at 15 s of 20 s still pays for the sections cleared.

### Section kit (changes steepness, height, lip, speed need)
| Section name | Wave shape [E] | What to do | Real model |
|---|---|---|---|
| Drop-in | medium height, soft | bottom turn | Ranch takeoff is easy: pushed from behind |
| The Pit | steep (front width x0.7), lip thrown, barrel for Pros | stay deep, hold line | Ranch 14 s barrel; Pipeline |
| The Wall | open face, height x1 | carves, cutbacks, combos | Malibu, J-Bay |
| The Fat Spot | height x0.6, wide, slow | pump (W/S) to keep flow | "sectiony" waves, Uluwatu |
| The Ramp | short steep bumps | launch an air | Ranch air section |
| Closeout | wave speeds up, pitches all at once | last big trick or get pounded | closeout |
| Inside Reform | small clean roller after the break | beginner end, exit | Endless Surf reform |
Sample 28 s Ranch ride: Drop-in 0-3 s, Pit 3-9, Wall 9-16, Fat Spot 16-19, Ramp 19-23, Closeout 23-28.

How the NPC loop must change: trick every 1.3-2 s was tuned for 5 s rides (10+ tricks in 20 s); wipeouts land 0.8-2.5 s after catch. Use section-driven tricks (barrel only in the Pit, air only on the Ramp, floater at the Fat Spot) and a **per-section fall chance** q = 1 - (1-p)^(1/n) (p is today's per-ride chance, n = sections), higher on hard sections for low skill.

## 4. Proposal for the next pools (all [E])
| Pool | Size (x length by z width) | Ride | Wave model | Wave character | Unlock |
|---|---|---|---|---|---|
| Main (today) | 60 x 24 max | ~5 s | straight | beach break, beginner-intermediate | start |
| **Reef Pool -> "Point Pool"** | **75 x 36** | **~10 s** | A, break starts at a wall (`peakOffset` = half-width - 3) | long mellow right/left (Malibu/Snapper): c 4.5-5, peel 3.3 (alpha ~55), 3 sections: Drop-in, Wall, Inside Reform | $18k (existing) |
| **Big Wave Lagoon** | **75 x 50** (or B at 45 x 100: ~14 s) | ~12 s | A (or B) | heavy: height x1.25, steep Pit then Wall, Pro barrels, peel angle ~40 | $80k (existing) |
| **Surf Ranch** (late) | **50 x 150 / 210 / 280**: three stages via `SetStage` | **20 / 30 / 40 s** | B | the full section kit; one big wave every ~25 s; 2-3 takeoff spots along z (later spots = only the end sections, cheaper seat) | after the Lagoon, ~$300k [E] |
Price: pay roughly in proportion to ride seconds. Reef x2.5 and Lagoon x5 already fit (2x and 2.4x the 5 s ride); give the Ranch ~x12-16 but few waves per hour (see [BALANCE.md](../BALANCE.md)). Paddle-back is 3 m/s (`paddleBackSpeed`): 200 m = 67 s, so add a return lane or shuttle for the Ranch.

### Code changes (in order)
1. **Per-pool size.** `PoolSpec` gets its own length and width; `PoolLength`/`PoolWidth` are global consts used in `BuildPool` (decks, basin, machine house `PoolWidth - 2`, beach landing at `PoolLength x 0.95`, torches) and the Reef/Lagoon `pathToPool` uses the global `halfW`. `WaveWater.length/width` already serialize and `Resize` works.
2. **Ride end in meters.** `Surfer.rideEndProgress` (0.88, serialized on the prefab) and the player's `0.9` are fractions: replace with `water.RideOver(waveId)` (or `endX` metres). Same for `breakAt` and `beachBreakStart` (fractions grow oddly on long pools). Make `peakOffset` scale with width.
3. **Slower waves:** new Point/Lagoon wave speeds (c 4.5-5), peel set from a target angle: `peelSpeed = c / tan(alpha)`; add `peelAngle` to `WaveType` and compute. Check Famous presets still make sense.
4. **Sections** belong on the pool (reef), not `WaveType`: a list `Section { name, startMeters, heightMul, steepMul, barrel, speedNeed }` read by `LocalHeight` (per-z multipliers), lip (barrel flag), `Surfer.DoTrick`, and the HUD.
5. **Mode B** (angled crest, section 2): per-z crest, `CrestXAt`, chunked mesh. Needed only for the Ranch (and optionally the Lagoon).
6. **Player:** `PlayerSurfing.Ride` has no pocket logic (glued to crest x, free in z). For long rides use `TryGetPocket`: you are carried along the pocket, A/D shifts you +-6 m; behind it = whitewash wipeout, far ahead = lose flow. Score by section, not per second. End on `RideOver`, not 0.9.
7. **HUD/camera for long rides:** ride progress bar with section ticks, big section banner (3 s warning), timer, a "best ride" stat; camera looks down the line (small yaw toward travel), speed FOV capped; ensure comfort option.
8. **Layout.** Pools tile along z: Main z=0 (decks 28/28), Reef z=64, Lagoon z=116, decks 12 south / 16 north, `pathOut` z values hard-coded (58, 110). With new widths compute `z_next = z_prev + W_prev/2 + north + south + W_next/2`: Reef W36 -> z~70 (spans 52-88), Lagoon W50 -> z~141 (116-166), deck to ~182. The park's sand slabs end near z=204 (`WorldArt.SandSlab`): enough for those two.
   The Ranch (50 x 280 at most) will not fit the z stack. Put it **north, rotated 90 degrees** (its long peel axis along world x) at z ~215-265, x from ~-30 to ~250 (extend the north sand slab), with its own check-in. `BuildPool` and path points must then be built under a rotated root, and **`Surfer` line ~561 (`landing.z = transform.position.z`) assumes pool z = world z** and must be rewritten in local space. Walk from the entrance is ~230 m (~2.5 min at 1.5 m/s [E]): add a shuttle or a second desk. Unverified: exact entrance positions, other hard-coded routes in `ParkExtras`/`GlowUpBuilder`/`BeachGuests`.

## Ideas for our game
- **Step 4+ growth, session 1 (small, safe): "Point Pool".** Do code changes 1-3 only: Reef Pool becomes 75 x 36, slower c, start at the wall, ride-end in meters. Target 10 s. Playtest: does the player see the ride last twice as long? Costs ~2x water mesh, no new systems.
- **Step 4+, session 2: sections on the pool.** Add the section list (Drop-in, Wall, Inside Reform) with banners and section-driven NPC tricks. Per-section fall chance and partial pay.
- **Step 4+, session 3: player pocket + HUD** (changes 6-7), so a 10 s ride has goals: "stay in the pocket", progress bar.
- **Step 4+: Lagoon "Pit" wave.** Lagoon 75 x 50, Pro barrel section first, then Wall; a "J-Bay Line" and "Teahupo'o" feel; peel angle ~40.
- **Later: the Surf Ranch.** Mode B + chunked mesh + stages 20/30/40 s (reuse `SetStage` like the main pool's Pool Expansion), seats by takeoff spot, return shuttle, visible foil carriage gliding along the rail at v_p (cool to watch), pro contest events. Highest risk (water code + rotation), so do it last and let Ido see the Point Pool first.
- **Tune by feel:** expose `peelAngle` in `WaveType` so Ido can slide it and watch ride seconds in the Wave Control panel; show "ride 11.2 s" on the score card.
