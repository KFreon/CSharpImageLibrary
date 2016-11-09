using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary.Headers
{
    /// <summary>
    /// Contains header information about a DDS File.
    /// </summary>
    public class DDS_Header : AbstractHeader
    {
        const int HeaderSize = 148;

        /// <summary>
        /// Characters beginning a file that indicate file is a DDS image.
        /// </summary>
        public const string Identifier = "DDS ";

        #region Standard Enums and structs
        /// <summary>
        /// Contains information about DDS Pixel Format.
        /// </summary>
        public struct DDS_PIXELFORMAT
        {
            /// <summary>
            /// Size in bytes?
            /// </summary>
            public int dwSize { get; set; }

            /// <summary>
            /// Option flags.
            /// </summary>
            public int dwFlags { get; set; }

            /// <summary>
            /// String version of flags showing names of each.
            /// </summary>
            public string dwFlagsString
            {
                get
                {
                    return ((DDSdwFlags)dwFlags).ToString();
                }
            }

            /// <summary>
            /// FourCC of DDS, i.e. DXT1, etc
            /// </summary>
            public FourCC dwFourCC { get; set; }

            /// <summary>
            /// RGB Channel width.
            /// </summary>
            public int dwRGBBitCount { get; set; }

            /// <summary>
            /// Red bit mask. i.e. pixel is FF12AA22, so mask might be FF000000 and we get pure red.
            /// </summary>
            public uint dwRBitMask { get; set; }

            /// <summary>
            /// Green bit mask. i.e. pixel is FF12AA22, so mask might be 00FF0000 and we get 00120000.
            /// </summary>
            public uint dwGBitMask { get; set; }

            /// <summary>
            /// Blue bit mask. i.e. pixel is FF12AA22, so mask might be 0000FF00 and we get 0000AA00.
            /// </summary>
            public uint dwBBitMask { get; set; }

            /// <summary>
            /// Alpha bit mask. i.e. pixel is FF12AA22, so mask might be 000000FF and we get 00000022.
            /// </summary>
            public uint dwABitMask { get; set; }

            /// <summary>
            /// Fill PixelFormat from full DDS header
            /// </summary>
            /// <param name="temp"></param>
            public DDS_PIXELFORMAT(byte[] temp)
            {
                dwSize = BitConverter.ToInt32(temp, 72);
                dwFlags = BitConverter.ToInt32(temp, 76);
                dwFourCC = (FourCC)BitConverter.ToInt32(temp, 80);
                dwRGBBitCount = BitConverter.ToInt32(temp, 84);
                dwRBitMask = BitConverter.ToUInt32(temp, 88);
                dwGBitMask = BitConverter.ToUInt32(temp, 92);
                dwBBitMask = BitConverter.ToUInt32(temp, 96);
                dwABitMask = BitConverter.ToUInt32(temp, 100);
            }

            /// <summary>
            /// String representation of DDS pixel format.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("--DDS_PIXELFORMAT--");
                sb.AppendLine($"dwSize: {dwSize}");
                sb.AppendLine($"dwFlags: 0x{dwFlags.ToString("X")}");  // As hex
                sb.AppendLine($"dwFourCC: 0x{dwFourCC.ToString("X")}");  // As Hex
                sb.AppendLine($"dwRGBBitCount: {dwRGBBitCount}");
                sb.AppendLine($"dwRBitMask: 0x{dwRBitMask.ToString("X")}");  // As Hex
                sb.AppendLine($"dwGBitMask: 0x{dwGBitMask.ToString("X")}");  // As Hex
                sb.AppendLine($"dwBBitMask: 0x{dwBBitMask.ToString("X")}");  // As Hex
                sb.AppendLine($"dwABitMask: 0x{dwABitMask.ToString("X")}");  // As Hex
                sb.AppendLine("--END DDS_PIXELFORMAT--");
                return Environment.NewLine + sb.ToString() + Environment.NewLine;
            }
        }

        /// <summary>
        /// Old method of identifying Compressed textures.
        /// DX10 indicates new texture, the DX10 Additional header will contain the true format. See <see cref="DXGI_FORMAT"/>.
        /// </summary>
        public enum FourCC
        {
            /// <summary>
            /// (BC1) Block Compressed Texture. Compresses 4x4 texels.
            /// Used for Simple Non Alpha.
            /// </summary>
            DXT1 = 0x31545844,  // 1TXD i.e. DXT1 backwards

            /// <summary>
            /// (BC2) Block Compressed Texture. Compresses 4x4 texels.
            /// Used for Sharp Alpha. Premultiplied alpha. 
            /// </summary>
            DXT2 = 0x32545844,

            /// <summary>
            /// (BC2) Block Compressed Texture. Compresses 4x4 texels.
            /// Used for Sharp Alpha. 
            /// </summary>
            DXT3 = 0x33545844,

            /// <summary>
            /// (BC3) Block Compressed Texture. Compresses 4x4 texels.
            /// Used for Gradient Alpha. Premultiplied alpha.
            /// </summary>
            DXT4 = 0x34545844,

            /// <summary>
            /// (BC3) Block Compressed Texture. Compresses 4x4 texels.
            /// Used for Gradient Alpha. 
            /// </summary>
            DXT5 = 0x35545844,

            /// <summary>
            /// Fancy new DirectX 10+ format indicator. DX10 Header will contain true format.
            /// </summary>
            DX10 = 0x30315844,
        }

        public enum DDSdwFlags
        {
            DDSD_CAPS = 0x1,            // Required
            DDSD_HEIGHT = 0x2,          // Required
            DDSD_WIDTH = 0x4,           // Required
            DDSD_PITCH = 0x8,           // Required when Pitch is specified for uncompressed textures
            DDSD_PIXELFORMAT = 0x1000,  // Required in all DDS
            DDSD_MIPMAPCOUNT = 0x20000, // Required for a Mipmapped texture
            DDSD_LINEARSIZE = 0x80000,  // Required when Pitch is specified
            DDSD_DEPTH = 0x800000       // Required in Depth texture (Volume)
        }

        public enum DDSdwCaps
        {
            DDSCAPS_COMPLEX = 0x8,      // Optional, must be specified on image that has more than one surface. (mipmap, cube, volume)
            DDSCAPS_MIPMAP = 0x400000,  // Optional, should be set for mipmapped image
            DDSCAPS_TEXTURE = 0x1000    // Required
        }

        public enum DDS_PFdwFlags
        {
            DDPF_ALPHAPIXELS = 0x1,     // Texture has alpha - dwRGBAlphaBitMask has a value
            DDPF_ALPHA = 0x2,           // Older flag indicating alpha channel in uncompressed data. dwRGBBitCount has alpha channel bitcount, dwABitMask has valid data.
            DDPF_FOURCC = 0x4,          // Contains compressed RGB. dwFourCC has a value
            DDPF_RGB = 0x40,            // Contains uncompressed RGB. dwRGBBitCount and RGB bitmasks have a value
            DDPF_YUV = 0x200,           // Older flag indicating things set as YUV
            DDPF_LUMINANCE = 0x20000    // Older flag for single channel uncompressed data
        }
        #endregion Standard Enums and Structs

        #region DXGI/DX10
        /// <summary>
        /// Additional header used by DXGI/DX10 DDS'.
        /// </summary>
        public struct DDS_DXGI_DX10_Additional
        {
            /// <summary>
            /// Surface format.
            /// </summary>
            public DXGI_FORMAT dxgiFormat;

            /// <summary>
            /// Dimension of texture (1D, 2D, 3D)
            /// </summary>
            public D3D10_RESOURCE_DIMENSION resourceDimension;

            /// <summary>
            /// Identifies less common options. e.g. 0x4 = DDS_RESOURCE_MISC_TEXTURECUBE
            /// </summary>
            public uint miscFlag;

            /// <summary>
            /// Number of elements in array.
            /// For 2D textures that are cube maps, it's the number of cubes.
            /// For 3D textures, must be 1.
            /// </summary>
            public uint arraySize;

            /// <summary>
            /// Alpha flags.
            /// </summary>
            public DXGI_MiscFlags miscFlags2;

            /// <summary>
            /// Read DX10-DXGI header from full DDS header block.
            /// </summary>
            /// <param name="fullHeaderBlock">Entire DDS header block.</param>
            public DDS_DXGI_DX10_Additional(byte[] fullHeaderBlock)
            {
                dxgiFormat = (DXGI_FORMAT)BitConverter.ToInt32(fullHeaderBlock, 124);
                resourceDimension = (D3D10_RESOURCE_DIMENSION)BitConverter.ToInt64(fullHeaderBlock, 128);
                miscFlag = BitConverter.ToUInt32(fullHeaderBlock, 136);
                arraySize = BitConverter.ToUInt32(fullHeaderBlock, 140);
                miscFlags2 = (DXGI_MiscFlags)BitConverter.ToInt32(fullHeaderBlock, 144);
            }

            /// <summary>
            /// Shows string description of additional DX10 header.
            /// </summary>
            /// <returns>String header.</returns>
            public override string ToString()
            {
                return UsefulThings.General.StringifyObject(this, true);
            }
        }

        /// <summary>
        /// Option flags to indicate alpha mode.
        /// </summary>
        public enum DXGI_MiscFlags
        {
            /// <summary>
            /// Alpha content unknown. Default for legacy files, assumed to be "Straight".
            /// </summary>
            DDS_ALPHA_MODE_UNKNOWN = 0,

            /// <summary>
            /// Any alpha is "straight". Standard. i.e. RGBA all separate channels. 
            /// </summary>
            DDS_ALPHA_MODE_STRAIGHT = 1,

            /// <summary>
            /// Any alpha channels are premultiplied i.e. RGB (without A) with premultiplied alpha has values that include alpha by way of multiplying the original RGB with the A.
            /// </summary>
            DDS_ALPHA_MODE_PREMULTIPLIED = 2,

            /// <summary>
            /// Alpha is fully opaque.
            /// </summary>
            DDS_ALPHA_MODE_OPAQUE = 3,

            /// <summary>
            /// Alpha channel isn't for transparency.
            /// </summary>
            DDS_ALPHA_MODE_CUSTOM = 4,
        }

        /// <summary>
        /// Indicates type of DXGI/DX10 texture.
        /// </summary>
        public enum D3D10_RESOURCE_DIMENSION
        {
            // TODO: Incomplete?

            /// <summary>
            /// 1D Texture specified by dwWidth of DDS_Header = size of texture. Typically, dwHeight = 1 and DDSD_HEIGHT flag is also set in dwFlags.
            /// </summary>
            DDS_DIMENSION_TEXTURE1D = 2,

            /// <summary>
            /// 2D Texture specified by dwWidth and dwHeight of DDS_Header. Can be cube map if miscFlag and arraySize members are set.
            /// </summary>
            DDS_DIMENSION_TEXTURE2D = 3,

            /// <summary>
            /// 3D Texture specified by dwWidth, dwHeight, and dwDepth. Must have DDSD_DEPTH Flag set in dwFlags.
            /// </summary> 
            DDS_DIMENSION_TEXTURE3D = 4, 
        }

        /// <summary>
        /// DXGI/DX10 formats.
        /// </summary>
        public enum DXGI_FORMAT : uint
        {
            DXGI_FORMAT_UNKNOWN = 0,
            DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
            DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
            DXGI_FORMAT_R32G32B32A32_UINT = 3,
            DXGI_FORMAT_R32G32B32A32_SINT = 4,
            DXGI_FORMAT_R32G32B32_TYPELESS = 5,
            DXGI_FORMAT_R32G32B32_FLOAT = 6,
            DXGI_FORMAT_R32G32B32_UINT = 7,
            DXGI_FORMAT_R32G32B32_SINT = 8,
            DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
            DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
            DXGI_FORMAT_R16G16B16A16_UNORM = 11,
            DXGI_FORMAT_R16G16B16A16_UINT = 12,
            DXGI_FORMAT_R16G16B16A16_SNORM = 13,
            DXGI_FORMAT_R16G16B16A16_SINT = 14,
            DXGI_FORMAT_R32G32_TYPELESS = 15,
            DXGI_FORMAT_R32G32_FLOAT = 16,
            DXGI_FORMAT_R32G32_UINT = 17,
            DXGI_FORMAT_R32G32_SINT = 18,
            DXGI_FORMAT_R32G8X24_TYPELESS = 19,
            DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
            DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
            DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
            DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
            DXGI_FORMAT_R10G10B10A2_UNORM = 24,
            DXGI_FORMAT_R10G10B10A2_UINT = 25,
            DXGI_FORMAT_R11G11B10_FLOAT = 26,
            DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
            DXGI_FORMAT_R8G8B8A8_UNORM = 28,
            DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
            DXGI_FORMAT_R8G8B8A8_UINT = 30,
            DXGI_FORMAT_R8G8B8A8_SNORM = 31,
            DXGI_FORMAT_R8G8B8A8_SINT = 32,
            DXGI_FORMAT_R16G16_TYPELESS = 33,
            DXGI_FORMAT_R16G16_FLOAT = 34,
            DXGI_FORMAT_R16G16_UNORM = 35,
            DXGI_FORMAT_R16G16_UINT = 36,
            DXGI_FORMAT_R16G16_SNORM = 37,
            DXGI_FORMAT_R16G16_SINT = 38,
            DXGI_FORMAT_R32_TYPELESS = 39,
            DXGI_FORMAT_D32_FLOAT = 40,
            DXGI_FORMAT_R32_FLOAT = 41,
            DXGI_FORMAT_R32_UINT = 42,
            DXGI_FORMAT_R32_SINT = 43,
            DXGI_FORMAT_R24G8_TYPELESS = 44,
            DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
            DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
            DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
            DXGI_FORMAT_R8G8_TYPELESS = 48,
            DXGI_FORMAT_R8G8_UNORM = 49,
            DXGI_FORMAT_R8G8_UINT = 50,
            DXGI_FORMAT_R8G8_SNORM = 51,
            DXGI_FORMAT_R8G8_SINT = 52,
            DXGI_FORMAT_R16_TYPELESS = 53,
            DXGI_FORMAT_R16_FLOAT = 54,
            DXGI_FORMAT_D16_UNORM = 55,
            DXGI_FORMAT_R16_UNORM = 56,
            DXGI_FORMAT_R16_UINT = 57,
            DXGI_FORMAT_R16_SNORM = 58,
            DXGI_FORMAT_R16_SINT = 59,
            DXGI_FORMAT_R8_TYPELESS = 60,
            DXGI_FORMAT_R8_UNORM = 61,
            DXGI_FORMAT_R8_UINT = 62,
            DXGI_FORMAT_R8_SNORM = 63,
            DXGI_FORMAT_R8_SINT = 64,
            DXGI_FORMAT_A8_UNORM = 65,
            DXGI_FORMAT_R1_UNORM = 66,
            DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
            DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
            DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
            DXGI_FORMAT_BC1_TYPELESS = 70,
            DXGI_FORMAT_BC1_UNORM = 71,
            DXGI_FORMAT_BC1_UNORM_SRGB = 72,
            DXGI_FORMAT_BC2_TYPELESS = 73,
            DXGI_FORMAT_BC2_UNORM = 74,
            DXGI_FORMAT_BC2_UNORM_SRGB = 75,
            DXGI_FORMAT_BC3_TYPELESS = 76,
            DXGI_FORMAT_BC3_UNORM = 77,
            DXGI_FORMAT_BC3_UNORM_SRGB = 78,
            DXGI_FORMAT_BC4_TYPELESS = 79,
            DXGI_FORMAT_BC4_UNORM = 80,
            DXGI_FORMAT_BC4_SNORM = 81,
            DXGI_FORMAT_BC5_TYPELESS = 82,
            DXGI_FORMAT_BC5_UNORM = 83,
            DXGI_FORMAT_BC5_SNORM = 84,
            DXGI_FORMAT_B5G6R5_UNORM = 85,
            DXGI_FORMAT_B5G5R5A1_UNORM = 86,
            DXGI_FORMAT_B8G8R8A8_UNORM = 87,
            DXGI_FORMAT_B8G8R8X8_UNORM = 88,
            DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
            DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
            DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
            DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
            DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
            DXGI_FORMAT_BC6H_TYPELESS = 94,
            DXGI_FORMAT_BC6H_UF16 = 95,
            DXGI_FORMAT_BC6H_SF16 = 96,
            DXGI_FORMAT_BC7_TYPELESS = 97,
            DXGI_FORMAT_BC7_UNORM = 98,
            DXGI_FORMAT_BC7_UNORM_SRGB = 99,
            DXGI_FORMAT_AYUV = 100,
            DXGI_FORMAT_Y410 = 101,
            DXGI_FORMAT_Y416 = 102,
            DXGI_FORMAT_NV12 = 103,
            DXGI_FORMAT_P010 = 104,
            DXGI_FORMAT_P016 = 105,
            DXGI_FORMAT_420_OPAQUE = 106,
            DXGI_FORMAT_YUY2 = 107,
            DXGI_FORMAT_Y210 = 108,
            DXGI_FORMAT_Y216 = 109,
            DXGI_FORMAT_NV11 = 110,
            DXGI_FORMAT_AI44 = 111,
            DXGI_FORMAT_IA44 = 112,
            DXGI_FORMAT_P8 = 113,
            DXGI_FORMAT_A8P8 = 114,
            DXGI_FORMAT_B4G4R4A4_UNORM = 115,
            DXGI_FORMAT_P208 = 130,
            DXGI_FORMAT_V208 = 131,
            DXGI_FORMAT_V408 = 132,
            DXGI_FORMAT_FORCE_UINT = 0xffffffff,
        }
        #endregion DXGI/DX10

        #region Properties
        /// <summary>
        /// Size of header in bytes. Must be 124.
        /// </summary>
        public int dwSize { get; set; }

        /// <summary>
        /// Option flags.
        /// </summary>
        public int dwFlags { get; set; }

        /// <summary>
        /// String representation of Flags showing names.
        /// </summary>
        public string dwFlagsString
        {
            get
            {
                return ((DDSdwFlags)dwFlags).ToString();
            }
        }

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
        public int dwCaps { get; set; }

        /// <summary>
        /// String version showing flag names.
        /// </summary>
        public string dwCapsString
        {
            get
            {
                // DDS_FlagStrinfiy
                return ((DDSdwCaps)dwCaps).ToString();
            }
        }

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

        /// <summary>
        /// Surface format of DDS.
        /// e.g. DXT1, V8U8, etc
        /// </summary>
        public override ImageEngineFormat Format
        {
            get
            {
                return DetermineDDSSurfaceFormat(this);
            }
        }

        string DDS_FlagStringify(Type enumType)
        {
            string flags = "";

            string[] names = Enum.GetNames(enumType);
            int[] values = (int[])Enum.GetValues(enumType);
            for (int i = 0; i < names.Length; i++)
            {
                if ((dwFlags & values[i]) != 0)
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
            byte[] temp = new byte[HeaderSize];
            stream.Read(temp, 0, temp.Length);

            if (!CheckIdentifier(temp))
                throw new FormatException("Stream is not a recognised DDS image.");

            // Start header
            dwSize = BitConverter.ToInt32(temp, 4);
            dwFlags = BitConverter.ToInt32(temp, 8);
            Height = BitConverter.ToInt32(temp, 12);
            Width = BitConverter.ToInt32(temp, 16);
            dwPitchOrLinearSize = BitConverter.ToInt32(temp, 20);
            dwDepth = BitConverter.ToInt32(temp, 24);
            dwMipMapCount = BitConverter.ToInt32(temp, 28);
            for (int i = 0; i < 11; ++i)
                dwReserved1[i] = BitConverter.ToInt32(temp, 28 + (i * 4));

            // DDS PixelFormat
            ddspf = new DDS_PIXELFORMAT(temp);

            dwCaps = BitConverter.ToInt32(temp, 104);
            dwCaps2 = BitConverter.ToInt32(temp, 108);
            dwCaps3 = BitConverter.ToInt32(temp, 112);
            dwCaps4 = BitConverter.ToInt32(temp, 116);
            dwReserved2 = BitConverter.ToInt32(temp, 120);

            // DX10 Additional header
            if (ddspf.dwFourCC == FourCC.DX10)
                DX10_DXGI_AdditionalHeader = new DDS_DXGI_DX10_Additional(temp);

            return HeaderSize;
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
        /// Determines friendly format from FourCC, with additional DXGI/DX10 format.
        /// </summary>
        /// <param name="fourCC">FourCC of DDS (DXT1-5)</param>
        /// <param name="additionalDX10"></param>
        /// <returns>Friendly format.</returns>
        static ImageEngineFormat ParseFourCC(FourCC fourCC, DXGI_FORMAT additionalDX10 = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
        {
            if (fourCC == FourCC.DX10)
                return ImageEngineFormat.DDS_DX10; // TODO: Need to add these at some point.

            if (Enum.IsDefined(typeof(ImageEngineFormat), fourCC))
                return (ImageEngineFormat)fourCC;
            else
                return ImageEngineFormat.DDS_ARGB;
        }

        /// <summary>
        /// Determines DDS Surface Format given the header.
        /// </summary>
        /// <param name="header">Fully loaded DDS Header.</param>
        /// <returns>Friendly format.</returns>
        public static ImageEngineFormat DetermineDDSSurfaceFormat(DDS_Header header)
        {
            ImageEngineFormat format = ParseFourCC(header.ddspf.dwFourCC, header.DX10_DXGI_AdditionalHeader.dxgiFormat);

            // Since ARGB is the default, need to do further checks to determine uncompressed formats.
            if (format == ImageEngineFormat.DDS_ARGB)
            {
                // KFreon: Apparently all these flags mean it's a V8U8 image...
                if (header.ddspf.dwRGBBitCount == 0x10 &&
                           header.ddspf.dwRBitMask == 0xFF &&
                           header.ddspf.dwGBitMask == 0xFF00 &&
                           header.ddspf.dwBBitMask == 0x00 &&
                           header.ddspf.dwABitMask == 0x00)
                    format = ImageEngineFormat.DDS_V8U8; 

                // KFreon: Test for L8/G8
                else if (header.ddspf.dwABitMask == 0 &&
                        header.ddspf.dwBBitMask == 0 &&
                        header.ddspf.dwGBitMask == 0 &&
                        header.ddspf.dwRBitMask == 255 &&
                        header.ddspf.dwFlags == 131072 &&
                        header.ddspf.dwSize == 32 &&
                        header.ddspf.dwRGBBitCount == 8)
                    format = ImageEngineFormat.DDS_G8_L8;

                // KFreon: A8L8. This can probably be something else as well, but it seems to work for now
                else if (header.ddspf.dwRGBBitCount == 16)
                    format = ImageEngineFormat.DDS_A8L8;

                // KFreon: RGB test.
                else if (header.ddspf.dwRGBBitCount == 24)
                    format = ImageEngineFormat.DDS_RGB;
            }

            return format;
        }

        internal static bool CheckIdentifier(byte[] IDBlock)
        {
            for (int i = 0; i < Identifier.Length; i++)
                if (IDBlock[i] != Identifier[i])
                    return false;

            return true;
        }
    }
}
