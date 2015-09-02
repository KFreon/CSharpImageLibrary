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
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>BGRA Pixel Data as stream.</returns>
        internal static List<MipMap> Load(Stream stream)
        {
            // KFreon: Read pixel data. Note: No blue channel. Only 2 colour channels.
            Func<Stream, List<byte>> PixelReader = fileData =>
            {
                byte red = (byte)fileData.ReadByte();
                byte green = red;
                byte blue = red;

                //int test = blue | (0x7F + green) << 8 | (0x7F + red) << 16;
                //int test = blue | green << 8 | red << 16;

                return new List<byte>() { blue, green, red };
            };

            return DDSGeneral.LoadUncompressed(stream, PixelReader);
        }

        internal static bool Save(List<MipMap> MipMaps, Stream destination)
        {
            Action<BinaryWriter, Stream, int> PixelWriter = (writer, pixels, unused) =>
            {
                // BGRA
                byte[] colours = new byte[3];
                pixels.Read(colours, 0, 3);
                /*byte blue = (byte)pixels.ReadByte();
                byte green = (byte)pixels.ReadByte();
                byte red = (byte)pixels.ReadByte();*/
                //byte alpha = (byte)pixels.ReadByte();
                pixels.Position++;  // Skip alpha

                int b1 = (int)(colours[0] * 3 * 0.082);
                int g1 = (int)(colours[1] * 3 * 0.6094);
                int r1 = (int)(colours[2] * 3 * 0.3086);

                int test = (int)((b1 + g1 + r1)/ 3f);
                writer.Write((byte)test);
            };

            var header = DDSGeneral.Build_DDS_Header(MipMaps.Count, MipMaps[0].Height, MipMaps[0].Width, ImageEngineFormat.DDS_G8_L8);
            return DDSGeneral.WriteDDS(MipMaps, destination, header, PixelWriter, false);
        }
    }
}
