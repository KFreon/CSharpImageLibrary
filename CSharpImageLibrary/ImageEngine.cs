using CSharpImageLibrary.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace CSharpImageLibrary
{
    public static class ImageEngine
    {
        public static bool WindowsCodecsAvailable
        {
            get; private set;
        }

        public static bool WindowsCodecsPresent()
        {
            byte[] testData = Resources.DXT1_CodecTest;

            try
            {
                BitmapImage bmp = AttemptUsingWindowsCodecs(testData);

                if (bmp == null)
                    return false;  // KFreon: Decoding failed. PROBABLY due to no decoding available
            }
            catch(Exception e)
            {
                return false;  // KFreon: Non decoding related error - Who knows...
            }

            return true;
        }

        private static BitmapImage AttemptUsingWindowsCodecs(byte[] ImageFileData)
        {
            BitmapImage img = null;
            try
            {
                img = UsefulThings.WPF.Images.CreateWPFBitmap(ImageFileData);
            }
            catch (NotSupportedException e) when (e.Message.Contains("decoded", StringComparison.OrdinalIgnoreCase))
            {
                img = null;
            }

            return img;
        }

        private static BitmapImage AttemptUsingWindowsCodecs(string imagePath)
        {
            BitmapImage img = null;
            try
            {
                img = UsefulThings.WPF.Images.CreateWPFBitmap(imagePath);
            }
            catch (FileFormatException fileformatexception)
            {
                Debug.WriteLine(fileformatexception);
            }
            catch(NotSupportedException notsupportedexception)
            {
                Debug.WriteLine(notsupportedexception);
            }
            return img;
        }

        static ImageEngine()
        {
            WindowsCodecsAvailable = WindowsCodecsPresent();
        }

        public static MemoryTributary LoadImage(BitmapImage bmp, out double Width, out double Height, out Format Format, string extension)
        { 
            if (!WindowsCodecsAvailable)
            {
                // KFreon: Not on Windows 8+ or something like that
                Width = 0;
                Height = 0;
                Format = new Format();
                return null;
            }

            Height = bmp.Height;
            Width = bmp.Width;

            // KFreon: Get format, choosing data source based on how BitmapImage was created.
            if (bmp.UriSource != null)
                Format = ParseFormat(bmp.UriSource.OriginalString);
            else if (bmp.StreamSource != null)
                Format = ParseFormat(bmp.StreamSource, ParseExtension(extension));
            else
                throw new InvalidDataException("Bitmap doesn't seem to have a suitable source.");


            if (Format.InternalFormat == ImageEngineFormat.Unknown)
                Console.WriteLine();


            MemoryTributary pixelData = new MemoryTributary();

            int size = (int)(4 * Width * Height);
            byte[] pixels = new byte[size];
            int stride = (int)Width * 4;

            bmp.CopyPixels(pixels, stride, 0);
            pixelData.Write(pixels, 0, pixels.Length);
            return pixelData;
        }

        public static MemoryTributary LoadV8U8(string imagePath, out double Width, out double Height)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadV8U8(fs, out Width, out Height);
        }

        public static MemoryTributary LoadV8U8(Stream stream, out double Width, out double Height)
        {
            DDS_HEADER header = null;
            Format format = ParseDDSFormat(stream, out header);

            Width = header.dwWidth;
            Height = header.dwHeight;

            int mipMapBytes = (int)(Width * Height * 2);  // KFreon: 2 bytes per pixel
            MemoryTributary imgData = new MemoryTributary();

            using (BinaryWriter writer = new BinaryWriter(imgData, Encoding.Default, true))
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        sbyte red = (sbyte)stream.ReadByte();
                        sbyte green = (sbyte)stream.ReadByte();
                        byte blue = 0xFF;

                        int fCol = blue | (0x7F + green) << 8 | (0x7F + red) << 16 | 0xFF << 24;
                        writer.Write(fCol);
                    }
                }
            }
                

            return imgData;
        }

        public static MemoryTributary LoadImage(string imagePath, out double Width, out double Height, out Format Format)
        {
            Width = 0;
            Height = 0;
            Format = new Format();


            if (!WindowsCodecsAvailable)
                return null;

            BitmapImage bmp = AttemptUsingWindowsCodecs(imagePath);

            if (bmp == null)
            {
                // KFreon: Unsupported by Windows Codecs
                // e.g. V8U8, 3Dc, G8/L8

                Format test = ParseDDSFormat(imagePath);

                switch (test.InternalFormat)
                {
                    case ImageEngineFormat.DDS_V8U8:
                        Format = new Format(ImageEngineFormat.DDS_V8U8);
                        return LoadV8U8(imagePath, out Width, out Height);
                    case ImageEngineFormat.DDS_G8_L8:
                        break;
                    case ImageEngineFormat.DDS_ATI1N_BC4:
                        break;
                    case ImageEngineFormat.DDS_ATI2_3Dc:
                        break;
                    // TODO: RBGA?
                }





                Console.WriteLine();
                return null;  // TODO: Temporary return
            }
            else
            {
                return LoadImage(bmp, out Width, out Height, out Format, Path.GetExtension(imagePath));
            }
        }

        /// <summary>
        /// Builds mips for image. Note, doesn't keep the topmost as it's stored in the PixelData array of the main image.
        /// </summary>
        /// <param name="bmp">Image to build mips for.</param>
        /// <returns></returns>
        private static List<ImageEngineImage> BuildMipMaps(BitmapImage bmp, string extension)
        {
            // KFreon: Smallest dimension so mipping stops at say 2x1 instead of trying to go 1x0.5
            double determiningDimension = bmp.Width > bmp.Height ? bmp.Height : bmp.Width;
            BitmapImage workingImage = bmp;

            List<ImageEngineImage> MipMaps = new List<ImageEngineImage>();

            if (WindowsCodecsAvailable)
            {
                while (determiningDimension > 1)
                {
                    workingImage = UsefulThings.WPF.Images.ScaleImage(workingImage, 0.5);
                    MipMaps.Add(new ImageEngineImage(workingImage, extension));
                    determiningDimension /= 2;
                }
            }

            return MipMaps;
        }


        private static SupportedExtensions ParseExtension(string extension)
        {
            SupportedExtensions ext = SupportedExtensions.DDS;
            string tempext = Path.GetExtension(extension).Replace(".", "");
            if (!Enum.TryParse(tempext, true, out ext))
                return SupportedExtensions.UNKNOWN;

            return ext;
        }

        private static Format ParseFormat(string imagePath)
        {
            SupportedExtensions ext = ParseExtension(imagePath);

            using (FileStream fs = new FileStream(imagePath, FileMode.Open))
                return ParseFormat(fs, ext);
        }

        private static Format ParseFormat(Stream imgData, SupportedExtensions ext)
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


        private static Format ParseDDSFormat(string imagePath)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                DDS_HEADER header;
                return ParseDDSFormat(fs, out header);
            }
        }

        private static Format ParseDDSFormat(Stream stream, out DDS_HEADER header)
        {
            Format format = new Format();

            stream.Seek(0, SeekOrigin.Begin);
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true))
            {
                header = null;

                int Magic = reader.ReadInt32();
                if (Magic != 0x20534444)
                    return new Format();  // KFreon: Not a DDS

                header = new DDS_HEADER();
                Read_DDS_HEADER(header, reader);

                if (((header.ddspf.dwFlags & 0x00000004) != 0) && (header.ddspf.dwFourCC == 0x30315844 /*DX10*/))
                    throw new Exception("DX10 not supported yet!");

                format = ImageFormats.ParseFourCC(header.ddspf.dwFourCC);

                if (header.ddspf.dwRGBBitCount == 0x10 &&
                           header.ddspf.dwRBitMask == 0xFF &&
                           header.ddspf.dwGBitMask == 0xFF00 &&
                           header.ddspf.dwBBitMask == 0x00 &&
                           header.ddspf.dwABitMask == 0x00)
                    format = new Format(ImageEngineFormat.DDS_V8U8);  // KFreon: V8U8
            }

            return format;
        }

        #region DDS Header Stuff
        /// <summary>
        /// Reads DDS header from file.
        /// </summary>
        /// <param name="h">Header struct.</param>
        /// <param name="r">File reader.</param>
        public static void Read_DDS_HEADER(DDS_HEADER h, BinaryReader r)
        {
            h.dwSize = r.ReadInt32();
            h.dwFlags = r.ReadInt32();
            h.dwHeight = r.ReadInt32();
            h.dwWidth = r.ReadInt32();
            h.dwPitchOrLinearSize = r.ReadInt32();
            h.dwDepth = r.ReadInt32();
            h.dwMipMapCount = r.ReadInt32();
            for (int i = 0; i < 11; ++i)
            {
                h.dwReserved1[i] = r.ReadInt32();
            }
            Read_DDS_PIXELFORMAT(h.ddspf, r);
            h.dwCaps = r.ReadInt32();
            h.dwCaps2 = r.ReadInt32();
            h.dwCaps3 = r.ReadInt32();
            h.dwCaps4 = r.ReadInt32();
            h.dwReserved2 = r.ReadInt32();
        }

        /// <summary>
        /// Reads DDS pixel format.
        /// </summary>
        /// <param name="p">Pixel format struct.</param>
        /// <param name="r">File reader.</param>
        private static void Read_DDS_PIXELFORMAT(DDS_PIXELFORMAT p, BinaryReader r)
        {
            p.dwSize = r.ReadInt32();
            p.dwFlags = r.ReadInt32();
            p.dwFourCC = r.ReadInt32();
            p.dwRGBBitCount = r.ReadInt32();
            p.dwRBitMask = r.ReadInt32();
            p.dwGBitMask = r.ReadInt32();
            p.dwBBitMask = r.ReadInt32();
            p.dwABitMask = r.ReadInt32();
        }

        public class DDS_HEADER
        {
            public int dwSize;
            public int dwFlags;
            public int dwHeight;
            public int dwWidth;
            public int dwPitchOrLinearSize;
            public int dwDepth;
            public int dwMipMapCount;
            public int[] dwReserved1 = new int[11];
            public DDS_PIXELFORMAT ddspf = new DDS_PIXELFORMAT();
            public int dwCaps;
            public int dwCaps2;
            public int dwCaps3;
            public int dwCaps4;
            public int dwReserved2;
        }

        public class DDS_PIXELFORMAT
        {
            public int dwSize;
            public int dwFlags;
            public int dwFourCC;
            public int dwRGBBitCount;
            public int dwRBitMask;
            public int dwGBitMask;
            public int dwBBitMask;
            public int dwABitMask;

            public DDS_PIXELFORMAT()
            {
            }
        }

        #endregion DDS Header Stuff
    }
}
