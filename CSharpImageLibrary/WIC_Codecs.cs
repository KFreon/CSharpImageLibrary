﻿using CSharpImageLibrary.Properties;
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
using System.Windows;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides native Windows codec functionality for Windows 8.1+.
    /// </summary>
    internal static class WIC_Codecs
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
                Debug.WriteLine(e.ToString());
                WindowsCodecsAvailable = false;
                return false;  // KFreon: Non decoding related error - Who knows...
            }

            return true;
        }


        #region Loading
        /// <summary>
        /// Loads useful information from an image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="decodeWidth">Width to decode to. Aspect unchanged if decodeHeight = 0.</param>
        /// <param name="decodeHeight">Height to decode to. Aspect unchanged if decodeWidth = 0.</param>
        /// <param name="scale">DOMINANT. decodeWidth and decodeHeight ignored if this is > 0. Amount to scale by. Range 0-1.</param>
        /// <param name="isDDS">True = Image is a DDS.</param>
        /// <returns>BGRA Pixel Data as stream.</returns>
        internal static List<MipMap> LoadWithCodecs(string imageFile, int decodeWidth, int decodeHeight, double scale, bool isDDS)
        {
            if (isDDS && !WindowsCodecsAvailable)
                return null;

            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadWithCodecs(fs, decodeWidth, decodeHeight, scale, isDDS);
        }


        /// <summary>
        /// Loads useful information from image stream using Windows 8.1+ codecs.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <param name="decodeWidth">Width to decode as. Aspect ratio unchanged if decodeHeight = 0.</param>
        /// <param name="decodeHeight">Height to decode as. Aspect ratio unchanged if decodeWidth = 0.</param>
        /// <param name="isDDS">True = image is a DDS.</param>
        /// <param name="scale">DOMINANT. DecodeWidth and DecodeHeight ignored if this is > 0. Amount to scale by. Range 0-1.</param>
        /// <returns>BGRA Pixel Data as stream.</returns>
        internal static List<MipMap> LoadWithCodecs(Stream stream, int decodeWidth, int decodeHeight, double scale, bool isDDS)
        {
            if (isDDS && !WindowsCodecsAvailable)
                return null;

            bool alternateDecodeDimensions = decodeHeight != 0 || decodeWidth != 0 || scale != 0;
            int alternateWidth = decodeWidth;
            int alternateHeight = decodeHeight;

            List<MipMap> mipmaps = new List<MipMap>();

            if (isDDS)
            {
                // KFreon: Attempt to load any mipmaps
                stream.Seek(0, SeekOrigin.Begin);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnDemand);

                // Setup alternateDimensions if required
                if (scale != 0)
                {
                    alternateHeight = (int)(decoder.Frames[0].Height * scale);
                    alternateWidth = (int)(decoder.Frames[0].Width * scale);
                }

                foreach (var mipmap in decoder.Frames)
                {
                    // KFreon: Skip mipmaps that are too big if asked to load a smaller image
                    if (alternateDecodeDimensions)
                    {
                        if (mipmap.Width > alternateWidth || mipmap.Height > alternateHeight)
                            continue;
                    }

                    mipmaps.Add(new MipMap(mipmap));
                }

                if (mipmaps.Count == 0)
                {
                    // KFreon: Image has no mips, so resize largest
                    var mip = new MipMap(decoder.Frames[0]);
                    mip = ImageEngine.Resize(mip, scale, false);
                    mipmaps.Add(mip);
                }
            }
            else
            {
                // KFreon: No Mipmaps
                BitmapImage bmp = AttemptUsingWindowsCodecs(stream, alternateWidth, alternateHeight);
                if (bmp == null)
                    return null;

                mipmaps.Add(new MipMap(bmp));
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
        #endregion Loading


        /// <summary>
        /// Saves image using internal Codecs - DDS and mippables not supported.
        /// </summary>
        /// <param name="image">Image as bmp source.</param>
        /// <param name="destination">Image stream to save to.</param>
        /// <param name="format">Destination image format.</param>
        /// <returns>True on success.</returns>
        internal static bool SaveWithCodecs(BitmapSource image, Stream destination, ImageEngineFormat format)
        {
            BitmapFrame frame = BitmapFrame.Create(image);

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
    }
}
