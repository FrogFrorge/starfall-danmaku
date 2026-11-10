#pragma once

#include "CoreMinimal.h"

// All gameplay runs in a fixed 384 x 448 "field" coordinate space (origin top-left, +Y down),
// stepped at a fixed 60 Hz. Rendering scales the field to the screen.
namespace Danmaku
{
	constexpr float FieldW = 384.f;
	constexpr float FieldH = 448.f;
	constexpr float StepSeconds = 1.f / 60.f;
	constexpr float PlayerHitRadius = 2.5f;
	constexpr float GrazeRadius = 18.f;
	constexpr float CollectLineY = 112.f;   // above this line, all items fly to the player
	constexpr int32 DeathBombWindow = 8;
	constexpr int32 NumCharacters = 4;
	constexpr int32 NumDolls = 4;

	FORCEINLINE FVector2f Dir(float Deg)
	{
		const float R = FMath::DegreesToRadians(Deg);
		return FVector2f(FMath::Cos(R), FMath::Sin(R));
	}
	FORCEINLINE float AngleOf(const FVector2f& V)
	{
		return FMath::RadiansToDegrees(FMath::Atan2(V.Y, V.X));
	}
	FORCEINLINE float DistSq(const FVector2f& A, const FVector2f& B)
	{
		return FVector2f::DistSquared(A, B);
	}

	// ---- language (DanmakuText.cpp): 0 = Traditional Chinese, 1 = English ----
	int32 GetLanguage();
	void SetLanguage(int32 Language);   // also saved to the user settings
	inline bool IsEnglish() { return GetLanguage() == 1; }
	/** English for a Chinese source string (the Chinese itself when in Chinese mode or not translated). */
	const TCHAR* Tr(const TCHAR* Zh);
	FString TrStr(const FString& Zh);
}

enum class EDanmakuState : uint8 { Title, StageSelect, DiffSelect, CharSelect, Playing, StageClear, GameOver, Clear, SpellSelect };

/** Playable characters (all male). */
enum class ECharacter : uint8
{
	Ren,    // 蓮 - wide talisman fan,   bomb: barrier shockwave
	Sou,    // 蒼 - piercing bow laser,  bomb: heaven-thunder wide beam
	Haru,   // 晴 - straight + homing,   bomb: time-slow barrier (not invincible)
	Yuki,   // 雪 - four puppet familiars, bomb: puppet storm
};

enum class EBulletShape : uint8 { Small, Medium, Large, Rice, Star, Crescent, Comet, Sakura, Maple, Snow, Water, Fire, Chrys, Tile };
enum class EBulletEvent : uint8 { None, AimAtPlayer, SplitRing, StopThenAim, SplitTwo };
enum class EItemType : uint8 { Power, BigPower, Point, Star, LifePiece, BombPiece };
enum class EShotType : uint8 { Talisman, Straight, Homing, Doll };
/** Sound effect events pushed by the simulation, played by ADanmakuAudio. */
enum class ESfx : uint8 { Shot, EnemyShot, Graze, Item, EnemyDie, PlayerDie, Bomb, Spell, SpellGet, Break, MenuMove, MenuOk, MenuBack, Pause, Count };

inline float BulletRadius(EBulletShape S)
{
	switch (S)
	{
	case EBulletShape::Small:    return 3.f;
	case EBulletShape::Medium:   return 4.5f;
	case EBulletShape::Large:    return 9.f;
	case EBulletShape::Rice:     return 2.5f;
	case EBulletShape::Star:     return 3.5f;
	case EBulletShape::Crescent: return 4.f;
	case EBulletShape::Comet:    return 4.f;
	case EBulletShape::Sakura:   return 4.f;
	case EBulletShape::Maple:    return 4.5f;
	case EBulletShape::Snow:     return 3.5f;
	case EBulletShape::Water:    return 3.5f;
	case EBulletShape::Fire:     return 4.5f;
	case EBulletShape::Chrys:    return 4.5f;
	case EBulletShape::Tile:     return 3.6f;
	}
	return 3.f;
}

inline float BulletVisualSize(EBulletShape S)
{
	switch (S)
	{
	case EBulletShape::Small:    return 10.f;
	case EBulletShape::Medium:   return 15.f;
	case EBulletShape::Large:    return 30.f;
	case EBulletShape::Rice:     return 14.f;
	case EBulletShape::Star:     return 14.f;
	case EBulletShape::Crescent: return 17.f;
	case EBulletShape::Comet:    return 20.f;
	case EBulletShape::Sakura:   return 16.f;
	case EBulletShape::Maple:    return 19.f;
	case EBulletShape::Snow:     return 15.f;
	case EBulletShape::Water:    return 15.f;
	case EBulletShape::Fire:     return 19.f;
	case EBulletShape::Chrys:    return 18.f;
	case EBulletShape::Tile:     return 13.6f;
	}
	return 10.f;
}

