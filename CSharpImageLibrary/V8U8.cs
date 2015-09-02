using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides V8U8 format functionality.
    /// </summary>
    internal static class V8U8
    {
        /// <summary>
        /// Loads useful information from V8U8 image.
        /// </summary>
        /// <param name="imagePath">Path to V8U8 image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>BGRA Pixel data as stream.</returns>
        internal static List<MipMap> Load(string imagePath)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs);
        }


        /// <summary>
        /// Loads useful information from V8U8 image stream.
        /// </summary>
        /// <param name="stream">Stream containing entire V8U8 image. Not just Pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>BGRA Pixel data as stream.</returns>
        internal static List<MipMap> Load(Stream stream)
        {
            // KFreon: Read pixel data. Note: No blue channel. Only 2 colour channels.
            Func<Stream, List<byte>> PixelReader = fileData =>
            {
                byte red = (byte)(fileData.ReadByte() - 130);
                byte green = (byte)(fileData.ReadByte() - 130);
                //Debug.WriteLine($"Red: {red}  Green: {green}");
                byte blue = 0xFF;

                //return blue | (0x7F + green) << 8 | (0x7F + red) << 16;
                return new List<byte>() { blue, green, red };
            };

            return DDSGeneral.LoadUncompressed(stream, PixelReader);
        }

        internal static bool Save(List<MipMap> MipMaps, Stream destination)
        {
            Action<BinaryWriter, Stream, int> PixelWriter = (writer, pixels, unused) =>
            {
                // BGRA
                pixels.Position++; // No blue
                byte[] colours = new byte[2];

                pixels.Read(colours, 0, 2);

                /*byte green = (byte)(pixels.ReadByte() + 130);
                byte red = (byte)(pixels.ReadByte() + 130);

                writer.Write(red);  // Red
                writer.Write(green);  // Green*/

                writer.Write(colours);

                pixels.Position++;    // No alpha
            };


            var header = DDSGeneral.Build_DDS_Header(MipMaps.Count, MipMaps[0].Height, MipMaps[0].Width, ImageEngineFormat.DDS_V8U8);
            return DDSGeneral.WriteDDS(MipMaps, destination, header, PixelWriter, false);
        }
    }
}
