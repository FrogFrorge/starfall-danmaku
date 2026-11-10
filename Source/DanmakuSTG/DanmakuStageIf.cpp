// Extra-IF: "what if the hero was too late". The Four Gods Barrier broke on the Night of the Heaven's Gate, and the four
// perils it held down — 饕餮 (gluttony), 檮杌 (the untamable), 混沌 (faceless chaos) and 窮奇 (the winged tiger who
// punishes the honest) — are loose. One of them blocks the road (which one depends on the player character), then all
// four fight in relay: two cards each, the last one flies off as the next swoops in, and the endurance card and last
// card have all four on the field at once.

#include "DanmakuWorld.h"

using namespace Danmaku;

namespace
{
	const FLinearColor Bronze(0.85f, 0.7f, 0.35f);
	const FLinearColor Jade(0.45f, 0.9f, 0.55f);
	const FLinearColor Crimson(1.f, 0.3f, 0.25f);
	const FLinearColor Iron(0.78f, 0.8f, 0.86f);
	const FLinearColor TigerGold(1.f, 0.7f, 0.3f);
	const FLinearColor Pearl(0.94f, 0.95f, 1.f);
	const FLinearColor Ink(0.55f, 0.4f, 0.85f);
	const FLinearColor Indigo(0.45f, 0.5f, 1.f);
	const FLinearColor Silver(0.85f, 0.9f, 1.f);

	FLinearColor Rainbow(float H) { return FLinearColor::MakeFromHSV8(uint8(FMath::Fmod(H, 255.f)), 120, 255); }
}

// =====================================================================================
// Shared pieces
// =====================================================================================

int32 ADanmakuWorld::SwallowBullets(const FVector2f& At, float PullRadius, float EatRadius, float Pull, TArray<FVector2f>* Eaten)
{
	// 饕餮's mouth: bullets nearby bend toward it, and the ones that reach it are eaten (and counted)
	const float PR2 = PullRadius * PullRadius, ER2 = EatRadius * EatRadius;
	int32 Count = 0;
	for (FBullet& B : Bullets)
	{
		if (B.bDead || B.bHarmless || B.bBombProof || B.bSpat) continue;
		// bullets the beast just spat out of this very mouth aren't swallowed again right away
		if (B.Age < 40) continue;
		const FVector2f D = At - B.Pos;
		const float D2 = D.SizeSquared();
		if (D2 > PR2) continue;
		if (D2 < ER2)
		{
			B.bDead = true;
			++Boss.MawEaten;
			++Count;
			if (Eaten) Eaten->Add(B.Pos);
			continue;
		}
		if (B.Motion == 2) B.Vel += D.GetSafeNormal() * Pull * 2.f;
		else if (B.Motion == 0) B.Angle += FMath::Clamp(FMath::FindDeltaAngleDegrees(B.Angle, AngleOf(D)), -1.f, 1.f) * Pull * 40.f;
	}
	return Count;
}

void ADanmakuWorld::SpitBullet(const FVector2f& At, float Speed, float Angle, EBulletShape Shape, const FLinearColor& Color)
{
	FBullet& B = Fire(At, Speed, Angle, Shape, Color);
	B.bSpat = true;
}

void ADanmakuWorld::PlaceQuad(int32 T, float Spread)
{
	// the four beasts circle the top of the field in a slowly turning diamond
	const FVector2f C(FieldW * 0.5f, 140.f);
	Boss.Pos = C;
	Boss.MoveTime = Boss.MoveDur;
	for (int32 i = 0; i < 4; ++i)
	{
		const float A = T * 0.35f + i * 90.f + 45.f;
		const FVector2f Target = C + FVector2f(FMath::Cos(FMath::DegreesToRadians(A)) * Spread, FMath::Sin(FMath::DegreesToRadians(A)) * Spread * 0.45f);
		Boss.QuadPos[i] = FMath::Lerp(Boss.QuadPos[i], Target, 0.04f);
	}
}

// =====================================================================================
// 饕餮 (gluttony) — midboss cards
// =====================================================================================

// 饕符「貪食之口」: a mouth drifts around the field swallowing the boss's own rings, then spits everything it ate back out
void ADanmakuWorld::IF_TaotieMaw(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 90.f));
	Boss.MawPos = FVector2f(FieldW * 0.5f + FMath::Sin(T * 0.017f) * 120.f, 210.f + FMath::Sin(T * 0.029f) * 50.f);
	if (Every(T, 12, 0)) Ring(Boss.Pos, 24, 1.7f, T * 1.3f, EBulletShape::Medium, Bronze);
	// every bullet the mouth eats comes straight back out as three green ones flying anywhere; the green ones are never eaten
	TArray<FVector2f> Eaten;
	SwallowBullets(Boss.MawPos, 95.f, 12.f, 0.05f, &Eaten);
	for (const FVector2f& E : Eaten)
	{
		for (int32 k = 0; k < 3; ++k) SpitBullet(Boss.MawPos, Rng.FRandRange(1.5f, 2.f), Rng.FRandRange(0.f, 360.f), EBulletShape::Rice, Jade);
	}
	if (Eaten.Num() > 0) AddParticle(Boss.MawPos, FVector2f::ZeroVector, 50.f, Jade, 10, true);
	Boss.MawEaten = 0;
	if (T % 6 == 0) AddParticle(Boss.MawPos, FVector2f::ZeroVector, 70.f, FLinearColor(0.35f, 0.25f, 0.1f), 14, true);
}

