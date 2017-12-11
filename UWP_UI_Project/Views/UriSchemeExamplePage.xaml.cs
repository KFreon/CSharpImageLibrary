﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using UWP_UI_Project.ViewModels;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UWP_UI_Project.Views
{
    // TODO WTS: This page exists purely as an example of how to launch a specific page
    // in response to a protocol launch and pass it a value. It is expected that you will
    // delete this page once you have changed the handling of a protocol launch to meet your
    // needs and redirected to another of your pages.
    public sealed partial class UriSchemeExamplePage : Page
    {
        public UriSchemeExampleViewModel ViewModel { get; } = new UriSchemeExampleViewModel();

        public UriSchemeExamplePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Capture the passed in value and assign it to a property that's displayed on the view
            ViewModel.Secret = e.Parameter.ToString();
        }
    }
}
