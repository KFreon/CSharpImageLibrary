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
    /// Provides ATI1 (BC4) format functionality.
    /// This is a single channel, 8 bit image.
    /// </summary>
    internal static class ATI1
    {
        /// <summary>
        /// Loads important information from ATI1 image.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height</param>
        /// <returns>RGBA pixel data as stream.</returns>
        internal static MemoryTributary Load(string imagePath, out double Width, out double Height)
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
        internal static MemoryTributary Load(Stream stream, out double Width, out double Height)
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
            byte[] channel = DDSGeneral.Decompress8BitBlock(compressed);
            List<byte[]> DecompressedBlock = new List<byte[]>();
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(new byte[16]);  // No alpha
            return DecompressedBlock;
        }
    }
}
