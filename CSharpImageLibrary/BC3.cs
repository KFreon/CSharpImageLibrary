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
        /// <returns>16 byte RGBA channels as stream.</returns>
        internal static MemoryTributary Load(string imageFile, out double Width, out double Height)
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
        /// <returns>16 byte RGBA channels as stream.</returns>
        internal static MemoryTributary Load(Stream compressed, out double Width, out double Height)
        {
            return DDSGeneral.LoadBlockCompressedTexture(compressed, out Width, out Height, DecompressBC3);
        }


        /// <summary>
        /// Reads a 16 byte BC3 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC3 compressed image stream.</param>
        /// <returns>List of RGBA channels.</returns>
        private static List<byte[]> DecompressBC3(Stream compressed)
        {
            byte[] alpha = DDSGeneral.Decompress8BitBlock(compressed, false);
            List<byte[]> DecompressedBlock = DDSGeneral.DecompressRGBBlock(compressed);
            DecompressedBlock[3] = alpha;
            return DecompressedBlock;
        }

        internal static bool Save(object stream)
        {
            throw new NotImplementedException();
        }
    }
}
