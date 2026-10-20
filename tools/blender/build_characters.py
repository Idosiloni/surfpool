"""
Builds the low-poly people (surfers, staff, guests) in Blender and exports each body part as an FBX.

Run in Blender (Blender MCP): exec(open(path).read(), ns). It borrows the helpers and palette from
build_models.py (everything above its final build line), so materials and export settings match the rest of
the park. Set EXPORT_DIR in ns before running to export somewhere else (e.g. a staging folder).

Parts are separate objects with their pivot at the joint they hang from, built facing -y (Unity +z):
  CharHead (neck base), hair styles CharHairShort/Long/Bun/Curly/Buzz and CharCap (neck base; the cap is in the
  shirt color), CharTorso and CharShorts (hips),
  CharUpperArm (shoulder) + CharForearm (elbow, with the hand), CharThigh (hip) + CharShin (knee, with the foot).
Joint offsets the Unity rig uses: shoulders hips + (+-0.27, 0.5, 0), head hips + 0.55 up, hips +-0.1 sideways,
elbow 0.29 below the shoulder, knee 0.41 below the hip. Standing hips height is 0.85.
Colors are painted per person in Unity by material name: Skin, Shirt, Shorts, Hair (Eyes/White/Brow/Mouth stay).
"""
import os
import bpy
import bmesh

_here = "/Users/ido/surf-pool-sim/tools/blender/build_models.py"
_src = open(_here).read()
exec(_src[:_src.rindex("result = {")], globals())

PALETTE.update({
    "Shirt": (0.85, 0.85, 0.85),
    "Shorts": (0.2, 0.3, 0.5),
    "Skin": (0.85, 0.65, 0.5),
    "Hair": (0.25, 0.16, 0.1),
    "Eyes": (0.08, 0.07, 0.07),
    "Brow": (0.18, 0.12, 0.08),
    "Mouth": (0.45, 0.16, 0.14),
})
EXPORT_DIR = globals().get("EXPORT_DIR_OVERRIDE", EXPORT_DIR)

UPPER_ARM, FOREARM = 0.29, 0.26
THIGH, SHIN = 0.41, 0.38


def rings(name, profile, mat, sides=8, depth_scale=1.0, center=(0.0, 0.0), cap_top=True, cap_bottom=True):
    """A lathe-like low-poly body: profile is [(z, radius)], bottom to top; depth_scale squashes front-back."""
    cx, cy = center
    verts, faces = [], []
    import math
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


def bevel_box(name, size, location, mat, width=0.03):
    obj = box(name, size, location, mat)
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = 1
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


# ---------------------------------------------------------------- head and hair
def char_head():
    parts = [
        rings("Neck", [(-0.02, 0.055), (0.09, 0.052)], "Skin", sides=7),
        bevel_box("Skull", (0.27, 0.26, 0.29), (0, 0, 0.2), "Skin", width=0.045),
        box("EarL", (0.03, 0.06, 0.075), (-0.145, 0.01, 0.19), "Skin"),
        box("EarR", (0.03, 0.06, 0.075), (0.145, 0.01, 0.19), "Skin"),
        box("Nose", (0.045, 0.05, 0.065), (0, -0.14, 0.18), "Skin"),
        # Eyes: white with a dark pupil in front, a brow above.
        box("EyeWhiteL", (0.06, 0.012, 0.05), (-0.062, -0.131, 0.222), "White"),
        box("EyeWhiteR", (0.06, 0.012, 0.05), (0.062, -0.131, 0.222), "White"),
        box("PupilL", (0.032, 0.012, 0.042), (-0.055, -0.138, 0.22), "Eyes"),
        box("PupilR", (0.032, 0.012, 0.042), (0.055, -0.138, 0.22), "Eyes"),
        box("BrowL", (0.07, 0.014, 0.018), (-0.062, -0.134, 0.268), "Brow", rotation=(0, -0.12, 0)),
        box("BrowR", (0.07, 0.014, 0.018), (0.062, -0.134, 0.268), "Brow", rotation=(0, 0.12, 0)),
        # A small smile: a flat middle and two corners turned up.
        box("Mouth", (0.05, 0.012, 0.016), (0, -0.133, 0.118), "Mouth"),
        box("MouthL", (0.022, 0.012, 0.014), (-0.033, -0.133, 0.125), "Mouth", rotation=(0, 0.6, 0)),
        box("MouthR", (0.022, 0.012, 0.014), (0.033, -0.133, 0.125), "Mouth", rotation=(0, -0.6, 0)),
    ]
    return join("CharHead", parts)


