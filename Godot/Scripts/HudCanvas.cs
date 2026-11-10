// Immediate-mode 2D drawing that mimics UE's UCanvas for the ported HUD.
// Every frame: Begin(), then draw calls in painter's order. Consecutive calls with the same blend mode and texture-repeat
// setting share one RenderingServer canvas item; a change of mode starts the next item, so the draw order is exactly
// the order of the calls (UE drew items in call order with a per-item blend mode).
// Colours: the UE code uses linear FLinearColor values; UE converted them to sRGB on output, so we do the same here.
using Godot;

namespace DanmakuSTG;

public enum EBlend { Translucent, Additive, Opaque }

public sealed class HudCanvas
{
	readonly Rid Parent;
	readonly List<Rid> Items = new();
	readonly Rid AddMaterial;
	int Cur = -1;
	EBlend CurBlend;
	bool CurRepeat;

	public HudCanvas(CanvasItem Owner)
	{
		Parent = Owner.GetCanvasItem();
		var M = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
		KeepAlive = M;
		AddMaterial = M.GetRid();
	}
	readonly Resource KeepAlive;

	/** Clears last frame's drawing. */
	public void Begin()
	{
		foreach (var I in Items) RenderingServer.CanvasItemClear(I);
		Cur = -1;
	}

	Rid Item(EBlend Blend, bool bRepeat)
	{
		if (Cur >= 0 && Blend == CurBlend && bRepeat == CurRepeat) return Items[Cur];
		++Cur;
		if (Cur >= Items.Count)
		{
			var N = RenderingServer.CanvasItemCreate();
			RenderingServer.CanvasItemSetParent(N, Parent);
			Items.Add(N);
		}
		var It = Items[Cur];
		RenderingServer.CanvasItemSetDrawIndex(It, Cur);
		RenderingServer.CanvasItemSetMaterial(It, Blend == EBlend.Additive ? AddMaterial : new Rid());
		RenderingServer.CanvasItemSetDefaultTextureRepeat(It, bRepeat ? RenderingServer.CanvasItemTextureRepeat.Enabled : RenderingServer.CanvasItemTextureRepeat.Disabled);
		CurBlend = Blend;
		CurRepeat = bRepeat;
		return It;
	}

	/** UE linear colour -> what Godot's sRGB canvas should be given. */
	public static Color Out(FLinearColor C)
	{
		var S = new Color(Mathf.Clamp(C.R, 0f, 1f), Mathf.Clamp(C.G, 0f, 1f), Mathf.Clamp(C.B, 0f, 1f), 1f).LinearToSrgb();
		return new Color(S.R, S.G, S.B, Mathf.Clamp(C.A, 0f, 1f));
	}

	/** Textured rectangle by top-left/size in pixels; UV0/UV1 in 0..1 of the texture (may go past 1 when bRepeat). */
	public void TexRect(Texture2D Tex, Vector2 TopLeft, Vector2 Size, FLinearColor Color, EBlend Blend = EBlend.Translucent,
		Vector2? UV0 = null, Vector2? UV1 = null, bool bRepeat = false)
	{
		if (Tex == null || Size.X == 0f || Size.Y == 0f) return;
		var It = Item(Blend, bRepeat);
		var TS = Tex.GetSize();
		Vector2 A = UV0 ?? Vector2.Zero, B = UV1 ?? Vector2.One;
		var Src = new Rect2(A * TS, (B - A) * TS);
		RenderingServer.CanvasItemAddTextureRectRegion(It, new Rect2(TopLeft, Size), Tex.GetRid(), Src, Out(Color));
	}

	/** Textured rectangle centred on Center, rotated by RotDeg (clockwise on screen, like UE's canvas yaw). */
	public void TexRectRotated(Texture2D Tex, Vector2 Center, Vector2 Size, FLinearColor Color, EBlend Blend, float RotDeg)
	{
		if (Tex == null) return;
		if (RotDeg == 0f) { TexRect(Tex, Center - Size * 0.5f, Size, Color, Blend); return; }
		var It = Item(Blend, false);
		RenderingServer.CanvasItemAddSetTransform(It, new Transform2D(Mathf.DegToRad(RotDeg), Center));
		RenderingServer.CanvasItemAddTextureRectRegion(It, new Rect2(-Size * 0.5f, Size), Tex.GetRid(), new Rect2(Vector2.Zero, Tex.GetSize()), Out(Color));
		RenderingServer.CanvasItemAddSetTransform(It, Transform2D.Identity);
	}

