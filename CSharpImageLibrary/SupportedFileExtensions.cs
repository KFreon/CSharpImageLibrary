using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpImageLibrary.Headers;
using UsefulThings;

namespace CSharpImageLibrary
{
    public class SupportedFileExtensions
    {
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
        /// Determines image type via headers.
        /// Keeps stream position.
        /// </summary>
        /// <param name="imgData">Image data, incl header.</param>
        /// <returns>Type of image.</returns>
        public static SupportedExtensions DetermineFileExtension(Stream imgData)
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


    }
}
