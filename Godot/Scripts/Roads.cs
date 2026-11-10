// Stage roads (道中): a different timeline of enemy waves for every stage, and the enemy attack patterns.
// Road lengths: stage 1 ~75 s, 2 ~90 s, 3 ~100 s, 4 ~110 s, 5 ~120 s, 6 ~60 s, Extra ~130 s plus a midboss.
// Enemy fire uses Every()/Cnt(), so it thins out on lower difficulties like the boss patterns do.
// Port of DanmakuRoads.cpp.
using static DanmakuSTG.RoadsLocals;

namespace DanmakuSTG;

static class RoadsLocals
{
	public static readonly FLinearColor RRed = new FLinearColor(1f, 0.3f, 0.3f);
	public static readonly FLinearColor RYellow = new FLinearColor(1f, 0.9f, 0.35f);
	public static readonly FLinearColor RGreen = new FLinearColor(0.45f, 1f, 0.5f);
	public static readonly FLinearColor RBlue = new FLinearColor(0.4f, 0.6f, 1f);
	public static readonly FLinearColor RPurple = new FLinearColor(0.7f, 0.45f, 1f);
	public static readonly FLinearColor ROrange = new FLinearColor(1f, 0.6f, 0.25f);
	public static readonly FLinearColor RPink = new FLinearColor(1f, 0.6f, 0.8f);
	public static readonly FLinearColor RWater = new FLinearColor(0.55f, 0.8f, 1f);
	public static readonly FLinearColor RIvory = new FLinearColor(1f, 0.96f, 0.85f);
	public static readonly FLinearColor[] Elem = { new FLinearColor(0.4f, 1f, 0.45f), new FLinearColor(1f, 0.45f, 0.25f), new FLinearColor(0.95f, 0.8f, 0.35f),
		new FLinearColor(0.92f, 0.95f, 1f), new FLinearColor(0.35f, 0.6f, 1f) };

	// set by UpdateStage from the difficulty: how many more dolls the fast waves squeeze in
	public static float GRoadDensity = 1f;

	/** True on the frames of a wave: Count spawns, Interval frames apart, starting at Start. K = spawn index.
	 *  On harder roads, fast waves (Interval <= 30) fit more spawns into the same stretch of time; K still runs
	 *  0..Count-1 (neighbouring spawns share a K), so positions computed from K stay on screen. */
	public static bool Rep(int F, int Start, int Count, int Interval, ref int K)
	{
		if (F < Start) return false;
		int D = F - Start;
		if (GRoadDensity <= 1f || Interval > 30)
		{
			if (D % Interval != 0) return false;
			K = D / Interval;
			return K < Count;
		}
		int C = FMath.RoundToInt(Count * GRoadDensity);
		float Step = (float)(Count * Interval) / (float)C;
		int J = FMath.RoundToInt(D / Step);
		if (J >= C || FMath.RoundToInt(J * Step) != D) return false;
		K = FMath.Min(Count - 1, J * Count / C);
		return true;
	}

	public static readonly float[] RoadEveryIv = { 1f, 0.8f, 0.67f, 0.56f };
	public static readonly float[] RoadCountMul = { 1f, 1.3f, 1.6f, 2f };
	public static readonly float[] RoadDensityDn = { 1f, 1.2f, 1.4f, 1.6f };
}

public partial class DanmakuWorld
{
	public bool RoadEvery(int Age, int Interval, int Offset)
	{
		int Tier = RoadTier();
		if (Tier < 0) return Every(Age, Interval, Offset);
		// Easy is the old road; each step up fires more often
		int N = FMath.Max(1, FMath.RoundToInt(Interval * RoadEveryIv[Tier]));
		return Age % N == FMath.RoundToInt(Offset * RoadEveryIv[Tier]) % N;
	}

	public int RoadCount(int N, bool bWasScaled = false)
	{
		int Tier = RoadTier();
		if (Tier < 0) return bWasScaled ? Cnt(N) : N;
		return FMath.Max(1, FMath.RoundToInt(N * RoadCountMul[Tier]));
	}

	public float RoadDensity()
	{
		int Tier = RoadTier();
		return Tier < 0 ? 1f : RoadDensityDn[Tier];
	}

	// =====================================================================================
	// Spawning helpers. Road enemies are kept fragile: swarm dolls die in a few hits, sentries and
	// spirits take a moment but never stall the road.
	// =====================================================================================

	public FEnemy Doll(FVector2f Pos, FVector2f Vel, int Pattern, bool bRed = false)
	{
		FEnemy E = SpawnEnemy(Pos, Vel, bRed ? 1 : 0, bRed ? 9f : 3f, Pattern, -1, -1, V2.ZeroVector, 1, 1);
		E.bRevenge = StageIndex >= ExtraStage || (RoadTier() >= 3 && StageIndex >= 1);
		return E;
	}

