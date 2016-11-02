using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary.DDS
{
    /// <summary>
    /// Provides general functions specific to DDS format
    /// </summary>
    public static class DDSGeneral
    {
        static byte V8U8Adjust = 128;  // KFreon: This is for adjusting out of signed land.  This gets removed on load and re-added on save.

        /// <summary>
        /// Value at which alpha is included in DXT1 conversions. i.e. pixels lower than this threshold are made 100% transparent, and pixels higher are made 100% opaque.
        /// </summary>
        public static float DXT1AlphaThreshold = 0.2f;

        #region Header Stuff        
       
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
            header.dwCaps = 0x1000 | (Mips == 1 ? 0 : (0x8 | 0x400000));  // Flags are: 0x8 = Optional: Used for mipmapped textures | 0x400000 = DDSCAPS_MIMPAP | 0x1000 = DDSCAPS_TEXTURE
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
        internal static bool Save(List<MipMap> MipMaps, Stream Destination, Format format)
        {
            DDSGeneral.DDS_HEADER header = DDSGeneral.Build_DDS_Header(MipMaps.Count, MipMaps[0].Height, MipMaps[0].Width, format.SurfaceFormat);

            Func<byte[], byte[]> Compressor = null;
            Action<Stream, Stream, int, int> PixelWriter = null;


            switch (format.SurfaceFormat)
            {
                case ImageEngineFormat.DDS_ARGB:   // Way different method
                    using (BinaryWriter writer = new BinaryWriter(Destination, Encoding.Default, true))
                        DDSGeneral.Write_DDS_Header(header, writer);

                    try
                    {
                        unsafe
                        {
                            for (int m = 0; m < MipMaps.Count; m++)
                            {
                                MipMaps[m].BaseImage.Lock();
                                var stream = new UnmanagedMemoryStream((byte*)MipMaps[m].BaseImage.BackBuffer.ToPointer(), 4 * MipMaps[m].Width * MipMaps[m].Height);
                                stream.CopyTo(Destination);
                                MipMaps[m].BaseImage.Unlock();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        throw;
                    }


                    return true;
                case ImageEngineFormat.DDS_A8L8:
                    PixelWriter = WriteA8L8Pixel;
                    break;
                case ImageEngineFormat.DDS_RGB:
                    PixelWriter = WriteRGBPixel;
                    break;
                case ImageEngineFormat.DDS_ATI1:
                    Compressor = CompressBC4Block;
                    break;
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    Compressor = CompressBC5Block;
                    break;
                case ImageEngineFormat.DDS_DXT1:
                    Compressor = CompressBC1Block;
                    break;
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                    Compressor = CompressBC2Block;
                    break;
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    Compressor = CompressBC3Block;
                    break;
                case ImageEngineFormat.DDS_G8_L8:
                    PixelWriter = WriteG8_L8Pixel;
                    break;
                case ImageEngineFormat.DDS_V8U8:
                    PixelWriter = WriteV8U8Pixel;
                    break;
                default:
                    throw new Exception("ahaaha");
            }


            // KFreon: Set to DDS pixel writer. Needs to be here or the Compressor function is null (due to inclusion or whatever it's called)
            if (PixelWriter == null)
                PixelWriter = (writer, pixels, width, height) =>
                {
                    byte[] texel = DDSGeneral.GetTexel(pixels, width, height);
                    byte[] CompressedBlock = Compressor(texel);
                    writer.Write(CompressedBlock, 0, CompressedBlock.Length);
                };

            return DDSGeneral.WriteDDS(MipMaps, Destination, header, PixelWriter, format.IsBlockCompressed);
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
        internal static bool WriteDDS(List<MipMap> MipMaps, Stream Destination, DDS_HEADER header, Action<Stream, Stream, int, int> PixelWriter, bool isBCd)
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(Destination, Encoding.Default, true))
                    Write_DDS_Header(header, writer);

                for (int m = 0; m < MipMaps.Count; m++)
                {
                    unsafe
                    {
                        MipMaps[m].BaseImage.Lock();
                        UnmanagedMemoryStream mipmap = new UnmanagedMemoryStream((byte*)MipMaps[m].BaseImage.BackBuffer.ToPointer(), MipMaps[m].Width * MipMaps[m].Height * 4);
                        using (var compressed = WriteMipMap(mipmap, MipMaps[m].Width, MipMaps[m].Height, PixelWriter, isBCd))
                            compressed.WriteTo(Destination);

                        MipMaps[m].BaseImage.Unlock();
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
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
        private static MemoryStream WriteMipMap(Stream pixelData, int Width, int Height, Action<Stream, Stream, int, int> PixelWriter, bool isBCd)
        {
            int bitsPerScanLine = isBCd ? 4 * Width : Width;  // KFreon: Bits per image line.

            MemoryStream mipmap = new MemoryStream(bitsPerScanLine * 2);  // not accurate length requirements

            // KFreon: Loop over rows and columns, doing extra moving if Block Compressed to accommodate texels.
            int texelCount = isBCd ? Height / 4 : Height;
            int compressedLineSize = 0;
            if (texelCount == 0)
            {
                // ignore for now...
                mipmap.Write(new byte[bitsPerScanLine], 0, bitsPerScanLine); // hopefully long enough to end it
            }
            else
            {
                Action<int, ParallelLoopState> action = new Action<int, ParallelLoopState>((rowr, loopstate) =>
                {
                    int rowIndex = rowr;
                    using (var compressedLine = WriteMipLine(pixelData, Width, Height, bitsPerScanLine, isBCd, rowIndex, PixelWriter))
                    {
                        if (compressedLine != null)
                        {
                            lock (mipmap)
                            {
                                // KFreon: Detect size of a compressed line
                                if (compressedLineSize == 0)
                                    compressedLineSize = (int)compressedLine.Length;

                                mipmap.Seek(rowIndex * compressedLineSize, SeekOrigin.Begin);
                                compressedLine.WriteTo(mipmap);
                            }
                        }
                        else if (ImageEngine.EnableThreading)
                            loopstate.Break();
                        else if (!ImageEngine.EnableThreading)
                            return;
                    }
                });

                // Decide whether to thread or not
                if (ImageEngine.EnableThreading)
                {
                    ParallelOptions po = new ParallelOptions();
                    po.MaxDegreeOfParallelism = -1;
                    Parallel.For(0, texelCount, po, (rowr, loopstate) => action(rowr, loopstate));
                }
                else
                {
                    for (int rowr = 0; rowr < texelCount; rowr++)
                        action(rowr, null);
                }
                
            }

            return mipmap;
        }

        private static MemoryStream WriteMipLine(Stream pixelData, int Width, int Height, int bitsPerScanLine, bool isBCd, int rowIndex, Action<Stream, Stream, int, int> PixelWriter)
        {
            MemoryStream CompressedLine = new MemoryStream(bitsPerScanLine); // Not correct compressed size but it's close enough to not have it waste tonnes of time copying.
            using (MemoryStream UncompressedLine = new MemoryStream(4 * bitsPerScanLine))
            {
                lock (pixelData)
                {
                    // KFreon: Ensure we're in the right place
                    pixelData.Position = rowIndex * 4 * bitsPerScanLine;  // Uncompressed location

                    // KFreon: since mip count is an estimate, check to see if there are any mips left to read.
                    if (pixelData.Position >= pixelData.Length)
                        return null;

                    // KFreon: Read compressed line
                    UncompressedLine.ReadFrom(pixelData, 4 * bitsPerScanLine);
                    UncompressedLine.Position = 0;
                }

                for (int w = 0; w < Width; w += (isBCd ? 4 : 1))
                {
                    PixelWriter(CompressedLine, UncompressedLine, Width, Height);
                    if (isBCd && w != Width - 4 && Width > 4 && Height > 4)  // KFreon: Only do this if dimensions are big enough
                        UncompressedLine.Seek(-(bitsPerScanLine * 4) + 4 * 4, SeekOrigin.Current);  // Not at an row end texel. Moves back up to read next texel in row.
                }
            }

            return CompressedLine;
        }
        #endregion Save


        #region Loading
        private static MipMap ReadUncompressedMipMap(Stream stream, int mipWidth, int mipHeight, Func<Stream, List<byte>> PixelReader)
        {
            // KFreon: Since mip count is an estimate, check if there are any mips left to read.
            if (stream.Position >= stream.Length)
                return null;

            int count = 0;
            byte[] mipmap = new byte[mipHeight * mipWidth * 4];
            for (int y = 0; y < mipHeight; y++)
            {
                for (int x = 0; x < mipWidth; x++)
                {
                    List<byte> bgra = PixelReader(stream);  // KFreon: Reads pixel using a method specific to the format as provided
                    mipmap[count++] = bgra[0];
                    mipmap[count++] = bgra[1];
                    mipmap[count++] = bgra[2];
                    mipmap[count++] = bgra[3];
                }
            }

            return new MipMap(UsefulThings.WPF.Images.CreateWriteableBitmap(mipmap, mipWidth, mipHeight));
        }

        private static MipMap ReadCompressedMipMap(Stream compressed, int mipWidth, int mipHeight, int blockSize, long mipOffset, Func<Stream, List<byte[]>> DecompressBlock)
        {
            //MemoryStream mipmap = new MemoryStream(4 * mipWidth * mipHeight);
            byte[] mipmapData = new byte[4 * mipWidth * mipHeight];

            // Loop over rows and columns NOT pixels
            int compressedLineSize = blockSize * mipWidth / 4;
            int bitsPerScanline = 4 * (int)mipWidth;
            int texelCount = mipHeight / 4;
            if (texelCount != 0)
            {
                Action<int, ParallelLoopState> action = new Action<int, ParallelLoopState>((rowr, loopstate) =>
                {
                    int row = rowr;
                    using (MemoryStream DecompressedLine = ReadBCMipLine(compressed, mipHeight, mipWidth, bitsPerScanline, mipOffset, compressedLineSize, row, DecompressBlock))
                    {
                        if (DecompressedLine != null)
                            lock (mipmapData)
                            {
                                int index = row * bitsPerScanline * 4;
                                DecompressedLine.Position = 0;
                                int length = DecompressedLine.Length > mipmapData.Length ? mipmapData.Length : (int)DecompressedLine.Length;
                                if (index + length <= mipmapData.Length)
                                    DecompressedLine.Read(mipmapData, index, length);
                            }
                        else if (ImageEngine.EnableThreading)
                            loopstate.Break();
                        else if (!ImageEngine.EnableThreading)
                            return;
                    }
                });

                if (ImageEngine.EnableThreading)
                    Parallel.For(0, texelCount, (rowr, loopstate) => action(rowr, loopstate));
                else
                    for (int rowr = 0; rowr < texelCount; rowr++)
                        action(rowr, null);

            }

            return new MipMap(UsefulThings.WPF.Images.CreateWriteableBitmap(mipmapData, mipWidth, mipHeight));
        }

        private static List<byte> ReadG8_L8Pixel(Stream fileData)
        {
            byte red = (byte)fileData.ReadByte();
            byte green = red;
            byte blue = red;  // KFreon: Same colour for other channels to make grayscale.

            return new List<byte>() { blue, green, red, 0xFF };
        }

        private static List<byte> ReadV8U8Pixel(Stream fileData)
        {
            byte[] rg = fileData.ReadBytes(2);
            byte red = (byte)(rg[0] - V8U8Adjust);
            byte green = (byte)(rg[1] - V8U8Adjust);
            byte blue = 0xFF;

            return new List<byte>() { blue, green, red, 0xFF };
        }

        private static List<byte> ReadRGBPixel(Stream fileData)
        {
            var rgb = fileData.ReadBytes(3);
            byte red = rgb[0];
            byte green = rgb[1];
            byte blue = rgb[2];
            return new List<byte>() { red, green, blue, 0xFF };
        }

        private static List<byte> ReadA8L8Pixel(Stream fileData)
        {
            var al = fileData.ReadBytes(2);
            return new List<byte>() { al[0], al[0], al[0], al[1] };
        }

        internal static List<MipMap> LoadDDS(Stream compressed, DDS_HEADER header, Format format, int desiredMaxDimension)
        {           
            List<MipMap> MipMaps = new List<MipMap>();

            int mipWidth = header.dwWidth;
            int mipHeight = header.dwHeight;

            int estimatedMips = header.dwMipMapCount == 0 ? EstimateNumMipMaps(mipWidth, mipHeight) + 1 : header.dwMipMapCount;
            long mipOffset = 128;  // Includes header

            // KFreon: Check number of mips is correct. i.e. For some reason, some images have more data than they should, so it can't detected it.
            // So I check the number of mips possibly contained in the image based on size and compare it to how many it should have.
            // Any image that is too short to contain all the mips it should loads only the top mip and ignores the "others".
            int testest = 0;
            var test = EnsureMipInImage(compressed.Length, mipWidth, mipHeight, 4, format, out testest, estimatedMips);  // Update number of mips too
            if (test == -1)
                estimatedMips = 1;

            // KFreon: Decide which mip to start loading at - going to just load a few mipmaps if asked instead of loading all, then choosing later. That's slow.
            if (desiredMaxDimension != 0 && estimatedMips > 1)
            {
                int tempEstimation;
                mipOffset = EnsureMipInImage(compressed.Length, mipWidth, mipHeight, desiredMaxDimension, format, out tempEstimation);  // Update number of mips too
                if (mipOffset > 128)
                {

                    double divisor = mipHeight > mipWidth ? mipHeight / desiredMaxDimension : mipWidth / desiredMaxDimension;
                    mipHeight = (int)(mipHeight / divisor);
                    mipWidth = (int)(mipWidth / divisor);

                    if (mipWidth == 0 || mipHeight == 0)  // Reset as a dimension is too small to resize
                    {
                        mipHeight = header.dwHeight;
                        mipWidth = header.dwWidth;
                        mipOffset = 128;
                    }
                    else
                        estimatedMips = tempEstimation + 1;  // cos it needs the extra one for the top?
                }
                else
                    mipOffset = 128;

            }

            compressed.Position = mipOffset;



            Func<Stream, List<byte[]>> DecompressBCBlock = null;
            Func<Stream, List<byte>> UncompressedPixelReader = null;
            switch (format.SurfaceFormat)
            {
                case ImageEngineFormat.DDS_RGB:
                    UncompressedPixelReader = ReadRGBPixel;
                    break;
                case ImageEngineFormat.DDS_A8L8:
                    UncompressedPixelReader = ReadA8L8Pixel;
                    break;
                case ImageEngineFormat.DDS_ARGB:  // leave this one. It has a totally different reading method and is done later
                    break;
                case ImageEngineFormat.DDS_ATI1:
                    DecompressBCBlock = DecompressATI1;
                    break;
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    DecompressBCBlock = DecompressATI2Block;
                    break;
                case ImageEngineFormat.DDS_DXT1:
                    DecompressBCBlock = DecompressBC1Block;
                    break;
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                    DecompressBCBlock = DecompressBC2Block;
                    break;
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    DecompressBCBlock = DecompressBC3Block;
                    break;
                case ImageEngineFormat.DDS_G8_L8:
                    UncompressedPixelReader = ReadG8_L8Pixel;
                    break;
                case ImageEngineFormat.DDS_V8U8:
                    UncompressedPixelReader = ReadV8U8Pixel;
                    break;
                default:
                    throw new Exception("ahaaha");
            }

            // KFreon: Read mipmaps
            for (int m = 0; m < estimatedMips; m++)
            {
                // KFreon: If mip is too small, skip out. This happens most often with non-square textures. I think it's because the last mipmap is square instead of the same aspect.
                if (mipWidth <= 0 || mipHeight <= 0 || compressed.Position >= compressed.Length)  // Needed cos it doesn't throw when reading past the end for some reason.
                {
                    Debugger.Break();
                    break;
                }

                MipMap mipmap = null;
                if (format.IsBlockCompressed)
                    mipmap = ReadCompressedMipMap(compressed, mipWidth, mipHeight, format.BlockSize, mipOffset, DecompressBCBlock);
                else
                {
                    int mipLength = mipWidth * mipHeight * 4;

                    var array = new byte[mipLength];
                    long position = compressed.Position;

                    if (format.SurfaceFormat == ImageEngineFormat.DDS_ARGB)
                    {
                        compressed.Position = position;
                        compressed.Read(array, 0, array.Length);
                    }
                    else
                        mipmap = ReadUncompressedMipMap(compressed, mipWidth, mipHeight, UncompressedPixelReader);

                    if (mipmap == null)
                        mipmap = new MipMap(UsefulThings.WPF.Images.CreateWriteableBitmap(array, mipWidth, mipHeight));
                }

                MipMaps.Add(mipmap);

                mipOffset += mipWidth * mipHeight * format.BlockSize / 16; // Only used for BC textures

                mipWidth /= 2;
                mipHeight /= 2;

            }
            if (MipMaps.Count == 0)
                Debugger.Break();
            return MipMaps;
        }

        private static MemoryStream ReadBCMipLine(Stream compressed, int mipHeight, int mipWidth, int bitsPerScanLine, long mipOffset, int compressedLineSize, int rowIndex, Func<Stream, List<byte[]>> DecompressBlock)
        {
            int bitsPerPixel = 4;

            MemoryStream DecompressedLine = new MemoryStream(bitsPerScanLine * 4);

            // KFreon: Read compressed line into new stream for multithreading purposes
            using (MemoryStream CompressedLine = new MemoryStream(compressedLineSize))
            {
                lock (compressed)
                {
                    // KFreon: Seek to correct texel
                    compressed.Position = mipOffset + rowIndex * compressedLineSize;  // +128 = header size

                    // KFreon: since mip count is an estimate, check to see if there are any mips left to read.
                    if (compressed.Position >= compressed.Length)
                        return null;

                    // KFreon: Read compressed line
                    CompressedLine.ReadFrom(compressed, compressedLineSize);
                    if (CompressedLine.Length < compressedLineSize)
                        Debugger.Break();
                }
                CompressedLine.Position = 0;

                // KFreon: Read texels in row
                for (int column = 0; column < mipWidth; column += 4)
                {
                    try
                    {
                        // decompress 
                        List<byte[]> decompressed = DecompressBlock(CompressedLine);
                        byte[] blue = decompressed[0];
                        byte[] green = decompressed[1];
                        byte[] red = decompressed[2];
                        byte[] alpha = decompressed[3];


                        // Write texel
                        int TopLeft = column * bitsPerPixel;// + rowIndex * 4 * bitsPerScanLine;  // Top left corner of texel IN BYTES (i.e. expanded pixels to 4 channels)
                        DecompressedLine.Seek(TopLeft, SeekOrigin.Begin);
                        byte[] block = new byte[16];
                        for (int i = 0; i < 16; i += 4)
                        {
                            // BGRA
                            for (int j = 0; j < 16; j += 4)
                            {
                                block[j] = blue[i + (j >> 2)];
                                block[j + 1] = green[i + (j >> 2)];
                                block[j + 2] = red[i + (j >> 2)];
                                block[j + 3] = alpha[i + (j >> 2)];
                            }
                            DecompressedLine.Write(block, 0, 16);

                            // Go one line of pixels down (bitsPerScanLine), then to the left side of the texel (4 pixels back from where it finished)
                            DecompressedLine.Seek(bitsPerScanLine - bitsPerPixel * 4, SeekOrigin.Current);
                        }
                    }
                    catch
                    {
                        // Ignore. Most likely error reading smaller mips that don't behave
                    }
                    
                }
            }
                
            return DecompressedLine;
        }
        #endregion Loading


        #region Mipmap Management
        /// <summary>
        /// Ensures all Mipmaps are generated in MipMaps.
        /// </summary>
        /// <param name="MipMaps">MipMaps to check.</param>
        /// <param name="mergeAlpha">True = flattens alpha, directly affecting RGB.</param>
        /// <returns>Number of mipmaps present in MipMaps.</returns>
        internal static int BuildMipMaps(List<MipMap> MipMaps, bool mergeAlpha)
        {
            if (MipMaps?.Count == 0)
                return 0;

            MipMap currentMip = MipMaps[0];

            // KFreon: Check if mips required
            int estimatedMips = DDSGeneral.EstimateNumMipMaps(currentMip.Width, currentMip.Height);
            if (MipMaps.Count > 1)
                return estimatedMips;

            // KFreon: Half dimensions until one == 1.
            MipMap[] newmips = new MipMap[estimatedMips];

            Action<int> action = new Action<int>(item =>
            {
                int index = item;
                MipMap newmip;
                newmip = ImageEngine.Resize(currentMip, 1f / Math.Pow(2, index), mergeAlpha);
                newmips[index - 1] = newmip;
            });

            // Start at 1 to skip top mip
            if (ImageEngine.EnableThreading)
                Parallel.For(1, estimatedMips + 1, item => action(item));
            else
                for (int item = 1; item < estimatedMips + 1; item++)
                    action(item);

            MipMaps.AddRange(newmips);
            return estimatedMips;
        }

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

        public static long EnsureMipInImage(long streamLength, int mainWidth, int mainHeight, int desiredMaxDimension, Format format, out int numMipMaps, double mipIndex = -1)
        {
            // TODO: Is the other estimated mips required?

            if (mainWidth <= desiredMaxDimension && mainHeight <= desiredMaxDimension)
            {
                numMipMaps = EstimateNumMipMaps(mainWidth, mainHeight);
                return 128; // One mip only
            }


            int dependentDimension = mainWidth > mainHeight ? mainWidth : mainHeight;

            mipIndex = mipIndex == -1 ? Math.Log((dependentDimension / desiredMaxDimension), 2) - 1 : mipIndex;
            if (mipIndex < -1)
                throw new InvalidDataException($"Invalid dimensions for mipmapping. Got desired: {desiredMaxDimension} and dependent: {dependentDimension}");


            int requiredOffset = (int)ImageEngine.ExpectedImageSize(mipIndex, format, mainHeight, mainWidth);  // +128 for header

            int limitingDimension = mainWidth > mainHeight ? mainHeight : mainWidth;
            double newDimDivisor = limitingDimension * 1f / desiredMaxDimension;
            numMipMaps = EstimateNumMipMaps((int)(mainWidth / newDimDivisor), (int)(mainHeight / newDimDivisor));

            // KFreon: Something wrong with the count here by 1 i.e. the estimate is 1 more than it should be 
            if (format.SurfaceFormat == ImageEngineFormat.DDS_ARGB)
                requiredOffset -= 2;

            // Should only occur when an image has no mips
            if (streamLength < requiredOffset)
                return -1;

            return requiredOffset;
        }
        #endregion Mipmap Management
    }
}
