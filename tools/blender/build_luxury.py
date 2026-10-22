"""
Builds the luxury-tier props (docs/research/park-layout-and-pool-shapes.md section 4: beach club -> resort -> luxury):
a shade sail, a cabana with a daybed, a fire pit and a three-tier fountain, then exports each as FBX into
Assets/_Game/Models/. Only these models are exported (the others keep their files).

Run in Blender (Blender MCP): exec(open(path).read(), ns). Borrows helpers/palette/export from build_models.py
(everything above its final build line), like build_shops.py does.

Conventions (same as build_models.py): models sit on z = 0, centered on x/y; the open / front side is Blender -Y.
A Blender point (x, y, z) lands in Unity at (-x, z, -y); WorldArt.Model adds a 180 degree yaw, so with yaw 0 the
-Y side faces Unity -Z. "Bulb" parts (the fire's flames) are swapped for a glowing material in Unity.
"""
import math
import bpy

_here = "/Users/ido/surf-pool-sim/tools/blender/build_models.py"
_src = open(_here).read()
exec(_src[:_src.rindex("result = {")], globals())
EXPORT_DIR = globals().get("EXPORT_DIR_OVERRIDE", EXPORT_DIR)

PALETTE.update({
    "Canvas": (0.97, 0.95, 0.90),
    "Stone": (0.86, 0.81, 0.72),
    "StoneDark": (0.62, 0.58, 0.52),
    "Water": (0.35, 0.74, 0.84),
    "Cushion": (0.18, 0.62, 0.62),
    "Ember": (0.78, 0.25, 0.08),
})


def ring(name, outer, inner, height, z0, mat, sides=16):
    """A flat round wall (a basin rim or fire-pit ring): an annulus with height, as one mesh."""
    verts, faces = [], []
    for k in range(sides):
        a = k / sides * math.tau
        c, s = math.cos(a), math.sin(a)
        verts += [(c * outer, s * outer, z0), (c * outer, s * outer, z0 + height),
                  (c * inner, s * inner, z0 + height), (c * inner, s * inner, z0)]
    for k in range(sides):
        a, b = k * 4, (k + 1) % sides * 4
        faces += [(a, b, b + 1, a + 1), (a + 1, b + 1, b + 2, a + 2), (a + 2, b + 2, b + 3, a + 3), (a + 3, b + 3, b, a)]
    obj = mesh_object(name, verts, faces, mat)
    recalc_normals(obj)
    return obj


def shade_sail(name="ShadeSail"):
    """A triangle of sail cloth stretched between three wooden posts of different heights (about 4.6 m across),
    sagging a little in the middle, with steel cables to the posts."""
    corners = [(-2.3, -1.4, 2.7), (2.3, -1.2, 3.25), (0.1, 2.2, 2.45)]
    parts = []
    for i, (x, y, z) in enumerate(corners):
        parts.append(cylinder("Post", 0.09, z + 0.35, (x * 1.12, y * 1.12, (z + 0.35) / 2), "DarkWood", vertices=8))
        parts.append(cylinder("Foot", 0.18, 0.12, (x * 1.12, y * 1.12, 0.06), "Concrete", vertices=8))
    # The sail: a fan of triangles from a sagging middle point, curved edges pulled in like a real sail.
    cx = sum(c[0] for c in corners) / 3
    cy = sum(c[1] for c in corners) / 3
    cz = sum(c[2] for c in corners) / 3 - 0.3
    verts, faces = [(cx, cy, cz)], []
    steps = 6
    for i in range(3):
        a, b = corners[i], corners[(i + 1) % 3]
        for k in range(steps):
            t = k / steps
            x, y, z = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t)
            pull = math.sin(t * math.pi) * 0.28  # the edge curves in toward the middle
            verts.append((x + (cx - x) * pull, y + (cy - y) * pull, z - math.sin(t * math.pi) * 0.12))
    n = 3 * steps
    for k in range(n):
        faces.append((0, 1 + k, 1 + (k + 1) % n))
    parts.append(mesh_object("Sail", verts, faces, "Canvas", double_sided=True))
    for x, y, z in corners:
        parts.append(cylinder("Cable", 0.012, 0.5, (x * 1.06, y * 1.06, z + 0.1), "Steel", vertices=4,
                              rotation=(0, math.radians(70), math.atan2(y, x))))
    return join(name, parts)


