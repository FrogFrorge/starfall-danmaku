// Stage 5 boss: 玄, the cryptic sorcerer of the five elements.
// One card per element, then 「相生相剋」 cycles through all five, each element feeding the next.

#include "DanmakuWorld.h"

using namespace Danmaku;

namespace
{
	const FLinearColor ElemCol[5] = {
		FLinearColor(0.4f, 1.f, 0.45f),   // wood
		FLinearColor(1.f, 0.45f, 0.25f),  // fire
		FLinearColor(0.95f, 0.8f, 0.35f), // earth
		FLinearColor(0.92f, 0.95f, 1.f),  // metal
		FLinearColor(0.35f, 0.6f, 1.f),   // water
	};
}

void ADanmakuWorld::S5_NonSpell1(int32 T)
{
	// five-coloured five-armed spiral with a slower counter-spiral, and aimed blades
	if (Every(T, 5, 0))
	{
		for (int32 i = 0; i < 5; ++i) Fire(Boss.Pos, 1.9f, T * 2.3f + 72.f * i, EBulletShape::Medium, ElemCol[i]);
	}
	if (Every(T, 9, 4))
	{
		for (int32 i = 0; i < 5; ++i) Fire(Boss.Pos, 1.4f, -T * 1.7f + 72.f * i + 36.f, EBulletShape::Small, ElemCol[(i + 2) % 5]);
	}
	if (Every(T, 45, 20)) Fan(Boss.Pos, 7, 42.f, 3.f, AimAngle(Boss.Pos), EBulletShape::Rice, ElemCol[3]);
	if (Every(T, 180, 90)) WanderBoss();
}

// 木符「萬木生長」: sprouting seeds plus two growing vines
void ADanmakuWorld::S5_Wood(int32 T)
{
	PatrolBoss(T);
	ElementVolley(0, T, 1.2f);
	if (Every(T, 48, 24)) Fan(Boss.Pos, Cnt(7), 50.f, 2.5f, AimAngle(Boss.Pos), EBulletShape::Rice, FLinearColor(0.55f, 0.95f, 0.4f));
	if (Every(T, 6, 0))
	{
		for (float Off : { 0.f, 120.f, 240.f })
		{
			FBullet& V = Fire(Boss.Pos, 1.7f, T * 2.2f + Off, EBulletShape::Rice, FLinearColor(0.55f, 0.95f, 0.4f));
			V.AngVel = 0.35f;
			V.AngVelDecay = 0.985f;
		}
	}
}

// 火符「烈火燎原」: a fountain of fire that rains down, rings of flame spreading from the boss
void ADanmakuWorld::S5_Fire(int32 T)
{
	PatrolBoss(T);
	ElementVolley(1, T, 1.7f);
	if (Every(T, 50, 25)) Fan(Boss.Pos, Cnt(5), 30.f, 2.4f, AimAngle(Boss.Pos), EBulletShape::Fire, FLinearColor(1.f, 0.75f, 0.3f));
	if (Every(T, 32, 0))
	{
		// a ring of flames that speeds up as it spreads; every other ring is offset by half a step
		const int32 N = Cnt(24);
		const float Base = (T / 32) % 2 ? 180.f / N : 0.f;
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Boss.Pos, 0.9f, Base + 360.f * i / N, EBulletShape::Fire, FLinearColor(1.f, 0.55f + 0.2f * (i % 2), 0.25f));
			B.Accel = 0.02f;
			B.MaxSpeed = 2.4f;
		}
	}
	if (Every(T, 120, 60))
	{
		// two rings that curl in opposite directions
		for (float Sgn : { -1.f, 1.f })
		{
			const int32 N = Cnt(16);
			for (int32 i = 0; i < N; ++i)
			{
				FBullet& B = Fire(Boss.Pos, 1.5f, 360.f * i / N, EBulletShape::Fire, FLinearColor(1.f, 0.4f, 0.2f));
				B.AngVel = Sgn * 0.45f;
				B.AngVelDecay = 0.99f;
			}
		}
	}
}

void ADanmakuWorld::S5_NonSpell2(int32 T)
{
	if (Every(T, 32, 0)) Ring(Boss.Pos, 26, 1.8f, Rng.FRandRange(0.f, 15.f), EBulletShape::Medium, ElemCol[(T / 32) % 5]);
	if (Every(T, 32, 16)) Fan(Boss.Pos, 9, 64.f, 2.5f, AimAngle(Boss.Pos), EBulletShape::Rice, ElemCol[(T / 32 + 2) % 5]);
	if (Every(T, 64, 40)) Ring(Boss.Pos, 14, 1.2f, AimAngle(Boss.Pos), EBulletShape::Large, ElemCol[(T / 64 + 4) % 5]);
	if (Every(T, 160, 80)) WanderBoss();
}

