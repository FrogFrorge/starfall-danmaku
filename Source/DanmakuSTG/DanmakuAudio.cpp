#include "DanmakuAudio.h"

#include "DanmakuWorld.h"
#include "Components/AudioComponent.h"
#include "HAL/FileManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "Sound/SoundWaveProcedural.h"

DEFINE_LOG_CATEGORY_STATIC(LogDanmakuAudio, Log, All);

namespace
{
	struct FSfxDef { const TCHAR* File; float Volume; float MinInterval; };
	const FSfxDef GSfx[int32(ESfx::Count)] = {
		{ TEXT("sfx_shot.wav"),        0.22f, 0.075f },  // Shot
		{ TEXT("sfx_enemy_shot.wav"),  0.16f, 0.07f },   // EnemyShot
		{ TEXT("sfx_graze.wav"),       0.30f, 0.05f },   // Graze
		{ TEXT("sfx_item.wav"),        0.25f, 0.035f },  // Item
		{ TEXT("sfx_enemy_die.wav"),   0.45f, 0.06f },   // EnemyDie
		{ TEXT("sfx_player_die.wav"),  0.90f, 0.f },     // PlayerDie
		{ TEXT("sfx_bomb.wav"),        0.85f, 0.f },     // Bomb
		{ TEXT("sfx_spell.wav"),       0.75f, 0.f },     // Spell
		{ TEXT("sfx_spell_get.wav"),   0.80f, 0.f },     // SpellGet
		{ TEXT("sfx_break.wav"),       0.60f, 0.1f },    // Break
		{ TEXT("sfx_menu_move.wav"),   0.45f, 0.f },     // MenuMove
		{ TEXT("sfx_menu_ok.wav"),     0.55f, 0.f },     // MenuOk
		{ TEXT("sfx_menu_back.wav"),   0.45f, 0.f },     // MenuBack
		{ TEXT("sfx_pause.wav"),       0.50f, 0.f },     // Pause
	};
	const TCHAR* GMusic[3] = { TEXT("bgm_title.wav"), TEXT("bgm_stage.wav"), TEXT("bgm_boss.wav") };
	constexpr float MusicVolume = 0.5f;
	constexpr float LaserVolume = 0.3f;
	constexpr int32 PoolSize = 24;
}

ADanmakuAudio::ADanmakuAudio()
{
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.TickGroup = TG_PostPhysics;
	RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
}

TSharedPtr<ADanmakuAudio::FPcm> ADanmakuAudio::LoadWav(const FString& FileName)
{
	const FString Path = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("Audio") / FileName);
	TArray<uint8> Bytes;
	if (!IFileManager::Get().FileExists(*Path) || !FFileHelper::LoadFileToArray(Bytes, *Path) || Bytes.Num() < 44) return nullptr;
	if (FMemory::Memcmp(Bytes.GetData(), "RIFF", 4) != 0 || FMemory::Memcmp(Bytes.GetData() + 8, "WAVE", 4) != 0) return nullptr;

	auto U16 = [&Bytes](int32 O) { return int32(Bytes[O]) | (int32(Bytes[O + 1]) << 8); };
	auto U32 = [&Bytes](int32 O) { return int32(Bytes[O]) | (int32(Bytes[O + 1]) << 8) | (int32(Bytes[O + 2]) << 16) | (int32(Bytes[O + 3]) << 24); };

	TSharedPtr<FPcm> Pcm = MakeShared<FPcm>();
	int32 Bits = 0;
	int32 Off = 12;
	while (Off + 8 <= Bytes.Num())
	{
		const int32 Size = U32(Off + 4);
		const int32 Body = Off + 8;
		if (FMemory::Memcmp(Bytes.GetData() + Off, "fmt ", 4) == 0)
		{
			if (U16(Body) != 1) return nullptr;   // PCM only
			Pcm->Channels = U16(Body + 2);
			Pcm->SampleRate = U32(Body + 4);
			Bits = U16(Body + 14);
		}
		else if (FMemory::Memcmp(Bytes.GetData() + Off, "data", 4) == 0)
		{
			const int32 N = FMath::Min(Size, Bytes.Num() - Body);
			Pcm->Data.Append(Bytes.GetData() + Body, N);
		}
		Off = Body + Size + (Size & 1);
	}
	if (Bits != 16 || Pcm->Data.Num() == 0 || Pcm->Channels <= 0) return nullptr;
	Pcm->Duration = float(Pcm->Data.Num()) / float(2 * Pcm->Channels * Pcm->SampleRate);
	return Pcm;
}

USoundWaveProcedural* ADanmakuAudio::MakeWave(const TSharedPtr<FPcm>& Pcm, bool bLoop)
{
	USoundWaveProcedural* Wave = NewObject<USoundWaveProcedural>(this);
	Wave->SetSampleRate(Pcm->SampleRate);
	Wave->NumChannels = Pcm->Channels;
	Wave->Duration = bLoop ? INDEFINITELY_LOOPING_DURATION : Pcm->Duration;
	Wave->SoundGroup = SOUNDGROUP_Default;
	Wave->bLooping = false;
	Wave->QueueAudio(Pcm->Data.GetData(), Pcm->Data.Num());
	if (bLoop)
	{
		// keep feeding the same buffer so the track loops forever (called on the audio thread)
		TSharedPtr<FPcm> Keep = Pcm;
		Wave->OnSoundWaveProceduralUnderflow = FOnSoundWaveProceduralUnderflow::CreateLambda(
			[Keep](USoundWaveProcedural* InWave, int32 SamplesRequired)
			{
				InWave->QueueAudio(Keep->Data.GetData(), Keep->Data.Num());
			});
	}
	return Wave;
}

