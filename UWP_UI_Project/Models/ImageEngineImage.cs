﻿using CSharpImageLibrary;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace UWP_UI_Project.Models
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
            : base(stream, maxDimension)
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

        private async Task Load(string path, int maxDimension = 0)
        {
            FilePath = path;

            var file = await StorageFile.GetFileFromPathAsync(path).AsTask();
            await Load(file, maxDimension);
            
        }

        public async Task Load(StorageFile file, int maxDimension = 0)
        {
            using (var stream = await file.OpenStreamForReadAsync())
            {
                using (var memoryStream = new MemoryStream((int)stream.Length))
                {
                    await stream.CopyToAsync(memoryStream);
                    Load(memoryStream, maxDimension);
                }
            }
        }

        public override void Initialise(string path, int maxDimension = 0)
        {
            Load(path, maxDimension).Wait();
        }

        private async Task<Stream> InitialiseAsync(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            return await InitialiseAsync(file);
        }

        private async Task<Stream> InitialiseAsync(StorageFile file)
        {
            return await file.OpenStreamForReadAsync();
        }

        public override byte[] Save(ImageFormats.ImageEngineFormatDetails destFormatDetails, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0, bool removeAlpha = true)
        {
            throw new NotImplementedException();
        }
    }
}
