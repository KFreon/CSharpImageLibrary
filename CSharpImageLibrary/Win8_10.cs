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

namespace CSharpImageLibrary
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
                BitmapImage bmp = AttemptUsingWindowsCodecs(testData);

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
        private static BitmapImage AttemptUsingWindowsCodecs(string imagePath)
        {
            if (!WindowsCodecsAvailable)
                return null;

            BitmapImage img = null;
            try
            {
                img = UsefulThings.WPF.Images.CreateWPFBitmap(imagePath);
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
        private static BitmapImage AttemptUsingWindowsCodecs(byte[] ImageFileData)
        {
            if (!WindowsCodecsAvailable)
                return null;

            BitmapImage img = null;
            try
            {
                img = UsefulThings.WPF.Images.CreateWPFBitmap(ImageFileData);
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
        /// <returns>BitmapImage of </returns>
        private static BitmapImage AttemptUsingWindowsCodecs(Stream stream)
        {
            if (!WindowsCodecsAvailable)
                return null;

            BitmapImage img = null;
            try
            {
                img = UsefulThings.WPF.Images.CreateWPFBitmap(stream);
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
        /// <param name="extension">Image file extension. Leave null to guess.</param>
        /// <returns>RGBA pixel data as stream.</returns>
        internal static MemoryTributary LoadImageWithCodecs(BitmapImage bmp, out double Width, out double Height, string extension = null)
        {
            // KFreon: Round up - some weird bug where bmp's would be 1023.4 or something.
            Height = Math.Ceiling(bmp.Height);
            Width = Math.Ceiling(bmp.Width);

            // KFreon: Read pixel data from image.
            MemoryTributary pixelData = new MemoryTributary();

            int size = (int)(4 * Width * Height);
            byte[] pixels = new byte[size];
            int stride = (int)Width * 4;

            bmp.CopyPixels(pixels, stride, 0);
            pixelData.Write(pixels, 0, pixels.Length);
            return pixelData;
        }

        internal static bool SaveWithCodecs(Stream destination)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Loads useful information from an image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>RGBA Pixel Data as stream.</returns>
        internal static MemoryTributary LoadWithCodecs(string imageFile, out double Width, out double Height)
        {
            if (!WindowsCodecsAvailable)
            {
                Width = 0;
                Height = 0;
                return null;
            }

            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadWithCodecs(fs, out Width, out Height, Path.GetExtension(imageFile));
        }


        /// <summary>
        /// Loads useful information from image stream using Windows 8.1+ codecs.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="extension">Extension of original file. Leave null to guess.</param>
        /// <returns>RGBA Pixel Data as stream.</returns>
        internal static MemoryTributary LoadWithCodecs(Stream stream, out double Width, out double Height, string extension = null)
        {
            if (!WindowsCodecsAvailable)
            {
                Width = 0;
                Height = 0;
                return null;
            }

            BitmapImage bmp = AttemptUsingWindowsCodecs(stream);
            if (bmp == null)
            {
                Width = 0;
                Height = 0;
                return null;
            }

            return LoadImageWithCodecs(bmp, out Width, out Height, extension);
        }
    }
}
