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
        public BitmapImage MainImage { get; } = new BitmapImage();

        public MainViewModel()
        {
        }

        internal async void Load(StorageFile file)
        {
            var image = await ImageEngineImage.CreateAsync(file);
            await MainImage.SetSourceAsync(await image.ToBitmapStream());
        }
    }
}
