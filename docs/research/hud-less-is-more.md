# HUD: less is more (why ours feels "too full")
Date: 2026-10-22

Tags: **[S]** = from a linked source, **[K]** = my general knowledge of the game (I could not open screenshots, so counts are from memory and may be off by one), **[G]** = my guess / judgment.
Builds on `game-ui-craft.md` (the 4 kinds of UI, Bonn study, motion table, Riders Republic) and `sim-ui.md` / `ui-powerwash.md`. Not repeated here.

Honest limits: I searched about 15 times. I found **no GDC talk and no published fade timings** for BOTW, PowerWash or any other game. gameuidatabase.com was not readable (403 earlier, see `ui-powerwash.md`). So every number in section 3 is my own starting point to tune by feel, and I say so.

---------------------------------------------------------------------------

## 0. What the HUD does today (read from the code, so we start from facts)
Already built (do NOT re-propose): H = clean HUD (saved setting, `GameSettings.CleanHud`); J = journal toggle (expands mission details, challenge list and all Park rows); the wave panel shows only near a machine; Park-panel rows flash in when their number changes and hide when 0; challenge card is one line ("2/3 done - J for details") until J; mission story + tip show only for 12 s after a mission starts, or while you are still in the tutorial chapters (`missions.Index < 6`), or with J; key hint line hides after 90 s; goal strip hides in menus/build/fishing; Mail tab keeps 30 messages; toasts max 3.

Still on screen all the time while you walk (1920x1080 reference, from `Game.uss` / `Hud.uxml`):
| Spot | Always-on things | Rough size |
|---|---|---|
| Top-left | Status card: money pill + tide, income/min, level badge + XP bar + "next level" text, stars + rating, day/clock, (event sticker) | card min 290 px wide |
| Top-left, below | Challenge strip (title + 1 line) | 380 px wide |
| Top-left, below | Mission card: chapter, title, objectives (+ story, tip in the first 12 s / tutorial) | 380 px wide |
| Top-right | Park panel: Session prices + Today's bills always, 5 more rows when they change | 370 px wide |
| Top-right | Wave panel (only near machine) | 370 px |
| Bottom-center | Goal strip with bar | 660 px wide |
| Bottom-left | Hint line (first 90 s) | one long line |
| Center | Crosshair + [E] prompt | small |
| Top-center | Toasts (max 3) | pills |
| In world | Surfer bubbles, mission marker | many |

Measured by me: each side column is about 32 + 370..380 px = **21% of the screen width**, so both columns together cover about **40% of the width** at the top. Left column can be 5 cards tall. That is the "too full" feeling. [G: arithmetic from the USS values]

---------------------------------------------------------------------------

## 1. Principles: how many always-on elements do good games show?

