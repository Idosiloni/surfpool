"""
Builds every sound in Surf Pool Sim from code (no downloads): numpy -> 16-bit WAV in Assets/_Game/Audio/.

Run:  python3 tools/audio/make_sounds.py
Each sound is seeded, so re-running gives the same files. Tweak a recipe, re-run, and Unity re-imports.
Recipes follow docs/research/sound-design.md. Every file is normalized to about -1 dB and checked for clipping;
loops are built so their end flows into their start.
"""
import os
import wave

import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Audio")


# ---------- basics ----------

def secs(n):
    return np.arange(int(SR * n)) / SR


def rng(seed):
    return np.random.default_rng(seed)


def white(n_samples, seed):
    return rng(seed).standard_normal(n_samples)


def spectral(x, shape):
    """Filters x by multiplying its spectrum with shape(freqs). Zero-phase, wraps around (good for loops)."""
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    return np.fft.irfft(spec * shape(f), len(x))


def lowpass(x, cutoff, order=2):
    return spectral(x, lambda f: 1 / np.sqrt(1 + (f / cutoff) ** (2 * order)))


def highpass(x, cutoff, order=2):
    return spectral(x, lambda f: 1 / np.sqrt(1 + (cutoff / np.maximum(f, 1e-3)) ** (2 * order)))


def bandpass(x, lo, hi):
    return highpass(lowpass(x, hi), lo)


def pink(n_samples, seed):
    return spectral(white(n_samples, seed), lambda f: 1 / np.sqrt(np.maximum(f, 20)))


def sweep_lowpass(x, start_hz, end_hz, blocks=48):
    """Low-pass whose cutoff glides from start to end (block-wise with crossfaded overlaps)."""
    n = len(x)
    out = np.zeros(n)
    size = n // blocks + 1
    hop = size // 2
    window = np.hanning(size * 2)
    pos, i = 0, 0
    while pos < n:
        seg = x[pos:pos + size * 2]
        k = pos / max(1, n)
        cutoff = start_hz * (end_hz / start_hz) ** k
        filtered = lowpass(np.pad(seg, (0, size * 2 - len(seg))), cutoff)
        out[pos:pos + len(seg)] += (filtered * window)[:len(seg)]
        pos += hop
        i += 1
    return out


def fade(x, fade_in=0.005, fade_out=0.02):
    x = x.copy()
    a, b = int(SR * fade_in), int(SR * fade_out)
    if a:
        x[:a] *= np.linspace(0, 1, a)
    if b:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def loopify(x, overlap_sec=2.0):
    """Makes a seamless loop: the tail is crossfaded into the head, so the last sample flows into the first."""
    n = int(SR * overlap_sec)
    body = x[:-n].copy()
    ramp = np.linspace(0, 1, n)
    body[:n] = body[:n] * ramp + x[-n:] * (1 - ramp)
    return body


def tone(freq, length, partials=((1, 1.0),), decay=None):
    t = secs(length)
    phase = 2 * np.pi * np.cumsum(np.broadcast_to(freq, t.shape)) / SR
    y = sum(a * np.sin(phase * m) for m, a in partials)
    if decay:
        y *= np.exp(-t / decay)
    return y


def place(buffer, sound, at_sec, gain=1.0, wrap=False):
    start = int(at_sec * SR)
    if wrap:
        idx = (np.arange(len(sound)) + start) % len(buffer)
        np.add.at(buffer, idx, sound * gain)
    else:
        end = min(len(buffer), start + len(sound))
        buffer[start:end] += sound[:end - start] * gain


def stereo_widen(mono, seed, delay_ms=11):
    """Cheap stereo: right channel is a slightly delayed, decorrelated copy (for beds and music)."""
    d = int(SR * delay_ms / 1000)
    right = np.roll(mono, d) * 0.85 + lowpass(white(len(mono), seed), 8000) * 0.0
    return np.stack([mono, right], axis=1)


written = []


