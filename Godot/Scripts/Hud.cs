// Port of DanmakuHUD.h + DanmakuHUD.cpp.
// Draws the whole game with HudCanvas (UE: UCanvas): playfield, sprites, bullets, sidebar, menus and overlays.
// All art is loaded at runtime from res://Art/*.png, the UI font from res://Fonts/.
using System.Globalization;
using System.Text;
using Godot;

namespace DanmakuSTG;

/** A rectangle of an illustration (in 0..1 UV) that is hair: it sways from its top (root) to its bottom (tip). */
public struct FHairZone
{
	public float U0, U1, V0, V1;
	public float Strength;
	public FHairZone(float U0, float U1, float V0, float V1, float Strength = 1f)
	{
		this.U0 = U0; this.U1 = U1; this.V0 = V0; this.V1 = V1; this.Strength = Strength;
	}
}

public sealed class DanmakuHud
{
	readonly Main Owner;
	readonly DanmakuWorld Sim;
	readonly HudCanvas Canvas;

	public DanmakuHud(Main Owner, DanmakuWorld World)
	{
		this.Owner = Owner;
		Sim = World;
		Canvas = new HudCanvas(Owner);
	}

	// =====================================================================================
	// File-local helpers (UE anonymous namespace)
	// =====================================================================================

	static float Hash(int N)
	{
		float S = FMath.Sin(N * 12.9898f) * 43758.5453f;
		return S - FMath.FloorToFloat(S);
	}

	static FLinearColor WithAlpha(FLinearColor C, float A) { C.A = A; return C; }
	static float Smooth(float A, float B, float X) { float T = FMath.Clamp((X - A) / (B - A), 0f, 1f); return T * T * (3f - 2f * T); }
	static Vector2 V2D(float S) => new Vector2(S, S);

	static readonly FLinearColor PanelColor = new(0.035f, 0.025f, 0.07f, 1f);
	static readonly FLinearColor LabelColor = new(0.86f, 0.8f, 1f, 1f);
	static readonly FLinearColor Gold = new(0.92f, 0.76f, 0.4f, 1f);
	static readonly FLinearColor GoldText = new(1f, 0.88f, 0.6f, 1f);

	// Hair regions of the illustrations (UV of the processed PNGs)
	static readonly FHairZone[] SiriusBodyHair = { new(0.06f, 0.36f, 0.12f, 0.62f, 1f), new(0.60f, 0.90f, 0.12f, 0.62f, 1f) };
	static readonly FHairZone[] SiriusPortraitHair = { new(0.08f, 0.36f, 0.14f, 1.0f, 0.8f), new(0.62f, 0.92f, 0.14f, 1.0f, 0.8f) };
	static readonly FHairZone[] SouBackHair = { new(0.36f, 0.82f, 0.05f, 0.47f, 1.25f) };
	// 千秋 (stage 2): long hair falling over both shoulders
	static readonly FHairZone[] ChiakiBodyHair = { new(0.18f, 0.40f, 0.10f, 0.40f, 0.9f), new(0.56f, 0.78f, 0.12f, 0.42f, 0.9f) };
	static readonly FHairZone[] ChiakiPortraitHair = { new(0.18f, 0.40f, 0.14f, 0.54f, 0.7f), new(0.56f, 0.78f, 0.16f, 0.57f, 0.7f) };
	// later bosses all have long hair down both sides
	static readonly FHairZone[] LongBodyHair = { new(0.20f, 0.42f, 0.10f, 0.42f, 0.75f), new(0.56f, 0.80f, 0.10f, 0.45f, 0.75f) };
	static readonly FHairZone[] LongPortraitHair = { new(0.20f, 0.42f, 0.14f, 0.56f, 0.6f), new(0.56f, 0.80f, 0.14f, 0.60f, 0.6f) };
	static readonly FHairZone[] SouPortraitHair = { new(0.06f, 0.38f, 0.12f, 0.78f, 0.9f), new(0.46f, 0.76f, 0.10f, 0.58f, 0.7f) };
	static readonly FHairZone[] NoHair = { };

	// procedural fallbacks
	Texture2D SoftTex;
	Texture2D DiscTex;
	Texture2D RingTex;
	Texture2D FxSeal, FxBolt, FxMandala, FxSigil, FxFlash;   // bomb effects
	Texture2D FxBamboo;
	Texture2D ArrowTex;
	Texture2D ShotTalismanGreen;                             // 晴's charm: the same paper with green writing                                      // 蒼's arrow (Canva, Tools/canva/raw/arrow_canva.png)                                      // 墨竹's bamboo shoots (Canva, Tools/canva/raw/bamboo_canva.png)
	Texture2D SquareTex;
	Texture2D HoleTex;      // opaque outside a soft circle (fog of war)

	// characters
	List<Texture2D> PlayerBacks = new();   // indexed by ECharacter
	List<Texture2D> Portraits = new();     // indexed by ECharacter
	Texture2D PortraitBoss;
	Texture2D BossBody;
	List<Texture2D> BossPortraits = new();   // per stage (index 0 = PortraitBoss)
	List<Texture2D> BossBodies = new();      // per stage
	List<Texture2D> StageBgs = new();        // per stage
	List<Texture2D> PlayerSheets = new();    // indexed by ECharacter
	List<Texture2D> BossSheets = new();      // per stage art index
	List<Texture2D> PlayerAnims = new();     // hand-made idle / left / right sheets (anim_<name>.png), indexed by ECharacter
	List<Texture2D> BossAnims = new();       // the same for boss art 0..10 (stages 1-6, the Extra boss, the four Extra-IF beasts)
	Texture2D FairyBlueArt;
	Texture2D FairyRedArt;
	Texture2D SpiritArt;

	// effects, items, bullets
	Texture2D BulletOrb;
	Texture2D BulletPetal;
	Texture2D BulletStar;
	Texture2D BulletCrescent;
	Texture2D BulletSakura;
	Texture2D BulletMaple;
	Texture2D BulletSnow;
	Texture2D BulletWater;
	Texture2D BulletFire;
	Texture2D BulletChrys;
	Texture2D BulletTile;
	Texture2D MagicCircle;
	Texture2D ShotTalisman;
	Texture2D ItemPower;
	Texture2D ItemPoint;
	Texture2D ItemOneUp;   // the 1UP item (Art/item_1up.png)
	Texture2D IconLife;
	Texture2D IconBomb;

	// backgrounds / UI
	Texture2D TitleArt;      // logo
	Texture2D TitleKeyArt;   // full-screen illustration
	Texture2D StageBg;
	Texture2D SpellBg;
	Texture2D UiPattern;

	FontFile UiFont;

	bool bTexturesLoaded = false;
	Vector2 Origin = Vector2.Zero;
	float Scale = 1f;
	float TextScale = 1f;
	float SmoothedFPS = 60f;
	float SpellBlend = 0f;
	float BossLean = 0f;

	// =====================================================================================
	// Setup
	// =====================================================================================

	/** bWrap is kept for parity with UE; repeating is chosen per draw call in HudCanvas. */
	static Texture2D LoadArt(string FileName, bool bWrap = false)
	{
		string Path = "res://Art/" + FileName;
		if (!ResourceLoader.Exists(Path)) return null;
		return GD.Load<Texture2D>(Path);
	}

	void LoadTextures()
	{
		bTexturesLoaded = true;

		SoftTex = HudCanvas.MakeTexture(64, (U, V) => FMath.Square(FMath.Max(0f, 1f - FMath.Sqrt(U * U + V * V))));
		DiscTex = HudCanvas.MakeTexture(64, (U, V) => (1f - FMath.Sqrt(U * U + V * V)) / 0.1f);
		RingTex = HudCanvas.MakeTexture(128, (U, V) =>
		{
			float R = FMath.Sqrt(U * U + V * V);
			float Dash = FMath.Cos(FMath.Atan2(V, U) * 12f) > -0.2f ? 1f : 0.15f;
			return FMath.Max((1f - FMath.Abs(R - 0.9f) / 0.06f) * Dash, (1f - FMath.Abs(R - 0.74f) / 0.025f) * 0.7f);
		});
		SquareTex = HudCanvas.MakeTexture(16, (U, V) => (1f - FMath.Max(FMath.Abs(U), FMath.Abs(V))) / 0.15f);
		HoleTex = HudCanvas.MakeTexture(128, (U, V) => Smooth(0.55f, 1f, FMath.Sqrt(U * U + V * V)));

		PlayerBacks = new() { LoadArt("player_back.png"), LoadArt("player_back_sou.png"), LoadArt("player_back_haru.png"), LoadArt("player_back_yuki.png") };
		Portraits   = new() { LoadArt("portrait_ren.png"), LoadArt("portrait_sou.png"), LoadArt("portrait_haru.png"), LoadArt("portrait_yuki.png") };
		PortraitBoss   = LoadArt("portrait_sirius.png");
		BossBody       = LoadArt("boss_body.png");
		FairyBlueArt   = LoadArt("fairy_blue.png");
		FairyRedArt    = LoadArt("fairy_red.png");
		SpiritArt      = LoadArt("spirit.png");

		BulletOrb      = LoadArt("bullet_orb.png");
		BulletPetal    = LoadArt("bullet_petal.png");
		BulletStar     = LoadArt("bullet_star.png");
		BulletCrescent = LoadArt("bullet_crescent.png");
		BulletSakura   = LoadArt("bullet_sakura.png");
		BulletMaple    = LoadArt("bullet_maple.png");
		BulletSnow     = LoadArt("bullet_snow.png");
		BulletWater    = LoadArt("bullet_water.png");
		BulletFire     = LoadArt("bullet_fire.png");
		BulletChrys    = LoadArt("bullet_chrys.png");
		BulletTile     = LoadArt("bullet_tile.png");
		MagicCircle    = LoadArt("magic_circle.png");
		ShotTalisman   = LoadArt("shot_talisman.png");
		FxSeal = LoadArt("fx_seal.png"); FxBolt = LoadArt("fx_bolt.png"); FxMandala = LoadArt("fx_mandala.png");
		FxSigil = LoadArt("fx_sigil.png"); FxFlash = LoadArt("fx_flash.png"); FxBamboo = LoadArt("fx_bamboo.png"); ArrowTex = LoadArt("shot_arrow.png");
		ShotTalismanGreen = LoadArt("shot_talisman_green.png");   // Tools/fx/make_bomb_fx.py
		ItemPower      = LoadArt("item_power.png");
		ItemPoint      = LoadArt("item_point.png");
		ItemOneUp      = LoadArt("item_1up.png");
		IconLife       = LoadArt("icon_life.png");
		IconBomb       = LoadArt("icon_bomb.png");

		TitleArt       = LoadArt("title.png");
		TitleKeyArt    = LoadArt("title_art.png");
		StageBg        = LoadArt("bg_stage.png", true);
		SpellBg        = LoadArt("bg_spell.png", true);
		UiPattern      = LoadArt("ui_pattern.png", true);
		// per-stage art: stage 1 uses the originals, later stages <name><N>.png (missing files fall back to stage 1)
		BossPortraits.Clear(); BossBodies.Clear(); StageBgs.Clear();
		BossPortraits.Add(PortraitBoss); BossBodies.Add(BossBody); StageBgs.Add(StageBg);
		for (int i = 2; i <= DanmakuWorld.ExtraStage + 1; ++i)
		{
			BossPortraits.Add(LoadArt($"portrait_boss{i}.png"));
			BossBodies.Add(LoadArt($"boss{i}_body.png"));
			StageBgs.Add(LoadArt($"bg_stage{i}.png", true));
		}
		PlayerSheets = new() { LoadArt("player_back_sheet.png"), LoadArt("player_back_sou_sheet.png"), LoadArt("player_back_haru_sheet.png"), LoadArt("player_back_yuki_sheet.png") };
		PlayerAnims = new() { LoadArt("anim_ren.png"), LoadArt("anim_sou.png"), LoadArt("anim_haru.png"), LoadArt("anim_yuki.png") };
		BossAnims.Clear();
		for (int i = 1; i <= 7; ++i) BossAnims.Add(LoadArt($"anim_boss{i}.png"));
		for (int i = 1; i <= 4; ++i) BossAnims.Add(LoadArt($"anim_beast{i}.png"));   // art indices 7..10
		BossSheets.Clear();
		BossSheets.Add(LoadArt("boss_body_sheet.png"));
		for (int i = 2; i <= DanmakuWorld.ExtraStage + 1; ++i) BossSheets.Add(LoadArt($"boss{i}_body_sheet.png"));
		// Extra-IF: the four beasts take art indices 7..10, the stage background is index 7
		StageBgs.Add(LoadArt("bg_stage8.png", true));
		for (int i = 1; i <= 4; ++i)
		{
			BossPortraits.Add(LoadArt($"portrait_beast{i}.png"));
			BossBodies.Add(LoadArt($"beast{i}_body.png"));
			BossSheets.Add(LoadArt($"beast{i}_body_sheet.png"));
		}
		if (MagicCircle != null) RingTex = MagicCircle;

		// Runtime font straight from the TTF (Noto Serif TC, OFL) so Chinese text renders
		const string FontPath = "res://Fonts/NotoSerifTC-SemiBold.ttf";
		if (ResourceLoader.Exists(FontPath)) UiFont = GD.Load<FontFile>(FontPath);
		// Simplified Chinese glyphs the TC font lacks (Noto Serif SC subset, OFL; Tools/loc/i18n/make_sc_font.py)
		const string ScPath = "res://Fonts/NotoSerifSC-SemiBold-subset.ttf";
		if (UiFont != null && ResourceLoader.Exists(ScPath))
			UiFont.Fallbacks = new Godot.Collections.Array<Font> { GD.Load<FontFile>(ScPath) };
	}

	static Texture2D At(List<Texture2D> L, int I) => I >= 0 && I < L.Count ? L[I] : null;

	Texture2D PlayerBackFor(int Char)
	{
		return At(PlayerBacks, Char) ?? (PlayerBacks.Count > 0 ? PlayerBacks[0] : null);
	}

	Texture2D PortraitFor(int Char)
	{
		return At(Portraits, Char) ?? (Portraits.Count > 0 ? Portraits[0] : null);
	}

	Texture2D BulletTex(byte Shape)
	{
		switch ((EBulletShape)Shape)
		{
		case EBulletShape.Rice:
		case EBulletShape.Comet:    return BulletPetal;
		case EBulletShape.Star:     return BulletStar;
		case EBulletShape.Crescent: return BulletCrescent;
		case EBulletShape.Sakura:   return BulletSakura ?? BulletStar;
		case EBulletShape.Maple:    return BulletMaple ?? BulletStar;
		case EBulletShape.Snow:     return BulletSnow ?? BulletStar;
		case EBulletShape.Water:    return BulletWater ?? BulletPetal;
		case EBulletShape.Fire:     return BulletFire ?? BulletPetal;
		case EBulletShape.Chrys:    return BulletChrys ?? BulletStar;
		case EBulletShape.Tile:     return BulletTile ?? BulletOrb;
		default:                    return BulletOrb;
		}
	}

	Texture2D BossPortraitFor(DanmakuWorld S)
	{
		int I = S.GetBossArtIndex();
		return At(BossPortraits, I) ?? PortraitBoss;
	}

	Texture2D BossBodyFor(DanmakuWorld S)
	{
		int I = S.GetBossArtIndex();
		return At(BossBodies, I) ?? BossBody;
	}

