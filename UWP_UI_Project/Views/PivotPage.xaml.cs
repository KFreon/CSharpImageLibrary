using System;
using UWP_UI_Project.Services;
using UWP_UI_Project.ViewModels;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UWP_UI_Project.Views
{
    public sealed partial class PivotPage : Page
    {
        public PivotViewModel ViewModel { get; } = new PivotViewModel();

        public PivotPage()
        {
            // We use NavigationCacheMode.Required to keep track the selected item on navigation. For further information see the following links.
            // https://msdn.microsoft.com/en-us/library/windows/apps/xaml/windows.ui.xaml.controls.page.navigationcachemode.aspx
            // https://msdn.microsoft.com/en-us/library/windows/apps/xaml/Hh771188.aspx
            NavigationCacheMode = NavigationCacheMode.Required;
            DataContext = ViewModel;
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ValueTuple<Type, StorageFile>)
            {
                (Type pageType, StorageFile file) = (ValueTuple<Type, StorageFile>)e.Parameter;

                if (pageType == typeof(MainPage))
                    ThePivot.SelectedItem = MainPage;

                var mainPage = (MainPage)MainPageFrame.Content;
                await mainPage.ViewModel.Load(file);
            }
        }
    }
}
