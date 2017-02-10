using CSharpImageLibrary;
using CSharpImageLibrary.DDS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UsefulThings.WPF;

namespace UI_Project
{
    public enum AlphaDisplaySettings
    {
        [Description("Don't show Alpha")]
        NoAlpha,

        [Description("Alpha 'merged' (premult) with RGB")]
        PremultiplyAlpha,

        [Description("Alpha Only")]
        AlphaOnly,
    }

    public class NewViewModel : ViewModelBase
    {
        bool useHighQualityScaling = true;
        public bool UseHighQualityScaling
        {
            get
            {
                return useHighQualityScaling;
            }
            set
            {
                SetProperty(ref useHighQualityScaling, value);
            }
        }

        bool useSourceFormatForSaving = false;
        public bool UseSourceFormatForSaving
        {
            get
            {
                return useSourceFormatForSaving;
            }
            set
            {
                SetProperty(ref useSourceFormatForSaving, value);
            }
        }

        Stopwatch timer = new Stopwatch();

        bool showHelpAbout = false;
        public bool ShowHelpAbout
        {
            get
            {
                return showHelpAbout;
            }
            set
            {
                SetProperty(ref showHelpAbout, value);
            }
        }

        bool isRedChannelOn = true;
        public bool IsRedChannelOn
        {
            get
            {
                return isRedChannelOn;
            }
            set
            {
                SetProperty(ref isRedChannelOn, value);

                if (IsImageLoaded)
                    SetChannel(Preview, value, 2);
            }
        }

        bool isGreenChannelOn = true;
        public bool IsGreenChannelOn
        {
            get
            {
                return isGreenChannelOn;
            }
            set
            {
                SetProperty(ref isGreenChannelOn, value);

                if (IsImageLoaded)
                    SetChannel(Preview, value, 1);
            }
        }

        bool isBlueChannelOn = true;
        public bool IsBlueChannelOn
        {
            get
            {
                return isBlueChannelOn;
            }
            set
            {
                SetProperty(ref isBlueChannelOn, value);

                if (IsImageLoaded)
                    SetChannel(Preview, value, 0);
            }
        }



        #region Settings Panel Properties
        bool settingsPanelOpen = false;
        public bool SettingsPanelOpen
        {
            get
            {
                return settingsPanelOpen;
            }
            set
            {
                SetProperty(ref settingsPanelOpen, value);
            }
        }

        public bool EnableThreading
        {
            get
            {
                return ImageEngine.EnableThreading;
            }
            set
            {
                ImageEngine.EnableThreading = value;
                OnPropertyChanged(nameof(EnableThreading));
            }
        }

        public bool UseWindowsCodecs
        {
            get
            {
                return ImageEngine.WindowsWICCodecsAvailable;
            }
            set
            {
                ImageEngine.WindowsWICCodecsAvailable = value;
                OnPropertyChanged(nameof(UseWindowsCodecs));
            }
        }

        public int NumThreads
        {
            get
            {
                return ImageEngine.NumThreads;
            }
            set
            {
                if (value < 1 && value != -1) // Allowed to be -1 for being infinite
                    return;

                ImageEngine.NumThreads = value;
                OnPropertyChanged(nameof(NumThreads));

                if (NumThreads == 1)
                    EnableThreading = false;
            }
        }
        #endregion Settings Panel Properties

        #region Info Panel Properties
        bool infoPanelOpen = false;
        public bool InfoPanelOpen
        {
            get
            {
                return infoPanelOpen;
            }
            set
            {
                SetProperty(ref infoPanelOpen, value);
            }
        }


        string cpuName = "Unknown";
        public string CPUName
        {
            get
            {
                if (cpuName == "Unknown")
                {
                    var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                    List<object> testting = new List<object>();
                    foreach (var item in searcher.Get())
                    {
                        cpuName = item["Name"].ToString();
                        break;
                    }
                }

                return cpuName;
            }
        }

        public int NumCores
        {
            get
            {
                return System.Environment.ProcessorCount;
            }
        }

        public bool Is64Bit
        {
            get
            {
                return System.Environment.Is64BitOperatingSystem;
            }
        }

        public bool IsRunning64Bit
        {
            get
            {
                return System.Environment.Is64BitProcess;
            }
        }

        string osVersion = "Unknown";
        public string OSVersion
        {
            get
            {
                if (osVersion == "Unknown")
                {
                    var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                                select x.GetPropertyValue("Caption")).FirstOrDefault();
                    osVersion = name != null ? name.ToString() : "Unknown";
                }

                return osVersion;
            }
        }

        ulong ramSize = 0;
        public ulong RAMSize
        {
            get
            {
                if (ramSize == 0)
                {
                    var searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
                    foreach (var item in searcher.Get())
                        ramSize += (ulong)item["Capacity"];
                }

                return ramSize;
            }
        }

