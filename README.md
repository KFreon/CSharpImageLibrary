# CSharpImageLibrary
Image processing library written entirely in C#.
This library uses built in Windows codecs as much as possible. This includes the new fancy Windows 8.1+ codecs for DXT1, 3, and 5 loading.
For those not on Windows 8.1+, standard GDI+ codecs are used (System.Drawing.Bitmap)

This library is INCOMPLETE. All I currently plan on doing with this is loading major image formats (mainly DDS) and saving/converting them including mipmapping.


Performance of this toolset is of some concern currently (details below), however I'm trying to get it all working first before major optimisation.

Combined Colours to indicate absolute worst performance.
---
![None](https://dl.dropboxusercontent.com/u/37301843/ImageEngine%20Test%20Combined%20Colours.jpg "Combined Colours to indicate absolute worst performance.")

Individual Colours to indicate worst performance per Format.
---
![None](https://dl.dropboxusercontent.com/u/37301843/ImageEngine%20Test%20Individual%20Colours.jpg "Individual Colours to indicate worst performance per Format.")
