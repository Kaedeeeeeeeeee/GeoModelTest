"""Small procedural-modelling helpers for the field-tool models.

Pure Python + numpy so the models can be regenerated anywhere (no Blender needed).
Authoring space is Unity's: +X right, +Y up, +Z forward, metres.
A face's outward normal is cross(b - a, c - a), the same rule Unity uses.
"""
import math

import numpy as np


def _unit(v):
    v = np.asarray(v, dtype=float)
    n = np.linalg.norm(v)
    return v / n if n > 0 else v


def basis(axis):
    """Two unit vectors perpendicular to `axis`, plus the normalised axis."""
    a = _unit(axis)
    ref = np.array([0.0, 1.0, 0.0]) if abs(a[1]) < 0.9 else np.array([1.0, 0.0, 0.0])
    u = _unit(np.cross(ref, a))
    v = np.cross(a, u)
    return u, v, a


def ring(center, axis, ru, rv=None, seg=12, phase=0.0, frame=None):
    """Elliptical ring of `seg` points around `axis`."""
    rv = ru if rv is None else rv
    u, v, _ = frame if frame is not None else basis(axis)
    c = np.asarray(center, dtype=float)
    pts = []
    for i in range(seg):
        t = phase + 2.0 * math.pi * i / seg
        pts.append(c + u * (math.cos(t) * ru) + v * (math.sin(t) * rv))
    return np.array(pts)


def chamfer_ring(center, u, v, hu, hv, chamfer=0.3):
    """Octagonal (chamfered rectangle) ring in the plane spanned by unit vectors u and v."""
    c = np.asarray(center, dtype=float)
    u = np.asarray(u, dtype=float)
    v = np.asarray(v, dtype=float)
    k = chamfer * min(hu, hv)
    corners = [(hu - k, hv), (hu, hv - k), (hu, -hv + k), (hu - k, -hv),
               (-hu + k, -hv), (-hu, -hv + k), (-hu, hv - k), (-hu + k, hv)]
    return np.array([c + u * a + v * b for a, b in corners])


def signed_volume(verts, faces):
    vol = 0.0
    for f in faces:
        a = verts[f[0]]
        for i in range(1, len(f) - 1):
            vol += float(np.dot(a, np.cross(verts[f[i]], verts[f[i + 1]])))
    return vol / 6.0


def oriented(verts, faces):
    """Flip every face when the closed shape is inside-out."""
    if signed_volume(verts, faces) < 0:
        faces = [tuple(reversed(f)) for f in faces]
    return verts, faces


def loft(rings, caps=True):
    """Skin consecutive rings (all the same point count) into a closed solid."""
    rings = [np.asarray(r, dtype=float) for r in rings]
    n = len(rings[0])
    verts = np.vstack(rings)
    faces = []
    for i in range(len(rings) - 1):
        a, b = i * n, (i + 1) * n
        for j in range(n):
            k = (j + 1) % n
            faces.append((a + j, a + k, b + k, b + j))
    if caps:
        faces.append(tuple(reversed(range(n))))
        last = (len(rings) - 1) * n
        faces.append(tuple(range(last, last + n)))
    return oriented(verts, faces)


def box(center, size, rot=None):
    cx, cy, cz = center
    hx, hy, hz = size[0] / 2.0, size[1] / 2.0, size[2] / 2.0
    lo = np.array([[-hx, -hy, -hz], [hx, -hy, -hz], [hx, -hy, hz], [-hx, -hy, hz]])
    hi = lo + np.array([0.0, 2.0 * hy, 0.0])
    verts, faces = loft([lo, hi])
    if rot is not None:
        verts = verts @ np.asarray(rot).T
    return verts + np.array([cx, cy, cz]), faces


def cyl(p0, p1, r0, r1=None, seg=12):
    r1 = r0 if r1 is None else r1
    p0 = np.asarray(p0, dtype=float)
    p1 = np.asarray(p1, dtype=float)
    frame = basis(p1 - p0)
    return loft([ring(p0, None, r0, seg=seg, frame=frame), ring(p1, None, r1, seg=seg, frame=frame)])


def lathe(center, axis, profile, seg=16):
    """Revolve [(radius, distance along axis), ...] around `axis` through `center`."""
    frame = basis(axis)
    c = np.asarray(center, dtype=float)
    return loft([ring(c + frame[2] * d, None, max(r, 1e-4), seg=seg, frame=frame) for r, d in profile])


