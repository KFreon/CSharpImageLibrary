using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CSharpImageLibrary.DDS.DX10_Helpers;
using static UsefulDotNetThings.General.Graphics;

namespace CSharpImageLibrary.DDS
{
    internal static class DDS_BlockHelpers
    {
        const double OneThird = 1f / 3f;
        const double TwoThirds = 2f / 3f;

        #region Block Compression
        #region RGB DXT
        internal static float[] pC3 = { 1f, 1f / 2f, 0f };
        internal static float[] pD3 = { 0f, 1f / 2f, 1f };

        internal static float[] pC4 = { 1f, 2f / 3f, 1f / 3f, 0f };
        internal static float[] pD4 = { 0f, 1f / 3f, 2f / 3f, 1f };

        static uint[] psteps3 = { 0, 2, 1 };
        static uint[] psteps4 = { 0, 2, 3, 1 };

        static ScRGBAColour Luminance = new ScRGBAColour(0.2125f / 0.7154f, 1f, 0.0721f / 0.7154f, 1f);
        static ScRGBAColour LuminanceInv = new ScRGBAColour(0.7154f / 0.2125f, 1f, 0.7154f / 0.0721f, 1f);

        static ScRGBAColour Decode565(uint wColour)
        {
            ScRGBAColour colour = new ScRGBAColour()
            {
                R = ((wColour >> 11) & 0x1F) / 31f,
                G = ((wColour >> 5) & 0x3F) / 63f,
                B = ((wColour >> 0) & 0x1F) / 31f,
                A = 1f
            };
            return colour;
        }

        static uint Encode565(ScRGBAColour colour)
        {
            ScRGBAColour temp = new ScRGBAColour()
            {
                R = (colour.R < 0f) ? 0f : (colour.R > 1f) ? 1f : colour.R,
                G = (colour.G < 0f) ? 0f : (colour.G > 1f) ? 1f : colour.G,
                B = (colour.B < 0f) ? 0f : (colour.B > 1f) ? 1f : colour.B
            };
            return (uint)(temp.R * 31f + 0.5f) << 11 | (uint)(temp.G * 63f + 0.5f) << 5 | (uint)(temp.B * 31f + 0.5f);
        }


        static ScRGBAColour ReadColourFromTexel(byte[] texel, int i, bool premultiply, ImageFormats.ImageEngineFormatDetails formatDetails)
        {
            // Pull out rgb from texel
            // Create current pixel colour
            ScRGBAColour current = new ScRGBAColour();

            // Check that texel is big enough
            if (i + 3 >= texel.Length)
                return current;  // Fully transparent colour


            current.A = formatDetails.ReadFloat(texel, i + 3 * formatDetails.ComponentSize);
            current.R = formatDetails.ReadFloat(texel, i + 2 * formatDetails.ComponentSize) * (premultiply ? current.A : 1.0f);
            current.G = formatDetails.ReadFloat(texel, i + formatDetails.ComponentSize) * (premultiply ? current.A : 1.0f);
            current.B = formatDetails.ReadFloat(texel, i) * (premultiply ? current.A : 1.0f);
            
            return current;
        }

