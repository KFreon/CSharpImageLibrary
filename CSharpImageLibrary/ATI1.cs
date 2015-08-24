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
            DDS_HEADER header;
            Format format = ImageFormats.ParseDDSFormat(stream, out header);

            Width = header.dwWidth;
            Height = header.dwHeight;

            MemoryTributary imgData = new MemoryTributary(4 * (int)Width * (int)Height);

            // KFreon: h is a row so it should increment by number of pixels in a row
            int row = 0;
            for (int h = 0;h<Width * Height; h += (int)Width * 4)  // Skip 4 rows as they'll be decoded as texel units
            {
                for (int w = 0; w < Width; w += 4 * 4)  // Skip 4 pixels as they'll be decoded as a texel unit
                {
                    // KFreon: Decompress current texel
                    byte[] decompressed = DecompressBlock(stream);

                    // KFreon: Write to output. NOTE: Texel is 4x4 block, so not contiguous write.

                    int count = 0;
                    for (int i = h; i < 4*4*(int)Width; i += (int)Width*4)
                    {
                        for (int j = w; j < 4 * 4; j += 4)
                        {
                            // KFreon: Seek to offset and write pixel
                            imgData.Seek(i + j, SeekOrigin.Begin);
                            imgData.WriteByte(decompressed[count]);  // KFreon: x3 cos it's a single channel texture represented in RGBA so it'll be grayscale.
                            imgData.WriteByte(decompressed[count]);
                            imgData.WriteByte(decompressed[count]);
                            imgData.Position++;  // KFreon: Skip alpha
                            count++;
                        }
                    }
                }
                row++;
            }

            return imgData;
        }


        private static byte[] DecompressBlock(Stream compressed)
        {
            byte[] DecompressedBlock = new byte[16];

            // KFreon: Read colour range and build palette
            ushort[] Colours = new ushort[8];
            
            // KFreon: Read min and max colours (not necessarily in that order)
            Colours[0] = (byte)compressed.ReadByte();
            Colours[1] = (byte)compressed.ReadByte();

            // KFreon: Choose which type of interpolation required.
            if (Colours[0] > Colours[1])
            {
                // KFreon: Interpolate other colours
                for (int i = 2; i < 8; i++)
                {
                    int firstbit = (8 - i);
                    int secondbit = (i - 1);
                    double test = (firstbit * Colours[0] + secondbit * Colours[1]) / 7.0f;
                    Colours[i] = (ushort)test;
                }
            }
            else
            {
                // KFreon: Interpolate other colours and add OPACITY
                for (int i = 2; i < 6; i++)
                    Colours[i] = (ushort)(((6 - i) * Colours[0] + (i - 1) * Colours[1]) / 5.0);
                Colours[6] = 0;
                Colours[7] = 255;
            }


            // KFreon: Decompress pixels
            ulong bitmask = (ulong)compressed.ReadByte() << 0 | (ulong)compressed.ReadByte() << 8 | (ulong)compressed.ReadByte() << 16 |   // KFreon: Read all 6 compressed bytes into single 
                (ulong)compressed.ReadByte() << 24 | (ulong)compressed.ReadByte() << 32 | (ulong)compressed.ReadByte() << 40;


            // KFreon: Bitshift and mask compressed data to get 3 bit indicies, and retrieve indexed colour of pixel.
            for (int i = 0; i < 16; i++)
            {
                if (i % 4 == 0)
                    Debug.WriteLine("");
                Debug.Write($"{bitmask >> (i * 3) & 0x7} ");
                DecompressedBlock[i] = (byte)Colours[bitmask >> (i * 3) & 0x7];
            }
            Debug.WriteLine("");

            return DecompressedBlock;
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
