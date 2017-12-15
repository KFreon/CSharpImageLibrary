using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static CSharpImageLibrary.DDS.DX10_Helpers;

namespace CSharpImageLibrary.DDS
{
    internal static class DDS_BlockHelpers
    {
        const float OneThird = 1f / 3f;
        const float TwoThirds = 2f / 3f;

        #region Block Compression
        #region RGB DXT
        /// <summary>
        /// This region contains stuff adpated/taken from the DirectXTex project: https://github.com/Microsoft/DirectXTex
        /// Things needed to be in the range 0-1 instead of 0-255, hence new struct etc
        /// </summary>
        [DebuggerDisplay("R:{r}, G:{g}, B:{b}, A:{a}")]
        internal struct RGBColour
        {
            internal Vector<float> Colours;
            public float r => Colours[0];
            public float g => Colours[1];
            public float b => Colours[2];
            public float a => Colours[3];


            public RGBColour(float red, float green, float blue, float alpha)
            {
                Colours = new Vector<float>(new[] { red, green, blue, alpha, 0, 0, 0, 0 });
            }

            public override string ToString()
            {
                return $"{r.ToString("F6")} {g.ToString("F6")} {b.ToString("F6")} {a.ToString("F6")}";
            }
        }

        internal static float[] pC3 = { 1f, 1f / 2f, 0f };
        internal static float[] pD3 = { 0f, 1f / 2f, 1f };

        internal static float[] pC4 = { 1f, 2f / 3f, 1f / 3f, 0f };
        internal static float[] pD4 = { 0f, 1f / 3f, 2f / 3f, 1f };

        static uint[] psteps3 = { 0, 2, 1 };
        static uint[] psteps4 = { 0, 2, 3, 1 };

        static RGBColour Luminance = new RGBColour(0.2125f / 0.7154f, 1f, 0.0721f / 0.7154f, 1f);
        static RGBColour LuminanceInv = new RGBColour(0.7154f / 0.2125f, 1f, 0.7154f / 0.0721f, 1f);

        static Vector<float> decodeAND = new Vector<float>(new float[] { 0x1F, 0x3F, 0x1F, 0,0,0,0,0 });
        static Vector<float> decodeDivide = new Vector<float>(new float[] { 31f, 63f, 31f, 0,0,0,0,0 });

        static RGBColour Decode565(uint wColour)
        {
            var test = new Vector<float>(new float[] { wColour >> 11, wColour >> 5, wColour, 1, 0, 0, 0, 0 });
            test = Vector.ConditionalSelect(decodeAND, test, Vector<float>.Zero);
            test /= decodeDivide;

            RGBColour colour = new RGBColour
            {
                Colours = test
            };
            return colour;
        }

        static uint Encode565(RGBColour colour)
        {
            var colours = Vector.LessThan(colour.Colours, Vector<float>.Zero);
            colours = Vector.GreaterThan(colours, Vector<int>.One);
            var temp = Vector.ConditionalSelect(colours, colour.Colours, Vector<float>.Zero);
            temp = temp * decodeDivide + new Vector<float>(0.5f);


            return (uint)temp[0] << 11 | (uint)temp[1] << 5 | (uint)temp[2];
        }


        static RGBColour ReadColourFromTexel(byte[] texel, int i, bool premultiply, ImageFormats.ImageEngineFormatDetails formatDetails)
        {
            // Pull out rgb from texel
            // Create current pixel colour

            // Check that texel is big enough
            if (i + 3 >= texel.Length)
                return new RGBColour();  // Fully transparent colour


            var a = formatDetails.ReadFloat(texel, i + 3 * formatDetails.ComponentSize);
            var r = formatDetails.ReadFloat(texel, i + 2 * formatDetails.ComponentSize) * (premultiply ? a : 1.0f);
            var g = formatDetails.ReadFloat(texel, i + formatDetails.ComponentSize) * (premultiply ? a : 1.0f);
            var b = formatDetails.ReadFloat(texel, i) * (premultiply ? a : 1.0f);
            
            return new RGBColour(r, g, b, a);
        }


