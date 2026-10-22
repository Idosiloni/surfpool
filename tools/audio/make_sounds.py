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


def wave_boom():
    """A Giant wave's lip landing: a deep thump you feel more than hear, then a long rolling rumble."""
    length = 3.6
    t = secs(length)
    thump = tone(62 * np.exp(-t * 1.6) + 30, length, ((1, 1), (2, 0.25)), decay=0.45)
    rumble = lowpass(pink(len(t), 141), 220, order=3)
    rumble = rumble / np.std(rumble) * np.where(t < 0.08, t / 0.08, np.exp(-(t - 0.08) / 1.3))
    rumble *= 0.8 + 0.2 * np.sin(2 * np.pi * 2.3 * t)  # the roll of water tumbling
    crack = bandpass(white(len(t), 142), 500, 3500) * np.exp(-t / 0.18)
    hiss = highpass(white(len(t), 143), 2500) * np.where(t < 0.3, t / 0.3, np.exp(-(t - 0.3) / 0.9))
    x = thump * 1.0 + rumble * 0.45 + crack / np.std(crack) * 0.15 + hiss / np.std(hiss) * 0.06
    save("wave_boom", fade(x, 0.002, 0.5))


def section_wall():
    """The wall standing up ahead of you: water sucked up the face, a rising airy whoosh over a low swell."""
    length = 1.8
    t = secs(length)
    env = np.sin(np.pi * np.clip(t / length, 0, 1)) ** 1.5
    whoosh = sweep_lowpass(highpass(pink(len(t), 151), 200), 400, 2600) * env
    swell = lowpass(pink(len(t), 152), 160) * env
    x = whoosh / np.std(whoosh) * 0.6 + swell / np.std(swell) * 0.35
    save("section_wall", fade(x, 0.05, 0.3))


def tube_roar():
    """Inside the barrel: a hollow roar, the noise of the breaking lip ringing in a tube of water (comb resonance)."""
    length = 3.0
    t = secs(length)
    noise = lowpass(pink(len(t), 161), 2400)
    roar = noise.copy()
    for delay_ms, gain in ((4.1, 0.7), (6.3, 0.5), (9.7, 0.35)):  # a tube rings at a few low pitches
        d = int(SR * delay_ms / 1000)
        roar[d:] += noise[:-d] * gain
    roar = bandpass(roar, 90, 1800)
    env = np.minimum(1, t / 0.5) * np.where(t > length - 0.9, (length - t) / 0.9, 1)
    swirl = 0.75 + 0.25 * np.sin(2 * np.pi * 0.9 * t + 1.3 * np.sin(2 * np.pi * 0.37 * t))
    low = lowpass(pink(len(t), 162), 120)
    x = roar / np.std(roar) * 0.6 * env * swirl + low / np.std(low) * 0.3 * env
    save("tube_roar", fade(x, 0.02, 0.3))


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
CHORDS_C = [(57, 60, 64, 67), (53, 57, 60, 64), (48, 52, 55, 59), (55, 59, 62, 65)]  # Am7 Fmaj7 Cmaj7 G7
PENTA = [72, 74, 76, 79, 81, 84]


def music(name, chords, seed, bpm=76, bars=16):
    """Calm background music: a soft pad and a fingerpicked guitar (one note at a time, no drums), warm and quiet.
    Not a loop: it fades in and out, and the game leaves long beach-only breaks between songs."""
    beat = 60 / bpm
    bar = beat * 4
    length = bar * bars + 3.0
    n = int(SR * length)
    x = np.zeros(n)
    r = rng(seed)
    pattern = [0, 2, 1, 3, 2, 1]  # gentle arpeggio order over the chord tones
    for b in range(bars):
        chord = chords[b % len(chords)]
        t0 = b * bar
        # bass note on beat 1, softly
        place(x, pluck(midi(chord[0] - 12), 2.4, 0.998, seed + b, soft=4), t0, 0.35)
        # fingerpicked eighth-note arpeggio, a little humanized
        for k in range(8):
            if k == 7 and r.random() < 0.5:
                continue
            m = chord[pattern[k % len(pattern)]] + (12 if k in (3, 5) else 0)
            at = max(0.0, t0 + k * beat / 2 + r.uniform(-0.012, 0.012))
            place(x, pluck(midi(m), 1.8, 0.996, seed * 100 + b * 10 + k, soft=4), at, r.uniform(0.11, 0.16))
        # warm pad
        pad_t = secs(bar + 0.5)
        pad = sum(np.sin(2 * np.pi * midi(m - 12) * pad_t) * 0.6 + np.sin(2 * np.pi * midi(m) * pad_t) * 0.4 for m in chord[:3])
        place(x, pad * np.sin(np.pi * np.clip(pad_t / (bar + 0.5), 0, 1)) ** 2, t0, 0.03)
    x = lowpass(x, 3200)
    # fade in and out
    fade_len = int(SR * 3)
    x[:fade_len] *= np.linspace(0, 1, fade_len)
    x[-fade_len:] *= np.linspace(1, 0, fade_len)
    save(name, stereo_widen(x, seed, 9), peak_db=-3)


