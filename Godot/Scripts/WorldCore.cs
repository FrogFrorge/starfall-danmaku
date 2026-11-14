// Port of DanmakuWorld.cpp: flow, menus, player, enemies, boss flow, stage 1 patterns, bullets, items, recovery, beams.
using Godot;
using static DanmakuSTG.WorldCoreLocals;

namespace DanmakuSTG;

static class WorldCoreLocals
{
	public static readonly FLinearColor ColRed = new(1f, 0.18f, 0.22f);
	public static readonly FLinearColor ColPink = new(1f, 0.45f, 0.75f);
	public static readonly FLinearColor ColMagenta = new(0.95f, 0.25f, 1f);
	public static readonly FLinearColor ColPurple = new(0.6f, 0.3f, 1f);
	public static readonly FLinearColor ColBlue = new(0.25f, 0.45f, 1f);
	public static readonly FLinearColor ColCyan = new(0.25f, 0.9f, 1f);
	public static readonly FLinearColor ColLightBlue = new(0.55f, 0.8f, 1f);
	public static readonly FLinearColor ColGreen = new(0.35f, 1f, 0.45f);
	public static readonly FLinearColor ColYellow = new(1f, 0.9f, 0.25f);
	public static readonly FLinearColor ColOrange = new(1f, 0.55f, 0.15f);
	public static readonly FLinearColor ColWhite = new(0.95f, 0.95f, 1f);
	public static readonly FLinearColor ColMoon = new(0.82f, 0.9f, 1f);
	public static readonly FLinearColor ColGold = new(1f, 0.82f, 0.45f);

	public const float RadToDeg = 57.29578f;

	public static bool InField(FVector2f P, float Margin)
	{
		return P.X > -Margin && P.X < FieldW + Margin && P.Y > -Margin && P.Y < FieldH + Margin;
	}

	public static int BombLength(ECharacter C)
	{
		switch (C)
		{
		case ECharacter.Ren:  return 120;
		case ECharacter.Sou:  return 320;   // about 20 frames of arrow flight, then 300 frames of thunder
		case ECharacter.Haru: return 480;
		case ECharacter.Yuki: return 180;
		}
		return 150;
	}

	// BeginPhase: where the four beasts start before an all-four phase
	public static readonly FVector2f[] QuadCorners = { new(-40f, -40f), new(FieldW + 40f, -40f), new(-40f, 220f), new(FieldW + 40f, 220f) };
	public static readonly string[] BeastNames = { "饕餮", "檮杌", "混沌", "窮奇" };
	public static readonly string[] SpellStageNames = { "第一面", "第二面", "第三面", "第四面", "第五面", "第六面", "Extra", "Extra-IF" };
	public static readonly int[] SpellCharMidOrder = { 2, 1, 3, 0 };
	public static readonly float[] SplitTwoOffsets = { 31.5f, -31.5f };
	public static readonly long[] ExtendTable = { 10000000L, 30000000L, 60000000L, 100000000L, 150000000L };
}

public partial class DanmakuWorld
{
	// =====================================================================================
	// Boss / stage accessors (the tables live in DanmakuBosses.cpp)
	// =====================================================================================

	public static int GetNumStages()
	{
		int N = 0;
		while (N < ExtraStage && FindBossDef(N) != null) ++N;
		return N;
	}

	public static bool IsExtraAvailable() { return FindBossDef(ExtraStage) != null; }
	public static bool IsIfAvailable() { return FindBossDef(IfStage) != null; }

	public static string BeastName(int Beast)
	{
		return Tr(BeastNames[FMath.Clamp(Beast, 0, 3)]);
	}

	/** Out must hold at least 4 entries. */
	public int BossTargets(FVector2f[] Out)
	{
		if (IsQuadPhase())
		{
			for (int i = 0; i < 4; ++i) Out[i] = Boss.QuadPos[i];
			return 4;
		}
		Out[0] = Boss.Pos;
		return 1;
	}

	public int GetDialogueArt()
	{
		int Sp = GetDialogueSpeaker();
		return Sp >= 2 ? BeastArt + Sp - 2 : GetBossArtIndex();
	}

	public string GetDialogueName()
	{
		int Sp = GetDialogueSpeaker();
		if (Sp >= 2) return BeastName(Sp - 2);
		return Sp == 1 ? GetBossName() : Tr(GetCharacterInfo(Player.Character).Name);
	}

	public FBossDef BossDef()
	{
		FBossDef D = FindBossDef(StageIndex);
		return D != null ? D : FindBossDef(0);
	}

	public string GetBossName()
	{
		if (!bMidboss && Boss.Actor >= 0 && Boss.Actor < 4) return BeastName(Boss.Actor);
		return Tr(ActiveDef().Name);
	}
	public string GetBossTitle() { return Tr(ActiveDef().Title); }
	public int GetBossArtIndex()
	{
		if (!bMidboss && StageIndex == IfStage)
		{
			// the relay: whoever is fighting now (before the first card, the first beast)
			int A = Boss.Actor;
			if (A < 0 && Phases().Count > 0) A = Phases()[0].Actor;
			return BeastArt + FMath.Clamp(A, 0, 3);
		}
		return ActiveDef().ArtIndex >= 0 ? ActiveDef().ArtIndex : StageIndex;
	}
	public string GetStageTitle() { return Tr(BossDef().StageTitle); }

	public bool IsSurvivalPhase()
	{
		return Boss.bActive && Boss.Phase >= 0 && Boss.Phase < Phases().Count && Phases()[Boss.Phase].bSurvival;
	}

	public bool IsBombImmunePhase()
	{
		if (bMidboss) return false;   // road bosses' last cards can still be bombed
		return Boss.bActive && Boss.bSpell && Boss.Phase >= 0 && Boss.Phase < Phases().Count && NextPhaseFrom(Boss.Phase + 1) >= Phases().Count;
	}

	public int GetDialogueSpeaker()
	{
		List<FDialogueLine> D = CurrentDialogue();
		return IsInDialogue() && DialogueIndex >= 0 && DialogueIndex < D.Count ? D[DialogueIndex].Speaker : 0;
	}

	public string GetDialogueText()
	{
		List<FDialogueLine> D = CurrentDialogue();
		return IsInDialogue() && DialogueIndex >= 0 && DialogueIndex < D.Count ? Tr(D[DialogueIndex].Text) : "";
	}

	static List<FSpellEntry> SpellEntryList = new();

	public static List<FSpellEntry> SpellEntries()
	{
		List<FSpellEntry> Entries = SpellEntryList;
		if (Entries.Count > 0) return Entries;
		void AddDef(int Stage, bool bMid, FBossDef D)
		{
			if (D == null) return;
			for (int i = 0; i < D.Phases.Count; ++i) if (D.Phases[i].bSpell) Entries.Add(new FSpellEntry(Stage, bMid, D, i));
		}
		for (int S = 0; S <= IfStage; ++S)
		{
			FBossDef D = FindBossDef(S);
			if (D == null) continue;
			AddDef(S, true, D.Midboss);
			// Extra-IF midbosses in beast order: 饕餮 (晴), 檮杌 (蒼), 混沌 (雪), 窮奇 (蓮)
			foreach (int C in SpellCharMidOrder) AddDef(S, true, D.CharMidboss[C]);
			AddDef(S, false, D);
		}
		return Entries;
	}

	static bool IsValidSpellIndex(int Index) => Index >= 0 && Index < SpellEntries().Count;

	public int GetSpellEntryCount() { return SpellEntries().Count; }

	public string GetSpellEntryLabel(int Index)
	{
		if (!IsValidSpellIndex(Index)) return "";
		FSpellEntry E = SpellEntries()[Index];
		string Who = Tr(E.Def.Name);
		int Actor = E.Def.Phases[E.Phase].Actor;
		if (!E.bMid && Actor >= 0) Who = Actor < 4 ? BeastName(Actor) : Tr("四凶");
		return Tr(SpellStageNames[FMath.Clamp(E.Stage, 0, 7)]) + "　" + Who;
	}

	public string GetSpellEntryName(int Index)
	{
		return IsValidSpellIndex(Index) ? Tr(SpellEntries()[Index].Def.Phases[SpellEntries()[Index].Phase].Name) : "";
	}

	public int GetSpellEntryArt(int Index)
	{
		if (!IsValidSpellIndex(Index)) return 0;
		FSpellEntry E = SpellEntries()[Index];
		int Actor = E.Def.Phases[E.Phase].Actor;
		if (!E.bMid && Actor >= 0) return BeastArt + FMath.Clamp(Actor, 0, 3);
		return E.Def.ArtIndex >= 0 ? E.Def.ArtIndex : E.Stage;
	}

	public int GetBossPhaseCount() { return Phases().Count; }

	public int NextPhaseFrom(int Index)
	{
		List<FPhaseDef> P = Phases();
		while (Index < P.Count && P[Index].MinDifficulty > Difficulty) ++Index;
		return Index;
	}

	public int GetBossPhasesRemaining()
	{
		int N = 0;
		for (int i = Boss.Phase < 0 ? 0 : Boss.Phase + 1; i < Phases().Count; ++i) N += Phases()[i].MinDifficulty <= Difficulty ? 1 : 0;
		return N;
	}

	public long GetCurrentSpellBonus()
	{
		if (!Boss.bSpell) return 0;
		// survival cards can't be finished early, so their bonus doesn't shrink with time
		if (IsSurvivalPhase()) return 6000000;
		float Frac = FMath.Clamp((float)Boss.TimeLeft / (float)FMath.Max(1, Boss.TimeTotal), 0f, 1f);
		return ((long)(2000000 + 4000000 * Frac) / 10) * 10;
	}

	// =====================================================================================
	// Flow
	// =====================================================================================

	public void BeginPlay()
	{
		Rng.Initialize(20261106);

		string Cmd = Host.GetCommandLine();
		bDemo = FParse.Param(Cmd, "DanmakuDemo");
		bDemoHits = FParse.Param(Cmd, "DanmakuHitTest");
		bBot = bDemo && FParse.Param(Cmd, "DanmakuBot");
		if (bBot) bDemoHits = true;
		FParse.Value(Cmd, "DanmakuTurbo=", ref TurboSteps);
		FParse.Value(Cmd, "DanmakuBotTag=", ref BotTag);
		FParse.Value(Cmd, "DanmakuBotSkill=", ref BotSkill);
		bBotNoSpellBomb = FParse.Param(Cmd, "DanmakuBotNoSpellBomb");
		bDemoLocked = FParse.Param(Cmd, "DanmakuLocked");
		bDemoFocus = FParse.Param(Cmd, "DanmakuFocus");   // hold focus the whole time, to look at the focused shot
		if (FParse.Param(Cmd, "DanmakuListSpells"))
		{
			// -DanmakuListSpells: writes Saved/SpellList.txt (the -DanmakuSpell=N index of every card)
			var SB = new System.Text.StringBuilder();
			for (int i = 0; i < GetSpellEntryCount(); ++i) SB.Append(i).Append('\t').Append(GetSpellEntryLabel(i)).Append('\t').Append(GetSpellEntryName(i)).Append('\n');
			Paths.SaveString(SB.ToString(), Paths.ProjectSavedDir() + "SpellList.txt");
		}
		if (FParse.Param(Cmd, "DanmakuDumpText")) DumpText();
		bLangPicker = FParse.Param(Cmd, "DanmakuLangPicker");   // screenshot the language window
		bMeasure = bBot && FParse.Param(Cmd, "DanmakuMeasure");
		bSweep = bMeasure && FParse.Param(Cmd, "DanmakuSweep");
		bRoadMeasure = bMeasure && FParse.Param(Cmd, "DanmakuRoadMeasure");
		FParse.Value(Cmd, "DanmakuSkip=", ref DemoSkipFrames);
		FParse.Value(Cmd, "DanmakuPhase=", ref DemoStartPhase);
		FParse.Value(Cmd, "DanmakuChar=", ref DemoCharacter);
		FParse.Value(Cmd, "DanmakuDiff=", ref DemoDifficulty);
		FParse.Value(Cmd, "DanmakuBombAt=", ref DemoBombAt);
		FParse.Value(Cmd, "DanmakuShotEvery=", ref DemoShotEvery);
		FParse.Value(Cmd, "DanmakuPauseAt=", ref DemoPauseAt);
		FParse.Value(Cmd, "DanmakuShots=", ref DemoShotCount);
		FParse.Value(Cmd, "DanmakuStage=", ref DemoStage);
		FParse.Value(Cmd, "DanmakuHPScale=", ref DemoHPScale);
		string Lang = "";
		if (FParse.Value(Cmd, "DanmakuLang=", ref Lang)) SetLanguage(LanguageFromCode(Lang));
		DemoStartPhase = FMath.Max(0, DemoStartPhase);
		DemoCharacter = FMath.Clamp(DemoCharacter, 0, NumCharacters - 1);
		SelectedCharacter = DemoCharacter;
		Difficulty = FMath.Clamp(DemoDifficulty, 0, NumDifficulties - 1);

		if (bDemo)
		{
			int DemoSpell = -1;
			FParse.Value(Cmd, "DanmakuSpell=", ref DemoSpell);
			if (bLangPicker) { State = EDanmakuState.Title; StateTimer = 0; }
			else if (FParse.Param(Cmd, "DanmakuSelect")) { State = EDanmakuState.CharSelect; StateTimer = 0; }
			else if (FParse.Param(Cmd, "DanmakuMusicRoom")) { State = EDanmakuState.MusicRoom; StateTimer = 0; MusicCursor = FMath.Max(0, DemoSpell); MusicPlaying = MusicTracks[FMath.Clamp(MusicCursor, 0, MusicTracks.Length - 1)].Track; }
			else if (FParse.Value(Cmd, "DanmakuEnding=", ref EndingPage)) { State = EDanmakuState.EndingScene; StateTimer = 0; Player.Character = (ECharacter)DemoCharacter; }
			else if (FParse.Param(Cmd, "DanmakuOptions")) { State = EDanmakuState.Options; StateTimer = 0; OptionCursor = 1; }
			else if (FParse.Param(Cmd, "DanmakuSpellSelect")) { State = EDanmakuState.SpellSelect; StateTimer = 0; SpellCursor = FMath.Max(0, DemoSpell); }
			else if (bSweep && IsValidSpellIndex(DemoSpell))
			{
				SweepIndex = DemoSpell;
				BotStartSpell(SweepIndex);
			}
			else if (IsValidSpellIndex(DemoSpell))
			{
				// start one card of spell practice directly
				FSpellEntry E = SpellEntries()[DemoSpell];
				bSpellPractice = bPractice = true;
				bExtra = E.Stage >= ExtraStage;
				bIf = E.Stage >= IfStage;
				PracticeStage = E.Stage;
				PracticeDef = E.Def;
				PracticePhase = E.Phase;
				if (bExtra) Difficulty = NumDifficulties;
				StartGame((ECharacter)DemoCharacter);
			}
			else if (FParse.Param(Cmd, "DanmakuDiffSelect")) { State = EDanmakuState.DiffSelect; StateTimer = 0; }
			else if (!FParse.Param(Cmd, "DanmakuTitle"))
			{
				if (DemoStage >= 0)
				{
					bExtra = DemoStage >= ExtraStage;
					bIf = DemoStage >= IfStage;
					bPractice = !bExtra;
					PracticeStage = FMath.Clamp(DemoStage, 0, GetNumStages() - 1);
					if (bExtra) Difficulty = NumDifficulties;
				}
				StartGame((ECharacter)DemoCharacter);
			}
		}
	}

