"""
Builds the low-poly surf park models in Blender and exports each one as an FBX into
Assets/_Game/Models/. Run inside Blender (e.g. via the Blender MCP):

    exec(open("/Users/ido/surf-pool-sim/tools/blender/build_models.py").read())

Every model is built from simple primitives with a few flat-color materials (the park palette below). The look
(docs/research/blender-high-level.md): every box and cylinder gets a small bevel BEFORE parts are joined, so light
catches the edges like a toy; the joined model is smooth-shaded with edges sharper than 40 degrees kept hard and
weighted normals, so flat faces stay clean and the bevels read round; thin sheets (fronds, canopies, flags) get real
thickness from Solidify. Models sit on z = 0 (Blender is Z-up) and are centered on x/y.
Re-running rebuilds and re-exports everything.
"""
import math
import os
import random

import bmesh
import bpy
from mathutils import Vector

EXPORT_DIR = "/Users/ido/surf-pool-sim/Assets/_Game/Models"

PALETTE = {
    "Sand": (0.91, 0.83, 0.63),
    "Wood": (0.69, 0.48, 0.29),
    "DarkWood": (0.45, 0.30, 0.18),
    "Trunk": (0.55, 0.43, 0.31),
    "TrunkDark": (0.47, 0.36, 0.26),
    "Leaf": (0.30, 0.69, 0.31),
    "LeafDark": (0.20, 0.52, 0.25),
    "Coconut": (0.40, 0.27, 0.15),
    "White": (0.96, 0.95, 0.92),
    "Teal": (0.18, 0.77, 0.71),
    "Coral": (0.94, 0.28, 0.44),
    "Yellow": (1.00, 0.82, 0.40),
    "Navy": (0.06, 0.13, 0.18),
    "Concrete": (0.80, 0.80, 0.77),
    "Steel": (0.55, 0.60, 0.64),
    "Glass": (0.35, 0.62, 0.78),
    "Red": (0.88, 0.20, 0.18),
    "Rock": (0.62, 0.60, 0.56),
    "Charcoal": (0.17, 0.18, 0.21),
    "KeyGrey": (0.80, 0.82, 0.84),
    "Green": (0.22, 0.78, 0.36),
    "Screen": (0.10, 0.35, 0.45),
    "BoardDeck": (0.97, 0.97, 0.95),   # tinted per surfer in Unity
    "Stringer": (0.55, 0.38, 0.22),
    "CarPaint": (0.95, 0.95, 0.95),    # tinted per car in Unity
    "Tire": (0.12, 0.12, 0.13),
    "Headlight": (1.0, 0.95, 0.7),
    "Taillight": (0.85, 0.12, 0.1),
    "Rust": (0.55, 0.27, 0.13),
    "Plywood": (0.62, 0.52, 0.38),
    "DeadGrass": (0.62, 0.58, 0.32),
    "Bulb": (1.0, 0.86, 0.5),          # swapped for a glowing material in Unity
    "Flower": (0.98, 0.45, 0.62),
    "Tan": (0.93, 0.72, 0.55),
}


# ---------------------------------------------------------------- helpers

def material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
    color = PALETTE[name]
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.85
    mat.diffuse_color = (*color, 1.0)
    return mat


def clear_scene():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)


BEVEL = 0.025  # m: the light-catching chamfer on every part's hard edges
SHARP_ANGLE = 40  # degrees: edges sharper than this stay hard when a model is smoothed


def finish(obj, mat_name):
    obj.data.materials.clear()
    obj.data.materials.append(material(mat_name))
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj


def apply_modifier(obj, modifier):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def bevel(obj, width, angle=30, segments=2):
    """Chamfers the edges sharper than `angle` (parts thinner than ~5 cm stay crisp: string lights, grille bars)."""
    if width < 0.004:
        return obj
    m = obj.modifiers.new("Bevel", "BEVEL")
    m.width = width
    m.segments = segments
    m.limit_method = "ANGLE"
    m.angle_limit = math.radians(angle)
    apply_modifier(obj, m)
    return obj


def solidify(obj, thickness=0.02):
    """Real thickness for a thin sheet (a frond, a canopy, a flag): edges that catch light, no z-fighting."""
    m = obj.modifiers.new("Solidify", "SOLIDIFY")
    m.thickness = thickness
    m.offset = 0
    apply_modifier(obj, m)
    return obj


def smooth_and_sharpen(obj, angle=SHARP_ANGLE):
    """Smooth shading, hard edges kept sharp, weighted normals: big faces stay flat-lit, bevels look rounded."""
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(angle))
    m = obj.modifiers.new("WeightedNormal", "WEIGHTED_NORMAL")
    m.keep_sharp = True
    m.weight = 50
    bpy.ops.object.modifier_apply(modifier=m.name)
    return obj


