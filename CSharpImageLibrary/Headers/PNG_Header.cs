using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary.Headers
{
    /// <summary>
    /// Contains information about a PNG file header.
    /// </summary>
    public class PNG_Header : AbstractHeader
    {
        /// <summary>
        /// Header Chunk containing specific PNG header information.
        /// </summary>
        public struct PNGChunk
        {
            /// <summary>
            /// Length of chunk.
            /// </summary>
            public int Length;

            /// <summary>
            /// Type of chunk.
            /// </summary>
            public int ChunkType;

            /// <summary>
            /// Data in chunk.
            /// </summary>
            public byte[] ChunkData;

            /// <summary>
            /// Cyclic Redundancy Check of chunk to determine validity. 
            /// Doesn't include Length field.
            /// Always present regardless of chunk data presence.
            /// </summary>
            public int CRC;

            /// <summary>
            /// Reads a PNG header chunk.
            /// </summary>
            /// <param name="headerBlock">Block of data containing headers.</param>
            /// <param name="offset">Offset in data to begin header.</param>
            public PNGChunk(byte[] headerBlock, int offset = 8)
            {
                Length = BitConverter.ToInt32(headerBlock, offset);
                ChunkType = BitConverter.ToInt32(headerBlock, offset + 4);
                ChunkData = new byte[Length];
                Array.Copy(headerBlock, ChunkData, Length);
                CRC = BitConverter.ToInt32(headerBlock, offset + 4 + Length);
            }
        }

        public enum ColourType
        {
            None = 0,
            Palette = 1,
            Colour = 2, 
            PaletteAndColour = 3,
            Alpha = 4,
            AlphaAndColour = 6
        }

        const int HeaderSize = 8 + (4 + 4 + 13 + 4);  // Only need to read identifier and IHDR chunk.

        /// <summary>
        /// Characters beginning the file marking it as a GIF image.
        /// </summary>
        public static string Identifier = "PNG";  // There are other chars before and after.

        #region Properties
        /// <summary>
        /// Bit depth PER PALETTE INDEX, not per pixel. Valid values 1, 2, 4, 8, 16.
        /// </summary>
        public byte BitDepth { get; private set; }

        /// <summary>
        /// Type of colour mapping method used.
        /// </summary>
        public ColourType colourType { get; private set; }

        /// <summary>
        /// Compression Method. Currently must be 0 (deflate)
        /// </summary>
        public byte CompressionMethod { get; private set; }

        /// <summary>
        /// Indicates preprocessing method performed before compression.
        /// Currently must be 0 (Adaptive filtering)
        /// </summary>
        public byte FilterMethod { get; private set; }

        /// <summary>
        /// Indicates interlacing method.
        /// 0 = none, 1 = Adam7
        /// </summary>
        public byte InterlaceMethod { get; private set; }
        #endregion Properties

        /// <summary>
        /// Image format.
        /// </summary>
        public override Format Format
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal static bool CheckIdentifier(byte[] IDBlock)
        {
            // ë
            if (IDBlock[0] != 137)
                return false;

            // Chars = 'PNG'
            for (int i = 1; i < Identifier.Length; i++)
                if (IDBlock[i] != Identifier[i])
                    return false;

            // \n\r→\n
            if (IDBlock[4] != 13 || IDBlock[5] != 10 || IDBlock[6] != 26 || IDBlock[7] != 10)
                return false;

            return true;
        }

        protected override long Load(Stream stream)
        {
            base.Load(stream);
            byte[] temp = new byte[HeaderSize];
            stream.Read(temp, 0, HeaderSize);

            if (!CheckIdentifier(temp))
                throw new FormatException("Stream is not a PNG Image");

            PNGChunk header = new PNGChunk(temp);
            Width = BitConverter.ToInt32(header.ChunkData, 0);
            Height = BitConverter.ToInt32(header.ChunkData, 4);
            BitDepth = header.ChunkData[8];
            colourType = (ColourType)header.ChunkData[9];
            CompressionMethod = header.ChunkData[10];
            FilterMethod = header.ChunkData[11];
            InterlaceMethod = header.ChunkData[12];

            return -1;  // Since we don't know the length of the entire header, no point returning any value.
        }

        public PNG_Header(Stream stream)
        {
            Load(stream);
        }

        public override string ToString()
        {
            return base.ToString();asf
        }
    }
}