// 饕符「鐘鼎饌宴」: bronze cauldrons tumble from the sky and burst into a feast; food sprays from the boss in an arc
void ADanmakuWorld::IF_TaotieFeast(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 36, 0))
	{
		for (int32 k = 0; k < 4; ++k)
		{
			FBullet& D = FireVec(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -10.f), FVector2f(0.f, 0.6f), EBulletShape::Large, Bronze);
			D.Gravity = FVector2f(0.f, 0.03f);
			D.Behavior = BehSplitRing;
			D.Timer = Rng.RandRange(55, 85);
			D.Param = 14;
			D.Param2 = 1.4f;
		}
	}
	if (Every(T, 5, 0))
	{
		FBullet& F = FireVec(Boss.Pos, Dir(Rng.FRandRange(-150.f, -30.f)) * Rng.FRandRange(2.f, 2.8f), EBulletShape::Rice, Jade);
		F.Gravity = FVector2f(0.f, 0.04f);
	}
}

// 饕符「饕餮紋」: the bronze mask motif — mirror-image curls, and the two eyes stare out as rings
void ADanmakuWorld::IF_TaotieMotif(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 105.f));
	if (Every(T, 4, 0))
	{
		for (float S : { -1.f, 1.f })
		{
			FBullet& B = Fire(Boss.Pos + FVector2f(S * 18.f, 0.f), 1.9f, 90.f + S * (55.f + FMath::Sin(T * 0.05f) * 45.f), EBulletShape::Rice, Bronze);
			B.AngVel = -S * 0.9f;
			B.AngVelDecay = 0.984f;
		}
	}
	if (Every(T, 44, 22))
	{
		for (float S : { -1.f, 1.f }) Ring(Boss.Pos + FVector2f(S * 42.f, -8.f), 18, 2.2f, AimAngle(Boss.Pos), EBulletShape::Medium, Jade);
	}
}

// =====================================================================================
// 檮杌 (the untamable) — midboss cards
// =====================================================================================

// 杌符「頑石不化」: boulders that never stop — they ricochet off every wall for a long time
void ADanmakuWorld::IF_TaowuStone(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 95.f));
	if (Every(T, 40, 0))
	{
		for (int32 k = 0; k < 3; ++k)
		{
			FBullet& B = Fire(Boss.Pos, 1.8f, Rng.FRandRange(30.f, 150.f), EBulletShape::Large, Crimson);
			B.WallMask = 1 | 2 | 4;
			B.Bounces = 8;
			B.Life = 620;
		}
	}
}

// 杌符「難訓之獸」: every shot first runs AWAY from the player, stops, then turns and charges
void ADanmakuWorld::IF_TaowuUntamed(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 44, 0))
	{
		// the main spread runs away from you; smaller spreads go toward you and 90° to either side
		const float Away = AimAngle(Boss.Pos) + 180.f;
		const float Dirs[4] = { 0.f, 180.f, 90.f, -90.f };
		for (int32 d = 0; d < 4; ++d)
		{
			const int32 N = Cnt(d == 0 ? 22 : 7);
			for (int32 i = 0; i < N; ++i)
			{
				FBullet& B = FireVec(Boss.Pos, Dir(Away + Dirs[d] + (i - N * 0.5f) * 12.f) * 2.6f, EBulletShape::Crescent, i % 2 ? Crimson : Iron);
				B.Behavior = BehPauseDash;
				B.Timer = 40;
				B.Param = (i % 3 - 1) * 40;
				B.Param2 = 2.7f;
			}
		}
	}
}

// 杌符「丈八之尾」: two long striped tails sweep all the way round the boss like the arms of a pinwheel
void ADanmakuWorld::IF_TaowuTail(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 110.f));
	if (Every(T, 3, 0) && (T / 3) % 5 < 3)
	{
		for (int32 k = 0; k < 2; ++k)
		{
			const float A = T * 1.3f + k * 180.f;
			FirePolar(Boss.Pos, A, 10.f, 2.0f, 0.12f, EBulletShape::Medium, (T / 6) % 2 ? TigerGold : FLinearColor(0.25f, 0.2f, 0.2f));
		}
	}
	if (Every(T, 48, 24)) Ring(Boss.Pos, 28, 1.5f, Rng.FRandRange(0.f, 20.f), EBulletShape::Small, Crimson);
}

// =====================================================================================
// 混沌 (faceless chaos) — midboss cards
// =====================================================================================

// 沌符「七竅未開」: seven holes open one by one in a face that isn't there — eyes, ears, nostrils, mouth — then all at once
void ADanmakuWorld::IF_HundunSeven(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 80.f));
	static const FVector2f Holes[7] = { { -50.f, -40.f }, { 50.f, -40.f }, { -105.f, 0.f }, { 105.f, 0.f }, { -16.f, 14.f }, { 16.f, 14.f }, { 0.f, 62.f } };
	const FVector2f Face(FieldW * 0.5f, 190.f);
	const int32 Cyc = FMath::Max(1, FMath::RoundToInt(420 * GetDifficultyInfo(Difficulty).IntervalScale));
	const int32 Ph = T % Cyc;
	const int32 Step = Cyc / 9;
	// the faceless head keeps muttering: slow sparks from the boss all the time
	if (Every(T, 9, 0)) Fire(Boss.Pos, 1.4f, Rng.FRandRange(20.f, 160.f), EBulletShape::Small, Pearl);
	for (int32 k = 0; k < 7; ++k)
	{
		if (Ph == k * Step + 1)
		{
			// a pale warning where the hole will open
			FBullet& W = Fire(Face + Holes[k], 0.f, 90.f, EBulletShape::Large, Pearl);
			W.bHarmless = true;
			W.Life = Step;
			W.Scale = 1.4f;
		}
		if (Ph == (k + 1) * Step || Ph == 8 * Step)
		{
			Ring(Face + Holes[k], 20, 1.6f, Rng.FRandRange(0.f, 30.f), EBulletShape::Small, Rainbow(k * 36.f));
			Fan(Face + Holes[k], 3, 20.f, 2.4f, AimAngle(Face + Holes[k]), EBulletShape::Rice, Ink);
		}
	}
}