def box(name, size, location, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel(obj, min(BEVEL, min(size) * 0.25) if min(size) >= 0.05 else 0)
    return finish(obj, mat)


def cylinder(name, radius, depth, location, mat, vertices=8, rotation=(0, 0, 0), radius_top=None):
    if radius_top is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius_top, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    # Only the cap rims get the chamfer (the sides of an 8-sided cylinder meet at 45 degrees and stay as they are).
    bevel(obj, min(BEVEL, radius * 0.25, depth * 0.25) if min(radius, depth) >= 0.05 else 0, angle=60)
    return finish(obj, mat)


def ico(name, radius, location, mat, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=radius, location=location)
    obj = bpy.context.active_object
    obj.name = name
    return finish(obj, mat)


def mesh_object(name, verts, faces, mat, double_sided=False):
    """Mesh from raw data. double_sided = a thin sheet seen from both sides: it gets 2 cm of real thickness."""
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if double_sided:
        solidify(obj, 0.02)
    return finish(obj, mat)


def recalc_normals(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()


def join(name, parts):
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    # Pivot at the world origin = the model's base center (models are built around it).
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    # Merge duplicate material slots so the FBX has one slot per color.
    bpy.ops.object.material_slot_remove_unused()
    smooth_and_sharpen(obj)
    return obj


def export(obj):
    os.makedirs(EXPORT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(EXPORT_DIR, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
    )
    return path


# Axis note: with these export settings a Blender point (x, y, z) lands in Unity at (-x, z, -y).
# So a model's Blender -Y side faces Unity +Z, and its Blender +X side faces Unity -X.

# ---------------------------------------------------------------- models

def lathe(name, path, profile, mats, sides=8):
    """One continuous round mesh along a path: rings of `sides` points, ring i centered at path[i] with radius
    profile[i]; mats[i] colors the band between ring i and i+1 (for trunk ring scars, stripes)."""
    verts, faces, face_mats = [], [], []
    for center, radius in zip(path, profile):
        for k in range(sides):
            a = k / sides * math.tau
            verts.append((center.x + math.cos(a) * radius, center.y + math.sin(a) * radius, center.z))
    for i in range(len(path) - 1):
        for k in range(sides):
            a, b = i * sides + k, i * sides + (k + 1) % sides
            faces.append((a, b, b + sides, a + sides))
            face_mats.append(mats[i])
    bottom, top = len(verts), len(verts) + 1
    verts += [tuple(path[0]), tuple(path[-1])]
    for k in range(sides):
        faces.append((bottom, (k + 1) % sides, k))
        face_mats.append(mats[0])
        n = (len(path) - 1) * sides
        faces.append((top, n + k, n + (k + 1) % sides))
        face_mats.append(mats[-1])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    names = sorted(set(mats), key=mats.index)
    for m in names:
        obj.data.materials.append(material(m))
    for poly, m in zip(obj.data.polygons, face_mats):
        poly.material_index = names.index(m)
    return obj


def palm_tree(name="PalmTree", height=6.5, seed=1):
    """One continuous bent trunk (flared base, ring scars, a leafy boot under the crown), feathery drooping
    fronds with a center rib and real thickness, a cluster of coconuts."""
    rng = random.Random(seed)
    parts = []
    lean = Vector((0.9 + rng.uniform(-0.2, 0.2), 0.2 + rng.uniform(-0.15, 0.15), 0))
    rings = 22
    path, profile, mats = [], [], []
    for i in range(rings + 1):
        t = i / rings
        path.append(Vector((0, 0, height * t)) + lean * (t ** 2))
        r = 0.27 * (1 - 0.42 * t)
        r *= 1 + 0.45 * max(0.0, 1 - t / 0.07)  # flared base
        r *= 1.07 if i % 2 == 0 else 0.97  # bumpy ring scars
        profile.append(r)
        mats.append("TrunkDark" if i % 2 == 0 else "Trunk")
    parts.append(lathe("Trunk", path, profile, mats, sides=10))
    top = path[-1]
    # The "boot": old frond bases bunched under the crown.
    parts.append(cylinder("Boot", 0.3, 0.55, top + Vector((0, 0, -0.1)), "TrunkDark", vertices=8, radius_top=0.2))

    fronds = 9
    for i in range(fronds):
        angle = (i / fronds) * math.tau + rng.uniform(-0.2, 0.2)
        length = rng.uniform(2.5, 3.2)
        droop = rng.uniform(1.0, 1.6)
        mat = "Leaf" if i % 2 == 0 else "LeafDark"
        verts, faces = [], []
        steps = 10
        for s in range(steps + 1):
            t = s / steps
            out = length * t
            down = droop * t * t - 0.4 * t
            # Leaflets: the edge zig-zags in and out, so a frond reads as feathery, not a paddle.
            width = (0.62 * math.sin(math.pi * min(1.0, t * 1.12)) + 0.05) * (1.0 if s % 2 == 0 else 0.72)
            center = Vector((math.cos(angle) * out, math.sin(angle) * out, -down))
            side = Vector((-math.sin(angle), math.cos(angle), 0)) * width
            sag = Vector((0, 0, -0.18 * width))  # leaflets hang down off the rib
            verts += [tuple(top + center - side + sag), tuple(top + center + Vector((0, 0, 0.1 * (1 - t)))),
                      tuple(top + center + side + sag)]
        for s in range(steps):
            a = s * 3
            faces += [(a, a + 3, a + 4, a + 1), (a + 1, a + 4, a + 5, a + 2)]
        parts.append(mesh_object(f"Frond{i}", verts, faces, mat, double_sided=True))

    for i in range(4):
        angle = i * math.tau / 4 + 0.4
        parts.append(ico(f"Coconut{i}", 0.16, top + Vector((math.cos(angle) * 0.24, math.sin(angle) * 0.24, -0.3 - 0.08 * (i % 2))), "Coconut"))
    return join(name, parts)


def kiosk(name, roof_mat):
    """Open-front beach kiosk frame: 4 chunky posts, a deep striped roof with a big overhang and a trim board, a
    scalloped valance, and a framed sign. Footprint 3.6 x 2.6 m, the open customer side faces -Y. Sits around a
    counter placed in Unity."""
    parts = []
    w, d, h = 3.6, 2.6, 2.5
    for x in (-w / 2 + 0.12, w / 2 - 0.12):
        for y in (-d / 2 + 0.12, d / 2 - 0.12):
            parts.append(box("Post", (0.2, 0.2, h), (x, y, h / 2), "Wood"))
            parts.append(box("Post Foot", (0.28, 0.28, 0.12), (x, y, 0.06), "DarkWood"))
    parts.append(box("Beam Front", (w + 0.1, 0.2, 0.22), (0, -d / 2 + 0.12, h), "DarkWood"))
    parts.append(box("Beam Back", (w + 0.1, 0.2, 0.22), (0, d / 2 - 0.12, h), "DarkWood"))
    # Striped roof: alternating colored slats, sloping down toward the customer side, with a white trim board.
    stripes = 9
    roof_w, roof_d = w + 1.2, d + 1.5
    for i in range(stripes):
        x = -roof_w / 2 + roof_w * (i + 0.5) / stripes
        mat = roof_mat if i % 2 == 0 else "White"
        parts.append(box("Roof", (roof_w / stripes, roof_d, 0.12), (x, -0.2, h + 0.38), mat, rotation=(math.radians(-12), 0, 0)))
    parts.append(box("Roof Trim", (roof_w + 0.08, 0.1, 0.2), (0, -roof_d / 2 - 0.12, h + 0.1), "White"))
    # Scalloped valance hanging off the front edge: every other flap a little longer.
    for i in range(stripes):
        x = -roof_w / 2 + roof_w * (i + 0.5) / stripes
        mat = roof_mat if i % 2 == 0 else "White"
        drop = 0.34 if i % 2 == 0 else 0.26
        parts.append(box("Valance", (roof_w / stripes, 0.05, drop), (x, -roof_d / 2 - 0.2, h - drop / 2), mat))
    parts.append(box("Sign Frame", (2.5, 0.1, 0.72), (0, -d / 2 - 0.06, h + 0.95), "White"))
    parts.append(box("Sign", (2.3, 0.12, 0.56), (0, -d / 2 - 0.07, h + 0.95), "Navy"))
    return join(name, parts)


def wave_machine_house(name="WaveMachineHouse"):
    """Fits the 10 x 22 x 5 m footprint behind the deep end. Long side along Y, pool side faces +X."""
    parts = []
    L, W, H = 10.0, 22.0, 5.0
    parts.append(box("Base", (L, W, H * 0.85), (0, 0, H * 0.85 / 2), "White"))
    parts.append(box("Band", (L + 0.05, W + 0.05, 0.6), (0, 0, H * 0.62), "Teal"))
    parts.append(box("Roof", (L + 0.6, W + 0.6, 0.35), (0, 0, H * 0.85 + 0.17), "Navy"))
    for i in range(5):
        y = -W / 2 + W * (i + 0.5) / 5
        parts.append(cylinder("Vent", 0.7, 1.2, (-1.5, y, H * 0.85 + 0.9), "Steel", vertices=10))
        parts.append(cylinder("VentCap", 0.85, 0.15, (-1.5, y, H * 0.85 + 1.55), "Navy", vertices=10))
    # Window strip on the pool side and a big intake grille just above the waterline.
    for i in range(8):
        y = -W / 2 + 1.5 + i * (W - 3) / 7
        parts.append(box("Window", (0.08, 1.6, 0.9), (L / 2 + 0.02, y, H * 0.62 + 0.9), "Glass"))
    parts.append(box("Grille", (0.1, W * 0.86, 0.9), (L / 2 + 0.03, 0, 0.75), "Navy"))
    for i in range(16):
        y = -W * 0.43 + i * (W * 0.86) / 15
        parts.append(box("GrilleBar", (0.14, 0.12, 0.9), (L / 2 + 0.06, y, 0.75), "Steel"))
    parts.append(box("Stripe", (0.06, W * 0.9, 0.18), (L / 2 + 0.04, 0, H * 0.45), "Coral"))
    return join(name, parts)


def surfboard(name="Surfboard"):
    """2 m board along Y, 0.5 m wide, pointed nose toward -Y (which becomes +Z, forward, in Unity),
    rounded tail, slight rocker. One material so it can be tinted per surfer."""
    outline = []
    steps = 12
    for i in range(steps + 1):
        t = i / steps  # 0 = tail, 1 = nose
        y = 1.0 - 2.0 * t
        width = 0.25 * math.sin(math.pi * (0.12 + 0.82 * t)) ** 0.7
        if t > 0.92:
            width *= (1 - t) / 0.08 * 0.5 + 0.0
        outline.append((y, max(width, 0.01)))
    verts, faces = [], []
    for y, w in outline:
        rocker = 0.06 * ((y + 0.1) ** 2)
        for side, x in ((0, -w), (1, w)):
            verts.append((x, y, rocker + 0.035))
        for side, x in ((0, -w), (1, w)):
            verts.append((x * 0.92, y, rocker - 0.035))
    n = len(outline)
    for i in range(n - 1):
        a, b = i * 4, (i + 1) * 4
        faces += [(a, a + 1, b + 1, b),                 # deck
                  (a + 2, b + 2, b + 3, a + 3),         # bottom
                  (a, b, b + 2, a + 2),                 # left rail
                  (a + 1, a + 3, b + 3, b + 1)]         # right rail
    faces += [(0, 2, 3, 1), ((n - 1) * 4, (n - 1) * 4 + 1, (n - 1) * 4 + 3, (n - 1) * 4 + 2)]
    obj = mesh_object(name, verts, faces, "White")
    recalc_normals(obj)
    return obj


# ---------------------------------------------------------------- surfboards (beginner foamie, funboard, shortboard)

def _board_width(t, nose_round, tail_width):
    """Half-width at t (0 = tail, 1 = nose) as a fraction of the max."""
    base = math.sin(math.pi * (0.10 + 0.80 * t)) ** 0.6
    if t < 0.15:  # squarer tail
        base = max(base, tail_width * (0.4 + t / 0.15 * 0.6))
    if t > 0.85:  # nose: round boards stay wide, pointy ones taper to a point
        k = min((t - 0.85) / 0.15, 1.0)
        base *= (1 - 0.7 * k ** 1.8) if nose_round else max(1 - k, 0.0) ** 1.2
    return base


def board(name, length, half_width, thickness, nose_round, tail_width, swallow, stripe_mat, fin_mat, stringer=True, bottom_mat="White"):
    """A surfboard along Y (nose toward -Y = Unity forward), deck material BoardDeck (tinted in Unity),
    with a wooden stringer, a colored stripe near the nose and three fins under the tail."""
    steps = 18
    stations = []
    for i in range(steps + 1):
        t = i / steps
        y = length / 2 - length * t
        w = max(half_width * _board_width(t, nose_round, tail_width), 0.012)
        rocker = 0.07 * ((t - 0.45) / 0.55) ** 2 + (0.05 * ((t - 0.85) / 0.15) ** 2 if t > 0.85 else 0)
        stations.append((t, y, w, rocker))
    verts, faces, face_mats = [], [], []
    for t, y, w, r in stations:
        h = thickness / 2 * (0.55 + 0.45 * math.sin(math.pi * min(max(t, 0.05), 0.95)))
        verts += [(-w, y, r + h), (w, y, r + h), (-w * 0.9, y, r - h), (w * 0.9, y, r - h)]
    n = len(stations)
    for i in range(n - 1):
        a, b = i * 4, (i + 1) * 4
        faces += [(a, a + 1, b + 1, b), (a + 2, b + 2, b + 3, a + 3), (a, b, b + 2, a + 2), (a + 1, a + 3, b + 3, b + 1)]
        face_mats += [0, 1, 0, 0]
    if swallow:  # notch the tail: replace the flat tail cap with two prongs (a V cut)
        faces += [(0, 2, 3, 1)]
    else:
        faces += [(0, 2, 3, 1)]
    faces += [((n - 1) * 4, (n - 1) * 4 + 1, (n - 1) * 4 + 3, (n - 1) * 4 + 2)]
    face_mats += [0, 0]
    obj = mesh_object(name, verts, faces, "BoardDeck")
    obj.data.materials.append(material(bottom_mat))
    for poly, m in zip(obj.data.polygons, face_mats):
        poly.material_index = m
    recalc_normals(obj)
    parts = [obj]

    def deck_z(t):
        for (t0, y0, w0, r0), (t1, y1, w1, r1) in zip(stations, stations[1:]):
            if t0 <= t <= t1:
                k = (t - t0) / max(t1 - t0, 1e-6)
                r = r0 + (r1 - r0) * k
                h = thickness / 2 * (0.55 + 0.45 * math.sin(math.pi * min(max(t, 0.05), 0.95)))
                return r + h, (w0 + (w1 - w0) * k)
        return stations[-1][3], stations[-1][2]

    if stringer:  # thin wooden strip down the middle of the deck
        sv, sf = [], []
        for i, (t, y, w, r) in enumerate(stations[1:-1]):
            z, _ = deck_z(t)
            sv += [(-0.006, y, z + 0.002), (0.006, y, z + 0.002)]
            if i:
                a = (i - 1) * 2
                sf.append((a, a + 1, a + 3, a + 2))
        parts.append(mesh_object(name + "Stringer", sv, sf, "Stringer"))
    # colored stripe band across the deck near the nose
    tv, tf = [], []
    for i, t in enumerate((0.70, 0.73, 0.76, 0.79)):
        y = length / 2 - length * t
        z, w = deck_z(t)
        tv += [(-w * 0.97, y, z + 0.003), (w * 0.97, y, z + 0.003)]
        if i:
            a = (i - 1) * 2
            tf.append((a, a + 1, a + 3, a + 2))
    parts.append(mesh_object(name + "Stripe", tv, tf, stripe_mat))
    # three fins under the tail (a thruster): center fin at the very back, side fins a bit forward
    tail_y = length / 2
    for fx, fy, size in ((0.0, tail_y - 0.07 * length, 1.0), (-half_width * 0.55, tail_y - 0.14 * length, 0.8), (half_width * 0.55, tail_y - 0.14 * length, 0.8)):
        _, _, _, r = stations[1]
        base = r - thickness / 2 * 0.6
        fin_len, fin_h = 0.11 * size, 0.11 * size
        fv = [(fx, fy - fin_len / 2, base), (fx, fy + fin_len / 2, base), (fx, fy + fin_len * 0.55, base - fin_h)]
        fv += [(fx + 0.008, x2, z2) for _, x2, z2 in fv]
        ff = [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]
        fin = mesh_object(name + "Fin", fv, ff, fin_mat)
        recalc_normals(fin)
        parts.append(fin)
    return join(name, parts)


def board_foamie():
    return board("BoardFoamie", 2.4, 0.31, 0.10, True, 0.85, False, "Teal", "Navy", stringer=False, bottom_mat="White")


def board_fun():
    return board("BoardFun", 2.1, 0.27, 0.075, True, 0.6, False, "Coral", "Navy")


def board_short():
    return board("BoardShort", 1.8, 0.235, 0.06, False, 0.45, True, "Teal", "Charcoal")


# ---------------------------------------------------------------- desk machines
# Operator side is Blender -Y. WorldArt.Model adds a 180 degree yaw, so place with yaw 180 when the
# operator stands at Unity +Z (the ticket desk) and yaw 0 when they stand at Unity -Z (wave console).

def cash_register(name="CashRegister"):
    parts = [
        box("Body", (0.46, 0.38, 0.12), (0, 0, 0.06), "Navy"),
        box("Drawer", (0.42, 0.03, 0.08), (0, -0.2, 0.055), "Steel"),
        box("Handle", (0.12, 0.02, 0.015), (0, -0.218, 0.07), "Charcoal"),
        box("Keypad", (0.30, 0.20, 0.035), (-0.05, -0.06, 0.15), "Charcoal", rotation=(math.radians(-18), 0, 0)),
        box("Printer", (0.11, 0.14, 0.08), (0.17, -0.04, 0.16), "White"),
        box("Receipt", (0.07, 0.005, 0.07), (0.17, -0.02, 0.23), "White", rotation=(math.radians(10), 0, 0)),
        box("Screen Back", (0.30, 0.04, 0.17), (0, 0.13, 0.27), "Navy", rotation=(math.radians(15), 0, 0)),
        box("Screen", (0.26, 0.01, 0.13), (0, 0.105, 0.273), "Screen", rotation=(math.radians(15), 0, 0)),
        box("Screen Post", (0.05, 0.05, 0.1), (0, 0.15, 0.15), "Navy"),
        cylinder("Pole", 0.015, 0.22, (0.15, 0.16, 0.23), "Steel"),
        box("Customer Display", (0.16, 0.03, 0.07), (0.15, 0.16, 0.36), "Charcoal"),
        box("Customer Digits", (0.13, 0.005, 0.045), (0.15, 0.178, 0.36), "Green"),
    ]
    for row in range(3):
        for col in range(4):
            mat = "Green" if (row, col) == (2, 3) else "Red" if (row, col) == (0, 3) else "KeyGrey"
            y = -0.12 + row * 0.055
            z = 0.145 + row * 0.018
            parts.append(box("Key", (0.045, 0.04, 0.02), (-0.155 + col * 0.068, y, z + 0.02), mat, rotation=(math.radians(-18), 0, 0)))
    return join(name, parts)


def computer(name="Computer"):
    parts = [
        box("Foot", (0.26, 0.2, 0.02), (0, 0.06, 0.01), "Steel"),
        box("Neck", (0.06, 0.04, 0.24), (0, 0.08, 0.13), "Steel"),
        box("Bezel", (0.66, 0.05, 0.44), (0, 0.04, 0.45), "Navy"),
        box("Back Hump", (0.36, 0.08, 0.24), (0, 0.09, 0.45), "Navy"),
        box("Keyboard", (0.44, 0.15, 0.025), (0, -0.2, 0.0125), "Charcoal"),
        box("Mouse Pad", (0.2, 0.17, 0.006), (0.36, -0.2, 0.003), "Teal"),
        box("Mouse", (0.06, 0.09, 0.03), (0.36, -0.2, 0.02), "White"),
        box("Sticker", (0.07, 0.006, 0.05), (0.24, 0.015, 0.25), "Coral"),
    ]
    for row in range(4):
        parts.append(box("Key Row", (0.40, 0.022, 0.012), (0, -0.25 + row * 0.032, 0.03), "KeyGrey"))
    return join(name, parts)


def wave_console(name="WaveConsole"):
    """1.2 x 0.8 m pedestal, top at 1.0 m. The big button sits at Unity local (0, 1.04, -0.15) = Blender (0, -0.15, 1.04)."""
    parts = [
        box("Plinth", (1.3, 0.9, 0.1), (0, 0, 0.05), "Charcoal"),
        box("Body", (1.2, 0.8, 0.86), (0, 0, 0.53), "White"),
        box("Band", (1.21, 0.81, 0.12), (0, 0, 0.75), "Teal"),
        box("Top", (1.24, 0.84, 0.05), (0, 0, 0.985), "Navy"),
        cylinder("Button Ring", 0.21, 0.03, (0, -0.15, 1.02), "Yellow", vertices=16),
        cylinder("Button Collar", 0.18, 0.035, (0, -0.15, 1.03), "Charcoal", vertices=16),
        box("Riser", (1.0, 0.22, 0.3), (0, 0.27, 1.15), "Navy"),
        cylinder("Lever Base", 0.06, 0.06, (0.45, -0.15, 1.04), "Steel", vertices=10),
        cylinder("Lever", 0.018, 0.28, (0.45, -0.12, 1.17), "Steel", rotation=(math.radians(-15), 0, 0)),
        ico("Lever Knob", 0.05, (0.45, -0.085, 1.31), "Red"),
        box("Light 1", (0.06, 0.04, 0.04), (-0.45, -0.3, 1.03), "Green"),
        box("Light 2", (0.06, 0.04, 0.04), (-0.35, -0.3, 1.03), "Yellow"),
        box("Light 3", (0.06, 0.04, 0.04), (-0.25, -0.3, 1.03), "Red"),
    ]
    for i in range(6):  # hazard stripes on the operator side
        parts.append(box("Hazard", (0.08, 0.01, 0.18), (-0.5 + i * 0.2, -0.405, 0.2), "Yellow", rotation=(0, math.radians(30), 0)))
    for i in range(3):  # vents on the sides
        for side in (-1, 1):
            parts.append(box("Vent", (0.01, 0.4, 0.04), (side * 0.605, 0, 0.35 + i * 0.08), "Charcoal"))
    return join(name, parts)


def price_stand(name="PriceStand"):
    """Pedestal (0.7 x 0.5, top at 1.0 m) for the +/- price buttons, with a big $ sign behind them."""
    parts = [
        box("Plinth", (0.8, 0.6, 0.08), (0, 0, 0.04), "Charcoal"),
        box("Body", (0.7, 0.5, 0.9), (0, 0, 0.53), "White"),
        box("Band", (0.71, 0.51, 0.1), (0, 0, 0.8), "Coral"),
        box("Top", (0.74, 0.54, 0.04), (0, 0, 0.98), "Navy"),
        box("Sign Post", (0.05, 0.05, 0.5), (0, 0.2, 1.25), "Steel"),
        box("Sign", (0.5, 0.04, 0.3), (0, 0.2, 1.55), "Yellow"),
        # "$": an S of three bars plus two short verticals, and a line through it
        box("S Top", (0.14, 0.01, 0.03), (0, 0.178, 1.63), "Navy"),
        box("S Mid", (0.14, 0.01, 0.03), (0, 0.178, 1.55), "Navy"),
        box("S Bot", (0.14, 0.01, 0.03), (0, 0.178, 1.47), "Navy"),
        box("S Left", (0.03, 0.01, 0.08), (-0.055, 0.178, 1.59), "Navy"),
        box("S Right", (0.03, 0.01, 0.08), (0.055, 0.178, 1.51), "Navy"),
        box("S Line", (0.02, 0.012, 0.24), (0, 0.176, 1.55), "Navy"),
        # + and - labels on the front, under the buttons (placed with yaw 180, Blender +x = the operator's right,
        # where the + button is)
        box("Plus H", (0.1, 0.02, 0.02), (0.17, -0.26, 0.9), "White"),
        box("Plus V", (0.02, 0.02, 0.1), (0.17, -0.26, 0.9), "White"),
        box("Minus", (0.1, 0.02, 0.02), (-0.17, -0.26, 0.9), "White"),
    ]
    return join(name, parts)


def counter(name="Counter"):
    """3 x 1 m service counter, 1.1 m tall. White front panel with a teal band toward the customer side (-Y)."""
    parts = [
        box("Kick", (2.9, 0.86, 0.12), (0, 0.02, 0.06), "Navy"),
        box("Body", (3.0, 0.9, 0.96), (0, 0, 0.6), "Wood"),
        box("Front Panel", (2.96, 0.04, 0.9), (0, -0.46, 0.6), "White"),
        box("Front Band", (2.98, 0.05, 0.16), (0, -0.475, 0.82), "Teal"),
        box("Front Stripe", (2.98, 0.05, 0.05), (0, -0.475, 0.68), "Coral"),
        box("Top", (3.12, 1.04, 0.07), (0, 0, 1.115), "DarkWood"),
        box("Shelf", (2.8, 0.3, 0.04), (0, 0.32, 0.55), "DarkWood"),
    ]
    for i in range(7):
        x = -1.35 + i * 0.45
        parts.append(box("Slat", (0.04, 0.06, 0.62), (x, -0.48, 0.38), "White"))
    return join(name, parts)


def board_rack(name="BoardRack"):
    """Wooden A-frame rack for rental boards: 2.4 m long along Y, 0.5 m deep, 1.2 m tall."""
    parts = [box("Base", (0.5, 2.4, 0.08), (0, 0, 0.04), "DarkWood")]
    lean = math.degrees(math.atan(0.2 / 1.2))
    for y in (-1.15, 1.15):  # inverted-V end frames, wide at the bottom
        parts.append(box("Leg", (0.08, 0.1, 1.24), (-0.15, y, 0.62), "Wood", rotation=(0, math.radians(lean), 0)))
        parts.append(box("Leg", (0.08, 0.1, 1.24), (0.15, y, 0.62), "Wood", rotation=(0, math.radians(-lean), 0)))
    for z in (0.35, 0.85):
        half = 0.25 - z * 0.2 / 1.2
        for side in (-1, 1):
            parts.append(box("Bar", (0.05, 2.4, 0.05), (side * half, 0, z), "Wood"))
    parts.append(box("Top Rail", (0.12, 2.4, 0.08), (0, 0, 1.22), "DarkWood"))
    parts.append(box("Sign Plate", (0.06, 0.9, 0.22), (0, 0, 1.38), "Teal"))
    return join(name, parts)


def return_crate(name="ReturnCrate"):
    """Slatted wooden crate (2 x 1 m, 0.6 m tall) where renters drop their boards."""
    parts = [box("Floor", (2.0, 1.0, 0.06), (0, 0, 0.05), "DarkWood")]
    for x in (-0.97, 0.97):
        for y in (-0.47, 0.47):
            parts.append(box("Corner", (0.08, 0.08, 0.62), (x, y, 0.31), "DarkWood"))
    for z in (0.15, 0.32, 0.49):
        parts.append(box("Slat", (2.0, 0.04, 0.12), (0, -0.49, z), "Wood"))
        parts.append(box("Slat", (2.0, 0.04, 0.12), (0, 0.49, z), "Wood"))
        parts.append(box("Slat", (0.04, 1.0, 0.12), (-0.99, 0, z), "Wood"))
        parts.append(box("Slat", (0.04, 1.0, 0.12), (0.99, 0, z), "Wood"))
    for y in (-0.5, 0.5):
        parts.append(box("Band", (2.02, 0.05, 0.07), (0, y, 0.6), "Teal"))
    for x in (-1.0, 1.0):
        parts.append(box("Band", (0.05, 1.02, 0.07), (x, 0, 0.6), "Teal"))
    return join(name, parts)


def bench(name="Bench"):
    """3 m wooden slat bench, 0.5 m seat height, 0.8 m deep (along Y), chunky legs with armrests."""
    parts = []
    for i in range(4):
        parts.append(box("Seat Slat", (3.0, 0.16, 0.06), (0, -0.3 + i * 0.2, 0.47), "Wood"))
    for x in (-1.3, 0, 1.3):
        parts.append(box("Leg", (0.1, 0.72, 0.45), (x, 0, 0.22), "DarkWood"))
    for x in (-1.42, 1.42):
        parts.append(box("Armrest", (0.1, 0.72, 0.07), (x, -0.02, 0.7), "DarkWood"))
        parts.append(box("Arm Post", (0.08, 0.08, 0.24), (x, -0.32, 0.58), "DarkWood"))
    parts.append(box("Back Slat", (3.0, 0.06, 0.15), (0, 0.38, 0.72), "Wood"))
    parts.append(box("Back Slat", (3.0, 0.06, 0.15), (0, 0.38, 0.9), "Teal"))
    for x in (-1.3, 1.3):
        parts.append(box("Back Post", (0.1, 0.07, 0.52), (x, 0.38, 0.72), "DarkWood"))
    return join(name, parts)


def car(name="Car"):
    """A small beach car with a surfboard on the roof. 4 m long along Y, nose toward -Y (Unity forward)."""
    parts = [
        box("Body", (1.8, 4.0, 0.62), (0, 0, 0.62), "CarPaint"),
        box("Hood Slope", (1.76, 0.9, 0.2), (0, -1.5, 0.98), "CarPaint", rotation=(math.radians(-8), 0, 0)),
        box("Cabin", (1.62, 2.0, 0.62), (0, 0.35, 1.22), "Glass"),
        box("Roof", (1.66, 1.9, 0.08), (0, 0.35, 1.56), "CarPaint"),
        box("Bumper Front", (1.84, 0.15, 0.22), (0, -2.05, 0.42), "Charcoal"),
        box("Bumper Back", (1.84, 0.15, 0.22), (0, 2.05, 0.42), "Charcoal"),
        box("Headlight L", (0.36, 0.05, 0.14), (-0.6, -2.02, 0.72), "Headlight"),
        box("Headlight R", (0.36, 0.05, 0.14), (0.6, -2.02, 0.72), "Headlight"),
        box("Taillight L", (0.32, 0.05, 0.14), (-0.65, 2.02, 0.75), "Taillight"),
        box("Taillight R", (0.32, 0.05, 0.14), (0.65, 2.02, 0.75), "Taillight"),
        box("Rack L", (0.06, 1.7, 0.06), (-0.55, 0.35, 1.64), "Steel"),
        box("Rack R", (0.06, 1.7, 0.06), (0.55, 0.35, 1.64), "Steel"),
    ]
    for x in (-0.86, 0.86):
        for y in (-1.3, 1.3):
            parts.append(cylinder("Wheel", 0.36, 0.26, (x, y, 0.36), "Tire", vertices=10, rotation=(0, math.radians(90), 0)))
            parts.append(cylinder("Hub", 0.16, 0.28, (x, y, 0.36), "Steel", vertices=8, rotation=(0, math.radians(90), 0)))
    roof_board = board("CarBoard", 2.3, 0.27, 0.07, True, 0.6, False, "Coral", "Navy")
    roof_board.location = (0, 0.25, 1.68)
    bpy.context.view_layer.objects.active = roof_board
    bpy.ops.object.select_all(action="DESELECT")
    roof_board.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    parts.append(roof_board)
    return join(name, parts)


def lifeguard_tower(name="LifeguardTower"):
    """Chunky stilts with X-braces, platform with railing and a rescue ring hanging on it, a ladder with round rungs,
    and a red-and-white roof with a wide overhang. Faces -Y (toward the pool when rotated)."""
    parts = []
    for x in (-0.6, 0.6):
        for y in (-0.6, 0.6):
            parts.append(box("Leg", (0.17, 0.17, 1.95), (x, y, 0.97), "White"))
    # X-braces on the two sides and the back (the front stays open to the ladder).
    brace = math.degrees(math.atan2(1.5, 1.2))
    for x in (-0.6, 0.6):
        for sign in (-1, 1):
            parts.append(box("Brace", (0.06, 1.9, 0.08), (x, 0, 0.95), "White", rotation=(math.radians(sign * brace), 0, 0)))
    for sign in (-1, 1):
        parts.append(box("Brace", (1.9, 0.06, 0.08), (0, 0.6, 0.95), "White", rotation=(0, math.radians(sign * brace), 0)))
    parts.append(box("Platform", (1.7, 1.7, 0.14), (0, 0, 1.95), "Wood"))
    parts.append(box("Platform Trim", (1.76, 1.76, 0.06), (0, 0, 1.86), "Red"))
    for x, y, sx, sy in ((0, -0.8, 1.7, 0.08), (0, 0.8, 1.7, 0.08), (-0.8, 0, 0.08, 1.7), (0.8, 0, 0.08, 1.7)):
        parts.append(box("Rail", (sx, sy, 0.1), (x, y, 2.5), "Red"))
    for x in (-0.8, 0.8):
        for y in (-0.8, 0.8):
            parts.append(box("RoofPost", (0.11, 0.11, 1.45), (x, y, 2.68), "White"))
    # Rescue ring on the front rail: a chunky red/white torus.
    bpy.ops.mesh.primitive_torus_add(major_radius=0.27, minor_radius=0.075, major_segments=16, minor_segments=6,
                                     location=(0.35, -0.86, 2.3), rotation=(math.radians(90), 0, 0))
    ring = bpy.context.active_object
    ring.name = "Ring"
    finish(ring, "Red")
    ring.data.materials.append(material("White"))
    for i, poly in enumerate(ring.data.polygons):
        angle = math.atan2(poly.center.z, poly.center.x)
        poly.material_index = 1 if int((angle + math.pi) / (math.pi / 2)) % 2 else 0
    parts.append(ring)
    roof_w, roof_d = 2.3, 2.4
    for i in range(6):
        x = -roof_w / 2 + roof_w * (i + 0.5) / 6
        parts.append(box("Roof", (roof_w / 6, roof_d, 0.09), (x, 0, 3.42), "Red" if i % 2 == 0 else "White",
                         rotation=(math.radians(-8), 0, 0)))
    parts.append(box("Roof Edge", (roof_w + 0.06, 0.08, 0.16), (0, -roof_d / 2 - 0.02, 3.5), "White"))
    for i in range(6):
        parts.append(cylinder("LadderStep", 0.035, 0.62, (0, 1.05, 0.3 * (i + 1)), "Wood", vertices=6, rotation=(0, math.radians(90), 0)))
    for x in (-0.31, 0.31):
        parts.append(box("LadderRail", (0.08, 0.08, 2.05), (x, 1.05, 1.02), "Wood"))
    parts.append(box("Seat", (0.6, 0.5, 0.1), (0, 0.25, 2.38), "Yellow"))
    return join(name, parts)


def beach_umbrella(name="BeachUmbrella", color="Teal"):
    """Striped canopy with a curved (two-step) slope and a scalloped rim, a fat pole with a little top knob."""
    parts = [cylinder("Pole", 0.05, 2.45, (0, 0, 1.22), "White", vertices=8)]
    segments = 8
    for i in range(segments):
        a0 = i / segments * math.tau
        a1 = (i + 1) / segments * math.tau
        am = (a0 + a1) / 2
        verts = [(0, 0, 2.5),
                 (math.cos(a0) * 0.75, math.sin(a0) * 0.75, 2.24), (math.cos(a1) * 0.75, math.sin(a1) * 0.75, 2.24),
                 (math.cos(a0) * 1.32, math.sin(a0) * 1.32, 1.93), (math.cos(a1) * 1.32, math.sin(a1) * 1.32, 1.93),
                 (math.cos(am) * 1.24, math.sin(am) * 1.24, 1.86)]
        faces = [(0, 2, 1), (1, 2, 4, 3), (3, 4, 5)]
        parts.append(mesh_object("Canopy", verts, faces, color if i % 2 == 0 else "White", double_sided=True))
    parts.append(ico("Knob", 0.08, (0, 0, 2.56), "White"))
    return join(name, parts)


def lounge_chair(name="LoungeChair"):
    """Chunky white frame, armrests, a thick coral cushion that follows the raised back."""
    parts = [box("Frame", (0.66, 1.75, 0.08), (0, 0, 0.32), "White")]
    for x in (-0.29, 0.29):
        for y in (-0.72, 0.72):
            parts.append(box("Leg", (0.07, 0.07, 0.3), (x, y, 0.15), "White"))
        parts.append(box("Armrest", (0.06, 0.6, 0.06), (x, 0.35, 0.52), "White"))
    parts.append(box("Back", (0.66, 0.72, 0.08), (0, 0.97, 0.62), "White", rotation=(math.radians(50), 0, 0)))
    parts.append(box("Cushion", (0.58, 1.6, 0.1), (0, -0.05, 0.41), "Coral"))
    parts.append(box("Back Cushion", (0.58, 0.66, 0.1), (0, 0.93, 0.7), "Coral", rotation=(math.radians(50), 0, 0)))
    parts.append(box("Pillow", (0.4, 0.2, 0.12), (0, 1.12, 0.92), "White", rotation=(math.radians(50), 0, 0)))
    return join(name, parts)


def rock(name="Rock", seed=3):
    """A chunky boulder that sits flat on the ground, with two smaller stones tucked against it."""
    rng = random.Random(seed)
    parts = []
    for i, (radius, offset) in enumerate(((0.75, (0, 0)), (0.32, (0.75, -0.25)), (0.24, (-0.6, 0.35)))):
        stone = ico(f"Stone{i}", radius, (offset[0], offset[1], radius * 0.45), "Rock", subdivisions=2)
        for v in stone.data.vertices:
            v.co *= rng.uniform(0.86, 1.12)
            v.co.z = max(v.co.z * 0.62, -radius * 0.42)  # squat, with a flat bottom
        parts.append(stone)
    return join(name, parts)


def taper(name, radius_top, radius_bottom, length, mat, vertices=7, scale_x=1.0, scale_y=1.0):
    """Tapered limb hanging down from the pivot (z = 0 to z = -length)."""
    obj = cylinder(name, radius_bottom, length, (0, 0, -length / 2), mat, vertices=vertices, radius_top=radius_top)
    obj.scale = (scale_x, scale_y, 1)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def char_torso():
    obj = cylinder("CharTorso", 0.2, 0.55, (0, 0, 0.275), "Shirt", vertices=8, radius_top=0.23)
    obj.scale = (1.0, 0.6, 1)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def char_shorts():
    obj = cylinder("CharShorts", 0.22, 0.24, (0, 0, -0.08), "Shorts", vertices=8, radius_top=0.21)
    obj.scale = (1.0, 0.62, 1)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def char_head():
    parts = [box("Skull", (0.26, 0.25, 0.28), (0, 0, 0.17), "Skin"),
             box("Neck", (0.1, 0.1, 0.06), (0, 0, 0.02), "Skin"),
             box("EyeL", (0.04, 0.02, 0.05), (-0.06, -0.126, 0.19), "Eyes"),
             box("EyeR", (0.04, 0.02, 0.05), (0.06, -0.126, 0.19), "Eyes"),
             box("Nose", (0.04, 0.04, 0.05), (0, -0.135, 0.15), "Skin")]
    return join("CharHead", parts)


def char_hair_short():
    parts = [box("Top", (0.29, 0.28, 0.08), (0, 0.01, 0.33), "Hair"),
             box("Back", (0.29, 0.06, 0.16), (0, 0.13, 0.24), "Hair"),
             box("Fringe", (0.27, 0.05, 0.05), (0, -0.12, 0.29), "Hair")]
    return join("CharHairShort", parts)


def char_hair_long():
    parts = [box("Top", (0.3, 0.29, 0.09), (0, 0.01, 0.33), "Hair"),
             box("Back", (0.3, 0.08, 0.38), (0, 0.14, 0.15), "Hair"),
             box("SideL", (0.05, 0.22, 0.24), (-0.15, 0.02, 0.2), "Hair"),
             box("SideR", (0.05, 0.22, 0.24), (0.15, 0.02, 0.2), "Hair")]
    return join("CharHairLong", parts)


def char_arm():
    arm = taper("Arm", 0.06, 0.045, 0.58, "Skin")
    hand = ico("Hand", 0.06, (0, 0, -0.6), "Skin")
    return join("CharArm", [arm, hand])


def char_leg():
    leg = taper("Leg", 0.085, 0.06, 0.8, "Skin")
    foot = box("Foot", (0.1, 0.2, 0.06), (0, -0.05, -0.82), "Skin")
    return join("CharLeg", [leg, foot])



# ---------------------------------------------------------------- park glow-up and life (ambience step)

def catenary(t, sag):
    """0 at both ends, -sag in the middle."""
    return -sag * (1 - (2 * t - 1) ** 2)


def seagull(name="Seagull"):
    """A gull in flight, about 1.3 m wingspan, flying toward -Y. Wings form a shallow M."""
    parts = [ico("Body", 0.14, (0, 0, 0), "White", subdivisions=1)]
    parts[0].scale = (0.75, 2.1, 0.8)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(scale=True)
    parts.append(ico("Head", 0.09, (0, -0.3, 0.05), "White", subdivisions=1))
    parts.append(cylinder("Beak", 0.025, 0.1, (0, -0.42, 0.04), "Yellow", vertices=5, rotation=(math.radians(90), 0, 0), radius_top=0.005))
    for side in (-1, 1):
        inner = [(side * 0.05, -0.08, 0.02), (side * 0.36, -0.06, 0.14), (side * 0.36, 0.08, 0.12), (side * 0.05, 0.1, 0.0)]
        parts.append(mesh_object("Wing Inner", inner, [(0, 1, 2, 3) if side > 0 else (3, 2, 1, 0)], "White", double_sided=True))
        outer = [(side * 0.36, -0.06, 0.14), (side * 0.68, 0.04, 0.04), (side * 0.36, 0.08, 0.12)]
        parts.append(mesh_object("Wing Tip", outer, [(0, 1, 2) if side > 0 else (2, 1, 0)], "Charcoal", double_sided=True))
    parts.append(mesh_object("Tail", [(-0.07, 0.22, 0.0), (0.07, 0.22, 0.0), (0, 0.38, 0.02)], [(0, 1, 2)], "White", double_sided=True))
    obj = join(name, parts)
    return obj


def hull(name, length, width, height, mat):
    """Boat hull: flat-ish bottom, pointy bow toward -Y."""
    hl, hw = length / 2, width / 2
    verts = [
        (0, -hl, height), (hw, -hl * 0.45, height), (hw, hl, height), (-hw, hl, height), (-hw, -hl * 0.45, height),
        (0, -hl * 0.92, 0), (hw * 0.6, -hl * 0.4, 0), (hw * 0.6, hl * 0.95, 0), (-hw * 0.6, hl * 0.95, 0), (-hw * 0.6, -hl * 0.4, 0),
    ]
    faces = [(0, 1, 2, 3, 4), (9, 8, 7, 6, 5), (0, 5, 6, 1), (1, 6, 7, 2), (2, 7, 8, 3), (3, 8, 9, 4), (4, 9, 5, 0)]
    obj = mesh_object(name, verts, faces, mat)
    recalc_normals(obj)
    return obj


def sailboat(name="Sailboat"):
    parts = [hull("Hull", 6.0, 2.0, 0.9, "Navy")]
    parts.append(box("Deck", (1.7, 4.2, 0.08), (0, 0.6, 0.92), "Wood"))
    parts.append(box("Cabin", (1.1, 1.6, 0.45), (0, 0.4, 1.15), "White"))
    parts.append(cylinder("Mast", 0.07, 8.0, (0, -0.4, 4.9), "White", vertices=6))
    parts.append(box("Boom", (0.08, 2.6, 0.08), (0, 0.9, 1.75), "White"))
    parts.append(mesh_object("Mainsail", [(0, -0.35, 8.6), (0, -0.35, 1.85), (0, 2.15, 1.85)], [(0, 1, 2)], "White", double_sided=True))
    parts.append(mesh_object("Jib", [(0, -0.45, 7.6), (0, -2.8, 1.1), (0, -0.5, 1.4)], [(0, 1, 2)], "Coral", double_sided=True))
    return join(name, parts)


def fishing_boat(name="FishingBoat"):
    parts = [hull("Hull", 7.0, 2.6, 1.1, "Red")]
    parts.append(box("Stripe", (2.62, 4.6, 0.12), (0, 1.0, 1.0), "White"))
    parts.append(box("Deck", (2.3, 5.2, 0.08), (0, 0.8, 1.12), "Wood"))
    parts.append(box("Wheelhouse", (1.8, 1.8, 1.5), (0, 0.2, 1.9), "White"))
    parts.append(box("Windows", (1.82, 0.4, 0.5), (0, -0.52, 2.2), "Glass"))
    parts.append(box("Roof", (2.0, 2.0, 0.1), (0, 0.2, 2.7), "Teal"))
    parts.append(cylinder("Mast", 0.05, 2.2, (0, 0.6, 3.8), "Steel", vertices=6))
    parts.append(box("Crossbar", (1.2, 0.05, 0.05), (0, 0.6, 4.4), "Steel"))
    parts.append(box("Net Boom", (0.06, 3.0, 0.06), (0.4, 2.6, 2.3), "Steel", rotation=(math.radians(-25), 0, 0)))
    parts.append(cylinder("Buoy", 0.18, 0.4, (1.2, 2.5, 1.4), "Yellow", vertices=8))
    return join(name, parts)


def jet_ski(name="JetSki"):
    """A jet ski with a rider, nose toward -Y."""
    parts = [hull("Hull", 3.0, 1.1, 0.55, "Yellow")]
    parts.append(box("Top", (0.9, 1.6, 0.18), (0, 0.35, 0.62), "Navy"))
    parts.append(box("Seat", (0.42, 1.0, 0.18), (0, 0.55, 0.78), "Charcoal"))
    parts.append(box("Bars", (0.8, 0.06, 0.06), (0, -0.25, 1.05), "Charcoal"))
    parts.append(box("Steer Post", (0.08, 0.08, 0.45), (0, -0.2, 0.82), "Charcoal"))
    # rider
    parts.append(box("Rider Legs", (0.36, 0.6, 0.25), (0, 0.35, 0.95), "Navy"))
    parts.append(box("Rider Torso", (0.42, 0.26, 0.6), (0, 0.4, 1.35), "Coral", rotation=(math.radians(-20), 0, 0)))
    parts.append(ico("Rider Head", 0.15, (0, 0.28, 1.8), "Tan"))
    parts.append(ico("Rider Hair", 0.155, (0, 0.33, 1.84), "Charcoal"))
    for side in (-1, 1):
        parts.append(box("Rider Arm", (0.1, 0.55, 0.1), (side * 0.25, 0.0, 1.32), "Tan", rotation=(math.radians(25), 0, 0)))
    return join(name, parts)


def blimp(name="Blimp"):
    """30 m blimp, long axis Y, nose toward -Y. The ad text is added in Unity."""
    env = ico("Envelope", 1.0, (0, 0, 0), "White", subdivisions=3)
    env.scale = (4.2, 15.0, 4.2)
    bpy.context.view_layer.objects.active = env
    bpy.ops.object.transform_apply(scale=True)
    parts = [env]
    band = cylinder("Band", 4.25, 1.4, (0, -2.0, 0), "Teal", vertices=24, rotation=(math.radians(90), 0, 0))
    parts.append(band)
    parts.append(cylinder("Nose Cap", 1.0, 0.4, (0, -14.9, 0), "Coral", vertices=12, rotation=(math.radians(90), 0, 0)))
    for angle in (0, 90, 180, 270):
        a = math.radians(angle)
        cx, cz = math.cos(a), math.sin(a)
        verts = [(cx * 1.6, 9.5, cz * 1.6), (cx * 5.2, 13.6, cz * 5.2), (cx * 5.2, 15.2, cz * 5.2), (cx * 1.0, 13.8, cz * 1.0)]
        parts.append(mesh_object("Fin", verts, [(0, 1, 2, 3)], "Coral", double_sided=True))
    parts.append(box("Gondola", (1.6, 4.6, 1.3), (0, -1.5, -4.6), "Navy"))
    parts.append(box("Gondola Windows", (1.64, 3.6, 0.45), (0, -1.6, -4.4), "Glass"))
    for side in (-1, 1):
        parts.append(cylinder("Engine", 0.35, 1.0, (side * 1.4, 0.6, -4.3), "Steel", vertices=8, rotation=(math.radians(90), 0, 0)))
    return join(name, parts)


def helicopter(name="Helicopter"):
    """Small news/tour helicopter, nose toward -Y; the main rotor is the separate HeliRotor model."""
    body = ico("Body", 1.0, (0, -0.4, 1.4), "Coral", subdivisions=2)
    body.scale = (1.0, 1.7, 0.95)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.transform_apply(scale=True)
    parts = [body]
    bubble = ico("Canopy", 0.85, (0, -1.3, 1.6), "Glass", subdivisions=2)
    bubble.scale = (0.95, 0.9, 0.8)
    bpy.context.view_layer.objects.active = bubble
    bpy.ops.object.transform_apply(scale=True)
    parts.append(bubble)
    parts.append(cylinder("Tail Boom", 0.28, 4.6, (0, 2.8, 1.65), "Coral", vertices=8, rotation=(math.radians(90), 0, 0), radius_top=0.14))
    parts.append(box("Tail Fin", (0.08, 0.8, 1.2), (0, 5.0, 2.2), "White"))
    parts.append(box("Tail Wing", (1.4, 0.5, 0.06), (0, 4.7, 1.7), "White"))
    parts.append(cylinder("Tail Rotor", 0.55, 0.05, (0.1, 5.1, 2.4), "Charcoal", vertices=10, rotation=(0, math.radians(90), 0)))
    parts.append(cylinder("Mast", 0.12, 0.5, (0, -0.4, 2.55), "Steel", vertices=8))
    for side in (-1, 1):
        parts.append(box("Skid", (0.1, 3.2, 0.1), (side * 0.85, -0.4, 0.05), "Steel"))
        for y in (-1.2, 0.4):
            parts.append(box("Strut", (0.08, 0.08, 0.6), (side * 0.7, y, 0.35), "Steel", rotation=(0, math.radians(side * -20), 0)))
    parts.append(box("Stripe", (2.02, 1.4, 0.18), (0, 0.2, 1.3), "White"))
    return join(name, parts)


def heli_rotor(name="HeliRotor"):
    """Two long blades and a hub; pivot at the hub so Unity can spin it."""
    parts = [cylinder("Hub", 0.2, 0.2, (0, 0, 0), "Steel", vertices=8)]
    parts.append(box("Blade", (0.3, 8.6, 0.05), (0, 0, 0.08), "Charcoal"))
    parts.append(box("Blade", (8.6, 0.3, 0.05), (0, 0, 0.08), "Charcoal"))
    return join(name, parts)


def park_arch(name="ParkArch"):
    """The big entrance sign: two tall surfboard pillars and a sign board across the top (text added in Unity).
    12 m wide inside the pillars; the board faces -Y (Unity +Z) and +Y."""
    parts = []
    for side, mat, stripe in ((-1, "Teal", "Yellow"), (1, "Coral", "Yellow")):
        x = side * 6.6
        pillar = board("Pillar Board", 7.6, 0.55, 0.22, True, 0.7, False, stripe, "Navy")
        pillar.rotation_euler = (math.radians(90), 0, 0)
        pillar.location = (x, 0, 3.9)
        bpy.context.view_layer.objects.active = pillar
        bpy.ops.object.select_all(action="DESELECT")
        pillar.select_set(True)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=False)
        parts.append(pillar)
        parts.append(box("Pillar Core", (0.5, 0.5, 7.0), (x, 0, 3.5), mat))
        parts.append(box("Footing", (1.4, 1.4, 0.4), (x, 0, 0.2), "Concrete"))
    parts.append(box("Sign Board", (14.4, 0.4, 2.4), (0, 0, 6.4), "Navy"))
    parts.append(box("Trim Top", (14.8, 0.5, 0.22), (0, 0, 7.7), "Yellow"))
    parts.append(box("Trim Bottom", (14.8, 0.5, 0.22), (0, 0, 5.1), "Yellow"))
    for i in range(15):  # marquee bulbs along the trims
        x = -7.0 + i * 1.0
        for z in (7.85, 4.95):
            parts.append(ico("Bulb", 0.11, (x, 0, z), "Bulb"))
    # little wave crest on top
    crest = [(-3.0, 0, 7.8), (-1.4, 0, 9.4), (0.6, 0, 9.9), (2.4, 0, 9.0), (1.1, 0, 8.8), (0.2, 0, 9.2), (-1.0, 0, 8.6), (-1.2, 0, 7.8)]
    parts.append(mesh_object("Crest", [(x, y - 0.15, z) for x, y, z in crest] + [(x, y + 0.15, z) for x, y, z in crest],
                             [tuple(range(8)), tuple(range(15, 7, -1))] + [(i, (i + 1) % 8, (i + 1) % 8 + 8, i + 8) for i in range(8)], "Teal"))
    recalc_normals(parts[-1])
    return join(name, parts)


def sign_old(name="SignOld"):
    """The sign you inherited: crooked posts and weathered plywood, a board missing. ~3.2 m wide."""
    parts = [
        box("Post L", (0.14, 0.14, 2.6), (-1.4, 0, 1.3), "DarkWood", rotation=(0, math.radians(4), 0)),
        box("Post R", (0.14, 0.14, 2.4), (1.4, 0, 1.2), "DarkWood", rotation=(0, math.radians(-3), 0)),
        box("Board Top", (3.3, 0.06, 0.42), (0, -0.1, 2.15), "Plywood", rotation=(0, math.radians(3), 0)),
        box("Board Mid", (3.1, 0.06, 0.42), (0.05, -0.1, 1.68), "Plywood", rotation=(0, math.radians(-2), 0)),
        box("Board Low Broken", (1.6, 0.06, 0.4), (-0.75, -0.1, 1.2), "DarkWood", rotation=(0, math.radians(8), 0)),
        box("Plank", (1.9, 0.05, 0.26), (0.5, -0.16, 0.75), "White", rotation=(0, math.radians(-6), 0)),
    ]
    return join(name, parts)


def sign_painted(name="SignPainted"):
    """The fixed-up sign: straight white posts and a fresh teal board. ~3.6 m wide."""
    parts = [
        box("Post L", (0.16, 0.16, 2.8), (-1.6, 0, 1.4), "White"),
        box("Post R", (0.16, 0.16, 2.8), (1.6, 0, 1.4), "White"),
        box("Board", (3.6, 0.1, 1.2), (0, -0.05, 2.0), "Teal"),
        box("Frame Top", (3.75, 0.14, 0.1), (0, -0.05, 2.62), "White"),
        box("Frame Bottom", (3.75, 0.14, 0.1), (0, -0.05, 1.38), "White"),
        box("Plank", (2.4, 0.08, 0.32), (0, -0.05, 0.95), "Coral"),
    ]
    return join(name, parts)


def pa_speaker(name="PaSpeaker"):
    """A 5 m pole with two horn speakers (one each way along -Y / +Y)."""
    parts = [cylinder("Pole", 0.07, 5.0, (0, 0, 2.5), "Steel", vertices=8)]
    parts.append(box("Arm", (0.08, 0.9, 0.08), (0, 0, 4.7), "Steel"))
    for side in (-1, 1):
        parts.append(cylinder("Horn", 0.32, 0.6, (0, side * 0.7, 4.55), "White", vertices=10,
                              rotation=(math.radians(90 * side + 0), 0, 0), radius_top=0.08))
        parts.append(cylinder("Driver", 0.12, 0.25, (0, side * 0.32, 4.55), "Charcoal", vertices=8, rotation=(math.radians(90), 0, 0)))
    return join(name, parts)


def flag_pole(name="FlagPole"):
    parts = [cylinder("Pole", 0.06, 6.0, (0, 0, 3.0), "White", vertices=8)]
    parts.append(ico("Top", 0.12, (0, 0, 6.05), "Yellow"))
    parts.append(cylinder("Base", 0.25, 0.2, (0, 0, 0.1), "Concrete", vertices=8))
    return join(name, parts)


def flag(name, mat):
    """1.6 x 1 m flag with a gentle wave, hoist edge on the pivot, flying toward +X (Unity -X)."""
    cols = 6
    verts = []
    for i in range(cols + 1):
        x = 1.6 * i / cols
        y = 0.12 * math.sin(i / cols * math.pi * 1.5) * (i / cols)
        verts += [(x, y, 0.0), (x, y, -1.0)]
    faces = [(i * 2, i * 2 + 2, i * 2 + 3, i * 2 + 1) for i in range(cols)]
    return mesh_object(name, verts, faces, mat, double_sided=True)


def bunting(name="Bunting"):
    """An 8 m string of pennants in park colors, hanging between two points at its ends (x = -4 .. 4)."""
    parts = []
    count = 13
    colors = ("Coral", "Yellow", "Teal", "White")
    rope = []
    for i in range(25):
        t = i / 24
        rope.append((-4 + 8 * t, 0, catenary(t, 0.6)))
    for i in range(24):
        (x0, _, z0), (x1, _, z1) = rope[i], rope[i + 1]
        mid = ((x0 + x1) / 2, 0, (z0 + z1) / 2)
        seg = box("Rope", (math.hypot(x1 - x0, z1 - z0) + 0.02, 0.02, 0.02), mid, "White",
                  rotation=(0, -math.atan2(z1 - z0, x1 - x0), 0))
        parts.append(seg)
    for i in range(count):
        t = (i + 0.5) / count
        x = -4 + 8 * t
        z = catenary(t, 0.6)
        w = 0.24
        parts.append(mesh_object("Pennant", [(x - w, 0, z), (x + w, 0, z), (x, 0, z - 0.55)], [(0, 2, 1)], colors[i % len(colors)], double_sided=True))
    return join(name, parts)


def string_lights(name="StringLights"):
    """An 8 m sagging wire of round bulbs (the Bulb material is swapped for a glowing one in Unity)."""
    parts = []
    pts = []
    for i in range(25):
        t = i / 24
        pts.append((-4 + 8 * t, 0, catenary(t, 0.5)))
    for i in range(24):
        (x0, _, z0), (x1, _, z1) = pts[i], pts[i + 1]
        parts.append(box("Wire", (math.hypot(x1 - x0, z1 - z0) + 0.02, 0.015, 0.015), ((x0 + x1) / 2, 0, (z0 + z1) / 2), "Charcoal",
                         rotation=(0, -math.atan2(z1 - z0, x1 - x0), 0)))
    for i in range(12):
        t = (i + 0.5) / 12
        x, z = -4 + 8 * t, catenary(t, 0.5)
        parts.append(cylinder("Socket", 0.03, 0.08, (x, 0, z - 0.04), "Charcoal", vertices=6))
        parts.append(ico("Bulb", 0.07, (x, 0, z - 0.13), "Bulb"))
    return join(name, parts)


def light_pole(name="LightPole"):
    """A 4.2 m wooden pole that string lights and bunting hang from."""
    parts = [box("Pole", (0.16, 0.16, 4.2), (0, 0, 2.1), "DarkWood"), box("Cap", (0.24, 0.24, 0.08), (0, 0, 4.24), "White")]
    return join(name, parts)


def flower_planter(name="FlowerPlanter"):
    rng = random.Random(11)
    parts = [box("Box", (1.6, 0.6, 0.5), (0, 0, 0.25), "Wood"), box("Rim", (1.7, 0.7, 0.06), (0, 0, 0.5), "White"),
             box("Soil", (1.5, 0.5, 0.04), (0, 0, 0.5), "Coconut")]
    for i in range(9):
        x, y = rng.uniform(-0.65, 0.65), rng.uniform(-0.18, 0.18)
        parts.append(ico("Leaves", rng.uniform(0.16, 0.22), (x, y, 0.62), "Leaf"))
    for i in range(10):
        x, y = rng.uniform(-0.68, 0.68), rng.uniform(-0.2, 0.2)
        parts.append(ico("Bloom", rng.uniform(0.07, 0.1), (x, y, rng.uniform(0.75, 0.85)), ("Flower", "Yellow", "White", "Coral")[i % 4]))
    return join(name, parts)


def weeds(name="Weeds"):
    """A scruffy clump of weeds and dry grass growing out of the cracks."""
    rng = random.Random(5)
    parts = []
    for i in range(14):
        a = rng.uniform(0, math.tau)
        r = rng.uniform(0, 0.25)
        x, y = math.cos(a) * r, math.sin(a) * r
        h = rng.uniform(0.25, 0.6)
        lean = rng.uniform(-0.15, 0.15)
        w = 0.05
        parts.append(mesh_object("Blade", [(x - w, y, 0), (x + w, y, 0), (x + lean, y + lean * 0.5, h)], [(0, 1, 2)],
                                 "LeafDark" if i % 3 else "DeadGrass", double_sided=True))
    return join(name, parts)


def trash_bags(name="TrashBags"):
    parts = []
    for i, (x, y, r) in enumerate(((0, 0, 0.42), (0.6, 0.25, 0.34), (-0.2, 0.55, 0.3))):
        bag = ico("Bag", r, (x, y, r * 0.85), "Charcoal", subdivisions=1)
        bag.scale = (1, 1, 0.9)
        parts.append(bag)
        parts.append(cylinder("Tie", 0.06, 0.16, (x, y, r * 1.7), "Yellow", vertices=5))
    return join(name, parts)


def rusty_barrel(name="RustyBarrel"):
    parts = [cylinder("Barrel", 0.3, 0.9, (0, 0, 0.45), "Rust", vertices=10)]
    for z in (0.18, 0.72):
        parts.append(cylinder("Ring", 0.315, 0.05, (0, 0, z), "DarkWood", vertices=10))
    parts.append(cylinder("Lid", 0.28, 0.03, (0, 0, 0.91), "Steel", vertices=10))
    return join(name, parts)


AMBIENCE_MODELS = (
    seagull, sailboat, fishing_boat, jet_ski, blimp, helicopter, heli_rotor, park_arch, sign_old, sign_painted,
    pa_speaker, flag_pole, lambda: flag("FlagCoral", "Coral"), lambda: flag("FlagTeal", "Teal"),
    lambda: flag("FlagYellow", "Yellow"), bunting, string_lights, light_pole, flower_planter, weeds, trash_bags, rusty_barrel,
)


def build_all(only_new=False):
    clear_scene()
    built = []
    factories = AMBIENCE_MODELS if only_new else (
        lambda: palm_tree("PalmTree", 6.5, seed=1),
        lambda: palm_tree("PalmTreeTall", 8.0, seed=7),
        lambda: kiosk("KioskTeal", "Teal"),
        lambda: kiosk("KioskCoral", "Coral"),
        wave_machine_house,
        surfboard,
        lifeguard_tower,
        lambda: beach_umbrella("BeachUmbrellaTeal", "Teal"),
        lambda: beach_umbrella("BeachUmbrellaCoral", "Coral"),
        lounge_chair,
        lambda: rock("Rock", 3),
        cash_register, computer, wave_console, price_stand, car, counter, board_rack, return_crate, bench,
        # Surfboards and people are built by build_boards.py and build_characters.py (the old board_* and char_*
        # functions here are not exported any more, so they can't overwrite the new models).
    ) + AMBIENCE_MODELS
    for factory in factories:
        obj = factory()
        if obj.location.length > 0:  # single-mesh models: make sure the pivot is the base center too
            bpy.context.scene.cursor.location = (0, 0, 0)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        built.append((obj.name, export(obj)))
        obj.hide_set(True)
    return built


# Set ONLY_NEW = True before exec() to rebuild just the ambience-step models.
result = {"exported": build_all(globals().get("ONLY_NEW", False))}
