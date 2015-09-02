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
        /// <returns>16 byte BGRA channels as stream.</returns>
        internal static List<MipMap> Load(string imageFile)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs);
        }


        /// <summary>
        /// Load important information from BC1 image stream.
        /// </summary>
        /// <param name="compressed">Stream containing entire image. NOT just pixels.</param>
        /// <returns>16 byte BGRA channels as stream.</returns>
        internal static List<MipMap> Load(Stream compressed)
        {
            return DDSGeneral.LoadBlockCompressedTexture(compressed, DecompressBC1Block);
        }


        /// <summary>
        /// Read an 8 byte BC1 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC1 compressed stream.</param>
        /// <returns>BGRA channels.</returns>
        private static List<byte[]> DecompressBC1Block(Stream compressed)
        {
            return DDSGeneral.DecompressRGBBlock(compressed, true);
        }


        /// <summary>
        /// Compress texel to 8 byte BC1 compressed block.
        /// </summary>
        /// <param name="texel">4x4 BGRA group of pixels.</param>
        /// <returns>8 byte BC1 compressed block.</returns>
        private static byte[] CompressBC1Block(byte[] texel)
        {
            return DDSGeneral.CompressRGBBlock(texel, true);
        }


        /// <summary>
        /// Saves a texture using BC1 compression.
        /// </summary>
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="MipMaps">List of MipMaps to save. Pixels only.</param>
        /// <returns>True if saved successfully.</returns>
        internal static bool Save(List<MipMap> MipMaps, Stream Destination)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(MipMaps.Count, MipMaps[0].Height, MipMaps[0].Width, ImageEngineFormat.DDS_DXT1);
            return DDSGeneral.WriteBlockCompressedDDS(MipMaps, Destination, header, CompressBC1Block);
        }
    }
}