        internal static ScRGBAColour[] OptimiseRGB(ScRGBAColour[] Colour, int uSteps)
        {
            float[] pC = uSteps == 3 ? pC3 : pC4;
            float[] pD = uSteps == 3 ? pD3 : pD4;

            // Find min max
            ScRGBAColour X = Luminance;
            ScRGBAColour Y = new ScRGBAColour();

            for (int i = 0; i < Colour.Length; i++)
            {
                ScRGBAColour current = Colour[i];

                // X = min, Y = max
                if (current.R < X.R)
                    X.R = current.R;

                if (current.G < X.G)
                    X.G = current.G;

                if (current.B < X.B)
                    X.B = current.B;


                if (current.R > Y.R)
                    Y.R = current.R;

                if (current.G > Y.G)
                    Y.G = current.G;

                if (current.B > Y.B)
                    Y.B = current.B;
            }

            // Diagonal axis - starts with difference between min and max
            ScRGBAColour diag = new ScRGBAColour()
            {
                R = Y.R - X.R,
                G = Y.G - X.G,
                B = Y.B - X.B
            };
            float fDiag = diag.R * diag.R + diag.G * diag.G + diag.B * diag.B;
            if (fDiag < 1.175494351e-38F)
            {
                ScRGBAColour min1 = new ScRGBAColour()
                {
                    R = X.R,
                    G = X.G,
                    B = X.B
                };
                ScRGBAColour max1 = new ScRGBAColour()
                {
                    R = Y.R,
                    G = Y.G,
                    B = Y.B
                };
                return new ScRGBAColour[] { min1, max1 };
            }

            float FdiagInv = 1f / fDiag;

            ScRGBAColour Dir = new ScRGBAColour()
            {
                R = diag.R * FdiagInv,
                G = diag.G * FdiagInv,
                B = diag.B * FdiagInv
            };
            ScRGBAColour Mid = new ScRGBAColour()
            {
                R = (X.R + Y.R) * .5f,
                G = (X.G + Y.G) * .5f,
                B = (X.B + Y.B) * .5f
            };
            float[] fDir = new float[4];

            for (int i = 0; i < Colour.Length; i++)
            {
                ScRGBAColour pt = new ScRGBAColour()
                {
                    R = Dir.R * (Colour[i].R - Mid.R),
                    G = Dir.G * (Colour[i].G - Mid.G),
                    B = Dir.B * (Colour[i].B - Mid.B)
                };
                float f = 0;
                f = pt.R + pt.G + pt.B;
                fDir[0] += f * f;

                f = pt.R + pt.G - pt.B;
                fDir[1] += f * f;

                f = pt.R - pt.G + pt.B;
                fDir[2] += f * f;

                f = pt.R - pt.G - pt.B;
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
                float f = X.G;
                X.G = Y.G;
                Y.G = f;
            }

            if ((iDirMax & 1) != 0)
            {
                float f = X.B;
                X.B = Y.B;
                Y.B = f;
            }

            if (fDiag < 1f / 4096f)
            {
                ScRGBAColour min1 = new ScRGBAColour()
                {
                    R = X.R,
                    G = X.G,
                    B = X.B
                };
                ScRGBAColour max1 = new ScRGBAColour()
                {
                    R = Y.R,
                    G = Y.G,
                    B = Y.B
                };
                return new ScRGBAColour[] { min1, max1 };
            }

            // newtons method for local min of sum of squares error.
            float fsteps = uSteps - 1;
            for (int iteration = 0; iteration < 8; iteration++)
            {
                ScRGBAColour[] pSteps = new ScRGBAColour[4];

                for (int iStep = 0; iStep < uSteps; iStep++)
                {
                    pSteps[iStep].R = X.R * pC[iStep] + Y.R * pD[iStep];
                    pSteps[iStep].G = X.G * pC[iStep] + Y.G * pD[iStep];
                    pSteps[iStep].B = X.B * pC[iStep] + Y.B * pD[iStep];
                }


                // colour direction
                Dir.R = Y.R - X.R;
                Dir.G = Y.G - X.G;
                Dir.B = Y.B - X.B;

                float fLen = Dir.R * Dir.R + Dir.G * Dir.G + Dir.B * Dir.B;

                if (fLen < (1f / 4096f))
                    break;

                float fScale = fsteps / fLen;
                Dir.R *= fScale;
                Dir.G *= fScale;
                Dir.B *= fScale;

                // Evaluate function and derivatives
                float d2X = 0, d2Y = 0;
                ScRGBAColour dX, dY;
                dX = new ScRGBAColour();
                dY = new ScRGBAColour();

                for (int i = 0; i < Colour.Length; i++)
                {
                    ScRGBAColour current = Colour[i];

                    float fDot = (current.R - X.R) * Dir.R + (current.G - X.G) * Dir.G + (current.B - X.B) * Dir.B;

                    int iStep = 0;
                    if (fDot <= 0)
                        iStep = 0;
                    else if (fDot >= fsteps)
                        iStep = uSteps - 1;
                    else
                        iStep = (int)(fDot + .5f);

                    ScRGBAColour diff = new ScRGBAColour()
                    {
                        R = pSteps[iStep].R - current.R,
                        G = pSteps[iStep].G - current.G,
                        B = pSteps[iStep].B - current.B
                    };
                    float fC = pC[iStep] * 1f / 8f;
                    float fD = pD[iStep] * 1f / 8f;

                    d2X += fC * pC[iStep];
                    dX.R += fC * diff.R;
                    dX.G += fC * diff.G;
                    dX.B += fC * diff.B;

                    d2Y += fD * pD[iStep];
                    dY.R += fD * diff.R;
                    dY.G += fD * diff.G;
                    dY.B += fD * diff.B;
                }

                // Move endpoints
                if (d2X > 0f)
                {
                    float f = -1f / d2X;
                    X.R += dX.R * f;
                    X.G += dX.G * f;
                    X.B += dX.B * f;
                }

                if (d2Y > 0f)
                {
                    float f = -1f / d2Y;
                    Y.R += dY.R * f;
                    Y.G += dY.G * f;
                    Y.B += dY.B * f;
                }

                float fEpsilon = (0.25f / 64.0f) * (0.25f / 64.0f);
                if ((dX.R * dX.R < fEpsilon) && (dX.G * dX.G < fEpsilon) && (dX.B * dX.B < fEpsilon) &&
                    (dY.R * dY.R < fEpsilon) && (dY.G * dY.G < fEpsilon) && (dY.B * dY.B < fEpsilon))
                {
                    break;
                }
            }

            ScRGBAColour min = new ScRGBAColour()
            {
                R = X.R,
                G = X.G,
                B = X.B
            };
            ScRGBAColour max = new ScRGBAColour()
            {
                R = Y.R,
                G = Y.G,
                B = Y.B
            };
            return new ScRGBAColour[] { min, max };
        }


