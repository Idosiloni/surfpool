"""
Draws the UI icons (white shapes on transparent, tinted in USS) and writes PNGs to Assets/_Game/UI/Icons/.
Run: python3 tools/ui/make_icons.py   (numpy only; shapes are distance fields, 4x supersampled for smooth edges)
"""
import os
import struct
import zlib

import numpy as np

SIZE, SS = 128, 4
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "UI", "Icons")
N = SIZE * SS
ys, xs = np.mgrid[0:N, 0:N]
X = (xs + 0.5) / N  # 0..1, left to right
Y = 1.0 - (ys + 0.5) / N  # 0..1, bottom to top


def circle(cx, cy, r):
    return np.hypot(X - cx, Y - cy) - r


def ring(cx, cy, r, w):
    return np.abs(np.hypot(X - cx, Y - cy) - r) - w / 2


def segment(ax, ay, bx, by, w):
    px, py = X - ax, Y - ay
    dx, dy = bx - ax, by - ay
    t = np.clip((px * dx + py * dy) / (dx * dx + dy * dy), 0, 1)
    return np.hypot(px - t * dx, py - t * dy) - w / 2


def polyline(points, w):
    d = np.full(X.shape, 9.0)
    for (ax, ay), (bx, by) in zip(points, points[1:]):
        d = np.minimum(d, segment(ax, ay, bx, by, w))
    return d


def polygon(points):
    """Signed distance-ish: negative inside (even-odd), distance to edges."""
    inside = np.zeros(X.shape, bool)
    edge = np.full(X.shape, 9.0)
    n = len(points)
    for i in range(n):
        (ax, ay), (bx, by) = points[i], points[(i + 1) % n]
        cond = ((ay > Y) != (by > Y)) & (X < (bx - ax) * (Y - ay) / (by - ay + 1e-9) + ax)
        inside ^= cond
        edge = np.minimum(edge, segment(ax, ay, bx, by, 0))
    return np.where(inside, -edge, edge)


def rounded_rect(cx, cy, hw, hh, r):
    qx, qy = np.abs(X - cx) - hw + r, np.abs(Y - cy) - hh + r
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r


def ellipse(cx, cy, rx, ry, angle=0.0):
    c, s = np.cos(angle), np.sin(angle)
    u = ((X - cx) * c + (Y - cy) * s) / rx
    v = (-(X - cx) * s + (Y - cy) * c) / ry
    return (np.hypot(u, v) - 1) * min(rx, ry)


def union(*ds):
    return np.minimum.reduce(ds)


def cut(a, b):
    return np.maximum(a, -b)


def save(name, d):
    alpha = (d < 0).astype(np.float32).reshape(SIZE, SS, SIZE, SS).mean(axis=(1, 3))
    rgba = np.zeros((SIZE, SIZE, 4), np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = (alpha * 255).round().astype(np.uint8)
    raw = b"".join(b"\x00" + rgba[y].tobytes() for y in range(SIZE))

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, name + ".png"), "wb") as f:
        f.write(png)


def wave():
    xs_ = np.linspace(0.1, 0.9, 40)
    crest = [(x, 0.42 + 0.12 * np.sin((x - 0.1) * 2 * np.pi * 0.9)) for x in xs_]
    curl = [(0.62 + 0.14 * np.cos(a), 0.62 + 0.14 * np.sin(a)) for a in np.linspace(-0.6, 3.6, 30)]
    lower = [(x, 0.2 + 0.06 * np.sin((x - 0.1) * 2 * np.pi * 1.2)) for x in xs_]
    return union(polyline(crest, 0.09), polyline(curl, 0.09), polyline(lower, 0.08))


def palm():
    trunk = polyline([(0.5 + 0.08 * np.sin(t * 2), 0.08 + t * 0.62) for t in np.linspace(0, 1, 20)], 0.08)
    leaves = union(ellipse(0.33, 0.72, 0.2, 0.06, 0.45), ellipse(0.67, 0.72, 0.2, 0.06, -0.45),
                   ellipse(0.38, 0.82, 0.16, 0.055, -0.3), ellipse(0.62, 0.82, 0.16, 0.055, 0.3), circle(0.53, 0.74, 0.07))
    return union(trunk, leaves)


def target():
    return union(ring(0.5, 0.5, 0.36, 0.09), ring(0.5, 0.5, 0.2, 0.09), circle(0.5, 0.5, 0.07))


def pin():
    head = circle(0.5, 0.62, 0.26)
    tip = polygon([(0.29, 0.52), (0.71, 0.52), (0.5, 0.1)])
    return cut(union(head, tip), circle(0.5, 0.63, 0.1))


