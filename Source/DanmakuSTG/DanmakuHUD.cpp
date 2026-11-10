#include "DanmakuHUD.h"

#include "DanmakuGameMode.h"
#include "DanmakuWorld.h"
#include "CanvasItem.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "Engine/Font.h"
#include "Engine/Texture2D.h"
#include "Engine/World.h"
#include "Fonts/CompositeFont.h"
#include "HAL/FileManager.h"
#include "ImageUtils.h"
#include "Misc/Paths.h"
#include "TextureResource.h"

using namespace Danmaku;

namespace
{
	UTexture2D* MakeTexture(int32 Size, bool bPremultiplied, TFunctionRef<float(float U, float V)> AlphaFn)
	{
		UTexture2D* Tex = UTexture2D::CreateTransient(Size, Size, PF_B8G8R8A8);
		if (!Tex) return nullptr;
		Tex->Filter = TF_Bilinear;
		Tex->SRGB = true;
		FTexture2DMipMap& Mip = Tex->GetPlatformData()->Mips[0];
		FColor* Px = static_cast<FColor*>(Mip.BulkData.Lock(LOCK_READ_WRITE));
		for (int32 Y = 0; Y < Size; ++Y)
		{
			for (int32 X = 0; X < Size; ++X)
			{
				const float U = ((X + 0.5f) / Size) * 2.f - 1.f;
				const float V = ((Y + 0.5f) / Size) * 2.f - 1.f;
				const uint8 A = static_cast<uint8>(FMath::RoundToInt(FMath::Clamp(AlphaFn(U, V), 0.f, 1.f) * 255.f));
				Px[Y * Size + X] = bPremultiplied ? FColor(A, A, A, A) : FColor(255, 255, 255, A);
			}
		}
		Mip.BulkData.Unlock();
		Tex->UpdateResource();
		return Tex;
	}

	float Hash(int32 N)
	{
		const float S = FMath::Sin(N * 12.9898f) * 43758.5453f;
		return S - FMath::FloorToFloat(S);
	}

	FLinearColor WithAlpha(FLinearColor C, float A) { C.A = A; return C; }
	float Smooth(float A, float B, float X) { const float T = FMath::Clamp((X - A) / (B - A), 0.f, 1.f); return T * T * (3.f - 2.f * T); }

	const FLinearColor PanelColor(0.035f, 0.025f, 0.07f, 1.f);
	const FLinearColor LabelColor(0.86f, 0.8f, 1.f, 1.f);
	const FLinearColor Gold(0.92f, 0.76f, 0.4f, 1.f);
	const FLinearColor GoldText(1.f, 0.88f, 0.6f, 1.f);

	// Hair regions of the illustrations (UV of the processed PNGs)
	const FHairZone SiriusBodyHair[] = { { 0.06f, 0.36f, 0.12f, 0.62f, 1.f }, { 0.60f, 0.90f, 0.12f, 0.62f, 1.f } };
	const FHairZone SiriusPortraitHair[] = { { 0.08f, 0.36f, 0.14f, 1.0f, 0.8f }, { 0.62f, 0.92f, 0.14f, 1.0f, 0.8f } };
	const FHairZone SouBackHair[] = { { 0.36f, 0.82f, 0.05f, 0.47f, 1.25f } };
	// 千秋 (stage 2): long hair falling over both shoulders
	const FHairZone ChiakiBodyHair[] = { { 0.18f, 0.40f, 0.10f, 0.40f, 0.9f }, { 0.56f, 0.78f, 0.12f, 0.42f, 0.9f } };
	const FHairZone ChiakiPortraitHair[] = { { 0.18f, 0.40f, 0.14f, 0.54f, 0.7f }, { 0.56f, 0.78f, 0.16f, 0.57f, 0.7f } };
	// later bosses all have long hair down both sides
	const FHairZone LongBodyHair[] = { { 0.20f, 0.42f, 0.10f, 0.42f, 0.75f }, { 0.56f, 0.80f, 0.10f, 0.45f, 0.75f } };
	const FHairZone LongPortraitHair[] = { { 0.20f, 0.42f, 0.14f, 0.56f, 0.6f }, { 0.56f, 0.80f, 0.14f, 0.60f, 0.6f } };
	const FHairZone SouPortraitHair[] = { { 0.06f, 0.38f, 0.12f, 0.78f, 0.9f }, { 0.46f, 0.76f, 0.10f, 0.58f, 0.7f } };

}

// =====================================================================================
// Setup
// =====================================================================================

UTexture2D* ADanmakuHUD::LoadArt(const TCHAR* FileName, bool bWrap)
{
	const FString Path = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("Art") / FileName);
	if (!IFileManager::Get().FileExists(*Path)) return nullptr;
	UTexture2D* Tex = FImageUtils::ImportFileAsTexture2D(Path);
	if (Tex)
	{
		Tex->Filter = TF_Bilinear;
		Tex->AddressX = bWrap ? TA_Wrap : TA_Clamp;
		Tex->AddressY = bWrap ? TA_Wrap : TA_Clamp;
		Tex->UpdateResource();
	}
	return Tex;
}

void ADanmakuHUD::LoadTextures()
{
	bTexturesLoaded = true;

	SoftTex = MakeTexture(64, true, [](float U, float V) { return FMath::Square(FMath::Max(0.f, 1.f - FMath::Sqrt(U * U + V * V))); });
	DiscTex = MakeTexture(64, false, [](float U, float V) { return (1.f - FMath::Sqrt(U * U + V * V)) / 0.1f; });
	RingTex = MakeTexture(128, true, [](float U, float V)
	{
		const float R = FMath::Sqrt(U * U + V * V);
		const float Dash = FMath::Cos(FMath::Atan2(V, U) * 12.f) > -0.2f ? 1.f : 0.15f;
		return FMath::Max((1.f - FMath::Abs(R - 0.9f) / 0.06f) * Dash, (1.f - FMath::Abs(R - 0.74f) / 0.025f) * 0.7f);
	});
	SquareTex = MakeTexture(16, false, [](float U, float V) { return (1.f - FMath::Max(FMath::Abs(U), FMath::Abs(V))) / 0.15f; });
	HoleTex = MakeTexture(128, false, [](float U, float V) { return Smooth(0.55f, 1.f, FMath::Sqrt(U * U + V * V)); });

	PlayerBacks = { LoadArt(TEXT("player_back.png")), LoadArt(TEXT("player_back_sou.png")), LoadArt(TEXT("player_back_haru.png")), LoadArt(TEXT("player_back_yuki.png")) };
	Portraits   = { LoadArt(TEXT("portrait_ren.png")), LoadArt(TEXT("portrait_sou.png")), LoadArt(TEXT("portrait_haru.png")), LoadArt(TEXT("portrait_yuki.png")) };
	PortraitBoss   = LoadArt(TEXT("portrait_sirius.png"));
	BossBody       = LoadArt(TEXT("boss_body.png"));
	FairyBlueArt   = LoadArt(TEXT("fairy_blue.png"));
	FairyRedArt    = LoadArt(TEXT("fairy_red.png"));
	SpiritArt      = LoadArt(TEXT("spirit.png"));

	BulletOrb      = LoadArt(TEXT("bullet_orb.png"));
	BulletPetal    = LoadArt(TEXT("bullet_petal.png"));
	BulletStar     = LoadArt(TEXT("bullet_star.png"));
	BulletCrescent = LoadArt(TEXT("bullet_crescent.png"));
	BulletSakura   = LoadArt(TEXT("bullet_sakura.png"));
	BulletMaple    = LoadArt(TEXT("bullet_maple.png"));
	BulletSnow     = LoadArt(TEXT("bullet_snow.png"));
	BulletWater    = LoadArt(TEXT("bullet_water.png"));
	BulletFire     = LoadArt(TEXT("bullet_fire.png"));
	BulletChrys    = LoadArt(TEXT("bullet_chrys.png"));
	BulletTile     = LoadArt(TEXT("bullet_tile.png"));
	MagicCircle    = LoadArt(TEXT("magic_circle.png"));
	ShotTalisman   = LoadArt(TEXT("shot_talisman.png"));
	ItemPower      = LoadArt(TEXT("item_power.png"));
	ItemPoint      = LoadArt(TEXT("item_point.png"));
	IconLife       = LoadArt(TEXT("icon_life.png"));
	IconBomb       = LoadArt(TEXT("icon_bomb.png"));

	TitleArt       = LoadArt(TEXT("title.png"));
	TitleKeyArt    = LoadArt(TEXT("title_art.png"));
	StageBg        = LoadArt(TEXT("bg_stage.png"), true);
	SpellBg        = LoadArt(TEXT("bg_spell.png"), true);
	UiPattern      = LoadArt(TEXT("ui_pattern.png"), true);
	// per-stage art: stage 1 uses the originals, later stages <name><N>.png (missing files fall back to stage 1)
	BossPortraits.Reset(); BossBodies.Reset(); StageBgs.Reset();
	BossPortraits.Add(PortraitBoss); BossBodies.Add(BossBody); StageBgs.Add(StageBg);
	for (int32 i = 2; i <= ADanmakuWorld::ExtraStage + 1; ++i)
	{
		BossPortraits.Add(LoadArt(*FString::Printf(TEXT("portrait_boss%d.png"), i)));
		BossBodies.Add(LoadArt(*FString::Printf(TEXT("boss%d_body.png"), i)));
		StageBgs.Add(LoadArt(*FString::Printf(TEXT("bg_stage%d.png"), i), true));
	}
	PlayerSheets = { LoadArt(TEXT("player_back_sheet.png")), LoadArt(TEXT("player_back_sou_sheet.png")), LoadArt(TEXT("player_back_haru_sheet.png")), LoadArt(TEXT("player_back_yuki_sheet.png")) };
	BossSheets.Reset();
	BossSheets.Add(LoadArt(TEXT("boss_body_sheet.png")));
	for (int32 i = 2; i <= ADanmakuWorld::ExtraStage + 1; ++i) BossSheets.Add(LoadArt(*FString::Printf(TEXT("boss%d_body_sheet.png"), i)));
	// Extra-IF: the four beasts take art indices 7..10, the stage background is index 7
	StageBgs.Add(LoadArt(TEXT("bg_stage8.png"), true));
	for (int32 i = 1; i <= 4; ++i)
	{
		BossPortraits.Add(LoadArt(*FString::Printf(TEXT("portrait_beast%d.png"), i)));
		BossBodies.Add(LoadArt(*FString::Printf(TEXT("beast%d_body.png"), i)));
		BossSheets.Add(LoadArt(*FString::Printf(TEXT("beast%d_body_sheet.png"), i)));
	}
	if (MagicCircle) RingTex = MagicCircle;

	// Runtime font straight from the TTF (Noto Serif TC, OFL) so Chinese text renders
	const FString FontPath = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / TEXT("Fonts/NotoSerifTC-SemiBold.ttf"));
	if (IFileManager::Get().FileExists(*FontPath))
	{
		UiFont = NewObject<UFont>(this);
		UiFont->FontCacheType = EFontCacheType::Runtime;
		UiFont->LegacyFontSize = 20;
		FTypefaceEntry& Entry = UiFont->CompositeFont.DefaultTypeface.Fonts.AddDefaulted_GetRef();
		Entry.Name = TEXT("Regular");
		Entry.Font = FFontData(FontPath, EFontHinting::Default, EFontLoadingPolicy::LazyLoad);
	}
}

ADanmakuWorld* ADanmakuHUD::GetSim() const
{
	const ADanmakuGameMode* GM = GetWorld() ? GetWorld()->GetAuthGameMode<ADanmakuGameMode>() : nullptr;
	return GM ? GM->Sim.Get() : nullptr;
}

UTexture2D* ADanmakuHUD::PlayerBackFor(int32 Char) const
{
	return PlayerBacks.IsValidIndex(Char) && PlayerBacks[Char] ? PlayerBacks[Char].Get() : (PlayerBacks.Num() ? PlayerBacks[0].Get() : nullptr);
}

UTexture2D* ADanmakuHUD::PortraitFor(int32 Char) const
{
	return Portraits.IsValidIndex(Char) && Portraits[Char] ? Portraits[Char].Get() : (Portraits.Num() ? Portraits[0].Get() : nullptr);
}

