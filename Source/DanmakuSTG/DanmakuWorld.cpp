#include "DanmakuWorld.h"

#include "Engine/World.h"
#include "GameFramework/PlayerController.h"
#include "InputCoreTypes.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"
#include "UnrealClient.h"
#include "HAL/PlatformMisc.h"

using namespace Danmaku;

namespace
{
	const FLinearColor ColRed(1.f, 0.18f, 0.22f);
	const FLinearColor ColPink(1.f, 0.45f, 0.75f);
	const FLinearColor ColMagenta(0.95f, 0.25f, 1.f);
	const FLinearColor ColPurple(0.6f, 0.3f, 1.f);
	const FLinearColor ColBlue(0.25f, 0.45f, 1.f);
	const FLinearColor ColCyan(0.25f, 0.9f, 1.f);
	const FLinearColor ColLightBlue(0.55f, 0.8f, 1.f);
	const FLinearColor ColGreen(0.35f, 1.f, 0.45f);
	const FLinearColor ColYellow(1.f, 0.9f, 0.25f);
	const FLinearColor ColOrange(1.f, 0.55f, 0.15f);
	const FLinearColor ColWhite(0.95f, 0.95f, 1.f);
	const FLinearColor ColMoon(0.82f, 0.9f, 1.f);
	const FLinearColor ColGold(1.f, 0.82f, 0.45f);

	constexpr float RadToDeg = 57.29578f;

	FORCEINLINE float Sq(float X) { return X * X; }

	bool InField(const FVector2f& P, float Margin)
	{
		return P.X > -Margin && P.X < FieldW + Margin && P.Y > -Margin && P.Y < FieldH + Margin;
	}

	int32 BombLength(ECharacter C)
	{
		switch (C)
		{
		case ECharacter::Ren:  return 120;
		case ECharacter::Sou:  return 240;
		case ECharacter::Haru: return 300;
		case ECharacter::Yuki: return 240;
		}
		return 150;
	}

}

// =====================================================================================
// Boss / stage accessors (the tables live in DanmakuBosses.cpp)
// =====================================================================================

int32 ADanmakuWorld::GetNumStages()
{
	int32 N = 0;
	while (N < ExtraStage && FindBossDef(N)) ++N;
	return N;
}

bool ADanmakuWorld::IsExtraAvailable() { return FindBossDef(ExtraStage) != nullptr; }
bool ADanmakuWorld::IsIfAvailable() { return FindBossDef(IfStage) != nullptr; }

const TCHAR* ADanmakuWorld::BeastName(int32 Beast)
{
	static const TCHAR* Names[4] = { TEXT("饕餮"), TEXT("檮杌"), TEXT("混沌"), TEXT("窮奇") };
	return Tr(Names[FMath::Clamp(Beast, 0, 3)]);
}

int32 ADanmakuWorld::BossTargets(FVector2f Out[4]) const
{
	if (IsQuadPhase())
	{
		for (int32 i = 0; i < 4; ++i) Out[i] = Boss.QuadPos[i];
		return 4;
	}
	Out[0] = Boss.Pos;
	return 1;
}

int32 ADanmakuWorld::GetDialogueArt() const
{
	const int32 Sp = GetDialogueSpeaker();
	return Sp >= 2 ? BeastArt + Sp - 2 : GetBossArtIndex();
}

FString ADanmakuWorld::GetDialogueName() const
{
	const int32 Sp = GetDialogueSpeaker();
	if (Sp >= 2) return BeastName(Sp - 2);
	return Sp == 1 ? GetBossName() : FString(Tr(GetCharacterInfo(Player.Character).Name));
}

const ADanmakuWorld::FBossDef& ADanmakuWorld::BossDef() const
{
	const FBossDef* D = FindBossDef(StageIndex);
	return D ? *D : *FindBossDef(0);
}

FString ADanmakuWorld::GetBossName() const
{
	if (!bMidboss && Boss.Actor >= 0 && Boss.Actor < 4) return BeastName(Boss.Actor);
	return Tr(ActiveDef().Name);
}
FString ADanmakuWorld::GetBossTitle() const { return Tr(ActiveDef().Title); }
int32 ADanmakuWorld::GetBossArtIndex() const
{
	if (!bMidboss && StageIndex == IfStage)
	{
		// the relay: whoever is fighting now (before the first card, the first beast)
		int32 A = Boss.Actor;
		if (A < 0 && Phases().Num() > 0) A = Phases()[0].Actor;
		return BeastArt + FMath::Clamp(A, 0, 3);
	}
	return ActiveDef().ArtIndex >= 0 ? ActiveDef().ArtIndex : StageIndex;
}
FString ADanmakuWorld::GetStageTitle() const { return Tr(BossDef().StageTitle); }

bool ADanmakuWorld::IsSurvivalPhase() const
{
	return Boss.bActive && Phases().IsValidIndex(Boss.Phase) && Phases()[Boss.Phase].bSurvival;
}

bool ADanmakuWorld::IsBombImmunePhase() const
{
	if (bMidboss) return false;   // road bosses' last cards can still be bombed
	return Boss.bActive && Boss.bSpell && Phases().IsValidIndex(Boss.Phase) && NextPhaseFrom(Boss.Phase + 1) >= Phases().Num();
}

int32 ADanmakuWorld::GetDialogueSpeaker() const
{
	const TArray<FDialogueLine>& D = CurrentDialogue();
	return IsInDialogue() && D.IsValidIndex(DialogueIndex) ? D[DialogueIndex].Speaker : 0;
}

FString ADanmakuWorld::GetDialogueText() const
{
	const TArray<FDialogueLine>& D = CurrentDialogue();
	return IsInDialogue() && D.IsValidIndex(DialogueIndex) ? FString(Tr(D[DialogueIndex].Text)) : FString();
}

const TArray<ADanmakuWorld::FSpellEntry>& ADanmakuWorld::SpellEntries()
{
	static TArray<FSpellEntry> List;
	if (List.Num() > 0) return List;
	auto AddDef = [](int32 Stage, bool bMid, const FBossDef* D)
	{
		if (!D) return;
		for (int32 i = 0; i < D->Phases.Num(); ++i) if (D->Phases[i].bSpell) List.Add({ Stage, bMid, D, i });
	};
	for (int32 S = 0; S <= IfStage; ++S)
	{
		const FBossDef* D = FindBossDef(S);
		if (!D) continue;
		AddDef(S, true, D->Midboss);
		// Extra-IF midbosses in beast order: 饕餮 (晴), 檮杌 (蒼), 混沌 (雪), 窮奇 (蓮)
		for (int32 C : { 2, 1, 3, 0 }) AddDef(S, true, D->CharMidboss[C]);
		AddDef(S, false, D);
	}
	return List;
}

int32 ADanmakuWorld::GetSpellEntryCount() const { return SpellEntries().Num(); }

FString ADanmakuWorld::GetSpellEntryLabel(int32 Index) const
{
	if (!SpellEntries().IsValidIndex(Index)) return FString();
	const FSpellEntry& E = SpellEntries()[Index];
	static const TCHAR* StageNames[] = { TEXT("第一面"), TEXT("第二面"), TEXT("第三面"), TEXT("第四面"), TEXT("第五面"), TEXT("第六面"), TEXT("Extra"), TEXT("Extra-IF") };
	FString Who = Tr(E.Def->Name);
	const int32 Actor = E.Def->Phases[E.Phase].Actor;
	if (!E.bMid && Actor >= 0) Who = Actor < 4 ? FString(BeastName(Actor)) : FString(Tr(TEXT("四凶")));
	return FString(Tr(StageNames[FMath::Clamp(E.Stage, 0, 7)])) + TEXT("　") + Who;
}

FString ADanmakuWorld::GetSpellEntryName(int32 Index) const
{
	return SpellEntries().IsValidIndex(Index) ? FString(Tr(SpellEntries()[Index].Def->Phases[SpellEntries()[Index].Phase].Name)) : FString();
}

int32 ADanmakuWorld::GetSpellEntryArt(int32 Index) const
{
	if (!SpellEntries().IsValidIndex(Index)) return 0;
	const FSpellEntry& E = SpellEntries()[Index];
	const int32 Actor = E.Def->Phases[E.Phase].Actor;
	if (!E.bMid && Actor >= 0) return BeastArt + FMath::Clamp(Actor, 0, 3);
	return E.Def->ArtIndex >= 0 ? E.Def->ArtIndex : E.Stage;
}

int32 ADanmakuWorld::GetBossPhaseCount() const { return Phases().Num(); }

int32 ADanmakuWorld::NextPhaseFrom(int32 Index) const
{
	const TArray<FPhaseDef>& P = Phases();
	while (Index < P.Num() && P[Index].MinDifficulty > Difficulty) ++Index;
	return Index;
}

int32 ADanmakuWorld::GetBossPhasesRemaining() const
{
	int32 N = 0;
	for (int32 i = Boss.Phase < 0 ? 0 : Boss.Phase + 1; i < Phases().Num(); ++i) N += Phases()[i].MinDifficulty <= Difficulty ? 1 : 0;
	return N;
}

int64 ADanmakuWorld::GetCurrentSpellBonus() const
{
	if (!Boss.bSpell) return 0;
	// survival cards can't be finished early, so their bonus doesn't shrink with time
	if (IsSurvivalPhase()) return 6000000;
	const float Frac = FMath::Clamp(float(Boss.TimeLeft) / float(FMath::Max(1, Boss.TimeTotal)), 0.f, 1.f);
	return (int64(2000000 + 4000000 * Frac) / 10) * 10;
}

// =====================================================================================
// Flow
// =====================================================================================

ADanmakuWorld::ADanmakuWorld()
{
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.TickGroup = TG_PrePhysics;
}

void ADanmakuWorld::BeginPlay()
{
	Super::BeginPlay();
	Rng.Initialize(20261106);

	const TCHAR* Cmd = FCommandLine::Get();
	bDemo = FParse::Param(Cmd, TEXT("DanmakuDemo"));
	bDemoHits = FParse::Param(Cmd, TEXT("DanmakuHitTest"));
	bBot = bDemo && FParse::Param(Cmd, TEXT("DanmakuBot"));
	if (bBot) bDemoHits = true;
	FParse::Value(Cmd, TEXT("DanmakuTurbo="), TurboSteps);
	FParse::Value(Cmd, TEXT("DanmakuBotTag="), BotTag);
	FParse::Value(Cmd, TEXT("DanmakuBotSkill="), BotSkill);
	bMeasure = bBot && FParse::Param(Cmd, TEXT("DanmakuMeasure"));
	bSweep = bMeasure && FParse::Param(Cmd, TEXT("DanmakuSweep"));
	bRoadMeasure = bMeasure && FParse::Param(Cmd, TEXT("DanmakuRoadMeasure"));
	FParse::Value(Cmd, TEXT("DanmakuSkip="), DemoSkipFrames);
	FParse::Value(Cmd, TEXT("DanmakuPhase="), DemoStartPhase);
	FParse::Value(Cmd, TEXT("DanmakuChar="), DemoCharacter);
	FParse::Value(Cmd, TEXT("DanmakuDiff="), DemoDifficulty);
	FParse::Value(Cmd, TEXT("DanmakuBombAt="), DemoBombAt);
	FParse::Value(Cmd, TEXT("DanmakuShotEvery="), DemoShotEvery);
	FParse::Value(Cmd, TEXT("DanmakuShots="), DemoShotCount);
	FParse::Value(Cmd, TEXT("DanmakuStage="), DemoStage);
	FParse::Value(Cmd, TEXT("DanmakuHPScale="), DemoHPScale);
	FString Lang;
	if (FParse::Value(Cmd, TEXT("DanmakuLang="), Lang)) SetLanguage(Lang.StartsWith(TEXT("en")) ? 1 : 0);
	DemoStartPhase = FMath::Max(0, DemoStartPhase);
	DemoCharacter = FMath::Clamp(DemoCharacter, 0, NumCharacters - 1);
	SelectedCharacter = DemoCharacter;
	Difficulty = FMath::Clamp(DemoDifficulty, 0, NumDifficulties - 1);

	if (bDemo)
	{
		int32 DemoSpell = -1;
		FParse::Value(Cmd, TEXT("DanmakuSpell="), DemoSpell);
		if (FParse::Param(Cmd, TEXT("DanmakuSelect"))) { State = EDanmakuState::CharSelect; StateTimer = 0; }
		else if (FParse::Param(Cmd, TEXT("DanmakuSpellSelect"))) { State = EDanmakuState::SpellSelect; StateTimer = 0; SpellCursor = FMath::Max(0, DemoSpell); }
		else if (bSweep && SpellEntries().IsValidIndex(DemoSpell))
		{
			SweepIndex = DemoSpell;
			BotStartSpell(SweepIndex);
		}
		else if (SpellEntries().IsValidIndex(DemoSpell))
		{
			// start one card of spell practice directly
			const FSpellEntry& E = SpellEntries()[DemoSpell];
			bSpellPractice = bPractice = true;
			bExtra = E.Stage >= ExtraStage;
			bIf = E.Stage >= IfStage;
			PracticeStage = E.Stage;
			PracticeDef = E.Def;
			PracticePhase = E.Phase;
			if (bExtra) Difficulty = NumDifficulties;
			StartGame(ECharacter(DemoCharacter));
		}
		else if (FParse::Param(Cmd, TEXT("DanmakuDiffSelect"))) { State = EDanmakuState::DiffSelect; StateTimer = 0; }
		else if (!FParse::Param(Cmd, TEXT("DanmakuTitle")))
		{
			if (DemoStage >= 0)
			{
				bExtra = DemoStage >= ExtraStage;
				bIf = DemoStage >= IfStage;
				bPractice = !bExtra;
				PracticeStage = FMath::Clamp(DemoStage, 0, GetNumStages() - 1);
				if (bExtra) Difficulty = NumDifficulties;
			}
			StartGame(ECharacter(DemoCharacter));
		}
	}
}

