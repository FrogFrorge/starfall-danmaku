"""Synthesizes all sound effects and background music for Starfall Danmaku into ../../Audio/*.wav
    python Tools/audio/make_audio.py
Everything is generated from code (numpy + scipy), so it is original and free to use.
Replace any file with your own WAV (16-bit PCM, any rate, mono or stereo) using the same name.
"""
import math
import os
import wave

import numpy as np
from scipy.signal import lfilter, butter

SR = 44100
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Audio"))
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(20261107)


# ----------------------------------------------------------------------------------------
# helpers
# ----------------------------------------------------------------------------------------
def t_axis(dur):
    return np.arange(int(dur * SR)) / SR


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def lowpass(x, cutoff, order=2):
    b, a = butter(order, min(cutoff / (SR / 2), 0.99), "low")
    return lfilter(b, a, x)


def highpass(x, cutoff, order=2):
    b, a = butter(order, min(cutoff / (SR / 2), 0.99), "high")
    return lfilter(b, a, x)


def bandpass(x, lo, hi, order=2):
    b, a = butter(order, [lo / (SR / 2), min(hi / (SR / 2), 0.99)], "band")
    return lfilter(b, a, x)


def env_adsr(n, a=0.005, d=0.05, s=0.6, r=0.05, hold=None):
    a_n, d_n, r_n = int(a * SR), int(d * SR), int(r * SR)
    hold_n = n - a_n - d_n - r_n if hold is None else int(hold * SR)
    hold_n = max(0, hold_n)
    e = np.concatenate([np.linspace(0, 1, a_n, endpoint=False), np.linspace(1, s, d_n, endpoint=False),
                        np.full(hold_n, s), np.linspace(s, 0, r_n)])
    if len(e) < n:
        e = np.pad(e, (0, n - len(e)))
    return e[:n]


def reverb(x, wet=0.25, room=0.82):
    """Small Schroeder reverb (4 combs + 2 allpasses)."""
    out = np.zeros_like(x)
    for d in (1557, 1617, 1491, 1422):
        a = np.zeros(d + 1); a[0] = 1; a[d] = -room
        out += lfilter([1], a, x)
    out /= 4
    for d, g in ((225, 0.5), (556, 0.5)):
        b = np.zeros(d + 1); b[0] = -g; b[d] = 1
        a = np.zeros(d + 1); a[0] = 1; a[d] = -g
        out = lfilter(b, a, out)
    return x * (1 - wet) + lowpass(out, 6000) * wet


def normalize(x, peak=0.89):
    m = np.max(np.abs(x)) or 1.0
    return x / m * peak


def save(name, x, stereo=False):
    x = np.clip(x, -1, 1)
    data = (x * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(2 if stereo else 1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print("wrote", name, "%.2fs" % (len(x) / SR / (2 if stereo else 1)))


# ----------------------------------------------------------------------------------------
# instruments
# ----------------------------------------------------------------------------------------
def koto(freq, dur, decay=0.996, bright=5000):
    """Karplus-Strong plucked string."""
    n = int(dur * SR)
    N = max(2, int(SR / freq))
    x = np.zeros(n)
    burst = rng.uniform(-1, 1, N)
    x[:N] = lowpass(burst, bright)
    a = np.zeros(N + 2); a[0] = 1; a[N] = -decay * 0.5; a[N + 1] = -decay * 0.5
    y = lfilter([1], a, x)
    y *= env_adsr(n, 0.001, 0.0, 1.0, min(0.05, dur * 0.3))
    return y


def flute(freq, dur, vib=0.004):
    t = t_axis(dur)
    v = 1 + vib * np.sin(2 * np.pi * 5.2 * t) * np.clip((t - 0.12) * 5, 0, 1)
    ph = 2 * np.pi * np.cumsum(freq * v) / SR
    tone = np.sin(ph) + 0.35 * np.sin(2 * ph) + 0.12 * np.sin(3 * ph)
    breath = lowpass(rng.uniform(-1, 1, len(t)), 2500) * 0.08
    return (tone + breath) * env_adsr(len(t), 0.05, 0.08, 0.8, 0.08)


def bass(freq, dur):
    t = t_axis(dur)
    y = sum(np.sin(2 * np.pi * freq * k * t) / k for k in range(1, 9))
    y = lowpass(y, 900)
    return y * env_adsr(len(t), 0.004, 0.12, 0.5, 0.04)


def pad(freqs, dur):
    t = t_axis(dur)
    y = np.zeros_like(t)
    for f in freqs:
        for det in (-0.004, 0.0, 0.004):
            ph = 2 * np.pi * f * (1 + det) * t
            y += sum(np.sin(k * ph) / k for k in range(1, 6))
    y = lowpass(y, 1600)
    return y * env_adsr(len(t), 0.25, 0.2, 0.8, 0.3) / (len(freqs) * 3)


def bell(freq, dur, index=2.5, ratio=1.4):
    t = t_axis(dur)
    mod = index * np.exp(-t * 3) * np.sin(2 * np.pi * freq * ratio * t)
    return np.sin(2 * np.pi * freq * t + mod) * np.exp(-t * 2.5)


def kick(dur=0.35):
    t = t_axis(dur)
    f = 45 + 90 * np.exp(-t * 30)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 10)


def taiko(dur=0.6):
    t = t_axis(dur)
    f = 55 + 40 * np.exp(-t * 18)
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 6)
    thump = lowpass(rng.uniform(-1, 1, len(t)), 400) * np.exp(-t * 25) * 0.6
    return body + thump