        internal static ScRGBAColour[] OptimiseRGB_BC67(ScRGBAColour[] Colour, int uSteps, int np, int[] pixelIndicies)
        {
            float[] pC = uSteps == 3 ? pC3 : pC4;
            float[] pD = uSteps == 3 ? pD3 : pD4;

            // Find min max
            ScRGBAColour X = new ScRGBAColour(1f, 1f, 1f, 0f);
            ScRGBAColour Y = new ScRGBAColour(0f, 0f, 0f, 0f);

            for (int i = 0; i < np; i++)
            {
                ScRGBAColour current = Colour[pixelIndicies[i]];

                // X = min, Y = max
                if (current.R < X.R)
                    X.R = current.R;

                if (current.G < X.G)
                    X.G = current.G;

                if (current.B < X.B)
                    X.B = current.B;


                if (current.R > Y.R)
                    Y.R = current.R;

                if (current.G > Y.G)
                    Y.G = current.G;

                if (current.B > Y.B)
                    Y.B = current.B;
            }


            // Diagonal axis - starts with difference between min and max
            ScRGBAColour diag = new ScRGBAColour()
            {
                R = Y.R - X.R,
                G = Y.G - X.G,
                B = Y.B - X.B
            };
            float fDiag = diag.R * diag.R + diag.G * diag.G + diag.B * diag.B;
            if (fDiag < 1.175494351e-38F)
                return new ScRGBAColour[] { X, Y };

            float FdiagInv = 1f / fDiag;

            ScRGBAColour Dir = new ScRGBAColour()
            {
                R = diag.R * FdiagInv,
                G = diag.G * FdiagInv,
                B = diag.B * FdiagInv
            };

            ScRGBAColour Mid = new ScRGBAColour()
            {
                R = (X.R + Y.R) * 0.5f,
                G = (X.G + Y.G) * 0.5f,
                B = (X.B + Y.B) * 0.5f
            };
            float[] fDir = new float[4];

            for (int i = 0; i < np; i++)
            {
                var current = Colour[pixelIndicies[i]];

                ScRGBAColour pt = new ScRGBAColour()
                {
                    R = Dir.R * (current.R - Mid.R),
                    G = Dir.G * (current.G - Mid.G),
                    B = Dir.B * (current.B - Mid.B)
                };
                float f = 0;
                f = pt.R + pt.G + pt.B;
                fDir[0] += f * f;

                f = pt.R + pt.G - pt.B;
                fDir[1] += f * f;

                f = pt.R - pt.G + pt.B;
                fDir[2] += f * f;

                f = pt.R - pt.G - pt.B;
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
                float f = X.G;
                X.G = Y.G;
                Y.G = f;
            }

            if ((iDirMax & 1) != 0)
            {
                float f = X.B;
                X.B = Y.B;
                Y.B = f;
            }

            if (fDiag < 1f / 4096f)
                return new ScRGBAColour[] { X, Y };

            // newtons method for local min of sum of squares error.
            float fsteps = uSteps - 1;
            for (int iteration = 0; iteration < 8; iteration++)
            {
                ScRGBAColour[] pSteps = new ScRGBAColour[4];

                for (int iStep = 0; iStep < uSteps; iStep++)
                {
                    pSteps[iStep].R = X.R * pC[iStep] + Y.R * pD[iStep];
                    pSteps[iStep].G = X.G * pC[iStep] + Y.G * pD[iStep];
                    pSteps[iStep].B = X.B * pC[iStep] + Y.B * pD[iStep];
                }


                // colour direction
                Dir.R = Y.R - X.R;
                Dir.G = Y.G - X.G;
                Dir.B = Y.B - X.B;

                float fLen = Dir.R * Dir.R + Dir.G * Dir.G + Dir.B * Dir.B;

                if (fLen < (1f / 4096f))
                    break;

                float fScale = fsteps / fLen;
                Dir.R *= fScale;
                Dir.G *= fScale;
                Dir.B *= fScale;

                // Evaluate function and derivatives
                float d2X = 0, d2Y = 0;
                ScRGBAColour dX, dY;
                dX = new ScRGBAColour();
                dY = new ScRGBAColour();

                for (int i = 0; i < np; i++)
                {
                    ScRGBAColour current = Colour[pixelIndicies[i]];

                    float fDot = 
                        (current.R - X.R) * Dir.R + 
                        (current.G - X.G) * Dir.G + 
                        (current.B - X.B) * Dir.B;


                    int iStep = 0;
                    if (fDot <= 0f)
                        iStep = 0;

                    if (fDot >= fsteps)
                        iStep = uSteps - 1;
                    else
                        iStep = (int)(fDot + .5f);


                    ScRGBAColour diff = new ScRGBAColour()
                    {
                        R = pSteps[iStep].R - current.R,
                        G = pSteps[iStep].G - current.G,
                        B = pSteps[iStep].B - current.B
                    };


                    float fC = pC[iStep] * (1f / 8f);
                    float fD = pD[iStep] * (1f / 8f);

                    d2X += fC * pC[iStep];
                    dX.R += fC * diff.R;
                    dX.G += fC * diff.G;
                    dX.B += fC * diff.B;

                    d2Y += fD * pD[iStep];
                    dY.R += fD * diff.R;
                    dY.G += fD * diff.G;
                    dY.B += fD * diff.B;
                }

                // Move endpoints
                if (d2X > 0f)
                {
                    float f = -1f / d2X;
                    X.R += dX.R * f;
                    X.G += dX.G * f;
                    X.B += dX.B * f;
                }

                if (d2Y > 0f)
                {
                    float f = -1f / d2Y;
                    Y.R += dY.R * f;
                    Y.G += dY.G * f;
                    Y.B += dY.B * f;
                }

                float fEpsilon = (0.25f / 64.0f) * (0.25f / 64.0f);
                if ((dX.R * dX.R < fEpsilon) && (dX.G * dX.G < fEpsilon) && (dX.B * dX.B < fEpsilon) &&
                    (dY.R * dY.R < fEpsilon) && (dY.G * dY.G < fEpsilon) && (dY.B * dY.B < fEpsilon))
                {
                    break;
                }
            }

            return new ScRGBAColour[] { X, Y };
        }


