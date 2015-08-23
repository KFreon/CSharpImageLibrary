using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;
using static CSharpImageLibrary.DDSGeneral;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides V8U8 format functionality
    /// </summary>
    public static class V8U8
    {
        /// <summary>
        /// Loads useful information from V8U8 image.
        /// </summary>
        /// <param name="imagePath">Path to V8U8 image file.</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <returns>Raw pixel data as stream.</returns>
        private static MemoryTributary Load(string imagePath, out double Width, out double Height)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }


        /// <summary>
        /// Loads useful information from V8U8 image stream.
        /// </summary>
        /// <param name="stream">Stream containing entire V8U8 image. Not just Pixels.</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <returns>Raw pixel data as stream.</returns>
        private static MemoryTributary Load(Stream stream, out double Width, out double Height)
        {
            // KFreon: Necessary to move stream position along to pixel data.
            DDS_HEADER header = null;
            Format format = ParseDDSFormat(stream, out header);

            Width = header.dwWidth;
            Height = header.dwHeight;

            int mipMapBytes = (int)(Width * Height * 2);  // KFreon: 2 bytes per pixel
            MemoryTributary imgData = new MemoryTributary();

            // KFreon: Read pixel data. Note: No blue channel. Only 2 colour channels.
            using (BinaryWriter writer = new BinaryWriter(imgData, Encoding.Default, true))
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        sbyte red = (sbyte)stream.ReadByte();
                        sbyte green = (sbyte)stream.ReadByte();
                        byte blue = 0xFF;

                        int fCol = blue | (0x7F + green) << 8 | (0x7F + red) << 16 | 0xFF << 24;
                        writer.Write(fCol);
                    }
                }
            }


            return imgData;
        }
    }
}
