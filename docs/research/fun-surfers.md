# Fun surfers: making NPC surfers charming, funny and worth watching
Date: 2026-10-20

Tags: **[S]** = from a source (linked). **[G]** = general surf/game knowledge, no source found in my searches. **[Idea]** = my suggestion. Many searches returned thin results (no Animal Crossing animation write-up, no TCG Card Shop customer list, no Parkitect bubble info, no Dave the Diver regulars list, no definitions of "hoot" or "party wave"). I say so where it matters.
Not repeated here: skill levels/etiquette ([surfing-basics.md](surfing-basics.md)), body poses ([surfing-motion.md](surfing-motion.md)), speed/juice/highlights ([surf-feel.md](surf-feel.md)).

## 1. What our NPC surfers do today (read from the code)
- **One personality:** every `Surfer` is the same person with a random skin/hair/shirt/board color (`Surfer.Init`). The only differences are `Skill` (Beginner/Intermediate/Pro), `Goofy` and `PreferredDirection`. Nothing about *who* they are.
- **Words:** `Shout(text, seconds)` writes one string that `GameHud.UpdateBubbles` shows above the head. Lines come from a few static arrays (`RideShouts`, `HappyShouts`, trick names) plus status text. Bubbles look the same for every surfer (only a skill badge from `CreateBubble(surfer.Skill)`).
- **Body:** `CharacterRig` modes Locomotion/Paddle/Ride/Sit/Lie/Flail/Straddle. Inputs: Style, Lean, Crouch, Air, Twist, ArmsUp, CheerArms, Sprint. Sitting in the lineup (`Straddle`) has no fidgets. Waiting in the desk line is a plain idle.
- **Wipeout:** `StartWipeout` + `Tumble`: spin on a random axis for 1.8 s, sink 0.9 m, board spins on its own. Every wipeout looks the same. `Flail` mode is a generic arms-and-legs wave.
- **Finish:** `FinishRide` just says "One more!" / a happy line. No celebration pose. Happy surfers walk out with a tip line for 5 s.
- **Reactions:** `Highlights.Moment` makes `Spectator`s (grandstand/rail) throw arms up; the other surfers in the water ignore everything. Spectators never look at what they cheer for.
- **Surfer-to-surfer:** only "Your wave, go go!" when skipping a wave because of a drop-in (`TryCatchWave`). No hooting, no chatting, no drama.