void ADanmakuWorld::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	PollInput();

	if (TurboSteps > 0)
	{
		// headless test runs: as many fixed steps per tick as asked, no matter how long the tick was
		for (int32 i = 0; i < TurboSteps; ++i) Step();
		return;
	}

	Accumulator += FMath::Min(DeltaSeconds, 0.1f);
	int32 Steps = 0;
	while (Accumulator >= StepSeconds && Steps < 4)
	{
		Accumulator -= StepSeconds;
		Step();
		++Steps;
	}
	if (Steps == 4) Accumulator = 0.f;
}

void ADanmakuWorld::PollInput()
{
	APlayerController* PC = GetWorld() ? GetWorld()->GetFirstPlayerController() : nullptr;
	if (!PC) return;

	auto Down = [PC](const FKey& K) { return PC->IsInputKeyDown(K); };

	float X = 0.f, Y = 0.f;
	if (Down(EKeys::Left)  || Down(EKeys::A) || Down(EKeys::Gamepad_DPad_Left))  X -= 1.f;
	if (Down(EKeys::Right) || Down(EKeys::D) || Down(EKeys::Gamepad_DPad_Right)) X += 1.f;
	if (Down(EKeys::Up)    || Down(EKeys::W) || Down(EKeys::Gamepad_DPad_Up))    Y -= 1.f;
	if (Down(EKeys::Down)  || Down(EKeys::S) || Down(EKeys::Gamepad_DPad_Down))  Y += 1.f;
	const float AX = PC->GetInputAnalogKeyState(EKeys::Gamepad_LeftX);
	const float AY = PC->GetInputAnalogKeyState(EKeys::Gamepad_LeftY);
	if (FMath::Abs(AX) > 0.35f) X += FMath::Sign(AX);
	if (FMath::Abs(AY) > 0.35f) Y -= FMath::Sign(AY);

	FVector2f Move(FMath::Clamp(X, -1.f, 1.f), FMath::Clamp(Y, -1.f, 1.f));
	if (!Move.IsNearlyZero()) Move.Normalize();
	CurInput.Move = Move;
	CurInput.bFocus = Down(EKeys::LeftShift) || Down(EKeys::RightShift) || Down(EKeys::Gamepad_RightShoulder) || Down(EKeys::Gamepad_LeftShoulder);
	CurInput.bShoot = Down(EKeys::Z) || Down(EKeys::J) || Down(EKeys::Gamepad_FaceButton_Bottom);

	const bool bBomb = Down(EKeys::X) || Down(EKeys::K) || Down(EKeys::Gamepad_FaceButton_Right);
	const bool bStart = Down(EKeys::Z) || Down(EKeys::Enter) || Down(EKeys::SpaceBar) || Down(EKeys::Gamepad_FaceButton_Bottom);
	const bool bPause = Down(EKeys::P) || Down(EKeys::Gamepad_Special_Right);
	const bool bRestart = Down(EKeys::R) || Down(EKeys::Gamepad_Special_Left);
	const bool bLeft = X < -0.5f;
	const bool bRight = X > 0.5f;
	const bool bUp = Y < -0.5f;
	const bool bDown = Y > 0.5f;

	bEdgeBomb |= bBomb && !bPrevBomb;
	bEdgeStart |= bStart && !bPrevStart;
	bEdgePause |= bPause && !bPrevPause;
	bEdgeRestart |= bRestart && !bPrevRestart;
	bEdgeLeft |= bLeft && !bPrevLeft;
	bEdgeRight |= bRight && !bPrevRight;
	bEdgeUp |= bUp && !bPrevUp;
	bEdgeDown |= bDown && !bPrevDown;
	bPrevBomb = bBomb; bPrevStart = bStart; bPrevPause = bPause; bPrevRestart = bRestart; bPrevLeft = bLeft; bPrevRight = bRight; bPrevUp = bUp; bPrevDown = bDown;
}

void ADanmakuWorld::Step()
{
	if (bEdgePause && State == EDanmakuState::Playing)
	{
		bPaused = !bPaused;
		Sfx(ESfx::Pause);
	}
	if (bPaused && bEdgeRestart)
	{
		StartGame(Player.Character);
	}

	if (!bPaused)
	{
		++Frame;
		++StateTimer;
		switch (State)
		{
		case EDanmakuState::Title:
			if (bDemo && StateTimer > 150) bEdgeStart = true;
			if (bEdgeUp)   { TitleCursor = (TitleCursor + 5) % 6; Sfx(ESfx::MenuMove); }
			if (bEdgeDown) { TitleCursor = (TitleCursor + 1) % 6; Sfx(ESfx::MenuMove); }
			if (TitleCursor == 5 && ((bEdgeStart && StateTimer > 10) || bEdgeLeft || bEdgeRight))
			{
				SetLanguage(1 - GetLanguage());
				Sfx(ESfx::MenuOk);
			}
			else if (bEdgeStart && StateTimer > 10)
			{
				if ((TitleCursor == 1 && !IsExtraAvailable()) || (TitleCursor == 2 && !IsIfAvailable()))
				{
					Sfx(ESfx::MenuBack);
				}
				else
				{
					Sfx(ESfx::MenuOk);
					bExtra = TitleCursor == 1 || TitleCursor == 2;
					bIf = TitleCursor == 2;
					bPractice = false;
					bSpellPractice = false;
					if (bExtra) { Difficulty = NumDifficulties; State = EDanmakuState::CharSelect; }
					else if (TitleCursor == 3) State = EDanmakuState::StageSelect;
					else if (TitleCursor == 4) State = EDanmakuState::SpellSelect;
					else { if (Difficulty >= NumDifficulties) Difficulty = 1; State = EDanmakuState::DiffSelect; }
					StateTimer = 0;
				}
			}
			break;
		case EDanmakuState::StageSelect:
			if (bEdgeUp)   { PracticeStage = (PracticeStage + GetNumStages() - 1) % GetNumStages(); Sfx(ESfx::MenuMove); }
			if (bEdgeDown) { PracticeStage = (PracticeStage + 1) % GetNumStages(); Sfx(ESfx::MenuMove); }
			if (bEdgeStart && StateTimer > 10)
			{
				Sfx(ESfx::MenuOk);
				bPractice = true;
				if (Difficulty >= NumDifficulties) Difficulty = 1;
				State = EDanmakuState::DiffSelect;
				StateTimer = 0;
			}
			else if (bEdgeBomb) { Sfx(ESfx::MenuBack); State = EDanmakuState::Title; StateTimer = 0; }
			break;
		case EDanmakuState::SpellSelect:
		{
			const int32 N = FMath::Max(1, GetSpellEntryCount());
			if (bEdgeUp)    { SpellCursor = (SpellCursor + N - 1) % N; Sfx(ESfx::MenuMove); }
			if (bEdgeDown)  { SpellCursor = (SpellCursor + 1) % N; Sfx(ESfx::MenuMove); }
			if (bEdgeLeft)  { SpellCursor = FMath::Max(0, SpellCursor - 10); Sfx(ESfx::MenuMove); }
			if (bEdgeRight) { SpellCursor = FMath::Min(N - 1, SpellCursor + 10); Sfx(ESfx::MenuMove); }
			if (bEdgeStart && StateTimer > 10 && SpellEntries().IsValidIndex(SpellCursor))
			{
				Sfx(ESfx::MenuOk);
				const FSpellEntry& E = SpellEntries()[SpellCursor];
				bSpellPractice = true;
				bPractice = true;
				bExtra = E.Stage >= ExtraStage;
				bIf = E.Stage >= IfStage;
				PracticeStage = E.Stage;
				PracticeDef = E.Def;
				PracticePhase = E.Phase;
				if (bExtra) { Difficulty = NumDifficulties; State = EDanmakuState::CharSelect; }
				else { if (Difficulty >= NumDifficulties) Difficulty = 1; State = EDanmakuState::DiffSelect; }
				StateTimer = 0;
			}
			else if (bEdgeBomb) { Sfx(ESfx::MenuBack); bSpellPractice = false; State = EDanmakuState::Title; StateTimer = 0; }
			break;
		}
		case EDanmakuState::DiffSelect:
			if (bDemo)
			{
				if (StateTimer % 40 == 0) bEdgeDown = true;
				if (StateTimer > 4 * 40 + 20 && Difficulty == DemoDifficulty) bEdgeStart = true;
			}
			if (bEdgeUp)   { Difficulty = (Difficulty + NumDifficulties - 1) % NumDifficulties; Sfx(ESfx::MenuMove); }
			if (bEdgeDown) { Difficulty = (Difficulty + 1) % NumDifficulties; Sfx(ESfx::MenuMove); }
			if (bEdgeStart && StateTimer > 10) { Sfx(ESfx::MenuOk); State = EDanmakuState::CharSelect; StateTimer = 0; }
			else if (bEdgeBomb) { Sfx(ESfx::MenuBack); State = bSpellPractice ? EDanmakuState::SpellSelect : (bPractice ? EDanmakuState::StageSelect : EDanmakuState::Title); StateTimer = 0; }
			break;
		case EDanmakuState::CharSelect:
			if (bDemo)
			{
				// attract mode: walk the cursor across the roster, then start
				if (StateTimer % 45 == 0) bEdgeRight = true;
				if (StateTimer > 4 * 45 + 20 && SelectedCharacter == DemoCharacter) bEdgeStart = true;
			}
			if (bEdgeLeft)  { SelectedCharacter = (SelectedCharacter + NumCharacters - 1) % NumCharacters; Sfx(ESfx::MenuMove); }
			if (bEdgeRight) { SelectedCharacter = (SelectedCharacter + 1) % NumCharacters; Sfx(ESfx::MenuMove); }
			if (bEdgeStart && StateTimer > 10) { Sfx(ESfx::MenuOk); StartGame(ECharacter(SelectedCharacter)); }
			else if (bEdgeBomb)
			{
				Sfx(ESfx::MenuBack);
				State = bSpellPractice && bExtra ? EDanmakuState::SpellSelect : (bExtra ? EDanmakuState::Title : EDanmakuState::DiffSelect);
				StateTimer = 0;
			}
			break;
		case EDanmakuState::Playing:
			UpdatePlaying();
			break;
		case EDanmakuState::StageClear:
			UpdateParticles();
			if ((StateTimer > 40 && bEdgeStart) || StateTimer > 240) StartStage(StageIndex + 1);
			break;
		case EDanmakuState::GameOver:
		case EDanmakuState::Clear:
			UpdateParticles();
			if (StateTimer > 60 && bEdgeRestart) StartGame(Player.Character);
			else if (StateTimer > 60 && bEdgeStart) { State = bSpellPractice ? EDanmakuState::SpellSelect : EDanmakuState::CharSelect; StateTimer = 0; }
			break;
		}
	}

	if (bDemo)
	{
		if (bSweep && !bBotDone && State == EDanmakuState::Clear) BotSweepNext();
		else if (bBot && !bBotDone && (State == EDanmakuState::GameOver || State == EDanmakuState::Clear))
		{
			bBotDone = true;
			BotFinish();
			DemoQuitTimer = TurboSteps > 0 ? 2 : 480;
		}
		if (DemoShotEvery > 0 && DemoShotsTaken < DemoShotCount && Frame > 0 && Frame % DemoShotEvery == 0)
		{
			const FString Path = FPaths::ConvertRelativePathToFull(
				FPaths::ProjectSavedDir() / FString::Printf(TEXT("DemoShots/shot_%02d.png"), DemoShotsTaken));
			FScreenshotRequest::RequestScreenshot(Path, false, false);
			if (++DemoShotsTaken >= DemoShotCount) DemoQuitTimer = 30;
		}
		if (DemoQuitTimer > 0 && --DemoQuitTimer == 0)
		{
			FPlatformMisc::RequestExit(false);
		}
	}

	bEdgeBomb = bEdgeStart = bEdgePause = bEdgeRestart = bEdgeLeft = bEdgeRight = bEdgeUp = bEdgeDown = false;
}

