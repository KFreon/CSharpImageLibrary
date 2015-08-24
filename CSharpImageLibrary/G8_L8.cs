using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary
{
    public static class G8_L8
    {
        /// <summary>
        /// Loads useful information from G8_L8 image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <returns>RGBA Pixel data as stream.</returns>
        public static MemoryTributary Load(string imageFile, out double Width, out double Height)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }


        /// <summary>
        /// Loads useful information from G8_L8 image stream.
        /// </summary>
        /// <param name="stream">Stream containing entire image. NOT just pixels.</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <returns>RGBA Pixel Data as stream.</returns>
        public static MemoryTributary Load(Stream stream, out double Width, out double Height)
        {
            // KFreon: Read pixel data. Note: No blue channel. Only 2 colour channels.
            Func<Stream, int> PixelReader = fileData =>
            {
                sbyte red = (sbyte)fileData.ReadByte();
                byte green = 0xFF;
                byte blue = 0xFF;

                return blue | (0x7F + green) << 8 | (0x7F + red) << 16 | 0xFF << 24;
            };

            return DDSGeneral.LoadUncompressed(stream, 1, out Width, out Height, PixelReader);
        }
    }
}
