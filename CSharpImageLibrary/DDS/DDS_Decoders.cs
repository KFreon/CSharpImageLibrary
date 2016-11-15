using CSharpImageLibrary.Headers;
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
        // Since a pixel channel colour is always a byte, this is constant. I realise this isn't good when colours are bigger than a byte or floats, but I'll get there.
        const int SignedAdjustment = 128;

        #region Compressed Readers
        internal static void DecompressBC1Block(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            DDS_BlockHelpers.DecompressRGBBlock(source, sourceStart, destination, decompressedStart, decompressedLineLength, true);
        }


        // TODO: Check that this does premultiplied alpha and stuff
        internal static void DecompressBC2Block(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            // KFreon: Decompress alpha (only half of the texel count though, since each byte is 2 texels of alpha)
            for (int i = 0; i < 8; i++)
            {
                // Start + alphaOffset + lineOffset.
                // DecompressedStart = Top Left corner of texel in full image in bytes.
                // alphaOffset = effectively column offset in a row of bitmap. Since a compressed byte has 2 pixels worth of alpha, i % 2 * 8 skips 2 pixels of BGRA each byte read, +3 selects alpha channel.
                // lineOffset = texels aren't contiguous i.e. each row in texel isn't next to each other when decompressed. Need to skip to next line in entire bitmap. i / 2 is truncated by int cast, 
                // so every 2 cycles (4 pixels, a full texel row) a bitmap line is skipped to the next line in texel.
                int offset = decompressedStart + ((i % 2) * 8 + 3) + (decompressedLineLength * (i / 2));
                destination[offset] = (byte)(source[sourceStart + i] * 0xF0 >> 4);
                destination[offset + 4] = (byte)(source[sourceStart + i] * 0x0F);
            }

            // +8 skips the above alpha, otherwise it's just a BC1 RGB block
            DDS_BlockHelpers.DecompressRGBBlock(source, sourceStart + 8, destination, decompressedStart, decompressedLineLength, false);
        }


        // TODO: Check that this does premultiplied
        internal static void DecompressBC3Block(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            // Alpha, +3 to select that channel.
            DDS_BlockHelpers.Decompress8BitBlock(source, sourceStart, destination, decompressedStart + 3, decompressedLineLength, false);

            // RGB
            DDS_BlockHelpers.DecompressRGBBlock(source, sourceStart + 8, destination, decompressedStart, decompressedLineLength, false);
        }


        internal static void DecompressATI2Block(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            // Red = +2 -- BGRA
            DDS_BlockHelpers.Decompress8BitBlock(source, sourceStart, destination, decompressedStart + 2, decompressedLineLength, false);

            // Green = +1, source + 8 to skip first compressed block.
            DDS_BlockHelpers.Decompress8BitBlock(source, sourceStart + 8, destination, decompressedStart + 1, decompressedLineLength, false);

            // KFreon: Alpha and blue need to be 255
            for (int i = 0; i < 16; i++)
            {
                int offset = GetDecompressedOffset(decompressedStart, decompressedLineLength, i);
                destination[offset] = 0xFF;  // Blue
                destination[offset + 3] = 0xFF;  // Alpha
            }
        }


        internal static void DecompressATI1(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength)
        {
            DDS_BlockHelpers.Decompress8BitBlock(source, sourceStart, destination, decompressedStart, decompressedLineLength, false);

            // KFreon: All channels are the same to make grayscale, and alpha needs to be 255.
            for (int i = 0; i < 16; i++)
            {
                int offset = GetDecompressedOffset(decompressedStart, decompressedLineLength, i);

                // Since one channel (blue) was set by the decompression above, just need to set the remaining channels
                destination[offset + 1] = destination[offset];
                destination[offset + 2] = destination[offset];
                destination[offset + 3] = 0xFF;  // Alpha
            }
        }

        internal static int GetDecompressedOffset(int start, int lineLength, int pixelIndex)
        {
            return start + (lineLength * (pixelIndex / 4)) + (pixelIndex % 4) * 4;
        }
        #endregion Compressed Readers

        #region Uncompressed Readers
        internal static void ReadUncompressed(byte[] source, int sourceStart, byte[] destination, int pixelCount, DDS_Header.DDS_PIXELFORMAT ddspf)
        {
            int signedAdjustment = ((ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_SIGNED) == DDS_Header.DDS_PFdwFlags.DDPF_SIGNED) ? SignedAdjustment : 0;
            int sourceIncrement = ddspf.dwRGBBitCount / 8;  // /8 for bits to bytes conversion
            bool oneChannel = (ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_LUMINANCE) == DDS_Header.DDS_PFdwFlags.DDPF_LUMINANCE;
            bool twoChannel = (ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_ALPHAPIXELS) == DDS_Header.DDS_PFdwFlags.DDPF_ALPHAPIXELS && oneChannel;

            for (int i = 0, j = 0; i < pixelCount * 4; i += 4, j += sourceIncrement)
            {
                int colour = ReadUncompressedColour(source, sourceStart + j, ddspf.dwRGBBitCount);

                byte red = 0;
                byte blue = 0;
                byte green = 0;
                byte alpha = 0xFF;

                if (twoChannel)
                {
                    Debugger.Break();
                    blue = (byte)(MaskAndShift(colour, ddspf.dwBBitMask) - signedAdjustment);
                    green = (byte)(MaskAndShift(colour, ddspf.dwABitMask) - signedAdjustment);
                    red = 0xFF;
                }
                else if (oneChannel)
                {
                    red = (byte)(MaskAndShift(colour, ddspf.dwRBitMask) - signedAdjustment);
                    blue = red;
                    green = red;
                }
                else
                {
                    blue = ddspf.dwBBitMask == 0 ? (byte)0xFF : (byte)(MaskAndShift(colour, ddspf.dwBBitMask) - signedAdjustment);
                    green = ddspf.dwGBitMask == 0 ? (byte)0xFF : (byte)(MaskAndShift(colour, ddspf.dwGBitMask) - signedAdjustment);
                    red = ddspf.dwRBitMask == 0 ? (byte)0xFF : (byte)(MaskAndShift(colour, ddspf.dwRBitMask) - signedAdjustment);
                    alpha = ddspf.dwABitMask == 0 ? (byte)0xFF : (byte)(MaskAndShift(colour, ddspf.dwABitMask));
                }


                destination[i] = blue;
                destination[i + 1] = green;
                destination[i + 2] = red;
                destination[i + 3] = alpha;
            }
        }

        static int ReadUncompressedColour(byte[] source, int start, int bitCount)
        {
            switch (bitCount)
            {
                case 8:
                    return source[start];
                case 16:
                    return BitConverter.ToInt16(source, start);
                case 24:
                case 32:
                    return BitConverter.ToInt32(source, start);
                default:
                    throw new InvalidOperationException($"Bitcount per channel is not allowed: {bitCount}");
            }
        }

        static byte MaskAndShift(int colour, uint mask)
        {
            if (mask == 0)
                return 0;

            var masked = colour & mask;
            
            // Shift - skip a byte (max mask 'size')
            while ((~mask & 0xFF) != 0)
            {
                masked >>= 8;
                mask >>= 8;
            }

            return (byte)masked;
        }
        #endregion Uncompressed Readers
    }
}
