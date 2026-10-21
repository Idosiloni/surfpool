# Character look: a "house style" so our people stop looking default
Date: 2026-10-21

Tags: **[S]** = from a linked source. **[G]** = my general knowledge, no source found. **[Idea]** = my suggestion/guess. Searches gave good dev quotes for Fall Guys, Two Point, Goose Game, A Short Hike, Animal Crossing and shape language, but NOTHING usable for Overcooked, Lethal Company, Gang Beasts, Human Fall Flat, Totally Reliable Delivery, Sable, Tchia, Moonglow Bay, Townscaper. Anything I say about those is [G].
Related: [fun-surfers.md](fun-surfers.md) (archetypes, lines, gestures), [surfing-motion.md](surfing-motion.md), [lowpoly-assets.md](lowpoly-assets.md) (palette).

## 1. What our people look like today (read from `tools/blender/build_characters.py`)
- Body about 1.75 m tall (hips 0.85 m, head joint at hips+0.55, skull box 0.27 x 0.26 x 0.29 m). That is **realistic proportions: head ~1/6 of height**.
- Face features are tiny: eye white 0.06 m wide, mouth 0.05 m, brows 0.07 m. Hands are 0.075 x 0.09 m, feet 0.095 x 0.24 m.
- Already good: parts painted per person by material name (Skin/Shirt/Shorts/Hair), 5 hair meshes + cap, one fixed smile, one body shape.
- Why it feels "default" [Idea]: same body, same face, same smile, only color changes. Color alone is the weakest way to tell people apart.
- Seeing distance (my math, 60 deg camera, 1080p, about 18 px per degree): a 0.06 m eye at 15 m is about 4 px; a 0.4 m head is about 28 px. So at pool distance **faces are invisible and only silhouette + color blocks read**. Faces matter only up close (desk, shops, lineup from the rail).