	public void Tick(float DeltaSeconds)
	{
		PollInput();

		if (TurboSteps > 0)
		{
			// headless test runs: as many fixed steps per tick as asked, no matter how long the tick was
			for (int i = 0; i < TurboSteps; ++i) Step();
			return;
		}

		Accumulator += FMath.Min(DeltaSeconds, 0.1f);
		int Steps = 0;
		while (Accumulator >= StepSeconds && Steps < 4)
		{
			Accumulator -= StepSeconds;
			Step();
			++Steps;
		}
		if (Steps == 4) Accumulator = 0f;
	}

	public void PollInput()
	{
		static bool Down(Key K) => Input.IsKeyPressed(K);
		static bool Pad(JoyButton B) => Input.IsJoyButtonPressed(0, B);

		float X = 0f, Y = 0f;
		if (Down(Key.Left)  || Down(Key.A) || Pad(JoyButton.DpadLeft))  X -= 1f;
		if (Down(Key.Right) || Down(Key.D) || Pad(JoyButton.DpadRight)) X += 1f;
		if (Down(Key.Up)    || Down(Key.W) || Pad(JoyButton.DpadUp))    Y -= 1f;
		if (Down(Key.Down)  || Down(Key.S) || Pad(JoyButton.DpadDown))  Y += 1f;
		float AX = Input.GetJoyAxis(0, JoyAxis.LeftX);
		float AY = Input.GetJoyAxis(0, JoyAxis.LeftY);   // Godot: positive is down (UE's LeftY is positive up)
		if (FMath.Abs(AX) > 0.35f) X += FMath.Sign(AX);
		if (FMath.Abs(AY) > 0.35f) Y += FMath.Sign(AY);

		FVector2f Move = new FVector2f(FMath.Clamp(X, -1f, 1f), FMath.Clamp(Y, -1f, 1f));
		if (!Move.IsNearlyZero()) Move = Move.GetSafeNormal();
		CurInput.Move = Move;
		CurInput.bFocus = Down(Key.Shift) || Pad(JoyButton.RightShoulder) || Pad(JoyButton.LeftShoulder);
		CurInput.bShoot = Down(Key.Z) || Down(Key.J) || Pad(JoyButton.A);

		bool bBomb = Down(Key.X) || Down(Key.K) || Pad(JoyButton.B);
		bool bStart = Down(Key.Z) || Down(Key.Enter) || Down(Key.Space) || Pad(JoyButton.A);
		bool bPause = Down(Key.P) || Pad(JoyButton.Start);
		bool bRestart = Down(Key.R) || Pad(JoyButton.Back);
		bool bLeft = X < -0.5f;
		bool bRight = X > 0.5f;
		bool bUp = Y < -0.5f;
		bool bDown = Y > 0.5f;

		bEdgeBomb |= bBomb && !bPrevBomb;
		bEdgeStart |= bStart && !bPrevStart;
		bEdgePause |= bPause && !bPrevPause;
		bEdgeRestart |= bRestart && !bPrevRestart;
		bEdgeLeft |= bLeft && !bPrevLeft;
		bEdgeRight |= bRight && !bPrevRight;
		bEdgeUp |= bUp && !bPrevUp;
		bEdgeDown |= bDown && !bPrevDown;
		bPrevBomb = bBomb; bPrevStart = bStart; bPrevPause = bPause; bPrevRestart = bRestart; bPrevLeft = bLeft; bPrevRight = bRight; bPrevUp = bUp; bPrevDown = bDown;
	}

	public void Step()
	{
		if (bEdgePause && State == EDanmakuState.Playing)
		{
			bPaused = !bPaused;
			PauseCursor = 0;
			Sfx(ESfx.Pause);
		}
		else if (bPaused)
		{
			// pause menu: resume / restart / back to the title (P and R still work as shortcuts)
			if (bEdgeUp)   { PauseCursor = (PauseCursor + 2) % 3; Sfx(ESfx.MenuMove); }
			if (bEdgeDown) { PauseCursor = (PauseCursor + 1) % 3; Sfx(ESfx.MenuMove); }
			if (bEdgeRestart || (bEdgeStart && PauseCursor == 1)) { Sfx(ESfx.MenuOk); StartGame(Player.Character); }
			else if (bEdgeStart && PauseCursor == 0) { Sfx(ESfx.MenuOk); bPaused = false; }
			else if (bEdgeStart && PauseCursor == 2) { Sfx(ESfx.MenuBack); ReturnToTitle(); }
			else if (bEdgeBomb) { Sfx(ESfx.MenuBack); bPaused = false; }
		}

		if (!bPaused)
		{
			++Frame;
			++StateTimer;
			switch (State)
			{
			case EDanmakuState.Title:
				if (bLangPicker)
				{
					// language window: English, 繁體中文, 日本語, Français
					int N = LanguageOrder.Length;
					if (bEdgeUp)   { LangPickCursor = (LangPickCursor + N - 1) % N; Sfx(ESfx.MenuMove); }
					if (bEdgeDown) { LangPickCursor = (LangPickCursor + 1) % N; Sfx(ESfx.MenuMove); }
					if (bEdgeStart) { SetLanguage(LanguageOrder[LangPickCursor]); bLangPicker = false; Sfx(ESfx.MenuOk); }
					else if (bEdgeBomb) { bLangPicker = false; Sfx(ESfx.MenuBack); }
					break;
				}
				if (bDemo && StateTimer > 150) bEdgeStart = true;
				if (bEdgeUp)   { TitleCursor = (TitleCursor + TitleItems - 1) % TitleItems; Sfx(ESfx.MenuMove); }
				if (bEdgeDown) { TitleCursor = (TitleCursor + 1) % TitleItems; Sfx(ESfx.MenuMove); }
				if (TitleCursor == 7 && ((bEdgeStart && StateTimer > 10) || bEdgeLeft || bEdgeRight))
				{
					bLangPicker = true;
					LangPickCursor = System.Math.Max(0, System.Array.IndexOf(LanguageOrder, GetLanguage()));
					Sfx(ESfx.MenuOk);
				}
				else if (bEdgeStart && StateTimer > 10 && TitleCursor == 5)
				{
					Sfx(ESfx.MenuOk);
					State = EDanmakuState.MusicRoom;
					MusicPlaying = -1;
					StateTimer = 0;
				}
				else if (bEdgeStart && StateTimer > 10 && TitleCursor == 6)
				{
					Sfx(ESfx.MenuOk);
					State = EDanmakuState.Options;
					OptionCursor = 0;
					StateTimer = 0;
				}
				else if (bEdgeStart && StateTimer > 10)
				{
					if ((TitleCursor == 1 && !IsExtraAvailable()) || (TitleCursor == 2 && !IsIfAvailable()))
					{
						Sfx(ESfx.MenuBack);
					}
					else
					{
						Sfx(ESfx.MenuOk);
						bExtra = TitleCursor == 1 || TitleCursor == 2;
						bIf = TitleCursor == 2;
						bPractice = false;
						bSpellPractice = false;
						if (bExtra) { Difficulty = NumDifficulties; State = EDanmakuState.CharSelect; }
						else if (TitleCursor == 3) State = EDanmakuState.StageSelect;
						else if (TitleCursor == 4) State = EDanmakuState.SpellSelect;
						else { if (Difficulty >= NumDifficulties) Difficulty = 1; State = EDanmakuState.DiffSelect; }
						StateTimer = 0;
					}
				}
				break;
			case EDanmakuState.EndingScene:
			{
				int Pages = Story.Endings[FMath.Clamp((int)Player.Character, 0, 3)].Pages.Length + 1;
				if ((bEdgeStart && StateTimer > 20) || (bDemo && StateTimer > 150))
				{
					Sfx(ESfx.MenuMove);
					StateTimer = 0;
					if (++EndingPage >= Pages) State = EDanmakuState.Clear;
				}
				break;
			}
			case EDanmakuState.MusicRoom:
			{
				int N = MusicTracks.Length;
				if (bEdgeUp)   { MusicCursor = (MusicCursor + N - 1) % N; MusicConfirm = -1; Sfx(ESfx.MenuMove); }
				if (bEdgeDown) { MusicCursor = (MusicCursor + 1) % N; MusicConfirm = -1; Sfx(ESfx.MenuMove); }
				if (bEdgeStart && StateTimer > 10)
				{
					int Trk = MusicTracks[MusicCursor].Track;
					if (IsMusicShown(Trk)) MusicPlaying = Trk;
					else if (MusicConfirm != Trk) { MusicConfirm = Trk; Sfx(ESfx.MenuOk); }   // first Z: the spoiler warning
					else { MusicRevealed.Add(Trk); MusicConfirm = -1; MusicPlaying = Trk; }   // second Z: reveal and play
				}
				else if (bEdgeBomb)
				{
					Sfx(ESfx.MenuBack);
					if (MusicConfirm >= 0) MusicConfirm = -1;   // X backs out of the warning first
					else { MusicPlaying = -1; MusicRevealed.Clear(); State = EDanmakuState.Title; StateTimer = 0; }
				}
				break;
			}
			case EDanmakuState.Options:
				if (bEdgeUp)   { OptionCursor = (OptionCursor + 2) % 3; Sfx(ESfx.MenuMove); }
				if (bEdgeDown) { OptionCursor = (OptionCursor + 1) % 3; Sfx(ESfx.MenuMove); }
				if (OptionCursor < 2 && (bEdgeLeft || bEdgeRight))
				{
					int D = bEdgeRight ? 1 : -1;
					if (OptionCursor == 0) SetMusicVolume(GetMusicVolume() + D);
					else SetSfxVolume(GetSfxVolume() + D);
					Sfx(ESfx.MenuMove);   // plays at the new sound volume
				}
				if ((bEdgeStart && StateTimer > 10 && OptionCursor == 2) || bEdgeBomb) { Sfx(ESfx.MenuBack); State = EDanmakuState.Title; StateTimer = 0; }
				break;
			case EDanmakuState.StageSelect:
				if (bEdgeUp)   { PracticeStage = (PracticeStage + GetNumStages() - 1) % GetNumStages(); Sfx(ESfx.MenuMove); }
				if (bEdgeDown) { PracticeStage = (PracticeStage + 1) % GetNumStages(); Sfx(ESfx.MenuMove); }
				if (bEdgeStart && StateTimer > 10)
				{
					Sfx(ESfx.MenuOk);
					bPractice = true;
					if (Difficulty >= NumDifficulties) Difficulty = 1;
					State = EDanmakuState.DiffSelect;
					StateTimer = 0;
				}
				else if (bEdgeBomb) { Sfx(ESfx.MenuBack); State = EDanmakuState.Title; StateTimer = 0; }
				break;
			case EDanmakuState.SpellSelect:
			{
				int N = FMath.Max(1, GetSpellEntryCount());
				if (bEdgeUp)    { SpellCursor = (SpellCursor + N - 1) % N; Sfx(ESfx.MenuMove); }
				if (bEdgeDown)  { SpellCursor = (SpellCursor + 1) % N; Sfx(ESfx.MenuMove); }
				if (bEdgeLeft)  { SpellCursor = FMath.Max(0, SpellCursor - 10); Sfx(ESfx.MenuMove); }
				if (bEdgeRight) { SpellCursor = FMath.Min(N - 1, SpellCursor + 10); Sfx(ESfx.MenuMove); }
				if (bEdgeStart && StateTimer > 10 && IsValidSpellIndex(SpellCursor) && !IsSpellUnlocked(SpellCursor)) Sfx(ESfx.MenuBack);   // not met yet
				else if (bEdgeStart && StateTimer > 10 && IsValidSpellIndex(SpellCursor))
				{
					Sfx(ESfx.MenuOk);
					FSpellEntry E = SpellEntries()[SpellCursor];
					bSpellPractice = true;
					bPractice = true;
					bExtra = E.Stage >= ExtraStage;
					bIf = E.Stage >= IfStage;
					PracticeStage = E.Stage;
					PracticeDef = E.Def;
					PracticePhase = E.Phase;
					if (bExtra) { Difficulty = NumDifficulties; State = EDanmakuState.CharSelect; }
					else { if (Difficulty >= NumDifficulties) Difficulty = 1; State = EDanmakuState.DiffSelect; }
					StateTimer = 0;
				}
				else if (bEdgeBomb) { Sfx(ESfx.MenuBack); bSpellPractice = false; State = EDanmakuState.Title; StateTimer = 0; }
				break;
			}
			case EDanmakuState.DiffSelect:
				if (bDemo)
				{
					if (StateTimer % 40 == 0) bEdgeDown = true;
					if (StateTimer > 4 * 40 + 20 && Difficulty == DemoDifficulty) bEdgeStart = true;
				}
				if (bEdgeUp)   { Difficulty = (Difficulty + NumDifficulties - 1) % NumDifficulties; Sfx(ESfx.MenuMove); }
				if (bEdgeDown) { Difficulty = (Difficulty + 1) % NumDifficulties; Sfx(ESfx.MenuMove); }
				if (bEdgeStart && StateTimer > 10) { Sfx(ESfx.MenuOk); State = EDanmakuState.CharSelect; StateTimer = 0; }
				else if (bEdgeBomb) { Sfx(ESfx.MenuBack); State = bSpellPractice ? EDanmakuState.SpellSelect : (bPractice ? EDanmakuState.StageSelect : EDanmakuState.Title); StateTimer = 0; }
				break;
			case EDanmakuState.CharSelect:
				if (bDemo)
				{
					// attract mode: walk the cursor across the roster, then start
					if (StateTimer % 45 == 0) bEdgeRight = true;
					if (StateTimer > 4 * 45 + 20 && SelectedCharacter == DemoCharacter) bEdgeStart = true;
				}
				if (bEdgeLeft)  { SelectedCharacter = (SelectedCharacter + NumCharacters - 1) % NumCharacters; Sfx(ESfx.MenuMove); }
				if (bEdgeRight) { SelectedCharacter = (SelectedCharacter + 1) % NumCharacters; Sfx(ESfx.MenuMove); }
				if (bEdgeStart && StateTimer > 10) { Sfx(ESfx.MenuOk); StartGame((ECharacter)SelectedCharacter); }
				else if (bEdgeBomb)
				{
					Sfx(ESfx.MenuBack);
					State = bSpellPractice && bExtra ? EDanmakuState.SpellSelect : (bExtra ? EDanmakuState.Title : EDanmakuState.DiffSelect);
					StateTimer = 0;
				}
				break;
			case EDanmakuState.Playing:
				UpdatePlaying();
				break;
			case EDanmakuState.StageClear:
				UpdateParticles();
				if ((StateTimer > 40 && bEdgeStart) || StateTimer > 240) StartStage(StageIndex + 1);
				break;
			case EDanmakuState.GameOver:
			case EDanmakuState.Clear:
				UpdateParticles();
				if (StateTimer > 60 && bEdgeRestart) StartGame(Player.Character);
				else if (StateTimer > 60 && bEdgeStart) { State = bSpellPractice ? EDanmakuState.SpellSelect : EDanmakuState.CharSelect; StateTimer = 0; }
				break;
			}
		}

		if (bDemo)
		{
			if (bSweep && !bBotDone && State == EDanmakuState.Clear) BotSweepNext();
			else if (bBot && !bBotDone && (State == EDanmakuState.GameOver || State == EDanmakuState.Clear))
			{
				bBotDone = true;
				BotFinish();
				DemoQuitTimer = TurboSteps > 0 ? 2 : 480;
			}
			// -DanmakuPauseAt=N: open the pause menu at frame N (cursor on "back to the title"), for screenshots
			if (DemoPauseAt > 0 && Frame == DemoPauseAt && State == EDanmakuState.Playing && !bPaused) { bPaused = true; PauseCursor = 2; }
			if (DemoShotEvery > 0 && DemoShotsTaken < DemoShotCount && Frame > 0 && Frame % DemoShotEvery == 0)
			{
				string Path = Paths.ProjectSavedDir() + $"DemoShots/shot_{DemoShotsTaken:00}.png";
				Host.RequestScreenshot(Path);
				if (++DemoShotsTaken >= DemoShotCount) DemoQuitTimer = 30;
			}
			if (DemoQuitTimer > 0 && --DemoQuitTimer == 0)
			{
				Host.RequestExit();
			}
		}

		bEdgeBomb = bEdgeStart = bEdgePause = bEdgeRestart = bEdgeLeft = bEdgeRight = bEdgeUp = bEdgeDown = false;
	}

