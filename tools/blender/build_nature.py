"""
Builds the dune and beach plants and props for the island around the park (Assets/_Game/Editor/Shore.cs places them):
two clumps of dune grass, two coastal bushes, a weathered sand fence, a piece of driftwood and a short palm, then
exports each as FBX into Assets/_Game/Models/. Only these models are exported (the others keep their files).

Run in Blender (Blender MCP): exec(open(path).read(), ns). Borrows helpers/palette/export from build_models.py
(everything above its final build line), like build_luxury.py does.

Conventions (same as build_models.py): models sit on z = 0, centered on x/y. A Blender point (x, y, z) lands in Unity
at (-x, z, -y). The fence runs along Blender X.
"""
import math
import random
import bpy

_here = "/Users/ido/surf-pool-sim/tools/blender/build_models.py"
_src = open(_here).read()
exec(_src[:_src.rindex("result = {")], globals())
EXPORT_DIR = globals().get("EXPORT_DIR_OVERRIDE", EXPORT_DIR)

PALETTE.update({
    "DuneGreen": (0.47, 0.62, 0.30),
    "DuneGreenLight": (0.64, 0.73, 0.38),
    "Straw": (0.80, 0.72, 0.46),
    "BushLeaf": (0.26, 0.55, 0.27),
    "BushLeafLight": (0.38, 0.66, 0.32),
    "Twig": (0.42, 0.31, 0.21),
    "Driftwood": (0.80, 0.75, 0.66),
    "DriftwoodDark": (0.62, 0.56, 0.47),
    "FenceWood": (0.72, 0.62, 0.48),
})


def blade(name, base, angle, height, lean, width, mat, segments=3):
    """One grass blade: a thin strip that leans out and curls over, tapering to a point (2 cm thick)."""
    ca, sa = math.cos(angle), math.sin(angle)
    px, py = -sa, ca  # across the blade
    verts = []
    for k in range(segments + 1):
        t = k / segments
        out = lean * t * t  # bends more toward the tip
        x, y, z = base[0] + ca * out, base[1] + sa * out, height * (t - 0.18 * t * t * lean / max(height, 0.01))
        w = width * (1 - t) + 0.004
        verts += [(x - px * w, y - py * w, z), (x + px * w, y + py * w, z)]
    faces = [(2 * k, 2 * k + 1, 2 * k + 3, 2 * k + 2) for k in range(segments)]
    return mesh_object(name, verts, faces, mat, double_sided=True)


def dune_grass(name, seed, blades=26, height=0.75):
    """A clump of marram grass: long blades fanning out from a tight base, two greens with a few straw-dry ones."""
    rng = random.Random(seed)
    parts = []
    for i in range(blades):
        a = rng.uniform(0, math.tau)
        r = rng.uniform(0, 0.12)
        h = height * rng.uniform(0.55, 1.1)
        lean = rng.uniform(0.15, 0.45) * h
        mat = "Straw" if i % 7 == 0 else ("DuneGreenLight" if i % 2 else "DuneGreen")
        parts.append(blade("Blade", (math.cos(a) * r, math.sin(a) * r, 0), a, h, lean, rng.uniform(0.022, 0.035), mat))
    return join(name, parts)


def bush(name, seed, width=1.6, height=0.95):
    """A round coastal shrub (sea grape): a mound of soft leafy lumps on short twigs, lighter on top."""
    rng = random.Random(seed)
    parts = []
    lumps = 9
    for i in range(lumps):
        a = rng.uniform(0, math.tau)
        r = width * 0.3 * math.sqrt(rng.uniform(0.1, 1.0))
        radius = width * rng.uniform(0.17, 0.24)
        z = height * (0.62 - 0.5 * r / width) + rng.uniform(-0.05, 0.08)
        leaf = ico("Leaves", radius, (math.cos(a) * r, math.sin(a) * r, z), "BushLeafLight" if z > height * 0.55 else "BushLeaf", subdivisions=2)
        for v in leaf.data.vertices:
            v.co *= rng.uniform(0.92, 1.08)
            v.co.z *= 0.82
        parts.append(leaf)
    for i in range(3):
        a = rng.uniform(0, math.tau)
        parts.append(cylinder("Twig", 0.035, height * 0.45, (math.cos(a) * 0.12, math.sin(a) * 0.12, height * 0.2), "Twig", vertices=5,
                              rotation=(rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), 0)))
    obj = join(name, parts)
    return smooth_and_sharpen(obj, 80)  # leaves read soft, not like green rocks


def sand_fence(name="SandFence", seed=21, length=4.0):
    """A weathered dune fence: thin slats wired together between two posts, leaning a little, a few slats missing."""
    rng = random.Random(seed)
    parts = []
    for x in (-length / 2, length / 2):
        parts.append(cylinder("Post", 0.06, 1.25, (x, 0, 0.55), "DarkWood", vertices=6))
    x = -length / 2 + 0.12
    while x < length / 2 - 0.08:
        if rng.random() > 0.08:  # a missing slat now and then
            h = rng.uniform(0.92, 1.08)
            parts.append(box("Slat", (0.07, 0.025, h), (x, 0, h / 2 - 0.05), "FenceWood",
                             rotation=(rng.uniform(-0.05, 0.05), rng.uniform(-0.06, 0.06), 0)))
        x += rng.uniform(0.11, 0.14)
    for z in (0.22, 0.78):
        parts.append(box("Wire", (length, 0.012, 0.012), (0, 0.02, z), "Charcoal"))
    fence = join(name, parts)
    fence.rotation_euler = (math.radians(4), 0, 0)  # leaning with the wind
    return fence


def driftwood(name="Driftwood", seed=31):
    """A sun-bleached log washed up on the beach, with a couple of broken branch stubs."""
    rng = random.Random(seed)
    # lathe() builds along Z, so build the log standing up and lay it down along X.
    path = [Vector((0.04 * math.sin(t * 5.0), 0.12 * math.sin(t * 3.0), 2.6 * t)) for t in (k / 6 for k in range(7))]
    profile = [0.16, 0.18, 0.17, 0.15, 0.13, 0.11, 0.07]
    log = lathe("Log", path, profile, ["Driftwood"] * 6, sides=7)
    log.rotation_euler = (0, math.pi / 2, 0)
    log.location = (-1.3, 0, 0.15)
    parts = [log]
    for x, a in ((-0.4, 0.8), (0.5, -1.0)):
        parts.append(cylinder("Stub", 0.06, 0.5, (x, math.copysign(0.25, a), 0.24), "DriftwoodDark", vertices=5, radius_top=0.035,
                              rotation=(a, 0, rng.uniform(-0.4, 0.4))))
    return join(name, parts)


MODELS = (
    lambda: dune_grass("DuneGrass", 3),
    lambda: dune_grass("DuneGrassTall", 8, blades=34, height=1.0),
    lambda: bush("Bush", 5),
    lambda: bush("BushSmall", 9, width=1.1, height=0.7),
    sand_fence,
    driftwood,
    lambda: palm_tree("PalmTreeShort", 4.6, seed=4),
)


def build_nature():
    clear_scene()
    built = []
    for factory in MODELS:
        obj = factory()
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        built.append(obj.name)
        if not globals().get("PREVIEW_ONLY", False):
            export(obj)
            obj.hide_set(True)
    return built


result = {"exported": build_nature()}