void ADanmakuWorld::StartGame(ECharacter Character)
{
	bPaused = false;

	Player = FDanmakuPlayer();
	Player.Character = Character;
	Player.Lives = GetDifficultyInfo(Difficulty).ExtraLives;
	BotLastLives = Player.Lives;
	Player.LifePieces = 0;
	Player.BombPieces = 0;
	NextExtend = 0;
	NextGrazeReward = GrazePerBombPiece;
	PopupTimer = 0;
	Player.Invuln = 60;
	for (int32 i = 0; i < NumDolls; ++i) Player.Dolls[i] = Player.Pos + FVector2f(0.f, 15.f + i * 12.f);
	SelectedCharacter = int32(Character);

	Boss = FBossState();
	DialogueIndex = -1;
	bEnding = false;
	Bullets.Reset();
	NewBullets.Reset();
	Shots.Reset();
	Enemies.Reset();
	Items.Reset();
	Particles.Reset();
	Score = 0;
	Graze = 0;
	SpellsCaptured = 0;
	SpellsTotal = 0;
	BombTime = 0;
	if (bSpellPractice)
	{
		StartStage(PracticeStage);
		SpellsTotal = 1;
		bMidboss = false;
		for (const FSpellEntry& E : SpellEntries()) if (E.Def == PracticeDef && E.Phase == PracticePhase) bMidboss = E.bMid;
		StartBoss();
		return;
	}
	StartStage(bExtra ? (bIf ? IfStage : ExtraStage) : (bPractice ? PracticeStage : 0));
}

void ADanmakuWorld::StartStage(int32 Stage)
{
	StageIndex = Stage;
	State = EDanmakuState::Playing;
	StateTimer = 0;
	StageFrame = bDemo ? DemoSkipFrames : 0;
	Boss = FBossState();
	DialogueIndex = -1;
	bEnding = false;
	Bullets.Reset();
	NewBullets.Reset();
	Shots.Reset();
	Enemies.Reset();
	Items.Reset();
	Beams.Reset();
	FogRadius = 0.f;
	BombTime = 0;
	bMidboss = false;
	bMidDone = false;
	if (const FBossDef* Mid = MidbossDef())
	{
		for (const FPhaseDef& P : Mid->Phases) if (P.bSpell && P.MinDifficulty <= Difficulty) ++SpellsTotal;
	}
	Player.Invuln = FMath::Max(Player.Invuln, 60);
	for (const FPhaseDef& P : Phases())
	{
		if (P.bSpell && P.MinDifficulty <= Difficulty) ++SpellsTotal;
	}
	ShowMessage(Tr(BossDef().StageTitle), Tr(BossDef().StageSub), 150);
}

void ADanmakuWorld::ShowMessage(const FString& Main, const FString& Sub, int32 Frames)
{
	Message = Main;
	SubMessage = Sub;
	MessageTimer = Frames;
	MessageDuration = FMath::Max(1, Frames);
}

void ADanmakuWorld::UpdatePlaying()
{
	if (MessageTimer > 0) --MessageTimer;
	if (bBot) UpdateBot();
	else if (bDemo) UpdateDemo();

	// Haru's bomb slows every enemy thing to half speed: they only advance on even frames.
	const bool bEnemyStep = !(IsTimeSlowed() && (Frame % 2 == 1));
	if (bEnemyStep && !Boss.bStarted) ++StageFrame;

	UpdatePlayer();
	UpdateBomb();
	if (bEnemyStep) UpdateStage();
	UpdateBoss(bEnemyStep);
	if (bEnding) UpdateEnding();
	if (bEnemyStep) UpdateEnemies();
	UpdateShots();
	UpdateBullets(bEnemyStep);
	UpdateBeams(bEnemyStep);
	UpdateRecovery();
	UpdateItems();
	UpdateParticles();

	Bullets.Append(NewBullets);
	NewBullets.Reset();
	HiScore = FMath::Max(HiScore, Score);
}

void ADanmakuWorld::UpdateDemo()
{
	CurInput.bShoot = true;
	CurInput.bFocus = (Frame / 300) % 2 == 1;
	const FVector2f Target(FieldW * 0.5f + FMath::Sin(Frame * 0.013f) * 120.f, 380.f);
	const FVector2f D = Target - Player.Pos;
	CurInput.Move = FVector2f(FMath::Clamp(D.X * 0.2f, -1.f, 1.f), FMath::Clamp(D.Y * 0.2f, -1.f, 1.f));
	if (DemoBombAt > 0 && StateTimer == DemoBombAt) bEdgeBomb = true;
}

// =====================================================================================
// Player
// =====================================================================================

bool ADanmakuWorld::CanBeHit() const
{
	return (!bDemo || bDemoHits) && State == EDanmakuState::Playing && Player.Invuln == 0 && Player.DeathTimer == 0
		&& Player.RespawnTimer == 0 && (BombTime == 0 || BombKind == ECharacter::Haru);
}

void ADanmakuWorld::UpdatePlayer()
{
	FDanmakuPlayer& P = Player;
	P.bShooting = false;
	P.bLaserOn = false;

	if (P.DeathTimer > 0)
	{
		if (bEdgeBomb && P.Bombs > 0)
		{
			P.DeathTimer = 0;
			TriggerBomb();
		}
		else if (--P.DeathTimer == 0)
		{
			KillPlayer();
		}
		return;
	}

	const FVector2f Prev = P.Pos;
	if (P.RespawnTimer > 0)
	{
		--P.RespawnTimer;
		P.Pos.Y = FMath::Lerp(FieldH - 48.f, FieldH + 24.f, P.RespawnTimer / 40.f);
		if (P.Invuln > 0) --P.Invuln;
	}
	else
	{
		if (P.Invuln > 0) --P.Invuln;
		P.bFocus = CurInput.bFocus;
		const FCharacterInfo& CI = GetCharacterInfo(P.Character);
		float Speed = P.bFocus ? CI.FocusSpeed : CI.Speed;
		// 蒼 runs faster while his bow is lowered
		if (P.Character == ECharacter::Sou && !CurInput.bShoot) Speed *= 1.5f;
		P.Pos += CurInput.Move * Speed;
		P.Pos.X = FMath::Clamp(P.Pos.X, 8.f, FieldW - 8.f);
		P.Pos.Y = FMath::Clamp(P.Pos.Y, 16.f, FieldH - 16.f);
	}

	if (CurInput.Move.X < -0.3f)      P.Lean = FMath::Max(-12, P.Lean - 1);
	else if (CurInput.Move.X > 0.3f)  P.Lean = FMath::Min(12, P.Lean + 1);
	else                              P.Lean -= FMath::Sign(P.Lean) * FMath::Min(2, FMath::Abs(P.Lean));

	P.Trail.Insert(P.Pos, 0);
	if (P.Trail.Num() > 80) P.Trail.SetNum(80);
	if (P.Character == ECharacter::Yuki)
	{
		UpdateDolls(P.Pos - Prev);
		// lost puppets come back one at a time, a minute apart
		if (P.DollsAlive < NumDolls && --P.DollRespawn <= 0)
		{
			P.Dolls[P.DollsAlive] = P.Pos;
			++P.DollsAlive;
			P.DollRespawn = P.DollsAlive < NumDolls ? DollRespawnFrames : 0;
			Popup(FString(Tr(TEXT("式神復活"))) + FString::Printf(TEXT("　%d/%d"), P.DollsAlive, NumDolls), FLinearColor(0.9f, 0.75f, 1.f));
			AddParticle(P.Pos, FVector2f::ZeroVector, 80.f, FLinearColor(0.9f, 0.7f, 1.f), 24, true);
		}
	}

	if (P.RespawnTimer > 0) return;

	if (bEdgeBomb) TriggerBomb();

	if (P.ShotCooldown > 0) --P.ShotCooldown;
	if (CurInput.bShoot && !IsInDialogue())
	{
		P.bShooting = true;
		if (P.Character == ECharacter::Sou)
		{
			UpdateLaser();
		}
		else if (P.ShotCooldown == 0)
		{
			FirePlayerShots();
			P.ShotCooldown = P.Character == ECharacter::Ren ? 5 : 6;
		}
	}
}

void ADanmakuWorld::UpdateDolls(const FVector2f& Delta)
{
	FDanmakuPlayer& P = Player;
	for (int32 i = 0; i < NumDolls; ++i)
	{
		if (P.bFocus)
		{
			// focus locks the formation: puppets move rigidly with the player
			P.Dolls[i] += Delta;
		}
		else
		{
			const int32 Idx = FMath::Min(P.Trail.Num() - 1, (i + 1) * 7);
			const FVector2f Target = (Idx >= 0 ? P.Trail[Idx] : P.Pos) + FVector2f(0.f, 12.f + i * 2.f);
			P.Dolls[i] = FMath::Lerp(P.Dolls[i], Target, 0.25f);
		}
	}
}

void ADanmakuWorld::AddShot(const FVector2f& Pos, float AngleDeg, float Speed, float Damage, EShotType Type)
{
	FShot& S = Shots.AddDefaulted_GetRef();
	S.Pos = Pos;
	S.Vel = Dir(AngleDeg) * Speed;
	S.Damage = Damage;
	S.Type = Type;
}

void ADanmakuWorld::FirePlayerShots()
{
	Sfx(ESfx::Shot);
	const FDanmakuPlayer& P = Player;
	const float PM = PowerMul();
	switch (P.Character)
	{
	case ECharacter::Ren:
	{
		// five-talisman fan; focus narrows it
		const float Spread = P.bFocus ? 2.5f : 7.f;
		for (int32 i = -2; i <= 2; ++i)
		{
			AddShot(P.Pos + FVector2f(i * 3.f, -10.f), -90.f + i * Spread, 15.f, 1.6f * PM, EShotType::Talisman);
		}
		break;
	}
	case ECharacter::Haru:
		AddShot(P.Pos + FVector2f(-7.f, -10.f), -90.f, 15.f, 3.f * PM, EShotType::Straight);
		AddShot(P.Pos + FVector2f(7.f, -10.f), -90.f, 15.f, 3.f * PM, EShotType::Straight);
		if ((Frame / 6) % 2 == 0)
		{
			AddShot(P.Pos + FVector2f(0.f, -6.f), -90.f + Rng.FRandRange(-35.f, 35.f), 8.f, 3.f * PM, EShotType::Homing);
		}
		break;
	case ECharacter::Yuki:
	{
		// with all four puppets the puppets hit 20% harder and Yuki himself 20% softer
		const bool bFull = P.DollsAlive >= NumDolls;
		AddShot(P.Pos + FVector2f(0.f, -10.f), -90.f, 15.f, 1.4f * PM * (bFull ? 0.8f : 1.f), EShotType::Straight);
		for (int32 i = 0; i < P.DollsAlive; ++i)
		{
			AddShot(P.Dolls[i] + FVector2f(0.f, -6.f), -90.f, 13.f, 1.4f * PM * (bFull ? 1.2f : 1.f), EShotType::Doll);
		}
		break;
	}
	case ECharacter::Sou:
		break;   // laser, see UpdateLaser
	}
}

void ADanmakuWorld::UpdateLaser()
{
	FDanmakuPlayer& P = Player;
	P.bLaserOn = true;
	const float Dmg = 1.4f * PowerMul();
	for (FEnemy& E : Enemies)
	{
		if (E.bDead || E.Pos.Y > P.Pos.Y || E.Pos.Y < -8.f) continue;
		if (FMath::Abs(E.Pos.X - P.Pos.X) < LaserHalfWidth + E.Radius)
		{
			E.HP -= Dmg;
			E.HitFlash = 2;
			if (Frame % 4 == 0) AddParticle(E.Pos, FVector2f(Rng.FRandRange(-1.f, 1.f), -1.f), 8.f, ColLightBlue, 10);
		}
	}
	FVector2f Tg[4];
	const int32 NT = BossTargets(Tg);
	bool bLaserHits = false;
	for (int32 i = 0; i < NT; ++i) bLaserHits |= Tg[i].Y < P.Pos.Y && FMath::Abs(Tg[i].X - P.Pos.X) < LaserHalfWidth + 24.f;
	if (Boss.bActive && Boss.bVulnerable && bLaserHits)
	{
		DamageBoss(Dmg);
		Score += 4;
		if (Frame % 3 == 0) AddParticle(Boss.Pos + FVector2f(Rng.FRandRange(-10.f, 10.f), 10.f), FVector2f(Rng.FRandRange(-2.f, 2.f), -1.f), 10.f, ColLightBlue, 12);
	}
}

void ADanmakuWorld::DamageBoss(float Amount)
{
	if (!Boss.bActive || !Boss.bVulnerable || IsSurvivalPhase() || Boss.ShieldTimer > 0) return;
	Boss.HP -= Amount;
	Boss.HitFlash = 2;
}

