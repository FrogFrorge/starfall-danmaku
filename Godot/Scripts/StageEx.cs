// Port of DanmakuStageEx.cpp.
// Extra boss: 一索, the playful mahjong master. Ten spell cards, like a Touhou Extra stage, including the
// survival card 「九蓮寶燈」 and the bomb-proof wrap-around winds of 「大四喜」.
using static DanmakuSTG.StageExLocals;

namespace DanmakuSTG;

static class StageExLocals
{
	public static readonly FLinearColor ColIvory = new FLinearColor(1f, 0.96f, 0.85f);
	public static readonly FLinearColor ColHaku = new FLinearColor(0.94f, 0.96f, 1f);    // white dragon
	public static readonly FLinearColor ColHatsu = new FLinearColor(0.35f, 0.92f, 0.47f); // green dragon
	public static readonly FLinearColor ColChun = new FLinearColor(1f, 0.35f, 0.35f);    // red dragon
	public static readonly FLinearColor[] ColWinds = { new FLinearColor(0.5f, 0.78f, 1f), new FLinearColor(1f, 0.72f, 0.47f), new FLinearColor(0.67f, 1f, 0.7f), new FLinearColor(1f, 0.57f, 0.84f) };
	public static readonly FLinearColor[] ColLotus = {
		new FLinearColor(1f, 0.84f, 0.84f), new FLinearColor(1f, 0.88f, 0.75f), new FLinearColor(1f, 0.94f, 0.7f),
		new FLinearColor(0.88f, 1f, 0.75f), new FLinearColor(0.75f, 1f, 0.78f), new FLinearColor(0.75f, 0.96f, 1f),
		new FLinearColor(0.82f, 0.9f, 1f),  new FLinearColor(0.9f, 0.82f, 1f),  new FLinearColor(1f, 0.78f, 0.96f) };
	public static readonly FLinearColor[] ColKokushi = {
		new FLinearColor(1f, 0.4f, 0.4f), new FLinearColor(1f, 0.6f, 0.3f), new FLinearColor(1f, 0.85f, 0.3f), new FLinearColor(0.7f, 1f, 0.3f),
		new FLinearColor(0.3f, 1f, 0.5f), new FLinearColor(0.3f, 1f, 0.85f), new FLinearColor(0.3f, 0.8f, 1f), new FLinearColor(0.4f, 0.5f, 1f),
		new FLinearColor(0.65f, 0.4f, 1f), new FLinearColor(0.9f, 0.4f, 1f), new FLinearColor(1f, 0.4f, 0.8f), new FLinearColor(0.95f, 0.95f, 1f),
		new FLinearColor(1f, 0.82f, 0.5f) };

	// =====================================================================================
	// Extra midboss: 玄 returns to warn the player (three spell cards)
	// =====================================================================================
	public static readonly FLinearColor[] MBElem = { new FLinearColor(0.4f, 1f, 0.45f), new FLinearColor(1f, 0.45f, 0.25f), new FLinearColor(0.95f, 0.8f, 0.35f),
		new FLinearColor(0.92f, 0.95f, 1f), new FLinearColor(0.35f, 0.6f, 1f) };
}

public partial class DanmakuWorld
{
	public void EX_NonSpell1(int T)
	{
		if (Every(T, 5, 0))
		{
			for (int i = 0; i < 4; ++i) Fire(Boss.Pos, 2.2f, T * 4.1f + 90f * i, EBulletShape.Tile, ColIvory);
		}
		if (Every(T, 7, 3))
		{
			for (int i = 0; i < 4; ++i) Fire(Boss.Pos, 1.6f, -T * 2.9f + 90f * i + 45f, EBulletShape.Small, ColHatsu);
		}
		if (Every(T, 40, 20)) Fan(Boss.Pos, 9, 56f, 3f, AimAngle(Boss.Pos), EBulletShape.Rice, ColChun);
		if (Every(T, 180, 90)) WanderBoss();
	}

