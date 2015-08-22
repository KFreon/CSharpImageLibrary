using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary
{
    public enum ImageEngineFormat
    {
        Unknown, JPG, PNG, BMP,
        DDS_DXT1 = 0x31545844,  // 1TXD i.e. DXT1 backwards
        DDS_DXT3 = 0x33545844,
        DDS_DXT5 = 0x35545844,
        DDS_ARGB = 6,
        DDS_ATI1N_BC4 = 0x55344342,  // BC4U backwards
        DDS_V8U8 = 117,
        DDS_G8_L8 = 7,
        DDS_ATI2_3Dc = 0x32495441  // ATI2 backwards
    }

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
                format.InternalFormat = ImageEngineFormat.Unknown;  // KFreon: NEEDS TO SEE if its JPG etc
            else
                format.InternalFormat = (ImageEngineFormat)FourCC;

            return format;
        }
    }
}