void ADanmakuWorld::TriggerBomb()
{
	if (Player.Bombs <= 0 || BombTime > 0) return;
	--Player.Bombs;
	StartBomb();
}

void ADanmakuWorld::StartBomb()
{
	Sfx(ESfx::Bomb);
	BombKind = Player.Character;
	BombDuration = BombLength(BombKind);
	BombTime = BombDuration;
	BombCenter = Player.Pos;
	BombRadius = 0.f;
	bBombHitBoss = false;
	// Haru's slow is not an invincibility bomb: only a short grace period
	Player.Invuln = FMath::Max(Player.Invuln, BombKind == ECharacter::Haru ? 90 : BombDuration + 60);
	if (Boss.bSpell) Boss.bBonusValid = false;
	if (IsBombImmunePhase() && BombKind != ECharacter::Haru) Popup(TEXT("最終符卡：Bomb 傷害無效"), FLinearColor(1.f, 0.6f, 0.6f));
	AddParticle(Player.Pos, FVector2f::ZeroVector, 300.f, GetCharacterInfo(BombKind).Color, 40, true);
}

void ADanmakuWorld::UpdateBomb()
{
	if (BombTime <= 0) return;
	--BombTime;
	const float T = 1.f - float(BombTime) / float(BombDuration);
	BombCenter = Player.Pos;

	switch (BombKind)
	{
	case ECharacter::Ren:
	{
		// barrier shockwave: expanding ring that erases bullets and hits the boss once, hard
		BombRadius = FMath::Lerp(20.f, 560.f, FMath::Min(1.f, T * 1.3f));
		const float R2 = Sq(BombRadius);
		for (FBullet& B : Bullets)
		{
			if (!B.bDead && !B.bBombProof && DistSq(B.Pos, BombCenter) < R2) { B.bDead = true; SpawnItem(EItemType::Star, B.Pos, true); }
		}
		for (FEnemy& E : Enemies)
		{
			if (!E.bDead && DistSq(E.Pos, BombCenter) < R2) { E.HP -= 3.f; E.HitFlash = 2; }
		}
		FVector2f Tg[4];
		const int32 NT = BossTargets(Tg);
		bool bInRange = false;
		for (int32 i = 0; i < NT; ++i) bInRange |= DistSq(Tg[i], BombCenter) < Sq(BombRadius + 28.f);
		if (!bBombHitBoss && Boss.bActive && Boss.bVulnerable && bInRange)
		{
			if (!IsBombImmunePhase()) DamageBoss(200.f);
			bBombHitBoss = true;
			Burst(Boss.Pos, 50, ColLightBlue, 6.f, 14.f, 40);
		}
		break;
	}
	case ECharacter::Sou:
	{
		// heaven-thunder: a very wide beam straight up, clears its column and keeps burning
		BombRadius = ThunderHalfWidth;
		for (FBullet& B : Bullets)
		{
			if (!B.bDead && !B.bBombProof && B.Pos.Y <= Player.Pos.Y && FMath::Abs(B.Pos.X - Player.Pos.X) <= ThunderHalfWidth + B.Radius)
			{
				B.bDead = true;
				SpawnItem(EItemType::Star, B.Pos, true);
			}
		}
		for (FEnemy& E : Enemies)
		{
			if (!E.bDead && E.Pos.Y <= Player.Pos.Y && FMath::Abs(E.Pos.X - Player.Pos.X) <= ThunderHalfWidth + E.Radius) { E.HP -= 3.f; E.HitFlash = 2; }
		}
		FVector2f Tg[4];
		const int32 NT = BossTargets(Tg);
		bool bInColumn = false;
		for (int32 i = 0; i < NT; ++i) bInColumn |= Tg[i].Y <= Player.Pos.Y && FMath::Abs(Tg[i].X - Player.Pos.X) <= ThunderHalfWidth + 24.f;
		if (Boss.bActive && !IsBombImmunePhase() && bInColumn)
		{
			DamageBoss(1.1f);
		}
		break;
	}
	case ECharacter::Haru:
		// time-slow barrier: handled through IsTimeSlowed()
		BombRadius = 60.f;
		break;
	case ECharacter::Yuki:
		// puppet storm: every puppet sprays a fan of shots
		if (BombTime % 6 == 0)
		{
			for (int32 i = 0; i < FMath::Max(1, Player.DollsAlive); ++i)
			{
				const FVector2f From = Player.DollsAlive > 0 ? Player.Dolls[i] : Player.Pos;
				const int32 Count = Rng.RandRange(5, 8);
				for (int32 k = 0; k < Count; ++k)
				{
					AddShot(From, -90.f + Rng.FRandRange(-22.f, 22.f), Rng.FRandRange(8.f, 11.f), 0.55f * PowerMul(), EShotType::Doll);
					Shots.Last().bFromBomb = true;
				}
			}
		}
		break;
	}
}

void ADanmakuWorld::PlayerHit()
{
	if (!CanBeHit()) return;
	if (bMeasure)
	{
		// measuring difficulty: count the hit, then a short grace period, no death
		++MeasureHits;
		Player.Invuln = 30;
		return;
	}
	if (Player.Character == ECharacter::Yuki && BombTime == 0)
	{
		// 雪 bombs by reflex: it costs two bombs, and puppets pay for whatever bombs he lacks
		const int32 FromBombs = FMath::Min(Player.Bombs, 2);
		const int32 FromDolls = 2 - FromBombs;
		if (FromDolls <= Player.DollsAlive)
		{
			Player.Bombs -= FromBombs;
			if (FromDolls > 0)
			{
				for (int32 i = 0; i < FromDolls; ++i) Burst(Player.Dolls[Player.DollsAlive - 1 - i], 20, FLinearColor(0.9f, 0.7f, 1.f), 3.f, 10.f, 30);
				Player.DollsAlive -= FromDolls;
				if (Player.DollRespawn <= 0) Player.DollRespawn = DollRespawnFrames;
			}
			if (Boss.bSpell) Boss.bBonusValid = false;
			StartBomb();
			Popup(FromDolls > 0 ? FString::Printf(TEXT("%s　-%d"), Tr(TEXT("自動 Bomb：式神代替")), FromDolls) : FString(Tr(TEXT("自動 Bomb"))), FLinearColor(0.9f, 0.75f, 1.f));
			return;
		}
	}
	Player.DeathTimer = DeathBombWindow;
	Sfx(ESfx::PlayerDie);
	if (Boss.bSpell) Boss.bBonusValid = false;
	Burst(Player.Pos, 24, ColRed, 4.f, 10.f, 30);
}

void ADanmakuWorld::KillPlayer()
{
	--Player.Lives;
	Burst(Player.Pos, 60, ColPink, 5.f, 12.f, 50);
	AddParticle(Player.Pos, FVector2f::ZeroVector, 200.f, ColWhite, 30, true);

	SpawnItem(EItemType::BigPower, Player.Pos + FVector2f(0.f, -20.f), false);
	for (int32 i = 0; i < 4; ++i)
	{
		SpawnItem(EItemType::Power, Player.Pos + FVector2f(Rng.FRandRange(-40.f, 40.f), -30.f), false);
	}
	Player.Power = FMath::Max(1.f, Player.Power - 1.f);
	CancelAllBullets(false, true);   // bomb-proof bullets also survive the death clear

	if (Player.Lives < 0)
	{
		Player.Lives = 0;
		State = EDanmakuState::GameOver;
		StateTimer = 0;
		return;
	}

	Player.Bombs = FMath::Max(Player.Bombs, 3);
	Player.Invuln = 200;
	Player.RespawnTimer = 40;
	Player.Pos = FVector2f(FieldW * 0.5f, FieldH + 24.f);
}

// =====================================================================================
// Stage timeline & small enemies
// =====================================================================================

FEnemy& ADanmakuWorld::SpawnEnemy(const FVector2f& Pos, const FVector2f& Vel, int32 Kind, float HP, int32 Pattern,
	int32 StopAt, int32 LeaveAt, const FVector2f& LeaveVel, int32 DropPower, int32 DropPoint)
{
	FEnemy& E = Enemies.AddDefaulted_GetRef();
	E.Pos = Pos;
	E.Vel = Vel;
	E.Kind = Kind;
	// toughness: stage 1 x1, stage 2 x1.1, stage 3 onward (Extra and Extra-IF too) x1.2 — later roads get harder through density, not HP
	E.HP = HP * (StageIndex == 0 ? 1.f : (StageIndex == 1 ? 1.1f : 1.2f));
	E.Pattern = Pattern;
	E.StopAt = StopAt;
	E.LeaveAt = LeaveAt;
	E.LeaveVel = LeaveVel;
	E.DropPower = DropPower;
	E.DropPoint = DropPoint;
	switch (Kind)
	{
	case 1:  E.Radius = 12.f; E.Color = FLinearColor(1.f, 0.35f, 0.35f); break;
	case 2:  E.Radius = 22.f; E.Color = FLinearColor(0.7f, 0.4f, 1.f); break;
	default: E.Radius = 10.f; E.Color = FLinearColor(0.35f, 0.6f, 1.f); break;
	}
	// a fox mask (spirit) fights alone: when one arrives the others vanish, and nothing else spawns while one is on screen
	if (Kind == 2)
	{
		for (int32 i = 0; i < Enemies.Num() - 1; ++i)
		{
			FEnemy& O = Enemies[i];
			if (O.bDead || O.Kind == 2) continue;
			O.bDead = true;
			AddParticle(O.Pos, FVector2f::ZeroVector, 30.f, O.Color, 14, true);
		}
	}
	else
	{
		for (int32 i = 0; i < Enemies.Num() - 1; ++i)
		{
			if (!Enemies[i].bDead && Enemies[i].Kind == 2) { E.bDead = true; break; }
		}
	}
	return Enemies.Last();
}

void ADanmakuWorld::UpdateEnemies()
{
	for (FEnemy& E : Enemies)
	{
		if (E.bDead) continue;
		++E.Age;
		if (E.HitFlash > 0) --E.HitFlash;
		if (E.Age == E.StopAt) E.Vel = FVector2f::ZeroVector;
		if (E.Age == E.LeaveAt) E.Vel = E.LeaveVel;
		E.Vel += E.Acc;
		E.Pos += E.Vel;
		if (E.SineAmp != 0.f) E.Pos.X += FMath::Sin(E.Age * E.SineFreq + E.SinePhase) * E.SineAmp;

		if (E.HP <= 0.f)
		{
			KillEnemy(E);
			continue;
		}
		if (E.Age > 30 && !InField(E.Pos, 48.f))
		{
			E.bDead = true;
			continue;
		}
		if (InField(E.Pos, -8.f))
		{
			EnemyFire(E);
		}
		if (CanBeHit() && DistSq(E.Pos, Player.Pos) < Sq(E.Radius * 0.6f + HitRadius()))
		{
			PlayerHit();
		}
	}
	Enemies.RemoveAllSwap([](const FEnemy& E) { return E.bDead; });
}

void ADanmakuWorld::KillEnemy(FEnemy& E)
{
	E.bDead = true;
	Sfx(ESfx::EnemyDie);
	Score += E.Kind == 2 ? 30000 : (E.Kind == 1 ? 5000 : 1000);
	Burst(E.Pos, E.Kind == 2 ? 40 : 14, E.Color, 3.f, 10.f, 30);
	AddParticle(E.Pos, FVector2f::ZeroVector, E.Radius * 5.f, E.Color, 20, true);
	if (E.bRevenge && StageIndex < ExtraStage)
	{
		if (E.Kind == 0) Fan(E.Pos, 3, 30.f, 1.6f, AimAngle(E.Pos), EBulletShape::Small, FLinearColor(1.f, 0.96f, 0.85f));
		else Ring(E.Pos, 8, 1.3f, AimAngle(E.Pos), EBulletShape::Small, FLinearColor(1.f, 0.96f, 0.85f));
	}
	else if (E.bRevenge)
	{
		const int32 N = Cnt(E.Kind == 0 ? 6 : 12);
		const float Base = Rng.FRandRange(0.f, 360.f);
		for (int32 i = 0; i < N; ++i) Fire(E.Pos, 1.25f, Base + 360.f * i / N, EBulletShape::Small, FLinearColor(1.f, 0.96f, 0.85f));
	}
	if (E.Kind == 2) SpawnItem(EItemType::BombPiece, E.Pos, false);
	for (int32 i = 0; i < E.DropPower; ++i) SpawnItem(EItemType::Power, E.Pos + FVector2f(Rng.FRandRange(-16.f, 16.f), Rng.FRandRange(-8.f, 8.f)), false);
	for (int32 i = 0; i < E.DropPoint; ++i) SpawnItem(EItemType::Point, E.Pos + FVector2f(Rng.FRandRange(-16.f, 16.f), Rng.FRandRange(-8.f, 8.f)), false);
}

// =====================================================================================
// Boss
// =====================================================================================

