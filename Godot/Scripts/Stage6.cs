// Stage 6 (final) boss: 燁, the solemn guardian of the four symbols.
// One card per beast, then 「四象歸一」: the longest card in the main game. It starts with the azure dragon only and
// adds the white tiger, vermilion bird and black tortoise as its HP drops below 75 / 50 / 25 %.
// Port of DanmakuStage6.cpp.
using static DanmakuSTG.Stage6Locals;

namespace DanmakuSTG;

static class Stage6Locals
{
	public static readonly FLinearColor ColDragon = new FLinearColor(0.3f, 0.85f, 1f);
	public static readonly FLinearColor ColTiger = new FLinearColor(0.95f, 0.96f, 1f);
	public static readonly FLinearColor ColBird = new FLinearColor(1f, 0.45f, 0.25f);
	public static readonly FLinearColor ColTortoise = new FLinearColor(0.35f, 0.5f, 1f);
	public static readonly FLinearColor ColGoldS6 = new FLinearColor(1f, 0.85f, 0.45f);

	// how hard each beast goes at each difficulty
	static readonly float[] BeastKTable = { 0.72f, 0.85f, 1f, 1.18f };
	public static float BeastK(int Difficulty)
	{
		return BeastKTable[FMath.Clamp(Difficulty, 0, 3)];
	}

	public static readonly string[] FinalNames = { "青龍", "白虎", "朱雀", "玄武" };
	public static readonly FLinearColor[] FinalCols = { ColDragon, ColTiger, ColBird, ColTortoise };
}

public partial class DanmakuWorld
{
	// ---------- the four beasts as reusable parts (K = intensity, 1 = full card) ----------

	public void S6_DragonPart(int T, float K)
	{
		// a dragon's body: a stream that snakes left and right down the field
		int Iv = FMath.Max(2, FMath.RoundToInt(4 / K));
		if (Every(T, Iv, 0))
		{
			FVector2f Head = Boss.Pos + new FVector2f(FMath.Sin(T * 0.028f) * 110f, 10f);
			// the dragon's body is two scales wide, three on Lunatic
			int Lanes = Difficulty >= 3 && K > 1f ? 3 : 2;
			for (int L = 0; L < Lanes; ++L)
			{
				float Off = (L - (Lanes - 1) * 0.5f) * 16f;
				FBullet B = Fire(Head + new FVector2f(Off, 0f), 2.1f, 90f + FMath.Sin(T * 0.06f) * 28f, EBulletShape.Rice, ColDragon);
				B.WaveAmp = 0.5f;
				B.WaveFreq = 0.04f;
			}
		}
		if (Every(T, FMath.RoundToInt(70 / K), 35)) Ring(Boss.Pos, Cnt(18), 1.6f, T * 0.9f, EBulletShape.Medium, new FLinearColor(0.45f, 0.95f, 0.8f));
	}

