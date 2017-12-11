using System.Linq;
using System.Windows.Media.Imaging;
using CSharpImageLibrary;

namespace CSharpImageLibrary_NET
{
    public static class Extensions
    {
        /// <summary>
        /// Creates a WPF image from this mipmap.
        /// </summary>
        /// <returns>WriteableBitmap of image.</returns>
        public static BitmapSource ToImage(this MipMapBase mipmap)
        {
            var tempPixels = CSharpImageLibrary.ImageEngine.GetPixelsAsBGRA32(mipmap.Width, mipmap.Height, mipmap.Pixels, mipmap.LoadedFormatDetails);
            var bmp = UsefulThings.WPF.Images.CreateWriteableBitmap(tempPixels, mipmap.Width, mipmap.Height);
            bmp.Freeze();
            return bmp;
        }

        /// <summary>
        /// Creates a WPF Bitmap from largest mipmap.
        /// Does NOT require that image remains alive.
        /// </summary>
        /// <param name="ShowAlpha">True = flattens alpha, directly affecting RGB.</param>
        /// <param name="maxDimension">Resizes image or uses a mipmap if available. Overrides mipIndex if specified.</param>
        /// <param name="mipIndex">Index of mipmap to retrieve. Overridden by maxDimension if it's specified.</param>
        /// <returns>WPF bitmap of largest mipmap.</returns>
        public static BitmapSource GetWPFBitmap(this ImageEngineImageBase<MipMap> image, int maxDimension = 0, bool ShowAlpha = false, int mipIndex = 0)
        {
            MipMapBase mip = image.MipMaps[mipIndex];
            return GetWPFBitmap(image, mip, maxDimension, ShowAlpha);
        }


        static BitmapSource GetWPFBitmap(ImageEngineImageBase<MipMap> image, MipMapBase mip, int maxDimension, bool ShowAlpha)
        {
            BitmapSource bmp = null;

            if (maxDimension != 0)
            {
                // Choose a mip of the correct size, if available.
                var sizedMip = image.MipMaps.Where(m => (m.Height <= maxDimension && m.Width <= maxDimension) || (m.Width <= maxDimension && m.Height <= maxDimension));
                if (sizedMip.Any())
                {
                    var mip1 = sizedMip.First();
                    bmp = mip1.ToImage();
                }
                else
                {
                    double scale = (double)maxDimension / (image.Height > image.Width ? image.Height : image.Width);
                    mip = mip.Resize(scale);
                    bmp = mip.ToImage();
                }
            }
            else
                bmp = mip.ToImage();

            if (!ShowAlpha)
                bmp = new FormatConvertedBitmap(bmp, System.Windows.Media.PixelFormats.Bgr32, null, 0);

            bmp.Freeze();
            return bmp;
        }
    }
}
