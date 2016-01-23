using CSharpImageLibrary.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsefulThings;
using System.Runtime.InteropServices;

namespace CSharpImageLibrary.General
{
    /// <summary>
    /// Provides main image functions
    /// </summary>
    public static class ImageEngine
    {
        /// <summary>
        /// True = Windows WIC Codecs are present (8+)
        /// </summary>
        public static bool WindowsWICCodecsAvailable
        {
            get; private set;
        }

        

        /// <summary>
        /// Constructor. Checks WIC status before any other operation.
        /// </summary>
        static ImageEngine()
        {
            WindowsWICCodecsAvailable = Win8_10.WindowsCodecsPresent();
            //WindowsWICCodecsAvailable = false;
        }


        #region Loading
        /// <summary>
        /// Loads image from file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="Format">Detected format.</param>
        /// <param name="desiredMaxDimension">Largest dimension to load as.</param>
        /// <returns>List of Mipmaps.</returns>
        internal static List<MipMap> LoadImage(string imagePath, out Format Format, int desiredMaxDimension, bool enforceResize)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadImage(fs, out Format, Path.GetExtension(imagePath), desiredMaxDimension, enforceResize);
        }


        /// <summary>
        /// Loads image from stream.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="Format">Detected format.</param>
        /// <param name="extension">File extension. Used to determine format more easily.</param>
        /// <param name="desiredMaxDimension">Largest dimension to load as.</param>
        /// <returns>List of Mipmaps.</returns>
        internal static List<MipMap> LoadImage(Stream stream, out Format Format, string extension, int desiredMaxDimension, bool enforceResize)
        {
            // KFreon: See if image is built-in codec agnostic.
            DDSGeneral.DDS_HEADER header = null;
            Format = ImageFormats.ParseFormat(stream, extension, ref header);
            List<MipMap> MipMaps = null;

            switch (Format.InternalFormat)
            {
                case ImageEngineFormat.BMP:
                case ImageEngineFormat.JPG:
                case ImageEngineFormat.PNG:
                    if (WindowsWICCodecsAvailable)
                        MipMaps = Win8_10.LoadWithCodecs(stream, desiredMaxDimension, desiredMaxDimension, false);
                    else
                    {
                        int width, height;
                        var mipImage = Win7.LoadImageWithCodecs(stream, out width, out height);
                        var mip = new MipMap(mipImage);
                        MipMaps = new List<MipMap>() { mip };
                    }
                    break;
                case ImageEngineFormat.DDS_DXT1:
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    if (WindowsWICCodecsAvailable)
                        MipMaps = Win8_10.LoadWithCodecs(stream, desiredMaxDimension, desiredMaxDimension, true);
                    else
                        MipMaps = DDSGeneral.LoadDDS(stream, header, Format, desiredMaxDimension);
                    break;
                case ImageEngineFormat.DDS_ARGB:
                case ImageEngineFormat.DDS_A8L8:
                case ImageEngineFormat.DDS_RGB:
                case ImageEngineFormat.DDS_ATI1:
                case ImageEngineFormat.DDS_ATI2_3Dc:
                case ImageEngineFormat.DDS_G8_L8:
                case ImageEngineFormat.DDS_V8U8:
                    MipMaps = DDSGeneral.LoadDDS(stream, header, Format, desiredMaxDimension);
                    break;
                default:
                    throw new InvalidDataException("Image format is unknown.");
            }

            if (MipMaps == null || MipMaps.Count == 0)
                throw new InvalidDataException("No mipmaps loaded.");

            
            // KFreon: No resizing requested
            if (desiredMaxDimension == 0)
                return MipMaps;

            // KFreon: Test if we need to resize
            var top = MipMaps.First();
            if (top.Width == desiredMaxDimension || top.Height == desiredMaxDimension)
                return MipMaps;


            // KFreon: Attempt to resize
            var sizedMips = MipMaps.Where(m => m.Width > m.Height ? m.Width <= desiredMaxDimension : m.Height <= desiredMaxDimension);
            if (sizedMips != null && sizedMips.Any())  // KFreon: If there's already a mip, return that.
                MipMaps = sizedMips.ToList();
            else if (enforceResize)
            {
                // Get top mip and clear others.
                var mip = MipMaps[0];
                MipMaps.Clear();
                MipMap output = null;

                int divisor = mip.Height < mip.Width ? mip.Width / desiredMaxDimension : mip.Height / desiredMaxDimension;
                output = Resize(mip, 1f / divisor);

                MipMaps.Add(output);
            }
            return MipMaps;
        }
        #endregion Loading



