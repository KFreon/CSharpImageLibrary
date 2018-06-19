using System.ComponentModel;

namespace CSharpImageLibraryCore
{
    /// <summary>
    /// File extensions supported. Used to get initial format.
    /// </summary>
    public enum SupportedExtensions
    {
        /// <summary>
        /// Format isn't known...
        /// </summary>
        [Description("Unknown format")]
        UNKNOWN,

        /// <summary>
        /// JPEG format. Good for small images, but is lossy, hence can have poor colours and artifacts at high compressions.
        /// </summary>
        [Description("Joint Photographic Images")]
        JPG,

        /// <summary>
        /// JPEG format. Good for small images, but is lossy, hence can have poor colours and artifacts at high compressions.
        /// </summary>
        [Description("Joint Photographic Images")]
        JPEG,

        /// <summary>
        /// BMP bitmap. Lossless but exceedingly poor bytes for pixel ratio i.e. huge filesize for little image.
        /// </summary>
        [Description("Bitmap Images")]
        BMP,

        /// <summary>
        /// Supports transparency, decent compression. Use this unless you can't.
        /// </summary>
        [Description("Portable Network Graphic Images")]
        PNG,

        /// <summary>
        /// DirectDrawSurface image. DirectX image, supports mipmapping, fairly poor compression/artifacting. Good for video memory due to mipmapping.
        /// </summary>
        [Description("DirectX Images")]
        DDS,

        /// <summary>
        /// Targa image.
        /// </summary>
        [Description("Targa Images")]
        TGA,

        /// <summary>
        /// Graphics Interchange Format images. Lossy compression, supports animation (this tool doesn't though), good for low numbers of colours.
        /// </summary>
        [Description("Graphics Interchange Images")]
        GIF,

        /// <summary>
        /// TIFF images. Compressed, and supports mipmaps.
        /// </summary>
        [Description("TIFF Images")]
        TIF,

        /// <summary>
        /// TIFF images. Compressed, and supports mipmaps.
        /// </summary>
        [Description("TIFF Images")]
        TIFF,
    }
}
