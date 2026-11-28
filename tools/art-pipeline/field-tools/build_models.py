#!/usr/bin/env python3
"""Generate the field-tool models (rock hammer, boring rig parts, core box) as OBJ files.

Usage:  python3 tools/art-pipeline/field-tools/build_models.py [--preview DIR]

The OBJ files land in Assets/Resources/Models/FieldTools/ and are loaded at runtime by
FieldToolModels.cs, which assigns materials by the `usemtl` names used here. Every part is
modelled around the pivot it is animated about, so the C# side only moves transforms.
Dimensions are metres. Keep RIG / CORE_BOX in sync with BoringRigLayout in C#.
"""
import hashlib
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from meshlib import Model, box, chamfer_ring, cyl, lathe, loft, moved, ring, rot_y, tube  # noqa: E402

X, Y, Z = np.eye(3)

# name -> (r, g, b, metallic, smoothness); mirrored in FieldToolModels.cs
MATERIALS = {
    "steel": (0.66, 0.68, 0.71, 0.75, 0.55),
    "dark": (0.20, 0.21, 0.23, 0.45, 0.35),
    "paint": (0.92, 0.47, 0.12, 0.10, 0.45),
    "band": (0.96, 0.77, 0.20, 0.05, 0.40),
    "grip": (0.12, 0.30, 0.56, 0.00, 0.25),
    "rubber": (0.09, 0.09, 0.10, 0.00, 0.20),
    "red": (0.74, 0.16, 0.13, 0.05, 0.45),
    "blue": (0.20, 0.45, 0.70, 0.05, 0.40),
    "wood": (0.74, 0.57, 0.36, 0.00, 0.15),
    "wood_dark": (0.55, 0.40, 0.24, 0.00, 0.15),
    "label": (0.93, 0.93, 0.88, 0.00, 0.20),
}

# Shared layout numbers (tower-local, metres).
RIG = {
    "apex_height": 3.2,
    "leg_length": 3.47,
    "foot_radius": 1.35,
    "pulley_center": (-0.115, -0.34, 0.0),  # relative to the apex
    "pulley_radius": 0.115,
    "swivel_height": 0.26,
    "rod_length": 3.3,
}
CORE_BOX = {
    "slots": 5,
    "slot_pitch": 0.26,
    "inner_length": 2.08,
    "wall": 0.025,
    "floor": 0.02,
    "height": 0.15,
}


def build_hammer():
    """One-piece forged rock pick: square striking face (+Z), pointed pick (-Z), nylon grip.
    Pivot = middle of the grip, handle along +Y. About 31 cm long, like a real 22 oz rock hammer."""
    m = Model("RockHammer")
    head = [  # z, centre y, half height, half width
        (0.054, 0.2050, 0.0115, 0.0115),
        (0.050, 0.2050, 0.0135, 0.0135),
        (0.040, 0.2050, 0.0135, 0.0135),
        (0.034, 0.2050, 0.0105, 0.0100),
        (0.024, 0.2050, 0.0150, 0.0115),
        (0.012, 0.2060, 0.0185, 0.0125),
        (-0.012, 0.2060, 0.0185, 0.0125),
        (-0.024, 0.2050, 0.0150, 0.0115),
        (-0.050, 0.2040, 0.0110, 0.0080),
        (-0.085, 0.2010, 0.0070, 0.0056),
        (-0.115, 0.1970, 0.0035, 0.0036),
        (-0.130, 0.1945, 0.0010, 0.0018),
    ]
    m.add(loft([chamfer_ring((0, yc, z), X, Y, hw, hh) for z, yc, hh, hw in head]), "steel", 40)
    shank = [(0.195, 0.0080, 0.0150), (0.165, 0.0062, 0.0125), (0.110, 0.0056, 0.0108), (0.072, 0.0060, 0.0112)]
    m.add(loft([chamfer_ring((0, y, 0), X, Z, hx, hz, 0.45) for y, hx, hz in shank]), "steel", 40)
    grip = [(0.086, 0.0075, 0.0120), (0.078, 0.0125, 0.0165), (0.050, 0.0128, 0.0172), (0.020, 0.0118, 0.0155),
            (-0.020, 0.0118, 0.0155), (-0.050, 0.0126, 0.0166), (-0.068, 0.0138, 0.0186),
            (-0.079, 0.0132, 0.0180), (-0.084, 0.0100, 0.0140)]
    m.add(loft([ring((0, y, 0), None, rz, rx, seg=14, frame=(Z, X, Y)) for y, rx, rz in grip]), "grip", 60)
    m.add(loft([ring((0, y, 0), None, 0.0180, 0.0135, seg=14, frame=(Z, X, Y)) for y in (0.058, 0.066)]), "band", 60)
    m.add(loft([ring((0, -0.083, 0), None, 0.0138, 0.0098, seg=14, frame=(Z, X, Y)),
                ring((0, -0.090, 0), None, 0.0122, 0.0085, seg=14, frame=(Z, X, Y))]), "steel", 60)
    return m


