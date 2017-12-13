using CSharpImageLibrary;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace UWP_UI_Project.Models
{
    public class ImageEngineImage : ImageEngineImageBase<MipMap>
    {
        public static async Task<ImageEngineImage> CreateAsync(StorageFile file, int maxDimension = 0)
        {
            var image = new ImageEngineImage();
            using (var stream = await file.OpenStreamForReadAsync())
            {
                image.OriginalData = await UsefulUWPThings.Streams.StreamToByteArray(stream.AsRandomAccessStream());
                await image.Load(new MemoryStream(image.OriginalData), maxDimension);
            }
            return image;
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

        private async Task Load(Stream stream, int maxDimension)
        {
            LoadData(stream);

            try
            {
                MipMaps = await ImageEngine.LoadImage(stream, Header, maxDimension, 0, FormatDetails);
            }
            catch (FileLoadException e)
            {
                MipMaps = CSharpImageLibrary.ImageEngine.LoadImage<MipMap>(stream, Header, maxDimension, 0, FormatDetails);
            }
        }
    }
}
