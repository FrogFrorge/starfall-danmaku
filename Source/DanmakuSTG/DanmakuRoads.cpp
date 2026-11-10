// Stage roads (道中): a different timeline of enemy waves for every stage, and the enemy attack patterns.
// Road lengths: stage 1 ~75 s, 2 ~90 s, 3 ~100 s, 4 ~110 s, 5 ~120 s, 6 ~60 s, Extra ~130 s plus a midboss.
// Enemy fire uses Every()/Cnt(), so it thins out on lower difficulties like the boss patterns do.

#include "DanmakuWorld.h"

using namespace Danmaku;

namespace
{
	const FLinearColor RRed(1.f, 0.3f, 0.3f);
	const FLinearColor RYellow(1.f, 0.9f, 0.35f);
	const FLinearColor RGreen(0.45f, 1.f, 0.5f);
	const FLinearColor RBlue(0.4f, 0.6f, 1.f);
	const FLinearColor RPurple(0.7f, 0.45f, 1.f);
	const FLinearColor ROrange(1.f, 0.6f, 0.25f);
	const FLinearColor RPink(1.f, 0.6f, 0.8f);
	const FLinearColor RWater(0.55f, 0.8f, 1.f);
	const FLinearColor RIvory(1.f, 0.96f, 0.85f);
	const FLinearColor Elem[5] = { FLinearColor(0.4f, 1.f, 0.45f), FLinearColor(1.f, 0.45f, 0.25f), FLinearColor(0.95f, 0.8f, 0.35f),
		FLinearColor(0.92f, 0.95f, 1.f), FLinearColor(0.35f, 0.6f, 1.f) };

	// set by UpdateStage from the difficulty: how many more dolls the fast waves squeeze in
	float GRoadDensity = 1.f;

	/** True on the frames of a wave: Count spawns, Interval frames apart, starting at Start. K = spawn index.
	 *  On harder roads, fast waves (Interval <= 30) fit more spawns into the same stretch of time; K still runs
	 *  0..Count-1 (neighbouring spawns share a K), so positions computed from K stay on screen. */
	bool Rep(int32 F, int32 Start, int32 Count, int32 Interval, int32& K)
	{
		if (F < Start) return false;
		const int32 D = F - Start;
		if (GRoadDensity <= 1.f || Interval > 30)
		{
			if (D % Interval != 0) return false;
			K = D / Interval;
			return K < Count;
		}
		const int32 C = FMath::RoundToInt(Count * GRoadDensity);
		const float Step = float(Count * Interval) / float(C);
		const int32 J = FMath::RoundToInt(D / Step);
		if (J >= C || FMath::RoundToInt(J * Step) != D) return false;
		K = FMath::Min(Count - 1, J * Count / C);
		return true;
	}
}

bool ADanmakuWorld::RoadEvery(int32 Age, int32 Interval, int32 Offset) const
{
	const int32 Tier = RoadTier();
	if (Tier < 0) return Every(Age, Interval, Offset);
	// Easy is the old road; each step up fires more often
	static const float Iv[4] = { 1.f, 0.8f, 0.67f, 0.56f };
	const int32 N = FMath::Max(1, FMath::RoundToInt(Interval * Iv[Tier]));
	return Age % N == FMath::RoundToInt(Offset * Iv[Tier]) % N;
}

int32 ADanmakuWorld::RoadCount(int32 N, bool bWasScaled) const
{
	const int32 Tier = RoadTier();
	if (Tier < 0) return bWasScaled ? Cnt(N) : N;
	static const float Mul[4] = { 1.f, 1.3f, 1.6f, 2.f };
	return FMath::Max(1, FMath::RoundToInt(N * Mul[Tier]));
}

float ADanmakuWorld::RoadDensity() const
{
	const int32 Tier = RoadTier();
	static const float Dn[4] = { 1.f, 1.2f, 1.4f, 1.6f };
	return Tier < 0 ? 1.f : Dn[Tier];
}

// =====================================================================================
// Spawning helpers. Road enemies are kept fragile: swarm dolls die in a few hits, sentries and
// spirits take a moment but never stall the road.
// =====================================================================================

FEnemy& ADanmakuWorld::Doll(const FVector2f& Pos, const FVector2f& Vel, int32 Pattern, bool bRed)
{
	FEnemy& E = SpawnEnemy(Pos, Vel, bRed ? 1 : 0, bRed ? 9.f : 3.f, Pattern, -1, -1, FVector2f::ZeroVector, 1, 1);
	E.bRevenge = StageIndex >= ExtraStage || (RoadTier() >= 3 && StageIndex >= 1);
	return E;
}

