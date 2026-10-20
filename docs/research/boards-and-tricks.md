# Boards and tricks
Date: 2026-10-20

Sourcing: sizes from [surfboard guides](https://node.albumsurf.com/?p=42455) (SEO-style, treat as rough); maneuver descriptions partly from [Rapture Camps](https://www.rapturecamps.com/learn-to-surf/surf-maneuvers), [Seabreeze](https://www.seabreeze.com.au/Articles/Stand+Up+Paddle/SUP-Surf-Moves-the-Cutback-Floater-and-Reentry_4515711.aspx), [SurferToday list](https://www.surfertoday.com/surfing/the-complete-list-of-surfing-tricks-and-maneuvers). **Prices are my guess** (not sourced). Basic skill/board overview is in [surfing-basics.md](surfing-basics.md).

## Board styles
| Board | Length | Who | How it rides | Rough price (guess, USD new) |
|---|---|---|---|---|
| Foamie / soft top | 6-9 ft | Beginners, kids, lessons | Very stable, slow, forgiving, safe in wipeouts | $100-350 |
| Longboard | 8-10+ ft | Chill/older surfers, improvers | Catches small waves easily, smooth long turns, noseriding | $400-900 |
| Funboard / mini-mal | 6'8"-8' | Improvers to intermediate | Easy paddle, turns better than longboard | $350-700 |
| Fish | 5'4"-6'4" | Intermediate+ | Wide, fast in small weak waves, loose and playful | $450-800 |
| Shortboard (thruster) | 5'6"-6'8" | Advanced, kids who shred | Fast, sharp turns, needs steep wave and skill | $500-900 |
| Gun | 7'-9'+ | Big-wave experts | Long, pointy, for big fast waves; not for pools (our pro wave is not that big) | $700-1200 |
| Bodyboard | ~3.5-4 ft, lie down | Anyone, kids | Ride prone, easy, great in barrels | $50-200 |

Visual cues for low-poly: length, nose shape (round / pointy), tail (round / swallowtail), 1 or 3 fins, color (foamies are bright).

## Maneuvers (low-poly animation friendly)
| Move | What it looks like | Level | Needs wave |
|---|---|---|---|
| Paddle | Lie down, arms swing | Beginner | Any |
| Pop-up | Jump from lying to crouched stance in one move | Beginner | Whitewater/Beginner |
| Straight ride / angle | Stand, board points to shore or slightly sideways | Beginner | Beginner |
| Trim | Stay mid-face, slight crouch, board sliding along the wave | Improver | Beginner+ (needs open face) |
| Bottom turn | Go down the face, carve a curve at the bottom, lean the body and swing back up | Intermediate | Intermediate |
| Top turn / snap | Quick sharp turn near the top, spray flies | Intermediate-Advanced | Intermediate+ |
| Cutback | S-shaped turn: back toward the foam then down the line again | Intermediate | Intermediate |
| Re-entry | Hit the lip and come back down the face | Advanced | Intermediate/Pro |
| Floater | Ride the board up over the breaking lip and float on it | Advanced | Pro (needs a lip) |
| Tube / barrel ride | Crouch deep, lip covers surfer, hand on wall | Advanced/Pro | Pro (plunging) |
| Air / aerial | Launch off lip, spin or fly, land back | Pro | Pro |
| Noseride / hang five-ten | Walk to front of longboard, toes over nose edge | Intermediate (longboard) | Beginner/Intermediate, small smooth |
| Kickout | Turn over the back of the wave to exit | Any | Any |

Simple animation approach (guess): few poses (lie, crouch, stand, lean left/right, arms out, tuck) blended; spin and tube are scripted rotations on the same model.

## Wipeout types (comedy)
- **Pearl:** nose digs in, surfer flies over the front.
- **Catching a rail:** edge bites, sudden sideways flop.
- **Pop-up fail:** falls back or sideways.
- **Lip hit / washing machine:** thrown by the lip, spins underwater.
- **Late drop:** wave too steep, free-fall, belly flop.
- **Board boomerang:** leash snaps the board toward the surfer.
- **Over-the-falls:** thrown forward down the face by the lip.
- **Nose-ride wipeout:** walks too far forward and tips the board.
See [surfing-basics.md](surfing-basics.md) for more; keep it PG, use floaty ragdolls and big splashes.

## Ideas for our game
- **More waves:** Skill levels decide which moves are possible: Beginner pops up and rides straight; Intermediate adds trim/bottom turn/cutback; Pro adds floater, tube, air. Wave type gates them.
- **More waves:** Wipeout chance = mismatch of skill vs wave difficulty. Choose variant by cause: too-steep wave = pearl or over-the-falls; low skill pop-up = fall back.
- **More waves:** Ride score = number of moves + style, shown as floating text ("CUTBACK!"). Happier surfers tip more.
- **More waves:** Simple sine carve already exists; add a one-shot cutback and a floater as extra curve shapes.
- **Later:** Board types as surfer visual variety: foamie for beginners, longboard for chill NPCs, shortboard for pros; random color.
- **Later:** Rental shop stock by board type (foamie cheap, shortboard expensive) and an unlock per board.
- **Later:** Board-wave bonus: longboard on small wave = extra happiness, fish on small wave = fun, shortboard on beginner wave = bored.
- **Later:** Trick unlocks tied to wave upgrades: tube unlocks Pro wave; air needs bigger lip.
- **Later:** Photo/influencer NPC rewards cool moves with reputation.
- **Later:** Contest events scoring variety of moves (peeling wave needed).
