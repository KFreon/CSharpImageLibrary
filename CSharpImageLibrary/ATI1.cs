using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides ATI1 format functionality.
    /// This is a single channel, 8 bit image.
    /// </summary>
    public static class ATI1
    {
        public static MemoryTributary Load(string imagePath, out double Width, out double Height)
        {
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }

        public static MemoryTributary Load(Stream stream, out double Width, out double Height)
        {
            return DDSGeneral.LoadBlockCompressedTexture(stream, out Width, out Height, DecompressATI1);
        }

        private static List<byte[]> DecompressATI1(Stream compressed)
        {
            byte[] channel = DDSGeneral.Decompress8BitBlock(compressed);
            List<byte[]> DecompressedBlock = new List<byte[]>();
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(channel);
            DecompressedBlock.Add(new byte[16]);  // No alpha
            return DecompressedBlock;
        }
    }
}
