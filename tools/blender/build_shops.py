"""
Builds the three counter shops (Acai & Shake Shack, Surf Gear shack, Custom Board shaper's shed), their props,
and the parts of the visible "Air Wall" wave machine, then exports each as FBX into Assets/_Game/Models/.

Run in Blender (Blender MCP): exec(open(path).read(), ns). Borrows helpers/palette/export from build_models.py
(everything above its final build line), like build_boards.py does.

Conventions (same as build_models.py): models sit on z = 0, centered on x/y; the customer / pool side is Blender -Y.
A Blender point (x, y, z) lands in Unity at (-x, z, -y); WorldArt.Model adds a 180 degree yaw, so with yaw 0 the
-Y side faces Unity -Z, and with yaw -90 it faces Unity +X.

Materials named "Garment", "Paint" and "MachineBody" are tinted per object in Unity (keep the names).
Moving parts (pistons, valve wheels, fan rotors, the dial needle) are their own models so code can animate them;
each one's pivot is its center of rotation / rest position.
"""
import math
import bpy

_here = "/Users/ido/surf-pool-sim/tools/blender/build_models.py"
_src = open(_here).read()
exec(_src[:_src.rindex("result = {")], globals())
EXPORT_DIR = globals().get("EXPORT_DIR_OVERRIDE", EXPORT_DIR)

PALETTE.update({
    "Acai": (0.33, 0.10, 0.40),
    "AcaiLight": (0.62, 0.36, 0.72),
    "Banana": (1.00, 0.90, 0.48),
    "Strawberry": (0.90, 0.13, 0.22),
    "Blueberry": (0.20, 0.20, 0.55),
    "Granola": (0.80, 0.56, 0.28),
    "CoconutFlake": (0.98, 0.97, 0.93),
    "Honey": (0.98, 0.66, 0.12),
    "Mango": (1.00, 0.58, 0.12),
    "Shell": (0.48, 0.32, 0.20),
    "Paper": (0.83, 0.70, 0.52),
    "Foam": (0.98, 0.97, 0.90),
    "Garment": (0.95, 0.95, 0.95),   # tinted in Unity
    "Paint": (0.95, 0.95, 0.95),     # tinted in Unity
    "MachineBody": (0.20, 0.42, 0.62),  # tinted in Unity (orange with Heavy-Duty Pumps)
    "Duct": (0.30, 0.62, 0.86),
    "Orange": (0.98, 0.50, 0.12),
    "Grate": (0.36, 0.38, 0.40),
    "Dark": (0.04, 0.06, 0.08),
    "Cork": (0.72, 0.55, 0.36),
    "Chrome": (0.82, 0.85, 0.88),
})


def torus(name, major, minor, location, mat, rotation=(0, 0, 0), major_segments=12, minor_segments=6):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, location=location, rotation=rotation,
                                     major_segments=major_segments, minor_segments=minor_segments)
    obj = bpy.context.active_object
    obj.name = name
    return finish(obj, mat)


def striped_roof(parts, w, d, h, mat, stripes=9, tilt=-12, overhang=(0.8, 1.0)):
    roof_w, roof_d = w + overhang[0], d + overhang[1]
    for i in range(stripes):
        x = -roof_w / 2 + roof_w * (i + 0.5) / stripes
        parts.append(box("Roof", (roof_w / stripes, roof_d, 0.08), (x, -0.15, h + 0.35), mat if i % 2 == 0 else "White",
                         rotation=(math.radians(tilt), 0, 0)))
    for i in range(stripes):
        x = -roof_w / 2 + roof_w * (i + 0.5) / stripes
        parts.append(box("Valance", (roof_w / stripes, 0.04, 0.28), (x, -roof_d / 2 - 0.12, h + 0.02), mat if i % 2 == 0 else "White"))


# ================================================================ Acai & Shake Shack