# ---------- a park that grows (ambience step) ----------

def wind_loop():
    """Soft sea breeze: a low rumble plus an airy whistle band that swells in slow gusts."""
    length = 28.0
    n = int(SR * length)
    r = rng(500)
    gust = lowpass(r.standard_normal(n), 0.25)
    gust = 0.35 + 0.65 * (gust - gust.min()) / (gust.max() - gust.min() + 1e-9)
    rumble = lowpass(pink(n, 501), 260)
    air = bandpass(pink(n, 502), 500, 1700)
    x = rumble / np.std(rumble) * 0.6 * (0.6 + 0.4 * gust) + air / np.std(air) * 0.35 * gust ** 2
    x = loopify(x, 2.0)
    save("amb_wind_loop", stereo_widen(x, 503, 13))


def crowd_big_loop():
    """A packed park: denser chatter, laughter-ish blips, distant whoops and bursts of clapping."""
    length = 30.0
    n = int(SR * length)
    r = rng(510)
    murmur = bandpass(pink(n, 511), 250, 1800)
    mod = 0.7 + 0.3 * np.tanh(lowpass(r.standard_normal(n), 2) * 30)
    x = murmur / np.std(murmur) * mod
    for _ in range(160):
        at = r.uniform(0, length)
        dur = r.uniform(0.12, 0.4)
        f0 = r.uniform(150, 340)
        st = secs(dur)
        glide = f0 * (1 + 0.2 * np.sin(np.pi * st / dur))
        buzz = np.sign(np.sin(2 * np.pi * np.cumsum(glide) / SR)) * 0.5
        voice = bandpass(buzz, 500, 1800) * np.sin(np.pi * st / dur) ** 2
        place(x, voice, at, r.uniform(0.15, 0.35), wrap=True)
    for k in range(5):  # distant "whoo!" swells
        at = r.uniform(0, length)
        d = 1.4
        st = secs(d)
        env = np.minimum(st / 0.3, 1) * np.exp(-np.maximum(st - 0.4, 0) / 0.4)
        w = np.zeros(len(st))
        for v in range(6):
            f0 = r.uniform(240, 420) * (1 + 0.25 * np.minimum(st / 0.3, 1))
            saw = 2 * ((np.cumsum(f0) / SR) % 1) - 1
            w += bandpass(saw, 700, 1400)
        place(x, w / np.std(w) * env, at, 0.5, wrap=True)
    for k in range(4):  # bursts of clapping
        at = r.uniform(0, length)
        d = r.uniform(1.5, 2.5)
        claps = np.zeros(int(SR * d))
        for c in range(int(d * 40)):
            click = bandpass(white(int(SR * 0.012), int(at * 1000) + c), 900, 5000) * np.exp(-secs(0.012) / 0.004)
            place(claps, click, r.uniform(0, d - 0.02), r.uniform(0.3, 1))
        claps *= np.sin(np.pi * np.linspace(0, 1, len(claps)))
        place(x, claps / (np.std(claps) + 1e-9), at, 0.35, wrap=True)
    x = loopify(x, 1.5)
    save("amb_crowd_big_loop", stereo_widen(x, 512, 17))


def chime_note(f, length):
    t = secs(length)
    return (np.sin(2 * np.pi * f * t) + 0.25 * np.sin(2 * np.pi * 2 * f * t) + 0.08 * np.sin(2 * np.pi * 3.01 * f * t)) \
        * np.minimum(t / 0.004, 1) * np.exp(-t / 0.9)