// 沌符「無面之舞」: a swirling dance with no direction — every bullet wanders on its own
void ADanmakuWorld::IF_HundunDance(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 3, 0))
	{
		FBullet& B = Fire(Boss.Pos, Rng.FRandRange(1.2f, 1.9f), Rng.FRandRange(0.f, 360.f), EBulletShape::Medium, Rainbow(T * 2.f));
		B.AngVel = Rng.FRandRange(-1.4f, 1.4f);
		B.AngVelDecay = 0.99f;
		B.WaveAmp = 0.6f;
		B.WaveFreq = 0.07f;
		B.WavePhase = Rng.FRandRange(0.f, 6.f);
	}
	if (Every(T, 24, 12)) Ring(Boss.Pos, 26, 1.8f, AimAngle(Boss.Pos), EBulletShape::Small, Pearl);
}

// 沌符「帝江歌舞」: 帝江 sings — notes fall along a melody, and every bar ends with a chord burst
void ADanmakuWorld::IF_HundunSong(int32 T)
{
	PatrolBoss(T);
	static const int32 Melody[16] = { 4, 5, 7, 5, 4, 2, 4, 1, 2, 4, 5, 7, 8, 7, 5, 4 };
	const int32 Beat = FMath::Max(1, FMath::RoundToInt(11 * GetDifficultyInfo(Difficulty).IntervalScale));
	if (T % Beat == 0)
	{
		const int32 N = (T / Beat) % 16;
		const float X = 24.f + Melody[N] * 37.f;
		for (int32 k = 0; k < 4; ++k)
		{
			FBullet& B = Fire(FVector2f(X + (k - 1.5f) * 9.f, -6.f - k * 6.f), 1.8f, 90.f, EBulletShape::Medium, Rainbow(Melody[N] * 28.f));
			B.WaveAmp = 0.4f;
			B.WaveFreq = 0.08f;
		}
		Ring(Boss.Pos, 22, 1.6f, N * 7.f + (N % 2) * (180.f / 22.f), EBulletShape::Small, Pearl);
	}
}

// =====================================================================================
// 窮奇 (the winged tiger) — midboss cards
// =====================================================================================

// 奇符「懲善揚惡」: honest shots fly straight at you; lying shots aim where you are about to be
void ADanmakuWorld::IF_QiongqiLie(int32 T)
{
	PatrolBoss(T);
	FVector2f PVel = FVector2f::ZeroVector;
	if (Player.Trail.Num() >= 4) PVel = (Player.Trail[0] - Player.Trail[3]) / 3.f;
	if (Every(T, 36, 0)) Fan(Boss.Pos, 3, 20.f, 2.5f, AimAngle(Boss.Pos), EBulletShape::Rice, Silver);
	if (Every(T, 36, 18))
	{
		const FVector2f Lead = Player.Pos + PVel * 36.f;
		Fan(Boss.Pos, 3, 20.f, 2.5f, AngleOf(Lead - Boss.Pos), EBulletShape::Crescent, Indigo);
	}
	if (Every(T, 9, 3)) for (int32 k = 0; k < 2; ++k) Fire(Boss.Pos, 1.4f, T * 4.7f + k * 180.f, EBulletShape::Small, Indigo);
}

// 奇符「翼虎狂風」: each beat of the wings blows a gale that pushes you sideways while feathers sweep the field
void ADanmakuWorld::IF_QiongqiGale(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 100.f));
	const int32 Cyc = 150;
	const int32 Ph = T % Cyc;
	const float WindDir = (T / Cyc) % 2 ? -1.f : 1.f;
	if (Ph >= 40 && Ph < 120 && Player.RespawnTimer == 0 && !IsInDialogue())
	{
		Player.Pos.X = FMath::Clamp(Player.Pos.X + WindDir * 1.1f, 8.f, FieldW - 8.f);
		if (T % 4 == 0) AddParticle(FVector2f(WindDir > 0 ? 0.f : FieldW, Rng.FRandRange(60.f, FieldH)), FVector2f(WindDir * 7.f, 0.f), 10.f, Silver, 40);
	}
	if (Every(T, 4, 0))
	{
		for (float S : { -1.f, 1.f })
		{
			Fire(Boss.Pos + FVector2f(S * 46.f, -6.f), 2.f, 90.f + S * (25.f + FMath::Sin(T * 0.07f) * 55.f), EBulletShape::Rice, S > 0 ? Indigo : Silver);
		}
	}
	// a full ring instead of a fan, with twice the gap between crescents
	if (Every(T, 30, 15)) Ring(Boss.Pos, Cnt(24), 2.6f, AimAngle(Boss.Pos) + (T / 30) % 2 * 7.5f, EBulletShape::Crescent, Indigo);
}

// 奇符「食人從首」: it eats people head first — red lines mark the columns above you, then teeth fall down them
void ADanmakuWorld::IF_QiongqiHead(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 60, 0))
	{
		const float X = FMath::Clamp(Player.Pos.X + Rng.FRandRange(-10.f, 10.f), 12.f, FieldW - 12.f);
		AddBeam(FVector2f(X, 0.f), 90.f, 12.f, 44, 6, 20, FLinearColor(1.f, 0.35f, 0.4f), 4);
	}
	if (Every(T, 5, 2))
	{
		FBullet& F = Fire(FVector2f(Rng.FRandRange(8.f, FieldW - 8.f), -6.f), 2.2f, 90.f + Rng.FRandRange(-8.f, 8.f), EBulletShape::Crescent, Indigo);
		F.Accel = 0.01f;
		F.MaxSpeed = 3.f;
	}
}

