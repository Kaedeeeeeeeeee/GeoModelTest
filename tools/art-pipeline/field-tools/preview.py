"""Software preview renders of the generated models (numpy + Pillow only).

Back faces are culled exactly as Unity would, so an inside-out part shows up as a hole here.
The rig is assembled with the same layout numbers the game uses.
"""
import math
import os

import numpy as np
from PIL import Image

LIGHT = np.array([-0.45, 0.80, -0.40])
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def _unit(v):
    v = np.asarray(v, dtype=float)
    return v / np.linalg.norm(v)


def gather(model, materials, rot=None, offset=(0, 0, 0)):
    tris, normals, colours = [], [], []
    for material, tri, tri_normals in model.triangles():
        if rot is not None:
            tri = tri @ np.asarray(rot).T
            tri_normals = tri_normals @ np.asarray(rot).T
        tris.append(tri + np.asarray(offset, dtype=float))
        normals.append(tri_normals)
        colours.append(materials[material][:3])
    return tris, normals, colours


def aim_down(direction):
    """Rotation taking -Y onto `direction` (used for the tripod legs)."""
    y = -_unit(direction)
    x = _unit(np.cross(y, [0.0, 0.0, 1.0]))
    z = np.cross(x, y)
    return np.column_stack([x, y, z])


def render(scene, eye, target, size=(960, 720), fov=32.0, ground=None, scale=2):
    tris = np.array([t for part in scene for t in part[0]])
    normals = np.array([n for part in scene for n in part[1]])
    colours = np.array([c for part in scene for c in part[2]])
    w, h = size[0] * scale, size[1] * scale
    eye = np.asarray(eye, dtype=float)
    f = _unit(np.asarray(target, dtype=float) - eye)
    r = _unit(np.cross([0.0, 1.0, 0.0], f))
    u = np.cross(f, r)
    focal = (h / 2.0) / math.tan(math.radians(fov) / 2.0)
    top, bottom = np.array([0.74, 0.84, 0.93]), np.array([0.93, 0.95, 0.96])
    image = np.repeat((top[None, :] + (bottom - top)[None, :] * np.linspace(0, 1, h)[:, None])[:, None, :], w, axis=1)
    depth = np.full((h, w), np.inf)
    if ground is not None:
        ys, xs = np.mgrid[0:h, 0:w]
        ray = f[None, None, :] * focal + r[None, None, :] * (xs - w / 2.0)[..., None] - u[None, None, :] * (ys - h / 2.0)[..., None]
        t = np.where(ray[..., 1] < -1e-6, -eye[1] / np.minimum(ray[..., 1], -1e-6), np.inf)
        hit = np.isfinite(t)
        px = eye[0] + ray[..., 0] * t
        pz = eye[2] + ray[..., 2] * t
        checker = ((np.floor(px) + np.floor(pz)) % 2) * 0.04
        fade = np.clip(1.0 - t * f.dot(f) / 400.0, 0, 1)
        col = np.asarray(ground)[None, None, :] * (0.92 + checker)[..., None]
        image = np.where(hit[..., None], col * fade[..., None] + image * (1 - fade[..., None]), image)
        depth = np.where(hit, t * focal / np.maximum(np.linalg.norm(ray, axis=-1), 1e-9) * 0 + np.inf, depth)
    rel = tris - eye
    vx, vy, vz = rel @ r, rel @ u, rel @ f
    for i in range(len(tris)):
        z = vz[i]
        if (z < 0.02).any():
            continue
        a, b, c = tris[i]
        if np.dot(np.cross(b - a, c - a), eye - a) <= 0:
            continue
        sx = w / 2.0 + vx[i] / z * focal
        sy = h / 2.0 - vy[i] / z * focal
        x0, x1 = int(max(math.floor(sx.min()), 0)), int(min(math.ceil(sx.max()), w - 1))
        y0, y1 = int(max(math.floor(sy.min()), 0)), int(min(math.ceil(sy.max()), h - 1))
        if x0 > x1 or y0 > y1:
            continue
        area = (sx[1] - sx[0]) * (sy[2] - sy[0]) - (sx[2] - sx[0]) * (sy[1] - sy[0])
        if abs(area) < 1e-9:
            continue
        ys, xs = np.mgrid[y0:y1 + 1, x0:x1 + 1]
        xs = xs + 0.5
        ys = ys + 0.5
        w0 = ((sx[1] - xs) * (sy[2] - ys) - (sx[2] - xs) * (sy[1] - ys)) / area
        w1 = ((sx[2] - xs) * (sy[0] - ys) - (sx[0] - xs) * (sy[2] - ys)) / area
        w2 = 1.0 - w0 - w1
        inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
        if not inside.any():
            continue
        inv = w0 / z[0] + w1 / z[1] + w2 / z[2]
        zz = 1.0 / np.maximum(inv, 1e-9)
        region = depth[y0:y1 + 1, x0:x1 + 1]
        visible = inside & (zz < region)
        if not visible.any():
            continue
        n = (normals[i][0][None, None, :] * w0[..., None] + normals[i][1][None, None, :] * w1[..., None]
             + normals[i][2][None, None, :] * w2[..., None])
        n = n / np.maximum(np.linalg.norm(n, axis=-1, keepdims=True), 1e-9)
        diffuse = np.clip(n @ LIGHT, 0, 1)
        sky = 0.5 + 0.5 * n[..., 1]
        half = _unit(LIGHT - f)
        spec = np.clip(n @ half, 0, 1) ** 24 * 0.25
        shade = 0.30 + 0.22 * sky + 0.62 * diffuse
        col = np.clip(colours[i][None, None, :] * shade[..., None] + spec[..., None], 0, 1)
        region[visible] = zz[visible]
        image[y0:y1 + 1, x0:x1 + 1][visible] = col[visible]
    out = Image.fromarray((np.clip(image, 0, 1) * 255).astype(np.uint8))
    return out.resize(size, Image.LANCZOS)


