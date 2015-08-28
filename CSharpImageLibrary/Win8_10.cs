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
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>RGBA pixel data as stream.</returns>
        internal static MemoryTributary LoadWithCodecs(BitmapImage bmp, out int Width, out int Height)
        {
            // KFreon: Round up - some weird bug where bmp's would be 1023.4 or something.
            Height = (int)Math.Ceiling(bmp.Height);
            Width = (int)Math.Ceiling(bmp.Width);

            return bmp.GetPixelsAsStream(Width, Height);
        }


        /// <summary>
        /// Loads useful information from an image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="decodeWidth">Width to decode to. Aspect unchanged if decodeHeight = 0.</param>
        /// <param name="decodeHeight">Height to decode to. Aspect unchanged if decodeWidth = 0.</param>
        /// <returns>RGBA Pixel Data as stream.</returns>
        internal static MemoryTributary LoadWithCodecs(string imageFile, out int Width, out int Height, int decodeWidth, int decodeHeight)
        {
            if (!WindowsCodecsAvailable)
            {
                Width = 0;
                Height = 0;
                return null;
            }

            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                //return LoadWithCodecs(fs, out Width, out Height, decodeWidth, decodeHeight, Path.GetExtension(imageFile));
                return LoadWithCodecs(fs, out Width, out Height, decodeWidth, decodeHeight);
        }

        internal static int BuildMipMaps(Stream pixelData, Stream destination, int Width, int Height)
        {
            BitmapImage bmp = UsefulThings.WPF.Images.CreateWPFBitmap(pixelData);
            int smallestDimension = Width > Height ? Height : Width;
            int newWidth = Width;
            int newHeight = Height;

            int count = 1;
            while (smallestDimension > 0)
            {
                newWidth /= 2;
                newHeight /= 2;

                bmp = UsefulThings.WPF.Images.ResizeImage(bmp, newWidth, newHeight);
                MemoryTributary data = bmp.GetPixelsAsStream(newWidth, newHeight);
                data.WriteTo(destination);
                smallestDimension /= 2;
                count++;
            }

            return count;
        }

        internal static bool SaveWithCodecs(MemoryTributary pixelsWithMips, Stream destination, ImageEngineFormat format, int Width, int Height)
        {
            int stride = 4 * (Width * 32 + 31) / 32;
            BitmapFrame frame = BitmapFrame.Create(BitmapFrame.Create(Width, Height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent, pixelsWithMips.ToArray(), stride));

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
            int Width;
            int Height;
            return LoadWithCodecs(stream, out Width, out Height, newWidth, newHeight);
        }


        /// <summary>
        /// Loads useful information from image stream using Windows 8.1+ codecs.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="decodeWidth">Width to decode as. Aspect ratio unchanged if decodeHeight = 0.</param>
        /// <param name="decodeHeight">Height to decode as. Aspect ratio unchanged if decodeWidth = 0.</param>
        /// <returns>RGBA Pixel Data as stream.</returns>
        internal static MemoryTributary LoadWithCodecs(Stream stream, out int Width, out int Height, int decodeWidth, int decodeHeight)
        {
            if (!WindowsCodecsAvailable)
            {
                Width = 0;
                Height = 0;
                return null;
            }

            BitmapImage bmp = AttemptUsingWindowsCodecs(stream, decodeWidth, decodeHeight);
            if (bmp == null)
            {
                Width = 0;
                Height = 0;
                return null;
            }

            return LoadWithCodecs(bmp, out Width, out Height);
        }
    }
}
