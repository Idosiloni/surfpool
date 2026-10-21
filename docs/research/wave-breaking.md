# How waves really break, and why ours looks like a white block
Date: 2026-10-21

Ido's playtest: "too much white water and then a huge blob moves." This file builds on [wave-physics.md](wave-physics.md), [wave-rendering.md](wave-rendering.md), [wave-pool-look.md](wave-pool-look.md), [wave-types-real.md](wave-types-real.md), [long-rides.md](long-rides.md), [wave-pools-and-ocean-waves.md](wave-pools-and-ocean-waves.md) (not repeated here). No game files were touched.
Tags: **[S]** = from a linked source. **[E]** = my estimate or tuning guess (tune by eye). Words once: **tau** = seconds since a spot on the crest broke; **bore** = the steep-fronted moving wall of broken water; **soup** = the low, flat, foamy leftover; **foam ball** = the fat white mass right where the lip lands.

## Part 1. The life of a breaking wave
| Step | What really happens | Numbers | Source |
|---|---|---|---|
| 1. Swell | Smooth, long, low bump. Never steeper than height/wavelength = 1/7 (theory), more like 1/10 before it breaks | e.g. 2 m wave, 6 s period = 56 m wavelength, so very gentle until it shoals [E arithmetic from wave-rendering.md] | [Stokes limit via search results](https://surfsimply.com/magazine/why-and-how-waves-break), [Dutch coastal manual](https://kennisbank-waterbouw.nl/EC/EC39.htm) |
| 2. Shoaling | Shallow bottom squeezes it: taller, steeper front, flat dip (trough) just ahead. The green face stands up over the last few seconds | Breaks when height is 0.6-0.7 of depth (spilling) or 0.7-0.8 (plunging); "about 1.3 x its height in depth" | [Surf Simply](https://surfsimply.com/magazine/why-and-how-waves-break), [UW study](https://digital.lib.washington.edu/researchworks/handle/1773/43651) |
| 3a. Spilling (soft) | Foam starts at the crest and spills DOWN the face. Air rides along in a roller. Break zone is wide and slow, wave stays mostly green below the foam | Gentle slope, low Iribarren number (< 0.4) | [Surf Simply](https://surfsimply.com/magazine/why-and-how-waves-break), [wave-rendering.md](wave-rendering.md) |
| 3b. Plunging (hollow) | Face goes vertical, lip is thrown forward in a thin sheet and lands in the trough. It traps air (the barrel), then a splash-up pops, sometimes 2-3 smaller ones | Lip reach ~0.5-1.1 x wave height, throw lasts ~1 s [E] | [Wikipedia: Breaking wave](https://en.wikipedia.org/wiki/Breaking_wave), [HAL lab study](https://hal.archives-ouvertes.fr/hal-00193225) |
| 4. Foam ball | Right at the landing spot the water is a fat, churning, white mass, taller than the bore that follows | ~0.7-1 x H tall, 1-1.5 x H wide, lasts 1-2 s [E] | Surf Ranch pros describe a foamball all ride ([wave-pool-look.md](wave-pool-look.md)); sizes [E] |
| 5. Bore | After a short violent re-organizing, it becomes a roller with a steep front and a lower, tapering back. Plungers do this over a SHORTER distance than spillers | Height falls fast; in the inner surf zone wave height is tied to depth: about 0.42 x depth | [ICCE bore papers](https://icce-ojs-tamu.tdl.org/icce/article/download/4073/3755/17180), [Thornton & Guza 1982](https://calhoun.nps.edu/entities/publication/b93cdf24-bd83-48f9-8dc9-708379ceba8c) (the 0.42 is an rms ratio, only the idea is used) |
| 6. Soup | Bore fades to a low bumpy sheet of foam with holes and streaks that sits there; old foam does NOT move with the wave | ~1/4 to 1/3 of the original height, foam lingers ~5-10 s [E] | Foam holes and tubes: [Monterey drone study](https://complete.bioone.org/journals/journal-of-coastal-research/volume-33/issue-6/JCOASTRES-D-16-00010.1/Analysis-of-Surface-Foam-Holes-Associated-with-Depth-Limited-Breaking/10.2112/JCOASTRES-D-16-00010.1.full) |
| 7. Shoulder | Far from the break the wave is still green and unbroken, a bit lower and softer than at the pocket | Pools reform it to ~50% height (Cove Peak, Endless inside wave) [E] | [wave-pool-look.md](wave-pool-look.md) |

### The peeling wave from above (the picture we must match)
- The crest is a long line. The break point slides along it. **Behind the break point = white and low. Ahead of it = green and tall.** Where they meet is the **pocket**, the steepest, most hollow part, "the concave part right in front of where it's breaking" ([glossary](https://aweekawake.substack.com/p/glossary-of-surf-terms-for-beginners)).
- The white part is NOT a rectangle. Right at the break point it is a fat ball. A few seconds of crest later it is a narrow bore front with a thin tail, and old foam is left behind as lace.
- Peel angle: surfers call a fast wave one with about a 30 degree peel line ([glossary hit, informal](https://flysurfbrand.com/en/blogs/surf/partes-de-una-ola-que-es-el-pocket)). Our angle is atan(travel/peel): Intermediate 4.2 / 4.94 m/s gives ~40 degrees, so the angle is fine. (This corrects wave-pool-look.md's "p/c = 0.38": it used unscaled speeds. With the pool multipliers Pro is 6.1 / 5.1.)
- Wave pool waves from the deck: Cove Reef = a clean green wall peeling along a concrete wall, 10-15 s, with a foamy end; the beginner Bay = knee-high white rollers (~0.5-0.6 m) running straight in ([wave-pool-look.md](wave-pool-look.md), [Wavegarden](https://wavegarden.com/?p=2883)). I found no source describing Cove whitewater shape in detail, so "narrow band, flat soup" is [E] from the ocean physics above.
- After it all breaks (the beach): a low rolling bore, then a swash line and a small backwash that makes chop for 10-20 s ([wave-pool-look.md](wave-pool-look.md)).

### How games fake it
- I could not find technical write-ups for True Surf, Barton Lynch, Surf World Series or Riders Republic surfing (same gap as [wave-rendering.md](wave-rendering.md)); the one surf-game article I found is marketing for [Virtual Surfing](https://www.surfertoday.com/surfing/virtual-surfing-the-game-that-promises-realistic-waves), which reviewers say still struggles with lip-hits-flat and foam after impact. I do not fill this from memory.
- What IS documented: Horizon Forbidden West uses **artist-authored breaking shapes played back by one parameter**; Sea of Thieves **keeps foam in a fading buffer separate from wave height**; Wind Waker style foam is a scrolling texture on flat colors ([wave-rendering.md](wave-rendering.md)). The lesson for us (my reading): keep THREE separate looks and animate each by age: green face, white foam, low flat bore. Don't let one number do all three.

## Part 2. What our wave does today (read from `WaveWater.cs`)
Main pool, Intermediate (scaled by the pool spec at `GreyboxBuilder.cs:80-82`, size 1): height 2.0, speed 4.2 m/s, peel 4.94 m/s, frontWidth 2.0, backWidth 8, breaks at x = 15 m of 60. Beginner: height 1.0, speed 3.0, closeout, breaks at x = 13 m. The **Starter pool is only 40 x 30**, so the first thing Ido sees is a Beginner wave breaking at x = 9 m across 30 m.

Lifetime in plain words: a Gaussian hill rolls down the pool at full height across the whole width. At the break point a spot flips from "hill" to "45% hill with white on it." It then stays that way, forever the same, until it reaches the beach, where everything goes white.

Likely causes, biggest first:
1. **A broken section has no age.** `Broken()` (line 566) depends only on z, with a 1 m soft edge (`soft = 1f`, line 571). A spot that broke 0.1 s ago looks exactly like one that broke 8 s ago: same `whitewashHeight` (line 480/606), same foam. So there is a ~2 m wide cliff from full height to 45%, then a uniform block. No foam ball, no shrinking soup.
2. **The broken shape is a wide smooth dome, not a bore.** Front widens by `1 + 0.8*broken` (line 477/598: 2.0 to 3.6 m) and the back stays a Gaussian of `backWidth` 8 m (colBack, line 452). That is a ~0.9 m hump about 15 m long that keeps its height all the way to the beach, a moving "blob". A real bore has a steep front and a short, tapering back. Near the beach `back = backWidth*(1+beachBreak)` (line 531) DOUBLES it (~16 m Gaussian, ~30 m long).
3. **White is counted twice.** Moving trail `exp(d/7)*0.75` (lines 453/646) times `strength` ~1.3 stays above the shader threshold (0.3 + noise 0.55..1.45, `StylizedWater.shader:121-123`) to roughly 8 m behind the crest [E]. The crest band is 1.1 + 1.4*broken = 2.5 m. Then `foamMemory` (line 504) keeps each spot white for `foamLinger` 3.5 s, ~15 m of lace at 4.2 m/s. Total: 15-25 m of white behind the crest along the whole broken side. Real bore foam is ~2-4 wave heights (4-8 m for a 2 m wave) at first.
4. **Beginner closeout is a white slab.** `closeoutAmount` (line 535) ramps 0 to 1 in 0.6 s for the WHOLE crest. `strength` = clamp01(1.0/1.5) + 0.7 = 1.37 (line 491), crest band 2.5 m, front 5.8 m wide, back 6 m, and `EmitSpray` fires ~54 blobs/s (45*1.5*0.8) across the whole width. In the Starter pool that is a 30 m wide, ~10 m deep white sheet that then travels ~30 m. This is almost certainly the "too much white water, then a huge blob moves."
5. **`beachBreakStart` 0.75 (line 54)** forces `broken = max(broken, beachBreak)` (line 585), so the last quarter of EVERY wave turns white across the full width at once, back width x2, plus full-width spray when `beachBreak > 0.3` (line 747). Intermediate and Pro lose their green shoulder 45 m down instead of peeling to the wall.
6. **No shoulder taper.** The unbroken side keeps full amplitude over the whole width (only `edgeDrop` 0.2 at the walls, line 50), so the green part is a uniform wall, not a face that stands up and gets hollow near the break. The pocket bump (0.15 x, 3 m wide, lines 604/623) is too small to read.
7. **Smaller things:** whitewash travels at full wave speed to the end (real bore slows); lumpiness `0.12*amplitude` Perlin is the same everywhere in the soup (line 618); foam strength has no age term.

## Part 3. Fix list, ranked by impact vs effort
Rule: `LocalHeight` (line 588) and `UpdateSurface` (line 407) must stay identical. `UpdateSurface` caches `colBack` per column (x only), so a z-dependent back width would silently break it. Do this instead: compute BOTH shapes per column (green back, bore tail) and blend per vertex with the same `Lerp` in both functions. Put `tau` (and the shoulder taper) in per-row caches next to `rowBroken` (line 433).

| # | Change | Where | Impact | Effort |
|---|---|---|---|---|
| 0 | **Numbers-only stopgap (10 min):** closeout boost `0.7` to `0.25` (line 491 and 643); trail `exp(d/7)` to `exp(d/2.5)` (453/646); Beginner `whitewashHeight` 0.55 to 0.35 and `backWidth` 6 to 3; front widening `0.8` to `0.2` (477/598); `beachBreakStart` 0.75 to 0.85; `foamLinger` 3.5 to 3 | WaveWater fields/consts | Medium: less white, smaller dome | Tiny |
| 1 | **Give broken water an age (tau)** and shrink it with age | `Broken`/new `AgeOf`, `LocalHeight`, `UpdateSurface` | Biggest: kills the block | Medium |
| 2 | **Bore shape instead of dome** | same | Big: kills the blob | Medium (with 1) |
| 3 | **Foam by age** (ball, narrow bore band, short trail) | `Foam`, `UpdateSurface`, `EmitSpray` | Big: kills "too much white" | Small-medium |
| 4 | **Beginner as a soft spilling roller that unzips** | `UpdateStates`, `Broken`, `EmitSpray` | Big for early game | Medium |
| 5 | **Beach: reform and swash, not instant white** | `UpdateStates`, `Broken` | Medium | Small |
| 6 | **Shoulder taper + hollower pocket** | `LocalHeight`/`UpdateSurface` | Medium: reads as a peeling wave | Small |

### 1. Age `tau` (seconds since this spot broke)
```
// peel = Mathf.Max(0.1f, st.type.peelSpeed)  (st.type is already the Scaled copy)
Right : tau = (z - BreakPoint(st,-1)) / peel        // broken side is higher z
Left  : tau = (BreakPoint(st,+1) - z) / peel
Peak  : tau = (st.breakDistance - |z|) / peel
tau = clamp(tau, -1, 20);  // <0: not broken yet
```
- Keep `broken` (sharp, 1 m soft) for foam gating and the pocket. Add a **slower height blend**: `hb = broken * Smooth(0, 1.2, tau)`. The face then slopes down over ~1.2 s of crest = 5-7 m, instead of a 2 m cliff [E].
- Broken height multiplier (fraction of `st.amplitude`), with `ww = whitewashHeight`:
`boreMult(tau) = 0.6*ww + 0.4*ww*exp(-tau/6) + (0.75 - ww)*exp(-tau/1.2)`  [E]
  (for ww = 0.45: 0.75 at tau 0 = foam ball; 0.53 at 1.2 s; 0.34 at 6 s = soup.) Today it is a flat 0.45 forever.

### 2. Bore shape (blend two shapes per vertex)
```
shapeG = d<=0 ? colBackG[c]               : exp(-(d/frontG)^2)   // as today, frontG = frontWidth (NO 1+0.8*broken)
shapeB = d<=0 ? exp(d / boreTail)  (cache per column as colTail) : exp(-(d/boreFront)^2)
boreFront = Mathf.Clamp(0.45*frontWidth, 0.9, 1.4);  boreTail = 3.0 (Beginner 2.5)   // [E], new serialized fields
h += edgeScale * Lerp(ampG*shapeG, ampB*shapeB, hb)
ampG = st.amplitude * shoulder * (1 + pocket);   ampB = st.amplitude * boreMult(tau)
```
- Front trough (line 482): keep, but use green front and `(1 - hb)`.
- Perlin lumps (line 487): scale by `exp(-tau/4)` so the soup goes flat.
- `beachBreak` should shrink the tail (`boreTail *= 1 - 0.5*beachBreak`), not double `back` (remove `(1 + beachBreak)` at line 531 for the broken shape; the green back may still grow).
- **Player sees:** a green face that collapses into a short, steep white wall, then a flat, bumpy soup behind it, instead of a big dome.

### 3. Foam by age (replaces the `crest`/`trail` terms, lines 495-498 and 645-650)
```
ball  = (1 - Smooth(0.8, 1.6, tau)) * exp(-((d-0.4)/1.4)^2)             // fat white ball, only while fresh
front = 0.85*(0.4 + 0.6*exp(-tau/4)) * exp(-((d-0.2)/0.9)^2)            // narrow band riding the bore front
trail = 0.45 * exp(-tau/3) * (d<0 ? exp(d/1.8) : 0)                     // short tail (was 7 m)
fresh = broken * max(ball, front, trail) * strength
strength = clamp01(amp/2) + (closeout ? 0.3 : 0.2)                      // was +0.7 / +0.3
```
- Let `foamMemory` (line 504) alone make the long lace; it already leaves ground-fixed foam. If it looks too thin raise `foamLinger` to ~4-5 s rather than widening the trail.
- Spray: only emit where tau < 1 s (near the break), and use `rate * 0.3` for the beach/closeout case in `EmitSpray` (line 747-752).
- **Player sees:** a bright white ball at the break, a thin white edge on the moving bore, and lace staying in place behind. Mostly water-colored between.

### 4. Beginner waves: small soft spilling rollers
Keep `closeout = true` for gameplay (`TryGetPocket` returns false so beginners ride the foam straight in; `Surfer.cs:1062`, `679`).
- Replace the uniform ramp (`closeoutAmount`, line 535) with an **unzip**: store `closeoutAge = t - breakTime` in `WaveState`, pick 2 seed points at about ±W/4 (jitter with a hash of `id`), and set `tau(z) = closeoutAge - minSeedDist(z)/6` m/s, `broken = Smooth(-0.1, 0.1, tau)`. A 30 m pool then goes white over ~3-4 s in two patches that run together, not all at once [E].
- Beginner numbers: `whitewashHeight` 0.4, `strength` +0.3 max, `boreFront` 1.4, `boreTail` 2.5 (a soft 1.5 m deep roller), foam ball 0.6 of height (use a per-type `ballHeight`).
- **Player sees:** a small green swell that spills from two points and runs sideways into a knee-high roller with a clear band of water color behind it.

### 5. Beach: reform, then swash
- For Intermediate and Pro: stop forcing `broken = 1` from `beachBreak`; instead keep shrinking amplitude with `breakEndScale`, delay `beachBreakStart` to ~0.85 and let the shoulder settle to ~50% with no foam (reform, [wave-pool-look.md](wave-pool-look.md) #7).
- Add a foam **swash band** near the waterline (x about 0.85-0.9 of length, band 1.5 m [E]) that brightens when `beachBreak` is high. Beginners already ride bore to the beach so keep their end white.
- **Player sees:** peelers run out to the wall as clean shoulders; only the last meters wash up white.

### 6. Shoulder taper and hollower pocket
```
ahead  = Right: bp - z | Left: z - bp | Peak: |z| - breakDistance   (>=0 on the unbroken side)
kFar   = (1 - exp(-(ahead/12)^2)) * Smooth(0, 8, breakDistance)        // 0 at the pocket, 1 far away, off before the break starts
shoulder  = 1 - 0.3*kFar              // unbroken height falls to ~70% far from the break  [E]
frontG    = frontWidth * (1 + 0.5*kFar)   // face gets softer and wider far away
pocket    = PocketMask * (1-broken) * 0.25  // was 0.15
```
- **Player sees:** the wave is tallest and steepest next to the white, and mellows toward the far wall (also good for beginner-friendly "easy side").

### Checks before calling it done
- Surf: `SurfaceHeightAt`, `AddLipStrip`, `EmitBlob`, NPC takeoff (`Surfer.cs:679` uses `TakeoffX + 1.5`) and beginner whitewash riders all read `LocalHeight`. A thinner bore changes where boards sit: playtest that beginners and the player still ride ON the bore front, not sunk behind it.
- Add a debug test that `LocalHeight` equals the `UpdateSurface` vertex height at ~100 random points.
- Screenshots (`EditorTools.DevShots.Shot`, async shader compile OFF) of: Starter pool Beginner at 1 s, 4 s, 8 s after the break; Intermediate peel at 2 s and 6 s; Pro at the same; the beach end.
- Count white pixels behind the crest before/after as a rough "too much white" measure [E, my idea].

## Ideas for our game
- **Step 5 polish, do first (1 session):** Part 3 #0 + #1 + #2 + #3 together. They are one idea ("broken water has an age"), and #0 is the safe first commit.
- **Step 5 polish:** #4 Beginner unzip, since Beginner waves are the first thing a new player sees (Starter pool).
- **Step 5 polish:** #6 shoulder taper and #5 reform/swash; both read as "real peeler" in screenshots.
- **Step 5 polish (cheap and funny):** a `WaveShape.Mellow` wave uses a long soft tail and a 0.5 ball so you can SEE the difference between Mellow / Standard / Hollow (ball 0.5 / 0.75 / 1.0, bore front 1.4 / 1.0 / 0.9).
- **Step 4+ growth:** a "Reform Reef" upgrade that unlocks the reform shoulder in the Reef/Lagoon (sells as "second wave" to Intermediate surfers).
- **Later:** per-pixel foam in the shader from `tau` (needs the GPU port in [wave-rendering.md](wave-rendering.md) #11-12), and a persistent foam grid so surfer wakes and bore soup share one texture.
