# GPU 2D Animation Engine
Animation Engine that supports sprites and clip 2D animations  
Unity 6.3  
URP  
Demo path: Assets/_App/Demo00/Scenes/SampleScene  

## Workflow:
1. Create a TextureAnimationClip scriptable object instance
![TextureAnimationClip](readme/gpuanim_textureanimationclips.jpg "TextureAnimationClip")
2. Assign characters or animated prefabs to the TextureAnimationClip scriptable object instance
3. Press "Bake" button
4. A GPUAnimationData scriptable object instance will be created with all the necessary data to animate all prefabs, including textures.
![GPUAnimationData](readme/gpuanim_animationdata_textures.jpg "GPUAnimationData")
5. Add the RenderCtrl script to a gameObject of your scene.
6. Assign the GPUAnimationData scriptable object instance to the Animation Data fiels of the RenderCtrl script.

## Demos
GPUAnimationData custom inspector  
[![Anti tiling and tiles blending](https://img.youtube.com/vi/O4xx1kKgA30/0.jpg)](https://youtu.be/O4xx1kKgA30)  

50 and 3000 instances of animated characters  
[![Anti tiling and tiles blending](https://img.youtube.com/vi/WyjpnOaPc7E/0.jpg)](https://youtube.com/shorts/WyjpnOaPc7E)

