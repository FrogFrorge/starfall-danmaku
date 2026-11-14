// Port of DanmakuAudio.h + DanmakuAudio.cpp.
// Plays the game's sound effects and music. WAV files are loaded from res://Audio/ and played through a pool of
// AudioStreamPlayer nodes (children of Main). The simulation pushes ESfx events; this class consumes them every frame.
using Godot;

namespace DanmakuSTG;

public sealed class DanmakuAudio
{
	readonly struct FSfxDef
	{
		public readonly string File; public readonly float Volume; public readonly float MinInterval;
		public FSfxDef(string File, float Volume, float MinInterval) { this.File = File; this.Volume = Volume; this.MinInterval = MinInterval; }
	}
	static readonly FSfxDef[] GSfx = {
		new("sfx_shot.wav",        0.22f, 0.075f),  // Shot
		new("sfx_enemy_shot.wav",  0.16f, 0.07f),   // EnemyShot
		new("sfx_graze.wav",       0.30f, 0.05f),   // Graze
		new("sfx_item.wav",        0.25f, 0.035f),  // Item
		new("sfx_enemy_die.wav",   0.45f, 0.06f),   // EnemyDie
		new("sfx_player_die.wav",  0.90f, 0f),      // PlayerDie
		new("sfx_bomb.wav",        0.85f, 0f),      // Bomb
		new("sfx_spell.wav",       0.75f, 0f),      // Spell
		new("sfx_spell_get.wav",   0.80f, 0f),      // SpellGet
		new("sfx_break.wav",       0.60f, 0.1f),    // Break
		new("sfx_menu_move.wav",   0.45f, 0f),      // MenuMove
		new("sfx_menu_ok.wav",     0.55f, 0f),      // MenuOk
		new("sfx_menu_back.wav",   0.45f, 0f),      // MenuBack
		new("sfx_pause.wav",       0.50f, 0f),      // Pause
		new("sfx_thunder.wav",     0.80f, 0.12f),   // Thunder (嵐's lightning strike; Tools/audio/make_sfx_nature.py)
		new("sfx_bamboo.wav",      0.55f, 0.08f),   // Bamboo (墨竹's bamboo shoot rising: wind)
		new("sfx_bamboo_hit.wav",  0.65f, 0.08f),   // BambooHit (the shoot hitting the top)
	};
	static readonly string[] GMusic = { "bgm_title.wav", "bgm_stage.wav", "bgm_boss.wav" };
	const float MusicVolume = 0.5f;
	const float LaserVolume = 0.3f;
	const int PoolSize = 24;
	const float MusicFadeTime = 0.8f;   // UE: MusicComp->FadeIn(0.8f, 1.f)

	readonly Main Owner;
	readonly DanmakuWorld Sim;

	readonly AudioStreamWav[] SfxPcm = new AudioStreamWav[(int)ESfx.Count];   // indexed by ESfx
	readonly AudioStreamWav[] MusicPcm = new AudioStreamWav[3];               // 0 title, 1 stage, 2 boss (stage 1 / fallback)
	readonly Dictionary<int, AudioStream> StageMusic = new();                 // track id -> music for it (loaded on first use)
	readonly Dictionary<AudioStreamWav, AudioStreamWav> LoopCopies = new();   // looping copy of each music/laser stream
	AudioStreamWav LaserPcm;

	readonly List<AudioStreamPlayer> SfxPool = new();
	AudioStreamPlayer MusicComp;
	AudioStreamPlayer LaserComp;

	int NextSfx = 0;
	int CurrentTrack = -2;
	bool bLaserPlaying = false;
	int PlayedCount = 0;
	float StatusTimer = 0f;
	readonly double[] LastPlayed = new double[(int)ESfx.Count];
	float MusicFade = -1f;   // seconds into the music fade-in (< 0: no fade running)

	public DanmakuAudio(Main Owner, DanmakuWorld World)
	{
		this.Owner = Owner;
		Sim = World;
		BeginPlay();
	}

	static AudioStreamWav LoadWav(string FileName)
	{
		string Path = "res://Audio/" + FileName;
		if (!ResourceLoader.Exists(Path)) return null;
		return GD.Load<AudioStreamWav>(Path);
	}

	/** A looping copy of the stream (UE re-queued the same PCM buffer on underflow so the track loops forever). */
	AudioStreamWav MakeLooping(AudioStreamWav Pcm)
	{
		if (LoopCopies.TryGetValue(Pcm, out var Found)) return Found;
		var Wave = (AudioStreamWav)Pcm.Duplicate();
		Wave.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
		Wave.LoopBegin = 0;
		Wave.LoopEnd = (int)(Pcm.GetLength() * Pcm.MixRate);   // in sample frames (works for PCM and compressed imports)
		LoopCopies[Pcm] = Wave;
		return Wave;
	}

