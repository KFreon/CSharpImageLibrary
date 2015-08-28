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


        /// <summary>
        /// Builds a header for DDS file format using provided information.
        /// </summary>
        /// <param name="Mips">Number of mips in image.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="surfaceformat">DDS FourCC.</param>
        /// <returns>Header for DDS file.</returns>
        public static DDS_HEADER Build_DDS_Header(int Mips, int Height, int Width, ImageEngineFormat surfaceformat)
        {
            DDS_HEADER header = new DDS_HEADER();
            header.dwSize = 124;
            header.dwFlags = 0x1 | 0x2 | 0x4 | 0x1000 | (Mips != 0 ? 0x20000 : 0x0);  // Flags to denote valid fields: DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_MIPMAPCOUNT
            header.dwWidth = Width;
            header.dwHeight = Height;
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


        /// <summary>
        /// Write DDS header to stream via BinaryWriter.
        /// </summary>
        /// <param name="header">Populated DDS header by Build_DDS_Header.</param>
        /// <param name="writer">Stream to write to.</param>
        public static void Write_DDS_Header(DDS_HEADER header, BinaryWriter writer)
        {
            // KFreon: Write magic number ("DDS")
            writer.Write(0x20534444);

            // KFreon: Write all header fields regardless of filled or not
            writer.Write(header.dwSize);
            writer.Write(header.dwFlags);
            writer.Write(header.dwHeight);
            writer.Write(header.dwWidth);
            writer.Write(header.dwPitchOrLinearSize);
            writer.Write(header.dwDepth);
            writer.Write(header.dwMipMapCount);

            // KFreon: Write reserved1
            for (int i = 0; i < 11; i++)
                writer.Write(0);

            // KFreon: Write PIXELFORMAT
            DDS_PIXELFORMAT px = header.ddspf;
            writer.Write(px.dwSize);
            writer.Write(px.dwFlags);
            writer.Write(px.dwFourCC);
            writer.Write(px.dwRGBBitCount);
            writer.Write(px.dwRBitMask);
            writer.Write(px.dwGBitMask);
            writer.Write(px.dwBBitMask);
            writer.Write(px.dwABitMask);

            writer.Write(header.dwCaps);
            writer.Write(header.dwCaps2);
            writer.Write(header.dwCaps3);
            writer.Write(header.dwCaps4);
            writer.Write(header.dwReserved2);
        }
        #endregion Header Stuff


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pixelData"></param>
        /// <param name="Destination"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <param name="Mips"></param>
        /// <param name="header"></param>
        /// <param name="CompressBlock"></param>
        /// <returns></returns>
        internal static bool WriteBlockCompressedDDS(Stream pixelData, Stream Destination, int Width, int Height, int Mips, DDS_HEADER header, Func<byte[], byte[]> CompressBlock)
        {
            Action<BinaryWriter, Stream> PixelWriter = (writer, pixels) =>
            {
                byte[] texel = DDSGeneral.GetTexel(pixels, Width);
                byte[] CompressedBlock = CompressBlock(texel);
                writer.Write(CompressedBlock);
            };

            return DDSGeneral.WriteDDS(pixelData, Destination, Width, Height, Mips, header, PixelWriter);
        }

        internal static bool WriteDDS(Stream pixelData, Stream Destination, int Width, int Height, int Mips, DDS_HEADER header, Action<BinaryWriter, Stream> PixelWriter)
        {
            try
            {
                pixelData.Seek(0, SeekOrigin.Begin);
                using (BinaryWriter writer = new BinaryWriter(Destination, Encoding.Default, true))
                {
                    Write_DDS_Header(header, writer);
                    for (int m = 1; m < Mips; m++)
                    {
                        for (int h = 0; h < Height / Mips; h++)
                        {
                            for (int w = 0; w < Width / Mips; w++)
                            {
                                PixelWriter(writer, pixelData);
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }


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
        /// <param name="isDXT1">True = DXT1, otherwise false.</param>
        /// <returns>16 pixel RGBA channels.</returns>
        internal static List<byte[]> DecompressRGBBlock(Stream compressed, bool isDXT1)
        {
            byte[] DecompressedBlock = new byte[16];
            int[] Colours = new int[4];

            // Read min max colours
            BinaryReader reader = new BinaryReader(compressed);
            ushort min = (ushort)reader.ReadInt16();

            byte r = (byte)((min & 0x1F));
            byte g = (byte)((min & 0x7E0) >> 5);
            byte b = (byte)((min & 0xF800) >> 11);

            byte r1 = (byte)(r << 3 | r >> 2);  // still mixed up somehow. FIGURE THIS OUT
            byte g1 = (byte)(g << 2 | g >> 3);
            byte b1 = (byte)(b << 3 | b >> 2);

            ushort max = (ushort)reader.ReadInt16();
            r = (byte)((max & 0x1F));
            g = (byte)((max & 0x7E0) >> 5);
            b = (byte)((max & 0xF800) >> 11);

            /*byte test1 = (byte)compressed.ReadByte();
            byte test2 = (byte)compressed.ReadByte();

            byte test3 = (byte)compressed.ReadByte();
            byte test4 = (byte)compressed.ReadByte();*/

            /*int min = test1 << 0 | test2 << 8;
            int max = test3 << 0 | test4 << 8;*/

            Colours = BuildRGBPalette(min, max, isDXT1);

            // Decompress pixels
            for (int i = 0; i < 16; i+=4)
            {
                byte bitmask = (byte)compressed.ReadByte();
                Debug.WriteLine($"bitmask: {bitmask}");
                for (int j = 0; j < 4; j++)
                    DecompressedBlock[i + j] = (byte)Colours[bitmask >> (2 * j) & 0x03];
            }
                

            for (int i = 0; i < 16; i += 4)
            {
                for (int j = 0; j < 4; j++)
                {
                    Debug.Write(DecompressedBlock[i + j] + " ");
                }
                Debug.WriteLine("");
            }

            // KFreon: Decode into RGBA
            List<byte[]> DecompressedChannels = new List<byte[]>();
            byte[] red = new byte[16];
            byte[] green = new byte[16];
            byte[] blue = new byte[16];
            byte[] alpha = new byte[16];
            DecompressedChannels.Add(red);
            DecompressedChannels.Add(green);
            DecompressedChannels.Add(blue);
            DecompressedChannels.Add(alpha);

            for (int i = 0; i < 16; i++)
            {
                byte colour = DecompressedBlock[i];
                if (colour == 0)
                    alpha[i] = 255;
                else
                {
                    /*red[i] = (byte)(colour >> 11 & 31); // Top 5 bits
                    green[i] = (byte)(colour >> 5 & 63);   // Middle 6 bits
                    blue[i] = (byte)(colour & 31); */ // Low 5 bits

                    /*red[i] = (byte)(colour & 0xF800); // Top 5 bits
                    green[i] = (byte)(colour & 0x07E0);   // Middle 6 bits
                    blue[i] = (byte)(colour & 0x001F); // Low 5 bits*/

                    red[i] = (byte)(colour >> 11 & 0x1F);
                    green[i] = (byte)(colour >> 5 & 0x3F);
                    blue[i] = (byte)(colour >> 0 & 0x1F);
                }
            }
            return DecompressedChannels;
        }
        #endregion


        #region Block Compression
        /// <summary>
        /// Compresses RGB channels using Block Compression.
        /// </summary>
        /// <param name="texel">16 pixel texel to compress.</param>
        /// <param name="isDXT1">Set true if DXT1.</param>
        /// <returns>8 byte compressed texel.</returns>
        public static byte[] CompressRGBBlock(byte[] texel, bool isDXT1)
        {
            if (texel.Length != 16)
                throw new ArgumentOutOfRangeException($"PixelColours must have 16 entries. Got: {texel.Length}");

            byte[] CompressedBlock = new byte[8];

            // Get Min and Max colours
            int min = 0;
            int max = 0;
            GetRGBMinMax(texel, out min, out max);

            if (isDXT1)
            {
                // KFreon: Check alpha (every 4 bytes)
                for (int i = 0; i < texel.Length; i += 4)
                {
                    if (texel[i] != 0) // Alpha found, switch min and max
                    {
                        int temp = min;
                        min = max;
                        max = temp;
                        break;
                    }
                }
            }


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
                byte twoIndicies = 0;
                for (int j = 0; j < 4; j++)
                {
                    int colour = texel[i + j];
                    int index = Colours.IndexOfMin(c => Math.Abs(colour - c));
                    twoIndicies |= (byte)(index << (2 * j));
                }
                CompressedBlock[i / 4 + 4] = twoIndicies;
            }

            return CompressedBlock;
        }

        /// <summary>
        /// Compresses single channel using Block Compression.
        /// </summary>
        /// <param name="texel">4 channel Texel to compress.</param>
        /// <param name="channel">0-3 (RGBA)</param>
        /// <param name="isSigned">true = uses alpha range -255 -- 255, else 0 -- 255</param>
        /// <returns>8 byte compressed texel.</returns>
        public static byte[] Compress8BitBlock(byte[] texel, int channel, bool isSigned)
        {
            if (texel.Length != 16)
                throw new ArgumentOutOfRangeException($"PixelColours must have 16 entries. Got: {texel.Length}");

            // KFreon: Get min and max - since it's a single channel, can just get min/max, directly from texel.
            byte min = texel.Min();
            byte max = texel.Max();

            byte[] CompressedBlock = new byte[8];

            // Write Colours
            CompressedBlock[0] = min;
            CompressedBlock[1] = max;

            // Build Palette
            byte[] Colours = Build8BitPalette(min, max, isSigned);

            // Compress Pixels
            ulong line = 0;
            int count = channel;
            for (int i = 0; i < 16; i++)
            {
                byte colour = texel[count];
                int index = Colours.IndexOfMin(c => Math.Abs(colour - c));
                line |= (ulong)index << (i * 3);
                count += 4;  // Only need 1 channel
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

        internal static void GetRGBMinMax(byte[] texel, out int min, out int max)
        {
            int[] RGB = new int[16];
            int count = 0;
            for (int i = 0; i < 64; i += 16) // texel row
            {
                for (int j = 0; j < 16; j += 4)  // pixels in row incl RGBA
                {
                    int pixelColour = texel[i + j] << 0 | texel[i + j + 1] << 5 | texel[i + j + 2] << 11;  // RGB - NO alpha
                    RGB[count++] = pixelColour;
                }
            }

            min = RGB.Min();
            max = RGB.Max();
        }

        /// <summary>
        /// Builds a palette for RGB channels (DXT only)
        /// </summary>
        /// <param name="min">First main colour. Often actually the minumum.</param>
        /// <param name="max">Second main colour. Often actually the maximum.</param>
        /// <param name="isDXT1">true = use DXT1 format (1 bit alpha)</param>
        /// <returns>4 Colours as integers.</returns>
        public static int[] BuildRGBPalette(int min, int max, bool isDXT1)
        {
            int[] Colours = new int[4];
            Colours[0] = min;
            Colours[1] = max;


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



        internal static byte[] GetTexel(Stream pixelData, int Width)
        {
            byte[] texel = new byte[16 * 4]; // 16 pixels, 4 bytes per pixel

            int bitsPerScanLine = 4 * Width;
            for (int i = 0; i < 64; i+=16)  // pixel rows
            {
                for (int j = 0; j < 16; j+=4)  // pixels in row
                {
                    for (int k = 0; k < 4; k++) // RGBA
                    {
                        texel[i + j + k] = (byte)pixelData.ReadByte();
                    }
                }
                pixelData.Seek(bitsPerScanLine - 4 * 4, SeekOrigin.Current);
            }

            return texel;
        }
    }
}
