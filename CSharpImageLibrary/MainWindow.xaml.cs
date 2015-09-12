using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UsefulThings.WPF;
using System.Diagnostics;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ViewModel vm = new ViewModel();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = vm;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.PageDown)
            {
                vm.GotoSmallerMip();
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                vm.GotoLargerMip();
                e.Handled = true;
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Supported Image Files|*.dds;*.jpg;*.png;*.jpeg;*.bmp";
            ofd.Title = "Select image to load";
            if (ofd.ShowDialog() == true)
                vm.LoadImage(ofd.FileName);
        }

        private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.GenerateSavePreview();
        }

        private void OpenConvertPanel_Click(object sender, RoutedEventArgs e)
        {
as
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = 
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
asf
        }
    }
}