// =====================================================================================
// The relay — two cards each
// =====================================================================================

// 凶符「吞天噬地」: 饕餮 inhales — you are dragged toward it, and bullets stream up from the ground into its mouth
void ADanmakuWorld::IF_Devour(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 80.f));
	if (Player.RespawnTimer == 0 && !IsInDialogue())
	{
		const FVector2f D = (Boss.Pos - Player.Pos).GetSafeNormal();
		Player.Pos += D * 0.36f;
	}
	if (Every(T, 4, 0))
	{
		const FVector2f From(Rng.FRandRange(10.f, FieldW - 10.f), FieldH + 6.f);
		Fire(From, 1.9f, AngleOf(Boss.Pos - From) + Rng.FRandRange(-6.f, 6.f), EBulletShape::Rice, Jade);
	}
	// every bullet it eats is spat straight back out somewhere in the 120° in front of it
	TArray<FVector2f> Eaten;
	SwallowBullets(Boss.Pos, 70.f, 16.f, 0.04f, &Eaten);
	for (int32 i = 0; i < Eaten.Num(); ++i) SpitBullet(Boss.Pos, Rng.FRandRange(1.6f, 2.1f), 90.f + Rng.FRandRange(-60.f, 60.f), EBulletShape::Medium, Bronze);
	Boss.MawEaten = 0;
}

// 凶符「無底之胃」: 饕餮 circles clockwise; a bronze cauldron always sits on the opposite side firing rings.
// Whatever of the cauldron's rings runs into 饕餮 is eaten and spat back out in a random direction.
void ADanmakuWorld::IF_Stomach(int32 T)
{
	// one big circle over the whole field, top to bottom
	const FVector2f C(FieldW * 0.5f, FieldH * 0.5f);
	const FVector2f R(150.f, 175.f);
	const float A = -90.f + T * 0.4f;   // y points down, so a growing angle turns clockwise on screen
	PlaceBoss(C + Dir(A) * R);
	Boss.MawPos = C - Dir(A) * R;
	// the cauldron
	FBullet& Pot = FireVec(Boss.MawPos, FVector2f::ZeroVector, EBulletShape::Large, Bronze);
	Pot.bHarmless = true;
	Pot.bBombProof = true;
	Pot.Life = 2;
	Pot.Scale = 2.2f;
	if (T % 8 == 0) AddParticle(Boss.MawPos, FVector2f::ZeroVector, 60.f, FLinearColor(0.6f, 0.45f, 0.15f), 14, true);
	if (T > 60 && Every(T, 26, 0))
	{
		const int32 N = Cnt(26);
		const float Base = (T / 26) % 2 ? 180.f / N : 0.f;
		for (int32 i = 0; i < N; ++i) Fire(Boss.MawPos, 1.7f, Base + A + 360.f * i / N, EBulletShape::Medium, Bronze);
	}
	TArray<FVector2f> Eaten;
	SwallowBullets(Boss.Pos, 26.f, 26.f, 0.f, &Eaten);
	for (int32 i = 0; i < Eaten.Num(); ++i) SpitBullet(Boss.Pos, Rng.FRandRange(1.4f, 2.1f), Rng.FRandRange(0.f, 360.f), EBulletShape::Rice, Jade);
	Boss.MawEaten = 0;
}

// 凶符「傲狠明德」: 檮杌 marks you, then charges straight at you, scattering iron behind it
void ADanmakuWorld::IF_Arrogance(int32 T)
{
	const int32 Cyc = FMath::Max(60, FMath::RoundToInt(150 * GetDifficultyInfo(Difficulty).IntervalScale));
	const int32 Ph = T % Cyc;
	if (Ph == 1)
	{
		// wherever it is, it charges at where you are — even straight down to the bottom of the field
		Boss.MawPos = FVector2f(FMath::Clamp(Player.Pos.X, 20.f, FieldW - 20.f), FMath::Clamp(Player.Pos.Y, 20.f, FieldH - 20.f));
		AddBeam(Boss.Pos, AngleOf(Boss.MawPos - Boss.Pos), 4.f, 40, 1, 1, FLinearColor(1.f, 0.3f, 0.3f));
	}
	if (Ph == 40) MoveBossTo(Boss.MawPos, 34);
	if (Ph > 40 && Ph < 70 && Ph % 2 == 0)
	{
		for (float S : { -1.f, 1.f }) Fire(Boss.Pos, 0.9f, AngleOf(Boss.Vel) + 180.f + S * 70.f, EBulletShape::Medium, Iron);
	}
	if (Ph == 70)
	{
		const int32 N = Cnt(20);
		const float Base = Rng.FRandRange(0.f, 18.f);
		Ring(Boss.Pos, N, 1.8f, Base, EBulletShape::Crescent, Crimson);
		Ring(Boss.Pos, N, 1.2f, Base + 180.f / N, EBulletShape::Crescent, Iron);
	}
	// then it just stands where it landed, glaring, until the next charge
	if (Ph == 120) Ring(Boss.Pos, 18, 1.4f, 0.f, EBulletShape::Large, Crimson);
}

