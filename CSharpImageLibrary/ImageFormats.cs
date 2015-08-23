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
        UNKNOWN,
        JPG,
        JPEG,
        BMP,
        PNG,
        DDS
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
        /// <returns>Format specified by FourCC. Otherwise ARGB.</returns>
        public static Format ParseFourCC(int FourCC)
        {
            Format format = new Format();

            if (!Enum.IsDefined(typeof(ImageEngineFormat), FourCC))
                format.InternalFormat = ImageEngineFormat.DDS_ARGB; 
            else
                format.InternalFormat = (ImageEngineFormat)FourCC;

            return format;
        }


        /// <summary>
        /// Gets image format from stream containing image file, along with extension of image file.
        /// </summary>
        /// <param name="imgData">Stream containing entire image file. NOT just pixels.</param>
        /// <param name="ext">File extension of image.</param>
        /// <returns>Format of image.</returns>
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


        /// <summary>
        /// Gets file extension from string of extension.
        /// </summary>
        /// <param name="extension">String containing file extension.</param>
        /// <returns>SupportedExtension of extension.</returns>
        public static SupportedExtensions ParseExtension(string extension)
        {
            SupportedExtensions ext = SupportedExtensions.DDS;
            string tempext = Path.GetExtension(extension).Replace(".", "");
            if (!Enum.TryParse(tempext, true, out ext))
                return SupportedExtensions.UNKNOWN;

            return ext;
        }


        /// <summary>
        /// Gets image format of image file.
        /// </summary>
        /// <param name="imagePath">Path to image file.</param>
        /// <returns>Format of image.</returns>
        public static Format ParseFormat(string imagePath)
        {
            SupportedExtensions ext = ParseExtension(imagePath);

            using (FileStream fs = new FileStream(imagePath, FileMode.Open))
                return ParseFormat(fs, ext);
        }

        /// <summary>
        /// Reads DDS format from DDS Header. 
        /// Not guaranteed to work. Format 'optional' in header.
        /// </summary>
        /// <param name="stream">Stream containing full image file. NOT just pixels.</param>
        /// <param name="header">DDS Header information.</param>
        /// <returns>Format of DDS.</returns>
        public static Format ParseDDSFormat(Stream stream, out DDS_HEADER header)
        {
            Format format = new Format(ImageEngineFormat.DDS_ARGB);

            stream.Seek(0, SeekOrigin.Begin);
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true))
            {
                header = null;

                // KFreon: Check image is a DDS
                int Magic = reader.ReadInt32();
                if (Magic != 0x20534444)
                    return new Format();  // KFreon: Not a DDS

                header = new DDS_HEADER();
                Read_DDS_HEADER(header, reader);

                if (((header.ddspf.dwFlags & 0x00000004) != 0) && (header.ddspf.dwFourCC == 0x30315844 /*DX10*/))
                    throw new Exception("DX10 not supported yet!");

                format = ImageFormats.ParseFourCC(header.ddspf.dwFourCC);

                // KFreon: Apparently all these flags mean it's a V8U8 image...
                if (header.ddspf.dwRGBBitCount == 0x10 &&
                           header.ddspf.dwRBitMask == 0xFF &&
                           header.ddspf.dwGBitMask == 0xFF00 &&
                           header.ddspf.dwBBitMask == 0x00 &&
                           header.ddspf.dwABitMask == 0x00)
                    format = new Format(ImageEngineFormat.DDS_V8U8);  // KFreon: V8U8


                // KFreon: Test for L8/G8
                if (header.ddspf.dwABitMask == 0 &&
                        header.ddspf.dwBBitMask == 0 &&
                        header.ddspf.dwGBitMask == 0 &&
                        header.ddspf.dwRBitMask == 255 &&
                        header.ddspf.dwFlags == 131072 &&
                        header.ddspf.dwSize == 32 &&
                        header.ddspf.dwRGBBitCount == 8)
                    format = new Format(ImageEngineFormat.DDS_G8_L8);
            }

            return format;
        }

        /// <summary>
        /// Reads DDS format from header given a filename.
        /// </summary>
        /// <param name="imagePath">Image filename.</param>
        /// <returns>Format of image.</returns>
        public static Format ParseDDSFormat(string imagePath)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                DDS_HEADER header;
                return ParseDDSFormat(fs, out header);
            }
        }
    }
}
