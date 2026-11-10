// Port of DanmakuTypes.h + DanmakuText.cpp.
// All gameplay runs in a fixed 384 x 448 "field" coordinate space (origin top-left, +Y down),
// stepped at a fixed 60 Hz. Rendering scales the field to the screen.
// The UE structs are classes here (reference semantics); where the C++ copied one by value, use .Clone().
using Godot;

namespace DanmakuSTG;

public static partial class Danmaku
{
	public const float FieldW = 384f;
	public const float FieldH = 448f;
	public const float StepSeconds = 1f / 60f;
	public const float PlayerHitRadius = 2.5f;
	public const float GrazeRadius = 18f;
	public const float CollectLineY = 112f;   // above this line, all items fly to the player
	public const int DeathBombWindow = 8;
	public const int NumCharacters = 4;
	public const int NumDolls = 4;
	public const int NumDifficulties = 4;
	/** Character speeds are listed in half-pixels per frame (蓮 8 / 4 = 4 / 2 field units per frame). */
	public const float SpeedUnit = 0.5f;

	public static FVector2f Dir(float Deg)
	{
		float R = FMath.DegreesToRadians(Deg);
		return new FVector2f(FMath.Cos(R), FMath.Sin(R));
	}
	public static float AngleOf(FVector2f V) => FMath.RadiansToDegrees(FMath.Atan2(V.Y, V.X));
	public static float DistSq(FVector2f A, FVector2f B) => A.DistanceSquaredTo(B);
	public static float Sq(float X) => X * X;

	// ---- language: 0 = Traditional Chinese, 1 = English (saved to user://settings.cfg) ----
	static int GLanguage = -1;
	static Dictionary<string, string> GMap;
	const string SettingsPath = "user://settings.cfg";

	public static int GetLanguage()
	{
		if (GLanguage < 0)
		{
			var Cfg = new ConfigFile();
			int L = 0;
			if (Cfg.Load(SettingsPath) == Error.Ok) L = (int)Cfg.GetValue("Danmaku", "Language", 0);
			GLanguage = FMath.Clamp(L, 0, 1);
		}
		return GLanguage;
	}

	public static void SetLanguage(int Language)
	{
		GLanguage = FMath.Clamp(Language, 0, 1);
		var Cfg = new ConfigFile();
		Cfg.Load(SettingsPath);
		Cfg.SetValue("Danmaku", "Language", GLanguage);
		Cfg.Save(SettingsPath);
	}

	public static bool IsEnglish() => GetLanguage() == 1;

	static Dictionary<string, string> Table()
	{
		if (GMap == null)
		{
			GMap = new Dictionary<string, string>(GTable.Length);
			foreach (var P in GTable) GMap[P.Zh] = P.En;
			foreach (var P in GTableExtra) GMap[P.Zh] = P.En;
			foreach (var P in Story.AllPairs()) GMap[P.Zh] = P.En;
		}
		return GMap;
	}

	/** English for a Chinese source string (the Chinese itself when in Chinese mode or not translated). */
	public static string Tr(string Zh)
	{
		if (string.IsNullOrEmpty(Zh) || GetLanguage() == 0) return Zh;
		return Table().TryGetValue(Zh, out var En) ? En : Zh;
	}

	public static string TrStr(string Zh)
	{
		if (GetLanguage() == 0 || string.IsNullOrEmpty(Zh)) return Zh;
		if (Table().TryGetValue(Zh, out var En)) return En;
		// pieces glued together at runtime: swap the full-width punctuation for its English look
		return Zh.Replace("　", "  ").Replace("（", " (").Replace("）", ")").Replace("「", "\"").Replace("」", "\"").Replace("：", ": ");
	}

	public static float BulletRadius(EBulletShape S) => S switch
	{
		EBulletShape.Small => 3f,
		EBulletShape.Medium => 4.5f,
		EBulletShape.Large => 9f,
		EBulletShape.Rice => 2.5f,
		EBulletShape.Star => 3.5f,
		EBulletShape.Crescent => 4f,
		EBulletShape.Comet => 4f,
		EBulletShape.Sakura => 4f,
		EBulletShape.Maple => 4.5f,
		EBulletShape.Snow => 3.5f,
		EBulletShape.Water => 3.5f,
		EBulletShape.Fire => 4.5f,
		EBulletShape.Chrys => 4.5f,
		EBulletShape.Tile => 3.6f,
		_ => 3f,
	};

