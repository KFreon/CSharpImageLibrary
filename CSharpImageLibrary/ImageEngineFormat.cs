using System.ComponentModel;
using static CSharpImageLibrary.Headers.RawDDSHeaderStuff;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Indicates image format.
    /// Use FORMAT struct.
    /// </summary>
    public enum ImageEngineFormat
    {
        /// <summary>
        /// Unknown image format. Using this as a save/load format will fail that operation.
        /// </summary>
        [Description("Unknown image format. Using this as a save/load format will fail that operation.")]
        Unknown = FourCC.Unknown,

        /// <summary>
        /// Internal use only.
        /// </summary>
        [Description("DX10 placeholder. Will NOT have any effect.")]
        DX10_Placeholder = FourCC.DX10,

        /// <summary>
        /// Standard JPEG image handled by everything.
        /// </summary>
        [Description("Standard JPEG image handled by everything.")]
        JPG = 2,

        /// <summary>
        /// Standard PNG image handled by everything. Uses alpha channel if available.
        /// </summary>
        [Description("Standard PNG image handled by everything. Uses alpha channel if available.")]
        PNG = 3,

        /// <summary>
        /// Standard BMP image handled by everything.
        /// </summary>
        [Description("Standard BMP image handled by everything.")]
        BMP = 4,

        /// <summary>
        /// Targa image. Multipage format. Can be used for mipmaps.
        /// </summary>
        [Description("Targa image. Multipage format. Can be used for mipmaps.")]
        TGA = 5,

        /// <summary>
        /// Standard GIF Image handled by everything. 
        /// </summary>
        [Description("Standard GIF Image handled by everything. ")]
        GIF = 6,

        /// <summary>
        /// (BC1) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Simple Non Alpha.
        /// </summary>
        [Description("(BC1) Block Compressed Texture. Compresses 4x4 texels. Used for Simple Non Alpha.")]
        DDS_DXT1 = FourCC.DXT1,

        /// <summary>
        /// (BC2) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Sharp Alpha. Premultiplied alpha. 
        /// </summary>
        [Description("(BC2) Block Compressed Texture. Compresses 4x4 texels. Used for Sharp Alpha. Premultiplied alpha. ")]
        DDS_DXT2 = FourCC.DXT2,

        /// <summary>
        /// (BC2) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Sharp Alpha. 
        /// </summary>
        [Description("(BC2) Block Compressed Texture. Compresses 4x4 texels. Used for Sharp Alpha. ")]
        DDS_DXT3 = FourCC.DXT3,

        /// <summary>
        /// (BC3) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Gradient Alpha. Premultiplied alpha.
        /// </summary>
        [Description("(BC3) Block Compressed Texture. Compresses 4x4 texels. Used for Gradient Alpha. Premultiplied alpha.")]
        DDS_DXT4 = FourCC.DXT4,

        /// <summary>
        /// (BC3) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Gradient Alpha. 
        /// </summary>
        [Description("(BC3) Block Compressed Texture. Compresses 4x4 texels. Used for Gradient Alpha. ")]
        DDS_DXT5 = FourCC.DXT5,

        /// <summary>
        /// Half float HDR block compressed Format. Very slow to compress.
        /// </summary>
        DDS_BC6 = FourCC.BC6,

        /// <summary>
        /// Floating point Block compressed format. Very slow to compress.
        /// </summary>
        DDS_BC7 = FourCC.BC7,

        /// <summary>
        /// Uncompressed ARGB DDS.
        /// </summary>
        [Description("Uncompressed ARGB DDS.")]
        DDS_ABGR_8 = FourCC.A8B8G8R8,

        /// <summary>
        /// (BC4) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Normal (bump) Maps. 8 bit single channel with alpha.
        /// </summary>
        [Description("(BC4) Block Compressed Texture. Compresses 4x4 texels. Used for Normal (bump) Maps. 8 bit single channel with optional 1bit alpha.")]
        DDS_ATI1 = FourCC.ATI1,

        /// <summary>
        /// Uncompressed pair of 8 bit channels.
        /// Used for Normal (bump) maps.
        /// </summary>
        [Description("Uncompressed pair of 8 bit channels. Used for Normal (bump) maps.")]
        DDS_V8U8 = FourCC.V8U8,

        /// <summary>
        /// Single 8 bit channel.
        /// Used for Luminescence.
        /// </summary>
        [Description("Single 8 bit channel. Used for Luminescence.")]
        DDS_G8_L8 = FourCC.L8,

        /// <summary>
        /// Alpha and single channel luminescence.
        /// Uncompressed.
        /// </summary>
        [Description("Alpha and single channel luminescence. Uncompressed.")]
        DDS_A8L8 = FourCC.A8L8,

        /// <summary>
        /// RGB. No alpha. 
        /// Uncompressed.
        /// </summary>
        [Description("RGB. No alpha. Uncompressed.")]
        DDS_RGB_8 = FourCC.R8G8B8,

        /// <summary>
        /// (BC5) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Normal (bump) Maps. Pair of 8 bit channels.
        /// </summary>
        [Description("(BC5) Block Compressed Texture. Compresses 4x4 texels. Used for Normal (bump) Maps. Pair of 8 bit channels.")]
        DDS_ATI2_3Dc = FourCC.ATI2N_3Dc,

        DDS_ARGB_8 = FourCC.A8R8G8B8,
        DDS_R5G6B5 = FourCC.R5G6B5,
        DDS_ARGB_4 = FourCC.A4R4G4B4,
        DDS_A8 = FourCC.A8,
        DDS_G16_R16 = FourCC.G16R16,
        DDS_ARGB_32F = FourCC.A32B32G32R32F,


        /// <summary>
        /// Format designed for scanners. Compressed.
        /// Allows mipmaps.
        /// </summary>
        [Description("Format designed for scanners. Compressed. Allows mipmaps.")]
        TIF = 240,
    }
}
