using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;
using static CSharpImageLibrary.Headers.DDS_Header.RawDDSHeaderStuff;
using static CSharpImageLibrary.ImageFormats;

namespace CSharpImageLibrary.Headers
{
    /// <summary>
    /// Contains header information about a DDS File.
    /// </summary>
    public partial class DDS_Header : AbstractHeader
    {
        public override HeaderType Type => HeaderType.DDS;

        const int MaxHeaderSize = ImageFormats.DDS_DX10_HEADER_LENGTH;

        /// <summary>
        /// Characters beginning a file that indicate file is a DDS image.
        /// </summary>
        public const string Identifier = "DDS ";

        #region Properties
        /// <summary>
        /// Size of header in bytes. Must be 124.
        /// </summary>
        public int dwSize { get; set; }

        /// <summary>
        /// Option flags.
        /// </summary>
        public DDSdwFlags dwFlags { get; set; }

        /// <summary>
        /// Pitch or linear size. I think this is stride in Windows lingo?
        /// </summary>
        public int dwPitchOrLinearSize { get; set; }

        /// <summary>
        /// Image depth. Usually not used.
        /// </summary>
        public int dwDepth { get; set; }

        /// <summary>
        /// Number of mipmaps.
        /// </summary>
        public int dwMipMapCount { get; set; }

        /// <summary>
        /// Not used, as per Windows DDS spec.
        /// </summary>
        public int[] dwReserved1 = new int[11];

        /// <summary>
        /// Pixel format of DDS.
        /// </summary>
        public DDS_PIXELFORMAT ddspf { get; set; }

        /// <summary>
        /// More option flags.
        /// </summary>
        public DDSdwCaps dwCaps { get; set; }

        /// <summary>
        /// Don't think it's used.
        /// </summary>
        public int dwCaps2;

        /// <summary>
        /// Don't think it's used.
        /// </summary>
        public int dwCaps3;

        /// <summary>
        /// Don't think it's used.
        /// </summary>
        public int dwCaps4;

        /// <summary>
        /// Not used as per Windows DDS spec.
        /// </summary>
        public int dwReserved2;
        #endregion Properties

        /// <summary>
        /// Additional header for newer DX10 images.
        /// </summary>
        public DDS_DXGI_DX10_Additional DX10_DXGI_AdditionalHeader { get; private set; }

        string DDS_FlagStringify(Type enumType)
        {
            string flags = "";

            string[] names = Enum.GetNames(enumType);
            int[] values = (int[])Enum.GetValues(enumType);
            for (int i = 0; i < names.Length; i++)
            {
                if (((int)dwFlags & values[i]) != 0)
                    flags += $"[{names[i]}] ";
            }
            
            return flags;
        }

        /// <summary>
        /// Reads DDS header from stream.
        /// </summary>
        /// <param name="stream">Fully formatted DDS image.</param>
        /// <returns>Header length.</returns>
        protected override long Load(Stream stream)
        {
            base.Load(stream);
            var temp = stream.ReadBytes(MaxHeaderSize);

            if (!CheckIdentifier(temp))
                throw new FormatException("Stream is not a recognised DDS image.");

            // Start header
            dwSize = BitConverter.ToInt32(temp, 4);
            dwFlags = (DDSdwFlags)BitConverter.ToInt32(temp, 8);
            Height = BitConverter.ToInt32(temp, 12);
            Width = BitConverter.ToInt32(temp, 16);
            dwPitchOrLinearSize = BitConverter.ToInt32(temp, 20);
            dwDepth = BitConverter.ToInt32(temp, 24);
            dwMipMapCount = BitConverter.ToInt32(temp, 28);
            for (int i = 0; i < 11; ++i)
                dwReserved1[i] = BitConverter.ToInt32(temp, 32 + (i * 4));

            // DDS PixelFormat
            ddspf = new DDS_PIXELFORMAT(temp);

            dwCaps = (DDSdwCaps)BitConverter.ToInt32(temp, 108);
            dwCaps2 = BitConverter.ToInt32(temp, 112);
            dwCaps3 = BitConverter.ToInt32(temp, 116);
            dwCaps4 = BitConverter.ToInt32(temp, 120);
            dwReserved2 = BitConverter.ToInt32(temp, 124);

            // DX10 Additional header
            if (ddspf.dwFourCC == FourCC.DX10)
                DX10_DXGI_AdditionalHeader = new DDS_DXGI_DX10_Additional(temp);

            return MaxHeaderSize;
        }

        /// <summary>
        /// Read Header from DDS Image.
        /// </summary>
        /// <param name="stream">Fully formatted DDS image.</param>
        public DDS_Header(Stream stream)
        {
            Load(stream);
        }

