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
            for (int i = 0; i < 16; i+=2)
            {
                byte twoAlphas = (byte)compressed.ReadByte();
                for (int j = 0; j < 2; j++)
                    alpha[i + j] = (byte)(twoAlphas << (j * 4));
            }
                    

            // KFreon: Organise output by adding alpha channel (channel read in RGB block is empty)
            List<byte[]> DecompressedBlock = DDSGeneral.DecompressRGBBlock(compressed, false);
            DecompressedBlock[3] = alpha;
            return DecompressedBlock;
        }


        /// <summary>
        /// Compress texel to 16 byte BC2 compressed block.
        /// </summary>
        /// <param name="texel">4x4 RGBA set of pixels.</param>
        /// <returns>16 byte BC2 compressed block.</returns>
        private static byte[] CompressBC2Block(byte[] texel)
        {
            // Compress Alpha
            byte[] Alpha = new byte[8];
            for (int i = 4; i < 64; i += 4)  // Only read alphas
            {
                byte twoAlpha = 0;
                for (int j = 0; j < 2; j++)
                    twoAlpha |= (byte)(texel[i] << (j * 4));
                Alpha[i / 4] = twoAlpha;
            }

            // Compress Colour
            byte[] RGB = DDSGeneral.CompressRGBBlock(texel, true);

            return Alpha.Concat(RGB).ToArray(Alpha.Length + RGB.Length);
        }


        /// <summary>
        /// Saves texture using BC2 compression.
        /// </summary>
        /// <param name="pixelsWithMips">4 channel stream containing mips (if requested)</param>
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Mips">Number of mips in pixelWidthMips (1 if no mips).</param>
        /// <returns>True if saved successfully.</returns>
        internal static bool Save(MemoryTributary pixelsWithMips, Stream Destination, int Width, int Height, int Mips)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(Mips, Height, Width, ImageEngineFormat.DDS_DXT1);
            return DDSGeneral.WriteBlockCompressedDDS(pixelsWithMips, Destination, Width, Height, Mips, header, CompressBC2Block);
        }
    }
}
