using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;
using Microsoft.IO;
using System.IO;

namespace CSharpImageLibrary.General
{
    /// <summary>
    /// Represents a mipmap of an image.
    /// </summary>
    public class MipMap
    {
        /// <summary>
        /// Pixels as stream.
        /// </summary>
        public MemoryStream Data { get; set; }

        /// <summary>
        /// Mipmap width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Mipmap height.
        /// </summary>
        public int Height { get; set; }


        /// <summary>
        /// Creates a mipmap from stream at a given width and height.
        /// </summary>
        /// <param name="data">Raw pixels.</param>
        /// <param name="width">Mipmap width.</param>
        /// <param name="height">Mipmap height.</param>
        public MipMap(MemoryStream data, int width, int height)
        {
            Data = data;
            Width = UsefulThings.General.RoundToNearestPowerOfTwo(width);
            Height = UsefulThings.General.RoundToNearestPowerOfTwo(height);
        }
    }
}