	/** Solid rectangle in pixels. */
	public void Rect(float X, float Y, float W, float H, FLinearColor Color, EBlend Blend = EBlend.Translucent)
	{
		if (W <= 0f || H <= 0f) return;
		RenderingServer.CanvasItemAddRect(Item(Blend, false), new Rect2(X, Y, W, H), Out(Color));
	}

	/** Straight line in pixels (UE FCanvasLineItem). */
	public void Line(Vector2 From, Vector2 To, float Thickness, FLinearColor Color, EBlend Blend = EBlend.Translucent)
	{
		RenderingServer.CanvasItemAddLine(Item(Blend, false), From, To, Out(Color), Thickness, true);
	}

	/** Textured triangles (pixel positions, 0..1 UVs), one colour for all vertices. */
	public void Triangles(Texture2D Tex, Vector2[] Points, Vector2[] UVs, int[] Indices, FLinearColor Color, EBlend Blend = EBlend.Translucent)
	{
		if (Tex == null || Points.Length == 0) return;
		var C = Out(Color);
		var Cols = new Color[Points.Length];
		for (int i = 0; i < Cols.Length; ++i) Cols[i] = C;
		RenderingServer.CanvasItemAddTriangleArray(Item(Blend, false), Indices, Points, Cols, UVs, null, null, Tex.GetRid());
	}

	/** Text with its top-left (or top-centre) at Pos, PixelSize = font size in pixels. Optional drop shadow like UE's. */
	public void Text(Font Fnt, string Str, Vector2 Pos, int PixelSize, FLinearColor Color, bool bCenter, bool bShadow = true)
	{
		if (Fnt == null || string.IsNullOrEmpty(Str) || PixelSize <= 0) return;
		var It = Item(EBlend.Translucent, false);
		float W = bCenter ? Fnt.GetStringSize(Str, HorizontalAlignment.Left, -1, PixelSize).X : 0f;
		var P = new Vector2(Pos.X - W * 0.5f, Pos.Y + Fnt.GetAscent(PixelSize));
		if (bShadow)
		{
			float Sh = Mathf.Max(1f, PixelSize / 18f);
			Fnt.DrawString(It, P + new Vector2(Sh, Sh), Str, HorizontalAlignment.Left, -1, PixelSize, new Color(0f, 0f, 0f, 0.85f * Mathf.Clamp(Color.A, 0f, 1f)));
		}
		Fnt.DrawString(It, P, Str, HorizontalAlignment.Left, -1, PixelSize, Out(Color));
	}

	public static float TextWidth(Font Fnt, string Str, int PixelSize) =>
		Fnt == null || string.IsNullOrEmpty(Str) ? 0f : Fnt.GetStringSize(Str, HorizontalAlignment.Left, -1, PixelSize).X;

	/** Procedural square texture: white, with alpha = Fn(U, V) for U, V in -1..1 (UE MakeTexture, non-premultiplied). */
	public static Texture2D MakeTexture(int Size, Func<float, float, float> AlphaFn)
	{
		var Img = Image.CreateEmpty(Size, Size, true, Image.Format.Rgba8);
		for (int Y = 0; Y < Size; ++Y)
		{
			for (int X = 0; X < Size; ++X)
			{
				float U = ((X + 0.5f) / Size) * 2f - 1f;
				float V = ((Y + 0.5f) / Size) * 2f - 1f;
				float A = Mathf.Clamp(AlphaFn(U, V), 0f, 1f);
				Img.SetPixel(X, Y, new Color(1f, 1f, 1f, A));
			}
		}
		Img.GenerateMipmaps();
		return ImageTexture.CreateFromImage(Img);
	}
}