def acai_shack(name="AcaiShack"):
    """4.4 x 3.0 m hut with a built-in 3 m front counter (x -2.05..0.95, top at 1.085 m, front edge at y = -1.25)
    and a back counter (top at 0.995 m). The server stands between them (y around 0) and walks in through the gap at
    the right end of the front counter. A giant acai bowl sits on the roof."""
    parts = []
    w, d, h = 4.4, 3.0, 2.6
    for x in (-w / 2 + 0.1, w / 2 - 0.1):
        for y in (-d / 2 + 0.1, d / 2 - 0.1):
            parts.append(box("Post", (0.16, 0.16, h), (x, y, h / 2), "Wood"))
    # Back wall (planks) and short side walls.
    parts.append(box("Back Wall", (w, 0.1, h), (0, d / 2 - 0.05, h / 2), "AcaiLight"))
    for i in range(6):
        parts.append(box("Plank Line", (0.03, 0.02, h - 0.2), (-w / 2 + 0.4 + i * 0.72, d / 2 - 0.11, h / 2), "Acai"))
    # Side walls: full on the left, only the back half on the right (that's the way in, behind the counter).
    parts.append(box("Side Wall", (0.08, d, 1.1), (-(w / 2 - 0.04), 0, 0.55), "White"))
    parts.append(box("Side Wall", (0.08, 1.3, 1.1), (w / 2 - 0.04, 0.85, 0.55), "White"))
    # Front counter (3 m, leaving a 1 m gap on the right to walk in): body, purple front with fruit dots, wood top.
    cx = -0.55
    parts.append(box("Counter Body", (3.0, 0.6, 1.0), (cx, -0.95, 0.5), "White"))
    parts.append(box("Counter Front", (3.02, 0.04, 0.8), (cx, -1.26, 0.55), "Acai"))
    for i, mat in enumerate(("Banana", "Strawberry", "Blueberry", "Mango", "Banana", "Strawberry")):
        parts.append(ico("Fruit Dot", 0.09, (cx - 1.25 + i * 0.5, -1.29, 0.62 + (i % 2) * 0.16), mat))
    parts.append(box("Counter Top", (3.2, 0.75, 0.07), (cx, -0.95, 1.05), "DarkWood"))
    # Back counter with a small fridge and shelves.
    parts.append(box("Back Counter", (w - 0.4, 0.55, 0.95), (0, 1.1, 0.475), "White"))
    parts.append(box("Back Top", (w - 0.3, 0.6, 0.05), (0, 1.1, 0.97), "DarkWood"))
    parts.append(box("Fridge", (0.8, 0.6, 1.7), (1.55, 1.05, 0.85), "Steel"))
    parts.append(box("Fridge Door", (0.7, 0.03, 1.5), (1.55, 0.74, 0.88), "Glass"))
    for z in (1.55, 1.95):
        parts.append(box("Shelf", (2.2, 0.25, 0.04), (-0.6, 1.3, z), "DarkWood"))
    for i in range(6):  # fruit jars on the shelves
        mat = ("Banana", "Strawberry", "Blueberry", "Mango", "Granola", "Honey")[i]
        parts.append(cylinder("Jar", 0.09, 0.22, (-1.5 + (i % 3) * 0.45 + (i // 3) * 0.2, 1.3, 1.68 + (i // 3) * 0.4), "Glass", vertices=8))
        parts.append(cylinder("Jar Fill", 0.075, 0.14, (-1.5 + (i % 3) * 0.45 + (i // 3) * 0.2, 1.3, 1.65 + (i // 3) * 0.4), mat, vertices=8))
    parts.append(box("Beam Front", (w, 0.16, 0.2), (0, -d / 2 + 0.1, h), "DarkWood"))
    parts.append(box("Beam Back", (w, 0.16, 0.2), (0, d / 2 - 0.1, h), "DarkWood"))
    striped_roof(parts, w, d, h, "Acai")
    parts.append(box("Sign", (2.4, 0.08, 0.55), (0, -d / 2 - 0.4, h + 1.15), "Navy"))
    for x in (-1.0, 1.0):
        parts.append(box("Sign Post", (0.06, 0.06, 0.9), (x, -d / 2 - 0.36, h + 0.6), "DarkWood"))
    # The giant bowl on the roof: coconut-shell bowl, purple top, fruit on top.
    bowl_z = h + 1.15
    parts.append(cylinder("Big Bowl", 0.75, 0.55, (0, 0.3, bowl_z), "Shell", vertices=12, radius_top=1.0))
    parts.append(cylinder("Big Acai", 0.95, 0.08, (0, 0.3, bowl_z + 0.29), "Acai", vertices=12))
    for i, mat in enumerate(("Banana", "Strawberry", "Blueberry", "Granola", "CoconutFlake")):
        a = i * math.tau / 5
        parts.append(ico("Big Topping", 0.2, (math.cos(a) * 0.5, 0.3 + math.sin(a) * 0.5, bowl_z + 0.38), mat))
    parts.append(cylinder("Spoon", 0.05, 1.0, (0.35, 0.45, bowl_z + 0.6), "Steel", vertices=6, rotation=(math.radians(25), math.radians(20), 0)))
    return join(name, parts)


def acai_bowl(name="AcaiBowl"):
    """Coconut-shell bowl, 0.24 m across, purple acai inside (top at 0.075 m)."""
    parts = [
        cylinder("Shell", 0.07, 0.07, (0, 0, 0.035), "Shell", vertices=10, radius_top=0.12),
        cylinder("Acai", 0.112, 0.012, (0, 0, 0.068), "Acai", vertices=10),
    ]
    return join(name, parts)


def shake_cup(name="ShakeCup"):
    """Tall clear-ish cup (0.2 m) filled with a purple shake, dome lid and a striped straw."""
    parts = [
        cylinder("Cup", 0.045, 0.18, (0, 0, 0.09), "White", vertices=10, radius_top=0.06),
        cylinder("Shake", 0.058, 0.04, (0, 0, 0.16), "AcaiLight", vertices=10),
        cylinder("Lid", 0.062, 0.03, (0, 0, 0.195), "White", vertices=10, radius_top=0.03),
        cylinder("Straw", 0.008, 0.16, (0.01, 0, 0.26), "Coral", vertices=6, rotation=(0, math.radians(8), 0)),
    ]
    return join(name, parts)


def topping(name, kind):
    """Small pile that sits on a bowl's acai (about 0.1 m across). Shown in the bins too, scaled up."""
    parts = []
    if kind == "banana":
        for i in range(4):
            a = i * 0.5
            parts.append(cylinder("Slice", 0.028, 0.012, (math.cos(a) * 0.03 - 0.02, math.sin(a) * 0.03, 0.008), "Banana", vertices=8,
                                  rotation=(math.radians(15), 0, a)))
    elif kind == "strawberry":
        for i in range(3):
            a = i * math.tau / 3
            parts.append(cylinder("Berry", 0.022, 0.035, (math.cos(a) * 0.03, math.sin(a) * 0.03, 0.015), "Strawberry", vertices=6,
                                  radius_top=0.004, rotation=(math.radians(70), 0, a)))
    elif kind == "blueberry":
        for i in range(6):
            a = i * math.tau / 6
            parts.append(ico("Blueberry", 0.013, (math.cos(a) * 0.035, math.sin(a) * 0.035, 0.012), "Blueberry"))
    elif kind == "granola":
        for i in range(9):
            a = i * 2.4
            r = 0.012 + (i % 3) * 0.015
            parts.append(box("Cluster", (0.016, 0.014, 0.012), (math.cos(a) * r, math.sin(a) * r, 0.008), "Granola", rotation=(a, a * 0.5, a)))
    elif kind == "coconut":
        for i in range(8):
            a = i * 2.1
            r = 0.01 + (i % 3) * 0.014
            parts.append(box("Flake", (0.02, 0.008, 0.003), (math.cos(a) * r, math.sin(a) * r, 0.006), "CoconutFlake", rotation=(0.3, 0, a)))
    elif kind == "honey":
        # A glossy puddle with a couple of drips on top.
        parts.append(cylinder("Puddle", 0.045, 0.008, (0, 0, 0.004), "Honey", vertices=10))
        for i in range(3):
            a = i * 2.1
            parts.append(ico("Drop", 0.012, (math.cos(a) * 0.025, math.sin(a) * 0.025, 0.012), "Honey"))
    elif kind == "mango":
        for i in range(5):
            a = i * 1.3
            parts.append(box("Cube", (0.02, 0.02, 0.02), (math.cos(a) * 0.03, math.sin(a) * 0.03, 0.01), "Mango", rotation=(0, 0, a)))
    return join(name, parts)


def topping_tub(name="ToppingTub"):
    """Steel tub set into the counter: 0.32 x 0.24 m, 0.12 m tall."""
    parts = [
        box("Tub", (0.32, 0.24, 0.1), (0, 0, 0.05), "Steel"),
        box("Rim", (0.34, 0.26, 0.02), (0, 0, 0.1), "Chrome"),
        box("Scoop", (0.04, 0.16, 0.015), (0.12, 0.02, 0.13), "Chrome", rotation=(math.radians(20), 0, 0)),
    ]
    return join(name, parts)


def blender_machine(name="BlenderMachine"):
    parts = [
        box("Base", (0.2, 0.2, 0.16), (0, 0, 0.08), "Navy"),
        box("Buttons", (0.12, 0.01, 0.04), (0, -0.105, 0.09), "KeyGrey"),
        cylinder("Jar", 0.08, 0.26, (0, 0, 0.29), "Glass", vertices=8, radius_top=0.1),
        cylinder("Fill", 0.075, 0.12, (0, 0, 0.23), "AcaiLight", vertices=8, radius_top=0.085),
        cylinder("Lid", 0.105, 0.04, (0, 0, 0.44), "Charcoal", vertices=8),
        box("Handle", (0.03, 0.04, 0.18), (0.11, 0, 0.3), "Charcoal"),
    ]
    return join(name, parts)


def stack(name, item_fn, count, step):
    parts = []
    for i in range(count):
        obj = item_fn()
        obj.location = (0, 0, i * step)
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
        parts.append(obj)
    return join(name, parts)


# ================================================================ Surf Gear shack

def gear_shack(name="GearShack"):
    """7 x 4.4 m open-front surf shop: plank back wall, half side walls, teal striped roof, boards on the walls.
    Floor stays at 0 so you can walk in."""
    parts = []
    w, d, h = 7.0, 4.4, 3.0
    parts.append(box("Floor", (w, d, 0.02), (0, 0, 0.01), "Wood"))
    for i in range(9):
        parts.append(box("Floor Line", (0.02, d, 0.022), (-w / 2 + 0.78 * (i + 0.5), 0, 0.011), "DarkWood"))
    parts.append(box("Back Wall", (w, 0.12, h), (0, d / 2 - 0.06, h / 2), "Wood"))
    for i in range(10):
        parts.append(box("Plank", (0.03, 0.02, h - 0.1), (-w / 2 + 0.35 + i * 0.7, d / 2 - 0.13, h / 2), "DarkWood"))
    for side in (-1, 1):
        parts.append(box("Side Wall", (0.12, d, h), (side * (w / 2 - 0.06), 0, h / 2), "White"))
        parts.append(box("Side Window", (0.14, 1.6, 0.9), (side * (w / 2 - 0.06), 0.2, 1.7), "Glass"))
        parts.append(box("Side Band", (0.14, d, 0.25), (side * (w / 2 - 0.06), 0, 0.7), "Teal"))
    for x in (-w / 2 + 0.12, w / 2 - 0.12):
        parts.append(box("Front Post", (0.18, 0.18, h), (x, -d / 2 + 0.09, h / 2), "DarkWood"))
    parts.append(box("Beam Front", (w, 0.2, 0.25), (0, -d / 2 + 0.1, h), "DarkWood"))
    striped_roof(parts, w, d, h, "Teal", stripes=13, tilt=-10)
    parts.append(box("Sign", (3.4, 0.1, 0.65), (0, -d / 2 - 0.35, h + 0.95), "Navy"))
    # Surfboards hung on the back wall (decor), noses up.
    for i, mat in enumerate(("Coral", "Yellow", "Teal", "White", "Coral")):
        x = -2.6 + i * 1.3
        parts.append(cylinder("Wall Board", 0.22, 1.9, (x, d / 2 - 0.24, 1.75), mat, vertices=10, rotation=(0, 0, 0)))
        parts.append(box("Board Stripe", (0.06, 0.02, 1.7), (x, d / 2 - 0.47, 1.75), "Navy"))
    for i in range(5):  # flatten the wall boards
        parts[-2 - i * 2].scale = (1, 0.18, 1)
    # Counter on the right front (the register sits on it in Unity), facing the doorway.
    parts.append(box("Counter", (1.8, 0.7, 1.0), (2.3, -1.1, 0.5), "White"))
    parts.append(box("Counter Band", (1.82, 0.72, 0.18), (2.3, -1.1, 0.75), "Coral"))
    parts.append(box("Counter Top", (1.95, 0.8, 0.06), (2.3, -1.1, 1.03), "DarkWood"))
    return join(name, parts)


def clothes_rack(name, garment):
    """1.4 m chrome rail with five hanging garments (boardshorts or rash guards). Garments use the tinted material."""
    parts = []
    for x in (-0.68, 0.68):
        parts.append(cylinder("Leg", 0.02, 1.45, (x, 0, 0.73), "Chrome", vertices=6))
        parts.append(box("Foot", (0.06, 0.45, 0.03), (x, 0, 0.015), "Chrome"))
    parts.append(cylinder("Rail", 0.018, 1.4, (0, 0, 1.45), "Chrome", vertices=6, rotation=(0, math.radians(90), 0)))
    for i in range(5):
        x = -0.5 + i * 0.25
        parts.append(box("Hanger", (0.02, 0.02, 0.06), (x, 0, 1.42), "Chrome"))
        if garment == "shorts":
            parts.append(box("Waist", (0.04, 0.34, 0.08), (x, 0, 1.34), "Garment"))
            for side in (-1, 1):
                parts.append(box("Leg", (0.035, 0.16, 0.42), (x, side * 0.085, 1.08), "Garment"))
            parts.append(box("Stripe", (0.045, 0.34, 0.03), (x, 0, 1.25), "White"))
        else:
            parts.append(box("Body", (0.035, 0.36, 0.55), (x, 0, 1.1), "Garment"))
            for side in (-1, 1):
                parts.append(box("Sleeve", (0.03, 0.12, 0.42), (x, side * 0.22, 1.16), "Garment", rotation=(math.radians(side * 12), 0, 0)))
            parts.append(box("Collar", (0.04, 0.12, 0.03), (x, 0, 1.38), "Navy"))
            parts.append(box("Panel", (0.04, 0.36, 0.08), (x, 0, 0.9), "Navy"))
    return join(name, parts)


def wax_crate(name="WaxCrate"):
    parts = [box("Crate", (0.6, 0.45, 0.35), (0, 0, 0.175), "Wood")]
    for z in (0.1, 0.25):
        parts.append(box("Slat", (0.62, 0.47, 0.03), (0, 0, z), "DarkWood"))
    colors = ("Coral", "Yellow", "Teal", "White", "Banana", "Coral", "Teal", "Yellow")
    for i, mat in enumerate(colors):
        parts.append(cylinder("Wax", 0.06, 0.05, (-0.21 + (i % 4) * 0.14, -0.08 + (i // 4) * 0.16, 0.38), mat, vertices=8))
    parts.append(box("Price Tag", (0.2, 0.02, 0.1), (0, -0.235, 0.26), "Yellow"))
    return join(name, parts)


def fin_wall(name="FinWall"):
    """1.1 m pegboard on legs with six fins."""
    parts = [box("Board", (1.1, 0.05, 0.9), (0, 0, 1.15), "Cork")]
    for x in (-0.5, 0.5):
        parts.append(box("Leg", (0.06, 0.06, 1.6), (x, 0.03, 0.8), "DarkWood"))
        parts.append(box("Foot", (0.08, 0.5, 0.04), (x, 0.03, 0.02), "DarkWood"))
    for i in range(6):
        mat = ("Coral", "Teal", "Navy", "Yellow", "Charcoal", "Coral")[i]
        x = -0.36 + (i % 3) * 0.36
        z = 1.3 - (i // 3) * 0.38
        verts = [(x - 0.09, -0.04, z - 0.12), (x + 0.09, -0.04, z - 0.12), (x + 0.05, -0.04, z + 0.12)]
        parts.append(mesh_object("Fin", verts, [(0, 1, 2)], mat, double_sided=True))
        parts.append(cylinder("Peg", 0.01, 0.06, (x, -0.04, z + 0.14), "Chrome", vertices=5, rotation=(math.radians(90), 0, 0)))
    return join(name, parts)


def leash_hooks(name="LeashHooks"):
    parts = [box("Panel", (0.8, 0.05, 0.7), (0, 0, 1.25), "White")]
    for x in (-0.35, 0.35):
        parts.append(box("Leg", (0.05, 0.05, 1.6), (x, 0.03, 0.8), "Steel"))
        parts.append(box("Foot", (0.07, 0.45, 0.04), (x, 0.03, 0.02), "Steel"))
    for i, mat in enumerate(("Charcoal", "Coral", "Teal", "Yellow")):
        x = -0.27 + i * 0.18
        parts.append(torus("Leash", 0.07, 0.012, (x, -0.06, 1.25), mat, rotation=(math.radians(90), 0, 0)))
        parts.append(torus("Leash", 0.055, 0.012, (x, -0.07, 1.22), mat, rotation=(math.radians(90), 0, 0)))
        parts.append(box("Cuff", (0.05, 0.03, 0.08), (x, -0.08, 1.12), "Charcoal"))
    return join(name, parts)


def sunscreen_stand(name="SunscreenStand"):
    parts = []
    for i, z in enumerate((0.45, 0.75, 1.05)):
        parts.append(box("Step", (0.7, 0.3 - i * 0.05, 0.04), (0, i * 0.04, z), "White"))
    for x in (-0.33, 0.33):
        parts.append(box("Side", (0.04, 0.32, 1.1), (x, 0.04, 0.55), "Teal"))
    for i in range(12):
        z = (0.45, 0.75, 1.05)[i // 4] + 0.09
        mat = ("Orange", "Yellow", "Coral", "White")[i % 4]
        parts.append(cylinder("Bottle", 0.035, 0.14, (-0.21 + (i % 4) * 0.14, (i // 4) * 0.04, z), mat, vertices=8))
        parts.append(cylinder("Cap", 0.02, 0.03, (-0.21 + (i % 4) * 0.14, (i // 4) * 0.04, z + 0.085), "Navy", vertices=6))
    parts.append(box("Sun Sign", (0.4, 0.03, 0.2), (0, 0.18, 1.35), "Yellow"))
    return join(name, parts)


def shop_bag(name="ShopBag"):
    """Paper bag (0.3 m tall) with rope handles; held at the handles (pivot at the handle top)."""
    parts = [
        box("Bag", (0.26, 0.12, 0.3), (0, 0, -0.27), "Paper"),
        box("Band", (0.265, 0.125, 0.06), (0, 0, -0.2), "Teal"),
        box("Logo", (0.08, 0.005, 0.08), (0, -0.063, -0.3), "Coral"),
        torus("Handle", 0.06, 0.008, (0, 0, -0.1), "Navy", rotation=(math.radians(90), 0, 0), major_segments=8),
    ]
    return join(name, parts)


def shop_basket(name="ShopBasket"):
    """Wire basket held in front of the camera while carrying gear."""
    parts = [box("Bottom", (0.36, 0.24, 0.01), (0, 0, 0.005), "Coral")]
    for z in (0.05, 0.1, 0.15):
        parts.append(box("Wire", (0.37, 0.01, 0.01), (0, -0.12, z), "Coral"))
        parts.append(box("Wire", (0.37, 0.01, 0.01), (0, 0.12, z), "Coral"))
        parts.append(box("Wire", (0.01, 0.25, 0.01), (-0.18, 0, z), "Coral"))
        parts.append(box("Wire", (0.01, 0.25, 0.01), (0.18, 0, z), "Coral"))
    for x in (-0.18, 0.18):
        for y in (-0.12, 0.12):
            parts.append(box("Corner", (0.012, 0.012, 0.16), (x, y, 0.08), "Coral"))
    parts.append(box("Handle", (0.02, 0.24, 0.02), (0, 0, 0.26), "Navy"))
    for y in (-0.11, 0.11):
        parts.append(box("Handle Leg", (0.015, 0.015, 0.11), (0, y, 0.2), "Navy"))
    return join(name, parts)


# ================================================================ Custom board shaper's shed

def shaper_shed(name="ShaperShed"):
    """8 x 5 m workshop with a wide open front. Plank walls, a board rack on the left wall, templates on the
    back wall, sawdust patches, a tin roof and a big sign board."""
    parts = []
    w, d, h = 8.0, 5.0, 3.2
    parts.append(box("Floor", (w, d, 0.02), (0, 0, 0.01), "Concrete"))
    for i, (x, y, s) in enumerate(((-1.0, 0.4, 1.2), (1.5, -0.3, 0.9), (-2.4, -0.8, 0.7), (2.6, 1.2, 0.8))):
        parts.append(cylinder("Dust", s, 0.024, (x, y, 0.012), "Foam", vertices=9))
    parts.append(box("Back Wall", (w, 0.14, h), (0, d / 2 - 0.07, h / 2), "Wood"))
    for side in (-1, 1):
        parts.append(box("Side Wall", (0.14, d, h), (side * (w / 2 - 0.07), 0, h / 2), "Wood"))
    for i in range(11):
        parts.append(box("Plank", (0.03, 0.02, h - 0.1), (-w / 2 + 0.4 + i * 0.72, d / 2 - 0.15, h / 2), "DarkWood"))
    for x in (-w / 2 + 0.12, w / 2 - 0.12):
        parts.append(box("Front Post", (0.22, 0.22, h), (x, -d / 2 + 0.11, h / 2), "DarkWood"))
    parts.append(box("Header", (w, 0.24, 0.5), (0, -d / 2 + 0.12, h - 0.2), "DarkWood"))
    parts.append(box("Door Roll", (w - 0.6, 0.35, 0.3), (0, -d / 2 + 0.2, h - 0.6), "Steel"))
    # Corrugated tin roof.
    for i in range(14):
        x = -w / 2 - 0.3 + (w + 0.6) * (i + 0.5) / 14
        parts.append(box("Roof Rib", ((w + 0.6) / 14 * 0.9, d + 0.8, 0.07), (x, 0, h + 0.15 + (0.04 if i % 2 else 0)), "Steel",
                         rotation=(math.radians(-5), 0, 0)))
    parts.append(box("Sign", (4.2, 0.12, 0.8), (0, -d / 2 - 0.2, h + 0.75), "Charcoal"))
    parts.append(box("Sign Trim", (4.4, 0.1, 0.1), (0, -d / 2 - 0.18, h + 0.3), "Yellow"))
    # Board templates on the back wall (white outlines).
    for i, (x, l) in enumerate(((-2.6, 1.8), (-1.6, 2.1), (2.0, 2.6))):
        tmpl = cylinder("Template", 0.24, l, (x, d / 2 - 0.2, 1.6), "Foam", vertices=12)
        tmpl.scale = (1, 0.12, 1)
        parts.append(tmpl)
    # Wall rack with finished boards on the left wall.
    for i, mat in enumerate(("Coral", "Teal", "Yellow", "White")):
        brd = cylinder("Rack Board", 0.24, 1.9, (-w / 2 + 0.35, -1.4 + i * 0.6, 1.4), mat, vertices=10)
        brd.scale = (0.2, 1, 1)
        parts.append(brd)
    parts.append(box("Rack Arm", (0.4, 2.6, 0.05), (-w / 2 + 0.3, -0.5, 0.45), "DarkWood"))
    # Workbench along the right wall with tools.
    parts.append(box("Bench", (0.8, 2.4, 0.9), (w / 2 - 0.55, 0.8, 0.45), "DarkWood"))
    parts.append(box("Bench Top", (0.9, 2.5, 0.06), (w / 2 - 0.55, 0.8, 0.93), "Wood"))
    parts.append(box("Sander", (0.25, 0.25, 0.12), (w / 2 - 0.55, 0.3, 1.02), "Coral"))
    parts.append(box("Planer", (0.35, 0.16, 0.14), (w / 2 - 0.55, 1.2, 1.03), "Yellow"))
    parts.append(box("Lamp", (2.4, 0.15, 0.06), (0, 0, h - 0.15), "Bulb"))
    return join(name, parts)


def shaping_stand(name="ShapingStand"):
    """Two padded stands 1.1 m apart along X: a board resting on them sits at z = 0.95 (lengthwise along X)."""
    parts = []
    for x in (-0.55, 0.55):
        for y in (-0.18, 0.18):
            parts.append(box("Leg", (0.06, 0.06, 0.9), (x, y, 0.45), "DarkWood", rotation=(math.radians(-10 if y < 0 else 10), 0, 0)))
        parts.append(box("Pad", (0.14, 0.46, 0.07), (x, 0, 0.9), "Charcoal"))
        parts.append(box("Brace", (0.05, 0.3, 0.05), (x, 0, 0.35), "Wood"))
    return join(name, parts)


def foam_blank(name="FoamBlank"):
    """Rough foam blank, 2.2 x 0.6 x 0.12 m, lengthwise along X, sitting on z = 0."""
    parts = [box("Blank", (2.2, 0.58, 0.12), (0, 0, 0.06), "Foam")]
    for x in (-1.12, 1.12):
        parts.append(box("End", (0.04, 0.4, 0.1), (x, 0, 0.05), "Foam"))
    parts.append(box("Stringer", (2.24, 0.012, 0.125), (0, 0, 0.062), "Stringer"))
    return join(name, parts)


def spray_booth(name="SprayBooth"):
    """2.6 x 1.6 m booth with a back panel, side curtains and an extractor box; the board hangs at z = 1.1."""
    parts = []
    w, d, h = 2.6, 1.6, 2.3
    for x in (-w / 2, w / 2):
        for y in (-d / 2, d / 2):
            parts.append(box("Frame", (0.06, 0.06, h), (x, y, h / 2), "Steel"))
    for y in (-d / 2, d / 2):
        parts.append(box("Top Rail", (w, 0.06, 0.06), (0, y, h), "Steel"))
    for x in (-w / 2, w / 2):
        parts.append(box("Top Rail", (0.06, d, 0.06), (x, 0, h), "Steel"))
    parts.append(box("Back", (w, 0.04, h - 0.1), (0, d / 2, h / 2), "White"))
    for x in (-w / 2, w / 2):
        parts.append(box("Curtain", (0.02, d * 0.9, h - 0.3), (x, 0, h / 2 + 0.1), "Glass"))
    parts.append(box("Extractor", (0.8, 0.5, 0.4), (0, d / 2 - 0.1, h + 0.2), "Steel"))
    parts.append(cylinder("Fan", 0.18, 0.05, (0, d / 2 - 0.36, h + 0.2), "Charcoal", vertices=10, rotation=(math.radians(90), 0, 0)))
    for x in (-0.7, 0.7):  # hooks the board hangs from
        parts.append(cylinder("Hook Wire", 0.008, 1.0, (x, 0, h - 0.5), "Chrome", vertices=4))
    # Paint splats on the back panel.
    for i, mat in enumerate(("Coral", "Teal", "Yellow", "AcaiLight", "Navy")):
        splat = cylinder("Splat", 0.12 + 0.04 * (i % 2), 0.01, (-0.9 + i * 0.45, d / 2 - 0.03, 0.6 + (i % 3) * 0.5), mat, vertices=7,
                         rotation=(math.radians(90), 0, 0))
        parts.append(splat)
    return join(name, parts)


def spray_can(name="SprayCan"):
    parts = [
        cylinder("Can", 0.04, 0.2, (0, 0, 0.1), "Paint", vertices=10),
        cylinder("Shoulder", 0.04, 0.03, (0, 0, 0.215), "Chrome", vertices=10, radius_top=0.025),
        cylinder("Cap", 0.022, 0.03, (0, 0, 0.24), "Paint", vertices=8),
        box("Label", (0.082, 0.082, 0.06), (0, 0, 0.1), "White"),
    ]
    parts[3].scale = (1, 1, 1)
    return join(name, parts)


def fin_box(name="FinBox"):
    """Small wooden box with fins sticking out the top."""
    parts = [box("Box", (0.4, 0.3, 0.2), (0, 0, 0.1), "Wood"), box("Rim", (0.42, 0.32, 0.03), (0, 0, 0.2), "DarkWood")]
    for i in range(4):
        x = -0.12 + i * 0.08
        verts = [(x - 0.04, 0.0, 0.2), (x + 0.04, 0.0, 0.2), (x + 0.02, 0.0, 0.34)]
        parts.append(mesh_object("Fin", verts, [(0, 1, 2)], "Paint", double_sided=True))
    return join(name, parts)


def wish_board(name="WishBoard"):
    """Cork board on a post for pinned order cards."""
    parts = [box("Cork", (1.2, 0.05, 0.8), (0, 0, 1.5), "Cork"), box("Frame", (1.28, 0.04, 0.88), (0, 0.02, 1.5), "DarkWood"),
             box("Post", (0.08, 0.08, 1.2), (0, 0.04, 0.6), "DarkWood")]
    for i in range(4):
        parts.append(box("Card", (0.22, 0.01, 0.16), (-0.4 + i * 0.27, -0.03, 1.55 + (i % 2) * 0.08), "White",
                         rotation=(0, math.radians(-6 + i * 4), 0)))
        parts.append(ico("Pin", 0.015, (-0.4 + i * 0.27, -0.04, 1.62 + (i % 2) * 0.08), "Red"))
    return join(name, parts)


# ================================================================ Air Wall wave machine

def machine_chamber(name="MachineChamber"):
    """One 3 m air-chamber module. Pool side = -Y, the module spans X -1.5..1.5 and Y -3..3.
    Base block 1.6 m tall (catwalk on top, front half), a 2.6 m tall air chamber behind, and a duct to the back.
    The piston cap sits on the chamber top at (0, 0.6, 4.35); the valve wheel at (0.75, 2.2, 3.4)."""
    parts = []
    parts.append(box("Base", (3.0, 6.0, 1.6), (0, 0, 0.8), "Concrete"))
    parts.append(box("Front Plate", (2.9, 0.06, 1.2), (0, -3.02, 0.8), "MachineBody"))
    parts.append(box("Mouth", (2.4, 0.08, 0.35), (0, -3.05, 0.3), "Dark"))
    for i in range(6):
        parts.append(box("Mouth Bar", (0.06, 0.1, 0.36), (-1.0 + i * 0.4, -3.07, 0.3), "Steel"))
    parts.append(box("Number Plate", (0.6, 0.04, 0.4), (0, -3.06, 1.15), "White"))
    # Catwalk grating + railing on the front half.
    parts.append(box("Catwalk", (3.0, 2.2, 0.06), (0, -1.9, 1.63), "Grate"))
    for i in range(7):
        parts.append(box("Grate Line", (0.02, 2.2, 0.065), (-1.35 + i * 0.45, -1.9, 1.635), "Charcoal"))
    for x in (-1.45, 0.0, 1.45):
        parts.append(box("Rail Post", (0.06, 0.06, 1.0), (x, -2.95, 2.13), "Yellow"))
    parts.append(box("Rail", (3.0, 0.06, 0.06), (0, -2.95, 2.6), "Yellow"))
    parts.append(box("Mid Rail", (3.0, 0.04, 0.04), (0, -2.95, 2.15), "Yellow"))
    # The air chamber: rounded steel tank with colored bands and a domed top.
    parts.append(cylinder("Chamber", 1.15, 2.6, (0, 0.6, 2.9), "MachineBody", vertices=12))
    parts.append(cylinder("Dome", 1.15, 0.5, (0, 0.6, 4.45), "MachineBody", vertices=12, radius_top=0.55))
    for z in (2.0, 3.0, 3.9):
        parts.append(cylinder("Band", 1.2, 0.12, (0, 0.6, z), "Steel", vertices=12))
    for a in range(8):  # bolts on the top band
        ang = a * math.tau / 8
        parts.append(box("Bolt", (0.08, 0.08, 0.08), (math.cos(ang) * 1.2, 0.6 + math.sin(ang) * 1.2, 3.9), "Charcoal"))
    parts.append(cylinder("Gland", 0.25, 0.25, (0, 0.6, 4.75), "Steel", vertices=8))
    # Duct out the back toward the blowers, with a flange and a valve body.
    parts.append(cylinder("Duct", 0.38, 2.0, (0, 2.2, 3.4), "Duct", vertices=10, rotation=(math.radians(90), 0, 0)))
    parts.append(cylinder("Flange", 0.48, 0.1, (0, 1.75, 3.4), "Steel", vertices=10, rotation=(math.radians(90), 0, 0)))
    parts.append(cylinder("Flange", 0.48, 0.1, (0, 2.95, 3.4), "Steel", vertices=10, rotation=(math.radians(90), 0, 0)))
    parts.append(box("Valve Body", (0.55, 0.5, 0.55), (0.35, 2.2, 3.4), "Charcoal"))
    parts.append(cylinder("Valve Stem", 0.04, 0.35, (0.65, 2.2, 3.4), "Steel", vertices=6, rotation=(0, math.radians(90), 0)))
    # Little warning stripes on the chamber's front skirt.
    for i in range(5):
        parts.append(box("Hazard", (0.12, 0.02, 0.4), (-0.8 + i * 0.4, -0.56, 1.85), "Yellow", rotation=(0, math.radians(30), 0)))
    return join(name, parts)


def chamber_piston(name="ChamberPiston"):
    """Piston that pumps out of the chamber top: rod + coral cap. Pivot at the rod's base (rest position)."""
    parts = [
        cylinder("Rod", 0.13, 1.2, (0, 0, 0.6), "Chrome", vertices=8),
        cylinder("Cap", 0.4, 0.22, (0, 0, 1.25), "Coral", vertices=10),
        cylinder("Cap Top", 0.3, 0.08, (0, 0, 1.4), "White", vertices=10),
    ]
    return join(name, parts)


def valve_wheel(name="ValveWheel"):
    """Red hand wheel, 0.6 m across, facing +X (axis along X). Pivot at its hub."""
    parts = [torus("Rim", 0.28, 0.035, (0, 0, 0), "Red", rotation=(0, math.radians(90), 0), major_segments=12)]
    for i in range(4):
        a = i * math.pi / 4
        parts.append(box("Spoke", (0.03, 0.56, 0.03), (0, 0, 0), "Red", rotation=(a, 0, 0)))
    parts.append(cylinder("Hub", 0.06, 0.08, (0, 0, 0), "Charcoal", vertices=8, rotation=(0, math.radians(90), 0)))
    return join(name, parts)


def blower_unit(name="BlowerUnit"):
    """Centrifugal blower: round housing (axis along Y, intake grille facing -Y), motor behind, outlet duct going up.
    2.8 m across, 3.2 m tall overall. The fan rotor sits at (0, -0.3, 1.6)."""
    parts = [
        box("Skid", (3.0, 2.4, 0.2), (0, 0.3, 0.1), "Charcoal"),
        cylinder("Housing", 1.4, 0.9, (0, 0, 1.6), "MachineBody", vertices=16, rotation=(math.radians(90), 0, 0)),
        cylinder("Back", 1.3, 0.2, (0, 0.5, 1.6), "MachineBody", vertices=16, rotation=(math.radians(90), 0, 0)),
        torus("Intake Ring", 1.25, 0.08, (0, -0.47, 1.6), "Steel", rotation=(math.radians(90), 0, 0), major_segments=16),
        cylinder("Motor", 0.55, 1.0, (0, 1.1, 1.3), "Steel", vertices=10, rotation=(math.radians(90), 0, 0)),
        box("Motor Box", (0.5, 0.4, 0.3), (0, 1.1, 1.95), "Charcoal"),
        box("Outlet", (0.9, 0.9, 1.5), (1.0, 0.0, 2.6), "MachineBody"),
        box("Outlet Cap", (1.0, 1.0, 0.12), (1.0, 0.0, 3.35), "Steel"),
        box("Leg", (0.2, 0.9, 0.8), (-0.9, 0.0, 0.5), "Charcoal"),
        box("Leg", (0.2, 0.9, 0.8), (0.9, 0.0, 0.5), "Charcoal"),
        box("Plate", (0.5, 0.02, 0.3), (-0.75, -0.46, 0.75), "White"),
    ]
    for i in range(6):  # grille bars across the intake
        parts.append(box("Grille", (2.4, 0.03, 0.04), (0, -0.5, 0.7 + i * 0.36), "Steel"))
    return join(name, parts)


def blower_fan(name="BlowerFan"):
    """Fan rotor, 2.2 m across, spinning around Y. Pivot at its hub."""
    parts = [cylinder("Hub", 0.25, 0.25, (0, 0, 0), "Chrome", vertices=10, rotation=(math.radians(90), 0, 0))]
    for i in range(7):
        a = i * math.tau / 7
        blade = box("Blade", (0.32, 0.05, 1.0), (math.sin(a) * 0.6, 0, math.cos(a) * 0.6), "White", rotation=(0, a, 0))
        blade.rotation_euler = (0, a, math.radians(0))
        bpy.ops.object.select_all(action="DESELECT")
        parts.append(blade)
    for p in parts[1:]:
        p.rotation_euler[0] += math.radians(18)  # pitch each blade a bit
    return join(name, parts)


def pressure_dial(name="PressureDial"):
    """Big round gauge on a post, face toward -Y, center at (0, -0.12, 2.6), 1.6 m across.
    Green / yellow / red arc segments; the needle is its own model."""
    parts = [
        box("Post", (0.2, 0.2, 2.6), (0, 0.1, 1.3), "Steel"),
        box("Foot", (0.6, 0.6, 0.1), (0, 0.1, 0.05), "Charcoal"),
        cylinder("Rim", 0.85, 0.16, (0, 0, 2.6), "Navy", vertices=20, rotation=(math.radians(90), 0, 0)),
        cylinder("Face", 0.75, 0.04, (0, -0.08, 2.6), "White", vertices=20, rotation=(math.radians(90), 0, 0)),
    ]
    # Arc from -120 deg (empty) to +120 deg (full): 9 ticks colored green, yellow, red.
    for i in range(13):
        a = math.radians(-120 + i * 20)
        mat = "Red" if i < 3 else "Yellow" if i < 7 else "Green"
        parts.append(box("Tick", (0.06, 0.02, 0.2), (math.sin(a) * 0.6, -0.11, 2.6 + math.cos(a) * 0.6), mat, rotation=(0, a, 0)))
    parts.append(cylinder("Hub", 0.08, 0.06, (0, -0.12, 2.6), "Charcoal", vertices=8, rotation=(math.radians(90), 0, 0)))
    return join(name, parts)


def dial_needle(name="DialNeedle"):
    """Needle pointing up (+Z) from its pivot; rotates around Y."""
    parts = [box("Needle", (0.05, 0.02, 0.62), (0, 0, 0.27), "Red"), box("Tail", (0.07, 0.02, 0.16), (0, 0, -0.06), "Red")]
    return join(name, parts)


def control_cabin(name="ControlCabin"):
    """3.4 x 2.6 m glass operator cabin on legs (floor at 0.5 m), screens inside, antenna mast. Front (-Y) = glass."""
    parts = []
    w, d = 3.4, 2.6
    parts.append(box("Floor", (w, d, 0.3), (0, 0, 0.55), "Charcoal"))
    for x in (-w / 2 + 0.2, w / 2 - 0.2):
        for y in (-d / 2 + 0.2, d / 2 - 0.2):
            parts.append(box("Leg", (0.15, 0.15, 0.45), (x, y, 0.22), "Steel"))
    for x in (-w / 2, w / 2):
        for y in (-d / 2, d / 2):
            parts.append(box("Post", (0.1, 0.1, 2.3), (x, y, 1.85), "White"))
    parts.append(box("Glass Front", (w, 0.04, 1.3), (0, -d / 2, 2.15), "Glass"))
    parts.append(box("Wall Front", (w, 0.06, 0.8), (0, -d / 2, 1.1), "White"))
    for x in (-w / 2, w / 2):
        parts.append(box("Glass Side", (0.04, d, 1.3), (x, 0, 2.15), "Glass"))
        parts.append(box("Wall Side", (0.06, d, 0.8), (x, 0, 1.1), "White"))
    parts.append(box("Back Wall", (w, 0.08, 2.3), (0, d / 2, 1.85), "White"))
    parts.append(box("Roof", (w + 0.4, d + 0.4, 0.2), (0, 0, 3.1), "Navy"))
    parts.append(box("Roof Band", (w + 0.42, d + 0.42, 0.1), (0, 0, 2.97), "Teal"))
    parts.append(box("Desk", (w - 0.4, 0.6, 0.12), (0, -d / 2 + 0.45, 1.5), "Charcoal"))
    for i in range(3):
        parts.append(box("Screen", (0.8, 0.05, 0.5), (-1.0 + i * 1.0, -d / 2 + 0.6, 1.95), "Screen", rotation=(math.radians(-10), 0, 0)))
    parts.append(box("Steps", (0.8, 0.6, 0.25), (w / 2 - 0.6, -d / 2 - 0.35, 0.12), "Grate"))
    parts.append(cylinder("Mast", 0.04, 1.6, (w / 2 - 0.3, d / 2 - 0.3, 4.0), "Steel", vertices=6))
    parts.append(cylinder("Dish", 0.45, 0.15, (w / 2 - 0.3, d / 2 - 0.3, 4.75), "White", vertices=12, radius_top=0.1,
                          rotation=(math.radians(-60), 0, math.radians(30))))
    return join(name, parts)


def air_tank(name="AirTank"):
    """Horizontal pressure tank (3.4 m long along X, 1.3 m across) on two saddles, white with a teal band."""
    parts = [
        cylinder("Tank", 0.65, 2.8, (0, 0, 1.05), "White", vertices=14, rotation=(0, math.radians(90), 0)),
        cylinder("Band", 0.68, 0.25, (0, 0, 1.05), "Teal", vertices=14, rotation=(0, math.radians(90), 0)),
    ]
    for x in (-1.4, 1.4):
        cap = cylinder("Cap", 0.65, 0.3, (x + (0.15 if x > 0 else -0.15), 0, 1.05), "White", vertices=14,
                       rotation=(0, math.radians(90 if x > 0 else -90), 0), radius_top=0.35)
        parts.append(cap)
    for x in (-0.9, 0.9):
        parts.append(box("Saddle", (0.25, 1.0, 0.5), (x, 0, 0.25), "Charcoal"))
    parts.append(cylinder("Gauge", 0.12, 0.06, (0.6, -0.68, 1.3), "Yellow", vertices=10, rotation=(math.radians(90), 0, 0)))
    parts.append(cylinder("Pipe", 0.1, 0.9, (0, 0, 1.9), "Duct", vertices=8))
    return join(name, parts)


def timer_clock(name="TimerClock"):
    """Post with a round clock box and an AUTO lamp housing on top; face toward -Y, clock center at z = 2.3."""
    parts = [
        box("Post", (0.14, 0.14, 2.4), (0, 0.08, 1.2), "Steel"),
        box("Foot", (0.5, 0.5, 0.08), (0, 0.08, 0.04), "Charcoal"),
        cylinder("Case", 0.45, 0.2, (0, 0, 2.3), "Yellow", vertices=16, rotation=(math.radians(90), 0, 0)),
        cylinder("Face", 0.38, 0.03, (0, -0.1, 2.3), "White", vertices=16, rotation=(math.radians(90), 0, 0)),
        box("Hand", (0.04, 0.02, 0.26), (0.05, -0.12, 2.38), "Navy", rotation=(0, math.radians(30), 0)),
        box("Hand", (0.04, 0.02, 0.18), (-0.06, -0.12, 2.27), "Navy", rotation=(0, math.radians(-120), 0)),
        box("Lamp Box", (0.3, 0.3, 0.12), (0, 0, 2.82), "Charcoal"),
    ]
    for i in range(12):
        a = i * math.tau / 12
        parts.append(box("Tick", (0.03, 0.02, 0.07), (math.sin(a) * 0.31, -0.115, 2.3 + math.cos(a) * 0.31), "Navy", rotation=(0, a, 0)))
    return join(name, parts)


def famous_board(name="FamousBoard"):
    """4 x 2 m poster board on two legs with pennant flags along the top. Face toward -Y, board center z = 2.6."""
    parts = [box("Board", (4.0, 0.1, 2.0), (0, 0, 2.6), "Navy"), box("Frame", (4.2, 0.08, 2.2), (0, 0.05, 2.6), "Yellow")]
    for x in (-1.7, 1.7):
        parts.append(box("Leg", (0.15, 0.15, 3.6), (x, 0.12, 1.8), "DarkWood"))
    parts.append(cylinder("Pennant Line", 0.01, 4.2, (0, -0.06, 3.85), "Charcoal", vertices=4, rotation=(0, math.radians(90), 0)))
    for i in range(9):
        x = -1.9 + i * 0.475
        mat = ("Coral", "Yellow", "Teal", "White", "Red", "Green", "Coral", "Yellow", "Teal")[i]
        verts = [(x - 0.17, -0.06, 3.85), (x + 0.17, -0.06, 3.85), (x, -0.06, 3.5)]
        parts.append(mesh_object("Pennant", verts, [(0, 1, 2)], mat, double_sided=True))
    # Five "spot" panels on the board (photos in Unity get text over them).
    for i in range(5):
        parts.append(box("Spot", (0.62, 0.02, 0.5), (-1.5 + i * 0.75, -0.06, 2.75), ("Teal", "Glass", "Coral", "Yellow", "Red")[i]))
    return join(name, parts)


def reef_switch(name="ReefSwitch"):
    """Lever box with LEFT / RIGHT arrows on a stand, face toward -Y, panel center at z = 1.5."""
    parts = [
        box("Stand", (0.16, 0.16, 1.2), (0, 0.1, 0.6), "Steel"),
        box("Panel", (1.4, 0.15, 0.7), (0, 0, 1.5), "Navy"),
        box("Lever Slot", (0.5, 0.05, 0.08), (0, -0.09, 1.3), "Charcoal"),
        box("Lever", (0.06, 0.06, 0.45), (0.12, -0.15, 1.45), "Steel", rotation=(0, math.radians(25), 0)),
        ico("Knob", 0.07, (0.22, -0.15, 1.64), "Red"),
    ]
    for side in (-1, 1):
        x = side * 0.45
        verts = [(x + side * 0.2, -0.08, 1.6), (x - side * 0.05, -0.08, 1.78), (x - side * 0.05, -0.08, 1.42)]
        parts.append(mesh_object("Arrow", verts, [(0, 1, 2) if side > 0 else (0, 2, 1)], "Yellow"))
        parts.append(box("Arrow Tail", (0.18, 0.02, 0.1), (x - side * 0.14, -0.08, 1.6), "Yellow"))
    return join(name, parts)


def status_lamp(name="StatusLamp"):
    """Short post with a lamp cage; the glowing bulb is a separate sphere in Unity at (0, 0.62, 0)."""
    parts = [
        cylinder("Post", 0.04, 0.5, (0, 0, 0.25), "Steel", vertices=6),
        cylinder("Base", 0.1, 0.06, (0, 0, 0.53), "Charcoal", vertices=8),
        cylinder("Cap", 0.11, 0.06, (0, 0, 0.75), "Charcoal", vertices=8),
    ]
    for i in range(4):
        a = i * math.tau / 4
        parts.append(box("Cage", (0.015, 0.015, 0.2), (math.cos(a) * 0.1, math.sin(a) * 0.1, 0.64), "Charcoal"))
    return join(name, parts)


# ================================================================ Surf photographer

def photo_tower(name="PhotoTower"):
    """A 3 m wooden photo tower: four legs, cross braces, a platform (top at z 3.0, 2.2 x 2.2) with a railing open on
    the front (-Y), a ladder at the back and a striped sun umbrella. The camera sits at the front edge."""
    parts = []
    for x in (-0.95, 0.95):
        for y in (-0.95, 0.95):
            parts.append(box("Leg", (0.16, 0.16, 3.0), (x * 1.0, y * 1.0, 1.5), "Wood", rotation=(0, 0, 0)))
    for z in (0.9, 2.0):
        for y in (-0.95, 0.95):
            parts.append(box("Brace", (2.0, 0.08, 0.1), (0, y, z), "DarkWood"))
        for x in (-0.95, 0.95):
            parts.append(box("Brace", (0.08, 2.0, 0.1), (x, 0, z), "DarkWood"))
    parts.append(box("Platform", (2.3, 2.3, 0.14), (0, 0, 3.0), "Wood"))
    for i in range(9):
        parts.append(box("Plank", (0.02, 2.3, 0.142), (-1.1 + i * 0.275, 0, 3.0), "DarkWood"))
    # Railing on three sides (the front stays open for the lens).
    for x in (-1.12, 1.12):
        parts.append(box("Rail", (0.06, 2.24, 0.06), (x, 0, 3.95), "White"))
        parts.append(box("Rail Low", (0.06, 2.24, 0.05), (x, 0, 3.5), "White"))
    parts.append(box("Rail Back", (2.24, 0.06, 0.06), (0, 1.12, 3.95), "White"))
    for x in (-1.12, 1.12):
        for y in (-1.12, 1.12):
            parts.append(box("Rail Post", (0.07, 0.07, 1.0), (x, y, 3.55), "White"))
    # Ladder at the back.
    for x in (-0.25, 0.25):
        parts.append(box("Ladder Side", (0.06, 0.06, 3.2), (x, 1.32, 1.55), "DarkWood", rotation=(math.radians(-8), 0, 0)))
    for i in range(9):
        parts.append(box("Rung", (0.5, 0.05, 0.05), (0, 1.3 + i * 0.045, 0.3 + i * 0.33), "Wood"))
    # Sign plate and a striped umbrella.
    parts.append(box("Sign", (1.4, 0.06, 0.32), (0, -1.16, 2.7), "Coral"))
    parts.append(cylinder("Pole", 0.03, 2.4, (0.8, 0.8, 4.2), "Steel", vertices=6))
    for i in range(8):
        a = i * math.tau / 8
        verts = [(0.8, 0.8, 5.45), (0.8 + math.cos(a) * 1.2, 0.8 + math.sin(a) * 1.2, 5.0),
                 (0.8 + math.cos(a + math.tau / 8) * 1.2, 0.8 + math.sin(a + math.tau / 8) * 1.2, 5.0)]
        parts.append(mesh_object("Canopy", verts, [(0, 1, 2)], "Coral" if i % 2 == 0 else "White", double_sided=True))
    return join(name, parts)


def camera_tripod(name="CameraTripod"):
    """A pro camera with a long lens on a tripod; the lens points -Y. Lens tip at about (0, -0.55, 1.42)."""
    parts = []
    for i in range(3):
        a = math.radians(90 + i * 120)
        top, foot = Vector((0, 0, 1.2)), Vector((math.cos(a) * 0.45, math.sin(a) * 0.45, 0.0))
        d = top - foot
        leg = cylinder("Leg", 0.022, d.length, (top + foot) / 2, "Charcoal", vertices=6)
        leg.rotation_mode = "QUATERNION"
        leg.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d.normalized())
        parts.append(leg)
    parts.append(cylinder("Head", 0.06, 0.12, (0, 0, 1.25), "Charcoal", vertices=8))
    parts.append(box("Body", (0.18, 0.14, 0.13), (0, 0.02, 1.38), "Charcoal"))
    parts.append(box("Grip", (0.05, 0.1, 0.12), (0.11, 0.02, 1.37), "Black" if "Black" in PALETTE else "Charcoal"))
    parts.append(cylinder("Lens", 0.055, 0.42, (0, -0.25, 1.4), "White", vertices=12, rotation=(math.radians(90), 0, 0)))
    parts.append(cylinder("Lens Ring", 0.062, 0.06, (0, -0.4, 1.4), "Charcoal", vertices=12, rotation=(math.radians(90), 0, 0)))
    parts.append(cylinder("Glass", 0.048, 0.01, (0, -0.465, 1.4), "Glass", vertices=12, rotation=(math.radians(90), 0, 0)))
    parts.append(box("Viewfinder", (0.06, 0.06, 0.05), (0, 0.09, 1.47), "Charcoal"))
    return join(name, parts)


def photo_kiosk(name="PhotoKiosk"):
    """A small photo shop counter (2.4 m) with a screen and a roof; prints pinned on the front."""
    parts = [box("Counter", (2.4, 0.8, 1.05), (0, 0, 0.525), "White"),
             box("Top", (2.5, 0.9, 0.06), (0, 0, 1.08), "DarkWood"),
             box("Band", (2.42, 0.82, 0.16), (0, 0, 0.8), "Teal")]
    for x in (-1.15, 1.15):
        parts.append(box("Post", (0.1, 0.1, 2.4), (x, 0.35, 1.2), "DarkWood"))
    parts.append(box("Roof", (2.8, 1.3, 0.1), (0, 0.05, 2.45), "Coral", rotation=(math.radians(-8), 0, 0)))
    parts.append(box("Sign", (1.8, 0.08, 0.36), (0, -0.62, 2.72), "Navy"))
    parts.append(box("Screen", (0.5, 0.05, 0.32), (0.6, 0.2, 1.3), "Screen", rotation=(math.radians(-10), 0, 0)))
    parts.append(box("Printer", (0.36, 0.3, 0.18), (-0.6, 0.15, 1.2), "KeyGrey"))
    for i in range(5):
        parts.append(box("Print", (0.24, 0.01, 0.18), (-0.9 + i * 0.45, -0.41, 0.62 + (i % 2) * 0.06), "White",
                         rotation=(0, math.radians(-5 + i * 3), 0)))
    return join(name, parts)


def photo_wall(name="PhotoWall"):
    """Wall of fame: a 3.4 x 2.2 m board on two legs with 6 frames (the photos go on quads in Unity)."""
    parts = [box("Board", (3.4, 0.12, 2.2), (0, 0, 1.9), "Navy"), box("Trim", (3.55, 0.08, 2.35), (0, 0.04, 1.9), "Sun" if "Sun" in PALETTE else "Yellow")]
    for x in (-1.4, 1.4):
        parts.append(box("Leg", (0.14, 0.14, 3.1), (x, 0.1, 1.55), "DarkWood"))
    for i in range(6):
        cx, cz = -1.05 + (i % 3) * 1.05, 2.35 - (i // 3) * 0.85
        parts.append(box("Frame", (0.92, 0.04, 0.66), (cx, -0.07, cz), "White"))
    parts.append(box("Title", (2.2, 0.04, 0.26), (0, -0.07, 3.2), "Coral"))
    return join(name, parts)


# ================================================================ build + export

MODELS = (
    acai_shack, acai_bowl, shake_cup,
    lambda: topping("TopBanana", "banana"), lambda: topping("TopStrawberry", "strawberry"),
    lambda: topping("TopBlueberry", "blueberry"), lambda: topping("TopGranola", "granola"),
    lambda: topping("TopCoconut", "coconut"), lambda: topping("TopHoney", "honey"), lambda: topping("TopMango", "mango"),
    topping_tub, blender_machine,
    lambda: stack("BowlStack", lambda: acai_bowl_empty(), 5, 0.04),
    lambda: stack("CupStack", lambda: cup_empty(), 6, 0.03),
    gear_shack, lambda: clothes_rack("ShortsRack", "shorts"), lambda: clothes_rack("RashRack", "rash"),
    wax_crate, fin_wall, leash_hooks, sunscreen_stand, shop_bag, shop_basket,
    shaper_shed, shaping_stand, foam_blank, spray_booth, spray_can, fin_box, wish_board,
    machine_chamber, chamber_piston, valve_wheel, blower_unit, blower_fan, pressure_dial, dial_needle,
    control_cabin, air_tank, timer_clock, famous_board, reef_switch, status_lamp,
    photo_tower, camera_tripod, photo_kiosk, photo_wall,
)


def acai_bowl_empty():
    return join("EmptyBowl", [cylinder("Shell", 0.07, 0.07, (0, 0, 0.035), "Shell", vertices=10, radius_top=0.12)])


def cup_empty():
    return join("EmptyCup", [cylinder("Cup", 0.045, 0.18, (0, 0, 0.09), "White", vertices=10, radius_top=0.06)])


def build_shops(only=None):
    clear_scene()
    built = []
    for factory in MODELS:
        obj = factory()
        if only and obj.name not in only:
            bpy.data.objects.remove(obj, do_unlink=True)
            continue
        if obj.location.length > 0:
            bpy.context.scene.cursor.location = (0, 0, 0)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        # join() keeps the first part's rotation/scale on the object; bake it into the mesh so Unity's root is clean
        # (WorldArt.Model and the shop code overwrite the root rotation).
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        built.append(obj.name)
        export(obj)
        obj.hide_set(True)
    return built


result = {"exported": build_shops(globals().get("ONLY"))}