        string gpuName = "Unknown";
        public string GPUName
        {
            get
            {
                if (gpuName == "Unknown")
                {
                    var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
                    List<object> testting = new List<object>();
                    foreach (var item in searcher.Get())
                    {
                        gpuName = item["Name"].ToString();
                        break;
                    }
                }

                return gpuName;
            }
        }
        #endregion Info Panel Properties

        #region Commands
        CommandHandler closeCommand = null;
        public CommandHandler CloseCommand
        {
            get
            {
                if (closeCommand == null)
                    closeCommand = new CommandHandler(() => CloseImage(true));

                return closeCommand;
            }
        }

        CommandHandler saveCommand = null;
        public CommandHandler SaveCommand
        {
            get
            {
                if (saveCommand == null)
                    saveCommand = new CommandHandler(() =>
                    {
                        Task.Run(async () =>
                        {
                            Busy = true;
                            try
                            {
                                if (SplitChannels)
                                    LoadedImage.SplitChannels(SavePath);
                                else
                                    await LoadedImage.Save(SavePath, SaveFormatDetails, SaveMipType, removeAlpha: GeneralRemovingAlpha);
                            }
                            catch (Exception e)
                            {
                                SaveError = e.ToString();
                            }
                            SaveAttempted = true;
                            Busy = false;
                            SavePath = UsefulThings.General.FindValidNewFileName(SavePath);  // Ensure save path is pointing to a new valid filepath
                        });
                    });

                return saveCommand;

            }
        }
        #endregion Commands

        #region Merge Channels Properties
        bool mergePanelOpen = false;
        public bool MergeChannelsPanelOpen
        {
            get
            {
                return mergePanelOpen;
            }
            set
            {
                SetProperty(ref mergePanelOpen, value);
            }
        }

        public MTRangedObservableCollection<MergeChannelsImage> MergeChannelsImages { get; set; } = new MTRangedObservableCollection<MergeChannelsImage>();

        public bool MergeChannelsReady
        {
            get
            {
                if (MergeChannelsImages.Count == 0)
                    return false;

                var dimensions = MergeChannelsImages.Where(t => t.IsRed || t.IsAlpha || t.IsBlue || t.IsGreen);
                if (dimensions.Count() == 0)
                    return false;

                int width = dimensions.First().Width;
                int height = dimensions.First().Height;

                return dimensions.All(t => t.Width == width) && dimensions.All(t => t.Height == height);
            }
        }
        #endregion Merge Channels Properties

        #region General Properties
        byte windowBackground_Red = 0;
        public byte WindowBackground_Red
        {
            get
            {
                return windowBackground_Red;
            }
            set
            {
                SetProperty(ref windowBackground_Red, value);
                OnPropertyChanged(nameof(WindowBackgroundColour));
            }
        }

        byte windowBackground_Green = 0;
        public byte WindowBackground_Green
        {
            get
            {
                return windowBackground_Green;
            }
            set
            {
                SetProperty(ref windowBackground_Green, value);
                OnPropertyChanged(nameof(WindowBackgroundColour));
            }
        }

        byte windowBackground_Blue = 0;
        public byte WindowBackground_Blue
        {
            get
            {
                return windowBackground_Blue;
            }
            set
            {
                SetProperty(ref windowBackground_Blue, value);
                OnPropertyChanged(nameof(WindowBackgroundColour));
            }
        }

        byte windowBackground_Alpha = 158;
        public byte WindowBackground_Alpha
        {
            get
            {
                return windowBackground_Alpha;
            }
            set
            {
                SetProperty(ref windowBackground_Alpha, value);
                OnPropertyChanged(nameof(WindowBackgroundColour));
            }
        }

        public Brush WindowBackgroundColour
        {
            get
            {
                return new SolidColorBrush(Color.FromArgb(WindowBackground_Alpha, WindowBackground_Red, WindowBackground_Green, WindowBackground_Blue));
            }
        }

        bool splitChannels = false;
        public bool SplitChannels
        {
            get
            {
                return splitChannels;
            }
            set
            {
                SetProperty(ref splitChannels, value);
                if (value)
                    SaveFormat = ImageEngineFormat.PNG;
            }
        }

        bool busy = false;
        public bool Busy
        {
            get
            {
                return busy;
            }
            set
            {
                SetProperty(ref busy, value);
            }
        }

        #region Alpha and Colour related Properties
        bool GeneralRemovingAlpha
        {
            get
            {
                return SaveFormat == ImageEngineFormat.DDS_DXT1 ? DXT1AlphaRemove : RemoveGeneralAlpha;
            }
        }

        uint aMask = 0xFF000000;
        public uint AMask
        {
            get
            {
                return aMask;
            }
            set
            {
                SetProperty(ref aMask, value);
            }
        }

