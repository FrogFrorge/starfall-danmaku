# Porting the UE C++ to Godot C# — rules for every file

The Godot project lives in `Godot/`. The C++ originals are in `Source/DanmakuSTG/`. The goal is a **faithful,
almost line-for-line translation**: same names, same numbers, same order of operations (including every
`Rng` call, so runs stay deterministic). Do not redesign, rebalance, rename or "improve" gameplay code.
Keep the original comments (translate nothing; Chinese strings stay exactly as they are).

## Already written (read these first, do not change them without a very good reason)
- `Scripts/Ue.cs` — compatibility layer: `FVector2f` = Godot `Vector2`, `FLinearColor` = Godot `Color`,
  `FMath.*` (same names/semantics as UE), `FRandomStream`, vector helpers (`Size()`, `SizeSquared()`,
  `GetSafeNormal()`, `IsNearlyZero()`), `V2.Distance/DistSquared/DotProduct/ZeroVector`, `LinColor.White/...`,
  `LinColor.MakeFromHSV8`, `c.WithA(a)`.
- `Scripts/Types.cs` — `Danmaku` static class (FieldW, FieldH, Dir, AngleOf, DistSq, Sq, Tr, TrStr, IsEnglish,
  GetLanguage, SetLanguage, BulletRadius, BulletVisualSize, GetCharacterInfo, GetDifficultyInfo) — all usable
  unqualified everywhere (`global using static`). Enums and the data classes `FBullet`, `FEnemy`, `FShot`,
  `FItem`, `FParticle`, `FBeamHazard`, `FDanmakuPlayer`, `FBossState`.
- `Scripts/World.cs` — `public partial class DanmakuWorld`: every field from `DanmakuWorld.h`, the inline
  header methods, `FPhaseDef`, `FBossDef`, `FDialogueLine`, `FSpellEntry`, `FPatternFn`, `FParse`, `Paths`.
- `Scripts/HudCanvas.cs`, `Scripts/Main.cs` (the Godot node; `World.Host` is it).