	public FEnemy Sentry(FVector2f Pos, int Pattern, int StopAt = 60, int LeaveAt = 300, float HP = 40f)
	{
		FEnemy E = SpawnEnemy(Pos, new FVector2f(0f, 2f), 1, HP * 0.75f, Pattern, StopAt, LeaveAt, new FVector2f(0f, -1.4f), 4, 2);
		E.bRevenge = StageIndex >= ExtraStage || (RoadTier() >= 3 && StageIndex >= 1) || (RoadTier() == 2 && StageIndex >= 3);
		return E;
	}

	public FEnemy Spirit(FVector2f Pos, int Pattern, float HP)
	{
		return SpawnEnemy(Pos, new FVector2f(0f, 1.5f), 2, HP * 0.7f, Pattern, 90, 690, new FVector2f(0f, -1f), 8, 8);
	}

	public void UpdateStage()
	{
		if (Boss.bStarted) return;
		int F = StageFrame;
		GRoadDensity = RoadDensity();
		switch (StageIndex)
		{
		case 0: RoadStage1(F); break;
		case 1: RoadStage2(F); break;
		case 2: RoadStage3(F); break;
		case 3: RoadStage4(F); break;
		case 4: RoadStage5(F); break;
		case 5: RoadStage6(F); break;
		case IfStage: RoadIf(F); break;
		default: RoadExtra(F); break;
		}
	}