UAudioComponent* ADanmakuAudio::MakeComponent()
{
	UAudioComponent* C = NewObject<UAudioComponent>(this);
	C->bAutoActivate = false;
	C->bAutoDestroy = false;
	C->bIsUISound = true;
	C->SetupAttachment(RootComponent);
	C->RegisterComponent();
	return C;
}

void ADanmakuAudio::BeginPlay()
{
	Super::BeginPlay();
	SfxPcm.SetNum(int32(ESfx::Count));
	for (int32 i = 0; i < int32(ESfx::Count); ++i) SfxPcm[i] = LoadWav(GSfx[i].File);
	for (int32 i = 0; i < 3; ++i) MusicPcm[i] = LoadWav(GMusic[i]);
	LaserPcm = LoadWav(TEXT("sfx_laser_loop.wav"));

	for (int32 i = 0; i < PoolSize; ++i) SfxPool.Add(MakeComponent());
	MusicComp = MakeComponent();
	LaserComp = MakeComponent();

	int32 Loaded = 0;
	for (const TSharedPtr<FPcm>& P : SfxPcm) Loaded += P.IsValid() ? 1 : 0;
	UE_LOG(LogDanmakuAudio, Log, TEXT("Loaded %d/%d sound effects, music: title=%d stage=%d boss=%d, laser=%d"),
		Loaded, int32(ESfx::Count), MusicPcm[0].IsValid(), MusicPcm[1].IsValid(), MusicPcm[2].IsValid(), LaserPcm.IsValid());
}

void ADanmakuAudio::PlaySfx(ESfx Sfx)
{
	const int32 Idx = int32(Sfx);
	if (!SfxPcm.IsValidIndex(Idx) || !SfxPcm[Idx].IsValid()) return;
	const double Now = FPlatformTime::Seconds();
	if (Now - LastPlayed[Idx] < GSfx[Idx].MinInterval) return;
	LastPlayed[Idx] = Now;

	UAudioComponent* C = SfxPool[NextSfx];
	NextSfx = (NextSfx + 1) % SfxPool.Num();
	C->Stop();
	C->SetSound(MakeWave(SfxPcm[Idx], false));
	C->SetVolumeMultiplier(GSfx[Idx].Volume);
	C->Play();
	++PlayedCount;
}

// Track ids: 0 title, 1 + 2*Stage road theme, 2 + 2*Stage boss theme.
// Stage N (N >= 1) looks for bgm_stage<N+1>.wav / bgm_boss<N+1>.wav and falls back to the stage-1 tracks.
TSharedPtr<ADanmakuAudio::FPcm> ADanmakuAudio::MusicForTrack(int32 Track)
{
	if (Track <= 2) return Track >= 0 ? MusicPcm[Track] : nullptr;
	if (const TSharedPtr<FPcm>* Found = StageMusic.Find(Track)) return *Found;
	const int32 Stage = (Track - 1) / 2;
	const bool bBoss = (Track % 2) == 0;
	TSharedPtr<FPcm> Pcm = LoadWav(FString::Printf(TEXT("%s%d.wav"), bBoss ? TEXT("bgm_boss") : TEXT("bgm_stage"), Stage + 1));
	if (!Pcm.IsValid()) Pcm = MusicPcm[bBoss ? 2 : 1];
	StageMusic.Add(Track, Pcm);
	return Pcm;
}

void ADanmakuAudio::SetMusic(int32 Track)
{
	if (Track == CurrentTrack) return;
	CurrentTrack = Track;
	MusicComp->Stop();
	const TSharedPtr<FPcm> Pcm = MusicForTrack(Track);
	if (!Pcm.IsValid()) return;
	MusicComp->SetSound(MakeWave(Pcm, true));
	MusicComp->SetVolumeMultiplier(MusicVolume);
	MusicComp->FadeIn(0.8f, 1.f);
}

void ADanmakuAudio::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	if (!Sim) return;

	// one-shot effects (each type at most once per frame)
	bool bSeen[int32(ESfx::Count)] = {};
	for (ESfx S : Sim->SfxQueue)
	{
		const int32 I = int32(S);
		if (I >= 0 && I < int32(ESfx::Count) && !bSeen[I])
		{
			bSeen[I] = true;
			PlaySfx(S);
		}
	}
	Sim->SfxQueue.Reset();

	// music follows the game state
	int32 Track = 0;
	switch (Sim->State)
	{
	case EDanmakuState::Playing:  Track = (Sim->Boss.bStarted && !Sim->bMidboss ? 2 : 1) + 2 * Sim->StageIndex; break;
	case EDanmakuState::StageClear: Track = CurrentTrack; break;
	case EDanmakuState::GameOver: Track = -1; break;
	case EDanmakuState::Clear:    Track = 0; break;
	default:                      Track = 0; break;
	}
	SetMusic(Track);
	StatusTimer += DeltaSeconds;
	if (StatusTimer > 3.f)
	{
		StatusTimer = 0.f;
		UE_LOG(LogDanmakuAudio, Log, TEXT("music track %d playing=%d, sfx played so far=%d"), CurrentTrack, MusicComp && MusicComp->IsPlaying(), PlayedCount);
	}
	if (MusicComp) MusicComp->SetPaused(Sim->bPaused);

	// Sou's continuous laser hum
	const bool bLaser = Sim->State == EDanmakuState::Playing && !Sim->bPaused && Sim->Player.bLaserOn && LaserPcm.IsValid();
	if (bLaser && !bLaserPlaying)
	{
		LaserComp->SetSound(MakeWave(LaserPcm, true));
		LaserComp->SetVolumeMultiplier(LaserVolume);
		LaserComp->Play();
	}
	else if (!bLaser && bLaserPlaying)
	{
		LaserComp->Stop();
	}
	bLaserPlaying = bLaser;
}
