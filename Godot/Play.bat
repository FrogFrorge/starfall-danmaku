@echo off
rem Double-click to play the Godot version of the game (builds the C# first if needed).
if not defined GODOT set GODOT=%USERPROFILE%\Godot\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64.exe
"C:\Program Files\dotnet\dotnet.exe" build "%~dp0DanmakuSTG.csproj" -v q -nologo >nul
start "" "%GODOT%" --path "%~dp0."
