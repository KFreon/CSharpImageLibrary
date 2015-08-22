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
        public double Width { get; private set; }
        public double Height { get; private set; }
        public Format Format { get; private set; }

        /// <summary>
        /// Raw pixel data for image - RGBA only, no compression - NO MIPS! Single level.
        /// </summary>
        public MemoryTributary PixelData { get; private set; }

        public string FilePath { get; private set; }
        public Stream FileStream { get; private set; }

        public ImageEngineImage(string imagePath)
        {
            double width = 0;
            double height = 0;
            Format format = new Format();
            FilePath = imagePath;


            PixelData = ImageEngine.LoadImage(imagePath, out width, out height, out format);

            Width = width;
            Height = height;
            Format = format;
        }

        public ImageEngineImage(BitmapImage img, string extension)
        {
            double width = 0;
            double height = 0;
            Format format = new Format();


            PixelData = ImageEngine.LoadImage(img, out width, out height, out format, extension);

            Width = width;
            Height = height;
            Format = format;
        }

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
