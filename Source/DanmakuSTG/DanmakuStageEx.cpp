// Extra boss: 一索, the playful mahjong master. Ten spell cards, like a Touhou Extra stage, including the
// survival card 「九蓮寶燈」 and the bomb-proof wrap-around winds of 「大四喜」.

#include "DanmakuWorld.h"

using namespace Danmaku;

namespace
{
	const FLinearColor ColIvory(1.f, 0.96f, 0.85f);
	const FLinearColor ColHaku(0.94f, 0.96f, 1.f);    // white dragon
	const FLinearColor ColHatsu(0.35f, 0.92f, 0.47f); // green dragon
	const FLinearColor ColChun(1.f, 0.35f, 0.35f);    // red dragon
	const FLinearColor ColWinds[4] = { FLinearColor(0.5f, 0.78f, 1.f), FLinearColor(1.f, 0.72f, 0.47f), FLinearColor(0.67f, 1.f, 0.7f), FLinearColor(1.f, 0.57f, 0.84f) };
	const FLinearColor ColLotus[9] = {
		FLinearColor(1.f, 0.84f, 0.84f), FLinearColor(1.f, 0.88f, 0.75f), FLinearColor(1.f, 0.94f, 0.7f),
		FLinearColor(0.88f, 1.f, 0.75f), FLinearColor(0.75f, 1.f, 0.78f), FLinearColor(0.75f, 0.96f, 1.f),
		FLinearColor(0.82f, 0.9f, 1.f),  FLinearColor(0.9f, 0.82f, 1.f),  FLinearColor(1.f, 0.78f, 0.96f) };
	const FLinearColor ColKokushi[13] = {
		FLinearColor(1.f, 0.4f, 0.4f), FLinearColor(1.f, 0.6f, 0.3f), FLinearColor(1.f, 0.85f, 0.3f), FLinearColor(0.7f, 1.f, 0.3f),
		FLinearColor(0.3f, 1.f, 0.5f), FLinearColor(0.3f, 1.f, 0.85f), FLinearColor(0.3f, 0.8f, 1.f), FLinearColor(0.4f, 0.5f, 1.f),
		FLinearColor(0.65f, 0.4f, 1.f), FLinearColor(0.9f, 0.4f, 1.f), FLinearColor(1.f, 0.4f, 0.8f), FLinearColor(0.95f, 0.95f, 1.f),
		FLinearColor(1.f, 0.82f, 0.5f) };
}

void ADanmakuWorld::EX_NonSpell1(int32 T)
{
	if (Every(T, 5, 0))
	{
		for (int32 i = 0; i < 4; ++i) Fire(Boss.Pos, 2.2f, T * 4.1f + 90.f * i, EBulletShape::Tile, ColIvory);
	}
	if (Every(T, 7, 3))
	{
		for (int32 i = 0; i < 4; ++i) Fire(Boss.Pos, 1.6f, -T * 2.9f + 90.f * i + 45.f, EBulletShape::Small, ColHatsu);
	}
	if (Every(T, 40, 20)) Fan(Boss.Pos, 9, 56.f, 3.f, AimAngle(Boss.Pos), EBulletShape::Rice, ColChun);
	if (Every(T, 180, 90)) WanderBoss();
}

// 立直「立直一發」: red lines mark the shots, then fast columns of tiles fly down them; the discards rain from above
void ADanmakuWorld::EX_Riichi(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 44, 0))
	{
		const float Aim = AimAngle(Boss.Pos);
		const int32 Lines = Difficulty >= 2 ? 5 : 3;
		for (int32 l = 0; l < Lines; ++l)
		{
			const float Off = (l - (Lines - 1) * 0.5f) * 21.f;
			AddBeam(Boss.Pos, Aim + Off, 2.f, 44, 1, 1, FLinearColor(1.f, 0.3f, 0.3f));
			for (int32 i = 0; i < 12; ++i)
			{
				FBullet& B = FireVec(Boss.Pos, FVector2f::ZeroVector, EBulletShape::Tile, ColIvory);
				B.Behavior = BehDelayedRelease;
				B.Param = 1;
				B.Timer = 44 + i * 3;
				B.Center = Dir(Aim + Off) * 4.6f;
			}
		}
		// 一發: a slow ring of red tiles goes out with every declaration
		const int32 N = Cnt(18);
		for (int32 i = 0; i < N; ++i) Fire(Boss.Pos, 1.1f, Aim + 180.f / N + 360.f * i / N, EBulletShape::Tile, ColChun);
	}
	if (Every(T, 5, 2))
	{
		// discards tumbling down from the top
		FBullet& D = FireVec(FVector2f(Rng.FRandRange(8.f, FieldW - 8.f), -8.f), FVector2f(Rng.FRandRange(-0.4f, 0.4f), 1.2f), EBulletShape::Tile, ColHaku);
		D.Gravity = FVector2f(0.f, 0.012f);
		D.Spin = Rng.FRandRange(-6.f, 6.f);
	}
}