	/** Pause menu "back to the title": drop the run. */
	public void ReturnToTitle()
	{
		bPaused = false;
		bSpellPractice = bPractice = bExtra = bIf = false;
		bEnding = false;
		DialogueIndex = -1;
		Boss = new FBossState();
		Bullets.Clear(); NewBullets.Clear(); Shots.Clear(); Enemies.Clear(); Items.Clear(); Particles.Clear(); Beams.Clear();
		FogRadius = 0f;
		BombTime = 0;
		State = EDanmakuState.Title;
		StateTimer = 0;
	}

	public void StartGame(ECharacter Character)
	{
		bPaused = false;

		Player = new FDanmakuPlayer();
		Player.Character = Character;
		Player.Lives = GetDifficultyInfo(Difficulty).ExtraLives;
		BotLastLives = Player.Lives;
		Player.LifePieces = 0;
		Player.BombPieces = 0;
		NextExtend = 0;
		NextGrazeReward = GrazePerBombPiece;
		PopupTimer = 0;
		Player.Invuln = 60;
		// like Touhou: Extra, Extra-IF and spell practice start at full power
		if (bExtra || bIf || bSpellPractice) Player.Power = 4f;
		for (int i = 0; i < NumDolls; ++i) Player.Dolls[i] = Player.Pos + new FVector2f(0f, 15f + i * 12f);
		SelectedCharacter = (int)Character;

		Boss = new FBossState();
		DialogueIndex = -1;
		bEnding = false;
		Bullets.Clear();
		NewBullets.Clear();
		Shots.Clear();
		Enemies.Clear();
		Items.Clear();
		Particles.Clear();
		Score = 0;
		Graze = 0;
		SpellsCaptured = 0;
		SpellsTotal = 0;
		BombTime = 0;
		if (bSpellPractice)
		{
			StartStage(PracticeStage);
			SpellsTotal = 1;
			bMidboss = false;
			foreach (FSpellEntry E in SpellEntries()) if (E.Def == PracticeDef && E.Phase == PracticePhase) bMidboss = E.bMid;
			StartBoss();
			return;
		}
		StartStage(bExtra ? (bIf ? IfStage : ExtraStage) : (bPractice ? PracticeStage : 0));
	}

	public void StartStage(int Stage)
	{
		StageIndex = Stage;
		State = EDanmakuState.Playing;
		StateTimer = 0;
		StageFrame = bDemo ? DemoSkipFrames : 0;
		Boss = new FBossState();
		DialogueIndex = -1;
		bEnding = false;
		Bullets.Clear();
		NewBullets.Clear();
		Shots.Clear();
		Enemies.Clear();
		Items.Clear();
		Beams.Clear();
		FogRadius = 0f;
		BombTime = 0;
		bMidboss = false;
		bMidDone = false;
		FBossDef Mid = MidbossDef();
		if (Mid != null)
		{
			foreach (FPhaseDef P in Mid.Phases) if (P.bSpell && P.MinDifficulty <= Difficulty) ++SpellsTotal;
		}
		Player.Invuln = FMath.Max(Player.Invuln, 60);
		foreach (FPhaseDef P in Phases())
		{
			if (P.bSpell && P.MinDifficulty <= Difficulty) ++SpellsTotal;
		}
		ShowMessage(Tr(BossDef().StageTitle), Tr(BossDef().StageSub), 150);
	}

	public void ShowMessage(string Main, string Sub, int Frames)
	{
		Message = Main;
		SubMessage = Sub;
		MessageTimer = Frames;
		MessageDuration = FMath.Max(1, Frames);
	}

	public void UpdatePlaying()
	{
		if (MessageTimer > 0) --MessageTimer;
		if (bBot) UpdateBot();
		else if (bDemo) UpdateDemo();

		// Haru's bomb slows every enemy thing to half speed: they only advance on even frames.
		bool bEnemyStep = !(IsTimeSlowed() && (Frame % 2 == 1));
		if (bEnemyStep && !Boss.bStarted) ++StageFrame;

		UpdatePlayer();
		UpdateBomb();
		if (bEnemyStep) UpdateStage();
		UpdateBoss(bEnemyStep);
		if (bEnding) UpdateEnding();
		if (bEnemyStep) UpdateEnemies();
		UpdateShots();
		UpdateBullets(bEnemyStep);
		UpdateBeams(bEnemyStep);
		UpdateRecovery();
		UpdateItems();
		UpdateParticles();

		Bullets.AddRange(NewBullets);
		NewBullets.Clear();
		HiScore = FMath.Max(HiScore, Score);
	}

	public void UpdateDemo()
	{
		CurInput.bShoot = true;
		CurInput.bFocus = bDemoFocus || (Frame / 300) % 2 == 1;
		FVector2f Target = new FVector2f(FieldW * 0.5f + FMath.Sin(Frame * 0.013f) * 120f, 380f);
		FVector2f D = Target - Player.Pos;
		CurInput.Move = new FVector2f(FMath.Clamp(D.X * 0.2f, -1f, 1f), FMath.Clamp(D.Y * 0.2f, -1f, 1f));
		if (DemoBombAt > 0 && StateTimer == DemoBombAt) bEdgeBomb = true;
	}

	// =====================================================================================
	// Player
	// =====================================================================================

	public bool CanBeHit()
	{
		return (!bDemo || bDemoHits) && State == EDanmakuState.Playing && Player.Invuln == 0 && Player.DeathTimer == 0
			&& Player.RespawnTimer == 0 && (BombTime == 0 || BombKind == ECharacter.Haru);
	}

	// Per character (Ren, Sou, Haru, Yuki): the frame of the left / right animation where the lean is
	// fullest, and the one closest to standing straight (printed by Tools/anim/pack_user_sheets.py, then checked by eye:
	// 蓮's right sheet starts standing at frame 0 and turns fully by frame 8, which the script misread).
	static readonly int[] LeftPeak = { 7, 9, 4, 4 }, LeftMid = { 11, 0, 11, 11 };
	static readonly int[] RightPeak = { 8, 9, 1, 1 }, RightMid = { 0, 0, 5, 5 };
	public const int AnimIdleStep = 6, AnimLeanStep = 3;   // sim frames per sheet frame

	/** Lean animation: play into the lean from its middle frame, hold the fullest frame while the key
	 *  stays down, and only play the way back to the middle once the key is released. */
	static void StepPlayerAnim(FDanmakuPlayer P, int Want)
	{
		int C = (int)P.Character;
		if (Want != 0 && Want != P.AnimDir)
		{
			P.AnimDir = Want; P.AnimMode = 0; P.AnimTick = 0;
			P.AnimFrame = Want < 0 ? LeftMid[C] : RightMid[C];
			return;
		}
		if (P.AnimDir == 0) { P.AnimFrame = 0; return; }   // idle: Hud runs the loop from the frame counter
		int Peak = P.AnimDir < 0 ? LeftPeak[C] : RightPeak[C];
		int Mid = P.AnimDir < 0 ? LeftMid[C] : RightMid[C];
		if (Want != 0)
		{
			if (P.AnimMode == 2) P.AnimMode = 3;            // pressed again on the way back: lean back in
			if (P.AnimMode == 4) P.AnimMode = 0;
			if (P.AnimMode == 1) return;                    // keep holding the lean
		}
		else if (P.AnimMode != 2 && P.AnimMode != 4) { P.AnimMode = P.AnimMode == 0 ? 4 : 2; P.AnimTick = 0; }   // let go before the lean was full: unwind it
		if (++P.AnimTick < AnimLeanStep) return;
		P.AnimTick = 0;
		switch (P.AnimMode)
		{
		case 0: P.AnimFrame = (P.AnimFrame + 1) % 12; if (P.AnimFrame == Peak) P.AnimMode = 1; break;
		case 3: P.AnimFrame = (P.AnimFrame + 11) % 12; if (P.AnimFrame == Peak) P.AnimMode = 1; break;
		case 2:
			if (P.AnimFrame == Mid) { P.AnimDir = 0; P.AnimFrame = 0; break; }
			P.AnimFrame = (P.AnimFrame + 1) % 12;
			break;
		case 4:
			if (P.AnimFrame == Mid) { P.AnimDir = 0; P.AnimFrame = 0; break; }
			P.AnimFrame = (P.AnimFrame + 11) % 12;
			break;
		}
	}

	/** -DanmakuDumpText: Saved/AllText.tsv (every translatable string: Chinese, English) and Saved/Dialogue.tsv
	 *  (every conversation the game shows, in order: scene, route, speaker, Chinese, English). Used by Tools/loc. */
	static void DumpText()
	{
		static string Clean(string T) => (T ?? "").Replace("\t", " ").Replace("\n", " ");
		var All = new System.Text.StringBuilder();
		foreach (var P in AllTextPairs()) All.Append(Clean(P.Zh)).Append('\t').Append(Clean(P.En)).Append('\n');
		Paths.SaveString(All.ToString(), Paths.ProjectSavedDir() + "AllText.tsv");

		var D = new System.Text.StringBuilder();
		string[] Route = { "蓮", "蒼", "晴", "雪" };
		string[] Beasts = { "饕餮", "檮杌", "混沌", "窮奇" };
		void Talk(string Scene, string BossName, List<FDialogueLine>[] Lines)
		{
			for (int c = 0; c < 4; ++c)
				foreach (var L in Lines[c])
				{
					string Who = L.Speaker == 0 ? Route[c] : L.Speaker == 1 ? BossName : Beasts[FMath.Clamp(L.Speaker - 2, 0, 3)];
					D.Append(Scene).Append('\t').Append(Route[c]).Append('\t').Append(Who).Append('\t')
					 .Append(Clean(L.Text)).Append('\t').Append(Clean(Table().TryGetValue(L.Text, out var En) ? En : "")).Append('\n');
				}
		}
		string[] StageName = { "第一面", "第二面", "第三面", "第四面", "第五面", "第六面", "Extra", "Extra-IF" };
		for (int s = 0; s <= IfStage; ++s)
		{
			FBossDef B = FindBossDef(s);
			if (B == null) continue;
			if (B.Midboss != null) Talk(StageName[s] + " 道中 " + B.Midboss.Name, B.Midboss.Name, B.Midboss.Dialogue);
			for (int c = 0; c < 4; ++c)
			{
				FBossDef M = B.CharMidboss[c];
				if (M == null) continue;
				var Only = new List<FDialogueLine>[] { new(), new(), new(), new() };
				Only[c].AddRange(M.Dialogue[c]);
				Talk(StageName[s] + " 道中 " + M.Name, M.Name, Only);
			}
			Talk(StageName[s] + " 戰前 " + (s == IfStage ? "四凶" : B.Name), B.Name, B.Dialogue);
			Talk(StageName[s] + " 戰後 " + (s == IfStage ? "四凶" : B.Name), B.Name, B.Ending);
		}
		for (int c = 0; c < Story.Endings.Length && c < 4; ++c)
		{
			var E = Story.Endings[c];
			D.Append("結局").Append('\t').Append(Route[c]).Append('\t').Append("（標題）").Append('\t').Append(Clean(E.Title)).Append('\t').Append(Clean(E.TitleEn)).Append('\n');
			foreach (var Pg in E.Pages)
				foreach (var L in Pg.Lines)
					D.Append("結局").Append('\t').Append(Route[c]).Append('\t').Append("（旁白）").Append('\t').Append(Clean(L.Zh)).Append('\t').Append(Clean(L.En)).Append('\n');
		}
		Paths.SaveString(D.ToString(), Paths.ProjectSavedDir() + "Dialogue.tsv");
	}