UTexture2D* ADanmakuHUD::BulletTex(uint8 Shape) const
{
	switch (EBulletShape(Shape))
	{
	case EBulletShape::Rice:
	case EBulletShape::Comet:    return BulletPetal;
	case EBulletShape::Star:     return BulletStar;
	case EBulletShape::Crescent: return BulletCrescent;
	case EBulletShape::Sakura:   return BulletSakura ? BulletSakura.Get() : BulletStar.Get();
	case EBulletShape::Maple:    return BulletMaple ? BulletMaple.Get() : BulletStar.Get();
	case EBulletShape::Snow:     return BulletSnow ? BulletSnow.Get() : BulletStar.Get();
	case EBulletShape::Water:    return BulletWater ? BulletWater.Get() : BulletPetal.Get();
	case EBulletShape::Fire:     return BulletFire ? BulletFire.Get() : BulletPetal.Get();
	case EBulletShape::Chrys:    return BulletChrys ? BulletChrys.Get() : BulletStar.Get();
	case EBulletShape::Tile:     return BulletTile ? BulletTile.Get() : BulletOrb.Get();
	default:                     return BulletOrb;
	}
}

UTexture2D* ADanmakuHUD::BossPortraitFor(const ADanmakuWorld& S) const
{
	const int32 I = S.GetBossArtIndex();
	return BossPortraits.IsValidIndex(I) && BossPortraits[I] ? BossPortraits[I].Get() : PortraitBoss.Get();
}

UTexture2D* ADanmakuHUD::BossBodyFor(const ADanmakuWorld& S) const
{
	const int32 I = S.GetBossArtIndex();
	return BossBodies.IsValidIndex(I) && BossBodies[I] ? BossBodies[I].Get() : BossBody.Get();
}

UTexture2D* ADanmakuHUD::StageBgFor(const ADanmakuWorld& S) const
{
	return StageBgs.IsValidIndex(S.StageIndex) && StageBgs[S.StageIndex] ? StageBgs[S.StageIndex].Get() : StageBg.Get();
}

TConstArrayView<FHairZone> ADanmakuHUD::BossBodyHair(const ADanmakuWorld& S) const
{
	switch (S.GetBossArtIndex())
	{
	case 0: return SiriusBodyHair;
	case 1: return ChiakiBodyHair;
	default: return LongBodyHair;
	}
}

TConstArrayView<FHairZone> ADanmakuHUD::BossPortraitHair(const ADanmakuWorld& S) const
{
	switch (S.GetBossArtIndex())
	{
	case 0: return SiriusPortraitHair;
	case 1: return ChiakiPortraitHair;
	default: return LongPortraitHair;
	}
}

// =====================================================================================
// Primitives
// =====================================================================================

FVector2D ADanmakuHUD::ToScreen(const FVector2f& P) const
{
	return Origin + FVector2D(P.X, P.Y) * Scale;
}

void ADanmakuHUD::Sprite(UTexture2D* Tex, const FVector2f& Center, const FVector2D& SizeUnits, FLinearColor Color, ESimpleElementBlendMode Blend, float RotDeg)
{
	if (!Tex || !Tex->GetResource()) return;
	if (Blend == SE_BLEND_Additive)
	{
		Color.R *= Color.A; Color.G *= Color.A; Color.B *= Color.A; Color.A = 1.f;
	}
	const FVector2D Size = SizeUnits * Scale;
	FCanvasTileItem Item(ToScreen(Center) - Size * 0.5, Tex->GetResource(), Size, Color);
	Item.BlendMode = Blend;
	if (RotDeg != 0.f)
	{
		Item.Rotation = FRotator(0.f, RotDeg, 0.f);
		Item.PivotPoint = FVector2D(0.5, 0.5);
	}
	Canvas->DrawItem(Item);
}

void ADanmakuHUD::ArtSprite(UTexture2D* Tex, const FVector2f& Center, float HeightUnits, const FLinearColor& Color, float RotDeg, float XScale)
{
	if (!Tex) return;
	const float Aspect = float(Tex->GetSizeX()) / float(FMath::Max(1, Tex->GetSizeY()));
	Sprite(Tex, Center, FVector2D(HeightUnits * Aspect * XScale, HeightUnits), Color, SE_BLEND_Translucent, RotDeg);
}

void ADanmakuHUD::SheetSprite(UTexture2D* Sheet, const FVector2f& Center, float HeightUnits, const FLinearColor& Color, float Lean, int32 Phase)
{
	if (!Sheet || !Sheet->GetResource()) return;
	constexpr int32 Cols = 12, Rows = 5;
	constexpr float PadY = 0.05f;   // must match Tools/anim/sprite_specs.py
	const float CellW = float(Sheet->GetSizeX()) / Cols, CellH = float(Sheet->GetSizeY()) / Rows;
	const int32 Row = FMath::Clamp(FMath::RoundToInt(Lean) + Rows / 2, 0, Rows - 1);
	const int32 Col = ((Phase % Cols) + Cols) % Cols;
	const float H = HeightUnits * (1.f + 2.f * PadY);
	const float W = H * CellW / CellH;
	const FVector2D TL = ToScreen(Center - FVector2f(W * 0.5f, H * 0.5f));
	FCanvasTileItem Item(TL, Sheet->GetResource(), FVector2D(W, H) * Scale,
		FVector2D(Col / double(Cols), Row / double(Rows)), FVector2D((Col + 1) / double(Cols), (Row + 1) / double(Rows)), Color);
	Item.BlendMode = SE_BLEND_Translucent;
	Canvas->DrawItem(Item);
}

void ADanmakuHUD::WarpSprite(UTexture2D* Tex, const FVector2f& Center, float HeightUnits, const FLinearColor& Color,
	float Trail, float Sway, float Phase, float CoreHalf, float FlutterTop, TConstArrayView<FHairZone> Hair)
{
	if (!Tex || !Tex->GetResource()) return;
	constexpr int32 GX = 14, GY = 18;
	const float Aspect = float(Tex->GetSizeX()) / float(FMath::Max(1, Tex->GetSizeY()));
	const float H = HeightUnits;
	const float W = HeightUnits * Aspect;
	const FVector2f TopLeft(Center.X - W * 0.5f, Center.Y - H * 0.5f);

	FVector2f P[GX + 1][GY + 1];
	for (int32 iy = 0; iy <= GY; ++iy)
	{
		const float V = float(iy) / GY;
		for (int32 ix = 0; ix <= GX; ++ix)
		{
			const float U = float(ix) / GX;
			// cloth: outer columns below FlutterTop, plus the hem
			const float Wc = Smooth(CoreHalf, CoreHalf + 0.22f, FMath::Abs(U - 0.5f)) * Smooth(FlutterTop, FlutterTop + 0.3f, V)
				+ 0.35f * Smooth(0.82f, 1.f, V);
			float DX = W * Wc * (Sway * 0.035f * FMath::Sin(Phase + V * 5.f + U * 3.f) - Trail * 0.07f * V);
			float DY = W * Wc * Sway * 0.012f * FMath::Cos(Phase * 1.3f + U * 4.f);

			// hair: anchored at the root, swinging more toward the tip, on its own slower rhythm
			float Wh = 0.f;
			for (const FHairZone& Z : Hair)
			{
				const float Wu = Smooth(Z.U0, Z.U0 + 0.07f, U) * (1.f - Smooth(Z.U1 - 0.07f, Z.U1, U));
				const float Wv = Smooth(Z.V0, Z.V1, V) * (1.f - Smooth(Z.V1, Z.V1 + 0.1f, V));
				Wh = FMath::Max(Wh, Z.Strength * Wu * Wv);
			}
			DX += W * Wh * (Sway * 0.05f * FMath::Sin(Phase * 0.8f + V * 3.5f + 1.3f) - Trail * 0.1f * V);
			DY += W * Wh * Sway * 0.012f * FMath::Cos(Phase * 0.9f + V * 2.f);

			P[ix][iy] = TopLeft + FVector2f(U * W + DX, V * H + DY);
		}
	}

	TArray<FCanvasUVTri> Tris;
	Tris.Reserve(GX * GY * 2);
	for (int32 iy = 0; iy < GY; ++iy)
	{
		for (int32 ix = 0; ix < GX; ++ix)
		{
			const FVector2D UV00(double(ix) / GX, double(iy) / GY), UV10(double(ix + 1) / GX, double(iy) / GY);
			const FVector2D UV01(double(ix) / GX, double(iy + 1) / GY), UV11(double(ix + 1) / GX, double(iy + 1) / GY);
			const FVector2D A = ToScreen(P[ix][iy]), B = ToScreen(P[ix + 1][iy]), C = ToScreen(P[ix][iy + 1]), D = ToScreen(P[ix + 1][iy + 1]);
			FCanvasUVTri T1;
			T1.V0_Pos = A; T1.V0_UV = UV00; T1.V0_Color = Color;
			T1.V1_Pos = B; T1.V1_UV = UV10; T1.V1_Color = Color;
			T1.V2_Pos = D; T1.V2_UV = UV11; T1.V2_Color = Color;
			FCanvasUVTri T2;
			T2.V0_Pos = A; T2.V0_UV = UV00; T2.V0_Color = Color;
			T2.V1_Pos = D; T2.V1_UV = UV11; T2.V1_Color = Color;
			T2.V2_Pos = C; T2.V2_UV = UV01; T2.V2_Color = Color;
			Tris.Add(T1);
			Tris.Add(T2);
		}
	}
	FCanvasTriangleItem Item(Tris, Tex->GetResource());
	Item.BlendMode = SE_BLEND_Translucent;
	Canvas->DrawItem(Item);
}

void ADanmakuHUD::Beam(const FVector2f& From, const FVector2f& To, float Width, const FLinearColor& Color)
{
	const FVector2f D = To - From;
	const float Len = D.Size();
	if (Len < 1.f) return;
	Sprite(SoftTex, (From + To) * 0.5f, FVector2D(Width, Len), Color, SE_BLEND_Additive, AngleOf(D) + 90.f);
}

void ADanmakuHUD::TiledRect(UTexture2D* Tex, float X, float Y, float W, float H, float TilePx, const FLinearColor& Color)
{
	if (!Tex || !Tex->GetResource() || W <= 0.f || H <= 0.f) return;
	FCanvasTileItem Item(FVector2D(X, Y), Tex->GetResource(), FVector2D(W, H),
		FVector2D(X / TilePx, Y / TilePx), FVector2D((X + W) / TilePx, (Y + H) / TilePx), Color);
	Item.BlendMode = SE_BLEND_Opaque;
	Canvas->DrawItem(Item);
}

void ADanmakuHUD::ScreenRect(float X, float Y, float W, float H, const FLinearColor& Color)
{
	FCanvasTileItem Item(FVector2D(X, Y), FVector2D(W, H), Color);
	Item.BlendMode = SE_BLEND_Translucent;
	Canvas->DrawItem(Item);
}

void ADanmakuHUD::FieldRect(float X, float Y, float W, float H, const FLinearColor& Color)
{
	const FVector2D P = ToScreen(FVector2f(X, Y));
	ScreenRect(P.X, P.Y, W * Scale, H * Scale, Color);
}

void ADanmakuHUD::InkPanel(float X, float Y, float W, float H, float Alpha)
{
	const float T = FMath::Max(1.f, 2.f * TextScale);
	ScreenRect(X, Y, W, H, FLinearColor(0.02f, 0.01f, 0.06f, Alpha));
	ScreenRect(X, Y, W, T, WithAlpha(Gold, 0.9f));
	ScreenRect(X, Y + H - T, W, T, WithAlpha(Gold, 0.9f));
	ScreenRect(X, Y + T * 2.f, W, 1.f, WithAlpha(Gold, 0.35f));
	ScreenRect(X, Y + H - T * 3.f, W, 1.f, WithAlpha(Gold, 0.35f));
}

float ADanmakuHUD::TextWidth(const FString& Text, float InScale)
{
	UFont* Font = UiFont ? UiFont.Get() : GEngine->GetMediumFont();
	const float K = InScale * TextScale * (UiFont ? 1.f : 1.3f);
	float XL = 0.f, YL = 0.f;
	Canvas->TextSize(Font, TrStr(Text), XL, YL, K, K);
	return XL;
}

