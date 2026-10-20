# Making the wave and water look great (stylized breaking waves, real-time)
Date: 2026-10-20

Builds on [wave-physics.md](wave-physics.md) and [lowpoly-assets.md](lowpoly-assets.md). Read-only research: no game files were touched.
Tags: [E] = my estimate (profile it in the Unity Profiler before trusting it). (guess) = my design suggestion, tune by eye. "verify" = link not opened or license not confirmed.

## TL;DR (top changes, in build order)
1. **Soft billboard spray** instead of 768-triangle white spheres (cheap, instantly nicer).
2. **Foam age + "lace" dissolve** using the unused vertex-color B channel (foam breaks into holes as it ages).
3. **Front trough + steeper face** in `LocalHeight` (the wave gets a real "hollow" in front).
4. **Lip v2:** a lip that curls over time, with inside/outside colors and a fake glow (SSS) where it is thin.
5. **Water v2 shader:** depth color, refraction, wall foam (URP depth/opaque textures are already ON in `PC_RPAsset`).
6. **Move height + foam to the GPU**, then raise vertex density and compute foam per pixel.

## 1. What our code does today (read from the files)
| Part | What it is | Limit that shows on screen |
|---|---|---|
| `WaveWater.cs` | Grid mesh, 3 verts/m. Every `Update` loops every vertex: `LocalHeight` (Gaussian bump, front width < back width, ripples, Perlin lumps in whitewash) + `Foam()` -> `Color[]`, then `RecalculateNormals()` | Face is only ~4 vertices wide (0.33 m cells vs 1.4 m front width) so it looks faceted. Heightfield = cannot overhang. |
| Mesh size | 60x24 m = 181x73 = ~13k verts per pool; 3 pools (Main grows 40x14 -> 50x19 -> 60x24; Reef and Lagoon are full size) -> up to ~40k verts updated on CPU **every frame, even for idle pools** | [E] 1-5 ms per pool in Mono (not profiled). **Check the Profiler first.** |
| Vertex color | R = foam, G = face brightness. **B and A are unused** | Free channels for foam age, lip thickness. |
| `BuildLip` / `AddLipStrip` | Separate mesh, 18x10 per break point. Circular arc, radius 0.42 x amplitude, up to 145 degrees. Curl ramps up for 1.5 m of peel, then **never changes** | One thin sheet, no time evolution, no impact splash. Same material as water. |
| `EmitBlob` / `BuildSpray` | `ParticleSystem` Mesh renderer, Unity sphere mesh, lit `Foam.mat`, max 4000 | [E] Unity's built-in sphere is ~768 triangles, so 1000 live blobs ~ 770k triangles. Lit white balls look like ping-pong balls. |
| `Shaders/StylizedWater.shader` | **Hand-written HLSL, not Shader Graph.** Opaque, Queue Geometry, Cull Off. Deep/face color by `1 - normal.y` + G; foam = R x value-noise (world XZ, 2 layers) with `smoothstep`; 2-step toon light; hard sparkle; weak fresnel. No depth, no refraction, no vertex displacement (a `DepthOnly` pass exists, flat) | Water is a flat opaque color: you never see the tiled floor, legs, or board under the surface. |
| Other Shader Graph files | Only 4 TextMeshPro graphs in `Assets/TextMesh Pro/Shaders` (text rendering). **No game Shader Graph.** | We should keep the water as text HLSL: plain text the agent can edit, diff and review. |
| Material sharing | `WorldArt.BuildEnvironment(sandMat, waterMat)` gives the **1600 m Ocean** (80x80 grid, 20 m cells, black vertex colors) the **same** `StylizedWater.mat`. The lip mesh also uses it | Every shader change also hits the ocean and lip. Use a keyword/second material (`StylizedWater_Ocean.mat`) for pool-only features. |
| URP settings | `PC_RPAsset`: Depth Texture ON, Opaque Texture ON, HDR ON, MSAA 1 (off-ish). `Mobile_RPAsset`: both OFF | Depth/refraction need no settings change on the PC asset. Check which asset the Mac quality level uses. |

