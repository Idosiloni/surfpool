"""
Builds the three surfboards (BoardFoamie, BoardFun, BoardShort) in Blender and exports them as FBX.

Run in Blender (Blender MCP): exec(open(path).read(), ns). Borrows helpers/palette/export from build_models.py
(everything above its final build line). Set EXPORT_DIR_OVERRIDE in ns to export elsewhere.

Boards lie along Y with the nose toward -Y (Unity forward). Each station of the outline is an 8-point ring:
bottom center, bottom edges, rail apexes (the widest point, low on the rail), deck edges and the crown, so
rails are rounded and the deck is domed. Materials: BoardDeck (tinted per surfer in Unity, so keep the name),
the rail color, the bottom, a stripe near the nose, a grip pad with a kick on the tail, a logo sticker, fins,
a wooden stringer along the crown and a leash plug at the tail.
"""
import math
import bpy

_here = "/Users/ido/surf-pool-sim/tools/blender/build_models.py"
_src = open(_here).read()
exec(_src[:_src.rindex("result = {")], globals())
EXPORT_DIR = globals().get("EXPORT_DIR_OVERRIDE", EXPORT_DIR)

PALETTE.update({"Grip": (0.12, 0.13, 0.15), "FoamBottom": (0.10, 0.38, 0.62)})


def _width(t, nose_round, tail_width):
    """Half-width at t (0 = tail, 1 = nose) as a fraction of the max."""
    base = math.sin(math.pi * (0.10 + 0.80 * t)) ** 0.6
    if t < 0.15:  # squarer tail
        base = max(base, tail_width * (0.4 + t / 0.15 * 0.6))
    if t > 0.85:  # nose: round boards stay wide, pointy ones taper to a point
        k = min((t - 0.85) / 0.15, 1.0)
        base *= (1 - 0.7 * k ** 1.8) if nose_round else max(1 - k, 0.0) ** 1.2
    return base


