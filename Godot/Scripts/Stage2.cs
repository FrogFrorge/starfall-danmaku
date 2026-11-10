// Stage 2 boss: 千秋, the painter of the four seasons.
// 春 polar petal rings that drift down, 夏 bouncing "sunbeam" streams, 秋 maple leaves blown in from the side,
// 冬 a blizzard that thickens over time.
// Port of DanmakuStage2.cpp.
using static DanmakuSTG.Stage2Locals;

namespace DanmakuSTG;

static class Stage2Locals
{
	public static readonly FLinearColor ColSakura = new FLinearColor(1f, 0.62f, 0.8f);
	public static readonly FLinearColor ColSakuraDeep = new FLinearColor(1f, 0.42f, 0.68f);
	public static readonly FLinearColor ColLeaf = new FLinearColor(0.5f, 0.95f, 0.5f);
	public static readonly FLinearColor ColSun = new FLinearColor(1f, 0.85f, 0.3f);
	public static readonly FLinearColor ColSunHot = new FLinearColor(1f, 0.55f, 0.2f);
	public static readonly FLinearColor ColAcorn = new FLinearColor(0.95f, 0.6f, 0.3f);
	public static readonly FLinearColor ColSnow = new FLinearColor(0.85f, 0.93f, 1f);
	public static readonly FLinearColor ColIce = new FLinearColor(0.5f, 0.75f, 1f);
}

public partial class DanmakuWorld
{
	public FBullet FirePolar(FVector2f Center, float Angle, float StartRadius, float RadSpeed, float AngSpeed, EBulletShape Shape, FLinearColor Color)
	{
		FBullet B = Fire(Center + Dir(Angle) * StartRadius, 0f, Angle, Shape, Color);
		B.Motion = 1;
		B.Center = Center;
		B.PolarAngle = Angle;
		B.PolarRadius = StartRadius;
		B.PolarRadSpeed = RadSpeed * GetDifficultyInfo(Difficulty).SpeedMul;
		B.PolarAngSpeed = AngSpeed;
		return B;
	}

	public FBullet FireVec(FVector2f Pos, FVector2f Vel, EBulletShape Shape, FLinearColor Color)
	{
		FBullet B = Fire(Pos, 0f, AngleOf(Vel), Shape, Color);
		B.Motion = 2;
		B.Vel = Vel;
		return B;
	}

	// ---------- non-spell 1: double petal spiral + aimed fan ----------

	public void S2_NonSpell1(int T)
	{
		if (Every(T, 6, 0))
		{
			int Arms = Cnt(6);
			for (int i = 0; i < Arms; ++i)
			{
				Fire(Boss.Pos, 2.1f, T * 3.1f + 360f * i / Arms, EBulletShape.Sakura, ColSakura);
				Fire(Boss.Pos, 1.7f, -T * 2.3f + 360f * i / Arms + 36f, EBulletShape.Sakura, ColSakuraDeep);
			}
		}
		if (Every(T, 56, 28)) Fan(Boss.Pos, 7, 40f, 3.2f, AimAngle(Boss.Pos), EBulletShape.Rice, ColLeaf);
		if (Every(T, 200, 100)) WanderBoss();
	}

	// ---------- 春符「春風催生」 : expanding, turning petal rings whose centre sinks toward the player ----------

