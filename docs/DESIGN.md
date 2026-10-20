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
- Art style: undecided (low-poly cartoon vs. semi-realistic). Gray placeholders until we compare both.
- The wave is faked (animated mesh + shader), not simulated water.

## Roadmap (each step ends playable)
- [x] **Step 1 — Wave:** walk around a pool in first person, press a button on the wave machine, a wave rolls.
- [x] **Step 2 — First surfer:** one NPC walks in, pays at the desk, rides the wave, leaves.
- [x] **Step 3 — Money loop:** ticket prices, cash register, money HUD, buy an upgrade.
- [ ] **Step 4+ — Grow:** side businesses and staff, one at a time:
  - [x] Board rentals (rental stand upgrade, rack → renter, return bin → rack restock chore)
  - [ ] Snack bar
  - [ ] Wave menu (beginner / intermediate / pro, left/right) with price multipliers
  - [ ] Hire staff (lifeguard, desk clerk, rental clerk) to automate chores
  - [ ] Surf shop, lessons, cleaning, bigger pools, day/night, reputation

## Ideas parking lot
- Surfer wipeouts (funny ragdolls)
- Wave designer: size, speed, left/right breaks
- Surf competitions as events
