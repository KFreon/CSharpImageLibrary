using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibraryWindows
{
    public static class WindowsImageEngine
    {
        public static void SplitChannels(MipMap mip, string savePath)
        {
            char[] channels = new char[] { 'B', 'G', 'R', 'A' };
            for (int i = 0; i < 4; i++)
            {
                // Extract channel into grayscale image
                var grayChannel = BuildGrayscaleFromChannel(mip.Pixels, i);

                // Save channel
                var img = UsefulThings.WPF.Images.CreateWriteableBitmap(grayChannel, mip.Width, mip.Height, PixelFormats.Gray8);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(img));
                byte[] bytes = null;
                using (MemoryStream ms = new MemoryStream(grayChannel.Length))
                {
                    encoder.Save(ms);
                    bytes = ms.ToArray();
                }

                if (bytes == null)
                    throw new InvalidDataException("Failed to save channel. Reason unknown.");

                string tempPath = Path.GetFileNameWithoutExtension(savePath) + "_" + channels[i] + ".png";
                string channelPath = Path.Combine(Path.GetDirectoryName(savePath), UsefulDotNetThings.General.IO.FindValidNewFileName(tempPath));
                File.WriteAllBytes(channelPath, bytes);
            }
        }

        static byte[] BuildGrayscaleFromChannel(byte[] pixels, int channel)
        {
            byte[] destination = new byte[pixels.Length / 4];
            for (int i = channel, count = 0; i < pixels.Length; i += 4, count++)
                destination[count] = pixels[i];


            return destination;
        }
    }
}
