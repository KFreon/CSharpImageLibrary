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
    /// Provides main image functions
    /// </summary>
    public static class ImageEngine
    {
        /// <summary>
        /// True = Windows WIC Codecs are present (8+)
        /// </summary>
        public static bool WindowsCodecsAvailable
        {
            get; private set;
        }

        

        /// <summary>
        /// Constructor. Checks WIC status before any other operation.
        /// </summary>
        static ImageEngine()
        {
            WindowsCodecsAvailable = WindowsCodecsPresent();
        }


        #region Loading
        /// <summary>
        /// Loads useful information from a WIC Image.
        /// </summary>
        /// <param name="bmp">Image to load.</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <param name="Format">Detected image format.</param>
        /// <param name="extension">Image file extension - needed for format checks.</param>
        /// <returns>Raw pixel data as stream.</returns>
        public static MemoryTributary LoadImage(BitmapImage bmp, out double Width, out double Height, out Format Format, string extension)
        { 
            if (!WindowsCodecsAvailable)
            {
                // KFreon: Not on Windows 8+ or something like that
                Width = 0;
                Height = 0;
                Format = new Format();
                return null;
            }

            // KFreon: Round up - some weird bug where bmp's would be 1023.4 or something.
            Height = Math.Ceiling(bmp.Height);
            Width = Math.Ceiling(bmp.Width);

            // KFreon: Get format, choosing data source based on how BitmapImage was created.
            if (bmp.UriSource != null)
                Format = ImageFormats.ParseFormat(bmp.UriSource.OriginalString);
            else if (bmp.StreamSource != null)
                Format = ImageFormats.ParseFormat(bmp.StreamSource, ImageFormats.ParseExtension(extension));
            else
                throw new InvalidDataException("Bitmap doesn't seem to have a suitable source.");


            if (Format.InternalFormat == ImageEngineFormat.Unknown)
                Console.WriteLine();


            // KFreon: Read pixel data from image.
            MemoryTributary pixelData = new MemoryTributary();

            int size = (int)(4 * Width * Height);
            byte[] pixels = new byte[size];
            int stride = (int)Width * 4;

            bmp.CopyPixels(pixels, stride, 0);
            pixelData.Write(pixels, 0, pixels.Length);
            return pixelData;
        }


        /// <summary>
        /// Loads useful information from an image file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <param name="Format">Detected image format.</param>
        /// <returns>Raw pixel data as stream.</returns>
        public static MemoryTributary LoadImage(string imagePath, out double Width, out double Height, out Format Format)
        {
            Width = 0;
            Height = 0;
            Format = new Format();

            // KFreon: Don't want to even try without WIC Codecs - wouldn't be able to load most images. Only the custom ones (V8U8, 3Dc, etc), no DXT1, 3, 5, JPG, etc
            if (!WindowsCodecsAvailable)
                return null;

            // KFreon: See if this image is supported by WIC and decode accordingly
            BitmapImage bmp = AttemptUsingWindowsCodecs(imagePath);
            if (bmp == null)
            {
                // KFreon: Unsupported by Windows Codecs
                // e.g. V8U8, 3Dc, G8/L8

                Format test = ImageFormats.ParseDDSFormat(imagePath);
                switch (test.InternalFormat)
                {
                    case ImageEngineFormat.DDS_V8U8:
                        Format = new Format(ImageEngineFormat.DDS_V8U8);
                        return V8U8.Load(imagePath, out Width, out Height);
                    case ImageEngineFormat.DDS_G8_L8:
                        Format = new Format(ImageEngineFormat.DDS_G8_L8);
                        return G8_L8.Load(imagePath, out Width, out Height);
                    case ImageEngineFormat.DDS_ATI1:
                        Format = new Format(ImageEngineFormat.DDS_ATI1);
                        return ATI1.Load(imagePath, out Width, out Height);
                    case ImageEngineFormat.DDS_ATI2_3Dc:
                        throw new NotImplementedException();
                    case ImageEngineFormat.DDS_ARGB:
                        Format = new Format(ImageEngineFormat.DDS_ARGB);
                        return RGBA.Load(imagePath, out Width, out Height);
                }

                return null;  // TODO: Temporary return
            }
            else
            {
                return LoadImage(bmp, out Width, out Height, out Format, Path.GetExtension(imagePath));
            }
        }
        #endregion Loading


        /// <summary>
        /// Builds mips for image. Note, doesn't keep the topmost as it's stored in the PixelData array of the main image.
        /// </summary>
        /// <param name="bmp">Image to build mips for.</param>
        /// <returns></returns>
        private static List<ImageEngineImage> BuildMipMaps(BitmapImage bmp, string extension)
        {
            // KFreon: Smallest dimension so mipping stops at say 2x1 instead of trying to go 1x0.5
            double determiningDimension = bmp.Width > bmp.Height ? bmp.Height : bmp.Width;
            BitmapImage workingImage = bmp;

            List<ImageEngineImage> MipMaps = new List<ImageEngineImage>();

            if (WindowsCodecsAvailable)
            {
                while (determiningDimension > 1)
                {
                    workingImage = UsefulThings.WPF.Images.ScaleImage(workingImage, 0.5);
                    MipMaps.Add(new ImageEngineImage(workingImage, extension));
                    determiningDimension /= 2;
                }
            }

            return MipMaps;
        }

        #region Windows WIC Codec tests
        /// <summary>
        /// Tests whether Windows WIC Codecs are present.
        /// </summary>
        /// <returns>True if WIC Codecs available</returns>
        public static bool WindowsCodecsPresent()
        {
            byte[] testData = Resources.DXT1_CodecTest;  // KFreon: Tiny test image in resources

            try
            {
                BitmapImage bmp = AttemptUsingWindowsCodecs(testData);

                if (bmp == null)
                    return false;  // KFreon: Decoding failed. PROBABLY due to no decoding available
            }
            catch (Exception e)
            {
                return false;  // KFreon: Non decoding related error - Who knows...
            }

            return true;
        }

        /// <summary>
        /// Attempts to read image using WIC Codecs.
        /// Returns null if unable to.
        /// </summary>
        /// <param name="ImageFileData">Entire image file. NOT raw pixel data.</param>
        /// <returns></returns>
        private static BitmapImage AttemptUsingWindowsCodecs(byte[] ImageFileData)
        {
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
        /// <param name="imagePath">Path to image file.</param>
        /// <returns></returns>
        private static BitmapImage AttemptUsingWindowsCodecs(string imagePath)
        {
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
        #endregion Windows WIC Codec tests
    }
}
