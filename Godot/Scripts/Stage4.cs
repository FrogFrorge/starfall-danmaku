// Stage 4 boss: 墨竹, the formal scholar of the four gentlemen (plum, orchid, bamboo, chrysanthemum).
// Port of DanmakuStage4.cpp.
using static DanmakuSTG.Stage4Locals;

namespace DanmakuSTG;

static class Stage4Locals
{
	public static readonly FLinearColor ColPlum = new FLinearColor(1f, 0.62f, 0.8f);
	public static readonly FLinearColor ColOrchid = new FLinearColor(0.85f, 0.72f, 1f);
	public static readonly FLinearColor ColBamboo = new FLinearColor(0.6f, 1f, 0.7f);
	public static FLinearColor ChrysTone(FRandomStream R) { return new FLinearColor(1f, R.FRandRange(0.75f, 0.95f), R.FRandRange(0.25f, 0.5f)); }
	public static int D4(int D) { return FMath.Clamp(D, 0, 3); }

	public static readonly int[] PlumCounts = { 1, 2, 3, 5 };
	public static readonly int[] OrchidWays = { 1, 2, 3, 6 };
	public static readonly int[] FinalInterval = { 99999, 16 * 60, 12 * 60, 7 * 60 };
	public static readonly int[] FinalStart = { 3, 3, 4, 5 };
	public static readonly int[] FinalPlums = { 0, 2, 3, 4 };
}

public partial class DanmakuWorld
{
	public void S4_NonSpell1(int T)
	{
		if (Every(T, 28, 0))
		{
			for (int k = 0; k < Cnt(4); ++k)
			{
				SpawnPlumSeed(Boss.Pos + Dir(Rng.FRandRange(0f, 360f)) * Rng.FRandRange(30f, 90f), Rng.RandRange(20, 30), ColPlum);
			}
		}
		if (Every(T, 40, 20)) Fan(Boss.Pos, 7, 44f, 2.7f, AimAngle(Boss.Pos), EBulletShape.Rice, ColBamboo);
		if (Every(T, 9, 4))
		{
			FBullet B = Fire(Boss.Pos, 1.6f, T * 6.1f, EBulletShape.Rice, ColOrchid);
			B.AngVel = 0.2f;
		}
		if (Every(T, 180, 90)) WanderBoss();
	}

	// 梅符「梅花盛開」: seeds appear around the boss and burst into five petals
	public void S4_Plum(int T)
	{
		PatrolBoss(T);
		if (!Every(T, 14, 0)) return;
		for (int k = 0; k < PlumCounts[D4(Difficulty)]; ++k)
		{
			FVector2f P = Boss.Pos + Dir(Rng.FRandRange(0f, 360f)) * Rng.FRandRange(42f, 116f);
			SpawnPlumSeed(new FVector2f(FMath.Clamp(P.X, 17f, FieldW - 17f), FMath.Clamp(P.Y, 27f, FieldH * 0.46f)), Rng.RandRange(22, 34), ColPlum, 5, Rng.FRandRange(1.1f, 1.45f));
		}
	}

	public void S4_NonSpell2(int T)
	{
		if (Every(T, 4, 0))
		{
			foreach (float Off in new[] { 0f, 180f })
			{
				FBullet B = Fire(Boss.Pos, 1.8f, T * 7.3f + Off, EBulletShape.Rice, ColOrchid);
				B.AngVel = 0.25f;
			}
		}
		if (Every(T, 36, 0)) Ring(Boss.Pos, 20, 1.6f, Rng.FRandRange(0f, 30f), EBulletShape.Sakura, ColPlum);
		if (Every(T, 72, 36)) Fan(Boss.Pos, 5, 24f, 3f, AimAngle(Boss.Pos), EBulletShape.Medium, ColBamboo);
		if (Every(T, 170, 85)) WanderBoss();
	}

	// 蘭符「蘭花路徑」: a fast spiral of orchid petals that start to curl after a moment
	public void S4_Orchid(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 105f));
		if (Every(T, 54, 27)) Ring(Boss.Pos, Cnt(18), 1.5f, AimAngle(Boss.Pos), EBulletShape.Rice, new FLinearColor(0.75f, 0.6f, 1f));
		if (!Every(T, 4, 0)) return;
		int N = OrchidWays[D4(Difficulty)];
		float Base = T * 38.7f;
		for (int i = 0; i < N; ++i)
		{
			FBullet B = Fire(Boss.Pos + new FVector2f(0f, 3f), Rng.FRandRange(1.05f, 1.6f), Base + 360f * i / N, EBulletShape.Rice, ColOrchid);
			B.Behavior = BehCurveAfter;
			B.Timer = Rng.RandRange(24, 42);
			B.Param2 = Rng.FRandRange(0.1f, 0.2f) * (Rng.FRand() < 0.5f ? -1f : 1f);
		}
	}

	// 竹符「竹林之勢」: bamboo shoots up from the ground after a warning and sheds leaves from the top
	public void S4_Bamboo(int T)
	{
		PatrolBoss(T);
		if (Every(T, Difficulty >= 3 ? 44 : 66, 0))
		{
			int Cols = Difficulty >= 2 ? 2 : 1;
			for (int i = 0; i < Cols; ++i)
			{
				AddBeam(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), FieldH), -90f, 8f, 120, 30, 60, ColBamboo, 2);
			}
		}
	}

	// 菊符「菊花之哀」: chrysanthemum bursts that get denser and faster as the boss weakens
	public void S4_Chrys(int T)
	{
		PatrolBoss(T);
		float Lost = 1f - FMath.Clamp(Boss.HP / FMath.Max(1f, Boss.MaxHP), 0f, 1f);
		int D = D4(Difficulty);
		int Burst = (5 + D * 3) + FMath.FloorToInt(Lost * 9f);
		int Interval = FMath.Max(6, 12 - FMath.FloorToInt(Lost * 6f));
		if (T % Interval != 0) return;
		for (int i = 0; i < Burst; ++i)
		{
			FBullet B = Fire(Boss.Pos + new FVector2f(0f, 3f), Rng.FRandRange(1.3f, 2.05f), Rng.FRandRange(0f, 360f), EBulletShape.Chrys, ChrysTone(Rng));
			B.Spin = Rng.FRandRange(-5f, 5f);
		}
	}

	// 四君子「君子之約」 (Normal+, survival): chrysanthemums roam the field, bouncing between plum seeds and bamboo
	public void S4_Final(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, FieldH * 0.45f));
		int D = D4(Difficulty);
		Action<float> Spawn = (float Angle) =>
		{
			FBullet C = FireVec(Boss.Pos, Dir(Angle) * 1.5f, EBulletShape.Chrys, new FLinearColor(1f, 0.85f, 0.35f));
			C.Behavior = BehFinalChrys;
			C.Param = 1;
			C.Param2 = 1.5f;
			C.Timer = 30;
			C.Timer2 = FinalPlums[D];
			C.Scale = 1.7f;
			C.Radius *= 1.7f;
			C.Spin = 3f;
			C.bBombProof = true;
		};
		if (T == 1)
		{
			for (int i = 0; i < FinalStart[D]; ++i) Spawn(90f + 360f * i / FinalStart[D]);
		}
		else if (T % FinalInterval[D] == 0)
		{
			Spawn(Rng.FRandRange(0f, 360f));
		}
	}
}
