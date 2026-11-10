// Scripted bullet behaviours and pattern helpers shared by stages 3 - Extra.

#include "DanmakuWorld.h"

using namespace Danmaku;

namespace
{
	int32 WallSide(FVector2f& P, float R, float Top = 0.f)
	{
		// 0 left, 1 right, 2 top, 3 bottom, -1 none; clamps the position back inside
		int32 Side = -1;
		if (P.X < R)               { P.X = R; Side = 0; }
		else if (P.X > FieldW - R) { P.X = FieldW - R; Side = 1; }
		if (P.Y < Top + R)         { P.Y = Top + R; Side = 2; }
		else if (P.Y > FieldH - R) { P.Y = FieldH - R; Side = 3; }
		return Side;
	}
}

bool ADanmakuWorld::UpdateBehavior(FBullet& B)
{
	switch (B.Behavior)
	{
	case BehPlumSeed:
		if (--B.Timer <= 0)
		{
			const float Rot = Rng.FRandRange(0.f, 360.f);
			const FLinearColor Petal = FMath::Lerp(B.Color, FLinearColor::White, 0.25f);
			for (int32 i = 0; i < B.Param; ++i)
			{
				const float A = Rot + 360.f * i / B.Param;
				Fire(B.Pos + Dir(A) * 4.f, B.Param2 * Rng.FRandRange(0.9f, 1.12f), A, EBulletShape::Sakura, Petal);
			}
			Burst(B.Pos, 6, B.Color, 1.5f, 8.f, 16);
			B.bDead = true;
			return false;
		}
		return true;

	case BehLotusSummon:
		if (--B.Timer <= 0)
		{
			BloomLotus(B.Pos, B.Param, B.Color);
			AddParticle(B.Pos, FVector2f::ZeroVector, 50.f, B.Color, 16, true);
			B.bDead = true;
			return false;
		}
		return true;

	case BehPauseHome:
		if (B.Timer > 0)
		{
			if (--B.Timer == 0) B.Vel = FVector2f::ZeroVector;
		}
		else if (--B.Timer2 <= 0)
		{
			B.Vel = Dir(AimAngle(B.Pos)) * B.Param2;
			B.Behavior = 0;
		}
		return true;

	case BehAssassin:
	{
		// 0 fly out, 1 pause at a wall, 2 stalk the player shedding shards
		if (B.Param != 1 && B.Age > 2 && WallSide(B.Pos, 8.f, 48.f) >= 0)
		{
			B.Param = 1;
			B.Vel = FVector2f::ZeroVector;
			B.Timer = 28;
			AddParticle(B.Pos, FVector2f::ZeroVector, 40.f, B.Color, 14, true);
		}
		if (B.Param == 1)
		{
			if (--B.Timer <= 0)
			{
				B.Param = 2;
				B.Vel = Dir(AimAngle(B.Pos)) * 1.25f;
				B.Pos += B.Vel * 2.f;
				B.Timer2 = 60;
			}
		}
		else if (B.Param == 2 && --B.Timer2 <= 0)
		{
			B.Timer2 = 60;
			const float Base = Frame * 2.9f;
			const int32 N = Cnt(8);
			for (int32 i = 0; i < N; ++i)
			{
				Fire(B.Pos, 1.5f, Base + 360.f * i / N, EBulletShape::Small, FMath::Lerp(B.Color, FLinearColor::White, 0.3f));
			}
		}
		return true;
	}

	case BehFinalChrys:
	{
		// drops slow orchids along its path; on a wall: plum seeds + aim at player, next wall: bamboo + bounce straight back
		if (--B.Timer <= 0)
		{
			B.Timer = 30;
			FBullet& O = FireVec(B.Pos, FVector2f::ZeroVector, EBulletShape::Rice, FLinearColor(0.85f, 0.72f, 1.f));
			O.Behavior = BehDelayedRelease;
			O.Timer = 300;
			O.Center = B.Vel;
		}
		const int32 Side = B.Age > 2 ? WallSide(B.Pos, B.Radius) : -1;
		if (Side >= 0)
		{
			if (B.Param == 1)
			{
				for (int32 i = 0; i < B.Timer2; ++i)
				{
					const float A = 360.f * i / FMath::Max(1, B.Timer2) + Rng.FRandRange(-10.f, 10.f);
					SpawnPlumSeed(B.Pos + Dir(A) * (B.Timer2 == 1 ? 0.f : 8.f), Rng.RandRange(18, 28), FLinearColor(1.f, 0.62f, 0.8f));
				}
				B.Param = 2;
				B.Vel = Dir(AimAngle(B.Pos)) * B.Param2;
			}
			else
			{
				SpawnWallBamboo(Side, B.Pos, Difficulty >= 3 ? 60 : 120, Difficulty >= 2 ? 2 : 1);
				B.Param = 1;
				static const FVector2f Inward[4] = { { 1.f, 0.f }, { -1.f, 0.f }, { 0.f, 1.f }, { 0.f, -1.f } };
				B.Vel = Inward[Side] * B.Param2;
			}
		}
		return true;
	}

	case BehDelayedRelease:
		if (--B.Timer <= 0)
		{
			if (B.Param == 1) B.Vel = B.Center;
			else
			{
				FVector2f V = B.Center;
				const FVector2f Perp(-V.Y, V.X);
				V = (V + Perp.GetSafeNormal() * Rng.FRandRange(-0.12f, 0.12f)) * Rng.FRandRange(0.8f, 1.6f);
				B.Vel = V;
			}
			B.Behavior = 0;
		}
		return true;

	case BehCurveAfter:
		if (B.Age >= B.Timer)
		{
			B.AngVel = B.Param2;
			B.AngVelDecay = 1.f;
			B.Behavior = 0;
		}
		return true;

	case BehSplitRing:
		if (--B.Timer <= 0)
		{
			const int32 N = Cnt(FMath::Max(1, B.Param));
			const float Base = Rng.FRandRange(0.f, 360.f);
			for (int32 i = 0; i < N; ++i)
			{
				Fire(B.Pos, B.Param2, Base + 360.f * i / N, EBulletShape::Small, FMath::Lerp(B.Color, FLinearColor::White, 0.25f));
			}
			AddParticle(B.Pos, FVector2f::ZeroVector, 30.f, B.Color, 12, true);
			B.bDead = true;
			return false;
		}
		return true;

	case BehPauseDash:
	{
		// slows to a stop, then dashes: Param = sideways offset from the player in px (9999 = a random direction)
		B.Vel *= 0.93f;
		if (--B.Timer <= 0)
		{
			const FVector2f Target = Player.Pos + FVector2f(float(B.Param), 0.f);
			const float A = B.Param == 9999 ? Rng.FRandRange(0.f, 360.f) : AngleOf(Target - B.Pos);
			B.Vel = Dir(A) * B.Param2;
			B.Behavior = 0;
		}
		return true;
	}

	case BehArm:
		// harmless spark that grows, then becomes a real bullet and drifts with velocity Center
		B.Scale = FMath::Min(1.f, B.Scale + 0.6f / FMath::Max(1, B.Timer));
		if (--B.Timer <= 0)
		{
			B.bHarmless = false;
			B.Scale = 1.f;
			B.Vel = B.Center;
			B.Behavior = 0;
		}
		return true;

	case BehGroundSplit:
		// falls until it hits the ground, then breaks into Param small slow bullets (speed ~Param2) flying back up
		if (B.Pos.Y > FieldH - 4.f)
		{
			const FVector2f At(B.Pos.X, FieldH - 4.f);
			const FLinearColor C = FMath::Lerp(B.Color, FLinearColor::White, 0.2f);
			const int32 N = B.Param;
			const float Sp = B.Param2;
			B.bDead = true;
			for (int32 i = 0; i < N; ++i)
			{
				FBullet& S = Fire(At, Sp * Rng.FRandRange(0.8f, 1.2f), Rng.FRandRange(220.f, 320.f), EBulletShape::Small, C);
				S.Life = 130;
			}
			AddParticle(At, FVector2f::ZeroVector, 24.f, C, 10, true);
			return false;
		}
		return true;

	case BehWallStop:
		// flies until a wall, waits Timer frames there, then dashes at the player at speed Param2
		if (B.Param == 0 && B.Age > 2 && WallSide(B.Pos, 6.f) >= 0) { B.Param = 1; B.Vel = FVector2f::ZeroVector; }
		else if (B.Param == 1 && --B.Timer <= 0) { B.Vel = Dir(AimAngle(B.Pos)) * B.Param2; B.Behavior = 0; }
		return true;

	default:
		return true;
	}
}

