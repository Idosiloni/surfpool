# Free low-poly assets for a sunny surf park
Date: 2026-10-20

**Warning:** I could not open every license page. Rows marked "verify" must be checked on the site before we ship. Quaternius reportedly moved from CC0 to its own "Quaternius Asset Licence (QAL v1.0)" (free for commercial games, no credit needed, but you can't resell the assets as a pack) - that came from a single third-party guide, so confirm on [quaternius.com](https://quaternius.com/). CC0 = no rules at all; CC BY = must credit the author.

## 1. Characters + animations
| Source | Link | License | What's in it | Format | Fit |
|---|---|---|---|---|---|
| Quaternius characters (Universal Base Characters, Ultimate Animated Character Pack, Animated Men/Women packs, modular outfits) | [quaternius.com](https://quaternius.com/) | verify (CC0 or QAL) | Many low-poly rigged people; some with walk/run/idle | FBX/Blend/OBJ (animated pack reported FBX, OBJ, Blend) | Best match for surfers/staff; modular outfits could let us make swimsuits/wetsuits |
| Kenney Animated Characters | [kenney.nl/assets](https://kenney.nl/assets) | CC0 (per Kenney; verify on pack page) | Blocky characters, a few animations | FBX/GLB | Too blocky if we go Quaternius style; fine for placeholders |
| Mixamo | [mixamo.com](https://www.mixamo.com/) | Free with Adobe account; use in games allowed, can't resell the animations alone (verify Adobe terms) | Hundreds of animations (idle, walk, sit, wave, fall) + auto-rigging | FBX | Use for wipeout/fall, cheer, walk. Needs humanoid rig; retarget in Unity Humanoid |
| Poly Pizza | [poly.pizza](https://poly.pizza/) | Per model: CC0 or CC BY | Searchable low-poly models of everything | GLB/FBX | Good for gap-filling; track credits for any CC BY |

Tip: Unity can reuse one animation set on any Humanoid-rigged character, so pick one character family, then add Mixamo clips.

## 2. Nature, palm trees, beach props
- **Quaternius nature packs** (Stylized Nature MegaKit, Ultimate Nature, Simple Nature, Stylized Tree) - [site](https://quaternius.com/). I did not see a dedicated palm pack on the page; check inside the packs or search "palm". License: verify.
- **Kenney** nature/props kits - [kenney.nl/assets](https://kenney.nl/assets) (e.g. Mini Forest). Mostly CC0. Palm/beach items: check specific packs.
- **Poly Pizza** search "palm tree", "beach umbrella", "deck chair", "surfboard", "cooler": [poly.pizza](https://poly.pizza/).
- Fallback: model a palm in Blender (curved trunk + 5-6 plane-ish leaves) - 15 minutes, matches our style exactly.

## 3. Buildings / kiosks / furniture
- Quaternius Ultimate Buildings / Simple Buildings / Ultimate Furniture - [site](https://quaternius.com/). Mostly houses/medieval; **kiosks need our own modeling** or a reskin.
- Kenney City Kit (Commercial/Industrial) - [kenney.nl/assets](https://kenney.nl/assets): small shops, usable as backdrop buildings outside the park.
- Cash register, shelves, benches: Quaternius furniture + Poly Pizza.

## 4. Ground, tile, pool textures, skyboxes
| Source | Link | License | Notes |
|---|---|---|---|
| ambientCG | [ambientcg.com](https://ambientcg.com/) | CC0 (stated on site; verify) | Tiles, concrete, sand, wood. For low-poly, use tiny/flat color versions or just plain colors |
| Poly Haven | [polyhaven.com](https://polyhaven.com/) | CC0 | HDRIs for sky/lighting, textures, some models |
| Kenney Skyboxes | [kenney.nl/assets](https://kenney.nl/assets) (Textures section) | CC0 (verify) | Simple skyboxes |
- Low-poly cartoon tip (my advice): flat colors + a simple gradient sky beats photo textures. Use Unity's procedural skybox with warm tint first, HDRI later.

## 5. Stylized water with foam (URP)
- [80.lv tutorial: stylized water shader in Unity](https://80.lv/articles/tutorial-making-a-stylized-water-shader-in-unity) - Shader Graph: color/depth, refraction, foam, waves (Alexander Ameye). Free, written.
- Ameye's free Shader Graph water package, discussed on the [Unity forum](https://discussions.unity.com/t/stylized-water-shader-free/780666) (2020 post; check current price/version for Unity 6).
- Unity's own URP nature shaders guide (stylized stream with edge foam) is summarized on 80.lv: [how to make nature shaders](https://80.lv/articles/tutorial-how-to-make-nature-shaders-in-unity).
- Core recipe (general): depth fade for shallow/deep color -> noise-scrolled normals -> foam where depth is small (shore/pool edge/near board) -> foam streak at wave crest from a texture or vertex color.
- Our wave is a faked animated mesh: put a **foam mask in vertex color** painted in Blender (crest = white) so the shader needs no simulation.
- Needs **Depth Texture** enabled in the URP asset (and Opaque Texture for refraction).

## 6. UI icon packs
- Kenney Input Prompts and Crosshair Pack - [kenney.nl/assets](https://kenney.nl/assets) (CC0 per Kenney; verify).
- Kenney also has UI packs (search the site); [Game-icons.net](https://game-icons.net/) - thousands of single-color icons, CC BY 3.0 (credit needed; verify).
- Draw our own simple icons (coin, board, wave, star, face emotes) in Figma/Blender; 10-20 icons is enough for Step 4.

## 7. What we model ourselves in Blender
| Thing | Why custom |
|---|---|
| Wave machine / foil housing | Unique, central; has the button/panel |
| Pool (basin, steps, lanes, edge) | Exact shape drives wave mesh and foam mask |
| Ticket desk, rental stand, board rack, return bin | Gameplay props; need exact interaction points |
| Surfboards (shortboard, longboard, foam "soft-top") | Few simple shapes, colorful variants |
| Computer/monitor with a flat screen quad | Needs clean UVs for world-space UI |
| Signs, flags, towel stand | Easy brand flavor |
Keep a style rule: chunky shapes, no tiny details, bevels on edges, flat materials, ~500-3000 tris per prop.

## 8. Color palette (suggestion, my guess)
| Role | Hex |
|---|---|
| Pool water shallow | `#5FE0E6` |
| Pool water deep | `#1A8FC4` |
| Foam / white | `#FFFFFF` |
| Sand | `#F2D8A0` |
| Deck / tile | `#E8E2D4` |
| Accent coral (buttons, boards) | `#FF6B5B` |
| Sun yellow (money, UI highlights) | `#FFC93C` |
| Palm green | `#3FA66B` |
| Wood / dark | `#8A5A3C` |
| Sky top | `#6EC6FF` |
Use 3-4 board colors from the same set so the park looks cohesive.

## Ideas for our game
- **Now (placeholders):** keep gray boxes; grab one Quaternius character family + Mixamo walk/idle/fall for NPC testing during Step 4 "More waves" (wipeouts need a fall animation).
- **Step 4 "Better UI":** import Kenney icons (or draw 10-20) for upgrade cards; Kenney Input Prompts for `[E]` hints.
- **Step 4 "Better world":** follow the 80.lv water tutorial; add vertex-color foam mask on the wave mesh; build wave machine, pool, desk, rental stand, boards in Blender; fill with Poly Pizza/Quaternius palms, umbrellas, chairs.
- **Before shipping:** make a `CREDITS.md`/asset license list as we import each pack (even for CC0) and re-check each license page.
- **Later:** swimsuit/wetsuit variants via Quaternius modular outfits for customer variety.