        internal static ScRGBAColour[] OptimiseRGBA_BC67(ScRGBAColour[] Colour, int uSteps, int np, int[] pixelIndicies)
        {
            float[] pC = uSteps == 3 ? pC3 : pC4;
            float[] pD = uSteps == 3 ? pD3 : pD4;

            // Find min max
            ScRGBAColour X = new ScRGBAColour(1f, 1f, 1f, 1f);
            ScRGBAColour Y = new ScRGBAColour();

            for (int i = 0; i < np; i++)
            {
                ScRGBAColour current = Colour[pixelIndicies[i]];

                // X = min, Y = max
                if (current.R < X.R)
                    X.R = current.R;

                if (current.G < X.G)
                    X.G = current.G;

                if (current.B < X.B)
                    X.B = current.B;

                if (current.A < X.A)
                    X.A = current.A;


                if (current.R > Y.R)
                    Y.R = current.R;

                if (current.G > Y.G)
                    Y.G = current.G;

                if (current.B > Y.B)
                    Y.B = current.B;

                if (current.A > Y.A)
                    Y.A = current.A;
            }

            // Diagonal axis - starts with difference between min and max
            ScRGBAColour diag = new ScRGBAColour()
            {
                R = Y.R - X.R,
                G = Y.G - X.G,
                B = Y.B - X.B,
                A = Y.A - X.A
            };
            float fDiag = diag.R * diag.R + diag.G * diag.G + diag.B * diag.B + diag.A * diag.A;
            if (fDiag < 1.175494351e-38F)
                return new ScRGBAColour[] { X, Y };

            float FdiagInv = 1f / fDiag;

            ScRGBAColour Dir = new ScRGBAColour()
            {
                R = diag.R * FdiagInv,
                G = diag.G * FdiagInv,
                B = diag.B * FdiagInv,
                A = diag.A * FdiagInv
            };
            ScRGBAColour Mid = new ScRGBAColour()
            {
                R = (X.R + Y.R) * 0.5f,
                G = (X.G + Y.G) * 0.5f,
                B = (X.B + Y.B) * 0.5f,
                A = (X.A + Y.A) * 0.5f
            };
            float[] fDir = new float[8];

            for (int i = 0; i < np; i++)
            {
                var current = Colour[pixelIndicies[i]];

                ScRGBAColour pt = new ScRGBAColour()
                {
                    R = Dir.R * (current.R - Mid.R),
                    G = Dir.G * (current.G - Mid.G),
                    B = Dir.B * (current.B - Mid.B),
                    A = Dir.A * (current.A - Mid.A)
                };
                float f = 0;
                f = pt.R + pt.G + pt.B + pt.A;   fDir[0] += f * f;
                f = pt.R + pt.G + pt.B - pt.A;   fDir[1] += f * f;
                f = pt.R + pt.G - pt.B + pt.A;   fDir[2] += f * f;
                f = pt.R + pt.G - pt.B - pt.A;   fDir[3] += f * f;
                f = pt.R - pt.G + pt.B + pt.A;   fDir[4] += f * f;
                f = pt.R - pt.G + pt.B - pt.A;   fDir[5] += f * f;
                f = pt.R - pt.G - pt.B + pt.A;   fDir[6] += f * f;
                f = pt.R - pt.G - pt.B - pt.A;   fDir[7] += f * f;
            }

            float fDirMax = fDir[0];
            int iDirMax = 0;
            for (int iDir = 1; iDir < 8; iDir++)
            {
                if (fDir[iDir] > fDirMax)
                {
                    fDirMax = fDir[iDir];
                    iDirMax = iDir;
                }
            }

            if ((iDirMax & 4) != 0)
            {
                float f = X.G;
                X.G = Y.G;
                Y.G = f;
            }

            if ((iDirMax & 2) != 0)
            {
                float f = X.B;
                X.B = Y.B;
                Y.B = f;
            }

            if ((iDirMax & 1) != 0)
            {
                float f = X.A;
                X.A = Y.A;
                Y.A = f;
            }

            if (fDiag < 1f / 4096f)
                return new ScRGBAColour[] { X, Y };


            // newtons method for local min of sum of squares error.
            float fsteps = uSteps - 1;
            float err = float.MaxValue;
            for (int iteration = 0; iteration < 8 && err > 0f; iteration++)
            {
                ScRGBAColour[] pSteps = new ScRGBAColour[BC7_MAX_INDICIES];

                for (int iStep = 0; iStep < uSteps; iStep++)
                {
                    pSteps[iStep].R = X.R * pC[iStep] + Y.R * pD[iStep];
                    pSteps[iStep].G = X.G * pC[iStep] + Y.G * pD[iStep];
                    pSteps[iStep].B = X.B * pC[iStep] + Y.B * pD[iStep];
                    pSteps[iStep].A = X.A * pC[iStep] + Y.A * pD[iStep];
                }


                // colour direction
                Dir.R = Y.R - X.R;
                Dir.G = Y.G - X.G;
                Dir.B = Y.B - X.B;
                Dir.A = Y.A - X.A;

                float fLen = Dir.R * Dir.R + Dir.G * Dir.G + Dir.B * Dir.B + Dir.A * Dir.A;

                if (fLen < (1f / 4096f))
                    break;

                float fScale = fsteps / fLen;
                Dir.R *= fScale;
                Dir.G *= fScale;
                Dir.B *= fScale;
                Dir.A *= fScale;

                // Evaluate function and derivatives
                float d2X = 0, d2Y = 0;
                ScRGBAColour dX, dY;
                dX = new ScRGBAColour();
                dY = new ScRGBAColour();

                for (int i = 0; i < np; i++)
                {
                    ScRGBAColour current = Colour[pixelIndicies[i]];

                    float fDot =
                        (current.R - X.R) * Dir.R +
                        (current.G - X.G) * Dir.G +
                        (current.B - X.B) * Dir.B +
                        (current.A - X.A) * Dir.A;

                    int iStep = 0;
                    if (fDot <= 0f)
                        iStep = 0;
                    else if (fDot >= fsteps)
                        iStep = uSteps - 1;
                    else
                        iStep = (int)(fDot + .5f);

                    ScRGBAColour diff = new ScRGBAColour()
                    {
                        R = pSteps[iStep].R - current.R,
                        G = pSteps[iStep].G - current.G,
                        B = pSteps[iStep].B - current.B,
                        A = pSteps[iStep].A - current.A
                    };
                    float fC = pC[iStep] * 1f / 8f;
                    float fD = pD[iStep] * 1f / 8f;

                    d2X += fC * pC[iStep];
                    dX.R += fC * diff.R;
                    dX.G += fC * diff.G;
                    dX.B += fC * diff.B;
                    dX.A += fC * diff.A;

                    d2Y += fD * pD[iStep];
                    dY.R += fD * diff.R;
                    dY.G += fD * diff.G;
                    dY.B += fD * diff.B;
                    dY.A += fD * diff.A;
                }

                // Move endpoints
                if (d2X > 0f)
                {
                    float f = -1f / d2X;
                    X.R += dX.R * f;
                    X.G += dX.G * f;
                    X.B += dX.B * f;
                    X.A += dX.A * f;
                }

                if (d2Y > 0f)
                {
                    float f = -1f / d2Y;
                    Y.R += dY.R * f;
                    Y.G += dY.G * f;
                    Y.B += dY.B * f;
                    Y.A += dY.A * f;
                }

                float fEpsilon = (0.25f / 64.0f) * (0.25f / 64.0f);
                if ((dX.R * dX.R < fEpsilon) && (dX.G * dX.G < fEpsilon) && (dX.B * dX.B < fEpsilon) && (dX.A * dX.A < fEpsilon) &&
                    (dY.R * dY.R < fEpsilon) && (dY.G * dY.G < fEpsilon) && (dY.B * dY.B < fEpsilon) && (dY.A * dY.A < fEpsilon))
                {
                    break;
                }
            }

            return new ScRGBAColour[] { X, Y };
        }