TArray<FString> ADanmakuHUD::WrapPx(const FString& InText, float MaxPx, float InScale)
{
	const FString Text = TrStr(InText);
	TArray<FString> Lines;
	FString Cur;
	auto Push = [&](const FString& Piece, const TCHAR* Sep)
	{
		const FString Try = Cur.IsEmpty() ? Piece : Cur + Sep + Piece;
		if (!Cur.IsEmpty() && TextWidth(Try, InScale) > MaxPx) { Lines.Add(Cur); Cur = Piece; }
		else Cur = Try;
	};
	if (IsEnglish())
	{
		TArray<FString> Words;
		Text.ParseIntoArray(Words, TEXT(" "));
		for (const FString& W : Words) Push(W, TEXT(" "));
	}
	else
	{
		const FString NoLead(TEXT("，。！？」）、…"));
		for (int32 i = 0; i < Text.Len(); ++i)
		{
			FString Piece = FString::Chr(Text[i]);
			// keep Latin words ("Boss", "Bomb") in one piece
			while (FChar::IsAlnum(Text[i]) && Text[i] < 128 && i + 1 < Text.Len() && FChar::IsAlnum(Text[i + 1]) && Text[i + 1] < 128) Piece.AppendChar(Text[++i]);
			// keep closing punctuation with the character before it
			while (i + 1 < Text.Len() && NoLead.Contains(FString::Chr(Text[i + 1]))) Piece.AppendChar(Text[++i]);
			Push(Piece, TEXT(""));
		}
	}
	if (!Cur.IsEmpty()) Lines.Add(Cur);
	return Lines;
}

void ADanmakuHUD::ScreenText(const FString& Text, float X, float Y, const FLinearColor& Color, float InScale, bool bCenter, float MaxWidth)
{
	UFont* Font = UiFont ? UiFont.Get() : GEngine->GetMediumFont();
	const FString Shown = TrStr(Text);
	if (MaxWidth > 0.f)
	{
		const float W = TextWidth(Shown, InScale);
		if (W > MaxWidth) InScale *= MaxWidth / W;
	}
	FCanvasTextItem Item(FVector2D(X, Y), FText::FromString(Shown), Font, Color);
	const float K = InScale * TextScale * (UiFont ? 1.f : 1.3f);
	Item.Scale = FVector2D(K, K);
	Item.EnableShadow(FLinearColor(0.f, 0.f, 0.f, 0.85f), FVector2D(1.5f, 1.5f));
	Item.bCentreX = bCenter;
	Canvas->DrawItem(Item);
}

void ADanmakuHUD::FieldText(const FString& Text, float X, float Y, const FLinearColor& Color, float InScale, bool bCenter, float MaxWidth)
{
	const FVector2D P = ToScreen(FVector2f(X, Y));
	ScreenText(Text, P.X, P.Y, Color, InScale, bCenter, MaxWidth * Scale);
}

TArray<FString> ADanmakuHUD::WrapText(const FString& InText, int32 MaxChars)
{
	TArray<FString> Lines;
	const FString Text = TrStr(InText);
	if (IsEnglish())
	{
		// English: wrap on spaces, about twice as many characters fit on a line
		const int32 Max = FMath::RoundToInt(MaxChars * 1.75f);
		TArray<FString> Words;
		Text.ParseIntoArray(Words, TEXT(" "));
		FString Cur;
		for (const FString& W : Words)
		{
			if (!Cur.IsEmpty() && Cur.Len() + 1 + W.Len() > Max) { Lines.Add(Cur); Cur.Reset(); }
			Cur += Cur.IsEmpty() ? W : TEXT(" ") + W;
		}
		if (!Cur.IsEmpty()) Lines.Add(Cur);
		return Lines;
	}
	FString Cur;
	for (int32 i = 0; i < Text.Len(); ++i)
	{
		Cur.AppendChar(Text[i]);
		// don't start a line with closing punctuation
		const bool bNextIsPunct = i + 1 < Text.Len() && FString(TEXT("，。！？」）、…")).Contains(FString::Chr(Text[i + 1]));
		if (Cur.Len() >= MaxChars && !bNextIsPunct)
		{
			Lines.Add(Cur);
			Cur.Reset();
		}
	}
	if (!Cur.IsEmpty()) Lines.Add(Cur);
	return Lines;
}

// =====================================================================================
// Main draw
// =====================================================================================

void ADanmakuHUD::DrawHUD()
{
	Super::DrawHUD();
	if (!Canvas) return;
	if (!bTexturesLoaded) LoadTextures();

	const float SW = Canvas->ClipX;
	const float SH = Canvas->ClipY;
	TextScale = SH / 720.f;

	if (const UWorld* W = GetWorld())
	{
		const float Dt = W->GetDeltaSeconds();
		if (Dt > 0.f) SmoothedFPS = FMath::Lerp(SmoothedFPS, 1.f / Dt, 0.05f);
	}

	ScreenRect(0.f, 0.f, SW, SH, PanelColor);
	ADanmakuWorld* SimPtr = GetSim();
	if (!SimPtr) return;
	const ADanmakuWorld& S = *SimPtr;

	if (S.State == EDanmakuState::Title) { DrawTitle(S, SW, SH); return; }
	if (S.State == EDanmakuState::CharSelect) { DrawSelect(S, SW, SH); return; }
	if (S.State == EDanmakuState::DiffSelect) { DrawDiffSelect(S, SW, SH); return; }
	if (S.State == EDanmakuState::StageSelect) { DrawStageSelect(S, SW, SH); return; }
	if (S.State == EDanmakuState::SpellSelect) { DrawSpellSelect(S, SW, SH); return; }

	Scale = (SH * 0.94f) / FieldH;
	Origin = FVector2D(FMath::Max(SH * 0.03f, (SW - FieldW * Scale) * 0.3f), SH * 0.03f);

	DrawBackground(S);
	DrawItems(S);
	DrawEnemies(S);
	DrawBoss(S);
	DrawShots(S);
	DrawPlayer(S);
	DrawParticles(S);
	DrawBullets(S);
	DrawBeams(S);
	DrawFog(S);
	DrawBomb(S);
	DrawHitbox(S);
	DrawPortraits(S);
	DrawBossUI(S);
	DrawFrame(SW, SH);
	DrawSidebar(S, SW, SH);
	DrawOverlays(S);
}

// =====================================================================================
// Title & character select (screen-space: Origin = 0, Scale = 1 so "units" are pixels)
// =====================================================================================

void ADanmakuHUD::DrawTitle(const ADanmakuWorld& S, float SW, float SH)
{
	Origin = FVector2D::ZeroVector;
	Scale = 1.f;
	if (TitleKeyArt)
	{
		const float AW = TitleKeyArt->GetSizeX(), AH = TitleKeyArt->GetSizeY();
		const float K = FMath::Max(SW / AW, SH / AH) * (1.02f + FMath::Sin(S.Frame * 0.004f) * 0.01f);
		FCanvasTileItem Art(FVector2D((SW - AW * K) * 0.5f, (SH - AH * K) * 0.5f), TitleKeyArt->GetResource(), FVector2D(AW * K, AH * K), FLinearColor::White);
		Art.BlendMode = SE_BLEND_Opaque;
		Canvas->DrawItem(Art);
	}
	constexpr int32 Bands = 32;
	for (int32 b = 0; b < Bands; ++b)
	{
		const float T = float(b) / Bands;
		ScreenRect(SW * 0.6f * T, 0.f, SW * 0.6f / Bands + 1.f, SH, FLinearColor(0.01f, 0.0f, 0.05f, 0.5f * FMath::Square(1.f - T)));
	}
	if (TitleArt)
	{
		const float LH = SH * 0.26f;
		const float LA = float(TitleArt->GetSizeX()) / float(FMath::Max(1, TitleArt->GetSizeY()));
		FCanvasTileItem Logo(FVector2D(SW * 0.04f, SH * 0.12f), TitleArt->GetResource(), FVector2D(LH * LA, LH), FLinearColor::White);
		Logo.BlendMode = SE_BLEND_Translucent;
		Canvas->DrawItem(Logo);
	}
	const float MX = SW * 0.07f;
	const TCHAR* Items[] = { TEXT("開始遊戲"), TEXT("Extra"), TEXT("Extra-IF"), TEXT("關卡練習"), TEXT("符卡練習"), IsEnglish() ? TEXT("Language: English") : TEXT("語言：繁體中文") };
	const bool bExtraOk = ADanmakuWorld::IsExtraAvailable();
	for (int32 i = 0; i < 6; ++i)
	{
		const bool bSel = i == S.TitleCursor;
		const bool bLocked = (i == 1 && !bExtraOk) || (i == 2 && !ADanmakuWorld::IsIfAvailable());
		const float Y = SH * 0.42f + i * 38.f * TextScale;
		const float X = MX + (bSel ? 18.f * TextScale : 0.f);
		if (bSel && MagicCircle) Sprite(MagicCircle, FVector2f(X - 20.f * TextScale, Y + 18.f * TextScale), FVector2D(34.f * TextScale), WithAlpha(GoldText, 0.8f), SE_BLEND_Additive, S.Frame * 1.2f);
		FLinearColor Col = bSel ? FLinearColor::White : LabelColor;
		if (bLocked) Col = FLinearColor(0.45f, 0.42f, 0.5f);
		ScreenText(Items[i], X, Y, Col, bSel ? 1.45f : 1.25f);
		if (bLocked && bSel) ScreenText(TEXT("（尚未開放）"), X + 120.f * TextScale, Y + 8.f * TextScale, Col, 0.8f);
	}
	const TCHAR* Help[] = { TEXT("方向鍵 / WASD　移動　　Shift　低速"), TEXT("Z　射擊／決定　　X　Bomb／返回"), TEXT("P　暫停　　R　重來") };
	for (int32 i = 0; i < 3; ++i) ScreenText(Help[i], MX, SH * 0.82f + i * 28.f * TextScale, LabelColor, 0.9f);
}

void ADanmakuHUD::DrawSelect(const ADanmakuWorld& S, float SW, float SH)
{
	Origin = FVector2D::ZeroVector;
	Scale = 1.f;
	if (UiPattern) TiledRect(UiPattern, 0.f, 0.f, SW, SH, 300.f * TextScale, FLinearColor(0.7f, 0.7f, 0.78f, 1.f));
	ScreenRect(0.f, 0.f, SW, SH, FLinearColor(0.02f, 0.01f, 0.06f, 0.55f));

	ScreenText(TEXT("選擇自機"), SW * 0.5f, SH * 0.045f, GoldText, 1.7f, true);

	const float Gap = SW * 0.015f;
	const float CardW = (SW * 0.9f - Gap * 3.f) / NumCharacters;
	const float CardH = SH * 0.76f;
	const float X0 = SW * 0.05f;
	const float Y0 = SH * 0.13f;

	for (int32 i = 0; i < NumCharacters; ++i)
	{
		const bool bSel = i == S.SelectedCharacter;
		const FCharacterInfo& Info = GetCharacterInfo(ECharacter(i));
		const float Lift = bSel ? -8.f * TextScale : 0.f;
		const float X = X0 + i * (CardW + Gap);
		const float Y = Y0 + Lift;

		InkPanel(X, Y, CardW, CardH, bSel ? 0.85f : 0.7f);
		if (bSel)
		{
			const float T = FMath::Max(2.f, 3.f * TextScale);
			const FLinearColor Glow = WithAlpha(Info.Color, 0.6f + 0.3f * FMath::Sin(S.Frame * 0.1f));
			ScreenRect(X - T, Y - T, CardW + 2 * T, T, Glow);
			ScreenRect(X - T, Y + CardH, CardW + 2 * T, T, Glow);
			ScreenRect(X - T, Y, T, CardH, Glow);
			ScreenRect(X + CardW, Y, T, CardH, Glow);
			if (MagicCircle) Sprite(MagicCircle, FVector2f(X + CardW * 0.5f, Y + CardH * 0.3f), FVector2D(CardW * 0.95f), WithAlpha(Info.Color, 0.22f), SE_BLEND_Additive, S.Frame * 0.3f);
		}

		// portrait; English descriptions are longer, so the portrait gives up some room for them
		const bool bEn = IsEnglish();
		const float PortraitFrac = bEn ? 0.36f : 0.44f;
		if (UTexture2D* Tex = PortraitFor(i))
		{
			const float Aspect = float(Tex->GetSizeX()) / float(FMath::Max(1, Tex->GetSizeY()));
			const float PH = FMath::Min(CardH * PortraitFrac, CardW * 0.98f / Aspect);
			const FLinearColor Tint = bSel ? FLinearColor::White : FLinearColor(0.5f, 0.5f, 0.58f, 1.f);
			ArtSprite(Tex, FVector2f(X + CardW * 0.5f, Y + 12.f * TextScale + PH * 0.5f), PH, Tint);
		}

		float TY = Y + CardH * (PortraitFrac + 0.02f);
		const float TW = CardW * 0.9f;
		const float LineH = (bEn ? 19.f : 22.f) * TextScale;
		ScreenText(Info.Name, X + CardW * 0.5f, TY, bSel ? FLinearColor::White : LabelColor, bEn ? 1.3f : 1.6f, true, TW);
		TY += (bEn ? 40.f : 50.f) * TextScale;
		ScreenText(Info.Title, X + CardW * 0.5f, TY, WithAlpha(Info.Color, 1.f), bEn ? 0.85f : 1.0f, true, TW);
		TY += (bEn ? 26.f : 30.f) * TextScale;
		for (const FString& L : WrapPx(Info.ShotDesc, TW, bEn ? 0.7f : 0.8f))
		{
			ScreenText(L, X + CardW * 0.5f, TY, LabelColor, bEn ? 0.7f : 0.8f, true);
			TY += LineH;
		}
		TY += 6.f * TextScale;
		for (const FString& L : WrapPx(Info.BombName, TW, bEn ? 0.75f : 0.85f))
		{
			ScreenText(L, X + CardW * 0.5f, TY, GoldText, bEn ? 0.75f : 0.85f, true);
			TY += LineH;
		}
		TY += 2.f * TextScale;
		for (const FString& L : WrapPx(Info.BombDesc, TW, bEn ? 0.66f : 0.72f))
		{
			ScreenText(L, X + CardW * 0.5f, TY, FLinearColor(0.75f, 0.72f, 0.85f), bEn ? 0.66f : 0.72f, true);
			TY += (bEn ? 17.f : 20.f) * TextScale;
		}
		TY += 4.f * TextScale;
		ScreenText(FString(Tr(TEXT("速度"))) + FString::Printf(TEXT("　%g / %g"), Info.Speed, Info.FocusSpeed), X + CardW * 0.5f, TY, WithAlpha(Info.Color, 1.f), bEn ? 0.66f : 0.72f, true);
		TY += (bEn ? 17.f : 20.f) * TextScale;
		for (const TCHAR* Trait : { Info.Trait1, Info.Trait2 })
		{
			for (const FString& L : WrapPx(FString(TEXT("◆ ")) + Tr(Trait), TW, bEn ? 0.62f : 0.68f))
			{
				ScreenText(L, X + CardW * 0.5f, TY, FLinearColor(0.95f, 0.88f, 0.7f), bEn ? 0.62f : 0.68f, true);
				TY += (bEn ? 16.f : 19.f) * TextScale;
			}
		}
	}
	ScreenText(TEXT("←　→　選擇　　　Z　決定　　　X　返回"), SW * 0.5f, SH * 0.94f, LabelColor, 0.95f, true);
}

