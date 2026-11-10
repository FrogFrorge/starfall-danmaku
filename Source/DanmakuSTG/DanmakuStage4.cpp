// Stage 4 boss: 墨竹, the formal scholar of the four gentlemen (plum, orchid, bamboo, chrysanthemum).

#include "DanmakuWorld.h"

using namespace Danmaku;

namespace
{
	const FLinearColor ColPlum(1.f, 0.62f, 0.8f);
	const FLinearColor ColOrchid(0.85f, 0.72f, 1.f);
	const FLinearColor ColBamboo(0.6f, 1.f, 0.7f);
	FLinearColor ChrysTone(FRandomStream& R) { return FLinearColor(1.f, R.FRandRange(0.75f, 0.95f), R.FRandRange(0.25f, 0.5f)); }
	int32 D4(int32 D) { return FMath::Clamp(D, 0, 3); }
}

void ADanmakuWorld::S4_NonSpell1(int32 T)
{
	if (Every(T, 28, 0))
	{
		for (int32 k = 0; k < Cnt(4); ++k)
		{
			SpawnPlumSeed(Boss.Pos + Dir(Rng.FRandRange(0.f, 360.f)) * Rng.FRandRange(30.f, 90.f), Rng.RandRange(20, 30), ColPlum);
		}
	}
	if (Every(T, 40, 20)) Fan(Boss.Pos, 7, 44.f, 2.7f, AimAngle(Boss.Pos), EBulletShape::Rice, ColBamboo);
	if (Every(T, 9, 4))
	{
		FBullet& B = Fire(Boss.Pos, 1.6f, T * 6.1f, EBulletShape::Rice, ColOrchid);
		B.AngVel = 0.2f;
	}
	if (Every(T, 180, 90)) WanderBoss();
}

// 梅符「梅花盛開」: seeds appear around the boss and burst into five petals
void ADanmakuWorld::S4_Plum(int32 T)
{
	PatrolBoss(T);
	if (!Every(T, 14, 0)) return;
	static const int32 Counts[4] = { 1, 2, 3, 5 };
	for (int32 k = 0; k < Counts[D4(Difficulty)]; ++k)
	{
		const FVector2f P = Boss.Pos + Dir(Rng.FRandRange(0.f, 360.f)) * Rng.FRandRange(42.f, 116.f);
		SpawnPlumSeed(FVector2f(FMath::Clamp(P.X, 17.f, FieldW - 17.f), FMath::Clamp(P.Y, 27.f, FieldH * 0.46f)), Rng.RandRange(22, 34), ColPlum, 5, Rng.FRandRange(1.1f, 1.45f));
	}
}

void ADanmakuWorld::S4_NonSpell2(int32 T)
{
	if (Every(T, 4, 0))
	{
		for (float Off : { 0.f, 180.f })
		{
			FBullet& B = Fire(Boss.Pos, 1.8f, T * 7.3f + Off, EBulletShape::Rice, ColOrchid);
			B.AngVel = 0.25f;
		}
	}
	if (Every(T, 36, 0)) Ring(Boss.Pos, 20, 1.6f, Rng.FRandRange(0.f, 30.f), EBulletShape::Sakura, ColPlum);
	if (Every(T, 72, 36)) Fan(Boss.Pos, 5, 24.f, 3.f, AimAngle(Boss.Pos), EBulletShape::Medium, ColBamboo);
	if (Every(T, 170, 85)) WanderBoss();
}

// 蘭符「蘭花路徑」: a fast spiral of orchid petals that start to curl after a moment
void ADanmakuWorld::S4_Orchid(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 105.f));
	if (Every(T, 54, 27)) Ring(Boss.Pos, Cnt(18), 1.5f, AimAngle(Boss.Pos), EBulletShape::Rice, FLinearColor(0.75f, 0.6f, 1.f));
	if (!Every(T, 4, 0)) return;
	static const int32 Ways[4] = { 1, 2, 3, 6 };
	const int32 N = Ways[D4(Difficulty)];
	const float Base = T * 38.7f;
	for (int32 i = 0; i < N; ++i)
	{
		FBullet& B = Fire(Boss.Pos + FVector2f(0.f, 3.f), Rng.FRandRange(1.05f, 1.6f), Base + 360.f * i / N, EBulletShape::Rice, ColOrchid);
		B.Behavior = BehCurveAfter;
		B.Timer = Rng.RandRange(24, 42);
		B.Param2 = Rng.FRandRange(0.1f, 0.2f) * (Rng.FRand() < 0.5f ? -1.f : 1.f);
	}
}

// 竹符「竹林之勢」: bamboo shoots up from the ground after a warning and sheds leaves from the top
void ADanmakuWorld::S4_Bamboo(int32 T)
{
	PatrolBoss(T);
	if (Every(T, Difficulty >= 3 ? 44 : 66, 0))
	{
		const int32 Cols = Difficulty >= 2 ? 2 : 1;
		for (int32 i = 0; i < Cols; ++i)
		{
			AddBeam(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), FieldH), -90.f, 8.f, 120, 30, 60, ColBamboo, 2);
		}
	}
}

// 菊符「菊花之哀」: chrysanthemum bursts that get denser and faster as the boss weakens
void ADanmakuWorld::S4_Chrys(int32 T)
{
	PatrolBoss(T);
	const float Lost = 1.f - FMath::Clamp(Boss.HP / FMath::Max(1.f, Boss.MaxHP), 0.f, 1.f);
	const int32 D = D4(Difficulty);
	const int32 Burst = (5 + D * 3) + FMath::FloorToInt(Lost * 9.f);
	const int32 Interval = FMath::Max(6, 12 - FMath::FloorToInt(Lost * 6.f));
	if (T % Interval != 0) return;
	for (int32 i = 0; i < Burst; ++i)
	{
		FBullet& B = Fire(Boss.Pos + FVector2f(0.f, 3.f), Rng.FRandRange(1.3f, 2.05f), Rng.FRandRange(0.f, 360.f), EBulletShape::Chrys, ChrysTone(Rng));
		B.Spin = Rng.FRandRange(-5.f, 5.f);
	}
}

// 四君子「君子之約」 (Normal+, survival): chrysanthemums roam the field, bouncing between plum seeds and bamboo
void ADanmakuWorld::S4_Final(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, FieldH * 0.45f));
	static const int32 Interval[4] = { 99999, 16 * 60, 12 * 60, 7 * 60 };
	static const int32 Start[4] = { 3, 3, 4, 5 };
	static const int32 Plums[4] = { 0, 2, 3, 4 };
	const int32 D = D4(Difficulty);
	auto Spawn = [this, D](float Angle)
	{
		FBullet& C = FireVec(Boss.Pos, Dir(Angle) * 1.5f, EBulletShape::Chrys, FLinearColor(1.f, 0.85f, 0.35f));
		C.Behavior = BehFinalChrys;
		C.Param = 1;
		C.Param2 = 1.5f;
		C.Timer = 30;
		C.Timer2 = Plums[D];
		C.Scale = 1.7f;
		C.Radius *= 1.7f;
		C.Spin = 3.f;
		C.bBombProof = true;
	};
	if (T == 1)
	{
		for (int32 i = 0; i < Start[D]; ++i) Spawn(90.f + 360.f * i / Start[D]);
	}
	else if (T % Interval[D] == 0)
	{
		Spawn(Rng.FRandRange(0.f, 360.f));
	}
}