// 凶符「不退之陣」: walls of iron advance and never retreat — find the gap — while tusks thrust in from both sides of you
void ADanmakuWorld::IF_NoRetreat(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 70.f));
	if (Every(T, 60, 0))
	{
		const int32 Slots = 16;
		// the gap wanders from row to row instead of jumping anywhere, so it can always be reached in time
		const int32 Prev = T == 0 ? (Slots - 4) / 2 : FMath::RoundToInt(Boss.MawPos.X);
		const int32 Gap = FMath::Clamp(Prev + Rng.RandRange(-3, 3), 1, Slots - 5);
		Boss.MawPos.X = float(Gap);
		for (int32 i = 0; i < Slots; ++i)
		{
			if (i >= Gap && i < Gap + 4) continue;
			FBullet& B = Fire(FVector2f((i + 0.5f) * FieldW / Slots, -8.f), 1.5f, 90.f, EBulletShape::Large, Iron);
			// standing at the bottom isn't safe: each iron ball shatters on the ground into shards
			B.Behavior = BehGroundSplit;
			B.Param = 2;
			B.Param2 = 0.9f;
		}
	}
}

// 凶符「混元一氣」: primordial mist — sparks appear anywhere (never right on you), flare up, then drift
void ADanmakuWorld::IF_PrimalMist(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 3, 0))
	{
		FVector2f P;
		for (int32 Try = 0; Try < 20; ++Try)
		{
			P = FVector2f(Rng.FRandRange(12.f, FieldW - 12.f), Rng.FRandRange(40.f, FieldH - 30.f));
			if (FVector2f::Distance(P, Player.Pos) > 80.f) break;
		}
		FBullet& B = FireVec(P, FVector2f::ZeroVector, EBulletShape::Medium, Rainbow(T * 3.f));
		B.Behavior = BehArm;
		B.Timer = 45;
		B.bHarmless = true;
		B.Scale = 0.4f;
		B.Center = Dir(Rng.FRandRange(0.f, 360.f)) * Rng.FRandRange(0.5f, 1.2f);
		B.Life = 600;
	}
	if (Every(T, 30, 15)) Ring(Boss.Pos, 28, 1.8f, AimAngle(Boss.Pos), EBulletShape::Small, Pearl);
}

// 凶符「渾沌初開」: heaven and earth split apart — a light arm and a dark arm turn against each other
void ADanmakuWorld::IF_ChaosSplit(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f, 120.f));
	if (Every(T, 4, 0))
	{
		for (int32 k = 0; k < 2; ++k)
		{
			const float S = k == 0 ? 1.f : -1.f;
			for (int32 a = 0; a < 2; ++a)
			{
				FBullet& B = Fire(Boss.Pos, 1.9f, S * T * 2.6f + a * 180.f + k * 90.f, EBulletShape::Medium, k == 0 ? Pearl : Ink);
				B.AngVel = S * 0.35f;
				B.AngVelDecay = 0.99f;
			}
		}
	}
	if (Every(T, 70, 35))
	{
		for (int32 k = 0; k < 2; ++k)
		{
			const int32 N = Cnt(24);
			for (int32 i = 0; i < N; ++i)
			{
				FBullet& B = Fire(Boss.Pos, 1.3f, 360.f * i / N + k * 9.f, EBulletShape::Large, k == 0 ? Pearl : Ink);
				B.AngVel = k == 0 ? 0.3f : -0.3f;
				B.AngVelDecay = 0.995f;
			}
		}
	}
}

// 凶符「顛倒是非」: right and wrong turned upside down — every pattern from above is mirrored from below
void ADanmakuWorld::IF_Inversion(int32 T)
{
	PatrolBoss(T);
	const FVector2f Mirror(Boss.Pos.X, FieldH + 10.f);
	if (Every(T, 5, 0))
	{
		const float A = 40.f + FMath::Fmod(T * 3.3f, 100.f);
		Fire(Boss.Pos, 1.8f, A, EBulletShape::Rice, Indigo);
		Fire(Mirror, 1.6f, -A, EBulletShape::Rice, Silver);
	}
	if (Every(T, 60, 30))
	{
		// the crescents come in from the two side walls, level with you, instead of from above and below
		const float Y = FMath::Clamp(Player.Pos.Y + Rng.FRandRange(-60.f, 60.f), 40.f, FieldH - 40.f);
		const FVector2f L(-6.f, Y), Rt(FieldW + 6.f, FMath::Clamp(Player.Pos.Y + Rng.FRandRange(-60.f, 60.f), 40.f, FieldH - 40.f));
		Fan(L, 5, 90.f, 1.7f, AngleOf(Player.Pos - L), EBulletShape::Crescent, Indigo);
		Fan(Rt, 5, 90.f, 1.7f, AngleOf(Player.Pos - Rt), EBulletShape::Crescent, Silver);
	}
}

// 凶符「窮奇之翼」: the tiger sweeps from wall to wall shedding feathers that hang in the air, then dive at you
void ADanmakuWorld::IF_Wings(int32 T)
{
	PlaceBoss(FVector2f(FieldW * 0.5f + FMath::Sin(T * 0.028f) * 140.f, 80.f + FMath::Abs(FMath::Cos(T * 0.028f)) * 40.f));
	if (Every(T, 3, 0))
	{
		FBullet& F = FireVec(Boss.Pos + FVector2f(Rng.FRandRange(-30.f, 30.f), 0.f), FVector2f(Rng.FRandRange(-0.3f, 0.3f), 1.3f), EBulletShape::Rice, (T / 3) % 2 ? Indigo : Silver);
		F.Behavior = BehPauseHome;
		F.Timer = 30;
		F.Timer2 = Rng.RandRange(20, 70);
		F.Param2 = 2.3f;
	}
	if (Every(T, 56, 28)) Ring(Boss.Pos, 28, 1.6f, T * 0.9f, EBulletShape::Crescent, Indigo);
}