struct FBullet
{
	FVector2f Pos = FVector2f::ZeroVector;
	float Speed = 2.f;
	float Angle = 90.f;       // degrees; 0 = right, 90 = down
	float Accel = 0.f;
	float AngVel = 0.f;       // degrees / frame
	float AngVelDecay = 1.f;  // AngVel *= this every frame
	float MinSpeed = -1000.f;
	float MaxSpeed = 1000.f;
	float Radius = 3.f;
	FLinearColor Color = FLinearColor::Red;
	EBulletShape Shape = EBulletShape::Small;
	EBulletEvent Event = EBulletEvent::None;
	int32 EventFrame = -1;
	float EventParam = 0.f;
	float EventParam2 = 0.f;
	int32 Bounces = 0;        // reflections left off the side walls
	// Orbiting around the boss until EventFrame, then rushing at the player (EventParam = rush speed)
	bool bOrbit = false;
	float OrbitAngle = 0.f;
	float OrbitRadius = 0.f;
	float OrbitSpeed = 0.f;   // degrees / frame
	// Motion: 0 = speed/angle, 1 = polar around a (moving) centre, 2 = velocity vector with gravity
	uint8 Motion = 0;
	FVector2f Center = FVector2f::ZeroVector;
	FVector2f CenterVel = FVector2f::ZeroVector;
	float PolarAngle = 0.f;      // degrees
	float PolarAngSpeed = 0.f;   // degrees / frame
	float PolarRadius = 0.f;
	float PolarRadSpeed = 0.f;
	FVector2f Vel = FVector2f::ZeroVector;
	FVector2f Gravity = FVector2f::ZeroVector;
	// sideways sway added on top of any motion
	float WaveAmp = 0.f, WaveFreq = 0.f, WavePhase = 0.f;
	// walls it reflects off: 1 = sides, 2 = top, 4 = bottom (0 = sides only, legacy)
	uint8 WallMask = 0;
	bool bDieAfterBounces = false;
	float KillBelowY = 100000.f; // vanishes when it falls below this line
	bool bBombProof = false;     // bombs can't erase it
	float Spin = 0.f;            // visual spin, degrees / frame (0 = face travel direction)
	float Scale = 1.f;           // size multiplier (visual and hitbox)
	// Scripted behaviour (see ADanmakuWorld::UpdateBehavior). Timer/Timer2/Param/Param2 are behaviour-specific.
	uint8 Behavior = 0;
	int32 Timer = 0, Timer2 = 0, Param = 0;
	float Param2 = 0.f;
	bool bHarmless = false;      // purely visual (seeds, summons): no hit, no graze
	bool bSpat = false;          // spat out by 饕餮: never swallowed again
	bool bWrap = false;          // leaves one edge and comes back from the opposite one
	int32 Life = 0;              // > 0: disappears after this many frames
	int32 Age = 0;
	bool bGrazed = false;
	bool bDead = false;
};

/** A telegraphed straight hazard: a warning line, then a beam that grows to full length, holds, and fades. */
struct FBeamHazard
{
	FVector2f Origin = FVector2f::ZeroVector;
	float Angle = -90.f;      // direction it extends in
	float MaxLength = 1000.f;
	float Length = 0.f;
	float Width = 10.f;
	int32 Warn = 60, Rise = 20, Hold = 40;
	int32 Age = 0;
	FLinearColor Color = FLinearColor::White;
	uint8 OnStrike = 0;       // 0 nothing, 1 fireballs from the bottom (lightning), 2 leaves (bamboo), 3 shrapnel where it hits the wall
	bool bStruck = false;
	bool bGrazed = false;
	bool bDead = false;
	bool IsActive() const { return Age >= Warn; }
};

struct FShot
{
	FVector2f Pos = FVector2f::ZeroVector;
	FVector2f Vel = FVector2f::ZeroVector;
	float Damage = 1.f;
	EShotType Type = EShotType::Talisman;
	bool bFromBomb = false;      // fired by a bomb (bosses' last cards ignore it)
	int32 Age = 0;
	bool bDead = false;
};