        private static Vector<float> GetfDirRGB(RGBColour Dir, RGBColour Mid, int loopCount, Func<int, RGBColour> currentGetter)
        {
            Vector<float> fDir = new Vector<float>();
            for (int i = 0; i < loopCount; i++)
            {
                var current = currentGetter(i);
                RGBColour pt = new RGBColour()
                {
                    Colours = Dir.Colours * (current.Colours - Mid.Colours)
                };

                var ptr = new Vector<float>(pt.r);
                var ptg = new Vector<float>(new[] { pt.g, pt.g, -pt.g, -pt.g, 0, 0, 0, 0 });
                var ptb = new Vector<float>(new[] { pt.b, -pt.b, pt.b, -pt.b, 0, 0, 0, 0 });

                var f = ptr + ptg + ptb;
                fDir += f * f;
            }

            return fDir;
        }

        private static Vector<float> GetfDirWithA(RGBColour Dir, RGBColour Mid, int loopCount, Func<int, RGBColour> currentGetter)
        {
            Vector<float> fDir = new Vector<float>();
            for (int i = 0; i < loopCount; i++)
            {
                var current = currentGetter(i);
                RGBColour pt = new RGBColour()
                {
                    Colours = Dir.Colours * (current.Colours - Mid.Colours)
                };

                var ptr = new Vector<float>(pt.r);
                var ptg = new Vector<float>(new[] { pt.g, pt.g, pt.g, pt.g, -pt.g, -pt.g, -pt.g, -pt.g});
                var ptb = new Vector<float>(new[] { pt.b, pt.b, -pt.b, -pt.b, pt.b, pt.b, -pt.b, -pt.b });
                var pta = new Vector<float>(new[] { pt.a, -pt.a, pt.a, -pt.a, pt.a, -pt.a, pt.a, -pt.a });

                var f = ptr + ptg + ptb + pta;
                fDir += f * f;
            }

            return fDir;
        }

        internal static RGBColour[] OptimiseGeneral(int uSteps, int loopCount, Func<int, RGBColour> currentGetter, bool useLuminance, bool useAlpha)
        {
            float[] pC = uSteps == 3 ? pC3 : pC4;
            float[] pD = uSteps == 3 ? pD3 : pD4;


            // Need to wonder if RGB version has alpha, since the dot product in particular affects all elements.

            // Find min max
            RGBColour X = useLuminance ? (useAlpha ? Luminance : new RGBColour(Luminance.r, Luminance.g, Luminance.b, 0)) : new RGBColour(1,1,1, useAlpha ? 1 : 0);
            RGBColour Y = new RGBColour();

            for (int i = 0; i < loopCount; i++)
            {
                var current = currentGetter(i);
                // X = min, Y = max
                X.Colours = Vector.Min(X.Colours, current.Colours);
                Y.Colours = Vector.Max(Y.Colours, current.Colours);
            }

            // Diagonal axis - starts with difference between min and max
            RGBColour diag = new RGBColour()
            {
                Colours = Y.Colours - X.Colours
            };

            float fDiag = 0;
            if (useAlpha)
                fDiag = Vector.Dot(diag.Colours, diag.Colours);
            else
            {
                var temp = new Vector<float>(new[] { diag.r, diag.g, diag.b, 0, 0, 0, 0, 0 });
                fDiag = Vector.Dot(temp, temp);
            }

            if (fDiag < 1.175494351e-38F)
                return new RGBColour[] { X, Y };

            float FdiagInv = 1f / fDiag;

            RGBColour Dir = new RGBColour()
            {
                Colours = diag.Colours * new Vector<float>(FdiagInv)
            };
            RGBColour Mid = new RGBColour()
            {
                Colours = (X.Colours + Y.Colours) * new Vector<float>(0.5f)
            };

            var fDir = new Vector<float>();
            if (useAlpha)
                fDir = GetfDirRGB(Dir, Mid, loopCount, currentGetter);
            else
                fDir = GetfDirWithA(Dir, Mid, loopCount, currentGetter);

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
                X.Colours = new Vector<float>(new[] { X.r, Y.g, X.b, X.a, 0, 0, 0, 0 });
                Y.Colours = new Vector<float>(new[] { Y.r, f, Y.b, Y.a, 0, 0, 0, 0 });
            }

