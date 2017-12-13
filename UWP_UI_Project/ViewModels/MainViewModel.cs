using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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

        internal async Task Load(StorageFile file)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var image = await ImageEngineImage.CreateAsync(file);
            Trace.WriteLine($"Loading: {watch.ElapsedMilliseconds}");
            await MainImage.SetSourceAsync(await image.ToBitmapStream());
            watch.Stop();
            Trace.WriteLine($"plus drawing: {watch.ElapsedMilliseconds}");
        }
    }
}
