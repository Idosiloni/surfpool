# Wave look 2: dark face, bright lip, real barrels, giant waves, green beginner waves
Date: 2026-10-21

Ido's ask: (1) darker face + brighter lip, (2) real barrels, (3) even bigger waves, (4) beginner waves that keep a little green face.
Builds on [wave-breaking.md](wave-breaking.md) (age-based whitewater, already in the code now), [wave-rendering.md](wave-rendering.md), [wave-pool-look.md](wave-pool-look.md), [wave-physics.md](wave-physics.md), [wave-types-real.md](wave-types-real.md), [surf-maneuvers.md](surf-maneuvers.md). Not repeated here. No game files were touched.
Tags: **[S]** = from a linked source. **[E]** = my estimate or tuning guess, tune by eye. **H** = wave height. Honest gap: I found NO source with measured tube sizes, lip thickness, or lip hang-time for pool waves, and no technical write-up of the wave look in Riders Republic, A Short Hike, Piper or Moana. Those numbers are [E], built from physics I can do by hand.

## Part 1. Color and light of a real wave
### Why it looks the way it does
- **Dark lower face.** Two reasons. (a) Thick water soaks up light: pure water absorbs red fast (about 0.34 per metre at 650 nm [S: [Pope and Fry style table](https://www.osapublishing.org/ao/upcoming_pdf.cfm?id=348325)]), so through 1-2 m of water you see a deep blue-green, not a bright one. (b) The face leans away from the sky, so it mirrors less sky than a flat surface. Painters say the same: "water is more reflective the more glancing the angle", so tilted-away parts and tilted-toward parts differ ([AJNR water paper](https://www.ajnr.org/content/ajnr/42/12/2109.full-text.pdf)). The trough also sits in the shadow of the next wave ([painting guide](https://scienceinsights.org/how-to-paint-ocean-water-waves-color-light/)).
- **Bright translucent top third / lip.** There the water is thin (under about 0.3 m), so light goes through it. With the sun behind the wave you see that light glow turquoise; painters mix phthalo green with a little yellow for it and use it only on thin top areas ([painting guide](https://scienceinsights.org/how-to-paint-ocean-water-waves-color-light/)). The fine print is [E]: the exact blue-vs-green reason mixes absorption, scattering and bubbles, and one page warns that thick sea water is not a clean green filter ([SDSU](https://aty.sdsu.edu/explain/waves/waves.html)). For a game, "thin = bright aqua-green, thick = dark teal-blue" is the rule that reads.
- **Sky on the face.** Flat/gentle parts (crest top, flat water) pick up sky color; steep faces show the water's own color ([painting guide](https://scienceinsights.org/how-to-paint-ocean-water-waves-color-light/)). So: do not tint the steep face with sky; tint the flat water and the crest top.
- **Lip edge.** Where the sheet is thinnest it breaks into spray: a feathered white edge, then bright pale-green just behind the edge, then the colored sheet. Foam shading: cool grey-blue shadows, slightly warm off-white mid-tones, pure white only on sunlit tops ([painting guide](https://scienceinsights.org/how-to-paint-ocean-water-waves-color-light/)).
- **Shadow under a thrown lip.** The lip blocks the sun from the face and the tube floor. Result: the darkest, most blue-black part of the whole wave is the pocket right under the lip, while the lip curtain itself is the brightest green. High contrast between them is what makes a barrel read. [E, from the physics above and surfers' "green room" talk]
- **Foam lines down the face.** On a spilling wave the foam falls from the crest in streaks that run DOWN the slope (and spray feathers off the top in offshore wind). [E, matches [Surf Simply on spilling](https://surfsimply.com/magazine/why-and-how-waves-break)]
- **Sun direction.** Sun behind the wave (you look toward the sun through the wave): glow is strongest, face dark, lip blazing, foam rimmed bright. Sun behind you: face reflects sky, looks flatter and paler, glow nearly gone, glitter on the face. Sun to the side: one half of each bump lit, other half dark (good for showing the shape). [E]

### Approximate colors (sRGB hex) [E, starting points to tune by eye]
| Where on the face (0 = trough, 1 = crest) | Clear pool water | Tropical lagoon water |
|---|---|---|
| Trough / pocket shadow, 0-0.15 | `#0A4A5E` | `#064B5C` |
| Lower face, 0.15-0.45 | `#0E7F92` | `#0A8E9C` |
| Mid face, 0.45-0.70 | `#19B0B0` | `#12B8B0` |
| Upper face, 0.70-0.90 | `#4FDDB8` | `#44E6B6` |
| Thin top / lip body | `#A8FFD9` (sun-lit), add yellow `#CFFFA0` where thinnest | `#9CFFD0` |
| Lip edge (spray) | `#F2FFFA` | `#F2FFFA` |
| Shadowed foam | `#9FB8C8` | `#9FB8C8` |
| Flat water (sky tint) | `#2AA6D8` | `#22C0D0` |
Today's shader: face `#1FC2D1`, deep `#0A5CB8`, glow `#73FFD1`. The face color is already a bright cyan, which is why there is little contrast with the lip. Darkening the BOTTOM is more important than brightening the top.

### How stylized games and artists do it (what I could confirm)
| Source | Recipe | Confidence |
|---|---|---|
| Painting guides | Dark base from ultramarine + a touch of green (never black), mid cerulean/phthalo on the face, translucent green-yellow only on thin top areas, sky colors on horizontal parts, vertical faces keep water color ([guide](https://scienceinsights.org/how-to-paint-ocean-water-waves-color-light/)) | Yes |
| Sea of Thieves | Depth-based color lerp from the scene behind to water color with exponential falloff in the community recreation ([80.lv](https://80.lv/articles/creating-a-3d-water-shader-in-unity-inspired-by-sea-of-thieves/)); Rare's own talk covers a compositing pass doing translucency and fog ([SIGGRAPH 2018](https://history.siggraph.org/wp-content/uploads/2022/09/2018-Talks-Ang_The-Technical-Art-of-Sea-of-Thieves.pdf)). Water color by thickness = our need | Partly (summaries only) |
| Wind Waker style | Flat color bands + scrolling foam texture; edge foam via a moving mask ([Godot shader](https://godotshaders.com/shader/wind-waker-water-no-textures-needed/), [Unity forum](https://discussions.unity.com/t/the-legend-of-zelda-wind-waker-styled-water/567867)). Commercial toon water lists "wave steepness coloring" as a feature | Community only |
| Horizon Forbidden West | Artist-authored breaking-wave shapes, not simulated ([Guerrilla](https://advances.realtimerendering.com/s2022/SIGGRAPH2022-Advances-Water-Malan.pdf)) | Partly |
| Riders Republic, A Short Hike, Piper, Moana, ArtStation low-poly | No usable technical source found. Not filled from memory | No |
My reading: every working recipe is the same three layers: **dark color by thickness, a bright rim where it is thin, and white only where it is broken.**

### Recipe: face gradient by height fraction [E]
```
hf = clamp01(heightAboveRest / waveAmplitude)          // 0 at flat water, 1 at crest (needs a new value, see Part 5 #1)
col = lerp(trough,  lower, smoothstep(0.00, 0.30, hf))
col = lerp(col,     mid,   smoothstep(0.30, 0.60, hf))
col = lerp(col,     upper, smoothstep(0.60, 0.85, hf))
col = lerp(col,     lipC,  smoothstep(0.85, 1.00, hf) * backlit)   // bright only when sun is behind
steepMask = saturate((1 - normal.y) * 2.2)             // already in the shader; keep: flat water keeps the pool color
```

## Part 2. Barrels
### Shape over time (plunging wave) [S where marked, otherwise E]
| Phase | What you see | Numbers [E unless marked] |
|---|---|---|
| 1. Steepening | Face goes vertical, trough dips in front ([wave-rendering.md](wave-rendering.md)) | 1.5-3 s before the break at a spot |
| 2. Pitch starts | Top edge leans out, no gap yet | Starts about 1.5 s of peel before the landing, i.e. 5-9 m of crest ahead of the break point at 3.2-6 m/s peel |
| 3. Throw | Lip sheet leaves the crest, curved like a thrown ball | Reach 0.5-1.1 H (Wikipedia calls it "the lip", HAL lab confirms plunging jets: [Breaking wave](https://en.wikipedia.org/wiki/Breaking_wave)). The sheet is thin at the tip, thick at the root |
| 4. Airborne | Tip falls | Free-fall time t = sqrt(2h/g): a 2.8 m wave (tip falls about 2 m) = 0.64 s; a 5 m wave (about 3.5 m) = 0.85 s. So about 0.6-0.9 s of air at any single spot, and the whole curl is visible for 1-1.5 s |
| 5. Landing | Tip hits the trough, throws a splash-up and a puff of air and mist out of the tube mouth (the "spit") | Splash 0.3-0.4 H; spit puff lasts 0.5 s |
| 6. Foam ball | Fat white mass where the lip landed, tube behind it | 0.7-1 H tall ([wave-breaking.md](wave-breaking.md)) |
| 7. Tube life | The tube is a moving hole: opens at the front, collapses behind into the ball | Tube length along the crest about 1-2 H |

- **Lip thickness.** Pool-type thin lip: about 0.08-0.15 H at the root, about 0.02 H at the tip. Teahupo'o is the thick extreme: scientists describe "the overturning lip being half the wave height" and a deeper trough in front ([Mapping Ignorance](https://mappingignorance.org/2024/07/31/anatomy-of-a-wave-the-olympic-surf-break-at-tahiti/)); waves 2-5 m, periods 14-20 s. A thick lip throws a thick "pillar" and blasts air out. [S for Teahupo'o; pool values E]
- **Tube opening.** A round tube is as wide as tall; "square" or wide tubes are about 1.5x wider than tall [E]. A surfer needs about 1.3-1.6 m of height crouched, so on a 2.8 m wave the opening is about 0.5 H tall (about 1.4 m) and 1.0-1.5 x that wide (1.4-2.1 m). That is tight: it is why Pro waves feel like barrels and Intermediate waves do not.
- **Barrel parts** (names for the game): lip/throw, curtain (the falling sheet seen from inside), eye (the opening you aim for), pocket (the hollow section), foam ball, spit. See [surf-maneuvers.md](surf-maneuvers.md).
- **Why steep reef waves barrel:** steep bottoms make waves break in shallower water compared to their height, which keeps them intact and hollow ([Bali Surf Camp FAQ](https://www.balisurfingcamp.com/blog/surf-science-faq); Iribarren 0.4-2 = plunging, see [wave-rendering.md](wave-rendering.md)).

### What makes a barrel READ
| View | What matters | Fix |
|---|---|---|
| From the deck / far camera | Silhouette and contrast. A dark hole (tube mouth) under a bright curved sheet with a white tip; the sheet must visibly separate from the face by a dark gap | Dark pocket under the lip, bright curtain, white tip, visible gap |
| Surfer's chase cam (behind and above, looking along the wave) | You look along the tube from behind: you see the back of the lip (the wave's top surface) and then the curtain curving over the left/right of the surfer, and the opening far ahead | Curtain must not block the screen: dither-fade lip triangles near the camera (screen-door discard, keeps water opaque); keep the brightest area ahead so the eye goes there |
| First-person (in the tube) | Curtain glow + dark floor + the bright eye ahead | Inside color dark teal with backlight glow added; see recipe below |

### How games fake it [S/E]
- A separate curled mesh sheet over a height-field surface (what we have); Horizon uses authored shape quads played back by a parameter ([Guerrilla](https://advances.realtimerendering.com/s2022/SIGGRAPH2022-Advances-Water-Malan.pdf)). Normals from the sheet, back faces = tube inside, thickness stored per vertex and used for glow, dark ambient inside. A height-field can never overhang, so the lip sheet MUST carry the barrel. [wave-rendering.md](wave-rendering.md) says the same.

### Our lip today (read from code)
`AddLipStrip`: 20 x 10 grid, `lipLength` 6 m along the crest, reach `lengthA` x amplitude from 0.05 up to 1.1 H, hook 10 to 125 degrees, tip foam only. Gaps against the real thing: the sheet is ONE layer (zero thickness, flat-looking), the root is at `center - 0.3` so there is no gap behind the lip, the inner side is a flat tube color, nothing happens when it lands, and `lipLength` 6 m is fixed (should follow peel speed x 1.5 s).

## Part 3. Bigger waves (3-6 m)
### What exists in real life
| Item | Number | Source |
|---|---|---|
| Highest wave pool wave (Guinness) | **3.769 m**, Surf Abu Dhabi, 6 Oct 2025 | [Guinness](https://www.guinnessworldrecords.com/world-records/76407-highest-wave-pool-wave) (page does not say how) |
| Surf Abu Dhabi | Foil (underwater wing) pulled down a ~700 m long pool, shaped floor makes it break; one wave every 3-4 minutes; waves reported 2.5-3 m, 100 m long | [Wave Pool Mag](https://wavepoolmag.com/how-the-kelly-slater-wave-pool-works/), [NSS Sports](https://www.nss-sports.com/en/news/40414/surf-wave-pool-how-do-they-work), [Arab News via search](https://arabnews.com/node/2590172/sport) |
| Surf Lakes (company claim, test site) | 2.4 m face, 200 x 150 m lake, up to 10 m deep at the core; 2.4 m was a safety limit from engineering models | [Tracks](https://tracksmag.com.au/surf-lakes-hits-maximum-target-wave-height-529934), [Wave Pool Mag](https://wavepoolmag.com/first-waves-roll-out-at-surf-lakes/) |
| Indoor record | 3.2 m, Chimelong Spaceship, 2023 | [Guinness](https://www.guinnessworldrecords.com/world-records/730581-largest-indoor-artificial-wave) |
| Cove (Wavegarden) | 0.5-0.6 m beginner to about 1.7-2 m advanced; some plans up to 2.4 m | [Wave Pool Mag](https://wavepoolmag.com/?p=3488), search summary |
| Ocean | Teahupo'o 2-5 m; Nazare claimed record 28.5 m (disputed, unofficial) | [Mapping Ignorance](https://mappingignorance.org/2024/07/31/anatomy-of-a-wave-the-olympic-surf-break-at-tahiti/), [Wikipedia](https://en.wikipedia.org/wiki/Praia_do_Norte_(Nazar%C3%A9)) |
Limits (my reading): a wave needs about 1.3 x its height in depth to break (rule in [wave-breaking.md](wave-breaking.md)), so a 3.8 m wave wants about 5 m of water at the break; and power per wave grows with H squared or worse, which is why Abu Dhabi is one wave every few minutes. A game wave beyond 3.8 m is fantasy: label it so.

### What changes with size [E, reasoning shown]
| Thing | Small (1-1.5 m) | Big (3-6 m) |
|---|---|---|
| Wave speed | slower | Speed ~ sqrt(g x depth): 2 m wave breaks in ~2.6 m water = 5 m/s; 5 m wave in ~6.5 m = 8 m/s. Height x2.5, speed only x1.6, so a big wave looks SLOWER and heavier |
| Fall time of the lip | 0.45 s | 0.85+ s (see Part 2). Scale time with sqrt(H ratio) |
| Lip thickness | thin sheet 0.1 H | thick pillar 0.2-0.3 H (Teahupo'o extreme = 0.5 H [S]) |
| Face color | bright aqua | deeper and darker lower 60%, bright band only on top 10% |
| Foam ball | knee-high | tall, white, boils for 3-4 s; spray rises higher and hangs longer |
| Spit | small puff | long jet, 1-2 s, with mist |
| Sound | crack | low boom + long hiss (pitch down, length x1.5) |
| Face texture | fine chop | chop is the same SIZE in meters so it looks finer; add big slow bumps |
| Camera | steady | small shake on landing, 0.1-0.2 s |

### An "XL / Giant" wave option [E]
- **Where:** only pools deep enough and with strong machines: Big Wave Lagoon and a late Surf Ranch-style pool. Not Cove/Starter/Reef.
- **Numbers:** keep Pro at 2.8 m. Lagoon `sizeMultiplier` 1.25 gives 3.5 m today. Add presets on the Wave Console: **Big** = 3.5 m (x1.25), **Giant** = 3.8 m (x1.36, matches the world record: nice fact for a tooltip), **Mega** = 5 m (x1.8, labeled fantasy, late game). Speed x sqrt(size ratio); lip fall gravity scaled so the throw stays readable (0.9 s).
- **Who can ride:** skill gate by the wave's level, same idea as `WaveWater.LevelOf`. Only Pro surfers and famous surfers; everyone else stays out or sits on the channel. Player needs a **Gun** board (long, narrow; add as `BoardKind` in `SurfQuiver`) for Giant+.
- **Risk/reward:** longer hold-down wipeouts, board snaps (`BoardBreak`) more likely, bigger tips and S-photo odds (`PhotoTower`), big crowd on the beach (Cove-style spectators). Power bill higher (`OperatingCosts`, per wave about x3 for Giant, [E]).

## Part 4. Small spilling (beginner) waves
- **Real small pool waves.** Cove "Bay" zone: reformed, smooth whitewater about 0.6 m high ("Perth plan" via search results); beginner insides are gentle knee-high rolling whitewater and the intermediate zone is waist-high with a green face; Bays are fed by the reform of the advanced Reef wave ([Wave Pool Mag search](https://wavepoolmag.com/?p=3107), [Wavegarden Costa del Sol](https://raisedwaterresearch.com/spot/wave-pool/europe/spain/wavegarden-costa-del-sol/)). Endless Surf makes its beginner wave by reforming the whitewash of the expert wave ([Wave Pool Mag](https://wavepoolmag.com/how-endless-surf-works-with-engineer-clement-ginestet/)). I could not find any description or photo analysis of how much green stays visible. Everything below is [E] from the spilling-breaker physics (foam falls from the top down the face, wave stays mostly green below it: [Surf Simply](https://surfsimply.com/magazine/why-and-how-waves-break)).
- **Three bands, top to bottom, on a freshly spilling small wave [E]:**
  1. Top 25-35% of H: foam. Thick, white, with a wavy lower edge (tongues of foam run down the face).
  2. Next 15-20%: foam streaks and holes fading into water color, thin translucent aqua (backlit).
  3. Bottom 45-55%: clean green water. The face must look green for at least 40% of its area at the moment the spill starts.
- **Widths [E]:** foam covers from about 0.5 H behind the crest to about 0.6-1.0 H ahead of it down the face; the white roller behind it narrows to about 1.5-2 H and a low 0.3-0.5 H height; leaves gaps of water color every 1-2 m (lace).
- **Reformed roller (the beachward white):** low (0.3-0.5 H), a steep little front face (the tongue of foam is the front), a pale-aqua translucent line along its leading edge, and a flat bubbly tail behind it. Not a white block.
- **Timeline of one beginner wave [E]:** swell with full green face → 0.5 s: foam appears at 1-2 points on the crest → 1.5 s: foam tongues reach down to mid face → 3 s: the green bottom is gone at those spots and a roller forms → 4-6 s: the unzip runs sideways and the whole wave is a roller with a bright front rim.

## Part 5. Recipes for OUR code (ranked by impact)
State of the code today: `Sample()` already returns face brightness G (`lipG` pow 2.5, only near the crest), `spillShape` (foam sits on the crest, face stays green), age-based bore. So most gains are in the **shader** and in one new vertex channel. Rule from wave-breaking.md: `LocalHeight` and `UpdateSurface` must stay identical.

| # | Change | Where | Numbers [E] | What the player sees |
|---|---|---|---|---|
| 1 | **Height fraction `hf` to the shader.** Write `uv0 = (heightAboveRest, wave amplitude)` for every water vertex (second `SetUVs` channel; the sum over waves uses the tallest wave). In the shader `hf = saturate(uv.x / max(uv.y, 0.3))` (flat water: hf = 0) | `WaveWater.UpdateSurface` + `Update`, `StylizedWater.shader` | Cost: 2 floats per vertex (8 B). The shader reads it with a `TEXCOORD1` | Enables #2, #3, #7. Nothing visible alone |
| 2 | **Dark lower face + bright upper third gradient** (recipe in Part 1). Replace `water = lerp(deep, _FaceColor, steep)` by the 4-stop gradient driven by `hf` x `steepMask`. New properties: `_FaceDark #0A4A5E`, `_FaceLow #0E7F92`, `_FaceMid #19B0B0`, `_FaceUp #4FDDB8`, `_LipColor #A8FFD9`. Keep the pool color on flat water | Shader | Dark band 0-0.15 H, lower 0.15-0.45, mid 0.45-0.7, upper 0.7-0.9, lip 0.9-1.0. Raise the sun glow only above hf 0.6 | The "dark face, bright top" Ido asked for: wave reads as a thick body with a glowing top |
| 3 | **Beginner keeps a green face.** Foam mask by height: `foam *= smoothstep(0.55, 0.8, hf)` for closeout waves (Beginner) while the spill is young (`age` < 2 s); then relax to `smoothstep(0.1, 0.4, hf)` over 2-5 s as the roller forms. Plus foam tongues: stretch foam noise along +x (down the slope) by 2.5x on the face, fade with `1 - smoothstep(0.3, 0.6, hf)` | Shader (+ already-existing `A` freshness) | Foam occupies the top 30%, streaks to mid-face, bottom 45% green; fresh = bright rim, old = lace | Beginner waves are a green swell with a foam crown, not a white sheet |
| 4 | **Lip rim light and white feathered edge.** In `AddLipStrip` write thickness into B or A: `thick = 1 - u` (1 at root, 0 at tip). Shader: lip body = `_LipColor`, plus `glow * thick^0.6 * backlit`; last 8% of the sheet = foam with noisy cut edge `step(noise, 1 - u)` (dithered edge, stays opaque); add a slight yellow `#CFFFA0` at thinnest. Add bloom threshold just under the lip color so it glows | `AddLipStrip` vertex colors, shader | Edge band 5-8% of the sheet; yellow tint at thick < 0.2 | A glowing, thin, feathered lip |
| 5 | **Barrel interior and shadow.** Backfaces (already detected by `inside`): base `#073A4A` instead of mixing at 0.7; add `backlit * thin * _GlowColor` so the curtain glows when the sun is behind. Darken the face under the lip: lip mesh writes a "shade" value on the SURFACE, i.e. in `Sample()` darken `hf` by `pocketShade = PocketMask * (1-broken) * 0.5` so G goes negative under the lip, shader subtracts it from the gradient | Shader, `Sample()` | Pocket shade -30% brightness over 2 m under the lip | A dark mouth under a bright curtain |
| 6 | **Real tube shape.** Two-layer lip: (a) outer sheet as now (thin), (b) 2nd offset sheet at +0.06 H thickness at the root, shrinking to 0 at the tip, joined at the edge; set root at `center + 0.2` not `center - 0.3` so there is a dark gap between lip and face; `lipLength = peelSpeed * 1.5 s` (about 5-9 m); lip reach 0.7 H for Intermediate "soft lip" and 1.1 H for Pro; thick-lip option (0.2 H root) for Hollow shape and Giant. Opening: keep tube height 0.5 H (trough dip + lip reach) | `AddLipStrip`, `LipShape` | Pitch starts 1.5 s ahead; fall 0.6-0.9 s | Wave looks like it has depth: tube, gap, thickness |
| 7 | **Landing: splash-up, spit, foam ball sync.** At lip landing (`tau` 0.8): add the splash bump `0.35 * A` over 0.8 m, a burst of 6-10 mist puffs, a short spray jet toward +x (spit) for 0.5 s, foam ball starts there. Sound `CrashMedium` pitch by H | `Sample()`, `EmitAtBreak`, Sfx | Burst once per spot | Barrel closes with a punch |
| 8 | **Chase-cam see-through.** Dither discard in lip fragment when within 6 m of the camera (`clip(noise - fade)`) so the player still sees the surfer through the curtain | Shader (lip only; keyword or `_LipFade`) | fade over 6-3 m | The curtain stops blocking the view |
| 9 | **XL waves.** Add `WaveSize { Standard, Big, Giant, Mega }` on the console: multiplier 1 / 1.25 / 1.36 / 1.8 over Pro, speed x sqrt(mult), lip root 0.2 H and reach 1.1 H at Giant+, foam ball 1 H, spray lifetime x1.4, camera shake 0.15 s, boom sound pitch 0.8, gun board needed | `WaveMachine`, `WaveWater` type scale, `SurfQuiver`, `Surfer.DoTrick` gates | See Part 3 | A visibly heavier wave, not just a scaled one |
| 10 | **Sun-direction cue.** Shader already uses `dot(viewDir, -light)`. Scale `behind` by 1.4 and add a soft rim (+10% lip color) at sunsets (low sun) so late-day sessions look better | Shader | -- | Golden-hour glow on the face |

### Checks before calling it done
- Screenshots (`EditorTools.DevShots.Shot`, async shader compile OFF) from the deck and the chase cam, sun behind and sun in front, for Beginner at 0.5, 2, 5 s; Intermediate; Pro at barrel; Giant. Compare against Part 1 table colors.
- White-pixel count behind the crest on Beginner (target: green visible on 40%+ of the face at the spill start).
- The ocean uses the same material (see [wave-rendering.md](wave-rendering.md)): keep the gradient pool-only (a keyword) or `hf = 0` when `uv.y` is 0.

## Ideas for our game
- **Step 5 polish, first session:** Part 5 #1 + #2 (height fraction + dark/bright gradient). It is the smallest change with the biggest "wow", and #3 (beginner green face) can go in the same session since it uses the same value.
- **Step 5 polish, second session:** #4 + #5 (lip glow, feathered edge, dark pocket). Then #6 (two-layer lip, gap) and #7 (landing splash and spit).
- **Step 5 polish:** #8 chase-cam dither, so barrels look good AND playable.
- **Step 4+ growth:** XL waves (#9) as an upgrade chain for Lagoon / Surf Ranch: "Big Wave Pack" (3.5 m), "Giant" (3.8 m = world record, show it in the upgrade card), "Mega" (5 m, fantasy), each with a power bill and a skill gate. Gun board in the shop as the key to riding them.
- **Later:** "Golden hour" events that tint the face (sun low), per-wave sun glow in photos (PhotoTower scores the lip glow), a "world record wave" daily event using the 3.769 m fact.
