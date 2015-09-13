using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpImageLibrary.General;
using UsefulThings;

namespace CSharpImageLibrary.Specifics
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
        /// <returns>BGRA Pixel data as stream.</returns>
        internal static List<MipMap> Load(string imageFile)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs);
        }


        /// <summary>
        /// Loads useful information from G8_L8 image stream.
        /// </summary>
        /// <param name="stream">Stream containing entire image. NOT just pixels.</param>
        /// <returns>BGRA Pixel Data as stream.</returns>
        internal static List<MipMap> Load(Stream stream)
        {
            // KFreon: Read pixel data. Note: No blue channel. Only 2 colour channels.
            Func<Stream, List<byte>> PixelReader = fileData =>
            {
                byte red = (byte)fileData.ReadByte();
                byte green = red;
                byte blue = red;  // KFreon: Same colour for other channels to make grayscale.

                return new List<byte>() { blue, green, red };
            };

            return DDSGeneral.LoadUncompressed(stream, PixelReader);
        }

        /// <summary>
        /// Saves Mipmaps to stream.
        /// </summary>
        /// <param name="MipMaps">List of mipmaps to save. Pixels only.</param>
        /// <param name="destination">Stream to save to.</param>
        /// <returns>True on success.</returns>
        internal static bool Save(List<MipMap> MipMaps, Stream destination)
        {
            Action<Stream, Stream, int, int> PixelWriter = (writer, pixels, unused, unused2) =>
            {
                // BGRA
                byte[] colours = new byte[3];
                pixels.Read(colours, 0, 3);
                pixels.Position++;  // Skip alpha

                // KFreon: Weight colours to look proper. Dunno if this affects things but anyway...Got weightings from ATi Compressonator
                int b1 = (int)(colours[0] * 3 * 0.082);
                int g1 = (int)(colours[1] * 3 * 0.6094);
                int r1 = (int)(colours[2] * 3 * 0.3086);

                int test = (int)((b1 + g1 + r1) / 3f);
                writer.WriteByte((byte)test);
            };

            var header = DDSGeneral.Build_DDS_Header(MipMaps.Count, MipMaps[0].Height, MipMaps[0].Width, ImageEngineFormat.DDS_G8_L8);
            return DDSGeneral.WriteDDS(MipMaps, destination, header, PixelWriter, false);
        }
    }
}