struct FEnemy
{
	FVector2f Pos = FVector2f::ZeroVector;
	FVector2f Vel = FVector2f::ZeroVector;
	FVector2f LeaveVel = FVector2f::ZeroVector;
	float HP = 1.f;
	float Radius = 10.f;
	int32 Age = 0;
	int32 StopAt = -1;
	int32 LeaveAt = -1;
	int32 Pattern = 0;
	int32 Kind = 0;
	int32 DropPower = 0;
	int32 DropPoint = 0;
	int32 HitFlash = 0;
	FVector2f Acc = FVector2f::ZeroVector;   // added to Vel every frame (curving paths)
	float SineAmp = 0.f, SineFreq = 0.f, SinePhase = 0.f;   // sideways sway
	bool bRevenge = false;   // bursts into a small ring when killed (Extra)
	FLinearColor Color = FLinearColor::Blue;
	bool bDead = false;
};

struct FItem
{
	FVector2f Pos = FVector2f::ZeroVector;
	FVector2f Vel = FVector2f::ZeroVector;
	EItemType Type = EItemType::Power;
	int32 Age = 0;
	bool bCollecting = false;
	bool bDead = false;
};

struct FParticle
{
	FVector2f Pos = FVector2f::ZeroVector;
	FVector2f Vel = FVector2f::ZeroVector;
	float Size = 8.f;
	FLinearColor Color = FLinearColor::White;
	int32 Life = 30;
	int32 MaxLife = 30;
	bool bRing = false;
};

struct FDanmakuPlayer
{
	ECharacter Character = ECharacter::Ren;
	FVector2f Pos = FVector2f(Danmaku::FieldW * 0.5f, Danmaku::FieldH - 48.f);
	FVector2f Dolls[Danmaku::NumDolls] = { Pos, Pos, Pos, Pos };
	TArray<FVector2f> Trail;     // recent positions, newest first (puppets follow it)
	bool bFocus = false;
	bool bShooting = false;
	bool bLaserOn = false;       // Sou's beam is firing this frame
	int32 Lives = 2;             // extra lives
	int32 Bombs = 3;
	int32 LifePieces = 0;        // 5 pieces = 1 extra life
	int32 BombPieces = 0;        // 3 pieces = 1 bomb
	float Power = 1.f;           // 1.00 - 4.00, scales shot damage
	int32 Invuln = 0;
	int32 DeathTimer = 0;        // > 0: just got hit, death-bomb window
	int32 RespawnTimer = 0;
	int32 ShotCooldown = 0;
	int32 Lean = 0;              // -12 .. 12, drives cloth trailing
	int32 DollsAlive = Danmaku::NumDolls;   // Yuki: puppets still with him (spent by his auto-bomb)
	int32 DollRespawn = 0;                  // frames until the next lost puppet comes back
};

struct FBossState
{
	bool bActive = false;
	bool bStarted = false;
	bool bVulnerable = false;
	bool bSpell = false;
	bool bBonusValid = false;
	FVector2f Pos = FVector2f(Danmaku::FieldW * 0.5f, -40.f);
	FVector2f MoveFrom = FVector2f::ZeroVector;
	FVector2f MoveTo = FVector2f::ZeroVector;
	FVector2f Vel = FVector2f::ZeroVector;
	int32 MoveTime = 0;
	int32 MoveDur = 0;
	int32 Phase = -1;
	int32 PhaseTime = 0;
	int32 TimeLeft = 0;
	int32 TimeTotal = 1;
	int32 GapTimer = 0;
	int32 EntryTimer = 0;
	int32 DefeatTimer = 0;
	int32 HitFlash = 0;
	float HP = 0.f;
	float MaxHP = 1.f;
	FString SpellName;
	int32 PatternTimer = 0;   // free per-phase scratch for patterns (next wave frame, ...)
	int32 Stage6Beasts = 0;   // how many of the four beasts have joined the stage 6 last spell / Tenhou's hands
	int32 ShieldTimer = 0;    // > 0: a spell card just started, the boss can't be damaged yet
	int32 JoinFrame[12] = {}; // Tenhou: the phase frame each earlier hand joined at
	// Extra-IF relay: which beast is fighting (-1 none, 0..3 one beast, 4 all four at once)
	int32 Actor = -1;
	int32 DepartActor = -1;   // the beast flying off after its two cards
	int32 DepartTimer = 0;
	FVector2f DepartPos = FVector2f::ZeroVector;
	FVector2f DepartVel = FVector2f::ZeroVector;
	int32 EntrySide = 1;      // the next beast enters from this side (+1 right, -1 left)
	FVector2f QuadPos[4];     // all-four phases: where each beast is
	FVector2f MawPos = FVector2f::ZeroVector;   // 饕餮: the mouth that swallows bullets
	int32 MawEaten = 0;
};

