"""
Builds the low-poly people (surfers, staff, guests) in Blender and exports each body part as an FBX.
House style "Chunky Beach Buddies" (docs/research/character-look.md): a big round head (about 1/4 of the height),
mitten hands, shoe-box feet, chunky limbs, and a face made of a few big shapes (eyes, brows, a mouth) that are their
own parts, so Unity can blink and switch expressions. Outfits are painted: every body part is split into material
bands (Top/Mid/Waist on the torso, SleeveShort/SleeveLong/Cuff on the arms, Bottom/ShortsLeg/Leg on the legs) that
CharacterRig colors per outfit (tee, rash guard, wetsuit, bikini...). Parts get UVs so patterns (stripes, hibiscus,
checker) can be laid on.

Run in Blender (Blender MCP): exec(open(path).read(), ns). It borrows helpers/palette/export from build_models.py.

Pivots are the joints the parts hang from, built facing -y (Unity +z). Joint offsets (unchanged, the rig depends on
them): shoulders hips + (+-0.27, 0.5, 0), head hips + 0.55 up, hips +-0.1 sideways, elbow 0.29 below the shoulder,
knee 0.41 below the hip, standing hips height 0.85.
Head attachments (hair, hats, glasses, beards, face parts) are all built around the same neck-base pivot.
"""
import math
import bpy
import bmesh

_here = "/Users/ido/surf-pool-sim/tools/blender/build_models.py"
_src = open(_here).read()
exec(_src[:_src.rindex("result = {")], globals())

PALETTE.update({
    # Painted per person in Unity (keep these names: CharacterRig matches them).
    "Skin": (0.88, 0.68, 0.52),
    "Top": (0.9, 0.9, 0.9), "Mid": (0.9, 0.9, 0.9), "Waist": (0.9, 0.9, 0.9),
    "SleeveShort": (0.9, 0.9, 0.9), "SleeveLong": (0.88, 0.68, 0.52), "Cuff": (0.88, 0.68, 0.52),
    "Bottom": (0.2, 0.3, 0.5), "ShortsLeg": (0.2, 0.3, 0.5), "Leg": (0.88, 0.68, 0.52),
    "Hair": (0.3, 0.2, 0.12), "Brow": (0.2, 0.13, 0.08), "Nose": (0.88, 0.68, 0.52), "Blush": (0.95, 0.6, 0.55),
    "HatMain": (0.9, 0.4, 0.3), "HatAccent": (0.95, 0.95, 0.92), "Lens": (0.08, 0.1, 0.14), "Frame": (0.1, 0.1, 0.12),
    # Fixed colors.
    "EyeWhite": (0.98, 0.97, 0.95), "Pupil": (0.06, 0.05, 0.06), "Glint": (1.0, 1.0, 1.0),
    "Mouth": (0.35, 0.1, 0.1), "MouthIn": (0.22, 0.05, 0.07), "Teeth": (0.98, 0.97, 0.94), "Tongue": (0.9, 0.38, 0.42),
})
EXPORT_DIR = globals().get("EXPORT_DIR_OVERRIDE", EXPORT_DIR)

UPPER_ARM, FOREARM = 0.29, 0.26
THIGH, SHIN = 0.41, 0.38
FACE_Y = -0.182  # front of the face (the skull surface, toward -y)


