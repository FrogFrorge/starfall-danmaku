#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "DanmakuTypes.h"
#include "DanmakuWorld.generated.h"

struct FDialogueLine
{
	int32 Speaker;        // 0 = player, 1 = boss
	const TCHAR* Text;
};

struct FDanmakuInput
{
	FVector2f Move = FVector2f::ZeroVector;
	bool bFocus = false;
	bool bShoot = false;
};

/**
 * The whole STG simulation: player, bullets, enemies, boss, items, score.
 * Pure 2D math stepped at a fixed 60 Hz; ADanmakuHUD reads this state and draws it.
 */
UCLASS()
class DANMAKUSTG_API ADanmakuWorld : public AActor
{
	GENERATED_BODY()

public:
	ADanmakuWorld();
	virtual void BeginPlay() override;
	virtual void Tick(float DeltaSeconds) override;

	// ---------- State read by the HUD ----------
	EDanmakuState State = EDanmakuState::Title;
	bool bPaused = false;
	int32 Frame = 0;
	int32 StateTimer = 0;
	int32 StageFrame = 0;
	int32 SelectedCharacter = 0;   // cursor on the select screen
	int32 Difficulty = 1;          // 0 Easy .. 3 Lunatic, 4 Extra

	// Run / stage flow
	int32 StageIndex = 0;          // 0..NumStages-1 main stages, ExtraStage for Extra
	bool bExtra = false;
	bool bPractice = false;
	int32 TitleCursor = 0;         // 0 start, 1 extra, 2 stage practice
	int32 PracticeStage = 0;
	// spell practice: pick one card from any boss, fight only that card
	bool bSpellPractice = false;
	int32 SpellCursor = 0;
	int32 GetSpellEntryCount() const;
	FString GetSpellEntryLabel(int32 Index) const;   // "第二面　千秋"
	FString GetSpellEntryName(int32 Index) const;    // the card name
	int32 GetSpellEntryArt(int32 Index) const;       // portrait to preview
	/** Character traits */
	float HitRadius() const { return Danmaku::PlayerHitRadius * (Player.Character == ECharacter::Ren ? 0.8f : 1.f); }
	int32 BombPiecesNeeded() const { return Player.Character == ECharacter::Ren ? 2 : BombPiecesPerBomb; }
	float CollectLine() const { return Danmaku::CollectLineY + (Player.Character == ECharacter::Sou ? 56.f : 0.f); }
	static constexpr float HaruSlowRadius = 64.f;
	static constexpr int32 DollRespawnFrames = 3600;
	int64 StageBonus = 0;
	static constexpr int32 ExtraStage = 6;
	static constexpr int32 IfStage = 7;      // Extra-IF: the four perils
	static constexpr int32 BeastArt = 7;     // art index of the first beast (饕餮, 檮杌, 混沌, 窮奇 = 7..10)
	bool bIf = false;
	static bool IsIfAvailable();
	/** Beast index (0..3) of a dialogue speaker or phase actor, -1 otherwise. */
	int32 GetDialogueArt() const;            // art index of the boss-side speaker
	FString GetDialogueName() const;         // name of the current speaker
	bool IsQuadPhase() const { return Boss.bActive && Boss.Actor == 4; }
	static const TCHAR* BeastName(int32 Beast);
	/** Points on the boss that player shots can hit (the four beasts in an all-four phase). */
	int32 BossTargets(FVector2f Out[4]) const;
	static int32 GetNumStages();   // main stages implemented so far
	static bool IsExtraAvailable();
	FString GetBossName() const;
	bool bMidboss = false;         // the current boss fight is the stage's midboss
	bool bMidDone = false;
	int32 GetBossArtIndex() const;
	FString GetBossTitle() const;
	FString GetStageTitle() const;
	bool IsSurvivalPhase() const;
	/** The boss's last card: bomb damage doesn't hurt it (Haru's bomb deals none anyway). */
	bool IsBombImmunePhase() const;
	bool IsShielded() const { return Boss.ShieldTimer > 0; }
	bool bEnding = false;          // post-fight conversation with the defeated boss