void ADanmakuWorld::SpawnPlumSeed(const FVector2f& At, int32 Delay, const FLinearColor& Color, int32 Petals, float PetalSpeed)
{
	FBullet& B = Fire(At, 0.f, Rng.FRandRange(0.f, 360.f), EBulletShape::Sakura, Color);
	B.Behavior = BehPlumSeed;
	B.Timer = Delay;
	B.Param = Petals;
	B.Param2 = PetalSpeed;
	B.bHarmless = true;
	B.bBombProof = true;
	B.Scale = 0.75f;
	B.Spin = 6.f;
}

void ADanmakuWorld::BloomLotus(const FVector2f& At, int32 Number, const FLinearColor& Color)
{
	if (Number == 1 || Number == 9)
	{
		// the "terminals" bloom into three straight rings
		static const int32 Counts[3] = { 10, 14, 18 };
		static const float Speeds[3] = { 1.25f, 1.55f, 1.85f };
		for (int32 r = 0; r < 3; ++r)
		{
			const int32 N = Cnt(Counts[r]);
			const float Base = Rng.FRandRange(0.f, 360.f);
			for (int32 i = 0; i < N; ++i) Fire(At, Speeds[r], Base + 360.f * i / N, EBulletShape::Rice, Color);
		}
	}
	else
	{
		// 2-8: petals open, pause, then dart at the player
		const int32 N = Cnt(12);
		const float Base = Rng.FRandRange(0.f, 360.f);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& P = FireVec(At, Dir(Base + 360.f * i / N) * 1.5f, EBulletShape::Rice, Color);
			P.Behavior = BehPauseHome;
			P.Timer = 24;
			P.Timer2 = 18;
			P.Param2 = 1.75f;
		}
	}
}

