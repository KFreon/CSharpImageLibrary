using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CSharpImageLibrary.ImageFormats;

namespace CSharpImageLibrary.Headers
{
    /// <summary>
    /// Base header class for image headers.
    /// </summary>
    public abstract class AbstractHeader
    {
        /// <summary>
        /// Indicates the header type.
        /// </summary>
        public enum HeaderType
        {
            DDS,
            BMP,
            JPG,
            PNG,
            GIF,
            TGA,
            TIFF,
            UNKNOWN,
        }

        /// <summary>
        /// Indicates the type of image this header is representing.
        /// </summary>
        public abstract HeaderType Type { get; }

        public ImageEngineFormatDetails FormatDetails { get; set; }

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

        /// <summary>
        /// Provides string representation of header.
        /// </summary>
        /// <returns>String of header properties.</returns>
        public override string ToString()
        {
            // Add some spacing for readability.
            return StringifyObject(this);
        }

        public static string StringifyObject(object obj, int level = 0, string propName = null)
        {
            var propertyList = TypeDescriptor.GetProperties(obj);
            StringBuilder sb = new StringBuilder();
            var classname = TypeDescriptor.GetClassName(obj);
            string tags = new string(Enumerable.Repeat('-', level * 3).ToArray());
            string spacing = new string(Enumerable.Repeat(' ', level * 3).ToArray());

            if (propertyList.Count == 0)
                return spacing + $"{propName} = {obj}";

            sb.AppendLine($"{tags} {classname} {tags}");
            foreach (PropertyDescriptor descriptor in propertyList)
                sb.AppendLine(spacing + StringifyObject(descriptor.GetValue(obj), level + 1, descriptor.Name));

            sb.AppendLine($"{tags} END {classname} {tags}");


            return sb.ToString();
        }
    }
}