### 1.1 Sourced rules of thumb
- Show only about **3 to 5 critical elements by default**; buffs, detailed stats and quest details appear on demand [S: [abratabia - game UI mistakes](https://www.abratabia.com/game-ui-design/game-ui-mistakes.php), a generic guide, not a studio].
- **The 30-second test:** hide an element; if you can play 30 s without it, it should not be always on [S: same page]. Great for Ido: cover each card with your hand and play.
- Each clutter element "seemed necessary when it was added" - clutter grows one reasonable card at a time [S: same page]. This is exactly how our HUD grew.
- Dynamic HUD = shows only what is relevant to the moment; reduces clutter and helps immersion [S: [Wikipedia HUD](https://en.wikipedia.org/wiki/HUD_(video_games))]. Contextual fade (show on change, fade when idle), HUD-on-demand (hold a key), and full no-HUD are the three common minimal styles, and the HUD amount should be a **player setting** [S: [HUD patterns guide](https://cdn.jsdelivr.net/npm/@hegemonart/get-design-done@1.60.4/reference/domains/gaming-patterns.md), a generic guide].
- Nintendo's UI designer wrote that the BOTW concept was a UI that "would be better if it wasn't there": only essential UI, info shown only when necessary, hearts flash white on damage, small elements plus animation so cues still get noticed [S: [Famicast summary](https://www.thefamicast.com/2017/12/part-2-immersive-impressionable-ui.html), [GamingBolt](https://gamingbolt.com/the-legend-of-zelda-breath-of-the-wild-pro-hud-removes-needless-clutter-from-your-screen)]. The optional **Pro HUD** was asked for by Nintendo of America/Europe to remove even more; it erases minimap, temperature and noise gauges and leaves only hearts [S: [ZeldaUniverse](https://zeldauniverse.net/2017/02/08/breath-of-the-wild-will-offer-a-trimmed-down-pro-hud-option/), [GamingBolt](https://gamingbolt.com/the-legend-of-zelda-breath-of-the-wild-pro-hud-removes-needless-clutter-from-your-screen)]. Lesson: even the "clean" game shipped an *extra* clean mode.
- Firewatch: a single dot crosshair (can be turned off); the map and compass are objects Henry takes out; other screen UI is radio choices, object names, objective banners. Cost: players get lost [S: [Mechanics of Magic](https://mechanicsofmagic.com/2021/04/28/critical-play-firewatch), [Game Developer](https://gamedeveloper.com/design/how-firewatch-s-ui-enhances-player-immersion)]. Lesson: diegetic is lovely but needs a cheap fallback (our mission marker is that fallback).
- Players hate **all-or-nothing** hiding: Medieval Dynasty players could not hide just the quest list ("the entire HUD or nothing"); Path of Exile 2 players asked for a way to hide the quest tracker because it cluttered a boss fight [S: [Medieval Dynasty thread](https://steamcommunity.com/app/1129580/discussions/0/3427822455454079639), [PoE2 thread](https://steamcommunity.com/app/2694490/discussions/0/830448456536475646)]. Our H key is all-or-nothing; that is okay, but the default should need it less.
- Sim-specific complaints: Two Point Campus - the more you struggle, the more alerts pile up (a summary alert plus one per student), and subtitles sit on top of the alert area [S: [Windows Central](https://www.windowscentral.com/gaming/xbox/two-point-campus-xbox-review)]. Planet Coaster 2 - alerts that do not clear after you fix the problem, and Frontier added per-category notification switches (facilities, finances, guests, rides, staff, water parks) in Update 2 [S: [Frontier forum](https://forums.frontier.co.uk/threads/absolute-feedback-for-the-pc2-devs-must-read.631207), [PC2 update notes](https://www.planetcoaster.com/updates/1-2-0), [Steam thread](https://steamcommunity.com/app/2688950/discussions/0/505068838387206983)]. Supermarket Simulator - players miss customer complaints while busy and ask for a hotkey log or a computer tab [S: Steam suggestion, see `sim-ui.md`]. Lesson: alerts must **clear themselves when fixed** and **merge**.
- Screen edges: Xbox calls the inner **90%** of the screen "title-safe" (at 1080p that is 1720x1000, i.e. 96 px in from left/right and 40 px from top/bottom); important text should live inside it [S: [Xbox screen areas](https://devdocs.xbox.com/build/core-features/graphics/overviews/screen-areas)]. That is for TVs; on a PC monitor 28-32 px margins (ours) are fine, but streamers' webcam boxes often sit in corners [G].

### 1.2 Always-on counts for the games in the brief
**All rows are [K] unless a source is noted** - from memory of playing/watching, no screenshots opened. Count = separate things permanently drawn while you walk/play normally, not counting the crosshair.

| Game | Always-on (about) | What they are | Contextual / elsewhere |
|---|---|---|---|
| Supermarket Simulator | 3-4 | money, day + clock, small task list [K; a [review](https://www.thumbculture.co.uk/supermarket-simulator-pc-review) describes a sequential task list but I could not confirm how it looks], held-item/prompt | Orders, prices, staff all on the in-world computer; complaints missed (see 1.1) |
| TCG Card Shop Simulator | 3-4 | money, level/XP, clock/day | Everything else on PC, prompts near objects |
| PowerWash Simulator | 1-2 | top-left: dirt remaining bar + job % while you spray (this is **PowerWash 2** [S: [TrueAchievements](https://www.trueachievements.com/game/PowerWash-Simulator-2/walkthrough/3)]); crosshair | Objectives, map, shop, settings on the tablet; hide-finished-tasks toggle [S: same]; HUD described as "cleaner and more minimal" in game 2 [S: [Tech-Gaming](https://www.tech-gaming.com/powerwash-simulator-2/)] |
| Stardew Valley | 4 | clock/date/gold box (top-right), energy bar (bottom-right; I believe always on, not sure), hotbar (bottom-center); health bar only in the mines | Journal/quests behind a key; **F4-style screenshot mode hides UI** [S: user-reported, [Steam thread](https://steamcommunity.com/app/413150/discussions/1/405692758722161420)] |
| Animal Crossing NH | 1-2 | almost nothing: item in hand, small pop-ups when something happens | Everything lives on the NookPhone; camera app can hide overlays (an accident Nintendo kept as a feature) [S: [Kotaku](https://kotaku.com/animal-crossing-new-horizons-camera-glitch-is-back-as-1844556994)] |
| Two Point Hospital/Campus | 6-8 (it is a management game) | top bar (money, date, rating), bottom toolbar, alert stack | Staff/patient info on click; alert overload when failing [S: Windows Central above] |
| Planet Coaster 2 | 5-7 | top bar, bottom toolbar, alerts | Per-category alert switches [S: above] |
| Zelda BOTW/TOTK | 2-3 normally | hearts, minimap (hideable in Pro HUD), stamina wheel only when used [K] | Pro HUD leaves only hearts [S: above] |
| Dave the Diver | 3-5 underwater | oxygen/depth/HP readouts, skill icons; more in the sushi bar | Smartphone for menus [S: see `game-ui-craft.md`]. (A [GamingTrend review](https://gamingtrend.com/reviews/dave-the-diver-a-fun-little-deep-sea-adventure/) says late-game *systems* pile up - that is about game systems, not screen clutter; I found no HUD critique.) |
| Lethal Company | 3-4 | health/stamina-ish bars, quota/credits shown on the ship terminal and monitors, scan names (I found no source) | Terminal and ship monitors are the menu [K] |
| Subnautica | 4 | health/food/water/oxygen bars bottom-left; depth/compass when relevant | PDA is the menu. (A "Better HUD" mod exists for *Subnautica 2*, hiding unused bits - hints that even that HUD feels busy, but it is a different game [S: [CurseForge](https://www.curseforge.com/subnautica-2/mods/better-hud)].) |
| Firewatch | 0-1 | dot crosshair (optional) | Map, compass, journal are objects [S: above] |

Pattern I draw from the whole table [G]: **sim games with a person in a world show 1-4 always-on things** (money, time, one task line, prompt). Only top-down *management* games (Two Point, Planet Coaster) show more, because you are not "standing inside" the world.

**Corners [K]:** money/time almost always top corner (left or right), consumable bars bottom-left or bottom-right, prompts center-low, task list one side (small), alerts a corner stack. **Sizes [K, no source]:** a single always-on corner cluster in these games is roughly 8-12% of the screen width; a big status card is the exception. Ours is two 21%-wide columns (measured, see section 0).

---------------------------------------------------------------------------

## 2. Techniques (short definitions, then how each applies)
| Technique | Plain meaning | Good example |
|---|---|---|
| Diegetic info | Information on an object inside the world | PowerWash/Supermarket tablet and PC; Firewatch map [S]; for us: SurfOS computer, desk board, ParkSign |
| Progressive disclosure | Show the 3-5 key things, reveal detail on request | abratabia rule [S]; our J journal already does this for challenges |
| Contextual fade | Appears when the number changes, leaves after a few seconds | Our Park rows (done). BOTW hearts flash on damage [S] |
| HUD on demand | Hold a key to peek, release to hide | Peek idea (Tab-hold or Alt) [G]; Pro/clean mode [S] |
| Objective collapses to one line | Full text only when new | Our mission card (12 s) is half-way; make the *default* the one-liner even in tutorial [G] |
| Notification management | queue, merge, clear when fixed, per-category off, log | PC2 category switches [S]; our Mail tab (done) |
| Proximity UI | Panel shows only when you stand at the thing | Our wave panel (done) - copy it |

---------------------------------------------------------------------------

## 3. "Show it when it changes" - timings
**Honest note: no source gave seconds for BOTW or PowerWash.** The numbers below are my starting points [G], consistent with the motion table already in `game-ui-craft.md` section 1.3 (in 200-300 ms, out about 75% of that, toast out 300-500 ms). Tune by playing.

| Event | Fade in | Stays visible | Fade out | Notes [G] |
|---|---|---|---|---|
| Stat changes (a Park row, XP, rating) | 0.2 s | **3-4 s** after the last change (restart the timer on each change) | 0.4 s | We already flash for 3 s; keep, add the fade |
| Money change | none (always visible, tiny) | - | - | Money is the one number that stays |
| New mission / objective step | 0.25 s | **8 s** full card, then collapse to one line | 0.3 s | Today 12 s, plus always full in tutorial: drop the tutorial exception once the player has done 1 mission |
| Level up / big reward | 0.35-0.5 s | 1.2-2 s banner | 0.3 s | Already built (`Celebration`) |
| "Next level brings..." text | 0.2 s | 5 s after XP is gained, or while holding the peek key | 0.4 s | |
| Goal strip (next upgrade) | 0.2 s | until you move away or afford it; minimize to a thin 1-line strip after 6 s of no change | 0.3 s | |
| Toast | 0.25 s | 3.8 s ok (done); long text +40 ms per char over 40 | 0.4 s | |
| Controls hint | 0.3 s | first 30-45 s of the first day only, then never (they are in Pause) | 1 s | Today 90 s every session |
| Bubble over a surfer | 0.15 s | only while there is something to say or patience is dropping | 0.2 s | Already contextual |
| Peek key (hold) | 0.15 s | while held | 0.2 s | New |

---------------------------------------------------------------------------

## 4. Our elements, ranked
Keys: **KEEP** always on; **CHANGE** show on change; **WORLD** show near the object / diegetic; **MENU** move to journal / computer / phone; **DEL** delete. Judgment [G] using the 30-second test (could you walk and run the park for 30 s without it?).

| Element (today) | Verdict | Reason |
|---|---|---|
| Money pill + tide | KEEP | The one number you glance at every few seconds; it is also the game's "juice" |
| Income / min chip | CHANGE | Useful but not for 30 s of play; show for 4 s when you earn, or as a tooltip on money |
| Day / clock | KEEP (shrink) | Closing time at 8 PM matters; make it a small chip next to money, not its own row |
| Star rating | CHANGE / MENU | Changes slowly; show on change (4 s) and in the computer. Keep stars only on the day report |
| Park level badge + XP bar | CHANGE | Keep the round level badge tiny; XP bar appears 4 s after XP gain |
| "Next level brings..." text | MENU / CHANGE | Move to the level badge tooltip + Journal. Show once right after a level-up (it is then a nice carrot) |
| Today's event (Heat Wave) | CHANGE | Show for 8 s at day start (banner); then only a small icon (sun/wind) next to the clock |
| Daily challenges | MENU | Already 1 line; make it show only when one is completed/progresses, otherwise in the journal (J) |
| Mission card: chapter, title, objectives | KEEP as 1 line | One line "Open the gates  [1/3]" plus the marker is all you need while walking |
| Mission story + tip | CHANGE | 8 s on a new mission, then only in the journal (J). Tutorial exception: show the tip only if the player has not moved toward the marker for 30 s |
| Goal strip (bottom) | CHANGE | Duplicates the mission card as a second "what next" (one is Kai's plan, one is next upgrade). Show it for ~6 s after money changes it, minimize to a thin strip otherwise; hide when a mission objective is on screen [G, see 5.1] |
| Wave machine panel | WORLD (keep as is) | Already only near the machine: the model of how to do it |
| Park panel: Session prices | MENU | Static number you set rarely; belongs on the price board (WORLD) or the computer |
| Park panel: Today's bills | CHANGE | Show when it changes and at 6 PM / closing; always in the day report |
| Park panel: lost customers, trash | CHANGE (done) | Keep the flash. Add a world signal for trash (see rec 8) |
| Park panel: board rental, staff wages, happy surfers | MENU / CHANGE | Computer Stats tab; flash on change only |
| Toasts | KEEP | Right pattern; merge duplicates, route low-value ones only to Mail |
| Hint line | CHANGE | First day only; the rest is in Pause / key legend |
| Crosshair + [E] prompt | KEEP | Contextual by nature; keep the dot tiny when nothing is in reach |
| Speech bubbles | KEEP, limit | Fine if only a few at once: cap at ~4 visible, nearest first [G] |
| Mission marker | KEEP | Best "quest tracker" there is: one diamond. Consider hiding when the mission card is hidden |
| Surf HUD (score, flow, combo) | KEEP while surfing | Separate mode; ok. While surfing, `hud-root--surfing` already hides the mission card, goal strip and Park panel (the status card stays) - the model for a quiet HUD |

### Target layout: walking play, nothing happening
```
+----------------------------------------------------------+
| [$ 1,240 ]  Day 3  2:40 PM  (sun)         (toasts center)  |   <- one small chip row, ~20% width
| Lv 4 (small badge)                                         |
| > Open the gates [1/3]                                     |   <- 1-line mission, ~20% width
|                                                            |
|                       (   .   )                           |   <- dot crosshair
|                       [E] Use                              |
|                                                            |
|                                                            |   <- bottom: empty
+----------------------------------------------------------+
```
- Top-left: 3 things (money+clock chip, level badge, 1-line mission). Top-right: **empty** unless a stat just changed or you are at the machine. Bottom: empty.
- Everything else slides in for 3-8 s when it changes, or opens with **J** (journal: story, tip, challenges, level preview, session prices) or the computer.
- That drops always-on elements from about 14 (money, income, level, XP, next-level, stars, clock, event, challenge, mission 4 lines, Park 2 rows, goal strip) to about **5** (money, clock, level badge, mission line, crosshair/prompt) [G, my count]. It meets the "3-5 by default" rule [S].
- Area: from about 40% of the width at the top to roughly 22% (one small cluster) [G estimate].

Why this works [G]: the player learns "the corners are quiet; if something shows up, it just changed". That is the PowerWash/BOTW idea, and it lets the nice beach be seen, which is the thing we are selling.

---------------------------------------------------------------------------

## 5. Ranked recommendations
Ranked by feel gained per hour of work. Items already shipped are left out (H clean mode, Park row flash, wave panel near-machine, J journal, Mail tab, toast max 3).

| # | Change | Size/time | Touches |
|---|---|---|---|
| 1 | **Mission card default = one line** (title + `[x/y]`, 44 px high). Full card (story, tip) for **8 s** on a new mission, with J, or with the peek key. Remove the "first 6 missions always full" exception (`missions.Index < 6`) (replace: tip appears again if the player idles 30 s) | S | mission card |
| 2 | **Quiet top-right.** Park panel hidden by default; Session prices and rental move to the computer/price board; "Today's bills" appears on change and after 5 PM. Rows stay up **3-4 s** with a 0.4 s fade | S-M | Park panel, bills |
| 3 | **Status card slim-down.** One row: coin + money, clock chip next to it. Level = small round badge. XP bar and "next level" show **4-5 s** after XP is gained and right after level-up; stars show on change. Drop the income/min row unless money just rose (4 s) | M | status card, level, XP, stars, income, event |
| 4 | **Hold-to-peek.** J already toggles the full mission details, challenge list and all Park rows, so "HUD on demand" exists as a toggle. Add a *hold* version on a free key (Alt; Tab opens the menu): full HUD fades in 0.15 s while held, out 0.2 s after release. A toggle is easy to forget on; a hold never gets stuck | S | all panels |
| 5 | **One "what's next" only.** When a mission points at an upgrade, the goal strip (`PlanUpgrade`) repeats the mission's objective; otherwise it shows the cheapest upgrade you could buy next - so it is sometimes a duplicate and sometimes a second goal. Show the goal strip only when no mission objective needs the player, or fold it into the mission line ("Next: Pool Expansion - $480"). Minimize to a thin strip after 6 s | S | goal strip, mission card |
| 6 | **Hint line first day only** (30-45 s), then only in Pause; show again the first time the player enters build mode or a shop | S | hint line |
| 7 | **Challenges only on progress.** A one-line pill for 4 s when a challenge advances or finishes; otherwise J. Today it is an always-on card row | S | challenge card |
| 8 | **Put numbers on objects.** Trash count on the bin / a sign at the entrance; session prices on the price board (reading it opens the price window); bills on the office desk or a clipboard (diegetic idea, PowerWash tablet style). Keep a 3 s HUD flash too, so nothing is missed | M-L | Park panel, prices, trash |
| 9 | **Notification manager.** Merge duplicates ("Beach is full! x3"), auto-clear alerts when the cause is fixed, tag each toast with a category and let low-value ones go to Mail only; later a per-category switch in Settings like Planet Coaster 2 [S] | M | toasts, Mail |
| 10 | **Event as a banner then an icon.** "Today: Heat Wave" is an 8 s banner at the morning bell, then a small icon beside the clock with a tooltip | S | event sticker |
| 11 | **Per-element HUD setting.** Settings: HUD amount = Full / Normal (new default) / Minimal (money + prompt) + the existing H clean toggle. Avoids the "all-or-nothing" complaint [S] | M | all panels |
| 12 | **Limit speech bubbles.** At most ~4 visible, nearest first, fade bubbles for surfers behind you or farther than ~25 m | S | bubbles |
| 13 | **Mission marker only when it helps.** Hide it while the target is on screen and within ~6 m; show distance only when it is more than ~15 m away | S | marker |
| 14 | **Peek-able level preview.** "Next level brings..." becomes a tooltip on the level badge and a line in the journal | S | level text |
| 15 | **Playtest with the 30-second test.** Ido covers each card with a sticky note, plays a day, and writes down which note he removes first. That list overrides everything above [S: abratabia test] | S | all |

---------------------------------------------------------------------------

## Ideas for our game
- **Step 5 (polish, one session, all small):** recs 1, 5, 6, 7, 10, 12, 13. All are visibility rules in `GameHud.cs` and `Game.uss` (a timer plus a `hidden`/fade class, the same trick `Flash()` uses).
- **Step 5, second session:** recs 2, 3, 4 (the new quiet HUD and the peek key). Check that tutorial players still know what to do: keep the marker and the first mission tip.
- **Before the Steam build:** recs 9 and 11 (notification categories and a HUD-amount setting). Reviewers and streamers hit these first.
- **Later / new content:** rec 8 as world objects: a price board by the gate, bills clipboard in the office, trash meter on the bin. Each also gives the player a reason to walk to places, which is the genre's core fun (like the Supermarket PC).
- **Design rule to keep (for every new feature):** a new system gets *one* of: a world object, a 3-8 s flash, or a menu tab. It does not get a permanent card. Ask "can the player play 30 s without it?" first.
- **Open question for Ido:** do the numbers (income/min, stars, XP) feel like *rewards* he wants to see all the time? If yes, keep a slim version, because a sim is partly about watching numbers rise [G].

## Sources
- [abratabia: game UI mistakes](https://www.abratabia.com/game-ui-design/game-ui-mistakes.php), [HUD patterns guide](https://cdn.jsdelivr.net/npm/@hegemonart/get-design-done@1.60.4/reference/domains/gaming-patterns.md), [Wikipedia: HUD](https://en.wikipedia.org/wiki/HUD_(video_games))
- BOTW: [Famicast, immersive UI](https://www.thefamicast.com/2017/12/part-2-immersive-impressionable-ui.html), [GamingBolt Pro HUD](https://gamingbolt.com/the-legend-of-zelda-breath-of-the-wild-pro-hud-removes-needless-clutter-from-your-screen), [ZeldaUniverse Pro HUD](https://zeldauniverse.net/2017/02/08/breath-of-the-wild-will-offer-a-trimmed-down-pro-hud-option/)
- Firewatch: [Mechanics of Magic](https://mechanicsofmagic.com/2021/04/28/critical-play-firewatch), [Game Developer](https://gamedeveloper.com/design/how-firewatch-s-ui-enhances-player-immersion)
- PowerWash: [TrueAchievements PW2 notes](https://www.trueachievements.com/game/PowerWash-Simulator-2/walkthrough/3), [Tech-Gaming PW2 review](https://www.tech-gaming.com/powerwash-simulator-2/)
- Complaints: [Medieval Dynasty](https://steamcommunity.com/app/1129580/discussions/0/3427822455454079639), [PoE2](https://steamcommunity.com/app/2694490/discussions/0/830448456536475646), [Two Point Campus review](https://www.windowscentral.com/gaming/xbox/two-point-campus-xbox-review), [PC2 forum feedback](https://forums.frontier.co.uk/threads/absolute-feedback-for-the-pc2-devs-must-read.631207), [PC2 update 1.2.0 notes](https://www.planetcoaster.com/updates/1-2-0), [Supermarket Simulator review](https://www.thumbculture.co.uk/supermarket-simulator-pc-review)
- Other: [Xbox screen areas](https://devdocs.xbox.com/build/core-features/graphics/overviews/screen-areas), [Kotaku ACNH HUD-free camera](https://kotaku.com/animal-crossing-new-horizons-camera-glitch-is-back-as-1844556994), [Stardew UI-hide thread](https://steamcommunity.com/app/413150/discussions/1/405692758722161420), [Subnautica Better HUD mod](https://www.curseforge.com/subnautica-2/mods/better-hud), [GamingTrend Dave the Diver](https://gamingtrend.com/reviews/dave-the-diver-a-fun-little-deep-sea-adventure/)
- Code read (read-only): `Assets/_Game/UI/Hud.uxml`, `Assets/_Game/UI/Game.uss`, `Assets/_Game/Scripts/UI/GameHud.cs`.
