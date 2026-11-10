"""Boss themes with their own character: each boss gets its own meter, scale, lead instrument, accompaniment and drums.
    python Tools/audio/boss_themes.py            -> writes Audio/bgm_boss*.wav (and the Extra-IF road theme)
    python Tools/audio/boss_themes.py boss3      -> only that track

  天狼   bgm_boss    4/4 168  A aeolian      celesta bells + flute over 16th koto arpeggios
  千秋   bgm_boss2   3/4 156  B dorian       flute waltz, koto on beats 2 and 3, woodblock
  嵐     bgm_boss3   4/4 188  C phrygian     erhu over a galloping shamisen ostinato, double kick, thunder
  墨竹   bgm_boss4   4/4 112  G in-sen       shakuhachi, guzheng glissandi, sparse woodblock and small gong (half-time)
  玄     bgm_boss5   6/8 138  F# harm. minor erhu over a 6/8 marimba ostinato, frame drum
  燁     bgm_boss6   4/4 176  D minor        brass stabs, sho organ, flute + bells, heavy taiko
  一索   bgm_boss7   4/4 196  E mixolydian   swung square-wave lead, walking bass, ride, claps, tile clicks
  四凶   bgm_boss8   7/8 200  E octatonic    erhu + detuned square, distorted bass, taiko, gong
  (road) bgm_stage8  4/4 150  E phrygian dom. shakuhachi over shamisen, driving drums
"""
import math, os, sys
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_audio as M
from make_audio import SR, t_axis, midi, lowpass, highpass, bandpass, env_adsr, reverb, normalize, save, mix_at

rng = np.random.default_rng(7171)

# ----------------------------------------------------------------------------------------
# extra instruments
# ----------------------------------------------------------------------------------------
def shamisen(f, dur):
    y = M.koto(f, dur, 0.985, 9500)
    click = highpass(rng.uniform(-1, 1, min(len(y), 400)), 3000) * np.exp(-np.arange(min(len(y), 400)) / 60)
    y[:len(click)] += click * 0.4
    return y


def erhu(f, dur, vib=0.009):
    t = t_axis(dur)
    bend = 1 - 0.06 * np.exp(-t * 18)                      # slide up into the note
    v = 1 + vib * np.sin(2 * np.pi * 5.8 * t) * np.clip((t - 0.1) * 4, 0, 1)
    ph = 2 * np.pi * np.cumsum(f * bend * v) / SR
    y = sum(np.sin(k * ph) / k ** 1.1 for k in range(1, 12))
    y = bandpass(y, 280, 4200)
    return y * env_adsr(len(t), 0.06, 0.1, 0.85, 0.08)


def marimba(f, dur):
    t = t_axis(dur)
    return (np.sin(2 * np.pi * f * t) * np.exp(-t * 4) + 0.35 * np.sin(2 * np.pi * f * 4 * t) * np.exp(-t * 14)
            + 0.1 * np.sin(2 * np.pi * f * 9.9 * t) * np.exp(-t * 30))


def shakuhachi(f, dur):
    t = t_axis(dur)
    bend = 1 - 0.03 * np.exp(-t * 10)
    v = 1 + 0.012 * np.sin(2 * np.pi * 4.6 * t) * np.clip((t - 0.25) * 3, 0, 1)
    ph = 2 * np.pi * np.cumsum(f * bend * v) / SR
    tone = np.sin(ph) + 0.18 * np.sin(2 * ph)
    breath = bandpass(rng.uniform(-1, 1, len(t)), f * 0.8, min(f * 6, 9000)) * 0.35
    return (tone + breath * (0.6 + 0.4 * np.exp(-t * 5))) * env_adsr(len(t), 0.09, 0.1, 0.8, 0.12)


def square(f, dur, detune=0.0):
    t = t_axis(dur)
    y = np.zeros_like(t)
    for d in ((0.0,) if not detune else (-detune, detune)):
        ph = 2 * np.pi * f * (1 + d) * t
        y += sum(np.sin(k * ph) / k for k in range(1, 14, 2))
    return lowpass(y, 4200) * env_adsr(len(t), 0.004, 0.05, 0.7, 0.04) * 0.6