def rig_scene(models, materials, rig, core_box, swivel_top=2.6, head_offset=0.0, with_cores=2):
    from meshlib import Model, cyl, rot_y, tube
    scene = []
    apex = np.array([0.0, rig["apex_height"], 0.0])
    scene.append(gather(models["RigHead"], materials, offset=apex))
    for yaw in (0.0, 120.0, 240.0):
        foot = np.array([math.sin(math.radians(yaw)), 0.0, math.cos(math.radians(yaw))]) * rig["foot_radius"]
        scene.append(gather(models["RigLeg"], materials, rot=aim_down(foot - apex), offset=apex))
    scene.append(gather(models["RigMachine"], materials))
    scene.append(gather(models["RigDrillHead"], materials, offset=(head_offset, 0, 0)))
    scene.append(gather(models["RigSwivel"], materials, offset=(0, swivel_top, 0)))
    scene.append(gather(models["RigRod"], materials, offset=(0, swivel_top - rig["swivel_height"], 0)))
    pump_at = np.array([-0.85, 0.0, 1.05])
    scene.append(gather(models["RigPump"], materials, offset=pump_at))
    box_at = np.array([2.1, 0.0, 0.0])
    scene.append(gather(models["CoreBox"], materials, offset=box_at))
    extra = Model("Extras")
    pc = apex + np.array(rig["pulley_center"])
    pr = rig["pulley_radius"]
    extra.add(cyl(pc + [pr, 0, 0], (0, swivel_top, 0), 0.006, seg=6), "dark", 60)
    extra.add(cyl((-0.62, 1.12, 0), pc + [-pr, 0, 0], 0.006, seg=6), "dark", 60)
    a = pump_at + np.array([-0.10, 0.43, -0.32])
    b = np.array([-0.11, swivel_top - 0.13, 0.0])
    hose = []
    for i in range(9):
        t = i / 8.0
        p = a + (b - a) * t
        p[1] -= math.sin(math.pi * t) * 0.35
        hose.append(p)
    extra.add(tube(hose, 0.018, seg=6), "rubber", 60)
    slots = core_box["slots"]
    for i in range(with_cores):
        x = box_at[0] + (i - (slots - 1) / 2.0) * core_box["slot_pitch"]
        z0 = -core_box["inner_length"] / 2 + 0.03
        y = core_box["floor"] + 0.10
        extra.add(cyl((x, y, z0), (x, y, z0 + 0.9), 0.1, seg=14), "wood_dark", 60)
        extra.add(cyl((x, y, z0 + 0.9), (x, y, z0 + 2.0), 0.1, seg=14), "label", 60)
    scene.append(gather(extra, materials))
    return scene


def render_all(models, materials, rig, core_box, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    hammer = [gather(models["RockHammer"], materials)]
    views = {
        "hammer_side": ((0.55, 0.12, -0.02), (0, 0.07, -0.02)),
        "hammer_three_quarter": ((0.34, 0.30, 0.34), (0, 0.08, -0.02)),
        "hammer_back": ((-0.30, 0.34, -0.36), (0, 0.08, -0.02)),
    }
    for name, (eye, target) in views.items():
        render(hammer, eye, target, size=(720, 600), fov=28).save(os.path.join(out_dir, name + ".png"))
    idle = rig_scene(models, materials, rig, core_box)
    ground = (0.52, 0.56, 0.40)
    render(idle, (1.3, 1.7, -6.2), (0.6, 1.35, 0), fov=40, ground=ground).save(os.path.join(out_dir, "rig_player_view.png"))
    render(idle, (-4.2, 2.6, -4.2), (0.3, 1.2, 0), fov=40, ground=ground).save(os.path.join(out_dir, "rig_left.png"))
    render(idle, (4.6, 3.0, 3.6), (0.5, 1.0, 0), fov=40, ground=ground).save(os.path.join(out_dir, "rig_back_right.png"))
    render(idle, (-1.6, 1.2, -2.0), (-0.35, 0.65, 0), fov=40, ground=ground).save(os.path.join(out_dir, "rig_machine_close.png"))
    pull = rig_scene(models, materials, rig, core_box, swivel_top=1.34, head_offset=0.0)
    render(pull, (1.3, 1.7, -6.2), (0.6, 1.35, 0), fov=40, ground=ground).save(os.path.join(out_dir, "rig_drilled.png"))
    render(idle, (3.4, 1.5, -2.4), (2.1, 0.1, 0), fov=40, ground=ground).save(os.path.join(out_dir, "core_box.png"))
