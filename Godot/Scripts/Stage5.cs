// Stage 5 boss: 玄, the cryptic sorcerer of the five elements.
// One card per element, then 「相生相剋」 cycles through all five, each element feeding the next.
// Port of DanmakuStage5.cpp.
using static DanmakuSTG.Stage5Locals;

namespace DanmakuSTG;

static class Stage5Locals
{
	public static readonly FLinearColor[] ElemCol = {
		new FLinearColor(0.4f, 1f, 0.45f),   // wood
		new FLinearColor(1f, 0.45f, 0.25f),  // fire
		new FLinearColor(0.95f, 0.8f, 0.35f), // earth
		new FLinearColor(0.92f, 0.95f, 1f),  // metal
		new FLinearColor(0.35f, 0.6f, 1f),   // water
	};

	public static readonly float[] EarthWalls = { 1.1f, 1.25f, 1.5f, 1.9f };
	public static readonly float[] WaterRivers = { 0.8f, 0.9f, 1f, 1.15f };
	public static readonly float[] CycleStrength = { 0.8f, 0.9f, 1f, 1.25f };
}

public partial class DanmakuWorld
{
	public void S5_NonSpell1(int T)
	{
		// five-coloured five-armed spiral with a slower counter-spiral, and aimed blades
		if (Every(T, 5, 0))
		{
			for (int i = 0; i < 5; ++i) Fire(Boss.Pos, 1.9f, T * 2.3f + 72f * i, EBulletShape.Medium, ElemCol[i]);
		}
		if (Every(T, 9, 4))
		{
			for (int i = 0; i < 5; ++i) Fire(Boss.Pos, 1.4f, -T * 1.7f + 72f * i + 36f, EBulletShape.Small, ElemCol[(i + 2) % 5]);
		}
		if (Every(T, 45, 20)) Fan(Boss.Pos, 7, 42f, 3f, AimAngle(Boss.Pos), EBulletShape.Rice, ElemCol[3]);
		if (Every(T, 180, 90)) WanderBoss();
	}

	// 木符「萬木生長」: sprouting seeds plus two growing vines
	public void S5_Wood(int T)
	{
		PatrolBoss(T);
		ElementVolley(0, T, 1.2f);
		if (Every(T, 6, 0))
		{
			foreach (float Off in new[] { 0f, 120f, 240f })
			{
				FBullet V = Fire(Boss.Pos, 1.7f, T * 2.2f + Off, EBulletShape.Rice, new FLinearColor(0.55f, 0.95f, 0.4f));
				V.AngVel = 0.35f;
				V.AngVelDecay = 0.985f;
			}
		}
	}