/** Static info about each playable character, shown on the select screen. */
struct FCharacterInfo
{
	const TCHAR* Name;      // display name
	const TCHAR* Title;     // short type label
	const TCHAR* ShotDesc;
	const TCHAR* BombName;
	const TCHAR* BombDesc;
	FLinearColor Color;
	float Speed;            // movement per frame
	float FocusSpeed;       // with Shift held
	const TCHAR* Trait1;    // the two special traits shown on the select screen
	const TCHAR* Trait2;
};

inline const FCharacterInfo& GetCharacterInfo(ECharacter C)
{
	static const FCharacterInfo Infos[Danmaku::NumCharacters] = {
		{ TEXT("蓮"), TEXT("廣域型"), TEXT("前方扇形五連符咒"), TEXT("結界「破邪衝擊」"), TEXT("擴散衝擊波清除彈幕，對 Boss 造成一次大傷害"), FLinearColor(0.55f, 0.75f, 1.f),
			4.f, 2.f, TEXT("判定縮小 20%"), TEXT("2 個碎片換 1 張靈符") },
		{ TEXT("蒼"), TEXT("貫穿型"), TEXT("雷弓光束持續貫穿"), TEXT("雷弓「天雷一閃」"), TEXT("超寬雷射縱向清場，持續傷害"), FLinearColor(1.f, 0.55f, 0.45f),
			6.f, 2.f, TEXT("不射擊時移動速度 ×1.5"), TEXT("道具回收線較低") },
		{ TEXT("晴"), TEXT("追蹤型"), TEXT("兩排直符＋追蹤護符"), TEXT("時符「緩流結界」"), TEXT("敵方減速一半（不無敵）"), FLinearColor(0.55f, 1.f, 0.55f),
			4.f, 3.f, TEXT("身邊的子彈減速 20%"), TEXT("道具掉落減速 20%") },
		{ TEXT("雪"), TEXT("操偶型"), TEXT("四體式神尾隨射擊"), TEXT("傀儡「式神暴雨」"), TEXT("式神扇形亂射"), FLinearColor(0.85f, 0.7f, 1.f),
			3.f, 1.f, TEXT("被彈時自動 Bomb（消耗 2 張靈符，不足時以式神抵）"), TEXT("式神每 60 秒復活；四體齊全時式神傷害 +20%、本體 −20%") },
	};
	return Infos[FMath::Clamp(int32(C), 0, Danmaku::NumCharacters - 1)];
}

/** Difficulty levels scale bullet counts, firing intervals and bullet speed of every pattern. */
struct FDifficultyInfo
{
	const TCHAR* Name;
	const TCHAR* Label;
	const TCHAR* Desc;
	float CountMul;       // bullets per volley
	float IntervalScale;  // > 1 = fires less often
	float SpeedMul;       // enemy bullet speed
	int32 ExtraLives;
	FLinearColor Color;
};

namespace Danmaku { constexpr int32 NumDifficulties = 4; }

inline const FDifficultyInfo& GetDifficultyInfo(int32 D)
{
	static const FDifficultyInfo Infos[Danmaku::NumDifficulties + 1] = {
		{ TEXT("Easy"),    TEXT("簡單"), TEXT("彈幕稀疏、速度較慢，殘機多兩個"), 0.45f, 1.7f,  0.78f, 4, FLinearColor(0.55f, 1.f, 0.6f) },
		{ TEXT("Normal"),  TEXT("普通"), TEXT("標準難度"),                       0.65f, 1.35f, 0.9f,  2, FLinearColor(0.55f, 0.8f, 1.f) },
		{ TEXT("Hard"),    TEXT("困難"), TEXT("密度更高，適合熟練的玩家"),         0.85f, 1.12f, 1.0f,  2, FLinearColor(1.f, 0.7f, 0.4f) },
		{ TEXT("Lunatic"), TEXT("瘋狂"), TEXT("全力的彈幕，非常人所能及"),         1.0f,  1.0f,  1.05f, 2, FLinearColor(1.f, 0.45f, 0.8f) },
		{ TEXT("Extra"),   TEXT("額外"), TEXT("通關後的特別挑戰"),                   0.9f,  1.05f, 1.0f,  2, FLinearColor(1.f, 0.85f, 0.4f) },
	};
	return Infos[FMath::Clamp(D, 0, Danmaku::NumDifficulties)];
}
