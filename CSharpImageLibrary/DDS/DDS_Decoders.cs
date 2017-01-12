using CSharpImageLibrary.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary.DDS
{
    internal static class DDS_Decoders
    {
        // Since a pixel channel colour is always a byte, this is constant. I realise this isn't good when colours are bigger than a byte or floats, but I'll get there.
        const int SignedAdjustment = 128;

        // TODO: Virtual/physical size. Less than 4x4 texels

        #region Compressed Readers
        internal static void DecompressBC1Block(byte[] source, int sourceStart, float[] destination, int decompressedStart, int decompressedLineLength, bool unused)
        {
            DDS_BlockHelpers.DecompressRGBBlock(source, sourceStart, destination, decompressedStart, decompressedLineLength, true, false);
        }


        internal static void DecompressBC2Block(byte[] source, int sourceStart, float[] destination, int decompressedStart, int decompressedLineLength, bool isPremultiplied)
        {
            // KFreon: Decompress alpha (only half of the texel count though, since each byte is 2 texels of alpha)
            for (int i = 0; i < 8; i++)
            {
                // Start + alphaOffset + lineOffset.
                // DecompressedStart = Top Left corner of texel in full image in bytes.
                // alphaOffset = effectively column offset in a row of bitmap. Since a compressed byte has 2 pixels worth of alpha, i % 2 * 8 skips 2 pixels of BGRA each byte read, +3 selects alpha channel.
                // lineOffset = texels aren't contiguous i.e. each row in texel isn't next to each other when decompressed. Need to skip to next line in entire bitmap. i / 2 is truncated by int cast, 
                // so every 2 cycles (4 pixels, a full texel row) a bitmap line is skipped to the next line in texel.
                int offset = decompressedStart + ((i % 2) * 8 + 3 * 4) + (decompressedLineLength * (i / 2));
                destination[offset] = (source[sourceStart + i] & 0xF0) / 255f;
                destination[offset + 4] = (source[sourceStart + i] & 0x0F << 4) / 255f;
            }

            // +8 skips the above alpha, otherwise it's just a BC1 RGB block
            DDS_BlockHelpers.DecompressRGBBlock(source, sourceStart + 8, destination, decompressedStart, decompressedLineLength, false, isPremultiplied);
        }


        internal static void DecompressBC3Block(byte[] source, int sourceStart, float[] destination, int decompressedStart, int decompressedLineLength, bool isPremultiplied)
        {
            // Alpha, +3 to select that channel.
            DDS_BlockHelpers.Decompress8BitBlock(source, sourceStart, destination, decompressedStart + 3 * 4, decompressedLineLength, false);

            // RGB
            DDS_BlockHelpers.DecompressRGBBlock(source, sourceStart + 8, destination, decompressedStart, decompressedLineLength, false, isPremultiplied);
        }

        // BC4
        internal static void DecompressATI1(byte[] source, int sourceStart, float[] destination, int decompressedStart, int decompressedLineLength, bool unused)
        {
            DDS_BlockHelpers.Decompress8BitBlock(source, sourceStart, destination, decompressedStart, decompressedLineLength, false);

            // KFreon: All channels are the same to make grayscale, and alpha needs to be 255.
            for (int i = 0; i < 16; i++)
            {
                int offset = GetDecompressedOffset(decompressedStart, decompressedLineLength, i);

                // Since one channel (blue) was set by the decompression above, just need to set the remaining channels
                destination[offset + 1] = destination[offset] / 255f;
                destination[offset + 2] = destination[offset] / 255f;
                destination[offset + 3] = 1f;  // Alpha
            }
        }

        // BC5
        internal static void DecompressATI2Block(byte[] source, int sourceStart, float[] destination, int decompressedStart, int decompressedLineLength, bool unused)
        {
            // Green = +1 -- BGRA
            DDS_BlockHelpers.Decompress8BitBlock(source, sourceStart, destination, decompressedStart + 1, decompressedLineLength, false);


            // Red = +2, source + 8 to skip first compressed block. 
            DDS_BlockHelpers.Decompress8BitBlock(source, sourceStart + 8, destination, decompressedStart + 2, decompressedLineLength, false);

            

            // KFreon: Alpha is 255, and blue needs to be calculated
            for (int i = 0; i < 16; i++)
            {
                int offset = GetDecompressedOffset(decompressedStart, decompressedLineLength, i);

                // Get Red and Green on the range -1 - 1
                // *2-1 moves the range from 0 - 1, to -1 - 1
                float green = destination[offset + 1] * 2f - 1f;
                float red = destination[offset + 2] * 2f - 1f;    

                // Z solution for: x2 + y2 + z2 = 1, unit normal vectors. Only consider +ve root as ATI2 is a tangent space mapping and Z must be +ve.
                // Also when 1 - x2 - y2 < 0, Z = NaN, but is compensated for in ExpandTo255.
                double Z = Math.Sqrt(1d - (red * red + green * green));

                // Clamp value to range
                if (Z > 1)
                    Z = 1;

                destination[offset] = (float)Z;  // Blue
                destination[offset + 3] = 1f;  // Alpha
            }
        }

        internal static void DecompressBC6(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength, bool unused)
        {
            
        }

        internal static void DecompressBC7(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength, bool unused)
        {

        }


        static byte ExpandTo255(double v)
        {
            if (double.IsNaN(v) || v == 0)
                return 128;
            else
                return (byte)(((v + 1d) / 2d) * 255d);
        }

        internal static int GetDecompressedOffset(int start, int lineLength, int pixelIndex)
        {
            return start + (lineLength * (pixelIndex / 4)) + (pixelIndex % 4) * 4;
        }
        #endregion Compressed Readers

        #region Uncompressed Readers
        internal static void ReadUncompressed(byte[] source, int sourceStart, float[] destination, int pixelCount, DDS_Header.DDS_PIXELFORMAT ddspf)
        {
            int signedAdjustment = ((ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_SIGNED) == DDS_Header.DDS_PFdwFlags.DDPF_SIGNED) ? SignedAdjustment : 0;
            int sourceIncrement = ddspf.dwRGBBitCount / 8;  // /8 for bits to bytes conversion
            bool oneChannel = (ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_LUMINANCE) == DDS_Header.DDS_PFdwFlags.DDPF_LUMINANCE;
            bool twoChannel = (ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_ALPHAPIXELS) == DDS_Header.DDS_PFdwFlags.DDPF_ALPHAPIXELS && oneChannel;

            uint AMask = ddspf.dwABitMask;
            uint RMask = ddspf.dwRBitMask;
            uint GMask = ddspf.dwGBitMask;
            uint BMask = ddspf.dwBBitMask;


            ///// Figure out channel existance and ordering.
            // Setup array that indicates channel offset from pixel start.
            // e.g. Alpha is usually first, and is given offset 0.
            // NOTE: Ordering array is in ARGB order, and the stored indices change depending on detected channel order.
            // A negative index indicates channel doesn't exist in data and sets channel to 0xFF.
            List<uint> maskOrder = new List<uint>(4) { AMask, RMask, GMask, BMask };
            maskOrder.Sort();
            maskOrder.RemoveAll(t => t == 0);  // Required, otherwise indicies get all messed up when there's only two channels, but it's not indicated as such.

            int AIndex = 0;
            int RIndex = 0;
            int GIndex = 0;
            int BIndex = 0;

            if (twoChannel)  // Note: V8U8 does not come under this one.
            {
                // Intensity is first byte, then the alpha. Set all RGB to intensity for grayscale.
                // Second mask is always RMask as determined by the DDS Spec.
                AIndex = AMask > RMask ? 1 : 0;
                RIndex = AMask > RMask ? 0 : 1;
                GIndex = AMask > RMask ? 0 : 1;
                BIndex = AMask > RMask ? 0 : 1;
            }
            else if (oneChannel)
            {
                // Decide whether it's alpha or not.
                AIndex = AMask == 0 ? -1 : 0; 
                RIndex = AMask == 0 ? 0 : -1; 
                GIndex = AMask == 0 ? 0 : -1;
                BIndex = AMask == 0 ? 0 : -1; 
            }
            else
            {
                // Set default ordering
                AIndex = AMask == 0 ? -1 : maskOrder.IndexOf(AMask) * ddspf.ComponentSize;
                RIndex = RMask == 0 ? -1 : maskOrder.IndexOf(RMask) * ddspf.ComponentSize;
                GIndex = GMask == 0 ? -1 : maskOrder.IndexOf(GMask) * ddspf.ComponentSize;
                BIndex = BMask == 0 ? -1 : maskOrder.IndexOf(BMask) * ddspf.ComponentSize;
            }

            // Get suitable reader
            Action<byte[], int, float[], int, int> reader = ReadByte;
            if (ddspf.ComponentSize == 2)
                reader = ReadUShort;
            else if (ddspf.ComponentSize == 4)
                reader = ReadFloat;


            // Determine order of things
            int destAInd = 3;
            int destRInd = 0;
            int destGInd = 1;
            int destBInd = 2;          

            for (int i = 0, j = sourceStart; i < pixelCount * 4; i += 4, j += sourceIncrement)
            {
                reader(source, j + BIndex, destination, i + destBInd, BIndex);
                reader(source, j + GIndex, destination, i + destGInd, GIndex);
                reader(source, j + RIndex, destination, i + destRInd, RIndex);
                reader(source, j + AIndex, destination, i + destAInd, AIndex);
            }
        }

        static void ReadByte(byte[] source, int sourceInd, float[] destination, int destInd, int CInd)
        {
            destination[destInd] = CInd == -1 ? 1f : (source[sourceInd] / 255f);
        }

        static void ReadUShort(byte[] source, int sourceInd, float[] destination, int destInd, int CInd)
        {
            destination[destInd] = CInd == -1 ? 1f : (BitConverter.ToUInt16(source, sourceInd) / ushort.MaxValue);
        }

        static void ReadFloat(byte[] source, int sourceInd, float[] destination, int destInd, int CInd)
        {
            destination[destInd] = CInd == -1 ? 1f : BitConverter.ToSingle(source, sourceInd);
        }


        #endregion Uncompressed Readers
    }
}
