# 星墜彈幕 — Godot 4.6 (C#) version

A port of the Unreal project one folder up. Gameplay, balance, art, audio, text and tools are the same; the C# is a
near line-for-line translation of the C++ (see `PORTING.md` for the rules and which file came from which .cpp).

## Open / run
- Editor: Godot 4.6.1 .NET (`Godot_v4.6.1-stable_mono_win64.exe`). `Play.bat` and `Editor.bat` look for it in `%USERPROFILE%\Godot\Godot_v4.6.1-stable_mono_win64\`; set the `GODOT` environment variable to use another path.
  → Import → pick this folder's `project.godot`. Press F5 to play.
- Build the C# from the command line (the editor also builds on play):
  `"C:\Program Files\dotnet\dotnet.exe" build DanmakuSTG.csproj`

## Command-line flags
All the UE flags work the same, written after `--`:
```
Godot_v4.6.1-stable_mono_win64_console.exe --path . -- -DanmakuDemo -DanmakuSpell=62 -DanmakuShotEvery=300 -DanmakuShots=1
```
Godot-only test flags: `-DanmakuMusicRoom` (opens the music room; `-DanmakuSpell=N` picks track N), `-DanmakuOptions`,
`-DanmakuPauseAt=N` (opens the pause menu at frame N), `-DanmakuBotNoSpellBomb` (the bot never bombs during a spell card).

Screenshots go to `Saved/DemoShots/`, bot results to `Saved/BotResult_<tag>.txt` (`Saved/` is ignored by Godot).

Headless bot run (no window): add `--headless` before `--`, e.g.
```
Godot_v4.6.1-stable_mono_win64_console.exe --headless --path . -- -DanmakuDemo -DanmakuBot -DanmakuTurbo=60 -DanmakuDiff=3 -DanmakuBotTag=lunatic
```

Recording a video: Godot's Movie Maker writes every frame at exactly 60 fps, no screen capture needed:
```
Godot_v4.6.1-stable_mono_win64_console.exe --path . --write-movie Saved/run.avi --fixed-fps 60 -- -DanmakuDemo -DanmakuBot -DanmakuDiff=3
```

## Files
- `Scripts/Ue.cs` — UE-style helpers (FMath, FRandomStream, vector helpers) so the ported code reads like the C++
- `Scripts/Types.cs`, `World.cs` — data types and the world's fields (DanmakuTypes.h, DanmakuWorld.h)
- `Scripts/WorldCore.cs` — DanmakuWorld.cpp (flow, player, boss, stage 1)
- `Scripts/Stage2..6.cs`, `StageEx.cs`, `StageIf.cs`, `Behaviors.cs`, `Roads.cs`, `Bosses.cs`, `Bot.cs`
- `Scripts/Hud.cs` + `HudCanvas.cs` — all drawing; `Audio.cs` — music and sound effects; `Main.cs` — the scene node

## Building the app (Windows)
```
Godot_v4.6.1-stable_mono_win64_console.exe --headless --path . --export-release "Windows Desktop" Build/StarfallDanmaku/StarfallDanmaku.exe
```
Then copy `Music/` next to the exe (the game reads your songs from there). Needs the 4.6.1 mono export templates installed.
Unlocks (spell practice, music room) are saved in `user://progress.cfg`; `-DanmakuLocked` shows the menus as a new player sees them.
