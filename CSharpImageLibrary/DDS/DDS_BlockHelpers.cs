using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary.DDS
{
    internal static class DDS_BlockHelpers
    {
        #region Block Compression
        #region RGB DXT
        /// <summary>
        /// This region contains stuff adpated/taken from the DirectXTex project: https://github.com/Microsoft/DirectXTex
        /// Things needed to be in the range 0-1 instead of 0-255, hence new struct etc
        /// </summary>
        struct RGBColour
        {
            public float r, g, b, a;

            public RGBColour(float red, float green, float blue, float alpha)
            {
                r = red;
                g = green;
                b = blue;
                a = alpha;
            }
        }

        static float[] pC3 = { 1f, 1f / 2f, 0f };
        static float[] pD3 = { 0f, 1f / 2f, 1f };

        static float[] pC4 = { 1f, 2f / 3f, 1f / 3f, 0f };
        static float[] pD4 = { 0f, 1f / 3f, 2f / 3f, 1f };

        static uint[] psteps3 = { 0, 2, 1 };
        static uint[] psteps4 = { 0, 2, 3, 1 };

        static RGBColour Luminance = new RGBColour(0.2125f / 0.7154f, 1f, 0.0721f / 0.7154f, 1f);
        static RGBColour LuminanceInv = new RGBColour(0.7154f / 0.2125f, 1f, 0.7154f / 0.0721f, 1f);

        static RGBColour Decode565(uint wColour)
        {
            RGBColour colour = new RGBColour();
            colour.r = ((wColour >> 11) & 31) * (1f / 31f);
            colour.g = ((wColour >> 5) & 63) * (1f / 63f);
            colour.b = ((wColour >> 0) & 31) * (1f / 31f);
            colour.a = 1f;

            return colour;
        }

        static uint Encode565(RGBColour colour)
        {
            RGBColour temp = new RGBColour();
            temp.r = (colour.r < 0f) ? 0f : (colour.r > 1f) ? 1f : colour.r;
            temp.g = (colour.g < 0f) ? 0f : (colour.g > 1f) ? 1f : colour.g;
            temp.b = (colour.b < 0f) ? 0f : (colour.b > 1f) ? 1f : colour.b;

            return (uint)(temp.r * 31f + 0.5f) << 11 | (uint)(temp.g * 63f + 0.5f) << 5 | (uint)(temp.b * 31f + 0.5f);
        }

        static RGBColour ReadColourFromTexel(byte[] texel, int i)
        {
            // Pull out rgb from texel
            byte r = texel[i + 2];
            byte g = texel[i + 1];
            byte b = texel[i];
            byte a = texel[i + 3];

            // Create current pixel colour
            RGBColour current = new RGBColour();
            current.r = r / 255f;
            current.g = g / 255f;
            current.b = b / 255f;
            current.a = a / 255f;

            return current;
        }

        private static RGBColour[] OptimiseRGB(RGBColour[] Colour, int uSteps)
        {
            float[] pC = uSteps == 3 ? pC3 : pC4;
            float[] pD = uSteps == 3 ? pD3 : pD4;

            // Find min max
            RGBColour X = Luminance;
            RGBColour Y = new RGBColour();

            for (int i = 0; i < Colour.Length; i++)
            {
                RGBColour current = Colour[i];

                // X = min, Y = max
                if (current.r < X.r)
                    X.r = current.r;

                if (current.g < X.g)
                    X.g = current.g;

                if (current.b < X.b)
                    X.b = current.b;


                if (current.r > Y.r)
                    Y.r = current.r;

                if (current.g > Y.g)
                    Y.g = current.g;

                if (current.b > Y.b)
                    Y.b = current.b;
            }

            // Diagonal axis - starts with difference between min and max
            RGBColour diag = new RGBColour();
            diag.r = Y.r - X.r;
            diag.g = Y.g - X.g;
            diag.b = Y.b - X.b;

            float fDiag = diag.r * diag.r + diag.g * diag.g + diag.b * diag.b;
            if (fDiag < 1.175494351e-38F)
            {
                RGBColour min1 = new RGBColour();
                min1.r = X.r;
                min1.g = X.g;
                min1.b = X.b;

                RGBColour max1 = new RGBColour();
                max1.r = Y.r;
                max1.g = Y.g;
                max1.b = Y.b;

                return new RGBColour[] { min1, max1 };
            }

            float FdiagInv = 1f / fDiag;

            RGBColour Dir = new RGBColour();
            Dir.r = diag.r * FdiagInv;
            Dir.g = diag.g * FdiagInv;
            Dir.b = diag.b * FdiagInv;

            RGBColour Mid = new RGBColour();
            Mid.r = (X.r + Y.r) * .5f;
            Mid.g = (X.g + Y.g) * .5f;
            Mid.b = (X.b + Y.b) * .5f;

            float[] fDir = new float[4];

            for (int i = 0; i < Colour.Length; i++)
            {
                RGBColour pt = new RGBColour();
                pt.r = Dir.r * (Colour[i].r - Mid.r);
                pt.g = Dir.g * (Colour[i].g - Mid.g);
                pt.b = Dir.b * (Colour[i].b - Mid.b);

                float f = 0;
                f = pt.r + pt.g + pt.b;
                fDir[0] += f * f;

                f = pt.r + pt.g - pt.b;
                fDir[1] += f * f;

                f = pt.r - pt.g + pt.b;
                fDir[2] += f * f;

                f = pt.r - pt.g - pt.b;
                fDir[3] += f * f;
            }

            float fDirMax = fDir[0];
            int iDirMax = 0;
            for (int iDir = 1; iDir < 4; iDir++)
            {
                if (fDir[iDir] > fDirMax)
                {
                    fDirMax = fDir[iDir];
                    iDirMax = iDir;
                }
            }

            if ((iDirMax & 2) != 0)
            {
                float f = X.g;
                X.g = Y.g;
                Y.g = f;
            }

            if ((iDirMax & 1) != 0)
            {
                float f = X.b;
                X.b = Y.b;
                Y.b = f;
            }

            if (fDiag < 1f / 4096f)
            {
                RGBColour min1 = new RGBColour();
                min1.r = X.r;
                min1.g = X.g;
                min1.b = X.b;

                RGBColour max1 = new RGBColour();
                max1.r = Y.r;
                max1.g = Y.g;
                max1.b = Y.b;


                return new RGBColour[] { min1, max1 };
            }

            // newtons method for local min of sum of squares error.
            float fsteps = uSteps - 1;
            for (int iteration = 0; iteration < 8; iteration++)
            {
                RGBColour[] pSteps = new RGBColour[4];

                for (int iStep = 0; iStep < uSteps; iStep++)
                {
                    pSteps[iStep].r = X.r * pC[iStep] + Y.r * pD[iStep];
                    pSteps[iStep].g = X.g * pC[iStep] + Y.g * pD[iStep];
                    pSteps[iStep].b = X.b * pC[iStep] + Y.b * pD[iStep];
                }


                // colour direction
                Dir.r = Y.r - X.r;
                Dir.g = Y.g - X.g;
                Dir.b = Y.b - X.b;

                float fLen = Dir.r * Dir.r + Dir.g * Dir.g + Dir.b * Dir.b;

                if (fLen < (1f / 4096f))
                    break;

                float fScale = fsteps / fLen;
                Dir.r *= fScale;
                Dir.g *= fScale;
                Dir.b *= fScale;

                // Evaluate function and derivatives
                float d2X = 0, d2Y = 0;
                RGBColour dX, dY;
                dX = new RGBColour();
                dY = new RGBColour();

                for (int i = 0; i < Colour.Length; i++)
                {
                    RGBColour current = Colour[i];

                    float fDot = (current.r - X.r) * Dir.r + (current.g - X.g) * Dir.g + (current.b - X.b) * Dir.b;

                    int iStep = 0;
                    if (fDot <= 0)
                        iStep = 0;
                    else if (fDot >= fsteps)
                        iStep = uSteps - 1;
                    else
                        iStep = (int)(fDot + .5f);

                    RGBColour diff = new RGBColour();
                    diff.r = pSteps[iStep].r - current.r;
                    diff.g = pSteps[iStep].g - current.g;
                    diff.b = pSteps[iStep].b - current.b;

                    float fC = pC[iStep] * 1f / 8f;
                    float fD = pD[iStep] * 1f / 8f;

                    d2X += fC * pC[iStep];
                    dX.r += fC * diff.r;
                    dX.g += fC * diff.g;
                    dX.b += fC * diff.b;

                    d2Y += fD * pD[iStep];
                    dY.r += fD * diff.r;
                    dY.g += fD * diff.g;
                    dY.b += fD * diff.b;
                }

                // Move endpoints
                if (d2X > 0f)
                {
                    float f = -1f / d2X;
                    X.r += dX.r * f;
                    X.g += dX.g * f;
                    X.b += dX.b * f;
                }

                if (d2Y > 0f)
                {
                    float f = -1f / d2Y;
                    Y.r += dY.r * f;
                    Y.g += dY.g * f;
                    Y.b += dY.b * f;
                }

                float fEpsilon = (0.25f / 64.0f) * (0.25f / 64.0f);
                if ((dX.r * dX.r < fEpsilon) && (dX.g * dX.g < fEpsilon) && (dX.b * dX.b < fEpsilon) &&
                    (dY.r * dY.r < fEpsilon) && (dY.g * dY.g < fEpsilon) && (dY.b * dY.b < fEpsilon))
                {
                    break;
                }
            }

            RGBColour min = new RGBColour();
            min.r = X.r;
            min.g = X.g;
            min.b = X.b;

            RGBColour max = new RGBColour();
            max.r = Y.r;
            max.g = Y.g;
            max.b = Y.b;


            return new RGBColour[] { min, max };
        }

        private static byte[] CompressRGBTexel(byte[] texel, bool isDXT1, float alphaRef)
        {
            bool dither = true;
            int uSteps = 4;

            // Determine if texel is fully and entirely transparent. If so, can set to white and continue.
            if (isDXT1)
            {
                int uColourKey = 0;

                // Alpha stuff
                for (int i = 0; i < texel.Length; i += 4)
                {
                    RGBColour colour = ReadColourFromTexel(texel, i);
                    if (colour.a < alphaRef)
                        uColourKey++;
                }

                if (uColourKey == 16)
                {
                    // Entire texel is transparent

                    byte[] retval1 = new byte[8];
                    retval1[2] = byte.MaxValue;
                    retval1[3] = byte.MaxValue;

                    retval1[4] = byte.MaxValue;
                    retval1[5] = byte.MaxValue;
                    retval1[6] = byte.MaxValue;
                    retval1[7] = byte.MaxValue;

                    return retval1;
                }

                uSteps = uColourKey > 0 ? 3 : 4;
            }

            RGBColour[] Colour = new RGBColour[16];
            RGBColour[] Error = new RGBColour[16];

            int index = 0;
            for (int i = 0; i < texel.Length; i += 4)
            {
                index = i / 4;
                RGBColour current = ReadColourFromTexel(texel, i);

                if (dither)
                {
                    // Adjust for accumulated error
                    // This works by figuring out the error between the current pixel colour and the adjusted colour? Dunno what the adjustment is. Looks like a 5:6:5 range adaptation
                    // Then, this error is distributed across the "next" few pixels and not the previous.
                    current.r += Error[index].r;
                    current.g += Error[index].g;
                    current.b += Error[index].b;
                }


                // 5:6:5 range adaptation?
                Colour[index].r = (int)(current.r * 31f + .5f) * (1f / 31f);
                Colour[index].g = (int)(current.g * 63f + .5f) * (1f / 63f);
                Colour[index].b = (int)(current.b * 31f + .5f) * (1f / 31f);

                if (dither)
                {
                    // Calculate difference between current pixel colour and adapted pixel colour?
                    RGBColour diff = new RGBColour();
                    diff.r = current.a * (byte)(current.r - Colour[index].r);
                    diff.g = current.a * (byte)(current.g - Colour[index].g);
                    diff.b = current.a * (byte)(current.b - Colour[index].b);

                    // If current pixel is not at the end of a row
                    if ((index & 3) != 3)
                    {
                        Error[index + 1].r += diff.r * (7f / 16f);
                        Error[index + 1].g += diff.g * (7f / 16f);
                        Error[index + 1].b += diff.b * (7f / 16f);
                    }

                    // If current pixel is not in bottom row
                    if (index < 12)
                    {
                        // If current pixel IS at end of row
                        if ((index & 3) != 0)
                        {
                            Error[index + 3].r += diff.r * (3f / 16f);
                            Error[index + 3].g += diff.g * (3f / 16f);
                            Error[index + 3].b += diff.b * (3f / 16f);
                        }

                        Error[index + 4].r += diff.r * (5f / 16f);
                        Error[index + 4].g += diff.g * (5f / 16f);
                        Error[index + 4].b += diff.b * (5f / 16f);

                        // If current pixel is not at end of row
                        if ((index & 3) != 3)
                        {
                            Error[index + 5].r += diff.r * (1f / 16f);
                            Error[index + 5].g += diff.g * (1f / 16f);
                            Error[index + 5].b += diff.b * (1f / 16f);
                        }
                    }
                }

                Colour[index].r *= Luminance.r;
                Colour[index].g *= Luminance.g;
                Colour[index].b *= Luminance.b;
            }

            // Palette colours
            RGBColour ColourA, ColourB, ColourC, ColourD;
            ColourA = new RGBColour();
            ColourB = new RGBColour();
            ColourC = new RGBColour();
            ColourD = new RGBColour();

            // OPTIMISER
            RGBColour[] minmax = OptimiseRGB(Colour, uSteps);
            ColourA = minmax[0];
            ColourB = minmax[1];

            // Create interstitial colours?
            ColourC.r = ColourA.r * LuminanceInv.r;
            ColourC.g = ColourA.g * LuminanceInv.g;
            ColourC.b = ColourA.b * LuminanceInv.b;

            ColourD.r = ColourB.r * LuminanceInv.r;
            ColourD.g = ColourB.g * LuminanceInv.g;
            ColourD.b = ColourB.b * LuminanceInv.b;


            // Yeah...dunno
            uint wColourA = Encode565(ColourC);
            uint wColourB = Encode565(ColourD);

            if (uSteps == 4 && wColourA == wColourB)
            {
                var bits = new byte[8];
                var c2 = BitConverter.GetBytes(wColourA);
                var c1 = BitConverter.GetBytes(wColourB);  //////////////////////////////////////////////////// MIN MAX
                bits[0] = c2[0];
                bits[1] = c2[1];

                bits[2] = c1[0];
                bits[3] = c1[1];
                return bits;
            }

            ColourC = Decode565(wColourA);
            ColourD = Decode565(wColourB);

            ColourA.r = ColourC.r * Luminance.r;
            ColourA.g = ColourC.g * Luminance.g;
            ColourA.b = ColourC.b * Luminance.b;

            ColourB.r = ColourD.r * Luminance.r;
            ColourB.g = ColourD.g * Luminance.g;
            ColourB.b = ColourD.b * Luminance.b;


            // Create palette colours
            RGBColour[] step = new RGBColour[4];
            uint Min = 0;
            uint Max = 0;

            if ((uSteps == 3) == (wColourA <= wColourB))
            {
                Min = wColourA;
                Max = wColourB;
                step[0] = ColourA;
                step[1] = ColourB;
            }
            else
            {
                Min = wColourB;
                Max = wColourA;
                step[0] = ColourB;
                step[1] = ColourA;
            }

            uint[] psteps;

            if (uSteps == 3)
            {
                psteps = psteps3;

                step[2].r = step[0].r + (1f / 2f) * (step[1].r - step[0].r);
                step[2].g = step[0].g + (1f / 2f) * (step[1].g - step[0].g);
                step[2].b = step[0].b + (1f / 2f) * (step[1].b - step[0].b);
            }
            else
            {
                psteps = psteps4;

                // "step" appears to be the palette as this is the interpolation
                step[2].r = step[0].r + (1f / 3f) * (step[1].r - step[0].r);
                step[2].g = step[0].g + (1f / 3f) * (step[1].g - step[0].g);
                step[2].b = step[0].b + (1f / 3f) * (step[1].b - step[0].b);

                step[3].r = step[0].r + (2f / 3f) * (step[1].r - step[0].r);
                step[3].g = step[0].g + (2f / 3f) * (step[1].g - step[0].g);
                step[3].b = step[0].b + (2f / 3f) * (step[1].b - step[0].b);
            }



            // Calculating colour direction apparently
            RGBColour Dir = new RGBColour();
            Dir.r = step[1].r - step[0].r;
            Dir.g = step[1].g - step[0].g;
            Dir.b = step[1].b - step[0].b;

            int fsteps = uSteps - 1;
            float fscale = (wColourA != wColourB) ? (fsteps / (Dir.r * Dir.r + Dir.g * Dir.g + Dir.b * Dir.b)) : 0.0f;
            Dir.r *= fscale;
            Dir.g *= fscale;
            Dir.b *= fscale;


            // Encoding colours apparently
            Array.Clear(Error, 0, Error.Length);  // Clear error for next bit
            uint dw = 0;
            index = 0;
            for (int i = 0; i < texel.Length; i += 4)
            {
                index = i / 4;
                RGBColour current = ReadColourFromTexel(texel, i);

                if ((uSteps == 3) && (current.a < alphaRef))
                {
                    dw = (uint)((3 << 30) | (dw >> 2));
                    continue;
                }

                current.r *= Luminance.r;
                current.g *= Luminance.g;
                current.b *= Luminance.b;


                if (dither)
                {
                    // Error again
                    current.r += Error[index].r;
                    current.g += Error[index].g;
                    current.b += Error[index].b;
                }


                float fdot = (current.r - step[0].r) * Dir.r + (current.g - step[0].g) * Dir.g + (current.b - step[0].b) * Dir.b;

                uint iStep = 0;
                if (fdot <= 0f)
                    iStep = 0;
                else if (fdot >= fsteps)
                    iStep = 1;
                else
                    iStep = psteps[(int)(fdot + .5f)];

                dw = (iStep << 30) | (dw >> 2);   // THIS  IS THE MAGIC here. This is the "list" of indicies. Somehow...


                // Dither again
                if (dither)
                {
                    // Calculate difference between current pixel colour and adapted pixel colour?
                    RGBColour diff = new RGBColour();
                    diff.r = current.a * (byte)(current.r - step[iStep].r);
                    diff.g = current.a * (byte)(current.g - step[iStep].g);
                    diff.b = current.a * (byte)(current.b - step[iStep].b);

                    // If current pixel is not at the end of a row
                    if ((index & 3) != 3)
                    {
                        Error[index + 1].r += diff.r * (7f / 16f);
                        Error[index + 1].g += diff.g * (7f / 16f);
                        Error[index + 1].b += diff.b * (7f / 16f);
                    }

                    // If current pixel is not in bottom row
                    if (index < 12)
                    {
                        // If current pixel IS at end of row
                        if ((index & 3) != 0)
                        {
                            Error[index + 3].r += diff.r * (3f / 16f);
                            Error[index + 3].g += diff.g * (3f / 16f);
                            Error[index + 3].b += diff.b * (3f / 16f);
                        }

                        Error[index + 4].r += diff.r * (5f / 16f);
                        Error[index + 4].g += diff.g * (5f / 16f);
                        Error[index + 4].b += diff.b * (5f / 16f);

                        // If current pixel is not at end of row
                        if ((index & 3) != 3)
                        {
                            Error[index + 5].r += diff.r * (1f / 16f);
                            Error[index + 5].g += diff.g * (1f / 16f);
                            Error[index + 5].b += diff.b * (1f / 16f);
                        }
                    }
                }
            }

            byte[] retval = new byte[8];
            var colour1 = BitConverter.GetBytes(Min);
            var colour2 = BitConverter.GetBytes(Max);
            retval[0] = colour1[0];
            retval[1] = colour1[1];

            retval[2] = colour2[0];
            retval[3] = colour2[1];

            var indicies = BitConverter.GetBytes(dw);
            retval[4] = indicies[0];
            retval[5] = indicies[1];
            retval[6] = indicies[2];
            retval[7] = indicies[3];

            return retval;
        }
        #endregion RGB DXT

        private static int GetClosestValue(byte[] arr, byte c)
        {
            int min = int.MaxValue;
            int index = 0;
            int minIndex = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                int check = arr[i] - c;
                check = (check ^ (check >> 7)) - (check >> 7);
                if (check < min)
                {
                    min = check;
                    minIndex = index;
                }

                index++;
            }
            return minIndex;
        }

        /// <summary>
        /// Compresses single channel using Block Compression.
        /// </summary>
        /// <param name="texel">4 channel Texel to compress.</param>
        /// <param name="channel">0-3 (BGRA)</param>
        /// <param name="isSigned">true = uses alpha range -255 -- 255, else 0 -- 255</param>
        /// <returns>8 byte compressed texel.</returns>
        public static byte[] Compress8BitBlock(byte[] texel, int channel, bool isSigned)
        {
            // KFreon: Get min and max
            byte min = byte.MaxValue;
            byte max = byte.MinValue;
            int count = channel;
            for (int i = 0; i < 16; i++)
            {
                byte colour = texel[count];
                if (colour > max)
                    max = colour;
                else if (colour < min)
                    min = colour;

                count += 4; // skip to next entry in channel
            }

            // Build Palette
            byte[] Colours = Build8BitPalette(min, max, isSigned);

            // Compress Pixels
            ulong line = 0;
            count = channel;
            List<int> indicies = new List<int>();
            for (int i = 0; i < 16; i++)
            {
                byte colour = texel[count];
                int index = GetClosestValue(Colours, colour);
                indicies.Add(index);
                line |= (ulong)index << (i * 3);
                count += 4;  // Only need 1 channel
            }

            byte[] CompressedBlock = new byte[8];
            byte[] compressed = BitConverter.GetBytes(line);
            CompressedBlock[0] = min;
            CompressedBlock[1] = max;
            for (int i = 2; i < 8; i++)
                CompressedBlock[i] = compressed[i - 2];

            return CompressedBlock;
        }
        #endregion Block Compression

        #region Block Decompression
        /// <summary>
        /// Decompresses an 8 bit channel.
        /// </summary>
        /// <param name="compressed">Compressed image data.</param>
        /// <param name="isSigned">true = use signed alpha range (-254 -- 255), false = 0 -- 255</param>
        /// <returns>Single channel decompressed (16 bits).</returns>
        internal static byte[] Decompress8BitBlock(Stream compressed, bool isSigned)
        {
            byte[] DecompressedBlock = new byte[16];

            // KFreon: Read min and max colours (not necessarily in that order)
            byte[] block = new byte[8];
            compressed.Read(block, 0, 8);

            byte min = block[0];
            byte max = block[1];

            byte[] Colours = Build8BitPalette(min, max, isSigned);

            // KFreon: Decompress pixels
            ulong bitmask = (ulong)block[2] << 0 | (ulong)block[3] << 8 | (ulong)block[4] << 16 |   // KFreon: Read all 6 compressed bytes into single 
                (ulong)block[5] << 24 | (ulong)block[6] << 32 | (ulong)block[7] << 40;


            // KFreon: Bitshift and mask compressed data to get 3 bit indicies, and retrieve indexed colour of pixel.
            for (int i = 0; i < 16; i++)
                DecompressedBlock[i] = (byte)Colours[bitmask >> (i * 3) & 0x7];

            return DecompressedBlock;
        }

        /// <summary>
        /// Decompresses a 3 channel (RGB) block.
        /// </summary>
        /// <param name="compressed">Compressed image data.</param>
        /// <param name="isDXT1">True = DXT1, otherwise false.</param>
        /// <returns>16 pixel BGRA channels.</returns>
        internal static List<byte[]> DecompressRGBBlock(Stream compressed, bool isDXT1)
        {
            int[] DecompressedBlock = new int[16];

            ushort colour0;
            ushort colour1;
            byte[] pixels = null;
            int[] Colours = null;

            List<byte[]> DecompressedChannels = new List<byte[]>(4);
            byte[] red = new byte[16];
            byte[] green = new byte[16];
            byte[] blue = new byte[16];
            byte[] alpha = new byte[16];
            DecompressedChannels.Add(blue);
            DecompressedChannels.Add(green);
            DecompressedChannels.Add(red);
            DecompressedChannels.Add(alpha);

            try
            {
                using (BinaryReader reader = new BinaryReader(compressed, Encoding.Default, true))
                {
                    // Read min max colours
                    colour0 = (ushort)reader.ReadInt16();
                    colour1 = (ushort)reader.ReadInt16();
                    Colours = BuildRGBPalette(colour0, colour1, isDXT1);

                    // Decompress pixels
                    pixels = reader.ReadBytes(4);
                }
            }
            catch (EndOfStreamException e)
            {
                Debug.WriteLine(e.ToString());
                // It's due to weird shaped mips at really low resolution. Like 2x4

                return DecompressedChannels;
            }



            for (int i = 0; i < 16; i += 4)
            {
                //byte bitmask = (byte)compressed.ReadByte();
                byte bitmask = pixels[i / 4];
                for (int j = 0; j < 4; j++)
                    DecompressedBlock[i + j] = Colours[bitmask >> (2 * j) & 0x03];
            }

            // KFreon: Decode into BGRA
            for (int i = 0; i < 16; i++)
            {
                int colour = DecompressedBlock[i];
                var rgb = ReadDXTColour(colour);
                red[i] = rgb[0];
                green[i] = rgb[1];
                blue[i] = rgb[2];
                alpha[i] = 0xFF;
            }
            return DecompressedChannels;
        }
        #endregion

        #region Palette/Colour
        /// <summary>
        /// Reads a packed DXT colour into RGB
        /// </summary>
        /// <param name="colour">Colour to convert to RGB</param>
        /// <returns>RGB bytes</returns>
        private static byte[] ReadDXTColour(int colour)
        {
            // Read RGB 5:6:5 data
            var b = (colour & 0x1F);
            var g = (colour & 0x7E0) >> 5;
            var r = (colour & 0xF800) >> 11;


            // Expand to 8 bit data
            byte testr = (byte)(r << 3);
            byte testg = (byte)(g << 2);
            byte testb = (byte)(b << 3);
            return new byte[3] { testr, testg, testb };
        }


        /// <summary>
        /// Creates a packed DXT colour from RGB.
        /// </summary>
        /// <param name="r">Red byte.</param>
        /// <param name="g">Green byte.</param>
        /// <param name="b">Blue byte.</param>
        /// <returns>DXT Colour</returns>
        private static int BuildDXTColour(byte r, byte g, byte b)
        {
            // Compress to 5:6:5
            byte r1 = (byte)(r >> 3);
            byte g1 = (byte)(g >> 2);
            byte b1 = (byte)(b >> 3);

            return (r1 << 11) | (g1 << 5) | (b1);
        }


        /// <summary>
        /// Builds palette for 8 bit channel.
        /// </summary>
        /// <param name="min">First main colour (often actually minimum)</param>
        /// <param name="max">Second main colour (often actually maximum)</param>
        /// <param name="isSigned">true = sets signed alpha range (-254 -- 255), false = 0 -- 255</param>
        /// <returns>8 byte colour palette.</returns>
        internal static byte[] Build8BitPalette(byte min, byte max, bool isSigned)
        {
            byte[] Colours = new byte[8];
            Colours[0] = min;
            Colours[1] = max;

            // KFreon: Choose which type of interpolation is required
            if (min > max)
            {
                // KFreon: Interpolate other colours
                for (int i = 2; i < 8; i++)
                {
                    double test = min + (max - min) * (i - 1) / 7;
                    Colours[i] = (byte)test;
                }
            }
            else
            {
                // KFreon: Interpolate other colours and add Opacity or something...
                for (int i = 2; i < 6; i++)
                {
                    //double test = ((8 - i) * min + (i - 1) * max) / 5.0f;   // KFreon: "Linear interpolation". Serves me right for trusting a website without checking it...
                    double extratest = min + (max - min) * (i - 1) / 5;
                    Colours[i] = (byte)extratest;
                }
                Colours[6] = (byte)(isSigned ? -254 : 0);  // KFreon: snorm and unorm have different alpha ranges
                Colours[7] = 255;
            }

            return Colours;
        }



        /// <summary>
        /// Builds an RGB palette from the min and max colours of a texel.
        /// </summary>
        /// <param name="Colour0">First colour, usually the min.</param>
        /// <param name="Colour1">Second colour, usually the max.</param>
        /// <param name="isDXT1">True = for DXT1 texels. Changes how the internals are calculated.</param>
        /// <returns>Texel palette.</returns>
        public static int[] BuildRGBPalette(int Colour0, int Colour1, bool isDXT1)
        {
            int[] Colours = new int[4];

            Colours[0] = Colour0;
            Colours[1] = Colour1;

            var Colour0s = ReadDXTColour(Colour0);
            var Colour1s = ReadDXTColour(Colour1);


            // Interpolate other 2 colours
            if (Colour0 > Colour1)
            {
                var r1 = (byte)(2f / 3f * Colour0s[0] + 1f / 3f * Colour1s[0]);
                var g1 = (byte)(2f / 3f * Colour0s[1] + 1f / 3f * Colour1s[1]);
                var b1 = (byte)(2f / 3f * Colour0s[2] + 1f / 3f * Colour1s[2]);

                var r2 = (byte)(1f / 3f * Colour0s[0] + 2f / 3f * Colour1s[0]);
                var g2 = (byte)(1f / 3f * Colour0s[1] + 2f / 3f * Colour1s[1]);
                var b2 = (byte)(1f / 3f * Colour0s[2] + 2f / 3f * Colour1s[2]);

                Colours[2] = BuildDXTColour(r1, g1, b1);
                Colours[3] = BuildDXTColour(r2, g2, b2);
            }
            else
            {
                // KFreon: Only for dxt1
                var r = (byte)(1 / 2f * Colour0s[0] + 1 / 2f * Colour1s[0]);
                var g = (byte)(1 / 2f * Colour0s[1] + 1 / 2f * Colour1s[1]);
                var b = (byte)(1 / 2f * Colour0s[2] + 1 / 2f * Colour1s[2]);

                Colours[2] = BuildDXTColour(r, g, b);
                Colours[3] = 0;
            }
            return Colours;
        }
        #endregion Palette/Colour

        /// <summary>
        /// Gets 4x4 texel block from stream.
        /// </summary>
        /// <param name="pixelData">Image pixels.</param>
        /// <param name="Width">Width of image.</param>
        /// <param name="Height">Height of image.</param>
        /// <returns>4x4 texel.</returns>
        internal static byte[] GetTexel(Stream pixelData, int Width, int Height)
        {
            byte[] texel = new byte[16 * 4]; // 16 pixels, 4 bytes per pixel

            // KFreon: Edge case for when dimensions are too small for texel
            int count = 0;
            if (Width < 4 || Height < 4)
            {
                for (int h = 0; h < Height; h++)
                    for (int w = 0; w < Width; w++)
                        for (int i = 0; i < 4; i++)
                        {
                            if (count >= 64)
                                return texel;
                            else
                                texel[count++] = (byte)pixelData.ReadByte();
                        }

                return texel;
            }

            // KFreon: Normal operation. Read 4x4 texel row by row.
            int bitsPerScanLine = 4 * Width;
            for (int i = 0; i < 64; i += 16)  // pixel rows
            {
                pixelData.Read(texel, i, 16);
                /*for (int j = 0; j < 16; j += 4)  // pixels in row
                    for (int k = 0; k < 4; k++) // BGRA
                        texel[i + j + k] = (byte)pixelData.ReadByte();*/

                pixelData.Seek(bitsPerScanLine - 4 * 4, SeekOrigin.Current);  // Seek to next line of texel
            }


            return texel;
        }
    }
}
