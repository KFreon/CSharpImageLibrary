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
        /// <returns>RGBA pixels.</returns>
        internal static MemoryTributary Load(string imagePath, out double Width, out double Height)
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
        /// <returns>RGBA pixels.</returns>
        internal static MemoryTributary Load(Stream stream, out double Width, out double Height)
        {
            return DDSGeneral.LoadBlockCompressedTexture(stream, out Width, out Height, DecompressATI2Block);
        }


        /// <summary>
        /// Decompresses ATI2 (BC5) block.
        /// </summary>
        /// <param name="compressed">Compressed data stream.</param>
        /// <returns>16 pixel RGBA channels.</returns>
        private static List<byte[]> DecompressATI2Block(Stream compressed)
        {
            byte[] red = DDSGeneral.Decompress8BitBlock(compressed);
            byte[] green = DDSGeneral.Decompress8BitBlock(compressed);
            List<byte[]> DecompressedBlock = new List<byte[]>();
            DecompressedBlock.Add(red);
            DecompressedBlock.Add(green);
            DecompressedBlock.Add(new byte[16]);
            DecompressedBlock.Add(new byte[16]);

            return DecompressedBlock;
        }

        internal static bool Save(Stream destination)
        {
            throw new NotImplementedException();
        }
    }
}
