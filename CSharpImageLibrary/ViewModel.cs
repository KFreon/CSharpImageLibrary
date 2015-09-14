using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CSharpImageLibrary.General;
using UsefulThings.WPF;

namespace CSharpImageLibrary
{
    /// <summary>
    /// View model for the main Converter form
    /// </summary>
    public class ViewModel : ViewModelBase
    {
        public ImageEngineImage img { get; set; }
        Stopwatch stopwatch = new Stopwatch();

        long saveElapsed = -1;
        public long SaveElapsedTime
        {
            get
            {
                return saveElapsed;
            }
            set
            {
                SetProperty(ref saveElapsed, value);
            }
        }

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
                SaveSuccess = null;
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
                SaveSuccess = null;
                SetProperty(ref savePath, value);
                OnPropertyChanged(nameof(IsSaveReady));
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
                SaveSuccess = null;
                SetProperty(ref saveFormat, value);
                OnPropertyChanged(nameof(IsSaveReady));
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

        public bool IsSaveReady
        {
            get
            {
                return !String.IsNullOrEmpty(SavePath) && SaveFormat != ImageEngineFormat.Unknown;
            }
        }

        public string SavingFailedErrorMessage
        {
            get; private set;
        }

        bool? saveSuccess = null;
        public bool? SaveSuccess
        {
            get
            {
                return saveSuccess;
            }
            set
            {
                SetProperty(ref saveSuccess, value);
            }
        }
        #endregion Save Properties


        public ViewModel()
        {
            Previews = new MTObservableCollection<BitmapSource>();
        }

        internal string GetAutoSavePath(ImageEngineFormat newformat)
        {
            return Path.GetDirectoryName(ImagePath) + "\\" + Path.GetFileNameWithoutExtension(ImagePath) + "_" + newformat + Path.GetExtension(ImagePath);
        }

        internal async void GenerateSavePreview()
        {
            if (img == null || SaveFormat == ImageEngineFormat.Unknown)
                return;

            stopwatch.Start();
            SavePreview = await Task.Run(() =>
            {
                using (MemoryStream stream = UsefulThings.RecyclableMemoryManager.GetStream())
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    img.Save(stream, SaveFormat, false, 1024);  // KFreon: Smaller size for quicker loading
                    watch.Stop();
                    Debug.WriteLine($"Preview Save took {watch.ElapsedMilliseconds}ms");
                    using (ImageEngineImage previewimage = new ImageEngineImage(stream))
                        return previewimage.GetWPFBitmap();
                }
            });
            stopwatch.Stop();
            Debug.WriteLine($"Preview generation took {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Reset();
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
            SaveSuccess = null;
            Previews.Clear();
            SavePreview = null;
            SavePath = null;
            SaveFormat = ImageEngineFormat.Unknown;

            stopwatch.Start();

            img = await Task.Run(() => new ImageEngineImage(path));

            Console.WriteLine("");
            Console.WriteLine($"Format: {img.Format}");
            stopwatch.Stop();
            Console.WriteLine($"Image Loading: {stopwatch.ElapsedMilliseconds}");
            stopwatch.Restart();

            MipWidth = img.Width;
            MipHeight = img.Height;

            Previews.Add(img.GeneratePreview(0));
            MipIndex = 0;
            Task.Run(() =>
            {
                for (int i = 1; i < img.NumMipMaps; i++)
                    Previews.Add(img.GeneratePreview(i));
            });

            stopwatch.Stop();
            Debug.WriteLine($"Image Preview: {stopwatch.ElapsedMilliseconds}");
            stopwatch.Reset();

            OnPropertyChanged(nameof(ImagePath));
            OnPropertyChanged(nameof(Format));
            OnPropertyChanged(nameof(NumMipMaps));
        }

        internal bool Save()
        {
            if (img != null && !String.IsNullOrEmpty(SavePath) && SaveFormat != ImageEngineFormat.Unknown)
            {
                try
                {
                    stopwatch.Start();
                    img.Save(SavePath, SaveFormat, GenerateMipMaps);
                    stopwatch.Stop();
                    Debug.WriteLine($"Saved format: {SaveFormat} in {stopwatch.ElapsedMilliseconds} milliseconds.");

                    SaveElapsedTime = stopwatch.ElapsedMilliseconds;

                    stopwatch.Reset();
                    SaveSuccess = true;
                    return true;
                }
                catch(Exception e)
                {
                    SavingFailedErrorMessage = e.ToString();
                    SaveSuccess = false;
                    return false;
                }
            }

            return false;
        }
    }
}
