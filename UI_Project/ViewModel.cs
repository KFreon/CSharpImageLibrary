using CSharpImageLibrary;
using CSharpImageLibrary.DDS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UsefulThings.WPF;

namespace UI_Project
{
    /// <summary>
    /// View model for the main Converter form
    /// </summary>
    public class ViewModel : ViewModelBase
    {
        /// <summary>
        /// Current image loaded.
        /// </summary>
        public ImageEngineImage img { get; set; }
        Stopwatch GeneralTimer = new Stopwatch();
        DispatcherTimer savePreviewUpdateTimer = new DispatcherTimer();

        /// <summary>
        /// True = Alpha in DXT1 is merged with its RGB, showing how many applications display such an image.
        /// </summary>
        public bool IsDXT1AlphaVisible
        {
            get
            {
                return SaveFormat == ImageEngineFormat.DDS_DXT1;
            }
        }

        bool flattenBlend = true;
        /// <summary>
        /// True = Merge DXT1 alpha with RGB when saving, to permanently show how most applications display it.
        /// </summary>
        public bool FlattenBlend
        {
            get
            {
                return flattenBlend;
            }
            set
            {
                SetProperty(ref flattenBlend, value);
                stripAlpha = !value;
                OnPropertyChanged(nameof(StripAlpha));
                DDSGeneral.DXT1AlphaThreshold = blendValue;
                GenerateSavePreview();
            }
        }

        bool stripAlpha = false;
        /// <summary>
        /// Remove DXT1 alpha during saving, to just get the good stuff.
        /// </summary>
        public bool StripAlpha
        {
            get
            {
                return stripAlpha;
            }
            set
            {
                SetProperty(ref stripAlpha, value);
                flattenBlend = !value;
                OnPropertyChanged(nameof(FlattenBlend));
                GenerateSavePreview();
                DDSGeneral.DXT1AlphaThreshold = 0f;  // KFreon: Strips the alpha out 
            }
        }

        float blendValue = DDSGeneral.DXT1AlphaThreshold;
        /// <summary>
        /// Threshold for DXT1 alpha to determine whether pixel is transparent or not.
        /// </summary>
        public float DXT1AlphaThreshold
        {
            get
            {
                DDSGeneral.DXT1AlphaThreshold = blendValue;
                return DDSGeneral.DXT1AlphaThreshold*100f;
            }
            set
            {
                DDSGeneral.DXT1AlphaThreshold = value/100f;
                OnPropertyChanged(nameof(DXT1AlphaThreshold));
                blendValue = value/100f;
                savePreviewUpdateTimer.Start();
            }
        }


        bool showAlphaPreviews = false;
        /// <summary>
        /// Determines whether user wants alpha displayed.
        /// </summary>
        public bool ShowAlphaPreviews
        {
            get
            {
                return showAlphaPreviews;
            }
            set
            {
                SetProperty(ref showAlphaPreviews, value);
                UpdatePreviews();
                OnPropertyChanged(nameof(SavePreview));
            }
        }

        long saveElapsed = -1;
        /// <summary>
        /// Elapsed time during saving.
        /// </summary>
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
        /// <summary>
        /// Currently selected image previews based on alpha settings. Gets swapped out when user changes between alpha and non-alpha display.
        /// </summary>
        public MTRangedObservableCollection<BitmapSource> Previews { get; set; }
        List<BitmapSource> AlphaPreviews { get; set; }
        List<BitmapSource> NonAlphaPreviews { get; set; }

        /// <summary>
        /// Number of mipmaps in loaded image.
        /// </summary>
        public int NumMipMaps
        {
            get
            {
                if (img != null)
                    return img.NumMipMaps;

                return -1;
            }
        }

        /// <summary>
        /// Format of currently loaded image.
        /// </summary>
        public string Format
        {
            get
            {
                return img?.Format.ToString();
            }
        }

        /// <summary>
        /// Path to loaded image.
        /// </summary>
        public string ImagePath
        {
            get
            {
                return img?.FilePath;
            }
        }

        /// <summary>
        /// Preview of image based on selected mipmap and alpha settings.
        /// </summary>
        public BitmapSource Preview
        {
            get
            {
                if (Previews?.Count == 0 || MipIndex >= Previews?.Count + 1)
                    return null;

                return Previews?[MipIndex - 1];
            }
        }

        /// <summary>
        /// Width of selected mipmap.
        /// </summary>
        public double? MipWidth
        {
            get
            {
                return Preview?.PixelWidth;
            }
        }

        /// <summary>
        /// Height of selected mipmap.
        /// </summary>
        public double? MipHeight
        {
            get
            {
                return Preview?.PixelHeight;
            }
        }

        int mipindex = 1;
        /// <summary>
        /// Selected mip index.
        /// </summary>
        public int MipIndex
        {
            get
            {
                return mipindex;
            }
            set
            {
                if (value < 0 || value >= NumMipMaps + 1)
                    return;

                SetProperty(ref mipindex, value);
                OnPropertyChanged(nameof(Preview));
                OnPropertyChanged(nameof(MipWidth));
                OnPropertyChanged(nameof(MipHeight));
            }
        }
        #endregion Original Image Properties


