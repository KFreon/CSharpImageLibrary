using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using CSharpImageLibrary.DDS;
using CSharpImageLibrary.Headers;
using static CSharpImageLibrary.Headers.RawDDSHeaderStuff;

namespace CSharpImageLibrary
{
    public partial class ImageFormats
    {
        /// <summary>
        /// Length of header in bytes when Additional DX10 Header is present.
        /// </summary>
        public const int DDS_DX10_HEADER_LENGTH = 148;


        /// <summary>
        /// Length of header when pre-DX10 format.
        /// </summary>
        public const int DDS_NO_DX10_HEADER_LENGTH = 128;



        /// <summary>
        /// Detailed representation of an image format.
        /// </summary>
        [DebuggerDisplay("Format: {Format}, ComponentSize: {ComponentSize}")]
        public class ImageEngineFormatDetails
        {
            struct FormatInfo
            {
                public ImageEngineFormat Name;
                public int BlockSize;
                public List<SupportedExtensions> Extensions;
                public bool IsDDS;
                public bool IsBlockCompressed;
                public int BitCount;
                public bool IsPremultiplied;
                public int ComponentSize;
                public int MaxNumChannels;
                public AbstractHeader.HeaderType Type;
            }

            static Dictionary<ImageEngineFormat, FormatInfo> FormatInfos = new Dictionary<ImageEngineFormat, FormatInfo>
            {
                { ImageEngineFormat.Unknown, new FormatInfo { Name = ImageEngineFormat.Unknown, Type = AbstractHeader.HeaderType.UNKNOWN, BlockSize = -1, Extensions = { SupportedExtensions.UNKNOWN }                      , BitCount = -1, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = -1, MaxNumChannels = -1 } },
                { ImageEngineFormat.PNG    , new FormatInfo { Name = ImageEngineFormat.PNG    , Type = AbstractHeader.HeaderType.PNG    , BlockSize = 1 , Extensions = { SupportedExtensions.PNG }                          , BitCount = 32, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  } },
                { ImageEngineFormat.JPG    , new FormatInfo { Name = ImageEngineFormat.JPG    , Type = AbstractHeader.HeaderType.JPG    , BlockSize = 1 , Extensions = { SupportedExtensions.JPG, SupportedExtensions.JPEG }, BitCount = 24, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 3  } },
                { ImageEngineFormat.BMP    , new FormatInfo { Name = ImageEngineFormat.BMP    , Type = AbstractHeader.HeaderType.BMP    , BlockSize = 1 , Extensions = { SupportedExtensions.BMP }                          , BitCount = 32, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  } },
                { ImageEngineFormat.TGA    , new FormatInfo { Name = ImageEngineFormat.TGA    , Type = AbstractHeader.HeaderType.TGA    , BlockSize = 1 , Extensions = { SupportedExtensions.TGA }                          , BitCount = 32, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  } },
                { ImageEngineFormat.GIF    , new FormatInfo { Name = ImageEngineFormat.GIF    , Type = AbstractHeader.HeaderType.GIF    , BlockSize = 1 , Extensions = { SupportedExtensions.GIF }                          , BitCount = 32, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  } },
                { ImageEngineFormat.TIF    , new FormatInfo { Name = ImageEngineFormat.TIF    , Type = AbstractHeader.HeaderType.TIFF   , BlockSize = 1 , Extensions = { SupportedExtensions.TIF }                          , BitCount = 32, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  } },
               
                { ImageEngineFormat.DDS_DXT1    , new FormatInfo { Name = ImageEngineFormat.DDS_DXT1    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { SupportedExtensions.DDS }, BitCount = 32, IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  } },
                { ImageEngineFormat.DDS_DXT2    , new FormatInfo { Name = ImageEngineFormat.DDS_DXT2    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { SupportedExtensions.DDS }, BitCount = 32, IsBlockCompressed = true, IsDDS = true, IsPremultiplied = true  , ComponentSize = 1, MaxNumChannels = 4  } },
                { ImageEngineFormat.DDS_DXT3    , new FormatInfo { Name = ImageEngineFormat.DDS_DXT3    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { SupportedExtensions.DDS }, BitCount = 32, IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  } },
                { ImageEngineFormat.DDS_DXT4    , new FormatInfo { Name = ImageEngineFormat.DDS_DXT4    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { SupportedExtensions.DDS }, BitCount = 32, IsBlockCompressed = true, IsDDS = true, IsPremultiplied = true  , ComponentSize = 1, MaxNumChannels = 4  } },
                { ImageEngineFormat.DDS_DXT5    , new FormatInfo { Name = ImageEngineFormat.DDS_DXT5    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { SupportedExtensions.DDS }, BitCount = 32, IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  } },
                { ImageEngineFormat.DDS_ATI1    , new FormatInfo { Name = ImageEngineFormat.DDS_ATI1    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 8, Extensions = { SupportedExtensions.DDS }, BitCount = 8 , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 1  } },
                { ImageEngineFormat.DDS_ATI2_3Dc, new FormatInfo { Name = ImageEngineFormat.DDS_ATI2_3Dc, Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { SupportedExtensions.DDS }, BitCount = 16, IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 2  } },
                { ImageEngineFormat.DDS_BC6     , new FormatInfo { Name = ImageEngineFormat.DDS_BC6     , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1, Extensions = { SupportedExtensions.DDS }, BitCount = 32, IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  } },
                { ImageEngineFormat.DDS_BC7     , new FormatInfo { Name = ImageEngineFormat.DDS_BC7     , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1, Extensions = { SupportedExtensions.DDS }, BitCount = 32, IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  } },
                                                                      
                { ImageEngineFormat.DDS_RGB_8   , new FormatInfo { Name = ImageEngineFormat.DDS_RGB_8   , Type = AbstractHeader.HeaderType.DDS, BlockSize = 3 , Extensions = { SupportedExtensions.DDS }, BitCount = 24 , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 3  } },
                { ImageEngineFormat.DDS_ARGB_8  , new FormatInfo { Name = ImageEngineFormat.DDS_ARGB_8  , Type = AbstractHeader.HeaderType.DDS, BlockSize = 4 , Extensions = { SupportedExtensions.DDS }, BitCount = 32 , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 4  } },
                { ImageEngineFormat.DDS_ARGB_4  , new FormatInfo { Name = ImageEngineFormat.DDS_ARGB_4  , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2 , Extensions = { SupportedExtensions.DDS }, BitCount = 16 , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 4  } },
                { ImageEngineFormat.DDS_ABGR_8  , new FormatInfo { Name = ImageEngineFormat.DDS_ABGR_8  , Type = AbstractHeader.HeaderType.DDS, BlockSize = 4 , Extensions = { SupportedExtensions.DDS }, BitCount = 32 , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 4  } },
                { ImageEngineFormat.DDS_V8U8    , new FormatInfo { Name = ImageEngineFormat.DDS_V8U8    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2 , Extensions = { SupportedExtensions.DDS }, BitCount = 16 , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 2  } },
                { ImageEngineFormat.DDS_G8_L8   , new FormatInfo { Name = ImageEngineFormat.DDS_G8_L8   , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1 , Extensions = { SupportedExtensions.DDS }, BitCount = 8  , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 1  } },
                { ImageEngineFormat.DDS_A8L8    , new FormatInfo { Name = ImageEngineFormat.DDS_A8L8    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2 , Extensions = { SupportedExtensions.DDS }, BitCount = 16 , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 2  } },
                { ImageEngineFormat.DDS_R5G6B5  , new FormatInfo { Name = ImageEngineFormat.DDS_R5G6B5  , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1 , Extensions = { SupportedExtensions.DDS }, BitCount = 16 , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 3  } },
                { ImageEngineFormat.DDS_A8      , new FormatInfo { Name = ImageEngineFormat.DDS_A8      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1 , Extensions = { SupportedExtensions.DDS }, BitCount = 8  , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 1  } },
                { ImageEngineFormat.DDS_G16_R16 , new FormatInfo { Name = ImageEngineFormat.DDS_G16_R16 , Type = AbstractHeader.HeaderType.DDS, BlockSize = 4 , Extensions = { SupportedExtensions.DDS }, BitCount = 32 , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 2  } },
                { ImageEngineFormat.DDS_ARGB_32F, new FormatInfo { Name = ImageEngineFormat.DDS_ARGB_32F, Type = AbstractHeader.HeaderType.DDS, BlockSize = 16, Extensions = { SupportedExtensions.DDS }, BitCount = 128, IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 4  } },
                
                { ImageEngineFormat.DX10_Placeholder, new FormatInfo { Name = ImageEngineFormat.DX10_Placeholder, Type = AbstractHeader.HeaderType.DDS, BlockSize = -1, Extensions = { SupportedExtensions.DDS }, BitCount = -1, IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 4  } },
                // TODO: Handle any format that gives masks. Use the bit functions that are in BC6 and 7.
            };

