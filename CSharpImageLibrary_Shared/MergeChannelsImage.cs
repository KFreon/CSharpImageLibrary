using System.IO;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Represents an channel candidate to be merged into a single image.
    /// </summary>
    public class MergeChannelsImage<IMG, MIP> where IMG : ImageEngineImageBase<MIP>, new() where MIP : MipMapBase, new() 
    {
        #region Properties
        /// <summary>
        /// Size of channel components in bytes. e.g. 16bit = 2.
        /// </summary>
        public int ComponentSize { get; private set; }

        /// <summary>
        /// Pixels of this channel.
        /// </summary>
        public byte[] Pixels { get; private set; }

        /// <summary>
        /// Path to image file.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Name to display in UI.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(FilePath);
            }
        }

        bool isRed = false;
        /// <summary>
        /// Indicates whether this channel is the red channel.
        /// </summary>
        public bool IsRed { get; set; }

        bool isGreen = false;
        /// <summary>
        /// Indicates whether this channel is the green channel.
        /// </summary>
        public bool IsGreen { get; set; }

        bool isBlue = false;
        /// <summary>
        /// Indicates whether this channel is the blue channel.
        /// </summary>
        public bool IsBlue { get; set; }

        bool isAlpha = false;
        /// <summary>
        /// Indicates whether this channel is the alpha channel.
        /// </summary>
        public bool IsAlpha { get; set; }

        /// <summary>
        /// Indicates if any of the channel colour settings have been set.
        /// </summary>
        public bool HasAssignedChannel
        {
            get
            {
                return IsAlpha || IsRed || IsBlue || IsGreen;
            }
        }

        /// <summary>
        /// Height of channel.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Width of channel.
        /// </summary>
        public int Width { get; private set; }
        #endregion Properties

        /// <summary>
        /// Creates a channel from an image. Can merge together with other channels to form a proper image again.
        /// MUST BE GRAYSCALE (PixelFormats.Gray8). 
        /// </summary>
        /// <param name="mainPath">Path to channel.</param>
        public MergeChannelsImage(string mainPath)
        {
            FilePath = mainPath;

            var img = new IMG();
            img.Initialise(mainPath);
            
            Width = img.Width;
            Height = img.Height;
        }

        /// <summary>
        /// Determines if this channel is compatible with other channels.
        /// </summary>
        /// <param name="channels">Channels to compare to.</param>
        /// <returns>True if compatible with all channels.</returns>
        public bool IsCompatibleWith(params MergeChannelsImage<IMG, MIP>[] channels)
        {
            foreach(var channel in channels)
            {
                if (channel == this || channel == null)
                    continue;


                if (Width != channel.Width || Height != channel.Height || Pixels.Length != channel.Pixels.Length)
                    return false;
            }

            return true;
        }
    }
}
