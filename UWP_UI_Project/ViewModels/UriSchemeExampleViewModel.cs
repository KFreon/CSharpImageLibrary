﻿using System;

using UWP_UI_Project.Helpers;

namespace UWP_UI_Project.ViewModels
{
    // TODO WTS: This class exists purely as part of the example of how to launch a specific page in response to a protocol launch and pass it a value. It is expected that you will delete this class once you have changed the handling of a protocol launch to meet your needs and redirected to another of your pages.
    public class UriSchemeExampleViewModel : Observable
    {
        // This property is just for displaying the passed in value
        private string _secret;

        public string Secret
        {
            get { return _secret; }
            set { Set(ref _secret, value); }
        }
    }
}
