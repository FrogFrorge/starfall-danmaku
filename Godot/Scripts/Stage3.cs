// Stage 3 boss: 嵐, the hot-headed weather master.
// 晴 rotating triple rings, 雨 water flung up that rains back down, 雷 telegraphed lightning that sets the ground on fire,
// 霧 a fog that keeps closing in while plain triple rings fly through it.
// Port of DanmakuStage3.cpp.
using static DanmakuSTG.Stage3Locals;

namespace DanmakuSTG;

static class Stage3Locals
{
	public static readonly FLinearColor ColSky1 = new FLinearColor(0.6f, 0.86f, 1f);
	public static readonly FLinearColor ColSky2 = new FLinearColor(0.7f, 1f, 0.86f);
	public static readonly FLinearColor ColSky3 = new FLinearColor(1f, 0.92f, 0.6f);
	public static readonly FLinearColor ColRain = new FLinearColor(0.6f, 0.82f, 1f);
	public static readonly FLinearColor ColBolt = new FLinearColor(1f, 0.95f, 0.6f);
	public static readonly FLinearColor ColFog1 = new FLinearColor(0.73f, 0.8f, 1f);
	public static readonly FLinearColor ColFog2 = new FLinearColor(0.86f, 0.92f, 1f);
	public static readonly FLinearColor ColFog3 = new FLinearColor(1f, 1f, 1f);

	public static readonly int[] SunnyCounts = { 18, 24, 30 };
	public static readonly float[] SunnyStart = { 7f, 9f, 11f };
	public static readonly float[] SunnyRad = { 0.95f, 1.05f, 1.15f };
}

public partial class DanmakuWorld
{
	public void S3_NonSpell1(int T)
	{
		if (Every(T, 7, 0))
		{
			int N = Cnt(6);
			for (int i = 0; i < N; ++i) Fire(Boss.Pos, 2.1f, T * 3.7f + 360f * i / N, EBulletShape.Water, ColRain);
		}
		if (Every(T, 6, 3))
		{
			FBullet D = FireVec(new FVector2f(Rng.FRandRange(6f, FieldW - 6f), -6f), new FVector2f(0f, Rng.FRandRange(1.8f, 2.4f)), EBulletShape.Water, ColRain);
			D.Gravity = new FVector2f(0f, 0.008f);
		}
		if (Every(T, 45, 22)) Fan(Boss.Pos, 7, 40f, 3f, AimAngle(Boss.Pos), EBulletShape.Medium, ColSky3);
		if (Every(T, 180, 90)) WanderBoss();
	}

	// 晴符「晴空萬里」: every half second three rings spiral outward together, alternating direction
	public void S3_Sunny(int T)
	{
		PlaceBoss(new FVector2f(FieldW * 0.5f, 110f + FMath.Sin(T * 0.02f) * 8f));
		if (!Every(T, 30, 0)) return;
		FLinearColor[] Cols = { ColSky1, ColSky2, ColSky3 };
		int Volley = T / FMath.Max(1, FMath.RoundToInt(30 * GetDifficultyInfo(Difficulty).IntervalScale));
		float AngSpeed = (Volley % 2 == 0 ? 1f : -1f) * 0.143f;
		float Offset = Rng.FRandRange(0f, 360f);
		for (int r = 0; r < 3; ++r)
		{
			int N = Cnt(SunnyCounts[r]);
			for (int i = 0; i < N; ++i)
			{
				FirePolar(Boss.Pos, Offset + r * 4.6f + 360f * i / N, SunnyStart[r], SunnyRad[r], AngSpeed, r == 2 ? EBulletShape.Small : EBulletShape.Medium, Cols[r]);
			}
		}
	}

	public void S3_NonSpell2(int T)
	{
		if (Every(T, 3, 0))
		{
			FBullet D = FireVec(new FVector2f(Rng.FRandRange(6f, FieldW - 6f), -6f), new FVector2f(Rng.FRandRange(-0.3f, 0.1f), Rng.FRandRange(1.6f, 2.3f)), EBulletShape.Water, ColRain);
			D.Gravity = new FVector2f(0f, 0.01f);
		}
		if (Every(T, 54, 0)) Ring(Boss.Pos, 24, 1.8f, Rng.FRandRange(0f, 30f), EBulletShape.Medium, ColSky1);
		if (Every(T, 54, 27)) Fan(Boss.Pos, 9, 48f, 2.6f, AimAngle(Boss.Pos), EBulletShape.Rice, ColSky3);
		if (Every(T, 160, 80)) WanderBoss();
	}

	// 雨符「雨幕延綿」: the boss flings water upward, it arcs and rains back down
	public void S3_Rain(int T)
	{
		PatrolBoss(T);
		if (!Every(T, 6, 0)) return;
		int N = Cnt(Rng.RandRange(5, 11));
		for (int i = 0; i < N; ++i)
		{
			FBullet D = FireVec(Boss.Pos + new FVector2f(Rng.FRandRange(-6f, 6f), 6f), Dir(Rng.FRandRange(238f, 302f)) * Rng.FRandRange(3.3f, 4.6f),
				EBulletShape.Water, ColRain);
			D.Gravity = new FVector2f(0f, Rng.FRandRange(0.05f, 0.072f));
		}
	}

	// 雷符「雷暴中心」: a bolt is marked under the player every second and strikes two seconds later; fire bursts from the ground
	public void S3_Storm(int T)
	{
		PatrolBoss(T);
		if (Every(T, 60, 0))
		{
			float X = FMath.Clamp(Player.Pos.X + Rng.FRandRange(-10f, 10f), 20f, FieldW - 20f);
			AddBeam(new FVector2f(X, FieldH), -90f, Rng.FRandRange(16f, 22f), 120, 4, 20, ColBolt, 1);
		}
	}

	// 霧符「雲裡霧裡」: vision shrinks for most of the card, while triple rings keep coming
	public void S3_Fog(int T)
	{
		PatrolBoss(T);
		float A = FMath.Clamp(T / (48f * 60f), 0f, 1f);
		FogRadius = FMath.Lerp(230f, 58f, A);
		if (Every(T, 15, 0))
		{
			FLinearColor[] Cols = { ColFog1, ColFog2, ColFog3 };
			TripleRings(Boss.Pos, Rng.FRandRange(0f, 360f), 0.55f, EBulletShape.Small, Cols);
		}
	}
}
