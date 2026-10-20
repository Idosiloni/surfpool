# Surf Pool Sim — Design

## Pitch
You own a run-down surf wave pool. Walk around in first person, sell surf sessions, run the
wave machine, keep surfers happy, and grow it into the best surf park around.

## Core loop
Surfers arrive → buy a session at the desk → wait their turn → ride waves → leave happy (or not)
→ you earn money and reputation → spend on upgrades → more and better surfers arrive.

## Decisions so far
- View: first person
- Engine: Unity 6 (URP), single-player
- Art style: **low-poly cartoon** (like TCG Card Shop Simulator). Custom models built in Blender (via Blender MCP)
  plus free CC0 packs (Kenney, Quaternius). Gray placeholders until the "Better world" step.
- The wave is faked (animated mesh + shader), not simulated water.

## Roadmap (each step ends playable)
- [x] **Step 1 — Wave:** walk around a pool in first person, press a button on the wave machine, a wave rolls.
- [x] **Step 2 — First surfer:** one NPC walks in, pays at the desk, rides the wave, leaves.
- [x] **Step 3 — Money loop:** ticket prices, cash register, money HUD, buy an upgrade.
- [ ] **Step 4 — Grow:** side businesses and depth, one at a time (order chosen 2026-10-20):
  - [x] Board rentals (rental stand upgrade, rack → renter, return bin → rack restock chore)
  - [x] **More waves:** wave menu (Beginner / Intermediate / Pro), surfer skill levels, wipeouts, wave upgrades
  - [x] **More upgrades:** upgrade tree with tabs, staff that automate chores (with wages), park rating, more capacity
  - [ ] **Better UI:** real HUD, upgrade computer screen with tabs/icons, wave control panel (UI Toolkit)
  - [ ] **Better world:** low-poly models (Blender + CC0 packs), stylized water with foam, lighting, sky
  - [ ] **Player surfing:** grab a board, paddle out and ride waves yourself in first person (after Better world)
  - [ ] Later: snack bar, surf shop, lessons, cleaning, day/night, bigger pool

## Ideas parking lot
- Surfer wipeouts (funny ragdolls)
- Wave designer: size, speed, left/right breaks
- Surf competitions as events