FEnemy& ADanmakuWorld::Sentry(const FVector2f& Pos, int32 Pattern, int32 StopAt, int32 LeaveAt, float HP)
{
	FEnemy& E = SpawnEnemy(Pos, FVector2f(0.f, 2.f), 1, HP * 0.75f, Pattern, StopAt, LeaveAt, FVector2f(0.f, -1.4f), 4, 2);
	E.bRevenge = StageIndex >= ExtraStage || (RoadTier() >= 3 && StageIndex >= 1) || (RoadTier() == 2 && StageIndex >= 3);
	return E;
}

FEnemy& ADanmakuWorld::Spirit(const FVector2f& Pos, int32 Pattern, float HP)
{
	return SpawnEnemy(Pos, FVector2f(0.f, 1.5f), 2, HP * 0.7f, Pattern, 90, 690, FVector2f(0.f, -1.f), 8, 8);
}

void ADanmakuWorld::UpdateStage()
{
	if (Boss.bStarted) return;
	const int32 F = StageFrame;
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
void ADanmakuWorld::RoadStage1(int32 F)
{
	int32 K;
	if (Rep(F, 90, 16, 16, K))   Doll(FVector2f(-16.f, 50.f + (K % 3) * 30.f), FVector2f(2.2f, 0.8f), K % 2 ? 0 : -1);
	if (Rep(F, 330, 16, 16, K))  Doll(FVector2f(FieldW + 16.f, 50.f + (K % 3) * 30.f), FVector2f(-2.2f, 0.8f), K % 2 ? 0 : -1);
	if (F == 640) for (int32 i = 0; i < 3; ++i) Sentry(FVector2f(96.f + i * 96.f, -20.f), 1, 60, 300);
	if (Rep(F, 900, 20, 20, K))  Doll(FVector2f(Rng.FRandRange(40.f, FieldW - 40.f), -16.f), FVector2f(Rng.FRandRange(-0.4f, 0.4f), 1.6f), K % 2 ? 2 : 0);
	if (F == 1400) Spirit(FVector2f(FieldW * 0.5f, -30.f), 3, 450.f);
	if (Rep(F, 1500, 8, 40, K))  Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 40.f), FVector2f(K % 2 ? 2.f : -2.f, 1.f), 0);
	if (Rep(F, 2100, 12, 16, K))
	{
		for (float Sgn : { -1.f, 1.f }) Doll(FVector2f(FieldW * 0.5f + Sgn * (20.f + K * 14.f), -16.f), FVector2f(0.f, 1.9f), K % 2 == 0 ? 0 : -1);
	}
	if (F == 2600) { Sentry(FVector2f(80.f, -20.f), 8, 70, 330); Sentry(FVector2f(FieldW - 80.f, -20.f), 8, 70, 330); }
	if (Rep(F, 2700, 16, 18, K)) Doll(FVector2f(Rng.FRandRange(40.f, FieldW - 40.f), -16.f), FVector2f(0.f, 2.1f), -1);
	if (Rep(F, 3150, 16, 24, K))
	{
		const bool bL = K % 2 == 0;
		Doll(FVector2f(bL ? -16.f : FieldW + 16.f, 30.f + (K % 4) * 12.f), FVector2f(bL ? 1.6f : -1.6f, 1.1f), 4);
	}
	if (F == 3700) for (int32 i = 0; i < 4; ++i) Sentry(FVector2f(64.f + i * 85.f, -20.f), 1, 60 + i * 10, 300);
	if (Rep(F, 3900, 10, 30, K)) Doll(FVector2f(48.f + (K % 5) * 72.f, -16.f), FVector2f(0.f, 1.5f), 2);
	if (F >= 4500 && (Enemies.Num() == 0 || F >= 4800)) StartBoss();
}

