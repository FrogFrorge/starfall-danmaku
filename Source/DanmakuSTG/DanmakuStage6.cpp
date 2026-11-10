// Stage 6 (final) boss: 燁, the solemn guardian of the four symbols.
// One card per beast, then 「四象歸一」: the longest card in the main game. It starts with the azure dragon only and
// adds the white tiger, vermilion bird and black tortoise as its HP drops below 75 / 50 / 25 %.

#include "DanmakuWorld.h"

using namespace Danmaku;

namespace
{
	const FLinearColor ColDragon(0.3f, 0.85f, 1.f);
	const FLinearColor ColTiger(0.95f, 0.96f, 1.f);
	const FLinearColor ColBird(1.f, 0.45f, 0.25f);
	const FLinearColor ColTortoise(0.35f, 0.5f, 1.f);
	const FLinearColor ColGoldS6(1.f, 0.85f, 0.45f);
}

// ---------- the four beasts as reusable parts (K = intensity, 1 = full card) ----------

void ADanmakuWorld::S6_DragonPart(int32 T, float K)
{
	// a dragon's body: a stream that snakes left and right down the field
	const int32 Iv = FMath::Max(2, FMath::RoundToInt(4 / K));
	if (Every(T, Iv, 0))
	{
		const FVector2f Head = Boss.Pos + FVector2f(FMath::Sin(T * 0.028f) * 110.f, 10.f);
		// the dragon's body is two scales wide, three on Lunatic
		const int32 Lanes = Difficulty >= 3 && K > 1.f ? 3 : 2;
		for (int32 L = 0; L < Lanes; ++L)
		{
			const float Off = (L - (Lanes - 1) * 0.5f) * 16.f;
			FBullet& B = Fire(Head + FVector2f(Off, 0.f), 2.1f, 90.f + FMath::Sin(T * 0.06f) * 28.f, EBulletShape::Rice, ColDragon);
			B.WaveAmp = 0.5f;
			B.WaveFreq = 0.04f;
		}
	}
	if (Every(T, FMath::RoundToInt(70 / K), 35)) Ring(Boss.Pos, Cnt(18), 1.6f, T * 0.9f, EBulletShape::Medium, FLinearColor(0.45f, 0.95f, 0.8f));
}

void ADanmakuWorld::S6_TigerPart(int32 T, float K)
{
	// claws: three parallel streaks raking at the player, then a wide crescent sweep
	const int32 Cyc = FMath::Max(1, FMath::RoundToInt(70 / K));
	const int32 Ph = T % Cyc;
	if (Ph == 0)
	{
		const float Aim = AimAngle(Boss.Pos);
		const FVector2f Fwd = Dir(Aim), Side = Dir(Aim + 90.f);
		for (int32 c = -1; c <= 1; ++c)
		{
			const int32 N = Cnt(9);
			for (int32 j = 0; j < N; ++j)
			{
				FBullet& B = Fire(Boss.Pos + Side * (c * 26.f) - Fwd * (j * 9.f), 3.3f, Aim + c * 4.f, EBulletShape::Crescent, ColTiger);
				B.Accel = 0.01f;
				B.MaxSpeed = 4.2f;
			}
		}
	}
	if (Ph == 24)
	{
		const int32 N = Cnt(26);
		const float Base = AimAngle(Boss.Pos) + 180.f / N;
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Boss.Pos, 1.6f, Base + 360.f * i / N, EBulletShape::Crescent, FLinearColor(0.85f, 0.9f, 1.f));
			B.Accel = 0.012f;
			B.MaxSpeed = 2.8f;
		}
	}
	// wind: gusts from both walls at once, in rows
	if (Every(T, FMath::Max(2, FMath::RoundToInt(10 / K)), 0))
	{
		const float Y = Rng.FRandRange(40.f, FieldH * 0.78f);
		Fire(FVector2f(-6.f, Y), 2.4f, 6.f + Rng.FRandRange(-4.f, 4.f), EBulletShape::Small, FLinearColor(0.8f, 0.9f, 1.f));
		Fire(FVector2f(FieldW + 6.f, Y + 30.f), 2.4f, 174.f + Rng.FRandRange(-4.f, 4.f), EBulletShape::Small, FLinearColor(0.8f, 0.9f, 1.f));
	}
}