## Mechanical rules
| C++ | C# |
|---|---|
| `void ADanmakuWorld::Foo(int32 T)` in a .cpp | method `public void Foo(int T)` inside `public partial class DanmakuWorld` (file starts with `namespace DanmakuSTG;`) |
| `int32` / `int64` / `uint8` / `uint32` | `int` / `long` / `byte` / `uint` |
| `1.f`, `0.5f`, `2.f * X` | `1f`, `0.5f`, `2f * X` |
| `TEXT("…")`, `FString`, `const TCHAR*` | `"…"`, `string`, `string` |
| `FString::Printf(TEXT("%d/%d"), A, B)` | `$"{A}/{B}"` (match the format: `%02d` → `{A:00}`, `%.1f` → `{A:0.0}` with `CultureInfo.InvariantCulture` if a decimal point matters, `%g` → `{A}`), `%lld` → `{A}` |
| `FVector2f(x, y)` | `new FVector2f(x, y)` |
| `FVector2f::ZeroVector` | `FVector2f.Zero` |
| `FLinearColor(r, g, b[, a])` | `new FLinearColor(r, g, b[, a])` |
| `FLinearColor::White` etc. | `LinColor.White` etc. |
| `C.A = x` on a copy | `C = C.WithA(x)` (Godot Color is a struct: fields are `R G B A`, assignable on a local) |
| `FVector2f::Distance(a,b)` / `DistSquared` / `DotProduct` | `V2.Distance(a,b)` / `V2.DistSquared(a,b)` / `V2.DotProduct(a,b)` |
| `V.Size()` / `V.SizeSquared()` / `V.GetSafeNormal()` / `V.IsNearlyZero()` | same names (extension methods) |
| `FMath::Foo(...)` | `FMath.Foo(...)` (exists for everything used; add to Ue.cs only if truly missing) |
| `TArray<T>` | `List<T>`; `Num()`→`Count`, `Add`, `Reset()`/`Empty()`→`Clear()`, `IsValidIndex(i)`→`i >= 0 && i < L.Count`, `Last()`→`L[^1]`, `Insert(x, 0)`, `SetNum(n)`→`RemoveRange(n, Count - n)` (when shrinking), `RemoveAllSwap(pred)`→`RemoveAll(pred)`, `AddDefaulted_GetRef()`→`var X = new T(); L.Add(X);` |
| `FBullet& B = Fire(...)` / `FEnemy& E = ...` | `FBullet B = Fire(...)` — the data types are **classes**, a reference is the same object |
| a C++ **copy** of a struct (`FBullet Copy = B;` with no `&`) | `B.Clone()` (FBullet has Clone; add a Clone to other types if needed) |
| `for (FBullet& B : Bullets)` | `foreach (var B in Bullets)` (Fire() adds to `NewBullets`, so this is safe) |
| `static const T Arr[N] = {...}` inside a function | a `static readonly T[] X = {...}` field next to the method (unique name) or a local array |
| file-scope helpers in `namespace { }` | `private static` members of the partial class or a `static class` in the same file (unique names — files share one class) |
| `TOptional`, `TFunctionRef`, lambdas | C# lambdas / `Func<>` |
| `check(...)`, `ensure(...)` | drop or `System.Diagnostics.Debug.Assert` |
| `UE_LOG(LogTemp, Display, TEXT("…"), …)` | `GD.Print(…)` (add `using Godot;`) |
| `FFileHelper::SaveStringToFile(Text, *Path)` | `Paths.SaveString(Text, Path)` |
| `FPaths::ProjectSavedDir() / TEXT("X")` | `Paths.ProjectSavedDir() + "X"` |
| `FCommandLine::Get()` | `Host.GetCommandLine()` |
| `FParse::Param(Cmd, TEXT("X"))` / `FParse::Value(Cmd, TEXT("X="), V)` | `FParse.Param(Cmd, "X")` / `FParse.Value(Cmd, "X=", ref V)` |
| `FPlatformMisc::RequestExit(false)` | `Host.RequestExit()` |
| `FScreenshotRequest::RequestScreenshot(Path, false, false)` | `Host.RequestScreenshot(Path)` |
| enum `EBulletShape::Small` | `EBulletShape.Small`; `uint8(Shape)` → `(byte)Shape` |
| `ECharacter(i)` / `int32(C)` | `(ECharacter)i` / `(int)C` |
| member function pointers `&W::IF_Stomach` (pattern tables) | `static (w, t) => w.IF_Stomach(t)` (type `FPatternFn`) |
| `(this->*Fn)(T)` | `Fn(this, T)` |
| `Phases()[i].Name` (TCHAR*) | same, it is a `string` |
| integer division / modulo | identical in C# for ints (keep the int casts exactly as in C++) |
| `bool` from int (`if (N)`) | write the comparison (`if (N != 0)`) |
| default arguments | keep them as C# optional parameters |
| `const` locals | just locals |

File-local helpers (the C++ `namespace { ... }` blocks: colours like `ColSakura`, small free functions like `Rep`,
`Rainbow`, `Hash`): put them in a top-level `static class <FileName>Locals` in the same file (e.g. `Stage2Locals`,
`RoadsLocals`) and add `using static DanmakuSTG.<FileName>Locals;` at the top of that file, so they keep their
original short names inside that file without clashing with other files. Mutable file-scope globals (e.g. `GRoadDensity`)
go there too as `static` fields.

Watch out for:
- C++ `int / int` stays integer; `float / int` stays float — keep every literal's type as in C++ (`360.f * i / N` → `360f * i / N`).
- `%` on negative ints and floats behaves the same as C++ in C#.
- `FMath::RoundToInt` is floor(x + 0.5): use `FMath.RoundToInt`, never `Math.Round`.
- Names must not collide across files (all files are one partial class). If two .cpp files had a file-local
  helper or constant with the same name, prefix it with the file name (e.g. `S2_ColSun`).
- Godot `Vector2`/`Color` are structs: `B.Pos.X += 1` on a class field works; on a property of a struct it doesn't.
- `Dir`, `AngleOf`, `Sq`, `DistSq`, `FieldW`, `FieldH`, `Tr`, `GetDifficultyInfo`, ... are already global — do not redefine them.
- Do not create new files other than the ones you were assigned. Don't touch other agents' files.

## Building
`"C:\Program Files\dotnet\dotnet.exe" build DanmakuSTG.csproj`
Other files are being ported at the same time by other people, so the build will report errors about members that
don't exist yet (CS1061 / CS0103 for methods defined in someone else's file). **Only fix errors in your own files.**
Before finishing, make sure your files have no errors of their own (syntax, types, wrong API).