def pa_chime():
    """The classic three-tone 'ding-dong-ding' before an announcement, with a stadium echo."""
    length = 2.6
    x = np.zeros(int(SR * length))
    for i, m in enumerate((79, 76, 72)):  # G5 E5 C5
        place(x, chime_note(midi(m), 1.6), i * 0.36, 0.8)
    echo(x, 0.14, 0.3)
    save("pa_chime", fade(megaphone(x, soft=False), 0.002, 0.3), peak_db=-2)


def echo(x, delay, gain, repeats=3):
    d = int(SR * delay)
    for k in range(1, repeats + 1):
        x[d * k:] += x[:-d * k] * gain ** k
    return x


def megaphone(x, soft=True):
    """Small PA horn: no deep bass, no sparkle, a little crunchy."""
    y = bandpass(x, 350, 3200)
    if soft:
        y = np.tanh(y / (np.std(y) + 1e-9) * 1.5)
    return y


def pa_voice(name, seed):
    """An announcer you can't quite make out: syllables of a gliding voice through formant filters and a PA horn."""
    r = rng(seed)
    length = 2.8
    n = int(SR * length)
    x = np.zeros(n)
    at = 0.1
    base = r.uniform(105, 135)
    while at < length - 0.6:
        words = r.integers(2, 5)
        for w in range(words):
            d = r.uniform(0.11, 0.22)
            st = secs(d)
            f0 = base * (1 + 0.12 * np.sin(np.pi * st / d) + r.uniform(-0.08, 0.12))
            phase = np.cumsum(f0) / SR
            buzz = 2 * (phase % 1) - 1
            lo, hi = [(600, 1100), (350, 900), (800, 2300), (500, 1600)][r.integers(0, 4)]
            syl = bandpass(buzz, lo, hi) + 0.4 * bandpass(buzz, 2200, 3000)
            syl *= np.sin(np.pi * st / d) ** 1.5
            place(x, syl, at, r.uniform(0.7, 1.0))
            at += d + r.uniform(0.01, 0.05)
        at += r.uniform(0.15, 0.3)
        base *= r.uniform(0.95, 1.05)
    x = megaphone(x)
    echo(x, 0.16, 0.28)
    save(name, fade(x, 0.005, 0.2), peak_db=-3)


def brass(f, length, attack=0.03, decay=None):
    """Bright brass-ish note: stacked harmonics with a soft attack and slight vibrato."""
    t = secs(length)
    vib = 1 + 0.004 * np.sin(2 * np.pi * 5.5 * t) * np.minimum(t / 0.3, 1)
    phase = 2 * np.pi * np.cumsum(f * vib) / SR
    y = sum(np.sin(phase * k) / k ** 1.1 for k in range(1, 9))
    env = np.minimum(t / attack, 1) * (0.75 + 0.25 * np.exp(-t / 0.15))
    if decay:
        env *= np.exp(-t / decay)
    env *= np.minimum((length - t) / 0.15, 1)
    return lowpass(y * env, 3500)


def cymbal(length, seed, swell=False):
    t = secs(length)
    env = (t / length) ** 2 if swell else np.exp(-t / (length * 0.35))
    return highpass(white(len(t), seed), 4500) * env


def timpani(length=1.2, f=62):
    t = secs(length)
    return np.sin(2 * np.pi * np.cumsum(f * (1 + 0.15 * np.exp(-t / 0.05))) / SR) * np.exp(-t / 0.35)


def fanfare_level():
    """Level-up sting: a quick brass pickup into a held major chord, with a cymbal."""
    length = 2.6
    x = np.zeros(int(SR * length))
    for i, m in enumerate((67, 67, 67)):  # da-da-da
        place(x, brass(midi(m), 0.12, 0.01), i * 0.11, 0.6)
    for m, g in ((72, 0.7), (76, 0.55), (79, 0.5), (60, 0.5)):  # C major, held
        place(x, brass(midi(m), 1.9, 0.02, 1.6), 0.36, g)
    place(x, cymbal(1.8, 520), 0.36, 0.25)
    place(x, timpani(), 0.36, 0.5)
    echo(x, 0.17, 0.2, 2)
    save("fanfare_level", fade(x, 0.002, 0.25))


