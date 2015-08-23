using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace CSharpImageLibrary
{
    public class ImageEngineImage
    {
        /// <summary>
        /// Width of image.
        /// </summary>
        public double Width { get; private set; }

        /// <summary>
        /// Height of image.
        /// </summary>
        public double Height { get; private set; }

        /// <summary>
        /// Format of image and whether it's mippable.
        /// </summary>
        public Format Format { get; private set; }

        /// <summary>
        /// Raw pixel data for image - RGBA only, no compression - NO MIPS! Single level.
        /// </summary>
        public MemoryTributary PixelData { get; private set; }

        /// <summary>
        /// Path to file. Null if no file e.g. thumbnail from memory.
        /// </summary>
        public string FilePath { get; private set; }


        /// <summary>
        /// Creates a new ImageEngineImage from file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        public ImageEngineImage(string imagePath)
        {
            double width = 0;
            double height = 0;
            Format format = new Format();
            FilePath = imagePath;

            // KFreon: Load image and save useful information including RGBA pixel data - may be processed from original into this form.
            PixelData = ImageEngine.LoadImage(imagePath, out width, out height, out format);


            // KFreon: Can't pass properties as out :(
            Width = width;
            Height = height;
            Format = format;
        }


        /// <summary>
        /// Creates new ImageEngineImage from WIC BitmapImage.
        /// </summary>
        /// <param name="img">Image to load.</param>
        /// <param name="extension">Extension of original file.</param>
        public ImageEngineImage(BitmapImage img, string extension)
        {
            double width = 0;
            double height = 0;
            Format format = new Format();

            // KFreon: Load image and save useful information including RGBA pixel data - may be processed from original into this form.
            PixelData = ImageEngine.LoadImage(img, out width, out height, out format, extension);

            Width = width;
            Height = height;
            Format = format;
        }


        /// <summary>
        /// TEMPORARY. Gets a preview.
        /// </summary>
        /// <returns>BitmapImage of image.</returns>
        public BitmapImage GeneratePreview()
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            // KFreon: NOTE: Seems to ignore alpha - pretty much ultra useful since premultiplying alpha often removes most of the image
            byte[] data = PixelData.ToArray();

            int stride = 4 * (int)Width;
            BitmapPalette palette = BitmapPalettes.Halftone256;
            PixelFormat pixelformat = PixelFormats.Bgra32;

            // KFreon: V8U8 needs some different settings
            if (Format.InternalFormat == ImageEngineFormat.DDS_V8U8)
            {
                stride = ((int)Width * 32 + 7) / 8;
                palette = BitmapPalettes.Halftone125;
                pixelformat = PixelFormats.Bgr32;
            }

            // KFreon: Create a bitmap from raw pixel data
            BitmapSource source = BitmapFrame.Create((int)Width, (int)Height, 96, 96, pixelformat, palette, data, stride);

            BitmapFrame frame = BitmapFrame.Create(source);
            encoder.Frames.Add(frame);

            MemoryTributary stream = new MemoryTributary();
            encoder.Save(stream);

            return UsefulThings.WPF.Images.CreateWPFBitmap(stream);
        }
    }
}