            if ((iDirMax & 1) != 0)
            {
                float f = X.b;
                X.Colours = new Vector<float>(new[] { X.r, X.g, Y.b, X.a, 0, 0, 0, 0 });
                Y.Colours = new Vector<float>(new[] { Y.r, Y.g, f, Y.a, 0, 0, 0, 0 });
            }

            if (useAlpha && (iDirMax & 1) != 0)
            {
                float f = X.a;
                X.Colours = new Vector<float>(new[] { X.r, X.g, X.b, Y.a, 0, 0, 0, 0 });
                Y.Colours = new Vector<float>(new[] { Y.r, Y.g, Y.b, f, 0, 0, 0, 0 });
            }

            if (fDiag < 1f / 4096f)
                return new RGBColour[] { X, Y };

            // newtons method for local min of sum of squares error.
            float fsteps = uSteps - 1;
            for (int iteration = 0; iteration < 8; iteration++)
            {
                RGBColour[] pSteps = new RGBColour[uSteps];

                for (int iStep = 0; iStep < uSteps; iStep++)
                    pSteps[iStep].Colours = X.Colours * new Vector<float>(pC[iStep]) + Y.Colours * new Vector<float>(pD[iStep]);


                // colour direction
                Dir.Colours = Y.Colours - X.Colours;

                float fLen = 0;
                if (useAlpha)
                    fLen = Vector.Dot(Dir.Colours, Dir.Colours);
                else
                {
                    var temp = new Vector<float>(new[] { Dir.r, Dir.g, Dir.b, 0, 0, 0, 0, 0 });
                    fLen = Vector.Dot(temp, temp);
                }

                if (fLen < (1f / 4096f))
                    break;

                float fScale = fsteps / fLen;
                Dir.Colours *= new Vector<float>(fScale);

                // Evaluate function and derivatives
                float d2X = 0, d2Y = 0;
                RGBColour dX, dY;
                dX = new RGBColour();
                dY = new RGBColour();

                for (int i = 0; i < loopCount; i++)
                {
                    RGBColour current = currentGetter(i);

                    var temp = (current.Colours - X.Colours) * Dir.Colours;
                    float fDot = temp[0] + temp[1] + temp[2] + (useAlpha ? temp[3] : 0);

                    int iStep = 0;
                    if (fDot <= 0)
                        iStep = 0;
                    else if (fDot >= fsteps)
                        iStep = uSteps - 1;
                    else
                        iStep = (int)(fDot + .5f);

                    RGBColour diff = new RGBColour()
                    {
                        Colours = pSteps[iStep].Colours - current.Colours
                    };
                    float fC = pC[iStep] * 1f / 8f;
                    float fD = pD[iStep] * 1f / 8f;

                    d2X += fC * pC[iStep];
                    dX.Colours += new Vector<float>(fC) * diff.Colours;

                    d2Y += fD * pD[iStep];
                    dY.Colours += new Vector<float>(fD) * diff.Colours;
                }

                // Move endpoints
                if (d2X > 0f)
                {
                    float f = -1f / d2X;
                    X.Colours += dX.Colours * new Vector<float>(f);
                }

                if (d2Y > 0f)
                {
                    float f = -1f / d2Y;
                    Y.Colours += dY.Colours * new Vector<float>(f);
                }

                float fEpsilon = (0.25f / 64.0f) * (0.25f / 64.0f);
                var dx2 = dX.Colours * dX.Colours;
                var dy2 = dY.Colours * dY.Colours;
                var both = new Vector<float>(new[] { dx2[0], dx2[1], dx2[2], fEpsilon, dy2[0], dy2[1], dy2[2], fEpsilon });
                if (Vector.LessThanAll(both, new Vector<float>(fEpsilon)))
                    break;
            }