        #region Save Properties
        MipHandling generateMips = MipHandling.Default;
        /// <summary>
        /// Determines what to do with mipmaps during saving.
        /// </summary>
        public MipHandling GenerateMipMaps
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
        /// <summary>
        /// Path to save image to.
        /// </summary>
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
        /// <summary>
        /// Format to save image as.
        /// </summary>
        public ImageEngineFormat SaveFormat
        {
            get
            {
                return saveFormat;
            }
            set
            {
                // Do nothing unless there's a change. Stops regenerating previews when nothing actually needs regeneration.
                if (value == saveFormat)
                    return;

                SaveSuccess = null;
                SetProperty(ref saveFormat, value);
                OnPropertyChanged(nameof(IsSaveReady));
                OnPropertyChanged(nameof(IsDXT1AlphaVisible));
            }
        }

        BitmapSource[] savePreviews = new BitmapSource[2];
        /// <summary>
        /// Preview of how image looks if it were saved with selected settings.
        /// </summary>
        public BitmapSource SavePreview
        {
            get
            {
                return ShowAlphaPreviews ? savePreviews[0] : savePreviews[1];
            }
        }

        /// <summary>
        /// Indicator as to whether saving is allowed based on mandatory settings.
        /// </summary>
        public bool IsSaveReady
        {
            get
            {
                return !String.IsNullOrEmpty(SavePath) && SaveFormat != ImageEngineFormat.Unknown;
            }
        }

        /// <summary>
        /// Error message from saving.
        /// </summary>
        public string SavingFailedErrorMessage
        {
            get; private set;
        }

        bool? saveSuccess = null;
        /// <summary>
        /// Indicates status of saving.
        /// </summary>
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


        /// <summary>
        /// View model constructor.
        /// </summary>
        public ViewModel()
        {
            Previews = new MTRangedObservableCollection<BitmapSource>();

            // KFreon: Timer starts when alpha slider is updated, waits for a second of inaction before making new previews (inaction because it's restarted everytime the slider changes, and when it makes a preview, it stops itself)
            // KFreon: Delay regeneration if previous previews are still being generated
            savePreviewUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            savePreviewUpdateTimer.Tick += (s, b) =>
            {
                savePreviewUpdateTimer.Stop();
            };
        }

        internal string GetAutoSavePath(ImageEngineFormat newformat)
        {
            string newpath = null;
            bool acceptablePath = false;
            int count = 1;


            string formatString = ImageFormats.GetExtensionOfFormat(newformat);

            string basepath = Path.GetDirectoryName(ImagePath) + "\\" + Path.GetFileNameWithoutExtension(ImagePath) + "." +
                (newformat == ImageEngineFormat.Unknown ? Path.GetExtension(ImagePath) : formatString);

            newpath = basepath;

            // KFreon: Check that path is not already taken
            while (!acceptablePath)
            {
                if (File.Exists(newpath))
                    newpath = Path.Combine(Path.GetDirectoryName(basepath),  Path.GetFileNameWithoutExtension(basepath) + "_" + count++ + Path.GetExtension(basepath));
                else
                    acceptablePath = true;
            }
            
            return newpath;
        }

        internal async void GenerateSavePreview()
        {
            if (img == null || SaveFormat == ImageEngineFormat.Unknown)
                return;


            // KFreon: TGA saving not supported
            if (img.Format == ImageEngineFormat.TGA)
                SaveFormat = ImageEngineFormat.PNG;

            savePreviews = await Task.Run(() =>
            {
                // Start barrier timer if not too close to previous save preview generation - stops thrashing.
                if (!savePreviewUpdateTimer.IsEnabled)
                    savePreviewUpdateTimer.Start(); 

                GeneralTimer.Reset(); // Timer to just measure timer
                GeneralTimer.Start();
                var stream = img.Save(SaveFormat, MipHandling.KeepTopOnly, 1024, mergeAlpha: (SaveFormat == ImageEngineFormat.DDS_DXT1 ? FlattenBlend : false));  // KFreon: Smaller size for quicker loading
                GeneralTimer.Stop();
                Debug.WriteLine($"{SaveFormat} preview generation took {GeneralTimer.ElapsedMilliseconds}ms");
                using (ImageEngineImage previewimage = new ImageEngineImage(stream))
                {
                    BitmapSource[] tempImgs = new BitmapSource[2];
                    tempImgs[0] = previewimage.GetWPFBitmap(ShowAlpha: true);
                    tempImgs[1] = previewimage.GetWPFBitmap(ShowAlpha: false);
                    return tempImgs;
                }
            });
            OnPropertyChanged(nameof(SavePreview));
        }

