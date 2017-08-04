using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CSharpImageLibrary.Headers;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides format functionality
    /// </summary>
    public partial class ImageFormats
    {
        /// <summary>
        /// Contains formats not yet capable of saving.
        /// </summary>
        public static List<ImageEngineFormat> SaveUnsupported = new List<ImageEngineFormat>() { ImageEngineFormat.TGA, ImageEngineFormat.Unknown, ImageEngineFormat.DX10_Placeholder };


        /// <summary>
        /// Get list of supported extensions in lower case.
        /// </summary>
        /// <param name="addDot">Adds preceeding dot to be same as Path.GetExtension.</param>
        /// <returns>List of supported extensions.</returns>
        public static List<string> GetSupportedExtensions(bool addDot = false)
        {
            if (addDot)
                return Enum.GetNames(typeof(SupportedExtensions)).Where(t => t != "UNKNOWN").Select(g => "." + g).ToList();
            else
                return Enum.GetNames(typeof(SupportedExtensions)).Where(t => t != "UNKNOWN").ToList();
        }


        /// <summary>
        /// Determines if file has a supported extension.
        /// </summary>
        /// <param name="filePath">Path of file to to check.</param>
        /// <param name="supported">Optionally list of supported extensions. Good if looping and can initialise supported and pass into this every loop.</param>
        /// <returns>True if supported.</returns>
        public static bool IsExtensionSupported(string filePath, List<string> supported = null)
        {
            List<string> supportedExts = supported ?? GetSupportedExtensions(true);
            return supportedExts.Contains(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get list of filter strings for dialog boxes of the Supported Images.
        /// </summary>
        /// <returns>List of filter strings.</returns>
        public static List<string> GetSupportedExtensionsForDialogBox()
        {
            List<string> filters = new List<string>();
            var names = GetSupportedExtensions();

            // All supported
            filters.Add("All Supported|*." + String.Join(";*.", names));

            foreach (var name in names)
            {
                var enumValue = (SupportedExtensions)Enum.Parse(typeof(SupportedExtensions), name);
                var desc = UsefulThings.General.GetEnumDescription(enumValue);

                filters.Add($"{desc}|*.{name}");
            }
            return filters;
        }

        /// <summary>
        /// Gets list of filter strings for dialog boxes already formatted as string.
        /// </summary>
        /// <returns>String of dialog filters</returns>
        public static string GetSupportedExtensionsForDialogBoxAsString()
        {
            return String.Join("|", GetSupportedExtensionsForDialogBox());
        }

        /// <summary>
        /// Get descriptions of supported images. Generally the description as would be seen in a SaveFileDialog.
        /// </summary>
        /// <returns>List of descriptions of supported images.</returns>
        public static List<string> GetSupportedExtensionsDescriptions()
        {
            List<string> descriptions = new List<string>();
            var names = GetSupportedExtensions();
            foreach (var name in names)
            {
                var enumValue = (SupportedExtensions)Enum.Parse(typeof(SupportedExtensions), name);
                descriptions.Add(UsefulThings.General.GetEnumDescription(enumValue));
            }

            return descriptions;
        }

        /// <summary>
        /// File extensions supported. Used to get initial format.
        /// </summary>
        public enum SupportedExtensions
        {
            /// <summary>
            /// Format isn't known...
            /// </summary>
            [Description("Unknown format")]
            UNKNOWN,

            /// <summary>
            /// JPEG format. Good for small images, but is lossy, hence can have poor colours and artifacts at high compressions.
            /// </summary>
            [Description("Joint Photographic Images")]
            JPG,

            /// <summary>
            /// JPEG format. Good for small images, but is lossy, hence can have poor colours and artifacts at high compressions.
            /// </summary>
            [Description("Joint Photographic Images")]
            JPEG,

            /// <summary>
            /// BMP bitmap. Lossless but exceedingly poor bytes for pixel ratio i.e. huge filesize for little image.
            /// </summary>
            [Description("Bitmap Images")]
            BMP,

            /// <summary>
            /// Supports transparency, decent compression. Use this unless you can't.
            /// </summary>
            [Description("Portable Network Graphic Images")]
            PNG,

            /// <summary>
            /// DirectDrawSurface image. DirectX image, supports mipmapping, fairly poor compression/artifacting. Good for video memory due to mipmapping.
            /// </summary>
            [Description("DirectX Images")]
            DDS,

            /// <summary>
            /// Targa image.
            /// </summary>
            [Description("Targa Images")]
            TGA,

            /// <summary>
            /// Graphics Interchange Format images. Lossy compression, supports animation (this tool doesn't though), good for low numbers of colours.
            /// </summary>
            [Description("Graphics Interchange Images")]
            GIF,

            /// <summary>
            /// TIFF images. Compressed, and supports mipmaps.
            /// </summary>
            [Description("TIFF Images")]
            TIF,

            /// <summary>
            /// TIFF images. Compressed, and supports mipmaps.
            /// </summary>
            [Description("TIFF Images")]
            TIFF,
        }

        /// <summary>
        /// Determines image type via headers.
        /// Keeps stream position.
        /// </summary>
        /// <param name="imgData">Image data, incl header.</param>
        /// <returns>Type of image.</returns>
        public static SupportedExtensions DetermineImageType(Stream imgData)
        {
            SupportedExtensions ext = SupportedExtensions.UNKNOWN;

            // KFreon: Save position and go back to start
            long originalPos = imgData.Position;
            imgData.Seek(0, SeekOrigin.Begin);

            var bits = new byte[8];
            imgData.Read(bits, 0, 8);

            // BMP
            if (BMP_Header.CheckIdentifier(bits)) 
                ext = SupportedExtensions.BMP;

            // PNG
            if (PNG_Header.CheckIdentifier(bits))  
                ext = SupportedExtensions.PNG;

            // JPG
            if (JPG_Header.CheckIdentifier(bits))
                ext = SupportedExtensions.JPG;

            // DDS
            if (DDS_Header.CheckIdentifier(bits))
                ext = SupportedExtensions.DDS;

            // GIF
            if (GIF_Header.CheckIdentifier(bits))
                ext = SupportedExtensions.GIF;

            if (TIFF_Header.CheckIdentifier(bits))
                ext = SupportedExtensions.TIF;

            // TGA (assumed if no other matches
            if (ext == SupportedExtensions.UNKNOWN)
                ext = SupportedExtensions.TGA;

            // KFreon: Reset stream position
            imgData.Seek(originalPos, SeekOrigin.Begin);

            return ext;
        }


        /// <summary>
        /// Gets file extension from string of extension.
        /// </summary>
        /// <param name="extension">String containing file extension.</param>
        /// <returns>SupportedExtension of extension.</returns>
        public static SupportedExtensions ParseExtension(string extension)
        {
            SupportedExtensions ext = SupportedExtensions.DDS;
            string tempext = extension.Contains('.') ? Path.GetExtension(extension).Replace(".", "") : extension;
            if (!Enum.TryParse(tempext, true, out ext))
                return SupportedExtensions.UNKNOWN;

            return ext;
        }


        /// <summary>
        /// Searches for a format within a string. Good for automatic file naming.
        /// </summary>
        /// <param name="stringWithFormatInIt">String containing format somewhere in it.</param>
        /// <returns>Format in string, or UNKNOWN otherwise.</returns>
        public static ImageEngineFormat FindFormatInString(string stringWithFormatInIt)
        {
            ImageEngineFormat detectedFormat = ImageEngineFormat.Unknown;
            foreach (var formatName in Enum.GetNames(typeof(ImageEngineFormat)))
            {
                string actualFormat = formatName.Replace("DDS_", "");
                bool check = stringWithFormatInIt.Contains(actualFormat, StringComparison.OrdinalIgnoreCase);

                if (actualFormat.Contains("3Dc"))
                    check = stringWithFormatInIt.Contains("3dc", StringComparison.OrdinalIgnoreCase) || stringWithFormatInIt.Contains("ati2", StringComparison.OrdinalIgnoreCase);
                else if (actualFormat == "A8L8")
                    check = stringWithFormatInIt.Contains("L8", StringComparison.OrdinalIgnoreCase) && !stringWithFormatInIt.Contains("G", StringComparison.OrdinalIgnoreCase);
                else if (actualFormat == "G8_L8")
                    check = !stringWithFormatInIt.Contains("A", StringComparison.OrdinalIgnoreCase) &&  stringWithFormatInIt.Contains("G8", StringComparison.OrdinalIgnoreCase);
                else if (actualFormat.Contains("ARGB"))
                    check = stringWithFormatInIt.Contains("A8R8G8B8", StringComparison.OrdinalIgnoreCase) || stringWithFormatInIt.Contains("ARGB", StringComparison.OrdinalIgnoreCase);

                if (check)
                {
                    detectedFormat = (ImageEngineFormat)Enum.Parse(typeof(ImageEngineFormat), formatName);
                    break;
                }
            }            

            return detectedFormat;
        }

        
        /// <summary>
        /// Gets file extension of supported surface formats.
        /// Doesn't include preceding dot.
        /// </summary>
        /// <param name="format">Format to get file extension for.</param>
        /// <returns>File extension without dot.</returns>
        static string GetExtensionOfFormat(ImageEngineFormat format)
        {
            string formatString = format.ToString().ToLowerInvariant();
            if (formatString.Contains('_'))
                formatString = "dds";

            return formatString;
        }

        /// <summary>
        /// Calculates the compressed size of an image with given parameters.
        /// </summary>
        /// <param name="numMipmaps">Number of mipmaps in image. JPG etc only have 1.</param>
        /// <param name="formatDetails">Detailed information about format.</param>
        /// <param name="width">Width of image (top mip if mip-able)</param>
        /// <param name="height">Height of image (top mip if mip-able)</param>
        /// <returns>Size of compressed image.</returns>
        public static int GetCompressedSize(int numMipmaps, ImageEngineFormatDetails formatDetails, int width, int height)
        {
            return DDS.DDSGeneral.GetCompressedSizeOfImage(numMipmaps, formatDetails, width, height);
        }
        


        /// <summary>
        /// Gets uncompressed size of image with mipmaps given dimensions and number of channels. 
        /// Assume 8 bits per channel.
        /// </summary>
        /// <param name="topWidth">Width of top mipmap.</param>
        /// <param name="topHeight">Height of top mipmap.</param>
        /// <param name="numChannels">Number of channels in image.</param>
        /// <param name="inclMips">Include size of mipmaps.</param>
        /// <returns>Uncompressed size in bytes including mipmaps.</returns>
        public static int GetUncompressedSize(int topWidth, int topHeight, int numChannels, bool inclMips)
        {
            return (int)(numChannels * (topWidth * topHeight) * (inclMips ? 4d/3d : 1d));
        }
    }
}
