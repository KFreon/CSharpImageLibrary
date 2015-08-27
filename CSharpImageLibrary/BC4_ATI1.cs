using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides ATI1 (Block Compressed 4 [BC4]) format functionality.
    /// This is a single channel, 8 bit image.
    /// </summary>
    internal static class BC4_ATI1
    {
        /// <summary>
        /// Loads important information from ATI1 image.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height</param>
        /// <returns>RGBA pixel data as stream.</returns>
        internal static MemoryTributary Load(string imagePath, out int Width, out int Height)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }


        /// <summary>
        /// Loads important information from ATI1 image stream.
        /// </summary>
        /// <param name="stream">Stream containing entire image file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>RGBA Pixel Data as stream.</returns>
        internal static MemoryTributary Load(Stream stream, out int Width, out int Height)
        {
            return DDSGeneral.LoadBlockCompressedTexture(stream, out Width, out Height, DecompressATI1);
        }


        /// <summary>
        /// Decompresses an ATI1 (BC4) block.
        /// </summary>
        /// <param name="compressed">Compressed data stream.</param>
        /// <returns>RGBA channels (16 bits each)</returns>
        private static List<byte[]> DecompressATI1(Stream compressed)
        {
            byte[] channel = DDSGeneral.Decompress8BitBlock(compressed, false);
            List<byte[]> DecompressedBlock = new List<byte[]>();
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(new byte[16]);  // No alpha
            return DecompressedBlock;
        }


        /// <summary>
        /// Compress texel to 8 byte BC4 compressed block.
        /// </summary>
        /// <param name="texel">4x4 RGBA set of pixels.</param>
        /// <returns>8 byte BC4 compressed block.</returns>
        private static byte[] CompressBC4Block(byte[] texel)
        {
            return DDSGeneral.Compress8BitBlock(texel, 0, false);
        }


        /// <summary>
        /// Saves texture using BC4 compression.
        /// </summary>
        /// <param name="pixelsWithMips">4 channel stream containing mips (if requested).</param>
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Mips">Number of mips in pixelsWithMips (1 if no mips).</param>
        /// <returns>True if saved successfully.</returns>
        internal static bool Save(MemoryTributary pixelsWithMips, Stream Destination, int Width, int Height, int Mips)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(Mips, Height, Width, ImageEngineFormat.DDS_ATI1);
            return DDSGeneral.WriteBlockCompressedDDS(pixelsWithMips, Destination, Width, Height, Mips, header, CompressBC4Block);
        }
    }
}