// ---------- stage 2: 四時繪卷 (~90 s) ----------
void ADanmakuWorld::RoadStage2(int32 F)
{
	int32 K;
	if (Rep(F, 90, 24, 14, K))
	{
		FEnemy& E = Doll(FVector2f(60.f + (K % 8) * 38.f, -16.f), FVector2f(0.f, 1.5f), K % 2 ? 6 : 0);
		E.SineAmp = 1.2f; E.SineFreq = 0.05f; E.SinePhase = K;
	}
	if (Rep(F, 520, 14, 24, K))
	{
		Doll(FVector2f(-16.f, -10.f), FVector2f(1.6f, 1.4f), 0);
		Doll(FVector2f(FieldW + 16.f, -10.f), FVector2f(-1.6f, 1.4f), K % 2 ? 2 : -1);
	}
	if (F == 1000) for (int32 i = 0; i < 3; ++i) Sentry(FVector2f(96.f + i * 96.f, -20.f), 15, 60, 320).Color = RPink;
	if (Rep(F, 1150, 10, 30, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 1.3f), 16);
	if (Rep(F, 1500, 26, 12, K))
	{
		FEnemy& E = Doll(FVector2f(FieldW + 16.f, 40.f + (K % 5) * 25.f), FVector2f(-1.9f, 0.3f), 5);
		E.SineAmp = 0.6f; E.SineFreq = 0.07f;
	}
	if (F == 2300) Spirit(FVector2f(FieldW * 0.5f, -30.f), 9, 500.f);
	if (Rep(F, 2450, 8, 50, K)) Doll(FVector2f(K % 2 ? 40.f : FieldW - 40.f, -16.f), FVector2f(K % 2 ? 1.2f : -1.2f, 1.6f), 6, true);
	if (Rep(F, 3100, 20, 18, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 1.2f), 16);
	if (Rep(F, 3300, 6, 70, K)) Sentry(FVector2f(60.f + K * 52.f, -20.f), 15, 60, 260).Color = K % 2 ? RWater : RIvory;
	if (Rep(F, 3800, 12, 34, K))
	{
		FEnemy& E = Doll(FVector2f(K % 2 ? 60.f : FieldW - 60.f, -16.f), FVector2f(K % 2 ? 0.8f : -0.8f, 1.6f), 2, true);
		E.Acc = FVector2f(K % 2 ? -0.012f : 0.012f, 0.f);
	}
	if (Rep(F, 4200, 20, 14, K))
	{
		FEnemy& E = Doll(FVector2f(-16.f, 40.f + (K % 5) * 25.f), FVector2f(1.9f, 0.3f), K % 3 == 0 ? 0 : -1);
		E.SineAmp = 0.6f; E.SineFreq = 0.07f;
	}
	if (F == 4500) { Sentry(FVector2f(90.f, -20.f), 15, 70, 360).Color = RGreen; Sentry(FVector2f(FieldW - 90.f, -20.f), 15, 70, 360).Color = ROrange; }
	if (Rep(F, 4700, 12, 24, K)) Doll(FVector2f(FieldW * 0.5f + (K % 2 ? 1.f : -1.f) * (40.f + K * 8.f), -16.f), FVector2f(0.f, 1.7f), 6);
	if (F >= 5400 && (Enemies.Num() == 0 || F >= 5700)) StartBoss();
}

// ---------- stage 3: 雲上之路 (~100 s) ----------
void ADanmakuWorld::RoadStage3(int32 F)
{
	int32 K;
	if (Rep(F, 90, 24, 12, K))  Doll(FVector2f(-16.f, 60.f + (K % 5) * 22.f), FVector2f(3.f, 0.25f), K % 2 ? 7 : -1);
	if (Rep(F, 420, 24, 12, K)) Doll(FVector2f(FieldW + 16.f, 60.f + (K % 5) * 22.f), FVector2f(-3.f, 0.25f), K % 2 ? 7 : -1);
	if (Rep(F, 900, 6, 50, K))  Sentry(FVector2f(60.f + K * 52.f, -20.f), 10, 50, 200, 30.f);
	if (Rep(F, 1000, 14, 22, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 1.8f), 0);
	if (Rep(F, 1400, 24, 16, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(Rng.FRandRange(-0.3f, 0.3f), 1.3f), 5);
	if (F == 2100) Spirit(FVector2f(FieldW * 0.5f, -30.f), 17, 550.f);
	if (Rep(F, 2250, 10, 36, K)) Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 50.f), FVector2f(K % 2 ? 2.4f : -2.4f, 0.6f), 7, true);
	if (Rep(F, 2850, 32, 10, K))
	{
		const bool bL = (K / 4) % 2 == 0;
		Doll(FVector2f(bL ? -16.f : FieldW + 16.f, 30.f + (K % 4) * 30.f), FVector2f(bL ? 2.6f : -2.6f, 0.4f), K % 4 == 0 ? 0 : (K % 4 == 2 ? 7 : -1));
	}
	if (Rep(F, 3500, 7, 60, K)) Sentry(FVector2f(Rng.FRandRange(60.f, FieldW - 60.f), -20.f), 10, 50, 220, 30.f);
	if (Rep(F, 3600, 14, 26, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 1.4f), 5);
	if (Rep(F, 4200, 22, 18, K))
	{
		const bool bL = K % 2 == 0;
		Doll(FVector2f(bL ? 20.f : FieldW - 20.f, -16.f), FVector2f(bL ? 1.2f : -1.2f, 1.6f), K % 3 == 0 ? 19 : 4);
	}
	if (F == 4900) for (int32 i = 0; i < 3; ++i) Sentry(FVector2f(96.f + i * 96.f, -20.f), 1, 60, 300);
	if (Rep(F, 5000, 16, 20, K)) Doll(FVector2f(-16.f + (K % 2) * (FieldW + 32.f), 80.f + (K % 3) * 20.f), FVector2f(K % 2 ? -2.8f : 2.8f, 0.2f), 7);
	if (F >= 6000 && (Enemies.Num() == 0 || F >= 6300)) StartBoss();
}

