# opentk-multipass-demo

An example of using OpenGL framebuffers to perform post-processing with multiple shader passes using the .NET [OpenTK](https://github.com/opentk) library. This does use my [eyecandy](https://github.com/MV10/eyecandy) library, but only as a convenience to avoid duplicating the simple `Shader` class (basically it compiles the shaders and has a few helper functions to set attributes and uniforms).

Note this assumes at least OpenGL 4.6 availability (which means it'll work on Windows and Linux, but not Mac or low-spec devices like the Raspberry Pi.)

This demo is a stack of five frag shaders using three framebuffers. The vertex shader is a simple pair of triangles that covers the entire display window. The frag shaders are mostly adapted from Shadertoy content.

![image](https://github.com/MV10/opentk-multipass-demo/assets/794270/bb948f00-0bc4-4040-9ca0-082e005bdc3f)

By default all five passes are output. You can hit the spacebar to short-circuit the shader passes and output the results of each stage.

The first pass renders a colorful plasma field to framebuffer 0.

The second pass desaturates the plasma field (using the plasma texture in framebuffer 0 as an input uniform) and renders the results to framebuffer 1.

The third pass runs a Sobel edge-detection algorithim (using the desaturated texture in framebuffer 1 as an input) and renders the results to framebuffer 2.

The fourth pass applies a black-and-white semi-transparent cloudy effect to the edges of the image (using the edge-detection texture in framebuffer 2 as an input) and renders the results to framebuffer 1.

The fifth and final pass colorizes the image. It uses both the original plasma texture still in framebuffer 0 as the source of color data, and mixes it with the black and white texture in framebuffer 1 which has the combined edge-detection and cloudy effects. This is rendered to framebuffer 2.

Finally, framebuffer 2 is blitted to the default backbuffer, the buffers are swapped and the whole thing appears on-screen.

Huge thanks to _themixedupstuff_ and _BoyBayKiller_ on the OpenTK discord channel for helping me to understand how to juggle multiple framebuffers.