def save(name, x, peak_db=-1.0):
    x = np.asarray(x, dtype=np.float64)
    peak = np.max(np.abs(x)) + 1e-12
    x = x / peak * (10 ** (peak_db / 20))
    assert np.max(np.abs(x)) < 1.0, name
    channels = 1 if x.ndim == 1 else x.shape[1]
    pcm = (x * 32767).astype(np.int16)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    written.append((name, len(x) / SR, channels))


# ---------- karplus-strong pluck (vectorized per period) ----------

def pluck(freq, length=1.6, damp=0.995, seed=0, soft=2):
    period = max(2, int(round(SR / freq)))
    buf = rng(seed).uniform(-1, 1, period)
    for _ in range(soft):  # smoother noise burst = rounder, nylon-ish attack
        buf = 0.5 * (buf + np.roll(buf, -1))
    total = int(SR * length)
    out = np.empty(total)
    for k in range(0, total, period):
        chunk = buf[:min(period, total - k)]
        out[k:k + len(chunk)] = chunk
        buf = damp * 0.5 * (buf + np.roll(buf, -1))
    return fade(out - out.mean(), 0.002, 0.05)


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


# ---------- ambience ----------

def surf_loop():
    length = 34.0
    n = int(SR * length)
    t = np.arange(n) / SR
    bed = lowpass(pink(n, 1), 900)
    breath = 0.55 + 0.25 * np.sin(2 * np.pi * t / 8.5) + 0.2 * np.sin(2 * np.pi * t / 13.6 + 1)
    x = bed * breath
    r = rng(2)
    for at in np.arange(1.0, length - 6, 6.8) + r.uniform(-1, 1, 5)[:len(np.arange(1.0, length - 6, 6.8))]:
        swell_len = 6.0
        st = secs(swell_len)
        env = np.where(st < 2.6, (st / 2.6) ** 2, np.exp(-(st - 2.6) / 1.2))
        swell = sweep_lowpass(white(len(st), int(at * 100)), 700, 1800)[::-1] * env
        place(x, swell, at, 0.9)
    x = loopify(x, 2.0)
    save("amb_surf_loop", stereo_widen(x, 3))


def crowd_loop():
    length = 26.0
    n = int(SR * length)
    t = np.arange(n) / SR
    r = rng(10)
    murmur = bandpass(pink(n, 11), 280, 1400)
    mod = 0.6 + 0.4 * np.tanh(lowpass(r.standard_normal(n), 3) * 30)
    x = murmur * mod
    # faint voice-like blips (vowel-ish formants on a gliding buzz)
    for _ in range(60):
        at = r.uniform(0, length - 1)
        dur = r.uniform(0.15, 0.45)
        f0 = r.uniform(140, 300)
        st = secs(dur)
        glide = f0 * (1 + 0.15 * np.sin(np.pi * st / dur))
        buzz = np.sign(np.sin(2 * np.pi * np.cumsum(glide) / SR)) * 0.5
        voice = bandpass(buzz, 500, 1600) * np.sin(np.pi * st / dur) ** 2
        place(x, voice, at, r.uniform(0.05, 0.15), wrap=True)
    x = loopify(x, 1.5)
    save("amb_crowd_loop", stereo_widen(x, 12, 17))


# ---------- waves ----------

def wave_crash(name, seed, length, bright, dark, decay, rumble):
    n = int(SR * length)
    t = np.arange(n) / SR
    swell = 0.35
    env = np.where(t < swell, (t / swell) ** 3, np.exp(-(t - swell) / decay))
    body = sweep_lowpass(pink(n, seed), bright, dark) * env
    fizz_env = np.where(t < swell, (t / swell) ** 2, np.exp(-(t - swell) / (decay * 0.45)))
    fizz = highpass(white(n, seed + 1), 3000) * fizz_env
    low = lowpass(np.cumsum(white(n, seed + 2)), 150)
    low = highpass(low, 30) * env * (0.5 + 0.5 * np.sin(2 * np.pi * 0.3 * t))
    x = body / np.std(body) * 0.7 + fizz / np.std(fizz) * 0.18 + low / (np.std(low) + 1e-9) * rumble
    save(name, fade(x, 0.01, 0.4))


