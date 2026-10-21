"""
Fabric prints for people's clothes (boardshorts, aloha shirts, rash guards): tileable 256x256 PNGs in
Assets/_Game/Textures/Patterns. They are white with a lighter/darker motif, because CharacterRig multiplies them by
the cloth color: a teal pair of shorts with the hibiscus print gets pale flowers on teal.
Order matters (Pattern enum without None): stripes, hibiscus, checker, palms, dots.

Run: python3 tools/art/make_patterns.py
"""
import math
import os
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Textures", "Patterns")
S = 256
BASE = (218, 218, 218, 255)      # the cloth itself (a bit below white, so motifs can be lighter)
LIGHT = (255, 255, 255, 255)
DARK = (150, 150, 150, 255)


def tiled(draw_fn):
    """Draw at 3x3 offsets so shapes crossing an edge wrap around (seamless tiling)."""
    img = Image.new("RGBA", (S, S), BASE)
    d = ImageDraw.Draw(img)
    for ox in (-S, 0, S):
        for oy in (-S, 0, S):
            draw_fn(d, ox, oy)
    return img


def stripes():
    def f(d, ox, oy):
        for i in range(4):
            y = i * 64 + oy
            d.rectangle([0 + ox, y, S + ox, y + 22], fill=LIGHT)
            d.rectangle([0 + ox, y + 30, S + ox, y + 36], fill=DARK)
    return tiled(f)


def flower(d, cx, cy, r, color, center):
    for k in range(5):
        a = k * 2 * math.pi / 5 + 0.3
        px, py = cx + math.cos(a) * r * 0.62, cy + math.sin(a) * r * 0.62
        d.ellipse([px - r * 0.48, py - r * 0.48, px + r * 0.48, py + r * 0.48], fill=color)
    d.ellipse([cx - r * 0.22, cy - r * 0.22, cx + r * 0.22, cy + r * 0.22], fill=center)


def hibiscus():
    spots = [(40, 50, 30), (150, 30, 24), (210, 140, 32), (90, 160, 26), (30, 220, 22), (170, 220, 20), (120, 95, 14)]
    def f(d, ox, oy):
        for x, y, r in spots:
            # a leaf behind each flower
            d.ellipse([x + ox + r * 0.2, y + oy - r * 0.2, x + ox + r * 1.4, y + oy + r * 0.45], fill=DARK)
            flower(d, x + ox, y + oy, r, LIGHT, DARK)
    return tiled(f)


def checker():
    img = Image.new("RGBA", (S, S), BASE)
    d = ImageDraw.Draw(img)
    n = 4
    c = S // n
    for i in range(n):
        for j in range(n):
            if (i + j) % 2 == 0:
                d.rectangle([i * c, j * c, (i + 1) * c - 1, (j + 1) * c - 1], fill=LIGHT)
            else:
                d.rectangle([i * c, j * c, (i + 1) * c - 1, (j + 1) * c - 1], fill=DARK)
    return img


def palms():
    spots = [(60, 70), (190, 150), (110, 220)]
    def f(d, ox, oy):
        for x, y in spots:
            x, y = x + ox, y + oy
            d.line([x, y + 40, x + 6, y - 5], fill=DARK, width=7)
            for k in range(6):
                a = math.pi * (1.1 + k * 0.16)
                d.line([x + 6, y - 5, x + 6 + math.cos(a) * 34, y - 5 + math.sin(a) * 22 + 10], fill=LIGHT, width=8)
        d.ellipse([140 + ox, 40 + oy, 170 + ox, 70 + oy], fill=LIGHT)
    return tiled(f)


def dots():
    def f(d, ox, oy):
        for i in range(4):
            for j in range(4):
                x = i * 64 + (32 if j % 2 else 0) + 16 + ox
                y = j * 64 + 16 + oy
                d.ellipse([x - 11, y - 11, x + 11, y + 11], fill=LIGHT)
    return tiled(f)


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in (("stripes", stripes), ("hibiscus", hibiscus), ("checker", checker), ("palms", palms), ("dots", dots)):
        fn().save(os.path.join(OUT, f"pattern_{name}.png"))
        print("wrote", name)


if __name__ == "__main__":
    main()
