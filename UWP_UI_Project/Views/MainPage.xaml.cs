using System;

using UWP_UI_Project.ViewModels;

using Windows.UI.Xaml.Controls;

namespace UWP_UI_Project.Views
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
