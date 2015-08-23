using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides general functions specific to DDS format
    /// </summary>
    public static class DDSGeneral
    {
        #region Header Stuff
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

        /// <summary>
        /// Contains information about DDS Headers. 
        /// </summary>
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

        /// <summary>
        /// Contains information about DDS Pixel Format.
        /// </summary>
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
        #endregion Header Stuff


        /// <summary>
        /// Reads uncompressed image data using a format specific Pixel Reader.
        /// </summary>
        /// <param name="fileData">Stream of entire image. NOT just pixels.</param>
        /// <param name="PixelData">RGBA Pixel Data as read in this function.</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <param name="PixelReader">Function that knows how to read a pixel. Different for each format (V8U8, RGBA)</param>
        private static void ReadUncompressed(Stream fileData, MemoryTributary PixelData, double Width, double Height, Func<Stream, int> PixelReader)
        {
            using (BinaryWriter writer = new BinaryWriter(PixelData, Encoding.Default, true))
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int fCol = PixelReader(fileData);  // KFreon: Reads pixel using a method specific to the format as provided
                        writer.Write(fCol);
                    }
                }
            }
        }


        /// <summary>
        /// Loads an uncompressed DDS image given format specific Pixel Reader
        /// </summary>
        /// <param name="stream">Stream containing entire image. NOT just pixels.</param>
        /// <param name="NumChannels">Number of colour channels in image. (RGBA = 4)</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <param name="PixelReader">Function that knows how to read a pixel. Different for each format (V8U8, RGBA)</param>
        /// <returns></returns>
        public static MemoryTributary LoadUncompressed(Stream stream, int NumChannels, out double Width, out double Height, Func<Stream, int> PixelReader)
        {
            // KFreon: Necessary to move stream position along to pixel data.
            DDS_HEADER header = null;
            Format format = ImageFormats.ParseDDSFormat(stream, out header);

            Width = header.dwWidth;
            Height = header.dwHeight;

            int mipMapBytes = (int)(Width * Height * NumChannels);  // KFreon: 2 bytes per pixel
            MemoryTributary imgData = new MemoryTributary();

            DDSGeneral.ReadUncompressed(stream, imgData, Width, Height, PixelReader);

            return imgData;
        }
    }
}
