using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    internal static class DDSGeneral
    {
        #region Header Stuff
        /// <summary>
        /// Reads DDS header from file.
        /// </summary>
        /// <param name="h">Header struct.</param>
        /// <param name="r">File reader.</param>
        internal static void Read_DDS_HEADER(DDS_HEADER h, BinaryReader r)
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
        internal class DDS_HEADER
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
        internal class DDS_PIXELFORMAT
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

        public static DDS_HEADER Build_DDS_Header(int Mips, int height, int width, ImageEngineFormat surfaceformat)
        {
            DDS_HEADER header = new DDS_HEADER();
            header.dwSize = 124;
            header.dwFlags = 0x1 | 0x2 | 0x4 | 0x1000 | (Mips != 0 ? 0x20000 : 0x0);  // Flags to denote valid fields: DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_MIPMAPCOUNT
            header.dwWidth = width;
            header.dwHeight = height;
            header.dwCaps = 0x1000 | 0x8 | (Mips == 0 ? 0 : 0x400000);
            header.dwMipMapCount = Mips == 0 ? 1 : Mips;
            //header.dwPitchOrLinearSize = ((width + 1) >> 1)*4;

            DDS_PIXELFORMAT px = new DDS_PIXELFORMAT();
            px.dwFourCC = (int)surfaceformat;
            px.dwSize = 32;
            //px.dwFlags = 0x200;
            px.dwFlags = 0x80000;
            px.dwRGBBitCount = 16;
            px.dwRBitMask = 255;
            px.dwGBitMask = 0x0000FF00;

            header.ddspf = px;
            return header;
        }
        #endregion Header Stuff




        #region Loading
        /// <summary>
        /// Loads an uncompressed DDS image given format specific Pixel Reader
        /// </summary>
        /// <param name="stream">Stream containing entire image. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="PixelReader">Function that knows how to read a pixel. Different for each format (V8U8, RGBA)</param>
        /// <returns></returns>
        internal static MemoryTributary LoadUncompressed(Stream stream, out int Width, out int Height, Func<Stream, int> PixelReader)
        {
            // KFreon: Necessary to move stream position along to pixel data.
            DDS_HEADER header = null;
            Format format = ImageFormats.ParseDDSFormat(stream, out header);

            Width = header.dwWidth;
            Height = header.dwHeight;

            MemoryTributary imgData = new MemoryTributary();

            // KFreon: Read data
            using (BinaryWriter writer = new BinaryWriter(imgData, Encoding.Default, true))
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int fCol = PixelReader(stream);  // KFreon: Reads pixel using a method specific to the format as provided
                        writer.Write(fCol);
                    }
                }
            }

            return imgData;
        }


        /// <summary>
        /// Loads a block compressed (BCx) texture.
        /// </summary>
        /// <param name="compressed">Compressed image data.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="DecompressBlock">Format specific block decompressor.</param>
        /// <returns>16 pixel RGBA channels.</returns>
        internal static MemoryTributary LoadBlockCompressedTexture(Stream compressed, out int Width, out int Height, Func<Stream, List<byte[]>> DecompressBlock)
        {
            DDS_HEADER header;
            Format format = ImageFormats.ParseDDSFormat(compressed, out header);

            Width = header.dwWidth;
            Height = header.dwHeight;

            int bitsPerPixel = 4;
            MemoryTributary imgData = new MemoryTributary(bitsPerPixel * (int)Width * (int)Height);

            // Loop over rows and columns NOT pixels
            int bitsPerScanline = bitsPerPixel * (int)Width;
            for (int row = 0; row < Height; row += 4)
            {
                for (int column = 0; column < Width; column += 4)
                {
                    // decompress 
                    List<byte[]> decompressed = DecompressBlock(compressed);

                    // Write texel
                    int TopLeft = column * bitsPerPixel + row * bitsPerScanline;  // Top left corner of texel IN BYTES (i.e. expanded pixels to 4 channels)
                    imgData.Seek(TopLeft, SeekOrigin.Begin);
                    for (int i = 0; i < 16; i += 4)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            // RGBA
                            imgData.WriteByte(decompressed[0][i + j]);
                            imgData.WriteByte(decompressed[1][i + j]);
                            imgData.WriteByte(decompressed[2][i + j]);
                            imgData.WriteByte(decompressed[3][i + j]);
                        }
                        // Go one line of pixels down (bitsPerScanLine), then to the left side of the texel (4 pixels back from where it finished)
                        imgData.Seek(bitsPerScanline - bitsPerPixel * 4, SeekOrigin.Current);
                    }
                }
            }
            return imgData;
        }
        #endregion Loading


        #region Block Decompression
        /// <summary>
        /// Decompresses an 8 bit channel.
        /// </summary>
        /// <param name="compressed">Compressed image data.</param>
        /// <param name="isSigned">true = use signed alpha range (-254 -- 255), false = 0 -- 255</param>
        /// <returns>Single channel decompressed (16 bits).</returns>
        internal static byte[] Decompress8BitBlock(Stream compressed, bool isSigned)
        {
            byte[] DecompressedBlock = new byte[16];

            // KFreon: Read colour range and build palette

            // KFreon: Read min and max colours (not necessarily in that order)
            byte min = (byte)compressed.ReadByte();
            byte max = (byte)compressed.ReadByte();

            byte[] Colours = Build8BitPalette(min, max, isSigned);


            // KFreon: Decompress pixels
            ulong bitmask = (ulong)compressed.ReadByte() << 0 | (ulong)compressed.ReadByte() << 8 | (ulong)compressed.ReadByte() << 16 |   // KFreon: Read all 6 compressed bytes into single 
                (ulong)compressed.ReadByte() << 24 | (ulong)compressed.ReadByte() << 32 | (ulong)compressed.ReadByte() << 40;


            // KFreon: Bitshift and mask compressed data to get 3 bit indicies, and retrieve indexed colour of pixel.
            for (int i = 0; i < 16; i++)
                DecompressedBlock[i] = (byte)Colours[bitmask >> (i * 3) & 0x7];

            return DecompressedBlock;
        }


        /// <summary>
        /// Decompresses a 3 channel (RGB) block.
        /// </summary>
        /// <param name="compressed">Compressed image data.</param>
        /// <returns>16 pixel RGBA channels.</returns>
        internal static List<byte[]> DecompressRGBBlock(Stream compressed)
        {
            int[] DecompressedBlock = new int[16];
            int[] Colours = new int[4];

            // Read min max colours
            Colours[0] = compressed.ReadByte() << 0 | compressed.ReadByte() << 8;
            Colours[1] = compressed.ReadByte() << 0 | compressed.ReadByte() << 8;

            // Interpolate other 2 colours
            Colours[2] = 2 / 3 * Colours[0] + 1 / 3 * Colours[1];
            Colours[3] = 1 / 3 * Colours[0] + 2 / 3 * Colours[1];

            // Decompress pixels
            byte bitmask = (byte)compressed.ReadByte();
            for (int i = 0; i < 16; i++)
                DecompressedBlock[i] = Colours[bitmask >> (2 * i) & 0x03];

            // KFreon: Decode into RGBA
            List<byte[]> DecompressedChannels = new List<byte[]>();
            byte[] red = new byte[16];
            byte[] green = new byte[16];
            byte[] blue = new byte[16];
            byte[] alpha = new byte[16];

            for (int i = 0; i < 16; i++)
            {
                int colour = DecompressedBlock[i];
                if (colour == 0)
                    alpha[i] = 255;
                else
                {
                    /*red[i] = (byte)(colour >> 11 & 31); // Top 5 bits
                    green[i] = (byte)(colour >> 5 & 63);   // Middle 6 bits
                    blue[i] = (byte)(colour & 31);  // Low 5 bits*/

                    red[i] = (byte)(colour >> 11 & 0x1F); // Top 5 bits
                    green[i] = (byte)(colour >> 5 & 0x3F);   // Middle 6 bits
                    blue[i] = (byte)(colour & 0x1F);  // Low 5 bits
                }
            }
            return DecompressedChannels;
        }
        #endregion


        #region Block Compression
        /// <summary>
        /// Compresses RGB channels using Block Compression.
        /// </summary>
        /// <param name="min">First main colour (often actually minimum.</param>
        /// <param name="max">Second main colour (often actually maximum.</param>
        /// <param name="pixelColours">16 pixel texel to compress.</param>
        /// <param name="isDXT1">Set true if DXT1.</param>
        /// <returns>8 byte compressed texel.</returns>
        public static byte[] CompressRGBBlock(int min, int max, int[] pixelColours, bool isDXT1)
        {
            if (pixelColours.Length != 16)
                throw new ArgumentOutOfRangeException($"PixelColours must have 16 entries. Got: {pixelColours.Length}");

            byte[] CompressedBlock = new byte[8];

            // Write colours
            byte[] colour0 = BitConverter.GetBytes(min);
            byte[] colour1 = BitConverter.GetBytes(max);

            CompressedBlock[0] = colour0[0];
            CompressedBlock[1] = colour0[1];

            CompressedBlock[2] = colour1[0];
            CompressedBlock[3] = colour1[1];

            // Build interpolated palette
            int[] Colours = BuildRGBPalette(min, max, isDXT1);

            // Compress pixels
            for (int i = 0; i < 16; i += 4) // each "row" of 4 pixels is a single byte
            {
                byte indicies = 0;
                for (int j = 0; j < 4; j++)
                {
                    int colour = pixelColours[i + j];
                    int index = Colours.IndexOfMin(c => Math.Abs(colour - c));
                    indicies |= (byte)(index << (2 * j));
                }
                CompressedBlock[i / 4 + 4] = indicies;
            }

            return CompressedBlock;
        }


        /// <summary>
        /// Compresses single channel using Block Compression.
        /// </summary>
        /// <param name="min">First main colour (often actually minimum)</param>
        /// <param name="max">Second main colour (often actually maximum)</param>
        /// <param name="pixelColours">16 pixel texel to compress.</param>
        /// <param name="isSigned">true = uses alpha range -255 -- 255, else 0 -- 255</param>
        /// <returns>8 byte compressed texel.</returns>
        public static byte[] Compress8BitBlock(byte min, byte max, byte[] pixelColours, bool isSigned)
        {
            byte[] CompressedBlock = new byte[8];

            // Write Colours
            CompressedBlock[0] = min;
            CompressedBlock[1] = max;

            // Build Palette
            byte[] Colours = Build8BitPalette(min, max, isSigned);

            // Compress Pixels
            ulong line = 0;
            for (int i = 0; i < 16; i++)
            {
                byte colour = pixelColours[i];
                int index = Colours.IndexOfMin(c => Math.Abs(colour - c));
                line |= (ulong)index << (i * 3);
            }

            return BitConverter.GetBytes(line);
        }

        #endregion Block Compression



        #region Palette
        /// <summary>
        /// Builds palette for 8 bit channel.
        /// </summary>
        /// <param name="min">First main colour (often actually minimum)</param>
        /// <param name="max">Second main colour (often actually maximum)</param>
        /// <param name="isSigned">true = sets signed alpha range (-254 -- 255), false = 0 -- 255</param>
        /// <returns>8 byte colour palette.</returns>
        internal static byte[] Build8BitPalette(byte min, byte max, bool isSigned)
        {
            byte[] Colours = new byte[8];
            Colours[0] = min;
            Colours[1] = max;

            // KFreon: Choose which type of interpolation is required
            if (min > max)
            {
                // KFreon: Interpolate other colours
                for (int i = 2; i < 8; i++)
                {
                    double test = ((8 - i) * min + (i - 1) * max) / 7.0f;
                    Colours[i] = (byte)test;
                }
            }
            else
            {
                // KFreon: Interpolate other colours and add Opacity or something...
                for (int i = 2; i < 6; i++)
                {
                    double test = ((8 - i) * min + (i - 1) * max) / 5.0f;
                    Colours[i] = (byte)test;
                }
                Colours[6] = (byte)(isSigned ? -254 : 0);  // KFreon: snorm and unorm have different alpha ranges
                Colours[7] = 255;
            }

            return Colours;
        }

        /// <summary>
        /// Builds a palette for RGB channels (DXT only)
        /// </summary>
        /// <param name="min">First main colour (often actually minimum)</param>
        /// <param name="max">Second main colour (often actually maximum)</param>
        /// <param name="isDXT1">true = use DXT1 format (1 bit alpha)</param>
        /// <returns>4 Colours as integers.</returns>
        public static int[] BuildRGBPalette(int min, int max, bool isDXT1)
        {
            int[] Colours = new int[4];
            Colours[0] = min;
            Colours[1] = max;

            Debugger.Break();

            // Interpolate other 2 colours
            if (min > max && !isDXT1)
            {
                Colours[2] = 2 / 3 * Colours[0] + 1 / 3 * Colours[1];
                Colours[3] = 1 / 3 * Colours[0] + 2 / 3 * Colours[1];
            }
            else
            {
                // KFreon: Only for dxt1
                Colours[2] = 1 / 2 * Colours[0] + 1 / 2 * Colours[1];
                Colours[3] = 0;
            }

            return Colours;
        }

        #endregion Palette
    }
}
