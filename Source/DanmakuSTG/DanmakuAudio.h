#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "DanmakuTypes.h"
#include "DanmakuAudio.generated.h"

class ADanmakuWorld;
class UAudioComponent;
class USoundWaveProcedural;

/**
 * Plays the game's sound effects and music. WAV files are read at runtime from <Project>/Audio/
 * and streamed through procedural sound waves, so nothing has to be imported in the editor.
 * The simulation pushes ESfx events; this actor consumes them every frame.
 */
UCLASS()
class DANMAKUSTG_API ADanmakuAudio : public AActor
{
	GENERATED_BODY()

public:
	ADanmakuAudio();
	virtual void BeginPlay() override;
	virtual void Tick(float DeltaSeconds) override;

	UPROPERTY(Transient)
	TObjectPtr<ADanmakuWorld> Sim;

private:
	struct FPcm
	{
		TArray<uint8> Data;   // 16-bit PCM
		int32 SampleRate = 44100;
		int32 Channels = 1;
		float Duration = 0.f;
	};

	static TSharedPtr<FPcm> LoadWav(const FString& FileName);
	USoundWaveProcedural* MakeWave(const TSharedPtr<FPcm>& Pcm, bool bLoop);
	UAudioComponent* MakeComponent();
	void PlaySfx(ESfx Sfx);
	void SetMusic(int32 Track);

	TArray<TSharedPtr<FPcm>> SfxPcm;   // indexed by ESfx
	TSharedPtr<FPcm> MusicPcm[3];      // 0 title, 1 stage, 2 boss (stage 1 / fallback)
	TMap<int32, TSharedPtr<FPcm>> StageMusic;   // track id -> stage-specific music (loaded on first use)
	TSharedPtr<FPcm> MusicForTrack(int32 Track);
	TSharedPtr<FPcm> LaserPcm;

	UPROPERTY(Transient) TArray<TObjectPtr<UAudioComponent>> SfxPool;
	UPROPERTY(Transient) TObjectPtr<UAudioComponent> MusicComp;
	UPROPERTY(Transient) TObjectPtr<UAudioComponent> LaserComp;

	int32 NextSfx = 0;
	int32 CurrentTrack = -2;
	bool bLaserPlaying = false;
	int32 PlayedCount = 0;
	float StatusTimer = 0.f;
	double LastPlayed[int32(ESfx::Count)] = {};
};
