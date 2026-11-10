// Stage 2 boss: 千秋, the painter of the four seasons.
// 春 polar petal rings that drift down, 夏 bouncing "sunbeam" streams, 秋 maple leaves blown in from the side,
// 冬 a blizzard that thickens over time.

#include "DanmakuWorld.h"

using namespace Danmaku;

namespace
{
	const FLinearColor ColSakura(1.f, 0.62f, 0.8f);
	const FLinearColor ColSakuraDeep(1.f, 0.42f, 0.68f);
	const FLinearColor ColLeaf(0.5f, 0.95f, 0.5f);
	const FLinearColor ColSun(1.f, 0.85f, 0.3f);
	const FLinearColor ColSunHot(1.f, 0.55f, 0.2f);
	const FLinearColor ColAcorn(0.95f, 0.6f, 0.3f);
	const FLinearColor ColSnow(0.85f, 0.93f, 1.f);
	const FLinearColor ColIce(0.5f, 0.75f, 1.f);
}

FBullet& ADanmakuWorld::FirePolar(const FVector2f& Center, float Angle, float StartRadius, float RadSpeed, float AngSpeed, EBulletShape Shape, const FLinearColor& Color)
{
	FBullet& B = Fire(Center + Dir(Angle) * StartRadius, 0.f, Angle, Shape, Color);
	B.Motion = 1;
	B.Center = Center;
	B.PolarAngle = Angle;
	B.PolarRadius = StartRadius;
	B.PolarRadSpeed = RadSpeed * GetDifficultyInfo(Difficulty).SpeedMul;
	B.PolarAngSpeed = AngSpeed;
	return B;
}

FBullet& ADanmakuWorld::FireVec(const FVector2f& Pos, const FVector2f& Vel, EBulletShape Shape, const FLinearColor& Color)
{
	FBullet& B = Fire(Pos, 0.f, AngleOf(Vel), Shape, Color);
	B.Motion = 2;
	B.Vel = Vel;
	return B;
}

// ---------- non-spell 1: double petal spiral + aimed fan ----------

void ADanmakuWorld::S2_NonSpell1(int32 T)
{
	if (Every(T, 6, 0))
	{
		const int32 Arms = Cnt(6);
		for (int32 i = 0; i < Arms; ++i)
		{
			Fire(Boss.Pos, 2.1f, T * 3.1f + 360.f * i / Arms, EBulletShape::Sakura, ColSakura);
			Fire(Boss.Pos, 1.7f, -T * 2.3f + 360.f * i / Arms + 36.f, EBulletShape::Sakura, ColSakuraDeep);
		}
	}
	if (Every(T, 56, 28)) Fan(Boss.Pos, 7, 40.f, 3.2f, AimAngle(Boss.Pos), EBulletShape::Rice, ColLeaf);
	if (Every(T, 200, 100)) WanderBoss();
}

// ---------- 春符「春風催生」 : expanding, turning petal rings whose centre sinks toward the player ----------

void ADanmakuWorld::S2_Spring(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 30, 0))
	{
		const int32 Wave = T / FMath::Max(1, FMath::RoundToInt(30 * GetDifficultyInfo(Difficulty).IntervalScale));
		const float Sgn = Wave % 2 == 0 ? 1.f : -1.f;
		const int32 N = Cnt(16);
		const float Base = Rng.FRandRange(0.f, 360.f);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = FirePolar(Boss.Pos, Base + 360.f * i / N, 8.f, 0.9f, 0.29f * Sgn, EBulletShape::Sakura,
				Sgn > 0.f ? ColSakura : ColSakuraDeep);
			B.CenterVel = FVector2f(0.f, 0.46f);
		}
	}
	// petals drifting down through the field
	if (Every(T, 14, 7))
	{
		FBullet& B = FireVec(FVector2f(Rng.FRandRange(10.f, FieldW - 10.f), -8.f), FVector2f(Rng.FRandRange(-0.2f, 0.2f), Rng.FRandRange(0.9f, 1.3f)),
			EBulletShape::Sakura, ColSakura);
		B.WaveAmp = 0.6f;
		B.WaveFreq = 0.05f;
		B.WavePhase = Rng.FRandRange(0.f, 6.28f);
		B.Spin = Rng.FRandRange(-3.f, 3.f);
	}
}

// ---------- non-spell 2: swaying leaf rings ----------