// ---------- stage 4: 四君子庭園 (~110 s) ----------
void ADanmakuWorld::RoadStage4(int32 F)
{
	int32 K;
	if (Rep(F, 90, 16, 24, K))   Doll(FVector2f(48.f + (K % 6) * 58.f, -16.f), FVector2f(0.f, 1.5f), 11);
	if (Rep(F, 300, 16, 24, K))  Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 2.f), 0);
	if (Rep(F, 700, 22, 16, K))
	{
		FEnemy& E = Doll(FVector2f(FieldW * 0.5f, -16.f), FVector2f(0.f, 1.8f), 2);
		E.Acc = FVector2f(K % 2 ? 0.03f : -0.03f, -0.004f);
	}
	if (F == 1300) for (int32 i = 0; i < 4; ++i) Sentry(FVector2f(64.f + i * 85.f, -20.f), 8, 60, 330).Color = RPurple;
	if (Rep(F, 1900, 28, 14, K)) Doll(FVector2f(K % 2 ? 110.f : FieldW - 110.f, -16.f), FVector2f(0.f, 2.f), K % 4 == 0 ? 0 : (K % 4 == 2 ? 19 : -1));
	if (F == 2600) Spirit(FVector2f(FieldW * 0.5f, -30.f), 18, 600.f);
	if (Rep(F, 2700, 8, 50, K))  Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 30.f), FVector2f(K % 2 ? 1.6f : -1.6f, 0.8f), 11, true);
	if (Rep(F, 3350, 14, 30, K))
	{
		const bool bL = K % 2 == 0;
		Doll(FVector2f(bL ? -16.f : FieldW + 16.f, 50.f), FVector2f(bL ? 1.3f : -1.3f, 0.9f), 11, true);
	}
	if (Rep(F, 4000, 32, 11, K))
	{
		FEnemy& E = Doll(FVector2f(40.f + (K % 8) * 43.f, -16.f), FVector2f(0.f, 1.4f), K % 3 == 0 ? 6 : (K % 3 == 1 ? 0 : -1));
		E.SineAmp = 1.f; E.SineFreq = 0.04f; E.SinePhase = K * 0.7f;
	}
	if (Rep(F, 4900, 7, 55, K)) Sentry(FVector2f(60.f + (K % 6) * 52.f, -20.f), 15, 60, 300).Color = RYellow;
	if (Rep(F, 5200, 16, 22, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 1.8f), 11);
	if (F == 5800) { Sentry(FVector2f(120.f, -20.f), 1, 60, 320); Sentry(FVector2f(FieldW - 120.f, -20.f), 1, 60, 320); Sentry(FVector2f(FieldW * 0.5f, -20.f), 18, 80, 340, 60.f); }
	if (F >= 6600 && (Enemies.Num() == 0 || F >= 6900)) StartBoss();
}

