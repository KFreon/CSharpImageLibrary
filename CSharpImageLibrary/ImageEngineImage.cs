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

        public ImageEngineImage(BitmapImage img)
        {
            double width = 0;
            double height = 0;
            Format format = new Format();


            PixelData = ImageEngine.LoadImage(img, out width, out height, out format);

            Width = width;
            Height = height;
            Format = format;
        }

        public BitmapImage GeneratePreview()
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            BitmapFrame frame = BitmapFrame.Create(BitmapFrame.Create((int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32, BitmapPalettes.Halftone256, PixelData.ToArray(), 4 * (int)Width));
            encoder.Frames.Add(frame);

            MemoryTributary stream = new MemoryTributary();
            encoder.Save(stream);

            return UsefulThings.WPF.Images.CreateWPFBitmap(stream);
        }
    }
}