void ADanmakuWorld::S6_PhoenixPart(int32 T, float K)
{
	// wings of flame sweeping open and shut, embers falling from the sky
	if (Every(T, FMath::RoundToInt(7 / K), 0))
	{
		const float Spread = 40.f + FMath::Sin(T * 0.045f) * 32.f;
		for (float Sgn : { -1.f, 1.f })
		{
			Fire(Boss.Pos, 2.f, 90.f + Sgn * Spread, EBulletShape::Fire, ColBird);
		}
	}
	if (Every(T, FMath::RoundToInt(16 / K), 8))
	{
		FBullet& E = FireVec(FVector2f(Rng.FRandRange(8.f, FieldW - 8.f), -6.f), FVector2f(Rng.FRandRange(-0.3f, 0.3f), 0.9f), EBulletShape::Fire, FLinearColor(1.f, 0.7f, 0.3f));
		E.Gravity = FVector2f(0.f, 0.015f);
	}
}

void ADanmakuWorld::S6_TortoisePart(int32 T, float K)
{
	// a heavy shell: slow rings that ricochet once off every wall
	if (Every(T, FMath::RoundToInt(60 / K), 0))
	{
		const int32 N = Cnt(20);
		const float Base = Rng.FRandRange(0.f, 360.f);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Boss.Pos, 1.15f, Base + 360.f * i / N, EBulletShape::Water, ColTortoise);
			B.WallMask = 1 | 2 | 4;
			B.Bounces = 1;
		}
	}
	if (Every(T, FMath::RoundToInt(130 / K), 65)) Ring(Boss.Pos, Cnt(10), 0.85f, Rng.FRandRange(0.f, 36.f), EBulletShape::Large, FLinearColor(0.3f, 0.75f, 0.55f));
}

// ---------- phases ----------

namespace
{
	// how hard each beast goes at each difficulty
	float BeastK(int32 Difficulty)
	{
		static const float K[4] = { 0.72f, 0.85f, 1.f, 1.18f };
		return K[FMath::Clamp(Difficulty, 0, 3)];
	}
}

void ADanmakuWorld::S6_NonSpell1(int32 T)
{
	if (Every(T, 6, 0)) Ring(Boss.Pos, 8, 2.2f, T * 3.1f, EBulletShape::Medium, ColGoldS6);
	if (Every(T, 11, 5)) Ring(Boss.Pos, 6, 1.5f, -T * 2.2f, EBulletShape::Small, ColDragon);
	if (Every(T, 45, 20)) Fan(Boss.Pos, 9, 50.f, 3.f, AimAngle(Boss.Pos), EBulletShape::Rice, ColDragon);
	if (Every(T, 180, 90)) WanderBoss();
}

void ADanmakuWorld::S6_Dragon(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 95.f));
	S6_DragonPart(T, 0.95f * BeastK(Difficulty));
}

void ADanmakuWorld::S6_NonSpell2(int32 T)
{
	if (Every(T, 24, 0)) Ring(Boss.Pos, 28, 1.9f, Rng.FRandRange(0.f, 15.f), EBulletShape::Crescent, ColTiger);
	if (Every(T, 24, 12)) Fan(Boss.Pos, 7, 36.f, 3.2f, AimAngle(Boss.Pos), EBulletShape::Comet, ColGoldS6);
	if (Every(T, 8, 0))
	{
		const bool bLeft = (T / 8) % 2 == 0;
		Fire(FVector2f(bLeft ? -6.f : FieldW + 6.f, Rng.FRandRange(60.f, FieldH * 0.7f)), 2.f, bLeft ? 5.f : 175.f, EBulletShape::Small, FLinearColor(0.8f, 0.9f, 1.f));
	}
	if (Every(T, 170, 85)) WanderBoss();
}

