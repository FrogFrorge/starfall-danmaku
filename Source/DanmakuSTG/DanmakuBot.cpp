// Autopilot (-DanmakuDemo -DanmakuBot): plays the game on its own so whole runs can be tested and recorded.
// Every frame it predicts where each bullet, beam and enemy will be over the next half second, tries a set of
// movement plans (8 directions or standing still, fast or focused, held or stopped early), and takes the safest plan
// that also keeps it under its target. If even the best plan is hit within a few frames, it bombs.
// It only reads the simulation, so with the fixed timestep and RNG seed a run plays out the same way every time.

#include "DanmakuWorld.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"

using namespace Danmaku;

namespace
{
	constexpr int32 Horizon = 30;
	struct FThreat
	{
		FVector2f P[Horizon + 1];
		float R;
	};
}

void ADanmakuWorld::BotLogLine(const FString& Line)
{
	const FString Full = FString::Printf(TEXT("[%06d] st%d %s"), Frame, StageIndex, *Line);
	BotLog += Full + TEXT("\n");
	UE_LOG(LogTemp, Display, TEXT("BOT %s"), *Full);
}

FString ADanmakuWorld::BotWhere() const
{
	if (Boss.bActive && Boss.Phase >= 0 && Phases().IsValidIndex(Boss.Phase))
	{
		const FString Name = Phases()[Boss.Phase].Name;
		return FString::Printf(TEXT("%s phase %d %s"), bMidboss ? TEXT("midboss") : TEXT("boss"), Boss.Phase, Name.IsEmpty() ? TEXT("(nonspell)") : *Name);
	}
	return FString::Printf(TEXT("road frame %d"), StageFrame);
}

void ADanmakuWorld::BotFinish()
{
	const bool bWin = State == EDanmakuState::Clear;
	BotLogLine(FString::Printf(TEXT("RESULT %s  deaths %d  bombs %d  score %lld  spells %d/%d  at %s"),
		bWin ? TEXT("CLEAR") : TEXT("GAMEOVER"), BotDeaths, BotBombs, Score, SpellsCaptured, SpellsTotal, *BotWhere()));
	FFileHelper::SaveStringToFile(BotLog, *(FPaths::ProjectSavedDir() / FString::Printf(TEXT("BotResult_%s.txt"), *BotTag)));
}

void ADanmakuWorld::BotStartSpell(int32 Index)
{
	const FSpellEntry& E = SpellEntries()[Index];
	bSpellPractice = bPractice = true;
	bExtra = E.Stage >= ExtraStage;
	bIf = E.Stage >= IfStage;
	PracticeStage = E.Stage;
	PracticeDef = E.Def;
	PracticePhase = E.Phase;
	Difficulty = bExtra ? NumDifficulties : FMath::Clamp(DemoDifficulty, 0, NumDifficulties - 1);
	MeasureHits = MeasureFrames = 0;
	MeasureNear = MeasureClose = 0.0;
	StartGame(ECharacter(DemoCharacter));
}

void ADanmakuWorld::BotSweepNext()
{
	const FSpellEntry& E = SpellEntries()[SweepIndex];
	const float Minutes = FMath::Max(1, MeasureFrames) / 3600.f;
	SweepLog += FString::Printf(TEXT("%d\t%s\t%s\t%.1f\t%d\t%.1f\t%.2f\t%.2f\n"), SweepIndex, *GetSpellEntryLabel(SweepIndex),
		E.Def->Phases[E.Phase].Name, MeasureFrames / 60.f, MeasureHits, MeasureHits / Minutes,
		MeasureNear / FMath::Max(1, MeasureFrames), MeasureClose / FMath::Max(1, MeasureFrames));
	FFileHelper::SaveStringToFile(SweepLog, *(FPaths::ProjectSavedDir() / FString::Printf(TEXT("BotSweep_%s.txt"), *BotTag)), FFileHelper::EEncodingOptions::ForceUTF8);
	if (++SweepIndex < SpellEntries().Num()) BotStartSpell(SweepIndex);
	else { bBotDone = true; DemoQuitTimer = 2; }
}

