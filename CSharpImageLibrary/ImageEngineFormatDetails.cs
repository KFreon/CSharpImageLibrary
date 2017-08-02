using CSharpImageLibrary.DDS;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UsefulThings;
using System.ComponentModel;
using System.Collections.Generic;
using CSharpImageLibrary.Headers;
using static CSharpImageLibrary.Headers.DDS_Header;

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
                public string Name;
                public int BlockSize;
                public List<string> Extensions;
                public FourCC FourCC;
                public bool IsDDS;
                public bool IsBlockCompressed;
                public int BitCount;
                public bool IsPremultiplied;
                public int ComponentSize;
                public int MaxNumChannels;
                public AbstractHeader.HeaderType Type;
            }

            static List<FormatInfo> FormatInfos = new List<FormatInfo>
            {
                new FormatInfo { Name = "UNKNOWN"                    , Type = AbstractHeader.HeaderType.UNKNOWN, BlockSize = -1, Extensions = { "UNKNOWN" }       , BitCount = -1 , FourCC = FourCC.Unknown, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = -1, MaxNumChannels = -1 },
                new FormatInfo { Name = "Portable Network Graphic"   , Type = AbstractHeader.HeaderType.PNG, BlockSize = 1 , Extensions = { "png" }               , BitCount = 1  , FourCC = FourCC.Unknown, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  },
                new FormatInfo { Name = "JPEG"                       , Type = AbstractHeader.HeaderType.JPG, BlockSize = 1 , Extensions = { "jpg", "jpeg", "jp2" }, BitCount = 1  , FourCC = FourCC.Unknown, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 3  },
                new FormatInfo { Name = "Bitmap"                     , Type = AbstractHeader.HeaderType.BMP, BlockSize = 1 , Extensions = { "bmp" }               , BitCount = 1  , FourCC = FourCC.Unknown, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  },
                new FormatInfo { Name = "Targa"                      , Type = AbstractHeader.HeaderType.TGA, BlockSize = 1 , Extensions = { "tga" }               , BitCount = 1  , FourCC = FourCC.Unknown, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  },
                new FormatInfo { Name = "Graphics Interchange Format", Type = AbstractHeader.HeaderType.GIF, BlockSize = 1 , Extensions = { "gif" }               , BitCount = 1  , FourCC = FourCC.Unknown, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  },
                new FormatInfo { Name = "TIFF"                       , Type = AbstractHeader.HeaderType.TIFF, BlockSize = 1 , Extensions = { "tif", "tiff" }      , BitCount = 1  , FourCC = FourCC.Unknown, IsBlockCompressed = false, IsDDS = false, IsPremultiplied = false, ComponentSize = 1 , MaxNumChannels = 4  },

                new FormatInfo { Name = "DXT1"      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.DXT1       , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  },
                new FormatInfo { Name = "DXT2"      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.DXT2       , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = true  , ComponentSize = 1, MaxNumChannels = 4  },
                new FormatInfo { Name = "DXT3"      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.DXT3       , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  },
                new FormatInfo { Name = "DXT4"      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.DXT4       , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = true  , ComponentSize = 1, MaxNumChannels = 4  },
                new FormatInfo { Name = "DXT5"      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.DXT5       , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  },
                new FormatInfo { Name = "ATI1"      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 8, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.ATI1       , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 1  },
                new FormatInfo { Name = "ATI2_3Dc"  , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.ATI2N_3Dc  , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 2  },
                new FormatInfo { Name = "BC6"       , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.DX10       , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  },
                new FormatInfo { Name = "BC7"       , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.DX10       , IsBlockCompressed = true, IsDDS = true, IsPremultiplied = false , ComponentSize = 1, MaxNumChannels = 4  },

                new FormatInfo { Name = "RGB_8"     , Type = AbstractHeader.HeaderType.DDS, BlockSize = 3 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.R8G8B8       , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 3  },
                new FormatInfo { Name = "ARGB_8"    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 4 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.A8R8G8B8     , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 4  },
                new FormatInfo { Name = "ARGB_4"    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.A4R4G4B4     , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 4  },
                new FormatInfo { Name = "AbGR_8"    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 4 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.A8B8G8R8     , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 4  },
                new FormatInfo { Name = "V8U8"      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.V8U8         , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 2  },
                new FormatInfo { Name = "G8L8"      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.L8           , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 1  },
                new FormatInfo { Name = "A8L8"      , Type = AbstractHeader.HeaderType.DDS, BlockSize = 2 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.A8L8         , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 2  },
                new FormatInfo { Name = "R5G6B5"    , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.R5G6B5       , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 3  },
                new FormatInfo { Name = "A8"        , Type = AbstractHeader.HeaderType.DDS, BlockSize = 1 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.A8           , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 1  },
                new FormatInfo { Name = "G16_R16"   , Type = AbstractHeader.HeaderType.DDS, BlockSize = 4 , Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.G16R16       , IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 2  },
                new FormatInfo { Name = "ARGB_32F"  , Type = AbstractHeader.HeaderType.DDS, BlockSize = 16, Extensions = { "dds" }, BitCount = 1, FourCC = FourCC.A32B32G32R32F, IsBlockCompressed = false, IsDDS = true, IsPremultiplied = false, ComponentSize = 1, MaxNumChannels = 4  },
                // TODO: Handle any format that gives masks. Use the bit functions that are in BC6 and 7.
            };

            FormatInfo info;

            /// <summary>
            /// Indicates whether the image is a DX10 image.
            /// </summary>
            public bool IsDX10 { get; }


            /// <summary>
            /// Length of header (DDS only)
            /// </summary>
            public int HeaderSize => IsDX10 ? DDS_DX10_HEADER_LENGTH : DDS_NO_DX10_HEADER_LENGTH;


            /// <summary>
            /// DX10Format when Format is set to DX10.
            /// </summary>
            public Headers.DDS_Header.DXGI_FORMAT DX10Format { get; }

            /// <summary>
            /// Indicates whether format contains premultiplied alpha.
            /// </summary>
            public bool IsPremultipliedFormat => info.IsPremultiplied;

            /// <summary>
            /// Number of bytes in colour.
            /// </summary>
            public int ComponentSize => info.ComponentSize;

            /// <summary>
            /// Number of bits in colour.
            /// </summary>
            public int BitCount => info.BitCount;


            bool? isBlockCompressed = null;
            /// <summary>
            /// Indicates whether supported format is Block Compressed.
            /// </summary>
            public bool IsBlockCompressed => info.IsBlockCompressed;

            int blockSize = -1;
            /// <summary>
            /// Size of a discrete block in bytes. (e.g. 2 channel 8 bit colour = 2, DXT1 = 16). Block can mean texel (DXTn) or pixel (uncompressed)
            /// </summary>
            public int BlockSize => info.BlockSize;

            /// <summary>
            /// String representation of formats' file extension. No '.'.
            /// </summary>
            public string Extension => info.Extensions.First();

            /// <summary>
            /// All Supported extensions for the format (jpg, jpeg, ...)
            /// </summary>
            public List<string> AllExtensions => info.Extensions;

            /// <summary>
            /// Indicates whether format is a DDS format.
            /// </summary>
            public bool IsDDS => info.IsDDS;


            int numChannels = -1;
            /// <summary>
            /// Max number of supported channels. Usually 4, but some formats are 1 (G8), 2 (V8U8), or 3 (RGB) channels.
            /// </summary>
            public int MaxNumberOfChannels => info.MaxNumChannels;

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
                info = FormatInfos.First(n => n.Type == header.Type);

                // Handle DDS cases
                if(info.Type == AbstractHeader.HeaderType.DDS)
                {
                    var ddsHeader = (DDS_Header)header;

                    info = DetermineDDSSurfaceFormat(ddsHeader.ddspf);
                    
                    // Handle DX10
                    if(info.isdxt10)
                }
            }

            FormatInfo DetermineDDSSurfaceFormat(DDS_Header.DDS_PIXELFORMAT ddspf)
            {
                var info = FormatInfos.FirstOrDefault(f => f.FourCC == ddspf.dwFourCC);

                // Struct, so can't be null, hence check other properties.
                if (string.IsNullOrEmpty(info.Name))
                {
                    // KFreon: Apparently all these flags mean it's a V8U8 image...
                    if (ddspf.dwRGBBitCount == 16 &&
                               ddspf.dwRBitMask == 0x00FF &&
                               ddspf.dwGBitMask == 0xFF00 &&
                               ddspf.dwBBitMask == 0x00 &&
                               ddspf.dwABitMask == 0x00 &&
                               (ddspf.dwFlags & DDS_PFdwFlags.DDPF_SIGNED) == DDS_PFdwFlags.DDPF_SIGNED)
                        info = FormatInfos.First(n => n.FourCC == FourCC.V8U8);

                    // KFreon: Test for L8/G8
                    else if (ddspf.dwABitMask == 0 &&
                            ddspf.dwBBitMask == 0 &&
                            ddspf.dwGBitMask == 0 &&
                            ddspf.dwRBitMask == 0xFF &&
                            ddspf.dwFlags == DDS_PFdwFlags.DDPF_LUMINANCE &&
                            ddspf.dwRGBBitCount == 8)
                        info = FormatInfos.First(n => n.FourCC == FourCC.L8);


                    // KFreon: A8L8. This can probably be something else as well, but it seems to work for now
                    else if (ddspf.dwRGBBitCount == 16 &&
                            ddspf.dwFlags == (DDS_PFdwFlags.DDPF_ALPHAPIXELS | DDS_PFdwFlags.DDPF_LUMINANCE))
                        info = FormatInfos.First(n => n.FourCC == FourCC.A8L8);


                    // KFreon: G_R only.
                    else if (((ddspf.dwFlags & DDS_PFdwFlags.DDPF_RGB) == DDS_PFdwFlags.DDPF_RGB && !((ddspf.dwFlags & DDS_PFdwFlags.DDPF_ALPHAPIXELS) == DDS_PFdwFlags.DDPF_ALPHAPIXELS)) &&
                            ddspf.dwABitMask == 0 &&
                            ddspf.dwBBitMask == 0 &&
                            ddspf.dwGBitMask != 0 &&
                            ddspf.dwRBitMask != 0)
                        info = FormatInfos.First(n => n.FourCC == FourCC.G16R16);


                    // KFreon: RGB. RGB channels have something in them, but alpha doesn't.
                    else if (((ddspf.dwFlags & DDS_PFdwFlags.DDPF_RGB) == DDS_PFdwFlags.DDPF_RGB && !((ddspf.dwFlags & DDS_PFdwFlags.DDPF_ALPHAPIXELS) == DDS_PFdwFlags.DDPF_ALPHAPIXELS)) &&
                            ddspf.dwABitMask == 0 &&
                            ddspf.dwBBitMask != 0 &&
                            ddspf.dwGBitMask != 0 &&
                            ddspf.dwRBitMask != 0)
                    {
                        // TODO more formats?
                        if (ddspf.dwBBitMask == 31)
                            info = FormatInfos.First(n => n.FourCC == FourCC.R5G6B5);

                        else
                            info = FormatInfos.First(n => n.FourCC == FourCC.R8G8B8);
                    }

                    // KFreon: RGB and A channels are present.
                    else if (((ddspf.dwFlags & (DDS_PFdwFlags.DDPF_RGB | DDS_PFdwFlags.DDPF_ALPHAPIXELS)) == (DDS_PFdwFlags.DDPF_RGB | DDS_PFdwFlags.DDPF_ALPHAPIXELS)) ||
                            ddspf.dwABitMask != 0 &&
                            ddspf.dwBBitMask != 0 &&
                            ddspf.dwGBitMask != 0 &&
                            ddspf.dwRBitMask != 0)
                    {
                        // TODO: Some more formats here?
                        info = FormatInfos.First(n => n.FourCC == FourCC.A8R8G8B8);

                    }

                    // KFreon: If nothing else fits, but there's data in one of the bitmasks, assume it can be read.
                    else if (ddspf.dwABitMask != 0 || ddspf.dwRBitMask != 0 || ddspf.dwGBitMask != 0 || ddspf.dwBBitMask != 0)
                        info = FormatInfos[0];  // Unknown
                    else
                        throw new FormatException("DDS Format is unknown.");

                }
                return info;
            }

            int GetDX10BitCount(Headers.DDS_Header.DXGI_FORMAT DX10Format)
            {
                int dx10Format = 32;
                switch (DX10Format)
                {
                    // For now, 32 works.
                }

                return dx10Format;
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
