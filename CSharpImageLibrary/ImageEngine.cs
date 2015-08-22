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
        public static bool UsingWindowsCodecs
        {
            get; private set;
        }

        public static bool WindowsCodecsPresent()
        {
            byte[] testData = Resources.DXT1_CodecTest;
            try
            {
                BitmapImage bmp = UsefulThings.WPF.Images.CreateWPFBitmap(testData);
                if (bmp == null)
                    return false;
            }
            catch (NotSupportedException e) when (e.Message.Contains("codec", StringComparison.OrdinalIgnoreCase))
            {
                // KFreon: No suitable codecs found - still might actually have a codec, but something else weird could have happened...
                return false;
            }

            return true;
        }

        static ImageEngine()
        {
            UsingWindowsCodecs = WindowsCodecsPresent();
        }

        public static MemoryTributary LoadImage(BitmapImage bmp, out double Width, out double Height, out Format Format)
        {
            if (!UsingWindowsCodecs)
            {
                Width = 0;
                Height = 0;
                Format = new Format();
                return null;
            }

            Height = bmp.Height;
            Width = bmp.Width;

            // KFreon: Get format, choosing data source based on how BitmapImage was created.
            if (bmp.UriSource != null)
                Format = ParseDDSFormat(bmp.UriSource.OriginalString);
            else if (bmp.StreamSource != null)
                Format = ParseDDSFormat(bmp.StreamSource);
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

        public static MemoryTributary LoadImage(string imagePath, out double Width, out double Height, out Format Format)
        {
            if (!UsingWindowsCodecs)
            {
                Width = 0;
                Height = 0;
                Format = new Format();
                return null;
            }


            BitmapImage bmp = UsefulThings.WPF.Images.CreateWPFBitmap(imagePath);
            return LoadImage(bmp, out Width, out Height, out Format);
        }

        /// <summary>
        /// Builds mips for image. Note, doesn't keep the topmost as it's stored in the PixelData array of the main image.
        /// </summary>
        /// <param name="bmp">Image to build mips for.</param>
        /// <returns></returns>
        private static List<ImageEngineImage> BuildMipMaps(BitmapImage bmp)
        {
            // KFreon: Smallest dimension so mipping stops at say 2x1 instead of trying to go 1x0.5
            double determiningDimension = bmp.Width > bmp.Height ? bmp.Height : bmp.Width;
            BitmapImage workingImage = bmp;

            List<ImageEngineImage> MipMaps = new List<ImageEngineImage>();

            if (UsingWindowsCodecs)
            {
                while (determiningDimension > 1)
                {
                    workingImage = UsefulThings.WPF.Images.ScaleImage(workingImage, 0.5);
                    MipMaps.Add(new ImageEngineImage(workingImage));
                    determiningDimension /= 2;
                }
            }

            return MipMaps;
        }

        private static Format ParseDDSFormat(string imagePath)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open))
                return ParseDDSFormat(fs);
        }

        private static Format ParseDDSFormat(Stream stream)
        {
            Format format = new Format();

            stream.Seek(0, SeekOrigin.Begin);
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true))
            {
                int Magic = reader.ReadInt32();
                if (Magic != 0x20534444)
                    return new Format();  // KFreon: Not a DDS

                DDS_HEADER header = new DDS_HEADER();
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