void ADanmakuWorld::S6_Tiger(int32 T)
{
	// the tiger prowls from side to side faster than the other beasts
	PlaceBoss(FVector2f(FieldW * 0.5f + FMath::Sin(T * 0.03f) * 100.f, 100.f + FMath::Abs(FMath::Sin(T * 0.03f)) * 20.f));
	S6_TigerPart(T, 1.65f * BeastK(Difficulty));
	// Lunatic: the tiger also pounces with a fan of claws between strikes
	if (Difficulty >= 3 && Every(T, 70, 35)) Fan(Boss.Pos, 5, 32.f, 3.f, AimAngle(Boss.Pos), EBulletShape::Comet, ColTiger);
}

void ADanmakuWorld::S6_NonSpell3(int32 T)
{
	if (Every(T, 4, 0))
	{
		for (int32 i = 0; i < 4; ++i) Fire(Boss.Pos, 1.8f, -T * 2.7f + 90.f * i, EBulletShape::Fire, ColBird);
	}
	if (Every(T, 60, 30)) Ring(Boss.Pos, 24, 1.4f, 0.f, EBulletShape::Large, ColGoldS6);
	if (Every(T, 60, 0)) Fan(Boss.Pos, 5, 24.f, 3.2f, AimAngle(Boss.Pos), EBulletShape::Fire, FLinearColor(1.f, 0.75f, 0.3f));
	if (Every(T, 160, 80)) WanderBoss();
}

void ADanmakuWorld::S6_Phoenix(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 110.f + FMath::Sin(T * 0.03f) * 10.f));
	S6_PhoenixPart(T, 1.7f * BeastK(Difficulty));
	if (Every(T, 30, 15)) Fan(Boss.Pos, Cnt(5), 36.f, 2.6f, AimAngle(Boss.Pos) + ((T / 30) % 2 ? 4.5f : -4.5f), EBulletShape::Fire, ColBird);
}

void ADanmakuWorld::S6_Tortoise(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 100.f));
	S6_TortoisePart(T, 1.7f * BeastK(Difficulty));
	// the shell also sheds slow aimed scales between rings
	if (Every(T, 40, 20)) Fan(Boss.Pos, Cnt(5), 40.f, 1.6f, AimAngle(Boss.Pos), EBulletShape::Water, ColTortoise);
}

void ADanmakuWorld::S6_NonSpell4(int32 T)
{
	S6_DragonPart(T, 0.7f);
	S6_TigerPart(T, 0.6f);
	if (Every(T, 150, 75)) WanderBoss();
}

void ADanmakuWorld::S6_Final(int32 T)
{
	PatrolBoss(T);
	const float HP = FMath::Clamp(Boss.HP / FMath::Max(1.f, Boss.MaxHP), 0.f, 1.f);
	const int32 Beasts = HP > 0.75f ? 1 : HP > 0.5f ? 2 : HP > 0.25f ? 3 : 4;
	if (Beasts != Boss.Stage6Beasts)
	{
		// announce each new beast joining the card
		static const TCHAR* Names[4] = { TEXT("青龍"), TEXT("白虎"), TEXT("朱雀"), TEXT("玄武") };
		static const FLinearColor Cols[4] = { ColDragon, ColTiger, ColBird, ColTortoise };
		Boss.Stage6Beasts = Beasts;
		if (Beasts > 1)
		{
			ShowMessage(FString(Tr(Names[Beasts - 1])) + TEXT("　") + Tr(TEXT("降臨")), FString(), 90);
			AddParticle(Boss.Pos, FVector2f::ZeroVector, 420.f, Cols[Beasts - 1], 40, true);
			Sfx(ESfx::Spell);
		}
	}
	// all four beasts at once already stack up, so the difficulty boost counts half here
	const float K = (Beasts == 1 ? 1.f : Beasts == 2 ? 0.75f : Beasts == 3 ? 0.62f : 0.55f) * (1.f + (BeastK(Difficulty) - 1.f) * 0.5f);
	S6_DragonPart(T, K);
	if (Beasts >= 2) S6_TigerPart(T, K);
	if (Beasts >= 3) S6_PhoenixPart(T, K);
	if (Beasts >= 4) S6_TortoisePart(T, K);
}
