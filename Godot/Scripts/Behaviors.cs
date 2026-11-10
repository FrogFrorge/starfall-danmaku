// Scripted bullet behaviours and pattern helpers shared by stages 3 - Extra.
// Port of DanmakuBehaviors.cpp.
using static DanmakuSTG.BehaviorsLocals;

namespace DanmakuSTG;

static class BehaviorsLocals
{
	public static int WallSide(ref FVector2f P, float R, float Top = 0f)
	{
		// 0 left, 1 right, 2 top, 3 bottom, -1 none; clamps the position back inside
		int Side = -1;
		if (P.X < R)               { P.X = R; Side = 0; }
		else if (P.X > FieldW - R) { P.X = FieldW - R; Side = 1; }
		if (P.Y < Top + R)         { P.Y = Top + R; Side = 2; }
		else if (P.Y > FieldH - R) { P.Y = FieldH - R; Side = 3; }
		return Side;
	}

	public static readonly FVector2f[] Inward = { new FVector2f(1f, 0f), new FVector2f(-1f, 0f), new FVector2f(0f, 1f), new FVector2f(0f, -1f) };
}

public partial class DanmakuWorld
{
	public bool UpdateBehavior(FBullet B)
	{
		switch (B.Behavior)
		{
		case BehPlumSeed:
			if (--B.Timer <= 0)
			{
				float Rot = Rng.FRandRange(0f, 360f);
				FLinearColor Petal = FMath.Lerp(B.Color, LinColor.White, 0.25f);
				for (int i = 0; i < B.Param; ++i)
				{
					float A = Rot + 360f * i / B.Param;
					Fire(B.Pos + Dir(A) * 4f, B.Param2 * Rng.FRandRange(0.9f, 1.12f), A, EBulletShape.Sakura, Petal);
				}
				Burst(B.Pos, 6, B.Color, 1.5f, 8f, 16);
				B.bDead = true;
				return false;
			}
			return true;

		case BehLotusSummon:
			if (--B.Timer <= 0)
			{
				BloomLotus(B.Pos, B.Param, B.Color);
				AddParticle(B.Pos, V2.ZeroVector, 50f, B.Color, 16, true);
				B.bDead = true;
				return false;
			}
			return true;

		case BehPauseHome:
			if (B.Timer > 0)
			{
				if (--B.Timer == 0) B.Vel = V2.ZeroVector;
			}
			else if (--B.Timer2 <= 0)
			{
				// Param 9999: shoots off in a random direction instead of at the player
				B.Vel = Dir(B.Param == 9999 ? Rng.FRandRange(0f, 360f) : AimAngle(B.Pos)) * B.Param2;
				B.Behavior = 0;
			}
			return true;

		case BehAssassin:
		{
			// 0 fly out, 1 pause at a wall, 2 stalk the player shedding shards
			if (B.Param != 1 && B.Age > 2 && WallSide(ref B.Pos, 8f, 48f) >= 0)
			{
				B.Param = 1;
				B.Vel = V2.ZeroVector;
				B.Timer = 28;
				AddParticle(B.Pos, V2.ZeroVector, 40f, B.Color, 14, true);
			}
			if (B.Param == 1)
			{
				if (--B.Timer <= 0)
				{
					B.Param = 2;
					B.Vel = Dir(AimAngle(B.Pos)) * 1.25f;
					B.Pos += B.Vel * 2f;
					B.Timer2 = 60;
				}
			}
			else if (B.Param == 2 && --B.Timer2 <= 0)
			{
				B.Timer2 = 60;
				float Base = Frame * 2.9f;
				int N = Cnt(8);
				for (int i = 0; i < N; ++i)
				{
					Fire(B.Pos, 1.5f, Base + 360f * i / N, EBulletShape.Small, FMath.Lerp(B.Color, LinColor.White, 0.3f));
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
				FBullet O = FireVec(B.Pos, V2.ZeroVector, EBulletShape.Rice, new FLinearColor(0.85f, 0.72f, 1f));
				O.Behavior = BehDelayedRelease;
				O.Timer = 300;
				O.Center = B.Vel;
			}
			int Side = B.Age > 2 ? WallSide(ref B.Pos, B.Radius) : -1;
			if (Side >= 0)
			{
				if (B.Param == 1)
				{
					for (int i = 0; i < B.Timer2; ++i)
					{
						float A = 360f * i / FMath.Max(1, B.Timer2) + Rng.FRandRange(-10f, 10f);
						SpawnPlumSeed(B.Pos + Dir(A) * (B.Timer2 == 1 ? 0f : 8f), Rng.RandRange(18, 28), new FLinearColor(1f, 0.62f, 0.8f));
					}
					B.Param = 2;
					B.Vel = Dir(AimAngle(B.Pos)) * B.Param2;
				}
				else
				{
					SpawnWallBamboo(Side, B.Pos, Difficulty >= 3 ? 60 : 120, Difficulty >= 2 ? 2 : 1);
					B.Param = 1;
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
					FVector2f Perp = new FVector2f(-V.Y, V.X);
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
				B.AngVelDecay = 1f;
				B.Behavior = 0;
			}
			return true;

		case BehSplitRing:
			if (--B.Timer <= 0)
			{
				int N = Cnt(FMath.Max(1, B.Param));
				float Base = Rng.FRandRange(0f, 360f);
				for (int i = 0; i < N; ++i)
				{
					// Timer2 = 1: blossoms into flowers, every petal a different colour (嶺上開花)
				if (B.Timer2 == 1) Fire(B.Pos, B.Param2, Base + 360f * i / N, EBulletShape.Sakura, StageExLocals.ColLotus[(i + Frame) % StageExLocals.ColLotus.Length]);
				else Fire(B.Pos, B.Param2, Base + 360f * i / N, EBulletShape.Small, FMath.Lerp(B.Color, LinColor.White, 0.25f));
				}
				AddParticle(B.Pos, V2.ZeroVector, 30f, B.Color, 12, true);
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
				FVector2f Target = Player.Pos + new FVector2f((float)B.Param, 0f);
				float A = B.Param == 9999 ? Rng.FRandRange(0f, 360f) : AngleOf(Target - B.Pos);
				B.Vel = Dir(A) * B.Param2;
				B.Behavior = 0;
			}
			return true;
		}

		case BehArm:
			// harmless spark that grows, then becomes a real bullet and drifts with velocity Center
			B.Scale = FMath.Min(1f, B.Scale + 0.6f / FMath.Max(1, B.Timer));
			if (--B.Timer <= 0)
			{
				B.bHarmless = false;
				B.Scale = 1f;
				B.Vel = B.Center;
				B.Behavior = 0;
			}
			return true;

		case BehGroundSplit:
			// falls until it hits the ground, then breaks into Param small slow bullets (speed ~Param2) flying back up
			if (B.Pos.Y > FieldH - 4f)
			{
				FVector2f At = new FVector2f(B.Pos.X, FieldH - 4f);
				FLinearColor C = FMath.Lerp(B.Color, LinColor.White, 0.2f);
				int N = B.Param;
				float Sp = B.Param2;
				B.bDead = true;
				for (int i = 0; i < N; ++i)
				{
					FBullet S = Fire(At, Sp * Rng.FRandRange(0.8f, 1.2f), Rng.FRandRange(220f, 320f), EBulletShape.Small, C);
					S.Life = B.Timer == 1 ? 0 : 130;   // Timer 1: the shards drift on until they leave the field
				}
				AddParticle(At, V2.ZeroVector, 24f, C, 10, true);
				return false;
			}
			return true;

		case BehWallStop:
			// flies until a wall, waits Timer frames there, then dashes at the player at speed Param2
			if (B.Param == 0 && B.Age > 2 && WallSide(ref B.Pos, 6f) >= 0) { B.Param = 1; B.Vel = V2.ZeroVector; }
			else if (B.Param == 1 && --B.Timer <= 0) { B.Vel = Dir(AimAngle(B.Pos)) * B.Param2; B.Behavior = 0; }
			return true;

		default:
			return true;
		}
	}

	public void SpawnPlumSeed(FVector2f At, int Delay, FLinearColor Color, int Petals = 5, float PetalSpeed = 1.3f)
	{
		FBullet B = Fire(At, 0f, Rng.FRandRange(0f, 360f), EBulletShape.Sakura, Color);
		B.Behavior = BehPlumSeed;
		B.Timer = Delay;
		B.Param = Petals;
		B.Param2 = PetalSpeed;
		B.bHarmless = true;
		B.bBombProof = true;
		B.Scale = 0.75f;
		B.Spin = 6f;
	}

	static readonly int[] BloomLotus_Counts = { 10, 14, 18 };
	static readonly float[] BloomLotus_Speeds = { 1.25f, 1.55f, 1.85f };

	public void BloomLotus(FVector2f At, int Number, FLinearColor Color)
	{
		if (Number == 1 || Number == 9)
		{
			// the "terminals" bloom into three straight rings
			for (int r = 0; r < 3; ++r)
			{
				int N = Cnt(BloomLotus_Counts[r]);
				float Base = Rng.FRandRange(0f, 360f);
				for (int i = 0; i < N; ++i) Fire(At, BloomLotus_Speeds[r], Base + 360f * i / N, EBulletShape.Rice, Color);
			}
		}
		else
		{
			// 2-8: petals open, pause, then dart at the player
			int N = Cnt(12);
			float Base = Rng.FRandRange(0f, 360f);
			for (int i = 0; i < N; ++i)
			{
				FBullet P = FireVec(At, Dir(Base + 360f * i / N) * 1.5f, EBulletShape.Rice, Color);
				P.Behavior = BehPauseHome;
				P.Timer = 24;
				P.Timer2 = 18;
				P.Param2 = 1.75f;
			}
		}
	}

	static readonly int[] TripleRings_Counts = { 18, 24, 30 };
	static readonly float[] TripleRings_Speeds = { 2.9f, 3.6f, 4.3f };

	public void TripleRings(FVector2f At, float BaseAngle, float SpeedK, EBulletShape Shape, FLinearColor[] Colors)
	{
		for (int r = 0; r < 3; ++r)
		{
			int N = Cnt(TripleRings_Counts[r]);
			for (int i = 0; i < N; ++i)
			{
				Fire(At, TripleRings_Speeds[r] * SpeedK, BaseAngle + r * 6.9f + 360f * i / N, Shape, Colors[r]);
			}
		}
	}

	public void SpawnWallBamboo(int Side, FVector2f Hit, int Warn, int LeafRepeat)
	{
		// a bamboo stalk shoots from the wall that was hit straight across the field, then sheds leaves back
		FVector2f Origin = Hit;
		float Angle = 0f;
		switch (Side)
		{
		case 0: Origin.X = 0f;     Angle = 0f;   break;
		case 1: Origin.X = FieldW; Angle = 180f; break;
		case 2: Origin.Y = 0f;     Angle = 90f;  break;
		default: Origin.Y = FieldH; Angle = -90f; break;
		}
		for (int i = 0; i < LeafRepeat; ++i)
		{
			AddBeam(Origin, Angle, 8f, Warn + i * 6, 24, 50, new FLinearColor(0.6f, 1f, 0.7f), 2);
		}
	}

	public void ElementVolley(int Element, int T, float Intensity)
	{
		// One element of the five-element cycle. Intensity < 1 thins it out (used when elements overlap).
		float K = FMath.Max(0.2f, Intensity);
		Func<int, int, bool> Ev = (Interval, Offset) => Every(T, FMath.RoundToInt(Interval / K), Offset);
		switch (Element % 5)
		{
		case 0: // wood: seeds that sprout into rings of leaves
			if (Ev(26, 0))
			{
				float Aim = AimAngle(Boss.Pos);
				for (int j = -1; j <= 1; ++j)
				{
					FBullet S = Fire(Boss.Pos, 1.5f, Aim + j * 28f + Rng.FRandRange(-6f, 6f), EBulletShape.Medium, new FLinearColor(0.4f, 1f, 0.45f));
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
				FBullet F = FireVec(Boss.Pos, Dir(Rng.FRandRange(-140f, -40f)) * Rng.FRandRange(2.4f, 3.2f), EBulletShape.Fire, new FLinearColor(1f, Rng.FRandRange(0.35f, 0.6f), 0.2f));
				F.Gravity = new FVector2f(0f, 0.045f);
			}
			break;
		case 2: // earth: a falling wall of boulders with a gap
			if (Ev(80, 10))
			{
				const int Slots = 12;
				int Gap = Rng.RandRange(1, Slots - 3);
				for (int i = 0; i < Slots; ++i)
				{
					if (i >= Gap && i < Gap + 3) continue;
					FBullet R = Fire(new FVector2f((i + 0.5f) * FieldW / Slots, -8f), 0.8f, 90f, EBulletShape.Large, new FLinearColor(0.95f, 0.78f, 0.35f));
					R.Accel = 0.01f;
					R.MaxSpeed = 1.9f;
				}
			}
			break;
		case 3: // metal: blade rings that stop and lunge
			if (Ev(55, 20))
			{
				int N = Cnt(14);
				float Base = Rng.FRandRange(0f, 360f);
				for (int i = 0; i < N; ++i)
				{
					FBullet Bl = Fire(Boss.Pos, 3.2f, Base + 360f * i / N, EBulletShape.Crescent, new FLinearColor(0.92f, 0.95f, 1f));
					Bl.Accel = -0.08f;
					Bl.MinSpeed = 0f;
					Bl.Event = EBulletEvent.StopThenAim;
					Bl.EventFrame = 40;
					Bl.EventParam = 2.7f;
					Bl.EventParam2 = 20f;
				}
			}
			break;
		default: // water: two winding streams
			if (Ev(4, 0))
			{
				foreach (float Sgn in new[] { 1f, -1f })
				{
					FBullet W = Fire(Boss.Pos, 1.9f, 90f + Sgn * (35f + FMath.Sin(T * 0.035f) * 45f), EBulletShape.Water, new FLinearColor(0.35f, 0.6f, 1f));
					W.AngVel = -Sgn * 0.12f;
					W.AngVelDecay = 0.995f;
				}
			}
			break;
		}
	}
}
