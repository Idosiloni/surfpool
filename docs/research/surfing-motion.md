# Surfing motion: how real bodies move, mapped to our rig
Date: 2026-10-20

How to read this: **[S]** = from a source (linked). **[E]** = my estimate, not in any source. Coaching pages are surf-school marketing, so they give *advice*, not measurements; only the pop-up and paddling papers are real measurements. I could NOT get YouTube transcripts (Surf Simply, Kale Brock etc.: no transcript reachable through search/fetch). Ask Ido to watch 2-3 videos with this file next to him to check the [E] numbers. Ride times for Waco, The Wave Bristol and Alaia Bay were not found.

Already covered elsewhere: board types and wipeout names ([boards-and-tricks.md](boards-and-tricks.md)), skill ladder ([surfing-basics.md](surfing-basics.md)).

## Our rig, in one paragraph
Joints: model root (pos+rot), hips height, torso, head, 2 shoulders, 2 hips. **No knees, no elbows, no ankles.** So "crouch" = hips drop + legs splay sideways (`splay = acos(hipsY/0.86) + 8`) so feet stay on the deck. Because the model is yawed 90 degrees on the board, leg z-splay becomes *foot spacing along the board*. Leg z is therefore **derived**, not free; only hip x (tuck) is free. Foot angles on the board (45 degrees etc.) can't be shown. Signs: torso +x = bend forward, head -x = chin up, arm/leg forward = -x. In `Ride()` the arm numbers are "front arm / back arm" (the `side` flip for goofy is applied afterwards), and head/torso yaw are multiplied by `side`.

