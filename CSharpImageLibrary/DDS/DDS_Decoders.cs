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
        static byte V8U8Adjust = 128;  // KFreon: This is for adjusting out of signed land.  This gets removed on load and re-added on save.

        #region Compressed Readers
        internal static void DecompressBC1Block(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            DDS_BlockHelpers.DecompressRGBBlock(source, sourceStart, destination, decompressedStart, decompressedLineLength, true);
        }


        // TODO: Check that this does premultiplied alpha and stuff
        internal static void DecompressBC2Block(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            // KFreon: Decompress alpha
            for (int i = 0; i < 8; i++)
            {
                // Line offset since texels are a 4x4 block, can't just write data contiguously
                int lineOffset = i % 2 == 0 ? decompressedLineLength * (i / 2) : 0;
                int alphaOffset = i * 8 + 3 + lineOffset; // Since each byte read contains two texels worth of alpha, need to skip two texels each time. 
                destination[decompressedStart + alphaOffset] = (byte)(source[sourceStart + i] * 0xF0 >> 4);
                destination[decompressedStart + alphaOffset + 4] = (byte)(source[sourceStart + i] * 0x0F);
            }

            // +8 skips the above alpha, otherwise it's just a BC1 RGB block
            DDS_BlockHelpers.DecompressRGBBlock(source, sourceStart + 8, destination, decompressedStart, decompressedLineLength, false);
        }


        // TODO: Check that this does premultiplied
        internal static void DecompressBC3Block(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            // Alpha, +3 to select that channel.
            DDS_BlockHelpers.Decompress8BitBlockByChannel(source, sourceStart, destination, decompressedStart + 3, decompressedLineLength, false);

            // RGB
            DDS_BlockHelpers.DecompressRGBBlock(source, sourceStart + 8, destination, decompressedStart, decompressedLineLength, false);
        }


        internal static void DecompressATI2Block(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            // Red = +0
            DDS_BlockHelpers.Decompress8BitBlockByChannel(source, sourceStart, destination, decompressedStart, decompressedLineLength, false);

            // Green = +1
            DDS_BlockHelpers.Decompress8BitBlockByChannel(source, sourceStart, destination, decompressedStart + 1, decompressedLineLength, false);

            // KFreon: Alpha and blue need to be 255
            for (int i = 0; i < 16; i++)
            {
                int lineOffset = i % 4 == 0 ? decompressedLineLength * (i / 4) : 0;
                int fullOffset = decompressedStart + lineOffset + i * 4;
                destination[fullOffset + 2] = 0xFF;  // Blue
                destination[fullOffset + 3] = 0xFF;  // Alpha
            }
        }


        internal static void DecompressATI1(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            byte[] channel = DDS_BlockHelpers.Decompress8BitBlock(source, sourceStart, false);

            // KFreon: All channels are the same to make grayscale, and alpha needs to be 255.
            for (int i = 0; i < 16; i++)
            {
                int lineOffset = i % 4 == 0 ? decompressedLineLength * (i / 4) : 0;
                int fullOffset = decompressedStart + lineOffset + i * 4;
                destination[fullOffset] = channel[i]; // Red
                destination[fullOffset + 1] = channel[i]; // Green
                destination[fullOffset + 2] = channel[i];  // Blue
                destination[fullOffset + 3] = 0xFF;  // Alpha
            }
        }
#endregion Compressed Readers

        // TODO: Check RGBA ordering

        #region Uncompressed Readers
        internal static void ReadG8_L8Pixel(byte[] source, int sourceStart, byte[] destination, int pixelCount)
        {
            // KFreon: Same colour for other channels to make grayscale.
            for (int i = 0; i < pixelCount; i += 4)
            {
                byte colour = source[sourceStart + i];
                destination[i] = colour;
                destination[i + 1] = colour;
                destination[i + 2] = colour;
                destination[i + 3] = 0xFF;
            }
        }

        internal static void ReadV8U8Pixel(byte[] source, int sourceStart, byte[] destination, int pixelCount)
        {
            for(int i = 0; i < pixelCount; i += 4)
            {
                destination[i] = source[sourceStart + i - V8U8Adjust];
                destination[i + 1] = source[sourceStart + i + 1 - V8U8Adjust];
                destination[i + 2] = 0xFF;
                destination[i + 3] = 0xFF;
            }
        }

        internal static void ReadRGBPixel(byte[] source, int sourceStart, byte[] destination, int pixelCount)
        {
            for(int i = 0; i < pixelCount; i += 4)
            {
                destination[i] = source[sourceStart + i];
                destination[i + 1] = source[sourceStart + i + 1];
                destination[i + 2] = source[sourceStart + i + 2]; 
                destination[i + 3] = 0xFF;
            }
        }
        internal static void ReadARGBPixel(byte[] source, int sourceStart, byte[] destination, int pixelCount)
        {
            for (int i = 0; i < pixelCount; i += 4)
            {
                destination[i] = source[sourceStart + i];
                destination[i + 1] = source[sourceStart + i + 1];
                destination[i + 2] = source[sourceStart + i + 2];
                destination[i + 3] = source[sourceStart + i + 3];
            }
        }

        internal static void ReadA8L8Pixel(byte[] source, int sourceStart, byte[] destination, int pixelCount)
        {
            for (int i = 0; i < pixelCount; i += 4)
            {
                byte colour = source[sourceStart + i];
                destination[i] = colour;
                destination[i + 1] = colour;
                destination[i + 2] = colour;
                destination[i + 3] = source[sourceStart + i + 1];
            }
        }
        #endregion Uncompressed Readers
    }
}