def sho(freqs, dur):
    t = t_axis(dur)
    y = sum(np.sin(2 * np.pi * f * t) + 0.3 * np.sin(6 * np.pi * f * t) for f in freqs)
    trem = 1 + 0.04 * np.sin(2 * np.pi * 5 * t)
    return y * trem * env_adsr(len(t), 0.35, 0.2, 0.9, 0.3) / len(freqs)


def brass(freqs, dur):
    t = t_axis(dur)
    y = np.zeros_like(t)
    for f in freqs:
        for d in (-0.003, 0.003):
            ph = 2 * np.pi * f * (1 + d) * t
            y += sum(np.sin(k * ph) / k for k in range(1, 10))
    cut = 600 + 3000 * np.exp(-t * 6)
    out = np.zeros_like(y)
    # cheap sweeping filter: blend a bright and a dark copy
    bright, dark = lowpass(y, 3600), lowpass(y, 700)
    w = (cut - 600) / 3000
    out = bright * w + dark * (1 - w)
    return out * env_adsr(len(t), 0.02, 0.15, 0.6, 0.1) / (len(freqs) * 2)


def dist_bass(f, dur):
    return np.tanh(M.bass(f, dur) * 4) * 0.6


def guzheng_gliss(root, dur, up=True):
    notes = [root + i for i in (0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24)]
    if not up:
        notes = notes[::-1]
    out = np.zeros(int(dur * SR) + SR)
    for i, n in enumerate(notes):
        mix_at(out, M.koto(midi(n), 1.2, 0.996, 7000), i * 0.035, 0.5)
    return out


def gong(dur=3.0):
    t = t_axis(dur)
    y = sum(np.sin(2 * np.pi * f * t) * np.exp(-t * d) for f, d in ((110, 0.8), (161, 1.0), (237, 1.3), (379, 1.7), (512, 2.2)))
    return y * (1 - np.exp(-t * 60)) * 0.4


def woodblock():
    t = t_axis(0.08)
    return bandpass(rng.uniform(-1, 1, len(t)), 900, 1800) * np.exp(-t * 60) * 2


def clap():
    t = t_axis(0.18)
    n = bandpass(rng.uniform(-1, 1, len(t)), 900, 6000)
    e = np.exp(-t * 30) + 0.6 * np.exp(-np.maximum(t - 0.012, 0) * 60) * (t > 0.012)
    return n * e


def tom(f=110):
    t = t_axis(0.4)
    return np.sin(2 * np.pi * np.cumsum(f + 60 * np.exp(-t * 20)) / SR) * np.exp(-t * 7)


def ride():
    t = t_axis(0.6)
    return highpass(M.bell(3800, 0.6, 4, 2.7) + rng.uniform(-1, 1, len(t)) * 0.3 * np.exp(-t * 8), 3000) * 0.4


def thunder(dur=2.5):
    t = t_axis(dur)
    return lowpass(rng.uniform(-1, 1, len(t)), 300) * np.exp(-t * 1.6) * (1 - np.exp(-t * 20)) * 1.5


# ----------------------------------------------------------------------------------------
# scales and helpers
# ----------------------------------------------------------------------------------------
MODES = {
    "aeolian": [0, 2, 3, 5, 7, 8, 10], "dorian": [0, 2, 3, 5, 7, 9, 10], "phrygian": [0, 1, 3, 5, 7, 8, 10],
    "insen": [0, 1, 5, 7, 10], "harmonic": [0, 2, 3, 5, 7, 8, 11], "minor": [0, 2, 3, 5, 7, 8, 10],
    "mixolydian": [0, 2, 4, 5, 7, 9, 10], "octatonic": [0, 1, 3, 4, 6, 7, 9, 10], "phrygdom": [0, 1, 4, 5, 7, 8, 10],
}


def scale_notes(key, mode, lo, hi):
    out = []
    for o in range(-2, 4):
        for s in MODES[mode]:
            n = key + 12 * o + s
            if lo <= n <= hi:
                out.append(n)
    return sorted(out)


def chord(root, quality):
    third = {"m": 3, "M": 4, "d": 3, "s": 5}[quality]
    fifth = 6 if quality == "d" else 7
    return [root, root + third, root + fifth]


