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
using System.Windows.Media;
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

            // TODO: Component size - pixels
            //var baseBMP = UsefulThings.WPF.Images.CreateWriteableBitmap(currentMip.Pixels, currentMip.Width, currentMip.Height);
            //baseBMP.Freeze();

            Action<int> action = new Action<int>(item =>
            {
                int index = item;
                MipMap newmip;
                var scale = 1d / (2 << (index - 1));  // Shifting is 2^index - Math.Pow seems extraordinarly slow.
                //newmip = ImageEngine.Resize(baseBMP, scale, scale, currentMip.Width, currentMip.Height, currentMip.LoadedFormatDetails);
                newmip = ImageEngine.Resize(currentMip, scale, scale);
                newmips[index - 1] = newmip;
            });

            // Start at 1 to skip top mip
            if (ImageEngine.EnableThreading)
                Parallel.For(1, estimatedMips + 1, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, action);
            else
                for (int item = 1; item < estimatedMips + 1; item++)
                    action(item);

            MipMaps.AddRange(newmips);
            return MipMaps.Count;
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
        /// Determines whether an image size is suitable for DXT compression.
        /// </summary>
        /// <param name="width">Width of image.</param>
        /// <param name="height">Height of image.</param>
        /// <returns>True if size is suitable for DXT compression.</returns>
        public static bool CheckSize_DXT(int width, int height)
        {
            return width % 4 == 0 && height % 4 == 0;
        }


        /// <summary>
        /// Checks image file size to ensure requested mipmap is present in image.
        /// Header mip count can be incorrect or missing. Use this method to validate the mip you're after.
        /// </summary>
        /// <param name="streamLength">Image file stream length.</param>
        /// <param name="mainWidth">Width of image.</param>
        /// <param name="mainHeight">Height of image.</param>
        /// <param name="desiredMipDimension">Max dimension of desired mip.</param>
        /// <param name="destFormatDetails">Destination format details.</param>
        /// <param name="mipOffset">Offset of desired mipmap in image.</param>
        /// <returns>True if mip in image.</returns>
        public static bool EnsureMipInImage(long streamLength, int mainWidth, int mainHeight, int desiredMipDimension, ImageFormats.ImageEngineFormatDetails destFormatDetails, out int mipOffset)
        {
            if (mainWidth <= desiredMipDimension && mainHeight <= desiredMipDimension)
            {
                mipOffset = destFormatDetails.HeaderSize;
                return true; // One mip only
            }

            int dependentDimension = mainWidth > mainHeight ? mainWidth : mainHeight;
            int mipIndex = (int)Math.Log((dependentDimension / desiredMipDimension), 2);
            if (mipIndex < -1)
                throw new InvalidDataException($"Invalid dimensions for mipmapping. Got desired: {desiredMipDimension} and dependent: {dependentDimension}");

            int requiredOffset = GetMipOffset(mipIndex, destFormatDetails, mainHeight, mainWidth);  

            // KFreon: Something wrong with the count here by 1 i.e. the estimate is 1 more than it should be 
            if (destFormatDetails.SurfaceFormat == ImageEngineFormat.DDS_ARGB_8)  // TODO: Might not just be 8 bit, still don't know why it's wrong.
                requiredOffset -= 2;

            mipOffset = requiredOffset;

            // Should only occur when an image has 0 or 1 mipmap.
            //if (streamLength <= (requiredOffset - destFormatDetails.HeaderSize))
            if (streamLength <= requiredOffset)
                return false;

            return true;
        }

        internal static int GetMipOffset(double mipIndex, ImageFormats.ImageEngineFormatDetails destFormatDetails, int baseWidth, int baseHeight)
        {
            // -1 because if we want the offset of the mip, it's the sum of all sizes before it NOT including itself.
            return GetCompressedSizeUpToIndex(mipIndex - 1, destFormatDetails, baseWidth, baseHeight);  
        }

        internal static int GetCompressedSizeUpToIndex(double mipIndex, ImageFormats.ImageEngineFormatDetails destFormatDetails, int baseWidth, int baseHeight)
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
            bool requiresTinyAdjustment = false;
            int selectedMipDimensions = (int)(baseWidth / Math.Pow(2d, mipIndex));
            if (selectedMipDimensions < 4)
                requiresTinyAdjustment = true;

            double divisor = 1;
            if (destFormatDetails.IsBlockCompressed)
                divisor = 4;

            double shift = 1d / (4 << (int)(2 * (mipIndex - 1)));

            if (mipIndex == 0)
                shift = 1d;
            else if (mipIndex == -1)
                shift = 4d;

            double sumPart = mipIndex == -1 ? 0 :
                (1d / 3d) * (4d - shift);   // Shifting represents 4^-mipIndex. Math.Pow seems slow.

            double totalSize = destFormatDetails.HeaderSize + (sumPart * destFormatDetails.BlockSize * (baseWidth / divisor) * (baseHeight / divisor));
            if (requiresTinyAdjustment)
                totalSize += destFormatDetails.BlockSize * 2;

            return (int)totalSize;
        }

        internal static int GetCompressedSizeOfImage(int mipCount, ImageFormats.ImageEngineFormatDetails destFormatDetails, int baseWidth, int baseHeight)
        {
            return GetCompressedSizeUpToIndex(mipCount, destFormatDetails, baseWidth, baseHeight);
        }
        #endregion Mipmap Management
    }
}
