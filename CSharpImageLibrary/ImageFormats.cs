using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary
{
    public enum SupportedExtensions
    {
        UNKNOWN, JPG, JPEG, BMP, PNG, DDS
    }

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
        DDS_ATI1N_BC4 = 0x55344342,  // BC4U backwards
        DDS_V8U8 = 117,  // Doesn't seem like this value is used much - but it's in the programming guide for DDS by Microsoft
        DDS_G8_L8 = 7,  // No specific value it seems
        DDS_ATI2_3Dc = 0x32495441  // ATI2 backwards
    }

    [DebuggerDisplay("{ToString()}")]
    public struct Format
    {
        public ImageEngineFormat InternalFormat;
        public bool IsMippable
        {
            get
            {
                return InternalFormat.ToString().Contains("DDS");
            }
        }

        public Format(ImageEngineFormat format)
        {
            InternalFormat = format;
        }

        public override string ToString()
        {
            return $"Format: {InternalFormat}  IsMippable: {IsMippable}";
        }
    }

    public static class ImageFormats
    {
        public static Format ParseFourCC(int FourCC)
        {
            Format format = new Format();

            if (!Enum.IsDefined(typeof(ImageEngineFormat), FourCC))
                format.InternalFormat = ImageEngineFormat.DDS_ARGB;  // KFreon: NEEDS TO SEE if its JPG etc
            else
                format.InternalFormat = (ImageEngineFormat)FourCC;

            return format;
        }
    }
}
