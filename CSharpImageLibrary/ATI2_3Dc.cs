using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UsefulThings;
using static CSharpImageLibrary.DDSGeneral;

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
            DDS_HEADER header = null;
            Format format = ImageFormats.ParseDDSFormat(stream, out header);

            Width = header.dwWidth;
            Height = header.dwHeight;

            MemoryTributary imgData = new MemoryTributary(4 * (int)Width * (int)Height);

            // KFreon: Write to byte[] so we can have multiple readers
            byte[] data = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(data, 0, (int)stream.Length);


            int ptr1 = 0, ptr2 = 0, t1 = 0, t2 = 0;
            int[] YColours = new int[8], XColours = new int[8];
            uint bitmask1 = 0, bitmask2 = 0, CurrentOffset = 0;

            uint BPS = (uint)(Width * 32 / 8);

            for (int h = 0; h < Height; h+=4)
            {
                for (int w = 0; w < Width; w+=4)
                {
                    ptr2 = ptr1 + 8;

                    // Read Y Palette
                    ReadPalette(ptr1, ref data, ref YColours, out t1, out t2);
                    ptr1 += 2;


                    // Read X Palette
                    ReadPalette(ptr1, ref data, ref YColours, out t1, out t2);
                    ptr2 += 2;



                    // Decompress pixel data
                    CurrentOffset = 0;
                    for (int k = 0; k < 4; k += 2)
                    {
                        // First 3 bytes
                        bitmask1 = (uint)data[ptr1] << 0 | (uint)data[ptr1 + 1] << 8 | (uint)data[ptr1 + 2] << 16;
                        bitmask2 = (uint)data[ptr2] << 0 | (uint)data[ptr2 + 1] << 8 | (uint)data[ptr2 + 2] << 16;

                        for (int j = 0; j < 2; j++)
                        {
                            // Only put pixels out < height - KFreon: ...
                            if ((h + k + j) < Height)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    // Only put pixels out < width - KFreon: whyyyyy
                                    if ((w + i) < Width)
                                    {
                                        int t = 0, tx = 0, ty = 0;

                                        t1 = (int)(CurrentOffset + 3 * (w + i));
                                        imgData.Seek(t1 + 1, SeekOrigin.Begin);
                                        byte colour = (byte)YColours[bitmask1 & 0x07];
                                        ty = colour;
                                        imgData.WriteByte(colour);

                                        //calculate b (z) component ((r/255)^2 + (g/255)^2 + (b/255)^2 = 1
                                        // KFreon: ^ Whimsical magic
                                        t = 127 * 128 - (tx - 127) * (tx - 128) - (ty - 127) * (ty - 128);
                                        if (t > 0)
                                        {
                                            imgData.Seek(t1 + 2, SeekOrigin.Begin);
                                            imgData.WriteByte((byte)(Math.Sqrt(t) + 128));
                                        }
                                        else
                                        {
                                            imgData.Seek(t1 + 2, SeekOrigin.Begin);
                                            imgData.WriteByte(0x7F);
                                        }
                                    }
                                    bitmask1 >>= 3;
                                    bitmask2 >>= 3;
                                }
                                CurrentOffset += BPS;
                            }
                        }
                        ptr1 += 3;
                    }
                }
            }

            return imgData;
        }

        private static void ReadPalette(int ptr, ref byte[] data, ref int[] colours, out int t1, out int t2)
        {
            t1 = colours[0] = data[ptr];
            t2 = colours[1] = data[ptr + 1];
            if (t1 > t2)
            {
                for (int i = 2; i < 8; ++i)
                    colours[i] = t1 + ((t2 - t1) * (i - 1)) / 7;
            }
            else
            {
                for (int i = 2; i < 6; ++i)
                    colours[i] = t1 + ((t2 - t1) * (i - 1)) / 7;
                colours[6] = 0;
                colours[7] = 255;
            }
        }
    }
}
