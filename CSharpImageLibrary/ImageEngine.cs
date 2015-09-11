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
        public static bool WindowsWICCodecsAvailable
        {
            get; private set;
        }

        

        /// <summary>
        /// Constructor. Checks WIC status before any other operation.
        /// </summary>
        static ImageEngine()
        {
            //WindowsWICCodecsAvailable = Win8_10.WindowsCodecsPresent();
            WindowsWICCodecsAvailable = false;
        }


        #region Loading
        /// <summary>
        /// Loads useful information from an image file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="Format">Detected image format.</param>
        /// <returns>Raw pixel data as stream.</returns>
        internal static List<MipMap> LoadImage(string imagePath, out Format Format)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadImage(fs, out Format);
        }


        /// <summary>
        /// Loads formats which have no native decompressors. (V8U8, G8/L8, ATI1 and 2(3Dc), ARGB.
        /// </summary>
        /// <param name="stream">Image data.</param>
        /// <param name="Format">Detected format of image.</param>
        /// <returns>List of mipmaps.</returns>
        private static List<MipMap> LoadEsoterics(Stream stream, Format Format)
        {
            switch (Format.InternalFormat)
            {
                case ImageEngineFormat.DDS_V8U8:
                    return V8U8.Load(stream);
                case ImageEngineFormat.DDS_G8_L8:
                    return G8_L8.Load(stream);
                case ImageEngineFormat.DDS_ATI1:
                    return BC4_ATI1.Load(stream);
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    return BC5_ATI2_3Dc.Load(stream);
                case ImageEngineFormat.DDS_ARGB:
                    return RGBA.Load(stream);
            }
            return null;
        }

        /// <summary>
        /// Loads image from image stream.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <param name="Format">Image format (dds surfaces, jpg, png, etc)</param>
        /// <param name="extension">Extension of original file. Leave null to guess.</param>
        /// <returns>BGRA pixels.</returns>
        internal static List<MipMap> LoadImage(Stream stream, out Format Format, string extension = null)
        {
            // KFreon: See if image is built-in codec agnostic.
            Format = ImageFormats.ParseFormat(stream, extension);

            List<MipMap> output = LoadEsoterics(stream, Format);
            if (output != null)
                return output;

            // KFreon: Ok, none of those so try loading with built in codecs
            return LoadWithCodecs(stream, Format.InternalFormat);
        }


        /// <summary>
        /// Load image with internal codecs. Which set depends on OS.
        /// </summary>
        /// <param name="stream">Full Image stream.</param>
        /// <param name="Format">Detected format of image.</param>
        /// <param name="decodeWidth">Width to decode to. Leave as 0 to be natural dimensions.</param>
        /// <param name="decodeHeight">Height to decode to. Leave as 0 to be natural dimensions.</param>
        /// <returns>List of Mipmaps.</returns>
        private static List<MipMap> LoadWithCodecs(Stream stream, ImageEngineFormat Format, int decodeWidth = 0, int decodeHeight = 0)
        {
            List<MipMap> MipMaps = new List<MipMap>();
            if (WindowsWICCodecsAvailable)
                return Win8_10.LoadWithCodecs(stream, decodeWidth, decodeHeight, Format.ToString().Contains("DDS"));
            else
            {
                int height = 0;
                int width = 0;

                // KFreon: Handle DXT formats - all other DDS formats are above
                switch (Format)
                {
                    case ImageEngineFormat.DDS_DXT1:
                        MipMaps = BC1.Load(stream);
                        break;
                    case ImageEngineFormat.DDS_DXT2:
                    case ImageEngineFormat.DDS_DXT3:
                        MipMaps = BC2.Load(stream);
                        break;
                    case ImageEngineFormat.DDS_DXT4:
                    case ImageEngineFormat.DDS_DXT5:
                        MipMaps = BC3.Load(stream);
                        break;
                }


                // Resize if necessary
                bool needsResize = decodeWidth != 0 || decodeHeight != 0;
                if (MipMaps.Count == 0)
                {
                    // KFreon: None of those, so assume standard image formats (jpg, png, etc)
                    var mipdata = Win7.LoadImageWithCodecs(stream, out width, out height);
                    MipMaps.Add(new MipMap(mipdata, width, height));
                }


                // KFreon: Failed to load with anything. Bounce.
                if (MipMaps.Count == 0)
                    throw new InvalidDataException("Image incompatable with Windows 7 and/or internal codecs.");


                // KFreon: Only allow resizing if there's no mipmaps
                if (MipMaps.Count == 1 && needsResize)
                {
                    // KFreon: No Mip, so resize
                    System.Drawing.Bitmap img = new System.Drawing.Bitmap(MipMaps[0].Data);
                    System.Drawing.Bitmap newimg = (System.Drawing.Bitmap)UsefulThings.WinForms.Misc.resizeImage(img, new System.Drawing.Size(decodeWidth, decodeHeight));

                    var data = UsefulThings.RecyclableMemoryManager.GetStream(UsefulThings.WinForms.Misc.GetPixelDataFromBitmap(newimg));
                    MipMaps[0].Data = data;
                    MipMaps[0].Width = decodeWidth;
                    MipMaps[0].Height = decodeHeight;
                }
            }
            return MipMaps;
        }


        /// <summary>
        /// Loads image from file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="Format">Detected format.</param>
        /// <param name="desiredMaxDimension">Largest dimension to load as.</param>
        /// <returns>List of Mipmaps.</returns>
        internal static List<MipMap> LoadImage(string imagePath, out Format Format, int desiredMaxDimension)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadImage(fs, out Format, Path.GetExtension(imagePath), desiredMaxDimension);
        }


        /// <summary>
        /// Loads image from stream.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="Format">Detected format.</param>
        /// <param name="extension">File extension. Used to determine format more easily.</param>
        /// <param name="desiredMaxDimension">Largest dimension to load as.</param>
        /// <returns>List of Mipmaps.</returns>
        internal static List<MipMap> LoadImage(Stream stream, out Format Format, string extension, int desiredMaxDimension)
        {
            // KFreon: See if image is built-in codec agnostic.
            Format = ImageFormats.ParseFormat(stream, extension);
            List<MipMap> MipMaps = LoadEsoterics(stream, Format);
            if (MipMaps != null && MipMaps.Count != 0)
            {
                if (desiredMaxDimension == 0)
                    return MipMaps;

                // scale and return;
                if (WindowsWICCodecsAvailable)
                {
                    var sizedMip = MipMaps.Where(m => m.Width > m.Height ? m.Width == desiredMaxDimension : m.Height == desiredMaxDimension);
                    if (sizedMip != null && sizedMip.Any())  // KFreon: If there's already a mip, return that.
                    {
                        var mip = sizedMip.First();
                        MipMaps.Clear();
                        MipMaps.Add(mip);
                    }
                    else
                    {
                        // Get top mip and clear others.
                        var mip = MipMaps[0];
                        MipMaps.Clear();

                        //int stride = 4 * (mip.Width * 32 + 31) / 32;
                        int stride = 4 * mip.Width;
                        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                        BitmapFrame frame = BitmapFrame.Create(BitmapFrame.Create(mip.Width, mip.Height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent, mip.Data.ToArray(), stride));
                        encoder.Frames.Add(frame);
                        var output = UsefulThings.RecyclableMemoryManager.GetStream((int)mip.Data.Length);
                        encoder.Save(output);
                        MipMaps.Add(mip);
                    }
                }
                else
                {
                    // Get top mip and clear others.
                    var mip = MipMaps[0];
                    MipMaps.Clear();

                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(mip.Width, mip.Height);
                    var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, mip.Width, mip.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    byte[] pixels = new byte[mip.Data.Length];
                    Marshal.Copy(data.Scan0, pixels, 0, (int)mip.Data.Length);
                    bmp.UnlockBits(data);
                    MipMap newmip = new MipMap(UsefulThings.RecyclableMemoryManager.GetStream(pixels), mip.Width, mip.Height);
                    MipMaps.Add(newmip);
                }
            }
            else
                MipMaps = LoadWithCodecs(stream, Format.InternalFormat, desiredMaxDimension, desiredMaxDimension);

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

            if (temp.IsMippable && GenerateMips)
                BuildMipMaps(MipMaps);

            // KFreon: Resize if asked
            if (maxDimension != 0)
            {
                if (!UsefulThings.General.IsPowerOfTwo(maxDimension))
                    throw new ArgumentException($"{nameof(maxDimension)} must be a power of 2. Got {nameof(maxDimension)} = {maxDimension}");

                // KFreon: Check if there's a mipmap suitable, removes all larger mipmaps
                var validMipmap = MipMaps.Where(img => (img.Width == maxDimension && img.Height <= maxDimension) || (img.Height == maxDimension && img.Width <=maxDimension));  // Check if a mip dimension is maxDimension and that the other dimension is equal or smaller
                if (validMipmap?.Count() != 0)
                {
                    int index = MipMaps.IndexOf(validMipmap.First());
                    for (int i = 0; i < index; i++)
                        MipMaps[i].Data.Dispose();

                    MipMaps.RemoveRange(0, index);
                }
                else
                {
                    // KFreon: Get the amount the image needs to be scaled. Find largest dimension and get it's scale.
                    double scale = maxDimension * 1f / (MipMaps[0].Width > MipMaps[0].Height ? MipMaps[0].Width: MipMaps[0].Height);
                    /*Debug.WriteLine($"width: {MipMaps[0].Width}  height: {MipMaps[0].Height}  scale: {scale}");
                    Debug.WriteLine($"scaled width: {MipMaps[0].Width * scale}   scaled height: {MipMaps[0].Height * scale}");*/

                    // KFreon: No mip. Resize.
                    MipMaps[0] = Resize(MipMaps[0], scale);
                }
            }

            if (!GenerateMips)
                DestroyMipMaps(MipMaps);

            // KFreon: Try DDS formats first
            switch (format)
            {
                case ImageEngineFormat.DDS_V8U8:
                    return V8U8.Save(MipMaps, destination);
                case ImageEngineFormat.DDS_G8_L8:
                    return G8_L8.Save(MipMaps, destination);
                case ImageEngineFormat.DDS_ATI1:
                    return BC4_ATI1.Save(MipMaps, destination);
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    return BC5_ATI2_3Dc.Save(MipMaps, destination);
                case ImageEngineFormat.DDS_ARGB:
                    return RGBA.Save(MipMaps, destination);
                case ImageEngineFormat.DDS_DXT1:
                    return BC1.Save(MipMaps, destination);
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                    return BC2.Save(MipMaps, destination);
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    return BC3.Save(MipMaps, destination);
            }

            // KFreon: NOT any of the above then...

            // KFreon: Try saving with built in codecs
            var mip = MipMaps[0];
            if (WindowsWICCodecsAvailable)
                return Win8_10.SaveWithCodecs(mip.Data, destination, format, mip.Width, mip.Height);
            else
                return Win7.SaveWithCodecs(mip.Data, destination, format, mip.Width, mip.Height);
        }

        internal static MipMap Resize(MipMap mipMap, double scale)
        {
            if (WindowsWICCodecsAvailable)
                return Win8_10.Resize(mipMap, scale);
            else
                return Win7.Resize(mipMap, (int)(mipMap.Width * scale), (int)(mipMap.Height * scale));
        }


        /// <summary>
        /// Builds mipmaps. Expects at least one mipmap in given list.
        /// </summary>
        /// <param name="MipMaps">List of Mipmaps, both existing and generated.</param>
        /// <returns>Number of mips present (generated or otherwise)</returns>
        private static int BuildMipMaps(List<MipMap> MipMaps)
        {
            if (WindowsWICCodecsAvailable)
                return Win8_10.BuildMipMaps(MipMaps);
            else
                return Win7.BuildMipMaps(MipMaps);
        }


        /// <summary>
        /// Destroys mipmaps. Expects at least one mipmap in given list.
        /// </summary>
        /// <param name="MipMaps">List of Mipmaps.</param>
        /// <returns>Number of mips present.</returns>
        private static int DestroyMipMaps(List<MipMap> MipMaps)
        {
            MipMap mipmap = MipMaps[0];
            if (MipMaps.Count > 1)
            {
                foreach (var item in MipMaps)
                    if (item != mipmap)
                        item.Data.Dispose();

                MipMaps.Clear();
                MipMaps.Add(mipmap);
            }

            return MipMaps.Count;
        }

        /// <summary>
        /// Generates a thumbnail image as quickly and efficiently as possible.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        public static MemoryStream GenerateThumbnailToStream(Stream stream, int maxDimension)
        {
            Format format = new Format();
            var mipmaps = LoadImage(stream, out format, null, maxDimension);

            MemoryStream ms = UsefulThings.RecyclableMemoryManager.GetStream();
            Save(mipmaps, ImageEngineFormat.JPG, ms, false);

            foreach (var mip in mipmaps)
                mip.Data.Dispose();

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
            Format format = new Format();
            var mipmaps = LoadImage(stream, out format, null, maxDimension);

            bool success = false;
            using (FileStream fs = new FileStream(destination, FileMode.Create))
                success = Save(mipmaps, ImageEngineFormat.JPG, fs, false);

            foreach (var mip in mipmaps)
                mip.Data.Dispose();

            return success;
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

            if (format.Contains("dxt2", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT2;

            if (format.Contains("dxt3", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT3;

            if (format.Contains("dxt4", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT4;

            if (format.Contains("dxt5", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_DXT5;

            if (format.Contains("bmp", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.BMP;

            if (format.Contains("argb", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_ARGB;

            if (format.Contains("ati1", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_ATI1;

            if (format.Contains("ati2", StringComparison.OrdinalIgnoreCase) || format.Contains("3dc", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_ATI2_3Dc;

            if (format.Contains("l8", StringComparison.OrdinalIgnoreCase) || format.Contains("g8", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_G8_L8;

            if (format.Contains("v8u8", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.DDS_V8U8;

            if (format.Contains("jpg", StringComparison.OrdinalIgnoreCase) || format.Contains("jpeg", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.JPG;

            if (format.Contains("png", StringComparison.OrdinalIgnoreCase))
                parsedFormat = ImageEngineFormat.PNG;


            return parsedFormat;
        }
    }
}
