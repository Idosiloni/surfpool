# Sound design for Surf Pool Sim
Date: 2026-10-20

Goal: every action has a sound, the park always has a living "beach bed" underneath, and all of it can be made in Python (numpy -> WAV). Numbers marked (guess) are rules of thumb from game-audio practice, not measured from a specific game.

## 1. What makes sim/management games feel good
- **Every click answers back.** Soft click on UI, tiny "tick" on hover, deeper "thunk" on confirm. Silence on a button feels broken.
- **Money has its own sound.** A bright register/coin chime on income. Big sale = bigger chime (more partials, longer tail). Stack small ones: many small pings should not become painful (cap rate to ~8/sec, lower volume as they stack) (guess).
- **Unlock/purchase = a rising jingle** (3-5 notes going up, major key, under ~1.2 s). Rising pitch = "good thing happened".
- **Error/denied = short low, dull "bonk"** (never harsh). Falling pitch = "no".
- **Ambient crowd makes the place feel alive** even when nothing is happening. Park gets busier -> crowd bed gets louder/denser. This is the cheapest "it's growing" feedback.
- **Short and dry beats long and reverby** for frequent sounds (clicks, coins). Long tails only for rare events (level up, new pool).
- Streamer angle: sounds that are clear and satisfying are clip-able; keep them distinct from each other so a viewer can hear "that was a sale" without looking (guess).

