using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides ATI2_3Dc (Block Compressed 5 [BC5]) functionality.
    /// </summary>
    internal static class BC5_ATI2_3Dc
    {
        /// <summary>
        /// Loads ATI2 (BC5) image from file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>BGRA pixels.</returns>
        internal static MemoryTributary Load(string imagePath, out int Width, out int Height)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }


        /// <summary>
        /// Loads ATI2 (BC5) image from stream.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>BGRA pixels.</returns>
        internal static MemoryTributary Load(Stream stream, out int Width, out int Height)
        {
            return DDSGeneral.LoadBlockCompressedTexture(stream, out Width, out Height, DecompressATI2Block);
        }


        /// <summary>
        /// Decompresses ATI2 (BC5) block.
        /// </summary>
        /// <param name="compressed">Compressed data stream.</param>
        /// <returns>16 pixel BGRA channels.</returns>
        private static List<byte[]> DecompressATI2Block(Stream compressed)
        {
            byte[] red = DDSGeneral.Decompress8BitBlock(compressed, false);
            byte[] green = DDSGeneral.Decompress8BitBlock(compressed, false);
            List<byte[]> DecompressedBlock = new List<byte[]>();
            DecompressedBlock.Add(red);
            DecompressedBlock.Add(green);
            DecompressedBlock.Add(new byte[16]);
            DecompressedBlock.Add(new byte[16]);

            return DecompressedBlock;
        }


        /// <summary>
        /// Compresses texel to 16 byte BC5 block.
        /// </summary>
        /// <param name="texel">4x4 BGRA set of pixels.</param>
        /// <returns>16 byte BC5 block.</returns>
        private static byte[] CompressBC5Block(byte[] texel)
        {
            byte[] red = DDSGeneral.Compress8BitBlock(texel, 2, false);
            byte[] green = DDSGeneral.Compress8BitBlock(texel, 1, false);

            return red.Concat(green).ToArray(red.Length + green.Length);
        }


        /// <summary>
        /// Saves texture using BC5 compression.
        /// </summary>
        /// <param name="pixelsWithMips">4 channel stream containing mips (if requested).</param>
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Mips">Number of mips in pixelsWithMips (1 if no mips).</param>
        /// <returns>True if saved successfully.</returns>
        internal static bool Save(MemoryTributary pixelsWithMips, Stream Destination, int Width, int Height, int Mips)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(Mips, Height, Width, ImageEngineFormat.DDS_ATI2_3Dc);
            return DDSGeneral.WriteBlockCompressedDDS(pixelsWithMips, Destination, Width, Height, Mips, header, CompressBC5Block);
        }
    }
}
