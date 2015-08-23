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
using static CSharpImageLibrary.DDSGeneral;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides ATI1 format functionality.
    /// This is a single channel, 8 bit image.
    /// </summary>
    public static class ATI1
    {
        public static void TestWrite(MemoryTributary imgData, string destination, int Width, int Height)
        {
            // write out as png
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            int stride = 4 * Width;
            BitmapSource source = BitmapFrame.Create(Height, Width, 96, 96, PixelFormats.Bgr32, BitmapPalettes.Halftone125, imgData.ToArray(), stride);
            encoder.Frames.Add(BitmapFrame.Create(source));

            using (FileStream fs = new FileStream(destination, FileMode.Create))
                encoder.Save(fs);
        }


        /// <summary>
        /// Loads useful information from ATI1 image file.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <returns>RGBA Pixel data as stream.</returns>
        public static MemoryTributary Load(string imageFile, out double Width, out double Height)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(fs, out Width, out Height);
        }


        /// <summary>
        /// Loads useful information from ATI1 image stream. FROM RESIL - I just ported it.
        /// </summary>
        /// <param name="stream">Stream containing entire image. NOT just pixels.</param>
        /// <param name="Width">Detected Width.</param>
        /// <param name="Height">Detected Height.</param>
        /// <returns>RGBA Pixel Data as stream.</returns>
        public static MemoryTributary Load(Stream stream, out double Width, out double Height)
        {
            broken
            // KFreon: Necessary to move stream position along to pixel data.
            DDS_HEADER header = null;
            Format format = ImageFormats.ParseDDSFormat(stream, out header);

            Width = header.dwWidth;
            Height = header.dwHeight;

            int mipMapBytes = (int)(Width * Height * 3);  // KFreon: 2 bytes per pixel
            MemoryTributary imgData = new MemoryTributary((int)(4 * Width * Height));
            byte[] test = new byte[(int)(Width * Height)];

            int t1 = 0;
            uint bitmask = 0;
            int t2 = 0;
            int[] Colours = new int[8];

            int BPS = (int)Width;

            for (int h = 0; h < Height; h+=4)
            {
                for (int w = 0; w < Width; w+=4)
                {
                    // Read palette
                    t1 = Colours[0] = stream.ReadByte(); 
                    t2 = Colours[1] = stream.ReadByte();

                    if (t1 > t2)
                    {
                        for (int i = 2; i < 8; ++i)
                            Colours[i] = t1 + ((t2 - t1) * (i - 1)) / 7;
                    }
                    else
                    {
                        for (int i = 2; i < 6; ++i)
                            Colours[i] = t1 + ((t2 - t1) * (i - 1)) / 5;
                        Colours[6] = 0;
                        Colours[7] = 255;
                    }

                    // KFreon: Decompress pixel data
                    int CurrentOffset = 0;
                    for (int k = 0; k < 4;  k += 2)
                    {
                        // KFreon: First 3 bytes
                        byte[] first3 = new byte[3];
                        stream.Read(first3, 0, 3);
                        bitmask = (uint)(first3[0] << 0) | (uint)(first3[1] << 8) | (uint)(first3[2] << 16);

                        for (int j = 0; j < 2; j++)
                        {
                            // KFreon: Only put pixels out < height - I have no idea what this means...
                            if ((h + k + j) < Height)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    // KFreon: Only put pixels out < width - Again...what is this?
                                    if ((w + i) < Width)
                                    {
                                        t1 = CurrentOffset + w + i;

                                        // KFreon: Set value
                                        imgData.Seek(t1 * 4, SeekOrigin.Begin);
                                        byte colour = (byte)Colours[bitmask & 0x07];
                                        test[t1] = colour;

                                        // KFreon: Write same value in all 3 channels - white
                                        imgData.WriteByte(colour);
                                        imgData.WriteByte(colour);
                                        imgData.WriteByte(colour);
                                    }
                                    bitmask >>= 3;
                                }
                                CurrentOffset += BPS;
                            }
                        }
                    }
                }
            }

            arraywrite(test, (int)Width, (int)Height);

            return imgData;
        }

        private static void arraywrite(byte[] data, int width, int height)
        {
            List<string> lines = new List<string>();

            int count = 0;
            for (int h = 0; h < height; h++)
            {
                string line = "";
                for (int w = 0;w<width; w++)
                {
                    line += data[count++] + " ";
                }
                lines.Add(line);
            }

            Debug.WriteLine("");
            Debug.WriteLine(String.Join(Environment.NewLine, lines.ToArray()));
        }
    }
}
