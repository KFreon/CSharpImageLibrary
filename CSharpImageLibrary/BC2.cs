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
    /// Provides Block Compressed 2 (BC2) functionality. Also known as DXT2 and 3.
    /// </summary>
    public static class BC2
    {
        /// <summary>
        /// Load important information from BC2 image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>16 byte RGBA channels as stream.</returns>
        internal static MemoryTributary Load(string imageFile, out int Width, out int Height)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }


        /// <summary>
        /// Load important information from BC2 image stream.
        /// </summary>
        /// <param name="compressed">Stream containing entire image. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>16 byte RGBA channels as stream.</returns>
        internal static MemoryTributary Load(Stream compressed, out int Width, out int Height)
        {
            return DDSGeneral.LoadBlockCompressedTexture(compressed, out Width, out Height, DecompressBC2);
        }

        
        /// <summary>
        /// Reads a 16 byte BC2 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC2 compressed stream.</param>
        /// <returns>RGBA channels.</returns>
        private static List<byte[]> DecompressBC2(Stream compressed)
        {
            // KFreon: Read alpha
            byte[] alpha = new byte[16];
            using (BinaryReader br = new BinaryReader(compressed, Encoding.Default, true))
                for (int i = 0; i < 16; i++)
                    alpha[i] = (byte)br.ReadInt16();  // KFreon: Alpha values are shorts, so why not read them as such?

            // KFreon: Organise output by adding alpha channel (channel read in RGB block is empty)
            List<byte[]> DecompressedBlock = DDSGeneral.DecompressRGBBlock(compressed);
            DecompressedBlock[3] = alpha;
            return DecompressedBlock;
        }

        internal static bool Save(Stream pixelsWithMips, Stream destination)
        {
            throw new NotImplementedException();
        }
    }
}
