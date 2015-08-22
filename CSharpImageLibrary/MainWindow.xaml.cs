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

namespace CSharpImageLibrary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VM vm = new VM();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = vm;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                OrigImage.Source = UsefulThings.WPF.Images.CreateWPFBitmap(ofd.FileName);
                vm.LoadImage(ofd.FileName);
            }
        }
    }

    public class VM : ViewModelBase
    {
        BitmapImage preview = null;
        public BitmapImage Preview
        {
            get
            {
                return preview;
            }
            set
            {
                SetProperty(ref preview, value);
            }
        }

        string format = null;
        public string Format
        {
            get
            {
                return format;
            }
            set
            {
                SetProperty(ref format, value);
            }
        }

        string imagepath = null;
        public string ImagePath
        {
            get
            {
                return imagepath;
            }
            set
            {
                SetProperty(ref imagepath, value);
            }
        }


        public VM()
        {
            
        }

        public void LoadImage(string path)
        {
            ImageEngineImage img = new ImageEngineImage(path);
            Preview = img.GeneratePreview();

            Format = img.Format.InternalFormat.ToString();
            ImagePath = path;
        }
    }
}
