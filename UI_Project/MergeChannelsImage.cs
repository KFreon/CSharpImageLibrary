using CSharpImageLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UsefulThings.WPF;

namespace UI_Project
{
    public class MergeChannelsImage : ViewModelBase
    {
        #region Properties
        public byte[] Pixels { get; private set; }
        public string FilePath { get; private set; }

        public string DisplayName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(FilePath);
            }
        }

        bool isRed = false;
        public bool IsRed
        {
            get
            {
                return isRed;
            }
            set
            {
                SetProperty(ref isRed, value);
            }
        }

        bool isGreen = false;
        public bool IsGreen
        {
            get
            {
                return isGreen;
            }
            set
            {
                SetProperty(ref isGreen, value);
            }
        }

        bool isBlue = false;
        public bool IsBlue
        {
            get
            {
                return isBlue;
            }
            set
            {
                SetProperty(ref isBlue, value);
            }
        }

        bool isAlpha = false;
        public bool IsAlpha
        {
            get
            {
                return isAlpha;
            }
            set
            {
                SetProperty(ref isAlpha, value);
            }
        }

        public BitmapSource Thumbnail { get; private set; }

        public int Height { get; private set; }
        public int Width { get; private set; }
        #endregion Properties


        public MergeChannelsImage(string mainPath)
        {
            FilePath = mainPath;

            using (var img = new ImageEngineImage(mainPath))
            {
                Thumbnail = img.GetWPFBitmap(128);
                Width = img.Width;
                Height = img.Height;
                Pixels = img.MipMaps[0].Pixels;
            }
        }
    }
}
