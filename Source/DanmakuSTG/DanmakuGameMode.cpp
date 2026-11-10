#include "DanmakuGameMode.h"
#include "DanmakuWorld.h"
#include "DanmakuHUD.h"
#include "DanmakuAudio.h"
#include "Engine/World.h"
#include "GameFramework/PlayerController.h"

ADanmakuGameMode::ADanmakuGameMode()
{
	DefaultPawnClass = nullptr;
	HUDClass = ADanmakuHUD::StaticClass();
}

void ADanmakuGameMode::BeginPlay()
{
	Super::BeginPlay();

	FActorSpawnParameters Params;
	Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
	Sim = GetWorld()->SpawnActor<ADanmakuWorld>(ADanmakuWorld::StaticClass(), FTransform::Identity, Params);
	if (ADanmakuAudio* Audio = GetWorld()->SpawnActor<ADanmakuAudio>(ADanmakuAudio::StaticClass(), FTransform::Identity, Params))
	{
		Audio->Sim = Sim;
	}

	if (APlayerController* PC = GetWorld()->GetFirstPlayerController())
	{
		PC->SetInputMode(FInputModeGameOnly());
		PC->SetShowMouseCursor(false);
	}
}