        uint rMask = 0x00FF0000;
        public uint RMask
        {
            get
            {
                return rMask;
            }
            set
            {
                SetProperty(ref rMask, value);
            }
        }

        uint gMask = 0x0000FF00;
        public uint GMask
        {
            get
            {
                return gMask;
            }
            set
            {
                SetProperty(ref gMask, value);
            }
        }

        uint bMask = 0x000000FF;
        public uint BMask
        {
            get
            {
                return bMask;
            }
            set
            {
                SetProperty(ref bMask, value);
            }
        }
        #endregion Alpha and Colour related Properties

        #region Bulk Convert Properties
        public MTRangedObservableCollection<string> BulkConvertFiles { get; set; } = new MTRangedObservableCollection<string>();
        public MTRangedObservableCollection<string> BulkConvertFailed { get; set; } = new MTRangedObservableCollection<string>();

        bool bulkConvertOpen = false;
        public bool BulkConvertOpen
        {
            get
            {
                return bulkConvertOpen;
            }
            set
            {
                SetProperty(ref bulkConvertOpen, value);
            }
        }

        bool bulkFolderBrowseRecurse = true;
        public bool BulkFolderBrowseRecurse
        {
            get
            {
                return bulkFolderBrowseRecurse;
            }
            set
            {
                SetProperty(ref bulkFolderBrowseRecurse, value);
            }
        }

        bool bulkConvertRunning = false;
        public bool BulkConvertRunning
        {
            get
            {
                return bulkConvertRunning;
            }
            set
            {
                SetProperty(ref bulkConvertRunning, value);
            }
        }

        bool bulkConvertFinished = false;
        public bool BulkConvertFinished
        {
            get
            {
                return bulkConvertFinished;
            }
            set
            {
                SetProperty(ref bulkConvertFinished, value);
            }
        }

        string bulkSaveFolder = "";  // Can't be null
        public string BulkSaveFolder
        {
            get
            {
                return bulkSaveFolder;
            }
            set
            {
                SetProperty(ref bulkSaveFolder, value);
            }
        }

        bool bulkUseSourceDestination = false;
        public bool BulkUseSourceDestination
        {
            get
            {
                return bulkUseSourceDestination;
            }
            set
            {
                SetProperty(ref bulkUseSourceDestination, value);
            }
        }

        string bulkStatus = "Ready";
        public string BulkStatus
        {
            get
            {
                return bulkStatus;
            }
            set
            {
                SetProperty(ref bulkStatus, value);
            }
        }

        int bulkProgressMax = 0;
        public int BulkProgressMax
        {
            get
            {
                return bulkProgressMax;
            }
            set
            {
                SetProperty(ref bulkProgressMax, value);
            }
        }

        int bulkProgressValue = 1;
        public int BulkProgressValue
        {
            get
            {
                return bulkProgressValue;
            }
            set
            {
                SetProperty(ref bulkProgressValue, value);
            }
        }
        #endregion Bulk Convert Properties

        #region Loaded Image Properties
        bool loadFailed = false;
        public bool LoadFailed
        {
            get
            {
                return loadFailed;
            }
            set
            {
                SetProperty(ref loadFailed, value);
            }
        }

        string loadFailError = null;
        public string LoadFailError
        {
            get
            {
                return loadFailError;
            }
            set
            {
                SetProperty(ref loadFailError, value);
            }
        }


        bool isImageLoaded = false;
        public bool IsImageLoaded
        {
            get
            {
                return isImageLoaded;
            }
            set
            {
                SetProperty(ref isImageLoaded, value);
            }
        }

        public int MipWidth
        {
            get
            {
                if (MipIndex == LoadedImage?.MipMaps.Count)
                    return 0;

                return LoadedImage?.MipMaps[MipIndex]?.Width ?? -1;
            }
        }

        public int MipHeight
        {
            get
            {
                if (MipIndex == LoadedImage?.MipMaps.Count)
                    return 0;

                return LoadedImage?.MipMaps[MipIndex]?.Height ?? -1;
            }
        }


        int mipIndex = 0;
        public int MipIndex
        {
            get
            {
                return mipIndex;
            }
            set
            {
                if (mipIndex == value)
                    return;

                SetProperty(ref mipIndex, value);
                OnPropertyChanged(nameof(MipWidth));
                OnPropertyChanged(nameof(MipHeight));
                if (IsImageLoaded)
                    UpdateLoadedPreview();
            }
        }

        ImageEngineImage loadedImage = null;
        public ImageEngineImage LoadedImage
        {
            get
            {
                return loadedImage;
            }
            set
            {
                SetProperty(ref loadedImage, value);
                OnPropertyChanged(nameof(IsImageLoaded));
            }
        }

