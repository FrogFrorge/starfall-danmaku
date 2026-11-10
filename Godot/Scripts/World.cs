// Port of DanmakuWorld.h: the whole STG simulation (player, bullets, enemies, boss, items, score).
// Pure 2D math stepped at a fixed 60 Hz; DanmakuHud reads this state and draws it.
// The method bodies live in the other partial files, one per UE .cpp:
//   WorldCore.cs (DanmakuWorld.cpp), Behaviors.cs, Roads.cs, Bosses.cs, Stage2..6.cs, StageEx.cs, StageIf.cs, Bot.cs
// Everything is public (the UE private section included) so the HUD and tools can read it.
using Godot;

namespace DanmakuSTG;

public struct FDialogueLine
{
	public int Speaker;        // 0 = player, 1 = boss, 2..5 = the four beasts (Extra-IF relay)
	public string Text;
	public FDialogueLine(int Speaker, string Text) { this.Speaker = Speaker; this.Text = Text; }
}

public struct FDanmakuInput
{
	public FVector2f Move;
	public bool bFocus;
	public bool bShoot;
}

public delegate void FPatternFn(DanmakuWorld W, int T);

public sealed class FPhaseDef
{
	public float HP;
	public int Seconds;
	public bool bSpell;
	public string Name;
	public FPatternFn Pattern;
	public bool bSurvival = false;   // endurance card: boss can't be hurt, survive the timer
	public int MinDifficulty = 0;    // phase is skipped below this difficulty
	public int Actor = -1;           // Extra-IF relay: which beast fights this phase (4 = all four)
	public bool bTimerAtThird = false; // the timer stays frozen until the boss is down to 1/3 of its HP
	public FPhaseDef(float HP, int Seconds, bool bSpell, string Name, FPatternFn Pattern, bool bSurvival = false, int MinDifficulty = 0, int Actor = -1)
	{
		this.HP = HP; this.Seconds = Seconds; this.bSpell = bSpell; this.Name = Name; this.Pattern = Pattern;
		this.bSurvival = bSurvival; this.MinDifficulty = MinDifficulty; this.Actor = Actor;
	}
}

public sealed class FBossDef
{
	public string Name = "";
	public string Title = "";
	public string StageTitle = "";
	public string StageSub = "";
	public List<FPhaseDef> Phases = new();
	public List<FDialogueLine>[] Dialogue = { new(), new(), new(), new() };   // one conversation per player character (ECharacter)
	public FBossDef Midboss = null;   // optional mid-stage boss (Extra)
	public int ArtIndex = -1;         // whose art to use (-1 = this stage's)
	public List<FDialogueLine>[] Ending = { new(), new(), new(), new() };     // optional conversation after the boss is beaten
	public FBossDef[] CharMidboss = { null, null, null, null };               // midboss that depends on the player character
}

public struct FSpellEntry
{
	public int Stage; public bool bMid; public FBossDef Def; public int Phase;
	public FSpellEntry(int Stage, bool bMid, FBossDef Def, int Phase) { this.Stage = Stage; this.bMid = bMid; this.Def = Def; this.Phase = Phase; }
}

/** UE FParse on the joined command line ("-DanmakuDemo -DanmakuSkip=300 ..."). */
public static class FParse
{
	static IEnumerable<string> Tokens(string Cmd) => Cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
	static string Strip(string T) => T.TrimStart('-');
	public static bool Param(string Cmd, string Name)
	{
		foreach (var T in Tokens(Cmd)) if (string.Equals(Strip(T), Name, StringComparison.OrdinalIgnoreCase)) return true;
		return false;
	}
	static bool Raw(string Cmd, string Key, out string Val)
	{
		foreach (var T in Tokens(Cmd))
		{
			var S = Strip(T);
			if (S.StartsWith(Key, StringComparison.OrdinalIgnoreCase)) { Val = S.Substring(Key.Length).Trim('"'); return true; }
		}
		Val = null;
		return false;
	}
	/** Key includes the '=' like UE ("DanmakuSkip="). Leaves Value unchanged when missing, like UE. */
	public static bool Value(string Cmd, string Key, ref int Value)
	{
		if (Raw(Cmd, Key, out var V) && int.TryParse(V, out var I)) { Value = I; return true; }
		return false;
	}
	public static bool Value(string Cmd, string Key, ref float Value)
	{
		if (Raw(Cmd, Key, out var V) && float.TryParse(V, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var F)) { Value = F; return true; }
		return false;
	}
	public static bool Value(string Cmd, string Key, ref string Value)
	{
		if (Raw(Cmd, Key, out var V)) { Value = V; return true; }
		return false;
	}
}