        /// <summary>
        /// Load image from path.
        /// </summary>
        /// <param name="path">Path to image to load.</param>
        /// <returns>Nothing. Async needs task to await.</returns>
        public async Task LoadImage(string path)
        {
            bool testing = true;  // Set to true to load mips single threaded and only the full image instead of a smaller one first.

            // Load file into memory
            byte[] imgData = File.ReadAllBytes(path);

            Task<List<object>> fullLoadingTask = null;
            if (!testing)
            {
                // Load full size image
                ////////////////////////////////////////////////////////////////////////////////////////
                fullLoadingTask = Task.Run(() =>
                {
                    GeneralTimer.Reset();
                    GeneralTimer.Start();
                    ImageEngineImage fullimage = new ImageEngineImage(imgData);
                    GeneralTimer.Stop();
                    Console.WriteLine($"{fullimage.Format} Loading: {GeneralTimer.ElapsedMilliseconds}");

                    List<BitmapSource> alphas = new List<BitmapSource>();
                    List<BitmapSource> nonalphas = new List<BitmapSource>();

                    for (int i = 0; i < fullimage.NumMipMaps; i++)
                    {
                        alphas.Add(fullimage.GetWPFBitmap(ShowAlpha: true, mipIndex: i));
                        nonalphas.Add(fullimage.GetWPFBitmap(ShowAlpha: false, mipIndex: i));
                    }

                    List<object> bits = new List<object>();
                    bits.Add(fullimage);
                    bits.Add(alphas);
                    bits.Add(nonalphas);
                    return bits;
                });
                ////////////////////////////////////////////////////////////////////////////////////////
            }




            SaveSuccess = null;
            Previews.Clear();
            savePreviews = new BitmapSource[2];
            SavePath = null;
            SaveFormat = ImageEngineFormat.Unknown;

            


            // Want to load entire image, no resizing when testing.
            ////////////////////////////////////////////////////////////////////////////////////////
            if (testing)
            {
                GeneralTimer.Reset();
                GeneralTimer.Start();
                img = await Task.Run(() => new ImageEngineImage(imgData));
                GeneralTimer.Stop();
                Console.WriteLine($"{img.Format} Loading: {GeneralTimer.ElapsedMilliseconds}");
            }
            else
                img = await Task.Run(() => new ImageEngineImage(imgData, 256));
            ////////////////////////////////////////////////////////////////////////////////////////

            img.FilePath = path;            

            Previews.Add(img.GetWPFBitmap(maxDimension: 1024, ShowAlpha: ShowAlphaPreviews));
            MipIndex = 1;  // 1 based

            OnPropertyChanged(nameof(ImagePath));
            OnPropertyChanged(nameof(Format));
            OnPropertyChanged(nameof(NumMipMaps));
            OnPropertyChanged(nameof(Preview));
            OnPropertyChanged(nameof(MipWidth));
            OnPropertyChanged(nameof(MipHeight));

            // KFreon: Get full image details
            ////////////////////////////////////////////////////////////////////////////////////////
            if (!testing)
            {
                List<object> FullImageObjects = await fullLoadingTask;
                double? oldMipWidth = MipWidth;
                img = (ImageEngineImage)FullImageObjects[0];

                AlphaPreviews = (List<BitmapSource>)FullImageObjects[1];
                NonAlphaPreviews = (List<BitmapSource>)FullImageObjects[2];

                UpdatePreviews();

                // KFreon: Set selected mip index
                /*for (int i = 0; i < Previews.Count; i++)
                {
                    if (Previews[i].Width == oldMipWidth)
                    {
                        MipIndex = i + 1;  // 1 based
                        break;
                    }
                }*/
                MipIndex = 1;
            }
            
            ////////////////////////////////////////////////////////////////////////////////////////


            OnPropertyChanged(nameof(NumMipMaps));
            OnPropertyChanged(nameof(Preview));
            OnPropertyChanged(nameof(MipIndex));
            OnPropertyChanged(nameof(MipWidth));
            OnPropertyChanged(nameof(MipHeight));
            OnPropertyChanged(nameof(img));
        }

        private void UpdatePreviews()
        {
            if (AlphaPreviews == null || NonAlphaPreviews == null)
                return; 

            Previews.Clear();
            Previews.AddRange(ShowAlphaPreviews ? AlphaPreviews : NonAlphaPreviews);
            OnPropertyChanged(nameof(Preview));
        }

        internal bool Save()
        {
            if (img != null && !String.IsNullOrEmpty(SavePath) && SaveFormat != ImageEngineFormat.Unknown)
            {
                try
                {
                    GeneralTimer.Start();
                    img.Save(SavePath, SaveFormat, generateMips, mergeAlpha: (SaveFormat == ImageEngineFormat.DDS_DXT1 ? FlattenBlend : false));
                    GeneralTimer.Stop();
                    Debug.WriteLine($"Saved format: {SaveFormat} in {GeneralTimer.ElapsedMilliseconds} milliseconds.");

                    SaveElapsedTime = GeneralTimer.ElapsedMilliseconds;

                    GeneralTimer.Reset();
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