def wave_machine():
    # The machine pushing water: a deep thump, a hydraulic hum and a rising whoosh.
    length = 2.6
    t = secs(length)
    hum = tone(55, length, ((1, 1), (1.5, 0.5), (2, 0.3))) * (1 + 0.3 * np.sin(2 * np.pi * 0.7 * t))
    hum *= np.clip(t / 0.3, 0, 1) * np.exp(-np.maximum(t - 1.6, 0) / 0.4)
    thump = tone(48 * np.exp(-t * 3), length, decay=0.35)
    whoosh = sweep_lowpass(white(len(t), 31), 300, 2200)
    whoosh *= np.sin(np.pi * np.clip(t / length, 0, 1)) ** 2
    x = hum * 0.35 + thump * 0.9 + whoosh / np.std(whoosh) * 0.2
    save("wave_machine", fade(x, 0.005, 0.3))


def splash(name, seed, big):
    length = 1.1 if big else 0.7
    t = secs(length)
    noise = bandpass(white(len(t), seed), 800, 5000) * np.exp(-t / (0.28 if big else 0.18))
    x = noise / np.std(noise) * 0.5
    r = rng(seed + 5)
    for _ in range(10 if big else 6):
        d = r.uniform(0.02, 0.05)
        bt = secs(d)
        f = np.linspace(r.uniform(280, 400), r.uniform(800, 1100), len(bt))
        place(x, tone(f, d) * np.sin(np.pi * bt / d), r.uniform(0.05, 0.5), 0.35)
    if big:
        x += tone(80, length, decay=0.12) * 0.8
    save(name, fade(x, 0.002, 0.1))


# ---------- money & UI ----------

def bell(f, length, scale=1.0):
    t = secs(length)
    ratios = (1, 2.76, 5.4, 8.9)
    amps = (1, 0.6, 0.35, 0.2)
    taus = (0.35, 0.2, 0.12, 0.08)
    return sum(a * np.sin(2 * np.pi * f * r * t) * np.exp(-t / (tau * scale)) for r, a, tau in zip(ratios, amps, taus))


def coin(name, f):
    length = 0.8
    x = np.zeros(int(SR * length))
    click = highpass(white(int(SR * 0.003), 7), 2000)
    place(x, click, 0, 0.4)
    place(x, bell(f, 0.6), 0.0, 0.7)
    place(x, bell(f * 1.5, 0.7), 0.075, 0.9)
    save(name, fade(x, 0.001, 0.1))


def cash_big():
    # Register drawer + a cascade of coins: for big payments.
    length = 1.4
    x = np.zeros(int(SR * length))
    drawer = bandpass(white(int(SR * 0.12), 41), 400, 3000) * np.exp(-secs(0.12) / 0.04)
    place(x, drawer, 0, 0.5)
    place(x, bell(1320, 0.8), 0.04, 0.7)
    place(x, bell(1980, 0.9), 0.12, 0.9)
    r = rng(42)
    for i in range(7):
        place(x, bell(r.uniform(2400, 3600), 0.3, 0.5), 0.22 + i * 0.06 + r.uniform(0, 0.02), 0.25)
    save("cash_big", fade(x, 0.001, 0.2))


def ui_click():
    length = 0.06
    t = secs(length)
    f = np.linspace(950, 480, len(t))
    x = tone(f, length) * np.exp(-t / 0.012)
    place(x, highpass(white(int(SR * 0.003), 51), 3000), 0, 0.2)
    save("ui_click", fade(x, 0.0005, 0.01), peak_db=-3)


def ui_error():
    length = 0.28
    t = secs(length)
    x = np.zeros(len(t))
    for i, (a, b) in enumerate(((240, 200), (190, 150))):
        d = 0.12
        bt = secs(d)
        place(x, tone(np.linspace(a, b, len(bt)), d, ((1, 1), (2, 0.3))) * np.exp(-bt / 0.06), i * 0.13)
    save("ui_error", fade(x, 0.002, 0.03), peak_db=-3)


