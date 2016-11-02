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
using System.ComponentModel;
using CSharpImageLibrary.Headers;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Determines how Mipmaps are handled.
    /// </summary>
    public enum MipHandling
    {
        /// <summary>
        /// If mips are present, they are used, otherwise regenerated.
        /// </summary>
        [Description("If mips are present, they are used, otherwise regenerated.")]
        Default,

        /// <summary>
        /// Keeps existing mips if existing. Doesn't generate new ones either way.
        /// </summary>
        [Description("Keeps existing mips if existing. Doesn't generate new ones either way.")]
        KeepExisting,

        /// <summary>
        /// Removes old mips and generates new ones.
        /// </summary>
        [Description("Removes old mips and generates new ones.")]
        GenerateNew,

        /// <summary>
        /// Removes all but the top mip. Used for single mip formats.
        /// </summary>
        [Description("Removes all but the top mip. Used for single mip formats.")]
        KeepTopOnly
    }

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
        /// Enables threading of Loading and Saving operations to improve performance.
        /// </summary>
        public static bool EnableThreading { get; set; } = true;

        /// <summary>
        /// Enables GPU Accelerated encoding and decoding of all formats.
        /// NOTE: WIC formats (jpg, bmp, png etc) probably already use GPU, but are not covered by this flag.
        /// </summary>
        public static bool EnableGPUAcceleration { get; set; }

        /// <summary>
        /// Constructor. Checks WIC status before any other operation.
        /// </summary>
        static ImageEngine()
        {
            WindowsWICCodecsAvailable = WIC_Codecs.WindowsCodecsPresent();

            // GPU testing
            EnableGPUAcceleration = true;
        }


        // NEW LOADING
        internal static List<MipMap> LoadImage(Stream imageStream)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
        }

        internal static AbstractHeader LoadHeader(MemoryStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            // Determine type of image
            ImageFormats.SupportedExtensions ext = ImageFormats.DetermineImageType(stream);

            // Parse header
            AbstractHeader header = null;
            switch (ext)
            {
                case ImageFormats.SupportedExtensions.BMP:
                    header = new BMP_Header();
                    break;
                case ImageFormats.SupportedExtensions.DDS:
                    header = new DDS_Header();
                    break;
                case ImageFormats.SupportedExtensions.JPG:
                    header = new JPG_Header();
                    break;
                case ImageFormats.SupportedExtensions.PNG:
                    header = new PNG_Header();
                    break;
                case ImageFormats.SupportedExtensions.TGA:
                    header = new TGA_Header();
                    break;
                case ImageFormats.SupportedExtensions.GIF:
                    header = new GIF_Header();
                    break;
                default:
                    throw new NotSupportedException("Image type unknown.");
            }

            return header;
        }


        



        /// <summary>
        /// Save mipmaps as given format to stream.
        /// </summary>
        /// <param name="MipMaps">List of Mips to save.</param>
        /// <param name="format">Desired format.</param>
        /// <param name="destination">Stream to save to.</param>
        /// <param name="mipChoice">Determines how to handle mipmaps.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        /// <param name="mergeAlpha">True = alpha flattened down, directly affecting RGB.</param>
        /// <param name="mipToSave">0 based index on which mipmap to make top of saved image.</param>
        /// <returns>True on success.</returns>
        internal static bool Save(List<MipMap> MipMaps, ImageEngineFormat format, Stream destination, MipHandling mipChoice, bool mergeAlpha, int maxDimension = 0, int mipToSave = 0)
        {
            Format temp = new Format(format);
            List<MipMap> newMips = new List<MipMap>(MipMaps);

            if ((temp.IsMippable && mipChoice == MipHandling.GenerateNew) || (temp.IsMippable && newMips.Count == 1 && mipChoice == MipHandling.Default))
                DDSGeneral.BuildMipMaps(newMips, mergeAlpha);

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
                    newMips[0] = Resize(newMips[0], scale, mergeAlpha);
                }
            }

            // KFreon: Ensure we have a power of two for dimensions
            double fixScale = 0;
            if (!UsefulThings.General.IsPowerOfTwo(newMips[0].Width) || !UsefulThings.General.IsPowerOfTwo(newMips[0].Height))
            {
                int newWidth = UsefulThings.General.RoundToNearestPowerOfTwo(newMips[0].Width);
                int newHeigh = UsefulThings.General.RoundToNearestPowerOfTwo(newMips[0].Height);

                // KFreon: Assuming same scale in both dimensions...
                fixScale = 1.0*newWidth / newMips[0].Width;

                newMips[0] = Resize(newMips[0], fixScale, mergeAlpha);

            }


            if (fixScale != 0 || mipChoice == MipHandling.KeepTopOnly)
                DestroyMipMaps(newMips, mipToSave);

            if (fixScale != 0 && temp.IsMippable && mipChoice != MipHandling.KeepTopOnly)
                DDSGeneral.BuildMipMaps(newMips, mergeAlpha);


            bool result = false;
            if (temp.SurfaceFormat.ToString().Contains("DDS"))
                result = DDSGeneral.Save(newMips, destination, temp);
            else
            {
                // KFreon: Try saving with built in codecs
                var mip = newMips[0];
                if (WindowsWICCodecsAvailable)
                    result = WIC_Codecs.SaveWithCodecs(mip.BaseImage, destination, format);
            }

            if (mipChoice != MipHandling.KeepTopOnly && temp.IsMippable)
            {
                // KFreon: Necessary. Must be how I handle the lowest mip levels. i.e. WRONGLY :(
                // Figure out how big the file should be and make it that size

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


        /// <summary>
        /// Saves image to byte[].
        /// </summary>
        /// <param name="MipMaps">Mipmaps to save.</param>
        /// <param name="format">Format to save image as.</param>
        /// <param name="generateMips">Determines how to handle mipmaps.</param>
        /// <param name="desiredMaxDimension">Maximum dimension to allow. Resizes if required.</param>
        /// <param name="mipToSave">Mipmap to save. If > 0, all other mipmaps removed, and this mipmap saved.</param>
        /// <param name="mergeAlpha">True = Flattens alpha into RGB.</param>
        /// <returns>Byte[] containing fully formatted image.</returns>
        internal static byte[] Save(List<MipMap> MipMaps, ImageEngineFormat format, MipHandling generateMips, int desiredMaxDimension, int mipToSave, bool mergeAlpha)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Save(MipMaps, format, ms, generateMips, mergeAlpha, desiredMaxDimension, mipToSave);
                return ms.ToArray();
            }
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
        

        internal static MipMap Resize(MipMap mipMap, double scale, bool mergeAlpha)
        {
            WriteableBitmap bmp = mipMap.BaseImage;
            int origWidth = bmp.PixelWidth;
            int origHeight = bmp.PixelHeight;
            int origStride = origWidth * 4;
            int newWidth = (int)(origWidth * scale);
            int newHeight = (int)(origHeight * scale);
            int newStride = newWidth * 4;

            // KFreon: Only do the alpha bit if there is any alpha. Git #444 (https://github.com/ME3Explorer/ME3Explorer/issues/444) exposed the issue where if there isn't alpha, it overruns the buffer.
            bool alphaPresent = bmp.Format.ToString().Contains("a", StringComparison.OrdinalIgnoreCase);
            Console.WriteLine("PixelFormat (for checking if alpha resize is going to work): " + bmp.Format);

            WriteableBitmap alpha = new WriteableBitmap(origWidth, origHeight, 96, 96, PixelFormats.Bgr32, null);
            if (alphaPresent && !mergeAlpha)
            {
                // Pull out alpha since scaling with alpha doesn't work properly for some reason
                try
                {
                    unsafe
                    {
                        alpha.Lock();
                        int index = 3;
                        byte* alphaPtr = (byte*)alpha.BackBuffer.ToPointer();
                        byte* mainPtr = (byte*)bmp.BackBuffer.ToPointer();
                        for (int i = 0; i < origWidth * origHeight * 4; i += 4)
                        {
                            // Set all pixels in alpha to value of alpha from original image - otherwise scaling will interpolate colours
                            alphaPtr[i] = mainPtr[index];
                            alphaPtr[i + 1] = mainPtr[index];
                            alphaPtr[i + 2] = mainPtr[index];
                            alphaPtr[i + 3] = mainPtr[index];
                            index += 4;
                        }

                        alpha.Unlock();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    throw;
                }
            }
            
            FormatConvertedBitmap main = new FormatConvertedBitmap(bmp, PixelFormats.Bgr32, null, 0);

            

            // Scale RGB
            ScaleTransform scaletransform = new ScaleTransform(scale, scale);
            TransformedBitmap scaledMain = new TransformedBitmap(main, scaletransform);


            // Put alpha back in
            FormatConvertedBitmap newConv = new FormatConvertedBitmap(scaledMain, PixelFormats.Bgra32, null, 0);
            WriteableBitmap resized = new WriteableBitmap(newConv);

            if (alphaPresent && !mergeAlpha)
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
                    Debug.WriteLine(e.ToString());
                    throw;
                }
            }
            
            

            return new MipMap(resized);
        }


        /// <summary>
        /// Destroys mipmaps. Expects at least one mipmap in given list.
        /// </summary>
        /// <param name="MipMaps">List of Mipmaps.</param>
        /// <param name="mipToSave">Index of mipmap to save. 1 based, i.e. top is 1.</param>
        /// <returns>Number of mips present.</returns>
        private static int DestroyMipMaps(List<MipMap> MipMaps, int mipToSave)
        {
            MipMaps.RemoveRange(mipToSave + 1, MipMaps.Count - 1);  // +1 because mipToSave is 0 based and we want to keep it
            return 1;
        }

        /// <summary>
        /// Generates a thumbnail image as quickly and efficiently as possible.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="maxHeight">Max height to decode at. 0 means ignored, and aspect respected.</param>
        /// <param name="maxWidth">Max width to decode at. 0 means ignored, and aspect respected.</param>
        /// <param name="mergeAlpha">DXT1 only. True = Flatten alpha into RGB.</param>
        /// <param name="requireTransparency">True = uses PNG compression instead of JPG.</param>
        public static MemoryStream GenerateThumbnailToStream(Stream stream, int maxWidth, int maxHeight, bool mergeAlpha = false, bool requireTransparency = false)
        {
            Format format = new Format();
            DDSGeneral.DDS_HEADER header = null;
            var mipmaps = LoadImage(stream, out format, null, maxWidth, maxHeight, true, out header, mergeAlpha);

            MemoryStream ms = new MemoryStream();
            bool result = Save(mipmaps, requireTransparency ? ImageEngineFormat.PNG : ImageEngineFormat.JPG, ms, MipHandling.KeepTopOnly, mergeAlpha, maxHeight > maxWidth ? maxHeight : maxWidth);
            if (!result)
                ms = null;

            return ms;
        }


        /// <summary>
        /// Generates a thumbnail of image and saves it to a file.
        /// </summary>
        /// <param name="stream">Fully formatted image stream.</param>
        /// <param name="destination">File path to save to.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        /// <param name="mergeAlpha">DXT1 only. True = Flatten alpha into RGB.</param>
        /// <returns>True on success.</returns>
        public static bool GenerateThumbnailToFile(Stream stream, string destination, int maxDimension, bool mergeAlpha = false)
        {
            using (ImageEngineImage img = new ImageEngineImage(stream, null, maxDimension, true))
            {
                bool success = false;
                using (FileStream fs = new FileStream(destination, FileMode.Create))
                    success = img.Save(fs, ImageEngineFormat.JPG, MipHandling.KeepTopOnly, mergeAlpha: mergeAlpha, desiredMaxDimension: maxDimension);

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