def fanfare_pool():
    """New pool open: a bigger fanfare, IV - V - I with timpani and a long cymbal crash."""
    length = 4.4
    x = np.zeros(int(SR * length))
    place(x, cymbal(0.8, 530, swell=True), 0.0, 0.2)
    for i, m in enumerate((67, 69, 71)):
        place(x, brass(midi(m), 0.14, 0.01), 0.55 + i * 0.12, 0.6)
    t0 = 0.95
    for chord, at, d in (((65, 69, 72), t0, 0.45), ((67, 71, 74), t0 + 0.5, 0.45), ((72, 76, 79, 84, 60), t0 + 1.0, 2.3)):
        for m in chord:
            place(x, brass(midi(m), d, 0.02, 2.0 if d > 1 else None), at, 0.45)
        place(x, timpani(1.0, 55 if d < 1 else 49), at, 0.6)
    place(x, cymbal(2.6, 531), t0 + 1.0, 0.35)
    echo(x, 0.19, 0.22, 3)
    save("fanfare_pool", fade(x, 0.002, 0.4))


def firework_launch(name, seed):
    """Rising whistle with a hiss: the rocket going up."""
    r = rng(seed)
    length = 1.25
    t = secs(length)
    f = np.interp(t / length, [0, 1], [r.uniform(700, 900), r.uniform(2300, 2800)])
    whistle_ = tone(f, length) * np.minimum(t / 0.1, 1) * np.minimum((length - t) / 0.2, 1) * 0.5
    hiss = bandpass(white(len(t), seed + 1), 1500, 7000) * np.exp(-t / 0.5) * 0.6
    save(name, fade(whistle_ + hiss, 0.003, 0.1), peak_db=-4)


def firework_bang(name, seed):
    """Boom, then a crackle of sparks."""
    r = rng(seed)
    length = 2.6
    t = secs(length)
    x = np.zeros(len(t))
    boom = lowpass(white(len(t), seed), 500) * np.exp(-t / 0.18) * 2.5
    thump = np.sin(2 * np.pi * np.cumsum(55 * (1 + np.exp(-t / 0.05))) / SR) * np.exp(-t / 0.3)
    x += boom / np.std(boom[:int(SR * 0.3)]) * 0.5 + thump
    for c in range(r.integers(70, 110)):
        at = 0.15 + r.exponential(0.45)
        if at > length - 0.05:
            continue
        click = highpass(white(int(SR * 0.006), seed * 100 + c), 2500) * np.exp(-secs(0.006) / 0.0015)
        place(x, click, at, r.uniform(0.1, 0.45) * np.exp(-(at - 0.15) / 0.9))
    echo(x, 0.22, 0.25, 2)
    save(name, fade(x, 0.001, 0.3))


def jetski_loop():
    """Two-stroke buzz with water hiss, as a 2.4 s loop for a passing jet ski."""
    length = 2.4
    n = int(SR * length)
    t = np.arange(n) / SR
    f = 96 + 6 * np.sin(2 * np.pi * t / length * 2)  # whole cycles: loops cleanly
    phase = np.cumsum(f) / SR
    saw = 2 * (phase % 1) - 1
    engine = bandpass(np.tanh(saw * 2), 120, 2400)
    hiss = bandpass(white(n, 540), 800, 5000)
    x = engine / np.std(engine) + hiss / np.std(hiss) * 0.4 * (0.7 + 0.3 * np.sin(2 * np.pi * t / length * 3))
    x = loopify(np.concatenate([x, x[:int(SR * 0.3)]]), 0.3)
    save("loop_jetski", x, peak_db=-3)


def heli_loop():
    """Helicopter: blade chop pulses plus a turbine whine, an exact 2.5 s loop."""
    length = 2.5
    n = int(SR * length)
    x = np.zeros(n)
    chop_rate = 6.0
    for k in range(int(length * chop_rate)):
        d = 0.09
        pulse = lowpass(white(int(SR * d), 550 + k), 700) * np.sin(np.pi * secs(d) / d) ** 3
        place(x, pulse, k / chop_rate, 1.0, wrap=True)
    t = np.arange(n) / SR
    whine = np.sin(2 * np.pi * 1200 * t) * 0.05 + np.sin(2 * np.pi * 2400 * t) * 0.02
    rumble = lowpass(white(n, 551), 120)
    x = x / np.std(x) + whine / 0.05 * 0.15 + rumble / np.std(rumble) * 0.4
    save("loop_heli", x, peak_db=-3)


