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
using CSharpImageLibrary.DDS;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Determines how alpha is handled.
    /// </summary>
    public enum AlphaSettings
    {
        KeepAlpha,
        Premultiply,
        RemoveAlphaChannel,
    }

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
        public static bool WindowsWICCodecsAvailable { get; set; }

        /// <summary>
        /// Enables threading of Loading and Saving operations to improve performance.
        /// </summary>
        public static bool EnableThreading { get; set; } = true;

        /// <summary>
        /// CURRENTLY DISABLED. Didn't work :(
        /// Enables GPU Accelerated encoding and decoding of all formats.
        /// NOTE: WIC formats (jpg, bmp, png etc) probably already use GPU, but are not covered by this flag.
        /// </summary>
        public static bool EnableGPUAcceleration { get; set; } = false;

        /// <summary>
        /// Determines how many threads to use. -1 is infinite.
        /// </summary>
        public static int NumThreads { get; set; } = -1;

        /// <summary>
        /// Constructor. Checks WIC status before any other operation.
        /// </summary>
        static ImageEngine()
        {
            WindowsWICCodecsAvailable = WIC_Codecs.WindowsCodecsPresent();

            // Enable GPU Acceleration by default
            /*if (GPU.IsGPUAvailable)
                EnableGPUAcceleration = false;*/
        }

        internal static List<MipMap> LoadImage(Stream imageStream, AbstractHeader header, int maxDimension, double scale)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
            List<MipMap> MipMaps = null;

            int decodeWidth = header.Width > header.Height ? maxDimension : 0;
            int decodeHeight = header.Width < header.Height ? maxDimension : 0;

            switch (header.Format)
            {
                case ImageEngineFormat.DDS_DXT1:
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    MipMaps = WIC_Codecs.LoadWithCodecs(imageStream, decodeWidth, decodeHeight, scale, true);
                    if (MipMaps == null)
                    {
                        // Windows codecs unavailable/failed. Load with mine.
                        MipMaps = DDSGeneral.LoadDDS((MemoryStream)imageStream, (DDS_Header)header, maxDimension);
                    }
                    break;
                case ImageEngineFormat.DDS_G8_L8:
                case ImageEngineFormat.DDS_RGB:
                case ImageEngineFormat.DDS_V8U8:
                case ImageEngineFormat.DDS_A8L8:
                case ImageEngineFormat.DDS_ARGB:
                case ImageEngineFormat.DDS_ATI1:
                case ImageEngineFormat.DDS_ATI2_3Dc:
                case ImageEngineFormat.DDS_CUSTOM:
                    MipMaps = DDSGeneral.LoadDDS((MemoryStream)imageStream, (DDS_Header)header, maxDimension);
                    break;
                case ImageEngineFormat.GIF:
                case ImageEngineFormat.JPG:
                case ImageEngineFormat.PNG:
                case ImageEngineFormat.BMP:
                case ImageEngineFormat.TIF:
                    MipMaps = WIC_Codecs.LoadWithCodecs(imageStream, decodeWidth, decodeHeight, scale, false);
                    break;
                case ImageEngineFormat.TGA:
                    using (var tga = new TargaImage(imageStream, ((TGA_Header)header).header))
                        MipMaps = new List<MipMap>() { new MipMap(tga.ImageData, tga.Header.Width, tga.Header.Height) }; 
                    break;
                case ImageEngineFormat.DDS_DX10:
                    throw new FormatException("DX10/DXGI not supported properly yet.");
                default:
                    throw new FormatException($"Format unknown: {header.Format}.");
            }

            return MipMaps;
        }

        internal static AbstractHeader LoadHeader(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            // Determine type of image
            ImageFormats.SupportedExtensions ext = ImageFormats.DetermineImageType(stream);

            // Parse header
            AbstractHeader header = null;
            switch (ext)
            {
                case ImageFormats.SupportedExtensions.BMP:
                    header = new BMP_Header(stream);
                    break;
                case ImageFormats.SupportedExtensions.DDS:
                    header = new DDS_Header(stream);
                    break;
                case ImageFormats.SupportedExtensions.JPG:
                    header = new JPG_Header(stream);
                    break;
                case ImageFormats.SupportedExtensions.PNG:
                    header = new PNG_Header(stream);
                    break;
                case ImageFormats.SupportedExtensions.TGA:
                    header = new TGA_Header(stream);
                    break;
                case ImageFormats.SupportedExtensions.GIF:
                    header = new GIF_Header(stream);
                    break;
                case ImageFormats.SupportedExtensions.TIF:
                    header = new TIFF_Header(stream);
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
        /// <param name="mipChoice">Determines how to handle mipmaps.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        /// <param name="alphaSetting">Determines how to handle alpha.</param>
        /// <param name="mipToSave">0 based index on which mipmap to make top of saved image.</param>
        /// <param name="customMasks">Custom user defined masks for colours.</param>
        /// <returns>True on success.</returns>
        internal static byte[] Save(List<MipMap> MipMaps, ImageEngineFormat format, MipHandling mipChoice, AlphaSettings alphaSetting, int maxDimension = 0, int mipToSave = 0, List<uint> customMasks = null)
        {
            List<MipMap> newMips = new List<MipMap>(MipMaps);
            bool isMippable = ImageFormats.IsFormatMippable(format);
            if ((isMippable && mipChoice == MipHandling.GenerateNew) || (isMippable && newMips.Count == 1 && mipChoice == MipHandling.Default))
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

            // KFreon: Ensure we have a power of two for dimensions FOR DDS ONLY
            double fixScale = 0;
            if (format.ToString().Contains("DDS") && (!UsefulThings.General.IsPowerOfTwo(newMips[0].Width) || !UsefulThings.General.IsPowerOfTwo(newMips[0].Height)))
            {
                int newWidth = UsefulThings.General.RoundToNearestPowerOfTwo(newMips[0].Width);
                int newHeigh = UsefulThings.General.RoundToNearestPowerOfTwo(newMips[0].Height);

                // TODO: Enable DDS' properly when not square. Related to sub 4x4 issue of "virtual" space and padding.

                // KFreon: Assuming same scale in both dimensions...
                fixScale = 1.0*newWidth / newMips[0].Width;
                newMips[0] = Resize(newMips[0], fixScale);
            }

            if (fixScale != 0 || mipChoice == MipHandling.KeepTopOnly)
                DestroyMipMaps(newMips, mipToSave);

            if (fixScale != 0 && isMippable && mipChoice != MipHandling.KeepTopOnly)
                DDSGeneral.BuildMipMaps(newMips);


            byte[] destination = null;
            if (format.ToString().Contains("DDS"))
                destination = DDSGeneral.Save(newMips, format, alphaSetting, customMasks);
            else
            {
                // KFreon: Try saving with built in codecs
                var mip = newMips[0];
                destination = WIC_Codecs.SaveWithCodecs(mip.Pixels, format, mip.Width, mip.Height, alphaSetting).ToArray();
            }

            return destination;
        }      
        

        internal static MipMap Resize(MipMap mipMap, double scale)
        {
            int origWidth = mipMap.Width;
            int origHeight = mipMap.Height;
            int origStride = origWidth * 4;
            int newWidth = (int)(origWidth * scale);
            int newHeight = (int)(origHeight * scale);
            int newStride = newWidth * 4;

            // TODO: Issues with incorrect sizing when non-square
            var baseBMP = UsefulThings.WPF.Images.CreateWriteableBitmap(mipMap.Pixels, mipMap.Width, mipMap.Height);

            byte[] newPixels = null;
            if (newHeight > newWidth)
                newPixels = UsefulThings.WPF.Images.CreateWPFBitmap(baseBMP, decodeWidth: newWidth).GetPixelsAsBGRA32();
            else
                newPixels = UsefulThings.WPF.Images.CreateWPFBitmap(baseBMP, decodeHeight: newHeight).GetPixelsAsBGRA32();

            return new MipMap(newPixels, newWidth, newHeight);

            #region Old code, but want to keep not only for posterity, but I'm not certain the above works in the context below.
            // KFreon: Only do the alpha bit if there is any alpha. Git #444 (https://github.com/ME3Explorer/ME3Explorer/issues/444) exposed the issue where if there isn't alpha, it overruns the buffer.
            /*bool alphaPresent = mipMap.AlphaPresent;

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
                        for (int i = 0; i < origWidth * origHeight * 4; i += 4)
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
                    Debug.WriteLine(e.ToString());
                    throw;
                }
            }

            var bmp = UsefulThings.WPF.Images.CreateWriteableBitmap(mipMap.Pixels, mipMap.Width, mipMap.Height);
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
            
            return new MipMap(resized.GetPixelsAsBGRA32(), newWidth, newHeight, alphaPresent);*/
            #endregion Old code
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
            else if (format.Contains("tiff", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.TIF;


            return parsedFormat;
        }
    }
}
