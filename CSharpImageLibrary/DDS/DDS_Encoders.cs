using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CSharpImageLibrary.DDS.DDSGeneral;
using static CSharpImageLibrary.DDS.DDS_BlockHelpers;
using CSharpImageLibrary.Headers;

namespace CSharpImageLibrary.DDS
{
    internal static class DDS_Encoders
    {
        #region Compressed
        internal static void CompressBC1Block(float[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
        {
            CompressRGBTexel(imgData, sourcePosition, sourceLineLength, destination, destPosition, true, (alphaSetting == AlphaSettings.RemoveAlphaChannel ? 0 : DXT1AlphaThreshold), alphaSetting);
        }


        internal static void CompressBC2Block(float[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
        {
            // Compress Alpha
            if (alphaSetting == AlphaSettings.RemoveAlphaChannel)
            {
                // No alpha so fill with opaque alpha - has to be an alpha value, so make it so RGB is 100% visible.
                for (int i = 0; i < 8; i++)
                    destination[destPosition + i] = 0xFF;
            }
            else
            {
                int position = sourcePosition + 3;  // Only want to read alphas
                for (int i = 0; i < 8; i += 2)
                {
                    destination[destPosition + i] = (byte)(((byte)(imgData[position] * 255f) & 0xF0) | ((byte)(imgData[position + 4] * 255f) >> 4));
                    destination[destPosition + i + 1] = (byte)(((byte)(imgData[position + 8] * 255f) & 0xF0) | ((byte)(imgData[position + 12] * 255f) >> 4));

                    position += sourceLineLength;
                }
            }
            

            // Compress Colour
            CompressRGBTexel(imgData, sourcePosition, sourceLineLength, destination, destPosition + 8, false, 0f, alphaSetting);
        }

        internal static void CompressBC3Block(float[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
        {
            // Compress Alpha
            if (alphaSetting == AlphaSettings.RemoveAlphaChannel)
            {
                // No alpha so fill with opaque alpha - has to be an alpha value, so make it so RGB is 100% visible.
                for (int i = 0; i < 8; i++)
                    destination[destPosition + i] = 0xFF;
            }
            else
                Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition, 3, false);

            // Compress Colour
            CompressRGBTexel(imgData, sourcePosition, sourceLineLength, destination, destPosition + 8, false, 0f, alphaSetting);
        }

        // ATI1
        internal static void CompressBC4Block(float[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
        {
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition, 0, false);
        }


        // ATI2 3Dc
        internal static void CompressBC5Block(float[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
        {
            // Green: Channel 1.
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition, 1, false);

            // Red: Channel 0, 8 destination offset to be after Green.
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition + 8, 0, false);
        }
        #endregion Compressed

        internal static int WriteUncompressed(float[] source, byte[] destination, int destStart, DDS_Header.DDS_PIXELFORMAT dest_ddspf)
        {
            int byteCount = dest_ddspf.dwRGBBitCount / 8;
            bool requiresSignedAdjust = (dest_ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_SIGNED) == DDS_Header.DDS_PFdwFlags.DDPF_SIGNED;
            bool oneChannel = (dest_ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_LUMINANCE) == DDS_Header.DDS_PFdwFlags.DDPF_LUMINANCE;
            bool twoChannel = oneChannel && (dest_ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_ALPHAPIXELS) == DDS_Header.DDS_PFdwFlags.DDPF_ALPHAPIXELS;

            uint AMask = dest_ddspf.dwABitMask;
            uint RMask = dest_ddspf.dwRBitMask;
            uint GMask = dest_ddspf.dwGBitMask;
            uint BMask = dest_ddspf.dwBBitMask;

            ///// Figure out channel existance and ordering.
            // Setup array that indicates channel offset from pixel start.
            // e.g. Alpha is usually first, and is given offset 0.
            // NOTE: Ordering array is in ARGB order, and the stored indices change depending on detected channel order.
            // A negative index indicates channel doesn't exist in data and sets channel to 0xFF.

            if (dest_ddspf.dwFourCC == DDS_Header.FourCC.A32B32G32R32F)
            {
                AMask = 4;
                BMask = 3;
                GMask = 2;
                RMask = 1;
            }

            List<uint> maskOrder = new List<uint>(4) { AMask, RMask, GMask, BMask };
            maskOrder.Sort();
            maskOrder.RemoveAll(t => t == 0);  // Required, otherwise indicies get all messed up when there's only two channels, but it's not indicated as such.

            int AIndex = 0;
            int RIndex = 0;
            int GIndex = 0;
            int BIndex = 0;

            // Determine channel ordering
            AIndex = AMask == 0 ? -1 : maskOrder.IndexOf(AMask) * dest_ddspf.ComponentSize;
            RIndex = RMask == 0 ? -1 : maskOrder.IndexOf(RMask) * dest_ddspf.ComponentSize;
            GIndex = GMask == 0 ? -1 : maskOrder.IndexOf(GMask) * dest_ddspf.ComponentSize;
            BIndex = BMask == 0 ? -1 : maskOrder.IndexOf(BMask) * dest_ddspf.ComponentSize;

            // Determine writer
            Action<float[], int, byte[], int> writer = ReadFloatWriteByte;
            if (dest_ddspf.ComponentSize == 2)
                writer = ReadFloatWriteUShort;
            else if (dest_ddspf.ComponentSize == 4)
                writer = ReadFloatWriteFloat;

            int sourceAInd = 3;
            int sourceRInd = 0;
            int sourceGInd = 1;
            int sourceBInd = 2;

            for (int i = 0; i < source.Length; i += 4, destStart += byteCount)
            {
                if (twoChannel) // No large components - silly spec...
                {
                    byte red = (byte)((source[i] * 255f) + (requiresSignedAdjust ? 128 : 0));
                    byte alpha = (byte)(source[i + 3] * 255f);

                    destination[destStart] = AMask > RMask ? red : alpha;
                    destination[destStart + 1] = AMask > RMask ? alpha : red;
                }
                else if (oneChannel) // No large components - silly spec...
                {
                    byte blue = (byte)((source[i] * 255f) + (requiresSignedAdjust ? 128 : 0));
                    byte green = (byte)((source[i + 1] * 255f) + (requiresSignedAdjust ? 128 : 0));
                    byte red = (byte)((source[i + 2] * 255f) + (requiresSignedAdjust ? 128 : 0));
                    byte alpha = (byte)(source[i + 3] * 255f);

                    destination[destStart] = (byte)(blue * 0.082 + green * 0.6094 + blue * 0.3086); // Weightings taken from ATI Compressonator. Dunno if this changes things much.
                }
                else
                {
                    if (AMask != 0)
                        writer(source, i + sourceAInd, destination, destStart + AIndex);

                    if (RMask != 0)
                        writer(source, i + sourceRInd, destination, destStart + RIndex);

                    if (GMask != 0)
                        writer(source, i + sourceGInd, destination, destStart + GIndex);

                    if (BMask != 0)
                        writer(source, i + sourceBInd, destination, destStart + BIndex);


                    // Signed adjustments - Only happens for bytes for now. V8U8
                    if (requiresSignedAdjust)
                    {
                        if (RMask != 0)
                            destination[destStart + RIndex] += 128;

                        if (GMask != 0)
                            destination[destStart + GIndex] += 128;

                        if (BMask != 0)
                            destination[destStart + BIndex] += 128;
                    }
                }
            }

            return destStart;
        }

        static void ReadFloatWriteByte(float[] source, int sourceInd, byte[] destination, int destInd)
        {
            destination[destInd] = (byte)(source[sourceInd] * 255f);
        }


        static void ReadFloatWriteUShort(float[] source, int sourceInd, byte[] destination, int destInd)
        {
            byte[] bytes = BitConverter.GetBytes((ushort)(source[sourceInd] * uint.MaxValue));

            destination[destInd] = bytes[0];
            destination[destInd + 1] = bytes[1];
        }


        static void ReadFloatWriteFloat(float[] source, int sourceInd, byte[] destination, int destInd)
        {
            byte[] bytes = BitConverter.GetBytes(source[sourceInd]);

            destination[destInd] = bytes[0];
            destination[destInd + 1] = bytes[1];
            destination[destInd + 2] = bytes[2];
            destination[destInd + 3] = bytes[3];
        }
    }
}