def music_party(name, chords, seed, bpm=96, bars=16):
    """Upbeat park-speaker song for a busy park: the guitar picking plus a soft beat and a shaker."""
    beat = 60 / bpm
    bar = beat * 4
    length = bar * bars + 3.0
    n = int(SR * length)
    x = np.zeros(n)
    r = rng(seed)
    pattern = [0, 2, 1, 3, 2, 1, 3, 2]
    kick_t = secs(0.25)
    kick = np.sin(2 * np.pi * np.cumsum(50 * (1 + 1.5 * np.exp(-kick_t / 0.03))) / SR) * np.exp(-kick_t / 0.09)
    shaker = highpass(white(int(SR * 0.05), seed), 5000) * np.exp(-secs(0.05) / 0.015)
    for b in range(bars):
        chord = chords[b % len(chords)]
        t0 = b * bar
        for k in (0, 2.5):  # bass: beat 1 and the "and" of 3
            place(x, pluck(midi(chord[0] - 12), 1.2, 0.997, seed + b, soft=4), t0 + k * beat, 0.4)
        for k in range(8):
            m = chord[pattern[k]] + (12 if k in (3, 6) else 0)
            at = max(0.0, t0 + k * beat / 2 + r.uniform(-0.008, 0.008))
            place(x, pluck(midi(m), 1.2, 0.995, seed * 100 + b * 10 + k, soft=3), at, r.uniform(0.13, 0.18))
        if 1 <= b < bars - 1:
            for k in range(4):
                place(x, kick, t0 + k * beat, 0.35 if k % 2 == 0 else 0.2)
            for k in range(8):
                place(x, shaker, t0 + k * beat / 2, 0.05 if k % 2 else 0.03)
        if b % 2 == 1:  # marimba-ish melody fill
            for k in range(3):
                m = PENTA[r.integers(0, len(PENTA))]
                place(x, tone(midi(m), 0.5, ((1, 1), (4, 0.25)), 0.18), t0 + (2 + k * 0.5) * beat, 0.12)
    x = lowpass(x, 4200)
    fade_len = int(SR * 3)
    x[:fade_len] *= np.linspace(0, 1, fade_len)
    x[-fade_len:] *= np.linspace(1, 0, fade_len)
    save(name, stereo_widen(x, seed, 9), peak_db=-3)



# ---------- shops & machine ----------

def plop(name, seed, f0):
    # A soft wet "plop": a quick downward pitch blip with a little click (toppings landing).
    length = 0.18
    t = secs(length)
    f = f0 * np.exp(-t * 18) + f0 * 0.45
    x = tone(f, length, ((1, 1), (2, 0.2))) * np.exp(-t / 0.045)
    place(x, lowpass(white(int(SR * 0.006), seed), 2500), 0, 0.25)
    save(name, fade(x, 0.001, 0.03), peak_db=-4)


# ---------- UI family (docs/research/game-ui-craft.md #4): all built from one water-droplet "bloop", so the menus
# sound like a pool. Hover near silent, confirm rises, back/close falls. ----------

def droplet(f0, f1, length, decay):
    t = secs(length)
    f = f1 + (f0 - f1) * np.exp(-t * 30)
    return tone(f, length, ((1, 1), (2, 0.12))) * np.exp(-t / decay)


def ui_open():
    """A window opens: a soft airy swish up into a rising droplet."""
    length = 0.2
    t = secs(length)
    swish = sweep_lowpass(white(len(t), 1201), 600, 3200) * np.sin(np.pi * t / length) ** 2
    x = swish / np.std(swish) * 0.12
    place(x, droplet(520, 980, 0.12, 0.035), 0.06, 0.8)
    save("ui_open", fade(x, 0.003, 0.03), peak_db=-5)


def ui_close():
    """A window closes: a shorter swish down and a falling droplet."""
    length = 0.14
    t = secs(length)
    swish = sweep_lowpass(white(len(t), 1202), 2600, 500) * np.sin(np.pi * t / length) ** 2
    x = swish / np.std(swish) * 0.1
    place(x, droplet(900, 420, 0.1, 0.03), 0.02, 0.8)
    save("ui_close", fade(x, 0.003, 0.03), peak_db=-6)


def ui_tab():
    """Switching tabs: one bright droplet."""
    x = droplet(700, 1250, 0.09, 0.022)
    place(x, highpass(white(int(SR * 0.002), 1203), 3000), 0, 0.15)
    save("ui_tab", fade(x, 0.001, 0.02), peak_db=-5)


