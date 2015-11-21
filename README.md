# CSharpImageLibrary
Image processing library written entirely in C#.
This library uses built in Windows codecs as much as possible. This includes the new fancy Windows 8.1+ codecs for DXT1, 3, and 5 loading.
For those not on Windows 8.1+, standard GDI+ codecs are used (System.Drawing.Bitmap)


[Nuget Package](https://www.nuget.org/packages/CSharpImageLibrary/)

Current Features
====
- Supported formats: DXT1, 3, 5 (maybe 2 and 4 as well) [Otherwise known as BC1,2,3,4,5], V8U8, G8/L8, ATI1, ATI2/3Dc, ARGB, jpg, png, bmp.   
- Reads mips and uses them when saving.
- Load and save any of the supported formats with, or without, mipmaps.
- Access to Pixel data (RGBA) for all mipmap levels.
- Speed seems pretty good.
- Easy Object Oriented usage.

Usage
===
ImageEngineImage img = new ImageEngineImage("path to image file", 512)  // Loads image at a max dimension of 512. e.g. a 1024x2048 image would be loaded in as a 256x512 image.
BitmapImage bitmap = new BitmapImage(img.MipMaps[0]);   // Pseudo code of course, but the mipmap is raw pixels, and needs to be encoded to something else.


Overall results for loading (in Debug mode)
---
![None](http://s22.postimg.org/a35l8rz01/Capture.jpg "Overall results for loading (in debug mode)")