	public void UpdatePlayer()
	{
		FDanmakuPlayer P = Player;
		P.bShooting = false;
		P.bLaserOn = false;

		if (P.DeathTimer > 0)
		{
			if (bEdgeBomb && P.Bombs > 0)
			{
				P.DeathTimer = 0;
				TriggerBomb();
			}
			else if (--P.DeathTimer == 0)
			{
				KillPlayer();
			}
			return;
		}

		FVector2f Prev = P.Pos;
		if (P.RespawnTimer > 0)
		{
			--P.RespawnTimer;
			P.Pos.Y = FMath.Lerp(FieldH - 48f, FieldH + 24f, P.RespawnTimer / 40f);
			if (P.Invuln > 0) --P.Invuln;
		}
		else
		{
			if (P.Invuln > 0) --P.Invuln;
			P.bFocus = CurInput.bFocus;
			FCharacterInfo CI = GetCharacterInfo(P.Character);
			float Speed = (P.bFocus ? CI.FocusSpeed : CI.Speed) * SpeedUnit;
			// 蒼 runs faster while his bow is lowered
			if (P.Character == ECharacter.Sou && !CurInput.bShoot) Speed *= 1.5f;
			P.Pos += CurInput.Move * Speed;
			P.Pos.X = FMath.Clamp(P.Pos.X, 8f, FieldW - 8f);
			P.Pos.Y = FMath.Clamp(P.Pos.Y, 16f, FieldH - 16f);
		}

		if (CurInput.Move.X < -0.3f)      P.Lean = FMath.Max(-12, P.Lean - 1);
		else if (CurInput.Move.X > 0.3f)  P.Lean = FMath.Min(12, P.Lean + 1);
		else                              P.Lean -= FMath.Sign(P.Lean) * FMath.Min(2, FMath.Abs(P.Lean));
		StepPlayerAnim(P, CurInput.Move.X < -0.3f ? -1 : CurInput.Move.X > 0.3f ? 1 : 0);

		P.Trail.Insert(0, P.Pos);
		if (P.Trail.Count > 80) P.Trail.RemoveRange(80, P.Trail.Count - 80);
		if (P.Character == ECharacter.Yuki)
		{
			UpdateDolls(P.Pos - Prev);
			// one puppet per power level: a new level brings a puppet out, spending power takes one away
			int Want = PowerLevel();
			while (P.DollsAlive < Want)
			{
				P.Dolls[P.DollsAlive] = P.Pos;
				++P.DollsAlive;
				AddParticle(P.Pos, FVector2f.Zero, 60f, new FLinearColor(0.9f, 0.7f, 1f), 18, true);
			}
			if (P.DollsAlive > Want) P.DollsAlive = Want;
		}

		if (P.RespawnTimer > 0) return;

		if (bEdgeBomb) TriggerBomb();

		if (P.ShotCooldown > 0) --P.ShotCooldown;
		if (CurInput.bShoot && !IsInDialogue())
		{
			P.bShooting = true;
			if (P.ShotCooldown == 0)
			{
				FirePlayerShots();
				// 蓮 fires every 3 frames while focused (half the talismans), every 6 otherwise; everyone else every 6
				P.ShotCooldown = P.Character == ECharacter.Ren && P.bFocus ? 3 : 6;
			}
		}
	}

	public void UpdateDolls(FVector2f Delta)
	{
		FDanmakuPlayer P = Player;
		for (int i = 0; i < NumDolls; ++i)
		{
			if (P.bFocus)
			{
				// focus locks the formation: puppets move rigidly with the player
				P.Dolls[i] += Delta;
			}
			else
			{
				int Idx = FMath.Min(P.Trail.Count - 1, (i + 1) * 7);
				FVector2f Target = (Idx >= 0 ? P.Trail[Idx] : P.Pos) + new FVector2f(0f, 12f + i * 2f);
				P.Dolls[i] = FMath.Lerp(P.Dolls[i], Target, 0.25f);
			}
		}
	}

	public void AddShot(FVector2f Pos, float AngleDeg, float Speed, float Damage, EShotType Type)
	{
		var S = new FShot();
		Shots.Add(S);
		S.Pos = Pos;
		S.Vel = Dir(AngleDeg) * Speed;
		S.Damage = Damage;
		S.Type = Type;
	}

	public void FirePlayerShots()
	{
		Sfx(ESfx.Shot);
		FDanmakuPlayer P = Player;
		// Damage per second at power 1 / 2 / 3 / 4 (enemies and bosses alike):
		//   蓮 60 / 80 / 100 / 120 (focused: the same)   蒼 60 / 73 / 90 / 120
		//   晴 60 + 15 x 靈力 = 75 .. 120, counting the decimals (eighteen charms share it; the fan is wide)
		//   雪 40 / 60 / 80 / 120
		int Lv = PowerLevel();
		switch (P.Character)
		{
		case ECharacter.Ren:
		{
			if (P.bFocus)
			{
				// focused: a tight fan of half as many talismans (1 / 2 / 2 / 3) every 3 frames; at levels 1 and 3,
				// where half isn't whole, each talisman hits harder so the damage per second stays the same
				int N = (2 + Lv) / 2;
				float Dmg = Lv == 1 ? 3f : Lv == 3 ? 2.5f : 2f;
				for (int i = 0; i < N; ++i)
				{
					float C = i - (N - 1) * 0.5f;
					AddShot(P.Pos + new FVector2f(C * 3f, -10f), -90f + C * 2.5f, 15f, Dmg, EShotType.Talisman);
				}
				break;
			}
			// two straight talismans and one homing charm per power level, 2 damage each, every 6 frames
			AddShot(P.Pos + new FVector2f(-7f, -10f), -90f, 15f, 2f, EShotType.Talisman);
			AddShot(P.Pos + new FVector2f(7f, -10f), -90f, 15f, 2f, EShotType.Talisman);
			foreach (float A in RenHomingAngles[Lv - 1])
			{
				AddShot(P.Pos + new FVector2f(A * 0.3f, -6f), -90f + A, 11f, 2f, EShotType.Homing);
			}
			break;
		}
		case ECharacter.Haru:
		{
			// always eighteen charms: two straight up and eight pairs fanning out, every 6 frames. The volley is
			// worth 6 + 1.5 x 靈力, split evenly between them, so he does 60 + 15 x 靈力 a second wherever the fan
			// lands (75 at 1.00 靈力 up to 120 at 4.00) — and unlike everyone else that counts the decimals too.
			// Focused, the same eighteen charms close into half the angle.
			float Step = P.bFocus ? 2.5f : 5f;
			float Dmg = (6f + 1.5f * FMath.Clamp(P.Power, 1f, 4f)) / 18f;
			float Inward = 0f;
			if (P.bFocus)
			{
				// focused, the two halves slide 2.5 degrees inward every volley until they lie on top of each other
				// (11.25 degrees in, where each left charm meets a right one), then open back out: a slow sway
				int Phase = P.SprayToggle % 10;
				Inward = FMath.Min(11.25f, 2.5f * (Phase <= 5 ? Phase : 10 - Phase));
				P.SprayToggle = (P.SprayToggle + 1) % 10;
			}
			for (int s = -1; s <= 1; s += 2) AddShot(P.Pos + new FVector2f(7f * s, -10f), -90f, 15f, Dmg, EShotType.Straight);
			for (int k = 1; k <= 8; ++k)
			{
				for (int s = -1; s <= 1; s += 2)
				{
					AddShot(P.Pos + new FVector2f(6f * s, -8f), -90f + s * (k * Step - Inward), 14f, Dmg, EShotType.Straight);
				}
			}
			break;
		}
		case ECharacter.Yuki:
		{
			// one shot of his own and one per puppet (puppets = power level), 2 damage each;
			// at full power (4.00) the puppets hit for 2.8 and Yuki himself for 0.8
			bool bFull = P.Power >= 4f;
			AddShot(P.Pos + new FVector2f(0f, -10f), -90f, 15f, bFull ? 0.8f : 2f, EShotType.Straight);
			for (int i = 0; i < P.DollsAlive; ++i)
			{
				AddShot(P.Dolls[i] + new FVector2f(0f, -6f), -90f, 13f, YukiDollDamage(), EShotType.Doll);
			}
			break;
		}
		case ECharacter.Sou:
		{
			// one arrow every 6 frames, focused or not: 3 + power level damage; every (4 - power level) arrows the
			// next one pierces for 12 instead (at full power every arrow pierces)
			int Every = 4 - Lv;
			bool bPierce = Every <= 0 || P.ArrowCount >= Every;
			P.ArrowCount = bPierce ? 0 : P.ArrowCount + 1;
			AddShot(P.Pos + new FVector2f(0f, -14f), -90f, 18f, bPierce ? 12f : 3f + Lv, EShotType.Arrow);
			Shots[^1].bPierce = bPierce;
			break;
		}
		}
	}

	/** 蓮's homing charms by power level: ahead, then out to the sides a pair at a time. */
	static readonly float[][] RenHomingAngles =
	{
		new[] { 0f },
		new[] { -15f, 15f },
		new[] { 0f, -15f, 15f },
		new[] { -15f, 15f, -30f, 30f },
	};

	/** 雪's puppet shot: 2, or 2.8 at full power. His bomb's shots hit just as hard. */
	public float YukiDollDamage() => Player.Power >= 4f ? 2.8f : 2f;

	public void DamageBoss(float Amount)
	{
		if (!Boss.bActive || !Boss.bVulnerable || IsSurvivalPhase() || Boss.ShieldTimer > 0) return;
		Boss.HP -= Amount;
		Boss.HitFlash = 2;
	}

	public void TriggerBomb()
	{
		if (Player.Bombs <= 0 || BombTime > 0) return;
		--Player.Bombs;
		StartBomb();
	}

	public void StartBomb()
	{
		Sfx(ESfx.Bomb);
		BombKind = Player.Character;
		BombDuration = BombLength(BombKind);
		BombTime = BombDuration;
		BombCenter = Player.Pos;
		BombRadius = 0f;
		bBombHitBoss = false;
		++BombSerial;
		bBombArrowFlying = BombKind == ECharacter.Sou;
		BombArrowPos = Player.Pos + new FVector2f(0f, -16f);
		BombArrowAge = 0;
		// Haru's slow is not an invincibility bomb: only a short grace period
		Player.Invuln = FMath.Max(Player.Invuln, BombKind == ECharacter.Haru ? 90 : BombDuration + 60);
		if (Boss.bSpell) Boss.bBonusValid = false;
		if (IsBombImmunePhase() && BombKind != ECharacter.Haru) Popup("最終符卡：Bomb 傷害無效", new FLinearColor(1f, 0.6f, 0.6f));
		AddParticle(Player.Pos, FVector2f.Zero, 300f, GetCharacterInfo(BombKind).Color, 40, true);
	}

	public void UpdateBomb()
	{
		if (BombTime <= 0) return;
		--BombTime;
		float T = 1f - (float)BombTime / (float)BombDuration;
		if (BombKind != ECharacter.Sou) BombCenter = Player.Pos;   // 蒼's thunder stays where his arrow landed

		switch (BombKind)
		{
		case ECharacter.Ren:
		{
			// barrier shockwave: expanding ring that erases bullets and hits the boss once, hard
			BombRadius = FMath.Lerp(20f, 560f, FMath.Min(1f, T * 1.3f));
			float R2 = Sq(BombRadius);
			foreach (FBullet B in Bullets)
			{
				if (!B.bDead && !B.bBombProof && DistSq(B.Pos, BombCenter) < R2) { B.bDead = true; SpawnItem(EItemType.Star, B.Pos, true); }
			}
			foreach (FEnemy E in Enemies)
			{
				// 300 once to every fairy the ring reaches
				if (!E.bDead && E.BombHitSerial != BombSerial && DistSq(E.Pos, BombCenter) < R2) { E.HP -= 300f; E.HitFlash = 4; E.BombHitSerial = BombSerial; }
			}
			FVector2f[] Tg = new FVector2f[4];
			int NT = BossTargets(Tg);
			bool bInRange = false;
			for (int i = 0; i < NT; ++i) bInRange |= DistSq(Tg[i], BombCenter) < Sq(BombRadius + 28f);
			if (!bBombHitBoss && Boss.bActive && Boss.bVulnerable && bInRange)
			{
				if (!IsBombImmunePhase()) DamageBoss(300f);
				bBombHitBoss = true;
				Burst(Boss.Pos, 50, ColLightBlue, 6f, 14f, 40);
			}
			break;
		}
		case ECharacter.Sou:
		{
			// heaven-thunder: first the arrow flies up; it lands on the first enemy or the boss it meets, or after
			// about a third of a second. The thunder then strikes that column, top to bottom, and stays there.
			if (bBombArrowFlying)
			{
				BombArrowPos += new FVector2f(0f, -14f);
				bool bLand = ++BombArrowAge >= 20 || BombArrowPos.Y < 30f;
				foreach (FEnemy E in Enemies)
					if (!E.bDead && E.Pos.Y > -8f && DistSq(E.Pos, BombArrowPos) < Sq(E.Radius + 10f)) bLand = true;
				FVector2f[] At = new FVector2f[4];
				int NA = BossTargets(At);
				for (int i = 0; i < NA; ++i) bLand |= Boss.bActive && DistSq(At[i], BombArrowPos) < Sq(32f);
				if (bLand)
				{
					bBombArrowFlying = false;
					BombCenter = BombArrowPos;
					Sfx(ESfx.Thunder);
					Burst(BombCenter, 30, new FLinearColor(1f, 0.6f, 0.45f), 5f, 12f, 30);
				}
				break;
			}
			if (BombArrowPos.Y > -80f) BombArrowPos += new FVector2f(0f, -14f);   // the arrow flies on out of the field
			BombRadius = ThunderHalfWidth;
			float X = BombCenter.X;
			foreach (FBullet B in Bullets)
			{
				if (!B.bDead && !B.bBombProof && FMath.Abs(B.Pos.X - X) <= ThunderHalfWidth + B.Radius)
				{
					B.bDead = true;
					SpawnItem(EItemType.Star, B.Pos, true);
				}
			}
			foreach (FEnemy E in Enemies)
			{
				if (!E.bDead && FMath.Abs(E.Pos.X - X) <= ThunderHalfWidth + E.Radius) { E.HP -= 1f; E.HitFlash = 2; }
			}
			FVector2f[] Tg = new FVector2f[4];
			int NT = BossTargets(Tg);
			bool bInColumn = false;
			for (int i = 0; i < NT; ++i) bInColumn |= FMath.Abs(Tg[i].X - X) <= ThunderHalfWidth + 24f;
			if (Boss.bActive && !IsBombImmunePhase() && bInColumn)
			{
				DamageBoss(1f);
			}
			break;
		}
		case ECharacter.Haru:
			// time-slow barrier: handled through IsTimeSlowed()
			BombRadius = 60f;
			break;
		case ECharacter.Yuki:
			// puppet storm: every puppet sprays a fan of shots
			if (BombTime % 6 == 0)
			{
				for (int i = 0; i < FMath.Max(1, Player.DollsAlive); ++i)
				{
					FVector2f From = Player.DollsAlive > 0 ? Player.Dolls[i] : Player.Pos;
					int Count = Rng.RandRange(5, 8);
					for (int k = 0; k < Count; ++k)
					{
						AddShot(From, -90f + Rng.FRandRange(-22f, 22f), Rng.FRandRange(8f, 11f), YukiDollDamage(), EShotType.Doll);
						Shots[^1].bFromBomb = true;
					}
				}
			}
			break;
		}
	}

