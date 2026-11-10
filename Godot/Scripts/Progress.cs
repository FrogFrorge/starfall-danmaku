// Unlocks, Touhou style: a spell card shows up in spell practice once you've met it in the game, and a song is named in the
// music room once you've heard it in the game. Saved in user://progress.cfg (each player's own machine). Demo / bot runs
// never unlock anything, so testing doesn't spoil the real save.
using System.Collections.Generic;
using Godot;

namespace DanmakuSTG;

public partial class DanmakuWorld
{
	const string ProgressFile = "user://progress.cfg";
	static HashSet<string> GSpellsSeen;
	static HashSet<int> GMusicHeard;
	/** Locked songs the player chose to hear anyway in this music-room visit (spoilers accepted; not saved). */
	public readonly HashSet<int> MusicRevealed = new();
	/** The locked song whose spoiler warning is showing (Z again plays it), -1 none. */
	public int MusicConfirm = -1;
	/** -DanmakuLocked: a demo run that shows the menus as a brand-new player sees them (nothing unlocked, nothing saved). */
	public bool bDemoLocked = false;

	static void LoadProgress()
	{
		if (GSpellsSeen != null) return;
		GSpellsSeen = new HashSet<string>();
		GMusicHeard = new HashSet<int> { 0 };   // the title theme is always known
		var Cfg = new ConfigFile();
		if (Cfg.Load(ProgressFile) != Error.Ok) return;
		foreach (string S in ((string)Cfg.GetValue("Progress", "Spells", "")).Split('\n'))
			if (S.Length > 0) GSpellsSeen.Add(S);
		foreach (string S in ((string)Cfg.GetValue("Progress", "Music", "")).Split(','))
			if (int.TryParse(S, out int T)) GMusicHeard.Add(T);
	}

	static void SaveProgress()
	{
		var Cfg = new ConfigFile();
		Cfg.SetValue("Progress", "Spells", string.Join("\n", GSpellsSeen));
		Cfg.SetValue("Progress", "Music", string.Join(",", GMusicHeard));
		Cfg.Save(ProgressFile);
	}

	static string SpellKey(int Stage, string Name) => Stage + "|" + Name;

	public bool IsSpellUnlocked(int Index)
	{
		if (!IsValidSpellIndex(Index)) return false;
		if (bDemo) return !bDemoLocked;   // automated tests and captures see everything
		LoadProgress();
		FSpellEntry E = SpellEntries()[Index];
		return GSpellsSeen.Contains(SpellKey(E.Stage, E.Def.Phases[E.Phase].Name));
	}

	public void MarkSpellSeen(string Name)
	{
		if (bDemo || string.IsNullOrEmpty(Name)) return;
		LoadProgress();
		if (GSpellsSeen.Add(SpellKey(StageIndex, Name))) SaveProgress();
	}

	public bool IsMusicUnlocked(int Track)
	{
		if (bDemo) return !bDemoLocked || Track == 0;
		LoadProgress();
		return GMusicHeard.Contains(Track);
	}

	/** Called by Audio when a song starts playing during a run. */
	public void MarkMusicHeard(int Track)
	{
		if (bDemo || Track < 0) return;
		LoadProgress();
		if (GMusicHeard.Add(Track)) SaveProgress();
	}

	/** The music room shows this song's name and comment: heard in the game, or spoilers accepted. */
	public bool IsMusicShown(int Track) => IsMusicUnlocked(Track) || MusicRevealed.Contains(Track);
}
