using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpImageLibrary.Headers;
using static CSharpImageLibrary.Headers.DDS_Header.RawDDSHeaderStuff;
using static CSharpImageLibrary.DDS.DDSGeneral;
using System.Diagnostics;

namespace CSharpImageLibrary.DDS
{
    internal static class DDSLoading
    {
        private static T ReadUncompressedMipMap<T>(MemoryStream stream, int mipOffset, int mipWidth, int mipHeight, DDS_PIXELFORMAT ddspf, ImageFormats.ImageEngineFormatDetails formatDetails) where T : MipMapBase, new()
        {
            byte[] data = stream.GetBuffer();
            byte[] mipmap = new byte[mipHeight * mipWidth * 4 * formatDetails.ComponentSize];

            // Smaller sizes breaks things, so just exclude them
            if (mipHeight >= 4 && mipWidth >= 4)
                DDS_Decoders.ReadUncompressed(data, mipOffset, mipmap, mipWidth * mipHeight, ddspf, formatDetails);

            var mip = new T();
            mip.Initialise(mipmap, mipWidth, mipHeight, formatDetails);
            return mip;
        }

        private static T ReadCompressedMipMap<T>(MemoryStream compressed, int mipWidth, int mipHeight, int mipOffset, ImageFormats.ImageEngineFormatDetails formatDetails, Action<byte[], int, byte[], int, int, bool> DecompressBlock) where T : MipMapBase, new()
        {
            // Gets stream as data. Note that this array isn't necessarily the correct size. Likely to have garbage at the end.
            // Don't want to use ToArray as that creates a new array. Don't want that.
            byte[] CompressedData = compressed.GetBuffer();

            byte[] decompressedData = new byte[4 * mipWidth * mipHeight * formatDetails.ComponentSize];
            int decompressedRowLength = mipWidth * 4;
            int texelRowSkip = decompressedRowLength * 4;

            int texelCount = (mipWidth * mipHeight) / 16;
            int numTexelsInRow = mipWidth / 4;

            if (texelCount != 0)
            {
                var action = new Action<int>(texelIndex =>
                {
                    int compressedPosition = mipOffset + texelIndex * formatDetails.BlockSize;
                    int decompressedStart = (int)(texelIndex / numTexelsInRow) * texelRowSkip + (texelIndex % numTexelsInRow) * 16;

                    // Problem with how I handle dimensions (no virtual padding or anything yet)
                    
                    if (!CheckSize_DXT(mipWidth, mipHeight))
                        return;


                    DecompressBlock(CompressedData, compressedPosition, decompressedData, decompressedStart, decompressedRowLength, formatDetails.IsPremultipliedFormat);

                });

                // Actually perform decompression using threading, no threading, or GPU.
                if (ImageEngine.EnableThreading)
                    Parallel.For(0, texelCount, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, (texelIndex, loopstate) =>
                    {
                        if (ImageEngine.IsCancellationRequested)
                            loopstate.Stop();

                        action(texelIndex);
                    });
                else
                    for (int texelIndex = 0; texelIndex < texelCount; texelIndex++)
                    {
                        if (ImageEngine.IsCancellationRequested)
                            break;

                        action(texelIndex);
                    }
            }
            // No else here cos the lack of texels means it's below texel dimensions (4x4). So the resulting block is set to 0. Not ideal, but who sees 2x2 mipmaps?

            if (ImageEngine.IsCancellationRequested)
                return null;

            var mip = new T();
            mip.Initialise(decompressedData, mipWidth, mipHeight, formatDetails);
            return mip;
        }