            FormatInfo Format;

            /// <summary>
            /// Indicates whether the image is a DX10 image.
            /// </summary>
            public bool IsDX10 => DX10Format != DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;


            /// <summary>
            /// Length of header (DDS only)
            /// </summary>
            public int HeaderSize => IsDX10 ? DDS_DX10_HEADER_LENGTH : DDS_NO_DX10_HEADER_LENGTH;


            /// <summary>
            /// DX10Format when Format is set to DX10.
            /// </summary>
            public DXGI_FORMAT DX10Format { get; private set; }

            /// <summary>
            /// Indicates whether format contains premultiplied alpha.
            /// </summary>
            public bool IsPremultipliedFormat => Format.IsPremultiplied;

            /// <summary>
            /// Number of bytes in colour.
            /// </summary>
            public int ComponentSize => Format.ComponentSize;

            /// <summary>
            /// Number of bits in colour.
            /// </summary>
            public int BitCount => Format.BitCount;

            /// <summary>
            /// Indicates whether supported format is Block Compressed.
            /// </summary>
            public bool IsBlockCompressed => Format.IsBlockCompressed;

            /// <summary>
            /// Size of a discrete block in bytes. (e.g. 2 channel 8 bit colour = 2, DXT1 = 16). Block can mean texel (DXTn) or pixel (uncompressed)
            /// </summary>
            public int BlockSize => Format.BlockSize;

