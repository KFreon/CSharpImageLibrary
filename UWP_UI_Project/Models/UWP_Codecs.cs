using CSharpImageLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using static CSharpImageLibrary.ImageFormats;

namespace UWP_UI_Project.Models
{
    public static class UWP_Codecs
    {
        public static async Task<List<MipMapBase>> LoadFromStream(Stream stream, uint decodeWidth, uint decodeHeight, double scale, ImageEngineFormatDetails loadedFormatDetails)
        {
            bool alternateDecodeDimensions = decodeHeight != 0 || decodeWidth != 0 || scale != 0;
            List<MipMapBase> mipmaps = new List<MipMapBase>();

            stream.Seek(0, SeekOrigin.Begin);
            

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
            uint currentWidth = alternateDecodeDimensions ? decodeWidth : decoder.OrientedPixelWidth;
            uint currentHeight = alternateDecodeDimensions ? decodeHeight : decoder.OrientedPixelHeight;

            if (scale != 0)
            {
                currentWidth = (uint)(decoder.OrientedPixelWidth * scale);
                currentHeight = (uint)(decoder.OrientedPixelHeight * scale);
            }

            for (uint i = 0; i < decoder.FrameCount; i++)
            {
                var frame = await decoder.GetFrameAsync(i);
                var pixelDataProvider = await frame.GetPixelDataAsync(frame.BitmapPixelFormat, frame.BitmapAlphaMode,
                    new BitmapTransform() { InterpolationMode = BitmapInterpolationMode.Cubic, ScaledHeight = currentHeight, ScaledWidth = currentWidth }, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.ColorManageToSRgb);
                var pixels = pixelDataProvider.DetachPixelData();

                mipmaps.Add(new MipMap(pixels, (int)currentWidth, (int)currentHeight, loadedFormatDetails));

                currentHeight /= 2;
                currentWidth /= 2;
            }

            return mipmaps;
        }
    }
}
