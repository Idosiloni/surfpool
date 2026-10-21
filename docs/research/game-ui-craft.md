# Game UI craft: what great games do, and an audit of our UI
Date: 2026-10-21

Tags: **[S]** = from a linked source, **[K]** = general game-UI knowledge from the named games (I could not open screenshots, so no pixel specs), **[G]** = my guess/suggestion.
Does not repeat `sim-ui.md` (HUD layout of job sims), `ui-powerwash.md` (the "Sunset Surf Shop" brief, first gap list) or `polish-and-next.md` (juice list). Many items from `ui-powerwash.md` are now built (Lilita font, chunky buttons, stickers, banners, toasts with kinds, count-up money). This file looks at what is **still** missing.

Sourcing note: web search was thin for game-specific teardowns (Hades, Dave the Diver, Skate had no good UI articles). I say so where it matters.

---------------------------------------------------------------------------

## Part 1 - What makes game UI excellent

### 1.1 Show only what is needed, when it is needed
- A common model sorts UI into 4 kinds by two questions: "is it part of the story world?" and "is it in the 3D space?" -> non-diegetic (flat HUD), diegetic (a screen/sign inside the world), spatial (floating in the 3D world), meta (screen blood, dirty lens). From "Beyond the HUD" (Fagerholt and Lorentzon, 2009) [S: [Wayline summary](https://www.wayline.io/blog/your-ui-is-killing-immersion-fix-it), [Stray Spark](https://www.strayspark.studio/blog/game-ui-ux-design-principles)].
- Practical rule: start from an empty screen, add only what earns its place. Always-on = what you need every second; prompts, status and objectives appear when relevant; the rest sits behind a menu [S: same].
- Diegetic UI needs a fallback (if the player can't see the thing, they can't read it) [S: same].
- A 2014-2025 study of 30 Game of the Year nominees found HUDs getting more minimal and more context-based over time; players called minimal HUDs more immersive, but clarity depended more on familiarity than on minimalism [S: [Uni Bonn paper](https://bonndoc.ulb.uni-bonn.de/xmlui/handle/20.500.11811/14207?show=full)].
- Forza Horizon: HUD is mostly transparent, speed/map in the lower corners so the road stays visible; the map uses size and contrast to rank importance (story events bold, side stuff dull) [S: [Mechanics of Magic](https://mechanicsofmagic.com/?p=6144)].
- Riders Republic warning: always-popping world-space names and mission markers annoyed players, and a "No HUD" option that doesn't remove them felt broken [S: one [Steam post](https://steamcommunity.com/app/2290180/discussions/0/3817410454880410616), single opinion]. Lesson: give a real "clean screen" switch.
- Zelda BOTW/TOTK [K]: hearts and a round stamina wheel only appear/grow when used; the map/inventory live on an in-world tablet (Sheikah Slate), so the world stays on screen. Hades [K]: health bar and a few icons, big damage numbers; all the reading happens in pause-style menus. A Short Hike [K]: almost no HUD; the stamina feather and a small coin counter.
- Phone/tablet as the menu is a pattern: Animal Crossing's Nook Phone (home-screen icons, a red badge on the app that needs you) [S: [UX Collective](https://uxdesign.cc/animal-crossing-delight-by-design-e2c0a1a4d462)], Dave the Diver's smartphone [S: [Gear Diary](https://geardiary.com/2023/05/01/dave-the-diver-early-access-review-a-great-start-and-i-want-to-dive-deeper/)]. We have the SurfOS computer; the badge idea is already in our `.tab-badge`.

### 1.2 Hierarchy, type and contrast
- **Text size (real numbers).** Xbox Accessibility Guideline 101: minimum default text height on **PC/VR = 18 px at 1080p (36 px at 4K)**, console = 26 px at 1080p (52 at 4K); players should be able to scale text up to **200%** [S: [Xbox XAG 101](https://devdocs.xbox.com/gaming/accessibility/xbox-accessibility-guidelines/101)]. Other numbers floating around: Game Accessibility Guidelines cite 28 px at 1080p for 10-foot TV UI as a floor; IGDA GASIG says ideally 32 px at 1080p, 46 px for short-lived text like subtitles [S: [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/?p=3285), [IGDA GASIG](https://igda-gasig.org/?p=1387)]. Xbox handheld: never below 14 px, recommended 18 px [S: [Xbox handheld](https://devdocs.xbox.com/build/gdk-and-engines/handheld/handheld-guidelines-and-testcases)]. My reading for a PC mouse game: **body 18+ px, important numbers 24+ px, nothing smaller than 14 px** (and 14 only for rarely-read extras).
- **Contrast.** WCAG-style targets: 4.5:1 for normal text, 3:1 for large text; keep contrast high over busy backgrounds [S: [secondary summary](https://abratabia.com/game-accessibility/visual-accessibility.php)]. Over a 3D scene you cannot control the background, so use an outline or a dark pill behind text, never bare text [K].
- **One idea per element.** Persona 5 turns every menu into a poster: huge type, a hovered item grows and gets a color that stands out from the red/black/white palette, strong diagonals; its UI lead says he treats menus as entertainment, and the typography was inspired by ransom-note album covers [S: [Mechanics of Magic](https://mechanicsofmagic.com/2022/04/23/visual-design-of-games-persona-5/), [Atlus interview coverage](https://www.actugaming.net/les-menus-stylises-de-persona-et-metaphor-refantazio-sont-tres-beaux-mais-aussi-tres-penibles-a-concevoir-selon-atlus-678450/)]. Lesson [G]: a strong identity trick used everywhere (for us: wax stickers, wave edges) beats many small decorations.
- **State colors in lists.** A Hades II fan mod re-sorts boons into picked / available / sacrifice / unavailable with different title colors and half transparency for the unavailable [S: [Thunderstore mod page](https://thunderstore.io/c/hades-ii/p/SMarBe/Improved_Boon_Info_UI/v/1.2.0/)] - the base game's list was ambiguous enough that players fixed it. Lesson: every card needs clear states (can buy / can't yet / owned / locked) that differ by more than color.
- **Color roles.** Few colors, each with one meaning (Splatoon: your team ink color is the HUD color, which ties UI to play [K]). We already do this (yellow = money, coral = bad, teal = go).
- **Do not use color alone** for critical info: add a shape, icon or text [S: Wayline, abratabia above].

### 1.3 Motion: numbers to start from
Sources are style guides, not playtested game research, so treat as starting points to tune by feel [S: [Motion Design guide](https://git.secluded.site/agent-skills/blob/main/vendor/impeccable/skill/reference/motion-design.md), [GSAP easing reference](https://cdn.jsdelivr.net/npm/claude-code-skills@0.5.0/data/skills/gsap-animation-helper/reference/easing.md)].
| What | Duration | Curve | Notes |
|---|---|---|---|
| Button press / hover feedback | 80-150 ms | ease-out | Instant-feeling |
| Window / menu open | 150-300 ms | ease-out, optional small overshoot | Start from scale 0.9, not 0 |
| Window close | about 75% of open (110-200 ms) | ease-in or ease-out | Exits should be faster than entrances |
| Toast in / out | 200-250 in, 300-500 out | ease-out / ease-in | |
| Stagger between siblings | 40-80 ms each, stop after about 5 | same as parent | Cap total at about 400 ms |
| Reward banner (level-up, purchase) | 350-500 in, hold 1.2-2 s, 300 out | ease-out-back | Playful games can overshoot more |
- "ease-out-back" = moves past the target then settles. Good for playful UI; keep it off tiny text and off things that appear a lot [S: same guides].
- Balatro layers shake + particles + number roll + sound at once, and scales shake with score size; players on Steam also reported the cursor-driven UI sway made some of them sick, which is why it has a toggle [S: [design breakdown](https://blakecrosley.com/guides/design/balatro) (the code in it is the author's own illustration), [Steam thread](https://steamcommunity.com/app/2379780/discussions/0/4201364375109001844/?ctp=2)]. Persona 5 / Celeste / Slay the Spire [K]: motion carries meaning (hover grows, selected pulses), not just decoration.
- **Reduce-motion option**: set transition times to 0 (Roblox's docs suggest exactly that) and offer to turn off screen shake, camera bob, parallax [S: [Roblox accessibility docs](https://create.roblox.com/docs/en-us/production/publishing/accessibility.md), Xbox guideline 117 as listed in search].

### 1.4 Juice for numbers and rewards
- Money: count up to the new value (about 0.3-0.6 s), pop the label (scale 1.1-1.2 for about 100 ms), spawn "+$" that floats and fades (1-1.5 s), tiny coin sound whose pitch rises when many come in a row [K; matches PowerWash's per-piece cash ding in `polish-and-next.md`].
- Scale the effect to the size of the win (Balatro: bigger score = bigger shake) [S: Balatro breakdown above]. A $5 tip and a $500 contest prize should not feel the same.
- Combine channels: motion + color + sound + particles beat any one alone [S: same].

### 1.5 Notifications (toasts)
General (non-game) rules, adapted [S: [Product Builder notification doc](https://productbuilder.net/ru/skills/notification-design), [Singapore Gov design system](https://designsystem.tech.gov.sg/components/toast)]:
- Limit how many show at once; extra ones wait in a **queue**, not get thrown away.
- Skip duplicates; merge repeats ("3 new surfers" not 3 toasts).
- Errors stay a bit longer (or until clicked); success/info auto-dismiss in a few seconds; 2-3 lines max.
- Higher priority can push lower priority out.
- [K/G] Games add: a **log** you can open later (Animal Crossing has a chat log in the phone [S: [Nookipedia](https://nookipedia.com/wiki/NookPhone)]; Supermarket Simulator players asked for one, see `sim-ui.md`), and a sound per kind.

### 1.6 Menus, hover, focus, tooltips
- Hovered item gets: bigger or lifted + color change + (soft) sound. Persona 5 does all three [S: Mechanics of Magic above].
- Keyboard/controller: every clickable thing needs a visible **focus state** that looks different from hover, a logical order, and Esc/back to leave. Two Point Campus is praised because its UI and bindings were designed for controllers from the start, with any item reachable in a couple of presses; Planet Coaster 2 is criticised for deep nested menus that hide basics and tooltips that vanish once you pick an object [S: [Windows Central](https://www.windowscentral.com/gaming/xbox/two-point-campus-xbox-review), [Frontier forum](https://forums.frontier.co.uk/threads/feedback-after-a-few-days-the-ui-lack-of-rides-and-so-much-more-to-say.629756), [Xbox Tavern](https://www.xboxtavern.com/planet-coaster-2-review/)]. Lesson: keep menus flat (our 5 tabs are good), and one-sentence tooltips on everything with a number.
- Tooltip pattern [K]: appear after about 300-500 ms hover, stay while hovering, show the verb + effect + cost, never cover the thing they explain.

### 1.7 Onboarding prompts
- Key prompt shaped like the key, near the thing, only while you can use it; "can't" reasons shown (greyed + reason) [K; see `sim-ui.md` section 2].
- World-anchored hint (arrow/ring on the target) beats a text line. PowerWash 2 floats a reticle over the last dirty spots [S: via `ui-powerwash.md`].
- Hints fade once the action has been done a few times [K].

### 1.7b Other games from the brief (general knowledge [K], no source opened, one lesson each)
- **Stardew Valley:** everything lives on one always-visible clock/gold/energy corner plus a hotbar; level-up and "letter in the mailbox" are small screens with their own sound. Lesson: a mailbox/log (our #14) and a short celebratory moment for small wins.
- **Unpacking:** almost no UI; the "interface" is the objects you place, and a soft chime tells you an item is in the right spot. Lesson: feedback through the world (sound + glow when something is placed right), good for our build mode and shop stations.
- **Celeste:** UI is tiny and quiet (strawberry/death counters appear only when changed, then leave); pause menu has accessibility "Assist Mode" sliders. Lesson: counters that show on change then fade, and generous assist options.
- **Slay the Spire:** huge readable numbers on cards and intents (icon + number over enemies), hover shows keyword tooltips for any bold word. Lesson: tooltip on every number/keyword (#13), icon + number over customers (we have patience bars).
- **Mario Kart:** big chunky item slot, position number, short animated call-outs (item roulette, "FINAL LAP"), all with bouncy motion; the HUD is toy-like, not realistic. Lesson: our sticker/ribbon style fits; call-outs should scale with the event size (#12).
- **Splatoon 3:** team color = HUD color, tied to the ink in the world. Lesson: our wave-difficulty colors (beginner blue, intermediate indigo, pro magenta) already do this; reuse them on bubbles, report and call-outs.

### 1.8 Action-sports score displays
- Tony Hawk (1+2): meter top-left that fills from any scoring action, flashes/changes color when full (special tricks unlocked), a running combo list where special tricks are yellow and gaps are blue, a multiplier that rises per trick; bailing drops the whole combo (and in 1+2, the special meter too); a "perfect landing" bonus (+50% in THPS2) [S: [speedrun.com guide](https://www.speedrun.com/thps1/guides/1wg8v), [StrategyWiki](https://strategywiki.org/wiki/Tony_Hawk%27s_Pro_Skater_2_(Game_Boy_Advance)/Gameplay), [Gfinity](https://www.gfinityesports.com/article/tony-hawk-s-pro-skater-1-2-remake-remaster-how-special-tricks-guide-ps4-xbox-pc)]. I found no good source on Skate or Riders Republic trick-score fading; [K]: they show trick names big and short, then fade quickly.
- Lessons: [G] big text near the middle for the moment (1-2 s), small sticky total near an edge, color-code special/risky moves, show what you stand to lose (combo timer).

### 1.9 End-of-day / results screens
- Small "moment" screens matter: Animal Crossing, Stardew, Overcooked all have level-up, "you got X", end-of-round score cards [S: lists in `ui-powerwash.md`].
- Good results screens [K]: lines appear one by one (stagger 80-150 ms) with a tick sound, totals count up, one highlight ("best moment"), one clear button for "next".

### 1.10 UI sound
- Convention: rising tone = confirm/forward, falling = back/cancel, flat/buzz = denied. Hover should be near silent. Every navigation sound must still be pleasant at the 100th repeat; shorter and softer wins. Build the whole set from one base sound by pitching/shortening/filtering so it forms a "pitch family". Big actions (buy, level-up) get richer sounds than row navigation [S: [bugnet UI sound article](https://bugnet.io/blog/ui-sound-design-for-indie-games) - I only have the search-result summary; when I tried to open the page I got the site's blog index, so I could not read the article itself. Treat as one practitioner's opinion].
- Animal Crossing gives each distinct action its own sound, which teaches the player what happened [S: UX Collective above].
- [G] Numbers to start: hover 10-30% of click volume, 40-80 ms long; click 80-150 ms; confirm/buy 250-500 ms; random pitch +-4%.

### 1.11 Accessibility checklist
- Text scale up to 200%, UI scale slider, high-contrast text, shape+color for state, reduce-motion switch, screen-shake/camera-bob off, no flashes over 3 per second, subtitles for voice [S: Xbox XAG 101; Game Accessibility Guidelines; abratabia above].

---------------------------------------------------------------------------

## Part 2 - Audit of our UI (read-only)

Files read: `Game.uss` (whole), `Hud.uxml`, `Settings.uxml`, `UpgradeScreen.uxml`, `WaveControl.uxml`, `UiMotion.cs`, `Core/Notifications.cs`, `GameHud.cs` (money, toasts, prompt, hidden toggles), `HoldRing.cs`, `DayReportScreen.cs`, `UpgradeScreen.cs` (tabs + card builder), `SurfHud.cs`, `MainMenu.cs`, `PauseMenu.cs`, `Audio/Sfx.cs`, `SoundBank.cs`; I grepped every `"hidden"` toggle in `Scripts/UI` and the `Sfx` calls in `PerkPicker.cs`. Only skimmed (USS + grep, not read line by line): `ShopHud.cs`, `StoryScreen.cs`, `ParkNamer.cs`, `WaveControlScreen.cs`, `BuildBar.uxml`, `Fishing.uxml`; findings about those are limited to what the USS and the grep show.
Checks done on my own claims: **close animation** = every screen closes with a bare `AddToClassList("hidden")` (UpgradeScreen:65, DayReport:133, PerkPicker:137, ParkNamer:176, Pause:128, Settings:75, Story:66, WaveControl:71, MainMenu:92); no `Hide` helper exists. **Upgrade cards**: `BuildCards()` clears and rebuilds every card in one go. **Prompt**: key "E" is typed into `Hud.uxml`; `UpdatePrompt()` only changes the label text.

### 2.0 What is already good (keep)
- Strong identity: ink outlines, thick bottom edge, stickers tilted 3-8 deg (`.cost-chip`, `.window-tag`, `.card-sticker`, `.price-stamp`), Lilita/Fredoka/Pacifico roles, color tokens in `:root`.
- Panel is `ScaleWithScreenSize` 1920x1080 (`GreyboxBuilder.cs:1487`), so px values below are "at 1080p".
- Money: real count-up (`shownMoney` in `GameHud`) + `.money-label--bump` (scale 1.12, 110 ms) + `.popup` "+$" floaters (1.3 s) + `Sfx.Money` merges sounds.
- Window pop-in (`UiMotion.Show`, `.screen-overlay--enter`: opacity 0, scale 0.92, 24 px down, 0.22 s ease-out-back). Perk cards already stagger (`.perk-card--1/2`, 0.08/0.16 s delay).
- Hold ring, dot crosshair that grows (`.crosshair--active` 10 -> 24 px), key-cap prompt, key-cap legends (`.key-legend`), sound on every Button click (`Sfx.OnUiClick`), error/buy sounds from `UpgradeShop`.

### 2.1 Screen-by-screen
| Screen / element | Does today | Weaknesses vs Part 1 |
|---|---|---|
| **Status card** (`Hud.uxml` `.status-card`, top-left) | Money pill, income chip, level badge + XP bar + next-level text, 5-star rating, day/clock, event sticker | 6 stacked things always on (this is the right "always visible" set, but it is dense). `.level-next` is **12 px**, `.xp-label` 14 px (below the 14-18 px floor). `.income-rate` and `.day-label` use `-unity-font-style: bold` on Fredoka (fine, but number rows would read better in Lilita). XP bar has `transition-duration: 0.4s` but no "level up" flash or fill-to-full-then-reset |
| **Mission + challenge cards** (`.mission-card`, 380 px, under status card) | Chapter strip, title, story, objectives with checkboxes, hint. Story/hint collapse (`missionStory/missionHint` toggled by `details`), challenge list opens with J (`journalOpen`) | Already partly contextual. Still `.mission-story` 15 px, `.mission-hint` 15 px. With status + mission + (challenge title) the left column is tall. `.objective-check--done` has no tick animation (flat color change) |
| **Wave + Park panels** (`.top-right`, 370 px) | Wave panel shows **only when you are near the machine** (`wavePanel` hidden when `!near`); Park panel always on with 7 stat rows (`.stat-row`), some hide at zero (`lostRow`, `trashRow`) | `.small-text` 16 px, `.stat-name/value` 18 px OK. The Park panel never hides, so 3-4 rows of numbers sit in the corner all day. No change flash when a stat changes (e.g. trash +1). Bills row is color `--wood` only |
| **Goal strip** (`.goal-strip`, bottom center, 660 px) | Next upgrade + bar + "Ready!" state; hides while the shop, build mode or fishing is open | Good. `.goal-amount` 17 px. No pulse/sparkle when it becomes `--ready` (only border/tile color) |
| **Crosshair + prompt** (`.crosshair`, `.prompt-box`) | Dot grows to 24 px sun ring; `[E] verb` box 1 px below | `.prompt-box` has no enter animation (pops in), key is hard-coded text "E" in `Hud.uxml`, no "can't" (greyed + reason) variant, no hold-vs-tap look (the `HoldRing` is separate). Not world-anchored: always at screen center |
| **Hint line** (`.hint`, bottom-left) | "WASD move . Shift ..." fades after a while (`.hint--hidden`, 1.5 s) | Plain sentence; not key-caps like the rest of the UI; one long line. A controls cheat sheet **does** exist in the Pause menu (`PauseMenu.Controls`, one plain multi-line `Label`), but it is a text wall, not key-caps, and `Settings.uxml` has no controls/keybinds (only sensitivity, invert, 3 volume sliders, quality, fullscreen) |
| **Toasts** (`GameHud.ShowToast`, `.toast*`) | Ink pill + icon tile by kind (Info/Good/Reward/Bad), slide down 24 px, 0.25 s ease-out-back, 3.8 s then 0.5 s leave; **max 3** | When a 4th arrives, `toasts.RemoveAt(0)` deletes the oldest **instantly** (no leave animation, message lost). No de-duplication (same text twice = two pills), no priority, no merge ("+3 more"), fixed time even for long text, no sound per kind, no message log to read missed ones. Icons: `icon--wave` for Info is generic. `.toast-text` 20 px good |
| **Cash popups** (`.popup-stack`, `ShowPopup`) | "+$12 reason" green pills floating up, `top = childCount * 30` | Popups are absolutely placed and overflow is visible, so they do not clip; the bug is **overlap**: when an earlier popup is removed after 1.5 s, `childCount` drops and a new popup can reuse a slot still occupied by one that is fading. No size scaling (a big sale looks like a tip). Fixed 1.3 s |
| **Surfer bubbles** (`.bubble`, `GameHud` bubble layer) | Skill badge + text + patience bar above surfers | Patience bar green / sun / coral only (color-only). `.skill-badge` 15 px letter. Text 17 px |
| **Windows** (`.screen-window` 1100x780, header strip, `.window-tag`) | Upgrade computer, Settings, Day report, Story, Pause, Wave control | **Fixed pixel sizes** (1100x780, 1220 wide report, 1040 perk) - fine at 16:9, but the scale mode "match" setting should be checked for ultrawide/4:3. Open has pop-in; **close has no animation** (`UiMotion` has only `Show`/`Enter`/`Bump`; all 9 screens close with a bare `.hidden`, see the list above). No round X; the upgrade and wave windows have a clickable `[Esc] Close` key-button in the footer (good, matches PowerWash), the rest use "Done"/buttons |
| **Tabs** (`.tab`, `.tab--active`, `.tab-badge`) | Colored active tab lifts 3 px, badge shows buyable count | Good. No keyboard/controller switching (Q/E or 1-5 hint), badge doesn't pop when the number rises |
| **Upgrade cards** (`.upgrade-card` 490 px) | Icon tile, title, crooked price chip, desc, buy button, owned stamp, PLAN/NEW stickers | `.card-requires` 15 px, `.card-stars-text` 15 px. No hover state on the **card** itself (only the button), so you cannot tell which card the mouse is over. Buying: the button squashes and `Sfx.Buy` plays, but the card itself doesn't react (no stamp-slam on `.price-stamp`, no flash). No tooltips (long effects only in `.card-desc`). Cards are built in one loop with no stagger. **Important for any card animation:** `UpgradeScreen.Update` calls `BuildCards()` (which does `cards.Clear()` and rebuilds every card) whenever money, owned count or level changes, and `SelectTab` does the same. A per-card hover/stamp animation would be wiped by the next rebuild, so animate by remembering the bought `id` and applying the class to that card right after the rebuild; also check the ScrollView keeps its scroll position (I did not test this) |
| **Buttons** (`.buy-button`, `.send-button`, `.close-button`, `.segment`, `.tab`) | hover scale 1.03, `:active` scale 0.96 + 3 px down, 0.1 s | Good. **No `:focus` styles anywhere in Game.uss** -> keyboard/controller users cannot see where they are. `:disabled` removes hover but the look change is only color |
| **Wave control** (`.wave-card`, `.segment`) | 3 wave cards, selected lifts 6 px, segment rows | `.wave-card-text` 16 px. Locked = `opacity: 0.5` (color/opacity only; has red text reason, good) |
| **Day report** (`DayReportScreen`, `.report-*`) | Title, summary line, income/expense rows, review cards, facts, one totals line | **Everything appears at once**; numbers are static text (no count-up, no row-by-row reveal, no "profit" slam). `.report-fact` 15 px, `.review-who` 14 px. Totals are one long text line (`Income . Spent . Profit`) instead of a big profit number. No "best moment" card/photo |
| **Level-up / celebrations** (`.celebration`, `.perk-*`) | Banner scales in 0.45 s with 84-104 px title; perk cards pick with mouse or 1/2/3; `PerkPicker` plays `Sound.Mission` on open and `Sound.Buy` on pick | Strong (best-feeling screen we have). Perk window has no close animation; no hover sound on the cards; cards have hover/active scale but no `:focus` |
| **Surf HUD** (`SurfHud`, `.surf-*`) | Score 40 px, flow bar, combo, call-out 46 px with pop (80 ms), score card (shown 3.2 s) | `.surf-flow-label` 13 px. Call-outs are bare text with outline (OK over water). `OnCall(text, mood)` only picks a color: size, hold time (`CallSeconds` 1.4 s) and pop are the same for a small carve and a huge air. Score label is just `score.text = ...` every frame (no pop or count). Flow bar color changes at "hot" (>0.75) but no pulse; combo timer is a 4 px bar. No visible "combo lost" animation besides the call-out text |
| **Main menu** (`.menu-*`) | Poster, logo 80 px, entrance slide | Footer 14 px and `#9AA5AB` (about 2.4:1 on cream by my calculation - too faint). No keyboard focus, no hover sound |
| **Settings** (`Settings.uxml`) | Sliders, toggles, quality | Missing: **UI scale**, text size, reduce motion / screen shake, colorblind-safe mode, subtitles, key bindings page, FOV |
| **Build bar** (`.build-card`) | Cards 148 px wide, selected lifts 4 px | `.build-beauty` 13 px, `.build-key` 14 px light grey `#9AA5AB`; `.build-card--expensive` = opacity only |
| **Fishing** (`.fish-*`) | Status 28 px, tension bar | Tension is color change only (sun -> coral); add shake/pulse |

### 2.2 Cross-cutting gaps
1. **Tiny text:** 12 px (`.level-next`), 13 px (`.build-beauty`, `.surf-flow-label`), 14 px (`.xp-label`, `.review-who`, `.build-key`, `.menu-footer`). Floor for PC is 14, goal 18 (Xbox XAG 101).
2. **Low contrast greys:** `#8FA3AB` (done objectives, `.icon--small`, `.report-empty`) and `#9AA5AB` (menu footer, build key) are about 2.4:1 on cream (my calc). `--muted` `#5B6B73` is about 5:1, fine.
3. **No focus state** (`:focus` appears nowhere in `Game.uss`), no keyboard/controller navigation except per-screen hotkeys (PerkPicker 1/2/3, Esc, Tab).
4. **UI sound is only "click on Button"** (`Sfx.OnUiClick`). No hover, back, toggle, tab, toast, reveal, count tick, window open/close sounds. Everything shares one `Sound.Click`.
5. **No exit animations** (windows, prompt, cards).
6. **No accessibility options** (UI scale, reduce motion, colorblind, text size).
7. **No tooltips.**
8. **No clean-screen switch** for streamers/screenshots (hide the three left cards + right panels). A Tab/F-key "photo mode" fits the Riders Republic lesson.
9. **Spacing is hand-set per class** (margins 4, 5, 6, 8, 10, 12, 14, 18 px mixed); no spacing tokens in `:root`. Not wrong, but makes alignment drift.
10. **Not world-anchored:** only the mission marker and surfer bubbles are in 3D space; interaction prompts, shop prompts and "press E to flip sign" are screen-center.

---------------------------------------------------------------------------

## Part 3 - Ranked improvements
Ranked by impact for effort. **S** = under 1 hour, **M** = about 1 session, **L** = bigger. Colors are our `:root` tokens.

| # | Change | Size |
|---|---|---|
| 1 | Toast queue + dedupe + sound per kind | S |
| 2 | Raise tiny text to 14/18 px and fix grey contrast | S |
| 3 | `:focus` + card hover states | S |
| 4 | UI sound family (hover/back/toggle/tab/toast/reveal) | S-M |
| 5 | Day report reveal: row-by-row, count-up, profit slam | M |
| 6 | Purchase "stamp slam" on upgrade cards | S |
| 7 | Window close animation + stagger on card grids | S |
| 8 | Surf-wave screen wipe (foam edge) between day report / next day / main menu | M |
| 9 | Contextual HUD: collapse left/right panels, clean-screen key | M |
| 10 | Settings: UI scale, reduce motion, shake, colorblind marks | M |
| 11 | Prompt v2: enter animation, can't-reason, shop-bell world-anchored | M |
| 12 | Surf score "wax sticker" displays that scale with trick size | M |
| 13 | Tooltips on upgrade cards and stats | M |
| 14 | Message log tab ("Mailbox") for missed toasts | M |
| 15 | Money wallet tide: wave fill that rises with daily goal | L |

### 1. Toast queue, dedupe and sound (S, files: `GameHud.cs` ShowToast, `Game.uss` `.toast*`)
- What: instead of `RemoveAt(0)`, give the oldest toast the `toast--leaving` class (0.25 s) before removing. If the same text is already showing, restart its timer and add a "x2" chip rather than a new pill. Keep a queue: show max 3, hold the rest and release when one leaves; `Bad` jumps the queue and lives 5.5 s (others 3.8 s; add about 40 ms per character above 40 characters, cap 6 s).
- Sound: `Info` = soft "bloop", `Good` = rising two notes, `Reward` = sparkle, `Bad` = short falling note. Use existing `Sound.Click/Buy/Mission/Error` as a start (pitch 0.9 / 1.1 / 1.0 / 0.8) and make new clips later in `tools/audio/make_sounds.py`.
- Feel: messages never vanish unread; spam turns into "Beach is full! x3".

### 2. Text size and contrast pass (S, `Game.uss`)
- Make nothing under 14 px; body at 16-18 px; key numbers 24+ px. Changes: `.level-next` 12 -> 15, `.build-beauty` 13 -> 15, `.surf-flow-label` 13 -> 15, `.xp-label` 14 -> 16, `.review-who` 14 -> 15, `.build-key` 14 -> 16, `.menu-footer` 14 -> 15, `.mission-story/.mission-hint/.card-requires/.card-stars-text/.report-fact` 15 -> 16-17. Widen `.level-next` `max-width: 210px` to 250.
- Colors: replace `#8FA3AB` and `#9AA5AB` with the existing `--muted` (`#5B6B73`, about 5:1 on cream, my calc) for anything under 24 px. A lighter grey like `#6F858E` is only about 3.6:1, so use it only on large text. For "done" objectives, a check mark or strike-through should carry the meaning, not faded text.
- Feel: reads clearly on a 1080p stream (compressed video mushes small text, see `sim-ui.md`).

### 3. Focus and card hover states (S, `Game.uss`)
- Catch: in UI Toolkit a mouse click also gives a Button focus, so a loud `:focus` style would stay stuck on a tab or button after every click. Fix one of two ways: (a) keep `:focus` subtle and different from hover, e.g. only `border-color: var(--sun)` (no scale), or (b) after a click call `button.Blur()` (register a `ClickEvent` once in `Sfx`/a small helper), and only show the strong style when the last input was keyboard/gamepad. I have not tested this in our Unity version; try it in the Editor.
- Add `:focus` to `.buy-button, .send-button, .close-button, .segment, .tab, .quality-button` with `border-color: var(--sun)` (USS has no outline property, so the border is the outline). Hover stays the lighter fill + scale 1.03.
- `.upgrade-card:hover { border-color: var(--ink-soft); translate: 0 -2px; }` with `transition-property: translate, border-color; 0.12s` (same recipe `.wave-card` already uses).
- `.perk-card:focus` same as hover.
- Feel: you always know what you are about to click; also first step to controller support.

### 4. UI sound family (S-M, `Sfx.cs` OnUiClick, `SoundBank`, `tools/audio/make_sounds.py`)
- Add `Sound.UiHover` (40 ms soft water "tick", volume 0.15 of click, pitch jitter +-4%), `UiBack` (falling, 120 ms), `UiToggle` (two-pitch click for on/off), `UiTab` (click at pitch 1.15), `UiOpen` / `UiClose` (soft "whoosh" 180 / 130 ms), `UiTick` (count-up tick, 25 ms, pitch rises with each step), `UiStamp` (thump, for #6).
- In `Sfx.Start`, register `PointerEnterEvent` on Buttons (use `TrickleDown`; play only when entering a new Button, with `minGap` 0.06 s). Make `OnUiClick` pick `UiBack` for buttons named `*Close`/`*Cancel`.
- Design rule from sources: hover near silent; confirm rises; back falls. Surf twist [G]: make all of them from one "water droplet bloop" so the UI sounds like a pool.

### 5. Day report reveal (M, `DayReportScreen.cs`, `Game.uss` `.report-*`)
- Rows slide in 12 px from the left + fade, 90 ms stagger, 220 ms each, ease-out; income numbers count up 0.5 s with `UiTick`; expense rows after income; then a **big profit pill** (Lilita 56 px, `--money` or `--coral`, scale 1.3 -> 1.0 over 350 ms ease-out-back, with `Sound.CashBig`). Review cards drop in last, tilted -1.5 deg each. Skip button/click fast-forwards everything.
- Replace the long totals text line with 3 chips: Income / Spent / Profit.
- Feel: the end of the day becomes the "closing ritual" CaseOh clips love (`caseoh-sims.md`).

### 6. Purchase stamp slam (S, `UpgradeScreen.cs`, `Game.uss` `.price-stamp`)
- On buy: `.price-stamp` starts at scale 2.2, opacity 0, rotate -20deg, animates to scale 1, rotate -8deg in 200 ms ease-in (hits hard), then card does `Bump` (scale 1.03, 100 ms) and plays `UiStamp`. The ink border flashes `--sun` for 300 ms.
- Catch: `BuildCards()` rebuilds all cards right after a buy (money changes), so remember the bought `id` in a field and add a `price-stamp--slam` class to that card's new stamp in the same rebuild (start state class for one frame, same trick as `UiMotion.Enter`).
- Feel: like a wax/rubber stamp on a surf-shop receipt. Very satisfying, cheap, unique.

### 7. Close animations and stagger (S, `UiMotion.cs`, `Game.uss`)
- Add `UiMotion.Hide(overlay)`: add `.screen-overlay--leave` (opacity 0, scale 0.96, translate 0 12px; 0.14 s ease-in = about 65% of the 0.22 s open) then `.hidden` after 150 ms. Call it where screens currently add `.hidden`.
- `UiMotion.Stagger(container)` helper: set `transition-delay` 40 ms x index (cap at 6) on `.upgrade-card` only when a tab is opened (`SelectTab`), not on the money-change rebuilds, or cards would re-animate every time you earn a coin.
- Feel: windows no longer blink out.

### 8. Surf-wave screen wipe (M, new `UiWipe` class, a `foam_edge.png` from `tools/ui/make_ui_art.py`)
- A full-screen element: left part solid `--ink`, right edge a tileable foam-curl PNG (wavy white + `--lagoon`). Transition `translate` from -110% to 0 in 350 ms, `ease-in-out`, hold 100 ms while the scene swaps, then out to +110% in 350 ms. Use between Day report -> next day, and main menu -> game. Reduce-motion = plain 150 ms fade.
- Feel: a wave washes across the screen. Unique to our theme, hides the load/reset pop.

### 9. Contextual HUD + clean-screen key (M, `GameHud.cs`, `Hud.uxml`, `Game.uss`)
- Left cards: show the mission card full-size for the first 8 s after it changes or while holding **Tab-hold** (peek); otherwise collapse to a 1-line chip (title + `[x/y]`, 44 px high). Challenge card same.
- Right Park panel: collapsed to money-relevant rows (bills, rental), others fade in with a 3 s "flash" when their value changes (`.stat-row--flash` background `rgba(255,200,87,0.35)`, 3 s transition).
- Note the HUD is already partly contextual (wave panel only near the machine, goal strip hides in menus, mission details collapse); this item finishes the job for the Park panel and left cards.
- F9 (or similar) = clean screen: hides all but crosshair/prompt/toasts. Save the choice.
- Feel: more beach on screen, still readable when it matters (PowerWash/BOTW approach, [S] sections 1.1 above).

### 10. Accessibility settings (M, `Settings.uxml`, `SettingsScreen.cs`, `GameSettings`)
- **UI scale** slider 80-150% (set `PanelSettings.scale` or the match slider; default 100%).
- **Reduce motion** toggle: sets a root class `.reduce-motion` with `* { transition-duration: 0s; }` style overrides for window/toast/banners (USS can target with a descendant selector), and turns off camera bob / the screen-shake you add later.
- **Colorblind marks**: patience bar gets 3 icon steps (face smile / neutral / frown instead of just green/yellow/coral); wave difficulty badges already have letters; upgrade states already have words.
- **Controls page** in Settings (key-cap list), **Subtitles** toggle for `Voices` lines [G].
- Feel: the game welcomes everyone, and Steam reviewers notice (Xbox guideline: scale to 200% of the base text).

### 11. Prompt v2 (M, `GameHud.cs` prompt, `.prompt*`, interactors)
- Enter: `.prompt-box` scale 0.85 -> 1, opacity 0 -> 1 in 120 ms ease-out-back; swap label with a 60 ms cross-fade.
- Key text from the action's real key name (not hard-coded "E"); hold actions show the key-cap plus a small ring drawn inside the key-cap, filled by `HoldRing`.
- "Can't" variant `.prompt-box--no`: sand background, `--muted` text, label like "Not enough money - $120" with a short shake (translate +-4 px, 2 cycles, 160 ms) and `UiBack`.
- Optional: for shops and the sign, show a small world-anchored bubble (like `missionMarker`) above the object when you are within 6 m, which fades when you look at it and the center prompt takes over.

### 12. Surf score as wax stickers (M, `SurfHud.cs`, `.surf-call`, `.surf-panel`)
- Trick call-outs become round "wax sticker" badges (a circle PNG with a rough edge, rotated -6 to +6 random) containing the trick name; size by trick value: small trick 30 px text, big trick 46, air/barrel 64 with 1.3 -> 1.0 pop (250 ms ease-out-back). Hold 1.2 s, fade 300 ms.
- Combo timer ring around the combo number instead of a 4 px bar; flow bar pulses (scale 1.0 -> 1.04, 400 ms loop) when `--hot`; at wipeout the combo number shatters (shakes 8 px, turns `--coral`, falls 40 px, fades in 400 ms) so losses are visible (THPS lesson: show what you lose).
- Feel: your session reads like a surf contest scorecard; stickers also look great in clips.

### 13. Tooltips (M, new `Tooltip` helper, `UpgradeScreen.cs`)
- Show after 350 ms hover on a card, stat row or chip; small ink pill, 16 px text, max width 320 px, appears above the cursor, never over the hovered thing. Content: effect in numbers ("+8% ride length"), cost, requirement. Use `UiOpen` at volume 0.3.

### 14. "Mailbox" log tab (M, `Notifications.cs`, `UpgradeScreen`)
- Keep last 30 notifications with the time; a tab shows them; the tab badge shows unread count. (Players asked for this in Supermarket Simulator, see `sim-ui.md`.)

### 15. Wallet tide (L, `GameHud.cs`, shader or a masked `VisualElement`)
- Behind the money number, a wave-shaped fill that rises as today's income approaches the daily goal and sloshes (translate x loop 4 s) when money comes in. Reaching the goal: wave overflows with foam. A [G] signature piece of HUD you will not find elsewhere; do last.

---------------------------------------------------------------------------

## Ideas for our game
(Roadmap in `docs/DESIGN.md`: Steps 1-4 are done; we are in **Step 5 "Polish & depth"**, and the open items are lessons area / celebrity visits / surf contests, real rigs, and the **Steam build**.)
- **Step 5 polish, one session (all small):** #1 toasts, #2 text/contrast, #3 focus/hover, #6 stamp slam, #7 close animation. They live in `Game.uss`, `UiMotion.cs`, `GameHud.cs`, `UpgradeScreen.cs`.
- **Step 5 sound follow-up (the "UI clicks" item is done, this is the next layer):** #4 UI sound family.
- **Step 5 "surf contests" item:** the judges' 0-10 card already exists; #12 (wax-sticker call-outs, scaled to trick size) plus #5 (results reveal) are the screen layer a contest day needs.
- **Step 5 day-end ritual (closing time + report already exist):** #5 report reveal, #8 wave wipe into the next day.
- **Before "Steam build":** #10 accessibility (UI scale, reduce motion, controls page), #2 text sizes, #3 focus/controller navigation, #9 clean-screen key for streamers.
- **After more panels arrive (lessons area, staff, shops):** #9 contextual HUD, #13 tooltips, #14 mailbox log.
- **Later / wow factor:** #15 wallet tide, day/night tinted UI (cream -> dusk purple, already suggested in `ui-powerwash.md`).
- **Identity rule to keep:** every new effect should feel like a surf-shop object (wax, stamp, sticker, foam, ticket), not a generic app animation.
- **Next research suggested:** hear/see the actual UI sounds of Animal Crossing, Stardew and PowerWash on video (I could not), and a pass on controller/Steam Deck needs.

## Sources
- [Wayline: UI immersion](https://www.wayline.io/blog/your-ui-is-killing-immersion-fix-it), [Stray Spark](https://www.strayspark.studio/blog/game-ui-ux-design-principles), [Uni Bonn HUD minimalism study](https://bonndoc.ulb.uni-bonn.de/xmlui/handle/20.500.11811/14207?show=full)
- [Xbox XAG 101 text display](https://devdocs.xbox.com/gaming/accessibility/xbox-accessibility-guidelines/101), [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/?p=3285), [IGDA GASIG](https://igda-gasig.org/?p=1387), [Xbox handheld](https://devdocs.xbox.com/build/gdk-and-engines/handheld/handheld-guidelines-and-testcases), [Roblox accessibility](https://create.roblox.com/docs/en-us/production/publishing/accessibility.md)
- [Motion design guide](https://git.secluded.site/agent-skills/blob/main/vendor/impeccable/skill/reference/motion-design.md), [GSAP easing](https://cdn.jsdelivr.net/npm/claude-code-skills@0.5.0/data/skills/gsap-animation-helper/reference/easing.md)
- [Persona 5 visual design](https://mechanicsofmagic.com/2022/04/23/visual-design-of-games-persona-5/), [Atlus menus](https://www.actugaming.net/les-menus-stylises-de-persona-et-metaphor-refantazio-sont-tres-beaux-mais-aussi-tres-penibles-a-concevoir-selon-atlus-678450/)
- [Balatro feedback](https://blakecrosley.com/guides/design/balatro), [Balatro Steam thread](https://steamcommunity.com/app/2379780/discussions/0/4201364375109001844/?ctp=2)
- [THPS guide](https://www.speedrun.com/thps1/guides/1wg8v), [THPS2 gameplay](https://strategywiki.org/wiki/Tony_Hawk%27s_Pro_Skater_2_(Game_Boy_Advance)/Gameplay)
- [Forza Horizon 4 analysis](https://mechanicsofmagic.com/?p=6144), [Riders Republic HUD complaint](https://steamcommunity.com/app/2290180/discussions/0/3817410454880410616)
- [Animal Crossing delight by design](https://uxdesign.cc/animal-crossing-delight-by-design-e2c0a1a4d462), [NookPhone](https://nookipedia.com/wiki/NookPhone), [Dave the Diver review](https://geardiary.com/2023/05/01/dave-the-diver-early-access-review-a-great-start-and-i-want-to-dive-deeper/)
- [Two Point Campus review](https://www.windowscentral.com/gaming/xbox/two-point-campus-xbox-review), [Planet Coaster 2 UI feedback](https://forums.frontier.co.uk/threads/feedback-after-a-few-days-the-ui-lack-of-rides-and-so-much-more-to-say.629756), [Xbox Tavern PC2](https://www.xboxtavern.com/planet-coaster-2-review/)
- [Hades II boon UI mod](https://thunderstore.io/c/hades-ii/p/SMarBe/Improved_Boon_Info_UI/v/1.2.0/), [Product Builder notification doc](https://productbuilder.net/ru/skills/notification-design), [Singapore toast guidance](https://designsystem.tech.gov.sg/components/toast), [bugnet UI sound](https://bugnet.io/blog/ui-sound-design-for-indie-games)
