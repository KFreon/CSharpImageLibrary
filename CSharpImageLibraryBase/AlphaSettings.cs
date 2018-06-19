namespace CSharpImageLibraryBase
{
    /// <summary>
    /// Determines how alpha is handled.
    /// </summary>
    public enum AlphaSettings
    {
        /// <summary>
        /// Keeps any existing alpha.
        /// </summary>
        KeepAlpha,

        /// <summary>
        /// Premultiplies RBG and Alpha channels. Alpha remains.
        /// </summary>
        Premultiply,

        /// <summary>
        /// Removes alpha channel.
        /// </summary>
        RemoveAlphaChannel,
    }
}
