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
    /// Provides Block Compressed 1 (BC1) functionality. Also known as DXT1.
    /// </summary>
    public static class BC1
    {
        /// <summary>
        /// Load important information from BC1 image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Width.</param>
        /// <returns>16 byte RGBA channels as stream.</returns>
        internal static MemoryTributary Load(string imageFile, out int Width, out int Height)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }


        /// <summary>
        /// Load important information from BC1 image stream.
        /// </summary>
        /// <param name="compressed">Stream containing entire image. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>16 byte RGBA channels as stream.</returns>
        internal static MemoryTributary Load(Stream compressed, out int Width, out int Height)
        {
            return DDSGeneral.LoadBlockCompressedTexture(compressed, out Width, out Height, DecompressBC1Block);
        }


        /// <summary>
        /// Read an 8 byte BC1 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC1 compressed stream.</param>
        /// <returns>RGBA channels.</returns>
        private static List<byte[]> DecompressBC1Block(Stream compressed)
        {
            return DDSGeneral.DecompressRGBBlock(compressed);
        }


        /// <summary>
        /// Compress texel to 8 byte BC1 compressed block.
        /// </summary>
        /// <param name="texel">4x4 RGBA group of pixels.</param>
        /// <returns>8 byte BC1 compressed block.</returns>
        private static byte[] CompressBC1Block(byte[] texel)
        {
            return DDSGeneral.CompressRGBBlock(texel, true);
        }


        /// <summary>
        /// Saves a texture using BC1 compression.
        /// </summary>
        /// <param name="pixelsWithMips">4 channel Stream containing mips (if required).</param>
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Mips">Number of mips in pixelsWithMips (1 if no mips).</param>
        /// <returns>True if saved successfully.</returns>
        internal static bool Save(MemoryTributary pixelsWithMips, Stream Destination, int Width, int Height, int Mips)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(Mips, Height, Width, ImageEngineFormat.DDS_DXT1);
            return DDSGeneral.WriteBlockCompressedDDS(pixelsWithMips, Destination, Width, Height, Mips, header, CompressBC1Block);
        }
    }
}