            /// <summary>
            /// String representation of formats' file extension. No '.'.
            /// </summary>
            public SupportedExtensions Extension => Format.Extensions.First();

            /// <summary>
            /// All Supported extensions for the format (jpg, jpeg, ...)
            /// </summary>
            public List<SupportedExtensions> AllExtensions => Format.Extensions;

            /// <summary>
            /// Indicates whether format is a DDS format.
            /// </summary>
            public bool IsDDS => Format.IsDDS;

            /// <summary>
            /// Max number of supported channels. Usually 4, but some formats are 1 (G8), 2 (V8U8), or 3 (RGB) channels.
            /// </summary>
            public int MaxNumberOfChannels => Format.MaxNumChannels;

            /// <summary>
            /// Writes the max value to array using the correct bit styles.
            /// e.g. Will write int.Max when component size is int.Length (4 bytes).
            /// </summary>
            public Action<byte[], int> SetMaxValue = null;

            /// <summary>
            /// Reads a byte from a source array using the correct bit styles.
            /// </summary>
            public Func<byte[], int, byte> ReadByte = null;

            /// <summary>
            /// Reads a ushort (int16) from a source array using the correct bit styles.
            /// </summary>
            public Func<byte[], int, ushort> ReadUShort = null;

            /// <summary>
            /// Reads a float from a source array using the correct bit styles.
            /// </summary>
            public Func<byte[], int, float> ReadFloat = null;

            Func<byte[], int, byte[]> ReadUShortAsArray = null;
            Func<byte[], int, byte[]> ReadFloatAsArray = null;

            /// <summary>
            /// Holds the encoder to be used when compressing/writing image.
            /// </summary>
            public Action<byte[], int, int, byte[], int, AlphaSettings, ImageEngineFormatDetails> BlockEncoder = null;

            /// <summary>
            /// Holds the decoder to be used when decompressing/reading image.
            /// </summary>
            public Action<byte[], int, byte[], int, int, bool> BlockDecoder = null;

            /// <summary>
            /// Writes a colour from source to destination performing correct bit style conversions if requried.
            /// </summary>
            public Action<byte[], int, ImageEngineFormatDetails, byte[], int> WriteColour = null;

            /// <summary>
            /// Details the given format.
            /// </summary>
            /// <param name="header">Header to get format details from.</param>
            public ImageEngineFormatDetails(AbstractHeader header)
            {
                Format = FormatInfos.First(n => n.Value.Type == header.Type).Value;

                // Handle DDS cases
                if(Format.Type == AbstractHeader.HeaderType.DDS)
                {
                    var ddsHeader = (DDS_Header)header;

                    Format = DetermineDDSSurfaceFormat(ddsHeader);

                    // Set read/write functions
                    

                }
            }

