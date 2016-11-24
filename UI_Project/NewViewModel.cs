using CSharpImageLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsefulThings.WPF;

namespace UI_Project
{
    public class NewViewModel : ViewModelBase
    {
        #region Loaded Image Properties
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
                return LoadedImage?.Width * LoadedImage?.Height * 4 ?? -1;
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

        int alphaDisplaySetting = 0;
        public int AlphaDisplaySetting
        {
            get
            {
                return alphaDisplaySetting;
            }
            set
            {
                SetProperty(ref alphaDisplaySetting, value);
            }
        }
        #endregion Loaded Image Properties

        #region Save Properties
        public int SaveCompressedSize
        {
            get
            {
                return ImageFormats.GetCompressedSize(SaveFormat, Width, Height, SaveMipType == MipHandling.KeepTopOnly ? 1 : MipCount);
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
            }
        }

        public int DXT1AlphaThreshold
        {
            get
            {
                return (int)(CSharpImageLibrary.DDS.DDSGeneral.DXT1AlphaThreshold * 100f);
            }
            set
            {
                SetProperty(ref CSharpImageLibrary.DDS.DDSGeneral.DXT1AlphaThreshold, value / 100f);
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
            }
        }
        #endregion Save Properties


        public NewViewModel() : base()
        {

        }

        internal async Task LoadImage(string path)
        {
            // Full image
            var fullLoad = Task.Run(() => new ImageEngineImage(path));
            

            // Quick previews
            LoadedImage = await Task.Run(() => new ImageEngineImage(path, 512));
            UpdatePreview(LoadedImage.MipMaps[0]);
        }

        internal async Task LoadImage(byte[] data)
        {
            // Full image
            var fullLoad = Task.Run(() => new ImageEngineImage(data));


            // Quick previews
            LoadedImage = await Task.Run(() => new ImageEngineImage(data, 512));
            UpdatePreview(LoadedImage.MipMaps[0]);
        }

        void UpdatePreview(MipMap mip)
        {
            // Create Preview Object if required
            if (Preview == null || (Preview.PixelHeight != mip.Height || Preview.PixelWidth != mip.Width))
                Preview = UsefulThings.WPF.Images.CreateWriteableBitmap(mip.Pixels, mip.Width, mip.Height);
            else
            {
                var rect = new System.Windows.Int32Rect(0, 0, mip.Width, mip.Height);
                preview.WritePixels(rect, mip.Pixels, mip.Width * 4, 0);
                Preview.AddDirtyRect(rect);
                OnPropertyChanged(nameof(Preview));
            }

            UpdateUI();
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
            OnPropertyChanged(nameof(UncompressedSize));
        }
    }
}