	public void PlayerHit()
	{
		if (!CanBeHit()) return;
		if (bMeasure)
		{
			// measuring difficulty: count the hit, then a short grace period, no death
			++MeasureHits;
			Player.Invuln = 30;
			return;
		}
		if (Player.Character == ECharacter.Yuki && BombTime == 0)
		{
			// 雪 bombs by reflex: it costs two bombs, and puppets pay for whatever bombs he lacks
			int FromBombs = FMath.Min(Player.Bombs, 2);
			int FromDolls = 2 - FromBombs;
			// a missing bomb is paid with a puppet, i.e. a whole power level; he always keeps at least one
			if (FromDolls <= PowerLevel() - 1)
			{
				Player.Bombs -= FromBombs;
				if (FromDolls > 0)
				{
					for (int i = 0; i < FromDolls; ++i) Burst(Player.Dolls[Player.DollsAlive - 1 - i], 20, new FLinearColor(0.9f, 0.7f, 1f), 3f, 10f, 30);
					Player.Power = FMath.Max(1f, Player.Power - FromDolls);
					Player.DollsAlive = PowerLevel();
				}
				if (Boss.bSpell) Boss.bBonusValid = false;
				StartBomb();
				Popup(FromDolls > 0 ? $"{Tr("自動 Bomb：式神代替")}　-{FromDolls}" : Tr("自動 Bomb"), new FLinearColor(0.9f, 0.75f, 1f));
				return;
			}
		}
		Player.DeathTimer = DeathBombWindow;
		Sfx(ESfx.PlayerDie);
		if (Boss.bSpell) Boss.bBonusValid = false;
		Burst(Player.Pos, 24, ColRed, 4f, 10f, 30);
	}

	public void KillPlayer()
	{
		--Player.Lives;
		Burst(Player.Pos, 60, ColPink, 5f, 12f, 50);
		AddParticle(Player.Pos, FVector2f.Zero, 200f, ColWhite, 30, true);

		SpawnItem(EItemType.BigPower, Player.Pos + new FVector2f(0f, -20f), false);
		for (int i = 0; i < 4; ++i)
		{
			SpawnItem(EItemType.Power, Player.Pos + new FVector2f(Rng.FRandRange(-40f, 40f), -30f), false);
		}
		Player.Power = FMath.Max(1f, Player.Power - 1f);
		CancelAllBullets(false, true);   // bomb-proof bullets also survive the death clear

		if (Player.Lives < 0)
		{
			Player.Lives = 0;
			State = EDanmakuState.GameOver;
			StateTimer = 0;
			return;
		}

		Player.Bombs = FMath.Max(Player.Bombs, 3);
		Player.Invuln = 200;
		Player.RespawnTimer = 40;
		Player.Pos = new FVector2f(FieldW * 0.5f, FieldH + 24f);
	}

	// =====================================================================================
	// Stage timeline & small enemies
	// =====================================================================================

	public FEnemy SpawnEnemy(FVector2f Pos, FVector2f Vel, int Kind, float HP, int Pattern,
		int StopAt = -1, int LeaveAt = -1, FVector2f LeaveVel = default, int DropPower = 1, int DropPoint = 0)
	{
		var E = new FEnemy();
		Enemies.Add(E);
		E.Pos = Pos;
		E.Vel = Vel;
		E.Kind = Kind;
		// toughness: stage 1 x1, stage 2 x1.1, stage 3 onward (Extra and Extra-IF too) x1.2 — later roads get harder through density, not HP
		E.HP = HP * (StageIndex == 0 ? 1f : (StageIndex == 1 ? 1.1f : 1.2f));
		E.Pattern = Pattern;
		E.StopAt = StopAt;
		E.LeaveAt = LeaveAt;
		E.LeaveVel = LeaveVel;
		E.DropPower = DropPower;
		E.DropPoint = DropPoint;
		switch (Kind)
		{
		case 1:  E.Radius = 12f; E.Color = new FLinearColor(1f, 0.35f, 0.35f); break;
		case 2:  E.Radius = 22f; E.Color = new FLinearColor(0.7f, 0.4f, 1f); break;
		default: E.Radius = 10f; E.Color = new FLinearColor(0.35f, 0.6f, 1f); break;
		}
		// a fox mask (spirit): fairies already on screen stay, but no new ones spawn while a fox mask is there
		if (Kind != 2)
		{
			for (int i = 0; i < Enemies.Count - 1; ++i)
			{
				if (!Enemies[i].bDead && Enemies[i].Kind == 2) { E.bDead = true; break; }
			}
		}
		return Enemies[^1];
	}

	public void UpdateEnemies()
	{
		// index loop (UE ranged-for): EnemyFire is someone else's code and may spawn enemies
		for (int Idx = 0, Num = Enemies.Count; Idx < Num; ++Idx)
		{
			FEnemy E = Enemies[Idx];
			if (E.bDead) continue;
			++E.Age;
			if (E.HitFlash > 0) --E.HitFlash;
			if (E.Age == E.StopAt) E.Vel = FVector2f.Zero;
			if (E.Age == E.LeaveAt) E.Vel = E.LeaveVel;
			E.Vel += E.Acc;
			E.Pos += E.Vel;
			if (E.SineAmp != 0f) E.Pos.X += FMath.Sin(E.Age * E.SineFreq + E.SinePhase) * E.SineAmp;

			if (E.HP <= 0f)
			{
				KillEnemy(E);
				continue;
			}
			if (E.Age > 30 && !InField(E.Pos, 48f))
			{
				E.bDead = true;
				continue;
			}
			if (InField(E.Pos, -8f))
			{
				EnemyFire(E);
			}
			if (CanBeHit() && DistSq(E.Pos, Player.Pos) < Sq(E.Radius * 0.6f + HitRadius()))
			{
				PlayerHit();
			}
		}
		Enemies.RemoveAll(E => E.bDead);
	}

	public void KillEnemy(FEnemy E)
	{
		E.bDead = true;
		Sfx(ESfx.EnemyDie);
		Score += E.Kind == 2 ? 30000 : (E.Kind == 1 ? 5000 : 1000);
		Burst(E.Pos, E.Kind == 2 ? 40 : 14, E.Color, 3f, 10f, 30);
		AddParticle(E.Pos, FVector2f.Zero, E.Radius * 5f, E.Color, 20, true);
		if (E.bRevenge && StageIndex < ExtraStage)
		{
			if (E.Kind == 0) Fan(E.Pos, 3, 30f, 1.6f, AimAngle(E.Pos), EBulletShape.Small, new FLinearColor(1f, 0.96f, 0.85f));
			else Ring(E.Pos, 8, 1.3f, AimAngle(E.Pos), EBulletShape.Small, new FLinearColor(1f, 0.96f, 0.85f));
		}
		else if (E.bRevenge)
		{
			int N = Cnt(E.Kind == 0 ? 6 : 12);
			float Base = Rng.FRandRange(0f, 360f);
			for (int i = 0; i < N; ++i) Fire(E.Pos, 1.25f, Base + 360f * i / N, EBulletShape.Small, new FLinearColor(1f, 0.96f, 0.85f));
		}
		if (E.Kind == 2) SpawnItem(EItemType.BombPiece, E.Pos, false);
		if (E.bCarry1Up) { SpawnItem(EItemType.OneUp, E.Pos, false); SpawnItem(EItemType.BigPower, E.Pos + new FVector2f(14f, 0f), false); }
		for (int i = 0; i < E.DropPower; ++i) SpawnItem(EItemType.Power, E.Pos + new FVector2f(Rng.FRandRange(-16f, 16f), Rng.FRandRange(-8f, 8f)), false);
		for (int i = 0; i < E.DropPoint; ++i) SpawnItem(EItemType.Point, E.Pos + new FVector2f(Rng.FRandRange(-16f, 16f), Rng.FRandRange(-8f, 8f)), false);
	}

	// =====================================================================================
	// Boss
	// =====================================================================================

	public void StartBoss()
	{
		CancelAllBullets(true);
		Boss = new FBossState();
		Boss.bActive = true;
		Boss.bStarted = true;
		Boss.Pos = new FVector2f(FieldW * 0.5f, -40f);
		MoveBossTo(new FVector2f(FieldW * 0.5f, 110f), 90);
		Boss.EntryTimer = 120;
		ShowMessage(Tr("警告"), GetBossTitle() + "　" + GetBossName(), 120);
	}

	public void StartMidboss()
	{
		bMidboss = true;
		StartBoss();
	}

	public void MoveBossTo(FVector2f Target, int Frames)
	{
		Boss.MoveFrom = Boss.Pos;
		Boss.MoveTo = Target;
		Boss.MoveTime = 0;
		Boss.MoveDur = FMath.Max(1, Frames);
	}

	public void WanderBoss()
	{
		float X = FMath.Clamp(Boss.Pos.X + Rng.FRandRange(-90f, 90f), 64f, FieldW - 64f);
		float Y = Rng.FRandRange(80f, 150f);
		MoveBossTo(new FVector2f(X, Y), 80);
	}

	public void PatrolBoss(int T)
	{
		if (bComposite) return;
		if (T % 170 == 60) WanderBoss();
	}

	public void PlaceBoss(FVector2f P)
	{
		// spell cards drive the boss along a curve; ease toward it so the first frame doesn't jump
		if (bComposite) return;
		Boss.MoveTime = Boss.MoveDur;
		Boss.Pos = FMath.Lerp(Boss.Pos, P, 0.12f);
	}

	public void UpdateBoss(bool bEnemyStep)
	{
		if (!Boss.bActive) return;
		if (Boss.HitFlash > 0) --Boss.HitFlash;
		if (Boss.DepartTimer > 0)
		{
			--Boss.DepartTimer;
			Boss.DepartPos += Boss.DepartVel;
		}
		if (!bEnemyStep)
		{
			Boss.Vel = FVector2f.Zero;
			return;
		}

		FVector2f PrevPos = Boss.Pos;
		if (Boss.MoveTime < Boss.MoveDur)
		{
			++Boss.MoveTime;
			float A = (float)Boss.MoveTime / Boss.MoveDur;
			float Ease = 1f - Sq(1f - A);
			Boss.Pos = FMath.Lerp(Boss.MoveFrom, Boss.MoveTo, Ease);
		}

		if (Boss.DefeatTimer > 0)
		{
			if (Frame % 4 == 0)
			{
				Burst(Boss.Pos + new FVector2f(Rng.FRandRange(-30f, 30f), Rng.FRandRange(-30f, 30f)), 8, ColGold, 3f, 14f, 30);
			}
			if (--Boss.DefeatTimer == 0)
			{
				Burst(Boss.Pos, 120, ColWhite, 7f, 16f, 70);
				AddParticle(Boss.Pos, FVector2f.Zero, 600f, ColLightBlue, 60, true);
				Boss.bActive = false;
				Sfx(ESfx.EnemyDie);
				Sfx(ESfx.Break);
				if (bSpellPractice)
				{
					State = EDanmakuState.Clear;
					StateTimer = 0;
					return;
				}
				if (bMidboss)
				{
					// beating the Extra / Extra-IF midboss drops a 1UP
					if (StageIndex >= ExtraStage) { SpawnItem(EItemType.OneUp, Boss.Pos, false); SpawnItem(EItemType.BigPower, Boss.Pos + new FVector2f(16f, 0f), false); }
					bMidboss = false;
					bMidDone = true;
					Boss = new FBossState();
					ShowMessage("……", StageIndex == IfStage ? Tr("凶獸逃向了天宮深處") : Tr("前方，就是天和閣"), 150);
					return;
				}
				if (BossDef().Ending[FMath.Clamp((int)Player.Character, 0, 3)].Count > 0)
				{
					// talk with the beaten boss before the stage ends
					bEnding = true;
					DialogueIndex = 0; DialoguePage = 0; DialoguePageCount = 1;
					DialogueTimer = 0;
					return;
				}
				FinishStage();
			}
			Boss.Vel = Boss.Pos - PrevPos;
			return;
		}

		if (Boss.EntryTimer > 0)
		{
			if (--Boss.EntryTimer == 0)
			{
				if (bSpellPractice) BeginPhase(FMath.Clamp(PracticePhase, 0, Phases().Count - 1));
				else if (bDemo && DemoStartPhase > 0) BeginPhase(FMath.Min(DemoStartPhase, Phases().Count - 1));
				else { DialogueIndex = 0; DialogueTimer = 0; DialoguePage = 0; DialoguePageCount = 1; }
			}
			Boss.Vel = Boss.Pos - PrevPos;
			return;
		}

		if (DialogueIndex >= 0)
		{
			++DialogueTimer;
			bool bAdvance = (DialogueTimer > 10 && bEdgeStart) || (bDemo && DialogueTimer > 90);
			if (bAdvance)
			{
				DialogueTimer = 0;
				Sfx(ESfx.MenuMove);
				if (NextDialogueBox() && DialogueIndex >= CurrentDialogue().Count)
				{
					DialogueIndex = -1;
					BeginPhase(NextPhaseFrom(0));
				}
			}
			Boss.Vel = Boss.Pos - PrevPos;
			return;
		}

		if (Boss.GapTimer > 0)
		{
			if (--Boss.GapTimer == 0)
			{
				Boss.bVulnerable = !Phases()[Boss.Phase].bSurvival;
				Boss.PhaseTime = 0;
				// every attackable spell card opens with 3 seconds of invulnerability
				if (Boss.bSpell && Boss.bVulnerable) Boss.ShieldTimer = 180;
			}
			Boss.Vel = Boss.Pos - PrevPos;
			return;
		}

		++Boss.PhaseTime;
		FPhaseDef Def = Phases()[Boss.Phase];
		// last spells: the clock only starts once the boss is down to a third of its health
		if (!Def.bTimerAtThird || Boss.HP <= Boss.MaxHP / 3f) --Boss.TimeLeft;
		if (Boss.ShieldTimer > 0) --Boss.ShieldTimer;
		Def.Pattern(this, Boss.PhaseTime);
		Boss.Vel = Boss.Pos - PrevPos;

		if (!Def.bSurvival)   // survival cards: the see-through boss can't hit you with its body
		{
			FVector2f[] Tg = new FVector2f[4];
			int NT = BossTargets(Tg);
			for (int i = 0; i < NT; ++i)
			{
				if (CanBeHit() && DistSq(Tg[i], Player.Pos) < Sq(20f + HitRadius())) PlayerHit();
			}
		}
		if (Boss.HP <= 0f || Boss.TimeLeft <= 0)
		{
			EndPhase(Boss.HP <= 0f || Def.bSurvival);   // surviving an endurance card counts as beating it
		}
	}

	public void FinishStage()
	{
		bool bMoreStages = !bPractice && !bExtra && StageIndex + 1 < GetNumStages();
		StageBonus = 2000000L * (StageIndex + 1) + (long)Player.Lives * 1000000 + (long)Player.Bombs * 500000 + (long)Graze * 1000;
		Score += StageBonus;
		HiScore = FMath.Max(HiScore, Score);
		State = bMoreStages ? EDanmakuState.StageClear : EDanmakuState.Clear;
		// the whole main game cleared: the character's ending plays before the results
		if (!bMoreStages && !bPractice && !bExtra && StageIndex == GetNumStages() - 1) { State = EDanmakuState.EndingScene; EndingPage = 0; }
		StateTimer = 0;
	}

