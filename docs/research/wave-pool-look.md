# How real wave pool waves look and move (and what to change in our WaveWater)
Date: 2026-10-20

Sourcing: numbers with a link come from articles or company pages (company numbers are marketing claims). **[E]** = my estimate or general surf knowledge, not found in a source. Text articles almost never describe colors, foam patterns or backlight, so all "look" notes in section 3 are [E] (from general knowledge of wave-pool videos and the ocean). Words explained once: **peel** = the break point sliding sideways along the wave; **closeout** = whole wave breaks at once; **spit** = a puff of spray shot out of a barrel's mouth; **shoaling** = a wave getting taller and steeper as water gets shallow (see [wave-physics.md](wave-physics.md)).

Corrections to older notes (older files untouched):
- The Wavegarden Cove has **no foil**. It uses a row of modules on a center pier; each module is a lever/paddle that fires in sequence ([Wave Pool Mag](https://wavepoolmag.com/?p=7869), [Wavegarden newsroom coverage via Australasian Leisure](https://www.ausleisure.com.au/news/new-generation-artificial-surf-technology-producing-1000-waves-an-hour)). Alaia Bay is a Cove too.
- The Cove makes lefts on one side of the pier and rights on the other, **no split peak** ([Wave Pool Mag](https://wavepoolmag.com/?p=7869)). Split peaks come from pneumatic pools (AWM's New Jersey site, Endless Surf A-frames). Our `Peak` type is fine; it just copies those.

## 1. Life of one wave, by technology
| Tech / pool | Machine | Wave size | Ride | Time between waves | What stands out |
|---|---|---|---|---|---|
| Wavegarden Cove (Bristol, URBNSURF, Lost Shore, Atlantic Park, Alaia Bay) | Modules (28-62) on a center pier, firing in a snake-like sequence; more modules = longer, more powerful wave | Reef up to 2.4 m face; Peak about half; Bay knee-high | 10 s (small) to 15 s (large) on Reef; 7-10 s Peak/Bay; avg up to ~16 s reported at Atlantic Park | 8 s per side claimed; real sessions slower | Three zones: Reef (1.0-2.5 m deep, barrels/airs), Peak (Malibu-style open face, reformed from Reef), Bay (0.5-1.2 m deep, knee-high whitewater rollers). "Dissipative shores" cut backwash ([Raised Water](https://raisedwaterresearch.com/wavegarden-cove/)) |
| Kelly Slater Surf Ranch | Hydrofoil dragged down a ~2,300 ft basin | 6.5 ft (2 m) face, can be scaled down | Up to ~45-60 s; barrel section can cover a surfer ~14 s | One wave per ~4 min (one source) | Starts flat-faced, steepens, hollow middle, flattens, then a long fast barrel; lip "cuppy and tight"; a churning foamball the whole ride; wave is pushed from the side and behind, so takeoff is easy ([Surfer Today](https://www.surfertoday.com/surfing/kelly-slater-unveils-the-ultimate-artificial-wave-pool), [Tracks](https://tracksmag.com.au/5-things-you-might-not-know-about-the-surf-ranch-511725), [Kingsriver Life](https://kingsriverlife.com/11/03/everybodys-gone-surfin-surfin-in-lemoore/)) |
| AWM PerfectSwell (Waco/BSR) | Air chambers hidden in a center wall; software fires them in patterns | 2-6 ft, 30-60 m long | ~10 s at Waco; 3-8 s at New Jersey | 120-160 waves/h in **3-wave sets, 1:10-1:30 between sets**; lefts and rights one at a time, ~30 min each | Mushy to pitching to aerial closeout, set by firing order ([Wave Pool Mag](https://wavepoolmag.com/how-the-american-wave-machines-perfectswell-system-works/)) |
| Endless Surf (pneumatic) | Fan-pressurized chambers like piano keys; heart-shaped pool, 6 zones | Up to 7 ft | ~26 s claimed; ES66 Brazil ~40 s, 5-6 sections | Every 6 s claimed, "no reset time" thanks to bathymetry + a wave-damping system | Expert wave's shoulder **reforms** into a mellow inside wave; whitewash reforms into a beginner wave (0.6 m wide shore wave, ~10 s straight); inside reform breaks 6-8 s ([Wave Pool Mag](https://wavepoolmag.com/how-endless-surf-works-with-engineer-clement-ginestet/), [Surf Park Central](https://surfparkcentral.com/interview-with-baptiste-caulonque-of-endless-surf-on-pneumatic-wave-technology/)); color-coded zones: red foamies, green longboards, orange shortboards |
| Surf Lakes | Giant plunger in the middle; rings travel out to reefs | Commercial up to ~2.4 m; "Island" is a heavy slab | Quick, punchy | Sets: sources disagree (3 waves max so far per Stab; "4 sets/h, ~1 min between" in another) | Plunger makes loud "Jurassic Park" noises; swell rolls in and moves through the lineup like the ocean ([Wave Pool Mag](https://wavepoolmag.com/firsthand-surf-lakes-average-bro-surfs-pros-namesake-break/), [Stab](https://stabmag.com/news/praises-and-criticism-of-the-yeppoon-wave-pool)) |
| Palm Springs Surf Club (Surf Loch, pneumatic) | Air pumps water in and out of chambers | Heavy slabby barrels | Barrel off the takeoff, then turns | 8 s interval in the pro session | "Five Slabs of Fury": top-to-bottom tube right off the takeoff ([Wave Pool Mag](https://wavepoolmag.com/palm-springs-surf-club-kicks-into-high-gear-adds-265-session/), [Raised Water](https://raisedwaterresearch.com/palms-springs-surf-club-pumps-its-first-barrels/)) |
| URBNSURF, Lost Shore, Atlantic Park | Cove | 0.5-2 m; 20+ wave types | up to 22 s (Lost Shore claim) | ~12 waves per surfer per 55-60 min session | Point/Bay areas on each side of pier; Malibu-style beginner wave ([Wave Pool Mag](https://wavepoolmag.com/?p=4143), [Wavegarden](https://wavegarden.com/wavegarden-consolidates-its-global-leadership-with-the-opening-of-lost-shore-surf-resort-in-edinburgh-the-ninth-surf-park-using-basque-technology/)) |
| O2 SURFTOWN, Waco/BSR details, Alaia Bay foil look | not found | | | | No usable visual detail found; skip |

### Moment by moment (mixing sources and [E])
1. **Machine fires.** Cove: a ripple of modules fires down the pier, back to front. Pneumatic: chambers "huff" and the water in front of the wall heaves. Ranch: foil rumbles down the basin. [E] You see a boil or bulge of foam at the machine wall, and a short dip in the water level right at the generator.
2. **Swell forms.** A smooth long bump, mostly one clean line across the pool, already near full size a few meters from the machine (more modules = longer, stronger wave, per Wave Pool Mag).
3. **Speed.** Shallow-water speed = sqrt(9.8 x depth). Pool depth 1-2.5 m gives about **3-5 m/s** [E, derived]. Ride lengths of 30-60 m in 10-15 s match this (about 4 m/s along the wave).
4. **Steepening (shoaling).** As the floor rises the front gets steeper and the crest sharper until height is about 0.75 x depth, then it breaks (rule from [wave-physics.md](wave-physics.md)). [E] A flat "draw-down" trough forms just ahead of the face as water is pulled up into it; Ranch surfers talk about waves that "suck up", which matches this.
5. **Break point and peel.** The break point slides sideways along the crest. Surfer speed is about the peel speed, so **peel is roughly 3-5 m/s** [E, derived from ride length], and for a good wave peel speed is close to wave speed (a diagonal line at about 45 degrees) [E]. I found no published peel angle for any pool. Patents disagree on which way "angle" is measured (one says small angle = fast wave, another says over 70 degrees peels too fast), so I do not quote degrees.
6. **Lip and barrel.** Lip is thick at the top and thins as it falls; the tube is roughly round to slightly oval, **width about 0.5-1 x height** [E]. Ranch lip is "cuppy and tight". Pro barrels throw a puff of spit out of the mouth as the tube closes (air squeezed out) [E].
7. **Whitewater.** Behind the break the wave turns into a foamy bore that keeps moving toward the shore at wave speed, getting lower and wider. [E] Foam is thickest and whitest at the front, then loosens into lacy trails and bubbles that sit for roughly 5-10 s before fading. Beginner Bay rollers are knee height (about 0.5 m).
8. **Reform.** Unbroken shoulder far from the break settles into a mellow open-face wave (Cove Peak, Endless inside wave) at about half height [E]; whitewash re-forms as a small clean roller for beginners (Endless 0.6 m).
9. **End.** Waves run into a sloping dissipative beach (Cove) or damping chamber/grated floor (patents, [Justia](https://patents.justia.com/patent/10207168) and [USPTO example](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/10662663)) so less water bounces back. A patent notes operators often wait until the pool is nearly flat before the next wave because backwash spoils it. [E] What is left is small chop (about 5-15 cm), settling in 10-20 s.

## 2. Sets and timing
| Pool | Spacing | Per hour |
|---|---|---|
| Cove | 8 s per side (claim) | up to ~1,000 (claim); ~12 waves per surfer per 55-60 min session |
| Endless Surf | every 6 s (claim) | n/a |
| Palm Springs (pro session) | 8 s | ~450 |
| PerfectSwell | sets of 3, 1:10-1:30 between sets | 120-160 |
| Surf Lakes | sets with ~1 min gap (claim; sources disagree) | 12 to 2,000 depending on source |
| Surf Ranch | ~4 min (one source) | ~15 |

- Waves per set: 3 (PerfectSwell). [E] Wave 2 or 3 is the biggest (borrowed from ocean sets).
- Riders per wave: Cove/Endless run lefts and rights at once; each side holds one rider per section, others wait. Atlantic Park runs 20-40 surfers per session.
- Our `WaveMachine`: `cooldown` 4 s, `setSpacing` 2.5 s. At speed 7 m/s that puts crests only ~17 m apart in a 60 m pool; real gaps are about 8 s x 4 m/s = **~32 m or more** [E].

## 3. Colors and light (all [E], general knowledge, no text source)
- **Depth color:** deep end dark blue to teal, shallow beach end pale turquoise/mint where the pale floor shows through. Outdoor pools with dark floors or algae look green-brown.
- **Face:** a steep wave face is lighter and glows yellow-green/aqua when the sun is behind it (light shines through thin water). Our `_FaceColor` and G channel already do the aqua part.
- **Foam:** bright white on top, bubble-textured, with lacy trails behind that thin out. Aerated water under foam looks milky pale blue.
- **Reflections:** on calm water, sky reflection and sun glints; after a wave the surface is wrinkled so glints break into sparkles.
- **Walls/edges:** light concrete or tile with a visible waterline, coping around the top.
- **Night:** floodlights make the lagoon look like a sports arena; Wavegarden tested lights **under the water** and one on the moving foil to light the wave from inside ([Surfer Today](https://www.surfertoday.com/surfing/wavegarden-tests-a-night-surfing-formula)); Bristol added four light columns ([Wave Pool Mag](https://wavepoolmag.com/the-wave-adds-outdoor-lighting-to-get-the-most-use-of-the-wave-pool/)); Alaia Bay runs green/red light nights with LEDs on boards ([Alaia Bay](https://booking.alaiabay.ch/flash-events-2026)).

## 4. Machine and surroundings
- **Look:** Cove = long pier with identical modules; pneumatic = chamber wall/boxy hall with fans or pumps; Ranch = rail track with a foil/train; Surf Lakes = a large steel plunger in the middle (loud, "Jurassic Park" groans).
- **Sound:** Cove modules thump in sequence [E]; pneumatic huffs and whooshes [E]; Surf Lakes rumble/groan (source above). No detailed recordings found.
- **Markers:** Endless Surf color-codes zones (red/green/orange). [E] Buoy lines and signs mark the peak, reform and shore areas; paddle channels along the walls let surfers return without crossing the break.
- **Lifeguards:** towers and pier-end platforms [E]. **Jet skis:** not found in any pool source; skip.

## 5. Beginner bay vs pro section
| | Beginner (Bay/shore) | Intermediate (Peak/inside) | Pro (Reef) |
|---|---|---|---|
| Depth | 0.5-1.2 m | middle | 1.0-2.5 m |
| Height | knee-high / 0.6 m | about half of Reef [E] | up to 2.4 m face |
| Breaks | whitewater rollers, ride straight | open face, Malibu-style, reformed | barrels, airs, hard turns |
| Ride | ~10 s straight | 7-10 s | 10-15 s (more on big pools) |
| Look | white foam wall, soft | clean green face, little foam | steep, hollow, thick lip, spit |

Ours: Beginner bump 1.0 m, whitewash 0.55 m (good: matches knee/chest height rollers); Intermediate 2.0 m and Pro 2.8 m are above real pool maximums (2.0-2.4 m face) so they read as "big", which is fine for a game; Pro `peelSpeed` is 3.2 m/s vs wave speed 8.5 m/s, so p/c is about 0.38 (real good waves about 1 [E]), meaning our break opens as a thin V instead of a diagonal line.

## What our WaveWater does now (so changes are specific)
- Wave = Gaussian bump (`LocalHeight`), narrow front (`frontWidth`), wide back (`backWidth`); crest moves linearly, `WaveCenter = -back + (t-start)*speed`.
- `Foam()` is **stateless**: foam exists only near the crest and a 7 m trail behind (`exp(d/7)*0.75`), so it vanishes with the wave.
- Beach: `beachBreak` starts at 75% of length and turns every wave into whitewash. By my reading of `BuildBasin` the ramp meets the water at about 85% of the pool length (check).
- Water is **opaque**, 2 colors (`_DeepColor`, `_FaceColor`) plus foam; no depth tint; vertex color B is unused.
- Lip is a thin one-sided curved sheet (`AddLipStrip`); spray is white sphere particles, 0.18-0.45 size, 0.6-1.2 s life, gravity 1.1.
- Chop: static `rippleHeight` 0.04 m, never changes with waves.

## Ideas for our game
All fit **Step 5 "Ambience + growth / polish"** unless noted. Ranked best look-per-effort first. Each is one session.

1. **Slower, spaced sets (WaveMachine `setSpacing`, `cooldown`; AWM sets).** Move `setSpacing` from 2.5 s to **6-8 s** and give waves 2-3 of a set +10-15% height [E] via a size argument on `WaveWater.SpawnWave`. Crests then sit ~30 m apart like real pools. **Warning: this changes waves per hour and income. Check `docs/BALANCE.md` before changing; maybe raise price or shorten `cooldown`.** Step 3 money loop link.
2. **Lingering foam trails with lace (WaveWater `Foam`/`Update`, shader).** Keep a `float[] foamMemory` per vertex: `memory = max(memory * exp(-dt/3.5), newFoam)`; lifetime 3-4 s [E] (real trails last ~5-10 s). Write memory to color R. In `StylizedWater.shader` add a second, finer cell/ring noise for bubbles (`_FoamNoiseScale` x3). Only ~13k vertices, cheap.
3. **Drawdown trough + steepening before the break (WaveWater `LocalHeight`, `UpdateStates`).** Add a negative lobe ahead of the face: `h -= amplitude * 0.08 * exp(-((d - front*1.8)/ (front*1.2))^2)` for d>0 (8% of height [E]). Also multiply `front` by Lerp(1.4, 1, InverseLerp(0.05, breakAt, progress)) so the wave visibly sharpens before it breaks. Note `SurfaceHeightAt` feeds surfers, so check their bobbing.
4. **Barrel spit and finer lip mist (EmitSpray/EmitBlob, `BuildSpray`, `AddLipStrip`).** When a barrel starts closing (`breakDistance` ~1.5 s), burst 10-15 blobs from the tube mouth along ride direction at 6-9 m/s [E]. For the lip, add a second thin strip offset 0.1 x radius to give the lip thickness (thicker at top). Add a lighter mist: many small blobs 0.05-0.12 m for the lip, rare big ones.
5. **Backlit face and depth tint (StylizedWater.shader, WaveWater `Foam` B channel).** (a) Backlight: `backlit = pow(saturate(dot(viewDir, -mainLight.direction)), 4) * steep` blending toward a new `_BacklitColor` (yellow-green, e.g. 0.55, 0.9, 0.55) [E]. (b) Depth: write `colors[i].b = InverseLerp(0.5, 0.85, x/length)` in `Foam()`; shader mixes `_DeepColor` toward a new pale `_ShallowColor` (0.35, 0.85, 0.85) [E] by B. Water stays opaque so no transparency work.
6. **Leftover chop and backwash that settle (WaveWater `LocalHeight`, new `residual` float).** After each wave passes x=0.5L add to `residual` (0.1 [E]); decay it with half-life ~8 s [E]; ripple term becomes `rippleHeight * (1 + 3*residual)` and choppier frequency. Later: a "Wave Absorber / dissipative shore" upgrade halves it (real: Cove dissipative shores, damping chambers).
7. **Reform zone (WaveWater `Broken`, `beachBreak`).** For Intermediate and Pro, delay `beachBreakStart` (to ~0.85) and, in the unbroken stretch, let the shoulder settle to ~50% height with no foam (Cove Peak, Endless inside wave). Beginners still ride the whitewash. Gives surfers a 2nd, gentler wave to ride.
8. **Slow the wave as it shoals (WaveWater `WaveCenter` becomes a stored position).** Store `center` in `ActiveWave` and advance it each frame by `speed * Lerp(1, 0.55, beachBreak)` [E] (from sqrt(g x depth) with depth falling to ~1/3). Longer-looking approach, and crests bunch up near the beach like real sets. Surfers read `CurrentWaveCenter`, so test the pocket logic.
9. **Ranch-style sections along a ride (WaveWater `PocketMask`, `BreakPoint`, `AddLipStrip`).** Make the pocket height and lip curl a function of `breakDistance`: flat-faced start (0-2 m), steeper, hollow barrel zone (~40-80% of the width), then flat end. Applies best to Pro/Pipeline; add a `Snapper Rocks`-style preset field `sections` to `FamousWave`.
10. **Machine boil + sequential thump (WaveMachine `TrySendSet`, `Sfx`, a particle at x=0).** On launch, a 1-1.5 s foam boil at the machine wall and 3-5 thump sounds spaced 0.1 s apart (Cove modules firing in sequence) instead of one `Sound.WaveMachine`. Make the wall water dip 0.2 m for a second [E].
11. **Shoreline swash line (WaveWater `Foam`).** From the ramp geometry the waterline is at about 0.85 x length (my derivation, check). Add a foam band there that pulses forward and back 0.5-1 m [E] when a wave's `beachBreak` is high, plus a faint reflected wave (10% height) running back.
12. **Peel/speed retune for diagonal breaks (WaveType `speed`, `peelSpeed`; FamousWaves `peel`).** Slow `speed` to ~4.5-5.5 m/s [E] (real 3-5 m/s) while keeping `peelSpeed` ~2.6-3.2, giving a ratio of 0.5-0.65 (diagonal wedge). Ride length and gameplay change, so playtest and also check surfer timing.
13. **Night water glow (shader `_GlowColor`, the day cycle script).** At night add an emissive aqua term on faces and foam (underwater lights, Wavegarden) and 4 floodlight poles (Bristol) as a build-mode unlock. [E] Fits the "Later" night sessions idea in [surf-park-design.md](surf-park-design.md).
14. **Lineup markers and lifeguard (GreyboxBuilder.BuildPool / WorldArt).** Floating buoy lines splitting inside/middle/outer lineup with Endless-style colors (red foamies, green longboards, orange shortboards), a paddle channel along one wall, a lifeguard chair on the pier. Pure props.
