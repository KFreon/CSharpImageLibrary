# CSharpImageLibrary
Image processing library written entirely in C# and Nuget Packages.
This library uses built in Windows codecs as much as possible. This includes the new fancy Windows 8.1+ codecs for DXT1, 3, and 5 loading.
For those not on Windows 8.1+, manual codecs are used.


[Nuget Package](https://www.nuget.org/packages/CSharpImageLibrary/)

Current Features
====
- Supported formats: DXT1-5 [Otherwise known as BC1,2,3], V8U8, G8/L8, ATI1, ATI2/3Dc [Otherwise known as BC4 and 5], ARGB (and variants e.g. ABGR), jpg, png, bmp, tga, gif, TIFF.   
- Reads mips and uses them when saving.
- Load and save any of the supported formats with, or without, mipmaps.
- Access to Pixel data (RGBA) for all mipmap levels.
- Speed seems pretty good.
- Easy Object Oriented usage.
- Library (DLL) or UI versions available (UI only on Git, Library on Nuget)

Planned Features
===
- GPU Acceleration (UPDATE: Failed in V4.0. Will try again later)

Usage
===
ImageEngineImage img = new ImageEngineImage("path to image file", 512)  // Loads image at a max dimension of 512. e.g. a 1024x2048 image would be loaded in as a 256x512 image.
BitmapImage bitmap = new BitmapImage(img.MipMaps[0]);   // Pseudo code of course, but the mipmap is raw pixels, and needs to be encoded to something else.


NEW TESTS!!

Performance Results in Release Mode, working on a 2k texture with Alpha and Mipmaps.
---
![None](http://i.imgur.com/4Q39A0D.jpg "Performance Results in Release Mode, working on a 2k texture with Alpha and Mipmaps.")


---
THIS IS OLD NOW.
Overall results for loading (in Debug mode)

---
![None](http://s22.postimg.org/a35l8rz01/Capture.jpg "Overall results for loading (in debug mode)")