	// 立直「立直一發」: red lines mark the shots, then fast columns of tiles fly down them; the discards rain from above
	public void EX_Riichi(int T)
	{
		PatrolBoss(T);
		if (Every(T, 44, 0))
		{
			float Aim = AimAngle(Boss.Pos);
			int Lines = Difficulty >= 2 ? 5 : 3;
			for (int l = 0; l < Lines; ++l)
			{
				float Off = (l - (Lines - 1) * 0.5f) * 21f;
				AddBeam(Boss.Pos, Aim + Off, 2f, 44, 1, 1, new FLinearColor(1f, 0.3f, 0.3f)).bHarmless = true;   // just the aim line
				for (int i = 0; i < 12; ++i)
				{
					FBullet B = FireVec(Boss.Pos, FVector2f.Zero, EBulletShape.Tile, ColIvory);
					B.Behavior = BehDelayedRelease;
					B.Param = 1;
					B.Timer = 44 + i * 3;
					B.Center = Dir(Aim + Off) * 4.6f;
				}
			}
			// 一發: a slow ring of red tiles goes out with every declaration
			int N = Cnt(18);
			for (int i = 0; i < N; ++i) Fire(Boss.Pos, 1.1f, Aim + 180f / N + 360f * i / N, EBulletShape.Tile, ColChun);
		}
		if (Every(T, 10, 2))
		{
			// discards tumbling down from the top, half as many as they used to be
			FBullet D = FireVec(new FVector2f(Rng.FRandRange(8f, FieldW - 8f), -8f), new FVector2f(Rng.FRandRange(-0.4f, 0.4f), 1.2f), EBulletShape.Tile, ColHaku);
			D.Gravity = new FVector2f(0f, 0.012f);
			D.Spin = Rng.FRandRange(-6f, 6f);
		}
	}

