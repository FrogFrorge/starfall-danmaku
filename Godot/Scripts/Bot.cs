// Autopilot (-DanmakuDemo -DanmakuBot): plays the game on its own so whole runs can be tested and recorded.
// Every frame it predicts where each bullet, beam and enemy will be over the next half second, tries a set of
// movement plans (8 directions or standing still, fast or focused, held or stopped early), and takes the safest plan
// that also keeps it under its target. If even the best plan is hit within a few frames, it bombs.
// It only reads the simulation, so with the fixed timestep and RNG seed a run plays out the same way every time.
// Port of DanmakuBot.cpp.
using Godot;
using static DanmakuSTG.BotLocals;

namespace DanmakuSTG;

static class BotLocals
{
	public const int Horizon = 30;
	public class FThreat
	{
		public FVector2f[] P = new FVector2f[Horizon + 1];
		public float R;
	}

	public static readonly int[] Period = { 1, 2, 4, 6, 4, 5 };
	public static readonly int[] Look = { 30, 22, 14, 10, 14, 12 };
	public static readonly float[] Sloppy = { 0f, 0.8f, 1.6f, 2.4f, 1.6f, 2.0f };
	public static readonly float[] Fumble = { 0f, 0f, 0f, 0f, 0.08f, 0.15f };
	public static readonly FVector2f[] Dirs = { new FVector2f(0f, 0f), new FVector2f(1f, 0f), new FVector2f(-1f, 0f), new FVector2f(0f, 1f), new FVector2f(0f, -1f),
		new FVector2f(0.7071f, 0.7071f), new FVector2f(-0.7071f, 0.7071f), new FVector2f(0.7071f, -0.7071f), new FVector2f(-0.7071f, -0.7071f) };
	public static readonly int[] HoldFor = { Horizon, 10, 4 };
}

public partial class DanmakuWorld
{
	public void BotLogLine(string Line)
	{
		string Full = $"[{Frame:000000}] st{StageIndex} {Line}";
		BotLog += Full + "\n";
		GD.Print($"BOT {Full}");
	}

	public string BotWhere()
	{
		if (Boss.bActive && Boss.Phase >= 0 && Boss.Phase < Phases().Count)
		{
			string Name = Phases()[Boss.Phase].Name ?? "";
			return $"{(bMidboss ? "midboss" : "boss")} phase {Boss.Phase} {(string.IsNullOrEmpty(Name) ? "(nonspell)" : Name)}";
		}
		return $"road frame {StageFrame}";
	}

	public void BotFinish()
	{
		bool bWin = State == EDanmakuState.Clear;
		BotLogLine($"RESULT {(bWin ? "CLEAR" : "GAMEOVER")}  deaths {BotDeaths}  bombs {BotBombs}  score {Score}  spells {SpellsCaptured}/{SpellsTotal}  at {BotWhere()}");
		Paths.SaveString(BotLog, Paths.ProjectSavedDir() + "BotResult_" + BotTag + ".txt");
	}

	public void BotStartSpell(int Index)
	{
		FSpellEntry E = SpellEntries()[Index];
		bSpellPractice = bPractice = true;
		bExtra = E.Stage >= ExtraStage;
		bIf = E.Stage >= IfStage;
		PracticeStage = E.Stage;
		PracticeDef = E.Def;
		PracticePhase = E.Phase;
		Difficulty = bExtra ? NumDifficulties : FMath.Clamp(DemoDifficulty, 0, NumDifficulties - 1);
		MeasureHits = MeasureFrames = 0;
		MeasureNear = MeasureClose = 0.0;
		StartGame((ECharacter)DemoCharacter);
	}

	public void BotSweepNext()
	{
		FSpellEntry E = SpellEntries()[SweepIndex];
		float Minutes = FMath.Max(1, MeasureFrames) / 3600f;
		SweepLog += FormattableString.Invariant($"{SweepIndex}\t{GetSpellEntryLabel(SweepIndex)}\t{E.Def.Phases[E.Phase].Name}\t{MeasureFrames / 60f:0.0}\t{MeasureHits}\t{MeasureHits / Minutes:0.0}\t{MeasureNear / FMath.Max(1, MeasureFrames):0.00}\t{MeasureClose / FMath.Max(1, MeasureFrames):0.00}\n");
		Paths.SaveString(SweepLog, Paths.ProjectSavedDir() + "BotSweep_" + BotTag + ".txt");
		if (++SweepIndex < SpellEntries().Count) BotStartSpell(SweepIndex);
		else { bBotDone = true; DemoQuitTimer = 2; }
	}

