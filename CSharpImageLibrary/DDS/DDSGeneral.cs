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
        public static float DXT1AlphaThreshold = 0.2f;


        #region Loading
        private static MipMap ReadUncompressedMipMap(MemoryStream stream, int mipOffset, int mipWidth, int mipHeight, Action<byte[], int, byte[], int> PixelReader)
        {
            byte[] data = stream.GetBuffer();
            byte[] mipmap = new byte[mipHeight * mipWidth * 4];
            PixelReader(data, mipOffset, mipmap, mipHeight * mipWidth);

            return new MipMap(UsefulThings.WPF.Images.CreateWriteableBitmap(mipmap, mipWidth, mipHeight));
        }

        private static MipMap ReadCompressedMipMap(MemoryStream compressed, int mipWidth, int mipHeight, int blockSize, int mipOffset, DDS_Header header, Action<byte[], int, byte[], int, int> DecompressBlock)
        {
            // Gets stream as data. Note that this array isn't necessarily the correct size. Likely to have garbage at the end.
            // Don't want to use ToArray as that creates a new array. Don't want that.
            byte[] CompressedData = compressed.GetBuffer();

            byte[] decompressedData = new byte[4 * mipWidth * mipHeight];
            int decompressedRowLength = mipWidth * 4;

            int texelCount = (mipWidth * mipHeight) / 16;
            if (texelCount != 0)
            {
                Action<int, ParallelLoopState> action = new Action<int, ParallelLoopState>((texelIndex, loopstate) =>
                {
                    int compressedPosition = mipOffset + texelIndex * blockSize;
                    int decompressedStart = texelIndex * 4;

                    DecompressBlock(CompressedData, compressedPosition, decompressedData, decompressedStart, decompressedRowLength);

                    if (ImageEngine.EnableThreading)
                        loopstate.Break();
                    else if (!ImageEngine.EnableThreading)
                        return;
                });

                // Actually perform decompression using threading, no threading, or GPU.
                if (ImageEngine.EnableGPUAcceleration)
                    Debugger.Break();  // TODO: GPU acceleration
                else if (ImageEngine.EnableThreading)
                    Parallel.For(0, texelCount, (texelIndex, loopstate) => action(texelIndex, loopstate));
                else
                    for (int index = 0; index < texelCount; index++)
                        action(index, null);
            }

            return new MipMap(UsefulThings.WPF.Images.CreateWriteableBitmap(decompressedData, mipWidth, mipHeight));
        }

        

        internal static List<MipMap> LoadDDS(MemoryStream compressed, DDS_Header header, int desiredMaxDimension)
        {           
            List<MipMap> MipMaps = new List<MipMap>();

            int mipWidth = header.Width;
            int mipHeight = header.Height;
            ImageEngineFormat format = header.Format;
            int blockSize = ImageFormats.GetBlockSize(format);

            int estimatedMips = header.dwMipMapCount;
            int mipOffset = 128;  // Includes header. 
            // TODO: Incorrect mip offset for DX10

            // KFreon: Check number of mips is correct. i.e. For some reason, some images have more data than they should, so it can't detected it.
            // So I check the number of mips possibly contained in the image based on size and compare it to how many it should have.
            // Any image that is too short to contain all the mips it should loads only the top mip and ignores the "others".
            if (!EnsureMipInImage(compressed.Length, mipWidth, mipHeight, 4, format, out estimatedMips, out mipOffset))  // Update number of mips too
                estimatedMips = 1;
            

            // KFreon: Decide which mip to start loading at - going to just load a few mipmaps if asked instead of loading all, then choosing later. That's slow.
            if (desiredMaxDimension != 0 && estimatedMips > 1)
            {
                int tempEstimation;
                if (!EnsureMipInImage(compressed.Length, mipWidth, mipHeight, desiredMaxDimension, format, out tempEstimation, out mipOffset))  // Update number of mips too
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
                        Debugger.Break();
                        estimatedMips = tempEstimation + 1;  // cos it needs the extra one for the top?
                    }
                }
                else  // The first mipmap
                    mipOffset = 128;

            }

            // Move to requested mipmap
            compressed.Position = mipOffset;

            Action<byte[], int, byte[], int, int> DecompressBCBlock = null;
            Action<byte[], int, byte[], int> UncompressedPixelReader = null;
            switch (format)
            {
                case ImageEngineFormat.DDS_RGB:
                    UncompressedPixelReader = DDS_Decoders.ReadRGBPixel;
                    break;
                case ImageEngineFormat.DDS_A8L8:
                    UncompressedPixelReader = DDS_Decoders.ReadA8L8Pixel;
                    break;
                case ImageEngineFormat.DDS_ARGB:
                    UncompressedPixelReader = DDS_Decoders.ReadARGBPixel;
                    break;
                case ImageEngineFormat.DDS_ATI1:
                    DecompressBCBlock = DDS_Decoders.DecompressATI1;
                    break;
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    DecompressBCBlock = DDS_Decoders.DecompressATI2Block;
                    break;
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
                case ImageEngineFormat.DDS_G8_L8:
                    UncompressedPixelReader = DDS_Decoders.ReadG8_L8Pixel;
                    break;
                case ImageEngineFormat.DDS_V8U8:
                    UncompressedPixelReader = DDS_Decoders.ReadV8U8Pixel;
                    break;
                default:
                    throw new Exception("Unknown format: " + format);
            }

            // KFreon: Read mipmaps
            for (int m = 0; m < estimatedMips; m++)
            {
                // KFreon: If mip is too small, skip out. This happens most often with non-square textures. I think it's because the last mipmap is square instead of the same aspect.
                if (mipWidth <= 0 || mipHeight <= 0)  // Needed cos it doesn't throw when reading past the end for some reason.
                {
                    Debugger.Break();
                    break;
                }

                MipMap mipmap = null;
                if (ImageFormats.IsBlockCompressed(format))  // TODO: Header needs to be used for colour masks
                    mipmap = ReadCompressedMipMap(compressed, mipWidth, mipHeight, blockSize, mipOffset, header, DecompressBCBlock);
                else
                    mipmap = ReadUncompressedMipMap(compressed, mipOffset, mipWidth, mipHeight, UncompressedPixelReader);

                MipMaps.Add(mipmap);

                mipOffset += mipWidth * mipHeight * blockSize / 16; // Only used for BC textures
                mipWidth /= 2;
                mipHeight /= 2;
            }

            if (MipMaps.Count == 0)
                Debugger.Break();
            return MipMaps;
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


        /// <summary>
        /// Checks image file size to ensure requested mipmap is present in image.
        /// Header mip count can be incorrect or missing. Use this method to validate the mip you're after.
        /// </summary>
        /// <param name="streamLength">Image file stream length.</param>
        /// <param name="mainWidth">Width of image.</param>
        /// <param name="mainHeight"></param>
        /// <param name="desiredMaxDimension"></param>
        /// <param name="format"></param>
        /// <param name="numMipMaps"></param>
        /// <param name="mipOffset"></param>
        /// <returns></returns>
        public static bool EnsureMipInImage(long streamLength, int mainWidth, int mainHeight, int desiredMaxDimension, ImageEngineFormat format, out int numMipMaps, out int mipOffset)
        {
            if (mainWidth <= desiredMaxDimension && mainHeight <= desiredMaxDimension)
            {
                numMipMaps = EstimateNumMipMaps(mainWidth, mainHeight);
                mipOffset = 128;
                return true; // One mip only
                // TODO: DX10 
            }

            int dependentDimension = mainWidth > mainHeight ? mainWidth : mainHeight;
            int mipIndex = (int)Math.Log((dependentDimension / desiredMaxDimension), 2) - 1;
            if (mipIndex < -1)
                throw new InvalidDataException($"Invalid dimensions for mipmapping. Got desired: {desiredMaxDimension} and dependent: {dependentDimension}");

            int requiredOffset = GetMipOffset(mipIndex, format, mainHeight, mainWidth);  // +128 for header

            int limitingDimension = mainWidth > mainHeight ? mainHeight : mainWidth;
            double newDimDivisor = limitingDimension * 1f / desiredMaxDimension;
            numMipMaps = EstimateNumMipMaps((int)(mainWidth / newDimDivisor), (int)(mainHeight / newDimDivisor));

            // KFreon: Something wrong with the count here by 1 i.e. the estimate is 1 more than it should be 
            if (format == ImageEngineFormat.DDS_ARGB)
                requiredOffset -= 2;

            mipOffset = requiredOffset;

            // Should only occur when an image has no mips
            if (streamLength < requiredOffset)
                return false;

            return true;
        }

        internal static int GetMipOffset(double mipIndex, ImageEngineFormat format, int baseWidth, int baseHeight)
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

            int divisor = 1;
            if (ImageFormats.IsBlockCompressed(format))
                divisor = 4;

            double sumPart = mipIndex == -1 ? 0 :
                (1 / 3f) * (4 - Math.Pow(4, -mipIndex));

            double totalSize = 128 + (sumPart * ImageFormats.GetBlockSize(format) * (baseWidth / divisor) * (baseHeight / divisor));

            return (int)totalSize;
        }
        #endregion Mipmap Management
    }
}