	Texture2D StageBgFor(DanmakuWorld S)
	{
		return At(StageBgs, S.StageIndex) ?? StageBg;
	}

	FHairZone[] BossBodyHair(DanmakuWorld S)
	{
		switch (S.GetBossArtIndex())
		{
		case 0: return SiriusBodyHair;
		case 1: return ChiakiBodyHair;
		default: return LongBodyHair;
		}
	}

	FHairZone[] BossPortraitHair(DanmakuWorld S)
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

	Vector2 ToScreen(FVector2f P)
	{
		return Origin + new Vector2(P.X, P.Y) * Scale;
	}

	void Sprite(Texture2D Tex, FVector2f Center, Vector2 SizeUnits, FLinearColor Color, EBlend Blend = EBlend.Translucent, float RotDeg = 0f)
	{
		if (Tex == null) return;
		// (UE premultiplied the colour by its alpha for additive; Godot's add blend already multiplies by alpha)
		Vector2 Size = SizeUnits * Scale;
		if (RotDeg != 0f) Canvas.TexRectRotated(Tex, ToScreen(Center), Size, Color, Blend, RotDeg);
		else Canvas.TexRect(Tex, ToScreen(Center) - Size * 0.5f, Size, Color, Blend);
	}

	void ArtSprite(Texture2D Tex, FVector2f Center, float HeightUnits, FLinearColor Color, float RotDeg = 0f, float XScale = 1f)
	{
		if (Tex == null) return;
		float Aspect = (float)Tex.GetWidth() / (float)FMath.Max(1, Tex.GetHeight());
		Sprite(Tex, Center, new Vector2(HeightUnits * Aspect * XScale, HeightUnits), Color, EBlend.Translucent, RotDeg);
	}

	/** Frame from a pre-rendered sheet (Tools/anim/make_sheets.py): rows = lean -2..2, columns = hair phases. Body never deforms. */
	void SheetSprite(Texture2D Sheet, FVector2f Center, float HeightUnits, FLinearColor Color, float Lean, int Phase)
	{
		if (Sheet == null) return;
		const int Cols = 12, Rows = 5;
		const float PadY = 0.05f;   // must match Tools/anim/sprite_specs.py
		float CellW = (float)Sheet.GetWidth() / Cols, CellH = (float)Sheet.GetHeight() / Rows;
		int Row = FMath.Clamp(FMath.RoundToInt(Lean) + Rows / 2, 0, Rows - 1);
		int Col = ((Phase % Cols) + Cols) % Cols;
		float H = HeightUnits * (1f + 2f * PadY);
		float W = H * CellW / CellH;
		Vector2 TL = ToScreen(Center - new FVector2f(W * 0.5f, H * 0.5f));
		Canvas.TexRect(Sheet, TL, new Vector2(W, H) * Scale, Color, EBlend.Translucent,
			new Vector2(Col / (float)Cols, Row / (float)Rows), new Vector2((Col + 1) / (float)Cols, (Row + 1) / (float)Rows));
	}

	/** Frame of a hand-made sheet (Tools/anim/pack_user_sheets.py): rows idle / left / right, 12 columns.
	 *  Every cell is centred on the first idle frame, which is AnimRefH px tall in the sheet. */
	void AnimSprite(Texture2D Sheet, FVector2f Center, float HeightUnits, FLinearColor Color, int Row, int Col)
	{
		if (Sheet == null) return;
		const int Cols = 12, Rows = 3;
		const float AnimRefH = 256f;   // must match REF_H in pack_user_sheets.py
		float CellW = (float)Sheet.GetWidth() / Cols, CellH = (float)Sheet.GetHeight() / Rows;
		Col = ((Col % Cols) + Cols) % Cols;
		float H = HeightUnits * CellH / AnimRefH;
		float W = H * CellW / CellH;
		Vector2 TL = ToScreen(Center - new FVector2f(W * 0.5f, H * 0.5f));
		Canvas.TexRect(Sheet, TL, new Vector2(W, H) * Scale, Color, EBlend.Translucent,
			new Vector2(Col / (float)Cols, Row / (float)Rows), new Vector2((Col + 1) / (float)Cols, (Row + 1) / (float)Rows));
	}

	/** Illustration as a deformable grid: body stays put, outer cloth flutters, hair zones sway from the root. */
	void WarpSprite(Texture2D Tex, FVector2f Center, float HeightUnits, FLinearColor Color,
		float Trail, float Sway, float Phase, float CoreHalf = 0.16f, float FlutterTop = 0.3f, FHairZone[] Hair = null)
	{
		if (Tex == null) return;
		Hair ??= NoHair;
		const int GX = 14, GY = 18;
		float Aspect = (float)Tex.GetWidth() / (float)FMath.Max(1, Tex.GetHeight());
		float H = HeightUnits;
		float W = HeightUnits * Aspect;
		var TopLeft = new FVector2f(Center.X - W * 0.5f, Center.Y - H * 0.5f);

		// grid vertices (shared by the triangles; same geometry as UE's per-triangle copies)
		var Points = new Vector2[(GX + 1) * (GY + 1)];
		var UVs = new Vector2[(GX + 1) * (GY + 1)];
		for (int iy = 0; iy <= GY; ++iy)
		{
			float V = (float)iy / GY;
			for (int ix = 0; ix <= GX; ++ix)
			{
				float U = (float)ix / GX;
				// cloth: outer columns below FlutterTop, plus the hem
				float Wc = Smooth(CoreHalf, CoreHalf + 0.22f, FMath.Abs(U - 0.5f)) * Smooth(FlutterTop, FlutterTop + 0.3f, V)
					+ 0.35f * Smooth(0.82f, 1f, V);
				float DX = W * Wc * (Sway * 0.035f * FMath.Sin(Phase + V * 5f + U * 3f) - Trail * 0.07f * V);
				float DY = W * Wc * Sway * 0.012f * FMath.Cos(Phase * 1.3f + U * 4f);

				// hair: anchored at the root, swinging more toward the tip, on its own slower rhythm
				float Wh = 0f;
				foreach (var Z in Hair)
				{
					float Wu = Smooth(Z.U0, Z.U0 + 0.07f, U) * (1f - Smooth(Z.U1 - 0.07f, Z.U1, U));
					float Wv = Smooth(Z.V0, Z.V1, V) * (1f - Smooth(Z.V1, Z.V1 + 0.1f, V));
					Wh = FMath.Max(Wh, Z.Strength * Wu * Wv);
				}
				DX += W * Wh * (Sway * 0.05f * FMath.Sin(Phase * 0.8f + V * 3.5f + 1.3f) - Trail * 0.1f * V);
				DY += W * Wh * Sway * 0.012f * FMath.Cos(Phase * 0.9f + V * 2f);

				int Idx = iy * (GX + 1) + ix;
				Points[Idx] = ToScreen(TopLeft + new FVector2f(U * W + DX, V * H + DY));
				UVs[Idx] = new Vector2(U, V);
			}
		}

		var Indices = new int[GX * GY * 6];
		int N = 0;
		for (int iy = 0; iy < GY; ++iy)
		{
			for (int ix = 0; ix < GX; ++ix)
			{
				int A = iy * (GX + 1) + ix, B = A + 1, C = A + (GX + 1), D = C + 1;
				Indices[N++] = A; Indices[N++] = B; Indices[N++] = D;
				Indices[N++] = A; Indices[N++] = D; Indices[N++] = C;
			}
		}
		Canvas.Triangles(Tex, Points, UVs, Indices, Color, EBlend.Translucent);
	}

	void Beam(FVector2f From, FVector2f To, float Width, FLinearColor Color)
	{
		FVector2f D = To - From;
		float Len = D.Size();
		if (Len < 1f) return;
		Sprite(SoftTex, (From + To) * 0.5f, new Vector2(Width, Len), Color, EBlend.Additive, AngleOf(D) + 90f);
	}

	void TiledRect(Texture2D Tex, float X, float Y, float W, float H, float TilePx, FLinearColor Color)
	{
		if (Tex == null || W <= 0f || H <= 0f) return;
		Canvas.TexRect(Tex, new Vector2(X, Y), new Vector2(W, H), Color, EBlend.Opaque,
			new Vector2(X / TilePx, Y / TilePx), new Vector2((X + W) / TilePx, (Y + H) / TilePx), bRepeat: true);
	}

	void ScreenRect(float X, float Y, float W, float H, FLinearColor Color)
	{
		Canvas.Rect(X, Y, W, H, Color, EBlend.Translucent);
	}

	void FieldRect(float X, float Y, float W, float H, FLinearColor Color)
	{
		Vector2 P = ToScreen(new FVector2f(X, Y));
		ScreenRect(P.X, P.Y, W * Scale, H * Scale, Color);
	}

	void InkPanel(float X, float Y, float W, float H, float Alpha = 0.78f)
	{
		float T = FMath.Max(1f, 2f * TextScale);
		ScreenRect(X, Y, W, H, new FLinearColor(0.02f, 0.01f, 0.06f, Alpha));
		ScreenRect(X, Y, W, T, WithAlpha(Gold, 0.9f));
		ScreenRect(X, Y + H - T, W, T, WithAlpha(Gold, 0.9f));
		ScreenRect(X, Y + T * 2f, W, 1f, WithAlpha(Gold, 0.35f));
		ScreenRect(X, Y + H - T * 3f, W, 1f, WithAlpha(Gold, 0.35f));
	}

	/** UE text scale K (LegacyFontSize 20 drawn at scale K) -> Godot font size in pixels (20 pt at 96 dpi). Tune here. */
	static int FontPx(float K) => Mathf.Max(1, Mathf.RoundToInt(20f * 1.333f * K));

	/** Width in pixels of the (translated) text at this scale. */
	float TextWidth(string Text, float InScale)
	{
		float K = InScale * TextScale;
		return HudCanvas.TextWidth(UiFont, TrStr(Text), FontPx(K));
	}

	/** Breaks the (translated) text into lines no wider than MaxPx: on spaces in English, between characters in Chinese. */
	List<string> WrapPx(string InText, float MaxPx, float InScale)
	{
		string Text = TrStr(InText) ?? "";
		var Lines = new List<string>();
		string Cur = "";
		void Push(string Piece, string Sep)
		{
			string Try = Cur.Length == 0 ? Piece : Cur + Sep + Piece;
			if (Cur.Length != 0 && TextWidth(Try, InScale) > MaxPx) { Lines.Add(Cur); Cur = Piece; }
			else Cur = Try;
		}
		if (IsLatin())
		{
			foreach (var W in GlueLatin(Text).Split(' ', StringSplitOptions.RemoveEmptyEntries)) Push(W.Replace(NoBreak, " "), " ");
		}
		else
		{
			const string NoLead = "，。！？」）、…』ー〜ぁぃぅぇぉっゃゅょゎァィゥェォッャュョヮ・";
			for (int i = 0; i < Text.Length; ++i)
			{
				var Piece = new StringBuilder();
				Piece.Append(Text[i]);
				// keep Latin words ("Boss", "Bomb") in one piece
				while (char.IsLetterOrDigit(Text[i]) && Text[i] < 128 && i + 1 < Text.Length && char.IsLetterOrDigit(Text[i + 1]) && Text[i + 1] < 128) Piece.Append(Text[++i]);
				// keep closing punctuation with the character before it
				while (i + 1 < Text.Length && NoLead.IndexOf(Text[i + 1]) >= 0) Piece.Append(Text[++i]);
				Push(Piece.ToString(), "");
			}
		}
		if (Cur.Length != 0) Lines.Add(Cur);
		return Lines;
	}

	/** MaxWidth > 0: the text shrinks until it fits (pixels for ScreenText, field units for FieldText). */
	void ScreenText(string Text, float X, float Y, FLinearColor Color, float InScale, bool bCenter = false, float MaxWidth = 0f)
	{
		string Shown = TrStr(Text);
		if (MaxWidth > 0f)
		{
			float W = TextWidth(Shown, InScale);
			if (W > MaxWidth) InScale *= MaxWidth / W;
		}
		float K = InScale * TextScale;
		Canvas.Text(UiFont, Shown, new Vector2(X, Y), FontPx(K), Color, bCenter);
	}

	void FieldText(string Text, float X, float Y, FLinearColor Color, float InScale, bool bCenter = true, float MaxWidth = 0f)
	{
		Vector2 P = ToScreen(new FVector2f(X, Y));
		ScreenText(Text, P.X, P.Y, Color, InScale, bCenter, MaxWidth * Scale);
	}

	const string NoBreak = "\u00A0";
	/** French puts a space before ! ? : ; » and after «; keep those marks on the word they belong to. */
	static string GlueLatin(string T) => T.Replace(" !", NoBreak + "!").Replace(" ?", NoBreak + "?").Replace(" :", NoBreak + ":")
		.Replace(" ;", NoBreak + ";").Replace(" »", NoBreak + "»").Replace("« ", "«" + NoBreak);

	static List<string> WrapText(string InText, int MaxChars)
	{
		var Lines = new List<string>();
		string Text = TrStr(InText) ?? "";
		if (IsLatin())
		{
			// English: wrap on spaces, about twice as many characters fit on a line
			int Max = FMath.RoundToInt(MaxChars * 1.75f);
			string CurE = "";
			foreach (var W0 in GlueLatin(Text).Split(' ', StringSplitOptions.RemoveEmptyEntries))
			{
				string W = W0.Replace(NoBreak, " ");
				if (CurE.Length != 0 && CurE.Length + 1 + W.Length > Max) { Lines.Add(CurE); CurE = ""; }
				CurE += CurE.Length == 0 ? W : " " + W;
			}
			if (CurE.Length != 0) Lines.Add(CurE);
			return Lines;
		}
		var Cur = new StringBuilder();
		for (int i = 0; i < Text.Length; ++i)
		{
			Cur.Append(Text[i]);
			// don't start a line with closing punctuation
			bool bNextIsPunct = i + 1 < Text.Length && "，。！？」）、…』ー〜ぁぃぅぇぉっゃゅょァィゥェォッャュョ・".IndexOf(Text[i + 1]) >= 0;
			if (Cur.Length >= MaxChars && !bNextIsPunct)
			{
				Lines.Add(Cur.ToString());
				Cur.Clear();
			}
		}
		if (Cur.Length != 0) Lines.Add(Cur.ToString());
		return Lines;
	}

	static string Inv(FormattableString F) => F.ToString(CultureInfo.InvariantCulture);

	// =====================================================================================
	// Main draw
	// =====================================================================================