def char_hair_short():
    parts = [
        bevel_box("Top", (0.29, 0.28, 0.08), (0, 0.005, 0.345), "Hair", width=0.025),
        box("Back", (0.29, 0.06, 0.17), (0, 0.125, 0.26), "Hair"),
        box("SideL", (0.025, 0.2, 0.09), (-0.142, 0.03, 0.29), "Hair"),
        box("SideR", (0.025, 0.2, 0.09), (0.142, 0.03, 0.29), "Hair"),
        # A messy surfer fringe swept to one side.
        box("Fringe1", (0.13, 0.05, 0.05), (-0.06, -0.125, 0.31), "Hair", rotation=(0, 0.25, 0)),
        box("Fringe2", (0.1, 0.05, 0.04), (0.07, -0.125, 0.32), "Hair", rotation=(0, -0.15, 0)),
        box("Tuft", (0.07, 0.07, 0.05), (0.05, 0.02, 0.39), "Hair", rotation=(0.3, 0.2, 0)),
    ]
    return join("CharHairShort", parts)


def char_hair_long():
    parts = [
        bevel_box("Top", (0.3, 0.29, 0.09), (0, 0.01, 0.345), "Hair", width=0.025),
        box("Back", (0.3, 0.08, 0.42), (0, 0.14, 0.16), "Hair"),
        box("SideL", (0.05, 0.22, 0.26), (-0.15, 0.03, 0.2), "Hair"),
        box("SideR", (0.05, 0.22, 0.26), (0.15, 0.03, 0.2), "Hair"),
        box("Fringe", (0.24, 0.05, 0.05), (0, -0.125, 0.315), "Hair"),
        box("Tips", (0.26, 0.06, 0.06), (0, 0.15, -0.06), "Hair", rotation=(0.3, 0, 0)),
    ]
    return join("CharHairLong", parts)


def char_hair_bun():
    parts = [
        bevel_box("Top", (0.29, 0.28, 0.07), (0, 0.01, 0.34), "Hair", width=0.025),
        box("Back", (0.29, 0.06, 0.2), (0, 0.13, 0.25), "Hair"),
        box("SideL", (0.025, 0.2, 0.12), (-0.142, 0.03, 0.27), "Hair"),
        box("SideR", (0.025, 0.2, 0.12), (0.142, 0.03, 0.27), "Hair"),
        box("Fringe", (0.24, 0.04, 0.04), (0, -0.125, 0.315), "Hair"),
        ico("Bun", 0.085, (0, 0.06, 0.42), "Hair", subdivisions=1),
    ]
    return join("CharHairBun", parts)


def char_hair_curly():
    # A big round mop of curls: a cluster of chunky blobs over the top and back.
    parts = []
    spots = [(0, 0.0, 0.36), (-0.1, 0.02, 0.33), (0.1, 0.02, 0.33), (0, 0.11, 0.33), (-0.11, 0.1, 0.27),
             (0.11, 0.1, 0.27), (0, 0.13, 0.22), (-0.08, -0.08, 0.32), (0.08, -0.08, 0.32), (-0.14, 0.0, 0.25),
             (0.14, 0.0, 0.25)]
    for i, (x, y, z) in enumerate(spots):
        parts.append(ico(f"Curl{i}", 0.085, (x, y, z), "Hair", subdivisions=1))
    return join("CharHairCurly", parts)


def char_cap():
    # A baseball cap in the shirt color, short hair poking out at the back and sides.
    parts = [
        rings("Crown", [(0.29, 0.155), (0.35, 0.15), (0.4, 0.1)], "Shirt", sides=8, depth_scale=1.0, center=(0, 0.01)),
        box("Brim", (0.2, 0.13, 0.015), (0, -0.19, 0.3), "Shirt", rotation=(-0.12, 0, 0)),
        box("Button", (0.03, 0.03, 0.02), (0, 0.01, 0.41), "Shirt"),
        box("HairBack", (0.27, 0.05, 0.1), (0, 0.13, 0.25), "Hair"),
        box("HairL", (0.025, 0.14, 0.07), (-0.142, 0.04, 0.27), "Hair"),
        box("HairR", (0.025, 0.14, 0.07), (0.142, 0.04, 0.27), "Hair"),
    ]
    return join("CharCap", parts)


