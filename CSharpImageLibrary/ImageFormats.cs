using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CSharpImageLibrary.DDSGeneral;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Indicates image format.
    /// Use FORMAT struct.
    /// </summary>
    public enum ImageEngineFormat
    {
        /// <summary>
        /// Unknown image format. Using this as a save/load format will fail that operation.
        /// </summary>
        Unknown = 1,

        /// <summary>
        /// Standard JPEG image handled by everything.
        /// </summary>
        JPG = 2,

        /// <summary>
        /// Standard PNG image handled by everything. Uses alpha channel if available.
        /// </summary>
        PNG = 3,

        /// <summary>
        /// Standard BMP image handled by everything.
        /// </summary>
        BMP = 4,

        /// <summary>
        /// (BC1) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Simple Non Alpha.
        /// </summary>
        DDS_DXT1 = 0x31545844,  // 1TXD i.e. DXT1 backwards

        /// <summary>
        /// (BC2) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Sharp Alpha. Premultiplied alpha. 
        /// </summary>
        DDS_DXT2 = 0x32545844,

        /// <summary>
        /// (BC2) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Sharp Alpha. 
        /// </summary>
        DDS_DXT3 = 0x33545844,

        /// <summary>
        /// (BC3) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Gradient Alpha. Premultiplied alpha.
        /// </summary>
        DDS_DXT4 = 0x34545844,

        /// <summary>
        /// (BC3) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Gradient Alpha. 
        /// </summary>
        DDS_DXT5 = 0x35545844,

        /// <summary>
        /// Uncompressed ARGB DDS.
        /// </summary>
        DDS_ARGB = 6,  // No specific value apparently

        /// <summary>
        /// (BC4) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Normal (bump) Maps. 8 bit single channel with alpha.
        /// </summary>
        DDS_ATI1 = 0x31495441,  // ATI1 backwards

        /// <summary>
        /// Uncompressed pair of 8 bit channels.
        /// Used for Normal (bump) maps.
        /// </summary>
        DDS_V8U8 = 8, 

        /// <summary>
        /// Pair of 8 bit channels.
        /// Used for Luminescence.
        /// </summary>
        DDS_G8_L8 = 7,  // No specific value it seems

        /// <summary>
        /// (BC5) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Normal (bump) Maps. Pair of 8 bit channels.
        /// </summary>
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
        /// Size of a compressed block.
        /// Returns -1 if format is not block compressed
        /// </summary>
        public int BlockSize
        {
            get
            {
                return GetBlockSize();
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

        private int GetBlockSize()
        {
            int blocksize = -1;
            switch (InternalFormat)
            {
                case ImageEngineFormat.DDS_ATI1:
                case ImageEngineFormat.DDS_DXT1:
                    blocksize = 8;
                    break;
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    blocksize = 16;
                    break;
            }
            return blocksize;
        }
    }

    /// <summary>
    /// Provides format functionality
    /// </summary>
    internal static class ImageFormats
    {
        /// <summary>
        /// File extensions supported. Used to get initial format.
        /// </summary>
        enum SupportedExtensions
        {
            UNKNOWN,
            JPG,
            BMP,
            PNG,
            DDS
        }


        /// <summary>
        /// Converts a DDS FourCC to a Format.
        /// </summary>
        /// <param name="FourCC">DDS FourCC to check.</param>
        /// <returns>Format specified by FourCC. Otherwise ARGB.</returns>
        private static Format ParseFourCC(int FourCC)
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
        /// <param name="extension">Extension of image file.</param>
        /// <returns>Format of image.</returns>
        internal static Format ParseFormat(Stream imgData, string extension)
        {
            SupportedExtensions ext = SupportedExtensions.UNKNOWN;

            // KFreon: Attempt to determine from data
            if (extension == null)
            {
                // KFreon: Save position and go back to start
                long originalPos = imgData.Position;
                imgData.Seek(0, SeekOrigin.Begin);

                char l1 = (char)imgData.ReadByte();
                char l2 = (char)imgData.ReadByte();
                char l3 = (char)imgData.ReadByte();
                char l4 = (char)imgData.ReadByte();

                // BMP
                if (l1 == 'B' && l2 == 'M') 
                    ext = SupportedExtensions.BMP;

                // PNG
                if (l1 == 137 && l2 == 'P' && l3 == 'N' && l4 == 'G')  
                    ext = SupportedExtensions.PNG;

                // JPG
                if (l1 == 0xFF && l2 == 0xD8 && l3 == 0xFF)
                    ext = SupportedExtensions.JPG;

                // DDS
                if (l1 == 'D' && l2 == 'D' && l3 == 'S')
                    ext = SupportedExtensions.DDS;

                // KFreon: Reset stream position
                imgData.Seek(originalPos, SeekOrigin.Begin);
            }
            else
                ext = ParseExtension(extension);

            if (ext == SupportedExtensions.UNKNOWN)
                return new Format();

            return ParseFormat(imgData, ext);
        }


        /// <summary>
        /// Gets Format of image.
        /// </summary>
        /// <param name="imgData">Stream containing entire image. NOT just pixels.</param>
        /// <param name="extension">Type of file.</param>
        /// <returns>Format of image.</returns>
        private static Format ParseFormat(Stream imgData, SupportedExtensions extension)
        {
            switch (extension)
            {
                case SupportedExtensions.BMP:
                    return new Format(ImageEngineFormat.BMP);
                case SupportedExtensions.DDS:
                    DDS_HEADER header;
                    return ParseDDSFormat(imgData, out header);
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
        private static SupportedExtensions ParseExtension(string extension)
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
        internal static Format ParseFormat(string imagePath)
        {
            SupportedExtensions ext = ParseExtension(imagePath);

            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
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

                if (format.InternalFormat == ImageEngineFormat.Unknown || format.InternalFormat == ImageEngineFormat.DDS_ARGB)
                {
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