	// 平和「平和斷么」: calm, perfectly symmetric alternating rings
	public void EX_Pinfu(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 105f));
		if (Every(T, 22, 0))
		{
			int W = T / FMath.Max(1, FMath.RoundToInt(22 * GetDifficultyInfo(Difficulty).IntervalScale));
			Ring(Boss.Pos, 28, 1.5f, (W % 2) * (180f / 28f), EBulletShape.Tile, W % 2 != 0 ? ColHatsu : ColHaku);
		}
		if (Every(T, 33, 11)) Ring(Boss.Pos, 12, 2.4f, AimAngle(Boss.Pos), EBulletShape.Medium, ColChun);   // the red rings come twice as often
	}

	public void EX_NonSpell2(int T)
	{
		if (Every(T, 34, 0)) Ring(Boss.Pos, 36, 1.8f, Rng.FRandRange(0f, 10f), EBulletShape.Tile, ColHaku);
		if (Every(T, 11, 0)) Fan(Boss.Pos, 3, 14f, 3.2f, AimAngle(Boss.Pos), EBulletShape.Rice, ColHatsu);
		if (Every(T, 68, 34)) Ring(Boss.Pos, 18, 1.2f, 0f, EBulletShape.Large, ColChun);
		if (Every(T, 160, 80)) WanderBoss();
	}

	// 一色「清一色」: three-armed spirals of one colour; each new spiral starts before the last one ends, turning the other way
	public void EX_Chinitsu(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 110f));
		const int Start = 170, Len = 300;
		for (int W = FMath.Max(0, (T - Len) / Start); W * Start <= T; ++W)
		{
			int L = T - W * Start;
			if (L < 0 || L >= Len) continue;
			float Dirn = W % 2 == 0 ? 1f : -1f;
			FLinearColor C = W % 2 == 0 ? ColHatsu : new FLinearColor(0.6f, 1f, 0.65f);
			if (Every(L, 3, 0))
			{
				for (int i = 0; i < 3; ++i) Fire(Boss.Pos, 1.9f, Dirn * L * 2.4f + 120f * i + W * 37f, EBulletShape.Tile, C);
			}
		}
		if (Every(T, 60, 30)) Ring(Boss.Pos, 18, 1.2f, 0f, EBulletShape.Large, new FLinearColor(0.3f, 0.75f, 0.4f));
	}

	// 對子「七對子」: seven pairs of tiles that fly together, then split apart
	public void EX_Chiitoi(int T)
	{
		PatrolBoss(T);
		if (Every(T, 75, 0))
		{
			// seven pairs come down from the top, like 地和 upside down. Each pair is one white tile and one red one:
			// they start together and open up as they fall, the white down-left and the red down-right, both at the
			// same speed. A tile that leaves one side comes back from the other, so all fourteen stay on the table.
			const int Pairs = 7;
			float Base = Rng.FRandRange(0f, FieldW / Pairs);
			for (int p = 0; p < Pairs; ++p)
			{
				float X = Base + p * (FieldW / Pairs);
				for (int k = 0; k < 2; ++k)
				{
					bool bRed = k != 0;
					FBullet B = Fire(new FVector2f(X + k * 3f, -6f), 2f, bRed ? 38f : 142f,
						EBulletShape.Tile, bRed ? ColChun : ColIvory);
					B.bWrapX = true;
				}
			}
		}
		// the round bullets between the pairs
		if (Every(T, 10, 5)) Fire(Boss.Pos, 1.6f, Rng.FRandRange(0f, 360f), EBulletShape.Medium, ColHaku);
	}

	public void EX_NonSpell3(int T)
	{
		if (Every(T, 4, 0))
		{
			Fire(Boss.Pos, 2f, T * 5.3f, EBulletShape.Tile, ColChun);
			Fire(Boss.Pos, 2f, -T * 5.3f, EBulletShape.Tile, ColHatsu);
			Fire(Boss.Pos, 1.6f, T * 5.3f + 180f, EBulletShape.Tile, ColHaku);
		}
		if (Every(T, 50, 25)) Ring(Boss.Pos, 24, 2.5f, AimAngle(Boss.Pos), EBulletShape.Small, ColHaku);
		if (Every(T, 150, 75)) WanderBoss();
	}

	// 開花「嶺上開花」: tiles are tossed into the air and blossom into rings
	public void EX_Rinshan(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 120f));
		if (Every(T, 22, 0))
		{
			int N = Cnt(5);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = FireVec(Boss.Pos, Dir(Rng.FRandRange(-150f, -30f)) * Rng.FRandRange(2.6f, 3.4f), EBulletShape.Tile, ColIvory);
				B.Gravity = new FVector2f(0f, 0.055f);
				B.Behavior = BehSplitRing;
				B.Timer = Rng.RandRange(60, 80);
				B.Param = 10;
				B.Param2 = 1.35f;
				B.Timer2 = 1;   // bursts into flowers of every colour
				B.Color = ColLotus[Rng.RandRange(0, 8)];
			}
		}
	}

	// 役滿一「大三元」: white, green and red dragons — three-armed, six-layered turning sprays plus slow rings
	public void EX_Daisangen(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 110f));
		FLinearColor[] Cols = { ColHaku, ColHatsu, ColChun };
		if (Every(T, 15, 0))
		{
			int Volley = T / FMath.Max(1, FMath.RoundToInt(15 * GetDifficultyInfo(Difficulty).IntervalScale));
			float Sgn = Volley % 2 == 0 ? 1f : -1f;
			float Base = T * 0.8f * Sgn;
			int NumLayers = FMath.Max(2, Cnt(6));
			for (int L = 0; L < NumLayers; ++L)
			{
				for (int Arm = 0; Arm < 3; ++Arm)
				{
					float A0 = Base + L * 30f + Arm * 120f;
					FVector2f O = Boss.Pos + Dir(A0) * (10f + L * 5f);
					for (int j = -1; j <= 1; ++j)
					{
						FBullet B = Fire(O, 1.55f + FMath.Abs(j) * 0.05f + L * 0.09f, A0 + j * 11.5f + Sgn * (4.6f + L * 0.86f), EBulletShape.Medium, Cols[Arm]);
						B.AngVel = Sgn * (0.069f + L * 0.0103f);
						B.AngVelDecay = 0.9988f;
						B.Accel = 0.002f;
						B.MaxSpeed = 3f;
					}
				}
			}
		}
		if (Every(T, 45, 0))
		{
			int N = Cnt(60);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Boss.Pos, 1.25f, T * 0.46f + 360f * i / N, EBulletShape.Small, Cols[i % 3]);
				B.AngVel = -0.04f;
				B.AngVelDecay = 0.999f;
				B.Accel = 0.0012f;
				B.MaxSpeed = 1.7f;
			}
		}
	}

	static readonly float[] EX_DaisuushiSpeeds = { 0.8f, 1.15f, 1.5f, 1.85f };
	static readonly float[] EX_DaisuushiOffsets = { 0f, 7.4f, 16.6f, 24.6f };

	// 役滿二「大四喜」: one volley of four rings of wind (four colours, four speeds) that wrap around the edges of the screen.
	// Bombs cannot erase them.
	public void EX_Daisuushi(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 120f));
		int K = T % 1500;
		// one volley: four rings at once, each wind its own colour and its own speed
		if (K == 1)
		{
			int Burst = 0;
			float[] Speeds = EX_DaisuushiSpeeds;
			float[] Offsets = EX_DaisuushiOffsets;
			for (int Rg = 0; Rg < 4; ++Rg)
			{
				int N = Cnt(24);   // 1.5x the bullets per ring (was 16)
				for (int i = 0; i < N; ++i)
				{
					FBullet B = Fire(Boss.Pos, Speeds[Rg], Offsets[Rg] + Burst * 4f + 360f * i / N, EBulletShape.Medium, ColWinds[Rg]);
					B.bWrap = true;
					B.bBombProof = true;
					B.Life = 1440;
				}
			}
			Sfx(ESfx.Spell);
		}
	}

	// 役滿三「九蓮寶燈」 (survival 45 s): nine numbered lotuses appear; 1 and 9 burst into rings, 2-8 into homing petals
	public void EX_Chuuren(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 100f));
		if (T == 1) Boss.PatternTimer = 120;
		if (T < Boss.PatternTimer) return;
		Boss.PatternTimer = T + FMath.RoundToInt(FMath.Lerp(300f, 60f, FMath.Clamp(T / 1800f, 0f, 1f)) * GetDifficultyInfo(Difficulty).IntervalScale * (bComposite ? CompInterval * 1.5f : 1f));

		List<FVector2f> Used = new();
		for (int Num = 1; Num <= 9; ++Num)
		{
			FVector2f P = new FVector2f(FMath.Clamp(Boss.Pos.X + Rng.FRandRange(-96f, 96f), 27f, FieldW - 27f), FMath.Clamp(Boss.Pos.Y + Rng.FRandRange(32f, 140f), 70f, FieldH - 32f));
			for (int Try = 0; Try < 100; ++Try)
			{
				FVector2f C = Boss.Pos + Dir(Rng.FRandRange(0f, 360f)) * Rng.FRandRange(53f, 160f);
				if (C.X < 27f || C.X > FieldW - 27f || C.Y < 70f || C.Y > FieldH - 32f) continue;
				if (V2.Distance(C, Player.Pos) < 80f) continue;
				bool bOk = true;
				foreach (FVector2f U in Used) if (V2.Distance(C, U) < 33f) { bOk = false; break; }
				if (bOk) { P = C; break; }
			}
			Used.Add(P);
			FBullet L = Fire(P, 0f, -90f, EBulletShape.Sakura, ColLotus[Num - 1]);
			L.Behavior = BehLotusSummon;
			L.Param = Num;
			L.Timer = 60;
			L.bHarmless = true;
			L.Scale = 1.6f;
			L.Spin = 2f;
		}
	}

	// 役滿奧義「國士無雙」: thirteen assassins come out — one first, then two at a time (bombs and the death clear can't remove them);
	// each one flies to a wall, then stalks the player, and does it again every time it reaches a wall
	public void EX_Kokushi(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 100f));
		// the first assassin comes out alone, then the rest two at a time
		if (T == 1) Boss.MawPos = new FVector2f(Rng.FRandRange(0f, 360f), 0f);
		const int Gap = 300;   // then two more every 5 seconds until all 13 are out
		if (T >= 45 && (T - 45) % Gap == 0 && (T - 45) / Gap <= 6)
		{
			int Wave = (T - 45) / Gap;
			float Base = Boss.MawPos.X;
			int First = Wave == 0 ? 0 : 2 * Wave - 1;
			int Last = Wave == 0 ? 0 : 2 * Wave;
			for (int Index = First; Index <= Last; ++Index)
			{
				FBullet A = FireVec(Boss.Pos, Dir(Base + Index * 360f / 13f) * 2.6f, EBulletShape.Large, ColKokushi[Index]);
				A.Behavior = BehAssassin;
				A.Param = 0;
				A.bBombProof = true;
				A.Scale = 1.3f;
				A.Radius *= 1.1f;
			}
			AddParticle(Boss.Pos, FVector2f.Zero, Wave == 0 ? 160f : 220f, new FLinearColor(1f, 0.9f, 0.6f), 30, true);
			Sfx(ESfx.Spell);
		}
	}

	static readonly string[] EX_TenhouPhaseNames = { "天和", "地和", "人和" };

	// 終局「天和」: three hands in one card, like 燁's last spell — each one joins the ones already in play.
	//   天和 (from the start): rings of tiles burst from the top edge; half of each ring flies off into the sky. Each ring turns
	//        a different way.
	//   地和 (2/3 health): rows of tiles rise from the bottom, the tiles scattered at random along each row.
	//   人和 (1/3 health): a three-armed spiral like 清一色's, turning clockwise only.
	// Speeds: 人和 > 天和 > 地和.
	public void EX_Tenhou(int T)
	{
		PatrolBoss(T);
		float Frac = FMath.Clamp(Boss.HP / FMath.Max(1f, Boss.MaxHP), 0f, 1f);
		int Want = Frac > 2f / 3f ? 1 : (Frac > 1f / 3f ? 2 : 3);
		while (Boss.Stage6Beasts < Want)
		{
			int H = Boss.Stage6Beasts++;
			Boss.JoinFrame[H] = T;
			if (H > 0)
			{
				ShowMessage("「" + Tr(EX_TenhouPhaseNames[H]) + "」", "", 90);
				// the card's title keeps its 天和 and stacks each new hand after it: 終局「天和」「地和」「人和」
				Boss.SpellName += QuoteName(EX_TenhouPhaseNames[H]);
				AddParticle(Boss.Pos, FVector2f.Zero, 420f, new FLinearColor(1f, 0.85f, 0.45f), 40, true);
				Sfx(ESfx.Spell);
			}
		}
		int N = Boss.Stage6Beasts;
		// the more hands on the table, the thinner each one gets
		float Thin = 1f + 0.35f * (N - 1);
		bool Ev(int H, int Iv) { int Local = T - Boss.JoinFrame[H]; int I = FMath.Max(2, FMath.RoundToInt(Iv * Thin * GetDifficultyInfo(Difficulty).IntervalScale)); return Local >= 0 && Local % I == 0; }

		// 天和: from the top
		if (Ev(0, 34))
		{
			FVector2f O = new FVector2f(FMath.Clamp(Boss.Pos.X + Rng.FRandRange(-130f, 130f), 20f, FieldW - 20f), -4f);
			int RN = Cnt(26);
			float Base = Rng.FRandRange(0f, 360f);
			for (int i = 0; i < RN; ++i) Fire(O, 1.8f, Base + 360f * i / RN, EBulletShape.Tile, i % 2 != 0 ? ColHaku : ColIvory);
		}
		// 地和: from the bottom — one row of tiles rises up, every tile at a random spot along the row
		if (N >= 2 && Ev(1, 40))
		{
			int Len = Cnt(10);
			for (int k = 0; k < Len; ++k) Fire(new FVector2f(Rng.FRandRange(10f, FieldW - 10f), FieldH + 6f), 1.2f, -90f, EBulletShape.Tile, ColHatsu);
		}
		// 人和: clockwise only
		if (N >= 3 && Ev(2, 4))
		{
			int L = T - Boss.JoinFrame[2];
			for (int i = 0; i < 3; ++i) Fire(Boss.Pos, 2.3f, L * 2.4f + 120f * i, EBulletShape.Tile, ColChun);
		}
	}

	// =====================================================================================
	// Extra midboss: 玄 returns to warn the player (three spell cards)
	// =====================================================================================

	// 逆符「五行逆轉」: rings in the five colours that spin one way, then all reverse together; a second layer turns the other way
	public void MB_Reverse(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 105f));
		if (Every(T, 20, 0))
		{
			int W = T / 20;
			float Sgn = W % 2 != 0 ? -1f : 1f;
			int N = Cnt(20);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Boss.Pos, 1.8f, W * 9f + 360f * i / N, EBulletShape.Medium, MBElem[(W + i) % 5]);
				B.AngVel = 0.55f * Sgn;
				B.AngVelDecay = 1f;
				B.Behavior = BehCurveAfter;
				B.Timer = 60;
				B.Param2 = -0.55f * Sgn;
			}
		}
		if (Every(T, 48, 24)) Fan(Boss.Pos, 7, 36f, 3f, AimAngle(Boss.Pos), EBulletShape.Rice, MBElem[(T / 48) % 5]);
	}

	static readonly int[] MB_LoShuSquare = { 4, 9, 2, 3, 5, 7, 8, 1, 6 };

	// 數符「河圖洛書」: the 3x3 magic square — seeds bloom in the order of the numbers 1 to 9, and the centre five answers with an aimed ring
	public void MB_LoShu(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 80f));
		int[] Square = MB_LoShuSquare;
		int Cyc = FMath.Max(1, FMath.RoundToInt(120 * GetDifficultyInfo(Difficulty).IntervalScale));
		if (T % Cyc == 1)
		{
			FVector2f C = new FVector2f(FieldW * 0.5f + Rng.FRandRange(-40f, 40f), 200f + Rng.FRandRange(-20f, 20f));
			for (int i = 0; i < 9; ++i)
			{
				FVector2f P = C + new FVector2f(((i % 3) - 1) * 96f, ((i / 3) - 1) * 76f);
				SpawnPlumSeed(P, 16 + Square[i] * 9, MBElem[Square[i] % 5], 6 + Square[i], 1.4f);
			}
		}
		if (T % Cyc == 1 + 16 + 5 * 9)
		{
			Ring(Boss.Pos, 30, 2.2f, AimAngle(Boss.Pos), EBulletShape.Small, MBElem[0]);
		}
		if (Every(T, 7, 3))
		{
			FBullet D = Fire(Boss.Pos, 1.2f, 90f + FMath.Sin(T * 0.05f) * 70f, EBulletShape.Medium, MBElem[(T / 12) % 5]);
			D.Accel = 0.01f;
			D.MaxSpeed = 2.2f;
		}
	}

	// 陣符「八門金鎖」: eight beams lock the field around the boss and burst into shrapnel where they hit the walls;
	// one gate is always left open
	public void MB_EightGates(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 150f));
		int Cyc = FMath.Max(1, FMath.RoundToInt(100 * GetDifficultyInfo(Difficulty).IntervalScale));
		if (T % Cyc == 1)
		{
			int Open = Rng.RandRange(0, 7);
			float Base = Rng.FRandRange(0f, 45f);
			for (int g = 0; g < 8; ++g)
			{
				if (g == Open) continue;
				AddBeam(Boss.Pos, Base + g * 45f, 7f, 64, 10, 36, new FLinearColor(1f, 0.85f, 0.45f), 3);
			}
		}
	}
}
