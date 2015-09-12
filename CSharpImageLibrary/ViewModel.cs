using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UsefulThings.WPF;

namespace CSharpImageLibrary
{
    /// <summary>
    /// View model for the main Converter form
    /// </summary>
    public class ViewModel : ViewModelBase
    {
        public ImageEngineImage img { get; set; }


        #region Original Image Properties
        public MTObservableCollection<BitmapSource> Previews { get; set; }

        public int NumMipMaps
        {
            get
            {
                if (img != null)
                    return img.NumMipMaps;

                return -1;
            }
        }

        public string Format
        {
            get
            {
                return img?.Format.InternalFormat.ToString();
            }
        }

        public string ImagePath
        {
            get
            {
                return img?.FilePath;
            }
        }

        public BitmapSource Preview
        {
            get
            {
                if (Previews?.Count == 0)
                    return null;

                return Previews?[MipIndex];
            }
        }


        int mipwidth = 0;
        public int MipWidth
        {
            get
            {
                return mipwidth;
            }
            set
            {
                SetProperty(ref mipwidth, value);
            }
        }

        int mipheight = 0;
        public int MipHeight
        {
            get
            {
                return mipheight;
            }
            set
            {
                SetProperty(ref mipheight, value);
            }
        }

        internal void GenerateSavePreview()
        {
            asf
        }

        int mipindex = 0;
        public int MipIndex
        {
            get
            {
                return mipindex;
            }
            set
            {
                SetProperty(ref mipindex, value);
                OnPropertyChanged(nameof(Preview));
            }
        }
        #endregion Original Image Properties

        #region Save Properties
        bool generateMips = true;
        public bool GenerateMipMaps
        {
            get
            {
                return generateMips;
            }
            set
            {
                SetProperty(ref generateMips, value);
            }
        }

        string savePath = null;
        public string SavePath
        {
            get
            {
                return savePath;
            }
            set
            {
                SetProperty(ref savePath, value);
            }
        }

        ImageEngineFormat saveFormat = ImageEngineFormat.Unknown;
        public ImageEngineFormat SaveFormat
        {
            get
            {
                return saveFormat;
            }
            set
            {
                SetProperty(ref saveFormat, value);
            }
        }

        BitmapSource savePreview = null;
        public BitmapSource SavePreview
        {
            get
            {
                return savePreview;
            }
            set
            {
                SetProperty(ref savePreview, value);
            }
        }
        
        #endregion Save Properties

        public ViewModel()
        {
            Previews = new MTObservableCollection<BitmapSource>();
        }

        public void GotoSmallerMip()
        {
            if (MipIndex + 1 >= img.NumMipMaps)
                return;
            else
                MipIndex++;

            MipWidth /= 2;
            MipHeight /= 2;
        }

        public void GotoLargerMip()
        {
            if (MipIndex == 0)
                return;
            else
                MipIndex--;

            MipHeight *= 2;
            MipWidth *= 2;
        }

        public async Task LoadImage(string path)
        {
            Previews.Clear();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();


            img = await Task.Run(() => new ImageEngineImage(path));

            Console.WriteLine("");
            Console.WriteLine($"Format: {img.Format}");
            Console.WriteLine($"Image Loading: {stopwatch.ElapsedMilliseconds}");

            MipWidth = img.Width;
            MipHeight = img.Height;
            stopwatch.Restart();

            Previews.Add(img.GeneratePreview(0));
            MipIndex = 0;
            Task.Run(() =>
            {
                for (int i = 1; i < img.NumMipMaps; i++)
                    Previews.Add(img.GeneratePreview(i));
            });

            Debug.WriteLine($"Image Preview: {stopwatch.ElapsedMilliseconds}");
            stopwatch.Stop();

            OnPropertyChanged(nameof(ImagePath));
            OnPropertyChanged(nameof(Format));
            OnPropertyChanged(nameof(NumMipMaps));
        }

        internal void Save()
        {
            if (img != null && String.IsNullOrEmpty(SavePath) && SaveFormat != ImageEngineFormat.Unknown)
            {
                Stopwatch watc = new Stopwatch();
                watc.Start();
                img.Save(SavePath, SaveFormat, GenerateMipMaps);
                watc.Stop();
                Debug.WriteLine($"Saved format: {SaveFormat} in {watc.ElapsedMilliseconds} milliseconds.");
            }
        }
    }
}
