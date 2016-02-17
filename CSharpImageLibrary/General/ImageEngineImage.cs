using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsefulThings;
using static CSharpImageLibrary.General.ImageEngine;

namespace CSharpImageLibrary.General
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
        public ImageEngineImage(string imagePath, int desiredMaxDimension, bool enforceResize)
        {
            LoadFromFile(imagePath, desiredMaxDimension, enforceResize);
        }

        /// <summary>
        /// Loads an image from a stream and scales (aspect safe) to a maximum size. Does NOT require that stream remains alive.
        /// e.g. 1024x512, desiredMaxDimension = 128 ===> Image is scaled to 128x64.
        /// </summary>
        /// <param name="stream">Full image stream.</param>
        /// <param name="extension">File extension of original image.</param>
        /// <param name="desiredMaxDimension">Maximum dimension.</param>
        public ImageEngineImage(Stream stream, string extension, int desiredMaxDimension, bool enforceResize)
        {
            LoadFromStream(stream, extension, desiredMaxDimension, enforceResize);
        }


        /// <summary>
        /// Loads an image from a byte array.
        /// </summary>
        /// <param name="imageFileData">Fully formatted image file data</param>
        public ImageEngineImage(byte[] imageFileData)
        {
            using (MemoryStream ms = new MemoryStream(imageFileData))
                LoadFromStream(ms);
        }


        /// <summary>
        /// Loads an image from a byte array and scales (aspect safe) to a maximum size.
        /// e.g. 1024x512, desiredMaxDimension = 128 ===> Image is scaled to 128x64.
        /// </summary>
        /// <param name="imageFileData">Full image file data.</param>
        /// <param name="desiredMaxDimension">Maximum dimension.</param>
        public ImageEngineImage(byte[] imageFileData, int desiredMaxDimension, bool enforceResize)
        {
            using (MemoryStream ms = new MemoryStream(imageFileData))
                LoadFromStream(ms, desiredMaxDimension: desiredMaxDimension);
        }


        private void LoadFromFile(string imagePath, int desiredMaxDimension = 0, bool enforceResize = true)
        {
            Format format = new Format();
            FilePath = imagePath;

            // KFreon: Load image and save useful information including BGRA pixel data - may be processed from original into this form.
            MipMaps = ImageEngine.LoadImage(imagePath, out format, desiredMaxDimension, enforceResize);


            // KFreon: Can't pass properties as out :(
            Format = format;
        }

        
        private void LoadFromStream(Stream stream, string extension = null, int desiredMaxDimension = 0, bool enforceResize = true)
        {
            Format format = new Format();

            // KFreon: Load image and save useful information including BGRA pixel data - may be processed from original into this form.
            MipMaps = ImageEngine.LoadImage(stream, out format, extension, desiredMaxDimension, enforceResize);

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
        public bool Save(string destination, ImageEngineFormat format, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0)
        {
            using (FileStream fs = new FileStream(destination, FileMode.Create))
                return Save(fs, format, GenerateMips, desiredMaxDimension, mipToSave);
        }


        /// <summary>
        /// Saves fully formatted image in specified format to stream.
        /// </summary>
        /// <param name="destination">Stream to save to.</param>
        /// <param name="format">Format to save as.</param>
        /// <param name="GenerateMips">True = Generates all mipmaps. False = Uses largest available Mipmap.</param>
        /// <param name="desiredMaxDimension">Maximum value of either image dimension.</param>
        /// <returns>True if success</returns>
        public bool Save(Stream destination, ImageEngineFormat format, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0)
        {
            return ImageEngine.Save(MipMaps, format, destination, GenerateMips, desiredMaxDimension, mipToSave);
        }

        /// <summary>
        /// Gets a preview.
        /// </summary>
        /// <param name="ShowAlpha">False = Creates a preview without alpha.</param>
        /// <param name="index">Index of mipmap to preview.</param>
        /// <returns>BitmapImage of image.</returns>
        public BitmapSource GeneratePreview(int index, bool ShowAlpha)
        {
            // KFreon: NOTE: Seems to ignore alpha - pretty much ultra useful since premultiplying alpha often removes most of the image
            MipMap mip = MipMaps[index];

            BitmapSource bmp;
            if (ShowAlpha)
                bmp = mip.BaseImage;
            else
                bmp = new FormatConvertedBitmap(mip.BaseImage, System.Windows.Media.PixelFormats.Bgr32, null, 0);

            if (!bmp.IsFrozen)
                bmp.Freeze();

            return bmp;
        }


        /// <summary>
        /// Releases resources used by mipmap MemoryStreams.
        /// </summary>
        public void Dispose()
        {
            if (MipMaps == null)
                return;
        }


        /// <summary>
        /// Creates a GDI+ bitmap from largest mipmap.
        /// Does NOT require that image remains alive.
        /// </summary>
        /// <param name="ignoreAlpha">True = Previews image without alpha channel.</param>
        /// <param name="maxDimension">Largest size to display.</param>
        /// <returns>GDI+ bitmap of largest mipmap.</returns>
        public System.Drawing.Bitmap GetGDIBitmap(bool ignoreAlpha, int maxDimension = 0)
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

            mip.BaseImage.Freeze();
            return UsefulThings.WinForms.Imaging.CreateBitmap(mip.BaseImage, ignoreAlpha);
        }


        /// <summary>
        /// Scales top mipmap and DESTROYS ALL OTHERS.
        /// </summary>
        /// <param name="DesiredDimension">Desired size of image.</param>
        public void Resize(int DesiredDimension)
        {
            double scale = (double)DesiredDimension / (double)MipMaps[0].Width;  // TODO Do height too?
            Resize(scale);
        }


        /// <summary>
        /// Scales top mipmap and DESTROYS ALL OTHERS.
        /// </summary>
        /// <param name="scale">Scaling factor. </param>
        public void Resize(double scale)
        {
            MipMaps[0] = ImageEngine.Resize(MipMaps[0], scale);
            MipMaps.RemoveRange(1, NumMipMaps - 1);
        }

        /// <summary>
        /// Creates a WPF Bitmap from largest mipmap.
        /// Does NOT require that image remains alive.
        /// </summary>
        /// <returns>WPF bitmap of largest mipmap.</returns>
        public BitmapSource GetWPFBitmap(int maxDimension = 0)
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
            mip.BaseImage.Freeze();
            return mip.BaseImage;
        }
    }
}
