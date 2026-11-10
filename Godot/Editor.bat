@echo off
rem Double-click to open this project in the Godot (.NET) editor. Press F5 inside the editor to play.
if not defined GODOT set GODOT=%USERPROFILE%\Godot\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64.exe
start "" "%GODOT%" -e --path "%~dp0."