def board(name, length, half_width, thickness, nose_round, tail_width, rail_mat, bottom_mat, stripe_mat, fin_mat,
          stringer=True, grip=True, fins=3):
    steps = 24
    stations = []
    for i in range(steps + 1):
        t = i / steps
        y = length / 2 - length * t
        w = max(half_width * _width(t, nose_round, tail_width), 0.012)
        rocker = 0.07 * ((t - 0.45) / 0.55) ** 2 + (0.05 * ((t - 0.85) / 0.15) ** 2 if t > 0.85 else 0)
        h = thickness / 2 * (0.5 + 0.5 * math.sin(math.pi * min(max(t, 0.04), 0.96)))
        stations.append((t, y, w, rocker, h))

    # Ring order (looking from the tail): 0 bottom center, 1 bottom edge L, 2 rail L, 3 deck edge L, 4 crown,
    # 5 deck edge R, 6 rail R, 7 bottom edge R.
    def ring(y, w, r, h):
        return [(0.0, y, r - h), (-w * 0.82, y, r - h * 0.85), (-w, y, r - h * 0.25), (-w * 0.86, y, r + h * 0.75),
                (0.0, y, r + h), (w * 0.86, y, r + h * 0.75), (w, y, r - h * 0.25), (w * 0.82, y, r - h * 0.85)]

    # Material per ring segment k (between ring point k and k+1): bottom, rail, rail (up onto the deck edge, so a
    # colored rail shows from above as an outline), deck, deck, rail, rail, bottom.
    seg_mat = [2, 1, 1, 0, 0, 1, 1, 2]
    verts, faces, mats = [], [], []
    for t, y, w, r, h in stations:
        verts += ring(y, w, r, h)
    for i in range(len(stations) - 1):
        a, b = i * 8, (i + 1) * 8
        for k in range(8):
            k2 = (k + 1) % 8
            faces.append((a + k, a + k2, b + k2, b + k))
            mats.append(seg_mat[k])
    last = (len(stations) - 1) * 8
    faces.append(tuple(range(8)))
    mats.append(0)
    faces.append(tuple(reversed(range(last, last + 8))))
    mats.append(0)
    obj = mesh_object(name, verts, faces, "BoardDeck")
    obj.data.materials.append(material(rail_mat))
    obj.data.materials.append(material(bottom_mat))
    for poly, m in zip(obj.data.polygons, mats):
        poly.material_index = m
    recalc_normals(obj)
    parts = [obj]

    def at(t):
        """Interpolated station (y, w, rocker, h) at t."""
        for s0, s1 in zip(stations, stations[1:]):
            if s0[0] <= t <= s1[0]:
                k = (t - s0[0]) / max(s1[0] - s0[0], 1e-6)
                return tuple(s0[j] + (s1[j] - s0[j]) * k for j in range(1, 5))
        return stations[-1][1:]

    def deck_strip(label, t_values, mat, inset=0.9, lift=0.003):
        """A thin sheet lying on the domed deck between deck edges, across the board at each t."""
        sv, sf = [], []
        for i, t in enumerate(t_values):
            y, w, r, h = at(t)
            sv += [(-w * 0.86 * inset, y, r + h * 0.75 + lift), (0.0, y, r + h + lift), (w * 0.86 * inset, y, r + h * 0.75 + lift)]
            if i:
                a = (i - 1) * 3
                sf += [(a, a + 1, a + 4, a + 3), (a + 1, a + 2, a + 5, a + 4)]
        part = mesh_object(name + label, sv, sf, mat)
        recalc_normals(part)
        return part

    if stringer:  # thin wooden strip down the crown
        sv, sf = [], []
        for i, (t, y, w, r, h) in enumerate(stations[1:-1]):
            sv += [(-0.006, y, r + h + 0.0025), (0.006, y, r + h + 0.0025)]
            if i:
                a = (i - 1) * 2
                sf.append((a, a + 1, a + 3, a + 2))
        parts.append(mesh_object(name + "Stringer", sv, sf, "Stringer"))
    parts.append(deck_strip("Stripe", (0.70, 0.725, 0.75, 0.775), stripe_mat, inset=0.98))
    # A little logo sticker on the nose half: a white square with a colored dot.
    y, w, r, h = at(0.6)
    parts.append(box(name + "Logo", (0.07, 0.07, 0.004), (0.0, y, r + h + 0.004), "White"))
    parts.append(box(name + "LogoDot", (0.035, 0.035, 0.004), (0.0, y, r + h + 0.007), stripe_mat))
    if grip:  # grip pad over the back of the deck, with a raised kick at the very tail
        parts.append(deck_strip("Grip", [0.02 + 0.03 * i for i in range(7)], "Grip", inset=0.85, lift=0.006))
        y, w, r, h = at(0.03)
        parts.append(box(name + "Kick", (w * 1.3, 0.05, 0.03), (0.0, y, r + h + 0.02), "Grip"))
    # Leash plug just in front of the tail.
    y, w, r, h = at(0.045)
    parts.append(cylinder(name + "Plug", 0.014, 0.01, (0.0, y - 0.02, r + h + 0.008), "Charcoal", vertices=8))

    tail_y = length / 2
    fin_set = [(0.0, tail_y - 0.07 * length, 1.0)]
    if fins == 3:
        fin_set += [(-half_width * 0.6, tail_y - 0.14 * length, 0.8), (half_width * 0.6, tail_y - 0.14 * length, 0.8)]
    for fx, fy, size in fin_set:
        t = (tail_y - fy) / length
        _, _, r, h = at(t)
        base = r - h * 0.9
        fin_len, fin_h = 0.12 * size, 0.12 * size
        # Raked fin: wide base, swept-back tip.
        fv = [(fx, fy - fin_len / 2, base), (fx, fy + fin_len / 2, base), (fx, fy + fin_len * 0.6, base - fin_h)]
        fv += [(fx + 0.008, y2, z2) for _, y2, z2 in fv]
        ff = [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]
        fin = mesh_object(name + "Fin", fv, ff, fin_mat)
        recalc_normals(fin)
        parts.append(fin)
    return join(name, parts)


def board_foamie():
    # Soft-top beginner board: wide and thick, colored rails, slick blue bottom, three soft fins, no grip.
    return board("BoardFoamie", 2.4, 0.31, 0.10, True, 0.85, "Teal", "FoamBottom", "Yellow", "Navy",
                 stringer=False, grip=False)


def board_fun():
    return board("BoardFun", 2.1, 0.27, 0.075, True, 0.6, "BoardDeck", "White", "Coral", "Navy")


def board_short():
    return board("BoardShort", 1.8, 0.235, 0.06, False, 0.45, "BoardDeck", "White", "Teal", "Charcoal")


BOARDS = [board_foamie, board_fun, board_short]


def build_boards():
    clear_scene()
    return [(b().name, None) for b in BOARDS]


def export_boards():
    clear_scene()
    built = []
    for factory in BOARDS:
        obj = factory()
        built.append((obj.name, export(obj)))
    return built


if globals().get("BUILD_BOARDS", True):
    result = {"exported": export_boards()}