/** Where tools write their output (UE: FPaths::ProjectSavedDir()). res://Saved has a .gdignore. */
public static class Paths
{
	public static string ProjectSavedDir()
	{
		string Dir = OS.HasFeature("editor") || !OS.HasFeature("template")
			? ProjectSettings.GlobalizePath("res://Saved/")
			: OS.GetExecutablePath().GetBaseDir() + "/Saved/";
		System.IO.Directory.CreateDirectory(Dir);
		return Dir;
	}
	public static void SaveString(string Text, string Path) => System.IO.File.WriteAllText(Path, Text, new System.Text.UTF8Encoding(false));
}

public partial class DanmakuWorld
{
	/** The Godot node that owns this simulation: input, screenshots and quitting go through it. */
	public Main Host;

	// ---------- State read by the HUD ----------
	public EDanmakuState State = EDanmakuState.Title;
	public bool bPaused = false;
	public int Frame = 0;
	public int StateTimer = 0;
	public int StageFrame = 0;
	public int SelectedCharacter = 0;   // cursor on the select screen
	public int Difficulty = 1;          // 0 Easy .. 3 Lunatic, 4 Extra

	// Run / stage flow
	public int StageIndex = 0;          // 0..NumStages-1 main stages, ExtraStage for Extra
	public bool bExtra = false;
	public bool bPractice = false;
	public int TitleCursor = 0;          // 0 start, 1 Extra, 2 Extra-IF, 3 stage practice, 4 spell practice, 5 music room, 6 options, 7 language
	public const int TitleItems = 8;
	public int MusicCursor = 0;          // music room: highlighted track (index into MusicTracks)
	public int MusicPlaying = -1;        // music room: track id being played (-1 = the title music)
	public int OptionCursor = 0;         // options: 0 music volume, 1 sound volume, 2 back
	public int PauseCursor = 0;
	public int EndingPage = 0;          // ending scene: page shown (Story.Endings; one extra page for "the end")          // pause menu: 0 resume, 1 restart, 2 back to title
	public int PracticeStage = 0;
	// spell practice: pick one card from any boss, fight only that card
	public bool bSpellPractice = false;
	public int SpellCursor = 0;
	/** Character traits */
	public float HitRadius() => PlayerHitRadius * (Player.Character == ECharacter.Ren ? 0.8f : 1f);
	public int BombPiecesNeeded() => Player.Character == ECharacter.Ren ? 2 : BombPiecesPerBomb;
	public float CollectLine() => CollectLineY + (Player.Character == ECharacter.Sou ? 56f : 0f);
	public const float HaruSlowRadius = 64f;
	public const int DollRespawnFrames = 3600;
	public long StageBonus = 0;
	public const int ExtraStage = 6;
	public const int IfStage = 7;      // Extra-IF: the four perils
	public const int BeastArt = 7;     // art index of the first beast (饕餮, 檮杌, 混沌, 窮奇 = 7..10)
	public bool bIf = false;
	public bool IsQuadPhase() => Boss.bActive && Boss.Actor == 4;
	public bool bMidboss = false;       // the current boss fight is the stage's midboss
	public bool bMidDone = false;
	public bool IsShielded() => Boss.ShieldTimer > 0;
	public bool bEnding = false;        // post-fight conversation with the defeated boss

