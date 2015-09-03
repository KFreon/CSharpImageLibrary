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
                h.dwReserved1[i] = r.ReadInt32();
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
            p.dwRBitMask = r.ReadUInt32();
            p.dwGBitMask = r.ReadUInt32();
            p.dwBBitMask = r.ReadUInt32();
            p.dwABitMask = r.ReadUInt32();
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

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("--DDS_HEADER--");
                sb.AppendLine($"dwSize: {dwSize}");
                sb.AppendLine($"dwFlags: 0x{dwFlags.ToString("X")}");  // As hex
                sb.AppendLine($"dwHeight: {dwHeight}");
                sb.AppendLine($"dwWidth: {dwWidth}");
                sb.AppendLine($"dwPitchOrLinearSize: {dwPitchOrLinearSize}");
                sb.AppendLine($"dwDepth: {dwDepth}");
                sb.AppendLine($"dwMipMapCount: {dwMipMapCount}");
                sb.AppendLine($"ddspf: ");
                sb.AppendLine(ddspf.ToString());
                sb.AppendLine($"dwCaps: 0x{dwCaps.ToString("X")}");
                sb.AppendLine($"dwCaps2: {dwCaps2}");
                sb.AppendLine($"dwCaps3: {dwCaps3}");
                sb.AppendLine($"dwCaps4: {dwCaps4}");
                sb.AppendLine($"dwReserved2: {dwReserved2}");
                sb.AppendLine("--END DDS_HEADER--");
                return sb.ToString();
            }
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
            public uint dwRBitMask;
            public uint dwGBitMask;
            public uint dwBBitMask;
            public uint dwABitMask;

            public DDS_PIXELFORMAT()
            {
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("--DDS_PIXELFORMAT--");
                sb.AppendLine($"dwSize: {dwSize}");
                sb.AppendLine($"dwFlags: 0x{dwFlags.ToString("X")}");  // As hex
                sb.AppendLine($"dwFourCC: 0x{dwFourCC.ToString("X")}");  // As Hex
                sb.AppendLine($"dwRGBBitCount: {dwRGBBitCount}");
                sb.AppendLine($"dwRBitMask: 0x{dwRBitMask.ToString("X")}");  // As Hex
                sb.AppendLine($"dwGBitMask: 0x{dwGBitMask.ToString("X")}");  // As Hex
                sb.AppendLine($"dwBBitMask: 0x{dwBBitMask.ToString("X")}");  // As Hex
                sb.AppendLine($"dwABitMask: 0x{dwABitMask.ToString("X")}");  // As Hex
                sb.AppendLine("--END DDS_PIXELFORMAT--");
                return sb.ToString();
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
            header.dwFlags = 0x1 | 0x2 | 0x4 | 0x0 | 0x1000 | (Mips != 1 ? 0x20000 : 0x0) | 0x0 | 0x0;  // Flags to denote fields: DDSD_CAPS = 0x1 | DDSD_HEIGHT = 0x2 | DDSD_WIDTH = 0x4 | DDSD_PITCH = 0x8 | DDSD_PIXELFORMAT = 0x1000 | DDSD_MIPMAPCOUNT = 0x20000 | DDSD_LINEARSIZE = 0x80000 | DDSD_DEPTH = 0x800000
            header.dwWidth = Width;
            header.dwHeight = Height;
            header.dwCaps = 0x1000 | (Mips != 1 ? 0 : (0x8 | 0x400000));  // Flags are: 0x8 = Optional: Used for mipmapped textures | 0x400000 = DDSCAPS_MIMPAP | 0x1000 = DDSCAPS_TEXTURE
            header.dwMipMapCount = Mips == 1 ? 1 : Mips;

            DDS_PIXELFORMAT px = new DDS_PIXELFORMAT();
            px.dwSize = 32;
            px.dwFourCC = (int)surfaceformat;
            px.dwFlags = 4;

            switch (surfaceformat)
            {
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    px.dwFlags |= 0x80000;
                    header.dwPitchOrLinearSize = (int)(Width * Height);
                    break;
                case ImageEngineFormat.DDS_ATI1:
                    header.dwFlags |= 0x80000;  
                    header.dwPitchOrLinearSize = (int)(Width * Height / 2f);
                    break;
                case ImageEngineFormat.DDS_G8_L8:
                    px.dwFlags = 0x20000;
                    header.dwPitchOrLinearSize = Width * 8; // maybe?
                    header.dwFlags |= 0x8;
                    px.dwRGBBitCount = 8;
                    px.dwRBitMask = 0xFF;
                    px.dwFourCC = 0x0;
                    break;
                case ImageEngineFormat.DDS_ARGB:
                    px.dwFlags = 0x41;
                    px.dwFourCC = 0x0;
                    px.dwRGBBitCount = 32;
                    px.dwRBitMask = 0xFF0000;
                    px.dwGBitMask = 0xFF00;
                    px.dwBBitMask = 0xFF;
                    px.dwABitMask = 0xFF000000;
                    break;
                case ImageEngineFormat.DDS_V8U8:
                    px.dwFourCC = 0x0;
                    px.dwFlags = 0x80000;  // 0x80000 not actually a valid value....
                    px.dwRGBBitCount = 16;
                    px.dwRBitMask = 0xFF;
                    px.dwGBitMask = 0xFF00;
                    break;
            }
            

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


        #region Saving
        /// <summary>
        /// Writes a block compressed DDS to stream. Uses format specific function to compress and write blocks.
        /// </summary>
        /// <param name="MipMaps">List of MipMaps to save. Pixels only.</param>
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="header">Header of DDS to use.</param>
        /// <param name="CompressBlock">Function to compress and write blocks with.</param>
        /// <returns>True on success.</returns>
        internal static bool WriteBlockCompressedDDS(List<MipMap> MipMaps, Stream Destination, DDS_HEADER header, Func<byte[], byte[]> CompressBlock)
        {
            Action<BinaryWriter, Stream, int, int> PixelWriter = (writer, pixels, width, height) =>
            {
                byte[] texel = DDSGeneral.GetTexel(pixels, width, height);
                byte[] CompressedBlock = CompressBlock(texel);
                writer.Write(CompressedBlock);
            };

            return DDSGeneral.WriteDDS(MipMaps, Destination, header, PixelWriter, true);
        }


        /// <summary>
        /// Writes a DDS file using a format specific function to write pixels.
        /// </summary>
        /// <param name="MipMaps">List of MipMaps to save. Pixels only.</param>
        /// <param name="Destination">Stream to save to.</param>
        /// <param name="header">Header to use.</param>
        /// <param name="PixelWriter">Function to write pixels. Optionally also compresses blocks before writing.</param>
        /// <param name="isBCd">True = Block Compressed DDS. Performs extra manipulation to get and order Texels.</param>
        /// <returns>True on success.</returns>
        internal static bool WriteDDS(List<MipMap> MipMaps, Stream Destination, DDS_HEADER header, Action<BinaryWriter, Stream, int, int> PixelWriter, bool isBCd)
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(Destination, Encoding.Default, true))
                {
                    Write_DDS_Header(header, writer);
                    for (int m = 0; m < MipMaps.Count ; m++)
                    {
                        MemoryStream mipmap = MipMaps[m].Data;
                        mipmap.Seek(0, SeekOrigin.Begin);
                        WriteMipMap(mipmap, MipMaps[m].Width, MipMaps[m].Height, PixelWriter, isBCd, writer);
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


        /// <summary>
        /// Write a mipmap to a stream using a format specific pixel writing function.
        /// </summary>
        /// <param name="pixelData">Pixels of mipmap.</param>
        /// <param name="Width">Mipmap Width.</param>
        /// <param name="Height">Mipmap Height.</param>
        /// <param name="PixelWriter">Function to write pixels with. Also compresses if block compressed texture.</param>
        /// <param name="isBCd">True = Block Compressed DDS.</param>
        /// <param name="writer">Stream to write to.</param>
        private static void WriteMipMap(Stream pixelData, int Width, int Height, Action<BinaryWriter, Stream, int, int> PixelWriter, bool isBCd, BinaryWriter writer)
        {
            int bitsPerScanLine = 4 * Width;  // KFreon: Bits per image line.

            // KFreon: Loop over rows and columns, doing extra moving if Block Compressed to accommodate texels.
            for (int h = 0; h < Height; h += (isBCd ? 4 : 1))
            {
                for (int w = 0; w < Width; w += (isBCd ? 4 : 1))
                {
                    PixelWriter(writer, pixelData, Width, Height);
                    if (isBCd && w != Width - 4 && Width > 4 && Height > 4)  // KFreon: Only do this if dimensions are big enough
                        pixelData.Seek(-(bitsPerScanLine * 4) + 4 * 4, SeekOrigin.Current);  // Not at an row end texel. Moves back up to read next texel in row.
                }

                if (isBCd && Width > 4 && Height > 4)  // Only do this jump if dimensions allow it
                    pixelData.Seek(-bitsPerScanLine + 4 * 4, SeekOrigin.Current);  // Row end texel. Just need to add 1.
            }
        }
        #endregion Save


        #region Loading
        /// <summary>
        /// Loads an uncompressed DDS image given format specific Pixel Reader
        /// </summary>
        /// <param name="stream">Stream containing entire image. NOT just pixels.</param>
        /// <param name="PixelReader">Function that knows how to read a pixel. Different for each format (V8U8, BGRA)</param>
        /// <returns></returns>
        internal static List<MipMap> LoadUncompressed(Stream stream, Func<Stream, List<byte>> PixelReader)
        {
            // KFreon: Necessary to move stream position along to pixel data.
            DDS_HEADER header = null;
            Format format = ImageFormats.ParseDDSFormat(stream, out header);

            List<MipMap> MipMaps = new List<MipMap>();

            int newWidth = header.dwWidth;
            int newHeight = header.dwHeight;

            // KFreon: Read data
            for (int m = 0; m < header.dwMipMapCount; m++)
            {
                int count = 0;
                byte[] mipmap = new byte[newHeight * newWidth * 4];
                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        List<byte> bgr = PixelReader(stream);  // KFreon: Reads pixel using a method specific to the format as provided
                        mipmap[count++] = bgr[0];
                        mipmap[count++] = bgr[1];
                        mipmap[count++] = bgr[2];
                        mipmap[count++] = 0xFF;
                    }
                }
                MipMaps.Add(new MipMap(UsefulThings.RecyclableMemoryManager.GetStream(mipmap), newWidth, newHeight));

                newWidth /= 2;
                newHeight /= 2;
            }

            return MipMaps;
        }


        /// <summary>
        /// Loads a block compressed (BCx) texture.
        /// </summary>
        /// <param name="compressed">Compressed image data.</param>
        /// <param name="DecompressBlock">Format specific block decompressor.</param>
        /// <returns>16 pixel BGRA channels.</returns>
        internal static List<MipMap> LoadBlockCompressedTexture(Stream compressed, Func<Stream, List<byte[]>> DecompressBlock)
        {
            DDS_HEADER header;
            Format format = ImageFormats.ParseDDSFormat(compressed, out header);
            int bitsPerPixel = 4;

            List<MipMap> MipMaps = new List<MipMap>();

            int mipWidth = header.dwWidth;
            int mipHeight = header.dwHeight;

            for (int m = 0; m < header.dwMipMapCount; m++)
            {
                MemoryStream mipmap = UsefulThings.RecyclableMemoryManager.GetStream(bitsPerPixel * (int)mipWidth * (int)mipHeight);

                // Loop over rows and columns NOT pixels
                int bitsPerScanline = bitsPerPixel * (int)mipWidth;
                for (int row = 0; row < mipHeight; row += 4)
                {
                    for (int column = 0; column < mipWidth; column += 4)
                    {
                        // decompress 
                        List<byte[]> decompressed = DecompressBlock(compressed);
                        byte[] blue = decompressed[0];
                        byte[] green = decompressed[1];
                        byte[] red = decompressed[2];
                        byte[] alpha = decompressed[3];


                        // Write texel
                        int TopLeft = column * bitsPerPixel + row * bitsPerScanline;  // Top left corner of texel IN BYTES (i.e. expanded pixels to 4 channels)
                        mipmap.Seek(TopLeft, SeekOrigin.Begin);
                        byte[] block = new byte[16];
                        for (int i = 0; i < 16; i += 4)
                        {
                            // BGRA
                            for (int j = 0; j < 16; j+=4)
                            {
                                block[j] = blue[i + (j >> 2)];
                                block[j+1] = green[i + (j >> 2)];
                                block[j+2] = red[i + (j >> 2)];
                                block[j+3] = alpha[i + (j >> 2)];
                            }
                            mipmap.Write(block, 0, 16);

                            // Go one line of pixels down (bitsPerScanLine), then to the left side of the texel (4 pixels back from where it finished)
                            mipmap.Seek(bitsPerScanline - bitsPerPixel * 4, SeekOrigin.Current);
                        }
                    }
                }
                MipMaps.Add(new MipMap(mipmap, mipWidth, mipHeight));

                mipWidth /= 2;
                mipHeight /= 2;
            }
            
            return MipMaps;
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
        /// <returns>16 pixel BGRA channels.</returns>
        internal static List<byte[]> DecompressRGBBlock(Stream compressed, bool isDXT1)
        {
            int[] DecompressedBlock = new int[16];
            int[] Colours = null;

            // Read min max colours
            BinaryReader reader = new BinaryReader(compressed);
            ushort min = (ushort)reader.ReadInt16();
            ushort max = (ushort)reader.ReadInt16();
            Colours = BuildRGBPalette(min, max, isDXT1);

            // Decompress pixels
            for (int i = 0; i < 16; i+=4)
            {
                byte bitmask = (byte)compressed.ReadByte();
                for (int j = 0; j < 4; j++)
                    DecompressedBlock[i + j] = Colours[bitmask >> (2 * j) & 0x03];
            }

            // KFreon: Decode into BGRA
            List<byte[]> DecompressedChannels = new List<byte[]>(4);
            byte[] red = new byte[16];
            byte[] green = new byte[16];
            byte[] blue = new byte[16];
            byte[] alpha = new byte[16];
            DecompressedChannels.Add(blue);
            DecompressedChannels.Add(green);
            DecompressedChannels.Add(red);
            DecompressedChannels.Add(alpha);

            for (int i = 0; i < 16; i++)
            {
                int colour = DecompressedBlock[i];
                var rgb = ReadDXTColour(colour);
                red[i] = rgb[0];
                green[i] = rgb[1];
                blue[i] = rgb[2];
                alpha[i] = (byte)(colour == 0 && max > min ? 0x0 : 0xFF);
            }
            return DecompressedChannels;
        }
        #endregion


        #region Block Compression
        /// <summary>
        /// Compresses RGB texel into DXT colours.
        /// </summary>
        /// <param name="texel">4x4 Texel to compress.</param>
        /// <param name="min">Minimum Colour value.</param>
        /// <param name="max">Maximum Colour value.</param>
        /// <returns>DXT Colours</returns>
        internal static int[] CompressRGBFromTexel(byte[] texel, out int min, out int max)
        {
            int[] RGB = new int[16];
            int count = 0;
            for (int i = 0; i < 64; i += 16) // texel row
            {
                for (int j = 0; j < 16; j += 4)  // pixels in row incl BGRA
                {
                    int pixelColour = BuildDXTColour(texel[i + j + 2], texel[i + j + 1], texel[i + j]);
                    RGB[count++] = pixelColour;
                }
            }

            min = RGB.Min();
            max = RGB.Max();

            return RGB;
        }


        /// <summary>
        /// Compresses RGB channels using Block Compression.
        /// </summary>
        /// <param name="texel">16 pixel texel to compress.</param>
        /// <param name="isDXT1">Set true if DXT1.</param>
        /// <returns>8 byte compressed texel.</returns>
        public static byte[] CompressRGBBlock(byte[] texel, bool isDXT1)
        {
            byte[] CompressedBlock = new byte[8];

            // Get Min and Max colours
            int min = 0;
            int max = 0;
            int[] texelColours = CompressRGBFromTexel(texel, out min, out max);

            if (isDXT1)
            {
                // KFreon: Check alpha (every 4 bytes)
                for (int i = 3; i < texel.Length; i += 4)
                {
                    if (texel[i] != 0xFF) // Alpha found, switch min and max
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
                byte fourIndicies = 0;
                for (int j = 0; j < 4; j++)
                {
                    int colour = texelColours[i + j];
                    int index = Colours.IndexOfMin(c => Math.Abs(colour - c));
                    fourIndicies |= (byte)(index << (2 * j));
                }
                CompressedBlock[i / 4 + 4] = fourIndicies;
            }

            return CompressedBlock;
        }

        /// <summary>
        /// Compresses single channel using Block Compression.
        /// </summary>
        /// <param name="texel">4 channel Texel to compress.</param>
        /// <param name="channel">0-3 (BGRA)</param>
        /// <param name="isSigned">true = uses alpha range -255 -- 255, else 0 -- 255</param>
        /// <returns>8 byte compressed texel.</returns>
        public static byte[] Compress8BitBlock(byte[] texel, int channel, bool isSigned)
        {
            // KFreon: Get min and max
            byte min = byte.MaxValue;
            byte max = byte.MinValue;
            int count = channel;
            for (int i = 0; i < 16; i++)
            {
                byte colour = texel[count];
                if (colour > max)
                    max = colour;
                else if (colour < min)
                    min = colour;

                count += 4; // skip to next entry in channel
            }

            // Build Palette
            byte[] Colours = Build8BitPalette(min, max, isSigned);

            // Compress Pixels
            ulong line = 0;
            count = channel;
            List<byte> indicies = new List<byte>();
            for (int i = 0; i < 16; i++)
            {
                byte colour = texel[count];
                byte index = (byte)Colours.IndexOfMin(c => Math.Abs(colour - c));
                indicies.Add(index);
                line |= (ulong)index << (i * 3); 
                count += 4;  // Only need 1 channel
            }

            byte[] CompressedBlock = new byte[8];
            byte[] compressed = BitConverter.GetBytes(line);
            CompressedBlock[0] = min;
            CompressedBlock[1] = max;
            for (int i = 2; i < 8; i++)
                CompressedBlock[i] = compressed[i - 2];

            return CompressedBlock;
        }
        #endregion Block Compression


        /// <summary>
        /// Gets 4x4 texel block from stream.
        /// </summary>
        /// <param name="pixelData">Image pixels.</param>
        /// <param name="Width">Width of image.</param>
        /// <param name="Height">Height of image.</param>
        /// <returns>4x4 texel.</returns>
        internal static byte[] GetTexel(Stream pixelData, int Width, int Height)
        {
            byte[] texel = new byte[16 * 4]; // 16 pixels, 4 bytes per pixel

            // KFreon: Edge case for when dimensions are too small for texel
            int count = 0;
            if (Width < 4 || Height < 4)
            {
                for (int h = 0; h < Height; h++)
                    for (int w = 0; w < Width; w++)
                        for (int i = 0; i < 4; i++)
                            texel[count++] = (byte)pixelData.ReadByte();

                return texel;
            }

            // KFreon: Normal operation. Read 4x4 texel row by row.
            int bitsPerScanLine = 4 * Width;
            for (int i = 0; i < 64; i += 16)  // pixel rows
            {
                for (int j = 0; j < 16; j += 4)  // pixels in row
                    for (int k = 0; k < 4; k++) // BGRA
                        texel[i + j + k] = (byte)pixelData.ReadByte();

                pixelData.Seek(bitsPerScanLine - 4 * 4, SeekOrigin.Current);  // Seek to next line of texel
            }
                

            return texel;
        }


        #region Palette/Colour
        /// <summary>
        /// Reads a packed DXT colour into RGB
        /// </summary>
        /// <param name="colour"></param>
        /// <returns>RGB bytes</returns>
        private static byte[] ReadDXTColour(int colour)
        {
            // Read RGB 5:6:5 data
            var b = (colour & 0x1F);
            var g = (colour & 0x7E0) >> 5;
            var r = (colour & 0xF800) >> 11;

            // Expand to 8 bit data
            byte r1 = (byte)Math.Round(r * 255f / 31f);
            byte g1 = (byte)Math.Round(g * 255f / 63f);
            byte b1 = (byte)Math.Round(b * 255f / 31f);

            return new byte[3] { r1, g1, b1 };
        }


        /// <summary>
        /// Creates a packed DXT colour from RGB.
        /// </summary>
        /// <param name="r">Red byte.</param>
        /// <param name="g">Green byte.</param>
        /// <param name="b">Blue byte.</param>
        /// <returns>DXT Colour</returns>
        private static int BuildDXTColour(byte r, byte g, byte b)
        {
            // Compress to 5:6:5
            byte r1 = (byte)(Math.Round(r * 31f / 255f));
            byte g1 = (byte)(Math.Round(g * 63f / 255f));
            byte b1 = (byte)(Math.Round(b * 31f / 255f));

            return r1 << 11 | g1 << 5 | b1;
        }


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
        /// <param name="min">First main colour. Often actually the minumum.</param>
        /// <param name="max">Second main colour. Often actually the maximum.</param>
        /// <param name="isDXT1">true = use DXT1 format (1 bit alpha)</param>
        /// <returns>4 Colours as integers.</returns>
        public static int[] BuildRGBPalette(int min, int max, bool isDXT1)
        {
            int[] Colours = new int[4];
            Colours[0] = min;
            Colours[1] = max;

            var minrgb = ReadDXTColour(min);
            var maxrgb = ReadDXTColour(max);

            // Interpolate other 2 colours
            if (min > max || !isDXT1)
            {
                var r = (byte)(2 / 3f * minrgb[0] + 1 / 3f * maxrgb[0]);
                var g = (byte)(2 / 3f * minrgb[1] + 1 / 3f * maxrgb[1]);
                var b = (byte)(2 / 3f * minrgb[2] + 1 / 3f * maxrgb[2]);

                Colours[2] = BuildDXTColour(r, g, b);

                r = (byte)(1 / 3f * minrgb[0] + 2 / 3f * maxrgb[0]);
                g = (byte)(1 / 3f * minrgb[1] + 2 / 3f * maxrgb[1]);
                b = (byte)(1 / 3f * minrgb[2] + 2 / 3f * maxrgb[2]);

                Colours[3] = BuildDXTColour(r, g, b);
            }
            else
            {
                // KFreon: Only for dxt1
                var r = (byte)(1 / 2f * minrgb[0] + 1 / 2f * maxrgb[0]);
                var g = (byte)(1 / 2f * minrgb[1] + 1 / 2f * maxrgb[1]);
                var b = (byte)(1 / 2f * minrgb[2] + 1 / 2f * maxrgb[2]);
            
                Colours[2] = BuildDXTColour(r, g, b);
                Colours[3] = 0;
            }

            return Colours;
        }
        #endregion Palette/Colour
        

        /// <summary>
        /// Estimates number of MipMaps for a given width and height EXCLUDING the top one.
        /// i.e. If output is 10, there are 11 mipmaps total.
        /// </summary>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>Number of mipmaps expected for image.</returns>
        internal static int EstimateNumMipMaps(int Width, int Height)
        {
            int limitingDimension = Width > Height ? Height : Width;
            return (int)Math.Log(limitingDimension, 2); // There's 10 mipmaps besides the main top one.
        }
    }
}
