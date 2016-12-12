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
        /// <summary>
        /// Indicates if image has any alpha present.
        /// </summary>
        //public bool AlphaPresent { get; set; }

        byte[] premultipliedPixels = null;
        /// <summary>
        /// Gets pixels when premultiplied by the alpha channel.
        /// </summary>
        public byte[] PremultipliedPixels
        {
            get
            {
                if (Pixels == null)
                    return null;

                if (premultipliedPixels == null)
                {
                    premultipliedPixels = new byte[Pixels.Length];

                    for (int ai = 3; ai < Pixels.Length; ai += 4)
                    {
                        for (int rgbi = 1; rgbi < 4; rgbi++)
                            premultipliedPixels[ai - rgbi] = (byte)(Pixels[ai - rgbi] * (Pixels[ai] / 255d));

                        premultipliedPixels[ai] = 0xFF;
                    }
                }

                return premultipliedPixels;
            }
        }

        byte[] rgbAOpaque = null;
        /// <summary>
        /// Gives RGB only, but suitable to display on an RGBA image i.e. sets alpha to opaque.
        /// </summary>
        public byte[] RGBAOpaque
        {
            get
            {
                if (Pixels == null)
                    return null;

                if (rgbAOpaque == null)
                {
                    rgbAOpaque = new byte[Pixels.Length];
                    for (int i = 0; i < Pixels.Length; i++)
                    {
                        if ((i + 1) % 4 == 0)
                            rgbAOpaque[i] = 0xFF;
                        else
                            rgbAOpaque[i] = Pixels[i];
                    }
                }

                return rgbAOpaque;
            }
        }

        byte[] alphaOnlyPixels = null;
        /// <summary>
        /// Returns a grayscale image of the alpha mask.
        /// </summary>
        public byte[] AlphaOnlyPixels
        {
            get
            {
                if (Pixels == null)
                    return null;

                if (alphaOnlyPixels == null)
                {
                    alphaOnlyPixels = new byte[Pixels.Length];
                    for (int ai = 3; ai < Pixels.Length; ai += 4)
                    {
                        for (int rgbi = 1; rgbi < 4; rgbi++)
                            alphaOnlyPixels[ai - rgbi] = (Pixels[ai]);

                        alphaOnlyPixels[ai] = 0xFF;
                    }
                }

                return alphaOnlyPixels;
            }
        }

        /// <summary>
        /// Pixels in bitmap image.
        /// </summary>
        public byte[] Pixels
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
        public MipMap(byte[] pixels, int width, int height)
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
    }
}
