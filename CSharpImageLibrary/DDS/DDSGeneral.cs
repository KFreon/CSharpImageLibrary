using CSharpImageLibrary.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace CSharpImageLibrary.DDS
{
    /// <summary>
    /// Provides general functions specific to DDS format
    /// </summary>
    public static class DDSGeneral
    {
        /// <summary>
        /// Value at which alpha is included in DXT1 conversions. i.e. pixels lower than this threshold are made 100% transparent, and pixels higher are made 100% opaque.
        /// </summary>
        public static double DXT1AlphaThreshold = 0.2;


        #region Loading
        private static MipMap ReadUncompressedMipMap(MemoryStream stream, int mipOffset, int mipWidth, int mipHeight, DDS_Header.DDS_PIXELFORMAT ddspf)
        {
            byte[] data = stream.GetBuffer();
            byte[] mipmap = new byte[mipHeight * mipWidth * 4];
            DDS_Decoders.ReadUncompressed(data, mipOffset, mipmap, mipWidth * mipHeight, ddspf);

            return new MipMap(mipmap, mipWidth, mipHeight);
        }

        private static MipMap ReadCompressedMipMap(MemoryStream compressed, int mipWidth, int mipHeight, int blockSize, int mipOffset, bool isPremultiplied, Action<byte[], int, byte[], int, int, bool> DecompressBlock)
        {
            // Gets stream as data. Note that this array isn't necessarily the correct size. Likely to have garbage at the end.
            // Don't want to use ToArray as that creates a new array. Don't want that.
            byte[] CompressedData = compressed.GetBuffer();

            byte[] decompressedData = new byte[4 * mipWidth * mipHeight];
            int decompressedRowLength = mipWidth * 4;
            int texelRowSkip = decompressedRowLength * 4;

            int texelCount = (mipWidth * mipHeight) / 16;
            int numTexelsInRow = mipWidth / 4;
            if (texelCount != 0)
            {
                Action<int, ParallelLoopState> action = new Action<int, ParallelLoopState>((texelIndex, loopstate) =>
                {
                    int compressedPosition = mipOffset + texelIndex * blockSize;
                    int decompressedStart = (int)(texelIndex / numTexelsInRow) * texelRowSkip + (texelIndex % numTexelsInRow) * 16;

                    // Problem with how I handle dimensions (no virtual padding or anything yet)
                    if (!UsefulThings.General.IsPowerOfTwo(mipWidth) || !UsefulThings.General.IsPowerOfTwo(mipHeight))
                        return;

                    try
                    {
                        DecompressBlock(CompressedData, compressedPosition, decompressedData, decompressedStart, decompressedRowLength, isPremultiplied);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw;
                    }
                });

                // Actually perform decompression using threading, no threading, or GPU.
                if (ImageEngine.EnableGPUAcceleration)
                    Debugger.Break(); 
                else if (ImageEngine.EnableThreading)
                    Parallel.For(0, texelCount, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, (texelIndex, loopstate) => action(texelIndex, loopstate));
                else
                    for (int texelIndex = 0; texelIndex < texelCount; texelIndex++)
                        action(texelIndex, null);
            }
            // No else here cos the lack of texels means it's below texel dimensions (4x4). So the resulting block is set to 0. Not ideal, but who sees 2x2 mipmaps?

            return new MipMap(decompressedData, mipWidth, mipHeight);
        }

        

        internal static List<MipMap> LoadDDS(MemoryStream compressed, DDS_Header header, int desiredMaxDimension)
        {
            MipMap[] MipMaps = null;

            int mipWidth = header.Width;
            int mipHeight = header.Height;
            ImageEngineFormat format = header.Format;
            int blockSize = ImageFormats.GetBlockSize(format);

            int estimatedMips = header.dwMipMapCount;
            int mipOffset = 128;  // Includes header. 
            // TODO: Incorrect mip offset for DX10

            if (!EnsureMipInImage(compressed.Length, mipWidth, mipHeight, 4, format, out mipOffset))  // Update number of mips too
                estimatedMips = 1;

            if (estimatedMips == 0)
                estimatedMips = EstimateNumMipMaps(mipWidth, mipHeight);

            mipOffset = 128;  // Needs resetting after checking there's mips in this image.

            // TESTUNIG
            if (estimatedMips == 0)
                estimatedMips = 1;

            int orig_estimatedMips = estimatedMips; // Store original count for later (uncompressed only I think)

            // KFreon: Decide which mip to start loading at - going to just load a few mipmaps if asked instead of loading all, then choosing later. That's slow.
            if (desiredMaxDimension != 0 && estimatedMips > 1)
            {
                if (!EnsureMipInImage(compressed.Length, mipWidth, mipHeight, desiredMaxDimension, format, out mipOffset))  // Update number of mips too
                    throw new InvalidDataException($"Requested mipmap does not exist in this image. Top Image Size: {mipWidth}x{mipHeight}, requested mip max dimension: {desiredMaxDimension}.");

                // Not the first mipmap. 
                if (mipOffset > 128)
                {

                    double divisor = mipHeight > mipWidth ? mipHeight / desiredMaxDimension : mipWidth / desiredMaxDimension;
                    mipHeight = (int)(mipHeight / divisor);
                    mipWidth = (int)(mipWidth / divisor);

                    if (mipWidth == 0 || mipHeight == 0)  // Reset as a dimension is too small to resize
                    {
                        mipHeight = header.Height;
                        mipWidth = header.Width;
                        mipOffset = 128;
                    }
                    else
                    {
                        // Update estimated mips due to changing dimensions.
                        estimatedMips = EstimateNumMipMaps(mipWidth, mipHeight);
                    }
                }
                else  // The first mipmap
                    mipOffset = 128;

            }

            // Move to requested mipmap
            compressed.Position = mipOffset;

            // Block Compressed texture chooser.
            Action<byte[], int, byte[], int, int, bool> DecompressBCBlock = null;
            switch (format)
            {
                case ImageEngineFormat.DDS_DXT1:
                    DecompressBCBlock = DDS_Decoders.DecompressBC1Block;
                    break;
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                    DecompressBCBlock = DDS_Decoders.DecompressBC2Block;
                    break;
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    DecompressBCBlock = DDS_Decoders.DecompressBC3Block;
                    break;
                case ImageEngineFormat.DDS_ATI1:
                    DecompressBCBlock = DDS_Decoders.DecompressATI1;
                    break;
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    DecompressBCBlock = DDS_Decoders.DecompressATI2Block;
                    break;
            }

            MipMaps = new MipMap[estimatedMips];

            // KFreon: Read mipmaps
            if (ImageFormats.IsBlockCompressed(format))    // Threading done in the decompression, not here.
            {
                for (int m = 0; m < estimatedMips; m++)
                {
                    // KFreon: If mip is too small, skip out. This happens most often with non-square textures. I think it's because the last mipmap is square instead of the same aspect.
                    if (mipWidth <= 0 || mipHeight <= 0)  // Needed cos it doesn't throw when reading past the end for some reason.
                    {
                        break;
                    }
                    MipMap mipmap = ReadCompressedMipMap(compressed, mipWidth, mipHeight, blockSize, mipOffset, (format == ImageEngineFormat.DDS_DXT2 || format == ImageEngineFormat.DDS_DXT4), DecompressBCBlock);

                    MipMaps[m] = mipmap;
                    mipOffset += (int)(mipWidth * mipHeight * (blockSize / 16d)); // Also put the division in brackets cos if the mip dimensions are high enough, the multiplications can exceed int.MaxValue)
                    mipWidth /= 2;
                    mipHeight /= 2;
                }
            }
            else
            {
                int startMip = orig_estimatedMips - estimatedMips;

                // UNCOMPRESSED - Can't really do threading in "decompression" so do it for the mipmaps.
                var action = new Action<int>(mipIndex =>
                {
                    // Calculate mipOffset and dimensions
                    int offset, width, height;
                    offset = GetMipOffset(mipIndex, format, header.Width, header.Height);
                    width = (int)(header.Width / Math.Pow(2, mipIndex));
                    height = (int)(header.Height / Math.Pow(2, mipIndex));

                    MipMap mipmap = null;
                    try
                    {
                        mipmap = ReadUncompressedMipMap(compressed, offset, width, height, header.ddspf);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }

                    MipMaps[mipIndex] = mipmap;
                });

                if (ImageEngine.EnableThreading)
                    Parallel.For(startMip, orig_estimatedMips, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, action);
                else
                    for (int i = startMip; i < orig_estimatedMips; i++)
                        action(i);
            }

            List<MipMap> mips = new List<MipMap>(MipMaps.Where(t => t != null));
            if (mips.Count == 0)
                throw new InvalidOperationException($"No mipmaps loaded. Estimated mips: {estimatedMips}, mip dimensions: {mipWidth}x{mipHeight}");
            return mips;
        }
        #endregion Loading

        #region Saving
        /// <summary>
        /// Determines whether an image size is suitable for DXT compression.
        /// Must be a power of 2 (technically just divisible by 4, but I'm lazy)
        /// </summary>
        /// <param name="width">Width of image.</param>
        /// <param name="height">Height of image.</param>
        /// <returns>True if size is suitable for DXT compression.</returns>
        public static bool CheckSize_DXT(int width, int height)
        {
            return UsefulThings.General.IsPowerOfTwo(width) && UsefulThings.General.IsPowerOfTwo(height);
        }

        internal static byte[] Save(List<MipMap> mipMaps, ImageEngineFormat saveFormat, AlphaSettings alphaSetting, List<uint> customMasks = null)
        {
            // Set compressor for Block Compressed textures
            Action<byte[], int, int, byte[], int, AlphaSettings> compressor = null;

            bool needCheckSize = saveFormat.ToString().Contains("DXT") || saveFormat.ToString().Contains("ATI");

            switch (saveFormat)
            {
                case ImageEngineFormat.DDS_ATI1:
                    compressor = DDS_Encoders.CompressBC4Block;
                    break;
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    compressor = DDS_Encoders.CompressBC5Block;
                    break;
                case ImageEngineFormat.DDS_DX10:
                    Debugger.Break();
                    break; // TODO: NOT SUPPORTED YET. DX10
                case ImageEngineFormat.DDS_DXT1:
                    compressor = DDS_Encoders.CompressBC1Block;
                    break;
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                    compressor = DDS_Encoders.CompressBC2Block;
                    break;
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                    compressor = DDS_Encoders.CompressBC3Block;
                    break;
            }

            int height = mipMaps[0].Height;
            int width = mipMaps[0].Width;

            if (needCheckSize && !CheckSize_DXT(width, height))
                throw new InvalidOperationException($"DXT compression formats require dimensions to be multiples of 4. Got: {width}x{height}.");


            int fullSize = GetCompressedSizeOfImage(mipMaps.Count, saveFormat, width, height);
            // +1 to get the full size, not just the offset of the last mip.
            //int fullSize = GetMipOffset(mipMaps.Count + 1, saveFormat, mipMaps[0].Width, mipMaps[0].Height);

            byte[] destination = new byte[fullSize];

            // Create header and write to destination
            DDS_Header header = new DDS_Header(mipMaps.Count, height, width, saveFormat, customMasks);
            header.WriteToArray(destination, 0);

            int blockSize = ImageFormats.GetBlockSize(saveFormat);

            if (ImageFormats.IsBlockCompressed(saveFormat))
            {
                int mipOffset = 128;
                foreach (MipMap mipmap in mipMaps)
                    mipOffset = WriteCompressedMipMap(destination, mipOffset, mipmap, blockSize, compressor, alphaSetting);
            }
            else
            {
                // UNCOMPRESSED
                var action = new Action<int>(mipIndex =>
                {
                    if (alphaSetting == AlphaSettings.RemoveAlphaChannel)
                    {
                        // Remove alpha by setting AMask = 0
                        var ddspf = header.ddspf;
                        ddspf.dwABitMask = 0;
                        header.ddspf = ddspf;
                    }

                    // Get MipOffset
                    int offset = GetMipOffset(mipIndex, saveFormat, width, height);

                    WriteUncompressedMipMap(destination, offset, mipMaps[mipIndex], saveFormat, header.ddspf);
                });

                if (ImageEngine.EnableThreading)
                    Parallel.For(0, mipMaps.Count, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, action);
                else
                    for (int i = 0; i < mipMaps.Count; i++)
                        action(i);
            }

            

            return destination;
        }


        static int WriteCompressedMipMap(byte[] destination, int mipOffset, MipMap mipmap, int blockSize, Action<byte[], int, int, byte[], int, AlphaSettings> compressor, AlphaSettings alphaSetting)  
        {
            int destinationTexelCount = mipmap.Width * mipmap.Height / 16;
            int sourceLineLength = mipmap.Width * 4;
            int numTexelsInLine = mipmap.Width / 4;

            var mipWriter = new Action<int>(texelIndex =>
            {
                // Since this is the top corner of the first texel in a line, skip 4 pixel rows (texel = 4x4 pixels) and the number of rows down the bitmap we are already.
                int sourceLineOffset = sourceLineLength * 4 * (texelIndex / numTexelsInLine);  // Length in bytes x 3 lines x texel line index (how many texel sized lines down the image are we). Index / width will truncate, so for the first texel line, it'll be < 0. For the second texel line, it'll be < 1 and > 0.

                int sourceTopLeftCorner = ((texelIndex % numTexelsInLine) * 16) + sourceLineOffset; // *16 since its 4 pixels with 4 channels each. Index % numTexels will effectively reset each line.
                compressor(mipmap.Pixels, sourceTopLeftCorner, sourceLineLength, destination, mipOffset + texelIndex * blockSize, alphaSetting);
            });

            // Choose an acceleration method.
            if (ImageEngine.EnableGPUAcceleration)
                Debugger.Break(); 
            else if (ImageEngine.EnableThreading)
                Parallel.For(0, destinationTexelCount, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, mipWriter);
            else
                for (int i = 0; i < destinationTexelCount; i++)
                    mipWriter(i);

            return mipOffset + destinationTexelCount * blockSize;
        }

        static int WriteUncompressedMipMap(byte[] destination, int mipOffset, MipMap mipmap, ImageEngineFormat saveFormat, DDS_Header.DDS_PIXELFORMAT ddspf)
        {
            return DDS_Encoders.WriteUncompressed(mipmap.Pixels, destination, mipOffset, ddspf);
        }
        #endregion Saving

        #region Mipmap Management
        /// <summary>
        /// Ensures all Mipmaps are generated in MipMaps.
        /// </summary>
        /// <param name="MipMaps">MipMaps to check.</param>
        /// <returns>Number of mipmaps present in MipMaps.</returns>
        internal static int BuildMipMaps(List<MipMap> MipMaps)
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

            var baseBMP = UsefulThings.WPF.Images.CreateWriteableBitmap(currentMip.Pixels, currentMip.Width, currentMip.Height);
            baseBMP.Freeze();

            Action<int> action = new Action<int>(item =>
            {
                int index = item;
                MipMap newmip;
                var scale = 1d / Math.Pow(2, index);
                newmip = ImageEngine.Resize(baseBMP, scale, scale, currentMip.Width, currentMip.Height);
                newmips[index - 1] = newmip;
            });

            // Start at 1 to skip top mip
            if (ImageEngine.EnableThreading)
                Parallel.For(1, estimatedMips + 1, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, action);
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
        public static int EstimateNumMipMaps(int Width, int Height)
        {
            int limitingDimension = Width > Height ? Height : Width;
            return (int)Math.Log(limitingDimension, 2); // There's 10 mipmaps besides the main top one.
        }


        /// <summary>
        /// Checks image file size to ensure requested mipmap is present in image.
        /// Header mip count can be incorrect or missing. Use this method to validate the mip you're after.
        /// </summary>
        /// <param name="streamLength">Image file stream length.</param>
        /// <param name="mainWidth">Width of image.</param>
        /// <param name="mainHeight">Height of image.</param>
        /// <param name="desiredMipDimension">Max dimension of desired mip.</param>
        /// <param name="format">Format of image.</param>
        /// <param name="mipOffset">Offset of desired mipmap in image.</param>
        /// <returns></returns>
        public static bool EnsureMipInImage(long streamLength, int mainWidth, int mainHeight, int desiredMipDimension, ImageEngineFormat format, out int mipOffset)
        {
            if (mainWidth <= desiredMipDimension && mainHeight <= desiredMipDimension)
            {
                mipOffset = 128;
                return true; // One mip only
                // TODO: DX10 
            }

            int dependentDimension = mainWidth > mainHeight ? mainWidth : mainHeight;
            int mipIndex = (int)Math.Log((dependentDimension / desiredMipDimension), 2) - 1;
            if (mipIndex < -1)
                throw new InvalidDataException($"Invalid dimensions for mipmapping. Got desired: {desiredMipDimension} and dependent: {dependentDimension}");

            int requiredOffset = GetMipOffset(mipIndex, format, mainHeight, mainWidth);  // +128 for header

            // KFreon: Something wrong with the count here by 1 i.e. the estimate is 1 more than it should be 
            if (format == ImageEngineFormat.DDS_ARGB)
                requiredOffset -= 2;

            mipOffset = requiredOffset;

            // Should only occur when an image has 0 or 1 mipmap.
            if (streamLength <= requiredOffset)
                return false;

            return true;
        }

        internal static int GetMipOffset(double mipIndex, ImageEngineFormat format, int baseWidth, int baseHeight)
        {
            // -1 because if we want the offset of the mip, it's the sum of all sizes before it NOT including itself.
            return GetCompressedSizeUpToIndex(mipIndex - 1, format, baseWidth, baseHeight);  
        }

        internal static int GetCompressedSizeUpToIndex(double mipIndex, ImageEngineFormat format, int baseWidth, int baseHeight)
        {
            /*
                Mipmapping halves both dimensions per mip down. Dimensions are then divided by 4 if block compressed as a texel is 4x4 pixels.
                e.g. 4096 x 4096 block compressed texture with 8 byte blocks e.g. DXT1
                Sizes of mipmaps:
                    4096 / 4 x 4096 / 4 x 8
                    (4096 / 4 / 2) x (4096 / 4 / 2) x 8
                    (4096 / 4 / 2 / 2) x (4096 / 4 / 2 / 2) x 8

                Pattern: Each dimension divided by 2 per mip size decreased.
                Thus, total is divided by 4.
                    Size of any mip = Sum(1/4^n) x divWidth x divHeight x blockSize,  
                        where n is the desired mip (0 based), 
                        divWidth and divHeight are the block compress adjusted dimensions (uncompressed textures lead to just original dimensions, block compressed are divided by 4)

                Turns out the partial sum of the infinite sum: Sum(1/4^n) = 1/3 x (4 - 4^-n). Who knew right?
            */

            // TODO: DDS going down past 4x4


            double divisor = 1;
            if (ImageFormats.IsBlockCompressed(format))
                divisor = 4;

            double sumPart = mipIndex == -1 ? 0 :
                (1d / 3d) * (4d - Math.Pow(4, -mipIndex));

            double totalSize = 128 + (sumPart * ImageFormats.GetBlockSize(format) * (baseWidth / divisor) * (baseHeight / divisor));

            return (int)totalSize;
        }

        internal static int GetCompressedSizeOfImage(int mipCount, ImageEngineFormat format, int baseWidth, int baseHeight)
        {
            return GetCompressedSizeUpToIndex(mipCount - 1, format, baseWidth, baseHeight);
        }
        #endregion Mipmap Management
    }
}