	// 火符「烈火燎原」: a fountain of fire that rains down, rings of flame spreading from the boss
	public void S5_Fire(int T)
	{
		PatrolBoss(T);
		ElementVolley(1, T, 1.7f);
		if (Every(T, 32, 0))
		{
			// a ring of flames that speeds up as it spreads; every other ring is offset by half a step
			int N = Cnt(24);
			float Base = (T / 32) % 2 != 0 ? 180f / N : 0f;
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Boss.Pos, 0.9f, Base + 360f * i / N, EBulletShape.Fire, new FLinearColor(1f, 0.55f + 0.2f * (i % 2), 0.25f));
				B.Accel = 0.02f;
				B.MaxSpeed = 2.4f;
			}
		}
		if (Every(T, 120, 60))
		{
			// two rings that curl in opposite directions
			foreach (float Sgn in new[] { -1f, 1f })
			{
				int N = Cnt(16);
				for (int i = 0; i < N; ++i)
				{
					FBullet B = Fire(Boss.Pos, 1.5f, 360f * i / N, EBulletShape.Fire, new FLinearColor(1f, 0.4f, 0.2f));
					B.AngVel = Sgn * 0.45f;
					B.AngVelDecay = 0.99f;
				}
			}
		}
	}

	public void S5_NonSpell2(int T)
	{
		if (Every(T, 32, 0)) Ring(Boss.Pos, 26, 1.8f, Rng.FRandRange(0f, 15f), EBulletShape.Medium, ElemCol[(T / 32) % 5]);
		if (Every(T, 32, 16)) Fan(Boss.Pos, 9, 64f, 2.5f, AimAngle(Boss.Pos), EBulletShape.Rice, ElemCol[(T / 32 + 2) % 5]);
		if (Every(T, 64, 40)) Ring(Boss.Pos, 14, 1.2f, AimAngle(Boss.Pos), EBulletShape.Large, ElemCol[(T / 64 + 4) % 5]);
		if (Every(T, 160, 80)) WanderBoss();
	}

	// 土符「山崩地裂」: walls of boulders fall with a gap to slip through, while rings spread from the boss
	public void S5_Earth(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 90f));
		// walls come faster and the rings get fuller with each difficulty; Hard and up also hurl aimed boulders
		int D = FMath.Clamp(Difficulty, 0, 3);
		ElementVolley(2, T, EarthWalls[D]);
		if (Every(T, 36, 0)) Ring(Boss.Pos, Cnt(10 + 2 * D), 1.6f, T * 1.7f, EBulletShape.Medium, ElemCol[2]);
		if (D >= 2 && Every(T, 64, 32)) Fan(Boss.Pos, D >= 3 ? 5 : 3, D >= 3 ? 48f : 30f, 2.1f, AimAngle(Boss.Pos), EBulletShape.Large, new FLinearColor(0.95f, 0.78f, 0.35f));
	}

	// 金符「金戈鐵刃」: blades stop and lunge, some beside the player and some anywhere; swords fall on both flanks
	public void S5_Metal(int T)
	{
		PatrolBoss(T);
		ElementVolley(3, T, 0.9f);
		if (Every(T, 50, 0))
		{
			// blades fan out, slow down, then dash: at the player's left, at the player's right, or somewhere random
			int N = Cnt(16);
			float Base = Rng.FRandRange(0f, 360f);
			for (int i = 0; i < N; ++i)
			{
				FBullet Bl = FireVec(Boss.Pos, Dir(Base + 360f * i / N) * 3f, EBulletShape.Crescent, new FLinearColor(0.92f, 0.95f, 1f));
				Bl.Behavior = BehPauseDash;
				Bl.Timer = 36;
				Bl.Param = i % 3 == 0 ? -64 : (i % 3 == 1 ? 64 : 9999);   // 9999 = random direction
				Bl.Param2 = 2.9f;
			}
		}
		if (Every(T, 18, 9))
		{
			// swords falling on both sides of the player, never on top of them
			foreach (float Sgn in new[] { -1f, 1f })
			{
				float X = FMath.Clamp(Player.Pos.X + Sgn * Rng.FRandRange(34f, 90f), 8f, FieldW - 8f);
				FBullet S = Fire(new FVector2f(X, -8f), 1.2f, 90f, EBulletShape.Crescent, new FLinearColor(1f, 0.9f, 0.6f));
				S.Accel = 0.04f;
				S.MaxSpeed = 3.6f;
			}
		}
	}

	public void S5_NonSpell3(int T)
	{
		ElementVolley((T / 240) % 5, T, 1f);
		if (Every(T, 40, 20)) Ring(Boss.Pos, 20, 2f, Rng.FRandRange(0f, 20f), EBulletShape.Small, ElemCol[(T / 240 + 1) % 5]);
		if (Every(T, 80, 0)) Fan(Boss.Pos, 5, 30f, 3f, AimAngle(Boss.Pos), EBulletShape.Large, ElemCol[(T / 240 + 3) % 5]);
		if (Every(T, 150, 75)) WanderBoss();
	}

	// 水符「流水無形」: two winding rivers and swaying rings of droplets
	public void S5_Water(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 100f));
		int D = FMath.Clamp(Difficulty, 0, 3);
		ElementVolley(4, T, WaterRivers[D]);
		if (Every(T, 70 - 5 * D, 35 - 2 * D))
		{
			int N = Cnt(18);
			float Base = Rng.FRandRange(0f, 360f);
			for (int i = 0; i < N; ++i)
			{
				FBullet W = Fire(Boss.Pos, 1.3f, Base + 360f * i / N, EBulletShape.Water, new FLinearColor(0.55f, 0.8f, 1f));
				W.WaveAmp = 0.6f;
				W.WaveFreq = 0.05f;
				W.WavePhase = i * 0.8f;
			}
		}
	}

	// 五行「相生相剋」: every 5 s the next element takes over while the previous one fades out
	public void S5_Cycle(int T)
	{
		PatrolBoss(T);
		float S = CycleStrength[FMath.Clamp(Difficulty, 0, 3)];
		// all five elements at once, each one firing slowly, so there is always something from every direction
		// but the gaps between the bullets stay wide enough to read
		for (int e = 0; e < 5; ++e) ElementVolley(e, T + e * 37, 0.42f * S);
		if (Every(T, 300, 0))
		{
			int Elem = (T / 300) % 5;
			Ring(Boss.Pos, 30, 1.5f, Rng.FRandRange(0f, 12f), EBulletShape.Large, ElemCol[Elem]);
			AddParticle(Boss.Pos, V2.ZeroVector, 260f, ElemCol[Elem], 30, true);
		}
	}
}
