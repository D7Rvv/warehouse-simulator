"""Offline Blender CPU material/geometry review, separate from Godot/mobile QA."""
from pathlib import Path
import importlib.util
import bpy
from mathutils import Vector

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('warehouse_art',HERE/'generate_assets.py')
art=importlib.util.module_from_spec(spec);spec.loader.exec_module(art)
OUTPUT=HERE.parents[1]/'builds/mobile-art'
OUTPUT.mkdir(parents=True,exist_ok=True)


def look(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()


def add_parcel(offset):
    existing=set(bpy.context.scene.objects)
    art.parcel()
    for obj in set(bpy.context.scene.objects)-existing:obj.location+=Vector(offset)


def lighting():
    scene=bpy.context.scene
    scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=32
    scene.cycles.use_denoising=False
    scene.render.resolution_x=960;scene.render.resolution_y=540;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.world.color=(.16,.18,.20)
    for location,power,size in [((2,-4,7),1000,5),((-4,1,5),650,4),((1,5,6),900,3)]:
        data=bpy.data.lights.new('Review softbox','AREA');data.energy=power;data.shape='DISK';data.size=size
        obj=bpy.data.objects.new('Review softbox',data);scene.collection.objects.link(obj);obj.location=location;look(obj,(0,1,1))
    data=bpy.data.cameras.new('Asset review camera');camera=bpy.data.objects.new('Asset review camera',data)
    scene.collection.objects.link(camera);scene.camera=camera;data.lens=46
    return camera


def main():
    art.make_materials()
    art.clear_objects();art.inspection_bench();add_parcel((-.33,-.05,.955))
    art.box('Review ground',(0,0,-.11),(30,30,.20),'concrete')
    camera=lighting();camera.location=(3.3,-4.3,2.8);look(camera,(-.05,0,.90))
    bpy.context.scene.render.filepath=str(OUTPUT/'inspection_review.png');bpy.ops.render.render(write_still=True)
    art.clear_objects();art.truck()
    for x,y in [(-.66,.78),(.42,1.50),(-.36,2.60)]:add_parcel((x,y,1.155))
    art.box('Review ground',(0,2,-.11),(35,35,.20),'concrete')
    camera=lighting();camera.location=(5.1,-8.1,3.9);look(camera,(0,1.3,1.80))
    bpy.context.scene.render.filepath=str(OUTPUT/'inbound_review.png');bpy.ops.render.render(write_still=True)
    art.clear_objects();art.export_gate()
    add_parcel((.15,-.34,.875))
    existing=set(bpy.context.scene.objects);art.conveyor()
    for obj in set(bpy.context.scene.objects)-existing:obj.location+=Vector((0,-1.60,0))
    art.box('Review ground',(0,0,-.11),(30,30,.20),'concrete')
    camera=lighting();camera.location=(5.2,-8.0,3.8);look(camera,(0,-.1,1.72))
    bpy.context.scene.render.filepath=str(OUTPUT/'export_review.png');bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT/'export-review.blend'))
    print('OFFLINE_REVIEW_RENDERED 3views; Godot/device FPS/thermal NOT measured',flush=True)


if __name__=='__main__':main()