def ui_toggle():
    """A switch: two quick droplets, low then high (played lower for "off")."""
    x = np.zeros(int(SR * 0.13))
    place(x, droplet(500, 640, 0.05, 0.015), 0, 0.8)
    place(x, droplet(760, 1020, 0.07, 0.018), 0.045, 1.0)
    save("ui_toggle", fade(x, 0.001, 0.02), peak_db=-5)


def ui_tick():
    """Count-up tick: a tiny droplet (the game raises its pitch with each step)."""
    x = droplet(1500, 1900, 0.03, 0.007)
    save("ui_tick", fade(x, 0.0005, 0.008), peak_db=-6)


def ui_stamp():
    """A rubber stamp on a receipt: a low thud, a paper slap and a little wobble."""
    length = 0.3
    t = secs(length)
    thud = tone(140 * np.exp(-t * 9) + 55, length, ((1, 1), (2, 0.3)), decay=0.07)
    slap = bandpass(white(len(t), 1206), 700, 4500) * np.exp(-t / 0.018)
    x = thud + slap / np.std(slap) * 0.35
    save("ui_stamp", fade(x, 0.0005, 0.05), peak_db=-3)


def blender_whirr():
    # Motor whine that spins up, chops through fruit (wobble), then spins down.
    length = 1.4
    t = secs(length)
    spin = np.clip(t / 0.25, 0, 1) * np.clip((length - t) / 0.3, 0, 1)
    f = 180 + 260 * spin
    motor = tone(f, length, ((1, 1), (2, 0.6), (3, 0.35), (5, 0.15)))
    chop = 0.6 + 0.4 * np.sign(np.sin(2 * np.pi * 9 * t))
    grind = bandpass(white(len(t), 811), 600, 4000)
    x = (motor * 0.5 + grind / np.std(grind) * 0.25) * spin * chop
    save("blender", fade(x, 0.01, 0.1), peak_db=-4)


def shop_bell():
    # The little counter bell: "ding!"
    length = 1.0
    x = np.zeros(int(SR * length))
    place(x, bell(1760, 0.9, 1.6), 0, 0.8)
    place(x, bell(2640, 0.6, 1.0), 0.0, 0.25)
    save("shop_bell", fade(x, 0.001, 0.15), peak_db=-3)


def scan_beep():
    length = 0.12
    t = secs(length)
    x = np.sin(2 * np.pi * 2350 * t) * np.clip((length - t) / 0.02, 0, 1)
    save("scan_beep", fade(x, 0.002, 0.01), peak_db=-6)


def planer():
    # Electric planer on foam: motor buzz + a soft shaving hiss in strokes.
    length = 1.2
    t = secs(length)
    buzz = tone(120 + 10 * np.sin(2 * np.pi * 3 * t), length, ((1, 1), (2, 0.7), (3, 0.5), (4, 0.3)))
    hiss = bandpass(white(len(t), 821), 1500, 7000)
    stroke = 0.5 + 0.5 * np.sin(2 * np.pi * 1.6 * t) ** 2
    x = buzz * 0.25 + hiss / np.std(hiss) * 0.35 * stroke
    save("planer", fade(x, 0.03, 0.15), peak_db=-5)


def spray_hiss():
    length = 0.9
    t = secs(length)
    hiss = highpass(white(len(t), 831), 2500)
    env = np.clip(t / 0.04, 0, 1) * np.clip((length - t) / 0.15, 0, 1)
    rattle = np.zeros(len(t))
    for i in range(3):
        place(rattle, bandpass(white(int(SR * 0.02), 840 + i), 2000, 6000), i * 0.05, 0.6)
    x = hiss / np.std(hiss) * 0.5 * env + rattle
    save("spray_hiss", fade(x, 0.005, 0.1), peak_db=-6)


def air_blast():
    # A wave chamber firing: valve clunk, big air whoosh, low "whump".
    length = 1.6
    t = secs(length)
    clunk = bandpass(white(int(SR * 0.05), 851), 200, 1800) * np.exp(-secs(0.05) / 0.012)
    air = sweep_lowpass(white(len(t), 852), 3500, 500) * np.exp(-t / 0.45)
    whump = tone(42 * np.exp(-t * 2), length, decay=0.4)
    x = air / np.std(air) * 0.4 + whump * 0.9
    place(x, clunk, 0, 0.8)
    save("air_blast", fade(x, 0.002, 0.3))