// 平和「平和斷么」: calm, perfectly symmetric alternating rings
void ADanmakuWorld::EX_Pinfu(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 105.f));
	if (Every(T, 22, 0))
	{
		const int32 W = T / FMath::Max(1, FMath::RoundToInt(22 * GetDifficultyInfo(Difficulty).IntervalScale));
		Ring(Boss.Pos, 28, 1.5f, (W % 2) * (180.f / 28.f), EBulletShape::Tile, W % 2 ? ColHatsu : ColHaku);
	}
	if (Every(T, 66, 11)) Ring(Boss.Pos, 12, 2.4f, AimAngle(Boss.Pos), EBulletShape::Medium, ColChun);
}

void ADanmakuWorld::EX_NonSpell2(int32 T)
{
	if (Every(T, 34, 0)) Ring(Boss.Pos, 36, 1.8f, Rng.FRandRange(0.f, 10.f), EBulletShape::Tile, ColHaku);
	if (Every(T, 11, 0)) Fan(Boss.Pos, 3, 14.f, 3.2f, AimAngle(Boss.Pos), EBulletShape::Rice, ColHatsu);
	if (Every(T, 68, 34)) Ring(Boss.Pos, 18, 1.2f, 0.f, EBulletShape::Large, ColChun);
	if (Every(T, 160, 80)) WanderBoss();
}

// 一色「清一色」: three-armed spirals of one colour; each new spiral starts before the last one ends, turning the other way
void ADanmakuWorld::EX_Chinitsu(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 110.f));
	constexpr int32 Start = 170, Len = 300;
	for (int32 W = FMath::Max(0, (T - Len) / Start); W * Start <= T; ++W)
	{
		const int32 L = T - W * Start;
		if (L < 0 || L >= Len) continue;
		const float Dirn = W % 2 == 0 ? 1.f : -1.f;
		const FLinearColor C = W % 2 == 0 ? ColHatsu : FLinearColor(0.6f, 1.f, 0.65f);
		if (Every(L, 3, 0))
		{
			for (int32 i = 0; i < 3; ++i) Fire(Boss.Pos, 1.9f, Dirn * L * 2.4f + 120.f * i + W * 37.f, EBulletShape::Tile, C);
		}
	}
	if (Every(T, 60, 30)) Ring(Boss.Pos, 18, 1.2f, 0.f, EBulletShape::Large, FLinearColor(0.3f, 0.75f, 0.4f));
}

// 對子「七對子」: seven pairs of tiles that fly together, then split apart
void ADanmakuWorld::EX_Chiitoi(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 40, 0))
	{
		const float Aim = AimAngle(Boss.Pos);
		for (int32 p = 0; p < 7; ++p)
		{
			const float A = Aim + (p - 3) * 16.f;
			for (float Sgn : { -1.f, 1.f })
			{
				FBullet& B = Fire(Boss.Pos, 2.3f, A, EBulletShape::Tile, p % 2 ? ColChun : ColIvory);
				B.Behavior = BehCurveAfter;
				B.Timer = 30;
				B.Param2 = Sgn * 0.6f;
			}
		}
	}
	if (Every(T, 20, 10)) Fire(Boss.Pos, 1.6f, Rng.FRandRange(0.f, 360.f), EBulletShape::Medium, ColHaku);
}

void ADanmakuWorld::EX_NonSpell3(int32 T)
{
	if (Every(T, 4, 0))
	{
		Fire(Boss.Pos, 2.f, T * 5.3f, EBulletShape::Tile, ColChun);
		Fire(Boss.Pos, 2.f, -T * 5.3f, EBulletShape::Tile, ColHatsu);
		Fire(Boss.Pos, 1.6f, T * 5.3f + 180.f, EBulletShape::Tile, ColHaku);
	}
	if (Every(T, 50, 25)) Ring(Boss.Pos, 24, 2.5f, AimAngle(Boss.Pos), EBulletShape::Small, ColHaku);
	if (Every(T, 150, 75)) WanderBoss();
}