	public void UpdateBot()
	{
		CurInput.bShoot = true;
		FDanmakuPlayer P = Player;
		if (bRoadMeasure)
		{
			// measuring roads: count until the boss shows up, log the road, then jump to the next stage's road
			if (!Boss.bStarted) ++MeasureFrames;
			else if (!bBotDone)
			{
				float Minutes = FMath.Max(1, MeasureFrames) / 3600f;
				SweepLog += FormattableString.Invariant($"{StageIndex + 1}\t{GetDifficultyInfo(Difficulty).Name}\t{MeasureFrames / 60f:0.0}\t{MeasureHits}\t{MeasureHits / Minutes:0.0}\t{MeasureNear / FMath.Max(1, MeasureFrames):0.00}\t{MeasureClose / FMath.Max(1, MeasureFrames):0.00}\n");
				Paths.SaveString(SweepLog, Paths.ProjectSavedDir() + "BotRoads_" + BotTag + ".txt");
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
		int Skill = FMath.Clamp(BotSkill, 0, 5);
		bool bLinear = Skill >= 4;
		if (Skill > 0 && P.DeathTimer == 0 && --BotHold > 0)
		{
			CurInput.Move = BotLastMove;
			CurInput.bFocus = bBotLastFocus;
			return;
		}
		BotHold = Period[Skill];

		// bookkeeping for the log
		if (P.Lives < BotLastLives) { ++BotDeaths; BotLogLine($"DIED at {BotWhere()} (lives left {P.Lives})"); }
		BotLastLives = P.Lives;

		// a hit is pending: bomb out of it if we can
		if (P.DeathTimer > 0)
		{
			if (P.Bombs > 0 && BotMayBomb()) { bEdgeBomb = true; ++BotBombs; BotLogLine($"deathbomb at {BotWhere()}"); }
			return;
		}
		if (P.RespawnTimer > 0 || IsInDialogue()) { CurInput.Move = V2.ZeroVector; return; }

		float HitR = HitRadius();
		float SpeedMul = GetDifficultyInfo(Difficulty).SpeedMul;
		FCharacterInfo CI = GetCharacterInfo(P.Character);
		bool bTimeSlow = IsTimeSlowed();

		// ---- predict threats near the player ----
		List<FThreat> Threats = new List<FThreat>(512);
		const float Reach2 = (240f * 240f);
		foreach (FBullet B in Bullets)
		{
			if (B.bDead || B.bHarmless) continue;
			if (V2.DistSquared(B.Pos, P.Pos) > Reach2) continue;
			FThreat T = new FThreat(); Threats.Add(T);
			T.R = B.Radius;
			T.P[0] = B.Pos;
			// copy the motion state and step it forward the same way UpdateBullets does (minus behaviours and walls)
			float Speed = B.Speed, Angle = B.Angle, AngVel = B.AngVel;
			FVector2f Vel = B.Vel, Center = B.Center;
			float PR = B.PolarRadius, PA = B.PolarAngle, OA = B.OrbitAngle;
			FVector2f Pos = B.Pos;
			for (int t = 1; t <= Horizon; ++t)
			{
				bool bStep = !bTimeSlow || ((Frame + t) % 2 == 0);
				if (bStep)
				{
					int Age = B.Age + t;
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
						Speed = FMath.Clamp(Speed + B.Accel, B.MinSpeed, B.MaxSpeed);
						Angle += AngVel;
						AngVel *= B.AngVelDecay;
						Pos += Dir(Angle) * (Speed * SpeedMul);
					}
					if (B.WaveAmp != 0f) Pos.X += FMath.Sin(Age * B.WaveFreq + B.WavePhase) * B.WaveAmp;
				}
				T.P[t] = Pos;
			}
			if (bLinear)
			{
				FVector2f V = T.P[1] - T.P[0];
				for (int t = 2; t <= Horizon; ++t) T.P[t] = T.P[0] + V * (float)t;
			}
		}
		// enemy bodies (and the boss) hurt on contact too
		foreach (FEnemy E in Enemies)
		{
			if (E.bDead || V2.DistSquared(E.Pos, P.Pos) > Reach2) continue;
			FThreat T = new FThreat(); Threats.Add(T);
			T.R = E.Radius * 0.6f + 2f;
			for (int t = 0; t <= Horizon; ++t) T.P[t] = E.Pos + E.Vel * (float)t;
		}
		FVector2f[] Targets = new FVector2f[4];
		int NT = Boss.bActive ? BossTargets(Targets) : 0;
		for (int i = 0; i < NT; ++i)
		{
			FThreat T = new FThreat(); Threats.Add(T);
			T.R = 24f;
			for (int t = 0; t <= Horizon; ++t) T.P[t] = Targets[i] + Boss.Vel * (float)t;
		}
		if (bMeasure && (bRoadMeasure ? !Boss.bStarted : Boss.bSpell))
		{
			// how crowded it is around the player: bullets within 32 px and within 64 px
			foreach (FThreat Th in Threats)
			{
				float D = V2.Distance(Th.P[0], P.Pos);
				if (D < 64f) MeasureNear += Period[Skill];
				if (D < 32f) MeasureClose += Period[Skill];
			}
		}
		int Hz = Look[Skill];
		float Margin = 1.2f + Sloppy[Skill];

		// ---- where we'd like to be ----
		FVector2f Want = new FVector2f(FieldW * 0.5f, 392f);
		if (NT > 0)
		{
			int Best = 0;
			for (int i = 1; i < NT; ++i) if (FMath.Abs(Targets[i].X - P.Pos.X) < FMath.Abs(Targets[Best].X - P.Pos.X)) Best = i;
			Want.X = Targets[Best].X;
		}
		else
		{
			// on the road: line up under the lowest enemy that's on screen
			float BestY = -1000f;
			foreach (FEnemy E in Enemies)
			{
				if (E.bDead || E.Pos.Y < 0f || E.Pos.Y > P.Pos.Y - 40f || E.Pos.X < 0f || E.Pos.X > FieldW) continue;
				if (E.Pos.Y > BestY) { BestY = E.Pos.Y; Want.X = E.Pos.X; }
			}
		}
		Want.X = FMath.Clamp(Want.X, 40f, FieldW - 40f);
		// items: drift toward the nearest one that's falling past us, when it's quiet
		if (Threats.Count < 40)
		{
			float BestD = (110f * 110f);
			foreach (FItem I in Items)
			{
				if (I.bDead || I.bCollecting) continue;
				float D = V2.DistSquared(I.Pos, P.Pos);
				if (D < BestD && I.Pos.Y > 200f) { BestD = D; Want = new FVector2f(I.Pos.X, FMath.Max(I.Pos.Y, 330f)); }
			}
		}

		// ---- try the plans ----
		int SafeFor = FMath.Max(P.Invuln, (BombTime > 0 && BombKind != ECharacter.Haru) ? BombTime : 0);

		float BestCost = float.MaxValue;
		int BestFirstHit = Horizon + 1;
		FVector2f BestMove = V2.ZeroVector;
		bool bBestFocus = false;

		for (int f = 0; f < 2; ++f)
		{
			float Spd = (f != 0 ? CI.FocusSpeed : CI.Speed) * SpeedUnit;
			for (int d = 0; d < 9; ++d)
			{
				for (int h = 0; h < (d == 0 ? 1 : 3); ++h)
				{
					FVector2f Pos = P.Pos;
					float Cost = 0f;
					int FirstHit = Horizon + 1;
					for (int t = 1; t <= Hz; ++t)
					{
						if (t <= HoldFor[h])
						{
							Pos += Dirs[d] * Spd;
							Pos.X = FMath.Clamp(Pos.X, 8f, FieldW - 8f);
							Pos.Y = FMath.Clamp(Pos.Y, 16f, FieldH - 16f);
						}
						if (t <= SafeFor) continue;
						float W = 1f / (1f + 0.12f * t);
						foreach (FThreat Th in Threats)
						{
							float Dd = V2.Distance(Pos, Th.P[t]) - Th.R - HitR;
							if (Dd < Margin)
							{
								FirstHit = FMath.Min(FirstHit, t);
								Cost += 1.0e5f * W;
							}
							else if (Dd < 18f)
							{
								Cost += 30f * W * FMath.Exp(-Dd / 4.5f);
							}
						}
						// beams: the warning line counts as soon as it's about to fire
						foreach (FBeamHazard Bm in Beams)
						{
							if (Bm.bDead) continue;
							int A = Bm.Age + t;
							if (A < Bm.Warn - 3 || A >= Bm.Warn + Bm.Rise + Bm.Hold) continue;
							FVector2f E = Bm.Origin + Dir(Bm.Angle) * Bm.MaxLength;
							FVector2f AB = E - Bm.Origin;
							float U = FMath.Clamp(V2.DotProduct(Pos - Bm.Origin, AB) / FMath.Max(1f, AB.SizeSquared()), 0f, 1f);
							float Dd = V2.Distance(Pos, Bm.Origin + AB * U) - Bm.Width * 0.5f - HitR;
							if (Dd < 3f) { FirstHit = FMath.Min(FirstHit, t); Cost += 1.0e5f * W; }
							else if (Dd < 16f) Cost += 20f * W * FMath.Exp(-Dd / 5f);
						}
					}
					// position preference, judged where the plan ends up
					Cost += 0.06f * FMath.Abs(Pos.X - Want.X) + 0.12f * FMath.Abs(Pos.Y - Want.Y);
					// corners and edges leave nowhere to go
					Cost += 0.4f * (FMath.Max(0f, 30f - Pos.X) + FMath.Max(0f, Pos.X - (FieldW - 30f)) + FMath.Max(0f, Pos.Y - (FieldH - 22f)));
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

		if (Fumble[Skill] > 0f && BotRng.FRand() < Fumble[Skill]) BestMove = Dirs[BotRng.RandRange(0, 8)];
		CurInput.Move = BestMove;
		CurInput.bFocus = bBestFocus;
		BotLastMove = BestMove;
		bBotLastFocus = bBestFocus;
		// nothing works: bomb now rather than wait for the hit
		if (BotMayBomb() && BestFirstHit <= 3 && P.Bombs > 0 && BombTime == 0 && P.Invuln == 0)
		{
			bEdgeBomb = true;
			++BotBombs;
			BotLogLine($"panic bomb at {BotWhere()}");
		}
	}
}
