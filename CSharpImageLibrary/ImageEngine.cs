using CSharpImageLibrary.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides main image functions
    /// </summary>
    public static class ImageEngine
    {
        /// <summary>
        /// True = Windows WIC Codecs are present (8+)
        /// </summary>
        public static bool WindowsWICCodecsAvailable
        {
            get; private set;
        }

        

        /// <summary>
        /// Constructor. Checks WIC status before any other operation.
        /// </summary>
        static ImageEngine()
        {
            WindowsWICCodecsAvailable = Win8_10.WindowsCodecsPresent();
        }


        #region Loading
        /// <summary>
        /// Loads useful information from an image file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Format">Detected image format.</param>
        /// <returns>Raw pixel data as stream.</returns>
        public static MemoryTributary LoadImage(string imagePath, out double Width, out double Height, out Format Format)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadImage(fs, out Width, out Height, out Format);
        }


        /// <summary>
        /// Loads image from image stream.
        /// </summary>
        /// <param name="stream">Stream containing entire file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Format">Image format (dds surfaces, jpg, png, etc)</param>
        /// <param name="extension">Extension of original file. Leave null to guess.</param>
        /// <returns>RGBA pixels.</returns>
        public static MemoryTributary LoadImage(Stream stream, out double Width, out double Height, out Format Format, string extension = null)
        {
            Width = 0;
            Height = 0;
            Format = new Format();

            // KFreon: See if image is built-in codec agnostic.
            Format test = ImageFormats.ParseFormat(stream, extension);

            switch (test.InternalFormat)
            {
                case ImageEngineFormat.DDS_V8U8:
                    return V8U8.Load(stream, out Width, out Height);
                case ImageEngineFormat.DDS_G8_L8:
                    return G8_L8.Load(stream, out Width, out Height);
                case ImageEngineFormat.DDS_ATI1:
                    return BC4_ATI1.Load(stream, out Width, out Height);
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    return BC5_ATI2_3Dc.Load(stream, out Width, out Height);
                case ImageEngineFormat.DDS_ARGB:
                    return RGBA.Load(stream, out Width, out Height);
            }

            // KFreon: NOT any of the above then...

            // KFreon: Try loading with built in codecs
            if (WindowsWICCodecsAvailable)
                return Win8_10.LoadWithCodecs(stream, out Width, out Height);
            else
            {
                // KFreon: Handle DXT formats - all other DDS formats are above
                switch (Format.InternalFormat)
                {
                    case ImageEngineFormat.DDS_DXT1:
                        return BC1.Load(stream, out Width, out Height);
                    case ImageEngineFormat.DDS_DXT2:
                    case ImageEngineFormat.DDS_DXT3:
                        return BC2.Load(stream, out Width, out Height);
                    case ImageEngineFormat.DDS_DXT4:
                    case ImageEngineFormat.DDS_DXT5:
                        return BC3.Load(stream, out Width, out Height);
                }

                // KFreon: None of those, so assume standard image formats (jpg, png, etc)
                return Win7.LoadImageWithCodecs(stream, out Width, out Height);
            }
        }
        #endregion Loading


        public static bool Save(Stream PixelData, ImageEngineFormat format, Stream destination)
        {
            // KFreon: Try DDS formats first
            switch (format)
            {
                case ImageEngineFormat.DDS_V8U8:
                    return V8U8.Save(PixelData, destination);
                case ImageEngineFormat.DDS_G8_L8:
                    return G8_L8.Save(PixelData, destination);
                case ImageEngineFormat.DDS_ATI1:
                    return BC4_ATI1.Save(PixelData, destination);
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    return BC5_ATI2_3Dc.Save(PixelData, destination);
                case ImageEngineFormat.DDS_ARGB:
                    return RGBA.Save(PixelData, destination);
                case ImageEngineFormat.DDS_DXT1:
                    return BC1.Save(PixelData, destination);
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                    return BC2.Save(PixelData, destination);
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    return BC3.Save(PixelData, destination);
            }

            // KFreon: NOT any of the above then...

            // KFreon: Try saving with built in codecs
            if (WindowsWICCodecsAvailable)
                return Win8_10.SaveWithCodecs(PixelData, destination);
            else
                return Win7.SaveWithCodecs(PixelData, destination);
        }
    }
}
