// Port of DanmakuStageIf.cpp.
// Extra-IF: "what if the hero was too late". The Four Gods Barrier broke on the Night of the Heaven's Gate, and the four
// perils it held down — 饕餮 (gluttony), 檮杌 (the untamable), 混沌 (faceless chaos) and 窮奇 (the winged tiger who
// punishes the honest) — are loose. One of them blocks the road (which one depends on the player character), then all
// four fight in relay: two cards each, the last one flies off as the next swoops in, and the endurance card and last
// card have all four on the field at once.
using static DanmakuSTG.StageIfLocals;

namespace DanmakuSTG;

static class StageIfLocals
{
	public static readonly FLinearColor Bronze = new FLinearColor(0.85f, 0.7f, 0.35f);
	public static readonly FLinearColor Jade = new FLinearColor(0.45f, 0.9f, 0.55f);
	public static readonly FLinearColor Crimson = new FLinearColor(1f, 0.3f, 0.25f);
	public static readonly FLinearColor Iron = new FLinearColor(0.78f, 0.8f, 0.86f);
	public static readonly FLinearColor TigerGold = new FLinearColor(1f, 0.7f, 0.3f);
	public static readonly FLinearColor Pearl = new FLinearColor(0.94f, 0.95f, 1f);
	public static readonly FLinearColor Ink = new FLinearColor(0.55f, 0.4f, 0.85f);
	public static readonly FLinearColor Indigo = new FLinearColor(0.45f, 0.5f, 1f);
	public static readonly FLinearColor Silver = new FLinearColor(0.85f, 0.9f, 1f);

	public static FLinearColor Rainbow(float H) { return LinColor.MakeFromHSV8((byte)FMath.Fmod(H, 255f), 120, 255); }
}

public partial class DanmakuWorld
{
	// =====================================================================================
	// Shared pieces
	// =====================================================================================

	public int SwallowBullets(FVector2f At, float PullRadius, float EatRadius, float Pull, List<FVector2f> Eaten = null)
	{
		// 饕餮's mouth: bullets nearby bend toward it, and the ones that reach it are eaten (and counted)
		float PR2 = PullRadius * PullRadius, ER2 = EatRadius * EatRadius;
		int Count = 0;
		foreach (var B in Bullets)
		{
			if (B.bDead || B.bHarmless || B.bBombProof || B.bSpat) continue;
			// bullets the beast just spat out of this very mouth aren't swallowed again right away
			if (B.Age < 40) continue;
			FVector2f D = At - B.Pos;
			float D2 = D.SizeSquared();
			if (D2 > PR2) continue;
			if (D2 < ER2)
			{
				B.bDead = true;
				++Boss.MawEaten;
				++Count;
				if (Eaten != null) Eaten.Add(B.Pos);
				continue;
			}
			if (B.Motion == 2) B.Vel += D.GetSafeNormal() * Pull * 2f;
			else if (B.Motion == 0) B.Angle += FMath.Clamp(FMath.FindDeltaAngleDegrees(B.Angle, AngleOf(D)), -1f, 1f) * Pull * 40f;
		}
		return Count;
	}

	public void SpitBullet(FVector2f At, float Speed, float Angle, EBulletShape Shape, FLinearColor Color)
	{
		FBullet B = Fire(At, Speed, Angle, Shape, Color);
		B.bSpat = true;
	}

	public void PlaceQuad(int T, float Spread)
	{
		// the four beasts circle the top of the field in a slowly turning diamond
		FVector2f C = new FVector2f(FieldW * 0.5f, 140f);
		Boss.Pos = C;
		Boss.MoveTime = Boss.MoveDur;
		for (int i = 0; i < 4; ++i)
		{
			float A = T * 0.35f + i * 90f + 45f;
			FVector2f Target = C + new FVector2f(FMath.Cos(FMath.DegreesToRadians(A)) * Spread, FMath.Sin(FMath.DegreesToRadians(A)) * Spread * 0.45f);
			Boss.QuadPos[i] = FMath.Lerp(Boss.QuadPos[i], Target, 0.04f);
		}
	}

	// =====================================================================================
	// 饕餮 (gluttony) — midboss cards
	// =====================================================================================

