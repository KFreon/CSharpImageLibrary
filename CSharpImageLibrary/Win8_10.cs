using CSharpImageLibrary.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UsefulThings;
using System.Windows.Media;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides native Windows codec functionality for Windows 8.1+
    /// </summary>
    internal static class Win8_10
    {
        static bool WindowsCodecsAvailable = true;

        static Win8_10()
        {
            Console.WriteLine();
        }

        /// <summary>
        /// Tests whether Windows WIC Codecs are present.
        /// </summary>
        /// <returns>True if WIC Codecs available</returns>
        internal static bool WindowsCodecsPresent()
        {
            byte[] testData = Resources.DXT1_CodecTest;  // KFreon: Tiny test image in resources

            try
            {
                BitmapImage bmp = AttemptUsingWindowsCodecs(testData, 0, 0);

                if (bmp == null)
                {
                    WindowsCodecsAvailable = false;
                    return false;  // KFreon: Decoding failed. PROBABLY due to no decoding available
                }
            }
            catch (Exception e)
            {
                WindowsCodecsAvailable = false;
                return false;  // KFreon: Non decoding related error - Who knows...
            }

            return true;
        }

        /// <summary>
        /// Attempts to read image using WIC Codecs.
        /// Returns null if unable to.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <returns></returns>
        private static BitmapImage AttemptUsingWindowsCodecs(string imagePath, int decodeWidth, int decodeHeight)
        {
            if (!WindowsCodecsAvailable)
                return null;

            BitmapImage img = null;
            try
            {
                img = UsefulThings.WPF.Images.CreateWPFBitmap(imagePath, decodeWidth, decodeHeight);
            }
            catch (FileFormatException fileformatexception)
            {
                Debug.WriteLine(fileformatexception);
            }
            catch (NotSupportedException notsupportedexception)
            {
                Debug.WriteLine(notsupportedexception);
            }
            return img;
        }

        /// <summary>
        /// Attempts to read image using WIC Codecs.
        /// Returns null if unable to.
        /// </summary>
        /// <param name="ImageFileData">Entire image file. NOT raw pixel data.</param>
        /// <returns></returns>
        private static BitmapImage AttemptUsingWindowsCodecs(byte[] ImageFileData, int decodeWidth, int decodeHeight)
        {
            if (!WindowsCodecsAvailable)
                return null;

            BitmapImage img = null;
            try
            {
                img = UsefulThings.WPF.Images.CreateWPFBitmap(ImageFileData, decodeWidth, decodeHeight);
            }
            catch (FileFormatException fileformatexception)
            {
                Debug.WriteLine(fileformatexception);
            }
            catch (NotSupportedException notsupportedexception)
            {
                Debug.WriteLine(notsupportedexception);
            }

            return img;
        }


        /// <summary>
        /// Attempts to read image using WIC Codecs.
        /// Returns null if unable to.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT raw pixels.</param>
        /// <param name="decodeWidth">Width to decode to. Aspect unchanged if decodeHeight = 0.</param>
        /// <param name="decodeHeight">Height to decode to. Aspect unchanged if decodeWidth = 0.</param>
        /// <returns>BitmapImage of stream.</returns>
        private static BitmapImage AttemptUsingWindowsCodecs(Stream stream, int decodeWidth, int decodeHeight)
        {
            if (!WindowsCodecsAvailable)
                return null;

            BitmapImage img = null;
            try
            {
                img = UsefulThings.WPF.Images.CreateWPFBitmap(stream, decodeWidth, decodeHeight);
            }
            catch (FileFormatException fileformatexception)
            {
                Debug.WriteLine(fileformatexception);
            }
            catch (NotSupportedException notsupportedexception)
            {
                Debug.WriteLine(notsupportedexception);
            }

            return img;
        }


        /// <summary>
        /// Loads useful information from a WIC Image.
        /// </summary>
        /// <param name="bmp">Image to load.</param>
        /// <returns>BGRA pixel data as stream.</returns>
        private static MemoryTributary LoadMipMap(BitmapSource bmp)
        {
            int width = (int)Math.Ceiling(bmp.Width);
            int height = (int)Math.Ceiling(bmp.Height);
            return bmp.GetPixelsAsStream(width, height);
        }


        /// <summary>
        /// Loads useful information from an image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="decodeWidth">Width to decode to. Aspect unchanged if decodeHeight = 0.</param>
        /// <param name="decodeHeight">Height to decode to. Aspect unchanged if decodeWidth = 0.</param>
        /// <returns>BGRA Pixel Data as stream.</returns>
        internal static List<MipMap> LoadWithCodecs(string imageFile, int decodeWidth, int decodeHeight, bool isDDS)
        {
            if (!WindowsCodecsAvailable)
                return null;

            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadWithCodecs(fs, decodeWidth, decodeHeight, isDDS);
        }

        internal static int BuildMipMaps(List<MipMap> MipMaps)
        {
            if (MipMaps?.Count == 0)
                return 0;

            MipMap currentMip = MipMaps[0];

            // KFreon: Check if mips required
            int estimatedMips = DDSGeneral.EstimateNumMipMaps(currentMip.Width, currentMip.Height);
            if (estimatedMips == MipMaps.Count)
                return estimatedMips;


            int newWidth = currentMip.Width;
            int newHeight = currentMip.Height;

            
            for (int i = 0; i < estimatedMips; i++)
            {
                BitmapImage bmp = UsefulThings.WPF.Images.CreateWPFBitmap(currentMip.Data);
                newWidth /= 2;
                newHeight /= 2;

                bmp = UsefulThings.WPF.Images.ResizeImage(bmp, newWidth, newHeight);
                MemoryTributary data = bmp.GetPixelsAsStream(newWidth, newHeight);
                MipMaps.Add(new MipMap(data, newWidth, newHeight));

                currentMip = MipMaps[i];
            }

            return estimatedMips;
        }


        /// <summary>
        /// Saves image using internal Codecs - DDS not supported.
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="destination"></param>
        /// <param name="format"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <returns></returns>
        internal static bool SaveWithCodecs(MemoryTributary pixels, Stream destination, ImageEngineFormat format, int Width, int Height)
        {
            int stride = 4 * Width;
            BitmapFrame frame = BitmapFrame.Create(BitmapFrame.Create(Width, Height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent, pixels.ToArray(), stride));

            BitmapEncoder encoder = null;
            switch (format)
            {
                case ImageEngineFormat.BMP:
                    encoder = new BmpBitmapEncoder();
                    break;
                case ImageEngineFormat.JPG:
                    encoder = new JpegBitmapEncoder();
                    ((JpegBitmapEncoder)encoder).QualityLevel = 90;
                    break;
                case ImageEngineFormat.PNG:
                    encoder = new PngBitmapEncoder();
                    break;
                default:
                    throw new InvalidOperationException($"Unable to encode format: {format} using Windows 8.1 Codecs.");
            }

            encoder.Frames.Add(frame);
            encoder.Save(destination);
            return true;
        }

        internal static MemoryTributary GenerateThumbnail(Stream stream, int newWidth, int newHeight)
        {
            var mips = LoadWithCodecs(stream, newWidth, newHeight, false);  // Don't want mips so isDDS == false
            return mips?[0].Data;  // Returns null if mips is null
        }


        /// <summary>
        /// Loads useful information from image stream using Windows 8.1+ codecs.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="decodeWidth">Width to decode as. Aspect ratio unchanged if decodeHeight = 0.</param>
        /// <param name="decodeHeight">Height to decode as. Aspect ratio unchanged if decodeWidth = 0.</param>
        /// <returns>BGRA Pixel Data as stream.</returns>
        internal static List<MipMap> LoadWithCodecs(Stream stream, int decodeWidth, int decodeHeight, bool isDDS)
        {
            if (!WindowsCodecsAvailable)
                return null;

            List<MipMap> mipmaps = new List<MipMap>();
            BitmapSource top = null;

            if (isDDS)
            {
                stream.Seek(0, SeekOrigin.Begin);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                foreach (var mipmap in decoder.Frames)
                {
                    MemoryTributary data = LoadMipMap(mipmap);
                    mipmaps.Add(new MipMap(data, (int)mipmap.Width, (int)mipmap.Height));
                }
                top = decoder.Frames[0];
            }
            else
            {
                // KFreon: No Mipmaps
                BitmapImage bmp = AttemptUsingWindowsCodecs(stream, decodeWidth, decodeHeight);
                if (bmp == null)
                    return null;

                MemoryTributary mipmap = LoadMipMap(bmp);
                top = bmp;
                mipmaps.Add(new MipMap(mipmap, (int)bmp.Width, (int)bmp.Height));
            }
            
            return mipmaps;
        }
    }
}