// ---------- stage 5: 五行仙壇 (~120 s) ----------
void ADanmakuWorld::RoadStage5(int32 F)
{
	int32 K;
	if (Rep(F, 90, 34, 11, K))
	{
		FEnemy& E = Doll(FVector2f(FieldW * 0.5f + FMath::Sin(K * 0.6f) * 120.f, -16.f), FVector2f(0.f, 1.6f), K % 3 == 0 ? 12 : (K % 3 == 1 ? 0 : -1));
		E.Color = Elem[K % 5];
	}
	if (Rep(F, 700, 12, 34, K)) Sentry(FVector2f(50.f + (K % 5) * 71.f, -20.f), 12, 50, 240, 30.f).Color = Elem[K % 5];
	if (Rep(F, 800, 16, 24, K)) Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 30.f + (K % 4) * 20.f), FVector2f(K % 2 ? 2.f : -2.f, 0.7f), 19).Color = Elem[K % 5];
	if (F == 1500) Spirit(FVector2f(FieldW * 0.5f, -30.f), 13, 650.f);
	if (Rep(F, 1650, 10, 40, K)) Doll(FVector2f(Rng.FRandRange(40.f, FieldW - 40.f), -16.f), FVector2f(0.f, 1.5f), 12, true).Color = Elem[K % 5];
	if (Rep(F, 2350, 26, 14, K))
	{
		const bool bL = K % 2 == 0;
		Doll(FVector2f(bL ? -16.f : FieldW + 16.f, 20.f + (K % 3) * 20.f), FVector2f(bL ? 1.4f : -1.4f, 1.2f), K % 3 == 0 ? 19 : 4);
	}
	if (Rep(F, 3100, 5, 70, K)) Sentry(FVector2f(80.f + K * 56.f, -20.f), 8, 60, 260).Color = Elem[K];
	if (Rep(F, 3200, 20, 18, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 2.f), 0).Color = Elem[K % 5];
	if (F == 3850) { Spirit(FVector2f(110.f, -30.f), 13, 420.f); Spirit(FVector2f(FieldW - 110.f, -30.f), 13, 420.f); }
	if (Rep(F, 4700, 40, 8, K))
	{
		const float A = 90.f + FMath::Sin(K * 0.5f) * 40.f;
		Doll(FVector2f(FieldW * 0.5f, -16.f), Dir(A) * 1.8f, K % 4 == 0 ? 0 : (K % 4 == 2 ? 2 : -1)).Color = Elem[K % 5];
	}
	if (Rep(F, 5500, 8, 50, K)) Sentry(FVector2f(60.f + (K % 6) * 52.f, -20.f), K % 2 ? 1 : 12, 60, 280).Color = Elem[K % 5];
	if (Rep(F, 5700, 18, 20, K)) Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 60.f), FVector2f(K % 2 ? 1.8f : -1.8f, 1.f), 19).Color = Elem[K % 5];
	if (F == 6300) for (int32 i = 0; i < 3; ++i) Sentry(FVector2f(96.f + i * 96.f, -20.f), 12, 60, 340).Color = Elem[i * 2];
	if (F >= 7200 && (Enemies.Num() == 0 || F >= 7500)) StartBoss();
}

// ---------- stage 6: 天宮 (~60 s, the final boss comes quickly) ----------
void ADanmakuWorld::RoadStage6(int32 F)
{
	int32 K;
	if (Rep(F, 90, 22, 12, K))
	{
		for (float Sgn : { -1.f, 1.f }) Doll(FVector2f(FieldW * 0.5f + Sgn * (24.f + K * 10.f), -16.f), FVector2f(Sgn * 0.3f, 1.9f), K % 3 == 0 ? 2 : (K % 3 == 1 ? 0 : -1), K % 4 == 0).Color = RYellow;
	}
	if (F == 560) for (int32 i = 0; i < 4; ++i) Sentry(FVector2f(64.f + i * 85.f, -20.f), 8, 60, 320).Color = RYellow;
	if (Rep(F, 700, 16, 20, K)) Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 40.f + (K % 3) * 20.f), FVector2f(K % 2 ? 2.2f : -2.2f, 0.7f), 19).Color = RYellow;
	if (F == 1150) Spirit(FVector2f(FieldW * 0.5f, -30.f), 3, 700.f);
	if (Rep(F, 1250, 8, 50, K)) Doll(FVector2f(Rng.FRandRange(40.f, FieldW - 40.f), -16.f), FVector2f(0.f, 1.6f), 2, true).Color = RYellow;
	if (Rep(F, 1850, 26, 12, K))
	{
		const bool bL = K % 2 == 0;
		Doll(FVector2f(bL ? -16.f : FieldW + 16.f, 40.f + (K % 4) * 25.f), FVector2f(bL ? 2.f : -2.f, 0.8f), K % 3 == 0 ? 19 : 4);
	}
	if (F == 2600) for (int32 i = 0; i < 4; ++i) Sentry(FVector2f(64.f + i * 85.f, -20.f), i % 2 ? 1 : 8, 60, 300).Color = RYellow;
	if (Rep(F, 2700, 16, 18, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 2.1f), 0).Color = RYellow;
	if (F >= 3600 && (Enemies.Num() == 0 || F >= 3900)) StartBoss();
}

