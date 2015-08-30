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
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Format">Detected image format.</param>
        /// <returns>Raw pixel data as stream.</returns>
        internal static MemoryTributary LoadImage(string imagePath, out int Width, out int Height, out Format Format)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadImage(fs, out Width, out Height, out Format);
        }

        private static MemoryTributary LoadEsoterics(Stream stream, out int Width, out int Height, Format Format)
        {
            switch (Format.InternalFormat)
            {
                case ImageEngineFormat.DDS_V8U8:
                    return V8U8.Load(stream, out Width, out Height);
                case ImageEngineFormat.DDS_G8_L8:
                    return G8_L8.Load(stream, out Width, out Height);
                case ImageEngineFormat.DDS_ATI1:
                    return BC4_ATI1.Load(stream, out Width, out Height);
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    return BC5_ATI2_3Dc.Load(stream, out Width, out Height);
                case ImageEngineFormat.DDS_ARGB:
                    return RGBA.Load(stream, out Width, out Height);
            }

            Width = 0;
            Height = 0;
            return null;
        }

        /// <summary>
        /// Loads image from image stream.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Format">Image format (dds surfaces, jpg, png, etc)</param>
        /// <param name="extension">Extension of original file. Leave null to guess.</param>
        /// <returns>BGRA pixels.</returns>
        internal static MemoryTributary LoadImage(Stream stream, out int Width, out int Height, out Format Format, string extension = null)
        {
            Width = 0;
            Height = 0;

            // KFreon: See if image is built-in codec agnostic.
            Format = ImageFormats.ParseFormat(stream, extension);

            MemoryTributary output = LoadEsoterics(stream, out Width, out Height, Format);
            if (output != null)
                return output;

            // KFreon: NOT any of the above then...

            // KFreon: Try loading with built in codecs
            return LoadWithCodecs(stream, out Width, out Height, Format.InternalFormat);
        }

        private static MemoryTributary LoadWithCodecs(Stream stream, out int Width, out int Height, ImageEngineFormat Format, int decodeWidth = 0, int decodeHeight = 0)
        {
            if (WindowsWICCodecsAvailable)
                return Win8_10.LoadWithCodecs(stream, out Width, out Height, decodeWidth, decodeHeight);
            else
            {
                int height = 0;
                int width = 0;

                // KFreon: Handle DXT formats - all other DDS formats are above
                MemoryTributary outstream = null;
                switch (Format)
                {
                    case ImageEngineFormat.DDS_DXT1:
                        outstream = BC1.Load(stream, out width, out height);
                        break;
                    case ImageEngineFormat.DDS_DXT2:
                    case ImageEngineFormat.DDS_DXT3:
                        outstream = BC2.Load(stream, out width, out height);
                        break;
                    case ImageEngineFormat.DDS_DXT4:
                    case ImageEngineFormat.DDS_DXT5:
                        outstream = BC3.Load(stream, out width, out height);
                        break;
                }

                bool needsResize = decodeWidth != 0 || decodeHeight != 0;
                if (outstream == null)
                {
                    // KFreon: None of those, so assume standard image formats (jpg, png, etc)
                    outstream = Win7.LoadImageWithCodecs(stream, out width, out height);
                }

                if (outstream == null)
                    throw new InvalidDataException("Image incompatable with Windows 7 and/or internal codecs.");

                

                if (needsResize)
                {
                    System.Drawing.Bitmap img = new System.Drawing.Bitmap(outstream);
                    System.Drawing.Bitmap newimg = (System.Drawing.Bitmap)UsefulThings.WinForms.Misc.resizeImage(img, new System.Drawing.Size(decodeWidth, decodeHeight));

                    Width = decodeWidth;
                    Height = decodeHeight;
                    return new MemoryTributary(UsefulThings.WinForms.Misc.GetPixelDataFromBitmap(newimg));
                }
                else
                {
                    Width = width;
                    Height = height;
                    return outstream;
                }
            }
        }

        internal static MemoryTributary LoadImage(string imagePath, out int Width, out int Height, out Format Format, int desiredMaxDimension)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadImage(fs, out Width, out Height, out Format, Path.GetExtension(imagePath), desiredMaxDimension);
        }

        internal static MemoryTributary LoadImage(Stream stream, out int Width, out int Height, out Format Format, string extension, int desiredMaxDimension)
        {
            Width = 0;
            Height = 0;

            // KFreon: See if image is built-in codec agnostic.
            Format = ImageFormats.ParseFormat(stream, extension);

            MemoryTributary output = LoadEsoterics(stream, out Width, out Height, Format);
            if (output != null)
            {
                // scale and return;
                if (WindowsWICCodecsAvailable)
                {
                    int stride = 4 * (Width * 32 + 31) / 32;
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    BitmapFrame frame = BitmapFrame.Create(BitmapFrame.Create(Width, Height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent, output.ToArray(), stride));
                    encoder.Frames.Add(frame);
                    output = new MemoryTributary();
                    encoder.Save(output);
                    return output;
                }
                else
                {
                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(Width, Height);
                    var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, Width, Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    byte[] pixels = new byte[output.Length];
                    Marshal.Copy(data.Scan0, pixels, 0, (int)output.Length);
                    bmp.UnlockBits(data);
                    return new MemoryTributary(pixels);
                }
            }
            else
                return LoadWithCodecs(stream, out Width, out Height, Format.InternalFormat, Width > Height ? 0 : desiredMaxDimension, Height > Width ? 0 : desiredMaxDimension);
        }
        #endregion Loading


        internal static bool Save(MemoryTributary PixelData, ImageEngineFormat format, Stream destination, int Width, int Height, bool GenerateMips)
        {
            MemoryTributary PixelsWithMips = new MemoryTributary();
            int Mips = 1;

            Format temp = new Format(format);

            if (temp.IsMippable && GenerateMips)
            {
                PixelData.Seek(0, SeekOrigin.Begin);
                PixelsWithMips.ReadFrom(PixelData, PixelData.Length);

                Mips = BuildMipMaps(PixelData, PixelsWithMips, Width, Height);
            }
            else
                PixelsWithMips = PixelData;
            

            // KFreon: Try DDS formats first
            switch (format)
            {
                case ImageEngineFormat.DDS_V8U8:
                    return V8U8.Save(PixelsWithMips, destination, Width, Height, Mips);
                case ImageEngineFormat.DDS_G8_L8:
                    return G8_L8.Save(PixelsWithMips, destination, Width, Height, Mips);
                case ImageEngineFormat.DDS_ATI1:
                    return BC4_ATI1.Save(PixelsWithMips, destination, Width, Height, Mips);
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    return BC5_ATI2_3Dc.Save(PixelsWithMips, destination, Width, Height, Mips);
                case ImageEngineFormat.DDS_ARGB:
                    return RGBA.Save(PixelsWithMips, destination, Width, Height, Mips);
                case ImageEngineFormat.DDS_DXT1:
                    return BC1.Save(PixelsWithMips, destination, Width, Height, Mips);
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                    return BC2.Save(PixelsWithMips, destination, Width, Height, Mips);
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    return BC3.Save(PixelsWithMips, destination, Width, Height, Mips);
            }

            // KFreon: NOT any of the above then...

            // KFreon: Try saving with built in codecs
            if (WindowsWICCodecsAvailable)
                return Win8_10.SaveWithCodecs(PixelsWithMips, destination, format, Width, Height);
            else
                return Win7.SaveWithCodecs(PixelsWithMips, destination, format, Width, Height);
        }

        private static int BuildMipMaps(MemoryTributary PixelData, Stream destination, int Width, int Height)
        {
            if (WindowsWICCodecsAvailable)
                return Win8_10.BuildMipMaps(PixelData, destination, Width, Height);
            else
                return Win7.BuildMipMaps(PixelData, destination, Width, Height);
        }

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