	public static float BulletVisualSize(EBulletShape S) => S switch
	{
		EBulletShape.Small => 10f,
		EBulletShape.Medium => 15f,
		EBulletShape.Large => 30f,
		EBulletShape.Rice => 14f,
		EBulletShape.Star => 14f,
		EBulletShape.Crescent => 17f,
		EBulletShape.Comet => 20f,
		EBulletShape.Sakura => 16f,
		EBulletShape.Maple => 19f,
		EBulletShape.Snow => 15f,
		EBulletShape.Water => 15f,
		EBulletShape.Fire => 19f,
		EBulletShape.Chrys => 18f,
		EBulletShape.Tile => 13.6f,
		_ => 10f,
	};

	static readonly FCharacterInfo[] CharacterInfos =
	{
		new("蓮", "廣域型", "前方扇形符咒（靈力越高越多）", "結界「破邪衝擊」", "擴散衝擊波清除彈幕，對 Boss 造成一次大傷害", new FLinearColor(0.55f, 0.75f, 1f),
			8f, 4f, "判定縮小 20%", "2 個碎片換 1 張靈符"),
		new("蒼", "貫穿型", "雷弓光束持續貫穿", "雷弓「天雷一閃」", "超寬雷射縱向清場，持續傷害", new FLinearColor(1f, 0.55f, 0.45f),
			10f, 5f, "不射擊時移動速度 ×1.5", "道具回收線較低"),
		new("晴", "追蹤型", "兩排直符＋追蹤護符", "時符「緩流結界」", "敵方減速一半（不無敵）", new FLinearColor(0.55f, 1f, 0.55f),
			9f, 6f, "身邊的子彈減速 20%", "道具掉落減速 20%"),
		new("雪", "操偶型", "四體式神尾隨射擊", "傀儡「式神暴雨」", "式神扇形亂射", new FLinearColor(0.85f, 0.7f, 1f),
			7f, 3f, "被彈時自動 Bomb（消耗 2 張靈符，不足時以式神抵）", "式神每 60 秒復活；四體齊全時式神傷害 +20%、本體 −20%"),
	};
	public static FCharacterInfo GetCharacterInfo(ECharacter C) => CharacterInfos[FMath.Clamp((int)C, 0, NumCharacters - 1)];

	static readonly FDifficultyInfo[] DifficultyInfos =
	{
		new("Easy",    "簡單", "彈幕稀疏、速度較慢，殘機多兩個", 0.45f, 1.7f,  0.78f, 4, new FLinearColor(0.55f, 1f, 0.6f)),
		new("Normal",  "普通", "標準難度",                       0.65f, 1.35f, 0.9f,  2, new FLinearColor(0.55f, 0.8f, 1f)),
		new("Hard",    "困難", "密度更高，適合熟練的玩家",         0.85f, 1.12f, 1.0f,  2, new FLinearColor(1f, 0.7f, 0.4f)),
		new("Lunatic", "瘋狂", "全力的彈幕，非常人所能及",         1.0f,  1.0f,  1.05f, 2, new FLinearColor(1f, 0.45f, 0.8f)),
		new("Extra",   "額外", "通關後的特別挑戰",                   0.9f,  1.05f, 1.0f,  2, new FLinearColor(1f, 0.85f, 0.4f)),
	};
	public static FDifficultyInfo GetDifficultyInfo(int D) => DifficultyInfos[FMath.Clamp(D, 0, NumDifficulties)];
}

public enum EDanmakuState : byte { Title, StageSelect, DiffSelect, CharSelect, Playing, StageClear, GameOver, Clear, SpellSelect, MusicRoom, Options, EndingScene }

/** Playable characters (all male). */
public enum ECharacter : byte
{
	Ren,    // 蓮 - wide talisman fan,   bomb: barrier shockwave
	Sou,    // 蒼 - piercing bow laser,  bomb: heaven-thunder wide beam
	Haru,   // 晴 - straight + homing,   bomb: time-slow barrier (not invincible)
	Yuki,   // 雪 - four puppet familiars, bomb: puppet storm
}

public enum EBulletShape : byte { Small, Medium, Large, Rice, Star, Crescent, Comet, Sakura, Maple, Snow, Water, Fire, Chrys, Tile }
public enum EBulletEvent : byte { None, AimAtPlayer, SplitRing, StopThenAim, SplitTwo }
public enum EItemType : byte { Power, BigPower, Point, Star, LifePiece, BombPiece, OneUp }
public enum EShotType : byte { Talisman, Straight, Homing, Doll }
/** Sound effect events pushed by the simulation, played by DanmakuAudio. */
public enum ESfx : byte { Shot, EnemyShot, Graze, Item, EnemyDie, PlayerDie, Bomb, Spell, SpellGet, Break, MenuMove, MenuOk, MenuBack, Pause, Count }