void ADanmakuWorld::UpdateBot()
{
	CurInput.bShoot = true;
	FDanmakuPlayer& P = Player;
	if (bRoadMeasure)
	{
		// measuring roads: count until the boss shows up, log the road, then jump to the next stage's road
		if (!Boss.bStarted) ++MeasureFrames;
		else if (!bBotDone)
		{
			const float Minutes = FMath::Max(1, MeasureFrames) / 3600.f;
			SweepLog += FString::Printf(TEXT("%d\t%s\t%.1f\t%d\t%.1f\t%.2f\t%.2f\n"), StageIndex + 1, GetDifficultyInfo(Difficulty).Name,
				MeasureFrames / 60.f, MeasureHits, MeasureHits / Minutes, MeasureNear / FMath::Max(1, MeasureFrames), MeasureClose / FMath::Max(1, MeasureFrames));
			FFileHelper::SaveStringToFile(SweepLog, *(FPaths::ProjectSavedDir() / FString::Printf(TEXT("BotRoads_%s.txt"), *BotTag)), FFileHelper::EEncodingOptions::ForceUTF8);
			MeasureHits = MeasureFrames = 0;
			MeasureNear = MeasureClose = 0.0;
			if (StageIndex + 1 < 6) StartStage(StageIndex + 1);
			else { bBotDone = true; DemoQuitTimer = 2; }
			return;
		}
	}
	else if (bMeasure && Boss.bActive && Boss.bSpell && !IsInDialogue()) ++MeasureFrames;
	// a handicapped bot only rethinks every few frames, like a human's reaction time
	// skills 4 and 5 are more human: they read every bullet as flying straight (curves, acceleration and gravity
	// surprise them) and sometimes fumble the input
	static const int32 Period[6] = { 1, 2, 4, 6, 4, 5 };
	static const int32 Look[6] = { 30, 22, 14, 10, 14, 12 };
	static const float Sloppy[6] = { 0.f, 0.8f, 1.6f, 2.4f, 1.6f, 2.0f };
	static const float Fumble[6] = { 0.f, 0.f, 0.f, 0.f, 0.08f, 0.15f };
	const int32 Skill = FMath::Clamp(BotSkill, 0, 5);
	const bool bLinear = Skill >= 4;
	if (Skill > 0 && P.DeathTimer == 0 && --BotHold > 0)
	{
		CurInput.Move = BotLastMove;
		CurInput.bFocus = bBotLastFocus;
		return;
	}
	BotHold = Period[Skill];

	// bookkeeping for the log
	if (P.Lives < BotLastLives) { ++BotDeaths; BotLogLine(FString::Printf(TEXT("DIED at %s (lives left %d)"), *BotWhere(), P.Lives)); }
	BotLastLives = P.Lives;

	// a hit is pending: bomb out of it if we can
	if (P.DeathTimer > 0)
	{
		if (P.Bombs > 0 && !bMeasure) { bEdgeBomb = true; ++BotBombs; BotLogLine(FString::Printf(TEXT("deathbomb at %s"), *BotWhere())); }
		return;
	}
	if (P.RespawnTimer > 0 || IsInDialogue()) { CurInput.Move = FVector2f::ZeroVector; return; }

	const float HitR = HitRadius();
	const float SpeedMul = GetDifficultyInfo(Difficulty).SpeedMul;
	const FCharacterInfo& CI = GetCharacterInfo(P.Character);
	const bool bTimeSlow = IsTimeSlowed();

	// ---- predict threats near the player ----
	TArray<FThreat> Threats;
	Threats.Reserve(512);
	const float Reach2 = (240.f * 240.f);
	for (const FBullet& B : Bullets)
	{
		if (B.bDead || B.bHarmless) continue;
		if (FVector2f::DistSquared(B.Pos, P.Pos) > Reach2) continue;
		FThreat& T = Threats.AddDefaulted_GetRef();
		T.R = B.Radius;
		T.P[0] = B.Pos;
		// copy the motion state and step it forward the same way UpdateBullets does (minus behaviours and walls)
		float Speed = B.Speed, Angle = B.Angle, AngVel = B.AngVel;
		FVector2f Vel = B.Vel, Center = B.Center;
		float PR = B.PolarRadius, PA = B.PolarAngle, OA = B.OrbitAngle;
		FVector2f Pos = B.Pos;
		for (int32 t = 1; t <= Horizon; ++t)
		{
			const bool bStep = !bTimeSlow || ((Frame + t) % 2 == 0);
			if (bStep)
			{
				const int32 Age = B.Age + t;
				if (B.bOrbit && Age < B.EventFrame)
				{
					OA += B.OrbitSpeed;
					Pos = Boss.Pos + Dir(OA) * B.OrbitRadius;
				}
				else if (B.bOrbit)
				{
					Pos += (Player.Pos - Pos).GetSafeNormal() * B.EventParam * SpeedMul;
				}
				else if (B.Motion == 1)
				{
					Center += B.CenterVel;
					PR += B.PolarRadSpeed;
					PA += B.PolarAngSpeed;
					Pos = Center + Dir(PA) * PR;
				}
				else if (B.Motion == 2)
				{
					Vel += B.Gravity;
					Pos += Vel * SpeedMul;
				}
				else
				{
					Speed = FMath::Clamp(Speed + B.Accel, B.MinSpeed, B.MaxSpeed);
					Angle += AngVel;
					AngVel *= B.AngVelDecay;
					Pos += Dir(Angle) * (Speed * SpeedMul);
				}
				if (B.WaveAmp != 0.f) Pos.X += FMath::Sin(Age * B.WaveFreq + B.WavePhase) * B.WaveAmp;
			}
			T.P[t] = Pos;
		}
		if (bLinear)
		{
			const FVector2f V = T.P[1] - T.P[0];
			for (int32 t = 2; t <= Horizon; ++t) T.P[t] = T.P[0] + V * float(t);
		}
	}
	// enemy bodies (and the boss) hurt on contact too
	for (const FEnemy& E : Enemies)
	{
		if (E.bDead || FVector2f::DistSquared(E.Pos, P.Pos) > Reach2) continue;
		FThreat& T = Threats.AddDefaulted_GetRef();
		T.R = E.Radius * 0.6f + 2.f;
		for (int32 t = 0; t <= Horizon; ++t) T.P[t] = E.Pos + E.Vel * float(t);
	}
	FVector2f Targets[4];
	const int32 NT = Boss.bActive ? BossTargets(Targets) : 0;
	for (int32 i = 0; i < NT; ++i)
	{
		FThreat& T = Threats.AddDefaulted_GetRef();
		T.R = 24.f;
		for (int32 t = 0; t <= Horizon; ++t) T.P[t] = Targets[i] + Boss.Vel * float(t);
	}
	if (bMeasure && (bRoadMeasure ? !Boss.bStarted : Boss.bSpell))
	{
		// how crowded it is around the player: bullets within 32 px and within 64 px
		for (const FThreat& Th : Threats)
		{
			const float D = FVector2f::Distance(Th.P[0], P.Pos);
			if (D < 64.f) MeasureNear += Period[Skill];
			if (D < 32.f) MeasureClose += Period[Skill];
		}
	}
	const int32 Hz = Look[Skill];
	const float Margin = 1.2f + Sloppy[Skill];

	// ---- where we'd like to be ----
	FVector2f Want(FieldW * 0.5f, 392.f);
	if (NT > 0)
	{
		int32 Best = 0;
		for (int32 i = 1; i < NT; ++i) if (FMath::Abs(Targets[i].X - P.Pos.X) < FMath::Abs(Targets[Best].X - P.Pos.X)) Best = i;
		Want.X = Targets[Best].X;
	}
	else
	{
		// on the road: line up under the lowest enemy that's on screen
		float BestY = -1000.f;
		for (const FEnemy& E : Enemies)
		{
			if (E.bDead || E.Pos.Y < 0.f || E.Pos.Y > P.Pos.Y - 40.f || E.Pos.X < 0.f || E.Pos.X > FieldW) continue;
			if (E.Pos.Y > BestY) { BestY = E.Pos.Y; Want.X = E.Pos.X; }
		}
	}
	Want.X = FMath::Clamp(Want.X, 40.f, FieldW - 40.f);
	// items: drift toward the nearest one that's falling past us, when it's quiet
	if (Threats.Num() < 40)
	{
		float BestD = (110.f * 110.f);
		for (const FItem& I : Items)
		{
			if (I.bDead || I.bCollecting) continue;
			const float D = FVector2f::DistSquared(I.Pos, P.Pos);
			if (D < BestD && I.Pos.Y > 200.f) { BestD = D; Want = FVector2f(I.Pos.X, FMath::Max(I.Pos.Y, 330.f)); }
		}
	}

	// ---- try the plans ----
	static const FVector2f Dirs[9] = { { 0.f, 0.f }, { 1.f, 0.f }, { -1.f, 0.f }, { 0.f, 1.f }, { 0.f, -1.f },
		{ 0.7071f, 0.7071f }, { -0.7071f, 0.7071f }, { 0.7071f, -0.7071f }, { -0.7071f, -0.7071f } };
	static const int32 HoldFor[3] = { Horizon, 10, 4 };
	const int32 SafeFor = FMath::Max(P.Invuln, (BombTime > 0 && BombKind != ECharacter::Haru) ? BombTime : 0);

	float BestCost = TNumericLimits<float>::Max();
	int32 BestFirstHit = Horizon + 1;
	FVector2f BestMove = FVector2f::ZeroVector;
	bool bBestFocus = false;

	for (int32 f = 0; f < 2; ++f)
	{
		const float Spd = f ? CI.FocusSpeed : CI.Speed;
		for (int32 d = 0; d < 9; ++d)
		{
			for (int32 h = 0; h < (d == 0 ? 1 : 3); ++h)
			{
				FVector2f Pos = P.Pos;
				float Cost = 0.f;
				int32 FirstHit = Horizon + 1;
				for (int32 t = 1; t <= Hz; ++t)
				{
					if (t <= HoldFor[h])
					{
						Pos += Dirs[d] * Spd;
						Pos.X = FMath::Clamp(Pos.X, 8.f, FieldW - 8.f);
						Pos.Y = FMath::Clamp(Pos.Y, 16.f, FieldH - 16.f);
					}
					if (t <= SafeFor) continue;
					const float W = 1.f / (1.f + 0.12f * t);
					for (const FThreat& Th : Threats)
					{
						const float Dd = FVector2f::Distance(Pos, Th.P[t]) - Th.R - HitR;
						if (Dd < Margin)
						{
							FirstHit = FMath::Min(FirstHit, t);
							Cost += 1.0e5f * W;
						}
						else if (Dd < 18.f)
						{
							Cost += 30.f * W * FMath::Exp(-Dd / 4.5f);
						}
					}
					// beams: the warning line counts as soon as it's about to fire
					for (const FBeamHazard& Bm : Beams)
					{
						if (Bm.bDead) continue;
						const int32 A = Bm.Age + t;
						if (A < Bm.Warn - 3 || A >= Bm.Warn + Bm.Rise + Bm.Hold) continue;
						const FVector2f E = Bm.Origin + Dir(Bm.Angle) * Bm.MaxLength;
						const FVector2f AB = E - Bm.Origin;
						const float U = FMath::Clamp(FVector2f::DotProduct(Pos - Bm.Origin, AB) / FMath::Max(1.f, AB.SizeSquared()), 0.f, 1.f);
						const float Dd = FVector2f::Distance(Pos, Bm.Origin + AB * U) - Bm.Width * 0.5f - HitR;
						if (Dd < 3.f) { FirstHit = FMath::Min(FirstHit, t); Cost += 1.0e5f * W; }
						else if (Dd < 16.f) Cost += 20.f * W * FMath::Exp(-Dd / 5.f);
					}
				}
				// position preference, judged where the plan ends up
				Cost += 0.06f * FMath::Abs(Pos.X - Want.X) + 0.12f * FMath::Abs(Pos.Y - Want.Y);
				// corners and edges leave nowhere to go
				Cost += 0.4f * (FMath::Max(0.f, 30.f - Pos.X) + FMath::Max(0.f, Pos.X - (FieldW - 30.f)) + FMath::Max(0.f, Pos.Y - (FieldH - 22.f)));
				if (Cost < BestCost)
				{
					BestCost = Cost;
					BestFirstHit = FirstHit;
					BestMove = Dirs[d];
					bBestFocus = f == 1;
				}
			}
		}
	}

	if (Fumble[Skill] > 0.f && BotRng.FRand() < Fumble[Skill]) BestMove = Dirs[BotRng.RandRange(0, 8)];
	CurInput.Move = BestMove;
	CurInput.bFocus = bBestFocus;
	BotLastMove = BestMove;
	bBotLastFocus = bBestFocus;
	// nothing works: bomb now rather than wait for the hit
	if (!bMeasure && BestFirstHit <= 3 && P.Bombs > 0 && BombTime == 0 && P.Invuln == 0)
	{
		bEdgeBomb = true;
		++BotBombs;
		BotLogLine(FString::Printf(TEXT("panic bomb at %s"), *BotWhere()));
	}
}
