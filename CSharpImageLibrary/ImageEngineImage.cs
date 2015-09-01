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
    /// <summary>
    /// Represents an image. Can use Windows codecs if available.
    /// </summary>
    public class ImageEngineImage
    {
        #region Properties
        /// <summary>
        /// Width of image.
        /// </summary>
        public int Width
        {
            get
            {
                return MipMaps[0].Width;
            }
        }

        /// <summary>
        /// Height of image.
        /// </summary>
        public int Height
        {
            get
            {
                return MipMaps[0].Height;
            }
        }

        public int NumMipMaps
        {
            get
            {
                return MipMaps.Count;
            }
        }

        /// <summary>
        /// Format of image and whether it's mippable.
        /// </summary>
        public Format Format { get; private set; }

        
        public List<MipMap> MipMaps { get; private set; }

        /// <summary>
        /// Path to file. Null if no file e.g. thumbnail from memory.
        /// </summary>
        public string FilePath { get; private set; }
        #endregion Properties

        /// <summary>
        /// Creates a new ImageEngineImage from file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        public ImageEngineImage(string imagePath)
        {
            Format format = new Format();
            FilePath = imagePath;

            // KFreon: Load image and save useful information including BGRA pixel data - may be processed from original into this form.
            MipMaps = ImageEngine.LoadImage(imagePath, out format);


            // KFreon: Can't pass properties as out :(
            Format = format;
        }


        /// <summary>
        /// Creates new ImageEngineImage from stream.
        /// </summary>
        /// <param name="stream">Image to load.</param>
        /// <param name="extension">Extension of original file.</param>
        public ImageEngineImage(Stream stream, string extension)
        {
            Format format = new Format();

            // KFreon: Load image and save useful information including BGRA pixel data - may be processed from original into this form.
            MipMaps = ImageEngine.LoadImage(stream, out format, extension);

            Format = format;
        }

        public ImageEngineImage(string imagePath, int desiredMaxDimension)
        {
            Format format = new Format();
            FilePath = imagePath;

            // KFreon: Load image and save useful information including BGRA pixel data - may be processed from original into this form.
            MipMaps = ImageEngine.LoadImage(imagePath, out format, desiredMaxDimension);


            // KFreon: Can't pass properties as out :(
            Format = format;
        }


        public ImageEngineImage(Stream stream, string extension, int desiredMaxDimension)
        {
            Format format = new Format();

            // KFreon: Load image and save useful information including BGRA pixel data - may be processed from original into this form.
            MipMaps = ImageEngine.LoadImage(stream, out format, extension, desiredMaxDimension);

            Format = format;
        }


        /// <summary>
        /// Saves image in specified format to file. If file exists, it will be overwritten.
        /// </summary>
        /// <param name="destination">File to save to.</param>
        /// <param name="format">Format to save as.</param>
        /// <param name="GenerateMips">Tr</param>
        /// <returns>True = Generates all mipmaps. False = Uses largest available Mipmap.</returns>
        public bool Save(string destination, ImageEngineFormat format, bool GenerateMips)
        {
            using (FileStream fs = new FileStream(destination, FileMode.Create))
                return Save(fs, format, GenerateMips);
        }


        /// <summary>
        /// Saves fully formatted image in specified format to stream.
        /// </summary>
        /// <param name="destination">Stream to save to.</param>
        /// <param name="format">Format to save as.</param>
        /// <param name="GenerateMips">True = Generates all mipmaps. False = Uses largest available Mipmap.</param>
        /// <returns>True if success</returns>
        public bool Save(Stream destination, ImageEngineFormat format, bool GenerateMips)
        {
            return ImageEngine.Save(MipMaps, format, destination, GenerateMips);
        }

        /// <summary>
        /// TEMPORARY. Gets a preview.
        /// </summary>
        /// <returns>BitmapImage of image.</returns>
        public BitmapImage GeneratePreview()
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            // KFreon: NOTE: Seems to ignore alpha - pretty much ultra useful since premultiplying alpha often removes most of the image
            byte[] data = MipMaps[0].Data.ToArray();

            int stride = 4 * (int)Width;
            BitmapPalette palette = BitmapPalettes.Halftone256;
            PixelFormat pixelformat = PixelFormats.Bgra32;           

            // KFreon: Create a bitmap from raw pixel data
            BitmapSource source = BitmapFrame.Create((int)Width, (int)Height, 96, 96, pixelformat, palette, data, stride);

            BitmapFrame frame = BitmapFrame.Create(source);
            encoder.Frames.Add(frame);

            MemoryTributary stream = new MemoryTributary(data.Length);
            encoder.Save(stream);

            return UsefulThings.WPF.Images.CreateWPFBitmap(stream);
        }
    }
}
