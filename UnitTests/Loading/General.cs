using CSharpImageLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CSharpImageLibrary.ImageFormats;

namespace UnitTests.Loading
{
    internal static class General
    {
        public static void CheckProperties(string resourceName, int maxDimension, ImageEngineFormat format)
        {
            ImageEngineFormatDetails details = new ImageEngineFormatDetails(format);
            var bytes = GetResource(resourceName);

            using (ImageEngineImage img = new ImageEngineImage(bytes, maxDimension))
            {
                Assert.AreEqual(img.BitCount, details.BitCount);
                Assert.AreEqual(img.BlockSize, details.BlockSize);
                Assert.AreEqual(img.ComponentSize, details.ComponentSize);
                Assert.AreEqual(img.FileExtension, details.Extension);
                Assert.AreEqual(img.Format, format);
                Assert.AreEqual(img.IsBlockCompressed, details.IsBlockCompressed);
                Assert.AreEqual(img.NumberOfChannels, details.MaxNumberOfChannels);

                Assert.AreNotEqual(img.CompressedSize, -1, 1.1);
                Assert.AreNotEqual(img.FilePath, null);
                Assert.AreNotEqual(img.Height, -1, 1.1);
                Assert.AreNotEqual(img.MipMaps.Count, -1, 1.1);
                Assert.AreEqual(img.NumMipMaps, img.MipMaps.Count);
                Assert.AreNotEqual(img.NumMipMaps, -1, 1.1);
                Assert.AreNotEqual(img.Width, -1, 1.1);
                Assert.AreNotEqual(img.UncompressedSize, -1, 1.1);
            }
        }

        private static byte[] GetResource(string name)
        {
            byte[] bytes = null;
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
            }

            return bytes;
        }
    }
}
