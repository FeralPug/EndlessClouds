# EndlessClouds
Simple Endless Volumetric(ish) clouds for Unity

Download files and import them into Unity. Create a gameobject in your scene and attach the CloudsManager component to it. Assign the compute shader to the appropriate field. If a camera is not assigned to the viewer it will grab the main camera in the scene. You can create a material and assign the Clouds.shader to it. You can then assign this to the material field on the CloudsManager. It will find the shader if you do not, but builds might not include the shader, so best to assign it via a material.
Adjust the mesh settings via the CloudManager Chunk Settings field. Adjust the material settings via the Cloud Material Settings field.

![Example Screen Shot](https://github.com/FeralPug/EndlessClouds/blob/main/Examples/CloudsScreenShot.jpg?raw=true "Screen Shot")
