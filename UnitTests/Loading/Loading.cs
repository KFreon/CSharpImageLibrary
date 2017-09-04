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
    public class Loading
    {
        static int MaxDimension = 32;

        [TestMethod]
        public void LoadAndCheckProperties_DXT1() => General.CheckProperties("DXT1.dds", MaxDimension, ImageEngineFormat.DDS_DXT1);

        [TestMethod]
        public void LoadAndCheckProperties_DXT2() => General.CheckProperties("DXT2.dds", MaxDimension, ImageEngineFormat.DDS_DXT2);

        [TestMethod]
        public void LoadAndCheckProperties_DXT3() => General.CheckProperties("DXT3.dds", MaxDimension, ImageEngineFormat.DDS_DXT3);

        [TestMethod]
        public void LoadAndCheckProperties_DXT4() => General.CheckProperties("DXT4.dds", MaxDimension, ImageEngineFormat.DDS_DXT4);

        [TestMethod]
        public void LoadAndCheckProperties_DXT5() => General.CheckProperties("DXT5.dds", MaxDimension, ImageEngineFormat.DDS_DXT5);

        [TestMethod]
        public void LoadAndCheckProperties_ATI1() => General.CheckProperties("ATI1.dds", MaxDimension, ImageEngineFormat.DDS_ATI1);

        [TestMethod]
        public void LoadAndCheckProperties_ATI2() => General.CheckProperties("ATI2.dds", MaxDimension, ImageEngineFormat.DDS_ATI2_3Dc);

        [TestMethod]
        public void LoadAndCheckProperties_BC6() => General.CheckProperties("BC6.dds", MaxDimension, ImageEngineFormat.DDS_BC6);

        [TestMethod]
        public void LoadAndCheckProperties_BC7() => General.CheckProperties("BC7.dds", MaxDimension, ImageEngineFormat.DDS_BC7);

        [TestMethod]
        public void LoadAndCheckProperties_A8() => General.CheckProperties("A8.dds", MaxDimension, ImageEngineFormat.DDS_A8);

        [TestMethod]
        public void LoadAndCheckProperties_A8L8() => General.CheckProperties("A8L8.dds", MaxDimension, ImageEngineFormat.DDS_A8L8);

        [TestMethod]
        public void LoadAndCheckProperties_ABGR8() => General.CheckProperties("ABGR8.dds", MaxDimension, ImageEngineFormat.DDS_ABGR_8);

        [TestMethod]
        public void LoadAndCheckProperties_ARGB32F() => General.CheckProperties("ARGB32F.dds", MaxDimension, ImageEngineFormat.DDS_ARGB_32F);

        [TestMethod]
        public void LoadAndCheckProperties_ARGB4() => General.CheckProperties("ARGB4.dds", MaxDimension, ImageEngineFormat.DDS_ARGB_4);

        [TestMethod]
        public void LoadAndCheckProperties_ARGB8() => General.CheckProperties("ARGB8.dds", MaxDimension, ImageEngineFormat.DDS_ARGB_8);        

        [TestMethod]
        public void LoadAndCheckProperties_G16R16() => General.CheckProperties("G16R16.dds", MaxDimension, ImageEngineFormat.DDS_G16_R16);

        [TestMethod]
        public void LoadAndCheckProperties_G8_L8() => General.CheckProperties("G8L8.dds", MaxDimension, ImageEngineFormat.DDS_G8_L8);

        [TestMethod]
        public void LoadAndCheckProperties_R5G6B5() => General.CheckProperties("R5G6B5.dds", MaxDimension, ImageEngineFormat.DDS_R5G6B5);

        [TestMethod]
        public void LoadAndCheckProperties_RGB8() => General.CheckProperties("RGB8.dds", MaxDimension, ImageEngineFormat.DDS_RGB_8);

        [TestMethod]
        public void LoadAndCheckProperties_V8U8() => General.CheckProperties("V8U8.dds", MaxDimension, ImageEngineFormat.DDS_V8U8);

        [TestMethod]
        public void LoadAndCheckProperties_GIF() => General.CheckProperties("GIF.gif", MaxDimension, ImageEngineFormat.GIF);

        [TestMethod]
        public void LoadAndCheckProperties_JPG() => General.CheckProperties("JPG.jpg", MaxDimension, ImageEngineFormat.JPG);

        [TestMethod]
        public void LoadAndCheckProperties_PNG() => General.CheckProperties("PNG.png", MaxDimension, ImageEngineFormat.PNG);

        [TestMethod]
        public void LoadAndCheckProperties_TGA() => General.CheckProperties("TGA.tga", MaxDimension, ImageEngineFormat.TGA);

        [TestMethod]
        public void LoadAndCheckProperties_TIF() => General.CheckProperties("TIF.tif", MaxDimension, ImageEngineFormat.TIF);

        [TestMethod]
        public void LoadAndCheckProperties_BMP() => General.CheckProperties("BMP.bmp", MaxDimension, ImageEngineFormat.BMP);
    }
}