def flag():
    return union(segment(0.25, 0.1, 0.25, 0.9, 0.08), polygon([(0.27, 0.88), (0.82, 0.72), (0.27, 0.54)]))


def pool():
    body = rounded_rect(0.5, 0.5, 0.4, 0.3, 0.1)
    inner = rounded_rect(0.5, 0.5, 0.32, 0.22, 0.06)
    waves = union(*[polyline([(x, y + 0.03 * np.sin(x * 25)) for x in np.linspace(0.24, 0.76, 25)], 0.05) for y in (0.42, 0.58)])
    return union(cut(body, inner), waves)


def board():
    b = ellipse(0.5, 0.5, 0.42, 0.11, 0.8)
    return cut(b, segment(0.22, 0.22, 0.78, 0.78, 0.025))


def person():
    return union(circle(0.5, 0.74, 0.14), cut(rounded_rect(0.5, 0.3, 0.26, 0.24, 0.2), rounded_rect(0.5, 0.02, 0.4, 0.1, 0.0)))


def tag():
    body = polygon([(0.15, 0.55), (0.45, 0.85), (0.85, 0.85), (0.85, 0.45), (0.55, 0.15)])
    return cut(body, circle(0.7, 0.7, 0.07))


def smile():
    face = cut(circle(0.5, 0.5, 0.4), circle(0.5, 0.5, 0.31))
    eyes = union(circle(0.38, 0.58, 0.05), circle(0.62, 0.58, 0.05))
    mouth = polyline([(0.5 + 0.17 * np.cos(a), 0.47 + 0.17 * np.sin(a)) for a in np.linspace(-2.6, -0.54, 20)], 0.06)
    return union(face, eyes, mouth)


def frown():
    face = cut(circle(0.5, 0.5, 0.4), circle(0.5, 0.5, 0.31))
    eyes = union(circle(0.38, 0.58, 0.05), circle(0.62, 0.58, 0.05))
    mouth = polyline([(0.5 + 0.15 * np.cos(a), 0.25 + 0.15 * np.sin(a)) for a in np.linspace(0.6, 2.54, 20)], 0.06)
    return union(face, eyes, mouth)


def trash():
    lid = rounded_rect(0.5, 0.76, 0.32, 0.04, 0.02)
    handle = rounded_rect(0.5, 0.83, 0.08, 0.04, 0.02)
    can = polygon([(0.24, 0.68), (0.76, 0.68), (0.7, 0.1), (0.3, 0.1)])
    slots = union(segment(0.4, 0.2, 0.4, 0.58, 0.05), segment(0.5, 0.2, 0.5, 0.58, 0.05), segment(0.6, 0.2, 0.6, 0.58, 0.05))
    return union(lid, handle, cut(can, slots))


def bolt():
    return polygon([(0.58, 0.92), (0.22, 0.45), (0.46, 0.45), (0.38, 0.08), (0.78, 0.58), (0.54, 0.58)])


def star():
    pts = []
    for i in range(10):
        a = np.pi / 2 + i * np.pi / 5
        r = 0.42 if i % 2 == 0 else 0.18
        pts.append((0.5 + r * np.cos(a), 0.48 + r * np.sin(a)))
    return polygon(pts)


def clock():
    return union(ring(0.5, 0.5, 0.36, 0.09), segment(0.5, 0.5, 0.5, 0.74, 0.07), segment(0.5, 0.5, 0.66, 0.42, 0.07))


def coin():
    s = polyline([(0.6 + 0.1 * np.cos(a), 0.58 + 0.08 * np.sin(a)) for a in np.linspace(0, np.pi * 1.5, 12)]
                 + [(0.6 - 0.1 * np.cos(a), 0.42 + 0.08 * np.sin(a)) for a in np.linspace(np.pi * 0.5, -np.pi, 12)], 0.06)
    return union(cut(circle(0.5, 0.5, 0.42), circle(0.5, 0.5, 0.33)), s, segment(0.5, 0.26, 0.5, 0.74, 0.05))


def main():
    for name, fn in [("wave", wave), ("palm", palm), ("target", target), ("pin", pin), ("flag", flag), ("pool", pool),
                     ("board", board), ("person", person), ("tag", tag), ("smile", smile), ("frown", frown),
                     ("trash", trash), ("bolt", bolt), ("star", star), ("clock", clock)]:
        save(name, fn())
        print("icon", name)


if __name__ == "__main__":
    main()
