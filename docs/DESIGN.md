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
  - [x] Ambience + growth: the park starts run-down (grimy paint, weeds, junk, a crooked hand-painted sign) and glows up by park level (fresh paint at 3, flags, then bunting/flower boxes/PA speakers at 5, string lights at 8, the KAI'S SURF PARK arch at 11, flag towers at 15); a rail crowd that grows each level, circling gulls, fishing boat, sailboats, jet skis, a blimp (14) and a news helicopter (18); fireworks + confetti + crowd cheer + fanfare + big banner on level-ups and new/bigger pools; wind, a crowd that gets louder and busier, music out of the park speakers (upbeat songs from level 8) and PA announcements
  - [x] UI restyle with a strong beach-surf art direction, "Sunset Surf Shop": Lilita One + Fredoka + Pacifico, ink-outlined chunky cards with dark sign strips, colored icon tiles, tilted price stickers + OWNED stamps, tab badges, key-cap buttons, toasts with kinds, pop-in windows over a dusk backdrop, retro sunset-poster main menu (see `docs/research/ui-powerwash.md`)
  - [ ] Steam build

## Ideas parking lot
- Surfer wipeouts (funny ragdolls)
- Wave designer: size, speed, left/right breaks
- Surf competitions as events
