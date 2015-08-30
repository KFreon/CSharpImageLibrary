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
                try
                {
                    OrigImage.Source = UsefulThings.WPF.Images.CreateWPFBitmap(ofd.FileName);
                }
                catch
                {
                    OrigImage.Source = null;
                }
                vm.LoadImage(ofd.FileName);
            }
        }

        private void NewImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == true)
                vm.Save(sfd.FileName, (ImageEngineFormat)FormatSelector.SelectedItem);
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

        ImageEngineImage img = null;

        bool generatemips = true;
        public bool GenerateMips
        {
            get
            {
                return generatemips;
            }
            set
            {
                SetProperty(ref generatemips, value);
            }
        }

        public VM()
        {
            
        }

        public void LoadImage(string path)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            img = new ImageEngineImage(path);

            Debug.WriteLine("");
            Debug.WriteLine($"Format: {img.Format}");
            Debug.WriteLine($"Image Loading: {stopwatch.ElapsedMilliseconds}");


            stopwatch.Restart();

            Preview = img.GeneratePreview();

            Debug.WriteLine($"Image Preview: {stopwatch.ElapsedMilliseconds}");
            stopwatch.Stop();

            Format = img.Format.InternalFormat.ToString();
            ImagePath = path;

            //ATI1.TestWrite(img.PixelData, @"R:\test.jpg", (int)img.Width, (int)img.Height);
        }

        internal void Save(string fileName, ImageEngineFormat format)
        {
            if (img != null)
            {
                Stopwatch watc = new Stopwatch();
                watc.Start();
                img.Save(fileName, format, GenerateMips);
                watc.Stop();
                Debug.WriteLine($"Saved format: {format} in {watc.ElapsedMilliseconds} milliseconds.");
            }
        }
    }
}
