using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSharpImageLibrary;
using CSharpImageLibrary.Headers;
using UsefulThings;

namespace CSharpImageLibrary_NET
{
    public static class ImageEngine
    {
        /// <summary>
        /// True = Windows WIC Codecs are present (8+)
        /// </summary>
        public static bool WindowsWICCodecsAvailable { get; set; }

        static ImageEngine()
        {
            WindowsWICCodecsAvailable = WIC_Codecs.WindowsCodecsPresent();
        }

        public static List<MipMapBase> LoadImage(Stream imageStream, AbstractHeader header, int maxDimension, double scale, ImageFormats.ImageEngineFormatDetails formatDetails)
        {
            if (!WindowsWICCodecsAvailable)
                return CSharpImageLibrary.ImageEngine.LoadImage<MipMap>(imageStream, header, maxDimension, scale, formatDetails);

            int decodeWidth = header.Width > header.Height ? maxDimension : 0;
            int decodeHeight = header.Width < header.Height ? maxDimension : 0;

            switch (formatDetails.SurfaceFormat)
            {
                case ImageEngineFormat.DDS_DXT1:
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    return WIC_Codecs.LoadWithCodecs(imageStream, decodeWidth, decodeHeight, scale, true, formatDetails);
                case ImageEngineFormat.GIF:
                case ImageEngineFormat.JPG:
                case ImageEngineFormat.PNG:
                case ImageEngineFormat.BMP:
                case ImageEngineFormat.TIF:
                    return WIC_Codecs.LoadWithCodecs(imageStream, decodeWidth, decodeHeight, scale, false, formatDetails);
                default:
                    throw new FileFormatException("Format unknown.");
            }
        }

        internal static void SplitChannels(MipMapBase mip, string savePath)
        {
            char[] channels = new char[] { 'B', 'G', 'R', 'A' };
            for (int i = 0; i < 4; i++)
            {
                // Extract channel into grayscale image
                var grayChannel = CSharpImageLibrary.ImageEngine.BuildGrayscaleFromChannel(mip.Pixels, i);

                // Save channel
                var img = UsefulThings.WPF.Images.CreateWriteableBitmap(grayChannel, mip.Width, mip.Height, PixelFormats.Gray8);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(img));
                byte[] bytes = null;
                using (MemoryStream ms = new MemoryStream(grayChannel.Length))
                {
                    encoder.Save(ms);
                    bytes = ms.ToArray();
                }

                if (bytes == null)
                    throw new InvalidDataException("Failed to save channel. Reason unknown.");

                string tempPath = Path.GetFileNameWithoutExtension(savePath) + "_" + channels[i] + ".png";
                string channelPath = Path.Combine(Path.GetDirectoryName(savePath), UsefulThings.General.FindValidNewFileName(tempPath));
                File.WriteAllBytes(channelPath, bytes);
            }
        }

        internal static byte[] Save(List<MipMapBase> MipMaps, ImageFormats.ImageEngineFormatDetails destFormatDetails, MipHandling mipChoice, AlphaSettings alphaSetting, int maxDimension = 0, int mipToSave = 0)
        {
            var newMips = CSharpImageLibrary.ImageEngine.PreSaveSetup(MipMaps, destFormatDetails, mipChoice, alphaSetting, maxDimension, mipToSave);

            // KFreon: Try saving with built in codecs
            var mip = newMips[0];

            // Fix formatting
            byte[] newPixels = new byte[mip.Width * mip.Height * 4];
            for (int i = 0, j = 0; i < newPixels.Length; i++, j += mip.LoadedFormatDetails.ComponentSize)
                newPixels[i] = mip.LoadedFormatDetails.ReadByte(mip.Pixels, j);

            return WIC_Codecs.SaveWithCodecs(newPixels, destFormatDetails.SurfaceFormat, mip.Width, mip.Height, alphaSetting);
        }

        internal static MipMapBase Resize(MipMapBase mipMap, double xScale, double yScale)
        {
            var baseBMP = UsefulThings.WPF.Images.CreateWriteableBitmap(mipMap.Pixels, mipMap.Width, mipMap.Height);
            baseBMP.Freeze();

            // KFreon: Only do the alpha bit if there is any alpha. Git #444 (https://github.com/ME3Explorer/ME3Explorer/issues/444) exposed the issue where if there isn't alpha, it overruns the buffer.
            bool alphaPresent = mipMap.IsAlphaPresent;

            WriteableBitmap alpha = new WriteableBitmap(mipMap.Width, mipMap.Height, 96, 96, PixelFormats.Bgr32, null);
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
                        for (int i = 0; i < mipMap.Width * mipMap.Height * 4; i += 4)
                        {
                            // Set all pixels in alpha to value of alpha from original image - otherwise scaling will interpolate colours
                            alphaPtr[i] = mipMap.Pixels[index];
                            alphaPtr[i + 1] = mipMap.Pixels[index];
                            alphaPtr[i + 2] = mipMap.Pixels[index];
                            alphaPtr[i + 3] = mipMap.Pixels[index];
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

            var bmp = UsefulThings.WPF.Images.CreateWriteableBitmap(mipMap.Pixels, mipMap.Width, mipMap.Height);
            FormatConvertedBitmap main = new FormatConvertedBitmap(bmp, PixelFormats.Bgr32, null, 0);



            // Scale RGB
            ScaleTransform scaletransform = new ScaleTransform(xScale, yScale);
            TransformedBitmap scaledMain = new TransformedBitmap(main, scaletransform);

            int newWidth = (int)(mipMap.Width * xScale);
            int newHeight = (int)(mipMap.Height * yScale);
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

            return new MipMap(resized.GetPixelsAsBGRA32(), newWidth, newHeight, mipMap.LoadedFormatDetails);
        }
    }
}
