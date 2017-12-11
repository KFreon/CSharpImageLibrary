using System;

using UWP_UI_Project.ViewModels;

using Windows.UI.Xaml.Controls;

namespace UWP_UI_Project.Views
{
    public sealed partial class BulkConvertPage : Page
    {
        public BulkConvertViewModel ViewModel { get; } = new BulkConvertViewModel();

        public BulkConvertPage()
        {
            InitializeComponent();
        }
    }
}