	public void S6_TigerPart(int T, float K)
	{
		// claws: three parallel streaks raking at the player, then a wide crescent sweep
		int Cyc = FMath.Max(1, FMath.RoundToInt(70 / K));
		int Ph = T % Cyc;
		if (Ph == 0)
		{
			float Aim = AimAngle(Boss.Pos);
			FVector2f Fwd = Dir(Aim), Side = Dir(Aim + 90f);
			for (int c = -1; c <= 1; ++c)
			{
				int N = Cnt(9);
				for (int j = 0; j < N; ++j)
				{
					FBullet B = Fire(Boss.Pos + Side * (c * 26f) - Fwd * (j * 9f), 3.3f, Aim + c * 4f, EBulletShape.Crescent, ColTiger);
					B.Accel = 0.01f;
					B.MaxSpeed = 4.2f;
				}
			}
		}
		if (Ph == 24)
		{
			int N = Cnt(26);
			float Base = AimAngle(Boss.Pos) + 180f / N;
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Boss.Pos, 1.6f, Base + 360f * i / N, EBulletShape.Crescent, new FLinearColor(0.85f, 0.9f, 1f));
				B.Accel = 0.012f;
				B.MaxSpeed = 2.8f;
			}
		}
		// wind: gusts from both walls at once, in rows
		if (Every(T, FMath.Max(2, FMath.RoundToInt(10 / K)), 0))
		{
			float Y = Rng.FRandRange(40f, FieldH * 0.78f);
			Fire(new FVector2f(-6f, Y), 2.4f, 6f + Rng.FRandRange(-4f, 4f), EBulletShape.Small, new FLinearColor(0.8f, 0.9f, 1f));
			Fire(new FVector2f(FieldW + 6f, Y + 30f), 2.4f, 174f + Rng.FRandRange(-4f, 4f), EBulletShape.Small, new FLinearColor(0.8f, 0.9f, 1f));
		}
	}

	public void S6_PhoenixPart(int T, float K)
	{
		// wings of flame sweeping open and shut, embers falling from the sky
		if (Every(T, FMath.RoundToInt(7 / K), 0))
		{
			float Spread = 40f + FMath.Sin(T * 0.045f) * 32f;
			foreach (float Sgn in new[] { -1f, 1f })
			{
				Fire(Boss.Pos, 2f, 90f + Sgn * Spread, EBulletShape.Fire, ColBird);
			}
		}
		if (Every(T, FMath.RoundToInt(16 / K), 8))
		{
			FBullet E = FireVec(new FVector2f(Rng.FRandRange(8f, FieldW - 8f), -6f), new FVector2f(Rng.FRandRange(-0.3f, 0.3f), 0.9f), EBulletShape.Fire, new FLinearColor(1f, 0.7f, 0.3f));
			E.Gravity = new FVector2f(0f, 0.015f);
		}
	}

	public void S6_TortoisePart(int T, float K)
	{
		// a heavy shell: slow rings that ricochet once off every wall
		if (Every(T, FMath.RoundToInt(60 / K), 0))
		{
			int N = Cnt(20);
			float Base = Rng.FRandRange(0f, 360f);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Boss.Pos, 1.15f, Base + 360f * i / N, EBulletShape.Water, ColTortoise);
				B.WallMask = 1 | 2 | 4;
				B.Bounces = 1;
			}
		}
		if (Every(T, FMath.RoundToInt(130 / K), 65)) Ring(Boss.Pos, Cnt(10), 0.85f, Rng.FRandRange(0f, 36f), EBulletShape.Large, new FLinearColor(0.3f, 0.75f, 0.55f));
	}

	// ---------- phases ----------

	public void S6_NonSpell1(int T)
	{
		if (Every(T, 6, 0)) Ring(Boss.Pos, 8, 2.2f, T * 3.1f, EBulletShape.Medium, ColGoldS6);
		if (Every(T, 11, 5)) Ring(Boss.Pos, 6, 1.5f, -T * 2.2f, EBulletShape.Small, ColDragon);
		if (Every(T, 45, 20)) Fan(Boss.Pos, 9, 50f, 3f, AimAngle(Boss.Pos), EBulletShape.Rice, ColDragon);
		if (Every(T, 180, 90)) WanderBoss();
	}

	public void S6_Dragon(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 95f));
		S6_DragonPart(T, 0.95f * BeastK(Difficulty));
	}

	public void S6_NonSpell2(int T)
	{
		if (Every(T, 24, 0)) Ring(Boss.Pos, 28, 1.9f, Rng.FRandRange(0f, 15f), EBulletShape.Crescent, ColTiger);
		if (Every(T, 24, 12)) Fan(Boss.Pos, 7, 36f, 3.2f, AimAngle(Boss.Pos), EBulletShape.Comet, ColGoldS6);
		if (Every(T, 8, 0))
		{
			bool bLeft = (T / 8) % 2 == 0;
			Fire(new FVector2f(bLeft ? -6f : FieldW + 6f, Rng.FRandRange(60f, FieldH * 0.7f)), 2f, bLeft ? 5f : 175f, EBulletShape.Small, new FLinearColor(0.8f, 0.9f, 1f));
		}
		if (Every(T, 170, 85)) WanderBoss();
	}

	public void S6_Tiger(int T)
	{
		// the tiger prowls from side to side faster than the other beasts
		PlaceBoss(new FVector2f(FieldW * 0.5f + FMath.Sin(T * 0.03f) * 100f, 100f + FMath.Abs(FMath.Sin(T * 0.03f)) * 20f));
		S6_TigerPart(T, 1.65f * BeastK(Difficulty));
		// Lunatic: the tiger also pounces with a fan of claws between strikes
		if (Difficulty >= 3 && Every(T, 70, 35)) Fan(Boss.Pos, 5, 32f, 3f, AimAngle(Boss.Pos), EBulletShape.Comet, ColTiger);
	}

	public void S6_NonSpell3(int T)
	{
		if (Every(T, 4, 0))
		{
			for (int i = 0; i < 4; ++i) Fire(Boss.Pos, 1.8f, -T * 2.7f + 90f * i, EBulletShape.Fire, ColBird);
		}
		if (Every(T, 60, 30)) Ring(Boss.Pos, 24, 1.4f, 0f, EBulletShape.Large, ColGoldS6);
		if (Every(T, 60, 0)) Fan(Boss.Pos, 5, 24f, 3.2f, AimAngle(Boss.Pos), EBulletShape.Fire, new FLinearColor(1f, 0.75f, 0.3f));
		if (Every(T, 160, 80)) WanderBoss();
	}

	public void S6_Phoenix(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 110f + FMath.Sin(T * 0.03f) * 10f));
		S6_PhoenixPart(T, 1.7f * BeastK(Difficulty));
		if (Every(T, 30, 15)) Fan(Boss.Pos, Cnt(5), 36f, 2.6f, AimAngle(Boss.Pos) + ((T / 30) % 2 != 0 ? 4.5f : -4.5f), EBulletShape.Fire, ColBird);
	}

	public void S6_Tortoise(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 100f));
		S6_TortoisePart(T, 1.7f * BeastK(Difficulty));
		// the shell also sheds slow aimed scales between rings
		if (Every(T, 40, 20)) Fan(Boss.Pos, Cnt(5), 40f, 1.6f, AimAngle(Boss.Pos), EBulletShape.Water, ColTortoise);
	}

	public void S6_NonSpell4(int T)
	{
		S6_DragonPart(T, 0.7f);
		S6_TigerPart(T, 0.6f);
		if (Every(T, 150, 75)) WanderBoss();
	}

	public void S6_Final(int T)
	{
		PatrolBoss(T);
		float HP = FMath.Clamp(Boss.HP / FMath.Max(1f, Boss.MaxHP), 0f, 1f);
		int Beasts = HP > 0.75f ? 1 : HP > 0.5f ? 2 : HP > 0.25f ? 3 : 4;
		if (Beasts != Boss.Stage6Beasts)
		{
			// announce each new beast joining the card
			Boss.Stage6Beasts = Beasts;
			if (Beasts > 1)
			{
				ShowMessage(Tr(FinalNames[Beasts - 1]) + "　" + Tr("降臨"), "", 90);
				AddParticle(Boss.Pos, V2.ZeroVector, 420f, FinalCols[Beasts - 1], 40, true);
				Sfx(ESfx.Spell);
			}
		}
		// all four beasts at once already stack up, so the difficulty boost counts half here
		float K = (Beasts == 1 ? 1f : Beasts == 2 ? 0.75f : Beasts == 3 ? 0.62f : 0.55f) * (1f + (BeastK(Difficulty) - 1f) * 0.5f);
		S6_DragonPart(T, K);
		if (Beasts >= 2) S6_TigerPart(T, K);
		if (Beasts >= 3) S6_PhoenixPart(T, K);
		if (Beasts >= 4) S6_TortoisePart(T, K);
	}
}