## 2. Breaker physics that matters for looks
### 2.1 Which breaker? (Iribarren number)
- **Iribarren number** (ξ, "surf similarity"): ξ = tan(β) / sqrt(H / L0), where β = bottom slope, H = wave height, L0 = deep-water wavelength = g T² / 2π. It compares how steep the ramp is with how steep the wave is. Source: [Coastal Wiki](https://www.coastalwiki.org/wiki/Surf_similarity_parameter).
- Cutoffs (Battjes 1974, via Coastal Wiki): **ξ < 0.4 spilling; 0.4 to 2 plunging; > 2 collapsing/surging.** Order of magnitude only: other sources note the types blend gradually and ξ alone does not classify perfectly ([Wikipedia](https://en.wikipedia.org/wiki/Iribarren_number)).
- Worked examples (my arithmetic, T = 6 s so L0 = 56 m):
| Wave | Ramp | H | ξ | Look |
|---|---|---|---|---|
| Beginner | 1:20 (tan 0.05) | 1 m | 0.37 | spilling: foam trickles from the crest |
| Intermediate | 1:10 (0.10) | 1.5 m | 0.61 | plunging-ish: steep face, short lip |
| Pro | 1:8 (0.125) | 2 m | 0.66 | plunging: lip throws, barrel |
| Very steep ramp | 1:4 | 1 m | 1.9 -> > 2 | collapsing: face falls in, no clean lip |
- Game mapping (guess): a per-wave number `plunge` 0..1. `plunge = 0` spilling (foam from crest, no lip), `1` full lip, back to 0 for collapsing.

### 2.2 How the face steepens and breaks
- Real wave: higher water travels faster than lower water, so the front leans forward until it is vertical and the top falls over. Crest sharp, trough flat, front steeper than back (we already have front 1.4 m vs back 9 m).
- Break rule is already in wave-physics.md (height/depth about 0.6-0.8).
- **Trough in front:** water is sucked up the face, so the surface just ahead of the face dips below rest level. This hollow is what makes a barrel read as hollow. We have none. (Wave-pool articles also describe the Cove barrel as concave with a trough, [Stab](https://stabmag.com/?p=23182).)
- **Plunging jet:** the lip is a thin sheet thrown out ahead, falling like a thrown ball; it lands in the trough ahead, trapping an air tube (the barrel) and pushing up a **splash-up**; the cycle can repeat 2-3 times, each smaller. After the collapse a horizontal vortex rolls and decays into turbulent foam ([Wikipedia: Breaking wave](https://en.wikipedia.org/wiki/Breaking_wave)). Exact jet numbers below are (guess) tuned for looks; the classic math is Longuet-Higgins' overturning-wave curves ([JFM 1982](https://data-ww3.ifremer.fr/BIB/Longuet-Higgins_JFM1982.pdf)), not needed for a game.

### 2.3 Formulas we can drop in
`A` = wave amplitude (m), `w` = front width, `d = x - center` (+d is ahead of the crest), `tau` = seconds since this part of the crest broke (we have it: `(breakDistance - |z - startZ|) / peelSpeed`).

```
// (a) Front trough: add to h, only ahead of the face, strongest right before/at the break
trough = -0.18*A * exp(-((d - 1.8*w)/(0.9*w))^2) * (d > 0) * troughAmount   // troughAmount 0..1 ramps up with plunge

// (b) Pointier crest, flatter trough: change the exponent (2 = Gaussian)
shape = exp(-pow(abs(d)/w, p));   p = 1.6 front, 2.0 back

// (c) Lean the face (Gerstner-style sideways push; visual-only, see warning)
vertex.x += lean * h(x,z);          // lean 0.2..0.5; overhang only if lean > w / (0.86*A)
// Gaussian max front slope = 0.86*A/w. For A=2.8, w=1.4: overhang at lean > 0.73.

// (d) Lip as a "curvature-profile" curve instead of a fixed circle:
//   walk along the lip in small steps ds; tangent angle below horizontal grows toward the tip
for k in 0..n:  s = k/n;  theta = thetaMax(tau) * s^p;      // p=1 circle, p>1 straight then hooks at tip, p<1 fat curled root
                pos += L(tau)*(1/n) * (cos(theta), -sin(theta));
// Keyframes (guess), L in units of A:   tau(s): 0 -> L 0.05, thetaMax 10deg
//                                              0.2 -> 0.35, 40deg
//                                              0.5 -> 0.80, 90deg
//                                              0.8 -> 1.10, 120deg (impact; then splash + collapse)
// Time compression: use a slower "gravity" (say 5 m/s^2) so the throw lasts ~0.8 s and players can see it.

// (e) Lip thickness (fake, as vertex-color A): t = 0.12*A*(1-s)^0.7 -> thin tip glows in the shader. No real volume needed.

// (f) Splash-up at impact (tau near 0.8): bump in the height function + particle burst
splash = 0.35*A * exp(-((d - dImpact)/0.8)^2) * sin(pi*clamp01((tau-0.8)/0.8));

// (g) Foam age in vertex color B (behind the crest):  age01 = clamp01(-d / (speed * foamLife)),  foamLife ~ 6 s (guess)
```
- **Warning for (c):** `SurfaceHeightAt`, `EmitBlob`, `AddLipStrip` all call `LocalHeight` as a heightfield (one y per x,z). Sideways vertex motion breaks that. Keep lean small and visual-only (don't use it in `SurfaceHeightAt`), and leave real overhangs to the lip mesh.
- For the lip to read as a tube, the water surface **under** the lip must fall away faster than the lip curves (trough (a) + steep face), or the lip floats over solid-looking water.

### 2.4 Whitewater (bore) and foam
| Thing | Real behavior | Visual rule (guess) |
|---|---|---|
| Bore speed | Broken front travels about with the wave, slows in shallow water (`sqrt(g*depth)`) | Foam front stays at the crest, trail grows behind |
| Bore height | Saturated surf zone: height about 0.5-0.8 of depth | Already have `whitewashHeight` + beach shrink |
| Foam trail | Lasts a few seconds to tens of seconds, thins into lace as bubbles pop | Intensity `exp(-age/2.5s)` AND dissolve threshold rising with age (holes grow) |
| Foam pattern | Fingers/streaks along the flow just behind the break; blobs/lace later | Young foam: noise stretched along x (flow); old foam: round blobs |
| Foam movement | Front foam moves with the wave; old foam sits still | Young foam uses coordinates relative to the wave (`x - center`), old foam uses world coordinates |
| Spit | Air squeezed out of the tube mouth blows a puff of mist ahead when the lip lands | One burst of 6-10 soft mist puffs at impact |
| Crest feathering | Wind blows spray off the top | Short streak particles trailing the lip edge |

## 3. Real-time techniques (what each one is)
Jargon once: **Gerstner wave** = sine wave that also moves points sideways, so crests sharpen and troughs widen. **Fresnel** = surfaces reflect more at grazing angles. **SSS (subsurface scattering)** = light passing into thin material and glowing out. **Soft particles** = particles that fade out where they touch other geometry (needs the depth texture). **Overdraw** = the GPU shading the same screen pixel many times (transparent particles stacking). **Flow map** = a texture of 2D arrows that tells a shader which way to scroll a pattern.

| Technique | How it works | Use for us |
|---|---|---|
| Gerstner / trochoidal waves | Position offset `P += (Dx*a*cos f, a*sin f, Dz*a*cos f)`, `f = k(D.xz - c t)`, `k = 2pi/lambda`, `c = sqrt(g/k)`, `a = s/k`, steepness `s` in 0..1, sum of steepness <= 1 or loops form. Normal from tangent x binormal. See [Catlike Coding: Waves](https://catlikecoding.com/unity/tutorials/flow/waves/) (formulas confirmed; license verify) and [GPU Gems ch.1](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-1-effective-water-simulation-physical-models) (not opened, verify) | **Good for ambient chop and the ocean** (3 waves, total steepness ~0.5). **Bad for our breaker:** it repeats everywhere forever; a pool wave is one traveling pulse that breaks at a point. Keep our pulse + break logic. |
| Crest/lip as separate mesh | Sweep a profile curve along the crest line (what `AddLipStrip` does). Horizon Forbidden West did the pro version: **artists author breaking-wave shapes (quads with an animation parameter), baked, not simulated**; the curl forms as the parameter nears 0.5 ([Guerrilla, SIGGRAPH 2022, slides could not be fully read](https://advances.realtimerendering.com/s2022/SIGGRAPH2022-Advances-Water-Malan.pdf)) | Same idea at small scale: **keyframed profiles / the curve in 2.3(d)**, time = `tau`. |
| Foam mask | Per-vertex (what we do), per-pixel analytic, or a persistent texture | See below. |
| Foam persistence texture | Sea of Thieves: foam from wave crests + depth-buffer foam around objects, **foam buffer blurred over time with feedback** to fake dispersal, blended with artist textures; more foam in storms ([SIGGRAPH 2018 talk, excerpt only](https://history.siggraph.org/wp-content/uploads/2022/09/2018-Talks-Ang_The-Technical-Art-of-Sea-of-Thieves.pdf)) | A small top-down foam grid per pool (e.g. 4 texels/m, 240x96 R8 for 60x24 m), CPU-written (crest, surfer wakes, wall hits), multiplied by 0.97/frame, uploaded as a texture. Gives surfer trails and lingering whitewater. |
| Foam dissolve | Foam = `smoothstep(threshold, threshold+edge, mask * noise)`; raise threshold with age | Already half there; add age. Add a thin darker/blue ring under the foam edge (cartoon outline) (guess). |
| Depth color | `depth01 = saturate((sceneDepthEye - surfaceDepthEye)/range)`; `color = lerp(shallow, deep, depth01)`; optional posterize steps ([Ameye](http://ameye.dev/notes/stylized-water-shader/), confirmed) | Pool is 5 m deep with tiled floor: shallow-teal near beach/walls, deep-blue in the middle. |
| Refraction | Offset the screen UV used to read Scene Color by wave normals (or scrolling noise) x strength; use undistorted UV where an object is in front of the water (Ameye) | See floor/legs wobble under water. |
| Fresnel + sky | Blend toward a sky/horizon color at grazing angles | We have a weak one; use sky color, not face color. |
| Fake SSS on thin crest | `H = normalize(L + N*0.2); glow = pow(saturate(dot(V, -H)), 4) * thickness * glowColor` (idea from Barre-Brisebois GDC 2011 translucency; I only saw it cited in [a Unity project write-up](https://gitee.com/oopenoo/UnityPlayground), verify). `thickness` = vertex color A (thin lip = 1) or crest height mask | Lip and wave top glow bright lagoon-green/yellow-green with the sun behind. Skip when sun can't reach the surface (shadowAttenuation). |
| Inside vs outside lip | `FRONT_FACE` (we already flip normals): back face = tube inside = deeper teal + soft glow; front = lighter outer face | Reads as a hollow barrel instantly. |
| Toon bands + stylized spec | `step()` the diffuse into 2-3 bands; spec = `pow(NdotH, exp2(10*s+1))` stepped ([Ameye](http://ameye.dev/notes/stylized-water-shader/)) | We have 2 bands + hard sparkle; add noise-modulated glitter so it shimmers. |
| Surface detail | Cheap fake normals: two scrolling noise layers added in the fragment shader | The mesh is coarse; this adds ripple detail without vertices. |
| Intersection foam | Foam where `sceneDepth - surfaceDepth` is small: white lines at pool walls, steps, board, ankles ([Unity nature shaders blog](https://unity.com/blog/engine-platform/nature-shaders-with-shader-graph-in-2022-lts), Linear01 depth method confirmed) | Foam line where water touches walls and the beach. Needs depth texture ON. |
| Caustics | Animated bright web pattern on what's under the water. Cheap version: in the water shader rebuild the world position of the floor from the depth texture, sample a Voronoi/noise pattern in that XZ, add it faded by depth. Or a URP Decal Projector over the floor (verify) | Shimmering lines on the pool tiles. |
| Flow map | Artist texture of arrows that distorts a tiled normal map, ~4 texels/m ([Valve, Water Flow in Portal 2, SIGGRAPH 2010](https://www.advances.realtimerendering.com/s2010/Vlachos-Waterflow(SIGGRAPH%202010%20Advanced%20RealTime%20Rendering%20Course).pdf); only search snippets read) | **Skip it.** Our flow is simple (+x, wave-relative), so offset UVs by `x - center` and scroll along +x; no texture needed. Maybe later for a river or lazy-river pool. |
| Spray | Mesh spheres (now) vs camera-facing quads with a soft puff texture, alpha fade, size over life, stretched billboards for fast droplets. Soft particles need depth texture ([URP particles docs](https://docs.unity3d.com/Manual/urp/particles-lit-shader.html)) | Billboards: 2 triangles per particle instead of ~768. Watch overdraw: big puffs stacking in first person. |
| Trails behind surfers | `TrailRenderer` with a foam streak texture (width over time, alpha fade), or write into the foam grid | Board wake and fin trail. Wind Waker boat-wake style splash meshes are only an unverified recollection from a forum, skip citing. |

### The opaque-water rule (important)
- Don't make the water alpha-blended. Lip and water both use `Cull Off`, the lip overlaps the water, and spray is transparent, so you get sorting flicker.
- Use [Ameye's](http://ameye.dev/notes/stylized-water-shader/) setup: surface stays **opaque** (ZWrite on) but is rendered in the **Transparent queue** so it is drawn after the floor, and it reads Scene Color (opaque texture) itself for "see-through". If the queue stays at Geometry, the depth texture contains the water itself and depth fade reads 0.
- The `DepthOnly` pass needs the same vertex displacement as the main pass once displacement moves to the GPU.

## 4. Games and talks
| Source | Take-away for us | Confirmed? |
|---|---|---|
| Horizon Forbidden West (Guerrilla, SIGGRAPH 2022) | Breaking waves are artist-authored overhanging shapes, baked and played back, not simulated | Partly (slide text unreadable by my tool) |
| Sea of Thieves (SIGGRAPH 2018 talk) | Crest foam, depth-intersection foam, blurred feedback foam buffer + artist textures | Yes, from slide excerpt |
| Moana (Disney, "Crashing Waves", SIGGRAPH 2017) | Film-only: procedural waves from profile curves for far shots, particle fluid solver for hero shots ([Disney](https://disneyanimation.com/publications/moana-crashing-waves/)). Lesson: art-directed profile curves | Yes, abstract level |
| Wind Waker / BotW | Foam by scrolling a texture whose UVs are warped by scrolling noise; flat bold colors. Only community breakdowns found ([Godot Wind Waker shader](https://godotshaders.com/shader/wind-waker-water-no-textures-needed/)) | Community only |
| Assassin's Creed IV | Reportedly FFT ocean + depth-based color; no talk found for foam | No primary source |
| Unity Boat Attack | Gerstner water in a local package, buoyancy with C# Jobs, caustics via a custom render pass, planar reflections. Repo: [Unity-Technologies/BoatAttack](https://github.com/Unity-Technologies/BoatAttack). **License: Unity Companion License** (Unity-dependent use, "as is"); Unity 2019.3 so needs porting to Unity 6 | Yes |
| Alexander Ameye tutorial | Best written recipe: depth color, refraction, surface/intersection foam, Gerstner, stylized spec, buoyancy ([ameye.dev](http://ameye.dev/notes/stylized-water-shader/)). **No license stated** for its `.unitypackage`/snippets: reimplement the ideas, don't copy files. His paid Stylized Water 2: store/Gumroad text says "not compatible with Unity 6 Render Graph" (verify) | Yes |
| Minions Art | Shader Graph intersection-foam + refraction tutorial, updated for Unity 6 ([Patreon](https://www.patreon.com/posts/30490169), search snippet only; paywall/license unknown) | Snippet |
| Catlike Coding | Gerstner formulas, clear math (verify license before copying code) | Yes (formulas) |
| **No technical source found** | Riptide GP, Maneater, Surf World Series, Barton Lynch Pro Surfing, True Surf, Tiny Glade, Hollow Knight, Cyanilux water, Daniel Ilett water, Ben Cloward water. I did not fill these from memory. Cyanilux's repos exist but I found no water breakdown | -- |

## 5. Performance notes (Mac / Metal) - all [E], profile in Unity Profiler and Xcode/Metal frame capture
| Item | Estimate | Advice |
|---|---|---|
| CPU mesh update (now) | 13k verts/pool x loops over waves with several `Exp`s + Perlin; plus `RecalculateNormals` | Profile first. Cheap wins: skip pools with no waves and no visible camera; use `Mesh.SetVertices(NativeArray)` + Burst job (typically 5-20x faster than managed loops). |
| GPU vertex displacement | Apple-silicon GPUs handle 100k-500k water verts easily | Move `LocalHeight` into the vertex stage; pass active waves (center, amplitude, widths, break state, 1-4 waves) as a float4 array via `MaterialPropertyBlock`/global array. Raise to 6-8 verts/m (~50-100k per pool). |
| Must keep a C# copy | `SurfaceHeightAt`, `EmitBlob`, lip, surfers need heights on CPU | One function in two languages must match exactly; add a debug test comparing both at 100 points. |
| Normals | CPU recalculation vs shader | In shader use finite differences (extra 2 height evaluations) or analytic derivative of the shape. |
| Foam per pixel | Evaluate the analytic foam mask in the fragment shader | Sharper than vertex colors at 0.33 m cells. [E] a few dozen ALU ops for 1-4 waves. |
| Opaque Texture copy | One extra full-screen color copy per frame | [E] noticeable bandwidth cost on tile-based GPUs at high res; measure with it on/off. |
| Particles | Billboards: 2 tris each. Mesh spheres: ~768 each | Cap ~1500 live billboards; limit total on-screen coverage (overdraw) rather than count. Soft particles add a depth read per pixel. |
| Shadows | Water casts none (ok); `ShadowCastingMode.Off` on water + lip | Keep it that way. |

## 6. Ideas for our game
Ranked by dependency and payoff; each is one session (about 1 coding session) unless noted. "Pool-only" means: keep the ocean on the old look via a keyword or `StylizedWater_Ocean.mat`.

| # | Build | Files | Improves | Step tag |
|---|---|---|---|---|
| 1 | **Soft billboard spray.** Generate a soft puff texture (small Python script like `tools/audio/`, or Editor code) with 2x2 puff variants; Particle renderer -> Billboard mode (Stretched for fast droplets), URP Particles/Unlit, soft particles on, size-over-life + fade-out; Alpha of start color; cap `maxParticles` ~1500. | `GreyboxBuilder.BuildSpray`, new texture in `Assets/_Game/Textures`, maybe `tools/` | Spray stops looking like white balls; far fewer triangles. [E] 1 session | Step 5 polish |
| 2 | **Foam age + lace.** `Foam()` writes B = age01 (formula 2.3g). Shader: dissolve threshold rises with age, young foam noise stretched along x and wave-relative, old foam round blobs and world-fixed; add a thin darker rim under foam edge. | `WaveWater.Foam`, `StylizedWater.shader` | Whitewater that visibly ages and breaks up | Step 5 polish |
| 3 | **Front trough + pointy crest + splash-up bump** in `LocalHeight` (2.3 a, b, f), scaled by a per-type `plunge` value (Iribarren idea: Beginner 0, Intermediate 0.5, Pro 1). Add `plunge` to `WaveType`. Keep `SurfaceHeightAt` consistent (it calls `LocalHeight`, so it moves with it; check surfer board lies right). | `WaveWater.LocalHeight`, `WaveType` | Hollow barrel look, wave type differences you can see | Step 5 polish |
| 4 | **Lip v2.** Curvature-profile lip with `tau` keyframes (2.3 d), thickness in vertex color A (2.3 e), lip length and curl evolve; at impact fire a spray burst + mist puffs ("spit") + the splash bump from #3. | `WaveWater.AddLipStrip/BuildLip/EmitAtBreak` | The lip throws, falls and lands instead of sitting still | Step 5 polish |
| 5 | **Lip shading + fake SSS.** Back-face = tube inside (deeper teal, soft glow), front = light; add `pow(dot(V,-H),4)*thickness` glow on lip and wave top where sun is behind; keyword so ocean ignores it. | `StylizedWater.shader`, `StylizedWater.mat` | Glowing green barrel, depth in the tube | Step 5 polish |
| 6 | **Fragment surface detail + toon spec polish.** Two scrolling noise normals, glitter modulated by noise, sky-color fresnel, HDR sparkle so existing bloom catches it. Ocean can share this. | `StylizedWater.shader` | Water shimmers instead of looking flat | Step 5 polish |
| 7 | **Water v2: depth color + refraction + wall/step foam line.** Move water to Queue Transparent but keep opaque + ZWrite (section 3 rule); read Scene Depth + Scene Color; shallow/deep color, UV-offset refraction, intersection foam. URP depth/opaque already ON in `PC_RPAsset`; also set them on whatever asset the Mac quality uses. Pool-only keyword. | `StylizedWater.shader`, `GreyboxBuilder.WaterMaterial` (queue, keyword), check `Assets/Settings/*_RPAsset.asset` flags in the Editor UI | See the pool floor, surfers' legs, foam lines at walls. Biggest "real water" jump. [E] 1-2 sessions | Step 5 polish |
| 8 | **Caustics on the floor** from the depth texture (rebuild floor world position, Voronoi pattern, fade with depth). Depends on #7. | `StylizedWater.shader` | Shimmering light on tiles | Later |
| 9 | **Board wake / surfer trail.** `TrailRenderer` on the board tail with foam streak texture and fade, plus bigger spray on landings (`EmitRiderSpray`). | Surfer/board scripts, trail material built in `GreyboxBuilder` or a builder helper | Surfers leave a visible track | Step 5 polish |
| 10 | **Profile + Burst the CPU mesh** (do before #11 if cost is high): skip idle/offscreen pools, `NativeArray` + `IJobParallelFor` for height + foam + colors, set bounds manually. | `WaveWater.Update` | Frees ms for everything else. [E] 1 session | Step 5 polish |
| 11 | **GPU vertex stage.** Move `LocalHeight` (and trough/pointy shape from #3) into the shader's vertex stage, plus the `DepthOnly` pass; waves passed as a float4 array; keep C# copy; normals by finite differences; add a test that compares C# vs HLSL. After this, raise `verticesPerMeter` to 6-8. Do **not** use sideways displacement until `SurfaceHeightAt` is solved. | `WaveWater.cs`, `StylizedWater.shader` | Smooth faces, 2x more vertices at lower CPU cost. [E] 1-2 sessions | Later |
| 12 | **Per-pixel foam.** Port `Broken/PocketMask/Foam` to HLSL (inputs from the same wave array), so foam edges are crisp at any distance; keep vertex colors only for lip. Depends on #11. | `StylizedWater.shader`, `WaveWater.cs` | Crisp cartoon foam lines | Later |
| 13 | **Persistent foam grid** (CPU R8 texture, 4 texels/m, decays x0.97/frame, written by crest, surfers, wall hits), sampled in the shader. Optional blur like Sea of Thieves. | new `FoamMap.cs`, `WaveWater.cs`, shader | Lingering whitewater, wakes, shore wash | Later |
| 14 | **Ocean upgrade (non-pool).** Own material: 3-wave Gerstner chop in the vertex stage (total steepness ~0.5), finer mesh near the camera or 256x256 grid, shoreline foam line where it meets the sand. | `WorldArt.OceanMesh`, new `StylizedWater_Ocean` | Beach view and background look alive | Later |
| 15 | **Flow-map (skip for now).** Not needed for wave pools; revisit for a lazy river. | -- | -- | Later |

### Notes for the builder
- Ido's rules apply: shader files are text, safe to edit; the scene is regenerated by `GreyboxBuilder`, so change materials/spray setup there, not in the scene.
- Do #1 to #6 without touching the CPU/GPU split; they all fit the current architecture.
- After each shader change, rebuild the scene and capture screenshots with `asyncShaderCompilation` off (see CLAUDE.md) so cyan "not compiled yet" frames don't fool us.