def footstep(name, seed, kind):
    # One step: sand = soft crunchy hiss, deck = short heel tap + scuff, wood = hollow knock.
    r = rng(seed)
    if kind == "sand":
        length = 0.22
        t = secs(length)
        crunch = bandpass(white(len(t), seed), 900 + r.uniform(-150, 150), 5000) * np.exp(-t / 0.06)
        grains = np.zeros(len(t))
        for _ in range(14):
            place(grains, highpass(white(int(SR * 0.004), seed + _ + 1), 3000), r.uniform(0.0, 0.12), r.uniform(0.1, 0.3))
        x = crunch / np.std(crunch) * 0.4 + grains
    elif kind == "deck":
        length = 0.16
        t = secs(length)
        tap = lowpass(white(len(t), seed), 2200) * np.exp(-t / 0.018)
        thud = tone(110 + r.uniform(-10, 10), length, decay=0.03)
        scuff = bandpass(white(len(t), seed + 7), 1500, 6000) * np.exp(-np.maximum(t - 0.03, 0) / 0.03) * (t > 0.03)
        x = tap / np.std(tap) * 0.5 + thud * 0.6 + scuff / (np.std(scuff) + 1e-9) * 0.12
    else:
        length = 0.25
        t = secs(length)
        knock = tone(np.array([180 + r.uniform(-15, 15)]) * np.ones(len(t)), length, ((1, 1), (2.3, 0.5), (3.9, 0.25)), decay=0.07)
        click = lowpass(white(int(SR * 0.01), seed), 3000)
        x = knock * 0.8
        place(x, click, 0, 0.4)
    save(name, fade(x, 0.001, 0.03), peak_db=-8)


# ---------- voices: babble syllables and exclamations ----------
# Characters "talk" Animal Crossing style: a few short voiced syllables per speech bubble, pitched per person in
# Unity (kids high, dads low). Source = a glottal pulse train with vibrato, shaped by vowel formants.

VOWELS = {"a": (800, 1200, 2500), "e": (500, 1900, 2600), "i": (310, 2300, 3000), "o": (500, 900, 2400), "u": (330, 800, 2300)}


def glottal(f0, length, seed=0, vibrato=4.5):
    t = secs(length)
    f = np.broadcast_to(f0, t.shape) * (1 + 0.012 * np.sin(2 * np.pi * vibrato * t))
    phase = np.cumsum(f) / SR
    saw = 2 * (phase % 1.0) - 1  # bright buzz, like vocal folds
    return lowpass(saw, 4500) + white(len(t), seed) * 0.02


def formants(x, vowel, width=110):
    f1, f2, f3 = VOWELS[vowel]
    def shape(f):
        return (np.exp(-((f - f1) / width) ** 2) * 1.0 + np.exp(-((f - f2) / (width * 1.4)) ** 2) * 0.55
                + np.exp(-((f - f3) / (width * 1.8)) ** 2) * 0.25 + 0.02)
    return spectral(x, shape)


def syllable(name, seed, vowel, consonant):
    r = rng(seed)
    length = r.uniform(0.085, 0.13)
    t = secs(length)
    f0 = 230 * np.linspace(1.0 + r.uniform(-0.04, 0.06), 1.0 + r.uniform(-0.08, 0.02), len(t))
    voice = formants(glottal(f0, length, seed), vowel)
    env = np.clip(t / 0.012, 0, 1) * np.clip((length - t) / 0.035, 0, 1)
    x = voice / (np.std(voice) + 1e-9) * env
    if consonant == "t":   # short bright tick
        place(x, highpass(white(int(SR * 0.012), seed + 1), 3500) * 0.8, 0)
    elif consonant == "s":  # little hiss
        place(x, bandpass(white(int(SR * 0.025), seed + 1), 4000, 9000) * 0.5, 0)
    elif consonant == "b":  # soft thump
        place(x, lowpass(white(int(SR * 0.01), seed + 1), 600) * 1.2, 0)
    save(name, fade(x, 0.002, 0.02), peak_db=-6)