	public bool NextDialogueBox()
	{
		if (DialoguePage + 1 < DialoguePageCount)
		{
			++DialoguePage;
			return false;
		}
		DialoguePage = 0;
		DialoguePageCount = 1;
		++DialogueIndex;
		return true;
	}

	public void UpdateEnding()
	{
		++DialogueTimer;
		bool bAdvance = (DialogueTimer > 10 && bEdgeStart) || (bDemo && DialogueTimer > 90);
		if (!bAdvance) return;
		DialogueTimer = 0;
		Sfx(ESfx.MenuMove);
		if (NextDialogueBox() && DialogueIndex >= CurrentDialogue().Count)
		{
			DialogueIndex = -1;
			bEnding = false;
			FinishStage();
		}
	}

	public void BeginPhase(int Index)
	{
		FPhaseDef Def = Phases()[Index];
		FogRadius = 0f;
		Boss.Phase = Index;
		Boss.HP = Boss.MaxHP = Def.HP * (bDemo ? DemoHPScale : 1f);
		Boss.TimeLeft = Boss.TimeTotal = Def.Seconds * 60;
		Boss.bSpell = Def.bSpell;
		Boss.SpellName = Tr(Def.Name);
		if (Def.bSpell) MarkSpellSeen(Def.Name);   // unlocks it in spell practice
		Boss.bBonusValid = true;
		Boss.GapTimer = 60;
		Boss.bVulnerable = false;
		Boss.PhaseTime = 0;
		Boss.PatternTimer = 0;
		Boss.Stage6Beasts = 0;
		Boss.ShieldTimer = 0;
		Array.Clear(Boss.JoinFrame);
		int NewActor = Def.Actor;
		if (NewActor != Boss.Actor && Boss.Actor >= 0)
		{
			// relay: the beast that just fought flies off to one side, the next one swoops in from the other
			if (Boss.Actor < 4)
			{
				Boss.DepartActor = Boss.Actor;
				Boss.DepartPos = Boss.Pos;
				Boss.DepartVel = new FVector2f(-Boss.EntrySide * 3.2f, -1.2f);
				Boss.DepartTimer = 90;
			}
			if (NewActor < 4) Boss.Pos = new FVector2f(Boss.EntrySide > 0 ? FieldW + 50f : -50f, 70f);
			else
			{
				for (int i = 0; i < 4; ++i) Boss.QuadPos[i] = QuadCorners[i];
				Boss.Pos = new FVector2f(FieldW * 0.5f, 110f);
			}
			Boss.EntrySide = -Boss.EntrySide;
			Boss.GapTimer = 90;
			ShowMessage(NewActor < 4 ? BeastName(NewActor) : Tr("四凶齊聚"), "", 90);
		}
		if (NewActor == 4 && Boss.Actor < 0)
		{
			for (int i = 0; i < 4; ++i) Boss.QuadPos[i] = QuadCorners[i];
		}
		Boss.Actor = NewActor;
		MoveBossTo(new FVector2f(FieldW * 0.5f, 110f), 50);
		if (Def.bSpell)
		{
			Sfx(ESfx.Spell);
			AddParticle(Boss.Pos, FVector2f.Zero, 400f, ColGold, 45, true);
		}
	}

	public void EndPhase(bool bDefeated)
	{
		FPhaseDef Def = Phases()[Boss.Phase];
		Sfx(ESfx.Break);
		FogRadius = 0f;
		CancelAllBullets(true);
		Burst(Boss.Pos, 40, ColWhite, 5f, 12f, 40);
		AddParticle(Boss.Pos, FVector2f.Zero, 300f, ColLightBlue, 30, true);

		if (Def.bSpell)
		{
			if (bDefeated && Boss.bBonusValid)
			{
				long Bonus = GetCurrentSpellBonus();
				Score += Bonus;
				++SpellsCaptured;
				Sfx(ESfx.SpellGet);
				ShowMessage(Tr("符卡收取！！"), Tr("獎勵") + $" {Bonus}", 150);
			}
			else
			{
				ShowMessage("收取失敗……", "", 120);
			}
		}

		if (Def.bSpell && bDefeated && Boss.bBonusValid) SpawnItem(EItemType.LifePiece, Boss.Pos, true);
		else if (!Def.bSpell && bDefeated) SpawnItem(EItemType.BombPiece, Boss.Pos, true);
		for (int i = 0; i < 4; ++i) SpawnItem(EItemType.Power, Boss.Pos + new FVector2f(Rng.FRandRange(-30f, 30f), Rng.FRandRange(-20f, 20f)), true);
		for (int i = 0; i < 6; ++i) SpawnItem(EItemType.Point, Boss.Pos + new FVector2f(Rng.FRandRange(-30f, 30f), Rng.FRandRange(-20f, 20f)), true);

		int Next = bSpellPractice ? Phases().Count : NextPhaseFrom(Boss.Phase + 1);
		if (Next < Phases().Count)
		{
			BeginPhase(Next);
		}
		else
		{
			Boss.bVulnerable = false;
			Boss.bSpell = false;
			Boss.SpellName = "";
			Boss.DefeatTimer = bSpellPractice ? 90 : 150;
		}
	}

	// ---------- Nonspells ----------

	public void Pattern_NonSpell1(int T)
	{
		if (Every(T, 12, 0)) Ring(Boss.Pos, 16, 2.4f, T * 1.7f, EBulletShape.Small, ColCyan);
		if (Every(T, 12, 6)) Ring(Boss.Pos, 16, 1.9f, -T * 1.3f, EBulletShape.Small, ColBlue);
		if (Every(T, 50, 25)) Fan(Boss.Pos, 7, 44f, 3.6f, AimAngle(Boss.Pos), EBulletShape.Medium, ColWhite);
		if (Every(T, 180, 90)) WanderBoss();
	}