// =====================================================================================
// All four together
// =====================================================================================

// 耐久「四凶降世」 (survival): each beast attacks in its own way from its corner of the diamond, faster as time runs on
void ADanmakuWorld::IF_QuadSurvive(int32 T)
{
	const float K = 1.4f + FMath::Clamp(T / 3600.f, 0.f, 1.f) * 0.9f;
	PlaceQuad(T, 120.f);
	auto Ev = [this, T, K](int32 Iv, int32 Off) { return Every(T, FMath::Max(2, FMath::RoundToInt(Iv / K)), Off); };
	const FVector2f* Q = Boss.QuadPos;
	if (Ev(56, 0))
	{
		const int32 N = Cnt(18);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Q[0], 2.8f, 360.f * i / N + T, EBulletShape::Medium, Bronze);
			B.Accel = -0.04f;
			B.MinSpeed = -1.4f;
			B.Life = 400;
		}
	}
	if (Ev(70, 20))
	{
		FBullet& B = Fire(Q[1], 1.6f, Rng.FRandRange(40.f, 140.f), EBulletShape::Large, Crimson);
		B.WallMask = 1 | 2 | 4;
		B.Bounces = 5;
		B.Life = 480;
	}
	if (Ev(8, 4))
	{
		FBullet& B = Fire(Q[2], 1.5f, Rng.FRandRange(0.f, 360.f), EBulletShape::Small, Rainbow(T * 2.f));
		B.AngVel = Rng.FRandRange(-1.f, 1.f);
		B.AngVelDecay = 0.99f;
	}
	if (Ev(36, 10)) Fan(Q[3], 3, 16.f, 2.6f, AimAngle(Q[3]), EBulletShape::Crescent, Indigo);
}

int32 ADanmakuWorld::QuadMainBeast() const
{
	// the beast this character met on the road leads: 蓮 窮奇, 蒼 檮杌, 晴 饕餮, 雪 混沌
	static const int32 MainOf[NumCharacters] = { 3, 1, 0, 2 };
	return MainOf[FMath::Clamp(int32(Player.Character), 0, NumCharacters - 1)];
}

// One beast's signature for the last card. The leader fires it in full; the other three back it up with a thinner copy.
void ADanmakuWorld::IF_BeastSig(int32 Beast, const FVector2f& P, int32 T, bool bMain)
{
	const float Lost = 1.f - FMath::Clamp(Boss.HP / FMath::Max(1.f, Boss.MaxHP), 0.f, 1.f);
	const float Iv = (bMain ? 0.85f : 1.6f) / (1.f + 0.4f * Lost);
	auto Ev = [this, T, Iv](int32 I, int32 Off) { return Every(T, FMath::RoundToInt(I * Iv), Off); };
	switch (Beast)
	{
	case 0:   // 饕餮: rings that run out of breath and fall back; the leader also eats and spits
		if (Ev(48, 0))
		{
			const int32 N = Cnt(bMain ? 24 : 12);
			const float Base = Rng.FRandRange(0.f, 360.f);
			for (int32 i = 0; i < N; ++i)
			{
				FBullet& B = Fire(P, 2.9f, Base + 360.f * i / N, EBulletShape::Medium, i % 2 ? Bronze : Jade);
				B.Accel = -0.04f;
				B.MinSpeed = -1.5f;
				B.Life = 420;
			}
		}
		if (bMain)
		{
			TArray<FVector2f> Eaten;
			SwallowBullets(P, 50.f, 16.f, 0.03f, &Eaten);
			for (int32 i = 0; i < Eaten.Num(); ++i) SpitBullet(P, Rng.FRandRange(1.5f, 2.f), 90.f + Rng.FRandRange(-60.f, 60.f), EBulletShape::Rice, Jade);
			Boss.MawEaten = 0;
		}
		break;
	case 1:   // 檮杌: crescents run away from you, stop, then charge; boulders that bounce around; iron sweeping down
		if (Ev(46, 8))
		{
			const int32 N = Cnt(bMain ? 12 : 5);
			const float Off = (T / FMath::Max(1, FMath::RoundToInt(46 * Iv))) % 2 ? 90.f : -90.f;
			for (int32 i = 0; i < N; ++i)
			{
				FBullet& B = FireVec(P, Dir(AimAngle(P) + 180.f + (i - N * 0.5f) * 14.f) * 2.4f, EBulletShape::Crescent, Crimson);
				B.Behavior = BehPauseDash;
				B.Timer = 36;
				B.Param = (i % 3 - 1) * 36;
				B.Param2 = bMain ? 2.6f : 2.2f;
			}
			if (bMain)
			{
				// and a second spread off to one side, alternating left and right
				for (int32 i = 0; i < Cnt(6); ++i)
				{
					FBullet& B = FireVec(P, Dir(AimAngle(P) + Off + (i - 2.5f) * 12.f) * 2.2f, EBulletShape::Crescent, Iron);
					B.Behavior = BehPauseDash;
					B.Timer = 44;
					B.Param2 = 2.4f;
				}
			}
		}
		if (Ev(90, 30))
		{
			for (int32 k = 0; k < (bMain ? 2 : 1); ++k)
			{
				FBullet& B = Fire(P, 1.7f, Rng.FRandRange(35.f, 145.f), EBulletShape::Large, Crimson);
				B.WallMask = 1 | 2 | 4;
				B.Bounces = bMain ? 4 : 2;
				B.bDieAfterBounces = true;
			}
		}
		if (Ev(7, 3))
		{
			const float Sweep = FMath::Fmod(T * 2.2f, 120.f);
			for (float S : { -1.f, 1.f }) Fire(P, 2.3f, 90.f + S * (15.f + Sweep), EBulletShape::Medium, S > 0 ? Iron : Crimson);
		}
		break;
	case 2:   // 混沌: sparks with no aim that wander on their own
		if (Ev(4, 2))
		{
			FBullet& B = Fire(P, Rng.FRandRange(1.3f, 1.9f), Rng.FRandRange(0.f, 360.f), EBulletShape::Small, Rainbow(T * 2.f));
			B.AngVel = Rng.FRandRange(-1.2f, 1.2f);
			B.AngVelDecay = 0.99f;
		}
		break;
	default:  // 窮奇: an honest shot at you and a lying one at where you're going
		if (Ev(34, 17))
		{
			FVector2f PVel = FVector2f::ZeroVector;
			if (Player.Trail.Num() >= 4) PVel = (Player.Trail[0] - Player.Trail[3]) / 3.f;
			// 5 crescents each, 21° apart (the helpers send 3)
			const int32 N = bMain ? 5 : 3;
			Fan(P, N, 21.f * (N - 1), 2.6f, AimAngle(P), EBulletShape::Crescent, Silver);
			Fan(P, N, 21.f * (N - 1), 2.6f, AngleOf(Player.Pos + PVel * 45.f - P), EBulletShape::Crescent, Indigo);
		}
		break;
	}
}

