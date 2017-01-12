using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;
using Microsoft.IO;
using System.IO;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Represents a mipmap of an image.
    /// </summary>
    public class MipMap
    {
        //float[] rgbAOpaque = null;
        /// <summary>
        /// Gives RGB only, but suitable to display on an RGBA image i.e. sets alpha to opaque.
        /// </summary>
        /*public float[] RGBAOpaque
        {
            get
            {
                if (Pixels == null)
                    return null;

                if (rgbAOpaque == null)
                {
                    rgbAOpaque = new float[Pixels.Length];

                    for (int i = 0; i < Pixels.Length; i+=4)
                    {
                        // Should be power 2.2, but too slow, so just 2.

                        rgbAOpaque[i] = Pixels[i] * Pixels[i];
                        rgbAOpaque[i + 1] = Pixels[i + 1] * Pixels[i + 1];
                        rgbAOpaque[i + 2] = Pixels[i + 2] * Pixels[i + 2];
                        rgbAOpaque[i + 3] = 1f;
                    }
                }

                return rgbAOpaque;
            }
        }*/

        //float[] premultipliedRGBA = null;
        /// <summary>
        /// Gives RGB only, but suitable to display on an RGBA image i.e. sets alpha to opaque.
        /// </summary>
        /*public float[] PremultipliedRGBA
        {
            get
            {
                if (Pixels == null)
                    return null;

                if (premultipliedRGBA == null)
                {
                    premultipliedRGBA = new float[Pixels.Length];

                    for (int i = 0; i < Pixels.Length; i += 4)
                    {
                        // Should be power 2.2, but too slow, so just 2.

                        premultipliedRGBA[i] = Pixels[i] * Pixels[i];
                        premultipliedRGBA[i + 1] = Pixels[i + 1] * Pixels[i + 1];
                        premultipliedRGBA[i + 2] = Pixels[i + 2] * Pixels[i + 2];
                        premultipliedRGBA[i + 3] = Pixels[i + 3];
                    }
                }

                return premultipliedRGBA;
            }
        }*/

        //float[] alphaOnlyPixels = null;
        /// <summary>
        /// Returns a grayscale image of the alpha mask.
        /// </summary>
        /*public float[] AlphaOnlyPixels
        {
            get
            {
                if (Pixels == null)
                    return null;

                if (alphaOnlyPixels == null)
                {
                    alphaOnlyPixels = new float[Pixels.Length];

                    for (int ai = 3; ai < Pixels.Length; ai+=4)
                    {
                        float a = Pixels[ai] * Pixels[ai];
                        alphaOnlyPixels[ai - 1] = a;
                        alphaOnlyPixels[ai - 2] = a;
                        alphaOnlyPixels[ai - 3] = a;

                        alphaOnlyPixels[ai] = 1f;
                    }
                }

                return alphaOnlyPixels;
            }
        }*/

        /// <summary>
        /// Pixels in bitmap image.
        /// </summary>
        public float[] Pixels
        {
            get; set;
        }

        /// <summary>
        /// Mipmap width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Mipmap height.
        /// </summary>
        public int Height { get; set; }


        /// <summary>
        /// Creates a Mipmap object from a WPF image.
        /// </summary>
        public MipMap(float[] pixels, int width, int height)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
        }


        /// <summary>
        /// Creates a WPF image from this mipmap.
        /// </summary>
        /// <returns>WriteableBitmap of image.</returns>
        public BitmapSource ToImage()
        {
            var bmp = UsefulThings.WPF.Images.CreateWriteableBitmap(Pixels, Width, Height);
            bmp.Freeze();
            return bmp;
        }

        void SetAlphaOpaque(byte[] source, int index)
        {
            /*switch (ComponentSize)
            {
                case 1:
                    source[index] = 0xFF;
                    break;
                case 2:
                    source[index] = 0xFF;
                    source[index + 1] = 0xFF;
                    break;
                case 4:
                    source[index] = 0;
                    source[index + 1] = 0;
                    source[index + 2] = 128;
                    source[index + 3] = 63;
                    break;
            }*/
        }

        void CopyChannelColour(byte[] source, int sourceInd, byte[] destination, int destInd)
        {
            /*switch (ComponentSize)
            {
                case 1:
                    destination[destInd] = source[sourceInd];
                    break;
                case 2:
                    destination[destInd] = source[sourceInd];
                    destination[destInd + 1] = source[sourceInd + 1];
                    break;
                case 4:
                    destination[destInd] = source[sourceInd];
                    destination[destInd + 1] = source[sourceInd + 1];
                    destination[destInd + 2] = source[sourceInd + 2];
                    destination[destInd + 3] = source[sourceInd + 3];
                    break;
            }*/
        }
    }
}