        internal static List<MipMapBase> LoadDDS<T>(MemoryStream compressed, DDS_Header header, int desiredMaxDimension, ImageFormats.ImageEngineFormatDetails formatDetails) where T : MipMapBase, new()
        {
            MipMapBase[] MipMaps = null;

            int mipWidth = header.Width;
            int mipHeight = header.Height;

            int estimatedMips = header.dwMipMapCount;
            int mipOffset = formatDetails.HeaderSize;
            int originalOffset = mipOffset;

            if (!EnsureMipInImage(compressed.Length, mipWidth, mipHeight, 4, formatDetails, out mipOffset))  // Update number of mips too
                estimatedMips = 1;

            if (estimatedMips == 0)
                estimatedMips = EstimateNumMipMaps(mipWidth, mipHeight);

            mipOffset = originalOffset;  // Needs resetting after checking there's mips in this image.

            // Ensure there's at least 1 mipmap
            if (estimatedMips == 0)
                estimatedMips = 1;

            int orig_estimatedMips = estimatedMips; // Store original count for later (uncompressed only I think)

            // KFreon: Decide which mip to start loading at - going to just load a few mipmaps if asked instead of loading all, then choosing later. That's slow.
            if (desiredMaxDimension != 0 && estimatedMips > 1)
            {
                if (!EnsureMipInImage(compressed.Length, mipWidth, mipHeight, desiredMaxDimension, formatDetails, out mipOffset))  // Update number of mips too
                    throw new InvalidDataException($"Requested mipmap does not exist in this image. Top Image Size: {mipWidth}x{mipHeight}, requested mip max dimension: {desiredMaxDimension}.");

                // Not the first mipmap. 
                if (mipOffset > formatDetails.HeaderSize)
                {

                    double divisor = mipHeight > mipWidth ? mipHeight / desiredMaxDimension : mipWidth / desiredMaxDimension;
                    mipHeight = (int)(mipHeight / divisor);
                    mipWidth = (int)(mipWidth / divisor);

                    if (mipWidth == 0 || mipHeight == 0)  // Reset as a dimension is too small to resize
                    {
                        mipHeight = header.Height;
                        mipWidth = header.Width;
                        mipOffset = formatDetails.HeaderSize;
                    }
                    else
                    {
                        // Update estimated mips due to changing dimensions.
                        estimatedMips = EstimateNumMipMaps(mipWidth, mipHeight);
                    }
                }
                else  // The first mipmap
                    mipOffset = formatDetails.HeaderSize;

            }

            // Move to requested mipmap
            compressed.Position = mipOffset;

            // Block Compressed texture chooser.
            Action<byte[], int, byte[], int, int, bool> DecompressBCBlock = formatDetails.BlockDecoder;

            MipMaps = new MipMapBase[estimatedMips];
            int blockSize = formatDetails.BlockSize;

            // KFreon: Read mipmaps
            if (formatDetails.IsBlockCompressed)    // Threading done in the decompression, not here.
            {
                for (int m = 0; m < estimatedMips; m++)
                {
                    if (ImageEngine.IsCancellationRequested)
                        break;


                    // KFreon: If mip is too small, skip out. This happens most often with non-square textures. I think it's because the last mipmap is square instead of the same aspect.
                    // Don't do the mip size check here (<4) since we still need to have a MipMap object for those lower than this for an accurate count.
                    if (mipWidth <= 0 || mipHeight <= 0)  // Needed cos it doesn't throw when reading past the end for some reason.
                        break;

                    MipMapBase mipmap = ReadCompressedMipMap<T>(compressed, mipWidth, mipHeight, mipOffset, formatDetails, DecompressBCBlock);
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
                    offset = GetMipOffset(mipIndex, formatDetails, header.Width, header.Height);

                    double divisor = mipIndex == 0 ? 1d : 2 << (mipIndex - 1);   // Divisor represents 2^mipIndex - Math.Pow seems very slow.
                    width = (int)(header.Width / divisor);
                    height = (int)(header.Height / divisor);

                    MipMapBase mipmap = null;
                    try
                    {
                        mipmap = ReadUncompressedMipMap<T>(compressed, offset, width, height, header.ddspf, formatDetails);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e.ToString());
                    }

                    MipMaps[mipIndex] = mipmap;
                });

                if (ImageEngine.EnableThreading)
                    Parallel.For(startMip, orig_estimatedMips, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, (mip, loopState) =>
                    {
                        if (ImageEngine.IsCancellationRequested)
                            loopState.Stop();

                        action(mip);
                    });
                else
                    for (int i = startMip; i < orig_estimatedMips; i++)
                    {
                        if (ImageEngine.IsCancellationRequested)
                            break;

                        action(i);
                    }
            }

            List<MipMapBase> mips = new List<MipMapBase>(MipMaps.Where(t => t != null));
            if (mips.Count == 0)
                throw new InvalidOperationException($"No mipmaps loaded. Estimated mips: {estimatedMips}, mip dimensions: {mipWidth}x{mipHeight}");
            return mips;
        }
    }
}