void ADanmakuWorld::StartBoss()
{
	CancelAllBullets(true);
	Boss = FBossState();
	Boss.bActive = true;
	Boss.bStarted = true;
	Boss.Pos = FVector2f(FieldW * 0.5f, -40.f);
	MoveBossTo(FVector2f(FieldW * 0.5f, 110.f), 90);
	Boss.EntryTimer = 120;
	ShowMessage(Tr(TEXT("警告")), GetBossTitle() + TEXT("　") + GetBossName(), 120);
}

void ADanmakuWorld::StartMidboss()
{
	bMidboss = true;
	StartBoss();
}

void ADanmakuWorld::MoveBossTo(const FVector2f& Target, int32 Frames)
{
	Boss.MoveFrom = Boss.Pos;
	Boss.MoveTo = Target;
	Boss.MoveTime = 0;
	Boss.MoveDur = FMath::Max(1, Frames);
}

void ADanmakuWorld::WanderBoss()
{
	const float X = FMath::Clamp(Boss.Pos.X + Rng.FRandRange(-90.f, 90.f), 64.f, FieldW - 64.f);
	const float Y = Rng.FRandRange(80.f, 150.f);
	MoveBossTo(FVector2f(X, Y), 80);
}

void ADanmakuWorld::PatrolBoss(int32 T)
{
	if (bComposite) return;
	if (T % 170 == 60) WanderBoss();
}

void ADanmakuWorld::PlaceBoss(const FVector2f& P)
{
	// spell cards drive the boss along a curve; ease toward it so the first frame doesn't jump
	if (bComposite) return;
	Boss.MoveTime = Boss.MoveDur;
	Boss.Pos = FMath::Lerp(Boss.Pos, P, 0.12f);
}

void ADanmakuWorld::UpdateBoss(bool bEnemyStep)
{
	if (!Boss.bActive) return;
	if (Boss.HitFlash > 0) --Boss.HitFlash;
	if (Boss.DepartTimer > 0)
	{
		--Boss.DepartTimer;
		Boss.DepartPos += Boss.DepartVel;
	}
	if (!bEnemyStep)
	{
		Boss.Vel = FVector2f::ZeroVector;
		return;
	}

	const FVector2f PrevPos = Boss.Pos;
	if (Boss.MoveTime < Boss.MoveDur)
	{
		++Boss.MoveTime;
		const float A = float(Boss.MoveTime) / Boss.MoveDur;
		const float Ease = 1.f - Sq(1.f - A);
		Boss.Pos = FMath::Lerp(Boss.MoveFrom, Boss.MoveTo, Ease);
	}

	if (Boss.DefeatTimer > 0)
	{
		if (Frame % 4 == 0)
		{
			Burst(Boss.Pos + FVector2f(Rng.FRandRange(-30.f, 30.f), Rng.FRandRange(-30.f, 30.f)), 8, ColGold, 3.f, 14.f, 30);
		}
		if (--Boss.DefeatTimer == 0)
		{
			Burst(Boss.Pos, 120, ColWhite, 7.f, 16.f, 70);
			AddParticle(Boss.Pos, FVector2f::ZeroVector, 600.f, ColLightBlue, 60, true);
			Boss.bActive = false;
			Sfx(ESfx::EnemyDie);
			Sfx(ESfx::Break);
			if (bSpellPractice)
			{
				State = EDanmakuState::Clear;
				StateTimer = 0;
				return;
			}
			if (bMidboss)
			{
				bMidboss = false;
				bMidDone = true;
				Boss = FBossState();
				ShowMessage(TEXT("……"), StageIndex == IfStage ? Tr(TEXT("凶獸逃向了天宮深處")) : Tr(TEXT("前方，就是天和閣")), 150);
				return;
			}
			if (BossDef().Ending[FMath::Clamp(int32(Player.Character), 0, 3)].Num() > 0)
			{
				// talk with the beaten boss before the stage ends
				bEnding = true;
				DialogueIndex = 0; DialoguePage = 0; DialoguePageCount = 1;
				DialogueTimer = 0;
				return;
			}
			FinishStage();
		}
		Boss.Vel = Boss.Pos - PrevPos;
		return;
	}

	if (Boss.EntryTimer > 0)
	{
		if (--Boss.EntryTimer == 0)
		{
			if (bSpellPractice) BeginPhase(FMath::Clamp(PracticePhase, 0, Phases().Num() - 1));
			else if (bDemo && DemoStartPhase > 0) BeginPhase(FMath::Min(DemoStartPhase, Phases().Num() - 1));
			else { DialogueIndex = 0; DialogueTimer = 0; DialoguePage = 0; DialoguePageCount = 1; }
		}
		Boss.Vel = Boss.Pos - PrevPos;
		return;
	}

	if (DialogueIndex >= 0)
	{
		++DialogueTimer;
		const bool bAdvance = (DialogueTimer > 10 && bEdgeStart) || (bDemo && DialogueTimer > 90);
		if (bAdvance)
		{
			DialogueTimer = 0;
			Sfx(ESfx::MenuMove);
			if (NextDialogueBox() && DialogueIndex >= CurrentDialogue().Num())
			{
				DialogueIndex = -1;
				BeginPhase(NextPhaseFrom(0));
			}
		}
		Boss.Vel = Boss.Pos - PrevPos;
		return;
	}

	if (Boss.GapTimer > 0)
	{
		if (--Boss.GapTimer == 0)
		{
			Boss.bVulnerable = !Phases()[Boss.Phase].bSurvival;
			Boss.PhaseTime = 0;
			// every attackable spell card opens with 3 seconds of invulnerability
			if (Boss.bSpell && Boss.bVulnerable) Boss.ShieldTimer = 180;
		}
		Boss.Vel = Boss.Pos - PrevPos;
		return;
	}

	++Boss.PhaseTime;
	--Boss.TimeLeft;
	if (Boss.ShieldTimer > 0) --Boss.ShieldTimer;
	const FPhaseDef& Def = Phases()[Boss.Phase];
	(this->*Def.Pattern)(Boss.PhaseTime);
	Boss.Vel = Boss.Pos - PrevPos;

	{
		FVector2f Tg[4];
		const int32 NT = BossTargets(Tg);
		for (int32 i = 0; i < NT; ++i)
		{
			if (CanBeHit() && DistSq(Tg[i], Player.Pos) < Sq(20.f + HitRadius())) PlayerHit();
		}
	}
	if (Boss.HP <= 0.f || Boss.TimeLeft <= 0)
	{
		EndPhase(Boss.HP <= 0.f || Def.bSurvival);   // surviving an endurance card counts as beating it
	}
}

void ADanmakuWorld::FinishStage()
{
	const bool bMoreStages = !bPractice && !bExtra && StageIndex + 1 < GetNumStages();
	StageBonus = 2000000LL * (StageIndex + 1) + int64(Player.Lives) * 1000000 + int64(Player.Bombs) * 500000 + int64(Graze) * 1000;
	Score += StageBonus;
	HiScore = FMath::Max(HiScore, Score);
	State = bMoreStages ? EDanmakuState::StageClear : EDanmakuState::Clear;
	StateTimer = 0;
}

bool ADanmakuWorld::NextDialogueBox()
{
	if (DialoguePage + 1 < DialoguePageCount)
	{
		++DialoguePage;
		return false;
	}
	DialoguePage = 0;
	DialoguePageCount = 1;
	++DialogueIndex;
	return true;
}

void ADanmakuWorld::UpdateEnding()
{
	++DialogueTimer;
	const bool bAdvance = (DialogueTimer > 10 && bEdgeStart) || (bDemo && DialogueTimer > 90);
	if (!bAdvance) return;
	DialogueTimer = 0;
	Sfx(ESfx::MenuMove);
	if (NextDialogueBox() && DialogueIndex >= CurrentDialogue().Num())
	{
		DialogueIndex = -1;
		bEnding = false;
		FinishStage();
	}
}

void ADanmakuWorld::BeginPhase(int32 Index)
{
	const FPhaseDef& Def = Phases()[Index];
	FogRadius = 0.f;
	Boss.Phase = Index;
	Boss.HP = Boss.MaxHP = Def.HP * (bDemo ? DemoHPScale : 1.f);
	Boss.TimeLeft = Boss.TimeTotal = Def.Seconds * 60;
	Boss.bSpell = Def.bSpell;
	Boss.SpellName = Tr(Def.Name);
	Boss.bBonusValid = true;
	Boss.GapTimer = 60;
	Boss.bVulnerable = false;
	Boss.PhaseTime = 0;
	Boss.PatternTimer = 0;
	Boss.Stage6Beasts = 0;
	Boss.ShieldTimer = 0;
	FMemory::Memzero(Boss.JoinFrame);
	const int32 NewActor = Def.Actor;
	if (NewActor != Boss.Actor && Boss.Actor >= 0)
	{
		// relay: the beast that just fought flies off to one side, the next one swoops in from the other
		if (Boss.Actor < 4)
		{
			Boss.DepartActor = Boss.Actor;
			Boss.DepartPos = Boss.Pos;
			Boss.DepartVel = FVector2f(-Boss.EntrySide * 3.2f, -1.2f);
			Boss.DepartTimer = 90;
		}
		if (NewActor < 4) Boss.Pos = FVector2f(Boss.EntrySide > 0 ? FieldW + 50.f : -50.f, 70.f);
		else
		{
			static const FVector2f Corners[4] = { { -40.f, -40.f }, { FieldW + 40.f, -40.f }, { -40.f, 220.f }, { FieldW + 40.f, 220.f } };
			for (int32 i = 0; i < 4; ++i) Boss.QuadPos[i] = Corners[i];
			Boss.Pos = FVector2f(FieldW * 0.5f, 110.f);
		}
		Boss.EntrySide = -Boss.EntrySide;
		Boss.GapTimer = 90;
		ShowMessage(NewActor < 4 ? FString(BeastName(NewActor)) : FString(Tr(TEXT("四凶齊聚"))), FString(), 90);
	}
	if (NewActor == 4 && Boss.Actor < 0)
	{
		static const FVector2f Corners[4] = { { -40.f, -40.f }, { FieldW + 40.f, -40.f }, { -40.f, 220.f }, { FieldW + 40.f, 220.f } };
		for (int32 i = 0; i < 4; ++i) Boss.QuadPos[i] = Corners[i];
	}
	Boss.Actor = NewActor;
	MoveBossTo(FVector2f(FieldW * 0.5f, 110.f), 50);
	if (Def.bSpell)
	{
		Sfx(ESfx::Spell);
		AddParticle(Boss.Pos, FVector2f::ZeroVector, 400.f, ColGold, 45, true);
	}
}

void ADanmakuWorld::EndPhase(bool bDefeated)
{
	const FPhaseDef& Def = Phases()[Boss.Phase];
	Sfx(ESfx::Break);
	FogRadius = 0.f;
	CancelAllBullets(true);
	Burst(Boss.Pos, 40, ColWhite, 5.f, 12.f, 40);
	AddParticle(Boss.Pos, FVector2f::ZeroVector, 300.f, ColLightBlue, 30, true);

	if (Def.bSpell)
	{
		if (bDefeated && Boss.bBonusValid)
		{
			const int64 Bonus = GetCurrentSpellBonus();
			Score += Bonus;
			++SpellsCaptured;
			Sfx(ESfx::SpellGet);
			ShowMessage(Tr(TEXT("符卡收取！！")), FString(Tr(TEXT("獎勵"))) + FString::Printf(TEXT(" %lld"), Bonus), 150);
		}
		else
		{
			ShowMessage(TEXT("收取失敗……"), FString(), 120);
		}
	}

	if (Def.bSpell && bDefeated && Boss.bBonusValid) SpawnItem(EItemType::LifePiece, Boss.Pos, true);
	else if (!Def.bSpell && bDefeated) SpawnItem(EItemType::BombPiece, Boss.Pos, true);
	for (int32 i = 0; i < 4; ++i) SpawnItem(EItemType::Power, Boss.Pos + FVector2f(Rng.FRandRange(-30.f, 30.f), Rng.FRandRange(-20.f, 20.f)), true);
	for (int32 i = 0; i < 6; ++i) SpawnItem(EItemType::Point, Boss.Pos + FVector2f(Rng.FRandRange(-30.f, 30.f), Rng.FRandRange(-20.f, 20.f)), true);

	const int32 Next = bSpellPractice ? Phases().Num() : NextPhaseFrom(Boss.Phase + 1);
	if (Next < Phases().Num())
	{
		BeginPhase(Next);
	}
	else
	{
		Boss.bVulnerable = false;
		Boss.bSpell = false;
		Boss.SpellName.Reset();
		Boss.DefeatTimer = bSpellPractice ? 90 : 150;
	}
}

// ---------- Nonspells ----------