def tube(points, r, seg=8):
    """Constant-radius tube along a polyline, using a parallel-transport frame."""
    pts = [np.asarray(p, dtype=float) for p in points]
    u, v, a = basis(pts[1] - pts[0])
    rings = []
    for i, p in enumerate(pts):
        if i == 0:
            t = _unit(pts[1] - pts[0])
        elif i == len(pts) - 1:
            t = _unit(pts[-1] - pts[-2])
        else:
            t = _unit(_unit(pts[i + 1] - p) + _unit(p - pts[i - 1]))
        u = _unit(u - t * np.dot(u, t))
        v = np.cross(t, u)
        rings.append(ring(p, None, r, seg=seg, frame=(u, v, t)))
    return loft(rings)


def rot_x(deg):
    c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]], dtype=float)


def rot_y(deg):
    c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]], dtype=float)


def rot_z(deg):
    c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]], dtype=float)


def moved(shape, offset=(0, 0, 0), rot=None):
    verts, faces = shape
    if rot is not None:
        verts = verts @ np.asarray(rot).T
    return verts + np.asarray(offset, dtype=float), faces


class Model:
    """A named model: a list of (verts, faces, material, smoothing angle) parts."""

    def __init__(self, name):
        self.name = name
        self.parts = []

    def add(self, shape, material, smooth=35.0):
        verts, faces = shape
        self.parts.append((np.asarray(verts, dtype=float), list(faces), material, smooth))
        return self

    def triangles(self):
        """Yield (material, positions 3x3, corner normals 3x3) for every triangle."""
        for verts, faces, material, smooth in self.parts:
            normals = []
            for f in faces:
                n = np.zeros(3)
                a = verts[f[0]]
                for i in range(1, len(f) - 1):
                    n += np.cross(verts[f[i]] - a, verts[f[i + 1]] - a)
                normals.append(n)
            by_position = {}
            for fi, f in enumerate(faces):
                for vi in f:
                    by_position.setdefault(tuple(np.round(verts[vi], 5)), []).append(fi)
            limit = math.cos(math.radians(smooth))
            for fi, f in enumerate(faces):
                face_normal = _unit(normals[fi])
                corners = []
                for vi in f:
                    total = np.zeros(3)
                    for other in by_position[tuple(np.round(verts[vi], 5))]:
                        if np.dot(_unit(normals[other]), face_normal) >= limit:
                            total += normals[other]
                    corners.append(_unit(total) if np.linalg.norm(total) > 0 else face_normal)
                for i in range(1, len(f) - 1):
                    idx = (0, i, i + 1)
                    yield (material,
                           np.array([verts[f[j]] for j in idx]),
                           np.array([corners[j] for j in idx]))

    def bounds(self):
        allv = np.vstack([p[0] for p in self.parts])
        return allv.min(axis=0), allv.max(axis=0)

    def triangle_count(self):
        return sum(len(f) - 2 for _, faces, _, _ in self.parts for f in faces)

    def write_obj(self, path, mtl_name):
        """Write a standard right-handed OBJ.

        Unity's OBJ importer mirrors X and flips the winding on import, so the file stores the
        mirrored model; after import the mesh matches the authoring space above.
        """
        positions, position_index = [], {}
        normals, normal_index = [], {}
        by_material = {}
        for material, tri, tri_normals in self.triangles():
            corners = []
            for p, n in zip(tri, tri_normals):
                pk = (round(-p[0], 5), round(p[1], 5), round(p[2], 5))
                nk = (round(-n[0], 4), round(n[1], 4), round(n[2], 4))
                if pk not in position_index:
                    position_index[pk] = len(positions) + 1
                    positions.append(pk)
                if nk not in normal_index:
                    normal_index[nk] = len(normals) + 1
                    normals.append(nk)
                corners.append((position_index[pk], normal_index[nk]))
            by_material.setdefault(material, []).append(tuple(reversed(corners)))
        with open(path, "w", newline="\n") as out:
            out.write("# Generated by tools/art-pipeline/field-tools/build_models.py - do not edit by hand.\n")
            out.write("mtllib %s\n" % mtl_name)
            out.write("o %s\n" % self.name)
            for p in positions:
                out.write("v %.5f %.5f %.5f\n" % p)
            for n in normals:
                out.write("vn %.4f %.4f %.4f\n" % n)
            for material in sorted(by_material):
                out.write("usemtl %s\n" % material)
                for corners in by_material[material]:
                    out.write("f " + " ".join("%d//%d" % c for c in corners) + "\n")