void ADanmakuHUD::DrawDiffSelect(const ADanmakuWorld& S, float SW, float SH)
{
	Origin = FVector2D::ZeroVector;
	Scale = 1.f;
	if (TitleKeyArt)
	{
		const float AW = TitleKeyArt->GetSizeX(), AH = TitleKeyArt->GetSizeY();
		const float K = FMath::Max(SW / AW, SH / AH);
		FCanvasTileItem Art(FVector2D((SW - AW * K) * 0.5f, (SH - AH * K) * 0.5f), TitleKeyArt->GetResource(), FVector2D(AW * K, AH * K), FLinearColor(0.55f, 0.55f, 0.65f, 1.f));
		Art.BlendMode = SE_BLEND_Opaque;
		Canvas->DrawItem(Art);
	}
	ScreenRect(0.f, 0.f, SW, SH, FLinearColor(0.02f, 0.01f, 0.06f, 0.5f));
	ScreenText(TEXT("選擇難度"), SW * 0.5f, SH * 0.07f, GoldText, 1.7f, true);

	const float BarW = SW * 0.5f, BarH = SH * 0.15f, GapY = SH * 0.03f;
	const float X = (SW - BarW) * 0.5f;
	float Y = SH * 0.2f;
	for (int32 i = 0; i < NumDifficulties; ++i)
	{
		const FDifficultyInfo& D = GetDifficultyInfo(i);
		const bool bSel = i == S.Difficulty;
		const float Shift = bSel ? 14.f * TextScale : 0.f;
		InkPanel(X + Shift, Y, BarW, BarH, bSel ? 0.88f : 0.62f);
		if (bSel)
		{
			const float T = FMath::Max(2.f, 3.f * TextScale);
			const FLinearColor Glow = WithAlpha(D.Color, 0.6f + 0.3f * FMath::Sin(S.Frame * 0.1f));
			ScreenRect(X + Shift - T, Y, T, BarH, Glow);
			ScreenRect(X + Shift + BarW, Y, T, BarH, Glow);
			if (MagicCircle) Sprite(MagicCircle, FVector2f(X + Shift + BarH * 0.55f, Y + BarH * 0.5f), FVector2D(BarH * 0.95f), WithAlpha(D.Color, 0.5f), SE_BLEND_Additive, S.Frame * 0.8f);
		}
		const FLinearColor TextCol = bSel ? FLinearColor::White : FLinearColor(0.6f, 0.58f, 0.7f);
		ScreenText(D.Name, X + Shift + BarH * 1.15f, Y + BarH * 0.12f, bSel ? WithAlpha(D.Color, 1.f) : TextCol, 1.6f);
		if (!IsEnglish()) ScreenText(D.Label, X + Shift + BarW * 0.62f, Y + BarH * 0.16f, TextCol, 1.3f);
		ScreenText(D.Desc, X + Shift + BarH * 1.15f, Y + BarH * 0.6f, bSel ? LabelColor : TextCol, 0.85f);
		Y += BarH + GapY;
	}
	ScreenText(TEXT("↑　↓　選擇　　　Z　決定　　　X　返回"), SW * 0.5f, SH * 0.93f, LabelColor, 0.95f, true);
}

void ADanmakuHUD::DrawSpellSelect(const ADanmakuWorld& S, float SW, float SH)
{
	Origin = FVector2D::ZeroVector;
	Scale = 1.f;
	if (UiPattern) TiledRect(UiPattern, 0.f, 0.f, SW, SH, 300.f * TextScale, FLinearColor(0.7f, 0.7f, 0.78f, 1.f));
	ScreenRect(0.f, 0.f, SW, SH, FLinearColor(0.02f, 0.01f, 0.06f, 0.6f));
	ScreenText(TEXT("符卡練習"), SW * 0.5f, SH * 0.04f, GoldText, 1.7f, true);

	// big preview of whoever casts the chosen card, behind the list
	const int32 Art = S.GetSpellEntryArt(S.SpellCursor);
	UTexture2D* P = BossPortraits.IsValidIndex(Art) && BossPortraits[Art] ? BossPortraits[Art].Get() : PortraitBoss.Get();
	if (P)
	{
		const float PH = SH * 0.8f;
		ArtSprite(P, FVector2f(SW * 0.8f, SH * 0.13f + PH * 0.5f), PH, FLinearColor(1.f, 1.f, 1.f, 0.95f));
	}

	const int32 N = S.GetSpellEntryCount();
	constexpr int32 Rows = 13;
	const int32 First = FMath::Clamp(S.SpellCursor - Rows / 2, 0, FMath::Max(0, N - Rows));
	const float RowH = SH * 0.058f;
	const float ListW = SW * 0.62f;
	float Y = SH * 0.12f;
	for (int32 i = First; i < FMath::Min(N, First + Rows); ++i)
	{
		const bool bSel = i == S.SpellCursor;
		const float X = SW * 0.04f + (bSel ? 14.f * TextScale : 0.f);
		ScreenRect(X, Y, ListW, RowH * 0.88f, bSel ? FLinearColor(0.16f, 0.1f, 0.3f, 0.92f) : FLinearColor(0.05f, 0.03f, 0.12f, 0.7f));
		if (bSel) ScreenRect(X, Y, 4.f * TextScale, RowH * 0.88f, GoldText);
		const FLinearColor TextCol = bSel ? FLinearColor::White : FLinearColor(0.62f, 0.6f, 0.72f);
		ScreenText(FString::Printf(TEXT("%02d"), i + 1), X + 10.f * TextScale, Y + RowH * 0.16f, bSel ? GoldText : TextCol, 0.8f);
		ScreenText(S.GetSpellEntryLabel(i), X + 46.f * TextScale, Y + RowH * 0.16f, bSel ? GoldText : TextCol, 0.8f, false, ListW * 0.3f);
		ScreenText(S.GetSpellEntryName(i), X + ListW * 0.36f, Y + RowH * 0.14f, TextCol, 0.88f, false, ListW * 0.62f);
		Y += RowH;
	}
	// scroll position
	if (N > Rows)
	{
		const float BarTop = SH * 0.12f, BarH = RowH * Rows;
		ScreenRect(SW * 0.04f + ListW + 22.f * TextScale, BarTop, 3.f, BarH, FLinearColor(1.f, 1.f, 1.f, 0.15f));
		ScreenRect(SW * 0.04f + ListW + 20.f * TextScale, BarTop + BarH * First / N, 7.f, BarH * Rows / N, WithAlpha(GoldText, 0.8f));
	}
	ScreenText(TEXT("↑　↓　選擇　　←　→　翻頁　　Z　決定　　X　返回"), SW * 0.5f, SH * 0.93f, LabelColor, 0.95f, true);
}

void ADanmakuHUD::DrawStageSelect(const ADanmakuWorld& S, float SW, float SH)
{
	Origin = FVector2D::ZeroVector;
	Scale = 1.f;
	if (UiPattern) TiledRect(UiPattern, 0.f, 0.f, SW, SH, 300.f * TextScale, FLinearColor(0.7f, 0.7f, 0.78f, 1.f));
	ScreenRect(0.f, 0.f, SW, SH, FLinearColor(0.02f, 0.01f, 0.06f, 0.55f));
	ScreenText(TEXT("關卡練習"), SW * 0.5f, SH * 0.05f, GoldText, 1.7f, true);

	const int32 N = ADanmakuWorld::GetNumStages();
	const float BarW = SW * 0.46f, BarH = FMath::Min(SH * 0.12f, SH * 0.7f / FMath::Max(1, N));
	float Y = SH * 0.17f;
	for (int32 i = 0; i < N; ++i)
	{
		const bool bSel = i == S.PracticeStage;
		const float X = SW * 0.06f + (bSel ? 16.f * TextScale : 0.f);
		InkPanel(X, Y, BarW, BarH * 0.86f, bSel ? 0.88f : 0.6f);
		// small preview of that stage's background as a strip
		UTexture2D* Bg = StageBgs.IsValidIndex(i) && StageBgs[i] ? StageBgs[i].Get() : StageBg.Get();
		if (Bg)
		{
			FCanvasTileItem Strip(FVector2D(X + 6.f, Y + 6.f), Bg->GetResource(), FVector2D(BarH * 1.2f, BarH * 0.86f - 12.f),
				FVector2D(0.0, 0.3), FVector2D(1.0, 0.45), FLinearColor(bSel ? 1.f : 0.5f, bSel ? 1.f : 0.5f, bSel ? 1.f : 0.55f, 1.f));
			Strip.BlendMode = SE_BLEND_Opaque;
			Canvas->DrawItem(Strip);
		}
		const FLinearColor TextCol = bSel ? FLinearColor::White : FLinearColor(0.6f, 0.58f, 0.7f);
		const TCHAR* StageNames[] = { TEXT("第一面"), TEXT("第二面"), TEXT("第三面"), TEXT("第四面"), TEXT("第五面"), TEXT("第六面") };
		ScreenText(StageNames[FMath::Clamp(i, 0, 5)], X + BarH * 1.35f, Y + BarH * 0.12f, bSel ? GoldText : TextCol, 1.3f);
		Y += BarH;
	}
	// big preview of the chosen boss
	UTexture2D* P = BossPortraits.IsValidIndex(S.PracticeStage) && BossPortraits[S.PracticeStage] ? BossPortraits[S.PracticeStage].Get() : PortraitBoss.Get();
	if (P)
	{
		const float PH = SH * 0.74f;
		ArtSprite(P, FVector2f(SW * 0.76f, SH * 0.16f + PH * 0.5f), PH, FLinearColor::White);
	}
	ScreenText(TEXT("↑　↓　選擇　　　Z　決定　　　X　返回"), SW * 0.5f, SH * 0.93f, LabelColor, 0.95f, true);
}

// =====================================================================================
// Playfield layers
// =====================================================================================