	// Life / bomb recovery (see UpdateRecovery)
	public const int LifePiecesPerLife = 5;
	public const int BombPiecesPerBomb = 3;
	public const int MaxLives = 8;
	public const int MaxBombs = 8;
	public const int GrazePerBombPiece = 250;
	public int NextExtend = 0;          // index into the score-extend table
	public int NextGrazeReward = GrazePerBombPiece;
	public string PopupText = "";       // short reward notice ("1UP!", "靈符 +1")
	public FLinearColor PopupColor = LinColor.White;
	public int PopupTimer = 0;

	// Hazards / effects driven by spell cards
	public List<FBeamHazard> Beams = new();
	public float FogRadius = 0f;        // > 0: only a circle of this radius around the player is visible

	public FDanmakuPlayer Player = new();
	public FBossState Boss = new();
	public List<FBullet> Bullets = new();
	public List<FShot> Shots = new();
	public List<FEnemy> Enemies = new();
	public List<FItem> Items = new();
	public List<FParticle> Particles = new();

	public long Score = 0;
	public long HiScore = 1000000;
	public int Graze = 0;
	public int SpellsCaptured = 0;
	public int SpellsTotal = 0;

	// Bomb (behaviour depends on the character that cast it)
	public int BombTime = 0;
	public int BombDuration = 1;
	public ECharacter BombKind = ECharacter.Ren;
	public FVector2f BombCenter = FVector2f.Zero;
	public float BombRadius = 0f;
	public bool bBombHitBoss = false;
	public bool IsTimeSlowed() => BombTime > 0 && BombKind == ECharacter.Haru;

	public string Message = "";
	public string SubMessage = "";
	public int MessageTimer = 0;
	public int MessageDuration = 1;

	public bool bDemo = false;

	// sound events for DanmakuAudio (consumed and cleared every frame)
	public List<ESfx> SfxQueue = new();
	public void Sfx(ESfx S) { if (SfxQueue.Count < 64) SfxQueue.Add(S); }

	// Pre-boss dialogue (-1 = none). Speaker: 0 = player, 1 = boss
	public int DialogueIndex = -1;
	public int DialogueTimer = 0;
	// a long line is shown over several boxes: the HUD measures it and reports how many boxes it needs
	public int DialoguePage = 0;
	public int DialoguePageCount = 1;
	public bool IsInDialogue() => DialogueIndex >= 0;

	public const float LaserHalfWidth = 9f;
	public const float ThunderHalfWidth = 38f;

	// ---------- (UE private section) ----------
	public FBossDef MidbossDef()
	{
		FBossDef D = BossDef();
		FBossDef C = D.CharMidboss[FMath.Clamp((int)Player.Character, 0, 3)];
		return C ?? D.Midboss;
	}
	public FBossDef ActiveDef()
	{
		if (bSpellPractice && PracticeDef != null) return PracticeDef;
		return bMidboss && MidbossDef() != null ? MidbossDef() : BossDef();
	}
	public FBossDef PracticeDef = null;
	public int PracticePhase = 0;
	public List<FDialogueLine> CurrentDialogue()
	{
		int C = FMath.Clamp((int)Player.Character, 0, 3);
		return bEnding ? BossDef().Ending[C] : ActiveDef().Dialogue[C];
	}
	public List<FPhaseDef> Phases() => ActiveDef().Phases;

	// ---- scripted bullet behaviours shared by stages 3+ ----
	public const byte BehPlumSeed = 1, BehLotusSummon = 2, BehPauseHome = 3, BehAssassin = 4, BehFinalChrys = 5, BehDelayedRelease = 6,
		BehCurveAfter = 7, BehSplitRing = 8, BehPauseDash = 9, BehArm = 10, BehWallStop = 11, BehGroundSplit = 12;

	// Tenhou replays earlier hands at reduced intensity: Every()/Cnt() are scaled and PlaceBoss is ignored
	public bool bComposite = false;
	public float CompInterval = 1f;
	public float CompCount = 1f;

