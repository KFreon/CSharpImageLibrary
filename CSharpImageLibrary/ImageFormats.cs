using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CSharpImageLibrary.DDSGeneral;

namespace CSharpImageLibrary
{
    /// <summary>
    /// File extensions supported. Used to get initial format.
    /// </summary>
    public enum SupportedExtensions
    {
        UNKNOWN, JPG, JPEG, BMP, PNG, DDS
    }


    /// <summary>
    /// Indicates image format.
    /// Use FORMAT struct.
    /// </summary>
    public enum ImageEngineFormat
    {
        Unknown = 1,
        JPG = 2,
        PNG = 3,
        BMP = 4,
        DDS_DXT1 = 0x31545844,  // 1TXD i.e. DXT1 backwards
        DDS_DXT3 = 0x33545844,
        DDS_DXT5 = 0x35545844,
        DDS_ARGB = 6,  // No specific value apparently
        DDS_ATI1 = 0x31495441,  // ATI1 backwards
        DDS_V8U8 = 117,  // Doesn't seem like this value is used much - but it's in the programming guide for DDS by Microsoft
        DDS_G8_L8 = 7,  // No specific value it seems
        DDS_ATI2_3Dc = 0x32495441  // ATI2 backwards
    }

    /// <summary>
    /// Indicates image format and whether it's a mippable format or not.
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public struct Format
    {
        /// <summary>
        /// Image format
        /// </summary>
        public ImageEngineFormat InternalFormat;

        /// <summary>
        /// True = can have mipmaps.
        /// </summary>
        public bool IsMippable
        {
            get
            {
                return InternalFormat.ToString().Contains("DDS");  // KFreon: Of the supported formats, only DDS' are mippable.
            }
        }

        /// <summary>
        /// Initialises a Format with an image format.
        /// </summary>
        /// <param name="format">Image format</param>
        public Format(ImageEngineFormat format)
        {
            InternalFormat = format;
        }

        /// <summary>
        /// Displays useful information about state of object.
        /// </summary>
        /// <returns>More useful description of object.</returns>
        public override string ToString()
        {
            return $"Format: {InternalFormat}  IsMippable: {IsMippable}";
        }
    }

    /// <summary>
    /// Provides format functionality
    /// </summary>
    public static class ImageFormats
    {
        /// <summary>
        /// Converts a DDS FourCC to a Format.
        /// </summary>
        /// <param name="FourCC">DDS FourCC to check.</param>
        /// <returns></returns>
        public static Format ParseFourCC(int FourCC)
        {
            Format format = new Format();

            if (!Enum.IsDefined(typeof(ImageEngineFormat), FourCC))
                format.InternalFormat = ImageEngineFormat.DDS_ARGB; 
            else
                format.InternalFormat = (ImageEngineFormat)FourCC;

            return format;
        }


        public static Format ParseFormat(Stream imgData, SupportedExtensions ext)
        {
            switch (ext)
            {
                case SupportedExtensions.BMP:
                    return new Format(ImageEngineFormat.BMP);
                case SupportedExtensions.DDS:
                    DDS_HEADER header;
                    return ParseDDSFormat(imgData, out header);
                case SupportedExtensions.JPEG:
                case SupportedExtensions.JPG:
                    return new Format(ImageEngineFormat.JPG);
                case SupportedExtensions.PNG:
                    return new Format(ImageEngineFormat.PNG);
            }

            return new Format();
        }

        public static SupportedExtensions ParseExtension(string extension)
        {
            SupportedExtensions ext = SupportedExtensions.DDS;
            string tempext = Path.GetExtension(extension).Replace(".", "");
            if (!Enum.TryParse(tempext, true, out ext))
                return SupportedExtensions.UNKNOWN;

            return ext;
        }

        public static Format ParseFormat(string imagePath)
        {
            SupportedExtensions ext = ParseExtension(imagePath);

            using (FileStream fs = new FileStream(imagePath, FileMode.Open))
                return ParseFormat(fs, ext);
        }
    }
}
