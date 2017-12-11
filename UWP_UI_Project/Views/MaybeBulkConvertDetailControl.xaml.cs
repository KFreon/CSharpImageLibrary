using System;

using UWP_UI_Project.Models;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UWP_UI_Project.Views
{
    public sealed partial class MaybeBulkConvertDetailControl : UserControl
    {
        public SampleOrder MasterMenuItem
        {
            get { return GetValue(MasterMenuItemProperty) as SampleOrder; }
            set { SetValue(MasterMenuItemProperty, value); }
        }

        public static readonly DependencyProperty MasterMenuItemProperty = DependencyProperty.Register("MasterMenuItem", typeof(SampleOrder), typeof(MaybeBulkConvertDetailControl), new PropertyMetadata(null, OnMasterMenuItemPropertyChanged));

        public MaybeBulkConvertDetailControl()
        {
            InitializeComponent();
        }

        private static void OnMasterMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MaybeBulkConvertDetailControl;
            control.ForegroundElement.ChangeView(0, 0, 1);
        }
    }
}