	// Life / bomb recovery (see UpdateRecovery)
	static constexpr int32 LifePiecesPerLife = 5;
	static constexpr int32 BombPiecesPerBomb = 3;
	static constexpr int32 MaxLives = 8;
	static constexpr int32 MaxBombs = 8;
	static constexpr int32 GrazePerBombPiece = 250;
	int32 NextExtend = 0;          // index into the score-extend table
	int32 NextGrazeReward = GrazePerBombPiece;
	FString PopupText;             // short reward notice ("1UP!", "靈符 +1")
	FLinearColor PopupColor = FLinearColor::White;
	int32 PopupTimer = 0;
	static int64 ExtendScore(int32 Index);

	// Hazards / effects driven by spell cards
	TArray<FBeamHazard> Beams;
	float FogRadius = 0.f;         // > 0: only a circle of this radius around the player is visible

	FDanmakuPlayer Player;
	FBossState Boss;
	TArray<FBullet> Bullets;
	TArray<FShot> Shots;
	TArray<FEnemy> Enemies;
	TArray<FItem> Items;
	TArray<FParticle> Particles;

	int64 Score = 0;
	int64 HiScore = 1000000;
	int32 Graze = 0;
	int32 SpellsCaptured = 0;
	int32 SpellsTotal = 0;

	// Bomb (behaviour depends on the character that cast it)
	int32 BombTime = 0;
	int32 BombDuration = 1;
	ECharacter BombKind = ECharacter::Ren;
	FVector2f BombCenter = FVector2f::ZeroVector;
	float BombRadius = 0.f;
	bool bBombHitBoss = false;
	bool IsTimeSlowed() const { return BombTime > 0 && BombKind == ECharacter::Haru; }

	FString Message;
	FString SubMessage;
	int32 MessageTimer = 0;
	int32 MessageDuration = 1;

	bool bDemo = false;

	// sound events for ADanmakuAudio (consumed and cleared every frame)
	TArray<ESfx> SfxQueue;
	void Sfx(ESfx S) { if (SfxQueue.Num() < 64) SfxQueue.Add(S); }

	// Pre-boss dialogue (-1 = none). Speaker: 0 = player, 1 = boss
	int32 DialogueIndex = -1;
	int32 DialogueTimer = 0;
	// a long line is shown over several boxes: the HUD measures it and reports how many boxes it needs
	int32 DialoguePage = 0;
	mutable int32 DialoguePageCount = 1;
	bool IsInDialogue() const { return DialogueIndex >= 0; }
	/** Z pressed during a conversation: next box of this line, or the next line. True when the line changed. */
	bool NextDialogueBox();
	int32 GetDialogueSpeaker() const;
	FString GetDialogueText() const;

	int32 GetBossPhaseCount() const;
	int32 GetBossPhasesRemaining() const;
	int64 GetCurrentSpellBonus() const;

