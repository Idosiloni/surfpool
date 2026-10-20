"""
Draws the UI art (backdrops, poster, patterns) for the "Sunset Surf Shop" look and writes PNGs to Assets/_Game/UI/Art/.
Run: python3 tools/ui/make_ui_art.py [out_dir]   (numpy only, 4x supersampled like make_icons.py)

- backdrop.png       dusk gradient behind full-screen windows (stretched)
- poster.png         retro surf poster: striped sun over three wave bands (main menu)
- header_waves.png   faint white wave lines, tiled along window headers
- divider.png        wavy line, tiled as a section divider (white, tinted in USS)
"""
import os
import struct
import sys
import zlib

import numpy as np

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "UI", "Art")


def hex_rgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


def save(name, rgba):
    """rgba: float array (H, W, 4) in 0..1, straight alpha."""
    h, w = rgba.shape[:2]
    data = (np.clip(rgba, 0, 1) * 255).round().astype(np.uint8)
    raw = b"".join(b"\x00" + data[y].tobytes() for y in range(h))

    def chunk(tag, payload):
        return struct.pack(">I", len(payload)) + tag + payload + struct.pack(">I", zlib.crc32(tag + payload) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, name + ".png"), "wb") as f:
        f.write(png)
    print("art", name, w, "x", h)


def downsample(a, ss):
    h, w = a.shape[0] // ss, a.shape[1] // ss
    return a.reshape(h, ss, w, ss, *a.shape[2:]).mean(axis=(1, 3))


def backdrop():
    """Deep sea at the top, dusk purple in the middle, a warm coral glow at the bottom."""
    h = 512
    t = np.linspace(0, 1, h)[:, None]
    stops = [(0.0, "#0B2E3A", 0.62), (0.55, "#3B2F6B", 0.52), (1.0, "#FF7A59", 0.50)]
    rgb = np.zeros((h, 1, 3), np.float32)
    alpha = np.zeros((h, 1), np.float32)
    for (t0, c0, a0), (t1, c1, a1) in zip(stops, stops[1:]):
        m = ((t >= t0) & (t <= t1))[:, 0]
        k = ((t[m] - t0) / (t1 - t0))
        k = k * k * (3 - 2 * k)
        rgb[m, 0] = hex_rgb(c0) * (1 - k) + hex_rgb(c1) * k
        alpha[m, 0] = (a0 * (1 - k) + a1 * k)[:, 0]
    rgba = np.concatenate([np.repeat(rgb, 4, axis=1), np.repeat(alpha[..., None], 4, axis=1)], axis=2)
    save("backdrop", rgba)


def poster():
    """70s surf-contest poster: a sun with cut stripes sinking behind teal / coral / yellow wave bands."""
    size, ss = 1024, 2
    n = size * ss
    ys, xs = np.mgrid[0:n, 0:n]
    x = (xs + 0.5) / n
    y = (ys + 0.5) / n  # 0 top .. 1 bottom
    rgb = np.zeros((n, n, 3), np.float32)
    a = np.zeros((n, n), np.float32)

    def paint(mask, color):
        rgb[mask] = hex_rgb(color)
        a[mask] = 1.0

    r = np.hypot(x - 0.5, y - 0.5)
    # Warm sky behind everything: cream at the top, peach toward the horizon.
    sky = r < 0.49
    ks = np.clip(y / 0.7, 0, 1)[..., None]
    sky_rgb = hex_rgb("#FFF3DC") * (1 - ks) + hex_rgb("#FFC9A3") * ks
    rgb[sky] = sky_rgb[sky]
    a[sky] = 1.0

    # Sun: yellow top fading to orange, with horizontal gaps that get thicker toward the horizon.
    sun = r < 0.40
    gaps = np.zeros_like(sun)
    for i, (yc, th) in enumerate([(0.56, 0.010), (0.62, 0.016), (0.68, 0.022), (0.74, 0.028)]):
        gaps |= np.abs(y - yc) < th / 2
    sun &= ~gaps
    k = np.clip((y - 0.1) / 0.8, 0, 1)[..., None]
    sun_rgb = hex_rgb("#FFD45E") * (1 - k) + hex_rgb("#FF8A3D") * k
    rgb[sun] = sun_rgb[sun]
    a[sun] = 1.0

    # Wave bands (front to back is bottom to top): each a wavy top edge with a curling lip.
    bands = [(0.64, "#14B8A6", 0.030, 3.0, 0.0), (0.72, "#FF6B57", 0.026, 3.5, 1.3), (0.80, "#FFC857", 0.022, 4.0, 2.2),
             (0.88, "#0B2E3A", 0.018, 4.5, 0.4)]
    for base, color, amp, freq, phase in bands:
        edge = base + amp * np.sin(x * np.pi * 2 * freq + phase) + 0.5 * amp * np.sin(x * np.pi * 2 * freq * 2.3 + phase)
        mask = (y > edge) & (r < 0.48)
        paint(mask, color)
        # Thin cream foam line on each crest.
        foam = (np.abs(y - edge - 0.010) < 0.004) & (r < 0.46)
        paint(foam, "#FFF7EC")

    # Soft round crop so the poster reads as a badge, with an ink ring.
    ring = (r > 0.48) & (r < 0.495)
    paint(ring, "#0B2E3A")
    a[r >= 0.495] = 0.0

    rgba = np.concatenate([rgb, a[..., None]], axis=2)
    save("poster", downsample(rgba, ss))


def header_waves():
    """Tileable faint wave lines (white, ~14% alpha) for the ink window header strip."""
    w, h, ss = 256, 64, 4
    ys, xs = np.mgrid[0:h * ss, 0:w * ss]
    x = (xs + 0.5) / (w * ss)
    y = (ys + 0.5) / (h * ss)
    d = np.full(x.shape, 9.0)
    for row, yc in enumerate((0.25, 0.75)):
        shift = 0.25 * row
        wave = yc + 0.12 * np.sin((x + shift) * np.pi * 2 * 2)  # 2 periods per tile, tiles seamlessly
        d = np.minimum(d, np.abs(y - wave) * h)  # distance in pixels (approx)
    alpha = (d < 2.2).astype(np.float32) * 0.16
    rgba = np.ones((h * ss, w * ss, 4), np.float32)
    rgba[..., 3] = alpha
    save("header_waves", downsample(rgba, ss))


def divider():
    """A wavy line, 64 x 16 tile, white (tinted in USS)."""
    w, h, ss = 64, 16, 4
    ys, xs = np.mgrid[0:h * ss, 0:w * ss]
    x = (xs + 0.5) / (w * ss)
    y = (ys + 0.5) / (h * ss)
    wave = 0.5 + 0.28 * np.sin(x * np.pi * 2)
    d = np.abs(y - wave) * h
    rgba = np.ones((h * ss, w * ss, 4), np.float32)
    rgba[..., 3] = (d < 1.6).astype(np.float32)
    save("divider", downsample(rgba, ss))


def main():
    backdrop()
    poster()
    header_waves()
    divider()


if __name__ == "__main__":
    main()
