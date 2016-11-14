using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CSharpImageLibrary.DDS.DDSGeneral;
using static CSharpImageLibrary.DDS.DDS_BlockHelpers;

namespace CSharpImageLibrary.DDS
{
    internal static class DDS_Encoders
    {
        static byte SignedAdjustment = 128;  // KFreon: This is for adjusting out of signed land.  This gets removed on load and re-added on save.

        #region Compressed
        internal static void CompressBC1Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            CompressRGBTexel(imgData, sourcePosition, sourceLineLength, destination, destPosition, true, DXT1AlphaThreshold);
        }


        // TODO: Check that this does premultiplied alpha dnd stuff.
        internal static void CompressBC2Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            // Compress Alpha
            int position = sourcePosition + 3;  // Only want to read alphas
            for (int i = 0; i < 16; i++) 
            {
                for (int j = 0; j < 2; j++)
                {
                    destination[destPosition + i + j] = (byte)(imgData[position] << 4 | imgData[position + 4]);
                    position += 8;
                }

                sourcePosition += sourceLineLength;
            }

            // Compress Colour
            CompressRGBTexel(imgData, sourcePosition, sourceLineLength, destination, destPosition + 8, false, 0f);
        }

        // TODO: Check if this does premultiplied
        internal static void CompressBC3Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            // Compress Alpha
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition, 3, false);

            // Compress Colour
            CompressRGBTexel(imgData, sourcePosition, sourceLineLength, destination, destPosition + 8, false, 0f);
        }

        
        internal static void CompressBC4Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition, 2, false);
        }

        internal static void CompressBC5Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            // Red: Channel 2, 0 destination offset
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition, 2, false);

            // Green: Channel 1, 8 destination offset to be after Red.
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition + 8, 1, false);
        }
        #endregion Compressed

        internal static void WriteUncompressed(byte[] source, byte[] destination, int destStart)
        {
            int byteCount = bitCount / 8;
            bool twoChannel = false;
            bool oneChannel = false;
            byte signedAdjust = 0;

            for (int i = 0; i < source.Length; i+=4, destStart += byteCount)
            {
                byte blue = (byte)(source[i] + signedAdjust);
                byte green = (byte)(source[i + 1] + signedAdjust);
                byte red = (byte)(source[i + 2] + signedAdjust);
                byte alpha = (byte)(source[i + 3]);

                if (twoChannel)
                {
                    destination[destStart] = blue;
                    destination[destStart + 1] = green;
                }
                else if (oneChannel)
                {

                }
                else
                {
                    // Originally should be ARGB

                }
            }
        }

        /*internal static void WriteG8_L8Pixel(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            // KFreon: Weight colours to look proper. Dunno if this affects things but anyway...Got weightings from ATi Compressonator
            int b1 = (int)(imgData[sourcePosition] * 3 * 0.082);
            int g1 = (int)(imgData[sourcePosition + 1] * 3 * 0.6094);
            int r1 = (int)(imgData[sourcePosition + 2] * 3 * 0.3086);

            int test = (int)((b1 + g1 + r1) / 3f);
            destination[destPosition] = (byte)test;
        }

        internal static void WriteV8U8Pixel(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            // No blue
            destination[destPosition] = (byte)(imgData[sourcePosition + 1] + V8U8Adjust);  // Green
            destination[destPosition + 1] = (byte)(imgData[sourcePosition + 2] + V8U8Adjust);  // Red
        }

        internal static void WriteA8L8Pixel(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            // First 3 channels are the same value, so just use the last one.
            destination[destPosition] = imgData[sourcePosition + 2];
            destination[destPosition + 1] = imgData[sourcePosition + 3];
        }

        internal static void WriteRGBPixel(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            destination[destPosition] = imgData[sourcePosition];
            destination[destPosition + 1] = imgData[sourcePosition + 1];
            destination[destPosition + 2] = imgData[sourcePosition + 2];
        }

        internal static void WriteARGBPixel(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition)
        {
            destination[destPosition] = imgData[sourcePosition];
            destination[destPosition + 1] = imgData[sourcePosition + 1];
            destination[destPosition + 2] = imgData[sourcePosition + 2];
            destination[destPosition + 3] = imgData[sourcePosition + 3];
        }*/
    }
}