void ADanmakuHUD::DrawBackground(const ADanmakuWorld& S)
{
	const bool bSpell = S.Boss.bActive && S.Boss.bSpell;
	SpellBlend = FMath::Lerp(SpellBlend, bSpell ? 1.f : 0.f, 0.04f);

	if (UTexture2D* Bg = StageBgFor(S))
	{
		const float TileH = FieldW * float(Bg->GetSizeY()) / float(FMath::Max(1, Bg->GetSizeX()));
		const double V0 = -double(S.Frame) * 0.45 / TileH;
		FCanvasTileItem Item(ToScreen(FVector2f(0.f, 0.f)), Bg->GetResource(), FVector2D(FieldW, FieldH) * Scale,
			FVector2D(0.0, V0), FVector2D(1.0, V0 + FieldH / TileH), FLinearColor::White);
		Item.BlendMode = SE_BLEND_Opaque;
		Canvas->DrawItem(Item);
	}
	else
	{
		FieldRect(0.f, 0.f, FieldW, FieldH, FLinearColor(0.03f, 0.02f, 0.09f, 1.f));
	}
	if (SpellBg && SpellBlend > 0.01f)
	{
		const float TileH = FieldW * float(SpellBg->GetSizeY()) / float(FMath::Max(1, SpellBg->GetSizeX()));
		const double V0 = -double(S.Frame) * 0.12 / TileH;
		FCanvasTileItem Item(ToScreen(FVector2f(0.f, 0.f)), SpellBg->GetResource(), FVector2D(FieldW, FieldH) * Scale,
			FVector2D(0.0, V0), FVector2D(1.0, V0 + FieldH / TileH), FLinearColor(1.f, 1.f, 1.f, SpellBlend));
		Item.BlendMode = SE_BLEND_Translucent;
		Canvas->DrawItem(Item);
	}
	FieldRect(0.f, 0.f, FieldW, FieldH, FLinearColor(0.01f, 0.0f, 0.04f, 0.32f));
	if (S.IsTimeSlowed())
	{
		FieldRect(0.f, 0.f, FieldW, FieldH, FLinearColor(0.1f, 0.35f, 0.15f, 0.12f));
	}

	for (int32 i = 0; i < 40; ++i)
	{
		const float H1 = Hash(i * 3 + 1), H2 = Hash(i * 3 + 2), H3 = Hash(i * 3 + 3);
		const FVector2f P(H1 * FieldW, FMath::Fmod(H2 * FieldH + S.Frame * (0.3f + H3 * 1.6f), FieldH));
		Sprite(SoftTex, P, FVector2D(2.f + H3 * 5.f), FLinearColor(0.65f, 0.75f, 1.f, 0.25f + H3 * 0.5f), SE_BLEND_Additive);
	}

	if (bSpell && MagicCircle)
	{
		Sprite(MagicCircle, S.Boss.Pos, FVector2D(330.f), FLinearColor(1.f, 0.85f, 0.55f, 0.13f * SpellBlend), SE_BLEND_Additive, S.Frame * 0.25f);
		Sprite(MagicCircle, S.Boss.Pos, FVector2D(190.f), FLinearColor(0.85f, 0.6f, 1.f, 0.12f * SpellBlend), SE_BLEND_Additive, -S.Frame * 0.4f);
	}
}

void ADanmakuHUD::DrawItems(const ADanmakuWorld& S)
{
	for (const FItem& I : S.Items)
	{
		FVector2f P = I.Pos;
		const bool bAbove = P.Y < 0.f;
		if (bAbove) P.Y = 6.f;
		const FLinearColor Tint(1.f, 1.f, 1.f, bAbove ? 0.5f : 1.f);
		switch (I.Type)
		{
		case EItemType::Power:    if (ItemPower) ArtSprite(ItemPower, P, 13.f, Tint); break;
		case EItemType::BigPower: if (ItemPower) ArtSprite(ItemPower, P, 22.f, Tint); break;
		case EItemType::Point:    if (ItemPoint) ArtSprite(ItemPoint, P, 13.f, Tint); break;
		case EItemType::LifePiece:
		case EItemType::BombPiece:
		{
			const bool bLife = I.Type == EItemType::LifePiece;
			const FLinearColor Glow = bLife ? FLinearColor(1.f, 0.5f, 0.7f, 0.7f * Tint.A) : FLinearColor(0.5f, 1.f, 0.6f, 0.7f * Tint.A);
			Sprite(SoftTex, P, FVector2D(34.f + 4.f * FMath::Sin(I.Age * 0.2f)), Glow, SE_BLEND_Additive);
			if (UTexture2D* Icon = bLife ? IconLife.Get() : IconBomb.Get()) ArtSprite(Icon, P, 18.f, Tint);
			break;
		}
		case EItemType::Star:
			Sprite(BulletStar ? BulletStar.Get() : SoftTex.Get(), P, FVector2D(12.f), FLinearColor(0.6f, 1.f, 0.6f, 0.8f * Tint.A), SE_BLEND_Additive, I.Age * 6.f);
			break;
		}
	}
}

void ADanmakuHUD::DrawEnemies(const ADanmakuWorld& S)
{
	for (const FEnemy& E : S.Enemies)
	{
		const float R = E.Radius;
		const FLinearColor Tint = E.HitFlash > 0 ? FLinearColor(1.f, 0.7f, 0.7f) : FLinearColor::White;
		Sprite(SoftTex, E.Pos, FVector2D(R * 4.f), WithAlpha(E.Color, 0.35f), SE_BLEND_Additive);
		UTexture2D* Art = E.Kind == 2 ? SpiritArt.Get() : (E.Kind == 1 ? FairyRedArt.Get() : FairyBlueArt.Get());
		const float T = (S.Frame + E.Age) + E.Pos.X;
		if (E.Kind == 2) WarpSprite(Art, E.Pos, R * 3.2f, Tint, -E.Vel.X * 0.4f, 1.6f, T * 0.22f, 0.16f, 0.0f);
		else             WarpSprite(Art, E.Pos, R * 3.2f, Tint, -E.Vel.X * 0.4f, 1.4f, T * 0.16f, 0.12f, 0.2f);
	}
}

void ADanmakuHUD::DrawBoss(const ADanmakuWorld& S)
{
	const FBossState& B = S.Boss;
	if (!B.bActive) return;

	Sprite(SoftTex, B.Pos, FVector2D(150.f), FLinearColor(0.45f, 0.35f, 1.f, 0.35f), SE_BLEND_Additive);
	Sprite(RingTex, B.Pos, FVector2D(110.f), FLinearColor(1.f, 0.85f, 0.55f, 0.55f), SE_BLEND_Additive, S.Frame * 0.8f);

	if (S.IsSurvivalPhase())
	{
		Sprite(RingTex, B.Pos, FVector2D(84.f + 6.f * FMath::Sin(S.Frame * 0.1f)), FLinearColor(1.f, 0.85f, 0.45f, 0.75f), SE_BLEND_Additive, -S.Frame * 1.5f);
		Sprite(SoftTex, B.Pos, FVector2D(110.f), FLinearColor(1.f, 0.8f, 0.4f, 0.25f), SE_BLEND_Additive);
	}
	if (S.IsShielded())
	{
		// spell card start: a few seconds where it can't be damaged
		const float K = FMath::Min(1.f, S.Boss.ShieldTimer / 30.f);
		Sprite(RingTex, B.Pos, FVector2D(78.f + 4.f * FMath::Sin(S.Frame * 0.3f)), FLinearColor(0.6f, 0.85f, 1.f, 0.8f * K), SE_BLEND_Additive, S.Frame * 2.f);
		Sprite(SoftTex, B.Pos, FVector2D(100.f), FLinearColor(0.5f, 0.75f, 1.f, 0.3f * K), SE_BLEND_Additive);
	}
	const FLinearColor BodyTint = B.HitFlash > 0 ? FLinearColor(1.f, 0.8f, 0.85f) : FLinearColor::White;
	// lean into the direction of travel (smoothed so the pose doesn't flicker)
	BossLean = FMath::Lerp(BossLean, FMath::Clamp(B.Vel.X * 1.6f, -2.f, 2.f), 0.15f);
	auto BeastSheet = [this](int32 Art) { return BossSheets.IsValidIndex(Art) ? BossSheets[Art].Get() : nullptr; };
	if (B.DepartTimer > 0 && B.DepartActor >= 0)
	{
		// the beast that just finished its cards flies off
		const float A = FMath::Min(1.f, B.DepartTimer / 40.f);
		SheetSprite(BeastSheet(ADanmakuWorld::BeastArt + B.DepartActor), B.DepartPos, 96.f, FLinearColor(1.f, 1.f, 1.f, A), B.DepartVel.X > 0.f ? 2.f : -2.f, S.Frame / 6);
	}
	if (S.IsQuadPhase())
	{
		static const FLinearColor Glow[4] = { FLinearColor(0.5f, 0.9f, 0.4f, 0.4f), FLinearColor(1.f, 0.3f, 0.3f, 0.4f), FLinearColor(0.9f, 0.9f, 1.f, 0.4f), FLinearColor(0.4f, 0.5f, 1.f, 0.4f) };
		for (int32 i = 0; i < 4; ++i)
		{
			Sprite(SoftTex, B.QuadPos[i], FVector2D(110.f), Glow[i], SE_BLEND_Additive);
			SheetSprite(BeastSheet(ADanmakuWorld::BeastArt + i), B.QuadPos[i] + FVector2f(0.f, 6.f), 80.f, BodyTint, 0.f, S.Frame / 6 + i * 3);
		}
		return;
	}
	const int32 ArtI = S.GetBossArtIndex();
	UTexture2D* Sheet = BossSheets.IsValidIndex(ArtI) ? BossSheets[ArtI].Get() : nullptr;
	if (Sheet)
	{
		SheetSprite(Sheet, B.Pos + FVector2f(0.f, 6.f), 96.f, BodyTint, BossLean, S.Frame / 6);
	}
	else if (UTexture2D* Body = BossBodyFor(S))
	{
		WarpSprite(Body, B.Pos + FVector2f(0.f, 6.f), 96.f, BodyTint, FMath::Clamp(B.Vel.X * 0.35f, -1.f, 1.f), 1.f, S.Frame * 0.08f,
			0.14f, 0.32f, BossBodyHair(S));
	}
	else
	{
		Sprite(DiscTex, B.Pos, FVector2D(30.f, 38.f), FLinearColor(0.3f, 0.25f, 0.6f));
	}
	FieldRect(B.Pos.X - 14.f, FieldH - 5.f, 28.f, 5.f, FLinearColor(1.f, 0.3f, 0.4f, 0.8f));
}

void ADanmakuHUD::DrawShots(const ADanmakuWorld& S)
{
	const FDanmakuPlayer& P = S.Player;
	const FLinearColor CharCol = GetCharacterInfo(P.Character).Color;
	for (const FShot& Shot : S.Shots)
	{
		const float Rot = AngleOf(Shot.Vel) + 90.f;
		switch (Shot.Type)
		{
		case EShotType::Talisman:
			Sprite(SoftTex, Shot.Pos, FVector2D(10.f, 26.f), FLinearColor(1.f, 0.5f, 0.5f, 0.35f), SE_BLEND_Additive, Rot);
			ArtSprite(ShotTalisman, Shot.Pos, 16.f, FLinearColor(1.f, 1.f, 1.f, 0.85f), Rot);
			break;
		case EShotType::Straight:
			Sprite(SoftTex, Shot.Pos, FVector2D(9.f, 26.f), WithAlpha(CharCol, 0.5f), SE_BLEND_Additive, Rot);
			ArtSprite(ShotTalisman, Shot.Pos, 15.f, FLinearColor(CharCol.R * 0.6f + 0.4f, CharCol.G * 0.6f + 0.4f, CharCol.B * 0.6f + 0.4f, 0.8f), Rot);
			break;
		case EShotType::Homing:
			Sprite(SoftTex, Shot.Pos, FVector2D(20.f), FLinearColor(0.5f, 1.f, 0.5f, 0.45f), SE_BLEND_Additive);
			ArtSprite(ShotTalisman, Shot.Pos, 15.f, FLinearColor(0.7f, 1.f, 0.7f, 0.95f), Rot);
			break;
		case EShotType::Doll:
			Sprite(BulletOrb ? BulletOrb.Get() : SoftTex.Get(), Shot.Pos, FVector2D(12.f), FLinearColor(0.95f, 0.75f, 1.f, 0.85f), SE_BLEND_Translucent);
			break;
		}
	}

	// Sou's bow laser
	if (P.bLaserOn)
	{
		const float Flicker = 0.85f + 0.15f * FMath::Sin(S.Frame * 1.7f);
		const FVector2f From = P.Pos + FVector2f(0.f, -14.f);
		const FVector2f To(P.Pos.X, -10.f);
		Beam(From, To, ADanmakuWorld::LaserHalfWidth * 3.2f, FLinearColor(0.45f, 0.7f, 1.f, 0.55f * Flicker));
		Beam(From, To, ADanmakuWorld::LaserHalfWidth * 1.1f, FLinearColor(0.85f, 0.95f, 1.f, 0.9f * Flicker));
		if (BulletStar) Sprite(BulletStar, From, FVector2D(30.f), FLinearColor(0.7f, 0.85f, 1.f, 0.9f), SE_BLEND_Additive, S.Frame * 9.f);
		for (int32 k = 0; k < 4; ++k)
		{
			const float Y = FMath::Fmod(S.Frame * 13.f + k * 97.f, FMath::Max(1.f, From.Y));
			Sprite(BulletStar ? BulletStar.Get() : SoftTex.Get(), FVector2f(P.Pos.X + FMath::Sin(S.Frame * 0.7f + k) * 6.f, Y), FVector2D(14.f), FLinearColor(0.75f, 0.9f, 1.f, 0.8f), SE_BLEND_Additive, S.Frame * 7.f);
		}
	}
}