            void SetupFunctions()
            {
                ReadByte = ReadByteFromByte;
                ReadUShort = ReadUShortFromByte;
                ReadFloat = ReadFloatFromByte;
                SetMaxValue = WriteByteMax;
                WriteColour = WriteByte;
                ReadUShortAsArray = ReadUShortFromByteAsArray;
                ReadFloatAsArray = ReadFloatFromByteOrUShortAsArray;

                if (ComponentSize == 2)
                {
                    ReadByte = ReadByteFromUShort;
                    ReadUShort = ReadUShortFromUShort;
                    ReadFloat = ReadFloatFromUShort;
                    SetMaxValue = WriteUShortMax;
                    WriteColour = WriteUShort;
                    ReadUShortAsArray = ReadUShortFromUShortAsArray;
                    // Don't need ReadFloatAsArray set here, as it's shared between byte and ushort reading.
                }
                else if (ComponentSize == 4)
                {
                    ReadByte = ReadByteFromFloat;
                    ReadUShort = ReadUShortFromFloat;
                    ReadFloat = ReadFloatFromFloat;
                    SetMaxValue = WriteFloatMax;
                    WriteColour = WriteFloat;
                    ReadUShortAsArray = ReadUShortFromFloatAsArray;
                    ReadFloatAsArray = ReadFloatFromFloatAsArray;
                }

                //TODO: Why so many functions...

                switch (Format.Name)
                {
                    case ImageEngineFormat.DDS_DXT1:
                        BlockEncoder = DDS_Encoders.CompressBC1Block;
                        BlockDecoder = DDS_Decoders.DecompressBC1Block;
                        break;
                    case ImageEngineFormat.DDS_DXT2:
                    case ImageEngineFormat.DDS_DXT3:
                        BlockEncoder = DDS_Encoders.CompressBC2Block;
                        BlockDecoder = DDS_Decoders.DecompressBC2Block;
                        break;
                    case ImageEngineFormat.DDS_DXT4:
                    case ImageEngineFormat.DDS_DXT5:
                        BlockEncoder = DDS_Encoders.CompressBC3Block;
                        BlockDecoder = DDS_Decoders.DecompressBC3Block;
                        break;
                    case ImageEngineFormat.DDS_ATI1:
                        BlockEncoder = DDS_Encoders.CompressBC4Block;
                        BlockDecoder = DDS_Decoders.DecompressBC4Block;
                        break;
                    case ImageEngineFormat.DDS_ATI2_3Dc:
                        BlockEncoder = DDS_Encoders.CompressBC5Block;
                        BlockDecoder = DDS_Decoders.DecompressBC5Block;
                        break;
                    case ImageEngineFormat.DDS_BC6:
                        BlockEncoder = DDS_Encoders.CompressBC6Block;
                        BlockDecoder = DDS_Decoders.DecompressBC6Block;
                        break;
                    case ImageEngineFormat.DDS_BC7:
                        BlockEncoder = DDS_Encoders.CompressBC7Block;
                        BlockDecoder = DDS_Decoders.DecompressBC7Block;
                        break;
                }
            }