## 1. Paddling
| Fact | Value | Source |
|---|---|---|
| Lie centered on stringer, nose just out of water | nose 2-5 cm above water (or 2-3 in) | [S] [Rapture paddle](https://www.rapturecamps.com/learn-to-surf/paddling/paddle-technique), [Rapture pop-up](https://www.rapturecamps.com/learn-to-surf/surf-fundamentals/pop-up) |
| Chest | gentle arch, chest slightly lifted; too high sinks the tail | [S] same. A 20-surfer sprint study found chest DOWN was faster than chest up ([Technique adjustments](https://www.sponet.de/sponet/Record/4028678?lng=en)) |
| Legs | together, still, toes just off the tail | [S] Rapture |
| Cruise cadence | one full left+right cycle every 2-3 s (= 20-30 cycles/min per arm) | [S] Rapture |
| Max effort cadence | about 0.94 Hz on a soft board (about 56 cycles/min per arm), stroke length 1.7 m | [S] [kinematics study](https://bms.sport-iat.de/bms/Record/4065212) (my conversion) |
| Arm path | fingers in first, enter 15-20 cm ahead of head, pull close to the rail to past the hip, elbow leaves water first, low recovery | [S] Rapture, sprint study (low arm recovery faster) |
| Head/torso | steady; side-to-side rocking adds drag | [S] Rapture |
| Look back | glance over shoulder every 5-10 strokes; spot the wave | [S] Rapture, [first waves](https://www.rapturecamps.com/learn-to-surf/surf-fundamentals/first-waves) |
| Catching | start paddling hard 3-4 s before the wave arrives, then head down and commit, no more looking back | [S] first waves |
| Wave lifts you | stop paddling, hands flat beside chest, cobra arch; pop up | [S] first waves |

Our `Paddle()`: `phase += dt*4` = 0.64 Hz per arm = 38 cycles/min. That is a plausible hard paddle but too fast for cruising and not fast enough for a sprint.

## 2. Pop-up
Measured (23 surfers, about 12 years experience, lab pop-up): **total 1.20 +/- 0.19 s; push-up 0.71 s (61%), reaching/landing 0.48 s (39%)**; hands 0.46 m apart, feet land 0.63 m apart; front foot takes about 61% of peak force ([PMC study](https://pmc.ncbi.nlm.nih.gov/articles/PMC7961430/)). Younger intermediates 1.12 s vs older intermediates 2.03 s ([Waikato](https://researchcommons.waikato.ac.nz/entities/publication/cea19540-31f7-410a-afa8-cefd35dc87e2)). Coaching: about 1 s with no knee stage; beginners should aim to be up within 2-3 s of the catch ([Rapture pop-up](https://www.rapturecamps.com/learn-to-surf/surf-fundamentals/pop-up)).

| Phase | What the body does | Time (pro / beginner) |
|---|---|---|
| Hands down | palms flat beside lower ribs, fingers forward, not on the rails | 0 |
| Press up | arms straight, chest high, hips and legs still on board | 0.5 s / 0.8 s [E for pro; study total x 0.6] |
| Back foot | back knee tucks, foot lands across the board near the tail pad (about 45 degrees) | 0.2 s / 0.5 s |
| Front foot | front foot lands between the hands, near the stringer | 0.15 s / 0.4 s |
| Rise | hands come up to chest height, low athletic crouch, about 60/40 weight front | 0.15 s / 0.5 s |

Totals: pro about 1.0 s (the study's 1.2 s is a lab "simulated" pop-up, real pros are likely faster, [E] 0.8 s); beginner 1.5-2.5 s [S+E].
Eyes: pick the target (beach, down the line) BEFORE popping and keep looking there. Looking down shifts weight forward and dives the nose ([S] Rapture).
**Beginner mistakes to animate:** knee-drag (stop on the knees, then stand: the "old man pop-up"), popping up too late, grabbing the rails, feet too close together, looking down, stiff locked legs after standing ([S] Rapture pop-up, [campvine](https://campvine.com/camp-blog-detail/75r2h3l4m3q49583/common-mistakes-new-surfers-make-and-how-to-avoid-them)). Hand spot differs by coach (under chest vs under shoulders): harmless.

Our `PopUp()` runs **0.55 s** split 30/30/40 (push-up lasts 0.165 s). That's half the measured time and the push-up phase is far too short (real: 60% of the time).

## 3. Stance
| Fact | Value | Source |
|---|---|---|
| Regular vs goofy | about 60-70% regular, 30-40% goofy (we use 35% goofy: fine) | [S] [Rapture stance](https://www.rapturecamps.com/learn-to-surf/surf-fundamentals/stance) (rough; a 12/11 split in the PMC study is just a small sample) |
| Foot spacing | shoulder-width or a bit more, centered on stringer | [S] Rapture |
| Foot angles | front 20-30 degrees toward nose (some coaches say 45), back more across the board (15-25, some say 90) | [S] Rapture, [Surf Today](https://www.surfertoday.com/surfing/how-to-adopt-a-proper-surfing-stance). Coaches disagree |
| Weight | about 60% back foot / 40% front for a basic stance | [S] Rapture (the pop-up page says 60% front: coaches disagree; peaks at the front foot after landing) |
| Knees | clearly bent, "like sitting in a low chair" | [S] Rapture |
| Hips/torso | hips low and slightly open toward the nose; lead shoulder dropped and pointing down the line, trailing shoulder back and a bit higher | [S] Rapture |
| Arms | front arm loosely extended toward travel, back arm relaxed at side/behind, hands about waist height; arms above shoulder height = too high | [S] Rapture stance, [intermediate turning](https://www.rapturecamps.com/learn-to-surf/surf-maneuvers/intermediate-turning) |
| Eyes | up on the wave face / shoreline / down the line, never at the feet | [S] |
| Poo stance | bending at the waist with the bum pushed out, instead of bending knees | [S] Rapture stance |
| Other beginner faults | too narrow = wobble; feet too far back (tail buried); locked front knee; stiff straight stand | [S] |

**Our spacing check [E, assumes hips stand about 0.9 m high; I did not read the prefab]:** spacing = 2 x 0.86 x sin(splay). Beginner crouch 0.15: splay 30 degrees, spacing about 0.87 m. Pro crouch 0.55: splay 49 degrees, spacing about **1.3 m**. Real is about 0.6-0.9 m for a 1.8 m person (landing width 0.63 m). Our deep pro crouch has feet too far apart. Fix idea in change #6.

## 4. Riding maneuvers (body sequence; times are mostly [E])
| Move | Body sequence | Duration |
|---|---|---|
| Trim / down the line | low-mid crouch, weight slightly back-center, upper third of the face, eyes down the line, front arm leading, back arm low behind. Speed comes from gravity, not effort | any length |
| Pump | extend on the downhill part of the face, compress on the uphill, **about 1 pump per second**, weight shifts only about 55/45 (front on the way down, back on the way up), legs work and the upper body stays quiet, arms move in sync ([S] [speed generation](https://www.rapturecamps.com/learn-to-surf/surf-maneuvers/speed-generation)) | 2-4 pumps in a row [E] |
| Bottom turn | 1) speed 2) **compress** deep (hips toward the board) 3) eyes + lead shoulder start the turn, look where you want to go 4) **extend** through the arc, drive through the rail 5) compress again to finish. Lead arm points where you're going (rudder), trailing arm waist-chest height and slightly behind ([S] intermediate turning; [Barefoot](https://barefootsurftravel.com/?p=9673)) | about 1 s [E] |
| Top turn / snap | rotate the upper body fully, shift weight to the back foot at the top, spray ([S] intermediate turning) | about 0.5 s [E] |
| Cutback | **order: head, lead shoulder, trailing shoulder, hips, board**, each a beat behind the one above; head and shoulders turn nearly 180 degrees; weight 65-70% back foot, front foot light; lead arm reaches back toward the breaking section; pump once or twice first; start high on the face ([S] [Rapture cutback](https://www.rapturecamps.com/learn-to-surf/surf-maneuvers/cutback), [Wavelength](https://wavelengthmag.com/?p=5715)) | 1.5-2.5 s whole S-turn [E] |
| Floater | shallow bottom turn, weight to back foot near the lip, hands up and forward to unweight the nose, stay low over the foam, then turn head/shoulders back toward the beach to come down ([S] [Barefoot](https://tutorials.barefootsurftravel.com/articles/how-to-do-a-floater/)) | 0.6-1 s [E] |
| Re-entry | no coaching source found. [E] hard top turn at the lip, bigger than a snap | about 0.7 s [E] |
| Tube | pull-in: stall, drop to an ultra-low stance, hips low, chest near thighs, eyes on the EXIT. Trailing hand touches/drags the wall = brake; release to speed up; lead arm toward the exit. Backside "pigdog": drop back knee, lead hand grabs the rail ([S] [Rapture barrel](https://www.rapturecamps.com/learn-to-surf/surf-maneuvers/barrel-riding/), [Carve](https://www.carvemag.com/?p=10722)) | fraction of a second up to 10+ s ([S]) |
| Air | speed, approach 60-80 degrees up the face, **compress deep, then explosive extension at the lip, grab the rail with the LEAD hand**, pull knees to chest (straight legs lose the board), look at the landing, extend legs just before touchdown, **compress deeply on contact**; stiff legs bounce you off ([S] [Rapture aerials](https://www.rapturecamps.com/learn-to-surf/surf-maneuvers/aerial-basics)) | airtime not sourced; our 0.7-0.8 s is fine |
| Kick-out | no source. [E] drive up and over the back of the wave, lean back, hips drop, then drop onto the board and paddle | 0.5 s [E] |

## 5. Frontside vs backside
| Body part | Frontside (facing the wave) | Backside (back to the wave) |
|---|---|---|
| Head | sees the face without turning | must turn over the trailing shoulder or surf "blind" |
| Shoulders | lead shoulder opens to the face, chest opens naturally | trailing shoulder pulled back and up, chest twisted toward the wave (unnatural) |
| Knees | angle in toward the face | angle out while the upper body twists in |
| Pressure | toe-side rail | heel-side rail |
| Feel | easier, more power | harder ([WSL](https://www.worldsurfleague.com/ultimate-surfer/challenges/482577)) |
Source: [Rapture take-off](https://www.rapturecamps.com/learn-to-surf/take-off/frontside-vs-backside-take-off). Which side you are on depends on stance and wave direction (regular on a right = frontside).

## 6. Wipeouts
- **Off the back / falling backward:** arms go up, body flat. **Pearl/nosedive:** nose digs, rider pitches forward over the nose. **Over the falls:** thrown forward and down by the lip. (Names in [boards-and-tricks.md](boards-and-tricks.md).)
- Safe falling: jump to the SIDE away from the board (arm's length), never over the nose or toward the fins. Deep water: "pencil" (legs together, toes down). Shallow: flat, arms and legs spread, stay in the top 1-2 ft. **Arms over the head, forearms crossed, on entry AND when surfacing** ([S] [Rapture falling](https://www.rapturecamps.com/learn-to-surf/surf-fundamentals/falling-safely), [Seabreeze](https://www.seabreeze.com.au/Articles/Surfing/Wiping-Out-Safely-How-to-Fall-in-the-Surf)).
- Underwater: curl loosely, tumbling stops in 2-5 s, typical beginner hold-down 3-8 s, bad ones 10-15 s ([S] Rapture). Pool waves are shallower and smaller, so [E] 1-3 s.
- Surfacing: arms up, feel for the board before lifting the head, grab the board, check the horizon.
Our `Flail()` windmills the arms the whole time (cartoon fun, but no real surfer does it for long); `Tumble()` is 1.8 s, which is a good game length.

## 7. Wave-pool specifics
| Place | Ride length | Source |
|---|---|---|
| Surf Ranch (Kelly Slater Wave Co) | about 45 s; Slater himself thinks about 20 s is ideal | [S] [Wave Pool Mag](https://wavepoolmag.com/how-the-kelly-slater-wave-pool-works/), [Lookout](https://lookout.co/olympic-wave-of-the-future-kelly-slater-on-whether-his-surf-ranch-or-other-wave-pools-could-play-host/story) |
| URBNSURF Melbourne advanced | up to 16-18 s, 7 ft faces | [S] [Raised Water](https://raisedwaterresearch.com/?p=294), [Stab](https://stabmag.com/news/exclusive-stab-interrogates-the-man-behind-australias-first-wavepool/) |
| URBNSURF intermediate | 12-16 s, 3-4 ft | [S] same |
| URBNSURF beginner bay | up to 10 s of whitewater rollers (1-3 ft) | [S] same |
| Waco, The Wave Bristol, Alaia Bay | not found | - |

- Maneuvers per ride: one real example, Toledo at Surf Ranch 2021: **10 turns, 2 air reverses, 1 barrel in about 45 s = a move every 3-4 s** ([S] [WSL photo feature](https://www.worldsurfleague.com/posts/473736/2021-jeep-surf-ranch-pro-photo-feature) via search summary). So a 15 s pool ride holds about 3-5 moves [E].
- Beginner whitewater ride posture: prone first, then pop-up; stand with knees bent, arms out for balance, hands near waist, eyes on a fixed point on shore, ride 30 m+ straight ([S] [first waves](https://www.rapturecamps.com/learn-to-surf/surf-fundamentals/first-waves)).
- **Ours:** `trickInterval` = 1.2-2.2 s (a trick about every 1.7 s, for ALL skills with matched waves). That is about double the pro rate and way too many for beginners.

## 8. How this maps to our rig
Ride poses use `Ride()`'s frame: front arm = the arm on the nose side, back arm = the other. hipsDrop is relative to standing hips height (m; `hipsY` in code is `hipsHeight - drop`). Torso/head yaw "toward travel" = the sign `-side` in code is used today; I give magnitudes. Hip z is NOT listed (derived from hipsDrop). Board roll/pitch are RideFeel's `Tilt` (wave face + lean) plus extras. Anything marked [E] is a starting point to tune by eye.

| Pose | hipsDrop | Torso pitch (+fwd) / yaw toward travel / roll | Head yaw toward travel / pitch | Front arm x,z | Back arm x,z | Hip x (both) | Board roll / pitch | Time |
|---|---|---|---|---|---|---|---|---|
| Paddle cruise (lying) | - | -10 (arch) / 0 / +-3 per stroke | 0 / -35 (current) | stroke 0.4 Hz | stroke 0.4 Hz | +-8 | 0 / 0 | cycle 2.5 s |
| Paddle sprint | - | -3 / 0 / +-5 | 0 / -25 | 0.9 Hz | 0.9 Hz | +-8 | 0 / 0 | 3-4 s before the catch |
| Look back | - | 0 / 0 | 70 / -10 | continues | continues | - | - | 0.6 s every 5-10 strokes |
| Pop-up (see section 2) | 0 -> 0.5 -> 0.3 | 62 pitch body, then 50 fwd -> 25 | 0 -> 50 -> 80 | -85,12 -> -60,25 | same | -10 -> -25 -> 0 | 0 | pro 1.0 s / beginner 2.0 s |
| Beginner stance (whitewater) | 0.10-0.15 | 28 / 12 / wobble +-5 | 70 / -10, looks down 20% of the time | -15, 80 | -25, 70 (wide, windmill) | 0 | wobble +-6 | held |
| Good stance / trim [E] | 0.25-0.30 | 20-25 / 25 / 0 | 90 / -5 | -15, 70 (toward nose, waist-chest) | -25, 30 (low, behind) | 0 | 5-10 toward the wall | held |
| Pump [E] | 0.20 <-> 0.32 sine | 20 / 20 / 0 | 90 / -5 | as trim, move +-10 in sync | same | 0 | 0 | period 1.0 s |
| Bottom turn | compress 0.35 -> extend 0.15 | 30 -> 15 / 40 / lean 10 | 90 -> 100 (looks up the face first) / -5 | pointing travel, z 75 | -30, 25 | 0 | lean 25-32 | about 1 s |
| Top turn / snap | 0.30 | 15 / 60 / lean 15 | 100 / 0 | 70 | 30 | 0 | lean 30 + ExtraRoll | 0.5 s |
| Cutback | 0.30 | 10 / **climbs to 120-170 then back** / 10 | **leads: 150 then back** | reaches back toward the foam | behind, low | 0 | lean 25-30, direction flips | 1.5-2.5 s |
| Floater | 0.25 | 15 / 30 / 0 | 90 / -5 | up, forward (-40, 60) | up (-30, 50) | 0 | along the lip, pitch up 10-15 | 0.8 s |
| Tube [E] | 0.40 (max, see feet note) | 45 / 20 / 0 | 90 / +10 (eyes up at the exit) | toward exit, z 80 | trailing arm touching the wall: z 15, x -60, +-10 as it drags | 0 | 10 | held |
| Air | 0.35 compress -> tuck | 40 -> 25 | 90 / look down at landing zone | lead hand grabs rail (-70, 12) | back at side | -35 tuck | board stays under the feet | 0.7 s, then land with 0.45 squash (current) |
| Wipeout cover | - | 20 | -30 | arms UP over the head, crossed: (-160, +-20) | same | -20 | - | until surfaced |

**Feet note:** with no knees, deeper hipsDrop = wider feet. At my 0.9 m assumption, drop 0.30 gives about 1.1 m spacing, still wide (real about 0.6-0.9 m). Cheap fix: cap hips drop at about 0.30 and get "lowness" from torso pitch + hip x +10..15 on both legs (a fake knee bend, bottom pushed back a bit) and a smaller `+8` constant in `splay`. Tube depth 0.40 will look wide; accept it or add a thigh tuck.

### First-person camera feel (`PlayerSurfing.UpdateCamera`)
- Real POV (helmet/head cam): the rider looks DOWN THE LINE, not at the nose; the head stays fairly level while the body leans, but head-mounted footage still tilts and shakes. Forum advice from a SUP rider: face forward and glance sideways, big head turns shake the footage; chest/board mounts shake less ([Seabreeze thread](https://www.seabreeze.com.au/forums/Stand-Up-Paddle/SUP/Helmet-cam-and-image-stabilization--vid-)). I found no measured horizon-tilt data: the rest is [E].
- [E] Real horizon tilt in a hard bottom turn is large (15-30 degrees); our `maxCameraRoll` = 7 is safe. Raise to about 10-12 for hard turns, keep it eased, and for comfort keep the horizon close to level (roll lags the board's lean by ~0.2 s, which it already does with `Exp(-6)`).
- [E] Eye height for a 1.75 m rider over the board: lying paddling about 0.4-0.5 m above water (we have `PaddleEye` 0.6), good stance about 1.2-1.3, deep crouch about 0.9-1.0, tube about 0.8. We have `StandEye` 1.4 minus up to 0.3: roughly right; allow down to 0.9 in tubes and bottom turns.
- Pop-up: eye should travel up over about the pop-up time (pro 1.0 s / beginner 2.0 s). We hold 0.85 for 0.18 s only.
- Look direction: when turning, a real rider's gaze leads the board by 0.1-0.2 s [E]; your camera follows the mouse, so add a gentle yaw nudge toward the turn only if it doesn't fight the mouse.
- Paddle bob `sin(t*4)*0.03` is fine; add a small left-right roll of +-1.5 degrees in time with strokes at the real cadence.

### Prioritized changes (biggest realism for least work)
1. **Pop-up timing and shape:** `CharacterRig.popUpTime` 0.55 -> 1.0 s (pro) to 2.0 s (beginner) driven by `Style`; in `PopUp()` change the 30/30/40 split to about 50/20/30 (long push-up, quick back foot then front foot). Add an optional knee-drag stall for Style 0. In `PlayerSurfing`, stretch the 0.18 s low-eye window (`popUpStart`) to about 0.8 s with a rising eye.
2. **Head and eyes:** `Ride()` head yaw is 70 deg (not even looking fully down the line). Use about 90 for good stance, 100 into a bottom turn, and 20% "look down" dips for beginners. Add a `LookYaw` input on `CharacterRig` so `Surfer.UpdateRideFeel` can lead turns with the head (head first, then shoulders, then hips).
3. **Make cutback/snap/floater different from each other:** `Surfer.DoTrick` gives all of them the same 0.45 s roll pulse. Add `CharacterRig.Twist` (torso+head yaw offset, +-120) so a cutback does a 180 degree head/shoulder counter-rotation with the board lean flipping, a floater lifts the arms and pitches the board up, and a snap is short and hard. (Path change too: cutback = go back toward the foam, then return.)
4. **Trick rate:** `trickInterval` 1.2-2.2 s -> around 3-4 s for pros, 5+ s for intermediates, almost none for beginners (they trim/wobble). Real pros do a move every 3-4 s.
5. **Pump and trim rhythm:** between tricks, `Ride()` should bob hips ~1 Hz (0.1 m) with a small front/back hip x shift, instead of standing still. Cheap and makes every ride look alive. Keep upper body quiet.
6. **Stance proportions:** cap crouch so feet stay about 0.6-0.9 m apart, add torso pitch/hip x for "low", move the front arm to point toward travel at waist-chest height, keep arms below shoulders, and give beginners a visible "poo stance" (hips back, torso pitch 35+) and wide arm windmilling.
7. **Paddling:** `Paddle()` rate 0.64 Hz per arm: make cruise about 0.4 Hz, sprint about 0.9 Hz for 3-4 s before a catch, add a periodic look-back (head yaw 70, 0.6 s) and a slight chest arch. Needs a `Sprint` flag on `CharacterRig` set by `Surfer`/`PlayerSurfing` when a wave is near.
8. **Wipeout variety and cover:** `Flail()`: split into off-the-back (fall flat on the back, arms up), pearl (pitch forward over the nose) and over-the-falls, then arms go over the head (-160 x, crossed) while underwater and when surfacing. Keep `Tumble` about 1.8 s.
- Minor: air grab with the lead hand (Rapture's beginner advice) instead of the back hand in `Ride()`; both look fine, lead hand is the one coaches teach.

## Ideas for our game
- **Step 5 polish, "Characters: real rigs/animations":** do changes 1, 2 and 4 first (about one session): they only touch numbers in `CharacterRig`, `Surfer.trickInterval` and one `PlayerSurfing` constant. Playtest by watching one Beginner and one Pro ride side by side.
- **Step 5 polish:** add `Twist`, `LookYaw` and `Sprint` as new public properties on `CharacterRig` (same pattern as `Lean`/`Crouch`/`Air`) so the rig code stays dumb and `Surfer` decides the story.
- **Step 5 polish:** a "style judge" bubble that names what the rider is doing ("Pumping", "Cutback"), driven by the same state that picks the pose, so Ido can see it matches.
- **Step 5 / Lessons area (planned):** a coach NPC that demonstrates the pop-up at slow speed (2.5 s) on the deck, then at pro speed (1 s); beginners in the lesson get the knee-drag and look-down faults, which fade as their skill grows.
- **Step 4+ growth:** skill progress shown visibly: pop-up gets faster (2.0 -> 1.0 s), stance gets lower, head looks down the line more. Players see their guests improve after buying Surf Coach.
- **Later:** a replay/photo cam (influencer NPC) works best if poses have clear peaks (compress, extend, tuck); the keyframe table above gives those.
- **Later:** ride lengths as upgrade goals: beginner 10 s, intermediate 12-16 s, advanced 16-18 s, "Surf Ranch" 45 s legend wave.
