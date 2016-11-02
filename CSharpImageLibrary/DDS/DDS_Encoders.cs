using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary.DDS
{
    internal static class DDS_Encoders
    {
        /// <summary>
        /// Compress texel to 8 byte BC1 compressed block.
        /// </summary>
        /// <param name="texel">4x4 BGRA group of pixels.</param>
        /// <returns>8 byte BC1 compressed block.</returns>
        private static byte[] CompressBC1Block(byte[] texel)
        {
            return CompressRGBTexel(texel, true, DXT1AlphaThreshold);
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
        /// Compress texel to 8 byte BC4 compressed block.
        /// </summary>
        /// <param name="texel">4x4 BGRA set of pixels.</param>
        /// <returns>8 byte BC4 compressed block.</returns>
        private static byte[] CompressBC4Block(byte[] texel)
        {
            return DDSGeneral.Compress8BitBlock(texel, 2, false);
        }

        /// <summary>
        /// Compress texel to 16 byte BC3 compressed block.
        /// </summary>
        /// <param name="texel">4x4 BGRA set of pixels.</param>
        /// <returns>16 byte BC3 compressed block.</returns>
        private static byte[] CompressBC3Block(byte[] texel)
        {
            // Compress Alpha
            byte[] Alpha = DDSGeneral.Compress8BitBlock(texel, 3, false);

            // Compress Colour
            byte[] RGB = DDSGeneral.CompressRGBTexel(texel, false, 0f);

            return Alpha.Concat(RGB).ToArray(Alpha.Length + RGB.Length);
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
                for (int j = 0; j < 8; j += 4)
                    twoAlpha |= (byte)(texel[i + j] << j);
                Alpha[i / 8] = twoAlpha;
            }

            // Compress Colour
            byte[] RGB = DDSGeneral.CompressRGBTexel(texel, false, 0f);

            return Alpha.Concat(RGB).ToArray(Alpha.Length + RGB.Length);
        }

        private static void WriteG8_L8Pixel(Stream writer, Stream pixels, int unused1, int unused2)
        {
            // BGRA
            byte[] colours = new byte[3];
            pixels.Read(colours, 0, 3);
            pixels.Position++;  // Skip alpha

            // KFreon: Weight colours to look proper. Dunno if this affects things but anyway...Got weightings from ATi Compressonator
            int b1 = (int)(colours[0] * 3 * 0.082);
            int g1 = (int)(colours[1] * 3 * 0.6094);
            int r1 = (int)(colours[2] * 3 * 0.3086);

            int test = (int)((b1 + g1 + r1) / 3f);
            writer.WriteByte((byte)test);
        }

        private static void WriteV8U8Pixel(Stream writer, Stream pixels, int unused1, int unused2)
        {
            // BGRA
            pixels.Position++; // No blue
            /*var bytes = pixels.ReadBytesFromStream(2);
            writer.Write(bytes, 0, 2);*/

            byte green = (byte)(pixels.ReadByte() + V8U8Adjust);
            byte red = (byte)(pixels.ReadByte() + V8U8Adjust);
            writer.Write(new byte[] { red, green }, 0, 2);
            pixels.Position++;    // No alpha
        }

        private static void WriteA8L8Pixel(Stream writer, Stream pixels, int unused1, int unused2)
        {
            // BGRA
            // First 3 channels are the same value, so just use the last one.
            pixels.Position += 2;
            writer.ReadFrom(pixels, 2);
        }

        private static void WriteRGBPixel(Stream writer, Stream pixels, int unused1, int unused2)
        {
            // BGRA
            var bytes = pixels.ReadBytes(3);
            writer.Write(bytes, 0, bytes.Length);
            pixels.Position++;
        }
    }
}
