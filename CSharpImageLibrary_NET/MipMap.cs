using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSharpImageLibrary;
using UsefulThings;
using static CSharpImageLibrary.ImageFormats;

namespace CSharpImageLibrary_NET
{
    public class MipMap : MipMapBase
    {
        public MipMap()
        {

        }

        public MipMap(byte[] pixels, int width, int height, ImageEngineFormatDetails details)
            : base(pixels, width, height, details)
        {

        }

        public override MipMapBase Resize(double xScale, double yScale)
        {
            // KFreon: Only do the alpha bit if there is any alpha. Git #444 (https://github.com/ME3Explorer/ME3Explorer/issues/444) exposed the issue where if there isn't alpha, it overruns the buffer.
            bool alphaPresent = IsAlphaPresent;

            WriteableBitmap alpha = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgr32, null);
            if (alphaPresent)// && !mergeAlpha)
            {
                // Pull out alpha since scaling with alpha doesn't work properly for some reason
                try
                {
                    unsafe
                    {
                        alpha.Lock();
                        int index = 3;
                        byte* alphaPtr = (byte*)alpha.BackBuffer.ToPointer();
                        for (int i = 0; i < Width * Height * 4; i += 4)
                        {
                            // Set all pixels in alpha to value of alpha from original image - otherwise scaling will interpolate colours
                            alphaPtr[i] = Pixels[index];
                            alphaPtr[i + 1] = Pixels[index];
                            alphaPtr[i + 2] = Pixels[index];
                            alphaPtr[i + 3] = Pixels[index];
                            index += 4;
                        }

                        alpha.Unlock();
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.ToString());
                    throw;
                }
            }

            var bmp = UsefulThings.WPF.Images.CreateWriteableBitmap(Pixels, Width, Height);
            FormatConvertedBitmap main = new FormatConvertedBitmap(bmp, PixelFormats.Bgr32, null, 0);

            // Scale RGB
            ScaleTransform scaletransform = new ScaleTransform(xScale, yScale);
            TransformedBitmap scaledMain = new TransformedBitmap(main, scaletransform);

            int newWidth = (int)(Width * xScale);
            int newHeight = (int)(Height * yScale);
            int newStride = (int)(newWidth * 4);

            // Put alpha back in
            FormatConvertedBitmap newConv = new FormatConvertedBitmap(scaledMain, PixelFormats.Bgra32, null, 0);
            WriteableBitmap resized = new WriteableBitmap(newConv);

            if (alphaPresent)// && !mergeAlpha)
            {
                TransformedBitmap scaledAlpha = new TransformedBitmap(alpha, scaletransform);
                WriteableBitmap newAlpha = new WriteableBitmap(scaledAlpha);

                try
                {
                    unsafe
                    {
                        resized.Lock();
                        newAlpha.Lock();
                        byte* resizedPtr = (byte*)resized.BackBuffer.ToPointer();
                        byte* alphaPtr = (byte*)newAlpha.BackBuffer.ToPointer();
                        for (int i = 3; i < newStride * newHeight; i += 4)
                            resizedPtr[i] = alphaPtr[i];

                        resized.Unlock();
                        newAlpha.Unlock();
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.ToString());
                    throw;
                }
            }

            return new MipMap(resized.GetPixelsAsBGRA32(), newWidth, newHeight, LoadedFormatDetails);
        }
    }
}
