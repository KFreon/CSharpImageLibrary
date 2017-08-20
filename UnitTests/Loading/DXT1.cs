using CSharpImageLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Loading
{
    [TestClass]
    public class DXT1
    {
        [TestMethod]
        public void AttemptLoad()
        {
            try
            {
                using (ImageEngineImage img = new ImageEngineImage("", 5))
                {
                    Assert.AreNotEqual(img.Format, ImageEngineFormat.Unknown);
                }
            }
            catch (Exception e)
            {
                Assert.Fail(e.ToString());
            }
        }



        [TestMethod]
        public void LoadAndCheckProperties()
        {
            using (ImageEngineImage img = new ImageEngineImage("", 5))
            {
                Assert.AreNotEqual(img.BitCount, -1, 1.1);
                Assert.AreEqual(img.BlockSize, 8);
                Assert.AreEqual(img.ComponentSize, 1);
                Assert.AreNotEqual(img.CompressedSize, -1, 1.1);
                Assert.AreEqual(img.FileExtension, ".dds");
                Assert.AreNotEqual(img.FilePath, null);
                Assert.AreEqual(img.Format, ImageEngineFormat.DDS_DXT1);
                Assert.AreNotEqual(img.Height, -1, 1.1);
                Assert.AreEqual(img.IsBlockCompressed, true);
                Assert.AreNotEqual(img.MipMaps.Count, -1, 1.1);
                Assert.AreEqual(img.NumberOfChannels, 4);
                Assert.AreEqual(img.NumMipMaps, img.MipMaps.Count);
                Assert.AreNotEqual(img.NumMipMaps, -1, 1.1);
                Assert.AreNotEqual(img.Width, -1, 1.1);
                Assert.AreNotEqual(img.UncompressedSize, -1, 1.1);
            }
        }
    }
}