	static constexpr float LaserHalfWidth = 9.f;
	static constexpr float ThunderHalfWidth = 38.f;

private:
	using FPatternFn = void (ADanmakuWorld::*)(int32);
	struct FPhaseDef
	{
		float HP;
		int32 Seconds;
		bool bSpell;
		const TCHAR* Name;
		FPatternFn Pattern;
		bool bSurvival = false;   // endurance card: boss can't be hurt, survive the timer
		int32 MinDifficulty = 0;  // phase is skipped below this difficulty
		int32 Actor = -1;         // Extra-IF relay: which beast fights this phase (4 = all four)
	};
	int32 NextPhaseFrom(int32 Index) const;
	struct FBossDef
	{
		const TCHAR* Name;
		const TCHAR* Title;
		const TCHAR* StageTitle;
		const TCHAR* StageSub;
		TArray<FPhaseDef> Phases;
		TArray<FDialogueLine> Dialogue[4];   // one conversation per player character (ECharacter)
		const FBossDef* Midboss = nullptr;   // optional mid-stage boss (Extra)
		int32 ArtIndex = -1;                 // whose art to use (-1 = this stage's)
		TArray<FDialogueLine> Ending[4];     // optional conversation after the boss is beaten
		const FBossDef* CharMidboss[4] = { nullptr, nullptr, nullptr, nullptr };   // midboss that depends on the player character
	};
	/** Boss / stage table (DanmakuBosses.cpp). Index = stage (ExtraStage = Extra). */
	static const FBossDef* FindBossDef(int32 Stage);
	const FBossDef& BossDef() const;
	const FBossDef* MidbossDef() const
	{
		const FBossDef& D = BossDef();
		const FBossDef* C = D.CharMidboss[FMath::Clamp(int32(Player.Character), 0, 3)];
		return C ? C : D.Midboss;
	}
	const FBossDef& ActiveDef() const
	{
		if (bSpellPractice && PracticeDef) return *PracticeDef;
		return bMidboss && MidbossDef() ? *MidbossDef() : BossDef();
	}
	struct FSpellEntry { int32 Stage; bool bMid; const FBossDef* Def; int32 Phase; };
	static const TArray<FSpellEntry>& SpellEntries();
	const FBossDef* PracticeDef = nullptr;
	int32 PracticePhase = 0;
	const TArray<FDialogueLine>& CurrentDialogue() const
	{
		const int32 C = FMath::Clamp(int32(Player.Character), 0, 3);
		return bEnding ? BossDef().Ending[C] : ActiveDef().Dialogue[C];
	}
	void FinishStage();
	void UpdateEnding();
	const TArray<FPhaseDef>& Phases() const { return ActiveDef().Phases; }
	void StartMidboss();
	void RoadStage1(int32 F); void RoadStage2(int32 F); void RoadStage3(int32 F); void RoadStage4(int32 F);
	void RoadStage5(int32 F); void RoadStage6(int32 F); void RoadExtra(int32 F); void RoadIf(int32 F);
	FEnemy& Doll(const FVector2f& Pos, const FVector2f& Vel, int32 Pattern, bool bRed = false);
	FEnemy& Sentry(const FVector2f& Pos, int32 Pattern, int32 StopAt = 60, int32 LeaveAt = 300, float HP = 40.f);
	FEnemy& Spirit(const FVector2f& Pos, int32 Pattern, float HP);
	// ---- Extra midboss: 玄 returns (DanmakuStageEx.cpp) ----
	void MB_Reverse(int32 T);
	void MB_LoShu(int32 T);
	void MB_EightGates(int32 T);

	void StartStage(int32 Stage);
	void UpdateRecovery();
	void AddLifePieces(int32 N, const TCHAR* Reason);
	void AddBombPieces(int32 N, const TCHAR* Reason);
	void Popup(const FString& Text, const FLinearColor& Color);
	void UpdateBeams(bool bMove);
	FBeamHazard& AddBeam(const FVector2f& Origin, float Angle, float Width, int32 Warn, int32 Rise, int32 Hold, const FLinearColor& Color, uint8 OnStrike = 0);
	void SpawnLeaves(const FVector2f& At, float Angle, int32 Count);

	// Flow
	void PollInput();
	void Step();
	void StartGame(ECharacter Character);
	void UpdatePlaying();
	void UpdateDemo();
	void ShowMessage(const FString& Main, const FString& Sub, int32 Frames);

	// Player
	void UpdatePlayer();
	void UpdateDolls(const FVector2f& Delta);
	void FirePlayerShots();
	void UpdateLaser();
	void TriggerBomb();
	void UpdateBomb();
	bool CanBeHit() const;
	void PlayerHit();
	void KillPlayer();
	float PowerMul() const { return 0.75f + 0.125f * Player.Power; }
	void AddShot(const FVector2f& Pos, float AngleDeg, float Speed, float Damage, EShotType Type);

	// Stage & enemies
	void UpdateStage();
	FEnemy& SpawnEnemy(const FVector2f& Pos, const FVector2f& Vel, int32 Kind, float HP, int32 Pattern,
		int32 StopAt = -1, int32 LeaveAt = -1, const FVector2f& LeaveVel = FVector2f::ZeroVector,
		int32 DropPower = 1, int32 DropPoint = 0);
	void UpdateEnemies();
	void EnemyFire(FEnemy& E);
	void KillEnemy(FEnemy& E);
	void DamageBoss(float Amount);