def build_rig_leg():
    """Telescopic tripod leg hanging from the apex (origin), pointing down -Y."""
    m = Model("RigLeg")
    length = RIG["leg_length"]
    m.add(box((0, -0.04, 0), (0.07, 0.12, 0.05)), "dark")
    m.add(cyl((0, -0.06, 0), (0, -2.42, 0), 0.042, seg=10), "steel", 60)
    m.add(cyl((0, -2.30, 0), (0, -length + 0.02, 0), 0.034, seg=10), "steel", 60)
    m.add(cyl((0, -2.30, 0), (0, -2.42, 0), 0.056, seg=10), "dark", 60)
    m.add(cyl((0.05, -2.36, 0), (0.12, -2.36, 0), 0.012, seg=6), "red", 60)
    m.add(cyl((0, -length + 0.03, 0), (0, -length, 0), 0.10, seg=12), "dark", 60)
    for y in (-0.9, -1.7):
        m.add(cyl((0, y, 0), (0, y - 0.05, 0), 0.047, seg=10), "band", 60)
    return m


def build_rig_head():
    """Tripod apex hub with the hanging sheave. Origin = apex."""
    m = Model("RigHead")
    px, py, _ = RIG["pulley_center"]
    m.add(cyl((0, -0.08, 0), (0, 0.08, 0), 0.10, seg=12), "dark", 60)
    m.add(cyl((0, 0.08, 0), (0, 0.14, 0), 0.10, 0.045, seg=12), "dark", 40)
    m.add(box((px * 0.5, -0.12, 0), (abs(px) + 0.10, 0.05, 0.07)), "dark")
    for z in (-0.036, 0.036):
        m.add(box((px, py + 0.07, z), (0.08, 0.34, 0.012)), "paint")
    r = RIG["pulley_radius"]
    profile = [(0.03, -0.022), (r + 0.025, -0.022), (r + 0.025, -0.014), (r, -0.005),
               (r, 0.005), (r + 0.025, 0.014), (r + 0.025, 0.022), (0.03, 0.022)]
    m.add(lathe((px, py, 0), Z, profile, seg=18), "steel", 50)
    m.add(cyl((px, py, -0.055), (px, py, 0.055), 0.018, seg=8), "dark", 60)
    return m