def cabana(name="Cabana"):
    """A 3.2 x 3.2 m resort cabana: white posts, a peaked canvas roof with a teal valance, curtains closing the back
    and sides (tied back at the front posts), and a daybed with a mattress and pillows. Open side = Blender -Y."""
    parts = []
    w, h = 3.2, 2.5
    half = w / 2 - 0.08
    for x in (-half, half):
        for y in (-half, half):
            parts.append(box("Post", (0.13, 0.13, h), (x, y, h / 2), "White"))
    # Roof: a low pyramid of canvas over a white frame, with a teal valance all round.
    parts.append(box("Frame", (w, w, 0.12), (0, 0, h + 0.06), "White"))
    peak = (0, 0, h + 0.95)
    rim = [(-w / 2 - 0.15, -w / 2 - 0.15), (w / 2 + 0.15, -w / 2 - 0.15), (w / 2 + 0.15, w / 2 + 0.15), (-w / 2 - 0.15, w / 2 + 0.15)]
    for i in range(4):
        (x0, y0), (x1, y1) = rim[i], rim[(i + 1) % 4]
        parts.append(mesh_object("Roof", [peak, (x0, y0, h + 0.12), (x1, y1, h + 0.12)], [(0, 1, 2)], "Canvas", double_sided=True))
    for x, y, sx, sy in ((0, -w / 2 - 0.15, w + 0.3, 0.04), (0, w / 2 + 0.15, w + 0.3, 0.04),
                         (-w / 2 - 0.15, 0, 0.04, w + 0.3), (w / 2 + 0.15, 0, 0.04, w + 0.3)):
        parts.append(box("Valance", (sx, sy, 0.26), (x, y, h + 0.02), "Cushion"))
    # Curtains: wavy panels on the back and both sides, tied-back bunches at the two front posts.
    def curtain(x0, y0, x1, y1):
        folds = 10
        verts, faces = [], []
        for k in range(folds + 1):
            t = k / folds
            wave = 0.06 * math.sin(k * math.pi)
            nx, ny = (y1 - y0), -(x1 - x0)
            length = math.hypot(nx, ny)
            ox, oy = nx / length * wave, ny / length * wave
            x, y = x0 + (x1 - x0) * t + ox, y0 + (y1 - y0) * t + oy
            verts += [(x, y, 0.05), (x, y, h - 0.05)]
        for k in range(folds):
            faces.append((2 * k, 2 * k + 2, 2 * k + 3, 2 * k + 1))
        parts.append(mesh_object("Curtain", verts, faces, "Canvas", double_sided=True))
    curtain(-half, half, half, half)
    curtain(-half, -half + 0.9, -half, half)
    curtain(half, -half + 0.9, half, half)
    for x in (-half, half):
        parts.append(cylinder("Tied Curtain", 0.16, h - 0.3, (x, -half + 0.12, (h - 0.3) / 2 + 0.2), "Canvas", vertices=8, radius_top=0.08))
        parts.append(box("Tie", (0.36, 0.36, 0.06), (x, -half + 0.12, 1.1), "Cushion"))
    # The daybed.
    parts.append(box("Bed Base", (2.3, 1.9, 0.36), (0, 0.35, 0.18), "DarkWood"))
    parts.append(box("Mattress", (2.2, 1.8, 0.2), (0, 0.35, 0.46), "White"))
    for x in (-0.65, 0.0, 0.65):
        parts.append(box("Pillow", (0.55, 0.22, 0.34), (x, 1.12, 0.66), "Cushion" if x != 0 else "Canvas", rotation=(math.radians(-15), 0, 0)))
    parts.append(box("Towel", (0.5, 0.7, 0.04), (0.6, 0.0, 0.58), "Coral"))
    return join(name, parts)


def fire_pit(name="FirePit"):
    """A low round stone fire pit (1.6 m), logs inside and a cluster of flames (the "Bulb" material glows in Unity)."""
    parts = [ring("Ring", 0.8, 0.58, 0.42, 0.0, "Stone"),
             cylinder("Bed", 0.6, 0.1, (0, 0, 0.12), "Charcoal", vertices=16)]
    for k in range(5):
        a = k / 5 * math.tau
        parts.append(cylinder("Log", 0.06, 0.62, (math.cos(a) * 0.12, math.sin(a) * 0.12, 0.26), "DarkWood", vertices=6,
                              rotation=(math.radians(68), 0, a + math.pi / 2)))
    parts.append(ico("Embers", 0.2, (0, 0, 0.22), "Ember"))
    for k, (r, hgt) in enumerate(((0.0, 0.75), (0.14, 0.5), (0.13, 0.55), (0.12, 0.45))):
        a = k * 2.1
        parts.append(cylinder("Flame", 0.13 if k == 0 else 0.09, hgt, (math.cos(a) * r, math.sin(a) * r, 0.3 + hgt / 2), "Bulb",
                              vertices=6, radius_top=0.0))
    return join(name, parts)


def fountain(name="Fountain"):
    """A three-tier stone fountain (basin 4 m across): water in each bowl, a column with a finial on top."""
    parts = [ring("Basin", 2.0, 1.75, 0.55, 0.0, "Stone"),
             cylinder("Basin Floor", 1.8, 0.1, (0, 0, 0.05), "StoneDark", vertices=20),
             cylinder("Basin Water", 1.78, 0.04, (0, 0, 0.42), "Water", vertices=20),
             cylinder("Column", 0.22, 2.2, (0, 0, 1.1), "Stone", vertices=10)]
    for radius, z in ((1.0, 1.15), (0.55, 1.85)):
        parts.append(cylinder("Bowl", radius, 0.22, (0, 0, z), "Stone", vertices=16, radius_top=radius * 0.85))
        parts.append(cylinder("Bowl Water", radius * 0.8, 0.03, (0, 0, z + 0.12), "Water", vertices=16))
    parts.append(ico("Finial", 0.16, (0, 0, 2.3), "Stone"))
    return join(name, parts)


MODELS = (shade_sail, cabana, fire_pit, fountain)


def build_luxury():
    clear_scene()
    built = []
    for factory in MODELS:
        obj = factory()
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        built.append(obj.name)
        export(obj)
        obj.hide_set(True)
    return built


result = {"exported": build_luxury()}
