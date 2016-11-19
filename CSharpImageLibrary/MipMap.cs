using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;
using Microsoft.IO;
using System.IO;
using System.Windows.Media.Imaging;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Represents a mipmap of an image.
    /// </summary>
    public class MipMap
    {
        /// <summary>
        /// Indicates whether mipmap has an alpha channel.
        /// </summary>
        public bool AlphaPresent { get; set; }

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
        public MipMap(byte[] pixels, int width, int height, bool alphaPresent)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
            AlphaPresent = alphaPresent;
        }


        /// <summary>
        /// Creates a WPF image from this mipmap.
        /// </summary>
        /// <returns>WriteableBitmap of image.</returns>
        public WriteableBitmap ToImage()
        {
            return UsefulThings.WPF.Images.CreateWriteableBitmap(Pixels, Width, Height);
        }
    }
}
