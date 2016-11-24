using CSharpImageLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UI_Project
{
    /// <summary>
    /// Interaction logic for NewMainWindow.xaml
    /// </summary>
    public partial class NewMainWindow : Window
    {
        public NewViewModel vm { get; private set; }

        DoubleAnimation windowWidthAnimation = new DoubleAnimation();
        DoubleAnimation windowHeightAnimation = new DoubleAnimation();
        Duration windowAnimDuration = new Duration(TimeSpan.FromSeconds(0.6));
        CubicEase windowAnimEaser = new CubicEase() { EasingMode = EasingMode.EaseOut };

        double VerticalRatio = 1.2/(1.2 + 2f) - .1;
        double HorizontalRatio = 0.5;
        bool IsBottomOpen = true;
        bool IsSideOpen = true;


        public NewMainWindow()
        {
            vm = new NewViewModel();

            // Wire up connections to enable VM changes to change window size.
            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "IsImageLoaded")
                {
                    if (vm.IsImageLoaded)
                        ExpandBottomPanel();
                    else
                    {
                        ContractSidePanel();
                        ContractBottomPanel();
                    }
                }
            };

            InitializeComponent();
            DataContext = vm;

            ContractSidePanel();
            ContractBottomPanel();
        }

        private void WindowMinMaxButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void WindowMinimiseButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void WindowCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UsefulThings.WPF.WindowBlur.EnableBlur(this);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = ImageFormats.GetSupportedExtensionsForDialogBoxAsString();
            ofd.Title = "Select image to load";
            if (ofd.ShowDialog() == true)
                vm.LoadImage(ofd.FileName);
        }

        void ExpandSidePanel()
        {
            if (IsSideOpen)
                return;

            windowWidthAnimation = new DoubleAnimation(this.Width, this.Width / HorizontalRatio, windowAnimDuration);
            windowWidthAnimation.EasingFunction = windowAnimEaser;

            this.BeginAnimation(Window.WidthProperty, windowWidthAnimation);

            IsSideOpen = true;
        }

        void ContractSidePanel()
        {
            if (!IsSideOpen)
                return;

            windowWidthAnimation = new DoubleAnimation(this.Width, this.Width * HorizontalRatio, windowAnimDuration);
            windowWidthAnimation.EasingFunction = windowAnimEaser;

            this.BeginAnimation(Window.WidthProperty, windowWidthAnimation);

            IsSideOpen = false;
        }

        void ExpandBottomPanel()
        {
            if (IsBottomOpen)
                return;

            windowHeightAnimation = new DoubleAnimation(this.Height, this.Height * (1 + VerticalRatio), windowAnimDuration);
            windowHeightAnimation.EasingFunction = windowAnimEaser;

            this.BeginAnimation(Window.HeightProperty, windowHeightAnimation);

            IsBottomOpen = true;
        }
        
        void ContractBottomPanel()
        {
            if (!IsBottomOpen)
                return;

            windowHeightAnimation = new DoubleAnimation(this.Height, this.Height * (1 - VerticalRatio), windowAnimDuration);
            windowHeightAnimation.EasingFunction = windowAnimEaser;

            this.BeginAnimation(Window.HeightProperty, windowHeightAnimation);

            IsBottomOpen = false;
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            ExpandSidePanel();

            // Save previews
            // Update any other required properties
        }
    }
}
