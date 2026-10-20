# Surf Pool Sim

First-person wave pool manager sim (CaseOh-style, like Supermarket Simulator / TCG Card Shop Simulator).
Unity 6000.6.6f1, URP, single-player, Mac first (Windows/Steam later). Design + roadmap: `docs/DESIGN.md`.

## Working rules
- Ido is the game designer and playtester; Claude writes code and drives the Editor.
- Each milestone must end playable. One feature per session.
- Drive the live Editor (Unity MCP `unity-editor`, or `unity command` via `~/.unity/bin/unity`) — check `unity status` first. Never hand-edit `.unity` / `.prefab` / `.asset` YAML while an Editor is connected.
- If the Editor won't connect, check for Safe Mode (compile errors) with `unity pipeline list`, fix the C#, restart.
- Use placeholder gray primitives until the art style (low-poly cartoon vs. semi-realistic) is chosen.
- Fake the wave (animated mesh + Shader Graph, surfers follow a path) — no fluid simulation.
- Scripts live in `Assets/_Game/Scripts/`, scenes in `Assets/_Game/Scenes/`.
- Commit every `.cs`/asset together with its `.meta`. Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`.
- Explain things to Ido in plain words; Ido is learning.
