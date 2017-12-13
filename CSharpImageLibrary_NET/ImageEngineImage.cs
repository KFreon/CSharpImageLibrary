using System;
using System.IO;
using System.Threading.Tasks;
using CSharpImageLibrary;

namespace CSharpImageLibrary_NET
{
    public class ImageEngineImage : ImageEngineImageBase<MipMap>
    {
        public static async Task<ImageEngineImage> CreateAsync(byte[] data, int maxDimension = 0)
        {
            var image = new ImageEngineImage
            {
                OriginalData = data
            };

            await image.Load(new MemoryStream(image.OriginalData), maxDimension);

            return image;
        }

        public static async Task<ImageEngineImage> CreateAsync(string path, int maxDimension = 0)
        {
            var bytes = File.ReadAllBytes(path);
            return await CreateAsync(bytes, maxDimension);
        }

        public override async Task Save(string destination, ImageFormats.ImageEngineFormatDetails destFormatDetails, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0, bool removeAlpha = true)
        {
            throw new NotImplementedException();
        }

        public override async Task Save(Stream destination, ImageFormats.ImageEngineFormatDetails destFormatDetails, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0, bool removeAlpha = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<byte[]> Save(ImageFormats.ImageEngineFormatDetails destFormatDetails, MipHandling GenerateMips, int desiredMaxDimension = 0, int mipToSave = 0, bool removeAlpha = true)
        {
            throw new NotImplementedException();
        }

        private async Task Load(Stream stream, int maxDimension = 0)
        {
            LoadData(stream);

            try
            {
                MipMaps = ImageEngine.LoadImage(stream, Header, maxDimension, 0, FormatDetails);
            }
            catch(FileFormatException e)
            {
                MipMaps = CSharpImageLibrary.ImageEngine.LoadImage<MipMap>(stream, Header, maxDimension, 0, FormatDetails);
            }
        }
    }
}
