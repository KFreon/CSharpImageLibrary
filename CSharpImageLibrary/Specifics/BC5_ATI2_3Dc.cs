using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CSharpImageLibrary.General;
using UsefulThings;

namespace CSharpImageLibrary.Specifics
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
        /// <returns>BGRA pixels.</returns>
        internal static List<MipMap> Load(string imagePath)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs);
        }


        /// <summary>
        /// Loads ATI2 (BC5) image from stream.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <returns>BGRA pixels.</returns>
        internal static List<MipMap> Load(Stream stream)
        {
            return DDSGeneral.LoadBlockCompressedTexture(stream, DecompressATI2Block);
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

            // KFreon: Alpha needs to be 255
            byte[] alpha = new byte[16];
            for (int i = 0; i < 16; i++)
                alpha[i] = 0xFF;
            DecompressedBlock.Add(alpha);

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
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="MipMaps">List of Mipmaps to save. Pixels only.</param>
        /// <returns>True if saved successfully.</returns>
        internal static bool Save(List<MipMap> MipMaps, Stream Destination)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(MipMaps.Count, MipMaps[0].Height, MipMaps[0].Width, ImageEngineFormat.DDS_ATI2_3Dc);
            return DDSGeneral.WriteBlockCompressedDDS(MipMaps, Destination, header, CompressBC5Block);
        }
    }
}
