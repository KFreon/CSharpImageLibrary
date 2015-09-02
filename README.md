# CSharpImageLibrary
Image processing library written entirely in C#.
This library uses built in Windows codecs as much as possible. This includes the new fancy Windows 8.1+ codecs for DXT1, 3, and 5 loading.
For those not on Windows 8.1+, standard GDI+ codecs are used (System.Drawing.Bitmap)

This library is INCOMPLETE. All I currently plan on doing with this is loading major image formats (mainly DDS) and saving/converting them including mipmapping.

Current Features
====
- Supported formats: DXT1, 3, 5 (maybe 2 and 4 as well) [Otherwise known as BC1,2,3,4,5], V8U8, G8/L8, ATI1, ATI2/3Dc, ARGB, jpg, png, bmp.   
- Reads mips and uses them when saving.
- Load and save any of the supported formats with, or without, mipmaps.
- Access to Pixel data (RGBA) for all mipmap levels


Performance of this toolset is of some concern currently (details below), however I'm trying to get it all working first before major optimisation.

Overall results for loading (in Debug mode)
---
![None](http://s12.postimg.org/gw5rjaoql/Image_Engine_Loading_Results.jpg "Overall results for loading (in debug mode)")
