using System;
using UWP_UI_Project.Services;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP_UI_Project.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        public StartPage()
        {
            this.InitializeComponent();
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.Desktop
            };

            picker.FileTypeFilter.Add(".dds");

            var selectedFile = await picker.PickSingleFileAsync();
            if (selectedFile != null)
            {
                var frame = (Frame)Window.Current.Content;
                frame.Navigate(typeof(PivotPage), (typeof(MainPage), selectedFile));
                return;
            }
        }
    }
}