// 終符「四凶・天地失序」: the last card, fought like a trio-plus-one — the beast you met on the road leads from the
// front with its own signature at full strength; the other three hang back and fire weaker copies of the same pattern
void ADanmakuWorld::IF_QuadFinal(int32 T)
{
	const int32 Main = QuadMainBeast();
	const float Lost = 1.f - FMath::Clamp(Boss.HP / FMath::Max(1.f, Boss.MaxHP), 0.f, 1.f);
	// the leader roams the front; the others keep a loose arc behind it
	const FVector2f Lead(FieldW * 0.5f + FMath::Sin(T * 0.011f) * 100.f, 150.f + FMath::Sin(T * 0.019f) * 22.f);
	static const FVector2f Slots[3] = { { -125.f, -62.f }, { 0.f, -95.f }, { 125.f, -62.f } };
	const FVector2f Back(FieldW * 0.5f, 160.f);
	Boss.Pos = Lead;
	Boss.MoveTime = Boss.MoveDur;
	int32 Slot = 0;
	for (int32 i = 0; i < 4; ++i)
	{
		FVector2f Target = Lead;
		if (i != Main)
		{
			const FVector2f S = Slots[Slot++];
			Target = Back + S + FVector2f(FMath::Sin(T * 0.023f + i) * 18.f, FMath::Cos(T * 0.031f + i) * 10.f);
		}
		Boss.QuadPos[i] = FMath::Lerp(Boss.QuadPos[i], Target, 0.05f);
	}
	// the three helpers back the leader up with a weaker copy of the leader's own pattern
	for (int32 i = 0; i < 4; ++i) IF_BeastSig(Main, Boss.QuadPos[i], T + i * 11, i == Main);
	// the four together: a slow ring from the leader, a little more often as they weaken
	const int32 RingIv = FMath::RoundToInt(FMath::Lerp(80.f, 50.f, Lost));
	if (Every(T, RingIv, RingIv / 2)) Ring(Boss.QuadPos[Main], Cnt(32), 1.5f, T * 0.7f, EBulletShape::Medium, FLinearColor(1.f, 0.85f, 0.5f));
}

// =====================================================================================
// Non-spells — every card is preceded by one. Later ones in a fight get a little denser.
// =====================================================================================

// 饕餮: alternating bronze / jade rings, a hungry aimed spray
void ADanmakuWorld::IF_NsTaotie(int32 T)
{
	if (Every(T, 18, 0)) Ring(Boss.Pos, Cnt(20), 2.0f, T * 2.1f, EBulletShape::Medium, (T / 18) % 2 ? Bronze : Jade);
	if (Every(T, 46, 23)) Fan(Boss.Pos, Cnt(9), 60.f, 3.0f, AimAngle(Boss.Pos), EBulletShape::Rice, Jade);
	if (Every(T, 160, 80)) WanderBoss();
}

// 檮杌: two iron lines sweeping down from both sides, boulders thrown at you
void ADanmakuWorld::IF_NsTaowu(int32 T)
{
	if (Every(T, 5, 0))
	{
		const float Sweep = FMath::Fmod(T * 2.4f, 140.f);
		for (float S : { -1.f, 1.f }) Fire(Boss.Pos, 2.5f, 90.f + S * (10.f + Sweep), EBulletShape::Crescent, S > 0 ? Iron : Crimson);
	}
	if (Every(T, 54, 27)) for (int32 k = 0; k < 3; ++k) Fire(Boss.Pos, 1.6f + k * 0.45f, AimAngle(Boss.Pos), EBulletShape::Large, Crimson);
	if (Every(T, 170, 85)) WanderBoss();
}

// 混沌: a spray with no aim at all, and pale rings that do aim
void ADanmakuWorld::IF_NsHundun(int32 T)
{
	if (Every(T, 3, 0))
	{
		FBullet& B = Fire(Boss.Pos, Rng.FRandRange(1.4f, 2.4f), Rng.FRandRange(0.f, 360.f), EBulletShape::Small, Rainbow(T * 3.f));
		B.AngVel = Rng.FRandRange(-0.8f, 0.8f);
		B.AngVelDecay = 0.985f;
	}
	if (Every(T, 40, 20)) Ring(Boss.Pos, Cnt(26), 1.9f, AimAngle(Boss.Pos), EBulletShape::Rice, Pearl);
	if (Every(T, 150, 75)) WanderBoss();
}

