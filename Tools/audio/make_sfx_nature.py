"""Two attack sounds for Starfall Danmaku, synthesized from code (numpy + scipy), written to Godot/Audio:

  sfx_thunder.wav  嵐's lightning strike: a sharp crack, a burst of crackle, then a low rolling rumble
  sfx_bamboo.wav      墨竹's bamboo shoot rising: a fast wind whoosh
  sfx_bamboo_hit.wav  the shoot hitting the top: a thump against a wall

    python Tools/audio/make_sfx_nature.py
"""
import os, wave
import numpy as np
from scipy.signal import butter, lfilter

SR = 44100
OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Godot", "Audio"))
rng = np.random.default_rng(20261113)


def filt(x, kind, f, order=2):
    b, a = butter(order, np.array(f) / (SR / 2), kind)
    return lfilter(b, a, x)


def t(dur):
    return np.arange(int(dur * SR)) / SR


def at(buf, sig, start):
    i = int(start * SR)
    n = min(len(sig), len(buf) - i)
    buf[i:i + n] += sig[:n]


def save(name, x, peak=0.9):
    x = filt(filt(x, "low", 3800, 2), "low", 6000, 2)   # roll off the top end, like the other sound effects
    x = x / (np.max(np.abs(x)) + 1e-9) * peak
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype(np.int16).tobytes())
    print("wrote", name, "%.2fs" % (len(x) / SR))


def thunder():
    dur = 2.2
    buf = np.zeros(int(dur * SR))
    # the crack: a very short, bright noise snap with a few fast echoes
    n = rng.standard_normal(int(0.06 * SR))
    snap = filt(n, "high", 1800) * np.exp(-t(0.06) * 70)
    for k, (dt, g) in enumerate(((0.0, 1.0), (0.012, 0.7), (0.031, 0.5), (0.055, 0.35))):
        at(buf, snap * g, dt)
    # crackle: random sparks over the first 0.35 s
    for _ in range(60):
        s = rng.uniform(0.0, 0.35)
        L = int(rng.uniform(0.002, 0.008) * SR)
        spark = rng.standard_normal(L) * np.exp(-np.arange(L) / (L / 4)) * rng.uniform(0.2, 0.6) * np.exp(-s * 6)
        at(buf, filt(spark, "high", 2500), s)
    # the boom and the roll: low noise swelling then rumbling away in uneven waves
    tt = t(dur)
    rum = filt(rng.standard_normal(len(tt)), "low", 160, 3)
    roll = (1 + 0.6 * np.sin(2 * np.pi * 3.1 * tt) * np.sin(2 * np.pi * 0.9 * tt + 1.0))
    env = np.minimum(1, tt / 0.05) * np.exp(-tt * 1.6) * roll
    buf += rum * env * 3.2
    body = filt(rng.standard_normal(len(tt)), "band", [200, 900]) * np.minimum(1, tt / 0.02) * np.exp(-tt * 5)
    buf += body * 0.9
    return buf


def bamboo_rise():
    """Something shooting past very fast: a wind whoosh that swells and sweeps up in pitch (about the 0.5 s rise)."""
    dur = 0.6
    tt = t(dur)
    noise = rng.standard_normal(len(tt))
    out = np.zeros(len(tt))
    step = 256
    for i0 in range(0, len(tt), step):          # band-pass sweep 300 Hz -> 2.4 kHz
        k = i0 / len(tt)
        f = 300 + 2100 * k ** 1.3
        n = min(step, len(tt) - i0)
        seg = filt(noise[max(0, i0 - 1024):i0 + n], "band", [f * 0.6, f * 1.6])[-n:]
        out[i0:i0 + n] = seg
    env = np.sin(np.pi * np.clip(tt / dur, 0, 1)) ** 1.5 * (0.4 + 0.6 * tt / dur)
    air = filt(rng.standard_normal(len(tt)), "high", 4000) * env * 0.25    # airy hiss on top
    return out * env + air


def bamboo_hit():
    """The shoot slamming into the top: a solid thump against a wall with a short knock and a little rattle."""
    dur = 0.45
    tt = t(dur)
    thump = np.sin(2 * np.pi * (95 - 40 * tt) * tt) * np.exp(-tt * 18)
    body = filt(rng.standard_normal(len(tt)), "low", 900, 3) * np.exp(-tt * 30) * 1.4
    knock = np.sin(2 * np.pi * 330 * tt) * np.exp(-tt * 45) * 0.5
    click = filt(rng.standard_normal(len(tt)), "high", 2500) * np.exp(-tt * 260) * 0.7
    rattle = filt(rng.standard_normal(len(tt)), "band", [1200, 4000]) * np.exp(-((tt - 0.06) / 0.05) ** 2) * 0.25
    return thump * 1.3 + body + knock + click + rattle


if __name__ == "__main__":
    save("sfx_thunder.wav", thunder())
    save("sfx_bamboo.wav", bamboo_rise(), 0.8)
    save("sfx_bamboo_hit.wav", bamboo_hit(), 0.9)