	// Boss
	void StartBoss();
	void UpdateBoss(bool bEnemyStep);
	void BeginPhase(int32 Index);
	void EndPhase(bool bDefeated);
	void MoveBossTo(const FVector2f& Target, int32 Frames);
	void WanderBoss();
	/** Spell cards whose pattern doesn't need the boss anywhere in particular: move around like in a non-spell. */
	void PatrolBoss(int32 T);
	void StartBomb();   // the bomb effect itself (TriggerBomb pays for it)
	void PlaceBoss(const FVector2f& P);
	void Pattern_NonSpell1(int32 T);
	void Pattern_Sun(int32 T);
	void Pattern_NonSpell2(int32 T);
	void Pattern_Moon(int32 T);
	void Pattern_Star(int32 T);
	void Pattern_Final(int32 T);
	// pattern pieces shared by the moon / star / final cards
	void SunRings(int32 T, int32 Interval, int32 Rings, int32 BaseCount);
	void LunarSideWave(int32 T);
	void OrbitingMoons(int32 Count, float Radius, int32 OrbitTime);
	void StarRain(int32 Num);
	void ConstellationBurst(int32 T);
	void MeteorSweep(bool bFromLeft);

	// ---- Stage 2: 千秋, painter of the four seasons (DanmakuStage2.cpp) ----
	void S2_NonSpell1(int32 T);
	void S2_Spring(int32 T);
	void S2_NonSpell2(int32 T);
	void S2_Summer(int32 T);
	void S2_Autumn(int32 T);
	void S2_Winter(int32 T);
	FBullet& FirePolar(const FVector2f& Center, float Angle, float StartRadius, float RadSpeed, float AngSpeed, EBulletShape Shape, const FLinearColor& Color);
	FBullet& FireVec(const FVector2f& Pos, const FVector2f& Vel, EBulletShape Shape, const FLinearColor& Color);

	// ---- scripted bullet behaviours shared by stages 3+ ----
	enum : uint8 { BehPlumSeed = 1, BehLotusSummon, BehPauseHome, BehAssassin, BehFinalChrys, BehDelayedRelease, BehCurveAfter, BehSplitRing, BehPauseDash,
		BehArm, BehWallStop, BehGroundSplit };
	bool UpdateBehavior(FBullet& B);   // false = bullet died
	void SpawnPlumSeed(const FVector2f& At, int32 Delay, const FLinearColor& Color, int32 Petals = 5, float PetalSpeed = 1.3f);
	void BloomLotus(const FVector2f& At, int32 Number, const FLinearColor& Color);
	void TripleRings(const FVector2f& At, float BaseAngle, float SpeedK, EBulletShape Shape, const FLinearColor* Colors);
	void SpawnWallBamboo(int32 Side, const FVector2f& Hit, int32 Warn, int32 LeafRepeat);
	void ElementVolley(int32 Element, int32 T, float Intensity);

