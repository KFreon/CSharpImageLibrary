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
        /// Size of channel components e.g. 16bit = 2.
        /// </summary>
        public int ComponentSize { get; set; } = 1;

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

                    Action<int> action = i =>
                    {
                        if ((i + 1) % (4 * ComponentSize) == 0)
                            SetAlphaOpaque(rgbAOpaque, i);
                        else
                            CopyChannelColour(Pixels, i, rgbAOpaque, i);
                    };

                    if (ImageEngine.EnableThreading)
                        Parallel.For(0, Pixels.Length / ComponentSize, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, index => action(index * ComponentSize));
                    else
                        for (int i = 0; i < Pixels.Length; i += ComponentSize)
                            action(i);
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

                    Action<int> action = ai =>
                    {
                        // Set alpha colour to RGB to make grayscale.
                        CopyChannelColour(Pixels, ai, alphaOnlyPixels, ai - 1 * ComponentSize);
                        CopyChannelColour(Pixels, ai, alphaOnlyPixels, ai - 2 * ComponentSize);
                        CopyChannelColour(Pixels, ai, alphaOnlyPixels, ai - 3 * ComponentSize);

                        SetAlphaOpaque(alphaOnlyPixels, ai); // Set Alpha to opaque just in case.
                    };

                    if (ImageEngine.EnableThreading)
                        Parallel.For(3 * ComponentSize, Pixels.Length / (4 * ComponentSize), new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, index => action(index * 4 * ComponentSize));
                    else
                        for (int ai = 3 * ComponentSize; ai < Pixels.Length; ai += 4 * ComponentSize)   // TODO: Alpha display is wrong, for some reason.
                            action(ai);
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
        public MipMap(byte[] pixels, int width, int height, int componentSize)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
            ComponentSize = componentSize;
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
            switch (ComponentSize)
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
            }
        }

        void CopyChannelColour(byte[] source, int sourceInd, byte[] destination, int destInd)
        {
            switch (ComponentSize)
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
            }
        }
    }
}
