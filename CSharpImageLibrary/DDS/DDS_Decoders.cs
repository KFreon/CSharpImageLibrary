using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary.DDS
{
    internal static class DDS_Decoders
    {
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
        /// Reads a 16 byte BC2 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC2 compressed stream.</param>
        /// <returns>BGRA channels.</returns>
        private static List<byte[]> DecompressBC2Block(Stream compressed)
        {
            // KFreon: Read alpha into byte[] for maximum speed? Might be cos it's a MemoryStream...
            byte[] CompressedAlphas = new byte[8];
            compressed.Read(CompressedAlphas, 0, 8);
            int count = 0;

            // KFreon: Read alpha
            byte[] alpha = new byte[16];
            for (int i = 0; i < 16; i += 2)
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
        /// Reads a 16 byte BC3 compressed block from stream.
        /// </summary>
        /// <param name="compressed">BC3 compressed image stream.</param>
        /// <returns>List of BGRA channels.</returns>
        private static List<byte[]> DecompressBC3Block(Stream compressed)
        {
            byte[] alpha = DDSGeneral.Decompress8BitBlock(compressed, false);
            List<byte[]> DecompressedBlock = DDSGeneral.DecompressRGBBlock(compressed, false);
            DecompressedBlock[3] = alpha;
            return DecompressedBlock;
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



            // KFreon: Alpha needs to be 255
            byte[] alpha = new byte[16];
            byte[] blue = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                alpha[i] = 0xFF;
                /*double r = red[i] / 255.0;
                double g = green[i] / 255.0;
                double test = 1 - (r * g);
                double anbs = Math.Sqrt(test);
                double ans = anbs * 255.0;*/
                blue[i] = (byte)0xFF;
            }

            DecompressedBlock.Add(blue);
            DecompressedBlock.Add(green);
            DecompressedBlock.Add(red);
            DecompressedBlock.Add(alpha);

            return DecompressedBlock;
        }

        /// <summary>
        /// Decompresses an ATI1 (BC4) block.
        /// </summary>
        /// <param name="compressed">Compressed data stream.</param>
        /// <returns>BGRA channels (16 bits each)</returns>
        private static List<byte[]> DecompressATI1(Stream compressed)
        {
            byte[] channel = DDSGeneral.Decompress8BitBlock(compressed, false);
            List<byte[]> DecompressedBlock = new List<byte[]>();

            // KFreon: All channels are the same to make grayscale.
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);

            // KFreon: Alpha needs to be 255
            byte[] alpha = new byte[16];
            for (int i = 0; i < 16; i++)
                alpha[i] = 0xFF;
            DecompressedBlock.Add(alpha);
            return DecompressedBlock;
        }
    }
}