	// ---- Stage 3: 嵐, the weather master (DanmakuStage3.cpp) ----
	void S3_NonSpell1(int32 T);
	void S3_Sunny(int32 T);
	void S3_NonSpell2(int32 T);
	void S3_Rain(int32 T);
	void S3_Storm(int32 T);
	void S3_Fog(int32 T);
	// ---- Stage 4: 墨竹, scholar of the four gentlemen (DanmakuStage4.cpp) ----
	void S4_NonSpell1(int32 T);
	void S4_Plum(int32 T);
	void S4_NonSpell2(int32 T);
	void S4_Orchid(int32 T);
	void S4_Bamboo(int32 T);
	void S4_Chrys(int32 T);
	void S4_Final(int32 T);
	// ---- Stage 5: 玄, sorcerer of the five elements (DanmakuStage5.cpp) ----
	void S5_NonSpell1(int32 T);
	void S5_Wood(int32 T);
	void S5_Fire(int32 T);
	void S5_NonSpell2(int32 T);
	void S5_Earth(int32 T);
	void S5_Metal(int32 T);
	void S5_NonSpell3(int32 T);
	void S5_Water(int32 T);
	void S5_Cycle(int32 T);
	// ---- Stage 6: 燁, guardian of the four symbols (DanmakuStage6.cpp) ----
	void S6_NonSpell1(int32 T);
	void S6_Dragon(int32 T);
	void S6_NonSpell2(int32 T);
	void S6_Tiger(int32 T);
	void S6_NonSpell3(int32 T);
	void S6_Phoenix(int32 T);
	void S6_Tortoise(int32 T);
	void S6_NonSpell4(int32 T);
	void S6_Final(int32 T);
	void S6_DragonPart(int32 T, float K);
	void S6_TigerPart(int32 T, float K);
	void S6_PhoenixPart(int32 T, float K);
	void S6_TortoisePart(int32 T, float K);
	// ---- Extra: 一索, the mahjong master (DanmakuStageEx.cpp) ----
	void EX_NonSpell1(int32 T);
	void EX_Riichi(int32 T);
	void EX_Pinfu(int32 T);
	void EX_NonSpell2(int32 T);
	void EX_Chinitsu(int32 T);
	void EX_Chiitoi(int32 T);
	void EX_NonSpell3(int32 T);
	void EX_Rinshan(int32 T);
	void EX_Daisangen(int32 T);
	void EX_Daisuushi(int32 T);
	void EX_Chuuren(int32 T);
	void EX_Kokushi(int32 T);
	void EX_Tenhou(int32 T);
	// ---- Extra-IF: the four perils (DanmakuStageIf.cpp) ----
	// midboss cards, three per beast
	void IF_TaotieMaw(int32 T);   void IF_TaotieFeast(int32 T); void IF_TaotieMotif(int32 T);
	void IF_TaowuStone(int32 T);  void IF_TaowuUntamed(int32 T); void IF_TaowuTail(int32 T);
	void IF_HundunSeven(int32 T); void IF_HundunDance(int32 T);  void IF_HundunSong(int32 T);
	void IF_QiongqiLie(int32 T);  void IF_QiongqiGale(int32 T);  void IF_QiongqiHead(int32 T);
	// relay cards, two per beast, then all four together
	void IF_Devour(int32 T);      void IF_Stomach(int32 T);
	void IF_Arrogance(int32 T);   void IF_NoRetreat(int32 T);
	void IF_PrimalMist(int32 T);  void IF_ChaosSplit(int32 T);
	void IF_Inversion(int32 T);   void IF_Wings(int32 T);
	void IF_QuadSurvive(int32 T); void IF_QuadFinal(int32 T);
	// non-spells before every card
	void IF_NsTaotie(int32 T); void IF_NsTaowu(int32 T); void IF_NsHundun(int32 T); void IF_NsQiongqi(int32 T); void IF_NsQuad(int32 T);
	void PlaceQuad(int32 T, float Spread);
	// returns how many it ate this frame; Eaten (optional) gets where each one was swallowed
	int32 SwallowBullets(const FVector2f& At, float PullRadius, float EatRadius, float Pull, TArray<FVector2f>* Eaten = nullptr);
	void SpitBullet(const FVector2f& At, float Speed, float Angle, EBulletShape Shape, const FLinearColor& Color);
	void IF_BeastSig(int32 Beast, const FVector2f& P, int32 T, bool bMain);
	int32 QuadMainBeast() const;
	// Tenhou replays earlier hands at reduced intensity: Every()/Cnt() are scaled and PlaceBoss is ignored
	bool bComposite = false;
	float CompInterval = 1.f;
	float CompCount = 1.f;

	// Bullets
	FBullet& Fire(const FVector2f& Pos, float Speed, float Angle, EBulletShape Shape, const FLinearColor& Color);
	void Ring(const FVector2f& Pos, int32 Count, float Speed, float BaseAngle, EBulletShape Shape, const FLinearColor& Color,
		float Accel = 0.f, float MaxSpeed = 1000.f);
	void Fan(const FVector2f& Pos, int32 Count, float Spread, float Speed, float CenterAngle, EBulletShape Shape, const FLinearColor& Color);
	float AimAngle(const FVector2f& From) const;
	// difficulty helpers: scaled firing interval / bullet count / symmetric fan half-width
	bool Every(int32 T, int32 Interval, int32 Offset = 0) const;
	int32 Cnt(int32 N) const;
	int32 Half(int32 N) const;
	void UpdateBullets(bool bMove);
	void HandleBulletEvent(FBullet& B);
	void CancelAllBullets(bool bToStars, bool bKeepProof = false);
	void UpdateShots();
	bool FindHomingTarget(const FVector2f& From, FVector2f& OutTarget) const;

