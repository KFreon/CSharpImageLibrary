using System;

using UWP_UIProject.ViewModels;

using Windows.UI.Xaml.Controls;

namespace UWP_UIProject.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
