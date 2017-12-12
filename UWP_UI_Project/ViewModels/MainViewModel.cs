using System;

using UWP_UI_Project.Helpers;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP_UI_Project.ViewModels
{
    public class MainViewModel : Observable
    {
        public BitmapImage MainImage { get; set; }

        public MainViewModel()
        {
        }
    }
}