def build_rig_machine():
    """Spindle-type boring machine on a skid. Origin = hole centre on the ground; body runs to -X."""
    m = Model("RigMachine")
    for z in (-0.3, 0.3):
        m.add(box((-0.5, 0.04, z), (1.75, 0.08, 0.09)), "dark")
    for x in (-1.3, -0.75, 0.3):
        m.add(box((x, 0.04, 0), (0.08, 0.06, 0.6)), "dark")
    m.add(box((-0.78, 0.095, 0), (1.1, 0.03, 0.62)), "steel")
    # Gearbox with the slide rails the drill head retracts on.
    m.add(box((-0.42, 0.31, 0), (0.38, 0.40, 0.36)), "paint")
    for z in (-0.15, 0.15):
        m.add(box((-0.20, 0.535, z), (0.82, 0.05, 0.06)), "steel")
    # Engine.
    m.add(box((-0.98, 0.36, 0), (0.56, 0.50, 0.42)), "dark")
    m.add(box((-0.98, 0.63, 0), (0.50, 0.04, 0.38)), "paint")
    m.add(box((-1.05, 0.73, -0.02), (0.30, 0.16, 0.28)), "red")
    m.add(cyl((-1.05, 0.81, -0.02), (-1.05, 0.84, -0.02), 0.035, seg=8), "dark", 60)
    m.add(cyl((-0.80, 0.65, 0.11), (-0.80, 0.82, 0.11), 0.07, seg=12), "dark", 60)
    m.add(cyl((-0.82, 0.65, -0.14), (-0.82, 1.25, -0.14), 0.026, seg=8), "steel", 60)
    m.add(cyl((-0.82, 0.74, -0.14), (-0.82, 0.96, -0.14), 0.05, seg=10), "dark", 60)
    m.add(cyl((-1.26, 0.36, 0), (-1.31, 0.36, 0), 0.16, seg=14), "steel", 60)
    m.add(box((-0.655, 0.36, 0.2), (0.14, 0.34, 0.05)), "band")
    for i in range(4):
        m.add(box((-0.98, 0.20 + i * 0.09, -0.215), (0.46, 0.035, 0.02)), "steel")
    # Hoist winch above the gearbox.
    wx, wy = -0.62, 1.0
    for z in (-0.2, 0.2):
        m.add(box((wx, 0.77, z), (0.07, 0.52, 0.03)), "dark")
    drum = [(0.05, -0.17), (0.17, -0.17), (0.17, -0.15), (0.09, -0.15),
            (0.09, 0.15), (0.17, 0.15), (0.17, 0.17), (0.05, 0.17)]
    m.add(lathe((wx, wy, 0), Z, drum, seg=16), "steel", 50)
    m.add(cyl((wx, wy, -0.14), (wx, wy, 0.14), 0.12, seg=14), "rubber", 60)
    m.add(cyl((wx, wy, -0.215), (wx - 0.14, wy + 0.3, -0.27), 0.012, seg=6), "dark", 60)
    m.add(cyl((wx - 0.14, wy + 0.3, -0.27), (wx - 0.155, wy + 0.335, -0.277), 0.024, seg=8), "red", 60)
    # Control levers on the operator side (-Z).
    for x in (-0.52, -0.44, -0.36):
        m.add(cyl((x, 0.38, -0.18), (x, 0.62, -0.34), 0.011, seg=6), "steel", 60)
        m.add(cyl((x, 0.62, -0.34), (x, 0.665, -0.37), 0.022, seg=8), "red", 60)
    return m


def build_rig_drill_head():
    """The part that slides back to clear the hole when the core is pulled. Same origin as the machine."""
    m = Model("RigDrillHead")
    m.add(box((0, 0.71, 0), (0.26, 0.30, 0.38)), "paint")
    for z in (-0.15, 0.15):
        m.add(box((0, 0.575, z), (0.26, 0.03, 0.08)), "dark")
    m.add(cyl((0, 0.86, 0), (0, 0.92, 0), 0.075, seg=12), "dark", 60)
    m.add(cyl((0, 0.92, 0), (0, 1.04, 0), 0.045, seg=10), "steel", 60)
    m.add(lathe((0, 0.56, 0), Y, [(0.06, 0.0), (0.085, -0.03), (0.085, -0.11), (0.05, -0.14)], seg=12), "steel", 40)
    m.add(cyl((0.02, 0.72, 0.19), (0.02, 0.72, 0.215), 0.09, seg=14), "dark", 60)
    m.add(cyl((0.02, 0.72, 0.215), (0.02, 0.72, 0.25), 0.02, seg=8), "steel", 60)
    return m


def build_rig_swivel():
    """Water swivel hanging from the hoist wire. Origin = top of the bail; the rod screws in below."""
    m = Model("RigSwivel")
    h = RIG["swivel_height"]
    m.add(tube([(-0.035, -0.09, 0), (-0.04, -0.03, 0), (0, 0, 0), (0.04, -0.03, 0), (0.035, -0.09, 0)], 0.009, seg=6),
          "steel", 60)
    m.add(lathe((0, 0, 0), Y, [(0.03, -0.07), (0.06, -0.09), (0.06, -0.20), (0.035, -0.23), (0.035, -h)], seg=12),
          "paint", 40)
    m.add(cyl((0, -0.15, 0), (-0.10, -0.13, 0), 0.018, seg=8), "steel", 60)
    return m


def build_rig_rod():
    """Drill rod with hexagonal couplings so the rotation reads. Origin = top end, runs down -Y."""
    m = Model("RigRod")
    length = RIG["rod_length"]
    m.add(cyl((0, 0, 0), (0, -length, 0), 0.022, seg=8), "steel", 60)
    for y in (-0.06, -1.06, -2.06):
        m.add(cyl((0, y, 0), (0, y - 0.09, 0), 0.036, seg=6), "dark", 20)
    return m