def exclaim(name, seed, vowels, f_start, f_end, length, shape="rise"):
    """A whole exclamation: the pitch glides while the vowel changes (crossfading formant versions)."""
    t = secs(length)
    if shape == "rise":
        f0 = f_start * (f_end / f_start) ** (t / length) ** 0.7
    elif shape == "arch":
        k = np.sin(np.pi * t / length)
        f0 = f_start + (f_end - f_start) * k
    else:  # fall
        f0 = f_start * (f_end / f_start) ** (t / length)
    src = glottal(f0, length, seed, vibrato=6)
    parts = [formants(src, v) for v in vowels]
    x = np.zeros(len(t))
    n = len(parts)
    for i, p in enumerate(parts):  # crossfade vowel i over its share of the time
        center = (i + 0.5) / n
        w = np.clip(1 - np.abs(t / length - center) * n, 0, 1) if n > 1 else np.ones(len(t))
        x += p / (np.std(p) + 1e-9) * w
    env = np.clip(t / 0.03, 0, 1) * np.clip((length - t) / 0.12, 0, 1)
    save(name, fade(x * env, 0.003, 0.05), peak_db=-4)


def hum(name, seed, length=0.4):
    t = secs(length)
    f0 = 200 * (1 + 0.08 * np.sin(np.pi * t / length))
    x = lowpass(glottal(f0, length, seed), 420)
    env = np.clip(t / 0.04, 0, 1) * np.clip((length - t) / 0.1, 0, 1)
    save(name, fade(x / (np.std(x) + 1e-9) * env, 0.004, 0.05), peak_db=-6)


def main():
    surf_loop()
    crowd_loop()
    wave_machine()
    wave_crash("wave_crash_small", 101, 2.6, 1800, 700, 0.6, 0.15)
    wave_crash("wave_crash_medium", 102, 3.6, 1500, 500, 0.95, 0.3)
    wave_crash("wave_crash_big", 103, 5.0, 1200, 300, 1.5, 0.55)
    wave_boom()
    section_wall()
    tube_roar()
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
    music("music_sunset", CHORDS_C, 9, bpm=70)
    wind_loop()
    crowd_big_loop()
    pa_chime()
    pa_voice("pa_voice_1", 601)
    pa_voice("pa_voice_2", 602)
    pa_voice("pa_voice_3", 603)
    fanfare_level()
    fanfare_pool()
    firework_launch("firework_launch_1", 701)
    firework_launch("firework_launch_2", 702)
    firework_bang("firework_bang_1", 711)
    firework_bang("firework_bang_2", 712)
    firework_bang("firework_bang_3", 713)
    jetski_loop()
    heli_loop()
    music_party("music_party", CHORDS_A, 21)
    plop("plop_1", 801, 520)
    plop("plop_2", 802, 430)
    plop("plop_3", 803, 610)
    blender_whirr()
    shop_bell()
    scan_beep()
    planer()
    spray_hiss()
    air_blast()
    ui_open()
    ui_close()
    ui_tab()
    ui_toggle()
    ui_tick()
    ui_stamp()
    babble = [("a", "b"), ("e", "t"), ("i", "s"), ("o", "b"), ("u", "t"), ("a", "t"), ("e", "b"), ("o", "s"), ("i", "t"), ("a", "s"), ("u", "b"), ("e", "s")]
    for i, (v, c) in enumerate(babble):
        syllable(f"voice_babble_{i + 1}", 1000 + i, v, c)
    exclaim("voice_yew_1", 1101, ["i", "u"], 300, 560, 0.55)
    exclaim("voice_yew_2", 1102, ["i", "e", "u"], 280, 520, 0.6)
    exclaim("voice_woo_1", 1103, ["u", "o"], 330, 470, 0.45, "arch")
    exclaim("voice_woo_2", 1104, ["o", "u"], 300, 440, 0.4, "arch")
    exclaim("voice_aah_1", 1105, ["a"], 520, 300, 0.55, "fall")
    exclaim("voice_aah_2", 1106, ["a", "o"], 560, 320, 0.6, "fall")
    exclaim("voice_ooh_1", 1107, ["o", "u"], 260, 340, 0.4, "arch")
    hum("voice_hmm_1", 1108)
    hum("voice_hmm_2", 1109, 0.5)
    for i in range(3):
        footstep(f"step_sand_{i + 1}", 900 + i, "sand")
        footstep(f"step_deck_{i + 1}", 910 + i, "deck")
        footstep(f"step_wood_{i + 1}", 920 + i, "wood")
    for name, length, ch in written:
        print(f"{name:20s} {length:6.2f}s  {'stereo' if ch == 2 else 'mono'}")


if __name__ == "__main__":
    main()