// ---------- Extra: 天和閣 (~130 s, 玄 appears at the halfway point) ----------
// Extra dolls and sentries burst into a small ring when they die, and the waves overlap.
void ADanmakuWorld::RoadExtra(int32 F)
{
	constexpr int32 MidAt = 3000;
	int32 K;
	if (Rep(F, 90, 28, 12, K))
	{
		const bool bL = K % 2 == 0;
		Doll(FVector2f(bL ? 30.f : FieldW - 30.f, -16.f), FVector2f(bL ? 0.8f : -0.8f, 1.8f), 14);
	}
	if (Rep(F, 300, 24, 14, K)) Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 40.f + (K % 4) * 22.f), FVector2f(K % 2 ? 2.4f : -2.4f, 0.5f), K % 2 ? 19 : 0);
	if (Rep(F, 700, 6, 45, K)) Sentry(FVector2f(60.f + K * 52.f, -20.f), 20, 60, 300).Color = RRed;
	if (Rep(F, 1000, 20, 16, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 2.f), 21);
	if (F == 1400) Spirit(FVector2f(FieldW * 0.5f, -30.f), 22, 650.f);
	if (Rep(F, 1500, 10, 40, K)) Doll(FVector2f(K % 2 ? 50.f : FieldW - 50.f, -16.f), FVector2f(0.f, 1.5f), 14, true);
	if (Rep(F, 2100, 36, 10, K))
	{
		FEnemy& E = Doll(FVector2f(40.f + (K % 8) * 43.f, -16.f), FVector2f(0.f, 1.5f), K % 3 == 0 ? 21 : 5);
		E.SineAmp = 0.8f; E.SineFreq = 0.05f; E.SinePhase = K;
	}
	if (Rep(F, 2300, 4, 60, K)) Sentry(FVector2f(80.f + K * 75.f, -20.f), 23, 60, 260).Color = RIvory;
	if (!bMidDone)
	{
		if (F >= MidAt)
		{
			if (bDemo && DemoSkipFrames > MidAt) bMidDone = true;
			else if (Enemies.Num() == 0 || F >= MidAt + 240) StartMidboss();
		}
		return;
	}
	if (Rep(F, 3300, 24, 14, K))
	{
		const bool bL = K % 2 == 0;
		Doll(FVector2f(bL ? -16.f : FieldW + 16.f, 40.f), FVector2f(bL ? 1.6f : -1.6f, 1.f), 14, true);
	}
	if (Rep(F, 3400, 20, 18, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(Rng.FRandRange(-0.4f, 0.4f), 2.1f), 21);
	if (Rep(F, 3800, 6, 50, K)) Sentry(FVector2f(60.f + K * 52.f, -20.f), 20, 60, 280).Color = RRed;
	if (F == 4100) { Spirit(FVector2f(110.f, -30.f), 22, 520.f); Spirit(FVector2f(FieldW - 110.f, -30.f), 13, 520.f); }
	if (Rep(F, 4300, 16, 22, K)) Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 70.f), FVector2f(K % 2 ? 2.2f : -2.2f, 0.4f), 19);
	if (Rep(F, 4850, 10, 40, K)) Sentry(FVector2f(50.f + (K % 6) * 57.f, -20.f), K % 2 ? 23 : 8, 60, 280).Color = RIvory;
	if (Rep(F, 5000, 24, 16, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 1.7f), 14);
	if (Rep(F, 5650, 44, 8, K)) Doll(FVector2f(FieldW * 0.5f, -16.f), Dir(90.f + FMath::Sin(K * 0.45f) * 50.f) * 1.9f, K % 4 == 0 ? 0 : (K % 4 == 2 ? 21 : -1));
	if (Rep(F, 6200, 12, 30, K)) Doll(FVector2f(K % 2 ? 40.f : FieldW - 40.f, -16.f), FVector2f(K % 2 ? 1.f : -1.f, 1.6f), 14, true);
	if (F == 6600)
	{
		for (int32 i = 0; i < 4; ++i) Sentry(FVector2f(64.f + i * 85.f, -20.f), i % 2 ? 20 : 23, 60, 360).Color = Elem[i];
		Spirit(FVector2f(FieldW * 0.5f, -30.f), 22, 600.f);
	}
	if (Rep(F, 6800, 20, 20, K)) Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 40.f + (K % 3) * 25.f), FVector2f(K % 2 ? 2.f : -2.f, 0.6f), 0);
	if (F >= 7800 && (Enemies.Num() == 0 || F >= 8100)) StartBoss();
}

// =====================================================================================
// Enemy attacks (-1 = doesn't shoot)
// =====================================================================================