void ADanmakuWorld::Pattern_NonSpell1(int32 T)
{
	if (Every(T, 12, 0)) Ring(Boss.Pos, 16, 2.4f, T * 1.7f, EBulletShape::Small, ColCyan);
	if (Every(T, 12, 6)) Ring(Boss.Pos, 16, 1.9f, -T * 1.3f, EBulletShape::Small, ColBlue);
	if (Every(T, 50, 25)) Fan(Boss.Pos, 7, 44.f, 3.6f, AimAngle(Boss.Pos), EBulletShape::Medium, ColWhite);
	if (Every(T, 180, 90)) WanderBoss();
}

void ADanmakuWorld::Pattern_NonSpell2(int32 T)
{
	if (Every(T, 38, 1))
	{
		const float Base = Rng.FRandRange(0.f, 360.f);
		const int32 N = Cnt(28);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Boss.Pos, 4.f, Base + i * 360.f / N, EBulletShape::Medium, ColOrange);
			B.Accel = -0.08f;
			B.MinSpeed = 0.f;
			B.Event = EBulletEvent::StopThenAim;
			B.EventFrame = 45;
			B.EventParam = 2.6f;
			B.EventParam2 = 15.f + (i % 3) * 8.f;
		}
	}
	if (T % 76 >= 50 && Every(T, 5, 0))
	{
		Fan(Boss.Pos, 7, 70.f, 3.2f, AimAngle(Boss.Pos), EBulletShape::Rice, ColYellow);
	}
	if (Every(T, 150, 75)) WanderBoss();
}

// ---------- 日輪「赫耀天環」 : curving solar rings, slow halo, aimed flares ----------

void ADanmakuWorld::SunRings(int32 T, int32 Interval, int32 Rings, int32 BaseCount)
{
	if (!Every(T, Interval)) return;
	const float Sgn = (T / Interval) % 2 == 0 ? 1.f : -1.f;
	const float Base = T * 0.032f * Sgn * RadToDeg;
	const int32 NR = FMath::Max(1, FMath::RoundToInt(Rings * GetDifficultyInfo(Difficulty).CountMul));
	for (int32 Rg = 0; Rg < NR; ++Rg)
	{
		const int32 Count = Cnt(BaseCount + Rg * 2);
		const float Rad = 9.f + Rg * 8.f;
		for (int32 i = 0; i < Count; ++i)
		{
			const float A = Base + 360.f * i / Count + Rg * 8.6f;
			const FLinearColor C(1.f, 0.62f + Rg * 0.1f, 0.33f);
			FBullet& B = Fire(Boss.Pos + Dir(A) * Rad, (2.6f + Rg * 0.3f) * 0.55f, A + Sgn * (20.f - Rg * 2.9f), EBulletShape::Medium, C);
			B.AngVel = Sgn * (0.52f - Rg * 0.07f);
			B.AngVelDecay = 0.9985f;
			B.Accel = 0.0014f;
			B.MaxSpeed = 2.65f;
		}
	}
}

void ADanmakuWorld::Pattern_Sun(int32 T)
{
	PatrolBoss(T);
	SunRings(T, 18, 2, 8);

	if (Every(T, 90, 0))
	{
		const int32 N = Cnt(24);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Boss.Pos, 1.1f, T * 0.57f + 360.f * i / N, EBulletShape::Medium, FLinearColor(1.f, 0.85f, 0.5f));
			B.AngVel = -0.11f;
			B.AngVelDecay = 0.999f;
			B.Accel = 0.0008f;
			B.MaxSpeed = 1.75f;
		}
	}
	if (Every(T, 75, 45))
	{
		const float Aim = AimAngle(Boss.Pos);
		const int32 JN = Half(2);
		for (int32 j = -JN; j <= JN; ++j)
		{
			FBullet& B = Fire(Boss.Pos, 3.0f, Aim + j * 6.9f, EBulletShape::Comet, FLinearColor(1.f, 0.5f, 0.35f));
			B.Accel = 0.0022f;
			B.MaxSpeed = 3.6f;
		}
	}
}

// ---------- 月蝕「月面反照」 : bouncing moon blades, orbiting moons ----------

void ADanmakuWorld::LunarSideWave(int32 T)
{
	const float Y = 90.f + FMath::Sin(T * 0.05f) * 30.f;
	const int32 N = Cnt(4);
	for (int32 i = 0; i < N; ++i)
	{
		for (int32 Side = 0; Side < 2; ++Side)
		{
			const float Sgn = Side == 0 ? 1.f : -1.f;
			const FVector2f V((3.2f + i * 0.2f) * 0.55f * Sgn, (0.9f + i * 0.16f) * 0.55f);
			const FVector2f P(Side == 0 ? -10.f - i * 7.f : FieldW + 10.f + i * 7.f, Y + i * 8.f);
			FBullet& B = Fire(P, V.Size(), AngleOf(V), EBulletShape::Crescent, ColMoon);
			B.AngVel = Sgn * 1.83f;
			B.Bounces = 1;
			B.Event = EBulletEvent::AimAtPlayer;
			B.EventFrame = 36;
			B.EventParam = 1.76f;
		}
	}
}

void ADanmakuWorld::OrbitingMoons(int32 Count, float Radius, int32 OrbitTime)
{
	Count = Cnt(Count);
	for (int32 i = 0; i < Count; ++i)
	{
		FBullet& B = Fire(Boss.Pos, 0.f, 0.f, EBulletShape::Large, FLinearColor(0.94f, 0.96f, 1.f));
		B.bOrbit = true;
		B.OrbitAngle = 360.f * i / Count;
		B.OrbitRadius = Radius;
		B.OrbitSpeed = (i % 2 == 0 ? 1.f : -1.f) * 2.f;
		B.EventFrame = OrbitTime + i * 3;
		B.EventParam = 2.0f;
	}
}

void ADanmakuWorld::Pattern_Moon(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 26, 0)) LunarSideWave(T);
	if (Every(T, 96, 0)) OrbitingMoons(8, 42.f, 78);
	if (Every(T, 140, 70))
	{
		const float Aim = AimAngle(Boss.Pos);
		const int32 JN = Half(4);
		for (int32 i = -JN; i <= JN; ++i)
		{
			FBullet& B = Fire(Boss.Pos, 1.4f, Aim + i * 10.3f, EBulletShape::Crescent, FLinearColor(0.8f, 0.88f, 1.f));
			B.AngVel = i < 0 ? -3.4f : 3.4f;
			B.Event = EBulletEvent::AimAtPlayer;
			B.EventFrame = 22;
			B.EventParam = 1.76f;
		}
	}
}

// ---------- 星雨「天河崩落」 : splitting star rain, constellation bursts, meteor sweeps ----------

void ADanmakuWorld::StarRain(int32 Num)
{
	Num = Cnt(Num);
	for (int32 i = 0; i < Num; ++i)
	{
		const FVector2f V(Rng.FRandRange(-0.3f, 0.3f), Rng.FRandRange(2.0f, 2.75f));
		FBullet& B = Fire(FVector2f(Rng.FRandRange(14.f, FieldW - 14.f), -10.f - i * 5.f), V.Size(), AngleOf(V), EBulletShape::Star, FLinearColor(0.7f, 0.84f, 1.f));
		B.Event = EBulletEvent::SplitTwo;
		B.EventFrame = 24 + Rng.RandRange(0, 18);
	}
}

void ADanmakuWorld::ConstellationBurst(int32 T)
{
	const float Aim = AimAngle(Boss.Pos);
	const int32 JN = Half(5);
	for (int32 i = -JN; i <= JN; ++i)
	{
		FBullet& B = Fire(Boss.Pos, 2.25f, Aim + i * 5.7f, EBulletShape::Star, FLinearColor(0.9f, 0.96f, 1.f));
		B.Accel = 0.0014f;
		B.MaxSpeed = 3.f;
	}
	const int32 NR = Cnt(8);
	for (int32 i = 0; i < NR; ++i)
	{
		FBullet& B = Fire(Boss.Pos, 1.27f, T * 3.4f + 360.f * i / NR, EBulletShape::Star, FLinearColor(0.63f, 0.78f, 1.f));
		B.Event = EBulletEvent::SplitTwo;
		B.EventFrame = 28;
	}
}

void ADanmakuWorld::MeteorSweep(bool bFromLeft)
{
	const int32 N = Cnt(9);
	for (int32 i = 0; i < N; ++i)
	{
		const FVector2f P(bFromLeft ? -12.f - i * 6.f : FieldW + 12.f + i * 6.f, 65.f + i * 21.f);
		const FVector2f V(bFromLeft ? 3.0f : -3.0f, 0.88f + i * 0.044f);
		FBullet& B = Fire(P, V.Size(), AngleOf(V), EBulletShape::Comet, FLinearColor(1.f, 0.82f, 0.67f));
		B.Accel = 0.0016f;
		B.MaxSpeed = 4.f;
	}
}

void ADanmakuWorld::Pattern_Star(int32 T)
{
	PatrolBoss(T);
	if (Every(T, 9, 0)) StarRain(2);
	// the boss's own aimed constellation volley only on Hard and Lunatic; Easy / Normal just dodge the rain and meteors
	if (Difficulty >= 2 && Every(T, 48, 0)) ConstellationBurst(T);
	if (Every(T, 120, 50)) MeteorSweep(Rng.FRand() < 0.5f);
}

// ---------- 日月星奧義「終夜天象」 : sun + moon + stars together ----------

void ADanmakuWorld::Pattern_Final(int32 T)
{
	PatrolBoss(T);

	if (Every(T, 20, 0))
	{
		const float Sgn = (T / 20) % 2 == 0 ? 1.f : -1.f;
		const float Base = T * 0.03f * Sgn * RadToDeg;
		for (int32 Rg = 0; Rg < 2; ++Rg)
		{
			const int32 Count = Cnt(8 + Rg * 3);
			for (int32 i = 0; i < Count; ++i)
			{
				const float A = Base + 360.f * i / Count;
				FBullet& B = Fire(Boss.Pos + Dir(A) * (11.f + Rg * 10.f), (2.9f + Rg * 0.35f) * 0.55f, A + Sgn * 25.8f, EBulletShape::Medium, FLinearColor(1.f, 0.69f, 0.37f));
				B.AngVel = Sgn * (0.46f - Rg * 0.057f);
				B.AngVelDecay = 0.9988f;
				B.Accel = 0.0011f;
				B.MaxSpeed = 2.65f;
			}
		}
	}
	if (Every(T, 60, 0)) LunarSideWave(T);
	if (Every(T, 10, 0)) StarRain(1);
	if (Every(T, 160, 80))
	{
		const int32 N = Cnt(18);
		for (int32 i = 0; i < N; ++i)
		{
			FBullet& B = Fire(Boss.Pos, (1.8f + (i % 3) * 0.35f) * 0.55f, 360.f * i / N + T * 0.69f, EBulletShape::Star,
				i % 2 == 0 ? FLinearColor(0.96f, 0.96f, 1.f) : ColGold);
			B.AngVel = (i % 2 == 0 ? -1.f : 1.f) * 0.29f;
			B.AngVelDecay = 0.9992f;
			B.Accel = 0.0008f;
			B.MaxSpeed = 2.3f;
		}
	}
	if (Every(T, 260, 130)) OrbitingMoons(8, 48.f, 70);
	if (Every(T, 240, 40)) MeteorSweep(Rng.FRand() < 0.5f);
}

// =====================================================================================
// Bullets
// =====================================================================================

FBullet& ADanmakuWorld::Fire(const FVector2f& Pos, float Speed, float Angle, EBulletShape Shape, const FLinearColor& Color)
{
	Sfx(ESfx::EnemyShot);
	FBullet& B = NewBullets.AddDefaulted_GetRef();
	B.Pos = Pos;
	B.Speed = Speed;
	B.Angle = Angle;
	B.Shape = Shape;
	B.Radius = BulletRadius(Shape);
	B.Color = Color;
	return B;
}

void ADanmakuWorld::Ring(const FVector2f& Pos, int32 Count, float Speed, float BaseAngle, EBulletShape Shape, const FLinearColor& Color, float Accel, float MaxSpeed)
{
	Count = Cnt(Count);
	for (int32 i = 0; i < Count; ++i)
	{
		FBullet& B = Fire(Pos, Speed, BaseAngle + 360.f * i / Count, Shape, Color);
		B.Accel = Accel;
		B.MaxSpeed = MaxSpeed;
	}
}

void ADanmakuWorld::Fan(const FVector2f& Pos, int32 Count, float Spread, float Speed, float CenterAngle, EBulletShape Shape, const FLinearColor& Color)
{
	if (Count > 1)
	{
		const int32 H = Half((Count - 1) / 2);
		Spread *= Count > 1 ? float(H * 2) / float(Count - 1) : 1.f;
		Count = H * 2 + (Count % 2 == 1 ? 1 : 2);
	}
	for (int32 i = 0; i < Count; ++i)
	{
		const float A = Count == 1 ? CenterAngle : CenterAngle - Spread * 0.5f + Spread * i / (Count - 1);
		Fire(Pos, Speed, A, Shape, Color);
	}
}

