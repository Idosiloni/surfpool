"""
Builds the low-poly surf park models in Blender and exports each one as an FBX into
Assets/_Game/Models/. Run inside Blender (e.g. via the Blender MCP):

    exec(open("/Users/ido/surf-pool-sim/tools/blender/build_models.py").read())

Every model is built from simple primitives, flat shaded, with a few flat-color materials
(the park palette below). Models sit on z = 0 (Blender is Z-up) and are centered on x/y.
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


def finish(obj, mat_name):
    obj.data.materials.clear()
    obj.data.materials.append(material(mat_name))
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj


def box(name, size, location, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, mat)


def cylinder(name, radius, depth, location, mat, vertices=8, rotation=(0, 0, 0), radius_top=None):
    if radius_top is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius_top, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    return finish(obj, mat)


def ico(name, radius, location, mat, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=radius, location=location)
    obj = bpy.context.active_object
    obj.name = name
    return finish(obj, mat)


def mesh_object(name, verts, faces, mat, double_sided=False):
    """Mesh from raw data. double_sided adds back faces (offset a hair) so thin sheets show from both sides in Unity."""
    if double_sided:
        count = len(verts)
        verts = list(verts) + [(x, y, z - 0.01) for x, y, z in verts]
        faces = list(faces) + [tuple(i + count for i in reversed(face)) for face in faces]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
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

def palm_tree(name="PalmTree", height=6.5, seed=1):
    """Curved segmented trunk, drooping fronds, a few coconuts."""
    rng = random.Random(seed)
    parts = []
    segments = 7
    lean = Vector((0.9, 0.2, 0))
    top = Vector((0, 0, 0))
    radius = 0.24
    for i in range(segments):
        t0, t1 = i / segments, (i + 1) / segments
        p0 = Vector((0, 0, height * t0)) + lean * (t0 ** 2)
        p1 = Vector((0, 0, height * t1)) + lean * (t1 ** 2)
        mid = (p0 + p1) / 2
        direction = (p1 - p0)
        seg = cylinder(f"Trunk{i}", radius * (1 - 0.35 * t0), direction.length * 1.02, mid, "Trunk", vertices=7,
                       radius_top=radius * (1 - 0.35 * t1) * 0.92)
        seg.rotation_mode = "QUATERNION"
        seg.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction.normalized())
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
        parts.append(seg)
        top = p1

    fronds = 8
    for i in range(fronds):
        angle = (i / fronds) * math.tau + rng.uniform(-0.2, 0.2)
        length = rng.uniform(2.4, 3.0)
        droop = rng.uniform(1.0, 1.5)
        mat = "Leaf" if i % 2 == 0 else "LeafDark"
        verts, faces = [], []
        steps = 5
        for s in range(steps + 1):
            t = s / steps
            out = length * t
            down = droop * t * t - 0.35 * t
            width = 0.55 * math.sin(math.pi * min(1.0, t * 1.15)) + 0.05
            center = Vector((math.cos(angle) * out, math.sin(angle) * out, -down))
            side = Vector((-math.sin(angle), math.cos(angle), 0)) * width
            verts += [tuple(top + center - side), tuple(top + center + Vector((0, 0, 0.12 * (1 - t)))),
                      tuple(top + center + side)]
        for s in range(steps):
            a = s * 3
            faces += [(a, a + 3, a + 4, a + 1), (a + 1, a + 4, a + 5, a + 2)]
        parts.append(mesh_object(f"Frond{i}", verts, faces, mat, double_sided=True))

    for i in range(3):
        angle = i * math.tau / 3
        parts.append(ico(f"Coconut{i}", 0.17, top + Vector((math.cos(angle) * 0.22, math.sin(angle) * 0.22, -0.25)), "Coconut"))
    return join(name, parts)


def kiosk(name, roof_mat):
    """Open-front beach kiosk frame: 4 posts, striped pitched roof with overhang, front sign board.
    Footprint 3.6 x 2.6 m, the open customer side faces -Y. Sits around a counter placed in Unity."""
    parts = []
    w, d, h = 3.6, 2.6, 2.5
    for x in (-w / 2 + 0.1, w / 2 - 0.1):
        for y in (-d / 2 + 0.1, d / 2 - 0.1):
            parts.append(box("Post", (0.14, 0.14, h), (x, y, h / 2), "Wood"))
    parts.append(box("Beam Front", (w, 0.16, 0.18), (0, -d / 2 + 0.1, h), "DarkWood"))
    parts.append(box("Beam Back", (w, 0.16, 0.18), (0, d / 2 - 0.1, h), "DarkWood"))
    # Striped roof: alternating colored slats, sloping down toward the customer side.
    stripes = 9
    roof_w, roof_d = w + 0.8, d + 1.0
    for i in range(stripes):
        x = -roof_w / 2 + roof_w * (i + 0.5) / stripes
        mat = roof_mat if i % 2 == 0 else "White"
        parts.append(box("Roof", (roof_w / stripes, roof_d, 0.08), (x, -0.15, h + 0.35), mat, rotation=(math.radians(-12), 0, 0)))
    # Scalloped valance hanging off the front edge.
    for i in range(stripes):
        x = -roof_w / 2 + roof_w * (i + 0.5) / stripes
        mat = roof_mat if i % 2 == 0 else "White"
        parts.append(box("Valance", (roof_w / stripes, 0.04, 0.28), (x, -roof_d / 2 - 0.12, h + 0.02), mat))
    parts.append(box("Sign", (2.0, 0.08, 0.45), (0, -d / 2 - 0.05, h + 0.85), "Navy"))
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


def lifeguard_tower(name="LifeguardTower"):
    """Stilts, platform with railing, ladder, and a red-and-white roof. Faces -Y (toward the pool when rotated)."""
    parts = []
    for x in (-0.55, 0.55):
        for y in (-0.55, 0.55):
            parts.append(box("Leg", (0.12, 0.12, 1.9), (x, y, 0.95), "White"))
    parts.append(box("Platform", (1.5, 1.5, 0.12), (0, 0, 1.9), "Wood"))
    for x, y, sx, sy in ((0, -0.72, 1.5, 0.06), (0, 0.72, 1.5, 0.06), (-0.72, 0, 0.06, 1.5), (0.72, 0, 0.06, 1.5)):
        parts.append(box("Rail", (sx, sy, 0.08), (x, y, 2.45), "Red"))
    for x in (-0.72, 0.72):
        for y in (-0.72, 0.72):
            parts.append(box("RoofPost", (0.08, 0.08, 1.4), (x, y, 2.6), "White"))
    for i in range(6):
        x = -0.9 + 1.8 * (i + 0.5) / 6
        parts.append(box("Roof", (1.8 / 6, 1.9, 0.07), (x, 0, 3.32), "Red" if i % 2 == 0 else "White",
                         rotation=(math.radians(-8), 0, 0)))
    for i in range(6):
        parts.append(box("LadderStep", (0.6, 0.06, 0.05), (0, 1.0, 0.3 * (i + 1)), "Wood"))
    for x in (-0.3, 0.3):
        parts.append(box("LadderRail", (0.06, 0.06, 2.0), (x, 1.0, 1.0), "Wood"))
    parts.append(box("Seat", (0.6, 0.5, 0.08), (0, 0.2, 2.35), "Yellow"))
    return join(name, parts)


def beach_umbrella(name="BeachUmbrella", color="Teal"):
    parts = [cylinder("Pole", 0.035, 2.4, (0, 0, 1.2), "White", vertices=6)]
    segments = 8
    for i in range(segments):
        a0 = i / segments * math.tau
        a1 = (i + 1) / segments * math.tau
        verts = [(0, 0, 2.45),
                 (math.cos(a0) * 1.25, math.sin(a0) * 1.25, 1.95),
                 (math.cos(a1) * 1.25, math.sin(a1) * 1.25, 1.95)]
        parts.append(mesh_object("Canopy", verts, [(0, 2, 1)], color if i % 2 == 0 else "White", double_sided=True))
    return join(name, parts)


def lounge_chair(name="LoungeChair"):
    parts = [box("Frame", (0.62, 1.7, 0.06), (0, 0, 0.32), "White")]
    for x in (-0.27, 0.27):
        for y in (-0.7, 0.7):
            parts.append(box("Leg", (0.05, 0.05, 0.3), (x, y, 0.15), "White"))
    parts.append(box("Back", (0.62, 0.7, 0.06), (0, 0.95, 0.62), "White", rotation=(math.radians(50), 0, 0)))
    parts.append(box("Cushion", (0.56, 1.6, 0.06), (0, -0.05, 0.38), "Coral"))
    return join(name, parts)


def rock(name="Rock", seed=3):
    rng = random.Random(seed)
    obj = ico(name, 0.7, (0, 0, 0.35), "Rock", subdivisions=1)
    for v in obj.data.vertices:
        v.co *= rng.uniform(0.75, 1.2)
        v.co.z *= 0.65
    return obj


def build_all():
    clear_scene()
    built = []
    for factory in (
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
    ):
        obj = factory()
        if obj.location.length > 0:  # single-mesh models: make sure the pivot is the base center too
            bpy.context.scene.cursor.location = (0, 0, 0)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        built.append((obj.name, export(obj)))
        obj.hide_set(True)
    return built


result = {"exported": build_all()}
