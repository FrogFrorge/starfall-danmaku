#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "Engine/EngineTypes.h"
#include "DanmakuHUD.generated.h"

class ADanmakuWorld;
class UTexture2D;
class UFont;

/** A rectangle of an illustration (in 0..1 UV) that is hair: it sways from its top (root) to its bottom (tip). */
struct FHairZone
{
	float U0, U1, V0, V1;
	float Strength = 1.f;
};

/**
 * Draws the whole game with UCanvas: playfield, sprites, bullets, sidebar, menus and overlays.
 * All art is loaded at runtime from <Project>/Art/*.png, the UI font from <Project>/Fonts/.
 */
UCLASS()
class DANMAKUSTG_API ADanmakuHUD : public AHUD
{
	GENERATED_BODY()

public:
	virtual void DrawHUD() override;

private:
	void LoadTextures();
	UTexture2D* LoadArt(const TCHAR* FileName, bool bWrap = false);
	ADanmakuWorld* GetSim() const;

	// ---- primitives ("field" positions are 384x448 playfield units; menus temporarily use pixels) ----
	FVector2D ToScreen(const FVector2f& P) const;
	void Sprite(UTexture2D* Tex, const FVector2f& Center, const FVector2D& SizeUnits, FLinearColor Color,
		ESimpleElementBlendMode Blend = SE_BLEND_Translucent, float RotDeg = 0.f);
	void ArtSprite(UTexture2D* Tex, const FVector2f& Center, float HeightUnits, const FLinearColor& Color, float RotDeg = 0.f, float XScale = 1.f);
	/** Illustration as a deformable grid: body stays put, outer cloth flutters, hair zones sway from the root. */
	void WarpSprite(UTexture2D* Tex, const FVector2f& Center, float HeightUnits, const FLinearColor& Color,
		float Trail, float Sway, float Phase, float CoreHalf = 0.16f, float FlutterTop = 0.3f,
		TConstArrayView<FHairZone> Hair = {});
	/** Frame from a pre-rendered sheet (Tools/anim/make_sheets.py): rows = lean -2..2, columns = hair phases. Body never deforms. */
	void SheetSprite(UTexture2D* Sheet, const FVector2f& Center, float HeightUnits, const FLinearColor& Color, float Lean, int32 Phase);
	void Beam(const FVector2f& From, const FVector2f& To, float Width, const FLinearColor& Color);
	void ScreenRect(float X, float Y, float W, float H, const FLinearColor& Color);
	void FieldRect(float X, float Y, float W, float H, const FLinearColor& Color);
	void TiledRect(UTexture2D* Tex, float X, float Y, float W, float H, float TilePx, const FLinearColor& Color);
	void InkPanel(float X, float Y, float W, float H, float Alpha = 0.78f);
	/** MaxWidth > 0: the text shrinks until it fits (pixels for ScreenText, field units for FieldText). */
	void ScreenText(const FString& Text, float X, float Y, const FLinearColor& Color, float Scale, bool bCenter = false, float MaxWidth = 0.f);
	void FieldText(const FString& Text, float X, float Y, const FLinearColor& Color, float Scale, bool bCenter = true, float MaxWidth = 0.f);
	/** Width in pixels of the (translated) text at this scale. */
	float TextWidth(const FString& Text, float Scale);
	/** Breaks the (translated) text into lines no wider than MaxPx: on spaces in English, between characters in Chinese. */
	TArray<FString> WrapPx(const FString& Text, float MaxPx, float Scale);
	static TArray<FString> WrapText(const FString& Text, int32 MaxChars);

