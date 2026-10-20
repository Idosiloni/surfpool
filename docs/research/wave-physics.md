# Wave physics, simply (and how to fake it)
Date: 2026-10-20

Sourcing: breaker/depth facts come from the linked coastal-engineering sources; the speed formula is standard textbook physics; items marked (guess) are my suggestions.

## Wave anatomy
| Part | Plain meaning | In our mesh |
|---|---|---|
| Crest | Highest line of the wave | Peak of the bump |
| Face | Steep front slope surfers ride | Steep front side |
| Lip | Very top edge that tips/throws forward | Top few vertices of the front |
| Trough | Low spot in front of / behind the wave | Flat water before the face |
| Pocket | Steepest, most powerful part of the face, right next to where it breaks | Where surfer ideally sits |
| Shoulder | The unbroken, gentler part further along the wave | Far side of the bump from the break |
| Whitewater / foam | Broken part of the wave, tumbling water | White foam mask behind the break |

A "peeling" wave breaks gradually along its length, so the break point travels sideways and surfers outrun it by riding along the face.

## How waves grow and break (shoaling)
- Out in deep water a wave is a smooth bump. When it reaches shallower water it slows down, the front is squeezed, the wave gets taller and steeper. This is called **shoaling**.
- The wave breaks when it gets too tall for the water under it. Rule of thumb: break when **wave height is about 0.6 to 0.8 of the water depth** (0.78 is the textbook limit, about 0.6 more practical on flat bottoms; plunging breakers measured near 0.7-0.8, spilling near 0.6-0.7). Source: [Dutch coastal manual](https://kennisbank-waterbouw.nl/EC/EC39.htm), [UW surf-zone study](https://digital.lib.washington.edu/researchworks/handle/1773/43651).
  - Example: a 1.2 m wave breaks where water is roughly 1.5-2 m deep.
- Game use: break point = where `bumpHeight / waterDepth > ~0.75`. Our sloped beach makes depth shrink, so the wave "decides" to break by itself.

## Breaker types
| Type | Looks like | Caused by | Fun to ride? |
|---|---|---|---|
| Spilling | Foam trickles down the face from the top | Gentle slope / flat shallow bottom | Easy, slow, great for beginners |
| Plunging | Lip throws out, makes a tube/barrel, big splash | Steeper or sudden-shallow bottom | Fast, hollow, pros |
| Collapsing | In-between: face falls in | Medium-steep slope | Short, punchy |
| Surging | Wave slides up the beach, barely breaks | Very steep shore | Not rideable |

Order from gentle to steep slope: spilling, plunging, collapsing, surging. See [US Army Corps coastal manual summary](https://pdhonline.com/courses/c772/Part-II-Chap_4entire.pdf). In wave pools the "seabed" is a built concrete ramp, so designers shape it to pick the type.

## Speed vs depth
- Shallow water formula: **speed = sqrt(g x depth)**, g = 9.8.
  - 4 m deep: about 6.3 m/s. 1 m deep: about 3.1 m/s. 0.5 m: about 2.2 m/s.
- So waves slow as the beach gets shallow. That is why a wave that rolls 60 m should slow near the end (matches our shrinking beach).
- Surfers glide down a face at roughly the wave speed or faster; if the wave is too slow or too small they stall.

## Period and sets
- **Period** = seconds between crests. Ocean swell: about 8-16 s. Longer period = more powerful.
- **Set** = group of 3-8 bigger waves with calmer lulls between them. Surfers wait for sets.
- Pools make one wave per cycle (every ~8-60 s depending on tech) or sets on a schedule. See [wave-pools.md](wave-pools.md) for session formats.

## How wave pools generate waves
| Tech | How it works | Example |
|---|---|---|
| Plow / foil | A big foil/plow is pulled along a channel, pushing water into a wave | Kelly Slater Wave Co., Wavegarden Lagoon |
| Paddles / pistons | Rows of pistons or levers push water; modules can fire in sequence | Wavegarden Cove |
| Pneumatic | Air chambers push air to displace water; firing order makes different shapes | Surf Loch |
| Plungers / dump | Drop or push a chunk of water | various |

Sources are secondhand and disagree on Cove details: [Wave Pool Mag overview](https://wavepoolmag.com/how-wave-pools-work-20-wave-technology-companies/), [SurferToday](https://www.surfertoday.com/surfing/how-do-wave-pools-work).
- Key idea: the machine makes a swell, the **shaped bottom** in the pool makes it break.
- **Lefts and rights:** the wave is made along a center pier/channel and peels away to both sides (Cove), or runs down one side with a diagonal reef/ramp so it breaks one direction. A **closeout** breaks all at once (bad); a **peeler** breaks progressively (good).
- A diagonal bottom (shallow on one side) makes the break travel sideways, giving a long ride. (Standard design idea.)

## Faking it in a game
| Technique | What it is | Use for us |
|---|---|---|
| Sum of sines | Add several sine waves of different size/direction | Background ripples, choppy water |
| Gerstner waves | Sines that move vertices sideways too, so crests get sharp and troughs wide | Sharper, more watery crest; easy upgrade from our bump |
| Displacement + normals | Move vertices in vertex shader, recompute normals for lighting | Smooth water without CPU cost |
| Foam masks | White texture/noise painted where the wave is steep or breaking | Whitewater behind break; foam where height/steepness is high |
| Lip / curl trick | Push top-row vertices forward and down over the face, or use a pre-baked curl mesh strip | Cheap plunging look without real fluid (guess: bend a ring of vertices around a pivot) |
| Particle spray | Burst particles from the lip and from the surfer's turns | Mist at the lip, spray on carves |
| Moving break point | A point sliding along the crest line; foam, lip, and height change only at/behind it | Peeling left/right breaks |

Tutorials (note: many are older Built-in render pipeline and need porting to URP/Shader Graph):
- [Catlike Coding: Waves (Gerstner, vertex displacement)](https://catlikecoding.com/unity/tutorials/flow/waves/)
- [Unity Learn: Creating a Vertex Displacement Shader](https://learn.unity.com/tutorial/creating-a-vertex-displacement-shader?uv=2019.4)
- [80.lv: Ocean shader with Gerstner waves in Unity (URP, foam, tessellation)](https://80.lv/articles/ocean-shader-with-gerstner-waves-in-unity)
- Search "Shader Graph Gerstner waves URP" for step-by-step videos.

### Where surfers sit on the wave
- **Pocket:** just in front of the crest, mid-face, closest to the foam. Fast and powerful. Our "fixed offset in front of the crest" is the pocket.
- **Shoulder:** further along, high on the gentle face. Slower, safer, beginners and longboards.
- **Whitewater:** behind the break, beginners riding foam straight to shore.
- Positions can be a lane number (0 = whitewater, 1 = shoulder, 2 = pocket, 3 = tube) (guess).
- If a surfer falls behind the break point they get caught by the foam; if too far ahead they run out of wave.

## Ideas for our game
- **More waves:** Wave type = a few numbers: height, speed, steepness, break-point sweep speed, foam amount. Beginner: small, slow, spilling, long gentle shoulder. Intermediate: medium, peeling, a bit steeper. Pro: tall, fast, steep, plunging with lip.
- **More waves:** Use `height / depth > ~0.75` for breaking so the beach slope naturally changes the break. A steeper beach upgrade = plunging, flatter = spilling.
- **More waves:** Add peeling: a break point moves left or right along the crest at constant speed; behind it height drops and foam rises. Choose left, right, or both = a wave menu option.
- **More waves:** Surfer lane (whitewater / shoulder / pocket) from skill: beginners foam, pros pocket.
- **More waves:** Slow the wave with `sqrt(g x depth)` near the beach so it feels real.
- **Better world:** Switch to Gerstner or sum-of-sines for ripples, add foam mask from steepness, and lip spray particles.
- **Later:** Sets (3-5 waves with a pause) as a wave-machine mode; a "big set" event.
- **Later:** Closeout vs peeler as a wave-quality score that affects surfer happiness.