        /// <summary>
        /// Save mipmaps as given format to stream.
        /// </summary>
        /// <param name="MipMaps">List of Mips to save.</param>
        /// <param name="format">Desired format.</param>
        /// <param name="destination">Stream to save to.</param>
        /// <param name="GenerateMips">True = Generate mipmaps for mippable images. False = Destroys them.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        /// <returns>True on success.</returns>
        internal static bool Save(List<MipMap> MipMaps, ImageEngineFormat format, Stream destination, bool GenerateMips, int maxDimension = 0)
        {
            Format temp = new Format(format);
            List<MipMap> newMips = new List<MipMap>(MipMaps);

            if (temp.IsMippable && GenerateMips)
                DDSGeneral.BuildMipMaps(newMips);

            // KFreon: Resize if asked
            if (maxDimension != 0 && maxDimension < newMips[0].Width && maxDimension < newMips[0].Height) 
            {
                if (!UsefulThings.General.IsPowerOfTwo(maxDimension))
                    throw new ArgumentException($"{nameof(maxDimension)} must be a power of 2. Got {nameof(maxDimension)} = {maxDimension}");


                // KFreon: Check if there's a mipmap suitable, removes all larger mipmaps
                var validMipmap = newMips.Where(img => (img.Width == maxDimension && img.Height <= maxDimension) || (img.Height == maxDimension && img.Width <=maxDimension));  // Check if a mip dimension is maxDimension and that the other dimension is equal or smaller
                if (validMipmap?.Count() != 0)
                {
                    int index = newMips.IndexOf(validMipmap.First());
                    newMips.RemoveRange(0, index);
                }
                else
                {
                    // KFreon: Get the amount the image needs to be scaled. Find largest dimension and get it's scale.
                    double scale = maxDimension * 1f / (newMips[0].Width > newMips[0].Height ? newMips[0].Width: newMips[0].Height);

                    // KFreon: No mip. Resize.
                    newMips[0] = Resize(newMips[0], scale);
                }
            }

            if (!GenerateMips)
                DestroyMipMaps(newMips);

            bool result = false;
            if (temp.InternalFormat.ToString().Contains("DDS"))
                result = DDSGeneral.Save(newMips, destination, temp);
            else
            {
                // KFreon: Try saving with built in codecs
                var mip = newMips[0];
                if (WindowsWICCodecsAvailable)
                    result = Win8_10.SaveWithCodecs(mip.BaseImage, destination, format);
                else
                    result = Win7.SaveWithCodecs(mip.BaseImage, destination, format, mip.Width, mip.Height);
            }

            if (GenerateMips && temp.IsMippable)
            {
                // KFreon: Necessary. Must be how I handle the lowest mip levels. i.e. WRONGLY :(
                // Figure out how big the file should be and make it that size
                // Calculate how many mips necessary - CAN'T just count newMips as it only contains useful mips. i.e. any dimension smaller than 4 is ignored.
                int numMips = DDSGeneral.EstimateNumMipMaps(newMips[0].Width, newMips[0].Height);

                int size = 0;
                int width = newMips[0].Width;
                int height = newMips[0].Height;

                int divisor = 1;
                if (temp.IsBlockCompressed)
                    divisor = 4;

                while(width >= 1 && height >= 1)
                {
                    int tempWidth = width;
                    int tempHeight = height;

                    if (temp.IsBlockCompressed)
                    {
                        if (tempWidth < 4)
                            tempWidth = 4;
                        if (tempHeight < 4)
                            tempHeight = 4;
                    }
                    

                    size += tempWidth / divisor * tempHeight / divisor * temp.BlockSize;
                    width /= 2;
                    height /= 2;
                }

                if (size > destination.Length - 128)
                {
                    byte[] blanks = new byte[size - (destination.Length - 128)];
                    destination.Write(blanks, 0, blanks.Length);
                }
            }

            

            return result;
        }


        internal static double ExpectedImageSize(double mipIndex, Format format, int baseWidth, int baseHeight)
        {
            /*
                Mipmapping halves both dimensions per mip down. Dimensions are then divided by 4 if block compressed as a texel is 4x4 pixels.
                e.g. 4096 x 4096 block compressed texture with 8 byte blocks e.g. DXT1
                Sizes of mipmaps:
                    4096 / 4 x 4096 / 4 x 8
                    (4096 / 4 / 2) x (4096 / 4 / 2) x 8
                    (4096 / 4 / 2 / 2) x (4096 / 4 / 2 / 2) x 8

                Pattern: Each dimension divided by 2 per mip size decreased.
                Thus, total is divided by 4.
                    Size of any mip = Sum(1/4^n) x divWidth x divHeight x blockSize,  
                        where n is the desired mip (0 based), 
                        divWidth and divHeight are the block compress adjusted dimensions (uncompressed textures lead to just original dimensions, block compressed are divided by 4)

                Turns out the partial sum of the infinite sum: Sum(1/4^n) = 1/3 x (4 - 4^-n). Who knew right?
            */

            int divisor = 1;
            if (format.IsBlockCompressed)
                divisor = 4;

            double sumPart = mipIndex == -1 ? 0 :
                (1 / 3f) * (4 - Math.Pow(4, -mipIndex));

            double totalSize = 128 + (sumPart * format.BlockSize * (baseWidth / divisor) * (baseHeight / divisor));


            return totalSize;
        }
        

