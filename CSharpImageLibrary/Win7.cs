using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides access to standard GDI+ Windows image formats.
    /// </summary>
    internal class Win7
    {
        /// <summary>
        /// Attempts to load image using GDI+ codecs.
        /// Returns null on failure.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <returns>Bitmap of image, or null if failed.</returns>
        private static Bitmap AttemptWindowsCodecs(string imageFile)
        {
            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(imageFile);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return bmp;
        }


        /// <summary>
        /// Attempts to load image using GDI+ codecs.
        /// </summary>
        /// <param name="stream">Entire file. NOT just pixels.</param>
        /// <returns>Bitmap of image, or null if failed.</returns>
        private static Bitmap AttemptWindowsCodecs(Stream stream)
        {
            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(stream);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return bmp;
        }


        /// <summary>
        /// Loads image with Windows GDI+ codecs.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>BGRA Pixels as stream.</returns>
        internal static MemoryTributary LoadImageWithCodecs(string imageFile, out int Width, out int Height)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadImageWithCodecs(fs, out Width, out Height, Path.GetExtension(imageFile));
        }


        /// <summary>
        /// Loads image with Windows GDI+ codecs.
        /// </summary>
        /// <param name="stream">Entire file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="extension"></param>
        /// <returns>BGRA Pixels as stream.</returns>
        internal static MemoryTributary LoadImageWithCodecs(Stream stream, out int Width, out int Height, string extension = null)
        {
            Bitmap bmp = AttemptWindowsCodecs(stream);

            if (bmp == null)
            {
                Width = 0;
                Height = 0;
                return null;
            }

            MemoryTributary imgData = LoadImageWithCodecs(bmp, out Width, out Height, extension);
            bmp.Dispose();
            return imgData;
        }


        /// <summary>
        /// Loads image with Windows GDI+ codecs.
        /// </summary>
        /// <param name="bmp">Bitmap to load.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="extension">Extension of original file. Leave null to guess.</param>
        /// <returns>BGRA pixels as stream.</returns>
        internal static MemoryTributary LoadImageWithCodecs(Bitmap bmp, out int Width, out int Height, string extension = null)
        {
            byte[] imgData = UsefulThings.WinForms.Misc.GetPixelDataFromBitmap(bmp);

            Width = bmp.Width;
            Height = bmp.Width;

            return new MemoryTributary(imgData);
        }

        internal static int BuildMipMaps(MemoryTributary pixelData, Stream destination, int Width, int Height)
        {
            Image bmp = UsefulThings.WinForms.Misc.CreateBitmap(pixelData.ToArray(), Width, Height);
            int determiningDimension = Width > Height ? Height : Width;
            int newWidth = Width;
            int newHeight = Height;
            int count = 1;

            while (determiningDimension > 1)
            {
                newWidth /= 2;
                newHeight /= 2;
                bmp = UsefulThings.WinForms.Misc.resizeImage(bmp, new Size(newWidth, newHeight));

                byte[] data = UsefulThings.WinForms.Misc.GetPixelDataFromBitmap((Bitmap)bmp);
                destination.Write(data, 0, data.Length);

                determiningDimension /= 2;
                count++;
            }

            return count;
        }

        internal static bool SaveWithCodecs(MemoryTributary pixelsWithMips, Stream destination, ImageEngineFormat format, int Width, int Height)
        {
            Bitmap bmp = UsefulThings.WinForms.Misc.CreateBitmap(pixelsWithMips.ToArray(), Width, Height);

            // KFreon: Get format
            System.Drawing.Imaging.ImageFormat imgformat = null;
            switch (format)
            {
                case ImageEngineFormat.BMP:
                    imgformat = System.Drawing.Imaging.ImageFormat.Bmp;
                    break;
                case ImageEngineFormat.JPG:
                    imgformat = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
                case ImageEngineFormat.PNG:
                    imgformat = System.Drawing.Imaging.ImageFormat.Png;
                    break;
            }

            if (imgformat == null)
                throw new InvalidDataException($"Unable to parse format to Windows 7 codec format: {format}");

            bmp.Save(destination, imgformat);

            return true;
        }

        internal static MemoryTributary GenerateThumbnail(Stream stream, int newWidth, int newHeight)
        {
            Bitmap bmp = new Bitmap(stream);
            Bitmap resized = new Bitmap(bmp, new Size(newWidth, newHeight));
            byte[] data = UsefulThings.WinForms.Misc.GetPixelDataFromBitmap(resized);
            return new MemoryTributary(data);
        }
    }
}
