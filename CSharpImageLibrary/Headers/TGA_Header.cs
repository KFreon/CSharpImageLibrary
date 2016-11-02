using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary.Headers
{
    public class TGA_Header : AbstractHeader
    {
        #region Properties
        #endregion Properties

        public TGA_Header(Stream stream)
        {

        }

        protected override long Load(Stream stream)
        {
            base.Load(stream);
        }

        public override string ToString()
        {
            return base.ToString();asd
        }
    }
}
