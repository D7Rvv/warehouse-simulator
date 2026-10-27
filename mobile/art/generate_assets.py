"""Original mobile warehouse props; execute with Blender 4.3.2 in background.

No reference-game mesh, photograph or texture is embedded. Geometry dimensions
are authored prototype estimates in meters. GLB is the runtime format: Blender
is a production tool, never a dependency of the Godot application.
"""
from pathlib import Path
import bpy
import math
import json
import hashlib
import random
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / 'assets/models'
TEXTURES = ROOT / 'assets/textures'
MODELS.mkdir(parents=True, exist_ok=True)
TEXTURES.mkdir(parents=True, exist_ok=True)


def color_material(name, color, rough=.6, metallic=0):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = rough
    bsdf.inputs['Metallic'].default_value = metallic
    return material


def textured_material(name, base_name, rough=.7, metallic=0):
    material = color_material(name, (1, 1, 1), rough, metallic)
    tree = material.node_tree
    tex = tree.nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(str(TEXTURES / (base_name + '_albedo.png')), check_existing=True)
    tree.links.new(tex.outputs['Color'], tree.nodes['Principled BSDF'].inputs['Base Color'])
    rough_texture = tree.nodes.new('ShaderNodeTexImage')
    rough_texture.image = bpy.data.images.load(str(TEXTURES / (base_name + '_roughness.png')), check_existing=True)
    rough_texture.image.colorspace_settings.name = 'Non-Color'
    tree.links.new(rough_texture.outputs['Color'], tree.nodes['Principled BSDF'].inputs['Roughness'])
    return material


M = {}


def make_materials():
    M.update({
        'concrete': textured_material('WH Concrete PBR', 'concrete', .9),
        'paper': textured_material('WH Kraft Paper PBR', 'kraft', .9),
        'rubber': textured_material('WH Rubber PBR', 'rubber', .86),
        'paint': textured_material('WH Blue Painted Steel PBR', 'blue_steel', .46, .35),
        'ivory': color_material('WH Warm Galvanized Sheet', (.68, .72, .70), .62, .35),
        'steel': color_material('WH Brushed Steel', (.39, .45, .46), .32, .8),
        'dark': color_material('WH Charcoal Metal', (.065, .085, .095), .64, .4),
        'yellow': color_material('WH Safety Ochre', (.91, .64, .085), .52, .18),
        'orange': color_material('WH Rack Orange', (.77, .22, .075), .50, .32),
        'wood': color_material('WH Pallet Timber', (.39, .27, .14), .95),
        'paperwhite': color_material('WH Label Stock', (.91, .89, .81), .92),
        'ink': color_material('WH Printed Ink', (.032, .047, .050), .92),
        'glass': color_material('WH Opaque Tinted Cab Glass', (.055, .135, .16), .24, .15),
        'screen': color_material('WH Standby LCD', (.035, .14, .16), .36),
        'green': color_material('WH Status Green', (.12, .45, .24), .45),
        'red': color_material('WH Tail Red', (.65, .045, .035), .30),
        'tape': color_material('WH Packing Tape', (.61, .44, .23), .48),
        'ao': color_material('WH Opaque Contact Detail', (.15, .16, .15), 1),
    })


def material(obj, key):
    obj.data.materials.append(M[key])
    return obj