            FormatInfo DetermineDDSSurfaceFormat(DDS_Header header)
            {
                DDS_PIXELFORMAT ddspf = header.ddspf;

                // Casting as the ImageEngineFormat is just a more readable version of FourCC for the most part. 
                bool validFourCC = FormatInfos.TryGetValue((ImageEngineFormat)ddspf.dwFourCC, out FormatInfo info);

                if (!validFourCC)   // FourCC not provided, need to guess.  TODO: Could guess FourCC'd formats too, in the case the FourCC is just not supplied...
                {
                    // KFreon: Apparently all these flags mean it's a V8U8 image...
                    if (ddspf.dwRGBBitCount == 16 &&
                               ddspf.dwRBitMask == 0x00FF &&
                               ddspf.dwGBitMask == 0xFF00 &&
                               ddspf.dwBBitMask == 0x00 &&
                               ddspf.dwABitMask == 0x00 &&
                               (ddspf.dwFlags & DDS_PFdwFlags.DDPF_SIGNED) == DDS_PFdwFlags.DDPF_SIGNED)
                        info = FormatInfos[ImageEngineFormat.DDS_V8U8];

                    // KFreon: Test for L8/G8
                    else if (ddspf.dwABitMask == 0 &&
                            ddspf.dwBBitMask == 0 &&
                            ddspf.dwGBitMask == 0 &&
                            ddspf.dwRBitMask == 0xFF &&
                            ddspf.dwFlags == DDS_PFdwFlags.DDPF_LUMINANCE &&
                            ddspf.dwRGBBitCount == 8)
                        info = FormatInfos[ImageEngineFormat.DDS_G8_L8];


                    // KFreon: A8L8. This can probably be something else as well, but it seems to work for now
                    else if (ddspf.dwRGBBitCount == 16 &&
                            ddspf.dwFlags == (DDS_PFdwFlags.DDPF_ALPHAPIXELS | DDS_PFdwFlags.DDPF_LUMINANCE))
                        info = FormatInfos[ImageEngineFormat.DDS_A8L8];


                    // KFreon: G_R only.
                    else if (((ddspf.dwFlags & DDS_PFdwFlags.DDPF_RGB) == DDS_PFdwFlags.DDPF_RGB && !((ddspf.dwFlags & DDS_PFdwFlags.DDPF_ALPHAPIXELS) == DDS_PFdwFlags.DDPF_ALPHAPIXELS)) &&
                            ddspf.dwABitMask == 0 &&
                            ddspf.dwBBitMask == 0 &&
                            ddspf.dwGBitMask != 0 &&
                            ddspf.dwRBitMask != 0)
                        info = FormatInfos[ImageEngineFormat.DDS_G16_R16];


                    // KFreon: RGB. RGB channels have something in them, but alpha doesn't.
                    else if (((ddspf.dwFlags & DDS_PFdwFlags.DDPF_RGB) == DDS_PFdwFlags.DDPF_RGB && !((ddspf.dwFlags & DDS_PFdwFlags.DDPF_ALPHAPIXELS) == DDS_PFdwFlags.DDPF_ALPHAPIXELS)) &&
                            ddspf.dwABitMask == 0 &&
                            ddspf.dwBBitMask != 0 &&
                            ddspf.dwGBitMask != 0 &&
                            ddspf.dwRBitMask != 0)
                    {
                        // TODO more formats?
                        if (ddspf.dwBBitMask == 31)
                            info = FormatInfos[ImageEngineFormat.DDS_R5G6B5];
                        else
                            info = FormatInfos[ImageEngineFormat.DDS_RGB_8];
                    }

                    // KFreon: RGB and A channels are present.
                    else if (((ddspf.dwFlags & (DDS_PFdwFlags.DDPF_RGB | DDS_PFdwFlags.DDPF_ALPHAPIXELS)) == (DDS_PFdwFlags.DDPF_RGB | DDS_PFdwFlags.DDPF_ALPHAPIXELS)) ||
                            ddspf.dwABitMask != 0 &&
                            ddspf.dwBBitMask != 0 &&
                            ddspf.dwGBitMask != 0 &&
                            ddspf.dwRBitMask != 0)
                    {
                        // TODO: Some more formats here?
                        info = FormatInfos[ImageEngineFormat.DDS_ARGB_8];   
                    }

                    // KFreon: If nothing else fits, but there's data in one of the bitmasks, assume it can be read.
                    else if (ddspf.dwABitMask != 0 || ddspf.dwRBitMask != 0 || ddspf.dwGBitMask != 0 || ddspf.dwBBitMask != 0)
                        info = FormatInfos[0];  // Unknown
                    else
                        throw new FormatException("DDS Format is unknown.");
                }

                // Handle DX10
                if (info.Name == ImageEngineFormat.DX10_Placeholder) 
                {
                    DX10Format = header.DX10_DXGI_AdditionalHeader.dxgiFormat;

                    switch (DX10Format)
                    {
                        case DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS:
                        case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                        case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                            info = FormatInfos[ImageEngineFormat.DDS_DXT1];
                            break;
                        case DXGI_FORMAT.DXGI_FORMAT_BC2_TYPELESS:
                        case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                        case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
                            info = FormatInfos[ImageEngineFormat.DDS_DXT3];
                            break;
                        case DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS:
                        case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                        case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                            info = FormatInfos[ImageEngineFormat.DDS_DXT5];
                            break;
                        case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                        case DXGI_FORMAT.DXGI_FORMAT_BC4_TYPELESS:
                        case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                            info = FormatInfos[ImageEngineFormat.DDS_ATI1];
                            break;
                        case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                        case DXGI_FORMAT.DXGI_FORMAT_BC5_TYPELESS:
                        case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                            info = FormatInfos[ImageEngineFormat.DDS_ATI2_3Dc];
                            break;
                        case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:  // TODO: Supporting other BC6 formats?
                            info = FormatInfos[ImageEngineFormat.DDS_BC6];
                            break;
                        case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:  // TODO: Supporting other BC7 formats?
                            info = FormatInfos[ImageEngineFormat.DDS_BC7];
                            break;
                    }
                }
                return info;
            }