        WriteableBitmap preview = null;
        public WriteableBitmap Preview
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


        string windowTitle = "Image Engine";
        public string WindowTitle
        {
            get
            {
                return windowTitle;
            }
            set
            {
                SetProperty(ref windowTitle, value);
            }
        }


        public string LoadedPath
        {
            get
            {
                return LoadedImage?.FilePath;
            }
        }

        public ImageEngineFormat LoadedFormat
        {
            get
            {
                return LoadedImage?.Format ?? ImageEngineFormat.Unknown;
            }
        }

        public int Width
        {
            get
            {
                return LoadedImage?.Width ?? -1;
            }
        }

        public int Height
        {
            get
            {
                return LoadedImage?.Height ?? -1;
            }
        }

        public int UncompressedSize
        {
            get
            {
                if (!IsImageLoaded)
                    return -1;

                return ImageFormats.GetUncompressedSize(LoadedImage.Width, LoadedImage.Height, LoadedImage.NumberOfChannels, LoadedImage.NumMipMaps > 1);
            }
        }

        public int LoadedCompressedSize
        {
            get
            {
                return LoadedImage?.CompressedSize ?? -1;
            }
        }

        public int MipCount
        {
            get
            {
                return LoadedImage?.NumMipMaps ?? -1;
            }
        }

        public string HeaderDetails
        {
            get
            {
                return LoadedImage?.Header?.ToString();
            }
        }

        AlphaDisplaySettings alphaDisplaySetting = 0;
        public AlphaDisplaySettings AlphaDisplaySetting
        {
            get
            {
                return alphaDisplaySetting;
            }
            set
            {
                SetProperty(ref alphaDisplaySetting, value);

                if (IsImageLoaded)
                {
                    UpdateLoadedPreview();

                    if (SavePanelOpen)
                        UpdateSavePreview(false); // Already have the image, just need to change some alpha bits around.
                }
            }
        }
        #endregion Loaded Image Properties

        #region Save Properties
        bool saveAttempted = false;
        public bool SaveAttempted
        {
            get
            {
                return saveAttempted;
            }
            set
            {
                SetProperty(ref saveAttempted, value);
            }
        }

        string saveError = null;
        public string SaveError
        {
            get
            {
                return saveError;
            }
            set
            {
                SetProperty(ref saveError, value);
            }
        }


        ImageEngineImage savePreviewIMG = null;
        WriteableBitmap savePreview = null;
        public WriteableBitmap SavePreview
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

        public bool IsSaveSmaller
        {
            get
            {
                return SaveCompressedSize < UncompressedSize;
            }
        }

        public double SaveCompressionRatio
        {
            get
            {
                var ratio = (1d * SaveCompressedSize) / UncompressedSize;
                if (ratio < 1)
                    ratio = 1 / ratio;

                return ratio * 100d;
            }
        }

        int saveCompressedSize = 0;
        public int SaveCompressedSize
        {
            get
            {
                if (SaveFormat.ToString().Contains("_") && IsImageLoaded)
                {
                    int estimatedMips = DDSGeneral.EstimateNumMipMaps(Width, Height);
                    var header = LoadedImage.Header as CSharpImageLibrary.Headers.DDS_Header;

                    return ImageFormats.GetCompressedSize(SaveMipType == MipHandling.KeepTopOnly || (SaveMipType == MipHandling.KeepExisting && MipCount == 1) ? 1 : estimatedMips, 
                        SaveFormatDetails, Width, Height);
                }

                return saveCompressedSize;
            }
            set
            {
                SetProperty(ref saveCompressedSize, value);
                OnPropertyChanged(nameof(SaveCompressionRatio));
                OnPropertyChanged(nameof(IsSaveSmaller));
            }
        }

