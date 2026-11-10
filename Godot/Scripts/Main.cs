// The one node in the scene. It owns the simulation (DanmakuWorld), the HUD and the audio, ticks them every frame,
// and gives the simulation what UE's engine used to: the command line, quitting, and screenshots.
//
// Command-line flags work like in the UE build, e.g.
//   Godot_v4.6.1-stable_mono_win64.exe --path Godot -- -DanmakuDemo -DanmakuStage=7 -DanmakuShotEvery=300 -DanmakuShots=2
// (flags may also be given before "--"). Headless test runs: add --headless and -DanmakuTurbo=60.
using Godot;

namespace DanmakuSTG;

public partial class Main : Node2D
{
	public DanmakuWorld World;
	public DanmakuHud Hud;
	public DanmakuAudio Audio;
	string Cmd = "";
	string PendingShot;

	public override void _Ready()
	{
		Cmd = string.Join(" ", OS.GetCmdlineArgs()) + " " + string.Join(" ", OS.GetCmdlineUserArgs());
		World = new DanmakuWorld { Host = this };
		Hud = new DanmakuHud(this, World);
		Audio = new DanmakuAudio(this, World);
		World.BeginPlay();
		RenderingServer.FramePostDraw += OnFramePostDraw;
	}

	public override void _Process(double Delta)
	{
		World.Tick((float)Delta);
		Hud.Draw();
		Audio.Tick();
	}

	public override void _ExitTree()
	{
		RenderingServer.FramePostDraw -= OnFramePostDraw;
	}

	/** The full command line (engine args and user args after "--"). */
	public string GetCommandLine() => Cmd;

	public void RequestExit() => GetTree().Quit();

	/** Saves the next rendered frame as a PNG at an absolute path (UE FScreenshotRequest). */
	public void RequestScreenshot(string Path) => PendingShot = Path;

	void OnFramePostDraw()
	{
		if (PendingShot == null) return;
		var Path = PendingShot;
		PendingShot = null;
		var Img = GetViewport().GetTexture().GetImage();
		if (Img == null) return;
		System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
		Img.SavePng(Path);
	}
}