public class FBullet
{
	public FVector2f Pos = FVector2f.Zero;
	public float Speed = 2f;
	public float Angle = 90f;       // degrees; 0 = right, 90 = down
	public float Accel = 0f;
	public float AngVel = 0f;       // degrees / frame
	public float AngVelDecay = 1f;  // AngVel *= this every frame
	public float MinSpeed = -1000f;
	public float MaxSpeed = 1000f;
	public float Radius = 3f;
	public FLinearColor Color = LinColor.Red;
	public EBulletShape Shape = EBulletShape.Small;
	public EBulletEvent Event = EBulletEvent.None;
	public int EventFrame = -1;
	public float EventParam = 0f;
	public float EventParam2 = 0f;
	public int Bounces = 0;         // reflections left off the side walls
	// Orbiting around the boss until EventFrame, then rushing at the player (EventParam = rush speed)
	public bool bOrbit = false;
	public float OrbitAngle = 0f;
	public float OrbitRadius = 0f;
	public float OrbitSpeed = 0f;   // degrees / frame
	// Motion: 0 = speed/angle, 1 = polar around a (moving) centre, 2 = velocity vector with gravity
	public byte Motion = 0;
	public FVector2f Center = FVector2f.Zero;
	public FVector2f CenterVel = FVector2f.Zero;
	public float PolarAngle = 0f;      // degrees
	public float PolarAngSpeed = 0f;   // degrees / frame
	public float PolarRadius = 0f;
	public float PolarRadSpeed = 0f;
	public FVector2f Vel = FVector2f.Zero;
	public FVector2f Gravity = FVector2f.Zero;
	// sideways sway added on top of any motion
	public float WaveAmp = 0f, WaveFreq = 0f, WavePhase = 0f;
	// walls it reflects off: 1 = sides, 2 = top, 4 = bottom (0 = sides only, legacy)
	public byte WallMask = 0;
	public bool bDieAfterBounces = false;
	public float KillBelowY = 100000f; // vanishes when it falls below this line
	public bool bBombProof = false;    // bombs can't erase it
	public float Spin = 0f;            // visual spin, degrees / frame (0 = face travel direction)
	public float Scale = 1f;           // size multiplier (visual and hitbox)
	// Scripted behaviour (see DanmakuWorld.UpdateBehavior). Timer/Timer2/Param/Param2 are behaviour-specific.
	public byte Behavior = 0;
	public int Timer = 0, Timer2 = 0, Param = 0;
	public float Param2 = 0f;
	public bool bHarmless = false;     // purely visual (seeds, summons): no hit, no graze
	public bool bSpat = false;         // spat out by 饕餮: never swallowed again
	public bool bWrap = false;         // leaves one edge and comes back from the opposite one
	public int Life = 0;               // > 0: disappears after this many frames
	public int Age = 0;
	public bool bGrazed = false;
	public bool bDead = false;
	public FBullet Clone() => (FBullet)MemberwiseClone();
}

/** A telegraphed straight hazard: a warning line, then a beam that grows to full length, holds, and fades. */
public class FBeamHazard
{
	public FVector2f Origin = FVector2f.Zero;
	public float Angle = -90f;      // direction it extends in
	public float MaxLength = 1000f;
	public float Length = 0f;
	public float Width = 10f;
	public int Warn = 60, Rise = 20, Hold = 40;
	public int Age = 0;
	public FLinearColor Color = LinColor.White;
	public byte OnStrike = 0;       // 0 nothing, 1 fireballs from the bottom (lightning), 2 leaves (bamboo), 3 shrapnel, 4 rising crescent
	public bool bStruck = false;
	public bool bHarmless = false;  // only a marker line: shows where shots will go, never hits
	public bool bGrazed = false;
	public bool bDead = false;
	public bool IsActive() => Age >= Warn;
}

public class FShot
{
	public FVector2f Pos = FVector2f.Zero;
	public FVector2f Vel = FVector2f.Zero;
	public float Damage = 1f;
	public EShotType Type = EShotType.Talisman;
	public bool bFromBomb = false;      // fired by a bomb (bosses' last cards ignore it)
	public int Age = 0;
	public bool bDead = false;
}

public class FEnemy
{
	public FVector2f Pos = FVector2f.Zero;
	public FVector2f Vel = FVector2f.Zero;
	public FVector2f LeaveVel = FVector2f.Zero;
	public float HP = 1f;
	public float Radius = 10f;
	public int Age = 0;
	public int StopAt = -1;
	public int LeaveAt = -1;
	public int Pattern = 0;
	public int Kind = 0;
	public int DropPower = 0;
	public int DropPoint = 0;
	public int HitFlash = 0;
	public FVector2f Acc = FVector2f.Zero;   // added to Vel every frame (curving paths)
	public float SineAmp = 0f, SineFreq = 0f, SinePhase = 0f;   // sideways sway
	public bool bRevenge = false;   // bursts into a small ring when killed
	public bool bCarry1Up = false;  // carries a 1UP item that drops when it is shot down
	public FLinearColor Color = LinColor.Blue;
	public bool bDead = false;
}