	public void S2_Spring(int T)
	{
		PatrolBoss(T);
		if (Every(T, 30, 0))
		{
			int Wave = T / FMath.Max(1, FMath.RoundToInt(30 * GetDifficultyInfo(Difficulty).IntervalScale));
			float Sgn = Wave % 2 == 0 ? 1f : -1f;
			int N = Cnt(16);
			float Base = Rng.FRandRange(0f, 360f);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = FirePolar(Boss.Pos, Base + 360f * i / N, 8f, 0.9f, 0.29f * Sgn, EBulletShape.Sakura,
					Sgn > 0f ? ColSakura : ColSakuraDeep);
				B.CenterVel = new FVector2f(0f, 0.46f);
			}
		}
		// petals drifting down through the field
		if (Every(T, 14, 7))
		{
			FBullet B = FireVec(new FVector2f(Rng.FRandRange(10f, FieldW - 10f), -8f), new FVector2f(Rng.FRandRange(-0.2f, 0.2f), Rng.FRandRange(0.9f, 1.3f)),
				EBulletShape.Sakura, ColSakura);
			B.WaveAmp = 0.6f;
			B.WaveFreq = 0.05f;
			B.WavePhase = Rng.FRandRange(0f, 6.28f);
			B.Spin = Rng.FRandRange(-3f, 3f);
		}
	}

	// ---------- non-spell 2: swaying leaf rings ----------

	public void S2_NonSpell2(int T)
	{
		if (Every(T, 32, 0))
		{
			int N = Cnt(24);
			float Base = Rng.FRandRange(0f, 360f);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Boss.Pos, 1.9f, Base + 360f * i / N, EBulletShape.Rice, i % 2 != 0 ? ColLeaf : ColSun);
				B.WaveAmp = 0.7f;
				B.WaveFreq = 0.06f;
				B.WavePhase = i * 0.7f;
			}
		}
		if (Every(T, 48, 24)) Fan(Boss.Pos, 9, 56f, 2.8f, AimAngle(Boss.Pos), EBulletShape.Sakura, ColSakuraDeep);
		if (Every(T, 170, 85)) WanderBoss();
	}

	// ---------- 夏符「夏日鋒芒」 : streams of sun orbs that look like lasers and ricochet off every wall ----------
	// (a random direction each burst; 5/6/7/8 evenly spaced streams of 5/6/7/8 orbs by difficulty)

	public void S2_Summer(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 130f));
		// straight sun rays that ricochet four times, a burst every 1.5 s (Normal ~2 s)
		int Cyc = FMath.Max(30, FMath.RoundToInt(90 * GetDifficultyInfo(Difficulty).IntervalScale));
		int K = T % Cyc;
		// Easy 5 directions x 5 bullets, Normal 6 x 6, Hard 7 x 7, Lunatic 8 x 8; each burst points a random way,
		// but the directions are always evenly spaced
		int Streams = 5 + FMath.Clamp(Difficulty, 0, 3);
		int PerStream = Streams;
		if (K == 0 || T <= 1) Boss.MawPos = new FVector2f(Rng.FRandRange(0f, 360f), 0f);
		if (K < PerStream * 2 && K % 2 == 0)
		{
			float Base = Boss.MawPos.X;
			for (int s = 0; s < Streams; ++s)
			{
				FBullet B = Fire(Boss.Pos, 2.3f, Base + 360f * s / Streams, EBulletShape.Medium, K < PerStream ? ColSun : ColSunHot);
				B.WallMask = 1 | 2 | 4;
				B.Bounces = 4;
				B.bDieAfterBounces = true;
			}
		}
	}

	// ---------- 秋符「秋風落葉」 : maple leaves blown in from the right, acorns dropping from above ----------

	public void S2_Autumn(int T)
	{
		PatrolBoss(T);
		// Lunatic blows an extra leaf in every other gust
		if (Every(T, 4, 0) || (Difficulty >= 3 && Every(T, 8, 2)))
		{
			float A = Rng.FRandRange(110f, 170f);
			FBullet B = FireVec(new FVector2f(FieldW + 8f, Rng.FRandRange(0f, FieldH * 0.55f)), Dir(A) * Rng.FRandRange(1.35f, 2.05f),
				EBulletShape.Maple, new FLinearColor(1f, Rng.FRandRange(0.3f, 0.65f), 0.15f));
			B.WaveAmp = 0.35f;
			B.WaveFreq = 0.04f;
			B.WavePhase = Rng.FRandRange(0f, 6.28f);
			B.Spin = Rng.FRandRange(-4f, 4f);
		}
		if (Every(T, 40, 20))
		{
			int N = Cnt(8);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = FireVec(new FVector2f(Rng.FRandRange(10f, FieldW - 10f), -6f), new FVector2f(0f, Rng.FRandRange(1.6f, 2.4f)), EBulletShape.Medium, ColAcorn);
				B.KillBelowY = FieldH / 3f;
			}
		}
	}

	// ---------- 冬符「冬日暴雪」 : snowfall that grows heavier, big flakes fall faster ----------

	public void S2_Winter(int T)
	{
		PatrolBoss(T);
		if (Every(T, 5, 0))
		{
			int N = Cnt(1 + FMath.Min(T, 2400) / 800);
			for (int i = 0; i < N; ++i)
			{
				float S = Rng.FRandRange(0.7f, 1.45f);
				FBullet B = FireVec(new FVector2f(Rng.FRandRange(4f, FieldW - 4f), -10f), new FVector2f(Rng.FRandRange(-0.25f, 0.25f), 0.55f + S * 0.65f),
					EBulletShape.Snow, S > 1.2f ? ColIce : ColSnow);
				B.Scale = S;
				B.Radius *= S;
				B.WaveAmp = 0.45f;
				B.WaveFreq = 0.03f;
				B.WavePhase = Rng.FRandRange(0f, 6.28f);
				B.Spin = Rng.FRandRange(-2f, 2f);
			}
		}
	}
}
