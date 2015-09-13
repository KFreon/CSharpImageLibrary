using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpImageLibrary.General;
using UsefulThings;

namespace CSharpImageLibrary.Specifics
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
        /// <returns>16 byte BGRA channels as stream.</returns>
        internal static List<MipMap> Load(string imageFile)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs);
        }


        /// <summary>
        /// Load important information from BC2 image stream.
        /// </summary>
        /// <param name="compressed">Stream containing entire image. NOT just pixels.</param>
        /// <returns>16 byte BGRA channels as stream.</returns>
        internal static List<MipMap> Load(Stream compressed)
        {
            return DDSGeneral.LoadBlockCompressedTexture(compressed, DecompressBC2);
        }

        
        /// <summary>
        /// Reads a 16 byte BC2 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC2 compressed stream.</param>
        /// <returns>BGRA channels.</returns>
        private static List<byte[]> DecompressBC2(Stream compressed)
        {
            // KFreon: Read alpha into byte[] for maximum speed? Might be cos it's a MemoryStream...
            byte[] CompressedAlphas = new byte[8];
            compressed.Read(CompressedAlphas, 0, 8);
            int count = 0;

            // KFreon: Read alpha
            byte[] alpha = new byte[16];
            for (int i = 0; i < 16; i+=2)
            {
                //byte twoAlphas = (byte)compressed.ReadByte();
                byte twoAlphas = CompressedAlphas[count++];
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
        /// <param name="texel">4x4 BGRA set of pixels.</param>
        /// <returns>16 byte BC2 compressed block.</returns>
        private static byte[] CompressBC2Block(byte[] texel)
        {
            // Compress Alpha
            byte[] Alpha = new byte[8];
            for (int i = 3; i < 64; i += 8)  // Only read alphas
            {
                byte twoAlpha = 0;
                for (int j = 0; j < 8; j+=4)
                    twoAlpha |= (byte)(texel[i + j] << j);
                Alpha[i / 8] = twoAlpha;
            }

            // Compress Colour
            byte[] RGB = DDSGeneral.CompressRGBBlock(texel, false);

            return Alpha.Concat(RGB).ToArray(Alpha.Length + RGB.Length);
        }


        /// <summary>
        /// Saves texture using BC2 compression.
        /// </summary>
        /// <param name="MipMaps">List of MipMaps to save. Pixels only.</param>
        /// <param name="Destination">Stream to save to.</param>
        /// <returns>True if saved successfully.</returns>
        internal static bool Save(List<MipMap> MipMaps, Stream Destination)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(MipMaps.Count, MipMaps[0].Height, MipMaps[0].Width, ImageEngineFormat.DDS_DXT3);
            return DDSGeneral.WriteBlockCompressedDDS(MipMaps, Destination, header, CompressBC2Block);
        }
    }
}
