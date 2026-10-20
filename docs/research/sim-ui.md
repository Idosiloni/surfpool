# UI in first-person job sims
Date: 2026-10-20

Games looked at: Supermarket Simulator, TCG Card Shop Simulator, Gas Station Simulator, Schedule I, PowerWash Simulator.
Honest note: only the complaints section is backed by a source I could open (Steam forums). The layout patterns come from general knowledge of these games, so treat them as **my summary, not verified specs**.

## 1. HUD layout (screen UI)
| Spot | What lives there | Notes |
|---|---|---|
| Top-left or top-right | Money (big number), sometimes level/XP | Money is the most important number; number ticks up when you earn (little "+$12" pop) |
| Top-center or corner | Day + clock, store open/closed | Clock tells you how long until closing/next event |
| Center | Tiny dot or ring crosshair | Changes shape or color when you can interact |
| Center-low | Interaction prompt, e.g. `[E] Pick up` | Appears only when looking at something usable |
| Bottom-left/right | Held item, hotkey hints (`[F] Flashlight`) | Hints often fade after you learn them |
| Side | Short task list / "today's goals" | Helps new players know what to do next |
- Keep the middle of the screen clear: first-person games need to see the world.
- Money and time are always visible. Everything else shows only when relevant.

## 2. Interaction prompts
- Aim at object -> crosshair changes + a label shows the verb and key ("Open", "Pick up", "Use register").
- Different verbs for hold vs tap (hold `E` to fill a bar for slow actions like restocking; tap for quick ones).
- Object highlight (outline or glow) is a strong second signal; low-poly games do this cheaply with an outline shader.
- Show "can't" reasons: "Hands full", "Not enough money", greyed prompt instead of nothing.
- Mouse-wheel/number keys to switch held items; show the held item name.

## 3. In-world computer / tablet screens
This is the heart of the genre: a physical monitor you walk up to, press `E`, camera locks onto the screen, cursor appears.
- **Tabs** along the top or left: Market / Upgrades / Staff / Stats / Settings (names vary by game).
- **Cards in a grid**: icon, name, price, small "+/-" quantity buttons, **Add to cart**.
- **Cart** on the right: line items, total, big **Checkout** button; money shown at top so you can see if you can afford it.
- **Upgrades** as cards with locked/unlocked/bought states, price, one-line effect, and a level pip row (e.g. 2/5).
- Filter/search and "sort by price" appear as the item list grows.
- Esc / right-click to leave the screen.

## 4. Customer feedback
- **Speech/thought bubbles** above the head with an icon, not words (coin = ready to pay, hourglass = waiting, "?" = can't find item).
- **Mood icons**: happy / neutral / angry face; often a color change (green -> yellow -> red) as patience drains.
- **Patience bar** or ring over the head only once they are waiting.
- Short sound cue when someone is ready to pay or is angry.
- Floating text on reward: "+$25", star pops.

## 5. Notifications
- Small toasts in a corner that stack and fade ("Order delivered", "New customer!").
- Critical ones (store closing, delivery arrived) get an icon + sound.
- A message log on the computer for things you missed (players ask for this, see complaints).

## 6. Streamer / readability
(My guess from how these games get streamed.)
- Big, high-contrast fonts; 1080p video gets compressed, so small text turns to mush.
- Money pops and big icons make good "moments" for chat.
- Keep important info off the extreme screen edges (some overlays/webcam boxes cover them).
- Bright, saturated colors and clear icons read well on stream; low-poly cartoon style helps.
- Short on-screen key hints save the streamer from explaining controls.

## 7. Common UI complaints (source: Steam discussions for Supermarket Simulator, mostly 2024)
- Reordering stock every day is tedious; players want auto-reorder with limits. ([thread](https://steamcommunity.com/app/2670630/discussions/0/4289187621811215457))
- Order cap (~10 items) feels slow as the shop grows. ([thread](https://steamcommunity.com/app/2670630/discussions/0/4431066216145353176))
- Walk back to the computer every time; players ask for a portable tablet tier. ([thread](https://steamcommunity.com/app/2670630/discussions/1/4523386962649812780))
- Small text on in-game monitors; settings need scrolling. 
- Missing events while on the computer (complaints, price changes); wish for a log/hotkey.
(Search summary; I did not find official dev responses.)

## 8. Doing it in Unity 6 with UI Toolkit
**What UI Toolkit is:** Unity's newer UI system. You write layout in UXML (like HTML), style in USS (like CSS), logic in C#.
- Screen HUD: a `UIDocument` with a Panel Settings asset set to Screen Space. Use `Label` for money, `VisualElement` for crosshair/prompt. Update text from C# events (don't poll every frame).
- Make the HUD scale: use a Scale With Screen Size reference resolution (e.g. 1920x1080) in Panel Settings.
- World-space UI: Unity 6.2+ supports World Space render mode on Panel Settings, so you can place a UI plane on a monitor mesh in the scene; Pixels Per Unit (default 100) controls sharpness vs size. See the manual: [Create world space UI](https://docs.unity3d.com/Manual/ui-systems/create-world-space-ui.html) and Unity's guide [World Space UI with UI Toolkit in 6.2](https://unity.com/en/resources/how-to-create-world-space-ui-toolkit).
- Check the Unity version the project actually uses before relying on World Space; if older, fallback is rendering the UI Document to a RenderTexture and putting that on the monitor material (my suggestion, unverified for this project).
- Input: when the computer screen is open, unlock the cursor and send pointer events to the panel; disable player look/move.
- Customer bubbles: world-space UI or simple sprites/TextMeshPro billboards (cheaper for many NPCs than a UI Toolkit panel each). My suggestion.
- Docs hub: [UI Toolkit manual](https://docs.unity3d.com/Manual/UIElements.html).

## Ideas for our game
- **Step 4 "Better UI" (next UI step):**
  - HUD: money top-left with "+$" pop, clock/day top-right, dot crosshair that grows on interactable, prompt under it (`[E] Sell ticket`).
  - Upgrade computer: tabs **Upgrades / Waves / Rentals / Stats**; card grid with icon, price, level pips; cart is optional (upgrades are buy-now, a cart fits future "order boards/wetsuits").
  - Wave control panel: big buttons for Beginner / Intermediate / Pro with colors matching difficulty; cooldown ring.
- **Step 2-3 polish (cheap):** surfer patience ring + mood face above head; coin icon when ready to pay.
- **Avoid known complaints:** add a message log tab; make text large; plan an auto-restock/auto-reorder upgrade for rental boards; a "tablet" upgrade so you don't walk back to the desk.
- **Later:** streamer-friendly "Show hints" toggle, UI scale slider.
