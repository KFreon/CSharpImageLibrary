using System;

using UWP_UI_Project.ViewModels;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UWP_UI_Project.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; } = new SettingsViewModel();

        //// TODO WTS: Change the URL for your privacy policy in the Resource File, currently set to https://YourPrivacyUrlGoesHere

        public SettingsPage()
        {
            InitializeComponent();
            ViewModel.Initialize();
        }
    }
}