void ADanmakuHUD::DrawPlayer(const ADanmakuWorld& S)
{
	const FDanmakuPlayer& P = S.Player;
	if (S.State == EDanmakuState::GameOver || P.DeathTimer > 0) return;

	// Yuki's puppets, connected by glowing threads
	if (P.Character == ECharacter::Yuki)
	{
		for (int32 i = 0; i < P.DollsAlive; ++i)
		{
			const FVector2f D = P.Dolls[i];
			Beam(P.Pos + FVector2f(i < 2 ? -8.f : 8.f, -6.f), D, 2.f, FLinearColor(0.9f, 0.75f, 1.f, 0.45f));
			Sprite(SoftTex, D, FVector2D(24.f), FLinearColor(0.85f, 0.6f, 1.f, 0.4f), SE_BLEND_Additive);
			if (FairyBlueArt) WarpSprite(FairyBlueArt, D, 18.f, FLinearColor(1.f, 0.85f, 1.f, 1.f), 0.f, 1.4f, S.Frame * 0.2f + i, 0.12f, 0.2f);
		}
	}

	if (P.Character == ECharacter::Haru && P.RespawnTimer == 0)
	{
		Sprite(RingTex, P.Pos, FVector2D(ADanmakuWorld::HaruSlowRadius * 2.f), FLinearColor(0.55f, 1.f, 0.6f, 0.22f), SE_BLEND_Additive, S.Frame * 0.4f);
	}
	if (P.Invuln > 0 && (S.Frame / 3) % 2 == 0) return;
	Sprite(SoftTex, P.Pos, FVector2D(46.f), WithAlpha(GetCharacterInfo(P.Character).Color, 0.3f), SE_BLEND_Additive);
	if (UTexture2D* Back = PlayerBackFor(int32(P.Character)))
	{
		UTexture2D* Sheet = PlayerSheets.IsValidIndex(int32(P.Character)) ? PlayerSheets[int32(P.Character)].Get() : nullptr;
		if (Sheet) SheetSprite(Sheet, P.Pos + FVector2f(0.f, -4.f), 58.f, FLinearColor::White, P.Lean / 6.f, S.Frame / 5);
		else ArtSprite(Back, P.Pos + FVector2f(0.f, -4.f), 58.f, FLinearColor::White);
	}
	else
	{
		Sprite(DiscTex, P.Pos, FVector2D(18.f, 24.f), FLinearColor(0.2f, 0.3f, 0.6f));
	}
}

void ADanmakuHUD::DrawBullets(const ADanmakuWorld& S)
{
	// grouped per texture so the canvas can batch them
	const EBulletShape Groups[][3] = {
		{ EBulletShape::Small, EBulletShape::Medium, EBulletShape::Large },
		{ EBulletShape::Rice, EBulletShape::Comet, EBulletShape::Comet },
		{ EBulletShape::Star, EBulletShape::Star, EBulletShape::Star },
		{ EBulletShape::Crescent, EBulletShape::Crescent, EBulletShape::Crescent },
		{ EBulletShape::Sakura, EBulletShape::Sakura, EBulletShape::Sakura },
		{ EBulletShape::Maple, EBulletShape::Maple, EBulletShape::Maple },
		{ EBulletShape::Snow, EBulletShape::Snow, EBulletShape::Snow },
		{ EBulletShape::Water, EBulletShape::Water, EBulletShape::Water },
		{ EBulletShape::Fire, EBulletShape::Fire, EBulletShape::Fire },
		{ EBulletShape::Chrys, EBulletShape::Chrys, EBulletShape::Chrys },
		{ EBulletShape::Tile, EBulletShape::Tile, EBulletShape::Tile },
	};
	for (const auto& G : Groups)
	{
		UTexture2D* Tex = BulletTex(uint8(G[0]));
		if (!Tex) Tex = DiscTex;
		for (int32 Pass = 0; Pass < 2; ++Pass)
		{
			for (const FBullet& B : S.Bullets)
			{
				if (B.Shape != G[0] && B.Shape != G[1] && B.Shape != G[2]) continue;
				const float V = BulletVisualSize(B.Shape);
				const float Spawn = B.Age < 8 ? (8 - B.Age) / 8.f : 0.f;
				const float K = 1.f + Spawn * 1.2f;
				float Rot = 0.f;
				FVector2D Size(V * 1.25f);
				switch (B.Shape)
				{
				case EBulletShape::Rice:     Rot = B.Angle + 90.f; Size = FVector2D(V * 1.6f); break;
				case EBulletShape::Comet:    Rot = B.Angle + 90.f; Size = FVector2D(V * 1.7f); break;
				case EBulletShape::Crescent: Rot = B.Angle + 90.f; Size = FVector2D(V * 1.6f); break;
				case EBulletShape::Star:     Rot = B.Age * 5.f;    Size = FVector2D(V * 1.7f); break;
				case EBulletShape::Sakura:
				case EBulletShape::Maple:
				case EBulletShape::Snow:
				case EBulletShape::Chrys:    Rot = B.Age * (B.Spin != 0.f ? B.Spin : 2.f); Size = FVector2D(V * 1.6f); break;
				case EBulletShape::Tile:     Rot = B.Spin != 0.f ? B.Age * B.Spin : 0.f;  Size = FVector2D(V * 1.5f); break;
				case EBulletShape::Water:
				case EBulletShape::Fire:     Rot = B.Angle - 90.f; Size = FVector2D(V * 1.8f); break;   // tip trails behind
				default: break;
				}
				Size *= B.Scale;
				if (Pass == 0)
				{
					Sprite(Tex, B.Pos, Size * K, WithAlpha(B.Color, 1.f - Spawn * 0.7f), SE_BLEND_Translucent, Rot);
				}
				else
				{
					Sprite(Tex, B.Pos, Size * 0.55f, FLinearColor(1.f, 1.f, 1.f, (1.f - Spawn) * 0.9f), SE_BLEND_Translucent, Rot);
				}
			}
		}
	}
}

void ADanmakuHUD::DrawBeams(const ADanmakuWorld& S)
{
	for (const FBullet& B : S.Bullets)
	{
		if (B.Behavior == 2)   // ADanmakuWorld::BehLotusSummon
		{
			const float Pulse = 1.f + 0.15f * FMath::Sin(B.Age * 0.4f);
			Sprite(RingTex, B.Pos, FVector2D(34.f * Pulse), WithAlpha(B.Color, 0.8f), SE_BLEND_Additive, B.Age * 3.f);
			FieldText(FString::FromInt(B.Param), B.Pos.X, B.Pos.Y - 10.f, FLinearColor(1.f, 1.f, 1.f, 1.f), 1.1f, true);
		}
		else if (B.Behavior == 4)   // assassin: a warning halo
		{
			Sprite(RingTex, B.Pos, FVector2D(40.f), WithAlpha(B.Color, 0.7f), SE_BLEND_Additive, -B.Age * 4.f);
		}
	}
	for (const FBeamHazard& Bm : S.Beams)
	{
		const FVector2f D = Dir(Bm.Angle);
		if (!Bm.IsActive())
		{
			// warning: the full width of the strike glows red, with a bright centre line that blinks faster near impact
			const float Left = float(Bm.Warn - Bm.Age) / float(FMath::Max(1, Bm.Warn));
			const float Speed = FMath::Lerp(0.9f, 0.25f, Left);
			const float Blink = 0.5f + 0.5f * FMath::Sin(Bm.Age * Speed);
			const FVector2f To = Bm.Origin + D * Bm.MaxLength;
			const FLinearColor Warn(1.f, 0.25f, 0.2f, 1.f);
			const FVector2f Perp(-D.Y, D.X);
			for (float Side : { -0.5f, 0.5f })
			{
				const FVector2f Off = Perp * (Bm.Width * Side);
				FCanvasLineItem Edge(ToScreen(Bm.Origin + Off), ToScreen(To + Off));
				Edge.SetColor(WithAlpha(Warn, 0.55f + 0.35f * Blink));
				Edge.LineThickness = FMath::Max(1.f, 1.5f * Scale);
				Canvas->DrawItem(Edge);
			}
			Beam(Bm.Origin, To, Bm.Width * 1.6f, WithAlpha(Warn, 0.18f + 0.22f * Blink));
			Beam(Bm.Origin, To, FMath::Max(3.f, Bm.Width * 0.25f), FLinearColor(1.f, 0.9f, 0.8f, 0.45f + 0.5f * Blink));
			Sprite(SoftTex, Bm.Origin, FVector2D(Bm.Width * 2.6f), WithAlpha(Warn, 0.6f + 0.3f * Blink), SE_BLEND_Additive);
			FieldText(TEXT("！"), Bm.Origin.X - D.X * 12.f, FMath::Clamp(Bm.Origin.Y + D.Y * 14.f - 10.f, 2.f, FieldH - 24.f), WithAlpha(Warn, 0.6f + 0.4f * Blink), 1.1f, true);
			continue;
		}
		const int32 End = Bm.Warn + Bm.Rise + Bm.Hold;
		const float Fade = FMath::Clamp(float(End - Bm.Age) / 12.f, 0.f, 1.f);
		const FVector2f Tip = Bm.Origin + D * Bm.Length;
		Beam(Bm.Origin, Tip, Bm.Width * 2.4f, WithAlpha(Bm.Color, 0.45f * Fade));
		Beam(Bm.Origin, Tip, Bm.Width * 1.1f, WithAlpha(Bm.Color, 0.9f * Fade));
		Beam(Bm.Origin, Tip, Bm.Width * 0.45f, FLinearColor(1.f, 1.f, 1.f, 0.9f * Fade));
		Sprite(SoftTex, Tip, FVector2D(Bm.Width * 2.2f), WithAlpha(Bm.Color, 0.8f * Fade), SE_BLEND_Additive);
	}
}

void ADanmakuHUD::DrawFog(const ADanmakuWorld& S)
{
	if (S.FogRadius <= 0.f || !HoleTex) return;
	const float R = S.FogRadius;
	const FVector2f P = S.Player.Pos;
	const FLinearColor Fog(0.07f, 0.07f, 0.13f, 0.96f);
	// soft-edged hole around the player, solid fog everywhere else (clipped to the field)
	const float L = FMath::Max(0.f, P.X - R), T = FMath::Max(0.f, P.Y - R);
	const float Rr = FMath::Min(FieldW, P.X + R), Bt = FMath::Min(FieldH, P.Y + R);
	FieldRect(0.f, 0.f, FieldW, T, Fog);
	FieldRect(0.f, Bt, FieldW, FieldH - Bt, Fog);
	FieldRect(0.f, T, L, Bt - T, Fog);
	FieldRect(Rr, T, FieldW - Rr, Bt - T, Fog);
	Sprite(HoleTex, P, FVector2D(R * 2.f), Fog, SE_BLEND_Translucent);
}

