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
    public class ImageEngineImage : IDisposable
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

        /// <summary>
        /// Number of mipmaps present.
        /// </summary>
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

        
        /// <summary>
        /// List of mipmaps. Single level images only have one mipmap.
        /// </summary>
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
            LoadFromFile(imagePath);
        }


        /// <summary>
        /// Creates new ImageEngineImage from stream.
        /// Does NOT require that stream remains alive.
        /// </summary>
        /// <param name="stream">Image to load.</param>
        /// <param name="extension">Extension of original file.</param>
        public ImageEngineImage(Stream stream, string extension = null)
        {
            LoadFromStream(stream, extension);
        }


        /// <summary>
        /// Loads an image from a file and scales (aspect safe) to a maximum size.
        /// e.g. 1024x512, desiredMaxDimension = 128 ===> Image is scaled to 128x64.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="desiredMaxDimension">Max dimension to save.</param>
        public ImageEngineImage(string imagePath, int desiredMaxDimension)
        {
            LoadFromFile(imagePath, desiredMaxDimension);
        }

        /// <summary>
        /// Loads an image from a stream and scales (aspect safe) to a maximum size. Does NOT require that stream remains alive.
        /// e.g. 1024x512, desiredMaxDimension = 128 ===> Image is scaled to 128x64.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="extension">File extension of original image.</param>
        /// <param name="desiredMaxDimension">Maximum dimension.</param>
        public ImageEngineImage(Stream stream, string extension, int desiredMaxDimension)
        {
            LoadFromStream(stream, extension, desiredMaxDimension);
        }


        /// <summary>
        /// Loads an image from a byte array.
        /// </summary>
        /// <param name="imageFileData">Fully formatted image file data.</param>
        public ImageEngineImage(byte[] imageFileData)
        {
            using (MemoryStream ms = RecyclableMemoryManager.GetStream(imageFileData))
                LoadFromStream(ms);
        }


        /// <summary>
        /// Loads an image from a byte array and scales (aspect safe) to a maximum size.
        /// e.g. 1024x512, desiredMaxDimension = 128 ===> Image is scaled to 128x64.
        /// </summary>
        /// <param name="imageFileData">Full image file data.</param>
        /// <param name="desiredMaxDimension">Maximum dimension.</param>
        public ImageEngineImage(byte[] imageFileData, int desiredMaxDimension)
        {
            using (MemoryStream ms = RecyclableMemoryManager.GetStream(imageFileData))
                LoadFromStream(ms, desiredMaxDimension: desiredMaxDimension);
        }


        private void LoadFromFile(string imagePath, int desiredMaxDimension = 0)
        {
            Format format = new Format();
            FilePath = imagePath;

            // KFreon: Load image and save useful information including BGRA pixel data - may be processed from original into this form.
            MipMaps = ImageEngine.LoadImage(imagePath, out format, desiredMaxDimension);


            // KFreon: Can't pass properties as out :(
            Format = format;
        }

        
        private void LoadFromStream(Stream stream, string extension = null, int desiredMaxDimension = 0)
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
        /// <param name="format">Desired image format.</param>
        /// <param name="GenerateMips">True = Generates all mipmaps. False = Uses largest available Mipmap.</param>
        /// <param name="desiredMaxDimension">Maximum value of either image dimension.</param>
        /// <returns>True if success.</returns>
        public bool Save(string destination, ImageEngineFormat format, bool GenerateMips, int desiredMaxDimension = 0)
        {
            using (FileStream fs = new FileStream(destination, FileMode.Create))
                return Save(fs, format, GenerateMips, desiredMaxDimension);
        }


        /// <summary>
        /// Saves fully formatted image in specified format to stream.
        /// </summary>
        /// <param name="destination">Stream to save to.</param>
        /// <param name="format">Format to save as.</param>
        /// <param name="GenerateMips">True = Generates all mipmaps. False = Uses largest available Mipmap.</param>
        /// <param name="desiredMaxDimension">Maximum value of either image dimension.</param>
        /// <returns>True if success</returns>
        public bool Save(Stream destination, ImageEngineFormat format, bool GenerateMips, int desiredMaxDimension = 0)
        {
            return ImageEngine.Save(MipMaps, format, destination, GenerateMips, desiredMaxDimension);
        }

        /// <summary>
        /// TEMPORARY. Gets a preview.
        /// </summary>
        /// <returns>BitmapImage of image.</returns>
        public BitmapSource GeneratePreview(int index)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 100;

            // KFreon: NOTE: Seems to ignore alpha - pretty much ultra useful since premultiplying alpha often removes most of the image
            MipMap mip = MipMaps[index];
            byte[] data = mip.Data.ToArray();

            int stride = 4 * (int)mip.Width;
            BitmapPalette palette = BitmapPalettes.Halftone256;
            PixelFormat pixelformat = PixelFormats.Bgra32;           

            // KFreon: Create a bitmap from raw pixel data
            BitmapSource source = BitmapFrame.Create((int)mip.Width, (int)mip.Height, 96, 96, pixelformat, palette, data, stride);
            return source;
        }


        /// <summary>
        /// Releases resources used by mipmap MemoryStreams.
        /// </summary>
        public void Dispose()
        {
            if (MipMaps == null)
                return;

            foreach (MipMap mipmap in MipMaps)
                mipmap.Data.Dispose();
        }


        /// <summary>
        /// Creates a GDI+ bitmap from largest mipmap.
        /// Does NOT require that image remains alive.
        /// </summary>
        /// <returns>GDI+ bitmap of largest mipmap.</returns>
        public System.Drawing.Bitmap GetGDIBitmap(int maxDimension = 0)
        {
            MipMap mip = MipMaps[0];

            if (maxDimension != 0)
            {
                // Choose a mip of the correct size, if available.
                var sizedMip = MipMaps.Where(m => (m.Height == maxDimension && m.Width <= maxDimension) || (m.Width == maxDimension && m.Height <= maxDimension));
                if (sizedMip.Any())
                    mip = sizedMip.First();
                else
                {
                    double scale = maxDimension * 1f / (Height > Width ? Height : Width);
                    mip = ImageEngine.Resize(mip, scale);
                }
            }
            


            return UsefulThings.WinForms.Imaging.CreateBitmap(mip.Data.ToArray(), mip.Width, mip.Height);
        }


        /// <summary>
        /// Creates a WPF Bitmap from largest mipmap.
        /// Does NOT require that image remains alive.
        /// </summary>
        /// <returns>WPF bitmap of largest mipmap.</returns>
        public BitmapSource GetWPFBitmap(int maxDimension = 0)
        {
            int stride = 4 * Width;

            MipMap mip = MipMaps[0];

            if (maxDimension != 0)
            {
                // Choose a mip of the correct size, if available.
                var sizedMip = MipMaps.Where(m => (m.Height == maxDimension && m.Width <= maxDimension) || (m.Width == maxDimension && m.Height <= maxDimension));
                if (sizedMip.Any())
                    mip = sizedMip.First();
                else
                {
                    double scale = maxDimension * 1f / (Height > Width ? Height : Width);
                    mip = ImageEngine.Resize(mip, scale);
                }
            }
            

            BitmapFrame frame = BitmapFrame.Create(BitmapFrame.Create(mip.Width, mip.Height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent, mip.Data.ToArray(), stride));
            return frame;
        }
    }
}
