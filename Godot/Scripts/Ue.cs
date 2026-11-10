// Compatibility layer so the gameplay code ported from Unreal reads almost line for line like the C++:
// FVector2f / FLinearColor are Godot's Vector2 / Color, FMath has the same names and semantics as UE's,
// and FRandomStream is UE's generator bit for bit (so seeds behave the same way).
global using FVector2f = Godot.Vector2;
global using FLinearColor = Godot.Color;
global using static DanmakuSTG.Danmaku;
global using System;
global using System.Collections.Generic;

namespace DanmakuSTG;

public static class FMath
{
	public const float PI = MathF.PI;
	public static float Sin(float x) => MathF.Sin(x);
	public static float Cos(float x) => MathF.Cos(x);
	public static float Tan(float x) => MathF.Tan(x);
	public static float Atan2(float y, float x) => MathF.Atan2(y, x);
	public static float Sqrt(float x) => MathF.Sqrt(x);
	public static float Exp(float x) => MathF.Exp(x);
	public static float Pow(float a, float b) => MathF.Pow(a, b);
	public static float Square(float x) => x * x;
	public static float Abs(float x) => MathF.Abs(x);
	public static int Abs(int x) => Math.Abs(x);
	public static float Sign(float x) => x > 0f ? 1f : (x < 0f ? -1f : 0f);
	public static int Sign(int x) => x > 0 ? 1 : (x < 0 ? -1 : 0);
	public static float Max(float a, float b) => a > b ? a : b;
	public static float Min(float a, float b) => a < b ? a : b;
	public static int Max(int a, int b) => a > b ? a : b;
	public static int Min(int a, int b) => a < b ? a : b;
	public static long Max(long a, long b) => a > b ? a : b;
	public static long Min(long a, long b) => a < b ? a : b;
	public static double Max(double a, double b) => a > b ? a : b;
	public static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
	public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
	public static float Lerp(float a, float b, float t) => a + (b - a) * t;
	public static double Lerp(double a, double b, double t) => a + (b - a) * t;
	public static FVector2f Lerp(FVector2f a, FVector2f b, float t) => a + (b - a) * t;
	public static FLinearColor Lerp(FLinearColor a, FLinearColor b, float t) =>
		new FLinearColor(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t, a.A + (b.A - a.A) * t);
	// UE: RoundToInt = FloorToInt(F + 0.5f)
	public static int RoundToInt(float f) => (int)MathF.Floor(f + 0.5f);
	public static int RoundToInt(double f) => (int)Math.Floor(f + 0.5);
	public static int FloorToInt(float f) => (int)MathF.Floor(f);
	public static int CeilToInt(float f) => (int)MathF.Ceiling(f);
	public static int TruncToInt(float f) => (int)f;
	public static float FloorToFloat(float f) => MathF.Floor(f);
	public static float Frac(float f) => f - MathF.Floor(f);
	public static float Fmod(float x, float y) => x % y;   // C fmodf: result has the sign of x
	public static float DegreesToRadians(float d) => d * (MathF.PI / 180f);
	public static float RadiansToDegrees(float r) => r * (180f / MathF.PI);
	public static float UnwindDegrees(float a)
	{
		while (a > 180f) a -= 360f;
		while (a < -180f) a += 360f;
		return a;
	}
	public static float FindDeltaAngleDegrees(float a1, float a2) => UnwindDegrees(a2 - a1);
	public static bool IsNearlyZero(float v, float tol = 1e-8f) => MathF.Abs(v) <= tol;
	public static bool IsNearlyEqual(float a, float b, float tol = 1e-8f) => MathF.Abs(a - b) <= tol;
	public static int DivideAndRoundUp(int a, int b) => (a + b - 1) / b;
}