            return new RGBColour[] { X, Y };
        }

        internal static RGBColour[] OptimiseRGB(RGBColour[] Colour, int uSteps)
        {
            return OptimiseGeneral(uSteps, Colour.Length, i => Colour[i], true, false);
        }

        internal static RGBColour[] OptimiseRGB_BC67(RGBColour[] Colour, int uSteps, int np, int[] pixelIndicies)
        {
            return OptimiseGeneral(uSteps, np, i => Colour[pixelIndicies[i]], false, false);
        }


        internal static RGBColour[] OptimiseRGBA_BC67(RGBColour[] Colour, int uSteps, int np, int[] pixelIndicies)
        {
            return OptimiseGeneral(uSteps, np, i => Colour[pixelIndicies[i]], false, true);
        }

        static int CheckDXT1TexelFullTransparency(RGBColour[] texel, byte[] destination, int destPosition, double alphaRef)
        {
            int uColourKey = 0;

            // Alpha stuff
            for (int i = 0; i < 16; i++)
            {
                if (texel[i].a < alphaRef)
                    uColourKey++;
            }

            if (uColourKey == 16)
            {
                // Entire texel is transparent

                for (int i = 0; i < 8; i++)
                    destination[destPosition + i] = 255;

                return -1;
            }

            return uColourKey > 0 ? 3 : 4;
        }

        /// <summary>
        /// Not exactly sure what this does or why.
        /// </summary>
        static void DoColourFixErrorCorrection(RGBColour[] Colour, RGBColour[] texel)
        {
            RGBColour[] Error = new RGBColour[16];
            var ditherMultiplier = new Vector<float>(new[] { 31f, 63, 31, 1,0,0,0,0 });
            var ditherAddition = new Vector<float>(new[] { 0.5f, 0.5f, 0.5f, 0, 0,0,0,0,0 });
            var ditherDivideBit = new Vector<float>(new[] { 1f/31f, 1f/63f, 1f/31f, 1,0,0,0,0});

            for (int i = 0; i < 16; i++)
            {
                RGBColour current = new RGBColour
                {
                    Colours = texel[i].Colours
                };

                if (true)  // Dither
                {
                    // Adjust for accumulated error
                    // This works by figuring out the error between the current pixel colour and the adjusted colour? Dunno what the adjustment is. Looks like a 5:6:5 range adaptation
                    // Then, this error is distributed across the "next" few pixels and not the previous.
                    current.Colours += Error[i].Colours;
                }


                // 5:6:5 range adaptation?
                Colour[i].Colours = (current.Colours * ditherMultiplier + ditherAddition) * ditherDivideBit;
                DoSomeDithering(current, i, Colour, i, Error);
                Colour[i].Colours *= Luminance.Colours;
            }
        }

        static RGBColour[] DoSomethingWithPalette(int uSteps, uint wColourA, uint wColourB, RGBColour ColourA, RGBColour ColourB)
        {
            // Create palette colours
            RGBColour[] step = new RGBColour[4];

            if ((uSteps == 3) == (wColourA <= wColourB))
            {
                step[0] = ColourA;
                step[1] = ColourB;
            }
            else
            {
                step[0] = ColourB;
                step[1] = ColourA;
            }


            if (uSteps == 3)
                step[2].Colours = step[0].Colours + new Vector<float>(0.5f) * (step[1].Colours - step[0].Colours);
            else
            {
                // "step" appears to be the palette as this is the interpolation
                step[2].Colours = step[0].Colours + new Vector<float>(1f/3f) * (step[1].Colours - step[0].Colours);
                step[3].Colours = step[0].Colours + new Vector<float>(2f/3f) * (step[1].Colours - step[0].Colours);
            }

            for (int i = 0; i < step.Length; i++)
            {
                var curr = step[i];
                step[i] = new RGBColour(curr.r, curr.g, curr.b, 1);
            }

            return step;
        }