## 2. What a real wave pool / surf park sounds like
Sources are general descriptions of wave-pool tech: [Wave pool overview](https://en.wikipedia.org/wiki/Wave_pool), [Surf park wiki-style overview](https://en.wikipedia.org/wiki/Artificial_wave). Sound descriptions below are my own summary (guess where noted).
| Sound | What it is like | Notes |
|---|---|---|
| Wave machine | Low mechanical hum, pumps/hydraulic thump, then a swelling "whoomp" as water is pushed | Plays *before* the wave arrives = anticipation cue |
| Wave building | Rising hiss/whoosh over 2-4 s as the swell approaches | Pitch of noise filter rises as wave grows |
| Breaking wave | Sudden bright crash (noise burst, fast attack), then decays | Bigger wave = lower cutoff + longer decay |
| Whitewash | Long fizzy, steady rumble/hiss, slowly fading | Fills the gap after the crash |
| Barrel | Muffled, hollow roar, low-passed (guess) | Nice rare "special" sound |
| Surfer on wave | Faint carving swish, board slap, splash on wipeout | Quiet; short |
| Crowd | Murmur bed + occasional "whoo!" cheers when a surfer lands a trick | Cheers on big moments only |
| Lifeguard whistle | Short sharp 2-3 kHz tone, 0.3-0.8 s, slight vibrato | Use for "pool closed / wave starting" |
| Seagulls | Sparse, random, high squawks 1-2 kHz sweeping down | Every 8-25 s, never regular |
| Beach music | Relaxed speaker music near the cafe / shop | Quiet, spatial |
| Pool side | Splashing, dripping, distant kids | Low-level texture |

## 3. Layering and mixing
**Two kinds of sound:**
- **Beds (looping, always on):** ocean/surf wash, crowd murmur, wind, music. Seamless loops, long (20-40 s) so repeats are not noticeable.
- **One-shots (events):** clicks, coins, jingles, splashes, whistle, cheers, gulls.

**Rough volume ladder (relative to 0 dB = loudest event)** (guess, tune by ear):
| Layer | Level |
|---|---|
| Unlock / level-up jingle | 0 dB |
| Wave crash (when your wave breaks) | -3 dB |
| Coin/register chime | -6 dB |
| UI click | -12 dB |
| Cheers | -10 dB |
| Music | -18 dB |
| Surf ambient bed | -20 dB |
| Crowd bed | -24 dB |
| Seagulls / small details | -20 dB, sparse |

**Mixer groups (Unity AudioMixer):** Master -> Music, Ambience, SFX, UI. Give Ido a slider for each later (settings step on the roadmap).

**3D falloff (Unity AudioSource, logarithmic rolloff)** (guess):
| Sound | Min dist | Max dist |
|---|---|---|
| Wave machine hum | 8 m | 60 m |
| Breaking wave | 10 m | 80 m |
| Crowd bed | 5 m | 40 m |
| Register / desk | 2 m | 15 m |
| Cafe music | 3 m | 25 m |
| UI sounds | 2D (no falloff) | |
Make ambient loops 2D or very wide (spatialBlend ~0.3) so they do not "pan" weirdly in first person.

**Fight repetition (important for sims where you hear the same sound 500 times):**
- Random pitch +/- 5-8 % and volume +/- 10 % on every one-shot.
- 3-4 variants of each frequent sound; never play the same variant twice in a row.
- Min gap between identical sounds (e.g. 80 ms) so they do not stack into loud doubled hits.
- Generate variants by changing the random seed in the synth code (see recipes).

**Ducking:** when a big event plays (unlock jingle, wave crash close to the player), lower Music and Ambience by 4-6 dB for ~1.5 s, then ease back over 0.5 s. In Unity use an AudioMixer snapshot or a "duck" group with a Send/Receive. Also duck music lightly while the pause menu is open.

**Tip:** low-pass the whole world a bit (cutoff ~800 Hz) when the player is underwater or in the barrel - cheap, strong effect (guess).

## 4. Chill beach music
- Tempo: 80-100 BPM (guess: "chill/lofi/tropical-house" range; many beach playlists sit in 85-100). Use 90.
- Feel: laid-back, light swing or straight eighths, soft kick optional, no big drops.
- Instruments: plucked ukulele/nylon guitar, soft marimba or vibes, warm pad, gentle bass, light shaker, optional steel-drum-ish bell. No harsh lead.
- Harmony: major 7th / add9 chords, 4-bar loop, e.g. **Cmaj7 - Am7 - Fmaj7 - G6** (I-vi-IV-V) or **Fmaj7 - Em7 - Dm7 - Cmaj7**. Loop 8-16 bars so it is ~20-40 s before repeating; keep 2-3 different tracks and shuffle.
- Keep music quiet: it is the bed under the ocean, not the star.
- Reference reading: [Karplus-Strong string synthesis](https://en.wikipedia.org/wiki/Karplus%E2%80%93Strong_string_synthesis), [Pink noise](https://en.wikipedia.org/wiki/Pink_noise), [Pentatonic scale](https://en.wikipedia.org/wiki/Pentatonic_scale).

## 5. Procedural synthesis recipes (Python, numpy -> WAV)
Use 44100 Hz, float32 in [-1, 1], write 16-bit PCM with the standard `wave` module. Normalize to about -1 dB peak. Always apply a 5-10 ms fade-in/out to avoid clicks.

```python
import numpy as np, wave
SR = 44100
def t(sec): return np.arange(int(SR*sec))/SR
def save(name, x, peak=0.9):
    x = x/ (np.max(np.abs(x))+1e-9) * peak
    with wave.open(name,'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x*32767).astype(np.int16).tobytes())
def white(sec, seed=0): return np.random.default_rng(seed).standard_normal(int(SR*sec))
def lowpass(x, cutoff):            # one-pole, cutoff can be an array (sweep)
    a = 1-np.exp(-2*np.pi*np.asarray(cutoff)/SR); a = np.broadcast_to(a, x.shape)
    y = np.zeros_like(x); s = 0.0
    for i in range(len(x)): s += a[i]*(x[i]-s); y[i]=s
    return y
def highpass(x, cutoff): return x - lowpass(x, cutoff)
def pink(sec, seed=0):             # FFT shaping: 1/sqrt(f)
    n=int(SR*sec); X=np.fft.rfft(white(sec,seed)); f=np.fft.rfftfreq(n,1/SR); f[0]=1
    return np.fft.irfft(X/np.sqrt(f), n)
def env_adsr(n, a, d, s, r):       # seconds, s=level
    e=np.concatenate([np.linspace(0,1,int(a*SR)), np.linspace(1,s,int(d*SR))])
    e=np.concatenate([e, np.full(max(0,n-len(e)-int(r*SR)), s), np.linspace(s,0,int(r*SR))])
    return e[:n]
```
(For speed with long files use `scipy.signal.lfilter` for filters instead of the Python loop.)

| Sound | Recipe |
|---|---|
| **Wave crash** (2.5-4 s) | Pink noise. Amplitude envelope: swell 0.4 s up -> sharp peak -> exponential decay `exp(-t/0.9)`. Sweep low-pass cutoff from ~1500 Hz down to ~400 Hz over the sound (bright crash that gets rumbly). Add a second layer: white noise high-passed at 3 kHz, shorter decay 0.5 s, for the "fizz". Mix 70/30. Bigger wave = longer decay, lower cutoff. |
| **Whitewash rumble** (4-6 s) | Brown-ish noise (cumulative sum of white, then high-pass 40 Hz) low-passed at ~600 Hz, plus faint white noise high-passed at 4 kHz at 20 % level. Slow fade in 0.5 s, out 3 s. Slight amplitude wobble at 0.3 Hz. |
| **Steady surf ambience** (loop, 30 s) | Pink noise low-passed ~900 Hz. Multiply by slow "breathing" envelope: `0.55 + 0.45*sin(2*pi*t/9)` + second sine at ~13 s (different periods = not obviously repeating). Add a few soft wave swells (noise bursts with 3 s rise, 4 s fall) at random times. **Make it loop:** crossfade the last 2 s into the first 2 s. |
| **Wave machine hum** (loop, 8 s) | Sines at 55 Hz + 82.5 Hz + 110 Hz (amps 1, 0.5, 0.3) with slight detune and 0.7 Hz tremolo; add low-passed noise at 20 %. Pitch/volume rises while charging (play with `pitch` in Unity instead of baking). |
| **Coin / register chime** (0.6-1 s) | Sum of inharmonic bell partials: freqs `f*[1, 2.76, 5.4, 8.9]` with `f`=1200-1500 Hz, amps `[1, .6, .35, .2]`, each with decay `exp(-t/tau)`, tau `[0.35,0.2,0.12,0.08]`. Add a 2 ms click at start. Second "ting" 90 ms later at f*1.5 (the classic ka-CHING). |
| **Soft UI click** (40-60 ms) | Sine 900 Hz dropping to 500 Hz (pitch glide) with `exp(-t/0.012)` decay, plus a tiny noise burst (3 ms, high-passed). Low volume. Hover tick: 1800 Hz, 20 ms. |
| **Happy unlock arpeggio** (~1 s) | Notes C5 E5 G5 C6 (523, 659, 784, 1047 Hz), start every 90 ms. Each note: sine + 0.3 * sine at 2f + 0.1 * at 3f, ADSR (a=3 ms, d=80 ms, s=0.4, r=0.3 s). Add a short sparkle: high-passed noise burst 0.1 s at the end. Big unlock: add E6 G6 and a soft delay (echo 0.18 s, 35 %). |
| **Error bonk** | Sine 220 Hz -> 150 Hz glide, 120 ms, soft decay. |
| **Splash** (0.6-1 s) | White noise band-passed ~1-4 kHz (high-pass 800 + low-pass 5k), fast attack, decay 0.25 s. Add small "bubble" blips: 6-10 short sine chirps rising 300 -> 900 Hz, 20-40 ms each, random times in first 0.5 s. Big splash = longer, add low thump (sine 80 Hz, 0.15 s). |
| **Seagull call** (0.5-0.9 s) | Sine/saw with frequency glide: start ~1800 Hz, rise to 2200 in 80 ms, fall to ~1300 Hz by 450 ms; add vibrato 35 Hz depth +/-40 Hz; harmonics 2f at 0.5, 3f at 0.25; envelope fast attack, slow release. Do 2-3 calls with 0.25 s gaps; random pitch per call. Sounds like a squeaky "kee-ow" (guess; tune by ear). |
| **Lifeguard whistle** (0.6 s) | Sine ~2800 Hz with FM wobble: `sin(2*pi*2800*t + 3*sin(2*pi*30*t))` (this gives the "pea" flutter) + band-passed noise at 20 % for breath. Attack 15 ms, release 80 ms. A "fweet-fweeet": two notes 0.3 s with a 0.05 s gap. |
| **Crowd cheer** (1.5 s) | Several voices fake: 8-12 layers of saw wave around 220-400 Hz with random vibrato and random detune, filtered to formant-like bands (800 Hz and 1200 Hz band-pass), plus noise. Swell 0.3 s, fall 1 s. Sounds crude (guess); layer with filtered noise "applause". Fallback: record-free CC0 cheer from freesound. |
| **Crowd murmur bed** | Pink noise band-passed 300-1500 Hz with random slow amplitude modulation + occasional faint "voice-like" blips (short saw glides with formant filter). Low level. |
| **Footstep on deck** (optional) | Noise burst, low-passed 1.2 kHz, 80 ms, with 20 Hz thump. 4 seeds. |

**Plucked string (Karplus-Strong):**
```python
def pluck(freq, sec=1.5, damp=0.996, seed=0):
    n=int(SR/freq); buf=np.random.default_rng(seed).uniform(-1,1,n); out=np.zeros(int(SR*sec))
    for i in range(len(out)):
        out[i]=buf[i%n]; buf[i%n]=damp*0.5*(buf[i%n]+buf[(i+1)%n])
    return out
```
Higher `damp` = longer sustain. Soften attack by low-passing the initial noise (smooth `buf` once) for a nylon/uke feel. Pitch accuracy is slightly off for high notes (integer delay length): fine for background music, or use fractional delay (guess it is fine).

**Simple looping chill track recipe (~16 bars @ 90 BPM = about 43 s):**
1. beat = 60/90 s. Chords (one per bar, 4 bars repeated 4x): Cmaj7, Am7, Fmaj7, G6.
2. Chord pluck: strum 4 notes with 25 ms offsets using `pluck`, on beats 1 and 2.5 (a gentle syncopation).
3. Bass: pluck an octave+ lower on the root, beat 1 and 3.
4. Melody: pentatonic (C D E G A), 1-3 notes per bar, random with a seed, quiet vibes (sine + 4f partial, decay 0.8 s). Rest often.
5. Pad: sum of chord sines with 1.5 s attack, very quiet.
6. Shaker: high-passed noise 40 ms on every offbeat, -20 dB.
7. Render bar by bar into a buffer so the **loop end = start** (let tails wrap by adding the overflow back to the beginning). Make 2-3 tracks with different progressions and seeds.

## 6. Unity wiring notes
- Import WAV as Vorbis, "Load Type: Streaming" for music and long beds; "Decompress on Load" for short one-shots.
- Looping beds: set `loop=true`; they need seamless ends (crossfade in Python).
- A `SfxPlayer` helper that takes a clip list, applies random pitch/volume and a min-gap, plus `AudioMixer` groups, covers 90 % of this. (Design note only, we do not write code here.)

## Ideas for our game
1. **Ka-ching on every ticket sale** at the desk (chime recipe, 4 variants, pitch +/-6 %); bigger chime for tips and VIP sessions. Also tick the money HUD with a tiny coin pitter.
2. **Wave sequence audio:** machine hum + rising whoosh before each wave, crash on break, whitewash tail. Scale crash length/lowpass with wave size (Beginner / Intermediate / Pro).
3. **Constant beach bed:** 30 s surf loop + wind + very quiet crowd murmur whose volume scales with surfers in the park.
4. **Mission-complete / unlock arpeggio** with a 5 dB music duck; bigger version for a new pool.
5. **Lifeguard whistle** when the session opens/closes and at end-of-day report; seagulls every 8-25 s (random, daytime only).
6. **Crowd "whoo!" cheer** when a surfer lands a trick or rides a barrel - the sound version of the "+$" pop.
7. **Splash on wipeout** (funny bubble-blip splash) - supports the wipeout comedy in the parking lot.
8. **UI**: soft click/hover/confirm/denied set for the SurfOS screens and menus.
9. **Music:** 3 chill tracks at 90 BPM (uke + marimba), -18 dB, shuffled, ducked on events; slightly different (faster, brighter) track for day, calmer for end-of-day report.
10. Build a single `tools/audio/make_sounds.py` (outside docs; later task) that writes WAVs into `Assets/_Game/Audio/` with seeds, so tweaking is re-running a script.
