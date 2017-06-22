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
        /// Pixels in bitmap image in which the components were byte sized.
        /// </summary>
        byte[] BytePixels
        {
            get; set;
        }

        /// <summary>
        /// Pixels in bitmap image in which the components were floats.
        /// </summary>
        float[] FloatPixels { get; set; }

        /// <summary>
        /// Pixels in bitmap image in which the components were int64 sized.
        /// </summary>
        long[] LongPixels { get; set; }

        /// <summary>
        /// Size of mipmap in memory.
        /// </summary>
        public int UncompressedSize { get; private set; }

        /// <summary>
        /// Details of the format that this mipmap was created from.
        /// </summary>
        public ImageFormats.ImageEngineFormatDetails LoadedFormatDetails { get; private set; }

        /// <summary>
        /// Mipmap width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Mipmap height.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Indicates if there is any alpha in image.
        /// </summary>
        public bool IsAlphaPresent
        {
            get
            {
                if (Pixels?.Length != 0)
                {
                    for (int i = 3; i < Pixels.Length; i += 4)   // TODO: ComponentSize
                    {
                        if (Pixels[i] != 0)
                            return true;
                    }
                }

                return false;
            }
        }


        /// <summary>
        /// Creates a Mipmap object from a WPF image.
        /// </summary>
        public MipMap(byte[] pixels, int width, int height, ImageFormats.ImageEngineFormatDetails details)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
            LoadedFormatDetails = details;

            UncompressedSize = ImageFormats.GetUncompressedSize(width, height, details.MaxNumberOfChannels, false);
        }


        /// <summary>
        /// Creates a WPF image from this mipmap.
        /// </summary>
        /// <returns>WriteableBitmap of image.</returns>
        public BitmapSource ToImage()
        {
            var tempPixels = ImageEngine.GetPixelsAsBGRA32(Width, Height, Pixels, LoadedFormatDetails);
            var bmp = UsefulThings.WPF.Images.CreateWriteableBitmap(tempPixels, Width, Height);
            bmp.Freeze();
            return bmp;
        }

        /// <summary>
        /// Gets suitable pixel array for the data loaded.
        /// Might be bytes, might be longs, etc.
        /// </summary>
        /// <returns>Array of the requested type.</returns>
        public T[] GetPixels<T>()
        {
            if (BytePixels != null)
                return (T[])(Array)BytePixels;
            else if (FloatPixels != null)
                return (T[])(Array)FloatPixels;
            else if (LongPixels != null)
                return (T[])(Array)LongPixels;
            else
                throw new InvalidDataException("No pixel data set. Ensure mipmap has been correctly loaded.");
        }

        public float[] GetPixelsAsFloat()
        {
            return GetPixels()
        }
    }
}