	AudioStreamPlayer MakeComponent()
	{
		var C = new AudioStreamPlayer();
		Owner.AddChild(C);
		return C;
	}

	void BeginPlay()
	{
		for (int i = 0; i < (int)ESfx.Count; ++i) SfxPcm[i] = LoadWav(GSfx[i].File);
		for (int i = 0; i < 3; ++i) MusicPcm[i] = LoadWav(GMusic[i]);
		LaserPcm = LoadWav("sfx_laser_loop.wav");

		for (int i = 0; i < PoolSize; ++i) SfxPool.Add(MakeComponent());
		MusicComp = MakeComponent();
		LaserComp = MakeComponent();
		// safety net: if a stream ever ends instead of looping, start it again
		MusicComp.Finished += () => { if (MusicComp.Stream != null && CurrentTrack >= 0) MusicComp.Play(); };
		LaserComp.Finished += () => { if (bLaserPlaying) LaserComp.Play(); };

		int Loaded = 0;
		foreach (var P in SfxPcm) Loaded += P != null ? 1 : 0;
		GD.Print($"Loaded {Loaded}/{(int)ESfx.Count} sound effects, music: title={(MusicPcm[0] != null ? 1 : 0)} stage={(MusicPcm[1] != null ? 1 : 0)} boss={(MusicPcm[2] != null ? 1 : 0)}, laser={(LaserPcm != null ? 1 : 0)}");
	}

	void PlaySfx(ESfx Sfx)
	{
		int Idx = (int)Sfx;
		if (Idx < 0 || Idx >= SfxPcm.Length || SfxPcm[Idx] == null) return;
		double Now = Time.GetTicksUsec() / 1e6;
		if (Now - LastPlayed[Idx] < GSfx[Idx].MinInterval) return;
		LastPlayed[Idx] = Now;

		AudioStreamPlayer C = SfxPool[NextSfx];
		NextSfx = (NextSfx + 1) % SfxPool.Count;
		C.Stop();
		C.Stream = SfxPcm[Idx];
		float Vol = GSfx[Idx].Volume * GetSfxVolume() / 10f;
		if (Vol <= 0.001f) return;
		C.VolumeDb = Mathf.LinearToDb(Vol);
		C.Play();
		++PlayedCount;
	}

	// ---- your own songs: Godot/Music/<name>.ogg / .mp3 / .wav, read straight from disk (no Godot import needed) ----
	// Names: title, stage1_road, stage1_boss, ... stage6_road, stage6_boss, extra_road, extra_boss, extraif_road, extraif_boss.
	// A song that isn't there falls back to the built-in track in Audio/.
	static string MusicDir() => OS.HasFeature("template")
		? OS.GetExecutablePath().GetBaseDir() + "/Music/"
		: ProjectSettings.GlobalizePath("res://Music/");

	static string MusicName(int Track)
	{
		if (Track == 0) return "title";
		int Stage = (Track - 1) / 2;
		string Kind = (Track % 2) == 0 ? "boss" : "road";
		string Where = Stage == DanmakuWorld.ExtraStage ? "extra" : Stage == DanmakuWorld.IfStage ? "extraif" : $"stage{Stage + 1}";
		return $"{Where}_{Kind}";
	}

	/** The other accepted naming: title, road1 / boss1 ... road6 / boss6, roadextra / bossextra, roadextraif / bossextraif. */
	static string MusicShortName(int Track)
	{
		if (Track == 0) return "title";
		int Stage = (Track - 1) / 2;
		string Kind = (Track % 2) == 0 ? "boss" : "road";
		string Where = Stage == DanmakuWorld.ExtraStage ? "extra" : Stage == DanmakuWorld.IfStage ? "extraif" : $"{Stage + 1}";
		return Kind + Where;
	}

	static AudioStream LoadUserMusic(string Name)
	{
		string Base = MusicDir() + Name;
		try
		{
			if (System.IO.File.Exists(Base + ".ogg"))
			{
				var S = AudioStreamOggVorbis.LoadFromFile(Base + ".ogg");
				if (S != null) { S.Loop = true; return S; }
			}
			if (System.IO.File.Exists(Base + ".mp3"))
			{
				return new AudioStreamMP3 { Data = System.IO.File.ReadAllBytes(Base + ".mp3"), Loop = true };
			}
			if (System.IO.File.Exists(Base + ".wav"))
			{
				var S = AudioStreamWav.LoadFromFile(Base + ".wav");
				if (S != null) return S;   // made looping in SetMusic
			}
		}
		catch (Exception E) { GD.PrintErr($"could not load {Base}: {E.Message}"); }
		return null;
	}