	// 饕符「貪食之口」: a mouth drifts around the field swallowing the boss's own rings, then spits everything it ate back out
	public void IF_TaotieMaw(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 90f));
		Boss.MawPos = new FVector2f(FieldW * 0.5f + FMath.Sin(T * 0.017f) * 120f, 210f + FMath.Sin(T * 0.029f) * 50f);
		if (Every(T, 12, 0)) Ring(Boss.Pos, 24, 1.7f, T * 1.3f, EBulletShape.Medium, Bronze);
		// every bullet the mouth eats comes straight back out as three green ones flying anywhere; the green ones are never eaten
		List<FVector2f> Eaten = new();
		SwallowBullets(Boss.MawPos, 95f, 12f, 0.05f, Eaten);
		foreach (FVector2f E in Eaten)
		{
			for (int k = 0; k < 3; ++k) SpitBullet(Boss.MawPos, Rng.FRandRange(1.5f, 2f), Rng.FRandRange(0f, 360f), EBulletShape.Rice, Jade);
		}
		if (Eaten.Count > 0) AddParticle(Boss.MawPos, FVector2f.Zero, 50f, Jade, 10, true);
		Boss.MawEaten = 0;
		if (T % 6 == 0) AddParticle(Boss.MawPos, FVector2f.Zero, 70f, new FLinearColor(0.35f, 0.25f, 0.1f), 14, true);
	}

	// 饕符「鐘鼎饌宴」: bronze cauldrons tumble from the sky and burst into a feast; food sprays from the boss in an arc
	public void IF_TaotieFeast(int T)
	{
		PatrolBoss(T);
		if (Every(T, 36, 0))
		{
			for (int k = 0; k < 4; ++k)
			{
				FBullet D = FireVec(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -10f), new FVector2f(0f, 0.6f), EBulletShape.Large, Bronze);
				D.Gravity = new FVector2f(0f, 0.03f);
				D.Behavior = BehSplitRing;
				D.Timer = Rng.RandRange(55, 85);
				D.Param = 14;
				D.Param2 = 1.4f;
			}
		}
		if (Every(T, 5, 0))
		{
			FBullet F = FireVec(Boss.Pos, Dir(Rng.FRandRange(-150f, -30f)) * Rng.FRandRange(2f, 2.8f), EBulletShape.Rice, Jade);
			F.Gravity = new FVector2f(0f, 0.04f);
		}
	}

	// 饕符「饕餮紋」: the bronze mask motif — mirror-image curls, and the two eyes stare out as rings
	public void IF_TaotieMotif(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 105f));
		if (Every(T, 4, 0))
		{
			foreach (float S in new[] { -1f, 1f })
			{
				FBullet B = Fire(Boss.Pos + new FVector2f(S * 18f, 0f), 1.9f, 90f + S * (55f + FMath.Sin(T * 0.05f) * 45f), EBulletShape.Rice, Bronze);
				B.AngVel = -S * 0.9f;
				B.AngVelDecay = 0.984f;
			}
		}
		if (Every(T, 44, 22))
		{
			foreach (float S in new[] { -1f, 1f }) Ring(Boss.Pos + new FVector2f(S * 42f, -8f), 18, 2.2f, AimAngle(Boss.Pos), EBulletShape.Medium, Jade);
		}
	}

	// =====================================================================================
	// 檮杌 (the untamable) — midboss cards
	// =====================================================================================

	// 杌符「頑石不化」: boulders that never stop — they ricochet off every wall for a long time
	public void IF_TaowuStone(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 95f));
		if (Every(T, 40, 0))
		{
			for (int k = 0; k < 3; ++k)
			{
				FBullet B = Fire(Boss.Pos, 1.8f, Rng.FRandRange(30f, 150f), EBulletShape.Large, Crimson);
				B.WallMask = 1 | 2 | 4;
				B.Bounces = 8;
				B.Life = 620;
			}
		}
	}

	// 杌符「難訓之獸」: every shot first runs AWAY from the player, stops, then turns and charges
	public void IF_TaowuUntamed(int T)
	{
		PatrolBoss(T);
		if (Every(T, 44, 0))
		{
			// the main spread runs away from you; smaller spreads go toward you and 90° to either side
			float Away = AimAngle(Boss.Pos) + 180f;
			float[] Dirs = { 0f, 180f, 90f, -90f };
			for (int d = 0; d < 4; ++d)
			{
				int N = Cnt(d == 0 ? 22 : 7);
				for (int i = 0; i < N; ++i)
				{
					bool bRed = i % 2 != 0;
					// the red crescents are the untamed ones: they fly off anywhere and dash off anywhere
					float A = bRed ? Rng.FRandRange(0f, 360f) : Away + Dirs[d] + (i - N * 0.5f) * 12f;
					FBullet B = FireVec(Boss.Pos, Dir(A) * 2.6f, EBulletShape.Crescent, bRed ? Crimson : Iron);
					B.Behavior = BehPauseDash;
					B.Timer = 40;
					B.Param = bRed ? 9999 : (i % 3 - 1) * 40;
					B.Param2 = 2.7f;
				}
			}
		}
	}

	// 杌符「丈八之尾」: two long striped tails sweep round the boss in opposite directions (clockwise twice as fast)
	public void IF_TaowuTail(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 110f));
		if (Every(T, 3, 0) && (T / 3) % 5 < 3)
		{
			for (int k = 0; k < 2; ++k)
			{
				// one tail turns clockwise, the other counter-clockwise at half the speed
				float A = k == 0 ? T * 1.8f : 180f - T * 0.9f;
				FirePolar(Boss.Pos, A, 10f, 2.0f, 0.12f, EBulletShape.Medium, (T / 6) % 2 != 0 ? TigerGold : new FLinearColor(0.25f, 0.2f, 0.2f));
			}
		}
		if (Every(T, 48, 24)) Ring(Boss.Pos, 28, 1.5f, Rng.FRandRange(0f, 20f), EBulletShape.Small, Crimson);
	}

	// =====================================================================================
	// 混沌 (faceless chaos) — midboss cards
	// =====================================================================================

	static readonly FVector2f[] IF_HundunSevenHoles = { new(-50f, -40f), new(50f, -40f), new(-105f, 0f), new(105f, 0f), new(-16f, 14f), new(16f, 14f), new(0f, 62f) };

	// 沌符「七竅未開」: seven holes open one by one in a face that isn't there — eyes, ears, nostrils, mouth — then all at once
	public void IF_HundunSeven(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 80f));
		FVector2f[] Holes = IF_HundunSevenHoles;
		FVector2f Face = new FVector2f(FieldW * 0.5f, 190f);
		int Cyc = FMath.Max(1, FMath.RoundToInt(420 * GetDifficultyInfo(Difficulty).IntervalScale));
		int Ph = T % Cyc;
		int Step = Cyc / 9;
		// the faceless head keeps muttering: slow sparks from the boss all the time
		if (Every(T, 9, 0)) Fire(Boss.Pos, 1.4f, Rng.FRandRange(20f, 160f), EBulletShape.Small, Pearl);
		for (int k = 0; k < 7; ++k)
		{
			if (Ph == k * Step + 1)
			{
				// a pale warning where the hole will open
				FBullet W = Fire(Face + Holes[k], 0f, 90f, EBulletShape.Large, Pearl);
				W.bHarmless = true;
				W.Life = Step;
				W.Scale = 1.4f;
			}
			if (Ph == (k + 1) * Step || Ph == 8 * Step)
			{
				Ring(Face + Holes[k], 20, 1.6f, Rng.FRandRange(0f, 30f), EBulletShape.Small, Rainbow(k * 36f));
				Fan(Face + Holes[k], 3, 20f, 2.4f, AimAngle(Face + Holes[k]), EBulletShape.Rice, Ink);
			}
		}
	}

	// 沌符「無面之舞」: a swirling dance with no direction — every bullet wanders on its own
	public void IF_HundunDance(int T)
	{
		PatrolBoss(T);
		if (Every(T, 3, 0))
		{
			FBullet B = Fire(Boss.Pos, Rng.FRandRange(1.2f, 1.9f), Rng.FRandRange(0f, 360f), EBulletShape.Medium, Rainbow(T * 2f));
			B.AngVel = Rng.FRandRange(-1.4f, 1.4f);
			B.AngVelDecay = 0.99f;
			B.WaveAmp = 0.6f;
			B.WaveFreq = 0.07f;
			B.WavePhase = Rng.FRandRange(0f, 6f);
		}
		if (Every(T, 24, 12)) Ring(Boss.Pos, 26, 1.8f, AimAngle(Boss.Pos), EBulletShape.Small, Pearl);
	}

	static readonly int[] IF_HundunSongMelody = { 4, 5, 7, 5, 4, 2, 4, 1, 2, 4, 5, 7, 8, 7, 5, 4 };

	// 沌符「帝江歌舞」: 帝江 sings — notes fall along a melody, and every bar ends with a chord burst
	public void IF_HundunSong(int T)
	{
		PatrolBoss(T);
		int[] Melody = IF_HundunSongMelody;
		int Beat = FMath.Max(1, FMath.RoundToInt(11 * GetDifficultyInfo(Difficulty).IntervalScale));
		if (T % Beat == 0)
		{
			int N = (T / Beat) % 16;
			float X = 24f + Melody[N] * 37f;
			for (int k = 0; k < 4; ++k)
			{
				FBullet B = Fire(new FVector2f(X + (k - 1.5f) * 9f, -6f - k * 6f), 1.8f, 90f, EBulletShape.Medium, Rainbow(Melody[N] * 28f));
				B.WaveAmp = 0.4f;
				B.WaveFreq = 0.08f;
			}
			Ring(Boss.Pos, 22, 1.6f, N * 7f + (N % 2) * (180f / 22f), EBulletShape.Small, Pearl);
		}
	}

	// =====================================================================================
	// 窮奇 (the winged tiger) — midboss cards
	// =====================================================================================

	// 奇符「懲善揚惡」: honest shots fly straight at you; lying shots aim where you are about to be
	public void IF_QiongqiLie(int T)
	{
		PatrolBoss(T);
		FVector2f PVel = FVector2f.Zero;
		if (Player.Trail.Count >= 4) PVel = (Player.Trail[0] - Player.Trail[3]) / 3f;
		if (Every(T, 36, 0)) Fan(Boss.Pos, 3, 20f, 2.5f, AimAngle(Boss.Pos), EBulletShape.Rice, Silver);
		if (Every(T, 36, 18))
		{
			// the lying shots come as a ring of crescents, lined up on where you're about to be
			FVector2f Lead = Player.Pos + PVel * 36f;
			Ring(Boss.Pos, Cnt(14), 2.3f, AngleOf(Lead - Boss.Pos), EBulletShape.Crescent, Indigo);
		}
		if (Every(T, 9, 3)) for (int k = 0; k < 2; ++k) Fire(Boss.Pos, 1.4f, T * 4.7f + k * 180f, EBulletShape.Small, Indigo);
	}

	// 奇符「翼虎狂風」: each beat of the wings blows a gale that pushes you sideways while feathers sweep the field
	public void IF_QiongqiGale(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 100f));
		const int Cyc = 150;
		int Ph = T % Cyc;
		float WindDir = (T / Cyc) % 2 != 0 ? -1f : 1f;
		if (Ph >= 40 && Ph < 120 && Player.RespawnTimer == 0 && !IsInDialogue())
		{
			Player.Pos.X = FMath.Clamp(Player.Pos.X + WindDir * 1.1f, 8f, FieldW - 8f);
			if (T % 4 == 0) AddParticle(new FVector2f(WindDir > 0 ? 0f : FieldW, Rng.FRandRange(60f, FieldH)), new FVector2f(WindDir * 7f, 0f), 10f, Silver, 40);
		}
		if (Every(T, 4, 0))
		{
			foreach (float S in new[] { -1f, 1f })
			{
				Fire(Boss.Pos + new FVector2f(S * 46f, -6f), 2f, 90f + S * (25f + FMath.Sin(T * 0.07f) * 55f), EBulletShape.Rice, S > 0 ? Indigo : Silver);
			}
		}
		// a full ring instead of a fan, with twice the gap between crescents
		if (Every(T, 30, 15)) Ring(Boss.Pos, Cnt(24), 2.6f, AimAngle(Boss.Pos) + (T / 30) % 2 * 7.5f, EBulletShape.Crescent, Indigo);
	}

	// 奇符「食人從首」: it eats people head first — red lines mark the columns above you, then teeth fall down them
	public void IF_QiongqiHead(int T)
	{
		PatrolBoss(T);
		if (Every(T, 60, 0))
		{
			float X = FMath.Clamp(Player.Pos.X + Rng.FRandRange(-10f, 10f), 12f, FieldW - 12f);
			AddBeam(new FVector2f(X, 0f), 90f, 12f, 44, 6, 20, new FLinearColor(1f, 0.35f, 0.4f), 4);
		}
		if (Every(T, 5, 2))
		{
			FBullet F = Fire(new FVector2f(Rng.FRandRange(8f, FieldW - 8f), -6f), 2.2f, 90f + Rng.FRandRange(-8f, 8f), EBulletShape.Crescent, Indigo);
			F.Accel = 0.01f;
			F.MaxSpeed = 3f;
		}
	}

	// =====================================================================================
	// The relay — two cards each
	// =====================================================================================

	// 凶符「吞天噬地」: 饕餮 inhales — you are dragged toward it, and bullets stream up from the ground into its mouth
	public void IF_Devour(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 80f));
		if (Player.RespawnTimer == 0 && !IsInDialogue())
		{
			FVector2f D = (Boss.Pos - Player.Pos).GetSafeNormal();
			Player.Pos += D * 0.36f;
		}
		if (Every(T, 4, 0))
		{
			FVector2f From = new FVector2f(Rng.FRandRange(10f, FieldW - 10f), FieldH + 6f);
			Fire(From, 1.9f, AngleOf(Boss.Pos - From) + Rng.FRandRange(-6f, 6f), EBulletShape.Rice, Jade);
		}
		// every bullet it eats is spat straight back out somewhere in the 120° in front of it
		List<FVector2f> Eaten = new();
		SwallowBullets(Boss.Pos, 70f, 16f, 0.04f, Eaten);
		for (int i = 0; i < Eaten.Count; ++i) SpitBullet(Boss.Pos, Rng.FRandRange(1.6f, 2.1f), 90f + Rng.FRandRange(-60f, 60f), EBulletShape.Medium, Bronze);
		Boss.MawEaten = 0;
	}

	// 凶符「無底之胃」: 饕餮 circles clockwise; a bronze cauldron always sits on the opposite side firing rings.
	// Whatever of the cauldron's rings runs into 饕餮 is eaten and spat back out as three bullets in random directions.
	public void IF_Stomach(int T)
	{
		// one big circle over the whole field, top to bottom
		FVector2f C = new FVector2f(FieldW * 0.5f, FieldH * 0.5f);
		FVector2f R = new FVector2f(150f, 175f);
		float A = -90f + T * 0.4f;   // y points down, so a growing angle turns clockwise on screen
		PlaceBoss(C + Dir(A) * R);
		Boss.MawPos = C - Dir(A) * R;
		// the cauldron
		FBullet Pot = FireVec(Boss.MawPos, FVector2f.Zero, EBulletShape.Large, Bronze);
		Pot.bHarmless = true;
		Pot.bBombProof = true;
		Pot.Life = 2;
		Pot.Scale = 2.2f;
		if (T % 8 == 0) AddParticle(Boss.MawPos, FVector2f.Zero, 60f, new FLinearColor(0.6f, 0.45f, 0.15f), 14, true);
		if (T > 60 && Every(T, 26, 0))
		{
			int N = Cnt(26);
			float Base = (T / 26) % 2 != 0 ? 180f / N : 0f;
			for (int i = 0; i < N; ++i) Fire(Boss.MawPos, 1.7f, Base + A + 360f * i / N, EBulletShape.Medium, Bronze);
		}
		List<FVector2f> Eaten = new();
		SwallowBullets(Boss.Pos, 26f, 26f, 0f, Eaten);
		// every bullet it eats comes back out as three
		for (int i = 0; i < Eaten.Count; ++i)
			for (int k = 0; k < 3; ++k) SpitBullet(Boss.Pos, Rng.FRandRange(1.4f, 2.1f), Rng.FRandRange(0f, 360f), EBulletShape.Rice, Jade);
		Boss.MawEaten = 0;
	}

	// 凶符「傲狠明德」: 檮杌 marks you, then charges straight at you, scattering iron behind it
	public void IF_Arrogance(int T)
	{
		int Cyc = FMath.Max(60, FMath.RoundToInt(150 * GetDifficultyInfo(Difficulty).IntervalScale));
		int Ph = T % Cyc;
		if (Ph == 1)
		{
			// wherever it is, it charges at where you are — even straight down to the bottom of the field
			Boss.MawPos = new FVector2f(FMath.Clamp(Player.Pos.X, 20f, FieldW - 20f), FMath.Clamp(Player.Pos.Y, 20f, FieldH - 20f));
			AddBeam(Boss.Pos, AngleOf(Boss.MawPos - Boss.Pos), 4f, 40, 1, 1, new FLinearColor(1f, 0.3f, 0.3f)).bHarmless = true;   // just the aim line
		}
		if (Ph == 40) MoveBossTo(Boss.MawPos, 34);
		if (Ph > 40 && Ph < 70 && Ph % 2 == 0)
		{
			foreach (float S in new[] { -1f, 1f }) Fire(Boss.Pos, 0.9f, AngleOf(Boss.Vel) + 180f + S * 70f, EBulletShape.Medium, Iron);
		}
		if (Ph == 70)
		{
			int N = Cnt(20);
			float Base = Rng.FRandRange(0f, 18f);
			Ring(Boss.Pos, N, 1.8f, Base, EBulletShape.Crescent, Crimson);
			Ring(Boss.Pos, N, 1.2f, Base + 180f / N, EBulletShape.Crescent, Iron);
		}
		// then it just stands where it landed, glaring, until the next charge
		if (Ph == 120) Ring(Boss.Pos, 18, 1.4f, 0f, EBulletShape.Large, Crimson);
	}

	// 凶符「不退之陣」: walls of iron advance and never retreat — find the gap — while tusks thrust in from both sides of you
	public void IF_NoRetreat(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 70f));
		if (Every(T, 60, 0))
		{
			const int Slots = 16;
			// the gap wanders from row to row instead of jumping anywhere, so it can always be reached in time
			int Prev = T == 0 ? (Slots - 4) / 2 : FMath.RoundToInt(Boss.MawPos.X);
			int Gap = FMath.Clamp(Prev + Rng.RandRange(-3, 3), 1, Slots - 5);
			Boss.MawPos.X = (float)Gap;
			for (int i = 0; i < Slots; ++i)
			{
				if (i >= Gap && i < Gap + 4) continue;
				FBullet B = Fire(new FVector2f((i + 0.5f) * FieldW / Slots, -8f), 1.5f, 90f, EBulletShape.Large, Iron);
				// standing at the bottom isn't safe: each iron ball shatters on the ground into shards
				B.Behavior = BehGroundSplit;
				B.Param = 2;
				B.Param2 = 0.9f;
				B.Timer = 1;   // the shards keep drifting until they leave the field
			}
		}
	}

	// 凶符「混元一氣」: primordial mist — sparks appear anywhere (never right on you), flare up, then drift
	public void IF_PrimalMist(int T)
	{
		PatrolBoss(T);
		if (Every(T, 3, 0))
		{
			FVector2f P = default;
			for (int Try = 0; Try < 20; ++Try)
			{
				P = new FVector2f(Rng.FRandRange(12f, FieldW - 12f), Rng.FRandRange(40f, FieldH - 30f));
				if (V2.Distance(P, Player.Pos) > 80f) break;
			}
			FBullet B = FireVec(P, FVector2f.Zero, EBulletShape.Medium, Rainbow(T * 3f));
			B.Behavior = BehArm;
			B.Timer = 45;
			B.bHarmless = true;
			B.Scale = 0.4f;
			B.Center = Dir(Rng.FRandRange(0f, 360f)) * Rng.FRandRange(0.5f, 1.2f);
			B.Life = 600;
		}
		if (Every(T, 30, 15)) Ring(Boss.Pos, 28, 1.8f, AimAngle(Boss.Pos), EBulletShape.Small, Pearl);
	}

	// 凶符「渾沌初開」: heaven and earth split apart — a light arm and a dark arm turn against each other
	public void IF_ChaosSplit(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 120f));
		if (Every(T, 4, 0))
		{
			for (int k = 0; k < 2; ++k)
			{
				float S = k == 0 ? 1f : -1f;
				// three light arms and three dark arms: six bullets at a time
				for (int a = 0; a < 3; ++a)
				{
					FBullet B = Fire(Boss.Pos, 1.9f, S * T * 2.6f + a * 120f + k * 60f, EBulletShape.Medium, k == 0 ? Pearl : Ink);
					B.AngVel = S * 0.35f;
					B.AngVelDecay = 0.99f;
				}
			}
		}
		if (Every(T, 70, 35))
		{
			for (int k = 0; k < 2; ++k)
			{
				int N = Cnt(24);
				for (int i = 0; i < N; ++i)
				{
					FBullet B = Fire(Boss.Pos, 1.3f, 360f * i / N + k * 9f, EBulletShape.Large, k == 0 ? Pearl : Ink);
					B.AngVel = k == 0 ? 0.3f : -0.3f;
					B.AngVelDecay = 0.995f;
				}
			}
		}
	}

	// 凶符「顛倒是非」: right and wrong turned upside down. 窮奇 throws straight lines of feathers all the way round,
	// honest (purple) and lying (white) lines alternating; a mirror below throws the same lines upward, colours swapped,
	// and the boss alone still throws its ring of crescents.
	void IF_InversionLines(FVector2f From, int Lines, float Base, float Spread, FLinearColor Honest, FLinearColor Liar)
	{
		const int PerLine = 8;
		for (int l = 0; l < Lines; ++l)
		{
			float A = Base + (Lines > 1 ? Spread * l / (Lines - 1) : 0f) - Spread * 0.5f;
			bool bHonest = l % 2 == 0;
			for (int k = 0; k < PerLine; ++k)
			{
				// one line = the same direction at rising speeds, so the feathers sit in a row
				// the lines fly straight (they used to turn sideways, which walled the boss off)
				FBullet F = Fire(From, 0.9f + k * 0.24f, A, EBulletShape.Rice, bHonest ? Honest : Liar);
				F.Scale = 0.5f;
				F.Radius *= 0.5f;
			}
		}
	}

	public void IF_Inversion(int T)
	{
		PatrolBoss(T);
		int Cyc = FMath.Max(30, FMath.RoundToInt(64 * GetDifficultyInfo(Difficulty).IntervalScale));
		int Ph = T % Cyc;
		int W = T / Cyc;
		if (Ph == 0)
		{
			// above: lines all the way round; the honest / lying order swaps every wave
			int N = Cnt(6);
			float Base = Rng.FRandRange(0f, 360f);
			IF_InversionLines(Boss.Pos, N, Base + 180f, 360f - 360f / N, Indigo, Silver);
		}
		if (Ph == Cyc / 2)
		{
			// below: the mirror image throws a fan of lines upward, turning the other way
			FVector2f Mirror = new FVector2f(Boss.Pos.X, FieldH + 10f);
			IF_InversionLines(Mirror, FMath.Max(2, Cnt(3)), -90f + Rng.FRandRange(-15f, 15f), 100f, Silver, Indigo);
		}
		if (Every(T, 90, 45))
		{
			// the boss alone throws a ring of crescents (alternating offset); the mirror below fires none
			int N = Cnt(16);
			float Base = AimAngle(Boss.Pos) + ((T / 90) % 2 != 0 ? 180f / N : 0f);
			Ring(Boss.Pos, N, 1.7f, Base, EBulletShape.Crescent, Indigo);
		}
	}

	// 凶符「窮奇之翼」: the tiger sweeps from wall to wall shedding feathers that hang in the air, then dive at you
	public void IF_Wings(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f + FMath.Sin(T * 0.028f) * 140f, 80f + FMath.Abs(FMath.Cos(T * 0.028f)) * 40f));
		if (Every(T, 3, 0))
		{
			// half-size feathers; three times as many purple ones as white ones
			bool bPurple = (T / 3) % 2 != 0;
			for (int k = 0; k < (bPurple ? 6 : 2); ++k)
			{
				FBullet F = FireVec(Boss.Pos + new FVector2f(Rng.FRandRange(-30f, 30f), 0f), new FVector2f(Rng.FRandRange(-0.3f, 0.3f), 1.3f), EBulletShape.Rice, (T / 3) % 2 != 0 ? Indigo : Silver);
				F.Behavior = BehPauseHome;
				F.Timer = 30;
				F.Timer2 = Rng.RandRange(20, 70);
				F.Param2 = 2.3f;
				F.Scale = 0.5f;
				F.Radius *= 0.5f;
				if ((T / 3) % 2 != 0) F.Param = 9999;   // the purple feathers dive off in any direction, the white ones at you
			}
		}
		if (Every(T, 56, 28)) Ring(Boss.Pos, 28, 1.6f, T * 0.9f, EBulletShape.Crescent, Indigo);
	}

	// =====================================================================================
	// All four together
	// =====================================================================================

	// 耐久「四凶降世」 (survival): each beast attacks in its own way from its corner of the diamond, faster as time runs on
	public void IF_QuadSurvive(int T)
	{
		float K = 1.4f + FMath.Clamp(T / 3600f, 0f, 1f) * 0.9f;
		PlaceQuad(T, 120f);
		bool Ev(int Iv, int Off) { return Every(T, FMath.Max(2, FMath.RoundToInt(Iv / K)), Off); }
		FVector2f[] Q = Boss.QuadPos;
		if (Ev(56, 0))
		{
			int N = Cnt(18);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Q[0], 2.8f, 360f * i / N + T, EBulletShape.Medium, Bronze);
				B.Accel = -0.04f;
				B.MinSpeed = -1.4f;
				B.Life = 400;
			}
		}
		if (Ev(70, 20))
		{
			FBullet B = Fire(Q[1], 1.6f, Rng.FRandRange(40f, 140f), EBulletShape.Large, Crimson);
			B.WallMask = 1 | 2 | 4;
			B.Bounces = 5;
			B.Life = 480;
		}
		if (Ev(8, 4))
		{
			FBullet B = Fire(Q[2], 1.5f, Rng.FRandRange(0f, 360f), EBulletShape.Small, Rainbow(T * 2f));
			B.AngVel = Rng.FRandRange(-1f, 1f);
			B.AngVelDecay = 0.99f;
		}
		if (Ev(36, 10)) Fan(Q[3], 3, 44f, 2.6f, AimAngle(Q[3]), EBulletShape.Crescent, Indigo);   // 22° between crescents
	}

	static readonly int[] QuadMainBeastMainOf = { 3, 1, 0, 2 };

	public int QuadMainBeast()
	{
		// the beast this character met on the road leads: 蓮 窮奇, 蒼 檮杌, 晴 饕餮, 雪 混沌
		int[] MainOf = QuadMainBeastMainOf;
		return MainOf[FMath.Clamp((int)Player.Character, 0, NumCharacters - 1)];
	}

	// One beast's signature for the last card. The leader fires it in full; the other three back it up with a thinner copy.
	public void IF_BeastSig(int Beast, FVector2f P, int T, bool bMain)
	{
		float Lost = 1f - FMath.Clamp(Boss.HP / FMath.Max(1f, Boss.MaxHP), 0f, 1f);
		float Iv = (bMain ? 0.85f : 1.6f) / (1f + 0.4f * Lost);
		bool Ev(int I, int Off) { return Every(T, FMath.RoundToInt(I * Iv), Off); }
		switch (Beast)
		{
		case 0:   // 饕餮: rings that run out of breath and fall back; the leader also eats and spits
			if (Ev(48, 0))
			{
				int N = Cnt(bMain ? 24 : 12);
				float Base = Rng.FRandRange(0f, 360f);
				for (int i = 0; i < N; ++i)
				{
					FBullet B = Fire(P, 2.9f, Base + 360f * i / N, EBulletShape.Medium, i % 2 != 0 ? Bronze : Jade);
					B.Accel = -0.04f;
					B.MinSpeed = -1.5f;
					B.Life = 420;
				}
			}
			if (bMain)
			{
				List<FVector2f> Eaten = new();
				SwallowBullets(P, 50f, 16f, 0.03f, Eaten);
				for (int i = 0; i < Eaten.Count; ++i) SpitBullet(P, Rng.FRandRange(1.5f, 2f), 90f + Rng.FRandRange(-60f, 60f), EBulletShape.Rice, Jade);
				Boss.MawEaten = 0;
			}
			break;
		case 1:   // 檮杌: crescents run away from you, stop, then charge; iron sweeping down (no bouncing boulders: too much at once)
			if (Ev(46, 8))
			{
				int N = Cnt(bMain ? 12 : 5);
				float Off = (T / FMath.Max(1, FMath.RoundToInt(46 * Iv))) % 2 != 0 ? 90f : -90f;
				for (int i = 0; i < N; ++i)
				{
					// the red moons are untamed: they fly off anywhere, stop, then dash off anywhere
					FBullet B = FireVec(P, Dir(Rng.FRandRange(0f, 360f)) * 2.4f, EBulletShape.Crescent, Crimson);
					B.Behavior = BehPauseDash;
					B.Timer = 36;
					B.Param = 9999;
					B.Param2 = bMain ? 2.6f : 2.2f;
				}
				if (bMain)
				{
					// and a second spread off to one side, alternating left and right
					for (int i = 0; i < Cnt(6); ++i)
					{
						FBullet B = FireVec(P, Dir(AimAngle(P) + Off + (i - 2.5f) * 12f) * 2.2f, EBulletShape.Crescent, Iron);
						B.Behavior = BehPauseDash;
						B.Timer = 44;
						B.Param2 = 2.4f;
					}
				}
			}
			if (Ev(7, 3))
			{
				float Sweep = FMath.Fmod(T * 2.2f, 120f);
				foreach (float S in new[] { -1f, 1f }) Fire(P, 2.3f, 90f + S * (15f + Sweep), EBulletShape.Medium, S > 0 ? Iron : Crimson);
			}
			break;
		case 2:   // 混沌: sparks with no aim that wander on their own
			if (Ev(4, 2))
			{
				FBullet B = Fire(P, Rng.FRandRange(1.3f, 1.9f), Rng.FRandRange(0f, 360f), EBulletShape.Small, Rainbow(T * 2f));
				B.AngVel = Rng.FRandRange(-1.2f, 1.2f);
				B.AngVelDecay = 0.99f;
			}
			break;
		default:  // 窮奇: an honest shot at you and a lying one at where you're going
			if (Ev(34, 17))
			{
				FVector2f PVel = FVector2f.Zero;
				if (Player.Trail.Count >= 4) PVel = (Player.Trail[0] - Player.Trail[3]) / 3f;
				// 5 crescents each, 30° apart (the helpers send 3)
				int N = bMain ? 5 : 3;
				Fan(P, N, 30f * (N - 1), 2.6f, AimAngle(P), EBulletShape.Crescent, Silver);
				Fan(P, N, 30f * (N - 1), 2.6f, AngleOf(Player.Pos + PVel * 45f - P), EBulletShape.Crescent, Indigo);
			}
			break;
		}
	}

	static readonly FVector2f[] IF_QuadFinalSlots = { new(-125f, -62f), new(0f, -95f), new(125f, -62f) };

	// 終符「四凶・天地失序」: the last card, fought like a trio-plus-one — the beast you met on the road leads from the
	// front with its own signature at full strength; the other three hang back and fire weaker copies of the same pattern
	public void IF_QuadFinal(int T)
	{
		int Main = QuadMainBeast();
		float Lost = 1f - FMath.Clamp(Boss.HP / FMath.Max(1f, Boss.MaxHP), 0f, 1f);
		// the leader roams the front; the others keep a loose arc behind it
		FVector2f Lead = new FVector2f(FieldW * 0.5f + FMath.Sin(T * 0.011f) * 100f, 150f + FMath.Sin(T * 0.019f) * 22f);
		FVector2f[] Slots = IF_QuadFinalSlots;
		FVector2f Back = new FVector2f(FieldW * 0.5f, 160f);
		Boss.Pos = Lead;
		Boss.MoveTime = Boss.MoveDur;
		int Slot = 0;
		for (int i = 0; i < 4; ++i)
		{
			FVector2f Target = Lead;
			if (i != Main)
			{
				FVector2f S = Slots[Slot++];
				Target = Back + S + new FVector2f(FMath.Sin(T * 0.023f + i) * 18f, FMath.Cos(T * 0.031f + i) * 10f);
			}
			Boss.QuadPos[i] = FMath.Lerp(Boss.QuadPos[i], Target, 0.05f);
		}
		// the three helpers back the leader up with a weaker copy of the leader's own pattern
		for (int i = 0; i < 4; ++i) IF_BeastSig(Main, Boss.QuadPos[i], T + i * 11, i == Main);
		// the four together: a slow ring from the leader, a little more often as they weaken
		int RingIv = FMath.RoundToInt(FMath.Lerp(80f, 50f, Lost));
		if (Every(T, RingIv, RingIv / 2)) Ring(Boss.QuadPos[Main], Cnt(32), 1.5f, T * 0.7f, EBulletShape.Medium, new FLinearColor(1f, 0.85f, 0.5f));
	}

	// =====================================================================================
	// Non-spells — every card is preceded by one. Later ones in a fight get a little denser.
	// =====================================================================================

	// 饕餮: alternating bronze / jade rings, a hungry aimed spray
	public void IF_NsTaotie(int T)
	{
		if (Every(T, 18, 0)) Ring(Boss.Pos, Cnt(20), 2.0f, T * 2.1f, EBulletShape.Medium, (T / 18) % 2 != 0 ? Bronze : Jade);
		if (Every(T, 46, 23)) Fan(Boss.Pos, Cnt(9), 60f, 3.0f, AimAngle(Boss.Pos), EBulletShape.Rice, Jade);
		if (Every(T, 160, 80)) WanderBoss();
	}

	// 檮杌: two iron lines sweeping down from both sides, boulders thrown at you
	public void IF_NsTaowu(int T)
	{
		if (Every(T, 5, 0))
		{
			float Sweep = FMath.Fmod(T * 2.4f, 140f);
			foreach (float S in new[] { -1f, 1f }) Fire(Boss.Pos, 2.5f, 90f + S * (10f + Sweep), EBulletShape.Crescent, S > 0 ? Iron : Crimson);
		}
		if (Every(T, 54, 27)) for (int k = 0; k < 3; ++k) Fire(Boss.Pos, 1.6f + k * 0.45f, AimAngle(Boss.Pos), EBulletShape.Large, Crimson);
		if (Every(T, 170, 85)) WanderBoss();
	}

	// 混沌: a spray with no aim at all, and pale rings that do aim
	public void IF_NsHundun(int T)
	{
		if (Every(T, 3, 0))
		{
			FBullet B = Fire(Boss.Pos, Rng.FRandRange(1.4f, 2.4f), Rng.FRandRange(0f, 360f), EBulletShape.Small, Rainbow(T * 3f));
			B.AngVel = Rng.FRandRange(-0.8f, 0.8f);
			B.AngVelDecay = 0.985f;
		}
		if (Every(T, 40, 20)) Ring(Boss.Pos, Cnt(26), 1.9f, AimAngle(Boss.Pos), EBulletShape.Rice, Pearl);
		if (Every(T, 150, 75)) WanderBoss();
	}

	// 窮奇: wingbeats — feathers fan out from both wings, then a crescent fan at you
	public void IF_NsQiongqi(int T)
	{
		if (Every(T, 7, 0))
		{
			foreach (float S in new[] { -1f, 1f })
			{
				Fire(Boss.Pos + new FVector2f(S * 40f, -4f), 2.4f, 90f + S * (10f + FMath.Sin(T * 0.05f) * 50f), EBulletShape.Rice, S > 0 ? Indigo : Silver);
			}
		}
		if (Every(T, 44, 22)) Fan(Boss.Pos, Cnt(7), 44f, 2.9f, AimAngle(Boss.Pos), EBulletShape.Crescent, Indigo);
		if (Every(T, 160, 80)) WanderBoss();
	}

	// =====================================================================================
	// Road: 崩落天宮 (~120 s, one beast blocks the way at the halfway point)
	// =====================================================================================

	public void RoadIf(int F)
	{
		const int MidAt = 3000;
		static bool Rep(int F, int Start, int Count, int Interval, ref int K)
		{
			if (F < Start) return false;
			int D = F - Start;
			if (D % Interval != 0) return false;
			K = D / Interval;
			return K < Count;
		}
		int K = 0;
		if (Rep(F, 90, 30, 12, ref K))
		{
			bool bL = K % 2 == 0;
			Doll(new FVector2f(bL ? -16f : FieldW + 16f, 30f + (K % 5) * 20f), new FVector2f(bL ? 2.2f : -2.2f, 0.6f), K % 3 == 0 ? 19 : 0).Color = Crimson;
		}
		if (Rep(F, 500, 6, 50, ref K)) Sentry(new FVector2f(60f + K * 52f, -20f), 8, 60, 300).Color = Bronze;
		if (Rep(F, 900, 30, 10, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 2f), K % 2 != 0 ? 2 : 21).Color = Indigo;
		if (F == 1400) { Spirit(new FVector2f(110f, -30f), 3, 600f).Color = Crimson; Spirit(new FVector2f(FieldW - 110f, -30f), 17, 600f).Color = Indigo; }
		if (Rep(F, 1500, 12, 36, ref K)) Doll(new FVector2f(K % 2 != 0 ? 40f : FieldW - 40f, -16f), new FVector2f(K % 2 != 0 ? 1f : -1f, 1.6f), 6, true).Color = Pearl;
		if (Rep(F, 2100, 40, 8, ref K))
		{
			FEnemy E = Doll(new FVector2f(FieldW * 0.5f, -16f), Dir(90f + FMath.Sin(K * 0.5f) * 50f) * 1.9f, K % 4 == 0 ? 0 : (K % 4 == 2 ? 19 : -1));
			E.Color = Bronze;
		}
		if (Rep(F, 2300, 4, 60, ref K)) Sentry(new FVector2f(80f + K * 75f, -20f), 12, 60, 260).Color = Jade;
		if (!bMidDone)
		{
			if (F >= MidAt)
			{
				if (bDemo && DemoSkipFrames > MidAt) bMidDone = true;
				else if (Enemies.Count == 0 || F >= MidAt + 240) StartMidboss();
			}
			return;
		}
		if (Rep(F, 3300, 30, 12, ref K)) Doll(new FVector2f(K % 2 != 0 ? -16f : FieldW + 16f, 40f + (K % 4) * 20f), new FVector2f(K % 2 != 0 ? 2f : -2f, 0.8f), 19, K % 5 == 0).Color = Crimson;
		if (Rep(F, 3600, 8, 45, ref K)) Sentry(new FVector2f(50f + (K % 6) * 57f, -20f), K % 2 != 0 ? 1 : 15, 60, 280).Color = Indigo;
		if (F == 4300) for (int i = 0; i < 2; ++i) Spirit(new FVector2f(110f + i * 164f, -30f), i != 0 ? 13 : 9, 600f);
		if (Rep(F, 4400, 30, 14, ref K)) Doll(new FVector2f(Rng.FRandRange(30f, FieldW - 30f), -16f), new FVector2f(0f, 1.8f), K % 3 == 0 ? 16 : 0).Color = Pearl;
		if (Rep(F, 5200, 44, 8, ref K)) Doll(new FVector2f(FieldW * 0.5f + (K % 2 != 0 ? -1f : 1f) * (20f + (K % 11) * 14f), -16f), new FVector2f(0f, 2f), K % 3 == 0 ? 2 : -1).Color = Bronze;
		if (F == 6000) for (int i = 0; i < 4; ++i) Sentry(new FVector2f(64f + i * 85f, -20f), i % 2 != 0 ? 1 : 8, 60, 360).Color = i % 2 != 0 ? Crimson : Indigo;
		if (F == 6500) OneUpCarrier();   // the last fairy before the four perils carries a 1UP
		if (F >= 7000 && (Enemies.Count == 0 || F >= 7300)) StartBoss();
	}
}