	public void Pattern_NonSpell2(int T)
	{
		if (Every(T, 38, 1))
		{
			float Base = Rng.FRandRange(0f, 360f);
			int N = Cnt(28);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Boss.Pos, 4f, Base + i * 360f / N, EBulletShape.Medium, ColOrange);
				B.Accel = -0.08f;
				B.MinSpeed = 0f;
				B.Event = EBulletEvent.StopThenAim;
				B.EventFrame = 45;
				B.EventParam = 2.6f;
				B.EventParam2 = 15f + (i % 3) * 8f;
			}
		}
		if (T % 76 >= 50 && Every(T, 5, 0))
		{
			Fan(Boss.Pos, 7, 70f, 3.2f, AimAngle(Boss.Pos), EBulletShape.Rice, ColYellow);
		}
		if (Every(T, 150, 75)) WanderBoss();
	}

	// ---------- 日輪「赫耀天環」 : curving solar rings, slow halo, aimed flares ----------

	public void SunRings(int T, int Interval, int Rings, int BaseCount)
	{
		if (!Every(T, Interval)) return;
		float Sgn = (T / Interval) % 2 == 0 ? 1f : -1f;
		float Base = T * 0.032f * Sgn * RadToDeg;
		int NR = FMath.Max(1, FMath.RoundToInt(Rings * GetDifficultyInfo(Difficulty).CountMul));
		for (int Rg = 0; Rg < NR; ++Rg)
		{
			int Count = Cnt(BaseCount + Rg * 2);
			float Rad = 9f + Rg * 8f;
			for (int i = 0; i < Count; ++i)
			{
				float A = Base + 360f * i / Count + Rg * 8.6f;
				FLinearColor C = new FLinearColor(1f, 0.62f + Rg * 0.1f, 0.33f);
				FBullet B = Fire(Boss.Pos + Dir(A) * Rad, (2.6f + Rg * 0.3f) * 0.55f, A + Sgn * (20f - Rg * 2.9f), EBulletShape.Medium, C);
				B.AngVel = Sgn * (0.52f - Rg * 0.07f);
				B.AngVelDecay = 0.9985f;
				B.Accel = 0.0014f;
				B.MaxSpeed = 2.65f;
			}
		}
	}

	public void Pattern_Sun(int T)
	{
		PatrolBoss(T);
		SunRings(T, 18, 2, 8);

		if (Every(T, 90, 0))
		{
			int N = Cnt(24);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Boss.Pos, 1.1f, T * 0.57f + 360f * i / N, EBulletShape.Medium, new FLinearColor(1f, 0.85f, 0.5f));
				B.AngVel = -0.11f;
				B.AngVelDecay = 0.999f;
				B.Accel = 0.0008f;
				B.MaxSpeed = 1.75f;
			}
		}
		if (Every(T, 75, 45))
		{
			float Aim = AimAngle(Boss.Pos);
			int JN = Half(2);
			for (int j = -JN; j <= JN; ++j)
			{
				FBullet B = Fire(Boss.Pos, 3.0f, Aim + j * 6.9f, EBulletShape.Comet, new FLinearColor(1f, 0.5f, 0.35f));
				B.Accel = 0.0022f;
				B.MaxSpeed = 3.6f;
			}
		}
	}

	// ---------- 月蝕「月面反照」 : bouncing moon blades, orbiting moons ----------

	public void LunarSideWave(int T)
	{
		float Y = 90f + FMath.Sin(T * 0.05f) * 30f;
		int N = Cnt(4);
		for (int i = 0; i < N; ++i)
		{
			for (int Side = 0; Side < 2; ++Side)
			{
				float Sgn = Side == 0 ? 1f : -1f;
				FVector2f V = new FVector2f((3.2f + i * 0.2f) * 0.55f * Sgn, (0.9f + i * 0.16f) * 0.55f);
				FVector2f P = new FVector2f(Side == 0 ? -10f - i * 7f : FieldW + 10f + i * 7f, Y + i * 8f);
				FBullet B = Fire(P, V.Size(), AngleOf(V), EBulletShape.Crescent, ColMoon);
				B.AngVel = Sgn * 1.83f;
				B.Bounces = 1;
				B.Event = EBulletEvent.AimAtPlayer;
				B.EventFrame = 36;
				B.EventParam = 1.76f;
			}
		}
	}

	public void OrbitingMoons(int Count, float Radius, int OrbitTime)
	{
		Count = Cnt(Count);
		for (int i = 0; i < Count; ++i)
		{
			FBullet B = Fire(Boss.Pos, 0f, 0f, EBulletShape.Large, new FLinearColor(0.94f, 0.96f, 1f));
			B.bOrbit = true;
			B.OrbitAngle = 360f * i / Count;
			B.OrbitRadius = Radius;
			B.OrbitSpeed = (i % 2 == 0 ? 1f : -1f) * 2f;
			B.EventFrame = OrbitTime + i * 3;
			B.EventParam = 2.0f;
		}
	}

	public void Pattern_Moon(int T)
	{
		PatrolBoss(T);
		if (Every(T, 26, 0)) LunarSideWave(T);
		if (Every(T, 96, 0)) OrbitingMoons(8, 42f, 78);
		if (Every(T, 140, 70))
		{
			float Aim = AimAngle(Boss.Pos);
			int JN = Half(4);
			for (int i = -JN; i <= JN; ++i)
			{
				FBullet B = Fire(Boss.Pos, 1.4f, Aim + i * 10.3f, EBulletShape.Crescent, new FLinearColor(0.8f, 0.88f, 1f));
				B.AngVel = i < 0 ? -3.4f : 3.4f;
				B.Event = EBulletEvent.AimAtPlayer;
				B.EventFrame = 22;
				B.EventParam = 1.76f;
			}
		}
	}

	// ---------- 星雨「天河崩落」 : splitting star rain, constellation bursts, meteor sweeps ----------

	public void StarRain(int Num)
	{
		Num = Cnt(Num);
		for (int i = 0; i < Num; ++i)
		{
			FVector2f V = new FVector2f(Rng.FRandRange(-0.3f, 0.3f), Rng.FRandRange(2.0f, 2.75f));
			FBullet B = Fire(new FVector2f(Rng.FRandRange(14f, FieldW - 14f), -10f - i * 5f), V.Size(), AngleOf(V), EBulletShape.Star, new FLinearColor(0.7f, 0.84f, 1f));
			B.Event = EBulletEvent.SplitTwo;
			B.EventFrame = 24 + Rng.RandRange(0, 18);
		}
	}

	public void ConstellationBurst(int T)
	{
		float Aim = AimAngle(Boss.Pos);
		int JN = Half(5);
		for (int i = -JN; i <= JN; ++i)
		{
			FBullet B = Fire(Boss.Pos, 2.25f, Aim + i * 5.7f, EBulletShape.Star, new FLinearColor(0.9f, 0.96f, 1f));
			B.Accel = 0.0014f;
			B.MaxSpeed = 3f;
		}
		int NR = Cnt(8);
		for (int i = 0; i < NR; ++i)
		{
			FBullet B = Fire(Boss.Pos, 1.27f, T * 3.4f + 360f * i / NR, EBulletShape.Star, new FLinearColor(0.63f, 0.78f, 1f));
			B.Event = EBulletEvent.SplitTwo;
			B.EventFrame = 28;
		}
	}

	public void MeteorSweep(bool bFromLeft)
	{
		int N = Cnt(9);
		for (int i = 0; i < N; ++i)
		{
			FVector2f P = new FVector2f(bFromLeft ? -12f - i * 6f : FieldW + 12f + i * 6f, 65f + i * 21f);
			FVector2f V = new FVector2f(bFromLeft ? 3.0f : -3.0f, 0.88f + i * 0.044f);
			FBullet B = Fire(P, V.Size(), AngleOf(V), EBulletShape.Comet, new FLinearColor(1f, 0.82f, 0.67f));
			B.Accel = 0.0016f;
			B.MaxSpeed = 4f;
		}
	}

	public void Pattern_Star(int T)
	{
		PatrolBoss(T);
		if (Every(T, 9, 0)) StarRain(2);
		// the boss's own aimed constellation volley only on Hard and Lunatic; Easy / Normal just dodge the rain and meteors
		if (Difficulty >= 2 && Every(T, 48, 0)) ConstellationBurst(T);
		if (Every(T, 120, 50)) MeteorSweep(Rng.FRand() < 0.5f);
	}

	// ---------- 日月星奧義「終夜天象」 : sun + moon + stars together ----------

	public void Pattern_Final(int T)
	{
		PatrolBoss(T);
		// toned down for a first boss: every layer fires less often than it used to (sun 20 -> 28, stars 10 -> 16, moons 60 -> 90 ...)
		if (Every(T, 28, 0))
		{
			float Sgn = (T / 28) % 2 == 0 ? 1f : -1f;
			float Base = T * 0.03f * Sgn * RadToDeg;
			for (int Rg = 0; Rg < 2; ++Rg)
			{
				int Count = Cnt(8 + Rg * 3);
				for (int i = 0; i < Count; ++i)
				{
					float A = Base + 360f * i / Count;
					FBullet B = Fire(Boss.Pos + Dir(A) * (11f + Rg * 10f), (2.9f + Rg * 0.35f) * 0.55f, A + Sgn * 25.8f, EBulletShape.Medium, new FLinearColor(1f, 0.69f, 0.37f));
					B.AngVel = Sgn * (0.46f - Rg * 0.057f);
					B.AngVelDecay = 0.9988f;
					B.Accel = 0.0011f;
					B.MaxSpeed = 2.65f;
				}
			}
		}
		if (Every(T, 90, 0)) LunarSideWave(T);
		if (Every(T, 16, 0)) StarRain(1);
		if (Every(T, 200, 100))
		{
			int N = Cnt(18);
			for (int i = 0; i < N; ++i)
			{
				FBullet B = Fire(Boss.Pos, (1.8f + (i % 3) * 0.35f) * 0.55f, 360f * i / N + T * 0.69f, EBulletShape.Star,
					i % 2 == 0 ? new FLinearColor(0.96f, 0.96f, 1f) : ColGold);
				B.AngVel = (i % 2 == 0 ? -1f : 1f) * 0.29f;
				B.AngVelDecay = 0.9992f;
				B.Accel = 0.0008f;
				B.MaxSpeed = 2.3f;
			}
		}
		if (Every(T, 360, 180)) OrbitingMoons(8, 48f, 70);
		if (Every(T, 330, 40)) MeteorSweep(Rng.FRand() < 0.5f);
	}

	// =====================================================================================
	// Bullets
	// =====================================================================================

	public FBullet Fire(FVector2f Pos, float Speed, float Angle, EBulletShape Shape, FLinearColor Color)
	{
		Sfx(ESfx.EnemyShot);
		var B = new FBullet();
		NewBullets.Add(B);
		B.Pos = Pos;
		B.Speed = Speed;
		B.Angle = Angle;
		B.Shape = Shape;
		B.Radius = BulletRadius(Shape);
		B.Color = Color;
		return B;
	}

	public void Ring(FVector2f Pos, int Count, float Speed, float BaseAngle, EBulletShape Shape, FLinearColor Color, float Accel = 0f, float MaxSpeed = 1000f)
	{
		Count = Cnt(Count);
		for (int i = 0; i < Count; ++i)
		{
			FBullet B = Fire(Pos, Speed, BaseAngle + 360f * i / Count, Shape, Color);
			B.Accel = Accel;
			B.MaxSpeed = MaxSpeed;
		}
	}

	public void Fan(FVector2f Pos, int Count, float Spread, float Speed, float CenterAngle, EBulletShape Shape, FLinearColor Color)
	{
		if (Count > 1)
		{
			int H = Half((Count - 1) / 2);
			Spread *= Count > 1 ? (float)(H * 2) / (float)(Count - 1) : 1f;
			Count = H * 2 + (Count % 2 == 1 ? 1 : 2);
		}
		for (int i = 0; i < Count; ++i)
		{
			float A = Count == 1 ? CenterAngle : CenterAngle - Spread * 0.5f + Spread * i / (Count - 1);
			Fire(Pos, Speed, A, Shape, Color);
		}
	}

	public float AimAngle(FVector2f From)
	{
		return AngleOf(Player.Pos - From);
	}

	public bool Every(int T, int Interval, int Offset = 0)
	{
		float K = GetDifficultyInfo(Difficulty).IntervalScale * CompInterval;
		int N = FMath.Max(1, FMath.RoundToInt(Interval * K));
		return T % N == FMath.RoundToInt(Offset * K) % N;
	}

	public int Cnt(int N)
	{
		return FMath.Max(1, FMath.RoundToInt(N * GetDifficultyInfo(Difficulty).CountMul * CompCount));
	}

	public int Half(int N)
	{
		return FMath.Max(0, FMath.RoundToInt(N * GetDifficultyInfo(Difficulty).CountMul));
	}

	public void UpdateBullets(bool bMove)
	{
		bool bHittable = CanBeHit();
		bool bCanGraze = State == EDanmakuState.Playing && Player.DeathTimer == 0 && Player.RespawnTimer == 0;

		bool bHaruCalm = Player.Character == ECharacter.Haru && Player.RespawnTimer == 0 && State == EDanmakuState.Playing;
		// index loop (UE ranged-for): UpdateBehavior is someone else's code
		for (int Idx = 0, Num = Bullets.Count; Idx < Num; ++Idx)
		{
			FBullet B = Bullets[Idx];
			if (B.bDead) continue;
			FVector2f Before = B.Pos;
			if (bMove)
			{
				++B.Age;
				if (B.Behavior != 0 && !UpdateBehavior(B)) continue;
				if (B.Life > 0 && B.Age > B.Life)
				{
					B.bDead = true;
					AddParticle(B.Pos, FVector2f.Zero, 10f, B.Color, 12);
					continue;
				}
				if (B.bOrbit)
				{
					B.OrbitAngle += B.OrbitSpeed;
					B.Pos = Boss.Pos + Dir(B.OrbitAngle) * B.OrbitRadius;
					if (B.Age >= B.EventFrame)
					{
						B.bOrbit = false;
						B.Angle = AimAngle(B.Pos);
						B.Speed = B.EventParam;
					}
				}
				else if (B.Motion == 1)
				{
					// polar: orbits/expands around a centre that can itself drift
					B.Center += B.CenterVel;
					B.PolarRadius += B.PolarRadSpeed;
					B.PolarAngle += B.PolarAngSpeed;
					B.Pos = B.Center + Dir(B.PolarAngle) * B.PolarRadius;
					B.Angle = B.PolarAngle;
				}
				else if (B.Motion == 2)
				{
					// free velocity with gravity (arcs, falling fire, rain)
					B.Vel += B.Gravity;
					B.Pos += B.Vel * GetDifficultyInfo(Difficulty).SpeedMul;
					if (!B.Vel.IsNearlyZero()) B.Angle = AngleOf(B.Vel);
				}
				else
				{
					if (B.Event != EBulletEvent.None && B.Age == B.EventFrame)
					{
						HandleBulletEvent(B);
						if (B.bDead) continue;
					}
					B.Speed = FMath.Clamp(B.Speed + B.Accel, B.MinSpeed, B.MaxSpeed);
					B.Angle += B.AngVel;
					B.AngVel *= B.AngVelDecay;
					B.Pos += Dir(B.Angle) * (B.Speed * GetDifficultyInfo(Difficulty).SpeedMul);
				}

				if (B.WaveAmp != 0f)
				{
					B.Pos.X += FMath.Sin(B.Age * B.WaveFreq + B.WavePhase) * B.WaveAmp;
				}
				if (B.Bounces > 0 && !B.bOrbit && B.Motion != 1)
				{
					byte Mask = B.WallMask != 0 ? B.WallMask : (byte)1;
					FVector2f V = B.Motion == 2 ? B.Vel : Dir(B.Angle);
					bool bHit = false;
					if ((Mask & 1) != 0 && ((B.Pos.X < 4f && V.X < 0f) || (B.Pos.X > FieldW - 4f && V.X > 0f))) { V.X = -V.X; bHit = true; }
					if ((Mask & 2) != 0 && B.Pos.Y < 4f && V.Y < 0f) { V.Y = -V.Y; bHit = true; }
					if ((Mask & 4) != 0 && B.Pos.Y > FieldH - 4f && V.Y > 0f) { V.Y = -V.Y; bHit = true; }
					if (bHit)
					{
						if (B.Motion == 2) B.Vel = V;
						else { B.Angle = AngleOf(V); B.AngVel = -B.AngVel; }
						if (--B.Bounces == 0 && B.bDieAfterBounces)
						{
							B.bDead = true;
							AddParticle(B.Pos, FVector2f.Zero, 10f, B.Color, 12);
							continue;
						}
					}
				}
				// 晴's calm: bullets close to him move 20% slower
				if (bHaruCalm && !B.bOrbit && B.Motion != 1 && DistSq(Before, Player.Pos) < Sq(HaruSlowRadius))
				{
					B.Pos = Before + (B.Pos - Before) * 0.8f;
				}
				if (B.Pos.Y > B.KillBelowY)
				{
					B.bDead = true;
					continue;
				}

				if (B.bWrapX)
				{
					const float MX = 10f;
					if (B.Pos.X < -MX) B.Pos.X += FieldW + 2 * MX; else if (B.Pos.X > FieldW + MX) B.Pos.X -= FieldW + 2 * MX;
				}
				if (B.bWrap)
				{
					const float M = 16f;
					if (B.Pos.X < -M) B.Pos.X += FieldW + 2 * M; else if (B.Pos.X > FieldW + M) B.Pos.X -= FieldW + 2 * M;
					if (B.Pos.Y < -M) B.Pos.Y += FieldH + 2 * M; else if (B.Pos.Y > FieldH + M) B.Pos.Y -= FieldH + 2 * M;
				}
				else if (!B.bOrbit && B.Age > 30 && !InField(B.Pos, B.Behavior != 0 ? 140f : 56f))
				{
					B.bDead = true;
					continue;
				}
			}

			if (B.bHarmless) continue;
			float D2 = DistSq(B.Pos, Player.Pos);
			if (bCanGraze && !B.bGrazed && D2 < Sq(B.Radius + GrazeRadius))
			{
				B.bGrazed = true;
				++Graze;
				Sfx(ESfx.Graze);
				Score += 500;
				AddParticle(B.Pos, (B.Pos - Player.Pos).GetSafeNormal() * 2f, 6f, ColWhite, 12);
			}
			if (bHittable && D2 < Sq(B.Radius + HitRadius()))
			{
				// assassins and roaming chrysanthemums are "pieces", not shots: they stay after hitting you
				if (!(B.bBombProof && (B.Behavior == BehAssassin || B.Behavior == BehFinalChrys))) B.bDead = true;
				PlayerHit();
			}
		}
		Bullets.RemoveAll(B => B.bDead);
	}

	public void HandleBulletEvent(FBullet B)
	{
		switch (B.Event)
		{
		case EBulletEvent.AimAtPlayer:
			B.Angle = AimAngle(B.Pos);
			B.Speed = B.EventParam;
			B.Accel = 0f;
			B.AngVel = 0f;
			B.Event = EBulletEvent.None;
			break;
		case EBulletEvent.StopThenAim:
			B.Speed = 0f;
			B.Accel = 0f;
			B.Event = EBulletEvent.AimAtPlayer;
			B.EventFrame = B.Age + FMath.Max(1, FMath.RoundToInt(B.EventParam2));
			break;
		case EBulletEvent.SplitRing:
		{
			int N = FMath.Max(1, FMath.RoundToInt(B.EventParam));
			float Base = Rng.FRandRange(0f, 360f);
			Ring(B.Pos, N, 2.0f, Base, EBulletShape.Small, B.Color);
			Ring(B.Pos, N, 1.3f, Base + 180f / N, EBulletShape.Small, ColPink);
			AddParticle(B.Pos, FVector2f.Zero, 60f, B.Color, 16, true);
			B.bDead = true;
			break;
		}
		case EBulletEvent.SplitTwo:
			// a falling star sheds two small sparks at +-31 degrees and keeps going
			foreach (float Off in SplitTwoOffsets)
			{
				FBullet N = Fire(B.Pos, 1.43f, B.Angle + Off, EBulletShape.Small, new FLinearColor(0.78f, 0.88f, 1f));
				N.Accel = 0.0006f;
				N.MaxSpeed = 2.1f;
			}
			B.Event = EBulletEvent.None;
			break;
		default:
			break;
		}
	}

	public void CancelAllBullets(bool bToStars, bool bKeepProof = false)
	{
		void CancelArray(List<FBullet> Arr)
		{
			foreach (FBullet B in Arr)
			{
				if (B.bDead) continue;
				if (bKeepProof && B.bBombProof) continue;
				B.bDead = true;
				if (bToStars) SpawnItem(EItemType.Star, B.Pos, true);
				else if (Particles.Count < 2500) AddParticle(B.Pos, FVector2f.Zero, BulletVisualSize(B.Shape) * 1.5f, B.Color, 14);
			}
			Arr.RemoveAll(B => B.bDead);
		}
		CancelArray(Bullets);
		CancelArray(NewBullets);
		Beams.Clear();
	}

	public bool FindHomingTarget(FVector2f From, out FVector2f OutTarget)
	{
		OutTarget = FVector2f.Zero;
		if (Boss.bActive && Boss.bVulnerable)
		{
			OutTarget = Boss.Pos;
			return true;
		}
		float Best = float.MaxValue;
		foreach (FEnemy E in Enemies)
		{
			if (E.bDead || !InField(E.Pos, -4f)) continue;
			float D = DistSq(E.Pos, From);
			if (D < Best) { Best = D; OutTarget = E.Pos; }
		}
		return Best < float.MaxValue;
	}

	public void UpdateShots()
	{
		foreach (FShot S in Shots)
		{
			++S.Age;
			if (S.Type == EShotType.Homing)
			{
				if (FindHomingTarget(S.Pos, out FVector2f Target))
				{
					float Cur = AngleOf(S.Vel);
					float Want = AngleOf(Target - S.Pos);
					float Turn = FMath.Clamp(FMath.FindDeltaAngleDegrees(Cur, Want), -6f, 6f);
					S.Vel = Dir(Cur + Turn) * S.Vel.Size();
				}
			}
			S.Pos += S.Vel;
			if (S.Age > 180 || S.Pos.Y < -24f || S.Pos.Y > FieldH + 24f || S.Pos.X < -24f || S.Pos.X > FieldW + 24f)
			{
				S.bDead = true;
				continue;
			}
			foreach (FEnemy E in Enemies)
			{
				if (E.bDead || E.HP <= 0f || E.Pos.Y < -8f) continue;
				if (DistSq(S.Pos, E.Pos) < Sq(E.Radius + 6f))
				{
					if (S.bPierce)
					{
						S.Pierced ??= new List<FEnemy>();
						if (S.Pierced.Contains(E)) continue;
						S.Pierced.Add(E);
						E.HP -= S.Damage;
						E.HitFlash = 3;
						Score += 10;
						AddParticle(E.Pos, new FVector2f(Rng.FRandRange(-1f, 1f), -1.5f), 10f, new FLinearColor(1f, 0.6f, 0.45f), 12);
						continue;
					}
					E.HP -= S.Damage;
					E.HitFlash = 3;
					S.bDead = true;
					Score += 10;
					break;
				}
			}
			bool bHitBoss = false;
			if (!S.bDead && !S.bPiercedBoss && Boss.bActive && Boss.bVulnerable)
			{
				FVector2f[] Tg = new FVector2f[4];
				int NT = BossTargets(Tg);
				for (int i = 0; i < NT; ++i) bHitBoss |= DistSq(S.Pos, Tg[i]) < Sq(30f);
			}
			if (bHitBoss)
			{
				if (!(S.bFromBomb && IsBombImmunePhase())) DamageBoss(S.Damage);
				if (S.bPierce) S.bPiercedBoss = true;   // goes on through, but hits the boss only once
				else S.bDead = true;
				Score += 10;
			}
			if (S.bDead && Particles.Count < 2500)
			{
				AddParticle(S.Pos, new FVector2f(Rng.FRandRange(-1f, 1f), Rng.FRandRange(-2f, -0.5f)), 8f, GetCharacterInfo(Player.Character).Color, 10);
			}
		}
		Shots.RemoveAll(S => S.bDead);
	}

	// =====================================================================================
	// Items & particles
	// =====================================================================================

	public void SpawnItem(EItemType Type, FVector2f Pos, bool bAutoCollect)
	{
		if (Items.Count >= 1500) return;
		var I = new FItem();
		Items.Add(I);
		I.Type = Type;
		I.Pos = Pos;
		I.bCollecting = bAutoCollect;
		I.Vel = new FVector2f(Rng.FRandRange(-1f, 1f), Rng.FRandRange(-3f, -1.8f));
	}

	public void UpdateItems()
	{
		bool bAlive = Player.DeathTimer == 0 && Player.RespawnTimer == 0;
		bool bAbovePOC = bAlive && Player.Pos.Y < CollectLine();
		// 晴's calm: items fall 20% slower
		float ItemFall = Player.Character == ECharacter.Haru ? 0.8f : 1f;

		foreach (FItem I in Items)
		{
			++I.Age;
			if (bAbovePOC) I.bCollecting = true;
			FVector2f ToP = Player.Pos - I.Pos;
			float Dist = ToP.Size();

			if (I.bCollecting && bAlive && (I.Type != EItemType.Star || I.Age > 20))
			{
				float Spd = I.Type == EItemType.Star ? 8f : 10f;
				if (Dist < Spd + 4f)
				{
					CollectItem(I);
					I.bDead = true;
					continue;
				}
				I.Pos += ToP / Dist * Spd;
			}
			else
			{
				if (I.Type == EItemType.Star)
				{
					I.Pos.Y -= 0.3f;
				}
				else
				{
					I.Vel.Y = FMath.Min(I.Vel.Y + 0.06f, 2.2f);
					I.Vel.X *= 0.95f;
					I.Pos += I.Vel * ItemFall;
				}
				if (bAlive && Dist < (Player.bFocus ? 48f : 22f))
				{
					I.bCollecting = true;
				}
				if (I.Pos.Y > FieldH + 24f) I.bDead = true;
			}
		}
		Items.RemoveAll(I => I.bDead);
	}

	public void CollectItem(FItem I)
	{
		Sfx(ESfx.Item);
		switch (I.Type)
		{
		case EItemType.Power:
			if (Player.Power >= 4f) Score += 1000;
			else { int Before = PowerLevel(); Player.Power = FMath.Min(4f, Player.Power + 0.01f); Score += 10; PowerUpPopup(Before); }
			break;
		case EItemType.BigPower:
			{ int Before = PowerLevel(); Player.Power = FMath.Min(4f, Player.Power + Rng.FRandRange(0.5f, 1.5f)); PowerUpPopup(Before); }
			Score += 1000;
			break;
		case EItemType.Point:
		{
			float T = FMath.Clamp((Player.Pos.Y - CollectLine()) / (FieldH - CollectLine()), 0f, 1f);
			Score += I.bCollecting && Player.Pos.Y < CollectLine() ? 50000 : (long)FMath.Lerp(40000f, 8000f, T);
			break;
		}
		case EItemType.Star:
			Score += 300;
			break;
		case EItemType.LifePiece:
			AddLifePieces(1, "符卡收取");
			break;
		case EItemType.BombPiece:
			AddBombPieces(1, "靈符碎片");
			break;
		case EItemType.OneUp:
			if (Player.Lives < MaxLives)
			{
				++Player.Lives;
				Sfx(ESfx.SpellGet);
				Popup("1UP！", new FLinearColor(1f, 0.55f, 0.7f));
				AddParticle(Player.Pos, FVector2f.Zero, 120f, new FLinearColor(1f, 0.55f, 0.7f), 30, true);
			}
			else Score += 2000000;
			break;
		}
	}

	// =====================================================================================
	// Life / bomb recovery — a mix of the systems from different Touhou games:
	//   * score extends (1UP at 10M / 30M / 60M / 100M / 150M, then every 100M)
	//   * life pieces: capturing a spell card drops one; 5 pieces = 1UP
	//   * bomb pieces: beating a non-spell, killing a big spirit, or every 250 grazes; 3 pieces = 1 bomb
	// Lives and bombs cap at 6; anything beyond that turns into score.
	// =====================================================================================

	public static long ExtendScore(int Index)
	{
		long[] Table = ExtendTable;
		int N = Table.Length;
		return Index < N ? Table[Index] : Table[N - 1] + (long)(Index - N + 1) * 100000000L;
	}

	/** A shot power level was just reached: say so, like Touhou's "Power Up" / "MAX". */
	public void PowerUpPopup(int Before)
	{
		int Now = PowerLevel();
		if (Now <= Before) return;
		Popup(Now >= 4 ? Tr("靈力 MAX！") : Tr("靈力升級！"), new FLinearColor(1f, 0.6f, 0.5f));
		Sfx(ESfx.MenuOk);
	}

	public void Popup(string Text, FLinearColor Color)
	{
		PopupText = Text;
		PopupColor = Color;
		PopupTimer = 110;
	}

	public void AddLifePieces(int N, string Reason)
	{
		Player.LifePieces += N;
		bool bExtend = false;
		while (Player.LifePieces >= LifePiecesPerLife)
		{
			Player.LifePieces -= LifePiecesPerLife;
			if (Player.Lives < MaxLives) { ++Player.Lives; bExtend = true; }
			else Score += 2000000;
		}
		if (bExtend)
		{
			Sfx(ESfx.SpellGet);
			Popup("1UP！", new FLinearColor(1f, 0.55f, 0.7f));
			AddParticle(Player.Pos, FVector2f.Zero, 120f, new FLinearColor(1f, 0.55f, 0.7f), 30, true);
		}
		else
		{
			Popup(Tr(Reason) + "　" + Tr("殘機碎片") + $" {Player.LifePieces}/{LifePiecesPerLife}", new FLinearColor(1f, 0.7f, 0.8f));
		}
	}

	public void AddBombPieces(int N, string Reason)
	{
		Player.BombPieces += N;
		bool bGot = false;
		while (Player.BombPieces >= BombPiecesNeeded())
		{
			Player.BombPieces -= BombPiecesNeeded();
			if (Player.Bombs < MaxBombs) { ++Player.Bombs; bGot = true; }
			else Score += 500000;
		}
		if (bGot)
		{
			Sfx(ESfx.Item);
			Popup("靈符 +1", new FLinearColor(0.6f, 1f, 0.65f));
			AddParticle(Player.Pos, FVector2f.Zero, 90f, new FLinearColor(0.6f, 1f, 0.65f), 26, true);
		}
		else
		{
			Popup(Tr(Reason) + $"　{Player.BombPieces}/{BombPiecesNeeded()}", new FLinearColor(0.7f, 1f, 0.75f));
		}
	}

	public void UpdateRecovery()
	{
		if (PopupTimer > 0) --PopupTimer;
		while (Score >= ExtendScore(NextExtend))
		{
			++NextExtend;
			if (Player.Lives < MaxLives)
			{
				++Player.Lives;
				Sfx(ESfx.SpellGet);
				Popup("分數獎勵　1UP！", new FLinearColor(1f, 0.55f, 0.7f));
			}
		}
		if (Graze >= NextGrazeReward)
		{
			NextGrazeReward += GrazePerBombPiece;
			AddBombPieces(1, "擦彈獎勵");
		}
	}

	public void AddParticle(FVector2f Pos, FVector2f Vel, float Size, FLinearColor Color, int Life, bool bRing = false)
	{
		if (Particles.Count >= 3000) return;
		var P = new FParticle();
		Particles.Add(P);
		P.Pos = Pos;
		P.Vel = Vel;
		P.Size = Size;
		P.Color = Color;
		P.Life = P.MaxLife = FMath.Max(1, Life);
		P.bRing = bRing;
	}

	public void Burst(FVector2f Pos, int Count, FLinearColor Color, float Speed, float Size, int Life)
	{
		for (int i = 0; i < Count; ++i)
		{
			AddParticle(Pos, Dir(Rng.FRandRange(0f, 360f)) * Rng.FRandRange(Speed * 0.3f, Speed), Size * Rng.FRandRange(0.6f, 1.2f), Color,
				FMath.RoundToInt(Life * Rng.FRandRange(0.6f, 1f)));
		}
	}

	public void UpdateParticles()
	{
		foreach (FParticle P in Particles)
		{
			P.Pos += P.Vel;
			P.Vel *= 0.94f;
			--P.Life;
		}
		Particles.RemoveAll(P => P.Life <= 0);
	}

	// =====================================================================================
	// Telegraphed beam hazards (lightning, bamboo, ...)
	// =====================================================================================

	public FBeamHazard AddBeam(FVector2f Origin, float Angle, float Width, int Warn, int Rise, int Hold, FLinearColor Color, byte OnStrike = 0)
	{
		var Bm = new FBeamHazard();
		Beams.Add(Bm);
		Bm.Origin = Origin;
		Bm.Angle = Angle;
		Bm.Width = Width;
		Bm.Warn = Warn;
		Bm.Rise = FMath.Max(1, Rise);
		Bm.Hold = Hold;
		Bm.Color = Color;
		Bm.OnStrike = OnStrike;
		// length to the edge of the field
		FVector2f D = Dir(Angle);
		float L = 2000f;
		if (D.X > 0.001f)  L = FMath.Min(L, (FieldW - Origin.X) / D.X);
		if (D.X < -0.001f) L = FMath.Min(L, -Origin.X / D.X);
		if (D.Y > 0.001f)  L = FMath.Min(L, (FieldH - Origin.Y) / D.Y);
		if (D.Y < -0.001f) L = FMath.Min(L, -Origin.Y / D.Y);
		Bm.MaxLength = FMath.Max(10f, L);
		return Bm;
	}

	public void SpawnLeaves(FVector2f At, float Angle, int Count)
	{
		for (int i = 0; i < Count; ++i)
		{
			FBullet B = FireVec(At + new FVector2f(Rng.FRandRange(-6f, 6f), Rng.FRandRange(-6f, 6f)),
				Dir(Angle + Rng.FRandRange(-14f, 14f)) * Rng.FRandRange(1.1f, 1.75f), EBulletShape.Rice,
				new FLinearColor(Rng.FRandRange(0.45f, 0.65f), Rng.FRandRange(0.85f, 1f), Rng.FRandRange(0.45f, 0.65f)));
			B.Spin = Rng.FRandRange(-5f, 5f);
		}
	}

	public void UpdateBeams(bool bMove)
	{
		bool bHittable = CanBeHit();
		foreach (FBeamHazard Bm in Beams)
		{
			if (Bm.bDead) continue;
			if (bMove) ++Bm.Age;
			int RiseEnd = Bm.Warn + Bm.Rise;
			if (Bm.Age < Bm.Warn) Bm.Length = 0f;
			else if (Bm.Age < RiseEnd) Bm.Length = Bm.MaxLength * (float)(Bm.Age - Bm.Warn + 1) / (float)Bm.Rise;
			else Bm.Length = Bm.MaxLength;

			FVector2f End = Bm.Origin + Dir(Bm.Angle) * Bm.Length;
			if (bMove && Bm.OnStrike == 2 && Bm.Age == Bm.Warn) Sfx(ESfx.Bamboo);   // the shoot starts to grow
			if (!Bm.bStruck && Bm.Age >= RiseEnd)
			{
				Bm.bStruck = true;
				if (Bm.OnStrike == 1) Sfx(ESfx.Thunder);
				else if (Bm.OnStrike == 2) Sfx(ESfx.BambooHit);
				else Sfx(ESfx.Break);
				if (Bm.OnStrike == 1)
				{
					// lightning: fire bursts out of the ground and rains back down
					int N = Cnt(Rng.RandRange(10, 20));
					for (int i = 0; i < N; ++i)
					{
						FBullet F = FireVec(new FVector2f(Bm.Origin.X + Rng.FRandRange(-36f, 36f), FieldH + Rng.FRandRange(2f, 14f)),
							new FVector2f(Rng.FRandRange(-1.3f, 1.3f), -Rng.FRandRange(6.3f, 7.8f)), EBulletShape.Fire,
							new FLinearColor(1f, Rng.FRandRange(0.47f, 0.78f), 0.24f));
						F.Gravity = new FVector2f(0f, Rng.FRandRange(0.07f, 0.09f));
					}
				}
				else if (Bm.OnStrike == 2)
				{
					SpawnLeaves(End, Bm.Angle + 180f, Cnt(Rng.RandRange(4, 9)));
				}
				else if (Bm.OnStrike == 4)
				{
					// 窮奇's teeth: where the beam lands, a slow crescent drifts back up to the top
					Fire(new FVector2f(End.X, FieldH - 6f), 0.5f, 270f, EBulletShape.Crescent, new FLinearColor(0.45f, 0.5f, 1f));
					Burst(new FVector2f(End.X, FieldH - 6f), 6, Bm.Color, 2f, 8f, 16);
				}
				else if (Bm.OnStrike == 3)
				{
					// the beam shatters against the wall and sprays shrapnel back into the field
					int N = Cnt(12);
					for (int i = 0; i < N; ++i)
					{
						FBullet Sh = Fire(End, Rng.FRandRange(0.65f, 1.15f), Bm.Angle + 180f + Rng.FRandRange(-75f, 75f), EBulletShape.Small, FMath.Lerp(Bm.Color, LinColor.White, 0.3f));
						Sh.Accel = 0.002f;   // half the speed it used to have
					}
					Burst(End, 10, Bm.Color, 2.5f, 10f, 20);
				}
			}
			if (Bm.Age >= RiseEnd + Bm.Hold)
			{
				Bm.bDead = true;
				continue;
			}
			if (Bm.IsActive() && Bm.Length > 0f)
			{
				// distance from the player to the beam segment
				FVector2f AB = End - Bm.Origin;
				float T = FMath.Clamp(V2.DotProduct(Player.Pos - Bm.Origin, AB) / FMath.Max(1f, AB.SizeSquared()), 0f, 1f);
				float D = V2.Distance(Player.Pos, Bm.Origin + AB * T);
				if (!Bm.bGrazed && D < Bm.Width * 0.5f + GrazeRadius && Player.DeathTimer == 0)
				{
					Bm.bGrazed = true;
					++Graze;
					Score += 500;
					Sfx(ESfx.Graze);
				}
				if (bHittable && !Bm.bHarmless && D < Bm.Width * 0.5f + HitRadius())
				{
					PlayerHit();
				}
			}
		}
		Beams.RemoveAll(B => B.bDead);
	}
}