def note(f, length=0.6):
    t = secs(length)
    env = np.minimum(t / 0.003, 1) * (0.4 + 0.6 * np.exp(-t / 0.08)) * np.exp(-t / 0.35)
    return tone(f, length, ((1, 1), (2, 0.3), (3, 0.1))) * env


def arpeggio(name, notes, step, echo, sparkle, tail=0.7):
    length = step * len(notes) + tail
    x = np.zeros(int(SR * length))
    for i, n in enumerate(notes):
        place(x, note(midi(n)), i * step)
    if sparkle:
        sp = highpass(white(int(SR * 0.15), 61), 6000) * np.exp(-secs(0.15) / 0.04)
        place(x, sp, step * (len(notes) - 1), 0.3)
    if echo:
        d = int(SR * 0.18)
        x[d:] += x[:-d] * 0.35
    save(name, fade(x, 0.001, 0.15))


# ---------- park life ----------

def whistle():
    length = 0.85
    x = np.zeros(int(SR * length))
    for at, d in ((0.0, 0.22), (0.3, 0.5)):
        t = secs(d)
        f = 2800 + 140 * np.sin(2 * np.pi * 34 * t)  # the pea rattling
        tw = tone(f, d) * np.minimum(t / 0.015, 1) * np.minimum((d - t) / 0.06, 1)
        breath = bandpass(white(len(t), int(at * 10) + 71), 2000, 4000) * 0.15
        place(x, tw + breath * np.minimum((d - t) / 0.06, 1), at)
    save("whistle", fade(x, 0.002, 0.02))


def seagull(name, seed):
    r = rng(seed)
    calls = r.integers(2, 4)
    length = calls * 0.62 + 0.3
    x = np.zeros(int(SR * length))
    for c in range(calls):
        d = r.uniform(0.38, 0.55)
        t = secs(d)
        base = r.uniform(0.85, 1.15)
        k = t / d
        f = base * np.interp(k, [0, 0.15, 1], [1700, 2150, 1250]) + 40 * np.sin(2 * np.pi * 35 * t)
        env = np.minimum(t / 0.02, 1) * (1 - k) ** 1.5
        call = tone(f, d, ((1, 1), (2, 0.5), (3, 0.25))) * env
        place(x, bandpass(call, 900, 6000), c * 0.6 + r.uniform(0, 0.05))
    save(name, fade(x, 0.002, 0.05))


def cheer(name, seed):
    length = 1.8
    t = secs(length)
    r = rng(seed)
    env = np.minimum(t / 0.25, 1) * np.exp(-np.maximum(t - 0.4, 0) / 0.55)
    x = np.zeros(len(t))
    for v in range(12):
        f0 = r.uniform(200, 420) * (1 + 0.25 * np.minimum(t / 0.3, 1))  # "whoo" rises
        f0 = f0 * (1 + 0.02 * np.sin(2 * np.pi * r.uniform(4, 7) * t))
        saw = 2 * ((np.cumsum(f0) / SR) % 1) - 1
        voice = bandpass(saw, 600, 1300) + 0.5 * bandpass(saw, 1800, 2600)
        x += voice * r.uniform(0.5, 1)
    applause = bandpass(white(len(t), seed + 3), 1000, 6000)
    applause *= (r.random(len(t)) < 0.004).astype(float).cumsum() % 2 * 0.5 + 0.5
    x = x / np.std(x) + applause / np.std(applause) * 0.4
    save(name, fade(x * env, 0.01, 0.2))


# ---------- music ----------

CHORDS_A = [(60, 64, 67, 71), (57, 60, 64, 67), (53, 57, 60, 64), (55, 59, 62, 64)]  # Cmaj7 Am7 Fmaj7 G6
CHORDS_B = [(53, 57, 60, 64), (52, 55, 59, 62), (50, 53, 57, 60), (48, 52, 55, 59)]  # Fmaj7 Em7 Dm7 Cmaj7
PENTA = [72, 74, 76, 79, 81, 84]