// 開花「嶺上開花」: tiles are tossed into the air and blossom into rings
void ADanmakuWorld::EX_Rinshan(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 120.f));
	if (Every(T, 22, 0))
	{
		const int32 N = Cnt(5);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = FireVec(Boss.Pos, Dir(Rng.FRandRange(-150.f, -30.f)) * Rng.FRandRange(2.6f, 3.4f), EBulletShape::Tile, ColIvory);
			B.Gravity = FVector2f(0.f, 0.055f);
			B.Behavior = BehSplitRing;
			B.Timer = Rng.RandRange(60, 80);
			B.Param = 10;
			B.Param2 = 1.35f;
			B.Color = ColLotus[Rng.RandRange(0, 8)];
		}
	}
}

// 役滿一「大三元」: white, green and red dragons — three-armed, six-layered turning sprays plus slow rings
void ADanmakuWorld::EX_Daisangen(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 110.f));
	const FLinearColor Cols[3] = { ColHaku, ColHatsu, ColChun };
	if (Every(T, 15, 0))
	{
		const int32 Volley = T / FMath::Max(1, FMath::RoundToInt(15 * GetDifficultyInfo(Difficulty).IntervalScale));
		const float Sgn = Volley % 2 == 0 ? 1.f : -1.f;
		const float Base = T * 0.8f * Sgn;
		const int32 NumLayers = FMath::Max(2, Cnt(6));
		for (int32 L = 0; L < NumLayers; ++L)
		{
			for (int32 Arm = 0; Arm < 3; ++Arm)
			{
				const float A0 = Base + L * 30.f + Arm * 120.f;
				const FVector2f O = Boss.Pos + Dir(A0) * (10.f + L * 5.f);
				for (int32 j = -1; j <= 1; ++j)
				{
					FBullet& B = Fire(O, 1.55f + FMath::Abs(j) * 0.05f + L * 0.09f, A0 + j * 11.5f + Sgn * (4.6f + L * 0.86f), EBulletShape::Medium, Cols[Arm]);
					B.AngVel = Sgn * (0.069f + L * 0.0103f);
					B.AngVelDecay = 0.9988f;
					B.Accel = 0.002f;
					B.MaxSpeed = 3.f;
				}
			}
		}
	}
	if (Every(T, 45, 0))
	{
		const int32 N = Cnt(60);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Boss.Pos, 1.25f, T * 0.46f + 360.f * i / N, EBulletShape::Small, Cols[i % 3]);
			B.AngVel = -0.04f;
			B.AngVelDecay = 0.999f;
			B.Accel = 0.0012f;
			B.MaxSpeed = 1.7f;
		}
	}
}

// 役滿二「大四喜」: four rings of wind that wrap around the edges of the screen. Bombs cannot erase them.
void ADanmakuWorld::EX_Daisuushi(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 120.f));
	const int32 K = T % 1500;
	if (K == 1 || (!bComposite && (K == 25 || K == 49)))
	{
		const int32 Burst = K / 24;
		static const float Speeds[4] = { 1.15f, 1.25f, 1.33f, 1.42f };
		static const float Offsets[4] = { 0.f, 7.4f, 16.6f, 24.6f };
		for (int32 Rg = 0; Rg < 4; ++Rg)
		{
			const int32 N = Cnt(16);
			for (int32 i = 0; i < N; ++i)
			{
				FBullet& B = Fire(Boss.Pos, Speeds[Rg], Offsets[Rg] + Burst * 4.f + 360.f * i / N, EBulletShape::Medium, ColWinds[Rg]);
				B.bWrap = true;
				B.bBombProof = true;
				B.Life = 1440;
			}
		}
		Sfx(ESfx::Spell);
	}
}

