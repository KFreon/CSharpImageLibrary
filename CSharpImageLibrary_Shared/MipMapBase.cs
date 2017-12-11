using static CSharpImageLibrary.ImageFormats;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Represents a mipmap of an image.
    /// </summary>
    public abstract class MipMapBase
    {
        public MipMapBase()
        {

        }

        public void Initialise(byte[] pixels, int width, int height, ImageEngineFormatDetails details)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
            LoadedFormatDetails = details;

            UncompressedSize = details.GetUncompressedSize(width, height, false);
        }


        /// <summary>
        /// Pixels in bitmap image.
        /// </summary>
        public byte[] Pixels
        {
            get; set;
        }

        /// <summary>
        /// Size of mipmap in memory.
        /// </summary>
        public int UncompressedSize { get; private set; }

        /// <summary>
        /// Details of the format that this mipmap was created from.
        /// </summary>
        public ImageFormats.ImageEngineFormatDetails LoadedFormatDetails { get; private set; }

        /// <summary>
        /// Mipmap width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Mipmap height.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Indicates if there is any alpha in image.
        /// </summary>
        public bool IsAlphaPresent
        {
            get
            {
                if (Pixels?.Length != 0)
                {
                    for (int i = 3; i < Pixels.Length; i += 4)   // TODO: ComponentSize
                    {
                        if (Pixels[i] != 0)
                            return true;
                    }
                }

                return false;
            }
        }


        /// <summary>
        /// Creates a Mipmap object from a WPF image.
        /// </summary>
        public MipMapBase(byte[] pixels, int width, int height, ImageFormats.ImageEngineFormatDetails details)
        {
            Initialise(pixels, width, height, details);
        }

        public abstract MipMapBase Resize(double xScale, double yScale);

        public MipMapBase Resize(double scale)
        {
            return Resize(scale, scale);
        }
    }
}