        public string DefaultSavePath
        {
            get
            {
                if (SaveFormatDetails == null)
                    return null;

                string name = null;
                if (LoadedImage?.FilePath == null)
                    name = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"ImageEngine_{SaveFormat}.{SaveFormatDetails.Extension}");
                else
                    name = $"{UsefulThings.General.GetFullPathWithoutExtension(LoadedImage.FilePath)}.{SaveFormatDetails.Extension}";

                return UsefulThings.General.FindValidNewFileName(name);
            }
        }

        string savePath = null;
        public string SavePath
        {
            get
            {
                return savePath ?? DefaultSavePath;
            }
            set
            {
                SetProperty(ref savePath, value);
                FixExtension();
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
                if (ImageFormats.SaveUnsupported.Contains(value))
                    return;

                bool changed = value != saveFormat;

                SetProperty(ref saveFormat, value);

                SaveFormatDetails = new ImageFormats.ImageEngineFormatDetails(SaveFormat);

                OnPropertyChanged(nameof(SaveCompressedSize));
                OnPropertyChanged(nameof(SaveCompressionRatio));
                OnPropertyChanged(nameof(IsSaveFormatMippable));
                OnPropertyChanged(nameof(IsSaveSmaller));

                if (SavePath == null)
                    return;

                // Test paths without extensions
                if (SavePath.Substring(0, SavePath.LastIndexOf('.')) == DefaultSavePath.Substring(0, DefaultSavePath.LastIndexOf('.')))
                    SavePath = DefaultSavePath;

                // Change extension as required
                FixExtension();

                // Ensure SavePath doesn't already exist
                SavePath = UsefulThings.General.FindValidNewFileName(SavePath);

                // Regenerate save preview
                if (changed && SavePanelOpen)
                    UpdateSavePreview();
            }
        }

        MipHandling saveMipType = MipHandling.Default;
        public MipHandling SaveMipType
        {
            get
            {
                return saveMipType;
            }
            set
            {
                SetProperty(ref saveMipType, value);
                OnPropertyChanged(nameof(SaveCompressedSize));
                OnPropertyChanged(nameof(SaveCompressionRatio));
                OnPropertyChanged(nameof(IsSaveSmaller));
            }
        }

        bool removeGeneralAlpha = false;
        public bool RemoveGeneralAlpha
        {
            get
            {
                return removeGeneralAlpha;
            }
            set
            {
                SetProperty(ref removeGeneralAlpha, value);
                OnPropertyChanged(nameof(SaveCompressedSize));
                OnPropertyChanged(nameof(SaveCompressionRatio));
                OnPropertyChanged(nameof(IsSaveSmaller));

                
                if (SavePanelOpen)
                    UpdateSavePreview();
            }
        }

        public bool IsSaveFormatMippable
        {
            get
            {
                return SaveFormatDetails?.IsMippable ?? false;
            }
        }


        DispatcherTimer SliderTimer = new DispatcherTimer();

        public double DXT1AlphaThreshold
        {
            get
            {
                return CSharpImageLibrary.DDS.DDSGeneral.DXT1AlphaThreshold;
            }
            set
            {
                SetProperty(ref CSharpImageLibrary.DDS.DDSGeneral.DXT1AlphaThreshold, value);

                // Update Save Preview - Not every change though (that could be a bunch in 1 second).
                SliderTimer.Stop();
                SliderTimer.Start();
            }
        }

        bool dxt1AlphaRemove = true;
        public bool DXT1AlphaRemove
        {
            get
            {
                return dxt1AlphaRemove;
            }
            set
            {
                SetProperty(ref dxt1AlphaRemove, value);

                if (SavePanelOpen)
                    UpdateSavePreview();
            }
        }

        public int JPG_CompressionSetting
        {
            get
            {
                return WIC_Codecs.JPGCompressionSetting;
            }
            set
            {
                SetProperty(ref CSharpImageLibrary.WIC_Codecs.JPGCompressionSetting, value);

                // Update Save Preview - Not every change though (that could be a bunch in 1 second).
                SliderTimer.Stop();
                SliderTimer.Start();
            }
        }

        bool savePanelOpen = false;
        public bool SavePanelOpen
        {
            get
            {
                return savePanelOpen;
            }
            set
            {
                SetProperty(ref savePanelOpen, value);
                if (value)
                    OnPropertyChanged(nameof(UncompressedSize));
            }
        }
        #endregion Save Properties
        #endregion General Properties


        ImageFormats.ImageEngineFormatDetails SaveFormatDetails = null;


        public NewViewModel() : base()
        {
            // Space out the Output Window a bit
            Trace.WriteLine("");
            Trace.WriteLine("");

            SliderTimer.Interval = TimeSpan.FromSeconds(1);
            SliderTimer.Tick += (arg, arg2) =>
            {
                UpdateSavePreview();
                SliderTimer.Stop();
            };

            MergeChannelsImages.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(MergeChannelsReady));

            // Set background colour
            WindowBackground_Alpha = Properties.Settings.Default.BackgroundAlpha;
            WindowBackground_Red = Properties.Settings.Default.BackgroundRed;
            WindowBackground_Green = Properties.Settings.Default.BackgroundGreen;
            WindowBackground_Blue = Properties.Settings.Default.BackgroundBlue;
        }

        internal async Task LoadImage(string path)
        {
            timer.Restart();
            byte[] bytes = null;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"IO failure when reading image from disk: {e.Message}");
                LoadFailError = $"IO failure.{Environment.NewLine} {e.Message}";
                LoadFailed = true;
                return;
            }

            Trace.WriteLine($"File read took: {timer.ElapsedMilliseconds}");

            bool success = await LoadImage(bytes);
            if (!success)
                return;

            LoadedImage.FilePath = path;
            SavePath = DefaultSavePath;
            OnPropertyChanged(nameof(LoadedPath));
        }

        internal async Task<bool> LoadImage(byte[] data)
        {
            LoadFailed = false;

            if (!timer.IsRunning)
                timer.Restart();

            CloseImage(false); // Don't need to update the UI here, it'll get updated after loading the image. But do need to reset some things
            WindowTitle = "Image Engine - View";

            // Full image
            try
            {
                LoadedImage = await Task.Run(() => new ImageEngineImage(data));
            }
            catch(Exception e)
            {
                LoadFailError = e.Message;
                LoadFailed = true;
                return false;
            }

            Trace.WriteLine($"Loading of {LoadedFormat} ({Width}x{Height}, {(MipCount > 1 ? "Mips Present" : "No Mips")}) = {timer.ElapsedMilliseconds}ms.");
            timer.Restart();
            UpdateLoadedPreview(true);

            SaveFormat = LoadedFormat;

            timer.Stop();
            Trace.WriteLine($"Preview of {LoadedFormat} ({Width}x{Height}, {(MipCount > 1 ? "Mips Present" : "No Mips")}) = {timer.ElapsedMilliseconds}ms.");

            IsImageLoaded = true;
            return true;
        }

        void UpdateUI()
        {
            // Update UI
            OnPropertyChanged(nameof(LoadedFormat));
            OnPropertyChanged(nameof(LoadedPath));
            OnPropertyChanged(nameof(LoadedCompressedSize));
            OnPropertyChanged(nameof(UncompressedSize));
            OnPropertyChanged(nameof(HeaderDetails));
            OnPropertyChanged(nameof(Height));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(MipCount));
        }

        public void FixExtension(bool indicateSavePathPropertyChanged = false)
        {
            if (SavePath == null || ImageFormats.SaveUnsupported.Contains(SaveFormat))
                return;

            string requiredExtension = "." + SaveFormatDetails.Extension;

            var currentExt = Path.GetExtension(savePath);
            if (currentExt == "")  // No extension
                SavePath += requiredExtension;
            else if (currentExt != requiredExtension)  // Existing extension
                SavePath = Path.ChangeExtension(SavePath, requiredExtension);

            if (indicateSavePathPropertyChanged)
                OnPropertyChanged(nameof(SavePath));
        }

        void CloseImage(bool updateUI)
        {
            // Clear things - should close panels when this happens
            LoadedImage = null;
            IsImageLoaded = false;
            SavePath = null;
            SaveError = null;
            SaveAttempted = false;
            MipIndex = 0;
            WindowTitle = "Image Engine";
            RemoveGeneralAlpha = false; // Other alpha settings not reset because they're specific, but this one spans formats.
            BulkProgressValue = 0;
            BulkProgressMax = 0;
            BulkStatus = "Ready";
            BulkConvertFinished = false;
            BulkConvertRunning = false;
            BulkConvertFiles.Clear();
            BulkConvertFailed.Clear();
            MergeChannelsImages.Clear();
            LoadFailed = false;
            LoadFailError = null;
            previousAlphaSetting = AlphaDisplaySettings.PremultiplyAlpha;
            SavePanelOpen = false;

            // Notify
            if (updateUI)
                UpdateUI();
        }

        internal void MergeLoad(IEnumerable<string> files)
        {
            // Determine which channels exist
            bool checkRed = !MergeChannelsImages.Any(t => t.IsRed);
            bool checkBlue = !MergeChannelsImages.Any(t => t.IsBlue);
            bool checkGreen = !MergeChannelsImages.Any(t => t.IsGreen);
            bool checkAlpha = !MergeChannelsImages.Any(t => t.IsAlpha);

            var newFiles = files.ToList();
            var newImages = new MergeChannelsImage[newFiles.Count];

            var action = new Action<int>(index => newImages[index] = new MergeChannelsImage(newFiles[index]));

            if (ImageEngine.EnableThreading)
                Parallel.For(0, newFiles.Count, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, action);
            else
                for (int i = 0; i < newFiles.Count; i++)
                    action(i);

            // Attempt to determine channel type from filename
            foreach (var img in newImages)
            {
                if (checkRed && img.DisplayName.EndsWith("_R", StringComparison.OrdinalIgnoreCase))
                {
                    img.IsRed = true;
                    checkRed = false;
                }
                else if (checkBlue && img.DisplayName.EndsWith("_B", StringComparison.OrdinalIgnoreCase))
                {
                    img.IsBlue = true;
                    checkBlue = false;
                }
                else if (checkGreen && img.DisplayName.EndsWith("_G", StringComparison.OrdinalIgnoreCase))
                {
                    img.IsGreen = true;
                    checkGreen = false;
                }
                else if (checkAlpha && img.DisplayName.EndsWith("_A", StringComparison.OrdinalIgnoreCase))
                {
                    img.IsAlpha = true;
                    checkAlpha = false;
                }
            }

            // Determine channel type by file order if required
            foreach (var img in newImages)
            {
                if (checkRed && !img.HasAssignedChannel)
                {
                    img.IsRed = true;
                    checkRed = false;
                }
                else if (checkBlue && !img.HasAssignedChannel)
                {
                    img.IsBlue = true;
                    checkBlue = false;
                }
                else if (checkGreen && !img.HasAssignedChannel)
                {
                    img.IsGreen = true;
                    checkGreen = false;
                }
                else if (checkAlpha && !img.HasAssignedChannel)
                {
                    img.IsAlpha = true;
                    checkAlpha = false;
                }
            }

            MergeChannelsImages.AddRange(newImages);
        }

        internal async Task MergeChannels()
        {
            // Get all channels
            var red = MergeChannelsImages.FirstOrDefault(t => t.IsRed);
            var blue = MergeChannelsImages.FirstOrDefault(t => t.IsBlue);
            var green = MergeChannelsImages.FirstOrDefault(t => t.IsGreen);
            var alpha = MergeChannelsImages.FirstOrDefault(t => t.IsAlpha);

            CloseImage(true);

            LoadedImage = await Task.Run(() => ImageEngine.MergeChannels(blue, green, red, alpha));

            LoadFailed = false;
            UpdateLoadedPreview(true);
            SaveFormat = ImageEngineFormat.PNG;
            MergeChannelsPanelOpen = false;
        }

        internal async Task DoBulkConvert()
        {
            BulkProgressMax = BulkConvertFiles.Count;
            BulkProgressValue = 0;
            BulkStatus = $"Converting {BulkProgressValue}/{BulkProgressMax} images.";
            BulkConvertFinished = false;
            BulkConvertRunning = true;

            Progress<int> progressReporter = new Progress<int>(index =>
            {
                BulkProgressValue++;
                BulkStatus = $"Converting {BulkProgressValue}/{BulkProgressMax} images.";
            });

            BulkConvertFailed.AddRange(await Task.Run(() => ImageEngine.BulkConvert(BulkConvertFiles, SaveFormatDetails, UseSourceFormatForSaving, BulkSaveFolder, SaveMipType, BulkUseSourceDestination, GeneralRemovingAlpha, progressReporter)));

            BulkStatus = "Conversion complete! ";
            if (BulkConvertFailed.Count > 0)
                BulkStatus += $"{BulkConvertFailed.Count} failed to convert.";

            BulkProgressValue = BulkProgressMax;
            BulkConvertFinished = true;
            BulkConvertRunning = false;
        }

        /// <summary>
        /// Sets visibility on a channel.
        /// </summary>
        /// <param name="channelValue">True = channel is enabled.</param>
        /// <param name="channel">0 = Blue, 1 = Green, 2 = Red.</param>
        unsafe void SetChannel(WriteableBitmap bmp, bool channelValue, int channel)
        {
            if (bmp == null)
                return;

            bmp.Lock();

            byte* ptr = (byte*)bmp.BackBuffer.ToPointer() + channel;

            byte[] pixels = null;
            if (channelValue)
            {
                MipMap current = LoadedImage.MipMaps[MipIndex];
                pixels = ImageEngine.GetPixelsAsBGRA32(current.Width, current.Height, current.Pixels, current.LoadedFormatDetails);

                for (int i = channel; i < pixels.Length; i += 4)
                {
                    *ptr = pixels[i];
                    ptr += 4;
                }
            }
            else
            {
                for (int i = channel; i < bmp.PixelWidth * bmp.PixelHeight * 4; i += 4)
                {
                    *ptr = 0;
                    ptr += 4;
                }
            }

            bmp.AddDirtyRect(new System.Windows.Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
            bmp.Unlock();
        }

        void UpdateLoadedPreview(bool forceRedraw = false)
        {
            if (MipIndex >= loadedImage.MipMaps.Count)   // Easy way out of handling indicies - Max is bound to mipcount e.g 12, but 0 based index, hence 12 = 13th value, out of range.
                return;

            var mip = LoadedImage.MipMaps[MipIndex];

            UpdatePreview(ref preview, mip.Width, mip.Height, mip.Pixels, mip.LoadedFormatDetails, forceRedraw);

            OnPropertyChanged(nameof(Preview));
            UpdateUI();
        }

        AlphaDisplaySettings previousAlphaSetting = AlphaDisplaySettings.PremultiplyAlpha;
        unsafe void UpdatePreview(ref WriteableBitmap bmp, int width, int height, byte[] pixels, ImageFormats.ImageEngineFormatDetails formatDetails, bool forceRedraw)
        {
            var rect = new System.Windows.Int32Rect(0, 0, width, height);

            byte[] tempPixels = pixels;
            if (formatDetails.ComponentSize != 1)
                tempPixels = ImageEngine.GetPixelsAsBGRA32(width, height, pixels, formatDetails);

            // Create new BitmapImage if necessary
            if (bmp == null || width != bmp.PixelWidth || height != bmp.PixelHeight)
            {
                bmp = UsefulThings.WPF.Images.CreateWriteableBitmap(tempPixels, width, height);
                forceRedraw = false;  // Don't want to redraw again after having just created it.
                previousAlphaSetting = AlphaDisplaySettings.PremultiplyAlpha;    // Need for forcing a redraw without messing up the UI.
            }

            // Same as in 'if' below, just needs to be able to continue on.
            if (forceRedraw)
            {
                bmp.WritePixels(rect, tempPixels, bmp.BackBufferStride, 0);
                previousAlphaSetting = AlphaDisplaySettings.PremultiplyAlpha;  // Need for forcing a redraw without messing up the UI.
            }

            // Change alpha display as necessary
            if (previousAlphaSetting == AlphaDisplaySettings.AlphaOnly && AlphaDisplaySetting == AlphaDisplaySettings.PremultiplyAlpha)
                bmp.WritePixels(rect, tempPixels, bmp.BackBufferStride, 0);
            else if (previousAlphaSetting == AlphaDisplaySettings.AlphaOnly && AlphaDisplaySetting == AlphaDisplaySettings.NoAlpha)
            {
                bmp.WritePixels(rect, tempPixels, bmp.BackBufferStride, 0);
                RemoveAlphaFromDisplay(bmp, tempPixels, rect);
            }
            else if (AlphaDisplaySetting == AlphaDisplaySettings.AlphaOnly)
            {
                bmp.Lock();

                byte* back = (byte*)bmp.BackBuffer.ToPointer();
                for (int ai = 3; ai < tempPixels.Length; ai += 4)
                {
                    *back++ = tempPixels[ai];
                    *back++ = tempPixels[ai];
                    *back++ = tempPixels[ai];
                    *back++ = 255;  // Alpha opaque
                }

                bmp.AddDirtyRect(rect);
                bmp.Unlock();
            }
            else if (previousAlphaSetting == AlphaDisplaySettings.NoAlpha && AlphaDisplaySetting == AlphaDisplaySettings.PremultiplyAlpha)
            {
                bmp.Lock();

                byte* back = (byte*)bmp.BackBuffer.ToPointer() + 3;
                for (int i = 3; i < tempPixels.Length; i += 4)
                {
                    *back = tempPixels[i];
                    back += 4;
                }

                bmp.AddDirtyRect(rect);
                bmp.Unlock();

            }
            else if (previousAlphaSetting == AlphaDisplaySettings.PremultiplyAlpha && AlphaDisplaySetting == AlphaDisplaySettings.NoAlpha)
                RemoveAlphaFromDisplay(bmp, tempPixels, rect);

            previousAlphaSetting = AlphaDisplaySetting;
        }

        unsafe void RemoveAlphaFromDisplay(WriteableBitmap bmp, byte[] tempPixels, System.Windows.Int32Rect rect)
        {
            bmp.Lock();

            byte* back = (byte*)bmp.BackBuffer.ToPointer() + 3;
            for (int i = 3; i < tempPixels.Length; i += 4)
            {
                *back = 255;
                back += 4;
            }

            bmp.AddDirtyRect(rect);
            bmp.Unlock();
        }

        public async Task UpdateSavePreview(bool needRegenerate = true)
        {
            if (!IsImageLoaded)
                return;


            timer.Restart();
            if (needRegenerate)
                await Task.Run(() =>
                {
                    // Save and reload to give accurate depiction of what it'll look like when saved.
                    byte[] data = LoadedImage.Save(SaveFormatDetails, MipHandling.KeepTopOnly, removeAlpha: GeneralRemovingAlpha);
                    SaveCompressedSize = data.Length;
                    savePreviewIMG = new ImageEngineImage(data);
                });

            Trace.WriteLine($"Saving of {SaveFormat} ({Width}x{Height}, No Mips) = {timer.ElapsedMilliseconds}ms.");
            timer.Restart();

            UpdatePreview(ref savePreview, savePreviewIMG.Width, savePreviewIMG.Height, savePreviewIMG.MipMaps[0].Pixels, SaveFormatDetails, true);

            // Update Properties
            OnPropertyChanged(nameof(SavePreview));

            timer.Stop();
            Trace.WriteLine($"Save preview of {SaveFormat} ({Width}x{Height}, No Mips) = {timer.ElapsedMilliseconds}ms.");
        }
    }
}
