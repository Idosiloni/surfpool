# Wave pool technologies, operator options, ocean wave basics, and wider pools
Date: 2026-10-21

Why this file: Ido said "pools need to be wider, long rides go left/right, not straight ahead" and "learn more about surf pools and ocean waves". Tags: **[S]** = from a linked source, **[K]** = my own background knowledge (not checked), **[E]** = my estimate/guess. Company numbers are marketing; sources often disagree and I say so. Older notes: [long-rides.md](long-rides.md) (peel math, code limits), [wave-pools.md](wave-pools.md), [wave-pool-look.md](wave-pool-look.md), [surf-park-design.md](surf-park-design.md).

Words: **peel** = the break point slides along the wave and the surfer rides ahead of it. **Lineup** = where surfers wait. **Face** = the front of the wave. **Closeout** = the whole wave breaks at once, no ride sideways.

## 1. The big idea (answers Ido's complaint)
- In every real long-ride pool, **the surfer goes along the wave, not toward the beach.** The wave itself travels only 40-70 m from the machine; the ride is 90-300 m **sideways** along it. [S: Cove 15 s ride; Ranch 45 s on a 700 m strip; see sections below]
- Real shape = **long in the peel direction, shallow in the wave-travel direction**. Our pools are the opposite (60 long x 24 wide; width = peel). So Ido is right: make the pool **wide** (peel axis) and keep length (wave travel) ~45-65 m.
- A ride of T seconds needs about `peel distance = surfer speed along the wave x T`. Real surfers on pool waves move ~5-7 m/s [E from long-rides.md math]. So 10 s = 55 m wide, 15 s = 90 m, 20 s = 120 m, 45 s (Ranch) = ~300 m.