float ADanmakuWorld::AimAngle(const FVector2f& From) const
{
	return AngleOf(Player.Pos - From);
}

bool ADanmakuWorld::Every(int32 T, int32 Interval, int32 Offset) const
{
	const float K = GetDifficultyInfo(Difficulty).IntervalScale * CompInterval;
	const int32 N = FMath::Max(1, FMath::RoundToInt(Interval * K));
	return T % N == FMath::RoundToInt(Offset * K) % N;
}

int32 ADanmakuWorld::Cnt(int32 N) const
{
	return FMath::Max(1, FMath::RoundToInt(N * GetDifficultyInfo(Difficulty).CountMul * CompCount));
}

int32 ADanmakuWorld::Half(int32 N) const
{
	return FMath::Max(0, FMath::RoundToInt(N * GetDifficultyInfo(Difficulty).CountMul));
}

void ADanmakuWorld::UpdateBullets(bool bMove)
{
	const bool bHittable = CanBeHit();
	const bool bCanGraze = State == EDanmakuState::Playing && Player.DeathTimer == 0 && Player.RespawnTimer == 0;

	const bool bHaruCalm = Player.Character == ECharacter::Haru && Player.RespawnTimer == 0 && State == EDanmakuState::Playing;
	for (FBullet& B : Bullets)
	{
		if (B.bDead) continue;
		const FVector2f Before = B.Pos;
		if (bMove)
		{
			++B.Age;
			if (B.Behavior != 0 && !UpdateBehavior(B)) continue;
			if (B.Life > 0 && B.Age > B.Life)
			{
				B.bDead = true;
				AddParticle(B.Pos, FVector2f::ZeroVector, 10.f, B.Color, 12);
				continue;
			}
			if (B.bOrbit)
			{
				B.OrbitAngle += B.OrbitSpeed;
				B.Pos = Boss.Pos + Dir(B.OrbitAngle) * B.OrbitRadius;
				if (B.Age >= B.EventFrame)
				{
					B.bOrbit = false;
					B.Angle = AimAngle(B.Pos);
					B.Speed = B.EventParam;
				}
			}
			else if (B.Motion == 1)
			{
				// polar: orbits/expands around a centre that can itself drift
				B.Center += B.CenterVel;
				B.PolarRadius += B.PolarRadSpeed;
				B.PolarAngle += B.PolarAngSpeed;
				B.Pos = B.Center + Dir(B.PolarAngle) * B.PolarRadius;
				B.Angle = B.PolarAngle;
			}
			else if (B.Motion == 2)
			{
				// free velocity with gravity (arcs, falling fire, rain)
				B.Vel += B.Gravity;
				B.Pos += B.Vel * GetDifficultyInfo(Difficulty).SpeedMul;
				if (!B.Vel.IsNearlyZero()) B.Angle = AngleOf(B.Vel);
			}
			else
			{
				if (B.Event != EBulletEvent::None && B.Age == B.EventFrame)
				{
					HandleBulletEvent(B);
					if (B.bDead) continue;
				}
				B.Speed = FMath::Clamp(B.Speed + B.Accel, B.MinSpeed, B.MaxSpeed);
				B.Angle += B.AngVel;
				B.AngVel *= B.AngVelDecay;
				B.Pos += Dir(B.Angle) * (B.Speed * GetDifficultyInfo(Difficulty).SpeedMul);
			}

			if (B.WaveAmp != 0.f)
			{
				B.Pos.X += FMath::Sin(B.Age * B.WaveFreq + B.WavePhase) * B.WaveAmp;
			}
			if (B.Bounces > 0 && !B.bOrbit && B.Motion != 1)
			{
				const uint8 Mask = B.WallMask ? B.WallMask : 1;
				FVector2f V = B.Motion == 2 ? B.Vel : Dir(B.Angle);
				bool bHit = false;
				if ((Mask & 1) && ((B.Pos.X < 4.f && V.X < 0.f) || (B.Pos.X > FieldW - 4.f && V.X > 0.f))) { V.X = -V.X; bHit = true; }
				if ((Mask & 2) && B.Pos.Y < 4.f && V.Y < 0.f) { V.Y = -V.Y; bHit = true; }
				if ((Mask & 4) && B.Pos.Y > FieldH - 4.f && V.Y > 0.f) { V.Y = -V.Y; bHit = true; }
				if (bHit)
				{
					if (B.Motion == 2) B.Vel = V;
					else { B.Angle = AngleOf(V); B.AngVel = -B.AngVel; }
					if (--B.Bounces == 0 && B.bDieAfterBounces)
					{
						B.bDead = true;
						AddParticle(B.Pos, FVector2f::ZeroVector, 10.f, B.Color, 12);
						continue;
					}
				}
			}
			// 晴's calm: bullets close to him move 20% slower
			if (bHaruCalm && !B.bOrbit && B.Motion != 1 && DistSq(Before, Player.Pos) < Sq(HaruSlowRadius))
			{
				B.Pos = Before + (B.Pos - Before) * 0.8f;
			}
			if (B.Pos.Y > B.KillBelowY)
			{
				B.bDead = true;
				continue;
			}

			if (B.bWrap)
			{
				constexpr float M = 16.f;
				if (B.Pos.X < -M) B.Pos.X += FieldW + 2 * M; else if (B.Pos.X > FieldW + M) B.Pos.X -= FieldW + 2 * M;
				if (B.Pos.Y < -M) B.Pos.Y += FieldH + 2 * M; else if (B.Pos.Y > FieldH + M) B.Pos.Y -= FieldH + 2 * M;
			}
			else if (!B.bOrbit && B.Age > 30 && !InField(B.Pos, B.Behavior ? 140.f : 56.f))
			{
				B.bDead = true;
				continue;
			}
		}

		if (B.bHarmless) continue;
		const float D2 = DistSq(B.Pos, Player.Pos);
		if (bCanGraze && !B.bGrazed && D2 < Sq(B.Radius + GrazeRadius))
		{
			B.bGrazed = true;
			++Graze;
			Sfx(ESfx::Graze);
			Score += 500;
			AddParticle(B.Pos, (B.Pos - Player.Pos).GetSafeNormal() * 2.f, 6.f, ColWhite, 12);
		}
		if (bHittable && D2 < Sq(B.Radius + HitRadius()))
		{
			// assassins and roaming chrysanthemums are "pieces", not shots: they stay after hitting you
			if (!(B.bBombProof && (B.Behavior == BehAssassin || B.Behavior == BehFinalChrys))) B.bDead = true;
			PlayerHit();
		}
	}
	Bullets.RemoveAllSwap([](const FBullet& B) { return B.bDead; });
}

void ADanmakuWorld::HandleBulletEvent(FBullet& B)
{
	switch (B.Event)
	{
	case EBulletEvent::AimAtPlayer:
		B.Angle = AimAngle(B.Pos);
		B.Speed = B.EventParam;
		B.Accel = 0.f;
		B.AngVel = 0.f;
		B.Event = EBulletEvent::None;
		break;
	case EBulletEvent::StopThenAim:
		B.Speed = 0.f;
		B.Accel = 0.f;
		B.Event = EBulletEvent::AimAtPlayer;
		B.EventFrame = B.Age + FMath::Max(1, FMath::RoundToInt(B.EventParam2));
		break;
	case EBulletEvent::SplitRing:
	{
		const int32 N = FMath::Max(1, FMath::RoundToInt(B.EventParam));
		const float Base = Rng.FRandRange(0.f, 360.f);
		Ring(B.Pos, N, 2.0f, Base, EBulletShape::Small, B.Color);
		Ring(B.Pos, N, 1.3f, Base + 180.f / N, EBulletShape::Small, ColPink);
		AddParticle(B.Pos, FVector2f::ZeroVector, 60.f, B.Color, 16, true);
		B.bDead = true;
		break;
	}
	case EBulletEvent::SplitTwo:
		// a falling star sheds two small sparks at +-31 degrees and keeps going
		for (float Off : { 31.5f, -31.5f })
		{
			FBullet& N = Fire(B.Pos, 1.43f, B.Angle + Off, EBulletShape::Small, FLinearColor(0.78f, 0.88f, 1.f));
			N.Accel = 0.0006f;
			N.MaxSpeed = 2.1f;
		}
		B.Event = EBulletEvent::None;
		break;
	default:
		break;
	}
}

void ADanmakuWorld::CancelAllBullets(bool bToStars, bool bKeepProof)
{
	auto CancelArray = [this, bToStars, bKeepProof](TArray<FBullet>& Arr)
	{
		for (FBullet& B : Arr)
		{
			if (B.bDead) continue;
			if (bKeepProof && B.bBombProof) continue;
			B.bDead = true;
			if (bToStars) SpawnItem(EItemType::Star, B.Pos, true);
			else if (Particles.Num() < 2500) AddParticle(B.Pos, FVector2f::ZeroVector, BulletVisualSize(B.Shape) * 1.5f, B.Color, 14);
		}
		Arr.RemoveAllSwap([](const FBullet& B) { return B.bDead; });
	};
	CancelArray(Bullets);
	CancelArray(NewBullets);
	Beams.Reset();
}

bool ADanmakuWorld::FindHomingTarget(const FVector2f& From, FVector2f& OutTarget) const
{
	if (Boss.bActive && Boss.bVulnerable)
	{
		OutTarget = Boss.Pos;
		return true;
	}
	float Best = TNumericLimits<float>::Max();
	for (const FEnemy& E : Enemies)
	{
		if (E.bDead || !InField(E.Pos, -4.f)) continue;
		const float D = DistSq(E.Pos, From);
		if (D < Best) { Best = D; OutTarget = E.Pos; }
	}
	return Best < TNumericLimits<float>::Max();
}

void ADanmakuWorld::UpdateShots()
{
	for (FShot& S : Shots)
	{
		++S.Age;
		if (S.Type == EShotType::Homing)
		{
			FVector2f Target;
			if (FindHomingTarget(S.Pos, Target))
			{
				const float Cur = AngleOf(S.Vel);
				const float Want = AngleOf(Target - S.Pos);
				const float Turn = FMath::Clamp(FMath::FindDeltaAngleDegrees(Cur, Want), -6.f, 6.f);
				S.Vel = Dir(Cur + Turn) * S.Vel.Size();
			}
		}
		S.Pos += S.Vel;
		if (S.Age > 180 || S.Pos.Y < -24.f || S.Pos.Y > FieldH + 24.f || S.Pos.X < -24.f || S.Pos.X > FieldW + 24.f)
		{
			S.bDead = true;
			continue;
		}
		for (FEnemy& E : Enemies)
		{
			if (E.bDead || E.HP <= 0.f || E.Pos.Y < -8.f) continue;
			if (DistSq(S.Pos, E.Pos) < Sq(E.Radius + 6.f))
			{
				E.HP -= S.Damage;
				E.HitFlash = 3;
				S.bDead = true;
				Score += 10;
				break;
			}
		}
		bool bHitBoss = false;
		if (!S.bDead && Boss.bActive && Boss.bVulnerable)
		{
			FVector2f Tg[4];
			const int32 NT = BossTargets(Tg);
			for (int32 i = 0; i < NT; ++i) bHitBoss |= DistSq(S.Pos, Tg[i]) < Sq(30.f);
		}
		if (bHitBoss)
		{
			if (!(S.bFromBomb && IsBombImmunePhase())) DamageBoss(S.Damage);
			S.bDead = true;
			Score += 10;
		}
		if (S.bDead && Particles.Num() < 2500)
		{
			AddParticle(S.Pos, FVector2f(Rng.FRandRange(-1.f, 1.f), Rng.FRandRange(-2.f, -0.5f)), 8.f, GetCharacterInfo(Player.Character).Color, 10);
		}
	}
	Shots.RemoveAllSwap([](const FShot& S) { return S.bDead; });
}

// =====================================================================================
// Items & particles
// =====================================================================================

void ADanmakuWorld::SpawnItem(EItemType Type, const FVector2f& Pos, bool bAutoCollect)
{
	if (Items.Num() >= 1500) return;
	FItem& I = Items.AddDefaulted_GetRef();
	I.Type = Type;
	I.Pos = Pos;
	I.bCollecting = bAutoCollect;
	I.Vel = FVector2f(Rng.FRandRange(-1.f, 1.f), Rng.FRandRange(-3.f, -1.8f));
}

