# Story and tutorial: a better Uncle Kai arc + a small guided start
Date: 2026-10-22

Tags: **[S]** = from a linked source I opened or got in search results, **[K]** = my general knowledge of the game (not re-checked), **[G]** = my guess / judgment.
Honest limits: I found no developer talks about onboarding for Supermarket Simulator, PowerWash, Stardew or Two Point; those rows are reviews/walkthroughs or [K]. The one strong designer source is the Unpacking GDC 2022 write-up. I could not confirm names of Two Point's tutorial advisors, so I name none.

---------------------------------------------------------------------------

## 0. What we have today (read from the code)
- **Letter** (`StoryScreen`): "Hey kiddo... the wave pool is yours now... I left you some notes. Follow them. P.S. 12 lost treasures." Then the `ParkNamer` card, then `MissionManager.Begin()`.
- **15 missions in 4 chapters** (`BuildStory()`), saved as ONE int `missionIndex` (`SaveSystem` line ~98). Missions never block; they pay cash and XP.
- Proof-of-done counters that exist in `GameStats`: `TicketsSold, WavesSent, SurfersServed, BoardsRented, TubeRides, TricksLanded, Wipeouts, PlayerRides, SnacksSold, ShopSales, TrashCollected, TotalEarned, BestJudgeScore`. Also `DayCycle.Day` + events `ParkClosing` / `DayEnded`, `shop.IsOwned(id)`, `StaffMember.Active.Count`, `PoolRegistry.OpenPools`, `ParkReputation.Stars`, `ParkLevel.Level`.
- **No counter yet** for: price changed at the board, sign flipped, skimmer/debris cleared, whistle/throw-out, kook rescued, perk picked, wave-console settings changed. Marked **[needs new stat]** below.

Problems I see in the current missions (fit-the-code review):
| # | Problem | Fix |
|---|---|---|
| 1 | "Back in business" packs earning, tips, rating and the price board into one text | Split: one small mission for the price board, one for earning |
| 2 | "Fix up the place" says "buy any upgrade" | Point at ONE cheap visible one: Bigger Lineup ($50, L1) or Rental Stand ($100, L1) |
| 3 | Rental Stand is $100 at L1 but its mission is #10, after Bigger Waves (L3, $1,000+) | Move rentals up, right after the first upgrade |
| 4 | Nothing teaches the day end (8 PM close + sign), trash, the skimmer, the whistle, perks | Triggered one-time tips (section 3), not linear missions |
| 5 | Ch.4 "Open all pools" stops at 3 pools; Cove (L22) and Ranch (L25) have no story | Add Ch.5 and Ch.6 (section 4) |
| 6 | Long `story` text vs the new one-line pill | Pill = 6-10 words; Kai flavor goes in the 8 s card / Mail tab |
| 7 | Missions 7-8 are one big gap (L3 purchases, $300 reward) with no in-between payoff | Add a tiny "level 2" mission between (the perk pick) |

---------------------------------------------------------------------------

## 1. How good sims onboard

