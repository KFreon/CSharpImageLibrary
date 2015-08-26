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
        /// <returns>RGBA Pixels as stream.</returns>
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
        /// <returns>RGBA Pixels as stream.</returns>
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
        /// <returns>RGBA pixels as stream.</returns>
        internal static MemoryTributary LoadImageWithCodecs(Bitmap bmp, out int Width, out int Height, string extension = null)
        {
            byte[] imgData = UsefulThings.WinForms.Misc.GetPixelDataFromBitmap(bmp);

            Width = bmp.Width;
            Height = bmp.Width;
            Debugger.Break();

            return new MemoryTributary(imgData);
        }

        internal static bool SaveWithCodecs(object stream)
        {
            throw new NotImplementedException();
        }

        internal static int BuildMipMaps(Stream pixelData, Stream destination)
        {
            throw new NotImplementedException();
        }

        internal static bool SaveWithCodecs(Stream pixelsWithMips, Stream destination)
        {
            throw new NotImplementedException();
        }
    }
}