        static uint DoOtherColourFixErrorCorrection(RGBColour[] texel, int uSteps, double alphaRef, RGBColour[] step, RGBColour Dir)
        {
            uint dw = 0;
            RGBColour[] Error = new RGBColour[16];

            uint[] psteps = uSteps == 3 ? psteps3 : psteps4;
            for (int i = 0; i < 16; i++)
            {
                RGBColour current = new RGBColour();
                current.Colours = texel[i].Colours;

                if ((uSteps == 3) && (current.a < alphaRef))
                {
                    dw = (uint)((3 << 30) | (dw >> 2));
                    continue;
                }
                current.Colours *= Luminance.Colours;

                if (true) // dither
                {
                    // Error again
                    current.Colours += Error[i].Colours;
                }

                float fdot = (current.r - step[0].r) * Dir.r + (current.g - step[0].g) * Dir.g + (current.b - step[0].b) * Dir.b;

                uint iStep = 0;
                if (fdot <= 0f)
                    iStep = 0;
                else if (fdot >= (uSteps - 1))
                    iStep = 1;
                else
                    iStep = psteps[(int)(fdot + .5f)];

                dw = (iStep << 30) | (dw >> 2);   // THIS  IS THE MAGIC here. This is the "list" of indicies. Somehow...

                DoSomeDithering(current, i, step, (int)iStep, Error);
            }

            return dw;
        }

        static void DoSomeDithering(RGBColour current, int index, RGBColour[] InnerColour, int InnerIndex, RGBColour[] Error)
        {
            if (true)  // Dither
            {
                // Calculate difference between current pixel colour and adapted pixel colour?
                var inner = InnerColour[InnerIndex];
                RGBColour diff = new RGBColour()
                {
                    Colours = current.a * (current.Colours - inner.Colours)
                };

                // If current pixel is not at the end of a row
                if ((index & 3) != 3)
                    Error[index + 1].Colours += diff.Colours * (7f / 16f);

                // If current pixel is not in bottom row
                if (index < 12)
                {
                    // If current pixel IS at end of row
                    if ((index & 3) != 0)
                        Error[index + 3].Colours += diff.Colours * (3f / 16f);

                    Error[index + 4].Colours += diff.Colours * (5f / 16f);

                    // If current pixel is not at end of row
                    if ((index & 3) != 3)
                        Error[index + 5].Colours += diff.Colours * (1f / 16f);
                }
            }
        }

        internal static void CompressRGBTexel(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, bool isDXT1, double alphaRef, AlphaSettings alphaSetting, ImageFormats.ImageEngineFormatDetails formatDetails)
        {
            int uSteps = 4;

            bool premultiply = alphaSetting == AlphaSettings.Premultiply;

            // Read texel
            RGBColour[] sourceTexel = new RGBColour[16];
            int position = sourcePosition;
            int count = 0;
            for (int i = 1; i <= 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    sourceTexel[count++] = ReadColourFromTexel(imgData, position, premultiply, formatDetails);
                    position += 4 * formatDetails.ComponentSize;
                }

                position = sourcePosition + sourceLineLength * i;
            }


            // TODO replace RGBColour with a SIMD vector for speed. Test difference between vector4 and vector<T>, might not be better.

            // Determine if texel is fully and entirely transparent. If so, can set to white and continue.
            if (isDXT1)
            {
                uSteps = CheckDXT1TexelFullTransparency(sourceTexel, destination, destPosition, alphaRef);
                if (uSteps == -1)
                    return;
            }

            RGBColour[] Colour = new RGBColour[16];

            // Some kind of colour adjustment. Not sure what it does, especially if it wasn't dithering...
            DoColourFixErrorCorrection(Colour, sourceTexel);
            

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
            ColourC.Colours = ColourA.Colours * LuminanceInv.Colours;
            ColourD.Colours = ColourB.Colours * LuminanceInv.Colours;

