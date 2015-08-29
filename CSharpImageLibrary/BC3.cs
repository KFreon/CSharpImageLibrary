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
    /// Provides Block Compressed 3 functionality. Also known as DXT4 and 5.
    /// </summary>
    public static class BC3
    {
        /// <summary>
        /// Load important information from image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>16 byte BGRA channels as stream.</returns>
        internal static MemoryTributary Load(string imageFile, out int Width, out int Height)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }


        /// <summary>
        /// Load important information from image stream.
        /// </summary>
        /// <param name="compressed">Stream containing entire image file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>16 byte BGRA channels as stream.</returns>
        internal static MemoryTributary Load(Stream compressed, out int Width, out int Height)
        {
            return DDSGeneral.LoadBlockCompressedTexture(compressed, out Width, out Height, DecompressBC3);
        }


        /// <summary>
        /// Reads a 16 byte BC3 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC3 compressed image stream.</param>
        /// <returns>List of BGRA channels.</returns>
        private static List<byte[]> DecompressBC3(Stream compressed)
        {
            byte[] alpha = DDSGeneral.Decompress8BitBlock(compressed, false);
            List<byte[]> DecompressedBlock = DDSGeneral.DecompressRGBBlock(compressed, false);
            DecompressedBlock[3] = alpha;
            return DecompressedBlock;
        }


        /// <summary>
        /// Compress texel to 16 byte BC3 compressed block.
        /// </summary>
        /// <param name="texel">4x4 BGRA set of pixels.</param>
        /// <returns>16 byte BC3 compressed block.</returns>
        private static byte[] CompressBC3Block(byte[] texel)
        {
            // Compress Alpha
            byte[] Alpha = DDSGeneral.Compress8BitBlock(texel, 3, false);

            // Compress Colour
            byte[] RGB = DDSGeneral.CompressRGBBlock(texel, true);

            return Alpha.Concat(RGB).ToArray(Alpha.Length + RGB.Length);
        }


        /// <summary>
        /// Saves a texture using BC3 compression.
        /// </summary>
        /// <param name="pixelsWithMips">4 channel stream containing mips (if requested)</param>
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Width.</param>
        /// <param name="Mips">Number of mips in pixelWithMips (1 if no mips).</param>
        /// <returns>True if saved successfully.</returns>
        internal static bool Save(MemoryTributary pixelsWithMips, Stream Destination, int Width, int Height, int Mips)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(Mips, Height, Width, ImageEngineFormat.DDS_DXT1);
            return DDSGeneral.WriteBlockCompressedDDS(pixelsWithMips, Destination, Width, Height, Mips, header, CompressBC3Block);
        }
    }
}