void ADanmakuHUD::DrawParticles(const ADanmakuWorld& S)
{
	for (const FParticle& P : S.Particles)
	{
		const float T = float(P.Life) / P.MaxLife;
		if (P.bRing) Sprite(RingTex, P.Pos, FVector2D(10.f + P.Size * (1.f - T)), WithAlpha(P.Color, T * 0.9f), SE_BLEND_Additive, (1.f - T) * 90.f);
		else         Sprite(SoftTex, P.Pos, FVector2D(1.f + P.Size * T), WithAlpha(P.Color, T), SE_BLEND_Additive);
	}
}

void ADanmakuHUD::DrawBomb(const ADanmakuWorld& S)
{
	if (S.BombTime <= 0) return;
	const float T = float(S.BombTime) / float(FMath::Max(1, S.BombDuration));
	const FDanmakuPlayer& P = S.Player;
	const FLinearColor C = GetCharacterInfo(S.BombKind).Color;

	switch (S.BombKind)
	{
	case ECharacter::Ren:
		FieldRect(0.f, 0.f, FieldW, FieldH, FLinearColor(0.8f, 0.9f, 1.f, 0.1f * T));
		Sprite(RingTex, S.BombCenter, FVector2D(S.BombRadius * 2.f), FLinearColor(1.f, 0.9f, 0.6f, T), SE_BLEND_Additive, S.Frame * 2.f);
		Sprite(SoftTex, S.BombCenter, FVector2D(S.BombRadius * 1.6f), FLinearColor(0.6f, 0.8f, 1.f, 0.25f * T), SE_BLEND_Additive);
		break;
	case ECharacter::Sou:
	{
		const float W = ADanmakuWorld::ThunderHalfWidth * 2.f;
		const FVector2f From = P.Pos + FVector2f(0.f, -10.f), To(P.Pos.X, -20.f);
		Beam(From, To, W * 1.6f, FLinearColor(1.f, 0.6f, 0.45f, 0.45f * T));
		Beam(From, To, W * 0.7f, FLinearColor(1.f, 0.9f, 0.75f, 0.8f * T));
		for (int32 k = 0; k < 8; ++k)
		{
			const FVector2f SP(P.Pos.X + FMath::Sin(S.Frame * 0.9f + k * 2.1f) * W * 0.4f, FMath::Fmod(S.Frame * 11.f + k * 61.f, FMath::Max(1.f, From.Y)));
			Sprite(BulletStar ? BulletStar.Get() : SoftTex.Get(), SP, FVector2D(26.f), FLinearColor(0.7f, 0.85f, 1.f, T), SE_BLEND_Additive, S.Frame * 10.f + k * 40.f);
		}
		break;
	}
	case ECharacter::Haru:
		Sprite(RingTex, P.Pos, FVector2D(170.f), FLinearColor(0.5f, 1.f, 0.6f, 0.6f * FMath::Min(1.f, T * 4.f)), SE_BLEND_Additive, S.Frame * 0.5f);
		Sprite(RingTex, P.Pos, FVector2D(110.f), FLinearColor(0.7f, 1.f, 0.7f, 0.4f * FMath::Min(1.f, T * 4.f)), SE_BLEND_Additive, -S.Frame * 0.9f);
		break;
	case ECharacter::Yuki:
		Sprite(RingTex, P.Pos, FVector2D(140.f), FLinearColor(0.9f, 0.7f, 1.f, 0.55f * T), SE_BLEND_Additive, S.Frame * 1.5f);
		for (int32 i = 0; i < P.DollsAlive; ++i) Sprite(SoftTex, P.Dolls[i], FVector2D(46.f), FLinearColor(0.9f, 0.7f, 1.f, 0.6f * T), SE_BLEND_Additive);
		break;
	}

	// bomb name banner
	const int32 Elapsed = S.BombDuration - S.BombTime;
	if (Elapsed < 100)
	{
		const float A = FMath::Min(1.f, FMath::Min(Elapsed, 100 - Elapsed) / 15.f);
		const FVector2D Pn = ToScreen(FVector2f(8.f, FieldH - 52.f));
		InkPanel(Pn.X, Pn.Y, FieldW * 0.62f * Scale, 26.f * Scale, 0.75f * A);
		FieldText(GetCharacterInfo(S.BombKind).BombName, 16.f, FieldH - 50.f, WithAlpha(C, A), 1.0f, false);
	}
}

void ADanmakuHUD::DrawHitbox(const ADanmakuWorld& S)
{
	const FDanmakuPlayer& P = S.Player;
	if (S.State != EDanmakuState::Playing || !P.bFocus || P.DeathTimer > 0) return;
	Sprite(RingTex, P.Pos, FVector2D(52.f), FLinearColor(1.f, 0.95f, 0.8f, 0.65f), SE_BLEND_Additive, S.Frame * 3.f);
	Sprite(DiscTex, P.Pos, FVector2D(9.f), FLinearColor(1.f, 0.2f, 0.3f));
	Sprite(DiscTex, P.Pos, FVector2D(6.f), FLinearColor::White);
}

void ADanmakuHUD::DrawPortraits(const ADanmakuWorld& S)
{
	const float Breath = 0.f;   // portraits stay still; only the battle sprites animate
	auto DrawPortrait = [this, &S, Breath](UTexture2D* Tex, float CenterX, float SlideX, float Alpha, float Dim, float Lift, float Grow, TConstArrayView<FHairZone> Hair)
	{
		if (!Tex) return;
		const float H = 300.f * Grow * (1.f + 0.008f * Breath);
		const FVector2f C(CenterX + SlideX, FieldH - H * 0.5f + 4.f - Lift + Breath * 1.5f);
		ArtSprite(Tex, C, H, FLinearColor(Dim, Dim, Dim, Alpha));
	};

	const int32 Char = int32(S.Player.Character);
	TConstArrayView<FHairZone> PlayerHair = Char == int32(ECharacter::Sou) ? TConstArrayView<FHairZone>(SouPortraitHair) : TConstArrayView<FHairZone>();

	if (S.IsInDialogue())
	{
		const bool bBossTalks = S.GetDialogueSpeaker() >= 1;
		const float Hop = S.DialogueTimer < 12 ? FMath::Sin(S.DialogueTimer / 12.f * PI) * 8.f : 0.f;
		DrawPortrait(PortraitFor(Char), 95.f, bBossTalks ? -12.f : 0.f, 1.f, bBossTalks ? 0.45f : 1.f, bBossTalks ? 0.f : 6.f + Hop, bBossTalks ? 0.97f : 1.02f, PlayerHair);
		const int32 DArt = S.GetDialogueArt();
		UTexture2D* SpeakerArt = BossPortraits.IsValidIndex(DArt) && BossPortraits[DArt] ? BossPortraits[DArt].Get() : BossPortraitFor(S);
		DrawPortrait(SpeakerArt, FieldW - 95.f, bBossTalks ? 0.f : 12.f, 1.f, bBossTalks ? 1.f : 0.45f, bBossTalks ? 6.f + Hop : 0.f, bBossTalks ? 1.02f : 0.97f, BossPortraitHair(S));

		const FVector2D BoxPos = ToScreen(FVector2f(10.f, FieldH - 104.f));
		InkPanel(BoxPos.X, BoxPos.Y, (FieldW - 20.f) * Scale, 92.f * Scale, 0.84f);
		const FString Name = S.GetDialogueName();
		FieldText(Name, 22.f, FieldH - 99.f, bBossTalks ? FLinearColor(0.85f, 0.7f, 1.f) : WithAlpha(GetCharacterInfo(S.Player.Character).Color, 1.f), 1.05f, false, FieldW - 60.f);
		constexpr int32 LinesPerBox = 3;
		const TArray<FString> Lines = WrapPx(S.GetDialogueText(), (FieldW - 64.f) * Scale, 0.95f);
		const int32 Boxes = FMath::Max(1, (Lines.Num() + LinesPerBox - 1) / LinesPerBox);
		S.DialoguePageCount = Boxes;
		const int32 Page = FMath::Clamp(S.DialoguePage, 0, Boxes - 1);
		float Y = FieldH - 76.f;
		for (int32 i = Page * LinesPerBox; i < FMath::Min(Lines.Num(), (Page + 1) * LinesPerBox); ++i)
		{
			FieldText(Lines[i], 22.f, Y, FLinearColor::White, 0.95f, false);
			Y += 19.f;
		}
		const bool bMore = Page + 1 < Boxes;
		if ((S.Frame / 20) % 2 == 0) FieldText(bMore ? TEXT("Z ▼") : TEXT("Z ▶"), FieldW - 44.f, FieldH - 32.f, GoldText, 0.8f, false);
		return;
	}

	// spell card declaration: boss portrait sweeps across
	const FBossState& B = S.Boss;
	if (B.bActive && B.bSpell)
	{
		const int32 T = B.GapTimer > 0 ? 60 - B.GapTimer : 60 + B.PhaseTime;
		if (T < 110)
		{
			const float A = T < 20 ? T / 20.f : (T > 80 ? (110 - T) / 30.f : 1.f);
			DrawPortrait(BossPortraitFor(S), FieldW - 110.f, (1.f - FMath::Min(1.f, T / 25.f)) * 120.f - T * 0.3f, A * 0.9f, 1.f, 0.f, 1.f, BossPortraitHair(S));
		}
	}
}

void ADanmakuHUD::DrawBossUI(const ADanmakuWorld& S)
{
	const FBossState& B = S.Boss;
	if (!B.bActive || B.Phase < 0 || B.DefeatTimer > 0) return;

	FieldText(S.GetBossName(), 6.f, 0.f, FLinearColor(0.92f, 0.8f, 1.f), 0.85f, false, 44.f);
	// remaining cards as stars, squeezed so they never run into the health bar
	const int32 Left = S.GetBossPhasesRemaining();
	const float StarGap = Left > 1 ? FMath::Min(10.f, 54.f / (Left - 1)) : 10.f;
	for (int32 i = 0; i < Left; ++i)
	{
		Sprite(BulletStar ? BulletStar.Get() : DiscTex.Get(), FVector2f(54.f + i * StarGap, 9.f), FVector2D(10.f), GoldText, SE_BLEND_Additive);
	}

	const float BarX = 112.f, BarW = FieldW - 152.f;
	const bool bSurvival = S.IsSurvivalPhase();
	const float HPFrac = bSurvival ? 1.f : FMath::Clamp(B.HP / B.MaxHP, 0.f, 1.f);   // survival: HP is locked
	FieldRect(BarX - 1.f, 4.f, BarW + 2.f, 7.f, WithAlpha(Gold, 0.9f));
	FieldRect(BarX, 5.f, BarW, 5.f, FLinearColor(0.05f, 0.02f, 0.08f, 0.9f));
	FieldRect(BarX, 5.f, BarW * HPFrac, 5.f, bSurvival ? FLinearColor(1.f, 0.82f, 0.35f, 0.55f + 0.25f * FMath::Sin(S.Frame * 0.12f)) : (B.bSpell ? FLinearColor(1.f, 0.45f, 0.7f) : FLinearColor(1.f, 0.9f, 0.85f)));
	if (bSurvival) FieldText(Tr(TEXT("無敵")), BarX + BarW * 0.5f, -1.f, GoldText, 0.6f, true);
	else if (S.IsShielded()) FieldText(Tr(TEXT("護盾")), BarX + BarW * 0.5f, -1.f, FLinearColor(0.6f, 0.85f, 1.f), 0.6f, true);
	FieldRect(BarX, 5.f, BarW * HPFrac, 1.5f, FLinearColor(1.f, 1.f, 1.f, 0.5f));

	const int32 Secs = FMath::Max(0, B.TimeLeft / 60);
	FieldText(FString::Printf(TEXT("%02d"), Secs), FieldW - 20.f, -1.f, Secs < 10 ? FLinearColor(1.f, 0.4f, 0.4f) : FLinearColor::White, 0.95f, true);

	if (B.bSpell && !B.SpellName.IsEmpty())
	{
		const FVector2D Pn = ToScreen(FVector2f(FieldW * 0.24f, 15.f));
		InkPanel(Pn.X, Pn.Y, FieldW * 0.74f * Scale, 34.f * Scale, 0.7f);
		FieldText(B.SpellName, FieldW * 0.61f, 16.f, FLinearColor(1.f, 0.95f, 1.f), 0.92f, true, FieldW * 0.7f);
		FString BonusText = B.bBonusValid ? FString(Tr(TEXT("獎勵"))) + FString::Printf(TEXT("　%lld"), S.GetCurrentSpellBonus()) : FString(Tr(TEXT("收取失敗")));
		if (S.IsBombImmunePhase() && !bSurvival) BonusText = FString(Tr(TEXT("最終符卡：Bomb 無效"))) + TEXT("　") + BonusText;
		FieldText(bSurvival ? FString(Tr(TEXT("耐久符卡　撐到時間結束！"))) + TEXT("　") + BonusText : BonusText, FieldW * 0.61f, 33.f, B.bBonusValid ? GoldText : FLinearColor(0.7f, 0.5f, 0.5f), 0.62f, true, FieldW * 0.7f);
	}
}

