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
        static byte SignedAdjustment = 128;  // KFreon: This is for adjusting out of signed land.  This gets removed on load and re-added on save.

        #region Compressed
        internal static void CompressBC1Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
        {
            CompressRGBTexel(imgData, sourcePosition, sourceLineLength, destination, destPosition, true, (alphaSetting == AlphaSettings.RemoveAlphaChannel ? 0 : DXT1AlphaThreshold), alphaSetting);
        }


        internal static void CompressBC2Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
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
                    destination[destPosition + i] = (byte)((imgData[position] & 0xF0) | (imgData[position + 4] >> 4));
                    destination[destPosition + i + 1] = (byte)((imgData[position + 8] & 0xF0) | (imgData[position + 12] >> 4));

                    position += sourceLineLength;
                }
            }
            

            // Compress Colour
            CompressRGBTexel(imgData, sourcePosition, sourceLineLength, destination, destPosition + 8, false, 0f, alphaSetting);
        }

        internal static void CompressBC3Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
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
        internal static void CompressBC4Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
        {
            // TODO: Use colour weightings here
            // blue * 0.082 + green * 0.6094 + blue * 0.3086
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition, 2, false);
        }


        // ATI2 3Dc
        internal static void CompressBC5Block(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, AlphaSettings alphaSetting)
        {
            // TODO: Blue channel influence? Weightings?
            // Maybe lesser * 0.082 + greater * 0.6094 + blue * 0.3086
            // Blue has same influence on each channel, but the weighting of each channel is dependent on which channel is being compressed.

            // Green: Channel 1, 8 destination offset to be after Red.
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition, 1, false);

            // Red: Channel 2, 0 destination offset
            Compress8BitBlock(imgData, sourcePosition, sourceLineLength, destination, destPosition + 8, 2, false);
        }
        #endregion Compressed

        internal static int WriteUncompressed(byte[] source, byte[] destination, int destStart, DDS_Header.DDS_PIXELFORMAT ddspf)
        {
            int byteCount = ddspf.dwRGBBitCount / 8;
            byte signedAdjust = (ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_SIGNED) == DDS_Header.DDS_PFdwFlags.DDPF_SIGNED ? SignedAdjustment : (byte)0;
            bool oneChannel = (ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_LUMINANCE) == DDS_Header.DDS_PFdwFlags.DDPF_LUMINANCE;
            bool twoChannel = oneChannel && (ddspf.dwFlags & DDS_Header.DDS_PFdwFlags.DDPF_ALPHAPIXELS) == DDS_Header.DDS_PFdwFlags.DDPF_ALPHAPIXELS;

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

            // Set default ordering
            AIndex = AMask == 0 ? -1 : maskOrder.IndexOf(AMask);
            RIndex = RMask == 0 ? -1 : maskOrder.IndexOf(RMask);
            GIndex = GMask == 0 ? -1 : maskOrder.IndexOf(GMask);
            BIndex = BMask == 0 ? -1 : maskOrder.IndexOf(BMask);

            for (int i = 0; i < source.Length; i+=4, destStart += byteCount)
            {
                byte blue = (byte)(source[i] + signedAdjust);
                byte green = (byte)(source[i + 1] + signedAdjust);
                byte red = (byte)(source[i + 2] + signedAdjust);
                byte alpha = (byte)(source[i + 3]);

                if (twoChannel)
                {
                    destination[destStart] = AMask > RMask ? red : alpha;
                    destination[destStart + 1] = AMask > RMask ? alpha : red;
                }
                else if (oneChannel)
                    destination[destStart] = (byte)(blue * 0.082 + green * 0.6094 + blue * 0.3086); // Weightings taken from ATI Compressonator. Dunno if this changes things much.
                else
                {
                    if (AMask != 0)
                        destination[destStart + AIndex] = alpha;

                    if (RMask != 0)
                        destination[destStart + RIndex] = red;

                    if (GMask != 0)
                        destination[destStart + GIndex] = green;

                    if (BMask != 0)
                        destination[destStart + BIndex] = blue;
                }
            }

            return destStart + byteCount; // Final byteCount increment, since it's the start index, not the end index.
        }

        static int Shift(byte channel, uint mask)
        {
            int shifted = 0;
            if (mask != 0)
            {
                shifted = channel;

                // Shift colour to position of mask. This method moves the mask back towards 0 a number of times, and the channel colour the same number "up".
                while ((mask & 0xFF) == 0)
                {
                    mask >>= 8;
                    shifted <<= 8;
                }
            }

            return shifted;
        }
    }
}
