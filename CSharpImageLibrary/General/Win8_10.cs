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

namespace CSharpImageLibrary.General
{
    /// <summary>
    /// Provides native Windows codec functionality for Windows 8.1+
    /// </summary>
    internal static class Win8_10
    {
        static bool WindowsCodecsAvailable = true;

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


        #region Loading
        /// <summary>
        /// Loads useful information from image stream using Windows 8.1+ codecs.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <param name="decodeWidth">Width to decode as. Aspect ratio unchanged if decodeHeight = 0.</param>
        /// <param name="decodeHeight">Height to decode as. Aspect ratio unchanged if decodeWidth = 0.</param>
        /// <param name="isDDS">True = image is a DDS.</param>
        /// <returns>BGRA Pixel Data as stream.</returns>
        internal static List<MipMap> LoadWithCodecs(Stream stream, int decodeWidth, int decodeHeight, bool isDDS)
        {
            if (!WindowsCodecsAvailable)
                return null;

            List<MipMap> mipmaps = new List<MipMap>();
            bool alternateDecodeDimensions = decodeWidth != 0 || decodeHeight != 0;

            if (isDDS)
            {
                // KFreon: Attempt to load any mipmaps
                stream.Seek(0, SeekOrigin.Begin);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnDemand);
                
                foreach (var mipmap in decoder.Frames)
                {
                    // KFreon: Skip mipmaps that are too big if asked to load a smaller image
                    if (alternateDecodeDimensions)
                        if (mipmap.Width > decodeWidth || mipmap.Height > decodeHeight)
                            continue;

                    int width = 0;
                    int height = 0;
                    MemoryStream data = LoadMipMap(mipmap, out width, out height);
                    mipmaps.Add(new MipMap(data, width, height));
                }

                if (mipmaps.Count == 0)
                {
                    // KFreon: No mips, so resize largest
                    int width = 0;
                    int height = 0;
                    MemoryStream data = LoadMipMap(decoder.Frames[0], out width, out height);
                    var mip = new MipMap(data, width, height);
                    double scale = decodeHeight != 0 ? decodeHeight * 1f / height: (decodeWidth != 0 ? decodeWidth * 1f / width : 0);
                    if (scale == 0)
                        throw new InvalidOperationException("No mips detected and no decodeWidth or decodeHeight specified. This is likely due to an invalid image or some weird error.");

                    mip = Resize(mip, scale);
                    mipmaps.Add(mip);
                }
            }
            else
            {
                // KFreon: No Mipmaps
                BitmapImage bmp = AttemptUsingWindowsCodecs(stream, decodeWidth, decodeHeight);
                if (bmp == null)
                    return null;

                int width = 0;
                int height = 0;
                MemoryStream mipmap = LoadMipMap(bmp, out width, out height);
                mipmaps.Add(new MipMap(mipmap, width, height));
            }

            return mipmaps;
        }


        /// <summary>
        /// Attempts to read image using WIC Codecs.
        /// Returns null if unable to.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="decodeHeight">Height to decode to. 0 = no scaling.</param>
        /// <param name="decodeWidth">Width to decode to. 0 = no scaling.</param>
        /// <returns>Loaded Image</returns>
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
        /// <param name="decodeWidth">Width to decode to. 0 = no scaling.</param>
        /// <param name="decodeHeight">Height to decode to. 0 = no scaling.</param>
        /// <returns>Loaded image.</returns>
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
        /// Loads a WIC image as a mipmap.
        /// </summary>
        /// <param name="bmp">MipMap to load.</param>
        /// <param name="Height">MipMap height</param>
        /// <param name="Width">MipMap Width.</param>
        /// <returns>BGRA pixel data as stream.</returns>
        private static MemoryStream LoadMipMap(BitmapSource bmp, out int Width, out int Height)
        {
            Width = (int)Math.Round(bmp.Width);
            Height = (int)Math.Round(bmp.Height);
            return bmp.GetPixelsAsStream(Width, Height);
        }


        /// <summary>
        /// Loads useful information from an image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="decodeWidth">Width to decode to. Aspect unchanged if decodeHeight = 0.</param>
        /// <param name="decodeHeight">Height to decode to. Aspect unchanged if decodeWidth = 0.</param>
        /// <param name="isDDS">True = Image is a DDS.</param>
        /// <returns>BGRA Pixel Data as stream.</returns>
        internal static List<MipMap> LoadWithCodecs(string imageFile, int decodeWidth, int decodeHeight, bool isDDS)
        {
            if (!WindowsCodecsAvailable)
                return null;

            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadWithCodecs(fs, decodeWidth, decodeHeight, isDDS);
        }
        #endregion Loading


        /// <summary>
        /// Ensures all Mipmaps are generated in MipMaps.
        /// </summary>
        /// <param name="MipMaps">MipMaps to check.</param>
        /// <returns>Number of mipmaps present in MipMaps.</returns>
        internal static int BuildMipMaps(List<MipMap> MipMaps)
        {
            if (MipMaps?.Count == 0)
                return 0;

            MipMap currentMip = MipMaps[0];

            // KFreon: Check if mips required
            int estimatedMips = DDSGeneral.EstimateNumMipMaps(currentMip.Width, currentMip.Height);
            if (estimatedMips == MipMaps.Count)
                return estimatedMips;

            // KFreon: Half dimensions until one == 1.
            for (int i = 1; i <= estimatedMips; i++)
            {
                MipMap newmip = Resize(currentMip, 1f / Math.Pow(2, i));
                MipMaps.Add(newmip);
            }

            return estimatedMips;
        }


        /// <summary>
        /// Saves image using internal Codecs - DDS and mippables not supported.
        /// </summary>
        /// <param name="pixels">BGRA pixels.</param>
        /// <param name="destination">Image stream to save to.</param>
        /// <param name="format">Destination image format.</param>
        /// <param name="Width">Width of image.</param>
        /// <param name="Height">Height of image.</param>
        /// <returns>True on success.</returns>
        internal static bool SaveWithCodecs(MemoryStream pixels, Stream destination, ImageEngineFormat format, int Width, int Height)
        {
            int stride = 4 * Width;
            BitmapFrame frame = BitmapFrame.Create(BitmapFrame.Create(Width, Height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent, pixels.ToArray(), stride));

            // KFreon: Choose encoder based on desired format.
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

        internal static MipMap Resize(MipMap mipMap, double scale)
        {
            BitmapImage bmp = null;
            using (MemoryStream ms = UsefulThings.RecyclableMemoryManager.GetStream())
            {
                if (!SaveWithCodecs(mipMap.Data, ms, ImageEngineFormat.PNG, mipMap.Width, mipMap.Height))
                    return null;

                bmp = UsefulThings.WPF.Images.CreateWPFBitmap(ms);
            }

            //bmp = UsefulThings.WPF.Images.ResizeImage(bmp, width, height);
            bmp = UsefulThings.WPF.Images.ScaleImage(bmp, scale);
            int bmpWidth = (int)bmp.Width;
            int bmpHeight = (int)bmp.Height;

            MemoryStream data = bmp.GetPixelsAsStream(bmpWidth, bmpHeight);
            return new MipMap(data, bmpWidth, bmpHeight);
        }
    }
}