	public float PowerMul() => 0.75f + 0.125f * Player.Power;   // bombs only; the main shot grows by power level instead
	/** Touhou-style power level 1..4 (靈力 1.00-1.99 = 1 ... 4.00 = 4): each level adds shots. */
	public int PowerLevel() => FMath.Clamp(FMath.FloorToInt(Player.Power + 0.001f), 1, 4);
	/** Sou's laser widens with each power level (full width at 4.00). */
	public float LaserWidthNow() => LaserHalfWidth * (0.55f + 0.15f * PowerLevel());

	public List<FBullet> NewBullets = new();
	public FRandomStream Rng = new FRandomStream();
	public float Accumulator = 0f;

	public FDanmakuInput CurInput;
	public bool bPrevBomb, bPrevStart, bPrevPause, bPrevRestart, bPrevLeft, bPrevRight, bPrevUp, bPrevDown;
	public bool bEdgeBomb, bEdgeStart, bEdgePause, bEdgeRestart, bEdgeLeft, bEdgeRight, bEdgeUp, bEdgeDown;

	// Demo / automated capture (-DanmakuDemo -DanmakuSkip=N -DanmakuPhase=N -DanmakuChar=N -DanmakuTitle
	//                            -DanmakuSelect -DanmakuBombAt=N -DanmakuShotEvery=N -DanmakuShots=N)
	public int DemoSkipFrames = 0;
	public int DemoStartPhase = 0;
	public int DemoCharacter = 0;
	public int DemoDifficulty = 1;
	public int DemoStage = -1;
	public float DemoHPScale = 1f;
	public int DemoBombAt = -1;
	public int DemoShotEvery = 0;
	public int DemoShotCount = 0;
	public int DemoShotsTaken = 0;
	public int DemoQuitTimer = 0;
	public int DemoPauseAt = 0;      // -DanmakuPauseAt=N: pause at frame N (screenshots of the pause menu)
	public bool bDemoHits = false;   // -DanmakuHitTest: the demo player can be hit

	// Road difficulty (Roads.cs). Tier 0 = Easy ... 3 = Lunatic; -1 = Extra / Extra-IF, whose roads keep
	// their own fixed layout.
	public int RoadTier() => StageIndex >= ExtraStage ? -1 : FMath.Clamp(Difficulty, 0, 3);

	// Autopilot (Bot.cs): -DanmakuBot plays by itself (and can be hit); -DanmakuTurbo=N runs N steps per tick
	// for fast headless tests; -DanmakuBotTag=name names the result file (Saved/BotResult_name.txt)
	public bool bBot = false;
	public int TurboSteps = 0;
	public string BotTag = "run";
	public string BotLog = "";
	public int BotDeaths = 0, BotBombs = 0, BotLastLives = 0;
	public bool bBotDone = false;
	// difficulty measurement: -DanmakuBotSkill=1..5 handicaps the bot; -DanmakuMeasure makes hits cost nothing (they're
	// only counted); -DanmakuSweep plays every spell card from -DanmakuSpell=N to the end and writes Saved/BotSweep_<tag>.txt
	public int BotSkill = 0;
	public bool bBotNoSpellBomb = false;   // -DanmakuBotNoSpellBomb: the bot never bombs during a spell card (non-spells and roads are fine)
	public bool BotMayBomb() => !bMeasure && !(bBotNoSpellBomb && Boss.bActive && Boss.bSpell);
	public bool bMeasure = false, bSweep = false, bRoadMeasure = false;   // -DanmakuRoadMeasure: roads of stages 1-6 instead
	public int SweepIndex = 0;
	public int MeasureHits = 0, MeasureFrames = 0;
	public double MeasureNear = 0.0, MeasureClose = 0.0;
	public int BotHold = 0;
	public FVector2f BotLastMove = FVector2f.Zero;
	public bool bBotLastFocus = false;
	public FRandomStream BotRng = new FRandomStream(777);   // the bot's own dice, so it never disturbs the game's RNG
	public string SweepLog = "";
}
