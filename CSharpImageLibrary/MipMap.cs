using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary
{
    public class MipMap
    {
        public MemoryTributary Data { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public MipMap(MemoryTributary data, int width, int height)
        {
            Data = data;
            Width = UsefulThings.General.RoundToNearestPowerOfTwo(width);
            Height = UsefulThings.General.RoundToNearestPowerOfTwo(height);
        }
    }
}
