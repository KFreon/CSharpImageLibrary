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


        private static List<byte[]> CompressBC1Block(Stream uncompressed)
        {
            
        }

        internal static bool Save(Stream PixelData, Stream Destination)
        {
            // Loop over texels
        }
    }
}