def snare(dur=0.2):
    t = t_axis(dur)
    n = bandpass(rng.uniform(-1, 1, len(t)), 1500, 8000) * np.exp(-t * 22)
    return n + 0.4 * np.sin(2 * np.pi * 185 * t) * np.exp(-t * 30)


def hat(dur=0.05):
    t = t_axis(dur)
    return highpass(rng.uniform(-1, 1, len(t)), 7000) * np.exp(-t * 70) * 0.5


def mix_at(buf, sig, start_s, gain=1.0):
    i = int(start_s * SR)
    if i >= len(buf):
        return
    j = min(len(buf), i + len(sig))
    buf[i:j] += sig[: j - i] * gain


# ----------------------------------------------------------------------------------------
# music
# ----------------------------------------------------------------------------------------
def chord_notes(root, minor=True):
    return [root, root + (3 if minor else 4), root + 7]


def compose_melody(scale, chords, bars, seed, steps_per_bar=8, rest_prob=0.12):
    """Random-walk melody in `scale` (midi notes), chord tones on strong beats, 2-bar motif repetition."""
    r = np.random.default_rng(seed)
    motif = []
    idx = len(scale) // 2
    for s in range(steps_per_bar * 2):
        idx = int(np.clip(idx + r.choice([-2, -1, -1, 0, 1, 1, 2]), 0, len(scale) - 1))
        motif.append(None if r.random() < rest_prob else idx)
    notes = []
    for b in range(bars):
        root = chords[b % len(chords)]
        tones = chord_notes(root % 12)
        variation = (b // 2) % 4
        for s in range(steps_per_bar):
            m = motif[(b % 2) * steps_per_bar + s]
            if m is None:
                notes.append(None)
                continue
            i = int(np.clip(m + (variation in (1, 3)) * (1 if s % 4 == 2 else 0) + (variation == 2) * 2, 0, len(scale) - 1))
            n = scale[i]
            if s % 4 == 0:  # snap strong beats to the nearest chord tone
                cands = [x for x in scale if x % 12 in tones]
                n = min(cands, key=lambda c: abs(c - n)) if cands else n
            notes.append(n)
    return notes


def render_track(name, bpm, chords, scale, bars, lead="koto", drums=True, intense=False, title=False, seed=1):
    beat = 60.0 / bpm
    bar = beat * 4
    length = bars * bar
    tail = 3.0
    buf = np.zeros(int((length + tail) * SR))
    step = bar / 8
    melody = compose_melody(scale, chords, bars, seed, 8, 0.25 if title else 0.12)

    for b in range(bars):
        t0 = b * bar
        root = chords[b % len(chords)]
        # pad
        mix_at(buf, pad([midi(n) for n in chord_notes(root)], bar + 0.3), t0, 0.22 if title else 0.18)
        if title:
            # slow koto arpeggio
            arp = chord_notes(root + 12) + [root + 24]
            for k in range(8):
                mix_at(buf, koto(midi(arp[k % 4]), 1.2, 0.997), t0 + k * step, 0.22)
        else:
            # bass
            bstep = step / (2 if intense else 1)
            for k in range(16 if intense else 8):
                n = root - 12 + (12 if k % 4 == 2 else 0)
                mix_at(buf, bass(midi(n), bstep * 0.95), t0 + k * bstep, 0.35)
            # arpeggio
            if intense:
                arp = chord_notes(root + 12) + [root + 19]
                for k in range(16):
                    mix_at(buf, koto(midi(arp[k % 4] + (12 if k % 8 >= 4 else 0)), 0.25, 0.99, 7000), t0 + k * step / 2, 0.13)
        # drums
        if drums:
            for k in range(8):
                ts = t0 + k * step
                if k in (0, 4) or (intense and k in (3, 6)):
                    mix_at(buf, kick(), ts, 0.6)
                if k in (2, 6):
                    mix_at(buf, snare(), ts, 0.32)
                mix_at(buf, hat(), ts + (step * 0.5 if intense else 0), 0.25)
                mix_at(buf, hat(), ts, 0.2)
            if b % 4 == 3:
                for k in range(4):
                    mix_at(buf, taiko(), t0 + bar * 0.5 + k * step, 0.5)
            if b % 8 == 0:
                mix_at(buf, taiko(), t0, 0.7)

    # lead melody (with flute doubling in the second half)
    for i, n in enumerate(melody):
        if n is None:
            continue
        ts = i * step
        dur = step * (2.2 if title else 1.6)
        b = i // 8
        if lead == "koto" or (b % 8) < 4:
            mix_at(buf, koto(midi(n), dur + 0.4, 0.995 if not title else 0.997), ts, 0.33)
        if lead == "flute" or (b % 8) >= 4:
            mix_at(buf, flute(midi(n + 12 if title else n), dur), ts, 0.22 if not title else 0.16)
    # sparkle bells on phrase starts
    for b in range(0, bars, 4):
        mix_at(buf, bell(midi(chords[b % len(chords)] + 36), 2.0), b * bar, 0.08)

    buf = reverb(buf, 0.28 if title else 0.2)
    # wrap the tail onto the start so the loop is seamless
    n_loop = int(length * SR)
    out = buf[:n_loop].copy()
    tail_part = buf[n_loop:]
    out[: len(tail_part)] += tail_part
    out = np.tanh(normalize(out, 0.95) * 1.2) / np.tanh(1.2)
    save(name, normalize(out, 0.8))


def make_music():
    # D minor with a Japanese colour (b2 passing tone), 150 bpm stage theme
    d_scale = [62, 63, 65, 67, 69, 70, 72, 74, 75, 77, 79, 81]
    render_track("bgm_stage.wav", 150, [62, 58, 60, 57, 55, 58, 60, 62], d_scale, 32, lead="koto", seed=7)
    # A minor, faster and denser boss theme
    a_scale = [69, 71, 72, 74, 76, 77, 79, 81, 83, 84, 86, 88]
    render_track("bgm_boss.wav", 168, [57, 53, 55, 52, 50, 53, 55, 52], a_scale, 32, lead="flute", intense=True, seed=11)
    # calm pentatonic title theme
    pent = [62, 65, 67, 69, 72, 74, 77, 79, 81]
    render_track("bgm_title.wav", 76, [62, 58, 55, 57], pent, 12, lead="koto", drums=False, title=True, seed=3)
    make_music_stage2()


def make_music_stage2():
    # Stage 2 (four seasons): lighter E minor pentatonic road theme, flowing B minor boss theme
    e_scale = [64, 67, 69, 71, 74, 76, 79, 81, 83, 86]
    render_track("bgm_stage2.wav", 140, [64, 60, 62, 59, 57, 60, 62, 64], e_scale, 32, lead="flute", seed=21)
    b_scale = [71, 73, 74, 76, 78, 79, 81, 83, 85, 86, 88]
    render_track("bgm_boss2.wav", 164, [59, 55, 57, 54, 52, 55, 57, 54], b_scale, 32, lead="koto", intense=True, seed=29)


def make_music_later():
    # Stage 3 (storm): C minor, driving
    c_scale = [60, 62, 63, 65, 67, 68, 70, 72, 74, 75, 77, 79]
    render_track("bgm_stage3.wav", 152, [60, 56, 58, 55, 53, 56, 58, 60], c_scale, 32, lead="koto", seed=31)
    render_track("bgm_boss3.wav", 172, [60, 58, 56, 55, 53, 55, 56, 58], c_scale, 32, lead="flute", intense=True, seed=37)
    # Stage 4 (scholar's garden): G minor pentatonic, elegant
    g_pent = [67, 70, 72, 74, 77, 79, 82, 84, 86]
    render_track("bgm_stage4.wav", 132, [55, 51, 53, 50, 48, 51, 53, 55], g_pent, 32, lead="flute", seed=41)
    render_track("bgm_boss4.wav", 160, [55, 53, 51, 50, 48, 50, 51, 53], g_pent, 32, lead="koto", intense=True, seed=43)
    # Stage 5 (five elements): F# minor, mysterious
    fs_scale = [66, 68, 69, 71, 73, 74, 76, 78, 80, 81, 83]
    render_track("bgm_stage5.wav", 144, [54, 50, 52, 49, 47, 50, 52, 54], fs_scale, 32, lead="koto", seed=47)
    render_track("bgm_boss5.wav", 170, [54, 52, 50, 49, 47, 49, 50, 52], fs_scale, 32, lead="flute", intense=True, seed=53)
    # Stage 6 (celestial palace): D minor, grand final boss
    d_scale = [62, 64, 65, 67, 69, 70, 72, 74, 76, 77, 79, 81]
    render_track("bgm_stage6.wav", 148, [62, 58, 60, 57, 55, 58, 60, 62], d_scale, 32, lead="flute", seed=59)
    render_track("bgm_boss6.wav", 176, [62, 60, 58, 57, 55, 57, 58, 60], d_scale, 40, lead="koto", intense=True, seed=61)
    # Extra (mahjong parlour): E minor, playful and fast
    e_scale = [64, 66, 67, 69, 71, 72, 74, 76, 78, 79, 81]
    render_track("bgm_stage7.wav", 156, [64, 60, 62, 59, 57, 60, 62, 64], e_scale, 32, lead="koto", seed=67)
    render_track("bgm_boss7.wav", 180, [64, 62, 60, 59, 57, 59, 60, 62], e_scale, 40, lead="flute", intense=True, seed=71)


# ----------------------------------------------------------------------------------------
# sound effects
# ----------------------------------------------------------------------------------------
def sweep(f0, f1, dur, shape="sin", curve=3.0):
    t = t_axis(dur)
    f = f1 + (f0 - f1) * np.exp(-t * curve / dur)
    ph = 2 * np.pi * np.cumsum(f) / SR
    if shape == "square":
        return np.sign(np.sin(ph)) * 0.6
    if shape == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    return np.sin(ph)


def save_sfx(name, x):
    """Every sound effect goes through this: the top end is rolled off so nothing sounds shrill when it repeats
    hundreds of times a minute (shots, grazes, items)."""
    save(name, normalize(lowpass(lowpass(x, 3800, 2), 6000, 2), 0.85))


def make_sfx():
    t = t_axis
    # player shot: soft papery tick
    d = 0.05; x = highpass(rng.uniform(-1, 1, len(t(d))), 2500) * np.exp(-t(d) * 90) * 0.6 + sweep(1900, 1200, d) * np.exp(-t(d) * 70) * 0.4
    save_sfx("sfx_shot.wav", normalize(x, 0.6))
    # enemy shot: short glassy "tan"
    d = 0.09; x = sweep(1100, 700, d, "tri") * np.exp(-t(d) * 40) + bell(2200, d, 1.0, 2.0) * 0.3
    save_sfx("sfx_enemy_shot.wav", normalize(x, 0.6))
    # graze
    d = 0.06; x = bandpass(rng.uniform(-1, 1, len(t(d))), 5000, 12000) * np.exp(-t(d) * 60) + np.sin(2 * np.pi * 3200 * t(d)) * np.exp(-t(d) * 80) * 0.4
    save_sfx("sfx_graze.wav", normalize(x, 0.6))
    # item pickup
    d = 0.14; x = bell(midi(96), d, 1.2, 2.0) + 0.5 * bell(midi(103), d, 1.0, 2.0)
    save_sfx("sfx_item.wav", normalize(x * env_adsr(len(t(d)), 0.002, 0.03, 0.6, 0.06), 0.6))
    # enemy destroyed: paper burst
    d = 0.3; x = lowpass(rng.uniform(-1, 1, len(t(d))), 3000) * np.exp(-t(d) * 14) + sweep(500, 90, d) * np.exp(-t(d) * 12) * 0.7
    save_sfx("sfx_enemy_die.wav", normalize(reverb(x, 0.2), 0.8))
    # player hit: descending "pichuun"
    d = 0.7
    x = sweep(1400, 160, d, "square", 2.0) * (1 + 0.3 * np.sin(2 * np.pi * 28 * t(d))) * np.exp(-t(d) * 3.5)
    x += lowpass(rng.uniform(-1, 1, len(t(d))), 2500) * np.exp(-t(d) * 6) * 0.4
    save_sfx("sfx_player_die.wav", normalize(reverb(lowpass(x, 5000), 0.3), 0.85))
    # bomb: deep boom + rising shimmer
    d = 2.0
    boom = sweep(90, 35, d, "sin", 4.0) * np.exp(-t(d) * 2.2)
    rumble = lowpass(rng.uniform(-1, 1, len(t(d))), 300) * np.exp(-t(d) * 2.0) * 1.2
    shimmer = np.zeros(len(t(d)))
    for k, n in enumerate([74, 77, 81, 84, 86, 89]):
        mix_at(shimmer, bell(midi(n), 1.4, 2.0, 2.0), 0.08 * k, 0.25)
    save_sfx("sfx_bomb.wav", normalize(reverb(boom + rumble + shimmer, 0.35), 0.9))
    # spell card declaration: swelling noise + upward bell arpeggio
    d = 1.6
    swell = bandpass(rng.uniform(-1, 1, len(t(d))), 800, 6000) * np.clip(t(d) / 0.5, 0, 1) * np.exp(-np.clip(t(d) - 0.5, 0, None) * 5) * 0.5
    arp = np.zeros(len(t(d)))
    for k, n in enumerate([69, 72, 76, 79, 83, 88]):
        mix_at(arp, bell(midi(n), 1.2, 3.0, 1.4), 0.35 + 0.07 * k, 0.3)
    mix_at(arp, taiko(1.0), 0.35, 0.8)
    save_sfx("sfx_spell.wav", normalize(reverb(swell + arp, 0.35), 0.85))
    # spell card captured: bright fanfare
    d = 1.4
    x = np.zeros(len(t(d)))
    for k, n in enumerate([72, 76, 79, 84]):
        mix_at(x, koto(midi(n), 1.0, 0.997, 7000), 0.09 * k, 0.5)
        mix_at(x, bell(midi(n + 12), 1.0, 1.5, 2.0), 0.09 * k, 0.25)
    mix_at(x, pad([midi(72), midi(76), midi(79)], 1.1), 0.36, 0.6)
    save_sfx("sfx_spell_get.wav", normalize(reverb(x, 0.3), 0.85))
    # phase break / bullets cancelled: glassy sweep
    d = 0.9
    x = bandpass(rng.uniform(-1, 1, len(t(d))), 2000, 12000) * np.exp(-t(d) * 5) * 0.5
    for k in range(10):
        f = rng.uniform(1500, 5000)
        mix_at(x, np.sin(2 * np.pi * f * t(0.6)) * np.exp(-t(0.6) * rng.uniform(5, 12)), rng.uniform(0, 0.2), 0.15)
    save_sfx("sfx_break.wav", normalize(reverb(x, 0.3), 0.75))
    # menu
    d = 0.09; save_sfx("sfx_menu_move.wav", normalize(koto(midi(84), d + 0.1, 0.99, 8000)[: len(t(d + 0.05))], 0.5))
    d = 0.35; x = np.zeros(len(t(d))); mix_at(x, bell(midi(79), 0.3, 1.5, 2.0), 0); mix_at(x, bell(midi(86), 0.25, 1.5, 2.0), 0.07)
    save_sfx("sfx_menu_ok.wav", normalize(x, 0.6))
    d = 0.25; save_sfx("sfx_menu_back.wav", normalize(koto(midi(67), d, 0.99, 4000) * 0.8 + koto(midi(62), d, 0.99, 4000) * 0.4, 0.5))
    d = 0.4; save_sfx("sfx_pause.wav", normalize(bell(midi(74), d, 1.0, 1.0), 0.5))
    # Sou's bow laser: seamless 1 s loop (all components periodic in 1 s)
    tt = t_axis(1.0)
    hum = (np.sin(2 * np.pi * 110 * tt) * 0.5 + np.sin(2 * np.pi * 220 * tt) * 0.35 + np.sin(2 * np.pi * 663 * tt) * 0.15)
    hum *= 0.75 + 0.25 * np.sin(2 * np.pi * 30 * tt)
    crackle = np.sin(2 * np.pi * 1760 * tt) * (0.5 + 0.5 * np.sin(2 * np.pi * 7 * tt)) * 0.12
    save_sfx("sfx_laser_loop.wav", normalize(hum + crackle, 0.5))


if __name__ == "__main__":
    make_sfx()
    make_music()
