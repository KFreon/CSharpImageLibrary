using CSharpImageLibrary;
using System;
using System.Threading.Tasks;
using UsefulUWPThings;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using static CSharpImageLibrary.ImageFormats;

namespace UWP_UI_Project.Models
{
    public class MipMap : MipMapBase
    {
        public MipMap()
        {

        }

        public MipMap(byte[] pixels, int width, int height, ImageEngineFormatDetails details)
            : base(pixels, width, height, details)
        {

        }

        public override MipMapBase Resize(double xScale, double yScale)
        {
            var task = UWPResize(xScale, yScale);
            task.Wait();
            return task.Result;
        }

        private async Task<MipMapBase> UWPResize(double xScale, double yScale)
        {
            int newWidth = (int)(Width * xScale);
            int newHeight = (int)(Height * yScale);

            using (var destination = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, destination);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)Width, (uint)Height, 96, 96, Pixels);
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.BitmapTransform.ScaledHeight = (uint)newHeight;
                encoder.BitmapTransform.ScaledWidth = (uint)newWidth;

                await encoder.FlushAsync();

                var bytes = await destination.ToByteArray();

                return new MipMap(bytes, newWidth, newHeight, LoadedFormatDetails);
            }
        }
    }
}
