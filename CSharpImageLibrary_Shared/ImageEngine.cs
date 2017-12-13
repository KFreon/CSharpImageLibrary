using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CSharpImageLibrary.DDS;
using CSharpImageLibrary.Headers;
using static CSharpImageLibrary.ImageFormats;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Determines how alpha is handled.
    /// </summary>
    public enum AlphaSettings
    {
        /// <summary>
        /// Keeps any existing alpha.
        /// </summary>
        KeepAlpha,

        /// <summary>
        /// Premultiplies RBG and Alpha channels. Alpha remains.
        /// </summary>
        Premultiply,

        /// <summary>
        /// Removes alpha channel.
        /// </summary>
        RemoveAlphaChannel,
    }

    /// <summary>
    /// Determines how Mipmaps are handled.
    /// </summary>
    public enum MipHandling
    {
        /// <summary>
        /// If mips are present, they are used, otherwise regenerated.
        /// </summary>
        [Description("If mips are present, they are used, otherwise regenerated.")]
        Default,

        /// <summary>
        /// Keeps existing mips if existing. Doesn't generate new ones either way.
        /// </summary>
        [Description("Keeps existing mips if existing. Doesn't generate new ones either way.")]
        KeepExisting,

        /// <summary>
        /// Removes old mips and generates new ones.
        /// </summary>
        [Description("Removes old mips and generates new ones.")]
        GenerateNew,

        /// <summary>
        /// Removes all but the top mip. Used for single mip formats.
        /// </summary>
        [Description("Removes all but the top mip. Used for single mip formats.")]
        KeepTopOnly
    }

    /// <summary>
    /// Provides main image functions
    /// </summary>
    public static class ImageEngine
    {
        internal static CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// Initiates a cancellation of currently running tasks.
        /// Not guaranteed to cancel immediately.
        /// </summary>
        public static void Cancel()
        {
            if (!cts.IsCancellationRequested)
                cts.Cancel();
        }


        /// <summary>
        /// Resets cancellation token source given an external source.
        /// </summary>
        /// <param name="yourCTS">External CTS to use.</param>
        public static void ResetCancellation(CancellationTokenSource yourCTS)
        {
            cts = yourCTS;
        }


        /// <summary>
        /// Resets cancellation token source.
        /// </summary>
        public static void ResetCancellation()
        {
            cts = new CancellationTokenSource();
        }


        /// <summary>
        /// Indicates whether cancellation has been requested for 
        /// </summary>
        public static bool IsCancellationRequested => cts.IsCancellationRequested;

        /// <summary>
        /// Enables threading of Loading and Saving operations to improve performance.
        /// </summary>
        public static bool EnableThreading { get; set; } = true;

        /// <summary>
        /// CURRENTLY DISABLED. Didn't work :(
        /// Enables GPU Accelerated encoding and decoding of all formats.
        /// NOTE: WIC formats (jpg, bmp, png etc) probably already use GPU, but are not covered by this flag.
        /// </summary>
        public static bool EnableGPUAcceleration { get; set; } = false;

        /// <summary>
        /// Determines how many threads to use. -1 is infinite.
        /// </summary>
        public static int NumThreads { get; set; } = -1;

        /// <summary>
        /// Constructor. Checks WIC status before any other operation.
        /// </summary>
        static ImageEngine()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Set NumThreads to be more sensible
            NumThreads = Environment.ProcessorCount - 1;
            if (NumThreads == 0) // Single core...
                NumThreads = 1;


            // Enable GPU Acceleration by default
            /*if (GPU.IsGPUAvailable)
                EnableGPUAcceleration = false;*/
        }

        public static List<MipMapBase> LoadImage<T>(Stream imageStream, AbstractHeader header, int maxDimension, double scale, ImageFormats.ImageEngineFormatDetails formatDetails) where T : MipMapBase, new()
        {
            imageStream.Seek(0, SeekOrigin.Begin);
            List<MipMapBase> MipMaps = null;

            switch (formatDetails.SurfaceFormat)
            {
                case ImageEngineFormat.DDS_DXT1:
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                case ImageEngineFormat.DDS_G8_L8:
                case ImageEngineFormat.DDS_ARGB_4:
                case ImageEngineFormat.DDS_RGB_8:
                case ImageEngineFormat.DDS_V8U8:
                case ImageEngineFormat.DDS_A8L8:
                case ImageEngineFormat.DDS_ARGB_8:
                case ImageEngineFormat.DDS_ARGB_32F:
                case ImageEngineFormat.DDS_ABGR_8:
                case ImageEngineFormat.DDS_G16_R16:
                case ImageEngineFormat.DDS_R5G6B5:
                case ImageEngineFormat.DDS_ATI1:
                case ImageEngineFormat.DDS_ATI2_3Dc:
                    MipMaps = DDSLoading.LoadDDS<T>((MemoryStream)imageStream, (DDS_Header)header, maxDimension, formatDetails);
                    break;
                default:
                    throw new FormatException($"Format unknown: {formatDetails.SurfaceFormat}.");
            }

            return MipMaps;
        }

        internal static AbstractHeader LoadHeader(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            // Determine type of image
            SupportedExtensions ext = SupportedFileExtensions.DetermineFileExtension(stream);

            // Parse header
            AbstractHeader header = null;
            switch (ext)
            {
                case SupportedExtensions.BMP:
                    header = new BMP_Header(stream);
                    break;
                case SupportedExtensions.DDS:
                    header = new DDS_Header(stream);
                    break;
                case SupportedExtensions.JPG:
                    header = new JPG_Header(stream);
                    break;
                case SupportedExtensions.PNG:
                    header = new PNG_Header(stream);
                    break;
                case SupportedExtensions.GIF:
                    header = new GIF_Header(stream);
                    break;
                case SupportedExtensions.TIF:
                    header = new TIFF_Header(stream);
                    break;
                default:
                    throw new NotSupportedException("Image type unknown.");
            }
            return header;
        }

        

        public static byte[] BuildGrayscaleFromChannel(byte[] pixels, int channel)
        {
            byte[] destination = new byte[pixels.Length / 4];
            for (int i = channel, count = 0; i < pixels.Length; i+=4, count++)
                destination[count] = pixels[i];
            

            return destination;
        }


        /// <summary>
        /// Save mipmaps as given format to stream.
        /// </summary>
        /// <param name="MipMaps">List of Mips to save.</param>
        /// <param name="mipChoice">Determines how to handle mipmaps.</param>
        /// <param name="maxDimension">Maximum value for either image dimension.</param>
        /// <param name="alphaSetting">Determines how to handle alpha.</param>
        /// <param name="mipToSave">0 based index on which mipmap to make top of saved image.</param>
        /// <param name="destFormatDetails">Details about the destination format.</param>
        /// <returns>True on success.</returns>
        public static List<MipMapBase> PreSaveSetup(List<MipMapBase> MipMaps, ImageFormats.ImageEngineFormatDetails destFormatDetails, MipHandling mipChoice, AlphaSettings alphaSetting, int maxDimension = 0, int mipToSave = 0)
        {
            List<MipMapBase> newMips = new List<MipMapBase>(MipMaps);

            int width = newMips[0].Width;
            int height = newMips[0].Height;

            if (destFormatDetails.IsDDS && (mipChoice == MipHandling.GenerateNew || (newMips.Count == 1 && mipChoice == MipHandling.Default)))
                DDSGeneral.BuildMipMaps(newMips);

            // KFreon: Resize if asked
            if (maxDimension != 0 && maxDimension < width && maxDimension < height) 
            {
                if (!UsefulDotNetThings.General.Maths.IsPowerOfTwo(maxDimension))
                    throw new ArgumentException($"{nameof(maxDimension)} must be a power of 2. Got {nameof(maxDimension)} = {maxDimension}");

                // KFreon: Check if there's a mipmap suitable, removes all larger mipmaps
                var validMipmap = newMips.Where(img => (img.Width == maxDimension && img.Height <= maxDimension) || (img.Height == maxDimension && img.Width <=maxDimension)).ToList();  // Check if a mip dimension is maxDimension and that the other dimension is equal or smaller
                if (validMipmap?.Count != 0)
                {
                    int index = newMips.IndexOf(validMipmap[0]);
                    newMips.RemoveRange(0, index);
                }
                else
                {
                    // KFreon: Get the amount the image needs to be scaled. Find largest dimension and get it's scale.
                    double scale = maxDimension * 1d / (width > height ? width : height);

                    // KFreon: No mip. Resize.
                    newMips[0] = newMips[0].Resize(scale);
                }
            }

            // KFreon: Ensure we have a power of two for dimensions FOR DDS ONLY
            TestDDSMipSize(newMips, destFormatDetails, width, height, out double fixXScale, out double fixYScale, mipChoice);

            if (fixXScale != 0 || fixYScale != 0 || mipChoice == MipHandling.KeepTopOnly)
                DestroyMipMaps(newMips, mipToSave);

            if ((fixXScale != 0 || fixXScale != 0) && destFormatDetails.IsDDS && mipChoice != MipHandling.KeepTopOnly)
                DDSGeneral.BuildMipMaps(newMips);

            return newMips;
        }      

        public static byte[] SaveDDS(List<MipMapBase> newMips, ImageEngineFormatDetails destFormatDetails, AlphaSettings alphaSetting)
        {
            return DDSSaving.Save(newMips, destFormatDetails, alphaSetting);
        }

        internal static void TestDDSMipSize(List<MipMapBase> newMips, ImageFormats.ImageEngineFormatDetails destFormatDetails, int width, int height, out double fixXScale, out double fixYScale, MipHandling mipChoice)
        {
            fixXScale = 0;
            fixYScale = 0;
            if (destFormatDetails.IsBlockCompressed && (!UsefulDotNetThings.General.Maths.IsPowerOfTwo(width) || !UsefulDotNetThings.General.Maths.IsPowerOfTwo(height)))
            {
                // If only keeping top mip, and that mip is divisible by 4, it's ok.
                if ((mipChoice == MipHandling.KeepTopOnly || mipChoice == MipHandling.KeepExisting) 
                    && DDSGeneral.CheckSize_DXT(width, height))
                    return;


                double newWidth = 0;
                double newHeight = 0;

                // Takes into account aspect ratio (a little bit)
                double aspect = width / height;
                if (aspect > 1)
                {
                    newWidth = UsefulDotNetThings.General.Maths.RoundToNearestPowerOfTwo(width);
                    var tempScale = newWidth / width;
                    newHeight = UsefulDotNetThings.General.Maths.RoundToNearestPowerOfTwo((int)(height * tempScale));
                }
                else
                {
                    newHeight = UsefulDotNetThings.General.Maths.RoundToNearestPowerOfTwo(height);
                    var tempScale = newHeight / height;
                    newWidth = UsefulDotNetThings.General.Maths.RoundToNearestPowerOfTwo((int)(width * tempScale));
                }


                // Little extra bit to allow integer cast from Double with the correct answer. Occasionally dimensions * scale would be 511.99999999998 instead of 512, so adding a little allows the integer cast to return correct value.
                fixXScale = 1d * newWidth / width + 0.001;
                fixYScale = 1d * newHeight / height + 0.001;
                newMips[0] = newMips[0].Resize(fixXScale, fixYScale);
            }
        }

        /// <summary>
        /// Destroys mipmaps. Expects at least one mipmap in given list.
        /// </summary>
        /// <param name="MipMaps">List of Mipmaps.</param>
        /// <param name="mipToSave">Index of mipmap to save.</param>
        /// <returns>Number of mips present.</returns>
        internal static int DestroyMipMaps(List<MipMapBase> MipMaps, int mipToSave = 0)
        {
            if (MipMaps.Count != 1)
                MipMaps.RemoveRange(mipToSave + 1, MipMaps.Count - 1);  // +1 because mipToSave is 0 based and we want to keep it

            return 1;
        }

        /// <summary>
        /// Gets pixels as a BGRA32 array regardless of their original format (float, short)
        /// </summary>
        /// <param name="width">Width of image.</param>
        /// <param name="height">Height of image.</param>
        /// <param name="pixels">Original pixels.</param>
        /// <param name="formatDetails">Details about format pixels array is currently in.</param>
        /// <returns>BGRA32 pixel array.</returns>
        public static byte[] GetPixelsAsBGRA32(int width, int height, byte[] pixels, ImageFormats.ImageEngineFormatDetails formatDetails)
        {
            if (formatDetails.ComponentSize == 1)
                return pixels;


            byte[] tempPixels = new byte[width * height * 4];

            Action<int> action = new Action<int>(ind => tempPixels[ind] = formatDetails.ReadByte(pixels, ind * formatDetails.ComponentSize));

            if (EnableThreading)
                Parallel.For(0, tempPixels.Length, new ParallelOptions { MaxDegreeOfParallelism = NumThreads }, ind => action(ind));
            else
                for (int i = 0; i < tempPixels.Length; i++)
                    action(i);

            return tempPixels;
        }
    }
}
