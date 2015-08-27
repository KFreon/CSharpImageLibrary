using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides G8_L8 functionality.
    /// </summary>
    internal static class G8_L8
    {
        /// <summary>
        /// Loads useful information from G8_L8 image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>RGBA Pixel data as stream.</returns>
        internal static MemoryTributary Load(string imageFile, out int Width, out int Height)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }


        /// <summary>
        /// Loads useful information from G8_L8 image stream.
        /// </summary>
        /// <param name="stream">Stream containing entire image. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>RGBA Pixel Data as stream.</returns>
        internal static MemoryTributary Load(Stream stream, out int Width, out int Height)
        {
            // KFreon: Read pixel data. Note: No blue channel. Only 2 colour channels.
            Func<Stream, int> PixelReader = fileData =>
            {
                sbyte red = (sbyte)fileData.ReadByte();
                byte green = 0xFF;
                byte blue = 0xFF;

                return blue | (0x7F + green) << 8 | (0x7F + red) << 16 | 0xFF << 24;
            };

            return DDSGeneral.LoadUncompressed(stream, out Width, out Height, PixelReader);
        }

        internal static bool Save(Stream pixelData, Stream destination, int Width, int Height, int Mips)
        {
            Action<BinaryWriter, Stream> PixelWriter = (writer, pixels) =>
            {
                writer.Write(pixels.ReadByte());  // Red
                pixels.Position += 3;    // No green, blue, or alpha
            };

            var header = DDSGeneral.Build_DDS_Header(Mips, Height, Width, ImageEngineFormat.DDS_G8_L8);
            return DDSGeneral.WriteDDS(pixelData, destination, Width, Height, Mips, header, PixelWriter);
        }
    }
}
