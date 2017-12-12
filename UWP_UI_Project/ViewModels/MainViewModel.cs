using System;
using System.IO;
using UWP_UI_Project.Helpers;
using UWP_UI_Project.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP_UI_Project.ViewModels
{
    public class MainViewModel : Observable
    {
        public BitmapImage MainImage { get; set; }

        public MainViewModel()
        {
        }

        internal async void Load(StorageFile file)
        {
            using (var image = new ImageEngineImage())
            {
                await image.Load(file);
                MainImage = await image.ToBitmap();

                St
                BitmapEncoder encoder = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, )

                Console.WriteLine();
            }
        }
    }
}