	// ---------- stage 1: 星光參道 (~75 s) ----------
	public void RoadStage1(int F)
	{
		int K = 0;
		if (Rep(F, 90, 16, 16, ref K))   Doll(new FVector2f(-16f, 50f + (K % 3) * 30f), new FVector2f(2.2f, 0.8f), K % 2 != 0 ? 0 : -1);
		if (Rep(F, 330, 16, 16, ref K))  Doll(new FVector2f(FieldW + 16f, 50f + (K % 3) * 30f), new FVector2f(-2.2f, 0.8f), K % 2 != 0 ? 0 : -1);
		if (F == 640) for (int i = 0; i < 3; ++i) Sentry(new FVector2f(96f + i * 96f, -20f), 1, 60, 300);
		if (Rep(F, 900, 20, 20, ref K))  Doll(new FVector2f(Rng.FRandRange(40f, FieldW - 40f), -16f), new FVector2f(Rng.FRandRange(-0.4f, 0.4f), 1.6f), K % 2 != 0 ? 2 : 0);
		if (F == 1400) Spirit(new FVector2f(FieldW * 0.5f, -30f), 3, 450f);
		if (Rep(F, 1500, 8, 40, ref K))  Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 40f), new FVector2f(K % 2 != 0 ? 2f : -2f, 1f), 0);
		if (Rep(F, 2100, 12, 16, ref K))
		{
			foreach (float Sgn in new[] { -1f, 1f }) Doll(new FVector2f(FieldW * 0.5f + Sgn * (20f + K * 14f), -16f), new FVector2f(0f, 1.9f), K % 2 == 0 ? 0 : -1);
		}
		if (F == 2600) { Sentry(new FVector2f(80f, -20f), 8, 70, 330); Sentry(new FVector2f(FieldW - 80f, -20f), 8, 70, 330); }
		if (Rep(F, 2700, 16, 18, ref K)) Doll(new FVector2f(Rng.FRandRange(40f, FieldW - 40f), -16f), new FVector2f(0f, 2.1f), -1);
		if (Rep(F, 3150, 16, 24, ref K))
		{
			bool bL = K % 2 == 0;
			Doll(new FVector2f(bL ? -16f : FieldW + 16f, 30f + (K % 4) * 12f), new FVector2f(bL ? 1.6f : -1.6f, 1.1f), 4);
		}
		if (F == 3700) for (int i = 0; i < 4; ++i) Sentry(new FVector2f(64f + i * 85f, -20f), 1, 60 + i * 10, 300);
		if (Rep(F, 3900, 10, 30, ref K)) Doll(new FVector2f(48f + (K % 5) * 72f, -16f), new FVector2f(0f, 1.5f), 2);
		if (F >= 4500 && (Enemies.Count == 0 || F >= 4800)) StartBoss();
	}

	// ---------- stage 2: 四時繪卷 (~90 s) ----------
	public void RoadStage2(int F)
	{
		int K = 0;
		if (Rep(F, 90, 24, 14, ref K))
		{
			FEnemy E = Doll(new FVector2f(60f + (K % 8) * 38f, -16f), new FVector2f(0f, 1.5f), K % 2 != 0 ? 6 : 0);
			E.SineAmp = 1.2f; E.SineFreq = 0.05f; E.SinePhase = K;
		}
		if (Rep(F, 520, 14, 24, ref K))
		{
			Doll(new FVector2f(-16f, -10f), new FVector2f(1.6f, 1.4f), 0);
			Doll(new FVector2f(FieldW + 16f, -10f), new FVector2f(-1.6f, 1.4f), K % 2 != 0 ? 2 : -1);
		}
		if (F == 1000) for (int i = 0; i < 3; ++i) Sentry(new FVector2f(96f + i * 96f, -20f), 15, 60, 320).Color = RPink;
		if (Rep(F, 1150, 10, 30, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 1.3f), 16);
		if (Rep(F, 1500, 26, 12, ref K))
		{
			FEnemy E = Doll(new FVector2f(FieldW + 16f, 40f + (K % 5) * 25f), new FVector2f(-1.9f, 0.3f), 5);
			E.SineAmp = 0.6f; E.SineFreq = 0.07f;
		}
		if (F == 2300) Spirit(new FVector2f(FieldW * 0.5f, -30f), 9, 500f);
		if (Rep(F, 2450, 8, 50, ref K)) Doll(new FVector2f(K % 2 != 0 ? 40f : FieldW - 40f, -16f), new FVector2f(K % 2 != 0 ? 1.2f : -1.2f, 1.6f), 6, true);
		if (Rep(F, 3100, 20, 18, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 1.2f), 16);
		if (Rep(F, 3300, 6, 70, ref K)) Sentry(new FVector2f(60f + K * 52f, -20f), 15, 60, 260).Color = K % 2 != 0 ? RWater : RIvory;
		if (Rep(F, 3800, 12, 34, ref K))
		{
			FEnemy E = Doll(new FVector2f(K % 2 != 0 ? 60f : FieldW - 60f, -16f), new FVector2f(K % 2 != 0 ? 0.8f : -0.8f, 1.6f), 2, true);
			E.Acc = new FVector2f(K % 2 != 0 ? -0.012f : 0.012f, 0f);
		}
		if (Rep(F, 4200, 20, 14, ref K))
		{
			FEnemy E = Doll(new FVector2f(-16f, 40f + (K % 5) * 25f), new FVector2f(1.9f, 0.3f), K % 3 == 0 ? 0 : -1);
			E.SineAmp = 0.6f; E.SineFreq = 0.07f;
		}
		if (F == 4500) { Sentry(new FVector2f(90f, -20f), 15, 70, 360).Color = RGreen; Sentry(new FVector2f(FieldW - 90f, -20f), 15, 70, 360).Color = ROrange; }
		if (Rep(F, 4700, 12, 24, ref K)) Doll(new FVector2f(FieldW * 0.5f + (K % 2 != 0 ? 1f : -1f) * (40f + K * 8f), -16f), new FVector2f(0f, 1.7f), 6);
		if (F >= 5400 && (Enemies.Count == 0 || F >= 5700)) StartBoss();
	}

	// ---------- stage 3: 雲上之路 (~100 s) ----------
	public void RoadStage3(int F)
	{
		int K = 0;
		if (Rep(F, 90, 24, 12, ref K))  Doll(new FVector2f(-16f, 60f + (K % 5) * 22f), new FVector2f(3f, 0.25f), K % 2 != 0 ? 7 : -1);
		if (Rep(F, 420, 24, 12, ref K)) Doll(new FVector2f(FieldW + 16f, 60f + (K % 5) * 22f), new FVector2f(-3f, 0.25f), K % 2 != 0 ? 7 : -1);
		if (Rep(F, 900, 6, 50, ref K))  Sentry(new FVector2f(60f + K * 52f, -20f), 10, 50, 200, 30f);
		if (Rep(F, 1000, 14, 22, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 1.8f), 0);
		if (Rep(F, 1400, 24, 16, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(Rng.FRandRange(-0.3f, 0.3f), 1.3f), 5);
		if (F == 2100) Spirit(new FVector2f(FieldW * 0.5f, -30f), 17, 550f);
		if (Rep(F, 2250, 10, 36, ref K)) Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 50f), new FVector2f(K % 2 != 0 ? 2.4f : -2.4f, 0.6f), 7, true);
		if (Rep(F, 2850, 32, 10, ref K))
		{
			bool bL = (K / 4) % 2 == 0;
			Doll(new FVector2f(bL ? -16f : FieldW + 16f, 30f + (K % 4) * 30f), new FVector2f(bL ? 2.6f : -2.6f, 0.4f), K % 4 == 0 ? 0 : (K % 4 == 2 ? 7 : -1));
		}
		if (Rep(F, 3500, 7, 60, ref K)) Sentry(new FVector2f(Rng.FRandRange(60f, FieldW - 60f), -20f), 10, 50, 220, 30f);
		if (Rep(F, 3600, 14, 26, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 1.4f), 5);
		if (Rep(F, 4200, 22, 18, ref K))
		{
			bool bL = K % 2 == 0;
			Doll(new FVector2f(bL ? 20f : FieldW - 20f, -16f), new FVector2f(bL ? 1.2f : -1.2f, 1.6f), K % 3 == 0 ? 19 : 4);
		}
		if (F == 4900) for (int i = 0; i < 3; ++i) Sentry(new FVector2f(96f + i * 96f, -20f), 1, 60, 300);
		if (Rep(F, 5000, 16, 20, ref K)) Doll(new FVector2f(-16f + (K % 2) * (FieldW + 32f), 80f + (K % 3) * 20f), new FVector2f(K % 2 != 0 ? -2.8f : 2.8f, 0.2f), 7);
		if (F >= 6000 && (Enemies.Count == 0 || F >= 6300)) StartBoss();
	}

	// ---------- stage 4: 四君子庭園 (~110 s) ----------
	public void RoadStage4(int F)
	{
		int K = 0;
		if (Rep(F, 90, 16, 24, ref K))   Doll(new FVector2f(48f + (K % 6) * 58f, -16f), new FVector2f(0f, 1.5f), 11);
		if (Rep(F, 300, 16, 24, ref K))  Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 2f), 0);
		if (Rep(F, 700, 22, 16, ref K))
		{
			FEnemy E = Doll(new FVector2f(FieldW * 0.5f, -16f), new FVector2f(0f, 1.8f), 2);
			E.Acc = new FVector2f(K % 2 != 0 ? 0.03f : -0.03f, -0.004f);
		}
		if (F == 1300) for (int i = 0; i < 4; ++i) Sentry(new FVector2f(64f + i * 85f, -20f), 8, 60, 330).Color = RPurple;
		if (Rep(F, 1900, 28, 14, ref K)) Doll(new FVector2f(K % 2 != 0 ? 110f : FieldW - 110f, -16f), new FVector2f(0f, 2f), K % 4 == 0 ? 0 : (K % 4 == 2 ? 19 : -1));
		if (F == 2600) Spirit(new FVector2f(FieldW * 0.5f, -30f), 18, 600f);
		if (Rep(F, 2700, 8, 50, ref K))  Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 30f), new FVector2f(K % 2 != 0 ? 1.6f : -1.6f, 0.8f), 11, true);
		if (Rep(F, 3350, 14, 30, ref K))
		{
			bool bL = K % 2 == 0;
			Doll(new FVector2f(bL ? -16f : FieldW + 16f, 50f), new FVector2f(bL ? 1.3f : -1.3f, 0.9f), 11, true);
		}
		if (Rep(F, 4000, 32, 11, ref K))
		{
			FEnemy E = Doll(new FVector2f(40f + (K % 8) * 43f, -16f), new FVector2f(0f, 1.4f), K % 3 == 0 ? 6 : (K % 3 == 1 ? 0 : -1));
			E.SineAmp = 1f; E.SineFreq = 0.04f; E.SinePhase = K * 0.7f;
		}
		if (Rep(F, 4900, 7, 55, ref K)) Sentry(new FVector2f(60f + (K % 6) * 52f, -20f), 15, 60, 300).Color = RYellow;
		if (Rep(F, 5200, 16, 22, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 1.8f), 11);
		if (F == 5800) { Sentry(new FVector2f(120f, -20f), 1, 60, 320); Sentry(new FVector2f(FieldW - 120f, -20f), 1, 60, 320); Sentry(new FVector2f(FieldW * 0.5f, -20f), 18, 80, 340, 60f); }
		if (F >= 6600 && (Enemies.Count == 0 || F >= 6900)) StartBoss();
	}

	// ---------- stage 5: 五行仙壇 (~120 s) ----------
	public void RoadStage5(int F)
	{
		int K = 0;
		if (Rep(F, 90, 34, 11, ref K))
		{
			FEnemy E = Doll(new FVector2f(FieldW * 0.5f + FMath.Sin(K * 0.6f) * 120f, -16f), new FVector2f(0f, 1.6f), K % 3 == 0 ? 12 : (K % 3 == 1 ? 0 : -1));
			E.Color = Elem[K % 5];
		}
		if (Rep(F, 700, 12, 34, ref K)) Sentry(new FVector2f(50f + (K % 5) * 71f, -20f), 12, 50, 240, 30f).Color = Elem[K % 5];
		if (Rep(F, 800, 16, 24, ref K)) Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 30f + (K % 4) * 20f), new FVector2f(K % 2 != 0 ? 2f : -2f, 0.7f), 19).Color = Elem[K % 5];
		if (F == 1500) Spirit(new FVector2f(FieldW * 0.5f, -30f), 13, 650f);
		if (Rep(F, 1650, 10, 40, ref K)) Doll(new FVector2f(Rng.FRandRange(40f, FieldW - 40f), -16f), new FVector2f(0f, 1.5f), 12, true).Color = Elem[K % 5];
		if (Rep(F, 2350, 26, 14, ref K))
		{
			bool bL = K % 2 == 0;
			Doll(new FVector2f(bL ? -16f : FieldW + 16f, 20f + (K % 3) * 20f), new FVector2f(bL ? 1.4f : -1.4f, 1.2f), K % 3 == 0 ? 19 : 4);
		}
		if (Rep(F, 3100, 5, 70, ref K)) Sentry(new FVector2f(80f + K * 56f, -20f), 8, 60, 260).Color = Elem[K];
		if (Rep(F, 3200, 20, 18, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 2f), 0).Color = Elem[K % 5];
		if (F == 3850) { Spirit(new FVector2f(110f, -30f), 13, 420f); Spirit(new FVector2f(FieldW - 110f, -30f), 13, 420f); }
		if (Rep(F, 4700, 40, 8, ref K))
		{
			float A = 90f + FMath.Sin(K * 0.5f) * 40f;
			Doll(new FVector2f(FieldW * 0.5f, -16f), Dir(A) * 1.8f, K % 4 == 0 ? 0 : (K % 4 == 2 ? 2 : -1)).Color = Elem[K % 5];
		}
		if (Rep(F, 5500, 8, 50, ref K)) Sentry(new FVector2f(60f + (K % 6) * 52f, -20f), K % 2 != 0 ? 1 : 12, 60, 280).Color = Elem[K % 5];
		if (Rep(F, 5700, 18, 20, ref K)) Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 60f), new FVector2f(K % 2 != 0 ? 1.8f : -1.8f, 1f), 19).Color = Elem[K % 5];
		if (F == 6300) for (int i = 0; i < 3; ++i) Sentry(new FVector2f(96f + i * 96f, -20f), 12, 60, 340).Color = Elem[i * 2];
		if (F >= 7200 && (Enemies.Count == 0 || F >= 7500)) StartBoss();
	}

	// ---------- stage 6: 天宮 (~60 s, the final boss comes quickly) ----------
	public void RoadStage6(int F)
	{
		int K = 0;
		if (Rep(F, 90, 22, 12, ref K))
		{
			foreach (float Sgn in new[] { -1f, 1f }) Doll(new FVector2f(FieldW * 0.5f + Sgn * (24f + K * 10f), -16f), new FVector2f(Sgn * 0.3f, 1.9f), K % 3 == 0 ? 2 : (K % 3 == 1 ? 0 : -1), K % 4 == 0).Color = RYellow;
		}
		if (F == 560) for (int i = 0; i < 4; ++i) Sentry(new FVector2f(64f + i * 85f, -20f), 8, 60, 320).Color = RYellow;
		if (Rep(F, 700, 16, 20, ref K)) Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 40f + (K % 3) * 20f), new FVector2f(K % 2 != 0 ? 2.2f : -2.2f, 0.7f), 19).Color = RYellow;
		if (F == 1150) Spirit(new FVector2f(FieldW * 0.5f, -30f), 3, 700f);
		if (Rep(F, 1250, 8, 50, ref K)) Doll(new FVector2f(Rng.FRandRange(40f, FieldW - 40f), -16f), new FVector2f(0f, 1.6f), 2, true).Color = RYellow;
		if (Rep(F, 1850, 26, 12, ref K))
		{
			bool bL = K % 2 == 0;
			Doll(new FVector2f(bL ? -16f : FieldW + 16f, 40f + (K % 4) * 25f), new FVector2f(bL ? 2f : -2f, 0.8f), K % 3 == 0 ? 19 : 4);
		}
		if (F == 2600) for (int i = 0; i < 4; ++i) Sentry(new FVector2f(64f + i * 85f, -20f), i % 2 != 0 ? 1 : 8, 60, 300).Color = RYellow;
		if (Rep(F, 2700, 16, 18, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 2.1f), 0).Color = RYellow;
		if (F >= 3600 && (Enemies.Count == 0 || F >= 3900)) StartBoss();
	}

	// ---------- Extra: 天和閣 (~130 s, 玄 appears at the halfway point) ----------
	// Extra dolls and sentries burst into a small ring when they die, and the waves overlap.
	public void RoadExtra(int F)
	{
		const int MidAt = 3000;
		int K = 0;
		// the opening is a warm-up: fewer dolls, the two waves don't overlap, and these ones don't burst when they die
		if (Rep(F, 90, 16, 20, ref K))
		{
			bool bL = K % 2 == 0;
			Doll(new FVector2f(bL ? 30f : FieldW - 30f, -16f), new FVector2f(bL ? 0.8f : -0.8f, 1.8f), K % 2 == 0 ? 14 : -1).bRevenge = false;
		}
		if (Rep(F, 440, 14, 18, ref K)) Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 40f + (K % 4) * 22f), new FVector2f(K % 2 != 0 ? 2.4f : -2.4f, 0.5f), K % 2 != 0 ? 19 : -1).bRevenge = false;
		if (Rep(F, 700, 6, 45, ref K)) Sentry(new FVector2f(60f + K * 52f, -20f), 20, 60, 300).Color = RRed;
		if (Rep(F, 1000, 20, 16, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 2f), 21);
		if (F == 1400) Spirit(new FVector2f(FieldW * 0.5f, -30f), 22, 650f);
		if (Rep(F, 1500, 10, 40, ref K)) Doll(new FVector2f(K % 2 != 0 ? 50f : FieldW - 50f, -16f), new FVector2f(0f, 1.5f), 14, true);
		if (Rep(F, 2100, 36, 10, ref K))
		{
			FEnemy E = Doll(new FVector2f(40f + (K % 8) * 43f, -16f), new FVector2f(0f, 1.5f), K % 3 == 0 ? 21 : 5);
			E.SineAmp = 0.8f; E.SineFreq = 0.05f; E.SinePhase = K;
		}
		if (Rep(F, 2300, 4, 60, ref K)) Sentry(new FVector2f(80f + K * 75f, -20f), 23, 60, 260).Color = RIvory;
		if (!bMidDone)
		{
			if (F >= MidAt)
			{
				if (bDemo && DemoSkipFrames > MidAt) bMidDone = true;
				else if (Enemies.Count == 0 || F >= MidAt + 240) StartMidboss();
			}
			return;
		}
		if (Rep(F, 3300, 24, 14, ref K))
		{
			bool bL = K % 2 == 0;
			Doll(new FVector2f(bL ? -16f : FieldW + 16f, 40f), new FVector2f(bL ? 1.6f : -1.6f, 1f), 14, true);
		}
		if (Rep(F, 3400, 20, 18, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(Rng.FRandRange(-0.4f, 0.4f), 2.1f), 21);
		if (Rep(F, 3800, 6, 50, ref K)) Sentry(new FVector2f(60f + K * 52f, -20f), 20, 60, 280).Color = RRed;
		if (F == 4100) { Spirit(new FVector2f(110f, -30f), 22, 520f); Spirit(new FVector2f(FieldW - 110f, -30f), 13, 520f); }
		if (Rep(F, 4300, 16, 22, ref K)) Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 70f), new FVector2f(K % 2 != 0 ? 2.2f : -2.2f, 0.4f), 19);
		if (Rep(F, 4850, 10, 40, ref K)) Sentry(new FVector2f(50f + (K % 6) * 57f, -20f), K % 2 != 0 ? 23 : 8, 60, 280).Color = RIvory;
		if (Rep(F, 5000, 24, 16, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 1.7f), 14);
		if (Rep(F, 5650, 44, 8, ref K)) Doll(new FVector2f(FieldW * 0.5f, -16f), Dir(90f + FMath.Sin(K * 0.45f) * 50f) * 1.9f, K % 4 == 0 ? 0 : (K % 4 == 2 ? 21 : -1));
		if (Rep(F, 6200, 12, 30, ref K)) Doll(new FVector2f(K % 2 != 0 ? 40f : FieldW - 40f, -16f), new FVector2f(K % 2 != 0 ? 1f : -1f, 1.6f), 14, true);
		if (F == 6600)
		{
			for (int i = 0; i < 4; ++i) Sentry(new FVector2f(64f + i * 85f, -20f), i % 2 != 0 ? 20 : 23, 60, 360).Color = Elem[i];
			Spirit(new FVector2f(FieldW * 0.5f, -30f), 22, 600f);
		}
		if (Rep(F, 6800, 20, 20, ref K)) Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 40f + (K % 3) * 25f), new FVector2f(K % 2 != 0 ? 2f : -2f, 0.6f), 0);
		if (F >= 7800 && (Enemies.Count == 0 || F >= 8100)) StartBoss();
	}

	// =====================================================================================
	// Enemy attacks (-1 = doesn't shoot)
	// =====================================================================================

	public void EnemyFire(FEnemy E)
	{
		int A = E.Age;
		Func<int, int, bool> Ev = (Interval, Offset) => RoadEvery(A, Interval, Offset);
		Func<int, int> RC = (N) => RoadCount(N);
		int Tier = FMath.Max(0, RoadTier());
		bool bStopped = E.StopAt >= 0 && A > E.StopAt && (E.LeaveAt < 0 || A < E.LeaveAt);
		switch (E.Pattern)
		{
		case 0:  // one aimed shot (Normal: a 3-way, Hard: twice, Lunatic: 5-way twice)
			if (A == 40 || (Tier >= 2 && A == 70))
			{
				if (Tier == 0) Fire(E.Pos, 2.4f, AimAngle(E.Pos), EBulletShape.Medium, RRed);
				else Fan(E.Pos, Tier >= 3 ? 5 : 3, Tier >= 3 ? 40f : 24f, 2.4f, AimAngle(E.Pos), EBulletShape.Medium, RRed);
			}
			break;
		case 1:  // rings while hovering
			if (bStopped && Ev(55, 0)) Ring(E.Pos, RC(14), 1.8f, A * 3f, EBulletShape.Small, RYellow);
			break;
		case 2:  // small aimed fans (a third one from Hard)
			if (A == 30 || A == 90 || (Tier >= 2 && A == 60)) Fan(E.Pos, RC(3), 24f + Tier * 6f, 2.5f, AimAngle(E.Pos), EBulletShape.Small, RGreen);
			break;
		case 3:  // big spirit: slow three-armed spiral and rings
			if (bStopped)
			{
				if (Ev(7, 0)) for (int k = 0; k < 3; ++k) Fire(E.Pos, 1.9f, k * 120f + A * 4.3f, EBulletShape.Rice, RPurple);
				if (Ev(75, 0)) Ring(E.Pos, RC(20), 1.2f, 7.5f, EBulletShape.Medium, RBlue, 0.015f, 2.4f);
			}
			break;
		case 4:  // one gentle aimed fan (a second one from Hard)
			if (A == 45 || (Tier >= 2 && A == 85)) Fan(E.Pos, RC(3), 20f + Tier * 8f, 2.3f, AimAngle(E.Pos), EBulletShape.Rice, ROrange);
			break;
		case 5:  // drips straight down
			if (A < 240 && Ev(32, 10)) Fire(E.Pos, 1.7f, 90f + Rng.FRandRange(-10f, 10f), EBulletShape.Water, RWater);
			break;
		case 6:  // a petal ring (twice from Hard)
			if (A == 40 || (Tier >= 2 && A == 90)) Ring(E.Pos, RC(8), 1.3f, Rng.FRandRange(0f, 45f), EBulletShape.Sakura, RPink);
			break;
		case 7:  // short aimed stream
			if (A < 160 && Ev(26, 10)) Fire(E.Pos, 2.5f, AimAngle(E.Pos), EBulletShape.Small, RBlue);
			break;
		case 8:  // two-armed spiral while hovering
			if (bStopped && Ev(9, 0)) foreach (float Off in new[] { 0f, 180f }) Fire(E.Pos, 1.8f, A * 5f + Off, EBulletShape.Rice, E.Color);
			break;
		case 9:  // petal spirit
			if (bStopped)
			{
				if (Ev(7, 0)) foreach (float Off in new[] { 0f, 180f }) Fire(E.Pos, 1.7f, A * 3.3f + Off, EBulletShape.Sakura, RPink);
				if (Ev(80, 40)) Ring(E.Pos, RC(16), 1.3f, 0f, EBulletShape.Sakura, new FLinearColor(1f, 0.45f, 0.7f));
			}
			break;
		case 10: // calls a lightning bolt onto the player's column
			if (A == 50) AddBeam(new FVector2f(FMath.Clamp(Player.Pos.X, 16f, FieldW - 16f), FieldH), -90f, 14f, 100, 4, 16, new FLinearColor(1f, 0.95f, 0.6f), 0);
			break;
		case 11: // drops plum seeds
			if (A == 40 || A == 100)
			{
				for (int k = 0; k < 2 + Tier / 2; ++k) SpawnPlumSeed(E.Pos + new FVector2f(Rng.FRandRange(-24f, 24f), Rng.FRandRange(-10f, 20f)), 25, RPink, 5, 1.2f);
			}
			break;
		case 12: // element-coloured rings
			if ((bStopped || E.StopAt < 0) && Ev(60, 20) && A < 400) Ring(E.Pos, RC(10), 1.6f, Rng.FRandRange(0f, 36f), EBulletShape.Medium, E.Color);
			break;
		case 13: // five-element spirit
			if (bStopped)
			{
				int El = (A / 180) % 5;
				if (Ev(22, 0)) Ring(E.Pos, RC(9), 1.7f, A * 3f, EBulletShape.Medium, Elem[El]);
				if (Ev(90, 45)) Fan(E.Pos, RC(5), 36f, 2.4f, AimAngle(E.Pos), EBulletShape.Rice, Elem[(El + 1) % 5]);
			}
			break;
		case 14: // tosses tiles
			if (A < 200 && Ev(45, 20)) Fan(E.Pos, RC(3), 18f, 2.3f, AimAngle(E.Pos), EBulletShape.Tile, RIvory);
			break;
		case 15: // sakura rings while hovering
			if (bStopped && Ev(60, 0)) Ring(E.Pos, RC(12), 1.6f, A * 2f, EBulletShape.Sakura, E.Color);
			break;
		case 16: // snowflakes
			if (A < 260 && Ev(36, 12))
			{
				FBullet B = Fire(E.Pos, 1.3f, 90f + Rng.FRandRange(-20f, 20f), EBulletShape.Snow, new FLinearColor(0.85f, 0.93f, 1f));
				B.Scale = 0.85f;
			}
			break;
		case 17: // storm spirit: water arcs and rings
			if (bStopped)
			{
				if (Ev(9, 0))
				{
					FBullet W = FireVec(E.Pos, Dir(Rng.FRandRange(235f, 305f)) * Rng.FRandRange(2.6f, 3.4f), EBulletShape.Water, RWater);
					W.Gravity = new FVector2f(0f, 0.05f);
				}
				if (Ev(80, 0)) Ring(E.Pos, RC(16), 1.4f, 0f, EBulletShape.Water, RBlue);
			}
			break;
		case 18: // chrysanthemum spirit
			if (bStopped && Ev(30, 0))
			{
				for (int k = 0; k < RoadCount(8, true); ++k)
				{
					FBullet B = Fire(E.Pos, Rng.FRandRange(1.2f, 1.6f), Rng.FRandRange(0f, 360f), EBulletShape.Chrys, new FLinearColor(1f, 0.85f, 0.35f));
					B.Spin = 3f;
				}
			}
			break;
		case 19: // quick aimed shots in a row (3 on Easy, one more per difficulty step)
			if (A >= 30 && A <= 30 + 8 * (2 + Tier) && (A - 30) % 8 == 0) Fire(E.Pos, 2.7f, AimAngle(E.Pos), EBulletShape.Rice, E.Color);
			break;
		case 20: // tile spiral while hovering
			if (bStopped && Ev(6, 0)) for (int k = 0; k < 3; ++k) Fire(E.Pos, 1.9f, A * 4.7f + k * 120f, EBulletShape.Tile, RIvory);
			break;
		case 21: // a ring of tiles that tumble down
			if (A == 40)
			{
				int N = RoadCount(10, true);
				for (int k = 0; k < N; ++k)
				{
					FBullet B = FireVec(E.Pos, Dir(360f * k / N) * 1.6f, EBulletShape.Tile, RIvory);
					B.Gravity = new FVector2f(0f, 0.02f);
				}
			}
			break;
		case 22: // mahjong spirit: two counter-rotating tile spirals and aimed red fans
			if (bStopped)
			{
				if (Ev(6, 0))
				{
					foreach (float Off in new[] { 0f, 120f, 240f })
					{
						Fire(E.Pos, 1.8f, A * 3.1f + Off, EBulletShape.Tile, RIvory);
						Fire(E.Pos, 1.5f, -A * 2.3f + Off, EBulletShape.Tile, RRed);
					}
				}
				if (Ev(70, 35)) Fan(E.Pos, 7, 48f, 2.6f, AimAngle(E.Pos), EBulletShape.Rice, RRed);
			}
			break;
		case 23: // marks the player with a red line, then throws a column of tiles down it
			if (bStopped && Ev(90, 0))
			{
				float Aim = AimAngle(E.Pos);
				AddBeam(E.Pos, Aim, 2f, 40, 1, 1, new FLinearColor(1f, 0.3f, 0.3f)).bHarmless = true;
				for (int i = 0; i < 6; ++i)
				{
					FBullet B = FireVec(E.Pos, V2.ZeroVector, EBulletShape.Tile, RIvory);
					B.Behavior = BehDelayedRelease;
					B.Param = 1;
					B.Timer = 40 + i * 4;
					B.Center = Dir(Aim) * 3.4f;
				}
			}
			break;
		default:
			break;
		}
	}
}