def build_rig_pump():
    """Mud pump with its water drum. Origin = centre of the unit on the ground."""
    m = Model("RigPump")
    drum = [(0.27, 0.0), (0.29, 0.02), (0.29, 0.30), (0.30, 0.31), (0.29, 0.32), (0.29, 0.58),
            (0.30, 0.59), (0.29, 0.60), (0.29, 0.86), (0.27, 0.88)]
    m.add(lathe((0, 0, 0.30), Y, drum, seg=18), "blue", 50)
    m.add(box((0, 0.04, -0.32), (0.50, 0.08, 0.34)), "dark")
    m.add(box((-0.10, 0.20, -0.32), (0.22, 0.24, 0.24)), "paint")
    m.add(cyl((0.02, 0.20, -0.32), (0.24, 0.20, -0.32), 0.10, seg=12), "dark", 60)
    m.add(cyl((-0.10, 0.32, -0.32), (-0.10, 0.42, -0.32), 0.025, seg=8), "steel", 60)
    m.add(tube([(-0.10, 0.25, -0.20), (-0.10, 0.70, -0.08), (-0.06, 1.00, 0.12), (0.0, 0.94, 0.26), (0.0, 0.70, 0.30)],
               0.025, seg=8), "rubber", 60)
    return m


def core_box_slot_x(index):
    return (index - (CORE_BOX["slots"] - 1) / 2.0) * CORE_BOX["slot_pitch"]


def build_core_box():
    """Wooden core box with five troughs. Origin = centre of the box on the ground; cores lie along Z."""
    m = Model("CoreBox")
    c = CORE_BOX
    wall, floor, height = c["wall"], c["floor"], c["height"]
    width = c["slots"] * c["slot_pitch"] - 0.02 + 2 * wall
    length = c["inner_length"] + 2 * wall
    m.add(box((0, floor / 2, 0), (width, floor, length)), "wood_dark")
    for x in (-(width - wall) / 2, (width - wall) / 2):
        m.add(box((x, height / 2, 0), (wall, height, length)), "wood")
    for z in (-(length - wall) / 2, (length - wall) / 2):
        m.add(box((0, height / 2, z), (width, height, wall)), "wood")
    for i in range(c["slots"] - 1):
        x = (core_box_slot_x(i) + core_box_slot_x(i + 1)) / 2
        m.add(box((x, floor + (height - floor - 0.02) / 2, 0), (0.02, height - floor - 0.02, c["inner_length"])), "wood")
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.add(box((sx * (width / 2 - 0.02), height / 2, sz * (length / 2 - 0.02)), (0.05, height + 0.006, 0.05)),
                  "steel")
    for sz in (-1, 1):
        z = sz * length / 2
        m.add(tube([(-0.15, 0.09, z), (-0.15, 0.085, z + sz * 0.055), (0.15, 0.085, z + sz * 0.055), (0.15, 0.09, z)],
                   0.012, seg=6), "rubber", 60)
    for i in range(c["slots"]):
        m.add(box((core_box_slot_x(i), height + 0.03, -length / 2 + wall / 2), (0.20, 0.06, 0.008)), "label")
    return m


BUILDERS = [build_hammer, build_rig_leg, build_rig_head, build_rig_machine, build_rig_drill_head,
            build_rig_swivel, build_rig_rod, build_rig_pump, build_core_box]