            // Yeah...dunno
            uint wColourA = Encode565(ColourC);
            uint wColourB = Encode565(ColourD);

            // Min max are equal - only interpolate 4 interstitial colours
            if (uSteps == 4 && wColourA == wColourB)
            {
                var c2 = BitConverter.GetBytes(wColourA);
                var c1 = BitConverter.GetBytes(wColourB);  ///////////////////// MIN MAX

                destination[destPosition] = c2[0];
                destination[destPosition + 1] = c2[1];

                destination[destPosition + 2] = c1[0];
                destination[destPosition + 3] = c1[1];
                return;
            }

            // Interpolate 6 colours or something
            ColourC = Decode565(wColourA);
            ColourD = Decode565(wColourB);

            ColourA.Colours = ColourC.Colours * Luminance.Colours;
            ColourB.Colours = ColourD.Colours * Luminance.Colours;

            var step = DoSomethingWithPalette(uSteps, wColourA, wColourB, ColourA, ColourB);

            // Calculating colour direction apparently
            RGBColour Dir = new RGBColour()
            {
                Colours = step[1].Colours - step[0].Colours
            };
            float fscale = (wColourA != wColourB) ? ((uSteps - 1) / (Dir.r * Dir.r + Dir.g * Dir.g + Dir.b * Dir.b)) : 0.0f;
            Dir.Colours *= fscale;

            // Encoding colours apparently
            uint dw = DoOtherColourFixErrorCorrection(sourceTexel, uSteps, alphaRef, step, Dir);

            uint Min = (uSteps == 3) == (wColourA <= wColourB) ? wColourA : wColourB;
            uint Max = (uSteps == 3) == (wColourA <= wColourB) ? wColourB : wColourA;

            var colour1 = BitConverter.GetBytes(Min);
            var colour2 = BitConverter.GetBytes(Max);

            destination[destPosition] = colour1[0];
            destination[destPosition + 1] = colour1[1];

            destination[destPosition + 2] = colour2[0];
            destination[destPosition + 3] = colour2[1];