	public void Draw()
	{
		Canvas.Begin();
		if (!bTexturesLoaded) LoadTextures();

		Vector2 VS = Owner.GetViewportRect().Size;
		float SW = VS.X;
		float SH = VS.Y;
		TextScale = SH / 720f;

		{
			float Dt = (float)Owner.GetProcessDeltaTime();
			if (Dt > 0f) SmoothedFPS = FMath.Lerp(SmoothedFPS, 1f / Dt, 0.05f);
		}

		ScreenRect(0f, 0f, SW, SH, PanelColor);
		if (Sim == null) return;
		DanmakuWorld S = Sim;

		if (S.State == EDanmakuState.Title) { DrawTitle(S, SW, SH); return; }
		if (S.State == EDanmakuState.CharSelect) { DrawSelect(S, SW, SH); return; }
		if (S.State == EDanmakuState.DiffSelect) { DrawDiffSelect(S, SW, SH); return; }
		if (S.State == EDanmakuState.StageSelect) { DrawStageSelect(S, SW, SH); return; }
		if (S.State == EDanmakuState.SpellSelect) { DrawSpellSelect(S, SW, SH); return; }
		if (S.State == EDanmakuState.MusicRoom) { DrawMusicRoom(S, SW, SH); return; }
		if (S.State == EDanmakuState.Options) { DrawOptions(S, SW, SH); return; }
		if (S.State == EDanmakuState.EndingScene) { DrawEndingScene(S, SW, SH); return; }

		Scale = (SH * 0.94f) / FieldH;
		Origin = new Vector2(FMath.Max(SH * 0.03f, (SW - FieldW * Scale) * 0.3f), SH * 0.03f);

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

	void DrawTitle(DanmakuWorld S, float SW, float SH)
	{
		Origin = Vector2.Zero;
		Scale = 1f;
		if (TitleKeyArt != null)
		{
			float AW = TitleKeyArt.GetWidth(), AH = TitleKeyArt.GetHeight();
			float K = FMath.Max(SW / AW, SH / AH) * (1.02f + FMath.Sin(S.Frame * 0.004f) * 0.01f);
			Canvas.TexRect(TitleKeyArt, new Vector2((SW - AW * K) * 0.5f, (SH - AH * K) * 0.5f), new Vector2(AW * K, AH * K), LinColor.White, EBlend.Opaque);
		}
		const int Bands = 32;
		for (int b = 0; b < Bands; ++b)
		{
			float T = (float)b / Bands;
			ScreenRect(SW * 0.6f * T, 0f, SW * 0.6f / Bands + 1f, SH, new FLinearColor(0.01f, 0.0f, 0.05f, 0.5f * FMath.Square(1f - T)));
		}
		if (TitleArt != null)
		{
			float LH = SH * 0.26f;
			float LA = (float)TitleArt.GetWidth() / (float)FMath.Max(1, TitleArt.GetHeight());
			Canvas.TexRect(TitleArt, new Vector2(SW * 0.04f, SH * 0.12f), new Vector2(LH * LA, LH), LinColor.White, EBlend.Translucent);
		}
		float MX = SW * 0.07f;
		string[] Items = { "開始遊戲", "Extra", "Extra-IF", "關卡練習", "符卡練習", "音樂室", "選項", LanguageLabel() };
		bool bExtraOk = DanmakuWorld.IsExtraAvailable();
		for (int i = 0; i < DanmakuWorld.TitleItems; ++i)
		{
			bool bSel = i == S.TitleCursor;
			bool bLocked = (i == 1 && !bExtraOk) || (i == 2 && !DanmakuWorld.IsIfAvailable());
			float Y = SH * 0.37f + i * 33f * TextScale;
			float X = MX + (bSel ? 18f * TextScale : 0f);
			if (bSel && MagicCircle != null) Sprite(MagicCircle, new FVector2f(X - 20f * TextScale, Y + 18f * TextScale), V2D(34f * TextScale), WithAlpha(GoldText, 0.8f), EBlend.Additive, S.Frame * 1.2f);
			FLinearColor Col = bSel ? LinColor.White : LabelColor;
			if (bLocked) Col = new FLinearColor(0.45f, 0.42f, 0.5f);
			ScreenText(Items[i], X, Y, Col, bSel ? 1.3f : 1.12f);
			if (bLocked && bSel) ScreenText("（尚未開放）", X + 120f * TextScale, Y + 8f * TextScale, Col, 0.8f);
		}
		string[] Help = { "方向鍵 / WASD　移動　　Shift　低速", "Z　射擊／決定　　X　Bomb／返回", "P　暫停　　R　重來" };
		for (int i = 0; i < 3; ++i) ScreenText(Help[i], MX, SH * 0.82f + i * 28f * TextScale, LabelColor, 0.9f);
		if (S.bLangPicker) DrawLanguagePicker(S, SW, SH);
	}

	/** The window that opens from the title menu's language entry; every language is written in itself. */
	void DrawLanguagePicker(DanmakuWorld S, float SW, float SH)
	{
		ScreenRect(0f, 0f, SW, SH, new FLinearColor(0f, 0f, 0.03f, 0.55f));
		float W = 460f * TextScale, RowH = 48f * TextScale;
		float H = 96f * TextScale + RowH * LanguageOrder.Length;
		float X = (SW - W) * 0.5f, Y = (SH - H) * 0.5f;
		InkPanel(X, Y, W, H, 0.92f);
		Canvas.Text(UiFont, "Language · 語言 · 言語 · Langue", new Vector2(SW * 0.5f, Y + 18f * TextScale), FontPx(0.9f * TextScale), GoldText, true);
		for (int i = 0; i < LanguageOrder.Length; ++i)
		{
			bool bSel = i == S.LangPickCursor;
			float RY = Y + 62f * TextScale + i * RowH;
			if (bSel)
			{
				ScreenRect(X + 24f * TextScale, RY - 4f * TextScale, W - 48f * TextScale, RowH - 6f * TextScale, new FLinearColor(1f, 0.85f, 0.5f, 0.14f));
				if (MagicCircle != null) Sprite(MagicCircle, new FVector2f(X + 48f * TextScale, RY + 16f * TextScale), V2D(30f * TextScale), WithAlpha(GoldText, 0.8f), EBlend.Additive, S.Frame * 1.2f);
			}
			string Name = LanguageName(LanguageOrder[i]) + (LanguageOrder[i] == GetLanguage() ? "  ✓" : "");
			Canvas.Text(UiFont, Name, new Vector2(SW * 0.5f, RY), FontPx((bSel ? 1.25f : 1.1f) * TextScale), bSel ? LinColor.White : LabelColor, true);
		}
		Canvas.Text(UiFont, "↑ ↓   Z  OK   X  ×", new Vector2(SW * 0.5f, Y + H - 30f * TextScale), FontPx(0.75f * TextScale), LabelColor, true);
	}

	void DrawSelect(DanmakuWorld S, float SW, float SH)
	{
		Origin = Vector2.Zero;
		Scale = 1f;
		if (UiPattern != null) TiledRect(UiPattern, 0f, 0f, SW, SH, 300f * TextScale, new FLinearColor(0.7f, 0.7f, 0.78f, 1f));
		ScreenRect(0f, 0f, SW, SH, new FLinearColor(0.02f, 0.01f, 0.06f, 0.55f));

		ScreenText("選擇自機", SW * 0.5f, SH * 0.045f, GoldText, 1.7f, true);

		float Gap = SW * 0.015f;
		float CardW = (SW * 0.9f - Gap * 3f) / NumCharacters;
		float CardH = SH * 0.76f;
		float X0 = SW * 0.05f;
		float Y0 = SH * 0.13f;

		for (int i = 0; i < NumCharacters; ++i)
		{
			bool bSel = i == S.SelectedCharacter;
			FCharacterInfo Info = GetCharacterInfo((ECharacter)i);
			float Lift = bSel ? -8f * TextScale : 0f;
			float X = X0 + i * (CardW + Gap);
			float Y = Y0 + Lift;

			InkPanel(X, Y, CardW, CardH, bSel ? 0.85f : 0.7f);
			if (bSel)
			{
				float T = FMath.Max(2f, 3f * TextScale);
				FLinearColor Glow = WithAlpha(Info.Color, 0.6f + 0.3f * FMath.Sin(S.Frame * 0.1f));
				ScreenRect(X - T, Y - T, CardW + 2 * T, T, Glow);
				ScreenRect(X - T, Y + CardH, CardW + 2 * T, T, Glow);
				ScreenRect(X - T, Y, T, CardH, Glow);
				ScreenRect(X + CardW, Y, T, CardH, Glow);
				if (MagicCircle != null) Sprite(MagicCircle, new FVector2f(X + CardW * 0.5f, Y + CardH * 0.3f), V2D(CardW * 0.95f), WithAlpha(Info.Color, 0.22f), EBlend.Additive, S.Frame * 0.3f);
			}

			// portrait; English descriptions are longer, so the portrait gives up some room for them
			bool bEn = IsLatin();
			float PortraitFrac = bEn ? 0.36f : 0.44f;
			Texture2D Tex = PortraitFor(i);
			if (Tex != null)
			{
				float Aspect = (float)Tex.GetWidth() / (float)FMath.Max(1, Tex.GetHeight());
				float PH = FMath.Min(CardH * PortraitFrac, CardW * 0.98f / Aspect);
				FLinearColor Tint = bSel ? LinColor.White : new FLinearColor(0.5f, 0.5f, 0.58f, 1f);
				ArtSprite(Tex, new FVector2f(X + CardW * 0.5f, Y + 12f * TextScale + PH * 0.5f), PH, Tint);
			}

			float TY = Y + CardH * (PortraitFrac + 0.02f);
			float TW = CardW * 0.9f;
			float LineH = (bEn ? 19f : 22f) * TextScale;
			ScreenText(Info.Name, X + CardW * 0.5f, TY, bSel ? LinColor.White : LabelColor, bEn ? 1.3f : 1.6f, true, TW);
			TY += (bEn ? 40f : 50f) * TextScale;
			ScreenText(Info.Title, X + CardW * 0.5f, TY, WithAlpha(Info.Color, 1f), bEn ? 0.85f : 1.0f, true, TW);
			TY += (bEn ? 26f : 30f) * TextScale;
			foreach (var L in WrapPx(Info.ShotDesc, TW, bEn ? 0.7f : 0.8f))
			{
				ScreenText(L, X + CardW * 0.5f, TY, LabelColor, bEn ? 0.7f : 0.8f, true);
				TY += LineH;
			}
			TY += 6f * TextScale;
			foreach (var L in WrapPx(Info.BombName, TW, bEn ? 0.75f : 0.85f))
			{
				ScreenText(L, X + CardW * 0.5f, TY, GoldText, bEn ? 0.75f : 0.85f, true);
				TY += LineH;
			}
			TY += 2f * TextScale;
			foreach (var L in WrapPx(Info.BombDesc, TW, bEn ? 0.66f : 0.72f))
			{
				ScreenText(L, X + CardW * 0.5f, TY, new FLinearColor(0.75f, 0.72f, 0.85f), bEn ? 0.66f : 0.72f, true);
				TY += (bEn ? 17f : 20f) * TextScale;
			}
			TY += 4f * TextScale;
			ScreenText(Tr("速度") + Inv($"　{Info.Speed} / {Info.FocusSpeed}"), X + CardW * 0.5f, TY, WithAlpha(Info.Color, 1f), bEn ? 0.66f : 0.72f, true);
			TY += (bEn ? 17f : 20f) * TextScale;
			foreach (var Trait in new[] { Info.Trait1, Info.Trait2 })
			{
				foreach (var L in WrapPx("◆ " + Tr(Trait), TW, bEn ? 0.62f : 0.68f))
				{
					ScreenText(L, X + CardW * 0.5f, TY, new FLinearColor(0.95f, 0.88f, 0.7f), bEn ? 0.62f : 0.68f, true);
					TY += (bEn ? 16f : 19f) * TextScale;
				}
			}
		}
		ScreenText("←　→　選擇　　　Z　決定　　　X　返回", SW * 0.5f, SH * 0.94f, LabelColor, 0.95f, true);
	}

	void DrawDiffSelect(DanmakuWorld S, float SW, float SH)
	{
		Origin = Vector2.Zero;
		Scale = 1f;
		if (TitleKeyArt != null)
		{
			float AW = TitleKeyArt.GetWidth(), AH = TitleKeyArt.GetHeight();
			float K = FMath.Max(SW / AW, SH / AH);
			Canvas.TexRect(TitleKeyArt, new Vector2((SW - AW * K) * 0.5f, (SH - AH * K) * 0.5f), new Vector2(AW * K, AH * K), new FLinearColor(0.55f, 0.55f, 0.65f, 1f), EBlend.Opaque);
		}
		ScreenRect(0f, 0f, SW, SH, new FLinearColor(0.02f, 0.01f, 0.06f, 0.5f));
		ScreenText("選擇難度", SW * 0.5f, SH * 0.07f, GoldText, 1.7f, true);

		float BarW = SW * 0.5f, BarH = SH * 0.15f, GapY = SH * 0.03f;
		float X = (SW - BarW) * 0.5f;
		float Y = SH * 0.2f;
		for (int i = 0; i < NumDifficulties; ++i)
		{
			FDifficultyInfo D = GetDifficultyInfo(i);
			bool bSel = i == S.Difficulty;
			float Shift = bSel ? 14f * TextScale : 0f;
			InkPanel(X + Shift, Y, BarW, BarH, bSel ? 0.88f : 0.62f);
			if (bSel)
			{
				float T = FMath.Max(2f, 3f * TextScale);
				FLinearColor Glow = WithAlpha(D.Color, 0.6f + 0.3f * FMath.Sin(S.Frame * 0.1f));
				ScreenRect(X + Shift - T, Y, T, BarH, Glow);
				ScreenRect(X + Shift + BarW, Y, T, BarH, Glow);
				if (MagicCircle != null) Sprite(MagicCircle, new FVector2f(X + Shift + BarH * 0.55f, Y + BarH * 0.5f), V2D(BarH * 0.95f), WithAlpha(D.Color, 0.5f), EBlend.Additive, S.Frame * 0.8f);
			}
			FLinearColor TextCol = bSel ? LinColor.White : new FLinearColor(0.6f, 0.58f, 0.7f);
			ScreenText(D.Name, X + Shift + BarH * 1.15f, Y + BarH * 0.12f, bSel ? WithAlpha(D.Color, 1f) : TextCol, 1.6f);
			if (IsChinese()) ScreenText(D.Label, X + Shift + BarW * 0.62f, Y + BarH * 0.16f, TextCol, 1.3f);
			ScreenText(D.Desc, X + Shift + BarH * 1.15f, Y + BarH * 0.6f, bSel ? LabelColor : TextCol, 0.85f);
			Y += BarH + GapY;
		}
		ScreenText("↑　↓　選擇　　　Z　決定　　　X　返回", SW * 0.5f, SH * 0.93f, LabelColor, 0.95f, true);
	}

	/** Touhou-style ending: a stage picture filling the screen, a portrait at the side, narration fading in at the bottom. */
	void DrawEndingScene(DanmakuWorld S, float SW, float SH)
	{
		Origin = Vector2.Zero;
		Scale = 1f;
		FEnding E = Story.Endings[FMath.Clamp((int)S.Player.Character, 0, 3)];
		float Fade = FMath.Clamp(S.StateTimer / 30f, 0f, 1f);
		void Cover(Texture2D Tex, float Dim)
		{
			if (Tex == null) return;
			float AW = Tex.GetWidth(), AH = Tex.GetHeight();
			float K = FMath.Max(SW / AW, SH / AH) * (1.04f + S.StateTimer * 0.00008f);   // a slow push-in
			Canvas.TexRect(Tex, new Vector2((SW - AW * K) * 0.5f, (SH - AH * K) * 0.5f), new Vector2(AW * K, AH * K), new FLinearColor(Dim, Dim, Dim, 1f), EBlend.Opaque);
		}
		ScreenRect(0f, 0f, SW, SH, LinColor.Black);

		if (S.EndingPage >= E.Pages.Length)
		{
			// the last page: the end, the ending number and title
			Cover(TitleKeyArt ?? At(StageBgs, 5), 0.35f);
			ScreenRect(0f, 0f, SW, SH, new FLinearColor(0f, 0f, 0.02f, 0.45f));
			ScreenText("全劇終", SW * 0.5f, SH * 0.3f, WithAlpha(GoldText, Fade), 2.4f, true);
			ScreenText(Tr(E.Title), SW * 0.5f, SH * 0.5f, WithAlpha(LinColor.White, Fade), 1.15f, true, SW * 0.9f);
			ScreenText("感謝遊玩", SW * 0.5f, SH * 0.62f, WithAlpha(LabelColor, Fade), 1.0f, true);
			if ((S.Frame / 30) % 2 == 0 && S.StateTimer > 30) ScreenText("Z　繼續", SW * 0.5f, SH * 0.88f, WithAlpha(LabelColor, 0.8f), 0.85f, true);
			return;
		}

		FEndingPage P = E.Pages[S.EndingPage];
		Cover(P.Bg >= 0 ? At(StageBgs, P.Bg) : TitleKeyArt, 0.75f);
		// darken toward the bottom so the text reads well
		const int Bands = 24;
		for (int b = 0; b < Bands; ++b)
		{
			float T = (float)b / Bands;
			ScreenRect(0f, SH * (0.45f + 0.55f * T), SW, SH * 0.55f / Bands + 1f, new FLinearColor(0.01f, 0f, 0.04f, 0.85f * T));
		}
		Texture2D Portrait = P.Portrait < 0 ? null : P.Portrait < 10 ? PortraitFor(P.Portrait) : At(BossPortraits, P.Portrait - 10);
		if (Portrait != null)
		{
			float H = SH * 0.92f;
			float A = (float)Portrait.GetWidth() / FMath.Max(1, Portrait.GetHeight());
			Canvas.TexRect(Portrait, new Vector2(SW * 0.97f - H * A, SH - H + (1f - Fade) * 20f), new Vector2(H * A, H), new FLinearColor(1f, 1f, 1f, 0.95f * Fade));
		}
		float Y = SH * 0.66f;
		float MaxW = SW * (Portrait != null ? 0.6f : 0.84f);
		for (int i = 0; i < P.Lines.Length; ++i)
		{
			// each line fades in a little after the one before
			float LA = FMath.Clamp((S.StateTimer - 10 - i * 25) / 25f, 0f, 1f);
			if (LA <= 0f) break;
			foreach (string W in WrapPx(P.Lines[i].Zh, MaxW, 0.95f))
			{
				ScreenText(W, SW * 0.08f, Y, WithAlpha(LinColor.White, LA), 0.95f);
				Y += 30f * TextScale;
			}
			Y += 6f * TextScale;
		}
		if ((S.Frame / 30) % 2 == 0 && S.StateTimer > 20 + P.Lines.Length * 25) ScreenText("Z ▶", SW * 0.93f, SH * 0.92f, GoldText, 0.8f);
	}

	void DrawMusicRoom(DanmakuWorld S, float SW, float SH)
	{
		Origin = Vector2.Zero;
		Scale = 1f;
		if (UiPattern != null) TiledRect(UiPattern, 0f, 0f, SW, SH, 300f * TextScale, new FLinearColor(0.7f, 0.7f, 0.78f, 1f));
		ScreenRect(0f, 0f, SW, SH, new FLinearColor(0.02f, 0.01f, 0.06f, 0.6f));
		ScreenText("音樂室", SW * 0.5f, SH * 0.035f, GoldText, 1.7f, true);

		var Tracks = MusicTracks;
		float ListX = SW * 0.05f, ListW = SW * 0.52f, RowH = SH * 0.042f;
		float Y = SH * 0.12f;
		for (int i = 0; i < Tracks.Length; ++i)
		{
			bool bSel = i == S.MusicCursor;
			bool bPlaying = Tracks[i].Track == S.MusicPlaying;
			float X = ListX + (bSel ? 12f * TextScale : 0f);
			if (bSel) ScreenRect(ListX, Y - 2f * TextScale, ListW + 12f * TextScale, RowH, new FLinearColor(0.35f, 0.25f, 0.6f, 0.45f));
			FLinearColor Col = bSel ? GoldText : (bPlaying ? new FLinearColor(0.75f, 1f, 0.85f) : LabelColor);
			bool bShown = S.IsMusicShown(Tracks[i].Track);
			ScreenText($"{i + 1:00}.", X, Y, Col, 0.8f);
			ScreenText((bPlaying ? "♪ " : "") + (bShown ? Tr(Tracks[i].Name) : LockedName), X + 40f * TextScale, Y, bShown ? Col : WithAlpha(Col, 0.6f), 0.8f, false, ListW - 44f * TextScale);
			Y += RowH;
		}

		// the selected track: where it plays and a short comment
		FMusicTrack Cur = Tracks[FMath.Clamp(S.MusicCursor, 0, Tracks.Length - 1)];
		float PX = SW * 0.6f, PY = SH * 0.12f, PW = SW * 0.36f, PH = SH * 0.62f;
		InkPanel(PX, PY, PW, PH, 0.8f);
		bool bCurShown = S.IsMusicShown(Cur.Track);
		ScreenText(bCurShown ? Tr(Cur.Name) : LockedName, PX + 16f * TextScale, PY + 16f * TextScale, GoldText, 0.95f, false, PW - 32f * TextScale);
		ScreenText(bCurShown ? Cur.Where : LockedName, PX + 16f * TextScale, PY + 50f * TextScale, new FLinearColor(0.85f, 0.75f, 1f), 0.8f, false, PW - 32f * TextScale);
		float TY = PY + 92f * TextScale;
		// a song not heard in the game yet: a hint, then (after Z) the spoiler warning
		string Body = bCurShown ? Tr(Cur.Comment)
			: S.MusicConfirm == Cur.Track ? Tr("這首曲子你還沒在遊戲裡聽過，曲名和介紹可能會劇透接下來的內容。確定要聽嗎？")
			: Tr("還沒在遊戲裡聽過的曲子。");
		FLinearColor BodyCol = !bCurShown && S.MusicConfirm == Cur.Track ? new FLinearColor(1f, 0.8f, 0.6f) : LinColor.White;
		foreach (string L in WrapPx(Body, PW - 32f * TextScale, 0.78f))
		{
			ScreenText(L, PX + 16f * TextScale, TY, BodyCol, 0.78f);
			TY += 26f * TextScale;
		}
		if (!bCurShown)
		{
			TY += 14f * TextScale;
			ScreenText(S.MusicConfirm == Cur.Track ? Tr("Z　聽　　X　取消") : Tr("Z　還是想聽"), PX + 16f * TextScale, TY, GoldText, 0.8f);
		}
		if (S.MusicPlaying >= 0)
		{
			foreach (var T in Tracks)
			{
				if (T.Track != S.MusicPlaying) continue;
				ScreenText(Tr("播放中") + "　" + (S.IsMusicShown(T.Track) ? Tr(T.Name) : LockedName), PX, PY + PH + 18f * TextScale, new FLinearColor(0.75f, 1f, 0.85f), 0.8f, false, PW);
			}
		}
		ScreenText("↑　↓　選擇　　Z　播放　　X　返回", SW * 0.5f, SH * 0.93f, LabelColor, 0.95f, true);
	}

	const string LockedName = "？？？？？？？？？？";

	void DrawOptions(DanmakuWorld S, float SW, float SH)
	{
		Origin = Vector2.Zero;
		Scale = 1f;
		if (UiPattern != null) TiledRect(UiPattern, 0f, 0f, SW, SH, 300f * TextScale, new FLinearColor(0.7f, 0.7f, 0.78f, 1f));
		ScreenRect(0f, 0f, SW, SH, new FLinearColor(0.02f, 0.01f, 0.06f, 0.6f));
		ScreenText("選項", SW * 0.5f, SH * 0.06f, GoldText, 1.7f, true);

		string[] Names = { "音樂音量", "音效音量", "返回" };
		int[] Values = { GetMusicVolume(), GetSfxVolume() };
		float PX = SW * 0.24f, PW = SW * 0.52f;
		for (int i = 0; i < 3; ++i)
		{
			bool bSel = i == S.OptionCursor;
			float Y = SH * 0.3f + i * SH * 0.14f;
			InkPanel(PX, Y, PW, SH * 0.1f, bSel ? 0.9f : 0.55f);
			FLinearColor Col = bSel ? GoldText : LabelColor;
			ScreenText(Names[i], PX + 24f * TextScale, Y + SH * 0.025f, Col, 1.05f);
			if (i < 2)
			{
				// ten blocks, filled up to the level, plus the percentage
				float BX = PX + PW * 0.42f, BW = PW * 0.4f, Cell = BW / 10f;
				for (int k = 0; k < 10; ++k)
				{
					bool bOn = k < Values[i];
					ScreenRect(BX + k * Cell + 2f, Y + SH * 0.035f, Cell - 4f, SH * 0.03f,
						bOn ? (bSel ? WithAlpha(GoldText, 0.95f) : new FLinearColor(0.75f, 0.7f, 0.95f, 0.85f)) : new FLinearColor(1f, 1f, 1f, 0.12f));
				}
				ScreenText($"{Values[i] * 10}%", BX + BW + 14f * TextScale, Y + SH * 0.025f, Col, 0.95f);
			}
		}
		ScreenText("↑　↓　選擇　　←　→　調整　　X　返回", SW * 0.5f, SH * 0.9f, LabelColor, 0.95f, true);
	}

	void DrawSpellSelect(DanmakuWorld S, float SW, float SH)
	{
		Origin = Vector2.Zero;
		Scale = 1f;
		if (UiPattern != null) TiledRect(UiPattern, 0f, 0f, SW, SH, 300f * TextScale, new FLinearColor(0.7f, 0.7f, 0.78f, 1f));
		ScreenRect(0f, 0f, SW, SH, new FLinearColor(0.02f, 0.01f, 0.06f, 0.6f));
		ScreenText("符卡練習", SW * 0.5f, SH * 0.04f, GoldText, 1.7f, true);

		// big preview of whoever casts the chosen card, behind the list
		int Art = S.GetSpellEntryArt(S.SpellCursor);
		Texture2D P = At(BossPortraits, Art) ?? PortraitBoss;
		// a card not met yet doesn't show who casts it
		if (P != null && S.IsSpellUnlocked(S.SpellCursor))
		{
			float PH = SH * 0.8f;
			ArtSprite(P, new FVector2f(SW * 0.8f, SH * 0.13f + PH * 0.5f), PH, new FLinearColor(1f, 1f, 1f, 0.95f));
		}

		int N = S.GetSpellEntryCount();
		const int Rows = 13;
		int First = FMath.Clamp(S.SpellCursor - Rows / 2, 0, FMath.Max(0, N - Rows));
		float RowH = SH * 0.058f;
		float ListW = SW * 0.62f;
		float Y = SH * 0.12f;
		for (int i = First; i < FMath.Min(N, First + Rows); ++i)
		{
			bool bSel = i == S.SpellCursor;
			float X = SW * 0.04f + (bSel ? 14f * TextScale : 0f);
			ScreenRect(X, Y, ListW, RowH * 0.88f, bSel ? new FLinearColor(0.16f, 0.1f, 0.3f, 0.92f) : new FLinearColor(0.05f, 0.03f, 0.12f, 0.7f));
			if (bSel) ScreenRect(X, Y, 4f * TextScale, RowH * 0.88f, GoldText);
			FLinearColor TextCol = bSel ? LinColor.White : new FLinearColor(0.62f, 0.6f, 0.72f);
			ScreenText($"{i + 1:00}", X + 10f * TextScale, Y + RowH * 0.16f, bSel ? GoldText : TextCol, 0.8f);
			bool bOpen = S.IsSpellUnlocked(i);   // cards not met in the game yet stay hidden
			ScreenText(bOpen ? S.GetSpellEntryLabel(i) : "？？？", X + 46f * TextScale, Y + RowH * 0.16f, bSel ? GoldText : TextCol, 0.8f, false, ListW * 0.3f);
			ScreenText(bOpen ? S.GetSpellEntryName(i) : LockedName, X + ListW * 0.36f, Y + RowH * 0.14f, bOpen ? TextCol : WithAlpha(TextCol, 0.55f), 0.88f, false, ListW * 0.62f);
			Y += RowH;
		}
		// scroll position
		if (N > Rows)
		{
			float BarTop = SH * 0.12f, BarH = RowH * Rows;
			ScreenRect(SW * 0.04f + ListW + 22f * TextScale, BarTop, 3f, BarH, new FLinearColor(1f, 1f, 1f, 0.15f));
			ScreenRect(SW * 0.04f + ListW + 20f * TextScale, BarTop + BarH * First / N, 7f, BarH * Rows / N, WithAlpha(GoldText, 0.8f));
		}
		ScreenText("↑　↓　選擇　　←　→　翻頁　　Z　決定　　X　返回", SW * 0.5f, SH * 0.93f, LabelColor, 0.95f, true);
	}

	static readonly string[] StageNames = { "第一面", "第二面", "第三面", "第四面", "第五面", "第六面" };

	void DrawStageSelect(DanmakuWorld S, float SW, float SH)
	{
		Origin = Vector2.Zero;
		Scale = 1f;
		if (UiPattern != null) TiledRect(UiPattern, 0f, 0f, SW, SH, 300f * TextScale, new FLinearColor(0.7f, 0.7f, 0.78f, 1f));
		ScreenRect(0f, 0f, SW, SH, new FLinearColor(0.02f, 0.01f, 0.06f, 0.55f));
		ScreenText("關卡練習", SW * 0.5f, SH * 0.05f, GoldText, 1.7f, true);

		int N = DanmakuWorld.GetNumStages();
		float BarW = SW * 0.46f, BarH = FMath.Min(SH * 0.12f, SH * 0.7f / FMath.Max(1, N));
		float Y = SH * 0.17f;
		for (int i = 0; i < N; ++i)
		{
			bool bSel = i == S.PracticeStage;
			float X = SW * 0.06f + (bSel ? 16f * TextScale : 0f);
			InkPanel(X, Y, BarW, BarH * 0.86f, bSel ? 0.88f : 0.6f);
			// small preview of that stage's background as a strip
			Texture2D Bg = At(StageBgs, i) ?? StageBg;
			if (Bg != null)
			{
				Canvas.TexRect(Bg, new Vector2(X + 6f, Y + 6f), new Vector2(BarH * 1.2f, BarH * 0.86f - 12f),
					new FLinearColor(bSel ? 1f : 0.5f, bSel ? 1f : 0.5f, bSel ? 1f : 0.55f, 1f), EBlend.Opaque,
					new Vector2(0f, 0.3f), new Vector2(1f, 0.45f));
			}
			FLinearColor TextCol = bSel ? LinColor.White : new FLinearColor(0.6f, 0.58f, 0.7f);
			ScreenText(StageNames[FMath.Clamp(i, 0, 5)], X + BarH * 1.35f, Y + BarH * 0.12f, bSel ? GoldText : TextCol, 1.3f);
			Y += BarH;
		}
		// big preview of the chosen boss
		Texture2D P = At(BossPortraits, S.PracticeStage) ?? PortraitBoss;
		if (P != null)
		{
			float PH = SH * 0.74f;
			ArtSprite(P, new FVector2f(SW * 0.76f, SH * 0.16f + PH * 0.5f), PH, LinColor.White);
		}
		ScreenText("↑　↓　選擇　　　Z　決定　　　X　返回", SW * 0.5f, SH * 0.93f, LabelColor, 0.95f, true);
	}

	// =====================================================================================
	// Playfield layers
	// =====================================================================================

	void DrawBackground(DanmakuWorld S)
	{
		bool bSpell = S.Boss.bActive && S.Boss.bSpell;
		SpellBlend = FMath.Lerp(SpellBlend, bSpell ? 1f : 0f, 0.04f);

		Texture2D Bg = StageBgFor(S);
		if (Bg != null)
		{
			float TileH = FieldW * (float)Bg.GetHeight() / (float)FMath.Max(1, Bg.GetWidth());
			double V0 = -(double)S.Frame * 0.45 / TileH;
			// keep the UV offset small (float precision on long runs); the texture repeats every 1.0 anyway
			V0 -= Math.Floor(V0);
			Canvas.TexRect(Bg, ToScreen(new FVector2f(0f, 0f)), new Vector2(FieldW, FieldH) * Scale, LinColor.White, EBlend.Opaque,
				new Vector2(0f, (float)V0), new Vector2(1f, (float)(V0 + FieldH / TileH)), bRepeat: true);
		}
		else
		{
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0.03f, 0.02f, 0.09f, 1f));
		}
		if (SpellBg != null && SpellBlend > 0.01f)
		{
			float TileH = FieldW * (float)SpellBg.GetHeight() / (float)FMath.Max(1, SpellBg.GetWidth());
			double V0 = -(double)S.Frame * 0.12 / TileH;
			V0 -= Math.Floor(V0);
			Canvas.TexRect(SpellBg, ToScreen(new FVector2f(0f, 0f)), new Vector2(FieldW, FieldH) * Scale, new FLinearColor(1f, 1f, 1f, SpellBlend), EBlend.Translucent,
				new Vector2(0f, (float)V0), new Vector2(1f, (float)(V0 + FieldH / TileH)), bRepeat: true);
		}
		FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0.01f, 0.0f, 0.04f, 0.32f));
		if (S.IsTimeSlowed())
		{
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0.1f, 0.35f, 0.15f, 0.12f));
		}

		for (int i = 0; i < 40; ++i)
		{
			float H1 = Hash(i * 3 + 1), H2 = Hash(i * 3 + 2), H3 = Hash(i * 3 + 3);
			var P = new FVector2f(H1 * FieldW, FMath.Fmod(H2 * FieldH + S.Frame * (0.3f + H3 * 1.6f), FieldH));
			Sprite(SoftTex, P, V2D(2f + H3 * 5f), new FLinearColor(0.65f, 0.75f, 1f, 0.25f + H3 * 0.5f), EBlend.Additive);
		}

		if (bSpell && MagicCircle != null)
		{
			Sprite(MagicCircle, S.Boss.Pos, V2D(330f), new FLinearColor(1f, 0.85f, 0.55f, 0.13f * SpellBlend), EBlend.Additive, S.Frame * 0.25f);
			Sprite(MagicCircle, S.Boss.Pos, V2D(190f), new FLinearColor(0.85f, 0.6f, 1f, 0.12f * SpellBlend), EBlend.Additive, -S.Frame * 0.4f);
		}
	}

	void DrawItems(DanmakuWorld S)
	{
		foreach (var I in S.Items)
		{
			FVector2f P = I.Pos;
			bool bAbove = P.Y < 0f;
			if (bAbove) P.Y = 6f;
			var Tint = new FLinearColor(1f, 1f, 1f, bAbove ? 0.5f : 1f);
			switch (I.Type)
			{
			// power and point items are drawn half see-through so they don't hide bullets
			case EItemType.Power:    if (ItemPower != null) ArtSprite(ItemPower, P, 13f, WithAlpha(Tint, Tint.A * 0.5f)); break;
			case EItemType.BigPower: if (ItemPower != null) ArtSprite(ItemPower, P, 22f, WithAlpha(Tint, Tint.A * 0.5f)); break;
			case EItemType.Point:    if (ItemPoint != null) ArtSprite(ItemPoint, P, 13f, WithAlpha(Tint, Tint.A * 0.5f)); break;
			case EItemType.LifePiece:
			case EItemType.BombPiece:
			{
				bool bLife = I.Type == EItemType.LifePiece;
				FLinearColor Glow = bLife ? new FLinearColor(1f, 0.5f, 0.7f, 0.7f * Tint.A) : new FLinearColor(0.5f, 1f, 0.6f, 0.7f * Tint.A);
				Sprite(SoftTex, P, V2D(34f + 4f * FMath.Sin(I.Age * 0.2f)), Glow, EBlend.Additive);
				Texture2D Icon = bLife ? IconLife : IconBomb;
				if (Icon != null) ArtSprite(Icon, P, 18f, Tint);
				break;
			}
			case EItemType.OneUp:
			{
				// the 1UP: a pulsing pink glow so it stands out from power and point items
				Sprite(SoftTex, P, V2D(46f + 6f * FMath.Sin(I.Age * 0.2f)), new FLinearColor(1f, 0.45f, 0.65f, 0.75f * Tint.A), EBlend.Additive);
				Texture2D Icon = ItemOneUp ?? IconLife;
				if (Icon != null) ArtSprite(Icon, P, 24f, Tint);
				break;
			}
			case EItemType.Star:
				Sprite(BulletStar ?? SoftTex, P, V2D(12f), new FLinearColor(0.6f, 1f, 0.6f, 0.8f * Tint.A), EBlend.Additive, I.Age * 6f);
				break;
			}
		}
	}

	// on-screen heights of the characters (field units), about Touhou's proportions; hitboxes are separate and unchanged
	const float PlayerSpriteH = 46f, BossSpriteH = 74f, QuadSpriteH = 62f;

	void DrawEnemies(DanmakuWorld S)
	{
		foreach (var E in S.Enemies)
		{
			float R = E.Radius;
			FLinearColor Tint = E.HitFlash > 0 ? new FLinearColor(1f, 0.7f, 0.7f) : LinColor.White;
			Sprite(SoftTex, E.Pos, V2D(R * 4f), WithAlpha(E.Color, 0.35f), EBlend.Additive);
			Texture2D Art = E.Kind == 2 ? SpiritArt : (E.Kind == 1 ? FairyRedArt : FairyBlueArt);
			float T = (S.Frame + E.Age) + E.Pos.X;
			if (E.Kind == 2) WarpSprite(Art, E.Pos, R * 2.6f, Tint, -E.Vel.X * 0.4f, 1.6f, T * 0.22f, 0.16f, 0.0f);
			else             WarpSprite(Art, E.Pos, R * 3.2f, Tint, -E.Vel.X * 0.4f, 1.4f, T * 0.16f, 0.12f, 0.2f);
		}
	}

	static readonly FLinearColor[] QuadGlow = { new(0.5f, 0.9f, 0.4f, 0.4f), new(1f, 0.3f, 0.3f, 0.4f), new(0.9f, 0.9f, 1f, 0.4f), new(0.4f, 0.5f, 1f, 0.4f) };

	void DrawBoss(DanmakuWorld S)
	{
		FBossState B = S.Boss;
		if (!B.bActive) return;

		Sprite(SoftTex, B.Pos, V2D(150f), new FLinearColor(0.45f, 0.35f, 1f, 0.35f), EBlend.Additive);
		Sprite(RingTex, B.Pos, V2D(110f), new FLinearColor(1f, 0.85f, 0.55f, 0.55f), EBlend.Additive, S.Frame * 0.8f);

		if (S.IsSurvivalPhase())
		{
			Sprite(RingTex, B.Pos, V2D(84f + 6f * FMath.Sin(S.Frame * 0.1f)), new FLinearColor(1f, 0.85f, 0.45f, 0.75f), EBlend.Additive, -S.Frame * 1.5f);
			Sprite(SoftTex, B.Pos, V2D(110f), new FLinearColor(1f, 0.8f, 0.4f, 0.25f), EBlend.Additive);
		}
		if (S.IsShielded())
		{
			// spell card start: a few seconds where it can't be damaged
			float K = FMath.Min(1f, S.Boss.ShieldTimer / 30f);
			Sprite(RingTex, B.Pos, V2D(78f + 4f * FMath.Sin(S.Frame * 0.3f)), new FLinearColor(0.6f, 0.85f, 1f, 0.8f * K), EBlend.Additive, S.Frame * 2f);
			Sprite(SoftTex, B.Pos, V2D(100f), new FLinearColor(0.5f, 0.75f, 1f, 0.3f * K), EBlend.Additive);
		}
		FLinearColor BodyTint = B.HitFlash > 0 ? new FLinearColor(1f, 0.8f, 0.85f) : LinColor.White;
		// survival cards: the boss turns half see-through (its body can't hit you), and back to solid once the card ends
		if (S.IsSurvivalPhase()) BodyTint = WithAlpha(BodyTint, 0.5f);
		// lean into the direction of travel (smoothed so the pose doesn't flicker)
		BossLean = FMath.Lerp(BossLean, FMath.Clamp(B.Vel.X * 1.6f, -2f, 2f), 0.15f);
		Texture2D BeastSheet(int ArtIdx) => At(BossSheets, ArtIdx);
		if (B.DepartTimer > 0 && B.DepartActor >= 0)
		{
			// the beast that just finished its cards flies off
			float A = FMath.Min(1f, B.DepartTimer / 40f);
			Texture2D DepartAnim = At(BossAnims, DanmakuWorld.BeastArt + B.DepartActor);
			if (DepartAnim != null) AnimSprite(DepartAnim, B.DepartPos, BossSpriteH, new FLinearColor(1f, 1f, 1f, A), B.DepartVel.X > 0f ? 2 : 1, S.Frame / DanmakuWorld.AnimIdleStep);
			else SheetSprite(BeastSheet(DanmakuWorld.BeastArt + B.DepartActor), B.DepartPos, BossSpriteH, new FLinearColor(1f, 1f, 1f, A), B.DepartVel.X > 0f ? 2f : -2f, S.Frame / 6);
		}
		if (S.IsQuadPhase())
		{
			for (int i = 0; i < 4; ++i)
			{
				Sprite(SoftTex, B.QuadPos[i], V2D(110f), QuadGlow[i], EBlend.Additive);
				Texture2D QuadAnim = At(BossAnims, DanmakuWorld.BeastArt + i);
				if (QuadAnim != null) AnimSprite(QuadAnim, B.QuadPos[i] + new FVector2f(0f, 5f), QuadSpriteH, BodyTint, 0, S.Frame / DanmakuWorld.AnimIdleStep + i * 3);
				else SheetSprite(BeastSheet(DanmakuWorld.BeastArt + i), B.QuadPos[i] + new FVector2f(0f, 5f), QuadSpriteH, BodyTint, 0f, S.Frame / 6 + i * 3);
			}
			return;
		}
		int ArtI = S.GetBossArtIndex();
		Texture2D Sheet = At(BossSheets, ArtI);
		Texture2D Anim = At(BossAnims, ArtI);
		Texture2D Body;
		if (Anim != null)
		{
			// moving left / right plays that loop, standing still plays idle (with a dead zone so it doesn't flicker)
			int Row = BossLean < -0.6f ? 1 : BossLean > 0.6f ? 2 : 0;
			AnimSprite(Anim, B.Pos + new FVector2f(0f, 5f), BossSpriteH, BodyTint, Row, S.Frame / DanmakuWorld.AnimIdleStep);
		}
		else if (Sheet != null)
		{
			SheetSprite(Sheet, B.Pos + new FVector2f(0f, 5f), BossSpriteH, BodyTint, BossLean, S.Frame / 6);
		}
		else if ((Body = BossBodyFor(S)) != null)
		{
			WarpSprite(Body, B.Pos + new FVector2f(0f, 5f), BossSpriteH, BodyTint, FMath.Clamp(B.Vel.X * 0.35f, -1f, 1f), 1f, S.Frame * 0.08f,
				0.14f, 0.32f, BossBodyHair(S));
		}
		else
		{
			Sprite(DiscTex, B.Pos, new Vector2(30f, 38f), new FLinearColor(0.3f, 0.25f, 0.6f));
		}
		FieldRect(B.Pos.X - 14f, FieldH - 5f, 28f, 5f, new FLinearColor(1f, 0.3f, 0.4f, 0.8f));
	}

	void DrawShots(DanmakuWorld S)
	{
		FDanmakuPlayer P = S.Player;
		FLinearColor CharCol = GetCharacterInfo(P.Character).Color;
		// 蓮 and 晴 both throw paper charms, drawn in their own colours whatever the shot does:
		// 蓮's have red writing on them, 晴's green
		bool bPaper = P.Character == ECharacter.Ren || P.Character == ECharacter.Haru;
		bool bGreen = P.Character == ECharacter.Haru;
		Texture2D Paper = bGreen ? (ShotTalismanGreen ?? ShotTalisman) : ShotTalisman;
		var PaperTint = new FLinearColor(1f, 1f, 1f, 0.95f);
		var PaperGlow = bGreen ? new FLinearColor(0.45f, 1f, 0.5f, 0.35f) : new FLinearColor(1f, 0.4f, 0.35f, 0.35f);
		foreach (var Shot in S.Shots)
		{
			float Rot = AngleOf(Shot.Vel) + 90f;
			switch (Shot.Type)
			{
			case EShotType.Talisman:
				Sprite(SoftTex, Shot.Pos, new Vector2(10f, 26f), bPaper ? PaperGlow : new FLinearColor(1f, 0.5f, 0.5f, 0.35f), EBlend.Additive, Rot);
				ArtSprite(bPaper ? Paper : ShotTalisman, Shot.Pos, 16f, bPaper ? PaperTint : new FLinearColor(1f, 1f, 1f, 0.85f), Rot);
				break;
			case EShotType.Straight:
				Sprite(SoftTex, Shot.Pos, new Vector2(9f, 26f), bPaper ? PaperGlow : WithAlpha(CharCol, 0.5f), EBlend.Additive, Rot);
				ArtSprite(bPaper ? Paper : ShotTalisman, Shot.Pos, 15f, bPaper ? PaperTint
					: new FLinearColor(CharCol.R * 0.6f + 0.4f, CharCol.G * 0.6f + 0.4f, CharCol.B * 0.6f + 0.4f, 0.8f), Rot);
				break;
			case EShotType.Homing:
				Sprite(SoftTex, Shot.Pos, V2D(20f), bPaper ? WithAlpha(PaperGlow, 0.45f) : new FLinearColor(0.5f, 1f, 0.5f, 0.45f), EBlend.Additive);
				ArtSprite(bPaper ? Paper : ShotTalisman, Shot.Pos, 15f, bPaper ? PaperTint : new FLinearColor(0.7f, 1f, 0.7f, 0.95f), Rot);
				break;
			case EShotType.Doll:
				Sprite(BulletOrb ?? SoftTex, Shot.Pos, V2D(12f), new FLinearColor(0.95f, 0.75f, 1f, 0.85f), EBlend.Translucent);
				break;
			case EShotType.Arrow:
				if (Shot.bPierce)
				{
					// the piercing arrow: bigger, wrapped in red lightning, with a streak behind it
					Beam(Shot.Pos + new FVector2f(0f, 40f), Shot.Pos, 7f, new FLinearColor(1f, 0.45f, 0.35f, 0.45f));
					Sprite(SoftTex, Shot.Pos, new Vector2(20f, 48f), new FLinearColor(1f, 0.5f, 0.4f, 0.55f), EBlend.Additive, Rot);
					if (FxBolt != null) Sprite(FxBolt, Shot.Pos + new FVector2f(0f, 6f), new Vector2(16f, 46f), new FLinearColor(1f, 0.75f, 0.7f, 0.8f), EBlend.Additive, Rot + ((S.Frame / 2) % 2) * 180f);
					ArtSprite(ArrowTex ?? ShotTalisman, Shot.Pos, 40f, LinColor.White, Rot);
				}
				else
				{
					Sprite(SoftTex, Shot.Pos, new Vector2(8f, 30f), WithAlpha(CharCol, 0.35f), EBlend.Additive, Rot);
					ArtSprite(ArrowTex ?? ShotTalisman, Shot.Pos, 30f, new FLinearColor(1f, 1f, 1f, 0.9f), Rot);
				}
				break;
			}
		}
	}

	void DrawPlayer(DanmakuWorld S)
	{
		FDanmakuPlayer P = S.Player;
		if (S.State == EDanmakuState.GameOver || P.DeathTimer > 0) return;

		// Yuki's puppets, connected by glowing threads
		if (P.Character == ECharacter.Yuki)
		{
			for (int i = 0; i < P.DollsAlive; ++i)
			{
				FVector2f D = P.Dolls[i];
				Beam(P.Pos + new FVector2f(i < 2 ? -8f : 8f, -6f), D, 2f, new FLinearColor(0.9f, 0.75f, 1f, 0.45f));
				Sprite(SoftTex, D, V2D(24f), new FLinearColor(0.85f, 0.6f, 1f, 0.4f), EBlend.Additive);
				if (FairyBlueArt != null) WarpSprite(FairyBlueArt, D, 18f, new FLinearColor(1f, 0.85f, 1f, 1f), 0f, 1.4f, S.Frame * 0.2f + i, 0.12f, 0.2f);
			}
		}

		if (P.Character == ECharacter.Haru && P.RespawnTimer == 0)
		{
			Sprite(RingTex, P.Pos, V2D(DanmakuWorld.HaruSlowRadius * 2f), new FLinearColor(0.55f, 1f, 0.6f, 0.22f), EBlend.Additive, S.Frame * 0.4f);
		}
		if (P.Invuln > 0 && (S.Frame / 3) % 2 == 0) return;
		Sprite(SoftTex, P.Pos, V2D(38f), WithAlpha(GetCharacterInfo(P.Character).Color, 0.3f), EBlend.Additive);
		Texture2D Back = PlayerBackFor((int)P.Character);
		if (Back != null)
		{
			Texture2D Sheet = At(PlayerSheets, (int)P.Character);
			Texture2D Anim = At(PlayerAnims, (int)P.Character);
			if (Anim != null)
				AnimSprite(Anim, P.Pos + new FVector2f(0f, -3f), PlayerSpriteH, LinColor.White,
					P.AnimDir < 0 ? 1 : P.AnimDir > 0 ? 2 : 0, P.AnimDir == 0 ? S.Frame / DanmakuWorld.AnimIdleStep : P.AnimFrame);
			else if (Sheet != null) SheetSprite(Sheet, P.Pos + new FVector2f(0f, -3f), PlayerSpriteH, LinColor.White, P.Lean / 6f, S.Frame / 5);
			else ArtSprite(Back, P.Pos + new FVector2f(0f, -3f), PlayerSpriteH, LinColor.White);
		}
		else
		{
			Sprite(DiscTex, P.Pos, new Vector2(18f, 24f), new FLinearColor(0.2f, 0.3f, 0.6f));
		}
	}

	// grouped per texture so the canvas can batch them
	static readonly EBulletShape[][] BulletGroups = {
		new[] { EBulletShape.Small, EBulletShape.Medium, EBulletShape.Large },
		new[] { EBulletShape.Rice, EBulletShape.Comet, EBulletShape.Comet },
		new[] { EBulletShape.Star, EBulletShape.Star, EBulletShape.Star },
		new[] { EBulletShape.Crescent, EBulletShape.Crescent, EBulletShape.Crescent },
		new[] { EBulletShape.Sakura, EBulletShape.Sakura, EBulletShape.Sakura },
		new[] { EBulletShape.Maple, EBulletShape.Maple, EBulletShape.Maple },
		new[] { EBulletShape.Snow, EBulletShape.Snow, EBulletShape.Snow },
		new[] { EBulletShape.Water, EBulletShape.Water, EBulletShape.Water },
		new[] { EBulletShape.Fire, EBulletShape.Fire, EBulletShape.Fire },
		new[] { EBulletShape.Chrys, EBulletShape.Chrys, EBulletShape.Chrys },
		new[] { EBulletShape.Tile, EBulletShape.Tile, EBulletShape.Tile },
	};

	void DrawBullets(DanmakuWorld S)
	{
		foreach (var G in BulletGroups)
		{
			Texture2D Tex = BulletTex((byte)G[0]);
			if (Tex == null) Tex = DiscTex;
			for (int Pass = 0; Pass < 2; ++Pass)
			{
				foreach (var B in S.Bullets)
				{
					if (B.Shape != G[0] && B.Shape != G[1] && B.Shape != G[2]) continue;
					float V = BulletVisualSize(B.Shape);
					float Spawn = B.Age < 8 ? (8 - B.Age) / 8f : 0f;
					float K = 1f + Spawn * 1.2f;
					float Rot = 0f;
					Vector2 Size = V2D(V * 1.25f);
					switch (B.Shape)
					{
					case EBulletShape.Rice:     Rot = B.Angle + 90f; Size = V2D(V * 1.6f); break;
					case EBulletShape.Comet:    Rot = B.Angle + 90f; Size = V2D(V * 1.7f); break;
					case EBulletShape.Crescent: Rot = B.Angle + 90f; Size = V2D(V * 1.6f); break;
					case EBulletShape.Star:     Rot = B.Age * 5f;    Size = V2D(V * 1.7f); break;
					case EBulletShape.Sakura:
					case EBulletShape.Maple:
					case EBulletShape.Snow:
					case EBulletShape.Chrys:    Rot = B.Age * (B.Spin != 0f ? B.Spin : 2f); Size = V2D(V * 1.6f); break;
					case EBulletShape.Tile:     Rot = B.Spin != 0f ? B.Age * B.Spin : 0f;  Size = V2D(V * 1.5f); break;
					case EBulletShape.Water:
					case EBulletShape.Fire:     Rot = B.Angle - 90f; Size = V2D(V * 1.8f); break;   // tip trails behind
					default: break;
					}
					Size *= B.Scale;
					if (Pass == 0)
					{
						Sprite(Tex, B.Pos, Size * K, WithAlpha(B.Color, 1f - Spawn * 0.7f), EBlend.Translucent, Rot);
					}
					else
					{
						Sprite(Tex, B.Pos, Size * 0.55f, new FLinearColor(1f, 1f, 1f, (1f - Spawn) * 0.9f), EBlend.Translucent, Rot);
					}
				}
			}
		}
	}

	static readonly float[] BeamSides = { -0.5f, 0.5f };

	void DrawBeams(DanmakuWorld S)
	{
		foreach (var B in S.Bullets)
		{
			if (B.Behavior == 2)   // DanmakuWorld.BehLotusSummon
			{
				float Pulse = 1f + 0.15f * FMath.Sin(B.Age * 0.4f);
				Sprite(RingTex, B.Pos, V2D(34f * Pulse), WithAlpha(B.Color, 0.8f), EBlend.Additive, B.Age * 3f);
				FieldText(B.Param.ToString(), B.Pos.X, B.Pos.Y - 10f, new FLinearColor(1f, 1f, 1f, 1f), 1.1f, true);
			}
			else if (B.Behavior == 4)   // assassin: a warning halo
			{
				Sprite(RingTex, B.Pos, V2D(40f), WithAlpha(B.Color, 0.7f), EBlend.Additive, -B.Age * 4f);
			}
		}
		foreach (var Bm in S.Beams)
		{
			FVector2f D = Dir(Bm.Angle);
			if (!Bm.IsActive())
			{
				// warning: the full width of the strike glows red, with a bright centre line that blinks faster near impact
				float Left = (float)(Bm.Warn - Bm.Age) / (float)FMath.Max(1, Bm.Warn);
				float Speed = FMath.Lerp(0.9f, 0.25f, Left);
				float Blink = 0.5f + 0.5f * FMath.Sin(Bm.Age * Speed);
				FVector2f To = Bm.Origin + D * Bm.MaxLength;
				var Warn = new FLinearColor(1f, 0.25f, 0.2f, 1f);
				var Perp = new FVector2f(-D.Y, D.X);
				foreach (float Side in BeamSides)
				{
					FVector2f Off = Perp * (Bm.Width * Side);
					Canvas.Line(ToScreen(Bm.Origin + Off), ToScreen(To + Off), FMath.Max(1f, 1.5f * Scale), WithAlpha(Warn, 0.55f + 0.35f * Blink));
				}
				Beam(Bm.Origin, To, Bm.Width * 1.6f, WithAlpha(Warn, 0.18f + 0.22f * Blink));
				Beam(Bm.Origin, To, FMath.Max(3f, Bm.Width * 0.25f), new FLinearColor(1f, 0.9f, 0.8f, 0.45f + 0.5f * Blink));
				Sprite(SoftTex, Bm.Origin, V2D(Bm.Width * 2.6f), WithAlpha(Warn, 0.6f + 0.3f * Blink), EBlend.Additive);
				FieldText("！", Bm.Origin.X - D.X * 12f, FMath.Clamp(Bm.Origin.Y + D.Y * 14f - 10f, 2f, FieldH - 24f), WithAlpha(Warn, 0.6f + 0.4f * Blink), 1.1f, true);
				continue;
			}
			int End = Bm.Warn + Bm.Rise + Bm.Hold;
			float Fade = FMath.Clamp((float)(End - Bm.Age) / 12f, 0f, 1f);
			FVector2f Tip = Bm.Origin + D * Bm.Length;
			if (Bm.OnStrike == 1 && FxBolt != null) { DrawLightning(S, Bm, D, Tip, Fade); continue; }
			if (Bm.OnStrike == 2 && FxBamboo != null) { DrawBamboo(Bm, D, Fade); continue; }
			Beam(Bm.Origin, Tip, Bm.Width * 2.4f, WithAlpha(Bm.Color, 0.45f * Fade));
			Beam(Bm.Origin, Tip, Bm.Width * 1.1f, WithAlpha(Bm.Color, 0.9f * Fade));
			Beam(Bm.Origin, Tip, Bm.Width * 0.45f, new FLinearColor(1f, 1f, 1f, 0.9f * Fade));
			Sprite(SoftTex, Tip, V2D(Bm.Width * 2.2f), WithAlpha(Bm.Color, 0.8f * Fade), EBlend.Additive);
		}
	}

	/** 嵐's lightning: a glow column with jagged bolts that re-flicker every couple of frames, and a flash where it lands. */
	void DrawLightning(DanmakuWorld S, FBeamHazard Bm, FVector2f D, FVector2f Tip, float Fade)
	{
		float Flash = Bm.Age - Bm.Warn < 6 ? 1f : 0.75f + 0.25f * FMath.Sin(Bm.Age * 2.1f);
		Beam(Bm.Origin, Tip, Bm.Width * 3.2f, WithAlpha(Bm.Color, 0.28f * Fade * Flash));
		Beam(Bm.Origin, Tip, Bm.Width * 0.9f, new FLinearColor(1f, 1f, 1f, 0.35f * Fade));
		float SegL = 170f, W = Bm.Width * 2.2f;
		float Rot = Bm.Angle + 90f;
		var Perp = new FVector2f(-D.Y, D.X);
		for (int k = 0; k * SegL < Bm.Length; ++k)
		{
			int Seed = (Bm.Age / 2) * 31 + k * 17 + (int)Bm.Origin.X;
			float Jit = FMath.Sin(Seed * 1.37f) * Bm.Width * 0.5f;
			float Turn = (Seed & 1) == 0 ? 0f : 180f;   // vary the bolt by turning it end over end (a negative size would shift it)
			FVector2f C = Bm.Origin + D * ((k + 0.5f) * SegL) + Perp * Jit;
			Sprite(FxBolt, C, new Vector2(W * 1.6f, SegL * 1.08f), WithAlpha(Bm.Color, 0.75f * Fade * Flash), EBlend.Additive, Rot + Turn);
			Sprite(FxBolt, C, new Vector2(W, SegL * 1.04f), new FLinearColor(1f, 1f, 1f, 0.95f * Fade * Flash), EBlend.Additive, Rot + Turn);
		}
		Sprite(FxFlash ?? SoftTex, Bm.Origin, V2D(Bm.Width * 7f), WithAlpha(Bm.Color, 0.7f * Fade * Flash), EBlend.Additive);
		Sprite(SoftTex, Bm.Origin, V2D(Bm.Width * 9f), new FLinearColor(1f, 0.95f, 0.85f, 0.55f * Fade), EBlend.Additive);
	}

	/** 墨竹's bamboo: the painted stalk grows from where it was warned; its trunk is exactly the beam's hit width. */
	void DrawBamboo(FBeamHazard Bm, FVector2f D, float Fade)
	{
		const float StalkFrac = 0.2f;   // trunk width / texture width in fx_bamboo.png
		float W = Bm.Width * 2f / StalkFrac;
		float SegL = W * FxBamboo.GetHeight() / FMath.Max(1f, FxBamboo.GetWidth());
		float Rot = Bm.Angle + 90f;
		Beam(Bm.Origin, Bm.Origin + D * Bm.Length, Bm.Width * 3f, WithAlpha(Bm.Color, 0.22f * Fade));   // soft green glow
		for (float L = 0f; L < Bm.Length; L += SegL)
		{
			float Piece = FMath.Min(SegL, Bm.Length - L);
			FVector2f C = Bm.Origin + D * (L + Piece * 0.5f);
			// the growing top piece is squashed rather than cut, which reads as the shoot still pushing up
			Sprite(FxBamboo, C, new Vector2(W, Piece), new FLinearColor(1f, 1f, 1f, Fade), EBlend.Translucent, Rot);
		}
	}

	void DrawFog(DanmakuWorld S)
	{
		if (S.FogRadius <= 0f || HoleTex == null) return;
		float R = S.FogRadius;
		FVector2f P = S.Player.Pos;
		var Fog = new FLinearColor(0.07f, 0.07f, 0.13f, 0.96f);
		// soft-edged hole around the player, solid fog everywhere else (clipped to the field)
		float L = FMath.Max(0f, P.X - R), T = FMath.Max(0f, P.Y - R);
		float Rr = FMath.Min(FieldW, P.X + R), Bt = FMath.Min(FieldH, P.Y + R);
		FieldRect(0f, 0f, FieldW, T, Fog);
		FieldRect(0f, Bt, FieldW, FieldH - Bt, Fog);
		FieldRect(0f, T, L, Bt - T, Fog);
		FieldRect(Rr, T, FieldW - Rr, Bt - T, Fog);
		Sprite(HoleTex, P, V2D(R * 2f), Fog, EBlend.Translucent);
	}

	void DrawParticles(DanmakuWorld S)
	{
		foreach (var P in S.Particles)
		{
			float T = (float)P.Life / P.MaxLife;
			if (P.bRing) Sprite(RingTex, P.Pos, V2D(10f + P.Size * (1f - T)), WithAlpha(P.Color, T * 0.9f), EBlend.Additive, (1f - T) * 90f);
			else         Sprite(SoftTex, P.Pos, V2D(1f + P.Size * T), WithAlpha(P.Color, T), EBlend.Additive);
		}
	}

	void DrawBomb(DanmakuWorld S)
	{
		if (S.BombTime <= 0) return;
		int Elapsed = S.BombDuration - S.BombTime;
		FDanmakuPlayer P = S.Player;
		FLinearColor C = GetCharacterInfo(S.BombKind).Color;
		float In = FMath.Min(1f, Elapsed / 12f);                 // quick build-up
		float Out = FMath.Min(1f, S.BombTime / 30f);             // fade at the end
		float A = In * Out;

		// the moment it goes off: a white flash and a shockwave ring from the player
		if (Elapsed < 10) FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(1f, 1f, 1f, 0.45f * (1f - Elapsed / 10f)));
		if (Elapsed < 40)
		{
			float K = Elapsed / 40f;
			Sprite(FxFlash ?? RingTex, S.BombCenter, V2D(40f + 520f * K), WithAlpha(C, 0.9f * (1f - K)), EBlend.Additive);
		}

		switch (S.BombKind)
		{
		case ECharacter.Ren:
		{
			// barrier seal: a great talisman circle that rides the shockwave, a second seal turning the other way
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0.75f, 0.85f, 1f, 0.12f * A));
			float R = FMath.Max(60f, S.BombRadius);
			Sprite(FxSeal ?? RingTex, S.BombCenter, V2D(R * 2.1f), new FLinearColor(0.75f, 0.88f, 1f, 0.95f * A), EBlend.Additive, S.Frame * 1.5f);
			Sprite(FxSeal ?? RingTex, S.BombCenter, V2D(R * 1.2f), new FLinearColor(1f, 0.85f, 0.6f, 0.7f * A), EBlend.Additive, -S.Frame * 2.4f);
			Sprite(SoftTex, S.BombCenter, V2D(R * 1.7f), new FLinearColor(0.5f, 0.7f, 1f, 0.22f * A), EBlend.Additive);
			// talismans flung out along the ring
			for (int k = 0; k < 16; ++k)
			{
				float Ang = k * 22.5f + S.Frame * 1.2f;
				FVector2f Dir = new FVector2f(FMath.Cos(FMath.DegreesToRadians(Ang)), FMath.Sin(FMath.DegreesToRadians(Ang)));
				Sprite(ShotTalisman ?? SoftTex, S.BombCenter + Dir * (R * 0.92f), new Vector2(14f, 26f), new FLinearColor(1f, 1f, 1f, A), EBlend.Translucent, Ang + 90f);
			}
			break;
		}
		case ECharacter.Sou:
		{
			// heaven-thunder: the beam, a column of crackling lightning inside it, and the sky lit up
			if (S.bBombArrowFlying)
			{
				// the arrow on its way up
				Beam(S.BombArrowPos + new FVector2f(0f, 70f), S.BombArrowPos, 10f, new FLinearColor(1f, 0.5f, 0.4f, 0.6f));
				Sprite(SoftTex, S.BombArrowPos, V2D(60f), new FLinearColor(1f, 0.6f, 0.45f, 0.7f), EBlend.Additive);
				ArtSprite(ArrowTex ?? ShotTalisman, S.BombArrowPos, 64f, LinColor.White, 0f);
				break;
			}
			float W = DanmakuWorld.ThunderHalfWidth * 2f;
			// the column stands where the arrow landed, from the top of the field to the bottom
			FVector2f From = new FVector2f(S.BombCenter.X, FieldH + 20f), To = new FVector2f(S.BombCenter.X, -20f);
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(1f, 0.55f, 0.45f, 0.07f * A * (0.7f + 0.3f * FMath.Sin(S.Frame * 1.7f))));
			Beam(From, To, W * 2.2f, new FLinearColor(1f, 0.45f, 0.35f, 0.35f * A));
			Beam(From, To, W * 1.3f, new FLinearColor(1f, 0.7f, 0.55f, 0.6f * A));
			Beam(From, To, W * 0.45f, new FLinearColor(1f, 1f, 0.95f, 0.95f * A));
			if (FxBolt != null)
			{
				float SegH = 150f;
				for (int k = 0; k * SegH < From.Y + SegH; ++k)
				{
					float Y = From.Y - (k + 0.5f) * SegH;
					int Seed = (S.Frame / 3) * 7 + k * 13;
					float Jit = FMath.Sin(Seed * 1.7f) * W * 0.25f;
					float Turn = (Seed & 1) == 0 ? 0f : 180f;
					Sprite(FxBolt, new FVector2f(From.X + Jit, Y), new Vector2(W * 1.6f, SegH * 1.05f), new FLinearColor(1f, 0.9f, 1f, 0.95f * A), EBlend.Additive, Turn);
					Sprite(FxBolt, new FVector2f(From.X - Jit * 1.6f, Y), new Vector2(W * 2.2f, SegH), new FLinearColor(1f, 0.5f, 0.4f, 0.6f * A), EBlend.Additive, 180f - Turn);
				}
			}
			Sprite(FxFlash ?? RingTex, S.BombCenter, V2D(W * 2.6f + 12f * FMath.Sin(S.Frame * 0.9f)), new FLinearColor(1f, 0.85f, 0.7f, 0.8f * A), EBlend.Additive);
			if (S.BombArrowPos.Y > -60f) ArtSprite(ArrowTex ?? ShotTalisman, S.BombArrowPos, 64f, LinColor.White, 0f);   // the arrow flying on
			break;
		}
		case ECharacter.Haru:
		{
			// mountain mandala: everything slows, the field turns green, leaves drift down
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0.3f, 0.9f, 0.4f, 0.10f * A));
			Sprite(FxMandala ?? RingTex, P.Pos, V2D(FieldH * 1.1f), new FLinearColor(0.55f, 1f, 0.6f, 0.45f * A), EBlend.Additive, S.Frame * 0.25f);
			Sprite(FxMandala ?? RingTex, P.Pos, V2D(DanmakuWorld.HaruSlowRadius * 3.4f), new FLinearColor(0.8f, 1f, 0.75f, 0.85f * A), EBlend.Additive, -S.Frame * 0.8f);
			for (int k = 0; k < 18; ++k)
			{
				float X = FMath.Fmod(k * 53.7f + FMath.Sin(S.Frame * 0.02f + k) * 30f + 400f, FieldW);
				float Y = FMath.Fmod(k * 97.3f + S.Frame * (0.8f + (k % 3) * 0.3f), FieldH + 40f) - 20f;
				Sprite(BulletMaple ?? BulletPetal ?? SoftTex, new FVector2f(X, Y), V2D(14f), new FLinearColor(0.6f, 1f, 0.55f, 0.8f * A), EBlend.Translucent, S.Frame * 2f + k * 40f);
			}
			break;
		}
		case ECharacter.Yuki:
		{
			// puppeteer's sigil under him, glowing threads to every puppet, wisteria petals falling
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0.75f, 0.55f, 1f, 0.09f * A));
			Sprite(FxSigil ?? RingTex, P.Pos, V2D(230f), new FLinearColor(0.9f, 0.7f, 1f, 0.9f * A), EBlend.Additive, S.Frame * 0.9f);
			Sprite(FxSigil ?? RingTex, P.Pos, V2D(120f), new FLinearColor(1f, 0.85f, 1f, 0.7f * A), EBlend.Additive, -S.Frame * 1.6f);
			for (int i = 0; i < P.DollsAlive; ++i)
			{
				Beam(P.Pos, P.Dolls[i], 2.2f, new FLinearColor(1f, 0.85f, 1f, 0.75f * A));
				Sprite(FxSigil ?? SoftTex, P.Dolls[i], V2D(54f), new FLinearColor(0.9f, 0.7f, 1f, 0.8f * A), EBlend.Additive, -S.Frame * 3f);
				Sprite(SoftTex, P.Dolls[i], V2D(50f), new FLinearColor(0.9f, 0.7f, 1f, 0.5f * A), EBlend.Additive);
			}
			for (int k = 0; k < 16; ++k)
			{
				float X = FMath.Fmod(k * 71.3f + 30f, FieldW);
				float Y = FMath.Fmod(k * 89.1f + S.Frame * (1.1f + (k % 4) * 0.25f), FieldH + 40f) - 20f;
				Sprite(BulletPetal ?? SoftTex, new FVector2f(X, Y), V2D(11f), new FLinearColor(0.85f, 0.65f, 1f, 0.85f * A), EBlend.Translucent, S.Frame * 3f + k * 30f);
			}
			break;
		}
		}
		DrawBombCutIn(S, Elapsed, C);
	}

	/** Touhou-style cut-in: the bomber's portrait slides in over the right of the field with the bomb's name. */
	void DrawBombCutIn(DanmakuWorld S, int Elapsed, FLinearColor C)
	{
		const int Len = 80;
		if (Elapsed >= Len) return;
		float Slide = 1f - FMath.Square(1f - FMath.Min(1f, Elapsed / 14f));          // ease out
		float Fade = FMath.Min(1f, (Len - Elapsed) / 20f);
		// coloured band behind the portrait
		FieldRect(FieldW * 0.52f, 0f, FieldW * 0.48f, FieldH, new FLinearColor(C.R * 0.4f, C.G * 0.4f, C.B * 0.5f, 0.22f * Slide * Fade));
		FieldRect(FieldW * 0.52f, 0f, 2.5f, FieldH, WithAlpha(C, 0.7f * Slide * Fade));
		Texture2D Por = PortraitFor((int)S.BombKind);
		if (Por != null)
		{
			// kept to the right half and see-through so the bullets stay readable
			float X = FMath.Lerp(FieldW + 120f, FieldW * 0.79f, Slide) - Elapsed * 0.1f;
			float Y = FieldH * 0.46f - Elapsed * 0.3f;
			ArtSprite(Por, new FVector2f(X, Y), FieldH * 0.72f, new FLinearColor(1f, 1f, 1f, 0.68f * Fade));
		}
		// the bomb's name, large, sliding in from the left along the bottom
		float NX = FMath.Lerp(-FieldW * 0.6f, 12f, Slide);
		Vector2 Pn = ToScreen(new FVector2f(NX - 4f, FieldH - 62f));
		InkPanel(Pn.X, Pn.Y, FieldW * 0.86f * Scale, 34f * Scale, 0.8f * Fade);
		FieldText(GetCharacterInfo(S.BombKind).BombName, NX + 8f, FieldH - 58f, WithAlpha(C, Fade), 1.3f, false, FieldW * 0.8f);
	}

	void DrawHitbox(DanmakuWorld S)
	{
		FDanmakuPlayer P = S.Player;
		if (S.State != EDanmakuState.Playing || !P.bFocus || P.DeathTimer > 0) return;
		Sprite(RingTex, P.Pos, V2D(52f), new FLinearColor(1f, 0.95f, 0.8f, 0.65f), EBlend.Additive, S.Frame * 3f);
		Sprite(DiscTex, P.Pos, V2D(9f), new FLinearColor(1f, 0.2f, 0.3f));
		Sprite(DiscTex, P.Pos, V2D(6f), LinColor.White);
	}

	void DrawPortraits(DanmakuWorld S)
	{
		const float Breath = 0f;   // portraits stay still; only the battle sprites animate
		void DrawPortrait(Texture2D Tex, float CenterX, float SlideX, float Alpha, float Dim, float Lift, float Grow, FHairZone[] Hair)
		{
			if (Tex == null) return;
			float H = 300f * Grow * (1f + 0.008f * Breath);
			var C = new FVector2f(CenterX + SlideX, FieldH - H * 0.5f + 4f - Lift + Breath * 1.5f);
			ArtSprite(Tex, C, H, new FLinearColor(Dim, Dim, Dim, Alpha));
		}

		int Char = (int)S.Player.Character;
		FHairZone[] PlayerHair = Char == (int)ECharacter.Sou ? SouPortraitHair : NoHair;

		if (S.IsInDialogue())
		{
			bool bBossTalks = S.GetDialogueSpeaker() >= 1;
			float Hop = S.DialogueTimer < 12 ? FMath.Sin(S.DialogueTimer / 12f * FMath.PI) * 8f : 0f;
			DrawPortrait(PortraitFor(Char), 95f, bBossTalks ? -12f : 0f, 1f, bBossTalks ? 0.45f : 1f, bBossTalks ? 0f : 6f + Hop, bBossTalks ? 0.97f : 1.02f, PlayerHair);
			int DArt = S.GetDialogueArt();
			Texture2D SpeakerArt = At(BossPortraits, DArt) ?? BossPortraitFor(S);
			DrawPortrait(SpeakerArt, FieldW - 95f, bBossTalks ? 0f : 12f, 1f, bBossTalks ? 1f : 0.45f, bBossTalks ? 6f + Hop : 0f, bBossTalks ? 1.02f : 0.97f, BossPortraitHair(S));

			Vector2 BoxPos = ToScreen(new FVector2f(10f, FieldH - 104f));
			InkPanel(BoxPos.X, BoxPos.Y, (FieldW - 20f) * Scale, 92f * Scale, 0.84f);
			string Name = S.GetDialogueName();
			FieldText(Name, 22f, FieldH - 99f, bBossTalks ? new FLinearColor(0.85f, 0.7f, 1f) : WithAlpha(GetCharacterInfo(S.Player.Character).Color, 1f), 1.05f, false, FieldW - 60f);
			const int LinesPerBox = 3;
			List<string> Lines = WrapPx(S.GetDialogueText(), (FieldW - 64f) * Scale, 0.95f);
			int Boxes = FMath.Max(1, (Lines.Count + LinesPerBox - 1) / LinesPerBox);
			S.DialoguePageCount = Boxes;
			int Page = FMath.Clamp(S.DialoguePage, 0, Boxes - 1);
			float Y = FieldH - 76f;
			for (int i = Page * LinesPerBox; i < FMath.Min(Lines.Count, (Page + 1) * LinesPerBox); ++i)
			{
				FieldText(Lines[i], 22f, Y, LinColor.White, 0.95f, false);
				Y += 19f;
			}
			bool bMore = Page + 1 < Boxes;
			if ((S.Frame / 20) % 2 == 0) FieldText(bMore ? "Z ▼" : "Z ▶", FieldW - 44f, FieldH - 32f, GoldText, 0.8f, false);
			return;
		}

		// spell card declaration: boss portrait sweeps across
		FBossState B = S.Boss;
		if (B.bActive && B.bSpell)
		{
			int T = B.GapTimer > 0 ? 60 - B.GapTimer : 60 + B.PhaseTime;
			if (S.IsQuadPhase())
			{
				// all four beasts sweep in together, side by side; the one leading this fight is drawn last, in front and a
				// little bigger, the other three a step back (smaller, slightly dimmer) so each stays mostly visible
				if (T < 110)
				{
					int Main = S.QuadMainBeast();
					float A = T < 20 ? T / 20f : (T > 80 ? (110 - T) / 30f : 1f);
					float Slide = (1f - FMath.Min(1f, T / 25f)) * 120f - T * 0.3f;
					for (int k = 0; k < 4; ++k)
					{
						int i = k == 3 ? Main : (k >= Main ? k + 1 : k);
						bool bMain = i == Main;
						DrawPortrait(At(BossPortraits, DanmakuWorld.BeastArt + i), 50f + i * 95f, Slide,
							A * 0.92f, bMain ? 1f : 0.82f, 0f, bMain ? 0.7f : 0.6f, NoHair);
					}
				}
			}
			else if (T < 110)
			{
				float A = T < 20 ? T / 20f : (T > 80 ? (110 - T) / 30f : 1f);
				DrawPortrait(BossPortraitFor(S), FieldW - 110f, (1f - FMath.Min(1f, T / 25f)) * 120f - T * 0.3f, A * 0.9f, 1f, 0f, 1f, BossPortraitHair(S));
			}
		}
	}

	void DrawBossUI(DanmakuWorld S)
	{
		FBossState B = S.Boss;
		if (!B.bActive || B.Phase < 0 || B.DefeatTimer > 0) return;

		FieldText(S.GetBossName(), 6f, 0f, new FLinearColor(0.92f, 0.8f, 1f), 0.85f, false, 44f);
		// remaining cards as stars, squeezed so they never run into the health bar
		int Left = S.GetBossPhasesRemaining();
		float StarGap = Left > 1 ? FMath.Min(10f, 54f / (Left - 1)) : 10f;
		for (int i = 0; i < Left; ++i)
		{
			Sprite(BulletStar ?? DiscTex, new FVector2f(54f + i * StarGap, 9f), V2D(10f), GoldText, EBlend.Additive);
		}

		const float BarX = 112f, BarW = FieldW - 152f;
		bool bSurvival = S.IsSurvivalPhase();
		float HPFrac = bSurvival ? 1f : FMath.Clamp(B.HP / B.MaxHP, 0f, 1f);   // survival: HP is locked
		FieldRect(BarX - 1f, 4f, BarW + 2f, 7f, WithAlpha(Gold, 0.9f));
		FieldRect(BarX, 5f, BarW, 5f, new FLinearColor(0.05f, 0.02f, 0.08f, 0.9f));
		FieldRect(BarX, 5f, BarW * HPFrac, 5f, bSurvival ? new FLinearColor(1f, 0.82f, 0.35f, 0.55f + 0.25f * FMath.Sin(S.Frame * 0.12f)) : (B.bSpell ? new FLinearColor(1f, 0.45f, 0.7f) : new FLinearColor(1f, 0.9f, 0.85f)));
		if (bSurvival) FieldText(Tr("無敵"), BarX + BarW * 0.5f, -1f, GoldText, 0.6f, true);
		else if (S.IsShielded()) FieldText(Tr("護盾"), BarX + BarW * 0.5f, -1f, new FLinearColor(0.6f, 0.85f, 1f), 0.6f, true);
		FieldRect(BarX, 5f, BarW * HPFrac, 1.5f, new FLinearColor(1f, 1f, 1f, 0.5f));

		int Secs = FMath.Max(0, B.TimeLeft / 60);
		FieldText($"{Secs:00}", FieldW - 20f, -1f, Secs < 10 ? new FLinearColor(1f, 0.4f, 0.4f) : LinColor.White, 0.95f, true);

		if (B.bSpell && !string.IsNullOrEmpty(B.SpellName))
		{
			Vector2 Pn = ToScreen(new FVector2f(FieldW * 0.24f, 15f));
			InkPanel(Pn.X, Pn.Y, FieldW * 0.74f * Scale, 34f * Scale, 0.7f);
			FieldText(B.SpellName, FieldW * 0.61f, 16f, new FLinearColor(1f, 0.95f, 1f), 0.92f, true, FieldW * 0.7f);
			string BonusText = B.bBonusValid ? Tr("獎勵") + $"　{S.GetCurrentSpellBonus()}" : Tr("收取失敗");
			if (S.IsBombImmunePhase() && !bSurvival) BonusText = Tr("最終符卡：Bomb 無效") + "　" + BonusText;
			FieldText(bSurvival ? Tr("耐久符卡　撐到時間結束！") + "　" + BonusText : BonusText, FieldW * 0.61f, 33f, B.bBonusValid ? GoldText : new FLinearColor(0.7f, 0.5f, 0.5f), 0.62f, true, FieldW * 0.7f);
		}
	}

	void DrawFrame(float SW, float SH)
	{
		float FW = FieldW * Scale, FH = FieldH * Scale;
		float[][] Rects = {
			new[] { 0f, 0f, Origin.X, SH },
			new[] { Origin.X + FW, 0f, SW - Origin.X - FW, SH },
			new[] { Origin.X, 0f, FW, Origin.Y },
			new[] { Origin.X, Origin.Y + FH, FW, SH - Origin.Y - FH },
		};
		foreach (var R in Rects)
		{
			if (UiPattern != null)
			{
				TiledRect(UiPattern, R[0], R[1], R[2], R[3], 300f * TextScale, new FLinearColor(0.8f, 0.8f, 0.85f, 1f));
				ScreenRect(R[0], R[1], R[2], R[3], new FLinearColor(0.02f, 0.01f, 0.06f, 0.35f));
			}
			else
			{
				ScreenRect(R[0], R[1], R[2], R[3], PanelColor);
			}
		}
		var Inner = new FLinearColor(0.45f, 0.3f, 0.75f, 1f);
		float T = FMath.Max(1f, 3f * TextScale);
		float G = FMath.Max(1f, 2f * TextScale);
		void Border(float Pad, float Th, FLinearColor C)
		{
			ScreenRect(Origin.X - Pad - Th, Origin.Y - Pad - Th, FW + 2 * (Pad + Th), Th, C);
			ScreenRect(Origin.X - Pad - Th, Origin.Y + FH + Pad, FW + 2 * (Pad + Th), Th, C);
			ScreenRect(Origin.X - Pad - Th, Origin.Y - Pad, Th, FH + 2 * Pad, C);
			ScreenRect(Origin.X + FW + Pad, Origin.Y - Pad, Th, FH + 2 * Pad, C);
		}
		Border(0f, G, Inner);
		Border(G + 1f, T, Gold);
	}

	void DrawSidebar(DanmakuWorld S, float SW, float SH)
	{
		float X0 = Origin.X + FieldW * Scale + 36f * TextScale;
		float Y = Origin.Y + 24f * TextScale;
		float Line = 34f * TextScale;
		float ValX = X0 + (GetLanguage() == LangFr ? 134f : IsLatin() ? 118f : 100f) * TextScale;

		InkPanel(X0 - 18f * TextScale, Y - 14f * TextScale, SW - X0 - 10f * TextScale, Line * 12.3f, 0.72f);

		void Row(string Label, string Value)
		{
			ScreenText(Label, X0, Y, LabelColor, 1.0f, false, ValX - X0 - 8f * TextScale);
			ScreenText(Value, ValX, Y, LinColor.White, 1.0f);
			Y += Line;
		}
		void IconRow(string Label, int Count, Texture2D Icon)
		{
			ScreenText(Label, X0, Y, LabelColor, 1.0f, false, ValX - X0 - 8f * TextScale);
			float IH = 26f * TextScale;
			float IX = ValX;
			for (int i = 0; i < FMath.Min(Count, DanmakuWorld.MaxLives); ++i)
			{
				if (Icon == null) break;
				float IW = FMath.Min(27f * TextScale, IH * (float)Icon.GetWidth() / (float)FMath.Max(1, Icon.GetHeight()));
				Canvas.TexRect(Icon, new Vector2(IX, Y + 2f * TextScale), new Vector2(IW, IH), LinColor.White, EBlend.Translucent);
				IX += IW + 4f * TextScale;
			}
			Y += Line;
		}

		Row("最高分", $"{S.HiScore:D10}");
		Row("分數", $"{S.Score:D10}");
		Y += Line * 0.4f;
		IconRow("殘機", S.Player.Lives, IconLife);
		// the pieces counters sit in a fixed column after room for six icons (the most you can hold), so they never move
		float PiecesX = ValX + (DanmakuWorld.MaxLives * 31f + 10f) * TextScale;
		ScreenText(Tr("碎片") + $" {S.Player.LifePieces}/{DanmakuWorld.LifePiecesPerLife}", PiecesX, Y - Line + 6f * TextScale, new FLinearColor(1f, 0.7f, 0.8f), 0.7f);
		IconRow("靈符", S.Player.Bombs, IconBomb);
		ScreenText(Tr("碎片") + $" {S.Player.BombPieces}/{S.BombPiecesNeeded()}", PiecesX, Y - Line + 6f * TextScale, new FLinearColor(0.7f, 1f, 0.75f), 0.7f);
		Y += Line * 0.4f;
		Row("靈力", Inv($"{S.Player.Power:F2} / 4.00"));
		Row("擦彈", $"{S.Graze}");
		Row("符卡", $"{S.SpellsCaptured} / {S.SpellsTotal}");

		// current character
		FCharacterInfo Info = GetCharacterInfo(S.Player.Character);
		Y += Line * 0.6f;
		ScreenText(Tr("自機") + "　" + Tr(Info.Name) + "（" + Tr(Info.Title) + "）", X0, Y, WithAlpha(Info.Color, 1f), 0.9f);
		Y += Line * 0.8f;
		FDifficultyInfo DI = GetDifficultyInfo(S.Difficulty);
		ScreenText(Tr("難度") + "　" + DI.Name + (IsChinese() ? "（" + Tr(DI.Label) + "）" : ""), X0, Y, WithAlpha(DI.Color, 1f), 0.9f);
		Y += Line * 0.8f;
		ScreenText(Tr("關卡") + "　" + S.GetStageTitle() + (S.bPractice ? Tr("（練習）") : ""), X0, Y, LabelColor, 0.9f);

		float LogoY = SH - 190f * TextScale;
		if (TitleArt != null)
		{
			float H = 120f * TextScale;
			float Aspect = (float)TitleArt.GetWidth() / (float)FMath.Max(1, TitleArt.GetHeight());
			Canvas.TexRect(TitleArt, new Vector2(X0, LogoY), new Vector2(H * Aspect, H), LinColor.White, EBlend.Translucent);
		}
		ScreenText(Inv($"{SmoothedFPS:F0} fps　") + Tr("彈數") + $" {S.Bullets.Count}", X0, SH - 40f * TextScale, new FLinearColor(0.55f, 0.55f, 0.65f), 0.65f);
	}

	void DrawOverlays(DanmakuWorld S)
	{
		const float CX = FieldW * 0.5f;

		if (S.MessageTimer > 0 && S.State == EDanmakuState.Playing)
		{
			int Elapsed = S.MessageDuration - S.MessageTimer;
			float A = FMath.Min(1f, FMath.Min(S.MessageTimer, Elapsed) / 20f);
			FieldText(S.Message, CX, 140f, new FLinearColor(1f, 0.92f, 0.75f, A), 1.7f, true, FieldW - 16f);
			if (!string.IsNullOrEmpty(S.SubMessage))
			{
				float SY = 178f;
				foreach (var L in WrapPx(S.SubMessage, (FieldW - 24f) * Scale, 1f))
				{
					FieldText(L, CX, SY, new FLinearColor(0.85f, 0.8f, 1f, A), 1.0f, true);
					SY += 22f;
				}
			}
		}

		switch (S.State)
		{
		case EDanmakuState.StageClear:
		{
			float A = FMath.Min(1f, S.StateTimer / 30f);
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0f, 0.02f, 0.06f, 0.5f * A));
			FieldText(S.GetStageTitle() + "　" + Tr("通過"), CX, 140f, new FLinearColor(0.75f, 1f, 0.85f, A), 1.9f, true);
			FieldText(Tr("關卡獎勵") + $"　{S.StageBonus}", CX, 200f, WithAlpha(GoldText, A), 1.1f, true);
			FieldText(Tr("分數") + $"　{S.Score}", CX, 230f, new FLinearColor(1f, 1f, 1f, A), 1.0f, true);
			if (S.StateTimer > 40 && (S.Frame / 20) % 2 == 0) FieldText("Z　前往下一關", CX, 280f, LabelColor, 0.95f, true);
			break;
		}
		case EDanmakuState.GameOver:
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0.05f, 0f, 0.02f, 0.6f));
			FieldText("遊戲結束", CX, 150f, new FLinearColor(1f, 0.45f, 0.5f), 2.0f, true);
			FieldText(Tr("分數") + $"　{S.Score}", CX, 210f, LinColor.White, 1.1f, true);
			if (S.StateTimer > 60)
			{
				FieldText("R　以同一自機重來", CX, 255f, LabelColor, 0.95f, true);
				FieldText(S.bSpellPractice ? "Z　返回符卡選擇" : "Z　返回選角", CX, 280f, LabelColor, 0.95f, true);
			}
			break;
		case EDanmakuState.Clear:
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0f, 0.02f, 0.06f, 0.55f));
			FieldText(S.bSpellPractice ? (S.SpellsCaptured > 0 ? "符卡收取！！" : "收取失敗……") : "通關！", CX, 140f, new FLinearColor(0.75f, 1f, 0.85f), 2.0f, true);
			FieldText(Tr("分數") + $"　{S.Score}", CX, 200f, LinColor.White, 1.1f, true);
			FieldText(Tr("符卡收取") + $"　{S.SpellsCaptured} / {S.SpellsTotal}", CX, 228f, GoldText, 1.0f, true);
			if (S.StateTimer > 60)
			{
				FieldText("R　再玩一次", CX, 270f, LabelColor, 0.95f, true);
				FieldText(S.bSpellPractice ? "Z　返回符卡選擇" : "Z　返回選角", CX, 295f, LabelColor, 0.95f, true);
			}
			break;
		default:
			break;
		}

		if (S.PopupTimer > 0 && S.State == EDanmakuState.Playing)
		{
			float A = FMath.Min(1f, S.PopupTimer / 20f);
			float Rise = (110 - S.PopupTimer) * 0.15f;
			FieldText(S.PopupText, CX, FieldH - 150f - Rise, WithAlpha(S.PopupColor, A), 1.05f, true, FieldW - 16f);
		}

		if (S.bPaused)
		{
			FieldRect(0f, 0f, FieldW, FieldH, new FLinearColor(0f, 0f, 0f, 0.55f));
			FieldText("暫停", CX, 150f, LinColor.White, 2.0f, true);
			string[] PauseItems = { "繼續", "重來", "回到主選單" };
			for (int i = 0; i < 3; ++i)
			{
				bool bSel = i == S.PauseCursor;
				float Y = 210f + i * 30f;
				if (bSel && MagicCircle != null) Sprite(MagicCircle, new FVector2f(CX - 70f, Y + 11f), V2D(22f), WithAlpha(GoldText, 0.8f), EBlend.Additive, S.Frame * 1.2f);
				FieldText(PauseItems[i], CX, Y, bSel ? GoldText : LabelColor, bSel ? 1.15f : 1.0f, true);
			}
			FieldText("↑　↓　選擇　　Z　決定　　P　繼續", CX, 320f, LabelColor, 0.75f, true, FieldW - 20f);
		}
	}
}