	// Track ids: 0 title, 1 + 2*Stage road theme, 2 + 2*Stage boss theme.
	// Your song in Music/ wins; otherwise stage N (N >= 1) uses Audio/bgm_stage<N+1>.wav / bgm_boss<N+1>.wav, then the stage-1 tracks.
	AudioStream MusicForTrack(int Track)
	{
		if (Track < 0) return null;
		if (StageMusic.TryGetValue(Track, out var Found)) return Found;
		AudioStream M = LoadUserMusic(MusicName(Track)) ?? LoadUserMusic(MusicShortName(Track));
		if (M == null)
		{
			if (Track <= 2) M = MusicPcm[Track];
			else
			{
				int Stage = (Track - 1) / 2;
				bool bBoss = (Track % 2) == 0;
				M = LoadWav($"{(bBoss ? "bgm_boss" : "bgm_stage")}{Stage + 1}.wav") ?? MusicPcm[bBoss ? 2 : 1];
			}
		}
		else GD.Print($"music: using your {MusicName(Track)}");
		StageMusic[Track] = M;
		return M;
	}

	void SetMusic(int Track)
	{
		if (Track == CurrentTrack) return;
		CurrentTrack = Track;
		MusicComp.Stop();
		MusicFade = -1f;
		AudioStream Music = MusicForTrack(Track);
		if (Music == null) return;
		MusicComp.Stream = Music is AudioStreamWav Pcm ? MakeLooping(Pcm) : Music;
		MusicComp.VolumeDb = -80f;
		MusicComp.Play();
		MusicFade = 0f;   // fade in to MusicVolume over 0.8 s
	}

	public void Tick()
	{
		if (Sim == null) return;
		float DeltaSeconds = (float)Owner.GetProcessDeltaTime();

		// one-shot effects (each type at most once per frame)
		var bSeen = new bool[(int)ESfx.Count];
		foreach (ESfx S in Sim.SfxQueue)
		{
			int I = (int)S;
			if (I >= 0 && I < (int)ESfx.Count && !bSeen[I])
			{
				bSeen[I] = true;
				PlaySfx(S);
			}
		}
		Sim.SfxQueue.Clear();

		// music follows the game state
		int Track = 0;
		switch (Sim.State)
		{
		case EDanmakuState.Playing:    Track = (Sim.Boss.bStarted && !Sim.bMidboss ? 2 : 1) + 2 * Sim.StageIndex; break;
		case EDanmakuState.StageClear: Track = CurrentTrack; break;
		case EDanmakuState.GameOver:   Track = -1; break;
		case EDanmakuState.Clear:      Track = 0; break;
		case EDanmakuState.MusicRoom:  Track = Sim.MusicPlaying >= 0 ? Sim.MusicPlaying : 0; break;
		default:                       Track = 0; break;
		}
		SetMusic(Track);
		if (Sim.State == EDanmakuState.Playing || Sim.State == EDanmakuState.Clear) Sim.MarkMusicHeard(Track);   // names it in the music room
		if (MusicFade >= 0f)
		{
			MusicFade += DeltaSeconds;
			float K = Mathf.Min(1f, MusicFade / MusicFadeTime);
			MusicComp.VolumeDb = Mathf.Max(-80f, Mathf.LinearToDb(MusicVolume * GetMusicVolume() / 10f * K));
			if (K >= 1f) MusicFade = -1f;
		}
		else if (MusicComp != null)
		{
			// follow the volume setting (changed in the options screen)
			MusicComp.VolumeDb = Mathf.Max(-80f, Mathf.LinearToDb(MusicVolume * GetMusicVolume() / 10f));
		}
		StatusTimer += DeltaSeconds;
		if (StatusTimer > 3f)
		{
			StatusTimer = 0f;
			GD.Print($"music track {CurrentTrack} playing={(MusicComp != null && MusicComp.Playing ? 1 : 0)}, sfx played so far={PlayedCount}");
		}
		if (MusicComp != null) MusicComp.StreamPaused = Sim.bPaused;

		// Sou's continuous laser hum
		bool bLaser = Sim.State == EDanmakuState.Playing && !Sim.bPaused && Sim.Player.bLaserOn && LaserPcm != null;
		if (bLaser && !bLaserPlaying)
		{
			LaserComp.Stream = MakeLooping(LaserPcm);
			LaserComp.VolumeDb = Mathf.Max(-80f, Mathf.LinearToDb(LaserVolume * GetSfxVolume() / 10f));
			LaserComp.Play();
		}
		else if (!bLaser && bLaserPlaying)
		{
			LaserComp.Stop();
		}
		bLaserPlaying = bLaser;
	}
}