// 役滿三「九蓮寶燈」 (survival 45 s): nine numbered lotuses appear; 1 and 9 burst into rings, 2-8 into homing petals
void ADanmakuWorld::EX_Chuuren(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 100.f));
	if (T == 1) Boss.PatternTimer = 120;
	if (T < Boss.PatternTimer) return;
	Boss.PatternTimer = T + FMath::RoundToInt(FMath::Lerp(300.f, 60.f, FMath::Clamp(T / 1800.f, 0.f, 1.f)) * GetDifficultyInfo(Difficulty).IntervalScale * (bComposite ? CompInterval * 1.5f : 1.f));

	TArray<FVector2f> Used;
	for (int32 Num = 1; Num <= 9; ++Num)
	{
		FVector2f P(FMath::Clamp(Boss.Pos.X + Rng.FRandRange(-96.f, 96.f), 27.f, FieldW - 27.f), FMath::Clamp(Boss.Pos.Y + Rng.FRandRange(32.f, 140.f), 70.f, FieldH - 32.f));
		for (int32 Try = 0; Try < 100; ++Try)
		{
			const FVector2f C = Boss.Pos + Dir(Rng.FRandRange(0.f, 360.f)) * Rng.FRandRange(53.f, 160.f);
			if (C.X < 27.f || C.X > FieldW - 27.f || C.Y < 70.f || C.Y > FieldH - 32.f) continue;
			if (FVector2f::Distance(C, Player.Pos) < 80.f) continue;
			bool bOk = true;
			for (const FVector2f& U : Used) if (FVector2f::Distance(C, U) < 33.f) { bOk = false; break; }
			if (bOk) { P = C; break; }
		}
		Used.Add(P);
		FBullet& L = Fire(P, 0.f, -90.f, EBulletShape::Sakura, ColLotus[Num - 1]);
		L.Behavior = BehLotusSummon;
		L.Param = Num;
		L.Timer = 60;
		L.bHarmless = true;
		L.Scale = 1.6f;
		L.Spin = 2.f;
	}
}

// 役滿奧義「國士無雙」: all thirteen assassins burst out at once in a ring (bombs and the death clear can't remove them);
// each one flies to a wall, then stalks the player, and does it again every time it reaches a wall
void ADanmakuWorld::EX_Kokushi(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 100.f));
	if (T == 45)
	{
		const float Base = Rng.FRandRange(0.f, 360.f / 13.f);
		for (int32 Index = 0; Index < 13; ++Index)
		{
			FBullet& A = FireVec(Boss.Pos, Dir(Base + Index * 360.f / 13.f) * 2.6f, EBulletShape::Large, ColKokushi[Index]);
			A.Behavior = BehAssassin;
			A.Param = 0;
			A.bBombProof = true;
			A.Scale = 1.3f;
			A.Radius *= 1.1f;
		}
		AddParticle(Boss.Pos, FVector2f::ZeroVector, 260.f, FLinearColor(1.f, 0.9f, 0.6f), 30, true);
		Sfx(ESfx::Spell);
	}
}

// 終局「天和」: the final hand. Every earlier hand comes back, one at a time: each time the boss loses another ninth of its
// health, the next hand joins on top of the ones already in play (each thinner, but all of them together).
void ADanmakuWorld::EX_Tenhou(int32 T)
{
	PatrolBoss(T);
	static const FPatternFn Hands[9] = { &ADanmakuWorld::EX_Riichi, &ADanmakuWorld::EX_Pinfu, &ADanmakuWorld::EX_Chinitsu, &ADanmakuWorld::EX_Chiitoi,
		&ADanmakuWorld::EX_Rinshan, &ADanmakuWorld::EX_Daisangen, &ADanmakuWorld::EX_Daisuushi, &ADanmakuWorld::EX_Chuuren, &ADanmakuWorld::EX_Kokushi };
	static const TCHAR* HandNames[9] = { TEXT("立直一發"), TEXT("平和斷么"), TEXT("清一色"), TEXT("七對子"), TEXT("嶺上開花"), TEXT("大三元"), TEXT("大四喜"), TEXT("九蓮寶燈"), TEXT("國士無雙") };

	const float Lost = 1.f - FMath::Clamp(Boss.HP / FMath::Max(1.f, Boss.MaxHP), 0.f, 1.f);
	const int32 Want = FMath::Clamp(1 + FMath::FloorToInt(Lost * 9.f), 1, 9);
	while (Boss.Stage6Beasts < Want)
	{
		const int32 H = Boss.Stage6Beasts++;
		Boss.JoinFrame[H] = T;
		if (H > 0)
		{
			ShowMessage(FString(TEXT("「")) + Tr(HandNames[H]) + TEXT("」"), FString(), 90);
			AddParticle(Boss.Pos, FVector2f::ZeroVector, 420.f, FLinearColor(1.f, 0.85f, 0.45f), 40, true);
			Sfx(ESfx::Spell);
		}
	}

	// the dealer's own tiles keep turning underneath everything
	const FLinearColor Cols[3] = { ColHaku, ColHatsu, ColChun };
	if (Every(T, 40, 0))
	{
		const float Base = T * 1.3f;
		for (int32 Arm = 0; Arm < 3; ++Arm)
		{
			FBullet& B = Fire(Boss.Pos, 1.7f, Base + Arm * 120.f, EBulletShape::Tile, Cols[Arm]);
			B.AngVel = 0.12f;
			B.AngVelDecay = 0.997f;
		}
	}

	const int32 N = Boss.Stage6Beasts;
	bComposite = true;
	// each extra hand thins all of them out a lot more than before, so the full table stays readable
	CompInterval = 1.f + 0.45f * (N - 1);
	CompCount = 1.f - 0.05f * (N - 1);
	for (int32 H = 0; H < N; ++H)
	{
		const int32 Local = T - Boss.JoinFrame[H] + 1;
		(this->*Hands[H])(Local);
	}
	bComposite = false;
	CompInterval = 1.f;
	CompCount = 1.f;
}