MODEL_META = """fileFormatVersion: 2
guid: %s
ModelImporter:
  serializedVersion: 22200
  internalIDToNameTable: []
  externalObjects: {}
  materials:
    materialImportMode: 2
    materialName: 0
    materialSearch: 1
    materialLocation: 1
  animations:
    legacyGenerateAnimations: 4
    bakeSimulation: 0
    resampleCurves: 1
    optimizeGameObjects: 0
    removeConstantScaleCurves: 0
    motionNodeName: 
    animationImportErrors: 
    animationImportWarnings: 
    animationRetargetingWarnings: 
    animationDoRetargetingWarnings: 0
    importAnimatedCustomProperties: 0
    importConstraints: 0
    animationCompression: 1
    animationRotationError: 0.5
    animationPositionError: 0.5
    animationScaleError: 0.5
    animationWrapMode: 0
    extraExposedTransformPaths: []
    extraUserProperties: []
    clipAnimations: []
    isReadable: 1
  meshes:
    lODScreenPercentages: []
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    useSRGBMaterialColor: 1
    sortHierarchyByName: 1
    importPhysicalCameras: 1
    importVisibility: 1
    importBlendShapes: 0
    importCameras: 0
    importLights: 0
    nodeNameCollisionStrategy: 1
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    keepQuads: 0
    weldVertices: 1
    bakeAxisConversion: 0
    preserveHierarchy: 0
    skinWeightsMode: 0
    maxBonesPerVertex: 4
    minBoneWeight: 0.001
    optimizeBones: 1
    meshOptimizationFlags: -1
    indexFormat: 0
    secondaryUVAngleDistortion: 8
    secondaryUVAreaDistortion: 15.000001
    secondaryUVHardAngle: 88
    secondaryUVMarginMethod: 1
    secondaryUVMinLightmapResolution: 40
    secondaryUVMinObjectScale: 1
    secondaryUVPackMargin: 4
    useFileScale: 1
    strictVertexDataChecks: 0
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 0
    tangentImportMode: 3
    normalCalculationMode: 4
    legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes: 0
    blendShapeNormalImportMode: 1
    normalSmoothingSource: 0
  referencedClips: []
  importAnimation: 0
  humanDescription:
    serializedVersion: 3
    human: []
    skeleton: []
    armTwist: 0.5
    foreArmTwist: 0.5
    upperLegTwist: 0.5
    legTwist: 0.5
    armStretch: 0.05
    legStretch: 0.05
    feetSpacing: 0
    globalScale: 1
    rootMotionBoneName: 
    hasTranslationDoF: 0
    hasExtraRoot: 0
    skeletonHasParents: 1
  lastHumanDescriptionAvatarSource: {instanceID: 0}
  autoGenerateAvatarMappingIfUnspecified: 1
  animationType: 0
  humanoidOversampling: 1
  avatarSetup: 0
  addHumanoidExtraRootOnlyWhenUsingAvatar: 1
  importBlendShapeDeformPercent: 1
  remapMaterialsIfMaterialImportModeIsNone: 0
  additionalBone: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

DEFAULT_META = """fileFormatVersion: 2
guid: %s
%sDefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write_meta(asset_path, kind):
    """Create the Unity .meta next to an asset once; an existing one keeps its GUID."""
    meta_path = asset_path + ".meta"
    if os.path.exists(meta_path):
        return
    guid = hashlib.md5(("geomodel-field-tools:" + os.path.basename(asset_path)).encode()).hexdigest()
    if kind == "model":
        text = MODEL_META % guid
    else:
        text = DEFAULT_META % (guid, "folderAsset: yes\n" if kind == "folder" else "")
    with open(meta_path, "w", newline="\n") as out:
        out.write(text)


def write_mtl(path):
    with open(path, "w", newline="\n") as out:
        out.write("# Generated by tools/art-pipeline/field-tools/build_models.py - do not edit by hand.\n")
        for name in sorted(MATERIALS):
            r, g, b = MATERIALS[name][:3]
            out.write("newmtl %s\nKd %.3f %.3f %.3f\n" % (name, r, g, b))


def main():
    repo = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
    out_dir = os.path.join(repo, "Assets", "Resources", "Models", "FieldTools")
    os.makedirs(out_dir, exist_ok=True)
    write_meta(os.path.dirname(out_dir), "folder")
    write_meta(out_dir, "folder")
    write_mtl(os.path.join(out_dir, "FieldTools.mtl"))
    write_meta(os.path.join(out_dir, "FieldTools.mtl"), "default")
    models = {}
    for builder in BUILDERS:
        model = builder()
        models[model.name] = model
        model.write_obj(os.path.join(out_dir, model.name + ".obj"), "FieldTools.mtl")
        write_meta(os.path.join(out_dir, model.name + ".obj"), "model")
        lo, hi = model.bounds()
        print("%-13s %5d tris  size %.3f x %.3f x %.3f" % ((model.name, model.triangle_count()) + tuple(hi - lo)))
    if "--preview" in sys.argv:
        import preview
        preview.render_all(models, MATERIALS, RIG, CORE_BOX, sys.argv[sys.argv.index("--preview") + 1])


if __name__ == "__main__":
    main()