// 窮奇: wingbeats — feathers fan out from both wings, then a crescent fan at you
void ADanmakuWorld::IF_NsQiongqi(int32 T)
{
	if (Every(T, 7, 0))
	{
		for (float S : { -1.f, 1.f })
		{
			Fire(Boss.Pos + FVector2f(S * 40.f, -4.f), 2.4f, 90.f + S * (10.f + FMath::Sin(T * 0.05f) * 50.f), EBulletShape::Rice, S > 0 ? Indigo : Silver);
		}
	}
	if (Every(T, 44, 22)) Fan(Boss.Pos, Cnt(7), 44.f, 2.9f, AimAngle(Boss.Pos), EBulletShape::Crescent, Indigo);
	if (Every(T, 160, 80)) WanderBoss();
}

// all four: each beast takes its turn firing at you, and a ring rises from the middle of the diamond
void ADanmakuWorld::IF_NsQuad(int32 T)
{
	PlaceQuad(T, 120.f);
	const FLinearColor Cols[4] = { Bronze, Crimson, Pearl, Indigo };
	for (int32 i = 0; i < 4; ++i)
	{
		if (Every(T, 40, i * 10)) Fan(Boss.QuadPos[i], Cnt(5), 34.f, 2.7f, AimAngle(Boss.QuadPos[i]), EBulletShape::Medium, Cols[i]);
	}
	if (Every(T, 60, 30)) Ring(Boss.Pos, Cnt(30), 1.6f, T * 0.8f, EBulletShape::Rice, FLinearColor(1.f, 0.85f, 0.5f));
}

// =====================================================================================
// Road: 崩落天宮 (~120 s, one beast blocks the way at the halfway point)
// =====================================================================================

void ADanmakuWorld::RoadIf(int32 F)
{
	constexpr int32 MidAt = 3000;
	auto Rep = [](int32 F, int32 Start, int32 Count, int32 Interval, int32& K)
	{
		if (F < Start) return false;
		const int32 D = F - Start;
		if (D % Interval != 0) return false;
		K = D / Interval;
		return K < Count;
	};
	int32 K;
	if (Rep(F, 90, 30, 12, K))
	{
		const bool bL = K % 2 == 0;
		Doll(FVector2f(bL ? -16.f : FieldW + 16.f, 30.f + (K % 5) * 20.f), FVector2f(bL ? 2.2f : -2.2f, 0.6f), K % 3 == 0 ? 19 : 0).Color = Crimson;
	}
	if (Rep(F, 500, 6, 50, K)) Sentry(FVector2f(60.f + K * 52.f, -20.f), 8, 60, 300).Color = Bronze;
	if (Rep(F, 900, 30, 10, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 2.f), K % 2 ? 2 : 21).Color = Indigo;
	if (F == 1400) { Spirit(FVector2f(110.f, -30.f), 3, 600.f).Color = Crimson; Spirit(FVector2f(FieldW - 110.f, -30.f), 17, 600.f).Color = Indigo; }
	if (Rep(F, 1500, 12, 36, K)) Doll(FVector2f(K % 2 ? 40.f : FieldW - 40.f, -16.f), FVector2f(K % 2 ? 1.f : -1.f, 1.6f), 6, true).Color = Pearl;
	if (Rep(F, 2100, 40, 8, K))
	{
		FEnemy& E = Doll(FVector2f(FieldW * 0.5f, -16.f), Dir(90.f + FMath::Sin(K * 0.5f) * 50.f) * 1.9f, K % 4 == 0 ? 0 : (K % 4 == 2 ? 19 : -1));
		E.Color = Bronze;
	}
	if (Rep(F, 2300, 4, 60, K)) Sentry(FVector2f(80.f + K * 75.f, -20.f), 12, 60, 260).Color = Jade;
	if (!bMidDone)
	{
		if (F >= MidAt)
		{
			if (bDemo && DemoSkipFrames > MidAt) bMidDone = true;
			else if (Enemies.Num() == 0 || F >= MidAt + 240) StartMidboss();
		}
		return;
	}
	if (Rep(F, 3300, 30, 12, K)) Doll(FVector2f(K % 2 ? -16.f : FieldW + 16.f, 40.f + (K % 4) * 20.f), FVector2f(K % 2 ? 2.f : -2.f, 0.8f), 19, K % 5 == 0).Color = Crimson;
	if (Rep(F, 3600, 8, 45, K)) Sentry(FVector2f(50.f + (K % 6) * 57.f, -20.f), K % 2 ? 1 : 15, 60, 280).Color = Indigo;
	if (F == 4300) for (int32 i = 0; i < 2; ++i) Spirit(FVector2f(110.f + i * 164.f, -30.f), i ? 13 : 9, 600.f);
	if (Rep(F, 4400, 30, 14, K)) Doll(FVector2f(Rng.FRandRange(30.f, FieldW - 30.f), -16.f), FVector2f(0.f, 1.8f), K % 3 == 0 ? 16 : 0).Color = Pearl;
	if (Rep(F, 5200, 44, 8, K)) Doll(FVector2f(FieldW * 0.5f + (K % 2 ? -1.f : 1.f) * (20.f + (K % 11) * 14.f), -16.f), FVector2f(0.f, 2.f), K % 3 == 0 ? 2 : -1).Color = Bronze;
	if (F == 6000) for (int32 i = 0; i < 4; ++i) Sentry(FVector2f(64.f + i * 85.f, -20.f), i % 2 ? 1 : 8, 60, 360).Color = i % 2 ? Crimson : Indigo;
	if (F >= 7000 && (Enemies.Num() == 0 || F >= 7300)) StartBoss();
}
