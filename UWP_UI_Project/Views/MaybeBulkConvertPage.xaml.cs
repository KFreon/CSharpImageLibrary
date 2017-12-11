using System;

using UWP_UI_Project.ViewModels;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UWP_UI_Project.Views
{
    public sealed partial class MaybeBulkConvertPage : Page
    {
        public MaybeBulkConvertViewModel ViewModel { get; } = new MaybeBulkConvertViewModel();

        public MaybeBulkConvertPage()
        {
            InitializeComponent();
            Loaded += MaybeBulkConvertPage_Loaded;
        }

        private async void MaybeBulkConvertPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadDataAsync(MasterDetailsViewControl.ViewState);
        }
    }
}