def rings(name, profile, mat, sides=16, depth_scale=1.0, center=(0.0, 0.0), cap_top=True, cap_bottom=True):
    """A lathe-like low-poly body: profile is [(z, radius)], bottom to top; depth_scale squashes front-back."""
    cx, cy = center
    verts, faces = [], []
    for z, r in profile:
        for i in range(sides):
            a = 2 * math.pi * (i + 0.5) / sides
            verts.append((cx + math.cos(a) * r, cy + math.sin(a) * r * depth_scale, z))
    for k in range(len(profile) - 1):
        for i in range(sides):
            a, b = k * sides + i, k * sides + (i + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    if cap_bottom:
        faces.append(tuple(reversed(range(sides))))
    if cap_top:
        top = (len(profile) - 1) * sides
        faces.append(tuple(range(top, top + sides)))
    obj = mesh_object(name, verts, faces, mat)
    recalc_normals(obj)
    return obj


def soft_box(name, size, location, mat, width=0.02, rotation=(0, 0, 0), segments=2):
    """A box with rounded (bevelled) edges: the "toy" look."""
    obj = box(name, size, location, mat, rotation=rotation)
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width = min(width, min(size) * 0.45)
    mod.segments = segments
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def blob(name, radius, location, mat, scale=(1, 1, 1), subdivisions=3):
    obj = ico(name, radius, location, mat, subdivisions=subdivisions)
    obj.scale = scale
    return obj


def tube(name, a, b, r0, r1, mat, sides=7):
    """A tapered cylinder from point a to point b (dreadlocks, ponytails, straps)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    obj = cylinder(name, r0, d.length, (a + b) / 2, mat, vertices=sides, radius_top=r1)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d.normalized())
    return obj


# ---------------------------------------------------------------- head and face
SKULL = [(0.04, 0.12), (0.08, 0.165), (0.15, 0.19), (0.24, 0.2), (0.33, 0.195), (0.4, 0.17), (0.45, 0.12), (0.475, 0.05)]


def char_head():
    """Big round head on a short neck. The face features are separate parts (CharEye, CharBrow, CharMouth*)."""
    parts = [
        rings("Neck", [(-0.03, 0.075), (0.07, 0.072)], "Skin", sides=8),
        rings("Skull", SKULL, "Skin", sides=20, depth_scale=0.92, center=(0, 0.005)),
        # Rosy blush spots on the cheeks (their own material: a warm tint of the skin).
        blush("BlushL", -0.115), blush("BlushR", 0.115),
        blob("EarL", 0.045, (-0.198, 0.0, 0.22), "Skin", scale=(0.55, 0.9, 1.2), subdivisions=2),
        blob("EarR", 0.045, (0.198, 0.0, 0.22), "Skin", scale=(0.55, 0.9, 1.2), subdivisions=2),
        # A small rounded nose (its own material, so a zinc nose can paint it white or neon).
        blob("Nose", 0.032, (0, FACE_Y - 0.008, 0.185), "Nose", scale=(1.05, 0.85, 1.1), subdivisions=3),
    ]
    return join("CharHead", parts)


def blush(name, x):
    disc = cylinder(name, 0.034, 0.006, (x, -0.168, 0.165), "Blush", vertices=16, rotation=(math.radians(90), 0, math.radians(-30 if x < 0 else 30)))
    disc.scale = (1.25, 1.0, 0.8)
    return disc


def char_eye():
    """One eye: rounded white, a big dark pupil and a glint. Pivot at its center (Unity scales it to blink)."""
    white = cylinder("White", 0.046, 0.02, (0, 0, 0), "EyeWhite", vertices=20, rotation=(math.radians(90), 0, 0))
    white.scale = (1.0, 1.0, 1.18)
    pupil = cylinder("Pupil", 0.031, 0.014, (0, -0.012, -0.008), "Pupil", vertices=18, rotation=(math.radians(90), 0, 0))
    pupil.scale = (1.0, 1.0, 1.2)
    glint = cylinder("Glint", 0.009, 0.008, (0.011, -0.02, 0.008), "Glint", vertices=10, rotation=(math.radians(90), 0, 0))
    return join("CharEye", [white, pupil, glint])


def char_brow():
    return join("CharBrow", [soft_box("Brow", (0.095, 0.022, 0.032), (0, 0, 0), "Brow", width=0.014, segments=3)])


def arc(name, major, minor, mat, keep_below=0.0):
    """The bottom half of a ring facing -y: a curved smile line."""
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=24, minor_segments=6,
                                     rotation=(math.radians(90), 0, 0))
    obj = bpy.context.active_object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z > keep_below + 0.001], context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()
    return finish(obj, mat)


def char_mouth_smile():
    smile = arc("Smile", 0.042, 0.009, "Mouth", keep_below=-0.004)
    smile.location = (0, 0, 0.02)
    return join("CharMouthSmile", [smile])


def char_mouth_grin():
    """Big open D-shaped grin with teeth and a tongue (stoked)."""
    parts = [
        soft_box("Open", (0.11, 0.014, 0.052), (0, 0, -0.008), "MouthIn", width=0.02),
        soft_box("Teeth", (0.088, 0.008, 0.016), (0, -0.008, 0.01), "Teeth", width=0.005),
        soft_box("Tongue", (0.05, 0.008, 0.016), (0, -0.007, -0.022), "Tongue", width=0.006),
    ]
    return join("CharMouthGrin", parts)


def char_mouth_o():
    """A round open "O" (scared, shouting)."""
    parts = [
        cylinder("O", 0.032, 0.014, (0, 0, 0), "MouthIn", vertices=10, rotation=(math.radians(90), 0, 0)),
    ]
    parts[0].scale = (0.9, 1, 1.15)
    return join("CharMouthO", parts)


def char_mouth_flat():
    return join("CharMouthFlat", [soft_box("Flat", (0.065, 0.016, 0.018), (0, 0, 0), "Mouth", width=0.007)])


# ---------------------------------------------------------------- hair (all fit the same skull)
def hair_cap(name, z0=0.27, thick=0.014, mat="Hair", front_z=0.33, swoop=0.0):
    """The hair shell over the top and back of the skull. Lower at the back than at the front."""
    profile = [(z0, 0.2 + thick), (0.34, 0.197 + thick), (0.4, 0.172 + thick), (0.45, 0.124 + thick), (0.49, 0.05)]
    shell = rings(name, profile, mat, sides=12, depth_scale=0.93, center=(0, 0.012))
    # Cut the face open: pull the front-bottom ring up to the hairline.
    me = shell.data
    for v in me.vertices:
        if v.co.y < -0.05 and v.co.z < front_z:
            # A swoop drops the hairline on one side (a side-swept fringe).
            edge = front_z - swoop * max(0.0, -v.co.x / 0.2)
            v.co.z = edge + (v.co.z - z0) * 0.25
    return shell


def back_plate(name, z_top, z_bottom, width, mat="Hair", depth=0.06, y=0.15):
    return soft_box(name, (width, depth, z_top - z_bottom), (0, y, (z_top + z_bottom) / 2), mat, width=0.025)


def char_hair_short():
    parts = [hair_cap("Cap", z0=0.24, swoop=0.05),
             soft_box("Tuft", (0.07, 0.07, 0.06), (0.03, 0.06, 0.49), "Hair", width=0.025, rotation=(-0.5, 0.3, 0), segments=3)]
    return join("CharHairShort", parts)


def char_hair_long():
    parts = [hair_cap("Cap", z0=0.2, front_z=0.35, swoop=0.04),
             back_plate("Back", 0.4, -0.02, 0.36, depth=0.08, y=0.17),
             soft_box("SideL", (0.06, 0.2, 0.3), (-0.2, 0.05, 0.18), "Hair", width=0.025),
             soft_box("SideR", (0.06, 0.2, 0.3), (0.2, 0.05, 0.18), "Hair", width=0.025)]
    return join("CharHairLong", parts)


def char_hair_bun():
    parts = [hair_cap("Cap", z0=0.25, thick=0.01), blob("Bun", 0.085, (0, 0.06, 0.53), "Hair", scale=(1, 1, 0.9)),
             cylinder("Tie", 0.05, 0.03, (0, 0.05, 0.47), "HatMain", vertices=10)]
    return join("CharHairBun", parts)


def char_hair_curly():
    parts = [hair_cap("Cap", z0=0.24)]
    spots = [(0, 0.0, 0.5), (-0.13, 0.02, 0.45), (0.13, 0.02, 0.45), (0, 0.14, 0.45), (-0.15, 0.12, 0.35),
             (0.15, 0.12, 0.35), (0, 0.18, 0.3), (-0.09, -0.11, 0.44), (0.09, -0.11, 0.44), (-0.19, 0.0, 0.32),
             (0.19, 0.0, 0.32), (-0.06, 0.08, 0.53), (0.07, 0.08, 0.53)]
    for i, (x, y, z) in enumerate(spots):
        parts.append(blob(f"Curl{i}", 0.1, (x, y, z), "Hair", subdivisions=2))
    return join("CharHairCurly", parts)


def char_hair_buzz():
    return join("CharHairBuzz", [hair_cap("Cap", z0=0.25, thick=0.006, front_z=0.36)])


def char_hair_mohawk():
    parts = [hair_cap("Shaved", z0=0.25, thick=0.004, front_z=0.37)]
    for i in range(6):
        t = i / 5
        y = -0.13 + t * 0.3
        z = 0.47 + math.sin(t * math.pi) * 0.05
        parts.append(soft_box(f"Spike{i}", (0.05, 0.07, 0.13 - abs(t - 0.4) * 0.05), (0, y, z + 0.05), "Hair", width=0.015,
                              rotation=(-0.4 + t * 0.8, 0, 0)))
    return join("CharHairMohawk", parts)


def char_hair_dreads():
    parts = [hair_cap("Cap", z0=0.24)]
    for i in range(11):
        a = math.radians(-120 + i * 24)
        x, y = math.sin(a) * 0.2, math.cos(a) * 0.18 + 0.02
        top = (x, y, 0.34)
        bottom = (x * 1.15, y * 1.1 + 0.03, 0.02 + (i % 3) * 0.04)
        parts.append(tube(f"Lock{i}", top, bottom, 0.03, 0.026, "Hair"))
    return join("CharHairDreads", parts)


def char_hair_ponytail():
    parts = [hair_cap("Cap", z0=0.24, thick=0.01),
             cylinder("Tie", 0.045, 0.035, (0, 0.2, 0.36), "HatMain", vertices=10, rotation=(math.radians(70), 0, 0)),
             tube("Tail", (0, 0.22, 0.36), (0, 0.27, 0.05), 0.06, 0.03, "Hair", sides=9)]
    return join("CharHairPonytail", parts)


def char_hair_mullet():
    parts = [hair_cap("Cap", z0=0.27, thick=0.01, front_z=0.37),
             back_plate("Party", 0.33, 0.0, 0.26, depth=0.07, y=0.18),
             soft_box("Spike", (0.1, 0.07, 0.07), (0, -0.12, 0.47), "Hair", width=0.02, rotation=(-0.4, 0, 0))]
    return join("CharHairMullet", parts)


def char_hair_mop():
    """Grom bowl cut: a helmet of hair with a straight heavy fringe."""
    parts = [hair_cap("Cap", z0=0.2, thick=0.025, front_z=0.31),
             soft_box("Fringe", (0.34, 0.07, 0.08), (0, -0.16, 0.34), "Hair", width=0.025)]
    return join("CharHairMop", parts)


def char_hair_spiky():
    parts = [hair_cap("Cap", z0=0.25, thick=0.01)]
    for i in range(9):
        a = math.radians(i * 40)
        r = 0.09 + (i % 2) * 0.05
        base = (math.cos(a) * r, math.sin(a) * r + 0.02, 0.45)
        tip = (math.cos(a) * (r + 0.07), math.sin(a) * (r + 0.07) + 0.02, 0.58 - (i % 2) * 0.04)
        parts.append(tube(f"Spike{i}", base, tip, 0.04, 0.005, "Hair", sides=5))
    return join("CharHairSpiky", parts)


# ---------------------------------------------------------------- hats, glasses, facial hair
def char_hat_cap():
    """Trucker cap: main color crown, white mesh front panel, a curved brim."""
    parts = [rings("Crown", [(0.33, 0.215), (0.43, 0.2), (0.5, 0.14), (0.53, 0.05)], "HatMain", sides=12, depth_scale=0.95,
                   center=(0, 0.01)),
             soft_box("Panel", (0.22, 0.03, 0.13), (0, -0.19, 0.42), "HatAccent", width=0.02, rotation=(-0.25, 0, 0)),
             soft_box("Brim", (0.26, 0.17, 0.02), (0, -0.27, 0.36), "HatMain", width=0.01, rotation=(-0.15, 0, 0)),
             blob("Button", 0.02, (0, 0.01, 0.535), "HatAccent", subdivisions=1)]
    return join("CharHatCap", parts)


def char_hat_bucket():
    parts = [rings("Crown", [(0.33, 0.22), (0.47, 0.2), (0.53, 0.17), (0.55, 0.0)], "HatMain", sides=12, depth_scale=0.95,
                   center=(0, 0.01)),
             rings("Brim", [(0.29, 0.31), (0.34, 0.22)], "HatMain", sides=12, depth_scale=0.95, center=(0, 0.01), cap_top=False,
                   cap_bottom=False),
             rings("Band", [(0.335, 0.223), (0.37, 0.218)], "HatAccent", sides=12, depth_scale=0.95, center=(0, 0.01),
                   cap_top=False, cap_bottom=False)]
    return join("CharHatBucket", parts)


def char_hat_visor():
    parts = [rings("Band", [(0.33, 0.215), (0.38, 0.215)], "HatMain", sides=12, depth_scale=0.93, center=(0, 0.012),
                   cap_top=False, cap_bottom=False),
             soft_box("Brim", (0.28, 0.16, 0.02), (0, -0.26, 0.345), "HatAccent", width=0.01, rotation=(-0.2, 0, 0))]
    return join("CharHatVisor", parts)


def char_hat_band():
    parts = [rings("Band", [(0.35, 0.214), (0.39, 0.209)], "HatMain", sides=12, depth_scale=0.93, center=(0, 0.012),
                   cap_top=False, cap_bottom=False),
             soft_box("Knot", (0.05, 0.04, 0.05), (0.06, 0.2, 0.37), "HatMain", width=0.012),
             tube("TailA", (0.05, 0.21, 0.36), (0.1, 0.27, 0.24), 0.02, 0.012, "HatMain", sides=5),
             tube("TailB", (0.07, 0.21, 0.36), (0.02, 0.28, 0.22), 0.02, 0.012, "HatMain", sides=5)]
    return join("CharHatBand", parts)


def char_glasses_sunnies():
    parts = [soft_box("LensL", (0.11, 0.02, 0.075), (-0.08, FACE_Y - 0.05, 0.25), "Lens", width=0.015),
             soft_box("LensR", (0.11, 0.02, 0.075), (0.08, FACE_Y - 0.05, 0.25), "Lens", width=0.015),
             soft_box("Bridge", (0.06, 0.02, 0.018), (0, FACE_Y - 0.05, 0.27), "Frame", width=0.006),
             soft_box("ArmL", (0.016, 0.22, 0.016), (-0.2, FACE_Y + 0.07, 0.26), "Frame", width=0.005),
             soft_box("ArmR", (0.016, 0.22, 0.016), (0.2, FACE_Y + 0.07, 0.26), "Frame", width=0.005)]
    return join("CharGlassesSunnies", parts)


def char_glasses_shield():
    parts = [soft_box("Lens", (0.34, 0.025, 0.08), (0, FACE_Y - 0.05, 0.255), "Lens", width=0.025),
             soft_box("Top", (0.35, 0.03, 0.018), (0, FACE_Y - 0.05, 0.3), "Frame", width=0.008),
             soft_box("ArmL", (0.016, 0.22, 0.016), (-0.2, FACE_Y + 0.07, 0.27), "Frame", width=0.005),
             soft_box("ArmR", (0.016, 0.22, 0.016), (0.2, FACE_Y + 0.07, 0.27), "Frame", width=0.005)]
    return join("CharGlassesShield", parts)


def char_glasses_round():
    parts = []
    for side in (-1, 1):
        parts.append(cylinder(f"Lens{side}", 0.047, 0.012, (side * 0.08, FACE_Y - 0.05, 0.25), "Lens", vertices=12,
                              rotation=(math.radians(90), 0, 0)))
        parts.append(torus(f"Rim{side}", 0.048, 0.008, (side * 0.08, FACE_Y - 0.053, 0.25), "Frame",
                           rotation=(math.radians(90), 0, 0)))
        parts.append(soft_box(f"Arm{side}", (0.012, 0.22, 0.012), (side * 0.2, FACE_Y + 0.07, 0.26), "Frame", width=0.004))
    parts.append(soft_box("Bridge", (0.05, 0.012, 0.012), (0, FACE_Y - 0.053, 0.265), "Frame", width=0.004))
    return join("CharGlassesRound", parts)


def char_beard():
    """A full chunky beard around the jaw (hair color)."""
    parts = [rings("Beard", [(0.02, 0.12), (0.08, 0.175), (0.16, 0.2), (0.2, 0.2)], "Hair", sides=12, depth_scale=0.92,
                   center=(0, 0.0), cap_top=False)]
    me = parts[0].data
    for v in me.vertices:  # keep only the front-ish half: push the back half inside the head
        if v.co.y > 0.04:
            v.co.y = 0.04 + (v.co.y - 0.04) * 0.2
            v.co.x *= 0.85
    return join("CharBeard", parts)


def char_mustache():
    parts = [soft_box("L", (0.08, 0.03, 0.03), (-0.04, FACE_Y - 0.02, 0.145), "Hair", width=0.012, rotation=(0, 0.35, 0)),
             soft_box("R", (0.08, 0.03, 0.03), (0.04, FACE_Y - 0.02, 0.145), "Hair", width=0.012, rotation=(0, -0.35, 0))]
    return join("CharMustache", parts)


def torus(name, major, minor, location, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, location=location, rotation=rotation,
                                     major_segments=14, minor_segments=5)
    obj = bpy.context.active_object
    obj.name = name
    return finish(obj, mat)


# ---------------------------------------------------------------- body (painted in bands)
def char_torso():
    """Waist to shoulders, a soft bean: Waist band, Mid (belly), Top (chest + shoulders)."""
    d = 0.68
    parts = [rings("Waist", [(0.0, 0.19), (0.12, 0.195)], "Waist", sides=12, depth_scale=d),
             rings("Mid", [(0.12, 0.195), (0.3, 0.228)], "Mid", sides=12, depth_scale=d),
             rings("Top", [(0.3, 0.228), (0.44, 0.235), (0.53, 0.18), (0.57, 0.1)], "Top", sides=12, depth_scale=d),
             blob("ShoulderL", 0.07, (-0.23, 0, 0.47), "Top", subdivisions=3),
             blob("ShoulderR", 0.07, (0.23, 0, 0.47), "Top", subdivisions=3)]
    return join("CharTorso", parts)


def char_shorts():
    """Hips and seat of the swimwear (the legs of the shorts are on the thighs so they swing with them)."""
    waist = rings("Seat", [(-0.16, 0.205), (-0.03, 0.215), (0.06, 0.2)], "Bottom", sides=12, depth_scale=0.7)
    band = rings("Band", [(0.03, 0.205), (0.07, 0.205)], "Bottom", sides=12, depth_scale=0.72, cap_top=False, cap_bottom=False)
    return join("CharShorts", [waist, band])


def char_upper_arm():
    return join("CharUpperArm", [
        rings("Sleeve", [(-0.14, 0.094), (0.0, 0.1)], "SleeveShort", cap_top=False),
        blob("SleeveTop", 0.1, (0, 0, 0.0), "SleeveShort", scale=(1, 1, 0.75)),
        rings("Arm", [(-UPPER_ARM - 0.01, 0.078), (-0.14, 0.086)], "SleeveLong"),
        blob("Elbow", 0.078, (0, 0, -UPPER_ARM), "SleeveLong", subdivisions=2)])


def char_forearm():
    parts = [rings("Forearm", [(-FOREARM + 0.02, 0.066), (0.0, 0.076)], "Cuff"),
             blob("Wrist", 0.066, (0, 0, -FOREARM + 0.01), "Skin", scale=(1, 1, 0.7), subdivisions=2),
             # Mitten hand: a soft rounded block, and a stubby thumb.
             soft_box("Mitten", (0.11, 0.08, 0.14), (0, 0, -FOREARM - 0.07), "Skin", width=0.038, segments=3),
             soft_box("Thumb", (0.04, 0.05, 0.065), (0.058, -0.03, -FOREARM - 0.045), "Skin", width=0.018, rotation=(0, 0.4, 0),
                      segments=3)]
    return join("CharForearm", parts)


def char_thigh():
    return join("CharThigh", [
        rings("ShortsLeg", [(-0.25, 0.126), (0.05, 0.13)], "ShortsLeg"),
        rings("Thigh", [(-THIGH + 0.02, 0.096), (-0.25, 0.11)], "Leg")])


def char_shin():
    parts = [rings("Shin", [(-SHIN + 0.03, 0.074), (-0.2, 0.088), (0.0, 0.09)], "Leg"),
             blob("Knee", 0.094, (0, 0, 0), "Leg", subdivisions=2),
             blob("Ankle", 0.076, (0, 0, -SHIN + 0.02), "Skin", scale=(1, 1, 0.6), subdivisions=2),
             # A big rounded foot (bare: surfers), toes toward -y.
             soft_box("Foot", (0.14, 0.29, 0.09), (0, -0.07, -SHIN - 0.035), "Skin", width=0.04, segments=3)]
    return join("CharShin", parts)


HAIR = [char_hair_short, char_hair_long, char_hair_bun, char_hair_curly, char_hair_buzz, char_hair_mohawk, char_hair_dreads,
        char_hair_ponytail, char_hair_mullet, char_hair_mop, char_hair_spiky]
HATS = [char_hat_cap, char_hat_bucket, char_hat_visor, char_hat_band]
GLASSES = [char_glasses_sunnies, char_glasses_shield, char_glasses_round]
FACE = [char_eye, char_brow, char_mouth_smile, char_mouth_grin, char_mouth_o, char_mouth_flat, char_beard, char_mustache]
PARTS = [char_head] + FACE + HAIR + HATS + GLASSES + [char_torso, char_shorts, char_upper_arm, char_forearm, char_thigh, char_shin]


def finish_part(obj):
    """Bake rotation/scale into the mesh (join keeps the first part's), flat shade, and add box-projected UVs."""
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    # Toy look: smooth rounded surfaces, crisp edges kept where faces meet at a sharp angle.
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(50))
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=0.35)
    bpy.ops.object.mode_set(mode="OBJECT")


def build_people(only=None):
    clear_scene()
    built = []
    for factory in PARTS:
        obj = factory()
        if only and obj.name not in only:
            bpy.data.objects.remove(obj, do_unlink=True)
            continue
        finish_part(obj)
        built.append(obj.name)
        export(obj)
    return built


if globals().get("BUILD_PEOPLE", True):
    result = {"exported": build_people(globals().get("ONLY"))}
