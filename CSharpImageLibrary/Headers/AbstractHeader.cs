using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary.Headers
{
    /// <summary>
    /// Base header class for image headers.
    /// </summary>
    public abstract class AbstractHeader
    {
        internal static readonly long MaxHeaderSize = ;

        /// <summary>
        /// Format of image as seen by header.
        /// </summary>
        public abstract Format Format { get; }

        /// <summary>
        /// Width of image.
        /// </summary>
        public virtual int Width { get; protected set; }

        /// <summary>
        /// Height of image.
        /// </summary>
        public virtual int Height { get; protected set; }

        /// <summary>
        /// Loads header from stream.
        /// </summary>
        /// <param name="stream">Stream to load header from.</param>
        /// <returns>Length of header.</returns>
        protected virtual long Load(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return 0;
        }
    }
}