# ----------------------------------------------------------------------------------------
# the renderer
# ----------------------------------------------------------------------------------------
def make_melody(st, bars_spec, r):
    """A motif (rhythm + contour) stated, varied, contrasted, restated: A A' B A'' over the given bars."""
    steps = st["steps"]
    sc = scale_notes(st["key"], st["mode"], st["key"] + 9, st["key"] + 31)
    rhythm = st["rhythm"]                      # note lengths in steps for one 2-bar phrase
    contour = [int(x) for x in r.integers(-2, 3, len(rhythm))]
    contour[0] = 0
    mel = []                                   # (start_step, length_steps, midi)
    for b0, kind in bars_spec:
        if kind == "rest":
            continue
        base = len(sc) // 2 + (3 if kind == "B" else 0)
        idx = base
        pos = b0 * steps
        rh = rhythm if kind != "B" else st.get("rhythm_b", rhythm)
        for i, L in enumerate(rh):
            step = contour[i % len(contour)]
            if kind == "A2":
                step = -step if i % 3 == 1 else step
            if kind == "B":
                step = int(r.integers(-2, 3))
            idx = int(np.clip(idx + step, 0, len(sc) - 1))
            bar = (pos // steps)
            ch = st["prog"][bar % len(st["prog"])]
            n = sc[idx]
            if (pos % steps) == 0:             # strong beat: snap to a chord tone
                tones = [(st["key"] + ch[0] + k) % 12 for k in chord(0, ch[1])]
                cands = [x for x in sc if x % 12 in tones]
                if cands:
                    n = min(cands, key=lambda c: abs(c - n))
            mel.append((pos, L, n))
            pos += L
    return mel


def render(name, st):
    steps = st["steps"]                         # steps per bar
    step_s = 60.0 / st["bpm"] / st.get("div", 2)
    bars = 32
    plan = [(0, "rest"), (2, "rest")] + [(4 + 2 * i, "A" if i % 2 == 0 else "A2") for i in range(4)] + \
           [(12 + 2 * i, "B") for i in range(4)] + [(20 + 2 * i, "A" if i % 2 == 0 else "A2") for i in range(4)] + \
           [(28, "B"), (30, "A2")]
    r = np.random.default_rng(st["seed"])
    mel = make_melody(st, plan, r)
    length = bars * steps * step_s
    buf = np.zeros(int((length + 4.0) * SR))

    def T(bar, s):
        sw = st.get("swing", 0.0) * step_s if s % 2 == 1 else 0.0
        return (bar * steps + s) * step_s + sw

    key = st["key"]
    for b in range(bars):
        root_off, q = st["prog"][b % len(st["prog"])]
        root = key + root_off - 12
        tones = chord(root, q)
        # ---- harmony bed
        bed = st.get("bed", "pad")
        if bed == "pad":
            mix_at(buf, M.pad([midi(n + 12) for n in tones], steps * step_s + 0.3), T(b, 0), 0.16)
        elif bed == "sho":
            mix_at(buf, sho([midi(n + 12) for n in tones] + [midi(root + 26)], steps * step_s + 0.4), T(b, 0), 0.22)
        elif bed == "brass":
            mix_at(buf, M.pad([midi(n + 12) for n in tones], steps * step_s + 0.3), T(b, 0), 0.12)
            for s in st.get("stabs", [0, 3, 6]):
                mix_at(buf, brass([midi(n + 12) for n in tones], step_s * 1.6), T(b, s), 0.5)
        # ---- accompaniment
        acc = st["acc"]
        if acc == "arp16":
            arp = [tones[0] + 12, tones[1] + 12, tones[2] + 12, tones[1] + 24]
            for k in range(steps * 2):
                mix_at(buf, M.koto(midi(arp[k % 4]), 0.3, 0.99, 7000), T(b, 0) + k * step_s / 2, 0.12)
        elif acc == "waltz":
            for s in (2, 4):
                for n in tones:
                    mix_at(buf, M.koto(midi(n + 12), 0.5, 0.993), T(b, s) + 0.01 * (n - root), 0.11)
        elif acc == "shamisen":
            pat = [0, 0, 7, 0, 12, 0, 7, 10]
            for k in range(steps * 2):
                mix_at(buf, shamisen(midi(root + 12 + pat[k % 8]), 0.25), T(b, 0) + k * step_s / 2, 0.15 if k % 4 else 0.22)
        elif acc == "guzheng":
            if b % 2 == 0:
                mix_at(buf, guzheng_gliss(root + 12, 0.5, b % 4 == 0), T(b, 0), 0.18)
            for s in (4, 6):
                mix_at(buf, M.koto(midi(tones[s // 2 - 1] + 24), 1.0, 0.997), T(b, s), 0.12)
        elif acc == "marimba68":
            pat = [0, 2, 1, 2, 0, 2]
            for k in range(6):
                mix_at(buf, marimba(midi(tones[pat[k]] + 12 + (12 if k == 3 else 0)), 0.5), T(b, k), 0.2)
        elif acc == "walking":
            pass                               # handled by the bass
        elif acc == "seven":
            pat = [0, 7, 0, 6, 0, 7, 12]
            for k in range(7):
                mix_at(buf, shamisen(midi(root + 12 + pat[k]), 0.2), T(b, k), 0.16)
        # ---- bass
        bass = st["bass"]
        if bass == "octave8":
            for k in range(steps):
                mix_at(buf, M.bass(midi(root - 12 + (12 if k % 2 else 0)), step_s * 0.9), T(b, k), 0.32)
        elif bass == "waltz":
            mix_at(buf, M.bass(midi(root - 12), step_s * 1.8), T(b, 0), 0.38)
        elif bass == "gallop":
            for k in range(steps):
                for sub in ((0, 0.5), (0.5, 0.25), (0.75, 0.25)) if k % 2 == 0 else ((0, 1.0),):
                    mix_at(buf, M.bass(midi(root - 12), step_s * sub[1] * 0.9), T(b, k) + sub[0] * step_s, 0.3)
        elif bass == "long":
            mix_at(buf, M.bass(midi(root - 12), steps * step_s * 0.95), T(b, 0), 0.3)
        elif bass == "six8":
            mix_at(buf, M.bass(midi(root - 12), step_s * 2.5), T(b, 0), 0.36)
            mix_at(buf, M.bass(midi(root - 5), step_s * 2.5), T(b, 3), 0.3)
        elif bass == "walking":
            walk = [0, 4 if q == "M" else 3, 7, 9 if q == "M" else 10]
            for k in range(4):
                mix_at(buf, M.bass(midi(root - 12 + walk[k]), step_s * 1.8), T(b, 2 * k), 0.36)
        elif bass == "dist7":
            for k, n in enumerate([0, 0, 1, 0, 0, 1, 0]):
                mix_at(buf, dist_bass(midi(root - 12 + (12 if n else 0)), step_s * 0.9), T(b, k), 0.28)
        # ---- drums
        dr = st["drums"]
        if dr == "rock":
            for k in range(steps):
                if k in (0, 4) or (k == 7 and b % 2): mix_at(buf, M.kick(), T(b, k), 0.6)
                if k in (2, 6): mix_at(buf, M.snare(), T(b, k), 0.3)
                mix_at(buf, M.hat(), T(b, k), 0.2)
                mix_at(buf, M.hat(), T(b, k) + step_s / 2, 0.12)
        elif dr == "waltz":
            mix_at(buf, M.kick(), T(b, 0), 0.45)
            for s in (2, 4): mix_at(buf, woodblock(), T(b, s), 0.25)
            for s in range(6): mix_at(buf, M.hat(), T(b, s), 0.12)
        elif dr == "double":
            for k in range(steps):
                mix_at(buf, M.kick(), T(b, k), 0.45)
                mix_at(buf, M.kick(), T(b, k) + step_s / 2, 0.3)
                if k in (2, 6): mix_at(buf, M.snare(), T(b, k), 0.35)
                mix_at(buf, M.hat(), T(b, k), 0.2)
            if b % 8 == 7:
                for k in range(8): mix_at(buf, M.taiko(), T(b, 0) + k * step_s, 0.45)
            if b % 8 == 0: mix_at(buf, thunder(), T(b, 0), 0.35)
        elif dr == "sparse":
            mix_at(buf, M.kick(), T(b, 0), 0.4)
            mix_at(buf, M.snare(), T(b, 4), 0.2)
            for s in (3, 6, 7): mix_at(buf, woodblock(), T(b, s), 0.2)
            if b % 4 == 0: mix_at(buf, gong(2.0), T(b, 0), 0.12)
        elif dr == "frame":
            for s, g in ((0, 0.5), (3, 0.35), (5, 0.25)): mix_at(buf, tom(90), T(b, s), g)
            for s in range(6): mix_at(buf, M.hat(), T(b, s), 0.12)
        elif dr == "epic":
            for k in range(steps):
                if k in (0, 3, 4, 7): mix_at(buf, M.kick(), T(b, k), 0.55)
                if k in (2, 6): mix_at(buf, M.snare(), T(b, k), 0.3)
                mix_at(buf, M.hat(), T(b, k), 0.15)
            for s in (0, 6): mix_at(buf, M.taiko(), T(b, s), 0.55)
            if b % 8 == 7:
                for k in range(8): mix_at(buf, M.taiko(), T(b, 0) + k * step_s, 0.5)
        elif dr == "swing":
            for k in range(steps):
                mix_at(buf, ride(), T(b, k), 0.18 if k % 2 == 0 else 0.1)
            mix_at(buf, M.kick(), T(b, 0), 0.4)
            mix_at(buf, M.kick(), T(b, 5), 0.25)
            for s in (2, 6): mix_at(buf, clap(), T(b, s), 0.3)
            for s in (3, 7): mix_at(buf, woodblock(), T(b, s), 0.12)   # tiles clicking
        elif dr == "seven":
            for s in (0, 2, 4): mix_at(buf, M.taiko(), T(b, s), 0.5 if s == 0 else 0.35)
            for s in (0, 4): mix_at(buf, M.kick(), T(b, s), 0.45)
            mix_at(buf, M.snare(), T(b, 2), 0.25)
            mix_at(buf, M.snare(), T(b, 5), 0.3)
            for s in range(7): mix_at(buf, M.hat(), T(b, s), 0.15)
            if b % 8 == 0: mix_at(buf, gong(), T(b, 0), 0.25)
        elif dr == "drive":
            for k in range(steps):
                if k % 2 == 0: mix_at(buf, M.kick(), T(b, k), 0.5)
                if k in (2, 6): mix_at(buf, M.snare(), T(b, k), 0.3)
                mix_at(buf, M.hat(), T(b, k), 0.18)
            if b % 4 == 3:
                for k in range(4): mix_at(buf, M.taiko(), T(b, 4 + k), 0.4)

    # ---- lead
    lead = st["lead"]
    for (pos, L, n) in mel:
        ts = (pos + (st.get("swing", 0.0) if pos % 2 == 1 else 0.0)) * step_s
        dur = L * step_s * 1.05
        if lead == "bells":
            mix_at(buf, M.bell(midi(n + 12), 1.4, 2.0, 3.5), ts, 0.2)
            mix_at(buf, M.flute(midi(n), dur), ts, 0.16)
        elif lead == "flute":
            mix_at(buf, M.flute(midi(n), dur), ts, 0.24)
        elif lead == "erhu":
            mix_at(buf, erhu(midi(n), dur + 0.05), ts, 0.2)
        elif lead == "shakuhachi":
            mix_at(buf, shakuhachi(midi(n), dur + 0.1), ts, 0.26)
        elif lead == "epic":
            mix_at(buf, M.flute(midi(n), dur), ts, 0.2)
            mix_at(buf, M.bell(midi(n + 12), 1.2, 1.6, 2.0), ts, 0.1)
            mix_at(buf, brass([midi(n - 12)], dur), ts, 0.25)
        elif lead == "square":
            mix_at(buf, square(midi(n), dur * 0.9), ts, 0.16)
        elif lead == "perils":
            mix_at(buf, erhu(midi(n), dur + 0.05), ts, 0.17)
            mix_at(buf, square(midi(n - 12), dur * 0.9, 0.006), ts, 0.1)

    buf = reverb(buf, st.get("wet", 0.2))
    n_loop = int(length * SR)
    out = buf[:n_loop].copy()
    tail = buf[n_loop:]
    out[:len(tail)] += tail
    out = np.tanh(normalize(out, 0.95) * 1.2) / np.tanh(1.2)
    save(name, normalize(out, 0.8))


# chord progressions: (semitones from the key, quality)
THEMES = {
    "bgm_boss.wav":   dict(bpm=168, steps=8, key=57, mode="aeolian", prog=[(0, "m"), (8, "M"), (10, "M"), (7, "m"), (5, "M"), (8, "M"), (10, "M"), (0, "m")],
                           rhythm=[2, 1, 1, 2, 2, 3, 1, 4], rhythm_b=[1, 1, 1, 1, 2, 2, 4, 4], lead="bells", acc="arp16", bass="octave8", drums="rock", seed=101),
    "bgm_boss2.wav":  dict(bpm=156, steps=6, key=59, mode="dorian", prog=[(0, "m"), (5, "M"), (3, "M"), (10, "M"), (0, "m"), (7, "m"), (5, "M"), (10, "M")],
                           rhythm=[2, 1, 1, 2, 4, 2], rhythm_b=[1, 1, 2, 2, 2, 4], lead="flute", acc="waltz", bass="waltz", drums="waltz", seed=202, wet=0.28),
    "bgm_boss3.wav":  dict(bpm=188, steps=8, key=60, mode="phrygian", prog=[(0, "m"), (1, "M"), (0, "m"), (10, "m"), (8, "M"), (1, "M"), (3, "M"), (1, "M")],
                           rhythm=[1, 1, 2, 1, 1, 2, 4, 4], rhythm_b=[2, 2, 1, 1, 1, 1, 4, 4], lead="erhu", acc="shamisen", bass="gallop", drums="double", seed=303),
    "bgm_boss4.wav":  dict(bpm=112, steps=8, key=55, mode="insen", prog=[(0, "s"), (5, "s"), (10, "s"), (0, "s"), (1, "M"), (5, "s"), (7, "s"), (0, "s")],
                           rhythm=[4, 2, 2, 6, 2], rhythm_b=[3, 1, 4, 2, 2, 4], lead="shakuhachi", acc="guzheng", bass="long", drums="sparse", seed=404, wet=0.32),
    "bgm_boss5.wav":  dict(bpm=138, div=3, steps=6, key=54, mode="harmonic", prog=[(0, "m"), (8, "M"), (5, "m"), (7, "M"), (0, "m"), (3, "M"), (7, "M"), (7, "M")],
                           rhythm=[3, 2, 1, 3, 3], rhythm_b=[1, 1, 1, 3, 2, 1, 3], lead="erhu", acc="marimba68", bass="six8", drums="frame", seed=505, wet=0.26),
    "bgm_boss6.wav":  dict(bpm=176, steps=8, key=62, mode="minor", prog=[(0, "m"), (10, "M"), (8, "M"), (7, "M"), (0, "m"), (5, "m"), (3, "M"), (7, "M")],
                           rhythm=[4, 2, 2, 3, 1, 4], rhythm_b=[2, 2, 2, 2, 4, 4], lead="epic", acc="none", bed="brass", bass="octave8", drums="epic", seed=606, wet=0.27),
    "bgm_boss7.wav":  dict(bpm=196, steps=8, key=64, mode="mixolydian", swing=0.33, prog=[(0, "M"), (5, "M"), (0, "M"), (10, "M"), (5, "M"), (5, "M"), (0, "M"), (7, "M")],
                           rhythm=[1, 1, 2, 1, 3, 2, 2, 4], rhythm_b=[3, 1, 3, 1, 2, 2, 4], lead="square", acc="walking", bed="none", bass="walking", drums="swing", seed=707, wet=0.15),
    "bgm_boss8.wav":  dict(bpm=200, div=2, steps=7, key=52, mode="octatonic", prog=[(0, "m"), (1, "M"), (0, "m"), (6, "d"), (3, "m"), (4, "M"), (1, "M"), (6, "d")],
                           rhythm=[2, 2, 3, 2, 2, 3], rhythm_b=[1, 1, 2, 3, 4, 3], lead="perils", acc="seven", bed="sho", bass="dist7", drums="seven", seed=808, wet=0.22),
    "bgm_stage8.wav": dict(bpm=150, steps=8, key=52, mode="phrygdom", prog=[(0, "M"), (1, "M"), (0, "M"), (10, "m"), (8, "M"), (1, "M"), (0, "M"), (0, "M")],
                           rhythm=[3, 1, 2, 2, 4, 4], rhythm_b=[2, 2, 2, 2, 8], lead="shakuhachi", acc="shamisen", bass="octave8", drums="drive", seed=909, wet=0.24),
}

if __name__ == "__main__":
    only = sys.argv[1:]
    for n, st in THEMES.items():
        if not only or any(o in n for o in only):
            render(n, st)