void ADanmakuWorld::UpdateItems()
{
	const bool bAlive = Player.DeathTimer == 0 && Player.RespawnTimer == 0;
	const bool bAbovePOC = bAlive && Player.Pos.Y < CollectLine();
	// 晴's calm: items fall 20% slower
	const float ItemFall = Player.Character == ECharacter::Haru ? 0.8f : 1.f;

	for (FItem& I : Items)
	{
		++I.Age;
		if (bAbovePOC) I.bCollecting = true;
		const FVector2f ToP = Player.Pos - I.Pos;
		const float Dist = ToP.Size();

		if (I.bCollecting && bAlive && (I.Type != EItemType::Star || I.Age > 20))
		{
			const float Spd = I.Type == EItemType::Star ? 8.f : 10.f;
			if (Dist < Spd + 4.f)
			{
				CollectItem(I);
				I.bDead = true;
				continue;
			}
			I.Pos += ToP / Dist * Spd;
		}
		else
		{
			if (I.Type == EItemType::Star)
			{
				I.Pos.Y -= 0.3f;
			}
			else
			{
				I.Vel.Y = FMath::Min(I.Vel.Y + 0.06f, 2.2f);
				I.Vel.X *= 0.95f;
				I.Pos += I.Vel * ItemFall;
			}
			if (bAlive && Dist < (Player.bFocus ? 48.f : 22.f))
			{
				I.bCollecting = true;
			}
			if (I.Pos.Y > FieldH + 24.f) I.bDead = true;
		}
	}
	Items.RemoveAllSwap([](const FItem& I) { return I.bDead; });
}

void ADanmakuWorld::CollectItem(const FItem& I)
{
	Sfx(ESfx::Item);
	switch (I.Type)
	{
	case EItemType::Power:
		if (Player.Power >= 4.f) Score += 1000;
		else { Player.Power = FMath::Min(4.f, Player.Power + 0.05f); Score += 10; }
		break;
	case EItemType::BigPower:
		Player.Power = FMath::Min(4.f, Player.Power + 1.f);
		Score += 1000;
		break;
	case EItemType::Point:
	{
		const float T = FMath::Clamp((Player.Pos.Y - CollectLine()) / (FieldH - CollectLine()), 0.f, 1.f);
		Score += I.bCollecting && Player.Pos.Y < CollectLine() ? 50000 : int64(FMath::Lerp(40000.f, 8000.f, T));
		break;
	}
	case EItemType::Star:
		Score += 300;
		break;
	case EItemType::LifePiece:
		AddLifePieces(1, TEXT("符卡收取"));
		break;
	case EItemType::BombPiece:
		AddBombPieces(1, TEXT("靈符碎片"));
		break;
	}
}

// =====================================================================================
// Life / bomb recovery — a mix of the systems from different Touhou games:
//   * score extends (1UP at 10M / 30M / 60M / 100M / 150M, then every 100M)
//   * life pieces: capturing a spell card drops one; 5 pieces = 1UP
//   * bomb pieces: beating a non-spell, killing a big spirit, or every 250 grazes; 3 pieces = 1 bomb
// Lives and bombs cap at 8; anything beyond that turns into score.
// =====================================================================================

int64 ADanmakuWorld::ExtendScore(int32 Index)
{
	static const int64 Table[] = { 10000000LL, 30000000LL, 60000000LL, 100000000LL, 150000000LL };
	constexpr int32 N = UE_ARRAY_COUNT(Table);
	return Index < N ? Table[Index] : Table[N - 1] + int64(Index - N + 1) * 100000000LL;
}

void ADanmakuWorld::Popup(const FString& Text, const FLinearColor& Color)
{
	PopupText = Text;
	PopupColor = Color;
	PopupTimer = 110;
}

void ADanmakuWorld::AddLifePieces(int32 N, const TCHAR* Reason)
{
	Player.LifePieces += N;
	bool bExtend = false;
	while (Player.LifePieces >= LifePiecesPerLife)
	{
		Player.LifePieces -= LifePiecesPerLife;
		if (Player.Lives < MaxLives) { ++Player.Lives; bExtend = true; }
		else Score += 2000000;
	}
	if (bExtend)
	{
		Sfx(ESfx::SpellGet);
		Popup(TEXT("1UP！"), FLinearColor(1.f, 0.55f, 0.7f));
		AddParticle(Player.Pos, FVector2f::ZeroVector, 120.f, FLinearColor(1.f, 0.55f, 0.7f), 30, true);
	}
	else
	{
		Popup(FString(Tr(Reason)) + TEXT("　") + Tr(TEXT("殘機碎片")) + FString::Printf(TEXT(" %d/%d"), Player.LifePieces, LifePiecesPerLife), FLinearColor(1.f, 0.7f, 0.8f));
	}
}

void ADanmakuWorld::AddBombPieces(int32 N, const TCHAR* Reason)
{
	Player.BombPieces += N;
	bool bGot = false;
	while (Player.BombPieces >= BombPiecesNeeded())
	{
		Player.BombPieces -= BombPiecesNeeded();
		if (Player.Bombs < MaxBombs) { ++Player.Bombs; bGot = true; }
		else Score += 500000;
	}
	if (bGot)
	{
		Sfx(ESfx::Item);
		Popup(TEXT("靈符 +1"), FLinearColor(0.6f, 1.f, 0.65f));
		AddParticle(Player.Pos, FVector2f::ZeroVector, 90.f, FLinearColor(0.6f, 1.f, 0.65f), 26, true);
	}
	else
	{
		Popup(FString(Tr(Reason)) + FString::Printf(TEXT("　%d/%d"), Player.BombPieces, BombPiecesNeeded()), FLinearColor(0.7f, 1.f, 0.75f));
	}
}

void ADanmakuWorld::UpdateRecovery()
{
	if (PopupTimer > 0) --PopupTimer;
	while (Score >= ExtendScore(NextExtend))
	{
		++NextExtend;
		if (Player.Lives < MaxLives)
		{
			++Player.Lives;
			Sfx(ESfx::SpellGet);
			Popup(TEXT("分數獎勵　1UP！"), FLinearColor(1.f, 0.55f, 0.7f));
		}
	}
	if (Graze >= NextGrazeReward)
	{
		NextGrazeReward += GrazePerBombPiece;
		AddBombPieces(1, TEXT("擦彈獎勵"));
	}
}

void ADanmakuWorld::AddParticle(const FVector2f& Pos, const FVector2f& Vel, float Size, const FLinearColor& Color, int32 Life, bool bRing)
{
	if (Particles.Num() >= 3000) return;
	FParticle& P = Particles.AddDefaulted_GetRef();
	P.Pos = Pos;
	P.Vel = Vel;
	P.Size = Size;
	P.Color = Color;
	P.Life = P.MaxLife = FMath::Max(1, Life);
	P.bRing = bRing;
}

void ADanmakuWorld::Burst(const FVector2f& Pos, int32 Count, const FLinearColor& Color, float Speed, float Size, int32 Life)
{
	for (int32 i = 0; i < Count; ++i)
	{
		AddParticle(Pos, Dir(Rng.FRandRange(0.f, 360.f)) * Rng.FRandRange(Speed * 0.3f, Speed), Size * Rng.FRandRange(0.6f, 1.2f), Color,
			FMath::RoundToInt(Life * Rng.FRandRange(0.6f, 1.f)));
	}
}

void ADanmakuWorld::UpdateParticles()
{
	for (FParticle& P : Particles)
	{
		P.Pos += P.Vel;
		P.Vel *= 0.94f;
		--P.Life;
	}
	Particles.RemoveAllSwap([](const FParticle& P) { return P.Life <= 0; });
}

// =====================================================================================
// Telegraphed beam hazards (lightning, bamboo, ...)
// =====================================================================================

FBeamHazard& ADanmakuWorld::AddBeam(const FVector2f& Origin, float Angle, float Width, int32 Warn, int32 Rise, int32 Hold, const FLinearColor& Color, uint8 OnStrike)
{
	FBeamHazard& Bm = Beams.AddDefaulted_GetRef();
	Bm.Origin = Origin;
	Bm.Angle = Angle;
	Bm.Width = Width;
	Bm.Warn = Warn;
	Bm.Rise = FMath::Max(1, Rise);
	Bm.Hold = Hold;
	Bm.Color = Color;
	Bm.OnStrike = OnStrike;
	// length to the edge of the field
	const FVector2f D = Dir(Angle);
	float L = 2000.f;
	if (D.X > 0.001f)  L = FMath::Min(L, (FieldW - Origin.X) / D.X);
	if (D.X < -0.001f) L = FMath::Min(L, -Origin.X / D.X);
	if (D.Y > 0.001f)  L = FMath::Min(L, (FieldH - Origin.Y) / D.Y);
	if (D.Y < -0.001f) L = FMath::Min(L, -Origin.Y / D.Y);
	Bm.MaxLength = FMath::Max(10.f, L);
	return Bm;
}

void ADanmakuWorld::SpawnLeaves(const FVector2f& At, float Angle, int32 Count)
{
	for (int32 i = 0; i < Count; ++i)
	{
		FBullet& B = FireVec(At + FVector2f(Rng.FRandRange(-6.f, 6.f), Rng.FRandRange(-6.f, 6.f)),
			Dir(Angle + Rng.FRandRange(-14.f, 14.f)) * Rng.FRandRange(1.1f, 1.75f), EBulletShape::Rice,
			FLinearColor(Rng.FRandRange(0.45f, 0.65f), Rng.FRandRange(0.85f, 1.f), Rng.FRandRange(0.45f, 0.65f)));
		B.Spin = Rng.FRandRange(-5.f, 5.f);
	}
}

void ADanmakuWorld::UpdateBeams(bool bMove)
{
	const bool bHittable = CanBeHit();
	for (FBeamHazard& Bm : Beams)
	{
		if (Bm.bDead) continue;
		if (bMove) ++Bm.Age;
		const int32 RiseEnd = Bm.Warn + Bm.Rise;
		if (Bm.Age < Bm.Warn) Bm.Length = 0.f;
		else if (Bm.Age < RiseEnd) Bm.Length = Bm.MaxLength * float(Bm.Age - Bm.Warn + 1) / float(Bm.Rise);
		else Bm.Length = Bm.MaxLength;

		const FVector2f End = Bm.Origin + Dir(Bm.Angle) * Bm.Length;
		if (!Bm.bStruck && Bm.Age >= RiseEnd)
		{
			Bm.bStruck = true;
			Sfx(ESfx::Break);
			if (Bm.OnStrike == 1)
			{
				// lightning: fire bursts out of the ground and rains back down
				const int32 N = Cnt(Rng.RandRange(10, 20));
				for (int32 i = 0; i < N; ++i)
				{
					FBullet& F = FireVec(FVector2f(Bm.Origin.X + Rng.FRandRange(-36.f, 36.f), FieldH + Rng.FRandRange(2.f, 14.f)),
						FVector2f(Rng.FRandRange(-1.3f, 1.3f), -Rng.FRandRange(6.3f, 7.8f)), EBulletShape::Fire,
						FLinearColor(1.f, Rng.FRandRange(0.47f, 0.78f), 0.24f));
					F.Gravity = FVector2f(0.f, Rng.FRandRange(0.07f, 0.09f));
				}
			}
			else if (Bm.OnStrike == 2)
			{
				SpawnLeaves(End, Bm.Angle + 180.f, Cnt(Rng.RandRange(4, 9)));
			}
			else if (Bm.OnStrike == 4)
			{
				// 窮奇's teeth: where the beam lands, a slow crescent drifts back up to the top
				Fire(FVector2f(End.X, FieldH - 6.f), 0.5f, 270.f, EBulletShape::Crescent, FLinearColor(0.45f, 0.5f, 1.f));
				Burst(FVector2f(End.X, FieldH - 6.f), 6, Bm.Color, 2.f, 8.f, 16);
			}
			else if (Bm.OnStrike == 3)
			{
				// the beam shatters against the wall and sprays shrapnel back into the field
				const int32 N = Cnt(12);
				for (int32 i = 0; i < N; ++i)
				{
					FBullet& Sh = Fire(End, Rng.FRandRange(1.3f, 2.3f), Bm.Angle + 180.f + Rng.FRandRange(-75.f, 75.f), EBulletShape::Small, FMath::Lerp(Bm.Color, FLinearColor::White, 0.3f));
					Sh.Accel = 0.004f;
				}
				Burst(End, 10, Bm.Color, 2.5f, 10.f, 20);
			}
		}
		if (Bm.Age >= RiseEnd + Bm.Hold)
		{
			Bm.bDead = true;
			continue;
		}
		if (Bm.IsActive() && Bm.Length > 0.f)
		{
			// distance from the player to the beam segment
			const FVector2f AB = End - Bm.Origin;
			const float T = FMath::Clamp(FVector2f::DotProduct(Player.Pos - Bm.Origin, AB) / FMath::Max(1.f, AB.SizeSquared()), 0.f, 1.f);
			const float D = FVector2f::Distance(Player.Pos, Bm.Origin + AB * T);
			if (!Bm.bGrazed && D < Bm.Width * 0.5f + GrazeRadius && Player.DeathTimer == 0)
			{
				Bm.bGrazed = true;
				++Graze;
				Score += 500;
				Sfx(ESfx::Graze);
			}
			if (bHittable && D < Bm.Width * 0.5f + HitRadius())
			{
				PlayerHit();
			}
		}
	}
	Beams.RemoveAllSwap([](const FBeamHazard& B) { return B.bDead; });
}