void ADanmakuHUD::DrawFrame(float SW, float SH)
{
	const float FW = FieldW * Scale, FH = FieldH * Scale;
	const float Rects[4][4] = {
		{ 0.f, 0.f, Origin.X, SH },
		{ Origin.X + FW, 0.f, SW - Origin.X - FW, SH },
		{ Origin.X, 0.f, FW, Origin.Y },
		{ Origin.X, Origin.Y + FH, FW, SH - Origin.Y - FH },
	};
	for (const auto& R : Rects)
	{
		if (UiPattern)
		{
			TiledRect(UiPattern, R[0], R[1], R[2], R[3], 300.f * TextScale, FLinearColor(0.8f, 0.8f, 0.85f, 1.f));
			ScreenRect(R[0], R[1], R[2], R[3], FLinearColor(0.02f, 0.01f, 0.06f, 0.35f));
		}
		else
		{
			ScreenRect(R[0], R[1], R[2], R[3], PanelColor);
		}
	}
	const FLinearColor Inner(0.45f, 0.3f, 0.75f, 1.f);
	const float T = FMath::Max(1.f, 3.f * TextScale);
	const float G = FMath::Max(1.f, 2.f * TextScale);
	auto Border = [&](float Pad, float Th, const FLinearColor& C)
	{
		ScreenRect(Origin.X - Pad - Th, Origin.Y - Pad - Th, FW + 2 * (Pad + Th), Th, C);
		ScreenRect(Origin.X - Pad - Th, Origin.Y + FH + Pad, FW + 2 * (Pad + Th), Th, C);
		ScreenRect(Origin.X - Pad - Th, Origin.Y - Pad, Th, FH + 2 * Pad, C);
		ScreenRect(Origin.X + FW + Pad, Origin.Y - Pad, Th, FH + 2 * Pad, C);
	};
	Border(0.f, G, Inner);
	Border(G + 1.f, T, Gold);
}

void ADanmakuHUD::DrawSidebar(const ADanmakuWorld& S, float SW, float SH)
{
	const float X0 = Origin.X + FieldW * Scale + 36.f * TextScale;
	float Y = Origin.Y + 24.f * TextScale;
	const float Line = 34.f * TextScale;
	const float ValX = X0 + (IsEnglish() ? 118.f : 100.f) * TextScale;

	InkPanel(X0 - 18.f * TextScale, Y - 14.f * TextScale, SW - X0 - 10.f * TextScale, Line * 12.3f, 0.72f);

	auto Row = [&](const TCHAR* Label, const FString& Value)
	{
		ScreenText(Label, X0, Y, LabelColor, 1.0f, false, ValX - X0 - 8.f * TextScale);
		ScreenText(Value, ValX, Y, FLinearColor::White, 1.0f);
		Y += Line;
	};
	auto IconRow = [&](const TCHAR* Label, int32 Count, UTexture2D* Icon)
	{
		ScreenText(Label, X0, Y, LabelColor, 1.0f, false, ValX - X0 - 8.f * TextScale);
		const float IH = 26.f * TextScale;
		float IX = ValX;
		for (int32 i = 0; i < FMath::Min(Count, 8); ++i)
		{
			if (!Icon) break;
			const float IW = IH * float(Icon->GetSizeX()) / float(FMath::Max(1, Icon->GetSizeY()));
			FCanvasTileItem Item(FVector2D(IX, Y + 2.f * TextScale), Icon->GetResource(), FVector2D(IW, IH), FLinearColor::White);
			Item.BlendMode = SE_BLEND_Translucent;
			Canvas->DrawItem(Item);
			IX += IW + 4.f * TextScale;
		}
		Y += Line;
	};

	Row(TEXT("最高分"), FString::Printf(TEXT("%010lld"), S.HiScore));
	Row(TEXT("分數"), FString::Printf(TEXT("%010lld"), S.Score));
	Y += Line * 0.4f;
	IconRow(TEXT("殘機"), S.Player.Lives, IconLife);
	ScreenText(FString(Tr(TEXT("碎片"))) + FString::Printf(TEXT(" %d/%d"), S.Player.LifePieces, ADanmakuWorld::LifePiecesPerLife), ValX + (FMath::Max(FMath::Min(S.Player.Lives, 8), FMath::Min(S.Player.Bombs, 8)) * 30.f + 14.f) * TextScale, Y - Line + 6.f * TextScale, FLinearColor(1.f, 0.7f, 0.8f), 0.7f);
	IconRow(TEXT("靈符"), S.Player.Bombs, IconBomb);
	ScreenText(FString(Tr(TEXT("碎片"))) + FString::Printf(TEXT(" %d/%d"), S.Player.BombPieces, S.BombPiecesNeeded()), ValX + (FMath::Max(FMath::Min(S.Player.Lives, 8), FMath::Min(S.Player.Bombs, 8)) * 30.f + 14.f) * TextScale, Y - Line + 6.f * TextScale, FLinearColor(0.7f, 1.f, 0.75f), 0.7f);
	Y += Line * 0.4f;
	Row(TEXT("靈力"), FString::Printf(TEXT("%.2f / 4.00"), S.Player.Power));
	Row(TEXT("擦彈"), FString::Printf(TEXT("%d"), S.Graze));
	Row(TEXT("符卡"), FString::Printf(TEXT("%d / %d"), S.SpellsCaptured, S.SpellsTotal));

	// current character
	const FCharacterInfo& Info = GetCharacterInfo(S.Player.Character);
	Y += Line * 0.6f;
	ScreenText(FString(Tr(TEXT("自機"))) + TEXT("　") + Tr(Info.Name) + TEXT("（") + Tr(Info.Title) + TEXT("）"), X0, Y, WithAlpha(Info.Color, 1.f), 0.9f);
	Y += Line * 0.8f;
	const FDifficultyInfo& DI = GetDifficultyInfo(S.Difficulty);
	ScreenText(FString(Tr(TEXT("難度"))) + TEXT("　") + DI.Name + (IsEnglish() ? FString() : FString(TEXT("（")) + DI.Label + TEXT("）")), X0, Y, WithAlpha(DI.Color, 1.f), 0.9f);
	Y += Line * 0.8f;
	ScreenText(FString(Tr(TEXT("關卡"))) + TEXT("　") + S.GetStageTitle() + (S.bPractice ? Tr(TEXT("（練習）")) : TEXT("")), X0, Y, LabelColor, 0.9f);

	const float LogoY = SH - 190.f * TextScale;
	if (TitleArt)
	{
		const float H = 120.f * TextScale;
		const float Aspect = float(TitleArt->GetSizeX()) / float(FMath::Max(1, TitleArt->GetSizeY()));
		FCanvasTileItem Item(FVector2D(X0, LogoY), TitleArt->GetResource(), FVector2D(H * Aspect, H), FLinearColor::White);
		Item.BlendMode = SE_BLEND_Translucent;
		Canvas->DrawItem(Item);
	}
	ScreenText(FString::Printf(TEXT("%.0f fps　"), SmoothedFPS) + Tr(TEXT("彈數")) + FString::Printf(TEXT(" %d"), S.Bullets.Num()), X0, SH - 40.f * TextScale, FLinearColor(0.55f, 0.55f, 0.65f), 0.65f);
}

void ADanmakuHUD::DrawOverlays(const ADanmakuWorld& S)
{
	const float CX = FieldW * 0.5f;

	if (S.MessageTimer > 0 && S.State == EDanmakuState::Playing)
	{
		const int32 Elapsed = S.MessageDuration - S.MessageTimer;
		const float A = FMath::Min(1.f, FMath::Min(S.MessageTimer, Elapsed) / 20.f);
		FieldText(S.Message, CX, 140.f, FLinearColor(1.f, 0.92f, 0.75f, A), 1.7f, true, FieldW - 16.f);
		if (!S.SubMessage.IsEmpty())
		{
			float SY = 178.f;
			for (const FString& L : WrapPx(S.SubMessage, (FieldW - 24.f) * Scale, 1.f))
			{
				FieldText(L, CX, SY, FLinearColor(0.85f, 0.8f, 1.f, A), 1.0f, true);
				SY += 22.f;
			}
		}
	}

	switch (S.State)
	{
	case EDanmakuState::StageClear:
	{
		const float A = FMath::Min(1.f, S.StateTimer / 30.f);
		FieldRect(0.f, 0.f, FieldW, FieldH, FLinearColor(0.f, 0.02f, 0.06f, 0.5f * A));
		FieldText(S.GetStageTitle() + TEXT("　") + Tr(TEXT("通過")), CX, 140.f, FLinearColor(0.75f, 1.f, 0.85f, A), 1.9f, true);
		FieldText(FString(Tr(TEXT("關卡獎勵"))) + FString::Printf(TEXT("　%lld"), S.StageBonus), CX, 200.f, WithAlpha(GoldText, A), 1.1f, true);
		FieldText(FString(Tr(TEXT("分數"))) + FString::Printf(TEXT("　%lld"), S.Score), CX, 230.f, FLinearColor(1.f, 1.f, 1.f, A), 1.0f, true);
		if (S.StateTimer > 40 && (S.Frame / 20) % 2 == 0) FieldText(TEXT("Z　前往下一關"), CX, 280.f, LabelColor, 0.95f, true);
		break;
	}
	case EDanmakuState::GameOver:
		FieldRect(0.f, 0.f, FieldW, FieldH, FLinearColor(0.05f, 0.f, 0.02f, 0.6f));
		FieldText(TEXT("遊戲結束"), CX, 150.f, FLinearColor(1.f, 0.45f, 0.5f), 2.0f, true);
		FieldText(FString(Tr(TEXT("分數"))) + FString::Printf(TEXT("　%lld"), S.Score), CX, 210.f, FLinearColor::White, 1.1f, true);
		if (S.StateTimer > 60)
		{
			FieldText(TEXT("R　以同一自機重來"), CX, 255.f, LabelColor, 0.95f, true);
			FieldText(S.bSpellPractice ? TEXT("Z　返回符卡選擇") : TEXT("Z　返回選角"), CX, 280.f, LabelColor, 0.95f, true);
		}
		break;
	case EDanmakuState::Clear:
		FieldRect(0.f, 0.f, FieldW, FieldH, FLinearColor(0.f, 0.02f, 0.06f, 0.55f));
		FieldText(S.bSpellPractice ? (S.SpellsCaptured > 0 ? TEXT("符卡收取！！") : TEXT("收取失敗……")) : TEXT("通關！"), CX, 140.f, FLinearColor(0.75f, 1.f, 0.85f), 2.0f, true);
		FieldText(FString(Tr(TEXT("分數"))) + FString::Printf(TEXT("　%lld"), S.Score), CX, 200.f, FLinearColor::White, 1.1f, true);
		FieldText(FString(Tr(TEXT("符卡收取"))) + FString::Printf(TEXT("　%d / %d"), S.SpellsCaptured, S.SpellsTotal), CX, 228.f, GoldText, 1.0f, true);
		if (S.StateTimer > 60)
		{
			FieldText(TEXT("R　再玩一次"), CX, 270.f, LabelColor, 0.95f, true);
			FieldText(S.bSpellPractice ? TEXT("Z　返回符卡選擇") : TEXT("Z　返回選角"), CX, 295.f, LabelColor, 0.95f, true);
		}
		break;
	default:
		break;
	}

	if (S.PopupTimer > 0 && S.State == EDanmakuState::Playing)
	{
		const float A = FMath::Min(1.f, S.PopupTimer / 20.f);
		const float Rise = (110 - S.PopupTimer) * 0.15f;
		FieldText(S.PopupText, CX, FieldH - 150.f - Rise, WithAlpha(S.PopupColor, A), 1.05f, true, FieldW - 16.f);
	}

	if (S.bPaused)
	{
		FieldRect(0.f, 0.f, FieldW, FieldH, FLinearColor(0.f, 0.f, 0.f, 0.55f));
		FieldText(TEXT("暫停"), CX, 170.f, FLinearColor::White, 2.0f, true);
		FieldText(TEXT("P　繼續　　R　重來"), CX, 225.f, LabelColor, 1.0f, true);
	}
}
