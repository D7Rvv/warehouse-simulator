#!/usr/bin/env python3
"""Seeded original 512px material maps. Pillow/NumPy, no downloaded artwork."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

ROOT=Path(__file__).resolve().parents[1]/'assets/textures'
ROOT.mkdir(parents=True,exist_ok=True)
SIZE=512
rng=np.random.default_rng(71027)


def save(name,base,noise,roughness):
    rgb=np.clip(np.asarray(base)[None,None,:]+noise[:,:,None],0,255).astype('uint8')
    Image.fromarray(rgb).save(ROOT/(name+'_albedo.png'),optimize=True)
    r=np.clip(roughness+noise*.30,0,255).astype('uint8')
    Image.fromarray(r).save(ROOT/(name+'_roughness.png'),optimize=True)


fine=rng.normal(0,3.5,(SIZE,SIZE))
coarse=np.asarray(Image.fromarray(rng.integers(0,255,(32,32),dtype=np.uint8)).resize((SIZE,SIZE)).filter(ImageFilter.GaussianBlur(10)),dtype=float)/255-.5
save('concrete',[139,143,137],fine+coarse*18,225)
paper=rng.normal(0,2.1,(SIZE,SIZE))
paper+=np.sin(np.arange(SIZE)[:,None]*1.9)*1.5
save('kraft',[151,111,66],paper,232)
rubber=rng.normal(0,1.7,(SIZE,SIZE))
rubber[::12,:]-=4
save('rubber',[38,43,45],rubber,217)
paint=rng.normal(0,1.9,(SIZE,SIZE))
for y in range(9,SIZE,71):paint[y:y+1,5:80]-=3
save('blue_steel',[39,86,111],paint,136)
print('ORIGINAL_TEXTURES_READY 8 maps at512x512')
