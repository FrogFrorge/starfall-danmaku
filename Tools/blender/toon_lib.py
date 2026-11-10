"""Small toolkit for building cel-shaded anime characters in Blender from Python.
Imported by build_chars.py (run inside Blender)."""
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
TEX = os.path.join(HERE, "tex")
FACE_W = 1.9  # keep in sync with make_faces.py


# ----------------------------------------------------------------------------------------
# Colour helpers
# ----------------------------------------------------------------------------------------
def lin(c):
    """sRGB 0-255 tuple -> linear RGBA"""
    def f(v):
        v /= 255.0
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    return (f(c[0]), f(c[1]), f(c[2]), 1.0)


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3])


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


# ----------------------------------------------------------------------------------------
# Scene
# ----------------------------------------------------------------------------------------
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _mats.clear()
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.film_transparent = True
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    world = bpy.data.worlds.new("World")
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[1].default_value = 0.0
    sc.world = world
    try:
        sc.eevee.taa_render_samples = 64
    except Exception:
        pass
    return sc


def add_camera(center_z, ortho_scale, yaw_deg=0.0, pitch_deg=0.0, dist=30.0):
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho_scale
    cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.collection.objects.link(cam)
    yaw = math.radians(yaw_deg)
    pitch = math.radians(pitch_deg)
    d = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = Vector((0, 0, center_z)) + d * dist
    cam.rotation_euler = (math.radians(90) - pitch, 0, yaw)
    bpy.context.scene.camera = cam
    return cam


def add_sun(rot=(50, 0, -35), strength=math.pi, angle_deg=1.0):
    ld = bpy.data.lights.new("Sun", "SUN")
    ld.energy = strength
    ld.angle = math.radians(angle_deg)
    ld.use_shadow = False
    sun = bpy.data.objects.new("Sun", ld)
    bpy.context.collection.objects.link(sun)
    sun.rotation_euler = tuple(math.radians(v) for v in rot)
    return sun


def render(path, w, h, pct=100):
    sc = bpy.context.scene
    sc.render.resolution_x = w
    sc.render.resolution_y = h
    sc.render.resolution_percentage = pct
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("rendered", path)


# ----------------------------------------------------------------------------------------
# Materials
# ----------------------------------------------------------------------------------------
_mats = {}
OUTLINE_SCALE = [1.0]


def toon(name, base, shadow=None, high=None, shadow_at=0.32, high_at=0.93, rim=None):
    """Cel material: constant colour ramp driven by diffuse light. Colours are sRGB 0-255."""
    key = name
    if key in _mats:
        return _mats[key]
    shadow = shadow or shade(base, 0.78)
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    bw = nt.nodes.new("ShaderNodeRGBToBW")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    cr = ramp.color_ramp
    cr.interpolation = "CONSTANT"
    cr.elements[0].position = 0.0
    cr.elements[0].color = lin(shadow)
    cr.elements[1].position = shadow_at
    cr.elements[1].color = lin(base)
    if high:
        e = cr.elements.new(high_at)
        e.color = lin(high)
    emit = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(diff.outputs[0], s2r.inputs[0])
    nt.links.new(s2r.outputs[0], bw.inputs[0])
    nt.links.new(bw.outputs[0], ramp.inputs[0])
    if rim:
        lw = nt.nodes.new("ShaderNodeLayerWeight")
        lw.inputs[0].default_value = 0.35
        rr = nt.nodes.new("ShaderNodeValToRGB")
        rr.color_ramp.interpolation = "CONSTANT"
        rr.color_ramp.elements[0].color = (0, 0, 0, 1)
        rr.color_ramp.elements[1].position = 0.62
        rr.color_ramp.elements[1].color = (1, 1, 1, 1)
        mixn = nt.nodes.new("ShaderNodeMix")
        mixn.data_type = "RGBA"
        mixn.blend_type = "SCREEN"
        nt.links.new(lw.outputs["Facing"], rr.inputs[0])
        nt.links.new(rr.outputs[0], mixn.inputs[0])
        nt.links.new(ramp.outputs[0], mixn.inputs[6])
        mixn.inputs[7].default_value = lin(rim)
        mixn.inputs[0].default_value = 0.0
        # scale fac by rim strength
        mul = nt.nodes.new("ShaderNodeMath")
        mul.operation = "MULTIPLY"
        mul.inputs[1].default_value = 0.35
        nt.links.new(rr.outputs[0], mul.inputs[0])
        nt.links.new(mul.outputs[0], mixn.inputs[0])
        nt.links.new(mixn.outputs[2], emit.inputs[0])
    else:
        nt.links.new(ramp.outputs[0], emit.inputs[0])
    nt.links.new(emit.outputs[0], out.inputs[0])
    _mats[key] = m
    return m


