using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary.Headers
{
    /// <summary>
    /// Contains information about a JPG image.
    /// </summary>
    public class JPG_Header : AbstractHeader
    {
        /// No printable characters mark a jpg. See <see cref="CheckIdentifier(byte[])"/>.

        const int HeaderSize = 20;
        #region Properties
        /// <summary>
        /// Length of data section (APP0/JFIF), includes thumbnail etc.
        /// </summary>
        public int DataSectionLength { get; private set; }

        /// <summary>
        /// Identifier as a JPEG image. Should always be JFIF.
        /// </summary>
        public string Identifier { get; private set; }

        /// <summary>
        /// Version of JFIF file was created with.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Units of resolution. Dunno what means what though.
        /// </summary>
        public byte ResolutionUnits { get; private set; }

        /// <summary>
        /// Horizontal resolution in unit specified by <see cref="ResolutionUnits"/>.
        /// </summary>
        public int HorizontalResolution { get; private set; }

        /// <summary>
        /// Vertical resolution in unit specified by <see cref="ResolutionUnits"/>.
        /// </summary>
        public int VerticalResolution { get; private set; }
        public int xpix { get; private set; }
        public int ypix { get; private set; } // TODO: Check if this is size of image or thumbnail.
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
            return IDBlock[0] == 0xFF && IDBlock[1] == 0xD8 && IDBlock[2] == 0xFF;
        }

        /// <summary>
        /// Read header of JPG image.
        /// </summary>
        /// <param name="stream">Fully formatted JPG image.</param>
        /// <returns>Length of header.</returns>
        protected override long Load(Stream stream)
        {
            base.Load(stream);
            byte[] temp = new byte[HeaderSize];
            stream.Read(temp, 0, temp.Length);

            if (!CheckIdentifier(temp))
                throw new FormatException("Stream is not a BMP Image");

            DataSectionLength = BitConverter.ToInt16(temp, 4);
            Identifier = BitConverter.ToString(temp, 6, 5);
            Version = BitConverter.ToInt16(temp, 11);
            ResolutionUnits = temp[13];
            HorizontalResolution = BitConverter.ToInt16(temp, 14);
            VerticalResolution = BitConverter.ToInt16(temp, 16);
            xpix = temp[17];
            ypix = temp[18];

            return HeaderSize;
        }

        /// <summary>
        /// Loads a JPG header.
        /// </summary>
        /// <param name="stream"></param>
        public JPG_Header(Stream stream)
        {
            Load(stream);
        }

        public override string ToString()
        {
            return base.ToString();asf
        }
    }
}