	// ---- screens / layers ----
	void DrawTitle(const ADanmakuWorld& S, float SW, float SH);
	void DrawSelect(const ADanmakuWorld& S, float SW, float SH);
	void DrawDiffSelect(const ADanmakuWorld& S, float SW, float SH);
	void DrawSpellSelect(const ADanmakuWorld& S, float SW, float SH);
	void DrawStageSelect(const ADanmakuWorld& S, float SW, float SH);
	void DrawBeams(const ADanmakuWorld& S);
	void DrawFog(const ADanmakuWorld& S);
	void DrawBackground(const ADanmakuWorld& S);
	void DrawItems(const ADanmakuWorld& S);
	void DrawEnemies(const ADanmakuWorld& S);
	void DrawBoss(const ADanmakuWorld& S);
	void DrawPlayer(const ADanmakuWorld& S);
	void DrawShots(const ADanmakuWorld& S);
	void DrawBullets(const ADanmakuWorld& S);
	void DrawParticles(const ADanmakuWorld& S);
	void DrawBomb(const ADanmakuWorld& S);
	void DrawHitbox(const ADanmakuWorld& S);
	void DrawBossUI(const ADanmakuWorld& S);
	void DrawPortraits(const ADanmakuWorld& S);
	void DrawFrame(float SW, float SH);
	void DrawSidebar(const ADanmakuWorld& S, float SW, float SH);
	void DrawOverlays(const ADanmakuWorld& S);

	UTexture2D* PlayerBackFor(int32 Char) const;
	UTexture2D* PortraitFor(int32 Char) const;
	UTexture2D* BulletTex(uint8 Shape) const;
	UTexture2D* BossPortraitFor(const ADanmakuWorld& S) const;
	UTexture2D* BossBodyFor(const ADanmakuWorld& S) const;
	UTexture2D* StageBgFor(const ADanmakuWorld& S) const;
	TConstArrayView<FHairZone> BossBodyHair(const ADanmakuWorld& S) const;
	TConstArrayView<FHairZone> BossPortraitHair(const ADanmakuWorld& S) const;

	// procedural fallbacks
	UPROPERTY(Transient) TObjectPtr<UTexture2D> SoftTex;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> DiscTex;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> RingTex;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> SquareTex;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> HoleTex;      // opaque outside a soft circle (fog of war)

	// characters
	UPROPERTY(Transient) TArray<TObjectPtr<UTexture2D>> PlayerBacks;   // indexed by ECharacter
	UPROPERTY(Transient) TArray<TObjectPtr<UTexture2D>> Portraits;     // indexed by ECharacter
	UPROPERTY(Transient) TObjectPtr<UTexture2D> PortraitBoss;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BossBody;
	UPROPERTY(Transient) TArray<TObjectPtr<UTexture2D>> BossPortraits;   // per stage (index 0 = PortraitBoss)
	UPROPERTY(Transient) TArray<TObjectPtr<UTexture2D>> BossBodies;      // per stage
	UPROPERTY(Transient) TArray<TObjectPtr<UTexture2D>> StageBgs;        // per stage
	UPROPERTY(Transient) TArray<TObjectPtr<UTexture2D>> PlayerSheets;    // indexed by ECharacter
	UPROPERTY(Transient) TArray<TObjectPtr<UTexture2D>> BossSheets;      // per stage art index
	UPROPERTY(Transient) TObjectPtr<UTexture2D> FairyBlueArt;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> FairyRedArt;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> SpiritArt;

	// effects, items, bullets
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletOrb;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletPetal;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletStar;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletCrescent;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletSakura;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletMaple;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletSnow;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletWater;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletFire;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletChrys;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> BulletTile;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> MagicCircle;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> ShotTalisman;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> ItemPower;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> ItemPoint;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> IconLife;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> IconBomb;

	// backgrounds / UI
	UPROPERTY(Transient) TObjectPtr<UTexture2D> TitleArt;      // logo
	UPROPERTY(Transient) TObjectPtr<UTexture2D> TitleKeyArt;   // full-screen illustration
	UPROPERTY(Transient) TObjectPtr<UTexture2D> StageBg;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> SpellBg;
	UPROPERTY(Transient) TObjectPtr<UTexture2D> UiPattern;

	UPROPERTY(Transient) TObjectPtr<UFont> UiFont;

	bool bTexturesLoaded = false;
	FVector2D Origin = FVector2D::ZeroVector;
	float Scale = 1.f;
	float TextScale = 1.f;
	float SmoothedFPS = 60.f;
	float SpellBlend = 0.f;
	float BossLean = 0.f;
};