| Game | What it does | Lesson for us |
|---|---|---|
| **Supermarket Simulator** | Very little. A review calls the start sink-or-swim: no instructions, cutscenes or backstory; the one on-screen objective is "complete 25 checkouts" in a store with nothing to sell yet. A first-day order players follow: stock shelves, set prices, open the shop; time doesn't pass until you let customers in [S: [Xbox Hub](https://www.thexboxhub.com/supermarket-simulator-review/), [Thumbculture](https://www.thumbculture.co.uk/supermarket-simulator-pc-review)] | CaseOh-style games survive on "figure it out", but the first 5 minutes are where players quit. A pause-the-clock-until-ready start is nice [G]. We are *less* blank than this, keep it |
| **TCG Card Shop Sim** | [K] Same family: a few prompts, then price cards, open, learn the rest from customers' bubbles | Customer bubbles ARE the tutorial. Our patience bubbles already do this |
| **PowerWash Simulator** | Only one job is available at the start (a removals van). Prompts walk you through spray types, nozzles and stances; the first real level starts with 12 stepping stones; other nozzles open after the first level's side goal [S: [TrueAchievements](https://www.trueachievements.com/game/PowerWash-Simulator/walkthrough/3), [TrueTrophies](https://www.truetrophies.com/game/PowerWash-Simulator/walkthrough/3), [Xbox Hub](https://www.thexboxhub.com/?p=127293)]. A "vision" button highlights what's still dirty [S: Xbox Hub] | Lock the choice to ONE thing; give one tool at a time; a "show me what's left" helper. Our marker plays that role |
| **Stardew Valley** | Almost no tutorial. Players say the early quests (planting, watering, a first craft) are the guidance; some got stuck on storage and harvesting [S: [Steam thread](https://steamcommunity.com/app/413150/discussions/0/1642041106362357027), [Stardew forums](https://forums.stardewvalley.net/threads/no-game-instructions-is-this-a-scam.53645/post-244155)]. [K] The grandfather's letter + the Mayor / a neighbor visit set up the story in minutes | Quests-as-tutorial is exactly our design. The letter is the emotional hook; keep it short. The gap Stardew players hit ("what is this box for?") is the gap our triggered tips fill |
| **Animal Crossing NH** | Guided opening with a task always visible; reviewers split: "swift and entertaining" vs "first days are slower than anything after", movement limited early [S: [Nintendo World Report](https://nintendoworldreport.com/review/53204/animal-crossing-new-horizons-switch-review), [Film Stories](https://filmstories.co.uk/?p=83030)]. [K] Tom Nook hands out a small chore, then the next | Low barrier for the first big goal is praised. Slow + limited movement is not. Keep the whole park walkable from minute 0 |
| **Two Point Hospital** | First level is a gentle tutorial: hire staff, build rooms, keep patients happy at a slow pace; some objectives must be met before moving on; starts with one hospital lot [S: [Vooks](https://vooks.net/two-point-hospital-switch-review), [Boar](https://theboar.org/2020/03/two-point-hospital-switch-review/)] | A friendly voice + "do this first" gate for the first level only, then open |
| **Dave the Diver** | Phone call -> swim and knife -> harpoon -> back to the sushi bar. The boss gives starter jobs ("catch 7 fish, earn 100 gold to repair the bar"). Complexity builds slowly; a big story (Sea People) grows under the daily loop [S: [Review Geek](https://www.thereviewgeek.com/davethediver-walkthrough-prologue/), [Punished Backlog](https://punishedbacklog.com/dave-the-diver-review/)] | **Best match for us.** Small job -> small money -> a new tool. Story beats are delivered by phone call / short scene right when a new mechanic unlocks |
| **Unpacking** | No words. Story comes from objects placed in rooms; the team found staging gaps and rebuilt a stage (the 5th became a childhood home) so the objects told the right thing [S: [Game Developer, GDC 2022](https://www.gamedeveloper.com/gdc2022/gdc-2022-unpacking-a-narrative-through-1-000-household-items), [Telling a story through belongings](https://www.gamedeveloper.com/design/telling-story-someones-belongings-unpacking)] | Our park's **glow-up IS a wordless story** (peeling paint -> bunting -> arch). Add props, not paragraphs: Kai's old board on the wall, a faded photo |

General rules that repeat across practitioner guides [S: [Wayline](https://www.wayline.io/blog/in-game-tutorials-design-guide), [Bugnet](https://bugnet.io/blog/how-to-design-a-tutorial-level)]; these are indie-blog advice, not studio standards:
- Teach **one mechanic at a time**, in a safe spot, then combine them.
- Hints should be **just-in-time** (appear when the player needs them, e.g. after a failed try or after standing still), not a pop-up wall.
- Make it **skippable / optional**; never force it on veterans or on a restart.
- Avoid text dumps: players do not read them.

"Show then let" (my short version) [G]: the game does it once or points at it (a surfer walks up, a bubble pops, the marker glows), then the player does it. First time = marker + one line. Second time = nothing.

When to stop holding hands [G]:
- Stop the marker when the player has done the action **twice**, or when they walk to the target *before* the mission says so.
- After the first 6 missions, drop the long text. After day 3, only triggered tips.
- Never repeat a tip. Keep every tip re-readable in Pause > Tips.

---------------------------------------------------------------------------

## 2. A light story that fits a chill sim

### 2.1 The big idea [G]
Small, funny, mostly **notes and one-liners**. No cutscenes. Each beat = (a) a Mail-tab note, (b) a one-line pill, (c) a prop appears in the park. The story is told by **who shows up** at the gate, as the park grows.

### 2.2 Decision for Ido: where is Kai?
The letter says "if you're reading this, the pool is yours now". That reads like Kai is gone. Pick one (I do not want to assume):
- **A. Retired and travelling (recommended [G]):** "I'm on a sailboat with no phone." Sends postcards and the odd crackly call. Can *return* at the end as the payoff. Funnier and warmer; no one dies in a chill game.
- **B. Notebook only:** Kai only exists in the notebook pages (the 12 treasures become pages). More mysterious; no return.
Small letter change for A: replace "If you're reading this" with "By the time you read this I'll be somewhere with sand and no phone signal."

### 2.3 Cast (5 people, each tied to things already in the game)
| Who | Job in the story | Tie to existing code |
|---|---|---|
| **Uncle Kai** | Voice of the plan. Postcards in the Mail tab (`Notifications.Log`), pill lines, treasure notes | `StoryScreen`, `Treasure` (12 pages), Kai's Plan |
| **Coco** (new, a teen from the town who works the desk when you hire one) | Comic relief; reacts to level-ups; "hires" talk | `desk_clerk` hire, `StaffMember` |
| **Marlo "Big M" Reyes** (new, local legend, old pro) | Mentor. Shows up at L5 to watch, L11 to judge your Pro waves, L15 as the first of the `FamousSurfers` legends | `FamousSurfers`, `SurferPersonalities` |
| **Brenda Tidewell, Inspector** (new, grumpy) | Rare visitor with a clipboard. Complains in the day report, then softens each time. Gives a "clean pool" or "safe park" score | `PoolDirt`, `RuleBreakers`, `ParkReviews` |
| **Splash Valley Resort / Chad Waverly** (new rival) | A rival park down the coast with slick ads. Fake "news" in the Mail tab, helicopter flyover at L18 (existing `Cruiser`), challenges you to the final contest | `ParkReputation`, `Cruiser` heli |
Name tweaks are mine [G]; Ido should rename freely. Keep surfer first names from `SurferNames`.

### 2.4 Tone rules
- Max 2 short sentences per line on screen. Jokes are about the *pool* and surfing, not about people.
- Always use the player's park name (`ParkName.Name`, `{0}`), never the hard-coded "Kai's Surf Park" (the player can rename it).
- Payoff = the people from the early game return (Coco runs the desk, Brenda gives 5 stars, Kai comes back or sends a final postcard).

---------------------------------------------------------------------------

## 3. Tutorial script (the first ~15 minutes)
Rules: **pill line = 6-10 words** (fits the minimal HUD); the 8 s card may carry one extra Kai sentence; marker on the target; skippable. Times are my estimates [G] for a first-time player.

| # | Told (pill) | Player does | Proof it's done | World cue | ~Time | Today's mission |
|---|---|---|---|---|---|---|
| 1 | "Walk to the ticket desk." | Walk | `NearRegister()` | Marker on desk, a surfer queues up | 20 s | M1 "Open the doors" (ok) |
| 2 | "Press E: sell a session." | Look at register, E | `TicketsSold>=1` | Surfer with a bubble at desk; hint cap shows E | 30 s | M2 (ok) |
| 3 | "Send a wave: red button." | Console, E | `WavesSent>=1` | Marker on console; surfer paddling out | 40 s | M3 (ok) |
| 4 | "Watch them ride. Keep waves coming." | Watch, send 1-2 more | `SurfersServed>=1` | Patience bar over their head; replay tease | 90 s | M4 (ok) |
| 5 | "Collect: tips pay out here." | Walk to the money pop | `TotalEarned>=40` [or **needs new stat** for "tip seen"] | Floating +$ above the surfer | 30 s | Part of M5; split it out |
| 6 | "Raise the price on the board." | + button at desk | **[needs new stat]** `PriceBoard` changed | Marker on price stand | 30 s | Hidden inside M5 text; make its own mission |
| 7 | "Open the computer, buy Bigger Lineup." | E on computer, buy ONE cheap upgrade | `shop.IsOwned("more_surfers")` | Marker on computer, upgrade card has the PLAN sticker (exists), lights up | 60 s | M6 "buy any upgrade" -> point at one |
| 8 | "Rent boards: open the stand." | Buy rental stand, hand a board to a renter | `Owns("rental_stand")` then `BoardsRented>=2` | Marker rack -> renter bubble | 2 min | M10; move up to here |
| 9 | "Grab your board. Ride a wave!" | Peg, paddle, pop up | `PlayerRides>=1` | Marker on peg; first-3-rides help (wider catch window, per `surf-feel.md` #14, check it exists) | 3 min | M7 (ok, but late; ok at L1) |
| 10 | "Pick up trash on the sand." | E on litter | `TrashCollected>=3` | Glowing litter; triggers when the first litter appears, not on a timer | 1 min | none. Triggered tip (below) |
| 11 | "Close at 8 PM: flip the sign." | Wait for dusk, flip `ParkSign` | `DayEnded` fired for day 1 | Gates close, guests go home, sign pulses | 3-5 min | none. Triggered tip (below) |

After step 11 the story hands over to the normal chapter flow (Bigger waves, Regulars, Hire help).

### 3.1 Triggered one-time tips (when to stop holding hands)
These fire from the **event itself**, show ONE pill line plus the marker, and never again (save a `seenTips` set of ids). They do not live in the mission list.
| Trigger | Line (pill) | Proof | Note |
|---|---|---|---|
| First litter spawns (`Trash.All.Count>0`) | "Litter! Pick it up (E)." | `TrashCollected` +1 | Later: hire Janitor |
| First pool debris (`PoolDebris.All`) | "Skim the pool with the net." | **[needs new stat]** | Tied to `pool_robot` later |
| 8 PM close (`DayCycle.ParkClosing`) | "Everyone's heading home. Flip the sign." | `DayEnded` | Teaches the day cycle |
| First sneak-in / wave hog (`RuleBreakers`, day 2-3) | "That one skipped the line! Whistle (Q)." [check key] | **[needs new stat]** throw-out count | Real key from `PlayerInteractor.FarSurfer` |
| First breakdown (`MachineService`) | "The machine is broken. Go repair it." | repair event | Marker on machine |
| First kook in trouble (`KookRescue`) | "Throw the ring!" | rescue event | 25 s window, so keep the line big |
| Level 2 (first perk) | "Pick a perk for the park." | perk chosen | `PerkPicker` opens itself |
| First shop (acai, gear) | "Fill the order bubble." | `SnacksSold`/`ShopSales`>=1 | One per shop |
Use the existing `hud-less-is-more.md` pop-in rules: they appear as an ink pill, 8 s, not as a card.

### 3.2 Save safety (important, Ido has a real save)
`missionIndex` is a plain int. If we insert missions in the middle, old saves jump to the wrong step. Options, best first [G]:
1. **Give each Mission a string `id` and save the id** (plus keep the old int for a one-time migration table: old index -> id). Cleanest long-term.
2. **Append only:** put new tutorial/story missions at the END or in a *second list* (`storyBeats`) with its own saved cursor.
3. **Triggered tips are a separate saved set** (`seenTips`): no risk to the mission list.
If we move rentals earlier (problem #3), do it with option 1, or skip the move and use a tip instead ("Rent stand is cheap now!") [G].

### 3.3 Skip and replay
- A "Skip tutorial" button on the first pill (small, bottom of the 8 s card) marks missions 1-6 as done with no reward. Respect veterans and reloads [S: practitioner advice, see rules above].
- Pause > "Kai's Notebook" lists every tip seen. Reuse the Mail tab if easier.

---------------------------------------------------------------------------

## 4. Story outline across the whole game
Level bands come from `UpgradeShop` levels (pool_expand_1 L3, expand_2 L10, Reef L15, Lagoon L20, Cove L22, Ranch L25) and `ParkGlowUp` (fresh paint L3, bunting L5, string lights L8, arch L11, blimp L14, legends L15, heli L18). **Beats that fire together with existing celebrations are almost free** [G]: add a Mail note + a pill line next to the `Celebration` already shown.

Lines use `{park}` = the player's park name. Kai voice = short, warm, a little goofy.

| Chapter (levels) | Beat | Trigger | Example lines |
|---|---|---|---|
| **1. The Reopening (L1-2)** | Letter, name the park, first customer, first upgrade | Existing | Letter: "By the time you read this I'll be somewhere sunny with no signal. Take care of the old girl." / pill: "Kai says: stay humble, stay stoked." |
| **2. Word Gets Around (L3-9)** | Coco joins the desk; first Kai postcard; first perk; Marlo watches from the beach | `desk_clerk` hired; L2 perk; L5 | Kai postcard: "Hear you fixed the paint! I can smell it from the boat." / Coco: "I'm not a morning person but I AM a ticket person." |
| **3. The Pro Tour (L10-14)** | Arch goes up (L11), Marlo judges your Pro wave, first inspector visit | `pool_expand_2`, `pro_waves`, L11 `ParkGlowUp` | Marlo: "That barrel? Old Kai would've cried." / Brenda: "Your net has a hole. And a duck. I'm writing both down." |
| **4. Legends (L15-19)** | Reef pool, first legend visits, news helicopter and rival ad (L18) | `pool_reef`, L15 `FamousVisits`, L18 | Kai: "They put OUR park on TV? Tell Marlo he owes me $5." / Chad's ad: "Splash Valley: the park that doesn't crumble!" |
| **5. The Big One (L20-24)** | Lagoon, the Cove (rights and lefts); the rival challenges you; Brenda's second visit | `pool_lagoon` L20, `pool_cove` L22 | Chad: "A contest. Winner keeps the coastline." / Brenda: "Fine. It's... acceptable." |
| **6. The Ranch (L25+)** | Surf Ranch; the contest day; final payoff: Kai returns (A) or the last notebook page (B); all 12 treasures found | `pool_ranch` L25, `DailyEvents.ContestDay`, `Treasure` all found | Kai: "Thirty years ago I dug a hole and dreamed. Look at it now." / ending toast: "{park}: the best surf park in the world." |
After the ending: the game keeps going (sandbox); Kai sends a rare postcard every 10 levels [G].

Reusing what exists (no new systems):
- 12 `Treasure` finds = 12 **notebook pages**, each with one funny Kai memory (the old systems already pay a bonus for finding all).
- `FamousSurfers` legends can carry one line about Kai ("He taught me to duck dive") instead of new characters.
- `SurferPersonalities` give Coco/Marlo ready-made voice styles.
- Chapters 5 and 6 replace the broken "Open all pools" cap (3 pools) in M15.

---------------------------------------------------------------------------

## 5. Ranked recommendations
| # | Recommendation | Size | Why |
|---|---|---|---|
| 1 | **Add `Mission.id` (string) and save the id**, keep the old int as a migration table | S | Everything else (reordering, inserting) depends on it; protects Ido's save |
| 2 | **Split "Back in business"**: own mission for the price board; make "Fix up the place" point at ONE cheap upgrade | S | Fixes two sentences of overloaded tutorial |
| 3 | **Triggered one-time tips** (`seenTips` saved): litter, 8 PM close/sign, first breakdown, first kook, first perk, first shop | M | Teaches the mechanics that no mission covers, at the exact moment they matter |
| 4 | **Decide Kai: retired (A) or notebook (B)**, then edit the letter's first line | S | Removes the "is Kai gone?" feeling. Needs Ido's choice |
| 5 | **Pill lines of 6-10 words**; move Kai's flavor into the 8 s card and Mail tab | S | Fits the minimal HUD |
| 6 | **Skip button + "Kai's Notebook" tip list** in Pause | S | Respects veterans; makes tips replayable |
| 7 | **Chapters 5-6** for Lagoon/Cove/Ranch and a real ending (contest day, Kai's return or last page) | M | Story currently stops at pool 3; the Cove/Ranch levels have no payoff |
| 8 | **Add Coco, Marlo, Brenda, rival** as Mail-tab notes + one in-world appearance each (a legend walk-on, a clipboard visitor) | M | Small recurring faces make a sim feel alive |
| 9 | **Treasure pages**: show a Kai memory when you find a treasure | S | Uses an existing system; gives the P.S. in the letter a payoff |
| 10 | **Props as story**: Kai's old board on the wall, a faded photo that "restores" with level (via `GlowItem`) | M | Unpacking-style wordless storytelling [S] |
| 11 | **Stop-the-marker rule**: hide a marker if the player already walks to the target before the pill appears, or after 2 completions | S | "When to stop holding hands" |
| 12 | **Postcard from Kai on each glow-up celebration** (L3, L5, L8, L11, L14, L18) | S | Almost free: piggyback on `Celebration`/`ParkGlowUp` |

---------------------------------------------------------------------------

## Ideas for our game
- **Step 5 / now (small, safe):** recs 1, 2, 5 and 11 together in one session. Add string ids, split M5/M6, shorten pill text. No new art.
- **Step 5 / next session:** rec 3 (triggered tips + `seenTips`) plus rec 6 (skip + notebook). Needs 3-4 new stats: price-changed, sign-flipped, skimmed, thrown-out (mark each in code as one-line counters in `GameStats`).
- **Step 5 / story pass:** rec 4 first (ask Ido), then recs 7, 9, 12 (chapters 5-6, treasure pages, glow-up postcards). Text only, all through `Notifications` / Mail.
- **Step 4+ growth hooks:** rec 8: make the inspector (Brenda) a rare visitor tied to `PoolDirt` and `RuleBreakers`, with a star score on the day report.
- **Later:** rec 10 props (needs Blender models in `build_shops.py`), a contest day finale (`DailyEvents.ContestDay` exists), a "postgame" Kai postcard every 10 levels.
- **Playtest check:** watch someone new play the first 10 min with no help from Ido; note where they stop and where they read the pill. [G] Fix the biggest stall first.