// 土符「山崩地裂」: walls of boulders fall with a gap to slip through, while rings spread from the boss
void ADanmakuWorld::S5_Earth(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 90.f));
	// walls come faster and the rings get fuller with each difficulty; Hard and up also hurl aimed boulders
	static const float Walls[4] = { 1.1f, 1.25f, 1.5f, 1.9f };
	const int32 D = FMath::Clamp(Difficulty, 0, 3);
	ElementVolley(2, T, Walls[D]);
	if (Every(T, 36, 0)) Ring(Boss.Pos, Cnt(10 + 2 * D), 1.6f, T * 1.7f, EBulletShape::Medium, ElemCol[2]);
	if (D >= 2 && Every(T, 64, 32)) Fan(Boss.Pos, D >= 3 ? 5 : 3, D >= 3 ? 48.f : 30.f, 2.1f, AimAngle(Boss.Pos), EBulletShape::Large, FLinearColor(0.95f, 0.78f, 0.35f));
}

// 金符「金戈鐵刃」: blades stop and lunge, some beside the player and some anywhere; swords fall on both flanks
void ADanmakuWorld::S5_Metal(int32 T)
{
	PatrolBoss(T);
	ElementVolley(3, T, 0.9f);
	if (Every(T, 50, 0))
	{
		// blades fan out, slow down, then dash: at the player's left, at the player's right, or somewhere random
		const int32 N = Cnt(16);
		const float Base = Rng.FRandRange(0.f, 360.f);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& Bl = FireVec(Boss.Pos, Dir(Base + 360.f * i / N) * 3.f, EBulletShape::Crescent, FLinearColor(0.92f, 0.95f, 1.f));
			Bl.Behavior = BehPauseDash;
			Bl.Timer = 36;
			Bl.Param = i % 3 == 0 ? -64 : (i % 3 == 1 ? 64 : 9999);   // 9999 = random direction
			Bl.Param2 = 2.9f;
		}
	}
	if (Every(T, 18, 9))
	{
		// swords falling on both sides of the player, never on top of them
		for (float Sgn : { -1.f, 1.f })
		{
			const float X = FMath::Clamp(Player.Pos.X + Sgn * Rng.FRandRange(34.f, 90.f), 8.f, FieldW - 8.f);
			FBullet& S = Fire(FVector2f(X, -8.f), 1.2f, 90.f, EBulletShape::Crescent, FLinearColor(1.f, 0.9f, 0.6f));
			S.Accel = 0.04f;
			S.MaxSpeed = 3.6f;
		}
	}
}

void ADanmakuWorld::S5_NonSpell3(int32 T)
{
	ElementVolley((T / 240) % 5, T, 1.f);
	if (Every(T, 40, 20)) Ring(Boss.Pos, 20, 2.f, Rng.FRandRange(0.f, 20.f), EBulletShape::Small, ElemCol[(T / 240 + 1) % 5]);
	if (Every(T, 80, 0)) Fan(Boss.Pos, 5, 30.f, 3.f, AimAngle(Boss.Pos), EBulletShape::Large, ElemCol[(T / 240 + 3) % 5]);
	if (Every(T, 150, 75)) WanderBoss();
}

// 水符「流水無形」: two winding rivers and swaying rings of droplets
void ADanmakuWorld::S5_Water(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 100.f));
	static const float Rivers[4] = { 0.8f, 0.9f, 1.f, 1.15f };
	const int32 D = FMath::Clamp(Difficulty, 0, 3);
	ElementVolley(4, T, Rivers[D]);
	if (Every(T, 70 - 5 * D, 35 - 2 * D))
	{
		const int32 N = Cnt(18);
		const float Base = Rng.FRandRange(0.f, 360.f);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& W = Fire(Boss.Pos, 1.3f, Base + 360.f * i / N, EBulletShape::Water, FLinearColor(0.55f, 0.8f, 1.f));
			W.WaveAmp = 0.6f;
			W.WaveFreq = 0.05f;
			W.WavePhase = i * 0.8f;
		}
	}
}

// 五行「相生相剋」: every 5 s the next element takes over while the previous one fades out
void ADanmakuWorld::S5_Cycle(int32 T)
{
	PatrolBoss(T);
	const int32 Elem = (T / 300) % 5;
	const float Into = float(T % 300) / 300.f;
	static const float Strength[4] = { 0.8f, 0.9f, 1.f, 1.25f };
	const float S = Strength[FMath::Clamp(Difficulty, 0, 3)];
	ElementVolley(Elem, T, (0.8f + 0.3f * Into) * S);
	ElementVolley(Elem + 4, T, 0.5f * (1.f - Into) * S);          // the element that just generated it
	if (Every(T, 300, 0))
	{
		Ring(Boss.Pos, 30, 1.5f, Rng.FRandRange(0.f, 12.f), EBulletShape::Large, ElemCol[Elem]);
		AddParticle(Boss.Pos, FVector2f::ZeroVector, 260.f, ElemCol[Elem], 30, true);
	}
}
