// Stage 3 boss: 嵐, the hot-headed weather master.
// 晴 rotating triple rings, 雨 water flung up that rains back down, 雷 telegraphed lightning that sets the ground on fire,
// 霧 a fog that keeps closing in while plain triple rings fly through it.

#include "DanmakuWorld.h"

using namespace Danmaku;

namespace
{
	const FLinearColor ColSky1(0.6f, 0.86f, 1.f);
	const FLinearColor ColSky2(0.7f, 1.f, 0.86f);
	const FLinearColor ColSky3(1.f, 0.92f, 0.6f);
	const FLinearColor ColRain(0.6f, 0.82f, 1.f);
	const FLinearColor ColBolt(1.f, 0.95f, 0.6f);
	const FLinearColor ColFog1(0.73f, 0.8f, 1.f);
	const FLinearColor ColFog2(0.86f, 0.92f, 1.f);
	const FLinearColor ColFog3(1.f, 1.f, 1.f);
}

void ADanmakuWorld::S3_NonSpell1(int32 T)
{
	if (Every(T, 7, 0))
	{
		const int32 N = Cnt(6);
		for (int32 i = 0; i < N; ++i) Fire(Boss.Pos, 2.1f, T * 3.7f + 360.f * i / N, EBulletShape::Water, ColRain);
	}
	if (Every(T, 6, 3))
	{
		FBullet& D = FireVec(FVector2f(Rng.FRandRange(6.f, FieldW - 6.f), -6.f), FVector2f(0.f, Rng.FRandRange(1.8f, 2.4f)), EBulletShape::Water, ColRain);
		D.Gravity = FVector2f(0.f, 0.008f);
	}
	if (Every(T, 45, 22)) Fan(Boss.Pos, 7, 40.f, 3.f, AimAngle(Boss.Pos), EBulletShape::Medium, ColSky3);
	if (Every(T, 180, 90)) WanderBoss();
}

// 晴符「晴空萬里」: every half second three rings spiral outward together, alternating direction
void ADanmakuWorld::S3_Sunny(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 110.f + FMath::Sin(T * 0.02f) * 8.f));
	if (!Every(T, 30, 0)) return;
	static const int32 Counts[3] = { 18, 24, 30 };
	static const float Start[3] = { 7.f, 9.f, 11.f };
	static const float Rad[3] = { 0.95f, 1.05f, 1.15f };
	const FLinearColor Cols[3] = { ColSky1, ColSky2, ColSky3 };
	const int32 Volley = T / FMath::Max(1, FMath::RoundToInt(30 * GetDifficultyInfo(Difficulty).IntervalScale));
	const float AngSpeed = (Volley % 2 == 0 ? 1.f : -1.f) * 0.143f;
	const float Offset = Rng.FRandRange(0.f, 360.f);
	for (int32 r = 0; r < 3; ++r)
	{
		const int32 N = Cnt(Counts[r]);
		for (int32 i = 0; i < N; ++i)
		{
			FirePolar(Boss.Pos, Offset + r * 4.6f + 360.f * i / N, Start[r], Rad[r], AngSpeed, r == 2 ? EBulletShape::Small : EBulletShape::Medium, Cols[r]);
		}
	}
}

void ADanmakuWorld::S3_NonSpell2(int32 T)
{
	if (Every(T, 3, 0))
	{
		FBullet& D = FireVec(FVector2f(Rng.FRandRange(6.f, FieldW - 6.f), -6.f), FVector2f(Rng.FRandRange(-0.3f, 0.1f), Rng.FRandRange(1.6f, 2.3f)), EBulletShape::Water, ColRain);
		D.Gravity = FVector2f(0.f, 0.01f);
	}
	if (Every(T, 54, 0)) Ring(Boss.Pos, 24, 1.8f, Rng.FRandRange(0.f, 30.f), EBulletShape::Medium, ColSky1);
	if (Every(T, 54, 27)) Fan(Boss.Pos, 9, 48.f, 2.6f, AimAngle(Boss.Pos), EBulletShape::Rice, ColSky3);
	if (Every(T, 160, 80)) WanderBoss();
}

// 雨符「雨幕延綿」: the boss flings water upward, it arcs and rains back down
void ADanmakuWorld::S3_Rain(int32 T)
{
	PatrolBoss(T);
	if (!Every(T, 6, 0)) return;
	const int32 N = Cnt(Rng.RandRange(5, 11));
	for (int32 i = 0; i < N; ++i)
	{
		FBullet& D = FireVec(Boss.Pos + FVector2f(Rng.FRandRange(-6.f, 6.f), 6.f), Dir(Rng.FRandRange(238.f, 302.f)) * Rng.FRandRange(3.3f, 4.6f),
			EBulletShape::Water, ColRain);
		D.Gravity = FVector2f(0.f, Rng.FRandRange(0.05f, 0.072f));
	}
}

// 雷符「雷暴中心」: a bolt is marked under the player every second and strikes two seconds later; fire bursts from the ground
void ADanmakuWorld::S3_Storm(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 60, 0))
	{
		const float X = FMath::Clamp(Player.Pos.X + Rng.FRandRange(-10.f, 10.f), 20.f, FieldW - 20.f);
		AddBeam(FVector2f(X, FieldH), -90.f, Rng.FRandRange(16.f, 22.f), 120, 4, 20, ColBolt, 1);
	}
}

// 霧符「雲裡霧裡」: vision shrinks for most of the card, while triple rings keep coming
void ADanmakuWorld::S3_Fog(int32 T)
{
	PatrolBoss(T);
	const float A = FMath::Clamp(T / (48.f * 60.f), 0.f, 1.f);
	FogRadius = FMath::Lerp(230.f, 58.f, A);
	if (Every(T, 15, 0))
	{
		const FLinearColor Cols[3] = { ColFog1, ColFog2, ColFog3 };
		TripleRings(Boss.Pos, Rng.FRandRange(0.f, 360.f), 0.55f, EBulletShape::Small, Cols);
	}
}
