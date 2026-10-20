# UI vibe: PowerWash Sim 2, sim-game UIs, and an art direction for Surf Pool
Date: 2026-10-20

Ido's goal: "a cool UI with the vibe we are looking for", not a copy. Today we have a simple HUD with plain cream cards, simple colors and one font.
So this file is: (1) honest notes on what I could and could not verify, (2) what makes polished stylized UIs feel like *a place*, (3) an art-direction brief for us, (4) gap list, (5) ranked changes.

## 0. What I could actually verify (read this first)
- The Game UI Database page for PowerWash Sim 2 (gameuidatabase.com id=2226) returned **403**. I could not open its screenshots, so I have **no pixel specs** (sizes, hex colors) from it. Anything below about PowerWash look is from reviews and is marked.
- interfaceingame.com pages load, but only as text catalogs (screen names and tags, images are lazy-loaded placeholders). So I got **which screens exist**, not how they look.
- Verified PowerWash facts (from articles):
  - HUD redesigned to be "cleaner and more minimal" so you watch the washing ([Tech-Gaming review](https://www.tech-gaming.com/powerwash-simulator-2/)).
  - Sequel UI "builds on the style" of game 1; a 2-person UI team (UI artist + senior UI artist) owns all UI/UX ([FuturLab UI artist blog](https://www.futurlab.co.uk/news/more-than-buttons-and-menus-thoughts-from-our-ui-artist)). That blog is generic; it stresses "fast and easy beats pretty but tedious".
  - Everything job-related lives on an in-game **tablet** (objectives, map, shop, settings), opened with Tab/Start; the controls list is in [Ludo.guide](https://www.ludo.guide/guide/powerwash-simulator/getting-started/understanding-the-ui). Nozzle = mouse wheel, pressure = Q/E.
  - Objective list is checklist style; in game 2 a **reticle floats over the last few dirty spots** and an orange-icon "dirt finder" can be turned off ([Nintendo Wire](https://nintendowire.com/reviews/powerwash-simulator-2-for-switch-2/), [Tech-Gaming](https://www.tech-gaming.com/powerwash-simulator-2/)).
  - Each finished task plays a "ding" and pays a little money ([Punished Backlog](https://punishedbacklog.com/powerwash-simulator-2-review/)). Music/ambience change as an area nears 100% ([Tech-Gaming](https://www.tech-gaming.com/powerwash-simulator-2/)).
  - Story pop-ups appear at the side of the screen during jobs; one reviewer found them distracting ([Gaming Trend](https://gamingtrend.com/reviews/powerwash-simulator-2-review-another-clean-sweep/)).
- Interface In Game screen lists (what screens good sims bother to design): Animal Crossing New Horizons ([list](https://interfaceingame.com/games/animal-crossing-new-horizons/)): Start screen, Menu, Inventory, Shop, Passport, Achievements, "You got Nook Miles", Postcard, Loader. Stardew Valley ([list](https://interfaceingame.com/games/stardew-valley/)): Level up, Skill level, Calendar, Journal, Letter, Store, Inventory full. Overcooked 2 ([list](https://interfaceingame.com/games/overcooked-2/)): Ready, Time's up, Score, New recipe, Paused, Loading level. Against the Storm ([list](https://interfaceingame.com/games/against-the-storm/)): 5 HUD variants, Hint, Level Up, Confirmation, Alerts. The Sims 4 ([list](https://interfaceingame.com/games/the-sims-4/)): Buy mode, Select place, Lessons.
- **Lesson from those lists (verified as a fact, the takeaway is mine):** these games design the *small* screens too: level-up, "you got X", end-of-round score, loading, letters/postcards. That is where personality lives. We have a day report and a story letter but nothing for level-up, purchase, or loading.

## 1. How polished stylized UIs get a "vibe" (general craft, my summary from experience of these games; not sourced screenshots)
| Trick | What it looks like | Why it works |
|---|---|---|
| Layered panels | Outer frame, inner "paper" panel, small header tab sticking out on top | Depth, feels like an object, not a rectangle |
| Hard offset shadow | Darker copy of the shape 4-6 px lower (no blur) | Chunky, toy-like, reads at stream quality |
| Thick outline + inner highlight | Dark 3 px outline, 1-2 px light line along the top edge | Looks "inflated" and tappable |
| Texture / pattern | Subtle tiled pattern (wood grain, paper grain, wave lines, dots) at 5-10% opacity | Kills the flat "web page" feel |
| Stickers and badges | Tilted (-3 to -6 deg) rounded tags: "NEW", "SALE", level medal, ribbon | Personality + tells the eye what changed |
| Characterful type pairing | Chunky display font for numbers/titles + friendly rounded body font | Display font = brand; body font = readability |
| Icon language | One style everywhere: same stroke width, same corner radius, filled glyph inside a colored circle/rounded square | Consistency is 80% of "premium" |
| Juice | Buttons squash on press, numbers pop and tick up, panels slide + overshoot, sparkles on reward | UI feels alive and rewarding (very CaseOh-clip-friendly) |
| Color roles | 1 base neutral, 1 brand color, 1 reward color, 1 danger color; nothing else | Keeps it calm; reward color only ever means money/good |

## 2. Art direction brief: "Sunset Surf Shop"
Idea in one line: **the UI is the surf shop's counter and the poster wall** - painted wood, sun-faded paper, wax stamps, retro surf-contest posters. Not a phone app.

### Palette (extends what we already use; existing values kept)
| Role | Hex | Use |
|---|---|---|
| Deep sea ink | `#0B2E3A` | Text, outlines, hard shadows (already ours) |
| Ocean teal | `#14B8A6` | Primary buttons, selected tabs (ours) |
| Lagoon light | `#7EE0D2` | Highlights, hover, progress shine (new) |
| Sunset coral | `#FF6B57` | Send/danger/sale stickers (ours) |
| Sunset orange | `#FF9A3D` | Poster banners, events, warm accents (new) |
| Sun yellow | `#FFC857` | Money chips, stars, XP (ours) |
| Cream paper | `#FFF7EC` | Panel face (ours) |
| Sand | `#EBD9BF` | Panel borders, paper edge (ours) |
| Driftwood | `#B07A4A` | Wood frames, header strips (new; replaces plain sand borders on big windows) |
| Dark wood | `#6B4428` | Wood shadow side, outlines of wood parts (new) |
| Dusk purple | `#5B3A8C` | Night / evening bar, "pro" accent, background tint behind windows (new, small doses) |
Rule: reward color (yellow/green) = money and good only; coral = bad/send only; wood and cream = structure.
Backdrop behind full-screen windows: change the flat `rgba(11,46,58,0.45)` to a dusk gradient image (teal-navy at top fading to purple-coral at bottom, 30-50% alpha).

### Fonts (all free, OFL = free for commercial use; I know these from Google Fonts but did not open the pages, so check each license on the page before importing)
| Pairing | Display (titles, big money, logo) | Body (labels, prose) | Vibe |
|---|---|---|---|
| A (safest) | [Lilita One](https://fonts.google.com/specimen/Lilita+One) | Fredoka (already in project) | Chunky cartoon + friendly; closest to CaseOh-sim look |
| B (retro surf poster) | [Chango](https://fonts.google.com/specimen/Chango) or [Bowlby One](https://fonts.google.com/specimen/Bowlby+One) | [Nunito](https://fonts.google.com/specimen/Nunito) | Fat 70s surf-contest lettering; use only for logo and big banners |
| C (script accent) | [Pacifico](https://fonts.google.com/specimen/Pacifico) for tiny taglines ("wave park tycoon", "Day 3") | Fredoka | Hand-painted sign feel; never for numbers |
Recommendation: A for everything, plus Pacifico only on the logo tagline and report headline. Number-heavy places (money, prices) use the display font because digits in Lilita/Chango are chunky and readable.
Unity note: UI Toolkit needs a Font Asset (SDF) per font; same process we used for `Fredoka SDF.asset` (Font Asset Creator). Make one per family, set `-unity-font-definition` per class.

### Shape language
- Big corner radius (18-28 px on windows, 12-16 on cards, full pill = height/2 on chips and buttons).
- Thick 3-4 px outlines in deep sea ink or dark wood on *interactive* things; thin 2 px sand lines on passive things. This one rule tells players what is clickable.
- Hard offset shadow: fake it with `border-bottom-width: 6px` in a darker color on buttons/cards (USS has no box-shadow).
- Slight tilt (rotate -2 to -5 deg) on badges and stickers only; panels stay straight.
- Wave motif: a wavy divider image (tileable) instead of `.divider` line; scalloped/wavy bottom edge on the logo panel.

### Textures and motifs (all made as small PNGs by a Python/Blender script like `tools/ui/make_icons.py`)
- Paper grain tile 128x128 (cream with 3% noise), tiled with `background-repeat` on `.card`/`.screen-window`.
- Wood plank strip, 9-sliced, for window title bars and the tab bar.
- Wax-stamp seal (circle with a wave and rough edge) for "Owned", "Level N", "Completed".
- Rope border or stitched dashed line for the mission card; sun-ray burst behind big reward numbers; palm-leaf corner flourish on the main menu.
- Retro surf poster for the main menu: big sun disc + three wave stripes in teal/coral/yellow behind the panel (one illustrated PNG, or the real 3D scene with a tinted vignette).

### Motion (USS `transition` + C# scheduling; all realistic)
- Panels open: scale 0.92 -> 1.0 + opacity, 0.18 s ease-out; closing 0.12 s.
- Buttons: hover `scale: 1.04`; press `scale: 0.96` (USS `:active`); 0.08-0.12 s.
- Money: label punch-scale to 1.15 and back on change; ticking count-up (C#, 0.4 s).
- Level up / purchase / mission complete: a banner that drops in, overshoots, holds 1.5 s, leaves (like our `mission-banner`, but with a sticker + sound). Sparkles = 6-10 small star `VisualElement`s flung outward by C# with transitions.
- Tab change: selected pill slides? (hard in USS) - cheaper: new tab pops scale 1.08 -> 1.0.

### What USS can and cannot do (Unity 6, so I plan around it)
- Can: rounded corners, borders per side, `background-image` with 9-slice (`-unity-slice-left/right/top/bottom`), `background-repeat` tiling, tint, `opacity`, `translate`/`rotate`/`scale`, `transition-*`, `letter-spacing`, text outline, `text-shadow`, vector images (SVG package).
- Cannot: CSS gradients, box-shadow, blur. Use small gradient PNGs (stretched, or 9-sliced) and offset darker sibling/bottom border for shadow.
- (Based on what I know of Unity 6 UI Toolkit; verify any single property in the editor before relying on it.)

## 3. PowerWash-style structure worth borrowing (clarity, not look)
- HUD is minimal during action; the heavy info (jobs, map, shop, settings) sits behind one **tablet** key. We already do this with SurfOS; our HUD is the opposite: three big cards always on.
- Pinned objective as a short checklist with a **world reticle** on the target (we have `missionMarker` diamond; good).
- Tiny reward ding per checklist line.
- Controls reference lives in the menu, with a short hint line in game that fades.
- Keep this and drop the rest: the art style should be ours.

## 4. Gap list vs our current UI
| Area | Today (file / element) | Gap |
|---|---|---|
| Global look | `Game.uss` `.card`: flat cream + 2 px sand border + 18 px radius; every panel looks the same | No layering, no texture, no shadow, no variety between HUD/shop/menu |
| Fonts | One font (Fredoka SDF) in bold/regular only | No display face; numbers and titles have no character |
| Money | `Hud.uxml` `moneyLabel` 52 px green text inside generic card | No coin icon, no pop/tick animation on the label (only the `popup` stack), no "wallet" shape |
| HUD density | `Hud.uxml` `top-left` card (money, level, XP, rating, income, day, event) + `top-right` card (wave + 7 stat rows) + goal strip + challenge + mission cards | Too many always-visible rows; 2 big cards cover the beach; stat rows are plain text lines |
| Crosshair/prompt | `.crosshair`, `.prompt-box`, `.key-badge` | Good base; badge is flat; no hold-progress ring, no press animation |
| Hint line | `.hint` white text bottom-left, fades | Plain text; no key-cap look, no controls screen |
| Toasts | `.toast` cream box + colored left bar | One style for good/bad/info; no icon, no slide-in |
| Windows | `.screen-window` 1100x780 with teal top border (`UpgradeScreen.uxml`, `Settings.uxml`, `DayReport.uxml`, `Story.uxml`, `Pause.uxml`) | Same box everywhere; no title banner/ribbon, no close X, no illustration |
| Shop cards | `.upgrade-card` white box + colored left border + `.cost-chip` + `.buy-button` | No big icon/illustration per upgrade; price is a small chip; no "NEW"/"best value" stickers; owned = just faded; no level pips as images |
| Tabs | `.tab` / `.tab--active` plain pills | No icons, no counts/badges ("2 affordable") |
| Main menu | `MainMenu.uxml` panel with 70 px logo text + teal line | Logo is plain text; background is just the scene; no art, no animation |
| Settings | `Settings.uxml` 3 stacked `report-column` boxes | No tabs, no controls/keybinds reference, no UI scale option |
| Icons | `Icons/*.png` single-tint monochrome 16-22 px | Tiny and one-color; no colored coin/medal/star set |
| Motion | `transition-duration` on a few things | No open/close animations, button press, count-up, level-up fanfare |
| Missing small screens | none | No level-up popup, no purchase "stamp", no loading card, no "new unlock" banner |

## 5. Ranked changes (each is one build step)
1. **Design tokens + two new fonts.** In `Game.uss` add a `:root`-style comment block of colors (done as USS variables `--c-ink`, `--c-teal` etc., supported in Unity 6) and import Lilita One as `Lilita SDF.asset`. Switch `.money-label`, `.screen-title`, `.menu-logo`, `.mission-title`, `.cost-chip`, `.wave-type` to it. Biggest vibe gain for lowest effort.
2. **Chunky panel kit.** New classes `.panel` (cream, 4 px ink-ish outline, 6 px darker `border-bottom`, 24 px radius) and `.panel-header` (driftwood 9-slice strip with title). Apply to `.card`, `.screen-window`, `.menu-panel`. Add paper-grain tile as `background-image` + `background-repeat`.
3. **Pill buttons that squash.** Rework `.buy-button`, `.send-button`, `.close-button`, `.segment`: 3 px outline, thick bottom edge, hover `scale: 1.04`, `:active` `scale: 0.96` and bottom edge shrinks 6 -> 2 px (looks pressed). Pure USS.
4. **Slim the HUD.** Move `top-right` stats rows (`rentalRow`, `servedRow`, `lostRow`, `trashRow`, `billsRow`, `wagesRow`) into a collapsed "Park" chip that expands on hold-Tab or when the value changes (flash 3 s). Keep money + clock + goal always. Controller: `GameHud.cs` toggles `.hidden`.
5. **Money wallet widget.** Coin icon (colored PNG) + display-font number in a yellow/cream pill with hard shadow; label punch-scales on change and counts up (C# in `GameHud.cs`); keep the green/red `popup`. Replace the `income-rate` line with a small chip under it.
6. **Shop card v2.** Per card: large colored icon tile (48 px rounded square, color by category, you already have the category colors), title in display font, level pips as small circles, price as a yellow pill that sits on the card's top-right corner and tilts -3 deg; "NEW" sticker when just unlocked; "OWNED" wax-stamp image replaces the faded text; unaffordable = price pill turns coral. Files: `UpgradeScreen.cs` builders + `Game.uss` `.upgrade-card*`.
7. **Windows open with life + header ribbon.** Add title banner (ribbon PNG 9-sliced, wave-colored) above `.screen-title`, a round X close button, and open/close scale+fade transition (class toggle `.screen-window--open`). Applies to all `screen-overlay` screens at once. Backdrop becomes a dusk gradient PNG.
8. **Main menu poster.** Replace plain text logo with display-font logo + wavy underline PNG + Pacifico tagline; add sun/waves illustrated PNG behind (`MainMenu.uxml` `menuRoot` background); buttons slide in staggered (40 ms apart) on start; palm corner flourish.
9. **Juice screens: level-up + purchase stamp + mission complete.** One reusable "banner" prefab in `Hud.uxml` (sticker, big title, sparkles, sound). Trigger from `ParkLevel`, `UpgradeScreen` buy, `MissionManager`. We already have `missionBanner` to extend.
10. **Toasts with icons and kinds.** `Notifications.Post` gets an optional kind (info/good/bad/unlock); toast gets a round icon, slides in from the top (translate), stacks max 3. Pure `GameHud.cs` + USS.
11. **Key-cap hint bar + Controls page.** Replace `.hint` text with a row of key-cap chips (`[WASD] Move [E] Use [Tab] Hints`) that fade after 20 s; add a "Controls" tab in `Settings.uxml` listing keys (PowerWash puts this in the tablet/menu).
12. **Icon set v2.** Extend `tools/ui/make_icons.py` to output 64 px colored icons (coin, medal, board, wave, wrench, broom, sun, star) with a dark outline and white inner highlight, one shared style. Used by items 5, 6, 10. Do last because others work with the old icons first.

Suggested build order: 1 -> 2 -> 3 (whole game changes look in three small steps), then 7, 6, 5, 4, then 8, 9, 10, 11, 12.

## Ideas for our game
- **Step 4+ (UI polish, now):** items 1-3 first; they touch only `Game.uss` and the font asset, so they are low risk and show instantly in every screen.
- **Step 3 money loop tie-in:** wallet widget (5) with count-up and coin sound makes earning feel good on stream; reward color = money only.
- **Step 4+ growth:** shop card v2 (6) and level-up/purchase banners (9) make each upgrade a "moment" that CaseOh-style clips love.
- **Later:** day/night-tinted UI (cream -> dusk purple panels in the evening), unlockable UI themes/stickers as a cosmetic reward, UI scale slider, colorblind-safe mode (add shape/icon to good/bad, not only color).
- **Next research (suggested):** screenshot-based pass of 3 sim UIs (Animal Crossing shop, Stardew level-up, Overcooked score screen) once images can be viewed, since I could not see actual pixels from the sites.
