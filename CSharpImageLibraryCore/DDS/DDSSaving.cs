using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CSharpImageLibrary.Headers;
using static CSharpImageLibrary.DDS.DDSGeneral;
using static CSharpImageLibrary.Headers.DDS_Header.RawDDSHeaderStuff;

namespace CSharpImageLibraryCore.DDS
{
    internal static class DDSSaving
    {
        internal static byte[] Save(List<MipMap> mipMaps, ImageFormats.ImageEngineFormatDetails destFormatDetails, AlphaSettings alphaSetting)
        {
            // Set compressor for Block Compressed textures
            Action<byte[], int, int, byte[], int, AlphaSettings, ImageFormats.ImageEngineFormatDetails> compressor = destFormatDetails.BlockEncoder;

            bool needCheckSize = destFormatDetails.IsBlockCompressed;

            int height = mipMaps[0].Height;
            int width = mipMaps[0].Width;

            if (needCheckSize && !CheckSize_DXT(width, height))
                throw new InvalidOperationException($"DXT compression formats require dimensions to be multiples of 4. Got: {width}x{height}.");

            // Create header and write to destination
            DDS_Header header = new DDS_Header(mipMaps.Count, height, width, destFormatDetails);
            int headerLength = destFormatDetails.HeaderSize;
            int fullSize = destFormatDetails.GetCompressedSize(width, height, mipMaps.Count);
            /*if (destFormatDetails.ComponentSize != 1)
                fullSize += (fullSize - headerLength) * destFormatDetails.ComponentSize;*/   // Size adjustment for destination to allow for different component sizes.

            byte[] destination = new byte[fullSize];
            header.WriteToArray(destination, 0);

            int blockSize = destFormatDetails.BlockSize;
            if (destFormatDetails.IsBlockCompressed)
            {
                int mipOffset = headerLength;
                foreach (MipMap mipmap in mipMaps)
                {
                    if (ImageEngine.IsCancellationRequested)
                        break;

                    var temp = WriteCompressedMipMap(destination, mipOffset, mipmap, blockSize, compressor, alphaSetting);
                    if (temp != -1)  // When dimensions too low.
                        mipOffset = temp;
                }
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
                    int offset = GetMipOffset(mipIndex, destFormatDetails, width, height);

                    WriteUncompressedMipMap(destination, offset, mipMaps[mipIndex], destFormatDetails, header.ddspf);
                });

                if (ImageEngine.EnableThreading)
                    Parallel.For(0, mipMaps.Count, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, (mip, loopState) =>
                    {
                        if (ImageEngine.IsCancellationRequested)
                            loopState.Stop();

                        action(mip);
                    });
                else
                    for (int i = 0; i < mipMaps.Count; i++)
                    {
                        if (ImageEngine.IsCancellationRequested)
                            break;

                        action(i);
                    }
            }

            return ImageEngine.IsCancellationRequested ? null : destination;
        }


        static int WriteCompressedMipMap(byte[] destination, int mipOffset, MipMap mipmap, int blockSize, Action<byte[], int, int, byte[], int, AlphaSettings, ImageFormats.ImageEngineFormatDetails> compressor,
            AlphaSettings alphaSetting)
        {
            if (mipmap.Width < 4 || mipmap.Height < 4)
                return -1;

            int destinationTexelCount = mipmap.Width * mipmap.Height / 16;
            int sourceLineLength = mipmap.Width * 4 * mipmap.LoadedFormatDetails.ComponentSize;
            int numTexelsInLine = mipmap.Width / 4;

            var mipWriter = new Action<int>(texelIndex =>
            {
                // Since this is the top corner of the first texel in a line, skip 4 pixel rows (texel = 4x4 pixels) and the number of rows down the bitmap we are already.
                int sourceLineOffset = sourceLineLength * 4 * (texelIndex / numTexelsInLine);  // Length in bytes x 3 lines x texel line index (how many texel sized lines down the image are we). Index / width will truncate, so for the first texel line, it'll be < 0. For the second texel line, it'll be < 1 and > 0.

                int sourceTopLeftCorner = ((texelIndex % numTexelsInLine) * 16) * mipmap.LoadedFormatDetails.ComponentSize + sourceLineOffset; // *16 since its 4 pixels with 4 channels each. Index % numTexels will effectively reset each line.
                compressor(mipmap.Pixels, sourceTopLeftCorner, sourceLineLength, destination, mipOffset + texelIndex * blockSize, alphaSetting, mipmap.LoadedFormatDetails);
            });

            // Choose an acceleration method.
            if (ImageEngine.EnableThreading)
                Parallel.For(0, destinationTexelCount, new ParallelOptions { MaxDegreeOfParallelism = ImageEngine.NumThreads }, (mip, loopState) =>
                {
                    if (ImageEngine.IsCancellationRequested)
                        loopState.Stop();

                    mipWriter(mip);
                });
            else
                for (int i = 0; i < destinationTexelCount; i++)
                {
                    if (ImageEngine.IsCancellationRequested)
                        break;

                    mipWriter(i);
                }

            return mipOffset + destinationTexelCount * blockSize;
        }

        static void WriteUncompressedMipMap(byte[] destination, int mipOffset, MipMap mipmap, ImageFormats.ImageEngineFormatDetails destFormatDetails, DDS_PIXELFORMAT ddspf)
        {
            DDS_Encoders.WriteUncompressed(mipmap.Pixels, destination, mipOffset, ddspf, mipmap.LoadedFormatDetails, destFormatDetails);
        }
    }
}
