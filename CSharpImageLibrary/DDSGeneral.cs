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
        #endregion Header Stuff


        #region Loading
        /// <summary>
        /// Loads an uncompressed DDS image given format specific Pixel Reader
        /// </summary>
        /// <param name="stream">Stream containing entire image. NOT just pixels.</param>
        /// <param name="NumChannels">Number of colour channels in image. (RGBA = 4)</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="PixelReader">Function that knows how to read a pixel. Different for each format (V8U8, RGBA)</param>
        /// <returns></returns>
        internal static MemoryTributary LoadUncompressed(Stream stream, int NumChannels, out double Width, out double Height, Func<Stream, int> PixelReader)
        {
            // KFreon: Necessary to move stream position along to pixel data.
            DDS_HEADER header = null;
            Format format = ImageFormats.ParseDDSFormat(stream, out header);

            Width = header.dwWidth;
            Height = header.dwHeight;

            int mipMapBytes = (int)(Width * Height * NumChannels);  // KFreon: 2 bytes per pixel
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
        internal static MemoryTributary LoadBlockCompressedTexture(Stream compressed, out double Width, out double Height, Func<Stream, List<byte[]>> DecompressBlock)
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
        /// <returns>Single channel decompressed (16 bits).</returns>
        internal static byte[] Decompress8BitBlock(Stream compressed)
        {
            byte[] DecompressedBlock = new byte[16];

            // KFreon: Read colour range and build palette
            ushort[] Colours = new ushort[8];

            // KFreon: Read min and max colours (not necessarily in that order)
            Colours[0] = (byte)compressed.ReadByte();
            Colours[1] = (byte)compressed.ReadByte();

            // KFreon: Choose which type of interpolation required.
            if (Colours[0] > Colours[1])
            {
                // KFreon: Interpolate other colours
                for (int i = 2; i < 8; i++)
                {
                    int firstbit = (8 - i);
                    int secondbit = (i - 1);
                    double test = (firstbit * Colours[0] + secondbit * Colours[1]) / 7.0f;
                    Colours[i] = (ushort)test;
                }
            }
            else
            {
                // KFreon: Interpolate other colours and add OPACITY
                for (int i = 2; i < 6; i++)
                    Colours[i] = (ushort)(((6 - i) * Colours[0] + (i - 1) * Colours[1]) / 5.0);
                Colours[6] = 0;
                Colours[7] = 255;
            }


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
    }
}
