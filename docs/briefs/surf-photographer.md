# Brief: Surf Photographer (proposal, not built)

Ranked #2 in `docs/research/polish-and-next.md` (the #1 juice pass is done). It's the most "only in our game"
idea: a camera job inside a surf sim. Size: L (2-3 sessions). Needs Ido's yes before building.

## The loop
1. Buy **Photo Tower** (Park upgrade, ~level 9, ~$4k): a small tower on the main pool's north deck with a long-lens
   camera on a tripod. E = step behind the camera (like the fishing rod: its own simple mode, Esc to leave).
2. Look through the lens: mouse aims, scroll zooms (2 zoom steps), left click = shutter (click + flash sound).
3. Each shot is **graded** from what's in frame, using things the game already knows:
   - a surfer is riding (`Surfer` state) and is near the center of the frame,
   - the moment: a trick, an air, a tube ride or a celebration right now = bonus (Surfer events / Highlights),
   - the surfer is big enough in frame (zoom), the board and the wave lip are both visible,
   - pro > intermediate > beginner.
   Grade = C / B / A / S with a short reason ("Late! He already landed", "Too far", "PERFECT: in the barrel").
4. The shot goes to a **Photo Kiosk** by the exit (or the gear shop counter): the surfer in the photo stops on the
   way out and buys a print if it's B or better ($15 B, $30 A, $80 S; times their skill multiplier).
5. S shots go on a **Wall of Fame** board in the park (real render textures, the last 6), and count toward a
   "Shoot 10 S photos" daily challenge.
6. Later: **Hire Photographer** (staff) shoots by themself, mostly B and A shots.

## Why it fits
- New verb (aim + timing), not another counter. Skill matters and is easy to read (grade + reason).
- It makes the surfing you already built (tricks, tubes, celebrations) worth watching closely.
- Streamer-friendly: S-grade barrel shots, a wall of your best photos.

## Tech notes
- Shot = `Camera.Render()` into a small RenderTexture (like `EditorTools.DevShots`), saved as a Texture2D for the
  kiosk / wall (keep the last ~12 in memory, no disk).
- Grading: project each riding surfer's position into the photo camera's viewport; score center distance, size
  (from bounds), state flags. All data exists on `Surfer` and `CharacterRig`.
- UI: a lens overlay (UXML + USS: frame lines, zoom, grade stamp that pops in like the OWNED stamp).
- Save: count of prints sold + S shots (not the images).

## Open questions for Ido
- Should the player be able to take photos anywhere (a phone camera) or only from the tower?
- Prints price and how often surfers buy.
- Is it OK that some surfers look at the camera and pose (shaka) when you aim at them?