public class FItem
{
	public FVector2f Pos = FVector2f.Zero;
	public FVector2f Vel = FVector2f.Zero;
	public EItemType Type = EItemType.Power;
	public int Age = 0;
	public bool bCollecting = false;
	public bool bDead = false;
}

public class FParticle
{
	public FVector2f Pos = FVector2f.Zero;
	public FVector2f Vel = FVector2f.Zero;
	public float Size = 8f;
	public FLinearColor Color = LinColor.White;
	public int Life = 30;
	public int MaxLife = 30;
	public bool bRing = false;
}

public class FDanmakuPlayer
{
	public ECharacter Character = ECharacter.Ren;
	public FVector2f Pos = new FVector2f(FieldW * 0.5f, FieldH - 48f);
	public FVector2f[] Dolls;
	public List<FVector2f> Trail = new();   // recent positions, newest first (puppets follow it)
	public bool bFocus = false;
	public bool bShooting = false;
	public bool bLaserOn = false;       // Sou's beam is firing this frame
	public int Lives = 2;               // extra lives
	public int Bombs = 3;
	public int LifePieces = 0;          // 5 pieces = 1 extra life
	public int BombPieces = 0;          // 3 pieces = 1 bomb
	public float Power = 1f;            // 1.00 - 4.00, scales shot damage
	public int Invuln = 0;
	public int DeathTimer = 0;          // > 0: just got hit, death-bomb window
	public int RespawnTimer = 0;
	public int ShotCooldown = 0;
	public int Lean = 0;                // -12 .. 12, drives cloth trailing
	public int DollsAlive = NumDolls;   // Yuki: puppets still with him (spent by his auto-bomb)
	public int DollRespawn = 0;         // frames until the next lost puppet comes back
	public FDanmakuPlayer() { Dolls = new[] { Pos, Pos, Pos, Pos }; }
}

public class FBossState
{
	public bool bActive = false;
	public bool bStarted = false;
	public bool bVulnerable = false;
	public bool bSpell = false;
	public bool bBonusValid = false;
	public FVector2f Pos = new FVector2f(FieldW * 0.5f, -40f);
	public FVector2f MoveFrom = FVector2f.Zero;
	public FVector2f MoveTo = FVector2f.Zero;
	public FVector2f Vel = FVector2f.Zero;
	public int MoveTime = 0;
	public int MoveDur = 0;
	public int Phase = -1;
	public int PhaseTime = 0;
	public int TimeLeft = 0;
	public int TimeTotal = 1;
	public int GapTimer = 0;
	public int EntryTimer = 0;
	public int DefeatTimer = 0;
	public int HitFlash = 0;
	public float HP = 0f;
	public float MaxHP = 1f;
	public string SpellName = "";
	public int PatternTimer = 0;   // free per-phase scratch for patterns (next wave frame, ...)
	public int Stage6Beasts = 0;   // how many of the four beasts have joined the stage 6 last spell / Tenhou's hands
	public int ShieldTimer = 0;    // > 0: a spell card just started, the boss can't be damaged yet
	public int[] JoinFrame = new int[12]; // Tenhou: the phase frame each earlier hand joined at
	// Extra-IF relay: which beast is fighting (-1 none, 0..3 one beast, 4 all four at once)
	public int Actor = -1;
	public int DepartActor = -1;   // the beast flying off after its two cards
	public int DepartTimer = 0;
	public FVector2f DepartPos = FVector2f.Zero;
	public FVector2f DepartVel = FVector2f.Zero;
	public int EntrySide = 1;      // the next beast enters from this side (+1 right, -1 left)
	public FVector2f[] QuadPos = new FVector2f[4];     // all-four phases: where each beast is
	public FVector2f MawPos = FVector2f.Zero;          // 饕餮: the mouth that swallows bullets
	public int MawEaten = 0;
}

/** Static info about each playable character, shown on the select screen. */
public record FCharacterInfo(string Name, string Title, string ShotDesc, string BombName, string BombDesc, FLinearColor Color,
	float Speed, float FocusSpeed, string Trait1, string Trait2);

/** Difficulty levels scale bullet counts, firing intervals and bullet speed of every pattern. */
public record FDifficultyInfo(string Name, string Label, string Desc, float CountMul, float IntervalScale, float SpeedMul, int ExtraLives, FLinearColor Color);