def box(name, location, size, key='steel', bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    material(obj, key)
    if bevel:
        mod = obj.modifiers.new('Physical edge highlight', 'BEVEL')
        mod.width = min(bevel, min(size) * .22)
        mod.segments = 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod = obj.modifiers.new('Weighted plane normals', 'WEIGHTED_NORMAL')
        mod.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def cylinder(name, location, radius, depth, key='steel', vertices=14, axis='Z'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    if axis == 'X': obj.rotation_euler.y = math.pi/2
    if axis == 'Y': obj.rotation_euler.x = math.pi/2
    material(obj, key)
    for p in obj.data.polygons:
        p.use_smooth = len(p.vertices) == 4
    return obj


def beam(name, start, end, thickness=.03, key='steel'):
    midpoint = (Vector(start) + Vector(end)) / 2
    direction = Vector(end) - Vector(start)
    obj = cylinder(name, midpoint, thickness / 2, direction.length, key, 8)
    obj.rotation_euler = direction.to_track_quat('Z', 'Y').to_euler()
    return obj


def text(name, body, location, size=.12, key='ink', rotation=(math.pi/2, 0, 0)):
    bpy.ops.object.text_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = body
    obj.data.size = size
    obj.data.align_x = 'CENTER'
    obj.data.align_y = 'CENTER'
    obj.data.extrude = .0002
    obj.data.resolution_u = 2
    obj.data.materials.append(M[key])
    bpy.ops.object.convert(target='MESH')
    return bpy.context.object


def contact(location, scale):
    # Tiny opaque contact patches under feet, not a fullscreen alpha shadow.
    return box('Grounded contact detail', (*location[:2], .004), (scale[0], scale[1], .006), 'ao')


def bolts(centers, radius=.012, key='steel'):
    for center in centers:
        cylinder('Fastener', center, radius, .012, key, 8, axis='Y')


def clear_objects():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0: bpy.data.meshes.remove(mesh)


def join_by_material():
    groups = {}
    for obj in list(bpy.context.scene.objects):
        if obj.type != 'MESH': continue
        key = (obj.get('cutaway_group', ''), obj.data.materials[0].name)
        groups.setdefault(key, []).append(obj)
    parents = {}
    for (group, name), objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects: obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        if len(objects) > 1:
            bpy.ops.object.join()
        obj = bpy.context.object
        obj.name = ('CUTAWAY_RIGHT_' if group else '') + name.replace('WH ', '')
        if group:
            if group not in parents:
                parent = bpy.data.objects.new(group, None)
                bpy.context.scene.collection.objects.link(parent)
                parents[group] = parent
            obj.parent = parents[group]
        bpy.context.scene.cursor.location = (0, 0, 0)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    return [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']


def warehouse_shell():
    # Open front (+Godot Z) and roof are intentional camera cutaways.
    box('Concrete slab', (0, 0, -.10), (16.6, 14.6, .20), 'concrete')
    box('Yard apron', (0, 11.0, -.12), (16.6, 7.5, .20), 'concrete')
    # In-plane joints are narrow opaque inserts, no alpha overdraw.
    for x in range(-8, 9, 2): box('Slab joint', (x, 0, .002), (.014, 14.1, .005), 'ao')
    for y in range(-6, 8, 2): box('Slab joint', (0, y, .002), (16.1, .014, .005), 'ao')
    for x in (-8.15, 8.15):
        before_side = set(bpy.context.scene.objects)
        box('Side plinth', (x, 0, .32), (.24, 14.4, .64), 'concrete')
        box('Side sheet', (x, 0, 3.25), (.12, 14.3, 5.25), 'ivory')
        for y in range(-7, 8):
            box('Sheet standing seam', (x + (-.065 if x > 0 else .065), y, 3.22), (.032, .026, 5.1), 'steel')
        for y in (-6, -2, 2, 6):
            box('Column', (x + (-.15 if x > 0 else .15), y, 3.0), (.22, .24, 6.0), 'dark', .015)
            box('Base shoe', (x + (-.15 if x > 0 else .15), y, .07), (.38, .40, .14), 'steel')
        if x > 0:
            for part in set(bpy.context.scene.objects) - before_side:
                part['cutaway_group'] = 'CutawayRightWall'
    for x0, x1 in [(-8.2, -5.7), (-2.3, 3.3), (6.7, 8.2)]:
        box('Rear wall module', ((x0+x1)/2, 7.12, 3), (x1-x0, .18, 6), 'ivory')
        box('Rear concrete plinth', ((x0+x1)/2, 7.0, .32), (x1-x0, .27, .64), 'concrete')
    for x, width, opening in [(-4, 3.4, 4.3), (5, 3.4, 3.7)]:
        box('Opening header infill', (x, 7.12, (opening+6)/2), (width, .18, 6-opening), 'ivory')
        box('Dark structural lintel', (x, 6.98, opening+.09), (width+.20, .27, .18), 'dark', .008)
        for edge in (-width/2, width/2):
            box('Dock structural jamb', (x+edge, 6.97, opening/2), (.14, .28, opening), 'steel', .01)
            box('Yellow bollard', (x+edge, 6.60, .55), (.20, .20, 1.10), 'yellow', .035)
            box('Bollard dark stripe', (x+edge, 6.48, .66), (.205, .025, .18), 'dark')
        for stripe in range(8):
            obj=box('Dock threshold warning', (x-width/2+.18+stripe*.40, 6.50, .008), (.19, .48, .012), 'yellow')
            obj.rotation_euler.z=.45
    # Roller shutter is visibly rolled above the loading opening, not blocking it.
    cylinder('Shutter barrel', (-4, 7.02, 4.43), .19, 3.48, 'steel', 20, 'X')
    box('Shutter case', (-4, 7.06, 4.65), (3.66, .50, .28), 'dark', .018)
    box('Inbound sign', (-4, 6.86, 5.15), (2.6, .04, .48), 'paint', .01)
    text('Inbound label', 'INBOUND / 01', (-4, 6.83, 5.14), .24, 'paperwhite')
    # Exposed roof trusses preserve depth without obscuring the overview camera.
    for y in (-5.8, .0, 5.8):
        beam('Truss chord', (-7.9,y,5.85), (7.9,y,5.85), .10, 'dark')
        for x in range(-7, 8, 2):
            beam('Truss diagonal', (x-1,y,5.85), (x,y,6.42), .065, 'steel')
            beam('Truss diagonal', (x,y,6.42), (x+1,y,5.85), .065, 'steel')
        box('LED strip casing', (0,y,5.67), (4.8,.16,.08), 'dark')
        box('Diffuser', (0,y-.002,5.62), (4.6,.13,.018), 'paperwhite')
    for x in (-6, -2, 2, 6):
        box('Aisle edge marking', (x, -.5, .008), (.045, 10.8, .008), 'yellow')


def truck():
    box('Chassis frame', (0, 3.35, .72), (2.12, 6.5, .25), 'dark', .03)
    box('Cargo floor', (0, 2.55, 1.075), (2.52, 5.20, .15), 'steel', .012)
    for x in (-1.27,1.27):
        box('Cargo side outer', (x,2.55,2.36), (.085,5.2,2.43), 'ivory', .012)
        for y in (.22,.85,1.50,2.15,2.80,3.45,4.10,4.78):
            box('Cargo wall brace', (x+(.052 if x<0 else -.052),y,2.33), (.035,.045,2.30), 'steel')
        box('Cargo lower rub rail', (x+(.055 if x<0 else -.055),2.55,1.38), (.05,5.0,.09), 'dark')
    box('Cargo headboard', (0,5.13,2.35),(2.55,.10,2.45),'ivory',.012)
    # A roof plus rear raised shutter makes an actual loading enclosure.
    box('Cargo roof',(0,2.55,3.615),(2.61,5.3,.08),'ivory',.02)
    for x in (-1.29,1.29):
        box('Rear frame',(x,.005,2.38),(.10,.16,2.55),'steel',.01)
    box('Rear lintel',(0,.005,3.64),(2.64,.18,.12),'steel',.01)
    box('Tail bumper',(0,-.16,.69),(2.67,.20,.15),'dark',.022)
    box('Tail lift folded',(0,-.24,.99),(2.48,.16,.19),'steel',.012)
    for x in (-1.06,1.06):
        box('Tail red lamp',(x,-.275,.89),(.24,.035,.11),'red',.018)
    box('Cab lower',(0,6.10,1.30),(2.38,1.84,1.05),'paint',.12)
    box('Cab upper',(0,5.94,2.24),(2.32,1.53,1.00),'paint',.08)
    box('Windscreen',(0,6.724,2.36),(1.98,.025,.62),'glass',.025)
    for x in (-1.178,1.178):
        box('Side window',(x,6.0,2.39),(.022,1.00,.59),'glass',.02)
        box('Cab door joint',(x,5.82,1.65),(.024,.80,.012),'dark')
        box('Door handle',(x*1.02,5.49,1.93),(.04,.18,.045),'steel',.012)
        box('Mirror arm',(x*1.07,6.42,2.26),(.25,.04,.045),'dark')
        box('Mirror',(x*1.17,6.45,2.35),(.10,.18,.26),'dark',.025)
    box('Cab nose grille',(0,7.04,1.30),(1.40,.035,.32),'dark',.02)
    for x in (-.83,.83):
        box('Headlamp',(x,7.045,1.36),(.34,.04,.20),'paperwhite',.03)
    box('Front bumper',(0,7.04,.92),(2.39,.16,.23),'steel',.04)
    for y in (1.25,4.30,6.15):
        for x in (-1.17,1.17):
            cylinder('Tyre',(x,y,.48),.47,.32,'rubber',20,'X')
            cylinder('Rim',(x+(-.17 if x<0 else .17),y,.48),.26,.025,'steel',16,'X')
            cylinder('Wheel hub',(x+(-.19 if x<0 else .19),y,.48),.10,.05,'dark',12,'X')
            contact((x,y),(.42,.50))
    for x in (-1.05,1.05):
        box('Rear mudflap',(x,1.8,.25),(.32,.045,.37),'rubber')
    for y in (.75,1.3,1.85,2.4,2.95,3.5,4.05,4.6):
        box('Floor anti-slip rib',(0,y,1.157),(2.38,.015,.012),'dark')


def inspection_bench():
    box('Bench worktop',(0,0,.91),(2.8,1.25,.08),'ivory',.025)
    box('Worktop front lip',(0,-.625,.89),(2.85,.04,.12),'steel',.012)
    for x in (-1.18,1.18):
        for y in (-.43,.43):
            box('Bench leg',(x,y,.43),(.10,.10,.86),'steel',.012)
            box('Adjustable foot',(x,y,.035),(.16,.16,.07),'dark',.012)
            contact((x,y),(.19,.19))
    box('Lower shelf',(0,0,.27),(2.47,1.0,.045),'steel')
    box('Scale plinth',(.63,-.06,.98),(.65,.62,.06),'dark',.014)
    box('Scale brushed tray',(.63,-.06,1.03),(.61,.58,.05),'steel',.018)
    box('Scale display case',(.65,.46,1.16),(.53,.11,.26),'dark',.018)
    box('Scale LCD',(.65,.399,1.18),(.44,.02,.14),'screen')
    text('Scale not measuring','---- g',(.65,.384,1.18),.09,'paperwhite')
    box('Inspection backboard',(-.55,.55,1.26),(1.27,.06,.60),'paint',.012)
    box('Document tray',(-.94,.12,.973),(.45,.55,.04),'dark',.012)
    for i in range(3):box('Paper sheets',(-.94,.1,.996+i*.004),(.37,.43,.004),'paperwhite')
    box('Scanner body',(-.03,.22,1.02),(.18,.26,.12),'dark',.025)
    box('Scanner lens',(-.03,.075,1.03),(.13,.025,.045),'red')
    cylinder('Lamp upright',(-1.15,.46,1.35),.027,.78,'steel',12)
    box('Task lamp hood',(-.83,.46,1.77),(.72,.20,.10),'dark',.028)
    box('Task lamp diffuser',(-.83,.46,1.714),(.61,.15,.012),'paperwhite')
    text('Inspection sign','INSPECTION',(-.55,.507,1.39),.13,'paperwhite')


def conveyor():
    box('Belt body',(0,0,.77),(.72,.99,.12),'dark',.012)
    box('Rubber belt',(0,0,.838),(.64,.97,.024),'rubber',.006)
    for x in (-.41,.41):
        box('Blue side channel',(x,0,.78),(.10,1.0,.20),'paint',.018)
        box('Guard rail',(x,0,.92),(.035,.98,.025),'steel',.006)
    for y in (-.40,.40):
        cylinder('Drive roller',(0,y,.78),.085,.75,'steel',16,'X')
        for x in (-.31,.31):
            box('Support leg',(x,y,.36),(.065,.075,.70),'steel',.01)
            box('Foot shoe',(x,y,.025),(.15,.17,.05),'dark',.007)
            contact((x,y),(.17,.19))
    for y in (-.44,-.22,0,.22,.44):
        for x in (-.467,.467):
            cylinder('Frame bolt',(x,y,.79),.015,.018,'steel',8,'X')
    box('Motor drive cover',(.45,.28,.65),(.10,.26,.19),'paint',.025)
    box('Control stop housing',(-.45,-.22,.87),(.08,.14,.11),'dark',.012)
    cylinder('Stop button',(-.45,-.22,.944),.035,.03,'red',12)
    # Low-profile original flow markers on top (Blender+Y = Godot-Z).
    for y in (-.19,.09):
        left=box('Flow chevron',(-.075,y,.855),(.035,.22,.004),'yellow')
        left.rotation_euler.z=-.65
        right=box('Flow chevron',(.075,y,.855),(.035,.22,.004),'yellow')
        right.rotation_euler.z=.65


def fence_panel(x, y, width, height=1.25):
    for edge in (-width/2,width/2):
        box('Guard fence post',(x+edge,y,height/2),(.06,.07,height),'yellow',.008)
    for z in (.14,height-.03):box('Fence rail',(x,y,z),(width,.05,.05),'dark')
    # Opaque sparse bars: no transparent fence layers or giant alpha texture.
    n=max(2,int(width/.18))
    for i in range(n):
        px=x-width/2+.10+i*(width-.20)/max(1,n-1)
        box('Fence vertical wire',(px,y,.67),(.012,.022,1.03),'steel')
    for z in (.40,.67,.94):box('Fence horizontal wire',(x,y,z),(width-.1,.022,.012),'steel')


def export_gate():
    for x in (-1.5,1.5):
        box('Gate column',(x,.65,1.76),(.19,.28,3.52),'paint',.025)
        box('Gate foot',(x,.65,.06),(.34,.44,.12),'steel',.015)
        contact((x,.65),(.40,.50))
        box('Bumper protector',(x,.455,.62),(.22,.09,1.07),'yellow',.022)
    box('Gate header',(0,.65,3.43),(3.18,.33,.30),'paint',.025)
    box('Original gate sign',(0,.453,3.45),(2.50,.04,.45),'dark',.018)
    text('Gate title','AIR / EXPORT 01',(0,.424,3.45),.24,'paperwhite')
    for x in (-1.48,1.48):
        box('Gate jamb lamp',(x,.44,2.70),(.045,.04,.21),'green',.008)
    # Recess behind the open threshold reads as an industrial receiving chute.
    box('Dark chute back',(0,1.08,1.48),(2.78,.12,2.8),'dark')
    for x in (-1.31,1.31):box('Inner chute rail',(x,.83,1.15),(.045,.55,2.0),'steel')
    for x in (-1.36,1.36):
        # Side returns rotate bars so their narrow profile doesn't become a wall.
        for y in (-.78,-.41,-.04):
            box('Safety barrier upright',(x,y,.65),(.055,.055,1.3),'yellow',.008)
        box('Safety barrier top',(x,-.40,1.28),(.055,.86,.055),'dark')
        for i in range(6):box('Safety barrier infill',(x,-.78+i*.145,.67),(.016,.016,1.10),'steel')
    fence_panel(-1.10,-.80,.62)
    fence_panel(1.10,-.80,.62)
    box('Gate belt',(0,-.05,.77),(1.40,1.95,.15),'paint',.02)
    box('Gate rubber',(0,-.05,.855),(1.24,1.91,.025),'rubber',.008)
    for x in (-.58,.58):
        for y in (-.75,.65):
            box('Gate belt support',(x,y,.37),(.075,.10,.74),'steel',.01)
    box('Operator console',(1.1,-.52,1.50),(.47,.16,.42),'dark',.025)
    box('Operator LCD',(1.1,-.613,1.57),(.35,.025,.20),'screen')
    text('Read only console','DEMO',(1.1,-.632,1.57),.08,'paperwhite')


def rack():
    for x in (-1.30,1.30):
        for y in (-.48,.48):
            box('Rack upright',(x,y,1.6),(.085,.085,3.2),'paint',.012)
            box('Rack baseplate',(x,y,.035),(.20,.20,.07),'steel',.008)
            contact((x,y),(.23,.23))
        for z in (.45,1.35,2.25,3.13):
            beam('Upright lateral',(x,-.46,z),(x,.46,z),.035,'steel')
        for z in (.45,1.35,2.25):
            beam('Rack bracing',(x,-.45,z),(x,.45,z+.86),.035,'steel')
    for z in (.28,1.3,2.32):
        for y in (-.48,.48):box('Rack orange beam',(0,y,z),(2.68,.09,.14),'orange',.01)
        box('Rack deck',(0,0,z+.075),(2.60,.93,.025),'steel')
    box('Rack identifier',(0,-.535,2.32),(.48,.02,.12),'paperwhite')
    text('Rack sign','RACK 01',(0,-.551,2.32),.075,'ink')


def pallet():
    for x in (-.33,0,.33):box('Pallet runner',(x,0,.04),(.10,1.20,.08),'wood',.006)
    for y in (-.49,0,.49):
        for x in (-.33,0,.33):box('Pallet block',(x,y,.085),(.12,.14,.09),'wood',.006)
    for i in range(6):box('Pallet top board',(0,-.52+i*.205,.14),(.80,.14,.035),'wood',.004)


def parcel():
    box('Corrugated box',(0,0,.225),(.62,.48,.45),'paper',.008)
    box('Top seam',(0,0,.453),(.012,.466,.003),'dark')
    box('Top packing tape',(0,0,.456),(.10,.476,.006),'tape')
    box('Tape front',(0,-.243,.36),(.10,.007,.18),'tape')
    box('Tape back',(0,.243,.36),(.10,.007,.18),'tape')
    box('Original blank shipping stock',(.13,-.245,.245),(.225,.004,.15),'paperwhite')
    # Barcode is abstract linework, never a copied real shipping label.
    rng=random.Random(2042)
    cursor=.037
    while cursor<.222:
        width=rng.choice([.002,.004,.006])
        box('Abstract barcode', (cursor,-.248,.222),(width,.002,.052),'ink')
        cursor+=width+rng.choice([.003,.005])
    text('Prototype label', 'SAMPLE',(.13,-.249,.279),.025,'ink')
    for x in (-.303,.303):
        box('Fold seam',(x,-.238,.20),(.006,.005,.36),'tape')


BUILDERS = {
    'warehouse_shell': warehouse_shell, 'truck': truck,
    'inspection_bench': inspection_bench, 'conveyor': conveyor,
    'export_gate': export_gate, 'rack': rack, 'pallet': pallet, 'parcel': parcel,
}


def export_one(name, builder):
    clear_objects()
    builder()
    objects=join_by_material()
    triangles=0
    dimensions=[]
    for obj in objects:
        obj.data.calc_loop_triangles()
        triangles+=len(obj.data.loop_triangles)
        dimensions.extend(obj.matrix_world @ Vector(v) for v in obj.bound_box)
    mins=[min(v[i] for v in dimensions) for i in range(3)]
    maxs=[max(v[i] for v in dimensions) for i in range(3)]
    if name == 'conveyor':
        assert mins[0] >= -.5001 and maxs[0] <= .5001 and mins[1] >= -.5001 and maxs[1] <= .5001, 'Conveyor must fit one preview grid cell.'
    path=MODELS/(name+'.glb')
    bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',
        export_yup=True,export_apply=True,export_materials='EXPORT',
        export_cameras=False,export_lights=False,export_animations=False,
        export_extras=True,export_texcoords=True,export_normals=True,
        export_image_format='AUTO',export_keep_originals=False)
    return {
        'file':path.relative_to(ROOT).as_posix(),'triangles':triangles,
        'merged_meshes':len(objects),'estimated_material_surfaces':len(objects),
        'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
        'blender_bounds_min':mins,'blender_bounds_max':maxs,
        'runtime_axes':'glTF Y-up, -Z into prop; camera looks from +Z',
        'pivot':'ground center; truck rear opening at z0, extends -Z',
    }


def main():
    make_materials()
    stats={name:export_one(name,builder) for name,builder in BUILDERS.items()}
    manifest={
        'version':1,'generator':'art/generate_assets.py','blender_version':bpy.app.version_string,
        'source_status':'P_ORIGINAL_ART','reference_match_verified':False,
        'camera_cutaway':'warehouse_shell: CutawayRightWall node / CUTAWAY_RIGHT_ mesh prefix contains right wall, plinth and supports; hide only overview/build, keep close-view enclosure.',
        'source':'Original procedural meshes and seeded textures, authored from written industrial design requirements. No reference-game assets or photographs used.',
        'dimensions':'Prototype engineering estimates in meters; not a measured real vehicle or a reference-game extraction.',
        'license':'Original generated project art; no external image or mesh content.',
        'text_font':'Blender built-in Bfont shapes; sign text/layout original, no reference-game typography copied.',
        'quality_status':'Generated and exported; Godot multi-view rendering and phone quality/performance acceptance are separate.',
        'mobile_design':['Merged by material per prop','No skinned meshes or per-prop Rigidbody','Opaque metal-bar safety guards instead of stacked alpha fencing','Shared named materials across props','512x512 original texture maps','Transparent construction proxy generated separately at runtime'],
        'models':stats,'unique_model_triangles':sum(s['triangles'] for s in stats.values()),
        'model_bytes':sum(s['bytes'] for s in stats.values()),
        'textures':{p.name:{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(TEXTURES.glob('*.png'))},
    }
    (ROOT/'assets/ART_MANIFEST.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('WAREHOUSE_ASSETS_EXPORTED '+json.dumps({'models':len(stats),'triangles':manifest['unique_model_triangles'],'bytes':manifest['model_bytes']}),flush=True)


if __name__=='__main__':main()