void ADanmakuWorld::S2_NonSpell2(int32 T)
{
	if (Every(T, 32, 0))
	{
		const int32 N = Cnt(24);
		const float Base = Rng.FRandRange(0.f, 360.f);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Boss.Pos, 1.9f, Base + 360.f * i / N, EBulletShape::Rice, i % 2 ? ColLeaf : ColSun);
			B.WaveAmp = 0.7f;
			B.WaveFreq = 0.06f;
			B.WavePhase = i * 0.7f;
		}
	}
	if (Every(T, 48, 24)) Fan(Boss.Pos, 9, 56.f, 2.8f, AimAngle(Boss.Pos), EBulletShape::Sakura, ColSakuraDeep);
	if (Every(T, 170, 85)) WanderBoss();
}

// ---------- 夏符「夏日鋒芒」 : streams of sun orbs that look like lasers and ricochet off every wall ----------

void ADanmakuWorld::S2_Summer(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 130.f));
	// straight sun rays that ricochet four times, a burst every 1.5 s (Normal ~2 s)
	const int32 Cyc = FMath::Max(30, FMath::RoundToInt(90 * GetDifficultyInfo(Difficulty).IntervalScale));
	const int32 K = T % Cyc;
	const int32 Cycle = T / Cyc;
	const int32 Streams = Cnt(10);
	const int32 PerStream = 11;
	if (K < PerStream * 2 && K % 2 == 0)
	{
		const float Base = Cycle * 23.f + (Cycle % 2 ? 180.f / Streams : 0.f);
		for (int32 s = 0; s < Streams; ++s)
		{
			FBullet& B = Fire(Boss.Pos, 2.3f, Base + 360.f * s / Streams, EBulletShape::Medium, K < PerStream ? ColSun : ColSunHot);
			B.WallMask = 1 | 2 | 4;
			B.Bounces = 4;
			B.bDieAfterBounces = true;
		}
	}
}

// ---------- 秋符「秋風落葉」 : maple leaves blown in from the right, acorns dropping from above ----------

void ADanmakuWorld::S2_Autumn(int32 T)
{
	PatrolBoss(T);
	// Lunatic blows an extra leaf in every other gust
	if (Every(T, 4, 0) || (Difficulty >= 3 && Every(T, 8, 2)))
	{
		const float A = Rng.FRandRange(110.f, 170.f);
		FBullet& B = FireVec(FVector2f(FieldW + 8.f, Rng.FRandRange(0.f, FieldH * 0.55f)), Dir(A) * Rng.FRandRange(1.35f, 2.05f),
			EBulletShape::Maple, FLinearColor(1.f, Rng.FRandRange(0.3f, 0.65f), 0.15f));
		B.WaveAmp = 0.35f;
		B.WaveFreq = 0.04f;
		B.WavePhase = Rng.FRandRange(0.f, 6.28f);
		B.Spin = Rng.FRandRange(-4.f, 4.f);
	}
	if (Every(T, 40, 20))
	{
		const int32 N = Cnt(8);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = FireVec(FVector2f(Rng.FRandRange(10.f, FieldW - 10.f), -6.f), FVector2f(0.f, Rng.FRandRange(1.6f, 2.4f)), EBulletShape::Medium, ColAcorn);
			B.KillBelowY = FieldH / 3.f;
		}
	}
}

// ---------- 冬符「冬日暴雪」 : snowfall that grows heavier, big flakes fall faster ----------

void ADanmakuWorld::S2_Winter(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 5, 0))
	{
		const int32 N = Cnt(1 + FMath::Min(T, 2400) / 800);
		for (int32 i = 0; i < N; ++i)
		{
			const float S = Rng.FRandRange(0.7f, 1.45f);
			FBullet& B = FireVec(FVector2f(Rng.FRandRange(4.f, FieldW - 4.f), -10.f), FVector2f(Rng.FRandRange(-0.25f, 0.25f), 0.55f + S * 0.65f),
				EBulletShape::Snow, S > 1.2f ? ColIce : ColSnow);
			B.Scale = S;
			B.Radius *= S;
			B.WaveAmp = 0.45f;
			B.WaveFreq = 0.03f;
			B.WavePhase = Rng.FRandRange(0.f, 6.28f);
			B.Spin = Rng.FRandRange(-2.f, 2.f);
		}
	}
}