        static int CheckDXT1TexelFullTransparency(ScRGBAColour[] texel, byte[] destination, int destPosition, double alphaRef)
        {
            int uColourKey = 0;

            // Alpha stuff
            for (int i = 0; i < 16; i++)
            {
                if (texel[i].A < alphaRef)
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
        static void DoColourFixErrorCorrection(ScRGBAColour[] Colour, ScRGBAColour[] texel)
        {
            ScRGBAColour[] Error = new ScRGBAColour[16];
            for (int i = 0; i < 16; i++)
            {
                ScRGBAColour current = new ScRGBAColour(texel[i].R, texel[i].G, texel[i].B, texel[i].A);

                if (true)  // Dither
                {
                    // Adjust for accumulated error
                    // This works by figuring out the error between the current pixel colour and the adjusted colour? Dunno what the adjustment is. Looks like a 5:6:5 range adaptation
                    // Then, this error is distributed across the "next" few pixels and not the previous.
                    current.R += Error[i].R;
                    current.G += Error[i].G;
                    current.B += Error[i].B;
                }


                // 5:6:5 range adaptation?
                Colour[i].R = (int)(current.R * 31f + .5f) * (1f / 31f);
                Colour[i].G = (int)(current.G * 63f + .5f) * (1f / 63f);
                Colour[i].B = (int)(current.B * 31f + .5f) * (1f / 31f);

                DoSomeDithering(current, i, Colour, i, Error);

                Colour[i].R *= Luminance.R;
                Colour[i].G *= Luminance.G;
                Colour[i].B *= Luminance.B;
            }
        }

        static ScRGBAColour[] DoSomethingWithPalette(int uSteps, uint wColourA, uint wColourB, ScRGBAColour ColourA, ScRGBAColour ColourB)
        {
            // Create palette colours
            ScRGBAColour[] step = new ScRGBAColour[4];

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
            {
                step[2].R = step[0].R + (1f / 2f) * (step[1].R - step[0].R);
                step[2].G = step[0].G + (1f / 2f) * (step[1].G - step[0].G);
                step[2].B = step[0].B + (1f / 2f) * (step[1].B - step[0].B);
            }
            else
            {
                // "step" appears to be the palette as this is the interpolation
                step[2].R = step[0].R + (1f / 3f) * (step[1].R - step[0].R);
                step[2].G = step[0].G + (1f / 3f) * (step[1].G - step[0].G);
                step[2].B = step[0].B + (1f / 3f) * (step[1].B - step[0].B);

                step[3].R = step[0].R + (2f / 3f) * (step[1].R - step[0].R);
                step[3].G = step[0].G + (2f / 3f) * (step[1].G - step[0].G);
                step[3].B = step[0].B + (2f / 3f) * (step[1].B - step[0].B);
            }

            return step;
        }


        static uint DoOtherColourFixErrorCorrection(ScRGBAColour[] texel, int uSteps, double alphaRef, ScRGBAColour[] step, ScRGBAColour Dir)
        {
            uint dw = 0;
            ScRGBAColour[] Error = new ScRGBAColour[16];

            uint[] psteps = uSteps == 3 ? psteps3 : psteps4;
            for (int i = 0; i < 16; i++)
            {
                ScRGBAColour current = new ScRGBAColour(texel[i].R, texel[i].G, texel[i].B, texel[i].A);

                if ((uSteps == 3) && (current.A < alphaRef))
                {
                    dw = (uint)((3 << 30) | (dw >> 2));
                    continue;
                }

                current.R *= Luminance.R;
                current.G *= Luminance.G;
                current.B *= Luminance.B;

                if (true) // dither
                {
                    // Error again
                    current.R += Error[i].R;
                    current.G += Error[i].G;
                    current.B += Error[i].B;
                }

                float fdot = (current.R - step[0].R) * Dir.R + (current.G - step[0].G) * Dir.G + (current.B - step[0].B) * Dir.B;

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

        static void DoSomeDithering(ScRGBAColour current, int index, ScRGBAColour[] InnerColour, int InnerIndex, ScRGBAColour[] Error)
        {
            if (true)  // Dither
            {
                // Calculate difference between current pixel colour and adapted pixel colour?
                var inner = InnerColour[InnerIndex];
                ScRGBAColour diff = new ScRGBAColour()
                {
                    R = current.A * (byte)(current.R - inner.R),
                    G = current.A * (byte)(current.G - inner.G),
                    B = current.A * (byte)(current.B - inner.B)
                };

                // If current pixel is not at the end of a row
                if ((index & 3) != 3)
                {
                    Error[index + 1].R += diff.R * (7f / 16f);
                    Error[index + 1].G += diff.G * (7f / 16f);
                    Error[index + 1].B += diff.B * (7f / 16f);
                }

                // If current pixel is not in bottom row
                if (index < 12)
                {
                    // If current pixel IS at end of row
                    if ((index & 3) != 0)
                    {
                        Error[index + 3].R += diff.R * (3f / 16f);
                        Error[index + 3].G += diff.G * (3f / 16f);
                        Error[index + 3].B += diff.B * (3f / 16f);
                    }

                    Error[index + 4].R += diff.R * (5f / 16f);
                    Error[index + 4].G += diff.G * (5f / 16f);
                    Error[index + 4].B += diff.B * (5f / 16f);

                    // If current pixel is not at end of row
                    if ((index & 3) != 3)
                    {
                        Error[index + 5].R += diff.R * (1f / 16f);
                        Error[index + 5].G += diff.G * (1f / 16f);
                        Error[index + 5].B += diff.B * (1f / 16f);
                    }
                }
            }
        }

        internal static void CompressRGBTexel(byte[] imgData, int sourcePosition, int sourceLineLength, byte[] destination, int destPosition, bool isDXT1, double alphaRef, AlphaSettings alphaSetting, ImageFormats.ImageEngineFormatDetails formatDetails)
        {
            int uSteps = 4;

            bool premultiply = alphaSetting == AlphaSettings.Premultiply;

            // Read texel
            ScRGBAColour[] sourceTexel = new ScRGBAColour[16];
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


            // TODO replace ScRGBAColour with a SIMD vector for speed. Test difference between vector4 and vector<T>, might not be better.

            // Determine if texel is fully and entirely transparent. If so, can set to white and continue.
            if (isDXT1)
            {
                uSteps = CheckDXT1TexelFullTransparency(sourceTexel, destination, destPosition, alphaRef);
                if (uSteps == -1)
                    return;
            }

            ScRGBAColour[] Colour = new ScRGBAColour[16];

            // Some kind of colour adjustment. Not sure what it does, especially if it wasn't dithering...
            DoColourFixErrorCorrection(Colour, sourceTexel);
            

            // Palette colours
            ScRGBAColour ColourA, ColourB, ColourC, ColourD;
            ColourA = new ScRGBAColour();
            ColourB = new ScRGBAColour();
            ColourC = new ScRGBAColour();
            ColourD = new ScRGBAColour();

            // OPTIMISER
            ScRGBAColour[] minmax = OptimiseRGB(Colour, uSteps);
            ColourA = minmax[0];
            ColourB = minmax[1];

            // Create interstitial colours?
            ColourC.R = ColourA.R * LuminanceInv.R;
            ColourC.G = ColourA.G * LuminanceInv.G;
            ColourC.B = ColourA.B * LuminanceInv.B;

            ColourD.R = ColourB.R * LuminanceInv.R;
            ColourD.G = ColourB.G * LuminanceInv.G;
            ColourD.B = ColourB.B * LuminanceInv.B;


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

            ColourA.R = ColourC.R * Luminance.R;
            ColourA.G = ColourC.G * Luminance.G;
            ColourA.B = ColourC.B * Luminance.B;

            ColourB.R = ColourD.R * Luminance.R;
            ColourB.G = ColourD.G * Luminance.G;
            ColourB.B = ColourD.B * Luminance.B;


            var step = DoSomethingWithPalette(uSteps, wColourA, wColourB, ColourA, ColourB);

            // Calculating colour direction apparently
            ScRGBAColour Dir = new ScRGBAColour()
            {
                R = step[1].R - step[0].R,
                G = step[1].G - step[0].G,
                B = step[1].B - step[0].B
            };
            float fscale = (wColourA != wColourB) ? ((uSteps - 1) / (Dir.R * Dir.R + Dir.G * Dir.G + Dir.B * Dir.B)) : 0.0f;
            Dir.R *= fscale;
            Dir.G *= fscale;
            Dir.B *= fscale;

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

        /// <summary>
        /// Reads a packed DXT colour into RGB
        /// </summary>
        /// <param name="colour">Colour to convert to RGB</param>
        /// <param name="blue">Blue value of colour.</param>
        /// <param name="red">Red value of colour.</param>
        /// <param name="green">Green value of colour.</param>
        private static void ReadDXTColour(int colour, ref byte red, ref byte blue, ref byte green)
        {
            // Read RGB 5:6:5 data
            // Expand to 8 bit data
            red = (byte)((colour & 0xF800) >> 8);
            blue = (byte)((colour & 0x7E0) >> 3);
            green = (byte)((colour & 0x1F) << 3);
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
        public static int[] BuildRGBPalette(int Colour0, int Colour1, bool isDXT1)
        {
            int[] Colours = new int[4];

            Colours[0] = Colour0;
            Colours[1] = Colour1;

            byte Colour0_R = 0;
            byte Colour0_G = 0;
            byte Colour0_B = 0;

            byte Colour1_R = 0;
            byte Colour1_G = 0;
            byte Colour1_B = 0;

            ReadDXTColour(Colour0, ref Colour0_R, ref Colour0_G, ref Colour0_B);
            ReadDXTColour(Colour1, ref Colour1_R, ref Colour1_G, ref Colour1_B);



            // Interpolate other 2 colours
            if (Colour0 > Colour1)
            {
                var r1 = (byte)(TwoThirds * Colour0_R + OneThird * Colour1_R);
                var g1 = (byte)(TwoThirds * Colour0_G + OneThird * Colour1_G);
                var b1 = (byte)(TwoThirds * Colour0_B + OneThird * Colour1_B);

                var r2 = (byte)(OneThird * Colour0_R + TwoThirds * Colour1_R);
                var g2 = (byte)(OneThird * Colour0_G + TwoThirds * Colour1_G);
                var b2 = (byte)(OneThird * Colour0_B + TwoThirds * Colour1_B);

                Colours[2] = BuildDXTColour(r1, g1, b1);
                Colours[3] = BuildDXTColour(r2, g2, b2);
            }
            else
            {
                // KFreon: Only for dxt1
                var r = (byte)(0.5 * Colour0_R + 0.5 * Colour1_R);
                var g = (byte)(0.5 * Colour0_G + 0.5 * Colour1_G);
                var b = (byte)(0.5 * Colour0_B + 0.5 * Colour1_B);

                Colours[2] = BuildDXTColour(r, g, b);
                Colours[3] = 0;
            }
            return Colours;
        }
        #endregion Palette/Colour
    }
}