            var indicies = BitConverter.GetBytes(dw);
            destination[destPosition + 4] = indicies[0];
            destination[destPosition + 5] = indicies[1];
            destination[destPosition + 6] = indicies[2];
            destination[destPosition + 7] = indicies[3];
        }
        #endregion RGB DXT

        
        public static void Compress8BitBlock(byte[] source, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, int channel, bool isSigned, ImageFormats.ImageEngineFormatDetails formatDetails)
        {
            // KFreon: Get min and max
            byte min = 255;
            byte max = 0;
            int channelBitSize = channel * formatDetails.ComponentSize;
            int count = sourcePosition + channelBitSize;

            byte[] sourceTexel = new byte[16];
            int sourceTexelInd = 0;
            for (int i = 1; i <= 4; i++)
            {
                for (int j= 0; j < 4; j++)
                {
                    byte colour = formatDetails.ReadByte(source, count);
                    sourceTexel[sourceTexelInd++] = colour; // Cache source
                    if (colour > max)
                        max = colour;
                    else if (colour < min)
                        min = colour;

                    count += 4 * formatDetails.ComponentSize; // skip to next entry in channel
                }
                count = sourcePosition + channelBitSize + sourceLineLength * i;
            }

            // Build Palette
            byte[] Colours = Build8BitPalette(min, max, isSigned);

            // Compress Pixels
            ulong line = 0;
            sourceTexelInd = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int ind = (i << 2) + j;
                    byte colour = sourceTexel[sourceTexelInd++];
                    int index = GetClosestValue(Colours, colour);
                    line |= (ulong)index << (ind * 3);
                }
            }

            byte[] compressed = BitConverter.GetBytes(line);
            destination[destPosition] = min;
            destination[destPosition + 1] = max;
            for (int i = 2; i < 8; i++)
                destination[destPosition + i] = compressed[i - 2];
        }

        private static int GetClosestValue(byte[] arr, byte c)
        {
            int min = arr[0] - c; 
            if (min == c)
                return 0;

            int minIndex = 0;
            for (int i = 1; i < arr.Length; i++)
            {
                int check = (arr[i] - c) & 0x7FFFFFFF;  // Knock off the sign bit

                if (check < min)
                {
                    min = check;
                    minIndex = i;
                }
            }
            return minIndex;
        }
        #endregion Block Compression

        #region Block Decompression

        internal static void Decompress8BitBlock(byte[] source, int sourceStart, byte[] destination, int decompressedStart, int decompressedLineLength, bool isSigned)
        {
            // KFreon: Read min and max colours (not necessarily in that order)
            byte min = source[sourceStart];
            byte max = source[sourceStart + 1];

            byte[] Colours = Build8BitPalette(min, max, isSigned);

            // KFreon: Decompress pixels
            ulong bitmask = (ulong)source[sourceStart + 2] << 0 | (ulong)source[sourceStart + 3] << 8 | (ulong)source[sourceStart + 4] << 16 |   // KFreon: Read all 6 compressed bytes into single.
                (ulong)source[sourceStart + 5] << 24 | (ulong)source[sourceStart + 6] << 32 | (ulong)source[sourceStart + 7] << 40;


            // KFreon: Bitshift and mask compressed data to get 3 bit indicies, and retrieve indexed colour of pixel.
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int index = i * 4 + j;
                    int destPos = decompressedStart + j * 4 + (i * decompressedLineLength);
                    destination[destPos] = Colours[bitmask >> (index * 3) & 0x7];
                }
            }
        }

        internal static void DecompressRGBBlock(byte[] source, int sourcePosition, byte[] destination, int destinationStart, int destinationLineLength, bool isDXT1, bool isPremultiplied)
        {
            ushort colour0;
            ushort colour1;
            int[] Colours = null;


            // Build colour palette
            try
            {
                // Read min max colours
                colour0 = (ushort)BitConverter.ToInt16(source, sourcePosition);
                colour1 = (ushort)BitConverter.ToInt16(source, sourcePosition + 2);
                Colours = BuildRGBPalette(colour0, colour1, isDXT1);
            }
            catch (EndOfStreamException e)
            {
                // It's due to weird shaped mips at really low resolution. Like 2x4
                Trace.WriteLine(e.ToString());
            }
            catch(ArgumentOutOfRangeException e)
            {
                Trace.WriteLine(e.ToString());
            }

            // Use palette to decompress pixel colours
            for (int i = 0; i < 4; i++)
            {
                byte bitmask = source[(sourcePosition + 4) + i];  // sourcePos + 4 is to skip the colours at the start of the texel.
                for (int j = 0; j < 4; j++)
                {
                    int destPos = destinationStart + j * 4 + (i * destinationLineLength);
                    UnpackDXTColour(Colours[bitmask >> (2 * j) & 0x03], destination, destPos, isPremultiplied);

                    if (isDXT1)
                        destination[destPos + 3] = 255;
                }
            }
        }
        #endregion

        #region Palette/Colour
        /// <summary>
        /// Reads a packed DXT colour into RGB
        /// </summary>
        /// <param name="colour">Colour to convert to RGB</param>
        /// <param name="destination">Decompressed array.</param>
        /// <param name="position">Position in destination to write RGB at.</param>
        /// <param name="isPremultiplied">True = RGB interpreted as being premultiplied with A channel.</param>
        /// <returns>RGB bytes</returns>
        private static void UnpackDXTColour(int colour, byte[] destination, int position, bool isPremultiplied)
        {
            // Read RGB 5:6:5 data, expand to 8 bit.
            destination[position + 2] = (byte)((colour & 0xF800) >> 8);  // Red
            destination[position + 1] = (byte)((colour & 0x7E0) >> 3);  // Green
            destination[position] = (byte)((colour & 0x1F) << 3);      // Blue

            if (isPremultiplied)
            {
                byte alpha = destination[position + 3];
                destination[position] *= alpha;
                destination[position + 1] *= alpha;
                destination[position + 2] *= alpha;
            }
        }

        static Vector<int> ReadDXTMask = new Vector<int>(new[] { 0xF800, 0x7E0, 0x1F, 0,0,0,0,0 });
        static Vector<int> ReadDXTDivision = new Vector<int>(new[] { 256, 8, 1, 1,1,1,1,1 });

        /// <summary>
        /// Reads a packed DXT colour into RGB
        /// </summary>
        /// <param name="colour">Colour to convert to RGB</param>
        /// <param name="blue">Blue value of colour.</param>
        /// <param name="red">Red value of colour.</param>
        /// <param name="green">Green value of colour.</param>
        private static Vector<float> ReadDXTColour(int colour)
        {
            // Read RGB 5:6:5 data
            // Expand to 8 bit data

            var temp = Vector.BitwiseAnd(new Vector<int>(colour), ReadDXTMask);
            temp /= ReadDXTDivision;
            return new Vector<float>(new float[] { temp[0], temp[1], (temp[2] << 3), 0, 0, 0, 0, 0 });
        }

        static Vector<float> BuildDXTDivider = new Vector<float>(new float[] { 8,4,8,0,0,0,0,0 });
        static Vector<float> BuildDXTMultiplier = new Vector<float>(new float[] { 2048,32,1,0,0,0,0,0 });

        /// <summary>
        /// Creates a packed DXT colour from RGB.
        /// </summary>
        /// <param name="r">Red byte.</param>
        /// <param name="g">Green byte.</param>
        /// <param name="b">Blue byte.</param>
        /// <returns>DXT Colour</returns>
        private static int BuildDXTColour(Vector<float> rgbs)
        {
            // Compress to 5:6:5
            rgbs /= BuildDXTDivider;
            rgbs *= BuildDXTMultiplier;
            return (byte)rgbs[0] | (byte)rgbs[1] | (byte)rgbs[2];
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
                Colours[2] = (byte)((6d * min + 1d * max) / 7d);  // NO idea what the +3 is...not in the Microsoft spec, but seems to be everywhere else.
                Colours[3] = (byte)((5d * min + 2d * max) / 7d);
                Colours[4] = (byte)((4d * min + 3d * max) / 7d);
                Colours[5] = (byte)((3d * min + 4d * max) / 7d);
                Colours[6] = (byte)((2d * min + 5d * max) / 7d);
                Colours[7] = (byte)((1d * min + 6d * max) / 7d);
            }
            else
            {
                // KFreon: Interpolate other colours and add Opacity or something...
                Colours[2] = (byte)((4d * min + 1d * max) / 5d);
                Colours[3] = (byte)((3d * min + 2d * max) / 5d);
                Colours[4] = (byte)((2d * min + 3d * max) / 5d);
                Colours[5] = (byte)((1d * min + 4d * max) / 5d);
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
        public static unsafe int[] BuildRGBPalette(int Colour0, int Colour1, bool isDXT1)
        {
            var Colours = new int[4];
            Colours[0] = Colour0;
            Colours[1] = Colour1;

            var colour0RGB = new Vector<float>();
            var colour1RGB = new Vector<float>();

            colour0RGB = ReadDXTColour(Colour0);
            colour1RGB = ReadDXTColour(Colour1);

            // Interpolate other 2 colours
            if (Colour0 > Colour1)
            {
                var c1 = TwoThirds * colour0RGB + OneThird * colour1RGB;
                var c2 = OneThird * colour0RGB + TwoThirds * colour1RGB;

                Colours[2] = BuildDXTColour(c1);
                Colours[3] = BuildDXTColour(c2);
            }
            else
            {
                // KFreon: Only for dxt1
                var rgbs = 0.5f * colour0RGB + 0.5f * colour1RGB;

                Colours[2] = BuildDXTColour(rgbs);
                Colours[3] = 0;
            }
            return Colours;
        }
        #endregion Palette/Colour
    }
}