## 2. How other games make NPCs memorable
| Game | What works | Source | Lesson |
|---|---|---|---|
| Two Point Hospital | Each character has traits that combine into a personality; two argumentative ones clash and make small stories. Animators aim for enough expression that you care about tiny people. Many gags are *visual* (light-bulb heads) and puns in names. Some traits only change looks/jokes, some change numbers (Grumpy lowers happiness). | [PCGamesN dev diary](https://pcgamesn.com/two-point-hospital/two-point-hospital-personalities-dev-diary), [Magic Game World](https://www.magicgameworld.com/?p=23562), [Intel](https://game.info.intel.com/gaming-access/wacky-characters-meet-rewarding-micromanagement-in-two-point-hospital) | Few traits, mix "flavor only" and "changes gameplay". Traits that interact (kook + local) make stories for free. |
| Planet Coaster | Guests have a "guest brain" with thoughts, moods and money. Players complain thoughts are repetitive, irrelevant, and that guests don't react strongly to rides (e.g. scared after a scary ride). Thoughts are clues for the manager. | [Frontier forum](https://forums.frontier.co.uk/goto/post?id=7895452), [PC2 tips](https://www.gfinityesports.com/article/planet-coaster-2-beginners-tips) | Bubbles must be varied and *useful or funny*, never five ways of saying the same thing. Reactions should show on the body, not only in text. |
| Animal Crossing | 8 villager personalities (cranky, jock, lazy, smug, normal, peppy, snooty, sisterly) that change speech, clothes, hobbies and how they treat each other; personality is separate from looks. | [Thonky](https://www.thonky.com/animal-crossing-new-horizons/list-of-villager-personalities), [TheGamer](https://www.thegamer.com/animal-crossing-new-horizons-villager-design/) | Personality = a *voice* (word lists) + a few behaviors. 6-8 types is plenty. |
| The Sims | Moods (moodlets) and traits drive idle behavior: a sad Sim cries mid-action, a slob burps while idle, playful Sims laugh randomly; acting on your trait gives a bonus. | [Sims wiki: autonomous reactions](https://sims.fandom.com/wiki/Autonomous_reactions), [GMTK transcript](https://amara.org/subtitles/cVoJS4OdmVql/en/1/download/The%20Genius%20AI%20Behind%20The%20Sims.en.txt) | Idle fidgets are how a trait shows when nothing is happening. |
| Fall Guys | Short stubby legs, big arms: "funniest in the air". Built to be badly suited for the task. Bold costumes help you spot yourself in a crowd. | [GGRecon](https://www.ggrecon.com/articles/fall-guys-developers-reveal-what-makes-their-jelly-beans-so-lovable), [PlayStation blog](https://blog.playstation.com/?p=224711) | Silhouette + outfit = recognizable at a glance. Failing should be funny, not sad. |
| TABS (Landfall) | Comedy comes from physics accidents; each match is a mini sketch. Reviewer thinks the wobble *is* the game. | [GameGeeker review](https://gamegeeker.com/de/games/totally-accurate-battle-simulator/review) (one reviewer's reading) | We do not need real physics: *authored* funny wipeouts (several variants, random) give the same "what will happen" tension. |
| TCG Card Shop Simulator | The famous NPC bit is the smelly customer with a green aura that makes others leave; you spray them. Players like the visual gag, but dislike having to stop other work, and want a kick-out option. | [Insider Gaming](https://insider-gaming.com/deal-smelly-customers-tcg-card-shop-simulator/), [Automaton](https://automaton-media.com/en/news/tcg-card-shop-simulator-lets-you-spray-stinky-customers-with-deodorant/) | A visible problem character is memorable, but give the player a quick, satisfying fix. |
| Supermarket Simulator | Reviewer jokes that shoppers complain about trash they dropped themselves; shoplifters get overwhelming by late game. | [Thumb Culture](https://www.thumbculture.co.uk/supermarket-simulator-pc-review), [Steam thread](https://steamcommunity.com/app/2670630/discussions/2/4354495576379864784) | Small human hypocrisy is funny. Keep problem characters rare enough not to pile up. |
| Dave the Diver | Special VIP customers appear when word gets around; cosplay guests. (No regulars list found.) | [Wikipedia](https://en.wikipedia.org/wiki/Dave_the_Diver) (thin) | Rare special visitors that make you work a bit differently. |
| Theme Hospital, Parkitect, Overcooked, Goose Game, Human Fall Flat, Wobbly Life | I found no usable sources in my searches. From my own knowledge [G]: Theme Hospital's loud PA jokes and visual illnesses; Overcooked's panic animations; Goose Game's tiny reactions to you. Treat as inspiration only. | | |

**Patterns [S+G]:** (1) a few clear types, (2) idle fidgets, (3) visible reactions to events, (4) rare special guests, (5) funny failure, (6) varied text, not repeated.

## 3. Surf culture to borrow (for NPC types)
Sources: [Lushpalm: kook](https://lushpalm.com/what-is-a-kook/), [The Inertia on locals](https://www.theinertia.com/?p=121505), [Wavetribe glossary](https://wavetribe.com/blogs/surfboards-waves/glossary-of-surfing-terms-and-surf-slang), [SurferToday maneuvers](https://www.surfertoday.com/surfing/the-complete-list-of-surfing-tricks-and-maneuvers), [Wikipedia glossary](https://en.wikipedia.org/wiki/Glossary_of_surfing). I did not find sources for groms/soul surfers/surf dads as types; those descriptions are [G].

| Type | Behavior | Our hook |
|---|---|---|
| **Kook** | Beginner or not, doesn't know etiquette; drops in, snakes [S]. | Occasionally takes off too close; victim yells. |
| **Grom** | Young surfer [S], small, fearless, hyper [G]. | Smaller model scale (0.75), fast paddle, tries tricks too big, wipes out and laughs. |
| **Local** | Territorial, loud, calls others kooks [S]. Respected ones give waves away [S]. | Sits closest to the peak, grumbles at kooks, but cheers real talent. |
| **Soul surfer / longboard cruiser** | Relaxed, arched back, hang five/ten toes on the nose [S for terms]. | Never in a hurry, long smooth rides, nose-walk, no tricks. |
| **Charger / shredder** | Takes the biggest waves, goes hard [G]. | High wipeout chance, big airs, bigger tips. |
| **Influencer** | Films self [G]. | Selfie pose, "Is this getting views?", gets mad if you ruin the shot. |
| **Surf dad / tourist hodad** | Dad on a foamie with a camera-phone; "hodad" = acts like a surfer but doesn't [S]. | Slow gait, sunhat, waves at the kids. |
| **Lifeguard** | Whistle, tower [G]; we already have `Lifeguard.Whistle`. | Reacts to wipeouts/arguments. |

**Lineup behaviors [G]:** hooting (yelling support when a friend catches a wave), party wave (several riders share one wave), snaking/drop-in with yelling, paddle battles for the peak, sitting and chatting, waxing the board, stretching/arm circles, fixing the leash, duck-diving (pushing the nose under a wave while paddling out), eskimo roll (longboarders flip the board over themselves to pass a wave), pouring water out of the ear, splashing water on the face.
**Moves with personality:** hang five/ten (toes over the nose) [S], soul arch (arched back, relaxed confidence) [S], tail slide [S], 360 [S], "claim" (arms up before the ride ends) [G], shaka "hang loose" sign [S], cheater five (Wikipedia redirects it to hang-five, so treat it as a hang-five variant [S, weak]), board-fling wipeouts [G].
**Slang for bubbles:** stoked, gnarly (good or bad) [S], shaka/"hang loose" [S], grom [S], kook, barney (uncool rookie) [S], hodad [S]. Plus [G]: "sick", "bomb", "send it", "pitted", "shred", "dawn patrol", "set coming!", "sorry, my bad!", "yeeew!".

## 4. What OUR rig can do (no new bones) [Idea]
The rig has: hips, torso, head, shoulders, elbows, hips (legs), knees, plus model offset/rotation. That is enough for all poses below; they are just new pose targets or timed offsets.

### 4a. Idle and lineup
- **Lineup fidgets (Straddle):** every 6-15 s one random gesture, 1.5-3 s long: look left/right (head yaw), look back at the sea, stretch arms overhead, arm circles, scratch head (one hand to head), splash the water with one hand (small spray), rub the eyes, check the leash (hand down by the ankle), bounce the nose of the board. New `CharacterRig.Gesture` enum + `PlayGesture(g)`; poses are small offsets on shoulders/elbows/head added on top of Straddle.
- **Sit and chat:** two sitting surfers within 3 m turn toward each other (rotate the model slightly), take turns doing a small talk gesture (one hand out, head nod) with a bubble each ("Dude, did you see that?"). Free story: bubbles pair up.
- **Desk-line body language by patience:** at patience > 0.6 relaxed sway; < 0.6 shift weight, look at wrist, arms crossed; < 0.3 foot tap and big sigh. `CharacterRig.Impatience` 0..1 fed from `Surfer.Patience`. The player gets a *visual* warning on top of the patience bar.
- **Duck-dive:** when a paddling-back surfer meets an oncoming wave crest, the nose dips, they sink 0.5 m under the surface for 0.8 s and pop back (reuse the `Tumble` sink maths). Needs "wave close" check like `WaveComing()`.
- **Walking with attitude:** `Gait` input: swagger (hips sway), bounce (grom), shuffle (dad), cool slow (soul). Scales the walk bob/arm swing the rig already has.

### 4b. Celebrations (after a good ride)
All are timed arm poses on top of the kick-out at the end (`kickOutStart`, `KickOutTime` 0.7 s in `Surfer`); extend that window to ~1.2 s for celebrators.
- **Claim:** both arms straight up as the wave ends (`ArmsUp = 1` already exists).
- **Shaka:** one arm raised, elbow bent, hand wobbles from side to side (rotate forearm +-20 degrees at 3 Hz). The hand is a small block, so it reads as a wave: fine at this scale.
- **Fist pump:** one arm up, elbow cycles 2 pumps.
- **Soul arch:** torso leaned back 15 degrees, head up, arms out low like wings, slow.
- **Hang five/ten (cruisers on longboards):** move the rider forward along the board (`StandSpot` becomes nose-side), front toes over the edge. Needs an input like `NoseWalk` 0..1 that shifts the stand spot forward. Only for boards longer than 2.4 m.
- **Tail slide / 360:** `Move` additions in `Surfer`; the 360 already exists as `airSpin`; add a ground-level `Move.Spin` that rotates `Face` direction 360 degrees over 0.8 s.
- **Cheer after others:** other lineup surfers hoot: one arm up, bubble "Yeeew!" (see 4e).

### 4c. Funny wipeouts (replace the one random `Tumble`)
Choose by archetype and by what is happening. Each is 1.5-2.5 s, all authored, then everyone pops up laughing or grumbling.
| Variant | How [Idea] |
|---|---|
| **Pearl** (nose dive) | Board nose dips under, the rider pitches forward over the nose in an arc (hips up, arms forward), splash, board shoots up into the air. For beginners at the start of the ride (`wipeoutAt` < 1 s). |
| **Cartwheel** | Rotate around the forward axis (not a random axis) 1.5 times with arms and legs spread, lands in a star shape. Pros attempting airs. |
| **Faceplant / belly flop** | Stiff body falls flat forward, big splash, then a thumbs-up from the water ("I'm OK!"). Beginners. |
| **Board fling** | Board is released and flies up in an arc (use the existing board transform, simple parabola with spin), lands 4-8 m away and bobs; the surfer must swim over to it (new short `SwimmingToBoard` step) with a bubble "My board!!". Funny and gives the lifeguard something to do. |
| **Washing machine** | Several slow rolls under water with bubbles; appears for the biggest waves. |
| **Slow-mo hero fail** | Pro in the tube wipes out: the camera slow-mo of `Highlights` already exists; call `Highlights.Moment(pos, 0.9f)` too. |
| **Walks it off** | Wipeout in whitewash; surfer stands up in shallow water, shrugs and thumbs-up. |
Add a reaction line by type (Section 5). Keep failure *never random-looking*: the board-fling and pearl should match the cause ("Too big for me!").

### 4d. Reactions to waves (good/bad)
- Good wave (diff == 0, quality 1, 2+ tricks): claim + shaka, higher tip text, nearby surfers hoot.
- Wave too small/big already skipped with a line: add a body tell: shrug (palms up, shoulders raised), shake head. 
- Got a wave stolen: arms out, "Seriously?!" (see drama below).
- Beginner pops up and stays up: both fists up and big grin; bubble "I'M STANDING!!" (rare, only first success).

### 4e. Interactions between surfers
- **Hoot:** when a lineup surfer catches a wave (`TryCatchWave` success), surfers within 8 m have a 40% chance to raise an arm and bubble "Yeeew!" / "Go go go!". Use a static event like `Highlights.Happened`. Cheap and makes the lineup feel alive.
- **Party wave:** two *friends* (or two grom/soul types) allowed to ride the same wave at a closer distance, both grinning, one giving a shaka to the other. Today `DropInSpace` prevents it; add `friendly` exception for ~15% of pairs. Big payoff screenshot.
- **Drop-in drama:** a kook ignores `DropInSpace`. The rider hit by it yells "HEY!" (arms out), the kook says "Sorry!!" and kicks out. Optional: the local's mood drops; the player gets a tiny "etiquette" event. The lifeguard whistle fires.
- **Paddle battle:** two surfers heading for the same peak slot paddle faster side by side with head-turns, one gives up with a wave ("all yours").
- **Beach high-five:** when two happy surfers walk out within 3 m, they stop, fist-bump (arms toward each other) and say "Sick session!". Needs a pair check in `Surfer.Update` for `WalkingOut`.
- **Everyone looks at the big thing:** `Spectator` and lineup surfers turn their heads to the highlight (new `CharacterRig.LookAt(Vector3)` rotating the head within +-70 degrees). Cheap, very noticeable.

### 4f. Traits that change style [Idea]
Add `enum Archetype` on `Surfer`. Suggested 7, each = a word list, a look, a gait, 1-2 numbers:
| Archetype | Looks | Numbers | Signature |
|---|---|---|---|
| Grom | 0.75 scale, bright rash vest | fast paddle, wipeout x1.4, tip x0.5 | cartwheel wipeout, "Again again!" |
| Kook | mismatched colors, big board, leash on wrong foot | drop-in chance 10%, mood drops when scolded | pearl wipeout |
| Soul cruiser | straw hat, longboard | rare tricks, long calm rides, tip x1.2 | hang five, soul arch |
| Charger | rash vest, headband | big airs, wipeout x1.3, tip x1.4 | claim + roar |
| Influencer | sunglasses, bright shorts | paid more if filmed? (tip x1.3) | selfie pose, complains about "bad lighting" |
| Local | cap, zinc nose, weathered | patience x1.5, grumpy | gives waves away, scolds kooks |
| Surf dad | sunhat, white zinc, belly | slow walk, patient | waves at kids, "Back in my day..." |
Mix rules: archetype is picked from Skill (kook mostly Beginner, charger mostly Pro, etc.). Only 2-3 numbers change so `docs/BALANCE.md` stays valid.

### 4g. Accessories and outfit (built in code, no bones) [Idea]
Small primitive meshes parented to `head`, `torso` or the board, created in `Surfer.Init` or `CharacterRig`:
- Sunglasses (two dark flat cubes on the face), cap/bucket hat (cylinder + brim), sun hat, headband (flattened torus/cylinder), zinc nose (white or neon pink cube on the nose; works as a painted color if the face has a nose mesh), rash vest (tint torso or add a second shirt layer), long-sleeve vs short.
- Board stickers: 1-3 small colored quads on the deck (`Surfer.Paint` already paints deck only; add a decal child). Gives each board identity.
- Silhouette rule (Fall Guys): vary head shape (hats) and bulk (dad belly = scale torso) so you can tell types from far away, not only by color.

### 4h. Rare moments worth a screenshot [Idea]
Each ~1 in 30-100 sessions, with a bubble, a sound and `Highlights.Moment`:
- **Perfect party wave:** 3+ riders on one wave, all hands up.
- **Grom lands a 360 first try,** the whole lineup hoots.
- **Board fling hits the lifeguard tower / lands on the beach** and a gull stands on it.
- **Barrel selfie:** influencer films from inside the tube and then falls.
- **Dad drops the phone in the water** and dives for it.
- **The old local gives away a wave** to the kid (soft music sting). 
- **Celebrity visit** (already on the roadmap): crowd runs to the rail.
- **Whole-lineup wave** (Mexican wave in the water): when a mega-highlight happens, everyone raises an arm in turn, left to right.
- **Gull steals a snack** from a beach guest (we already have gulls).

### 4i. Speech bubble polish [Idea]
- **Mood styles:** bubble color/shape from `Surfer` mood (happy = yellow, angry = red with jagged edge, scared = pale blue). One USS class per mood in `Game.uss`, toggled in `UpdateBubbles`. 
- **Emotes:** short bubbles with one big symbol ("!", "?!", "heart", "zzz", "$") for 1.2 s in front of longer lines. Quick to read from afar.
- **Limit spam:** Planet Coaster players hate repeated thoughts. Rules: max 6 bubbles on screen (closest/most important first), 8 s cooldown per surfer, never the same line twice in a row (keep last index), 25% chance for common lines.
- **Name tags** (first names) shown when you look at a surfer or when they speak: tiny, makes them people. Nicknames from archetype ("Big Dave", "Kai Jr.").
- **Voice:** every archetype has 6+ lines per situation (catch, skip, wipeout, finish, impatient). Examples: Grom "AGAIN! AGAIN!", Kook "Oops, was that your wave?", Local "Tourists...", Influencer "Smash that like button!", Dad "Watch me, kids!", Soul "Just feel the wave, man.", Charger "SEND IT!".

## 5. Mechanics that make fun surfers matter (new, "not Roblox") [Idea]
- **Referee the lineup:** drop-ins and paddle battles create tiny problems the *player* settles (whistle, call out, give a free token). Reward: etiquette stat that raises park rating. Builds on `Lifeguard.Whistle`.
- **Regulars with names:** a few named surfers return, level up (Beginner -> Intermediate) over days, remember how you treated them, and bring friends. Needs a save list in `SaveSystem`. High reward for loyalty, ties to the "grow" fantasy.
- **Photo mode / highlight reel:** photograph moments with a camera item; photos earn tips or sponsorship and fill an end-of-day gallery (see surf-feel item 15). Unique to surf parks; screenshots become content.
- **Stoke meter:** crowd "stoke" rises with hoots and party waves, boosts tips and spawn rate for a few minutes. Shows on the HUD like the flow meter.
- **Trait synergies:** grom + local pair is cute (local teaches grom, grom's tip jumps), kook + local is drama. Cheap to script as 2-3 special pair lines.

## 6. Ideas for our game (ranked: 15 buildable sessions, charm per hour)
One feature per session, per project rules. Step labels: Step 2 = surfers, Step 4+/5 = growth/polish. Effort S = under 1 h of code, M = 1 session, L = 1-2 sessions. Numbers are starting values to tune by eye.

| # | Change | Where | Effort | Why it's high value |
|---|---|---|---|---|
| 1 | **Archetypes + lines** (7 types, 6+ lines each, assigned in `Init` from Skill). Replace static arrays `RideShouts`/`HappyShouts` with a per-archetype table in a new `SurferPersonalities.cs`; `Shout` callers pick `Line(situation)`. No cooldown spam (store last index). | `Surfer.Init`, `TryCatchWave`, `DoTrick`, `FinishRide`, `LeaveAngry` | M | Foundation for most other items; text is the cheapest charm there is. |
| 2 | **Claim/shaka/fist-pump celebration** at the end of good rides (`FinishRide(false)`), 1.2 s; pick by archetype. Add `CharacterRig.Gesture` enum with `Claim, Shaka, FistPump, SoulArch` as timed pose offsets. | `CharacterRig` (new `PlayGesture`), `Surfer.FinishRide`, `KickOut` | S-M | Instantly readable moment from the beach. |
| 3 | **Funny wipeout variants** (pearl, cartwheel, faceplant, board fling + swim to board). Choose in `StartWipeout` by `Skill`/archetype/wipeoutAt; branch in `Tumble`. | `Surfer.StartWipeout`, `Tumble`, new `SwimmingToBoard` state | M-L | The #1 thing people clip/screenshot; on the DESIGN.md ideas list as "funny ragdolls". |
| 4 | **Heads turn + lineup fidgets:** `CharacterRig.LookAt(Vector3?)` for head yaw (+-70 degrees) used by `Spectator` and `Straddle` surfers on `Highlights.Happened`; plus random `Fidget` gestures every 6-15 s in Straddle (stretch, arm circle, splash, scratch). | `CharacterRig`, `Spectator.Update`, `Surfer` WaitingForWave | M | Makes every static NPC feel alive; huge gain for cheap math. |
| 5 | **Accessories + outfits** by archetype (sunglasses, caps, sun hat, zinc nose, headband, board stickers). Small primitives under `head`/board built in code. | `Surfer.Init`, `CharacterRig` (a `head` getter or `Accessorize()`), `Surfer.Paint` | M | Silhouette variety (Fall Guys lesson); biggest *look* upgrade Ido asked for. |
| 6 | **Hoot + drop-in drama:** on `TryCatchWave` success raise a static event `Surfer.CaughtWave`; neighbors within 8 m hoot (arm up, "Yeeew!"). Kook archetype ignores `DropInSpace` 10%; victim yells "HEY!", kook "Sorry!", `Lifeguard.Whistle`. | `Surfer.TryCatchWave`, `RiderNear`, `Lifeguard` | M | First surfer-to-surfer interaction; makes the lineup a society. |
| 7 | **Bubble polish:** mood colors via USS classes, one-symbol emotes, max 6 bubbles on screen, name tags. | `GameHud.UpdateBubbles/CreateBubble`, `Game.uss`, `Surfer.Status` | M | Planet Coaster lesson: fewer, better, readable bubbles. |
| 8 | **Impatient body language in lines:** `CharacterRig.Impatience` from `Surfer.Patience` (sway, watch check, arms crossed, foot tap). | `CharacterRig` idle, `Surfer.StandInLine` | S-M | Player reads the queue at a glance; ties to existing patience bar. |
| 9 | **Signature moves:** `Move.HangFive` (NoseWalk input shifts stand spot), `Move.Spin` 360 on the ground, soul arch cruising for longboards. | `Surfer.Move` enum, `DoTrick`, `UpdateRideFeel`, `CharacterRig.Ride` | M | Style differences you can read from the beach (cruiser vs charger). |
| 10 | **Party wave** (friends allowed closer, both shaka) + **beach high-five**. | `Surfer.RiderNear`, `Update` WalkingOut | M | Strong screenshot; sets up "stoke meter". |
| 11 | **Duck-dive** while paddling back through waves. | `Surfer.Update` PaddlingBack, `WaveComing()` | S-M | Realistic detail PC players notice; also makes the paddle look purposeful. |
| 12 | **Gait per archetype** (swagger, bounce, shuffle). | `CharacterRig.Locomotion` | S | Silhouette in motion. |
| 13 | **Rare moments system** (`RareMoments.cs`): chance each session, bubble + `Highlights.Moment`. Start with 3: grom 360, dad drops phone, old local gives a wave. | new class, `Surfer.FinishRide` | M | Story fuel; ties to `Treasure`/`DailyChallenges` style counters in `GameStats` ("moments seen"). |
| 14 | **Regulars:** named, saved, level up over days, come back with friends. | `SurferSpawner`, `SaveSystem` | L | Long-term attachment; the thing Dave the Diver/Animal Crossing do with returning characters. |
| 15 | **Photo item + gallery** of highlights at day end. | `Highlights`, `DayCycle.EndDay`, camera item | L | Unique mechanic, creates shareable images. |

**Suggested order:** 1, then 2 (S-M), 4, 3, 5, 6; the first six together would change how a screenshot of the lineup looks and how a minute of watching feels. Items 7 and 8 are good small sessions between bigger ones.
**Caution:** do not stack too many of these into one session. Each needs a short playtest (`tools/test/play_begin.sh` first, `play_end.sh` last).