/** UE's FRandomStream, same algorithm. A class (reference type): pass it around like UE's by-reference use. */
public sealed class FRandomStream
{
	int InitialSeed;
	uint Seed;
	public FRandomStream() { Initialize(0); }
	public FRandomStream(int seed) { Initialize(seed); }
	public void Initialize(int seed) { InitialSeed = seed; Seed = (uint)seed; }
	public void Reset() { Seed = (uint)InitialSeed; }
	public int GetCurrentSeed() => (int)Seed;
	void MutateSeed() { Seed = Seed * 196314165u + 907633515u; }
	public float GetFraction()
	{
		MutateSeed();
		uint bits = (BitConverter.SingleToUInt32Bits(1.0f) & 0xff800000u) | (Seed & 0x007fffffu);
		float result = BitConverter.UInt32BitsToSingle(bits);
		return result - MathF.Truncate(result);
	}
	public float FRand() => GetFraction();
	public int RandHelper(int a) => a > 0 ? Math.Min((int)(GetFraction() * a), a - 1) : 0;
	public int RandRange(int min, int max) => min + RandHelper(max - min + 1);
	public float FRandRange(float min, float max) => min + (max - min) * FRand();
}

/** UE vector helpers on Godot's Vector2. */
public static class VecExt
{
	public static float Size(this FVector2f v) => v.Length();
	public static float SizeSquared(this FVector2f v) => v.LengthSquared();
	public static bool IsNearlyZero(this FVector2f v, float tol = 1e-4f) => MathF.Abs(v.X) <= tol && MathF.Abs(v.Y) <= tol;
	public static bool IsZero(this FVector2f v) => v.X == 0f && v.Y == 0f;
	public static FVector2f GetSafeNormal(this FVector2f v)
	{
		float sq = v.X * v.X + v.Y * v.Y;
		if (sq < 1e-8f) return FVector2f.Zero;
		float inv = 1f / MathF.Sqrt(sq);
		return new FVector2f(v.X * inv, v.Y * inv);
	}
	/** In-place normalize like UE's (no-op for tiny vectors). */
	public static FVector2f Normalize(this ref FVector2f v) { v = v.GetSafeNormal(); return v; }
	public static FLinearColor WithA(this FLinearColor c, float a) => new FLinearColor(c.R, c.G, c.B, a);
}

public static class V2
{
	public static readonly FVector2f ZeroVector = FVector2f.Zero;
	public static float Distance(FVector2f a, FVector2f b) => a.DistanceTo(b);
	public static float DistSquared(FVector2f a, FVector2f b) => a.DistanceSquaredTo(b);
	public static float DotProduct(FVector2f a, FVector2f b) => a.X * b.X + a.Y * b.Y;
	public static float CrossProduct(FVector2f a, FVector2f b) => a.X * b.Y - a.Y * b.X;
}

public static class LinColor
{
	public static readonly FLinearColor White = new(1f, 1f, 1f, 1f);
	public static readonly FLinearColor Black = new(0f, 0f, 0f, 1f);
	public static readonly FLinearColor Red = new(1f, 0f, 0f, 1f);
	public static readonly FLinearColor Green = new(0f, 1f, 0f, 1f);
	public static readonly FLinearColor Blue = new(0f, 0f, 1f, 1f);
	public static readonly FLinearColor Yellow = new(1f, 1f, 0f, 1f);
	public static readonly FLinearColor Transparent = new(0f, 0f, 0f, 0f);
	/** UE FLinearColor::MakeFromHSV8: hue/sat/val 0..255; the result is linear (sRGB-decoded) like UE's. */
	public static FLinearColor MakeFromHSV8(byte h, byte s, byte v)
	{
		float H = h * 360f / 256f, S = s / 255f, V = v / 255f;
		float hDiv = H / 60f;
		int hi = (int)hDiv % 6;
		float f = hDiv - MathF.Floor(hDiv);
		float p = V * (1f - S), q = V * (1f - f * S), t = V * (1f - (1f - f) * S);
		float r, g, b;
		switch (hi)
		{
			case 0: r = V; g = t; b = p; break;
			case 1: r = q; g = V; b = p; break;
			case 2: r = p; g = V; b = t; break;
			case 3: r = p; g = q; b = V; break;
			case 4: r = t; g = p; b = V; break;
			default: r = V; g = p; b = q; break;
		}
		// UE builds an FColor (sRGB bytes) and converts it to linear
		return new FLinearColor(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), 1f);
	}
	static float SrgbToLinear(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
}
