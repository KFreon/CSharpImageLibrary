using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP_UI_Project.Models
{
    public static class Extensions
    {
        public static async Task<BitmapImage> ToBitmap(this ImageEngineImage img)
        {
            var stream = new InMemoryRandomAccessStream();
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)img.Width, (uint)img.Height, 96, 96, img.MipMaps[0].Pixels);
            await encoder.FlushAsync();

            BitmapImage image = new BitmapImage();
            await image.SetSourceAsync(stream);
            return image;
        }
    }
}