def flat(name, color, alpha=1.0):
    if name in _mats:
        return _mats[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs[0].default_value = lin(color)
    nt.links.new(emit.outputs[0], out.inputs[0])
    _mats[name] = m
    return m


def outline_mat(color):
    name = "Outline_%d_%d_%d" % tuple(color[:3])
    if name in _mats:
        return _mats[name]
    m = flat(name, color)
    m.use_backface_culling = True
    _mats[name] = m
    return m


def image_mat(name, path, unlit=True):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(path)
    tex.interpolation = "Cubic"
    tex.extension = "CLIP"
    emit = nt.nodes.new("ShaderNodeEmission")
    tr = nt.nodes.new("ShaderNodeBsdfTransparent")
    mx = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(tex.outputs["Color"], emit.inputs[0])
    nt.links.new(tex.outputs["Alpha"], mx.inputs[0])
    nt.links.new(tr.outputs[0], mx.inputs[1])
    nt.links.new(emit.outputs[0], mx.inputs[2])
    nt.links.new(mx.outputs[0], out.inputs[0])
    try:
        m.surface_render_method = "BLENDED"
    except Exception:
        pass
    m.use_backface_culling = True
    return m


# ----------------------------------------------------------------------------------------
# Object helpers
# ----------------------------------------------------------------------------------------
def link(obj, parent=None):
    if obj.name not in bpy.context.collection.objects:
        bpy.context.collection.objects.link(obj)
    if parent:
        obj.parent = parent
    return obj


def set_mat(obj, mat, outline=None, thick=0.02):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    if outline is not None and thick > 0:
        obj.data.materials.append(outline_mat(outline))
        mod = obj.modifiers.new("Outline", "SOLIDIFY")
        mod.thickness = thick * OUTLINE_SCALE[0]
        mod.offset = 1.0
        mod.use_flip_normals = True
        mod.use_rim = False
        mod.material_offset = 1
    return obj


def smooth_shade(obj):
    for p in obj.data.polygons:
        p.use_smooth = True


def mesh_from_bm(name, bm):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    link(obj)
    return obj


def uv_sphere(name, loc=(0, 0, 0), scale=(1, 1, 1), seg=48, rings=32, deform=None):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    for v in bm.verts:
        co = v.co.copy()
        if deform:
            co = deform(co)
        v.co = Vector((co.x * scale[0], co.y * scale[1], co.z * scale[2]))
    obj = mesh_from_bm(name, bm)
    obj.location = loc
    smooth_shade(obj)
    return obj


def lathe(name, profile, loc=(0, 0, 0), seg=40, sx=1.0, sy=1.0, open_front=0.0, deform=None):
    """Surface of revolution around Z. profile = [(radius, z), ...] top to bottom.
    open_front > 0 removes faces facing -Y within that angle (degrees half-width) -> open coat."""
    bm = bmesh.new()
    rings = []
    for (r, z) in profile:
        ring = []
        for i in range(seg):
            a = 2 * math.pi * i / seg
            co = Vector((math.sin(a) * r * sx, -math.cos(a) * r * sy, z))
            if deform:
                co = deform(co, a)
            ring.append(bm.verts.new(co))
        rings.append(ring)
    for k in range(len(rings) - 1):
        for i in range(seg):
            a = 2 * math.pi * (i + 0.5) / seg
            ang = math.degrees(a)
            if open_front > 0 and (ang < open_front or ang > 360 - open_front):
                continue
            j = (i + 1) % seg
            bm.faces.new((rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]))
    obj = mesh_from_bm(name, bm)
    obj.location = loc
    smooth_shade(obj)
    return obj


def strand(name, pts, r0, r1=0.0, power=0.9, flat_axis=None, flat=1.0, res=10, bevel_res=3):
    """Tapered tube along a smooth bezier through pts. Returns a mesh object."""
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = 1.0
    cu.bevel_resolution = bevel_res
    cu.resolution_u = res
    cu.use_fill_caps = True
    sp = cu.splines.new("BEZIER")
    sp.bezier_points.add(len(pts) - 1)
    n = len(pts)
    for i, p in enumerate(pts):
        bp = sp.bezier_points[i]
        bp.co = Vector(p)
        bp.handle_left_type = "AUTO"
        bp.handle_right_type = "AUTO"
        t = i / (n - 1)
        bp.radius = r1 + (r0 - r1) * (1 - t) ** power
    obj = bpy.data.objects.new(name, cu)
    link(obj)
    bpy.context.view_layer.objects.active = obj
    for o in bpy.context.selected_objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.view_layer.objects.active
    if flat_axis is not None and flat != 1.0:
        # flatten along an axis around the strand's centroid
        me = obj.data
        c = sum((v.co for v in me.vertices), Vector()) / max(1, len(me.vertices))
        ax = Vector(flat_axis).normalized()
        for v in me.vertices:
            d = v.co - c
            v.co = v.co - ax * d.dot(ax) * (1 - flat)
    smooth_shade(obj)
    return obj


def box(name, size, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.0):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    obj = mesh_from_bm(name, bm)
    obj.location = loc
    obj.rotation_euler = tuple(math.radians(r) for r in rot)
    if bevel > 0:
        m = obj.modifiers.new("Bevel", "BEVEL")
        m.width = bevel
        m.segments = 3
    return obj


def face_decal(name, head, image_path, center, width=FACE_W):
    """Subdivided plane in front of the head, shrink-wrapped onto it, textured with the face."""
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=48, y_segments=48, size=width / 2)
    uv = bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        for l in f.loops:
            co = l.vert.co
            l[uv].uv = (co.x / width + 0.5, co.y / width + 0.5)
    # grid lies in XY facing +Z -> rotate to XZ facing -Y
    for v in bm.verts:
        x, y, z = v.co
        v.co = Vector((x, -3.0, y))
    obj = mesh_from_bm(name, bm)
    obj.location = Vector(center)
    obj.data.materials.append(image_mat(name + "_mat", image_path))
    sw = obj.modifiers.new("Wrap", "SHRINKWRAP")
    sw.target = head
    sw.wrap_method = "PROJECT"
    sw.use_project_y = True
    sw.use_negative_direction = True
    sw.use_positive_direction = True
    sw.offset = 0.006
    return obj


def parent_all(root, objs):
    for o in objs:
        if o.parent is None and o != root:
            o.parent = root
