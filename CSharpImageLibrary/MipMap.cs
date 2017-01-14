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
    }
}
