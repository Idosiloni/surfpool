# Blender from Python: making our low-poly assets look pro
Date: 2026-10-22

Tags: **[S]** = from a linked source. **[K]** = my general Blender/game-art knowledge (check by eye). **[G]** = my judgment for our game.
Honest note: the Blender manual pages were too big for my fetch tool (it returned only menus), so most Blender facts are [K]. Test each snippet once in the MCP before trusting it. Target Blender 4.1+ (our scripts already call `shade_smooth_by_angle`; there is no `use_auto_smooth` any more [S: [4.1 notes](https://developer.blender.org/docs/release_notes/4.1/modeling/)]).
Related: [lowpoly-assets.md](lowpoly-assets.md), [character-look.md](character-look.md), [character-models-2.md](character-models-2.md).

## 1. What we do today (read from the scripts)
- Every prop = many overlapping primitives (`box`, `cylinder`, `ico`) -> `join()`. One flat color material per color.
- `finish()` sets **every face flat-shaded**. Boxes have razor-sharp, unbevelled edges, so light never "rolls" over a corner.
- Thin things (fronds, canopies, flags) use `double_sided=True` (a copy of the sheet 1 cm lower): no real thickness, no edge highlight.
- Palm trunk = 7 separate tilted cylinders. Rock = squashed icosphere. Roofs = 6-9 flat slats.
- Characters (`build_characters.py`) are already better: `soft_box` bevels each box **before** joining, then `shade_smooth_by_angle(50 deg)`. That is the look to spread to the props.
- Animation: none in Blender. Unity code moves things (`Flutter.cs` swings flags/bunting with sines; `WaveMachineRig`, `Cruiser`, `Bird` move parts). Props that Unity recolors **by material name**: `BoardDeck` (boards, tinted), `CarPaint` (property block), `Bulb` (swapped for glow in `GlowUpBuilder`), plus `ShopArt.Tint`, `CharacterRig.Wear` (Skin/Shirt/Shorts/Hair), `ParkGlowUp` worn/fresh paint swaps.

## 2. Amateur vs pro stylized low-poly (what to copy)
| Thing | Amateur | Pro | Source |
|---|---|---|---|
| Silhouette | Reads as "a box with a roof" | One exaggerated, readable outline: fat roof overhang, tapering legs, oversized sign/vent. Test: fill it black from 20 m, still know what it is? | [S: character-look.md](character-look.md) (3-read rule, Skelattack) |
| Edges | Razor-sharp 90 deg corners | Small **bevel/chamfer** on every big edge: a thin light line catches the sun and the shape looks "toy-like" | [K] |
| Proportions | Real-world sizes | Thicker posts, bigger roofs, rounder, 10-30% chunkier than real | [K], Animal Crossing / Fall Guys notes in [character-look.md](character-look.md) |
| Shape language | Random mix | Round = friendly, square = sturdy, triangle = danger. Beach = mostly round + a few squares | [S: RocketBrush via character-look.md] |
| Color | Many unrelated colors | 6-10 palette colors, 1 dominant + 1 accent per prop, **value contrast** (dark base, light top) | [K] |
| Shading | Flat everywhere or smooth everywhere | Smooth rounded forms + crisp hard edges only where the real object has them (weighted normals) | [K] |
| Surface | Perfectly clean | A little variation: slight color jitter per part, darker crevices (AO), a stripe/trim line | [K] |
| Detail | Tiny details that vanish at distance | Few **big** details (a bold stripe, a big vent), no 2 cm parts | [S: Skelattack devlog, via character-look.md] |
Reference looks: A Short Hike (flat cohesive shading + soft outline, [S](https://blog.playstation.com/2021/08/05/crafting-a-tiny-open-world-a-look-behind-the-scenes-at-the-creation-of-a-short-hike/)); Animal Crossing props (simple silhouette first, [S](https://www.thegamer.com/animal-crossing-new-horizons-villager-design/)); Untitled Goose Game (flat colors, [S](https://www.nintendolife.com/news/2019/09/feature_how_a_joke_hatched_gamings_most_horrible_goose)). Synty POLYGON = faceted, modular, palette-recolored kits [S: [Unity Store](https://assetstore.unity.com/packages/3d/vegetation/trees/polygon-nature-low-poly-3d-art-by-synty-120152) says modular + color variants]; whether they use vertex colors or a palette texture I could not confirm, so that is [G]. Islanders / Townscaper = flat color, rounded blocks, soft light and AO doing the work [K].

## 3. Pitfalls that decide what works (read before copying snippets)
1. **Flat-shaded faces ignore normal tricks.** Bevel "harden normals" and Weighted Normal only matter on smooth-shaded meshes. Change 3 things together: smooth shade -> mark sharp by angle -> weighted normals. A bevel alone looks like a bigger flat box.
2. **Bevel each primitive BEFORE `join()`.** Our props are overlapping boxes with no shared edges; beveling the joined mesh does little. Move `soft_box` logic into `box()`. Skip parts under ~5 cm or use 1 segment (string lights, grille bars, bunting) or triangle count explodes.
3. **Do not change the mesh export axis settings** (`axis_forward="-Z"`, `axis_up="Y"`, `bake_space_transform=True`, `FBX_SCALE_ALL`). Unity placement (`(x,y,z)->(-x,z,-y)`, 180 deg yaw in `WorldArt.Model()`) depends on them. For armature/animated exports write a **separate** `export_animated()` (see section 5), because `bake_space_transform` is described by Blender as experimental and known to break armatures/animation [K, from the operator's tooltip; verify].
4. **Vertex colors need a Unity shader that reads them.** URP Lit ignores them. Anything with AO/jitter in vertex color needs a small Shader Graph (Vertex Color node x base color). Rank those below pure-Blender fixes.
5. **Unity normals:** the model importer's Normals = *Import* uses the file's normals, *Calculate* rebuilds them from a smoothing angle [S: [Unity manual](https://docs.unity3d.com/Manual/FBXImporter-Model.html)]. For custom/weighted normals set **Import**; check each FBX once in the Inspector. Exporting "Apply Modifiers" once lost weighted normals in an old bug [S: [#64743](https://projects.blender.org/blender/blender-addons/issues/64743), 2019, status unknown]; safest = **apply modifiers yourself in the script** (we already do).
6. **Name-based material hooks:** keep material names `BoardDeck`, `CarPaint`, `Bulb`, `Skin/Shirt/Shorts/Hair`, worn/fresh variants. A palette-atlas rewrite would break these, so convert only props that are never recolored (see section 7).
7. **One material per color is fine** for ~40 props. Atlas/gradient-palette textures mostly help draw calls (SRP Batcher already handles same-shader materials well [K]); not a visual priority.

## 4. bpy techniques with the biggest visual jump
| Technique | What it gives | Notes |
|---|---|---|
| Bevel per part (modifier or `bmesh.ops.bevel`) | Light-catching edges | Width ~ 1-3 cm for props, 1-2 segments. `limit_method="ANGLE"` so only hard edges |
| Smooth + sharp-by-angle + **Weighted Normal** (keep sharp) | Big flat faces stay clean, bevels look rounded, no shading artifacts | Order: Bevel -> WeightedNormal; apply both before export |
| **Solidify** | Real thickness (leaves, canopy, flag, sign boards) | `thickness` 2-4 cm, `offset=0`; replaces `double_sided` hack |
| Lathe (`rings()` profile) / **Screw** | Round chunky things: trunks, barrels, posts, cups, bollards in ONE mesh | Profile of (z, radius) points; we have `rings` in build_characters |
| Curve -> mesh (`bevel_depth`, `bevel_resolution`) | Ropes, pipes, rails, palm trunk with a bend, handrails | Convert with `bpy.ops.object.convert(target="MESH")` before join |
| Mirror / Array | Symmetry, planks, railing posts | Cleaner code; small visual gain. Apply before export |
| Subdivision (1 level) + Decimate / displace | Rocks, bushes, clouds, rounded blobs | Subsurf 1 on a 20-face rock then jitter verts |
| bmesh inset + extrude | Panels, window frames, vent recesses, grille insets | Gives depth shadows (see section 6) |
| Vertex colors / color attributes | AO, per-part jitter, top-light gradient | Needs shader (pitfall 4) |
| Geometry Nodes from Python | Scatter pebbles/plants, instance planks | Tidier code, no big visual change; **low priority** |
| Boolean | Cut doors/windows | Fragile; prefer building the wall in pieces around the opening |
| LODs | Performance | Not needed at our poly counts; low priority |

## 5. Animation from Python
**When to animate in Unity code instead (our current way): keep doing this for** anything procedural or gameplay-driven: flags/bunting (`Flutter`), sway, spinning rotors, wave machine parts reacting to `WaveFired`, surfers, birds. Pros: no armature/FBX pitfalls, tweakable live, speed tied to wind/events. Use Blender animation only for **authored loops with shape you can't get from a sine**: cloth-like flag wave, palm fronds bending, a blender/cash register lid, a wipeout flail.

Options, from simplest:
1. **Separate parts + pivot at the joint** (what we do for people). Make fronds/flag/rotor separate FBX parts with origin at the hinge; Unity rotates them with a sine. Zero risk. [K]
2. **Shape keys (blend shapes)**: model the flag in 2-3 wave poses, export, drive `SetBlendShapeWeight` from a script (or loop an Animator). Good for flags, bunting, fronds; no skeleton. FBX exports shape keys; check Unity importer *Blend Shape Normals* = Import [S: Unity manual above].
3. **Armature + action loop**: small armature (3-5 bones) for a palm or flag. Export with a dedicated function:
```python
def export_animated(obj, arm, path):          # arm = armature object, obj = skinned mesh (parented, modifier applied)
    bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True); arm.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH","ARMATURE"},
        bake_space_transform=False, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=1.0, bake_anim_force_startend_keying=True)
```
   Then in Unity set the clip to Loop Time, and **test orientation** (without `bake_space_transform` the mesh may arrive rotated 90 deg or at scale 100; fix with importer *Bake Axis Conversion* / a parent). Expect 1-2 tries. All Actions exports every action that fits [S: [Blender 4.2 manual](https://docs.blender.org/manual/en/4.2/addons/import_export/scene_fbx.html)]; I'd keep it off and assign one action per file, one clip per prop.
4. Create actions from code: `obj.keyframe_insert("rotation_euler", frame=1)` for each frame, `fcurve.modifiers.new("CYCLES")` for loops; set `scene.frame_end` to the loop length.
Keep loops short (24-48 frames at 24-30 fps), first frame = last frame for seamless loops. NLA strips are only useful if you pack several clips in one file; skip for now.

## 6. Water and pool asset tips [K unless noted]
- **Pool coping / edge**: a rounded lip (bevel 3-5 cm, 2 segments) + a darker inner "gutter" line; the top edge catching light is what reads as a real pool. Add a 1-tile contrasting strip (Coral/Teal) just below.
- **Tiles**: do not model tiles. Use a Unity tiling texture or a vertex-color checker; or inset a grid of faces 1 cm so the grout shows as shadow.
- **Ladders**: curved handrails from a curve with `bevel_depth` (chrome Steel), 3 rungs, rubber feet. Chunky tubes (4-5 cm radius).
- **Lifeguard tower**: tapering legs, **cross-bracing** (X diagonals), railings with real thickness, a roof with overhang and a trim board, a rescue buoy and flag on the front. Red/white stripes; add a small chair/seat.
- **Cabanas**: striped Solidify canopy with a scalloped valance made from triangle teeth, tied corner posts, a hanging lantern (use the `Bulb` material).
- **Wave machine housing**: it is the star. Big silhouette: stepped roof, 2-3 oversized pipes/vents, a bold stripe, a warning-sign panel, an inset door, intake grille with recessed shadow, a name plate. Industrial = chunky bevels + hazard stripes (Yellow/Charcoal).
- **Piers**: planks with 2-3 mm gaps and random +/- 5 mm height jitter and slight rotation, posts that stick out above the deck, rope between posts (curve), a few mooring cleats. Wood color jitter per plank is the cheapest "handmade" look.
- **Wear variants**: we already swap worn/fresh materials in `ParkGlowUp`; for models, add a few extra "wear" parts (dents, patches, rust streaks) under a `GlowItem` instead of re-modelling.

## 7. Which of our models are safe to rework
| Model | Safe? | Why |
|---|---|---|
| WaveMachineHouse, MachineChamber, Counter, shacks, kiosks, LifeguardTower, Bench, LoungeChair, ParkArch, signs, light poles | Yes | Static, material names mostly colors we choose |
| PalmTree, Rock, Weeds, FlowerPlanter | Yes | Static |
| Boards (`BoardDeck`), Car (`CarPaint`), StringLights/Bunting (`Bulb`) | Keep material names | Unity tints/swaps by name |
| Characters (Skin/Shirt/Shorts/Hair) | Careful | `CharacterRig.Wear` bands; changing normals is fine, changing materials is not |

## 8. Ranked checklist for our scripts (best look per hour first)
Common helper block (put near `box()`):
```python
def smooth_and_sharpen(obj, angle=40):
    bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True); bpy.context.view_layer.objects.active = obj
    bpy.ops.object.shade_smooth()                        # (a) smooth everything
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(angle))  # (b) sharp edges by angle
    m = obj.modifiers.new("WN", "WEIGHTED_NORMAL"); m.keep_sharp = True; m.weight = 50
    bpy.ops.object.modifier_apply(modifier=m.name)       # (c) weighted normals, then bake in
```
1. **Bevel every box + smooth/sharp/weighted-normal (biggest jump, all props).** Change `box()` to bevel after scaling, skip when `min(size) < 0.05`, and change `finish()` to stop forcing flat. Then call `smooth_and_sharpen(joined)` in `join()` (angle 35-45). Snippet: `m = obj.modifiers.new("Bevel","BEVEL"); m.width = min(0.025, min(size)*0.3); m.segments = 2; m.limit_method = "ANGLE"; bpy.ops.object.modifier_apply(modifier="Bevel")`. Improves first: WaveMachineHouse, Kiosk, Counter, LifeguardTower, BoardRack, Bench. Check each FBX in Unity with Normals = Import. Flat-shaded look can stay as an opt-in `flat=True` for crates/boards.
2. **Solidify instead of `double_sided` (fronds, canopy, flag, bunting, sails).** `m = obj.modifiers.new("Sol","SOLIDIFY"); m.thickness = 0.03; m.offset = 0` then apply. Gives edges that catch light and removes the z-fighting hack. First: PalmTree, BeachUmbrella, Flag, Bunting.
3. **Palm trunk as one lathe/curve mesh with bulge rings.** Use `rings(profile=[(z, r)...], sides=8)` along a bent path (offset each ring's center by `lean*t**2`), add a fatter base and 4-5 darker "ring scar" bands via a second material; fronds get a central rib + Solidify. First: PalmTree (the most-seen prop). Sketch: `verts.append((cx + lean_x*t*t + cos(a)*r, cy + sin(a)*r, z))` per ring.
4. **Chunkier proportions pass.** Multiply roof overhang x1.3, post thickness x1.4, add a visible trim board under every roof. Pure numbers in the existing functions. First: Kiosk, AcaiShack, GearShack, LifeguardTower.
5. **Inset/extrude panels for detail depth** (windows, doors, vents, grille, sign plates): `bmesh.ops.inset_individual(bm, faces=[f], thickness=0.04, depth=-0.03)` on the front face, then `bmesh` extrude of the inner face. Cheap shadow lines. First: WaveMachineHouse (windows, grille, door), Counter, Computer.
6. **Real silhouette on the wave machine house.** Add stepped roof, two oversized pipes (curve, `bevel_depth=0.35`), hazard stripe, name plate. It is the hero prop; budget 2000 tris. First: WaveMachineHouse, MachineChamber.
7. **Planks with jitter (piers, decks, benches, boardwalk).** Loop: `z += rng.uniform(-0.004, 0.004)`, `rot_z = rng.uniform(-0.01, 0.01)`, 3 mm gaps, 2 wood tones. First: Pier modules, Bench, Deck.
8. **Chunky, flat-bottomed rocks and bushes.** Icosphere subdiv 1 -> noise displace -> `v.co.z = max(v.co.z, 0)` clamp -> scale z 0.6 -> smooth with sharp angle 50. Add 2-3 smaller stones merged. First: Rock, Weeds.
9. **Cross-bracing and trim on structures** (X braces on towers, ladders with side rails and rounded rungs as Curve tubes, cap boards on posts). First: LifeguardTower, ParkArch, Pier.
10. **Per-part color jitter** (needs a Unity step): `mesh.color_attributes.new("Col","BYTE_COLOR","CORNER")`, write 0.9-1.0 grey per part, then a Shader Graph multiplies base color by Vertex Color. Gives the "hand-painted" feel; the same slot carries AO later. Do this only after #1, because it needs a shader change. First: WaveMachineHouse, shacks, Pier.
11. **AO baked to vertex color** (bake with `bpy.ops.object.bake(type="AO", target="VERTEX_COLORS")` in Cycles, or fake it: darken vertices by `1 - clamp(height_above_ground)`, i.e. a cheap ground gradient: bottom 40 cm slightly darker). Needs the same shader. First: all buildings, PalmTree trunk base.
12. **Separate animated export + a first animated prop** using `export_animated()` (section 5): a 3-bone palm that sways, or a flag with 3 shape keys. Compare to `Flutter.cs`; keep Unity code if it already looks fine. First: PalmTree (only if rank-3 palm is done), Flag.
13. **Turn parts into a small "kit" with a manifest.** One list `[(name, fn, tris_budget)]` and a check that prints tri count per model and warns if > budget; keeps polish from bloating. Small, safe, helps all.
14. **Optional last: Array / Mirror / Geometry Nodes for rails, planks, scatter.** Tidier code, little visual gain. Skip until the above is done. LODs: skip (our poly counts are tiny).

## Ideas for our game
- **Step 5 (polish), session 1 (one feature):** ranks 1 + 2 together ("soft toy edges + thick leaves"). Rebuild all with `build_models.py`, rebuild the scene, then compare before/after screenshots with `EditorTools.DevShots.Shot` on kiosk, palm, tower. Ask Ido whether he likes the softness; if too round, drop bevel width to 1.5 cm.
- **Step 5, session 2:** ranks 3 + 4 on the palm and the kiosks/shacks (most-seen props); then rank 6 for the wave machine house, since it is the park's landmark and fits the machine-upgrade story (house gets bigger/shinier as `GlowItem` levels rise).
- **Step 5/6 growth:** ranks 5, 7, 9: pier planks, tower bracing, panel insets. Pairs with the Cove/Ranch pier modules (`CovePier`).
- **Later, with a Shader Graph session:** ranks 10-11 (vertex color jitter + AO) as one "Park Lit" shader, then re-export props with colors. Combine with the rim-light idea in [character-models-2.md](character-models-2.md).
- **Animation:** keep `Flutter` for flags/bunting. Try one shape-key or 3-bone palm (rank 12) as an experiment, tied to `DailyEvents.TodayWind` strength, and only adopt if it clearly beats the sine sway.
- **Quality gate:** every new model gets a black-silhouette check at 20 m and a tri count in the build output (rank 13).
