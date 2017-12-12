using CSharpImageLibrary;
using CSharpImageLibrary.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CSharpImageLibrary.ImageFormats;

namespace UWP_UI_Project.Models
{
    public static class ImageEngine
    {
        public static async Task<List<MipMapBase>> LoadImage(Stream imageStream, AbstractHeader header, int maxDimension, double scale, ImageEngineFormatDetails formatDetails)
        {
            int decodeWidth = header.Width > header.Height ? maxDimension : 0;
            int decodeHeight = header.Width < header.Height ? maxDimension : 0;

            switch (formatDetails.SurfaceFormat)
            {
                case ImageEngineFormat.DDS_DXT1:
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                case ImageEngineFormat.GIF:
                case ImageEngineFormat.JPG:
                case ImageEngineFormat.PNG:
                case ImageEngineFormat.BMP:
                case ImageEngineFormat.TIF:
                    return await UWP_Codecs.LoadFromStream(imageStream, (uint)decodeWidth, (uint)decodeHeight, scale, formatDetails);
                default:
                    throw new FileLoadException("Format unknown.");
            }
        }
    }
}
