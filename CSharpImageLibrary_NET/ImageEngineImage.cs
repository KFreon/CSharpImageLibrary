using System;
using System.IO;
using CSharpImageLibrary;

namespace CSharpImageLibrary_NET
{
    public class ImageEngineImage : ImageEngineImageBase<MipMap>
    {
        public ImageEngineImage()
        {

        }

        /// <summary>
        /// Creates an image supporting many formats including DDS.
        /// </summary>
        /// <param name="stream">Stream containing image.</param>
        /// <param name="maxDimension">Max dimension of created image. Useful for mipmapped images, otherwise resized.</param>
        public ImageEngineImage(MemoryStream stream, int maxDimension = 0) 
            :base(stream, maxDimension)
        {
        }


        /// <summary>
        /// Creates an image supporting many formats including DDS.
        /// </summary>
        /// <param name="path">Path to image.</param>
        /// <param name="maxDimension">Max dimension of created image. Useful for mipmapped images, otherwise resized.</param>
        public ImageEngineImage(string path, int maxDimension = 0) 
            : base(path, maxDimension)
        {
        }

        /// <summary>
        /// Creates an image supporting many formats including DDS.
        /// </summary>
        /// <param name="imageData">Fully formatted image data, not just pixels.</param>
        /// <param name="maxDimension">Max dimension of created image. Useful for mipmapped images, otherwise resized.</param>
        public ImageEngineImage(byte[] imageData, int maxDimension = 0) : base(imageData, maxDimension)
        {
        }

        public override void Initialise(string path, int maxDimension = 0)
        {
            FilePath = path;
            var imageData = File.ReadAllBytes(path);
            using (MemoryStream ms = new MemoryStream(imageData, 0, imageData.Length, false, true))  // Need to be able to access underlying byte[] using <Stream>.GetBuffer()
                Load(ms, maxDimension);
        }

        public override byte[] Save(ImageFormats.ImageEngineFormatDetails destFormatDetails, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0, bool removeAlpha = true)
        {
            byte[] imageData = null;

            if (!destFormatDetails.ValidSaveFormat)
                throw new InvalidOperationException($"Save format is not supported: {destFormatDetails.SurfaceFormat}");

            AlphaSettings alphaSetting = AlphaSettings.KeepAlpha;
            if (removeAlpha)
                alphaSetting = AlphaSettings.RemoveAlphaChannel;
            else if (destFormatDetails.IsPremultipliedFormat)
                alphaSetting = AlphaSettings.Premultiply;

            // If same format and stuff, can just return original data, or chunks of it.
            if (destFormatDetails.SurfaceFormat == Format)
                imageData = AttemptSaveUsingOriginalData(destFormatDetails, GenerateMips, desiredMaxDimension, mipToSave, alphaSetting);

            if (imageData == null)
            {
                var newMips = CSharpImageLibrary.ImageEngine.PreSaveSetup(MipMaps, destFormatDetails, GenerateMips, alphaSetting, desiredMaxDimension, mipToSave);
                if (destFormatDetails.IsDDS)
                    imageData = CSharpImageLibrary.ImageEngine.SaveDDS(newMips, destFormatDetails, alphaSetting);
                else
                {
                    // KFreon: Try saving with built in codecs
                    var mip = newMips[0];

                    // Fix formatting
                    byte[] newPixels = new byte[mip.Width * mip.Height * 4];
                    for (int i = 0, j = 0; i < newPixels.Length; i++, j += mip.LoadedFormatDetails.ComponentSize)
                        newPixels[i] = mip.LoadedFormatDetails.ReadByte(mip.Pixels, j);

                    imageData = WIC_Codecs.SaveWithCodecs(newPixels, destFormatDetails.SurfaceFormat, mip.Width, mip.Height, alphaSetting);
                }
            }

            return imageData;
        }
    }
}