        internal static MipMap Resize(MipMap mipMap, double scale)
        {
            WriteableBitmap bmp = mipMap.BaseImage;
            int origWidth = bmp.PixelWidth;
            int origHeight = bmp.PixelHeight;
            int origStride = origWidth * 4;
            int newWidth = (int)(origWidth * scale);
            int newHeight = (int)(origHeight * scale);
            int newStride = newWidth * 4;



            // Pull out alpha since scaling with alpha doesn't work properly for some reason
            WriteableBitmap alpha = new WriteableBitmap(origWidth, origHeight, 96, 96, PixelFormats.Bgr32, null);
            unsafe
            {
                int index = 3;
                byte* alphaPtr = (byte*)alpha.BackBuffer.ToPointer();
                byte* mainPtr = (byte*)bmp.BackBuffer.ToPointer();
                for (int i = 0; i < origWidth * origHeight * 3; i += 4)
                {
                    // Set all pixels in alpha to value of alpha from original image - otherwise scaling will interpolate colours
                    alphaPtr[i] = mainPtr[index];
                    alphaPtr[i + 1] = mainPtr[index];
                    alphaPtr[i + 2] = mainPtr[index];
                    alphaPtr[i + 3] = mainPtr[index];
                    index += 4;
                }
            }

            FormatConvertedBitmap main = new FormatConvertedBitmap(bmp, PixelFormats.Bgr32, null, 0);

            // Scale RGB and alpha
            ScaleTransform scaletransform = new ScaleTransform(scale, scale);
            TransformedBitmap scaledMain = new TransformedBitmap(main, scaletransform);
            TransformedBitmap scaledAlpha = new TransformedBitmap(alpha, scaletransform);

            // Put alpha back in
            FormatConvertedBitmap newConv = new FormatConvertedBitmap(scaledMain, PixelFormats.Bgra32, null, 0);
            WriteableBitmap resized = new WriteableBitmap(newConv);
            WriteableBitmap newAlpha = new WriteableBitmap(scaledAlpha);
            unsafe
            {
                byte* resizedPtr = (byte*)resized.BackBuffer.ToPointer();
                byte* alphaPtr = (byte*)newAlpha.BackBuffer.ToPointer();
                for (int i = 3; i < newStride; i += 4)
                    resizedPtr[i] = alphaPtr[i];
            }

            return new MipMap(resized);
        }


        /// <summary>
        /// Destroys mipmaps. Expects at least one mipmap in given list.
        /// </summary>
        /// <param name="MipMaps">List of Mipmaps.</param>
        /// <returns>Number of mips present.</returns>
        private static int DestroyMipMaps(List<MipMap> MipMaps)
        {
            MipMaps.RemoveRange(1, MipMaps.Count - 1);
            return 1;
        }

        /// <summary>
        /// Generates a thumbnail image as quickly and efficiently as possible.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        public static MemoryStream GenerateThumbnailToStream(Stream stream, int maxDimension)
        {
            Format format = new Format();
            var mipmaps = LoadImage(stream, out format, null, maxDimension, true);

            MemoryStream ms = new MemoryStream();
            Save(mipmaps, ImageEngineFormat.JPG, ms, false);

            return ms;
        }


        /// <summary>
        /// Generates a thumbnail of image and saves it to a file.
        /// </summary>
        /// <param name="stream">Fully formatted image stream.</param>
        /// <param name="destination">File path to save to.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        /// <returns>True on success.</returns>
        public static bool GenerateThumbnailToFile(Stream stream, string destination, int maxDimension)
        {
            using (ImageEngineImage img = new ImageEngineImage(stream, null, maxDimension, true))
            {
                bool success = false;
                using (FileStream fs = new FileStream(destination, FileMode.Create))
                    success = img.Save(fs, ImageEngineFormat.JPG, false);  // KFreon: Don't need to specify dimension here as it was done during loading

                return success;
            }                
        }


        /// <summary>
        /// Parses a string to an ImageEngineFormat.
        /// </summary>
        /// <param name="format">String representation of ImageEngineFormat.</param>
        /// <returns>ImageEngineFormat of format.</returns>
        public static ImageEngineFormat ParseFromString(string format)
        {
            ImageEngineFormat parsedFormat = ImageEngineFormat.Unknown;

            if (format.Contains("dxt1", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT1;
            else if (format.Contains("dxt2", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT2;
            else if (format.Contains("dxt3", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT3;
            else if (format.Contains("dxt4", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT4;
            else if (format.Contains("dxt5", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT5;
            else if (format.Contains("bmp", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.BMP;
            else if (format.Contains("argb", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_ARGB;
            else if (format.Contains("ati1", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_ATI1;
            else if (format.Contains("ati2", StringComparison.OrdinalIgnoreCase) || format.Contains("3dc", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_ATI2_3Dc;
            else if (format.Contains("l8", StringComparison.OrdinalIgnoreCase) || format.Contains("g8", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_G8_L8;
            else if (format.Contains("v8u8", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_V8U8;
            else if (format.Contains("jpg", StringComparison.OrdinalIgnoreCase) || format.Contains("jpeg", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.JPG;
            else if (format.Contains("png", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.PNG;


            return parsedFormat;
        }
    }
}
