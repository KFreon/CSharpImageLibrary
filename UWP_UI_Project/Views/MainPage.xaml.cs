using System;

using UWP_UI_Project.ViewModels;
using Windows.Storage.Pickers;
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

        private async void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".dds");


            var selectedFile = await picker.PickSingleFileAsync();
            if (selectedFile != null)
                ViewModel.Load(selectedFile);
        }
    }
}