        /// <summary>
        /// Creates a DDS header from a set of information.
        /// </summary>
        /// <param name="Mips">Number of mipmaps.</param>
        /// <param name="Height">Height of top mipmap.</param>
        /// <param name="Width">Width of top mipmap.</param>
        /// <param name="formatDetails">Format details that header represents.</param>
        public DDS_Header(int Mips, int Height, int Width, ImageEngineFormatDetails formatDetails)
        {
            ImageEngineFormat surfaceformat = formatDetails.SurfaceFormat;
            var dx10Format = formatDetails.DX10Format;

            dwSize = 124;
            dwFlags = DDSdwFlags.DDSD_CAPS | DDSdwFlags.DDSD_HEIGHT | DDSdwFlags.DDSD_WIDTH | DDSdwFlags.DDSD_PIXELFORMAT | (Mips != 1 ? DDSdwFlags.DDSD_MIPMAPCOUNT : 0);
            this.Width = Width;
            this.Height = Height;
            dwCaps = DDSdwCaps.DDSCAPS_TEXTURE | (Mips == 1 ? 0 : DDSdwCaps.DDSCAPS_COMPLEX | DDSdwCaps.DDSCAPS_MIPMAP);
            dwMipMapCount = Mips == 1 ? 1 : Mips;
            ddspf = new DDS_PIXELFORMAT(surfaceformat);

            if (formatDetails.IsDX10)
            {
                DX10_DXGI_AdditionalHeader = new DDS_DXGI_DX10_Additional
                {
                    dxgiFormat = dx10Format,
                    resourceDimension = D3D10_RESOURCE_DIMENSION.DDS_DIMENSION_TEXTURE2D,
                    miscFlag = DDS_DXGI_DX10_Additional.D3D10_RESOURCE_MISC_FLAGS.D3D10_RESOURCE_MISC_GENERATE_MIPS,
                    miscFlags2 = DXGI_MiscFlags.DDS_ALPHA_MODE_UNKNOWN,
                    arraySize = 1
                };
            }
                
        }

        internal static bool CheckIdentifier(byte[] IDBlock)
        {
            for (int i = 0; i < Identifier.Length; i++)
                if (IDBlock[i] != Identifier[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Writes header to destination array starting at index.
        /// </summary>
        /// <param name="destination">Array to write header to.</param>
        /// <param name="index">Index in destination to start writing at.</param>
        public void WriteToArray(byte[] destination, int index)
        {
            List<byte> headerData = new List<byte>(150);

            // KFreon: Write magic number ("DDS")
            headerData.AddRange(BitConverter.GetBytes(0x20534444));

            // KFreon: Write all header fields regardless of filled or not
            headerData.AddRange(BitConverter.GetBytes(dwSize));
            headerData.AddRange(BitConverter.GetBytes((int)dwFlags));
            headerData.AddRange(BitConverter.GetBytes(Height));
            headerData.AddRange(BitConverter.GetBytes(Width));
            headerData.AddRange(BitConverter.GetBytes(dwPitchOrLinearSize));
            headerData.AddRange(BitConverter.GetBytes(dwDepth));
            headerData.AddRange(BitConverter.GetBytes(dwMipMapCount));

            // KFreon: Write reserved1
            for (int i = 0; i < 11; i++)
                headerData.AddRange(BitConverter.GetBytes((int)0));

            // KFreon: Write PIXELFORMAT
            headerData.AddRange(BitConverter.GetBytes(ddspf.dwSize));
            headerData.AddRange(BitConverter.GetBytes((int)ddspf.dwFlags));
            headerData.AddRange(BitConverter.GetBytes((int)ddspf.dwFourCC));
            headerData.AddRange(BitConverter.GetBytes(ddspf.dwRGBBitCount));
            headerData.AddRange(BitConverter.GetBytes(ddspf.dwRBitMask));
            headerData.AddRange(BitConverter.GetBytes(ddspf.dwGBitMask));
            headerData.AddRange(BitConverter.GetBytes(ddspf.dwBBitMask));
            headerData.AddRange(BitConverter.GetBytes(ddspf.dwABitMask));


            headerData.AddRange(BitConverter.GetBytes((int)dwCaps));
            headerData.AddRange(BitConverter.GetBytes(dwCaps2));
            headerData.AddRange(BitConverter.GetBytes(dwCaps3));
            headerData.AddRange(BitConverter.GetBytes(dwCaps4));
            headerData.AddRange(BitConverter.GetBytes(dwReserved2));


            if (DX10_DXGI_AdditionalHeader.dxgiFormat != DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
            {
                headerData.AddRange(BitConverter.GetBytes((int)DX10_DXGI_AdditionalHeader.dxgiFormat));
                headerData.AddRange(BitConverter.GetBytes((int)DX10_DXGI_AdditionalHeader.resourceDimension));
                headerData.AddRange(BitConverter.GetBytes((uint)DX10_DXGI_AdditionalHeader.miscFlag));
                headerData.AddRange(BitConverter.GetBytes(DX10_DXGI_AdditionalHeader.arraySize));
                headerData.AddRange(BitConverter.GetBytes((uint)DX10_DXGI_AdditionalHeader.miscFlags2));
            }

            headerData.CopyTo(destination, index);
        }
    }
}
