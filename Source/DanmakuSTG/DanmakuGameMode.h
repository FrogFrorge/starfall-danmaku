#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "DanmakuGameMode.generated.h"

class ADanmakuWorld;

/** Spawns the danmaku simulation and uses ADanmakuHUD to draw it. No pawn is needed. */
UCLASS()
class DANMAKUSTG_API ADanmakuGameMode : public AGameModeBase
{
	GENERATED_BODY()

public:
	ADanmakuGameMode();
	virtual void BeginPlay() override;
	virtual void RestartPlayer(AController* NewPlayer) override {}

	UPROPERTY(Transient)
	TObjectPtr<ADanmakuWorld> Sim;
};