void ADanmakuWorld::EnemyFire(FEnemy& E)
{
	const int32 A = E.Age;
	auto Ev = [this, A](int32 Interval, int32 Offset) { return RoadEvery(A, Interval, Offset); };
	auto RC = [this](int32 N) { return RoadCount(N); };
	const int32 Tier = FMath::Max(0, RoadTier());
	const bool bStopped = E.StopAt >= 0 && A > E.StopAt && (E.LeaveAt < 0 || A < E.LeaveAt);
	switch (E.Pattern)
	{
	case 0:  // one aimed shot (Normal: a 3-way, Hard: twice, Lunatic: 5-way twice)
		if (A == 40 || (Tier >= 2 && A == 70))
		{
			if (Tier == 0) Fire(E.Pos, 2.4f, AimAngle(E.Pos), EBulletShape::Medium, RRed);
			else Fan(E.Pos, Tier >= 3 ? 5 : 3, Tier >= 3 ? 40.f : 24.f, 2.4f, AimAngle(E.Pos), EBulletShape::Medium, RRed);
		}
		break;
	case 1:  // rings while hovering
		if (bStopped && Ev(55, 0)) Ring(E.Pos, RC(14), 1.8f, A * 3.f, EBulletShape::Small, RYellow);
		break;
	case 2:  // small aimed fans (a third one from Hard)
		if (A == 30 || A == 90 || (Tier >= 2 && A == 60)) Fan(E.Pos, RC(3), 24.f + Tier * 6.f, 2.5f, AimAngle(E.Pos), EBulletShape::Small, RGreen);
		break;
	case 3:  // big spirit: slow three-armed spiral and rings
		if (bStopped)
		{
			if (Ev(7, 0)) for (int32 k = 0; k < 3; ++k) Fire(E.Pos, 1.9f, k * 120.f + A * 4.3f, EBulletShape::Rice, RPurple);
			if (Ev(75, 0)) Ring(E.Pos, RC(20), 1.2f, 7.5f, EBulletShape::Medium, RBlue, 0.015f, 2.4f);
		}
		break;
	case 4:  // one gentle aimed fan (a second one from Hard)
		if (A == 45 || (Tier >= 2 && A == 85)) Fan(E.Pos, RC(3), 20.f + Tier * 8.f, 2.3f, AimAngle(E.Pos), EBulletShape::Rice, ROrange);
		break;
	case 5:  // drips straight down
		if (A < 240 && Ev(32, 10)) Fire(E.Pos, 1.7f, 90.f + Rng.FRandRange(-10.f, 10.f), EBulletShape::Water, RWater);
		break;
	case 6:  // a petal ring (twice from Hard)
		if (A == 40 || (Tier >= 2 && A == 90)) Ring(E.Pos, RC(8), 1.3f, Rng.FRandRange(0.f, 45.f), EBulletShape::Sakura, RPink);
		break;
	case 7:  // short aimed stream
		if (A < 160 && Ev(26, 10)) Fire(E.Pos, 2.5f, AimAngle(E.Pos), EBulletShape::Small, RBlue);
		break;
	case 8:  // two-armed spiral while hovering
		if (bStopped && Ev(9, 0)) for (float Off : { 0.f, 180.f }) Fire(E.Pos, 1.8f, A * 5.f + Off, EBulletShape::Rice, E.Color);
		break;
	case 9:  // petal spirit
		if (bStopped)
		{
			if (Ev(7, 0)) for (float Off : { 0.f, 180.f }) Fire(E.Pos, 1.7f, A * 3.3f + Off, EBulletShape::Sakura, RPink);
			if (Ev(80, 40)) Ring(E.Pos, RC(16), 1.3f, 0.f, EBulletShape::Sakura, FLinearColor(1.f, 0.45f, 0.7f));
		}
		break;
	case 10: // calls a lightning bolt onto the player's column
		if (A == 50) AddBeam(FVector2f(FMath::Clamp(Player.Pos.X, 16.f, FieldW - 16.f), FieldH), -90.f, 14.f, 100, 4, 16, FLinearColor(1.f, 0.95f, 0.6f), 0);
		break;
	case 11: // drops plum seeds
		if (A == 40 || A == 100)
		{
			for (int32 k = 0; k < 2 + Tier / 2; ++k) SpawnPlumSeed(E.Pos + FVector2f(Rng.FRandRange(-24.f, 24.f), Rng.FRandRange(-10.f, 20.f)), 25, RPink, 5, 1.2f);
		}
		break;
	case 12: // element-coloured rings
		if ((bStopped || E.StopAt < 0) && Ev(60, 20) && A < 400) Ring(E.Pos, RC(10), 1.6f, Rng.FRandRange(0.f, 36.f), EBulletShape::Medium, E.Color);
		break;
	case 13: // five-element spirit
		if (bStopped)
		{
			const int32 El = (A / 180) % 5;
			if (Ev(22, 0)) Ring(E.Pos, RC(9), 1.7f, A * 3.f, EBulletShape::Medium, Elem[El]);
			if (Ev(90, 45)) Fan(E.Pos, RC(5), 36.f, 2.4f, AimAngle(E.Pos), EBulletShape::Rice, Elem[(El + 1) % 5]);
		}
		break;
	case 14: // tosses tiles
		if (A < 200 && Ev(45, 20)) Fan(E.Pos, RC(3), 18.f, 2.3f, AimAngle(E.Pos), EBulletShape::Tile, RIvory);
		break;
	case 15: // sakura rings while hovering
		if (bStopped && Ev(60, 0)) Ring(E.Pos, RC(12), 1.6f, A * 2.f, EBulletShape::Sakura, E.Color);
		break;
	case 16: // snowflakes
		if (A < 260 && Ev(36, 12))
		{
			FBullet& B = Fire(E.Pos, 1.3f, 90.f + Rng.FRandRange(-20.f, 20.f), EBulletShape::Snow, FLinearColor(0.85f, 0.93f, 1.f));
			B.Scale = 0.85f;
		}
		break;
	case 17: // storm spirit: water arcs and rings
		if (bStopped)
		{
			if (Ev(9, 0))
			{
				FBullet& W = FireVec(E.Pos, Dir(Rng.FRandRange(235.f, 305.f)) * Rng.FRandRange(2.6f, 3.4f), EBulletShape::Water, RWater);
				W.Gravity = FVector2f(0.f, 0.05f);
			}
			if (Ev(80, 0)) Ring(E.Pos, RC(16), 1.4f, 0.f, EBulletShape::Water, RBlue);
		}
		break;
	case 18: // chrysanthemum spirit
		if (bStopped && Ev(30, 0))
		{
			for (int32 k = 0; k < RoadCount(8, true); ++k)
			{
				FBullet& B = Fire(E.Pos, Rng.FRandRange(1.2f, 1.6f), Rng.FRandRange(0.f, 360.f), EBulletShape::Chrys, FLinearColor(1.f, 0.85f, 0.35f));
				B.Spin = 3.f;
			}
		}
		break;
	case 19: // quick aimed shots in a row (3 on Easy, one more per difficulty step)
		if (A >= 30 && A <= 30 + 8 * (2 + Tier) && (A - 30) % 8 == 0) Fire(E.Pos, 2.7f, AimAngle(E.Pos), EBulletShape::Rice, E.Color);
		break;
	case 20: // tile spiral while hovering
		if (bStopped && Ev(6, 0)) for (int32 k = 0; k < 3; ++k) Fire(E.Pos, 1.9f, A * 4.7f + k * 120.f, EBulletShape::Tile, RIvory);
		break;
	case 21: // a ring of tiles that tumble down
		if (A == 40)
		{
			const int32 N = RoadCount(10, true);
			for (int32 k = 0; k < N; ++k)
			{
				FBullet& B = FireVec(E.Pos, Dir(360.f * k / N) * 1.6f, EBulletShape::Tile, RIvory);
				B.Gravity = FVector2f(0.f, 0.02f);
			}
		}
		break;
	case 22: // mahjong spirit: two counter-rotating tile spirals and aimed red fans
		if (bStopped)
		{
			if (Ev(6, 0))
			{
				for (float Off : { 0.f, 120.f, 240.f })
				{
					Fire(E.Pos, 1.8f, A * 3.1f + Off, EBulletShape::Tile, RIvory);
					Fire(E.Pos, 1.5f, -A * 2.3f + Off, EBulletShape::Tile, RRed);
				}
			}
			if (Ev(70, 35)) Fan(E.Pos, 7, 48.f, 2.6f, AimAngle(E.Pos), EBulletShape::Rice, RRed);
		}
		break;
	case 23: // marks the player with a red line, then throws a column of tiles down it
		if (bStopped && Ev(90, 0))
		{
			const float Aim = AimAngle(E.Pos);
			AddBeam(E.Pos, Aim, 2.f, 40, 1, 1, FLinearColor(1.f, 0.3f, 0.3f));
			for (int32 i = 0; i < 6; ++i)
			{
				FBullet& B = FireVec(E.Pos, FVector2f::ZeroVector, EBulletShape::Tile, RIvory);
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