def music(name, chords, seed, bpm=90, bars=16):
    beat = 60 / bpm
    bar = beat * 4
    length = bar * bars
    n = int(SR * length)
    x = np.zeros(n)
    r = rng(seed)
    melody_rest = 0.45
    for b in range(bars):
        chord = chords[b % len(chords)]
        t0 = b * bar
        # strummed uke on 1 and the "and" of 2, a light upstroke on 4
        for at, gain, down in ((0, 0.55, True), (beat * 1.5, 0.45, True), (beat * 3, 0.3, False)):
            order = chord if down else chord[::-1]
            for i, m in enumerate(order):
                place(x, pluck(midi(m), 1.4, 0.994, seed * 100 + b * 10 + i), t0 + at + i * 0.022, gain / 4, wrap=True)
        # bass on 1 and 3
        root = chord[0] - 24
        place(x, pluck(midi(root), 1.2, 0.997, seed + b), t0, 0.5, wrap=True)
        place(x, pluck(midi(root + 7), 1.0, 0.997, seed + b + 50), t0 + beat * 2, 0.35, wrap=True)
        # marimba-ish melody, rests often
        for s in range(8):
            if r.random() < melody_rest or s % 2 == 1 and r.random() < 0.6:
                continue
            m = PENTA[r.integers(0, len(PENTA))]
            tt = secs(0.7)
            mallet = (np.sin(2 * np.pi * midi(m) * tt) + 0.25 * np.sin(2 * np.pi * midi(m) * 4 * tt)) * np.exp(-tt / 0.22)
            place(x, mallet, t0 + s * beat / 2, 0.16, wrap=True)
        # shaker on every offbeat 8th
        for s in range(8):
            sh = highpass(white(int(SR * 0.05), b * 8 + s), 5000) * np.exp(-secs(0.05) / 0.015)
            place(x, sh, t0 + s * beat / 2 + beat / 4, 0.05 if s % 2 else 0.08, wrap=True)
        # soft pad
        pad_t = secs(bar)
        pad = sum(np.sin(2 * np.pi * midi(m) * pad_t) for m in chord) * np.sin(np.pi * pad_t / bar) ** 2
        place(x, pad, t0, 0.025, wrap=True)
    x = lowpass(x, 7000)
    save(name, stereo_widen(x, seed, 9))


def main():
    surf_loop()
    crowd_loop()
    wave_machine()
    wave_crash("wave_crash_small", 101, 2.6, 1800, 700, 0.6, 0.15)
    wave_crash("wave_crash_medium", 102, 3.6, 1500, 500, 0.95, 0.3)
    wave_crash("wave_crash_big", 103, 5.0, 1200, 300, 1.5, 0.55)
    splash("splash_1", 201, False)
    splash("splash_2", 202, False)
    splash("splash_big", 203, True)
    coin("coin_1", 1320)
    coin("coin_2", 1400)
    coin("coin_3", 1250)
    cash_big()
    ui_click()
    ui_error()
    arpeggio("ui_buy", [72, 76, 79, 84], 0.08, False, True)
    arpeggio("mission_complete", [72, 76, 79, 84, 88, 91], 0.1, True, True, 1.0)
    arpeggio("day_end", [79, 76, 72, 67, 64, 60], 0.16, True, False, 1.2)
    whistle()
    seagull("seagull_1", 301)
    seagull("seagull_2", 302)
    seagull("seagull_3", 303)
    cheer("cheer_1", 401)
    cheer("cheer_2", 402)
    music("music_sunny", CHORDS_A, 7)
    music("music_lazy", CHORDS_B, 8)
    for name, length, ch in written:
        print(f"{name:20s} {length:6.2f}s  {'stereo' if ch == 2 else 'mono'}")


if __name__ == "__main__":
    main()