## 2. How other games get a memorable look from simple shapes
| Game | Trick | Source |
|---|---|---|
| Fall Guys | Stubby legs, big arms, "unathletic but ambitious" bean, shape chosen to be bad at the task = funny in the air. Costumes are treated like real clothes sized for the bean body, not new characters. Early prototypes were creepier; they tuned toward cute. | [GGRecon](https://www.ggrecon.com/articles/fall-guys-developers-reveal-what-makes-their-jelly-beans-so-lovable), [PlayStation blog](https://blog.playstation.com/?p=224711) |
| Two Point Hospital | Gags are visual (light-bulb head); animators push expression and body acting; traits make personalities. | [PCGamesN](https://pcgamesn.com/two-point-hospital/two-point-hospital-personalities-dev-diary), [Cosmocover](https://www.cosmocover.com/newsroom/two-point-hospital-dev-diary-how-to-build-a-game-with-personality) |
| Animal Crossing | Start from a simple silhouette; background villagers get few details, standout ones get more. Villagers should remind you of people you know. | [Siliconera via search](https://vip-develop.siliconera.com/?p=850169), [TheGamer](https://www.thegamer.com/animal-crossing-new-horizons-villager-design/) |
| Untitled Goose Game | Flat colors, storybook look; townsfolk modelled on faceless little people from UK kids' TV. Proof you can have charm with almost no face. | [Nintendo Life](https://www.nintendolife.com/news/2019/09/feature_how_a_joke_hatched_gamings_most_horrible_goose) |
| A Short Hike | Flat cohesive shading plus a soft outline keeps objects readable; tiny scope. | [PlayStation blog](https://blog.playstation.com/2021/08/05/crafting-a-tiny-open-world-a-look-behind-the-scenes-at-the-creation-of-a-short-hike/) |
| Overcooked, Gang Beasts, Human Fall Flat, Lethal Company, TABS | No source found. [G] Big heads, tiny or one-piece faces, expression comes from pose and animation, chunky mitten hands, one strong outfit color each (Overcooked chefs differ by hat/animal head). | none |

**Rules that came out of the sources:**
- **Shape language [S]:** circles = friendly, squares = sturdy, triangles = threat ([RocketBrush](https://rocketbrush.com/blog/shape-language-in-game-character-design-how-to-make-characters-readable-and-consistent)). For a chill surf park use mostly round/soft shapes; use squares for locals/lifeguards, one triangle-ish thing (shark-tooth hair, pointy hat) for a "troublemaker".
- **3-read rule [S]:** a character must read as silhouette first, then color, then detail ([Skillselion guide](https://skillselion.com/skills/omer-metin/skills-for-antigravity/character-design)). Test: fill the model black; can you still tell a grom from a dad?
- **Big face features survive small size [S]:** ([Skelattack devlog](https://www.indiedb.com/games/skelattack/tutorials/skelattack-character-design)). Detail that vanishes at small size is wasted.
- **Silhouette comes from one signature element + exaggeration [S]** (same devlog): a hat, a belly, a huge bun, a long board.
- **Variety from a kit [S/G]:** few body shapes x hair x outfit x accessories x palette. Standouts get extra detail, crowd gets less (Animal Crossing).

## 3. Real surf looks to turn into archetypes
Sources are thin: [Honolulu Museum boardshorts](https://honolulumuseum.org/stories/2020/07/permanent-collection/boardshorts-classic-surfing-attire/), [Hawaii Magazine](https://www.hawaiimagazine.com/boardshorts-a-perfect-fit-exhibition-spotlights-evolution-of-hawaii-surf-fashion/), [QAGOMA](https://blog.qagoma.qld.gov.au/a-board-a-bar-of-wax-and-a-pair-of-shorts/) say: 1950s custom cotton boardshorts, 1960s long "Jams" in aloha prints plus faded aloha shirts, longboards ruled until the late 60s/70s, 70s added psychedelic/ikat patterns, 80s-90s brands brought back Hawaiian motifs. No source found for zinc nose, tan lines or hair; those are [G].
| Look | What reads from far away [G] |
|---|---|
| Grom (kid) | Small, big head, neon rash vest, board almost as tall as them, messy hair, a gap-toothed grin |
| 60s longboard soul surfer | Long loose trunks in stripes or aloha print, bare chest or striped tee, bucket/straw hat, bleached hair, 9 ft board |
| Pro shortboarder | Fitted black/white rash vest or springsuit with bright color panels, sponsor stickers on the board, headband, athletic V-shape |
| Kook | Clashing colors, new stiff wetsuit worn wrong, huge foam board, leash on wrong foot, wide eyes |
| Old salty local | Weathered, white zinc nose, faded cap, gray beard/stubble, tattoos, faded trunks, leathery tan, mullet |
| Bodyboarder | Short fins, short board, swim trunks or bikini, rash vest, wrist leash |
| Surf mom / dad | Sun hat or visor, one-piece or board shorts with a belly, sunscreen-white shoulders, foamie board, phone/camera |
| Influencer | Sunglasses, matching color set, perfect hair or man bun, neon accessories, phone on a stick, bikini or tight springsuit |
Clothing and small stuff [G]: boardshort patterns (hibiscus/floral, stripes, checkerboard, tie-dye, camo), springsuit (short wetsuit) and fullsuit with color panels down the side, rash guard (shirt that stops rashes and sun), bikini and one-piece, bucket hat, trucker cap, sunglasses, zinc nose, tan lines (lighter strip where a watch or strap sat), bleached hair, dreadlocks, man bun, mullet, tattoos, wrist or ankle leash, wax comb, sticker on the board.

## 4. Recommended house style: "Chunky Beach Buddies" [Idea unless noted]
**Goal:** keep the rig and code (same joints), change proportions and add shapes so silhouette + 3 colors tell you who it is.

### 4a. Proportions (numbers are starting values to tune by eye)
| Part | Now | Recommended | Why / how |
|---|---|---|---|
| Total height | 1.75 m | keep 1.75 (adult). Grom 0.75 scale, kids 0.8 | keeps all joint offsets valid |
| Head | 0.29 m (1/6) | **scale head + hair + hat 1.4x about the neck pivot -> ~0.41 m, about 1/4.3 of height** | Biggest single change; faces become readable; no rig change (pivot is already the neck base). Try 1.5x for groms/influencers |
| Neck | visible | shorter (shrink 0.7) | chunkier |
| Torso | narrow waist | rounder, 10% wider, softer "bean"; body types in 4b | |
| Arms | thin | thicker (radius +30%) | arms are what you see on a board in poses (claim/shaka) |
| Hands | 0.075 x 0.09 | **mittens 0.14 x 0.09 x 0.16, thumb as a small block** | Fall Guys/Overcooked style [G]; a shaka or fist reads from 20 m |
| Feet | 0.095 x 0.24 | **0.14 x 0.30 x 0.09, rounded** (mostly hidden on a board, big on the sand/deck) | grounded walk, funny silhouette |
| Legs | long | keep length, thicker thighs (+25%) | stubby/chunky look without changing knee height |
Rule of thumb: **head 1/4, hands as big as the face's mouth-to-eyes span, feet a shoe-box.**

### 4b. Faces that read at 10-20 m
- Make features **2x bigger and darker**: eye whites 0.11 x 0.10 m, pupils 0.06 m (or just black ovals: Goose Game shows faceless works), brows 0.12 x 0.03 m thick, mouth 0.10 m wide. Place eyes lower and wider apart than now (cute rule [G]: eyes at or below the head's middle, wide apart).
- Keep it to **5 shapes per face**: 2 eyes, 2 brows, 1 mouth (drop the nose box to a tiny bump or none).
- **3 runtime expressions** (swap by hiding/showing or rotating the same blocks, driven from `Surfer` state):
| Expression | Eyes | Brows | Mouth | When |
|---|---|---|---|---|
| Happy | round, normal | raised, tilted up outward | wide U-smile (corners up), optional open | catch, good ride, tip, hoot |
| Focused | half-height (top lid flat) | low, angled down toward nose | small flat line, slightly off-center | paddling, pop-up, riding, carrying |
| Scared | tall ovals, small pupils | high, angled up in the middle (worry) | open black O | wipeout, big wave, shark/low-patience |
- Optional 4th: **angry** (brows angled down, mouth flat, red tint) for patience gone / drop-in victims; **sleepy/bored** for long queue (half lids).
- **Blink:** every 2-5 s scale eyes to 0.1 height for 0.12 s (random per person). It is the cheapest "alive" signal [G].
- Looking at things: reuse `CharacterRig.LookTarget` for head yaw; add pupil offset of +-0.02 m toward the target.
- Draw order trick: put features 1-2 cm proud of the skull so they never z-fight (current code already does).

### 4c. Palette (names -> hex are my guesses; match the park palette in [lowpoly-assets.md](lowpoly-assets.md))
- **Skin (8, warm, slightly saturated, no gray):** `#FDE0C8` `#F4C79B` `#E0A672` `#C98550` `#A5673A` `#7B4A2A` `#573420` `#3D2416`.
- **Hair (9):** black `#1E1A1A`, dark brown `#4A2E1D`, chestnut `#7A4A28`, sandy `#C9A15A`, sun-bleached `#F0DC9A`, ginger `#B8502A`, gray `#B5B5B0`, white `#F2F2EE`, fun dye teal `#2FC4B2` / pink `#FF6FA5` (rare, grom/influencer).
- **Fabric (use bright, 1 base + 1 accent per outfit):** coral `#FF6B5B`, sun `#FFC93C`, teal `#17B9B0`, sky `#4FA8E8`, navy `#233A66`, lime `#8CD04F`, hot pink `#FF4F9A`, orange `#FF9A2E`, white `#F6F4EE`, black `#2A2A30`, faded denim `#6F8EA8`, sand `#E8D5A8`.
- **Rule:** every outfit = max 3 colors; top and bottom must differ in brightness (so a body reads even in grayscale); skin never matches the water (`#5FE0E6`) or sand (`#F2D8A0`) hue exactly.

## 5. The kit (build once in Blender, mix at spawn) [Idea]
**Body types (4)** = scale presets on the same rig, so animations keep working:
1. Standard (1.0 wide), 2. Lean/athletic (0.9 wide, 1.03 tall; pros, influencers), 3. Round/dad (1.2 wide torso, belly mesh, 0.97 tall), 4. Kid/grom (0.75 scale, head 1.6x instead of 1.4x, shorter limbs). Optional 5: tall big (1.1 tall, wide shoulders) for locals/lifeguard.
**Hair (10)** (5 exist: short, long, bun, curly, buzz; plus cap): + 1 mullet (short top, long back strip), 2 dreadlocks (4 hanging blocks), 3 man bun (high bun, short sides), 4 bald/receding with side hair, 5 spiky bleached, 6 ponytail, 7 afro puff (big round), 8 bowl cut / grom mop. Each hair is a separate mesh in the Hair material (already how it works).
**Outfits (10)** = shirt + shorts mesh and a material pattern:
1. Boardshorts plain, 2. Floral/hibiscus shorts (flower dots via vertex color or small quads), 3. Striped long jams (60s), 4. Checkerboard shorts, 5. Rash vest (long sleeves, color blocks on shoulders), 6. Springsuit (short sleeves + short legs, side panel), 7. Fullsuit wetsuit (long arms + legs, chest zip line), 8. Bikini / one-piece (skin shows on belly; two blocks), 9. Aloha shirt + trunks (beach look for guests/staff), 10. Tank top + swim trunks (kids/dads).
Patterns: do stripes/checker as 2-3 stacked colored boxes or vertex colors, not textures (matches the flat style).
**Accessories (10):** sunglasses (2 dark wide boxes + bridge), trucker cap, bucket hat, wide sun hat/visor, headband, zinc nose (white or neon box over the nose), zinc stripe cheeks, tan lines (a lighter strip band on arm/leg: second skin material), wrist leash, board sticker set (1-3 colored quads on deck), plus wax comb in hand, camera/phone in hand (influencer/dad), beard/stubble block (local), tattoo patch (dark-blue quad on arm).
**Spawn recipe:** pick archetype -> body type -> signature items (always) -> 1-2 random extras -> palette. Keep a "signature" and a "random" part so two kooks still differ.

## 6. Archetype -> signature look (matches `Archetype` enum in `SurferPersonalities.cs`)
| Archetype | Body | Signature silhouette | Outfit / colors | Face default | Board |
|---|---|---|---|---|---|
| Grom | Kid, head 1.6x | Huge messy mop or bowl cut, board nearly taller than them | Neon rash vest + floral shorts; 2 bright colors | Happy, big grin, wide eyes | Small, many stickers |
| Kook | Standard, a bit lanky | Oversized new wetsuit hanging off, helmet-like cap or visor, big foam board | Clashing (lime + hot pink + navy), tags still on, leash on wrong ankle | Scared/unsure, eyes wide, brows worried | Huge soft-top, plain |
| SoulCruiser | Lean, relaxed | Straw/bucket hat, bleached hair or man bun, bare chest / striped tee | Long striped or aloha jams, faded earth-and-teal colors (60s) | Happy-calm, half-lidded | Long single-color board, wooden stringer |
| Charger | Athletic V-shape | Headband, tight rash vest or springsuit, short fitted shorts | Black + one neon (orange or lime), sponsor stickers | Focused, brows low | Short, covered in sponsor stickers |
| Influencer | Lean | Sunglasses, man bun or perfect high pony, phone in hand | Matching coral/pink/teal set, bikini or tight springsuit | Smug-happy | Pastel board, brand logo |
| Local | Big/stocky or lean-weathered | Faded trucker cap, white zinc nose, gray stubble, mullet, tattoo | Washed-out checker or plain trunks, old wetsuit top with patches | Focused/grumpy, one brow lower | Dinged, color-faded, old stickers |
| SurfDad | Round/dad | Sun hat or visor, belly, white sunscreen shoulders, camera on a strap | Aloha shirt over trunks, or tank + swim shorts | Happy-tired, sweat drop (rare) | Foamie, blue or yellow |
Guests who are not surfers [Idea]: bodyboarder (short fins, rash vest, wrist leash, short board), tourist (aloha shirt, camera, visor), lifeguard (red/yellow, whistle, tall build), shop staff (apron + color per shop so jobs read: acai purple, gear teal).

## Ideas for our game
Ordered by "most new look per hour". Each is one session. Step labels: Step 2 = surfers (we're past it, so these are polish), later = Step 4+.
1. **Bigger head, mittens, shoes (S, ~1 h):** in `build_characters.py` scale head+hair+cap 1.4x about the neck pivot, replace the hand box with a mitten, enlarge feet and thickness. No rig change. Playtest the poses (shaka, claim) for clipping with the board. (Step 4+ polish)
2. **Bigger faces + 3 expressions + blink (M):** rebuild face parts at ~2x size as separate named meshes (`EyeL`, `BrowL`, `Mouth` variants), add `CharacterRig.Expression {Happy, Focused, Scared}` and a blink timer; set from `Surfer` state (paddling/riding = Focused, wipeout = Scared, finish = Happy). (Step 4+)
3. **Body types (S-M):** four scale presets applied in `Surfer.Init` by archetype (Section 5); grom stays at 0.75 but gets the 1.6x head. Also add the dad belly mesh.
4. **Hair + accessories pack (M):** add 8 hair meshes and 10 accessories as Blender parts using the same pivot (neck base); assign per archetype signature + random extra. Test the black-silhouette check with all 7 archetypes lined up in a debug scene.
5. **Outfit set + patterns (M):** shirt/shorts variants (rash vest, springsuit, fullsuit, bikini, aloha shirt) as new torso/thigh meshes in the Shirt/Shorts materials; stripes/checker/floral via split boxes or vertex colors. Update palettes in `Surfer.Init` from Section 4c.
6. **Board identity (S):** sponsor stickers/stripes per archetype using the deck-decal idea in fun-surfers #5; ties board to rider.
7. **Look follows level (later):** at higher park levels spawn more influencers/pros with brand stickers; the glow-up system (`ParkGlowUp`) can reuse the same "fresher paint" idea for outfits.
8. **Character creator for the player (later):** the player is first-person, so only hands/arms show; but a mirror in the locker room or a "photo spot" screen with 4 body types, 10 hairs, 10 outfits lets Ido show off the kit. Also needed if we add multiplayer or photos.
9. **Debug tool (S):** an Editor menu "Surf Pool > Spawn Look Lineup" that places all archetypes x body types in a row for fast tuning (no save impact).

Guesses to verify by eye: head scale 1.4x (try 1.3-1.6), mitten size, face feature sizes, and the hex values. Do the "black silhouette at 15 m" screenshot test after each step.