	// Items & FX
	void SpawnItem(EItemType Type, const FVector2f& Pos, bool bAutoCollect);
	void UpdateItems();
	void CollectItem(const FItem& I);
	void AddParticle(const FVector2f& Pos, const FVector2f& Vel, float Size, const FLinearColor& Color, int32 Life, bool bRing = false);
	void Burst(const FVector2f& Pos, int32 Count, const FLinearColor& Color, float Speed, float Size, int32 Life);
	void UpdateParticles();

	TArray<FBullet> NewBullets;
	FRandomStream Rng;
	float Accumulator = 0.f;

	FDanmakuInput CurInput;
	bool bPrevBomb = false, bPrevStart = false, bPrevPause = false, bPrevRestart = false, bPrevLeft = false, bPrevRight = false, bPrevUp = false, bPrevDown = false;
	bool bEdgeBomb = false, bEdgeStart = false, bEdgePause = false, bEdgeRestart = false, bEdgeLeft = false, bEdgeRight = false, bEdgeUp = false, bEdgeDown = false;

	// Demo / automated capture (-DanmakuDemo -DanmakuSkip=N -DanmakuPhase=N -DanmakuChar=N -DanmakuTitle
	//                            -DanmakuSelect -DanmakuBombAt=N -DanmakuShotEvery=N -DanmakuShots=N)
	int32 DemoSkipFrames = 0;
	int32 DemoStartPhase = 0;
	int32 DemoCharacter = 0;
	int32 DemoDifficulty = 1;
	int32 DemoStage = -1;
	float DemoHPScale = 1.f;
	int32 DemoBombAt = -1;
	int32 DemoShotEvery = 0;
	int32 DemoShotCount = 0;
	int32 DemoShotsTaken = 0;
	int32 DemoQuitTimer = 0;
	bool bDemoHits = false;   // -DanmakuHitTest: the demo player can be hit

	// Road difficulty (DanmakuRoads.cpp). Tier 0 = Easy ... 3 = Lunatic; -1 = Extra / Extra-IF, whose roads keep
	// their own fixed layout. Higher tiers squeeze more dolls into each fast wave, fire more often and in bigger
	// volleys, and on Lunatic enemies spit a little burst back when they die.
	int32 RoadTier() const { return StageIndex >= ExtraStage ? -1 : FMath::Clamp(Difficulty, 0, 3); }
	bool RoadEvery(int32 Age, int32 Interval, int32 Offset) const;
	int32 RoadCount(int32 N, bool bWasScaled = false) const;
	float RoadDensity() const;

	// Autopilot (DanmakuBot.cpp): -DanmakuBot plays by itself (and can be hit); -DanmakuTurbo=N runs N steps per tick
	// for fast headless tests; -DanmakuBotTag=name names the result file (Saved/BotResult_name.txt)
	bool bBot = false;
	int32 TurboSteps = 0;
	FString BotTag = TEXT("run");
	FString BotLog;
	int32 BotDeaths = 0, BotBombs = 0, BotLastLives = 0;
	bool bBotDone = false;
	// difficulty measurement: -DanmakuBotSkill=1..3 handicaps the bot (shorter look-ahead, slower decisions, sloppier
	// dodging); -DanmakuMeasure makes hits cost nothing (they're only counted); -DanmakuSweep plays every spell card
	// from -DanmakuSpell=N to the end, one after another, and writes Saved/BotSweep_<tag>.txt
	int32 BotSkill = 0;
	bool bMeasure = false, bSweep = false, bRoadMeasure = false;   // -DanmakuRoadMeasure: roads of stages 1-6 instead
	int32 SweepIndex = 0;
	int32 MeasureHits = 0, MeasureFrames = 0;
	double MeasureNear = 0.0, MeasureClose = 0.0;
	int32 BotHold = 0;
	FVector2f BotLastMove = FVector2f::ZeroVector;
	bool bBotLastFocus = false;
	FRandomStream BotRng = FRandomStream(777);   // the bot's own dice, so it never disturbs the game's RNG
	FString SweepLog;
	void BotStartSpell(int32 Index);
	void BotSweepNext();
	void UpdateBot();
	void BotLogLine(const FString& Line);
	FString BotWhere() const;
	void BotFinish();
};