## 2. Technologies
| Tech | Shape / size | How the wave is made | Peel direction | Ride | Waves/hour | Surfers |
|---|---|---|---|---|---|---|
| **Wavegarden Cove** (Bristol, URBNSURF Melbourne, Alaia Bay, Wave Park Korea...) | Diamond or hexagon lagoon. Min ~120x120 m, standard ~150x150 m (2.25 ha). A **pier runs down the middle** with wave modules on it [S: [Raised Water](https://raisedwaterresearch.com/wavegarden-cove/)] | Piston paddles every ~1 m along the pier (Bristol: 40 modules over 80 m) push a wave out to **both sides**. Pistons fire back-to-front so the wave is built as it goes. Right side wave runs slightly behind the left | Reef breaks **left on one side of the pier and right on the other**. "Peak" surfers choose left or right | 10 s (small cove) to 15 s (large); Peak/Bay 7-10 s [S]. Operators claim 12-15 s (Bristol) [S: [price guide](https://wavepoolmag.com/2023-price-guide-to-the-worlds-surfable-wave-pools-and-surf-parks/)] | Max 1 wave per 8 s per side = 450 per side = ~900/h; Korea 400-600/h, URBNSURF 900-1,000/h claimed [S] | ~100 per hour in a mid cove (50 experienced, 50 beginners), ~25 in each Reef area; each surfer ~10-12 waves/session [S] |
| **Wavegarden Lagoon** (Surf Snowdonia, Adventure Parc, Wales; closed Sept 2023) | A **300 m long** straight lagoon; a foil/"plough" runs along a central pier | One wave per ~minute [S: [Peninsula](https://thepeninsulaqatar.com/article/01/08/2015/welsh-park-offers-world-s-longest-man-made-ride)] | Waves peel left and right off the pier | ~18-20 s, up to ~150 m [S] | ~60 (1/min) | 36-52 surfers in the water (pre-opening claims) [S] |
| **Kelly Slater Surf Ranch** (Lemoore, CA) | Long straight pool ~**700 x 150 m**; sources also say 2,000 ft/yards, so size is uncertain [S: [Wave Pool Mag](https://wavepoolmag.com/how-the-kelly-slater-wave-pool-works/)] | A big submerged **foil on a carriage** drives down a track; the pool floor is shaped so the wave stands up and breaks as it goes. Bidirectional: one direction = right, run back = left | The wave breaks along the whole pool length: the surfer rides **the entire 700 m lane** | up to **45 s**, first barrel up to 14 s [S] | 12-25 per hour: one wave every ~2-4 min while water settles [S] | Private/pro only. Private hire ~$50-70k per day, ~$625-875 per person-hour [S: price guide] |
| **American Wave Machines PerfectSwell** (Waco BSR, Skudin American Dream NJ, Japan) | Rectangle, 24 air chambers (each ~3 m) along **one long side, ~73 m** [S: [Raised Water](https://raisedwaterresearch.com/?p=168)]. Beach/shallow on the other side | Compressed air fires the chambers in a chosen order: ~40 wave shapes, set with an iPad [S] | Left or right. At Waco rights the first 30 min, lefts the last 30 [S] | 5-10 s (advanced 8-10, intermediate 6-8) [S] | 108-160/h; sets of 3, 5 s apart, ~30 s between sets [S] | Advanced max 9; beginner up to 20 [S] |
| **Surf Lakes** (Yeppoon, AU) | Round-ish lake ~200 x 150 m (one report). Central **plunger** | A huge plunger is lifted with air and dropped about every 6 s; **rings of swell** spread out and hit curved reefs. Sources say 4, 5 or 8 breaks, so I could not confirm "5" [S: [SurfToday](https://www.surfertoday.com/surfing/surf-lakes-is-already-making-waves-in-queensland), [Wave Pool Mag](https://wavepoolmag.com/how-the-surf-lakes-wave-pool-design-works/)] | Each reef peak gives a left and a right, so every ring feeds many surfers at once | Not found | Claims 2,000-2,400 surfable waves/h; the prototype is far below this [S] | Claim ~200 surfers x ~10 waves; capacity claims are projections |
| **Alaia Bay** (Switzerland; Cove tech) | Compact cove, ~8,300 m2 water [S: Monocle via search]; no whitewater bay, all sessions on the Reef [S: [guide](https://wavepoolmag.com/everything-you-need-to-surf-swiss-wave-pool/)] | Wavegarden Cove foil | Left and right | Not found | ~900 claimed; ~300 waves/h up to 2 m in another source [S] | Beginner and advanced never share the water. One skill level per hour [S] |
| **Endless Surf** | Row of air chambers (see [wave-pool-look.md](wave-pool-look.md), [long-rides.md](long-rides.md)) | Many chambers fire in a pattern; more chambers = longer wave | Left, right, split-peak (two waves) | up to ~40 s planned (5-6 sections) | Not found | - |
| **Unit** (floating pool, Utah etc.) | Standing wave; widths 8-16 m | River or pumped water flows over a ramp so the wave stays still and the rider stays put | - | As long as your legs hold: 45 s - 1 min+ [S: [Wave Pool Mag](https://wavepoolmag.com/unit-wave-pool-breaks-ground-in-utah/)] | - | 12-14 riders per hour [S] |
| **FlowRider / sheet waves** (also Citywave [K]) | Fixed pad 5.5-12.8 m wide x 13-16 m long [S: [Themed Attraction](https://www.themedattraction.com/how-do-flowriders-work/)] | Pumps shoot a thin sheet of water (9-13 m/s) up a membrane | Wave does not move; you stay in place | Unlimited | 120-720 rides/h [S] | 1-2 at a time |
| **Surfloch** | Not found. I did not verify anything about it | - | - | - | - | - |

**Photos in words (so we can model them):**
- **Cove**: from above a fat diamond. Straight stripe down the middle = the pier with a row of machines. Each side is a big water area; waves roll away from the pier toward two sandy/concrete beaches at the diamond edges while white foam slides along the crest. The break starts near the pier end and moves away. The surfers' line is long and mostly parallel to the pier. Beginner "Bay" zones sit close to the beach, whitewater only.
- **Ranch**: from above a skinny long rectangle, like a runway. A rail with a big machine (the carriage) runs down it. Wave follows the carriage, curls onto a reef on one side. Surfer rides the whole runway.
- **PerfectSwell**: a square-ish pond. Machine along one side (white air chambers). Wave runs across the pond toward a shallow side and peels along it.
- **Surf Lakes**: a lake with a huge round cone in the middle; rings expand, break on reef arcs with surfers spread all around the rim.

## 3. What operators control (the "menu")
| Control | What real parks do [S unless noted] |
|---|---|
| Height | Beginner knee/waist (0.3-0.6 m), intermediate 0.9-1.2 m (Bristol 3 ft), advanced/expert 1.8-2 m up to 2.4 m (8 ft). PerfectSwell: 1-2 ft beginner, 4 ft intermediate, 6 ft advanced |
| Frequency | Cove: one wave per 8 s per side at max. PerfectSwell: 3-wave sets, 5 s apart, ~30 s lull. Ranch: 1 per 2-4 min |
| Direction | Cove: left on one side, right on the other. PerfectSwell: rights first half hour, lefts second. Ranch: alternate by running the carriage back |
| Shape | Named waves. URBNSURF had 18 in testing: Malibu (beginner), Waikiki peaks, Giros I-IV (turn waves), Tubos I-V (barrels), Pointbreak, The Ramp (air), The Beast (steep slab) [S: [Wave Pool Mag](https://wavepoolmag.com/?p=3764)]. Alaia Bay: Waikiki, Malibu, Advanced, Expert, Pro, Beast Mode |
| Zones | Cove: Reef (green, advanced), Peak (middle), Bay (whitewater, beginner, 0.5-1.2 m deep) [S] |
| Session types | Beginner, improver, intermediate, advanced, expert barrel, lessons (90 min), private hire, "free surf" for validated surfers (Alaia Bay) |
| Rules | Skill levels don't mix. Beast Mode = only advanced surfers in the lineup [S] |

**Prices (1 hour unless noted; sources disagree, use as ballpark):**
| Place | Beginner | Intermediate | Advanced | Expert/barrel | Source |
|---|---|---|---|---|---|
| The Wave, Bristol (GBP) | 40 | 50 | - | 43-55 | [thewave.com via search](https://www.thewave.com/surf/surf-sessions) |
| URBNSURF Melbourne (AUD) | 30-35 (Bay) | 69-89 | 79-99 | 129-144 (~18 barrels) | [Wave Pool Mag](https://wavepoolmag.com/how-much-does-it-cost-to-surf-urbnsurf-melbourne-new-prices/) |
| Waco BSR (USD) | 89-129 | 109-149 | 119-159 | Pro 299-369 (90 min) | [price guide](https://wavepoolmag.com/2023-price-guide-to-the-worlds-surfable-wave-pools-and-surf-parks/) |
| American Dream NJ (USD, 2 h) | 99-125 | 195-250 | 195-250 | 250 | same |
| Alaia Bay | CHF 129-150 (~$110-160) | same price for every wave setting | | | same |
| Surf Snowdonia (USD) | 60 | 60 | 65 | - | same |
| Surf Ranch (USD) | private only: ~$425-575 per wave | | | | same |
Pattern: **price goes up with skill level and with expert barrels** (Expert is ~1.5-2x intermediate). Peak times cost more (Alaia Bay: Peak/Normal/Best tiers). Gear (wetsuit, board hire) is sometimes included, sometimes +GBP 12-17.

## 4. Ocean waves in plain words, and what a pool can copy
| Idea | Plain explanation [S: [BoM](https://www.bom.gov.au/resources/learn-and-explore/marine-knowledge-centre/waves-and-swell), [SurfToday](https://www.surfertoday.com/surfing/the-variables-of-an-ocean-swell), [Surf Simply](https://surfsimply.com/magazine/why-and-how-waves-break), [Inertia](https://www.theinertia.com/surf/spilling-surging-plunging-the-science-of-breaking-waves/)] | Pool imitation |
|---|---|---|
| Swell | Wind blowing over water makes waves. Bigger when wind is **strong**, blows **long**, and over a long **fetch** (distance of open water). Swell is waves that left the storm and travelled far | Wave **height** setting; "storm day" random big sets |
| Period vs height | Period = seconds between crests. Long period (10+ s) = far storm, organized, powerful, clean. Short period = choppy windswell. Same height, long period hits harder | Pool: wave **speed/power** and how clean the face looks. Our `travel` speed could be shown as "period" |
| Sets and lulls | Waves arrive in groups (sets) with calm gaps (lulls) [K for the "groups" cause; the sources only say swell lines form sets] | **Set count + lull seconds** (PerfectSwell: 3 waves, 30 s lull) |
| Breaker types | Slope of the bottom decides. Gentle = **spilling** (foam rolls down, slow, easy, beginner). Steeper/abrupt reef = **plunging** (lip throws, barrel). Very steep = **surging** (no ride) | Beginner = spilling/whitewater, Pro = plunging + barrel. Shape setting = how steep the "reef" is |
| Why waves peel | The wave hits shallow water at an angle, so one end breaks first and the break point runs along the wave. Needs the swell angle and reef/shore line to be diagonal; if too diagonal the break races off, if too straight it closes out [S: partly; the exact peel angle rules are in [long-rides.md](long-rides.md)] | Our `peelSpeed` is this. A pool does it with the angled reef or the staggered machine firing |
| Point break | Swell wraps around a headland: long, steady, one direction, slow peel (Malibu, J-Bay) [K] | "Point Pool": long lane, slow peel, mellow |
| Reef break | Waves hit a shallow rock shelf: hollow, powerful, barrels, tricky [K] | Pro barrel mode |
| Beach break | Sand bars; peaks pop up and move; mixed lefts/rights, often closeouts [K] | Main pool beach: random peaks and closeouts |
| Peak, left, right | A peak is the highest point; waves break one way from it. Left/right is from the surfer's view **facing the beach** [K] | Direction setting Left / Right / Peak (peak = both sides at once) |
| Closeout | Whole wave breaks at once, no shoulder to ride | Beginner whitewater mode; also "bad wave" outcome when the player picks too big a height |
| Wind | **Offshore** (blowing from land to sea) holds the lip up, cleaner, barrels. **Onshore** blows the lip over early: spilling, mushy, choppy. Roughly 10 knots offshore is best (old survey) [S] | Daily weather event: wind changes wave quality score |
| Tide | Water depth changes how and where waves break; some spots work only at certain tides [K] | Low/high tide = wave shape swap each hour (cheap daily event) |

## 5. Ranked suggestions for our pools (most impactful first)
Rules from the code (see [long-rides.md](long-rides.md)): a ride ends at 88-90% of pool length, so width helps only if the length limit stops cutting the ride. All numbers [E] unless noted.

1. **Make pools wider than long, and use the width.** Swap shape thinking: length (wave travel) ~45-60 m, width (peel) 60-120 m. Per-pool sizes already exist (`PoolSpec.length/width`). Cheapest first: Reef 60 x 75, Lagoon 60 x 100 (ride 10-12 s at peel ~4-6 m/s). Needs the wave to travel slower (c 4-5) so it does not reach the beach before the rider finishes; otherwise rides stay ~5 s (proved in long-rides.md).
2. **Peel direction relative to the walls:** wave travels down the length; break starts at one side wall (`peakOffset` near the wall) and runs to the far wall. For "Left" the break starts at the right wall and vice versa. "Peak" = starts in the middle and two riders go both ways (like Cove's pier). Rider should enter ~5-8 m ahead of the break point.
3. **Cove-style pool ("Wave Cove", mid-game):** a machine pier **in the center** along the width, two half-pools (each 55 x 100) mirrored: left wave on one, right wave on the other. Two surfers per wave at once, double the seats for the same machine. Visually it matches the diamond layout. Needs: two copies of the water mesh mirrored; ticket sells "Reef" and "Bay" seats. Nice for the park map photo.
4. **Ranch lane (late):** 50 x 200 m lane with a visible carriage on a rail moving at the peel speed; one wave every 25-60 s, very pricey tickets, pro events. Details: [long-rides.md](long-rides.md) mode B.
5. **Surf-Lakes plunger (late, special):** circular pool 120 m across, central plunger, 3-4 reef arcs; every plunge gives rings that break at all arcs, each arc has a left and a right. Great visuals, new water math (radial), so last.

**Ride length targets [E, from the real table above]**
| Wave level | Real example | Our target | Pool (length x width) |
|---|---|---|---|
| Beginner whitewater | Bay, 7-10 s | 8-10 s straight ahead is fine | main 45 x 24 |
| Intermediate green | PerfectSwell 6-8 s, Cove Peak 7-10 s | 8-10 s | 50 x 60 |
| Advanced | Cove Reef 10-15 s | 12-15 s | 60 x 90 |
| Pro barrel | Ranch barrel 14 s | 12 s barrel + wall | 60 x 100 |
| Legend | Ranch 45 s | 30-45 s | 50 x 250 lane |

**Surfers per pool [E from real numbers]:** beginner session 12-20 (Waco 20), intermediate 10-12, advanced 6-9 (Waco max 9), Ranch private 1-2. Cove-style: 25 per Reef area. Tie seats to width: roughly 1 surfer per 8-10 m of peel line.

**Wave settings menu for the player (Wave Control panel)** with what a real operator charges (ticket multiplier idea, base = your beginner price):
| Setting | Options | Real operator behavior |
|---|---|---|
| Height | S / M / L / XL (0.5 / 1 / 1.5 / 2 m) | Bigger = costs more power; only skilled surfers can use it |
| Shape | Mellow (spilling) / Open wall / Hollow / Barrel | Barrel/expert about 1.5-2x intermediate price |
| Direction | Left / Right / Peak (both) / Alternate | PerfectSwell alternates halves of the hour |
| Waves per hour | Chill 40 / Busy 80 / Max 120 | Cove max 450 per side; PerfectSwell 108-160 |
| Set pattern | Singles / Sets of 3 + lull 30 s / Sets of 5 | PerfectSwell sets of 3 with ~30 s lulls |
| Zone | Whitewater bay / Green reef | Cove Bay (cheap) vs Reef (premium) |
| Session type | Beginner / Intermediate / Advanced / Expert barrel / Private hire | Skill levels never mix; private = ~3-5x per hour |
Ticket ladder suggestion (relative to beginner = 1.0): intermediate 1.6, advanced 2.0, expert barrel 3.0, private hire 8-10 (Waco: 89 -> 109 -> 119 -> 299; Melbourne 30 -> 69 -> 89 -> 129).

## Ideas for our game
- **Step 2/3 (small, now):** Treat Ido's note first: change `PoolSpec` so Reef = 60 x 75 and Lagoon = 60 x 100, move break start to a side wall (`peakOffset`), slow the wave (c ~4.5), end ride by meters not by percent. Target ride 10 s. (Same plan as the "Point Pool" in long-rides.md, now with real Cove numbers behind it.)
- **Step 3:** Add the **Wave Control panel** rows above: Height, Shape, Direction, Waves/h, Set pattern. Price = base x skill x pool x shape multiplier (barrel 1.5-2x). Show "ride 11.2 s" on the score card so Ido can tune by feel.
- **Step 3:** Skill levels do not mix in a session: the sign says "BEGINNER HOUR"; surfer type must match, else they leave unhappy (Alaia Bay / Beast Mode rule).
- **Step 4:** Two-lineup **Cove pool**: pier machine in the middle, left side and right side, Reef and Bay zones. More seats per machine; one machine upgrade (more modules) = longer rides.
- **Step 4:** Daily weather: wind (offshore = cleaner, +score; onshore = mushy) and tide (changes shape hourly). Cheap events from the ocean table.
- **Step 4:** Set patterns with lull timer; the HUD shows "SET INCOMING 3 waves" (surfers paddle to position).
- **Later:** Surf-Ranch lane (carriage visible on a rail, barrels, 12-25 waves/h, pro contests, private hire at $$$), Surf-Lakes round pool, Unit-style standing wave as a cheap kids/trick attraction (sheet wave ~10 x 16 m pad, 1-2 riders).
- **Unknown / ask Ido:** Real peel angle for our feel; I did not verify Surfloch, Citywave, or Endless Surf footprints.