void ADanmakuWorld::TripleRings(const FVector2f& At, float BaseAngle, float SpeedK, EBulletShape Shape, const FLinearColor* Colors)
{
	static const int32 Counts[3] = { 18, 24, 30 };
	static const float Speeds[3] = { 2.9f, 3.6f, 4.3f };
	for (int32 r = 0; r < 3; ++r)
	{
		const int32 N = Cnt(Counts[r]);
		for (int32 i = 0; i < N; ++i)
		{
			Fire(At, Speeds[r] * SpeedK, BaseAngle + r * 6.9f + 360.f * i / N, Shape, Colors[r]);
		}
	}
}

void ADanmakuWorld::SpawnWallBamboo(int32 Side, const FVector2f& Hit, int32 Warn, int32 LeafRepeat)
{
	// a bamboo stalk shoots from the wall that was hit straight across the field, then sheds leaves back
	FVector2f Origin = Hit;
	float Angle = 0.f;
	switch (Side)
	{
	case 0: Origin.X = 0.f;    Angle = 0.f;   break;
	case 1: Origin.X = FieldW; Angle = 180.f; break;
	case 2: Origin.Y = 0.f;    Angle = 90.f;  break;
	default: Origin.Y = FieldH; Angle = -90.f; break;
	}
	for (int32 i = 0; i < LeafRepeat; ++i)
	{
		AddBeam(Origin, Angle, 8.f, Warn + i * 6, 24, 50, FLinearColor(0.6f, 1.f, 0.7f), 2);
	}
}

void ADanmakuWorld::ElementVolley(int32 Element, int32 T, float Intensity)
{
	// One element of the five-element cycle. Intensity < 1 thins it out (used when elements overlap).
	const float K = FMath::Max(0.2f, Intensity);
	auto Ev = [this, T, K](int32 Interval, int32 Offset) { return Every(T, FMath::RoundToInt(Interval / K), Offset); };
	switch (Element % 5)
	{
	case 0: // wood: seeds that sprout into rings of leaves
		if (Ev(26, 0))
		{
			const float Aim = AimAngle(Boss.Pos);
			for (int32 j = -1; j <= 1; ++j)
			{
				FBullet& S = Fire(Boss.Pos, 1.5f, Aim + j * 28.f + Rng.FRandRange(-6.f, 6.f), EBulletShape::Medium, FLinearColor(0.4f, 1.f, 0.45f));
				S.Behavior = BehSplitRing;
				S.Timer = 55;
				S.Param = 8;
				S.Param2 = 1.35f;
			}
		}
		break;
	case 1: // fire: an arcing fountain of flames
		if (Ev(5, 0))
		{
			FBullet& F = FireVec(Boss.Pos, Dir(Rng.FRandRange(-140.f, -40.f)) * Rng.FRandRange(2.4f, 3.2f), EBulletShape::Fire, FLinearColor(1.f, Rng.FRandRange(0.35f, 0.6f), 0.2f));
			F.Gravity = FVector2f(0.f, 0.045f);
		}
		break;
	case 2: // earth: a falling wall of boulders with a gap
		if (Ev(80, 10))
		{
			const int32 Slots = 12;
			const int32 Gap = Rng.RandRange(1, Slots - 3);
			for (int32 i = 0; i < Slots; ++i)
			{
				if (i >= Gap && i < Gap + 3) continue;
				FBullet& R = Fire(FVector2f((i + 0.5f) * FieldW / Slots, -8.f), 0.8f, 90.f, EBulletShape::Large, FLinearColor(0.95f, 0.78f, 0.35f));
				R.Accel = 0.01f;
				R.MaxSpeed = 1.9f;
			}
		}
		break;
	case 3: // metal: blade rings that stop and lunge
		if (Ev(55, 20))
		{
			const int32 N = Cnt(14);
			const float Base = Rng.FRandRange(0.f, 360.f);
			for (int32 i = 0; i < N; ++i)
			{
				FBullet& Bl = Fire(Boss.Pos, 3.2f, Base + 360.f * i / N, EBulletShape::Crescent, FLinearColor(0.92f, 0.95f, 1.f));
				Bl.Accel = -0.08f;
				Bl.MinSpeed = 0.f;
				Bl.Event = EBulletEvent::StopThenAim;
				Bl.EventFrame = 40;
				Bl.EventParam = 2.7f;
				Bl.EventParam2 = 20.f;
			}
		}
		break;
	default: // water: two winding streams
		if (Ev(4, 0))
		{
			for (float Sgn : { 1.f, -1.f })
			{
				FBullet& W = Fire(Boss.Pos, 1.9f, 90.f + Sgn * (35.f + FMath::Sin(T * 0.035f) * 45.f), EBulletShape::Water, FLinearColor(0.35f, 0.6f, 1.f));
				W.AngVel = -Sgn * 0.12f;
				W.AngVelDecay = 0.995f;
			}
		}
		break;
	}
}
