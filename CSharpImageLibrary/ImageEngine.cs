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

                    var data = new MemoryTributary(UsefulThings.WinForms.Misc.GetPixelDataFromBitmap(newimg));
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
            if (MipMaps != null)
            {
                // scale and return;
                if (WindowsWICCodecsAvailable)
                {
                    var sizedMip = MipMaps.Where(m => m.Width > m.Height ? m.Width == desiredMaxDimension : m.Height == desiredMaxDimension);
                    if (sizedMip != null)  // KFreon: If there's already a mip, return that.
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

                        int stride = 4 * (mip.Width * 32 + 31) / 32;
                        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                        BitmapFrame frame = BitmapFrame.Create(BitmapFrame.Create(mip.Width, mip.Height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent, mip.Data.ToArray(), stride));
                        encoder.Frames.Add(frame);
                        var output = new MemoryTributary((int)mip.Data.Length);
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
                    MipMap newmip = new MipMap(new MemoryTributary(pixels), mip.Width, mip.Height);
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
        /// <param name="GenerateMips">True = Generate mipmaps for mippable images.</param>
        /// <returns>True on success.</returns>
        internal static bool Save(List<MipMap> MipMaps, ImageEngineFormat format, Stream destination, bool GenerateMips)
        {
            Format temp = new Format(format);

            if (temp.IsMippable && GenerateMips)
                BuildMipMaps(MipMaps);

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
        /// Generates a thumbnail image as quickly and efficiently as possible.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="sourceFormat">Format of original image.</param>
        /// <param name="newWidth">Desired width.</param>
        /// <param name="newHeight">Desired height.</param>
        /// <returns>Formatted thumbnail as stream.</returns>
        public static MemoryTributary GenerateThumbnail(Stream stream, ImageEngineFormat sourceFormat, int newWidth, int newHeight)
        {
            // KFreon: No codecs save any DDS', so use mine/everyone elses
            if (sourceFormat.ToString().Contains("DDS"))
            {
                /*ImageEngineImage img = new ImageEngineImage(stream, "dds");
                img.Save()*/
                // TODO: Needs fixing
            }

            // jpg, bmp, png
            if (WindowsWICCodecsAvailable)
                return Win8_10.GenerateThumbnail(stream, newWidth, newHeight);
            else
                return Win7.GenerateThumbnail(stream, newWidth, newHeight);
        }
    }
}