// =====================================================================================
// Extra midboss: 玄 returns to warn the player (three spell cards)
// =====================================================================================

namespace
{
	const FLinearColor MBElem[5] = { FLinearColor(0.4f, 1.f, 0.45f), FLinearColor(1.f, 0.45f, 0.25f), FLinearColor(0.95f, 0.8f, 0.35f),
		FLinearColor(0.92f, 0.95f, 1.f), FLinearColor(0.35f, 0.6f, 1.f) };
}

// 逆符「五行逆轉」: rings in the five colours that spin one way, then all reverse together; a second layer turns the other way
void ADanmakuWorld::MB_Reverse(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 105.f));
	if (Every(T, 20, 0))
	{
		const int32 W = T / 20;
		const float Sgn = W % 2 ? -1.f : 1.f;
		const int32 N = Cnt(20);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Boss.Pos, 1.8f, W * 9.f + 360.f * i / N, EBulletShape::Medium, MBElem[(W + i) % 5]);
			B.AngVel = 0.55f * Sgn;
			B.AngVelDecay = 1.f;
			B.Behavior = BehCurveAfter;
			B.Timer = 60;
			B.Param2 = -0.55f * Sgn;
		}
	}
	if (Every(T, 48, 24)) Fan(Boss.Pos, 7, 36.f, 3.f, AimAngle(Boss.Pos), EBulletShape::Rice, MBElem[(T / 48) % 5]);
}

// 數符「河圖洛書」: the 3x3 magic square — seeds bloom in the order of the numbers 1 to 9, and the centre five answers with an aimed ring
void ADanmakuWorld::MB_LoShu(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 80.f));
	static const int32 Square[9] = { 4, 9, 2, 3, 5, 7, 8, 1, 6 };
	const int32 Cyc = FMath::Max(1, FMath::RoundToInt(120 * GetDifficultyInfo(Difficulty).IntervalScale));
	if (T % Cyc == 1)
	{
		const FVector2f C(FieldW * 0.5f + Rng.FRandRange(-40.f, 40.f), 200.f + Rng.FRandRange(-20.f, 20.f));
		for (int32 i = 0; i < 9; ++i)
		{
			const FVector2f P = C + FVector2f(((i % 3) - 1) * 96.f, ((i / 3) - 1) * 76.f);
			SpawnPlumSeed(P, 16 + Square[i] * 9, MBElem[Square[i] % 5], 6 + Square[i], 1.4f);
		}
	}
	if (T % Cyc == 1 + 16 + 5 * 9)
	{
		Ring(Boss.Pos, 30, 2.2f, AimAngle(Boss.Pos), EBulletShape::Small, MBElem[0]);
	}
	if (Every(T, 7, 3))
	{
		FBullet& D = Fire(Boss.Pos, 1.2f, 90.f + FMath::Sin(T * 0.05f) * 70.f, EBulletShape::Medium, MBElem[(T / 12) % 5]);
		D.Accel = 0.01f;
		D.MaxSpeed = 2.2f;
	}
}

// 陣符「八門金鎖」: eight beams lock the field around the boss and burst into shrapnel where they hit the walls;
// one gate is always left open
void ADanmakuWorld::MB_EightGates(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 150.f));
	const int32 Cyc = FMath::Max(1, FMath::RoundToInt(100 * GetDifficultyInfo(Difficulty).IntervalScale));
	if (T % Cyc == 1)
	{
		const int32 Open = Rng.RandRange(0, 7);
		const float Base = Rng.FRandRange(0.f, 45.f);
		for (int32 g = 0; g < 8; ++g)
		{
			if (g == Open) continue;
			AddBeam(Boss.Pos, Base + g * 45.f, 7.f, 64, 10, 36, FLinearColor(1.f, 0.85f, 0.45f), 3);
		}
	}
	if (Every(T, 20, 10)) Ring(Boss.Pos, 20, 1.6f, Rng.FRandRange(0.f, 30.f), EBulletShape::Small, MBElem[(T / 20) % 5]);
}