def char_hair_buzz():
    parts = [bevel_box("Top", (0.28, 0.27, 0.035), (0, 0.005, 0.345), "Hair", width=0.012),
             box("Back", (0.28, 0.02, 0.14), (0, 0.133, 0.27), "Hair"),
             box("SideL", (0.012, 0.22, 0.08), (-0.138, 0.02, 0.29), "Hair"),
             box("SideR", (0.012, 0.22, 0.08), (0.138, 0.02, 0.29), "Hair")]
    return join("CharHairBuzz", parts)


# ---------------------------------------------------------------- body
def char_torso():
    # Waist to shoulders: narrow waist, wider chest, rounded shoulders, a collar at the top.
    body = rings("Body", [(0.0, 0.17), (0.17, 0.175), (0.36, 0.215), (0.48, 0.21), (0.55, 0.13)], "Shirt", depth_scale=0.62)
    # Small rounded shoulders that fill the gap to the arms.
    caps = [ico("ShoulderL", 0.064, (-0.245, 0, 0.475), "Shirt", subdivisions=2),
            ico("ShoulderR", 0.064, (0.245, 0, 0.475), "Shirt", subdivisions=2)]
    return join("CharTorso", [body] + caps)


def char_shorts():
    # Boardshorts waist and seat; the legs of the shorts are on the thighs so they swing with them.
    waist = rings("Waist", [(-0.15, 0.2), (-0.02, 0.205), (0.07, 0.185)], "Shorts", depth_scale=0.64)
    band = rings("Band", [(0.04, 0.19), (0.075, 0.19)], "Shorts", depth_scale=0.66, cap_top=False, cap_bottom=False)
    return join("CharShorts", [waist, band])


def char_upper_arm():
    arm = rings("Arm", [(-UPPER_ARM, 0.05), (0.0, 0.058)], "Skin", sides=7)
    sleeve = rings("Sleeve", [(-0.11, 0.07), (0.02, 0.075)], "Shirt", sides=7)
    return join("CharUpperArm", [arm, sleeve])


def char_forearm():
    arm = rings("Forearm", [(-FOREARM, 0.042), (0.0, 0.05)], "Skin", sides=7)
    hand = bevel_box("Hand", (0.075, 0.045, 0.09), (0, 0, -FOREARM - 0.045), "Skin", width=0.012)
    thumb = box("Thumb", (0.025, 0.03, 0.045), (0.035, -0.025, -FOREARM - 0.025), "Skin", rotation=(0, 0.3, 0))
    return join("CharForearm", [arm, hand, thumb])


def char_thigh():
    leg = rings("Thigh", [(-THIGH, 0.068), (0.0, 0.085)], "Skin", sides=8)
    shorts_leg = rings("ShortsLeg", [(-0.25, 0.1), (0.04, 0.105)], "Shorts", sides=8)
    return join("CharThigh", [leg, shorts_leg])


def char_shin():
    shin = rings("Shin", [(-SHIN, 0.05), (-0.25, 0.062), (0.0, 0.066)], "Skin", sides=8)
    knee = ico("Knee", 0.068, (0, 0, 0), "Skin", subdivisions=1)
    foot = bevel_box("Foot", (0.095, 0.24, 0.065), (0, -0.055, -SHIN - 0.035), "Skin", width=0.015)
    return join("CharShin", [shin, knee, foot])


HAIR = [char_hair_short, char_hair_long, char_hair_bun, char_hair_curly, char_cap, char_hair_buzz]
PARTS = [char_head] + HAIR + [char_torso, char_shorts,
         char_upper_arm, char_forearm, char_thigh, char_shin]


def build_people():
    clear_scene()
    built = []
    for factory in PARTS:
        obj = factory()
        built.append((obj.name, export(obj)))
    return built


if globals().get("BUILD_PEOPLE", True):
    result = {"exported": build_people()}
