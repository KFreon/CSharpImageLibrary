using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace CSharpImageLibrary
{
    public static class ATI2_3Dc
    {
        public static MemoryTributary Load(string imagePath, out double Width, out double Height)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }

        public static MemoryTributary Load(Stream stream, out double Width, out double Height)
        {
            return DDSGeneral.LoadBlockCompressedTexture(stream, out Width, out Height, DecompressATI2Block);
        }


        private static List<byte[]> DecompressATI2Block(Stream compressed)
        {
            byte[] red = DDSGeneral.Decompress8BitBlock(compressed);
            byte[] green = DDSGeneral.Decompress8BitBlock(compressed);
            List<byte[]> DecompressedBlock = new List<byte[]>();
            DecompressedBlock.Add(red);
            DecompressedBlock.Add(green);
            DecompressedBlock.Add(new byte[16]);
            DecompressedBlock.Add(new byte[16]);

            return DecompressedBlock;
        }
    }
}