            #region Bit Conversions
            byte ReadByteFromByte(byte[] source, int sourceStart)
            {
                return source[sourceStart];
            }

            byte ReadByteFromUShort(byte[] source, int sourceStart)
            {
                return (byte)(((source[sourceStart + 1] << 8) | source[sourceStart]) * (255d / ushort.MaxValue));
            }

            byte ReadByteFromFloat(byte[] source, int sourceStart)
            {
                return (byte)(BitConverter.ToSingle(source, sourceStart) * 255f);
            }

            /************************************/

            ushort ReadUShortFromByte(byte[] source, int sourceStart)
            {
                return source[sourceStart];
            }

            ushort ReadUShortFromUShort(byte[] source, int sourceStart)
            {
                return BitConverter.ToUInt16(source, sourceStart);
            }

            ushort ReadUShortFromFloat(byte[] source, int sourceStart)
            {
                return (ushort)(BitConverter.ToSingle(source, sourceStart) * ushort.MaxValue);
            }

            /************************************/

            float ReadFloatFromByte(byte[] source, int sourceStart)
            {
                return source[sourceStart] / 255f;
            }

            float ReadFloatFromUShort(byte[] source, int sourceStart)
            {
                return BitConverter.ToUInt16(source, sourceStart) / (ushort.MaxValue * 1f);
            }

            float ReadFloatFromFloat(byte[] source, int sourceStart)
            {
                return BitConverter.ToSingle(source, sourceStart);
            }
            #endregion Bit Conversions

            #region Max Value Writers
            void WriteByteMax(byte[] source, int sourceStart)
            {
                source[sourceStart] = 255;
            }

            void WriteUShortMax(byte[] source, int sourceStart)
            {
                source[sourceStart] = 255;
                source[sourceStart + 1] = 255;
            }

            void WriteFloatMax(byte[] source, int sourceStart)
            {
                source[sourceStart] = 0;
                source[sourceStart + 1] = 0;
                source[sourceStart + 2] = 63;
                source[sourceStart + 3] = 127;
            }
            #endregion Max Value Writers

            #region Readers to arrays
            byte[] ReadUShortFromByteAsArray(byte[] source, int sourceStart)
            {
                return new byte[] { 0, source[sourceStart] };
            }

            byte[] ReadUShortFromUShortAsArray(byte[] source, int sourceStart)
            {
                return new byte[] { source[sourceStart], source[sourceStart + 1] };
            }

            byte[] ReadUShortFromFloatAsArray(byte[] source, int sourceStart)
            {
                return BitConverter.GetBytes(ReadUShortFromFloat(source, sourceStart));
            }

            /**/

            byte[] ReadFloatFromByteOrUShortAsArray(byte[] source, int sourceStart)
            {
                return BitConverter.GetBytes(ReadFloat(source, sourceStart));
            }
            byte[] ReadFloatFromFloatAsArray(byte[] source, int sourceStart)
            {
                return new byte[] { source[sourceStart], source[sourceStart + 1], source[sourceStart + 2], source[sourceStart + 3] };
            }
            #endregion Readers to arrays

            #region Writers
            void WriteByte(byte[] source, int sourceStart, ImageEngineFormatDetails sourceFormatDetails, byte[] destination, int destStart)
            {
                destination[destStart] = sourceFormatDetails.ReadByte(source, sourceStart);
            }

            void WriteUShort(byte[] source, int sourceStart, ImageEngineFormatDetails sourceFormatDetails, byte[] destination, int destStart)
            {
                byte[] bytes = sourceFormatDetails.ReadUShortAsArray(source, sourceStart);
                destination[destStart] = bytes[0];
                destination[destStart + 1] = bytes[1];
            }

            void WriteFloat(byte[] source, int sourceStart, ImageEngineFormatDetails sourceFormatDetails, byte[] destination, int destStart)
            {
                byte[] bytes = sourceFormatDetails.ReadFloatAsArray(source, sourceStart);
                destination[destStart] = bytes[0];
                destination[destStart + 1] = bytes[1];
                destination[destStart + 2] = bytes[2];
                destination[destStart + 3] = bytes[3];
            }
            #endregion Writers
        }
    }
}
