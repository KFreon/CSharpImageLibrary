using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary
{
    public static class Extensions
    {
        public static Vector<int> ShiftRightAndZeroNonRGB(this Vector<int> vector, int shift)
        {
            return new Vector<int>(new[] {
                vector[0] >> shift,
                vector[1] >> shift,
                vector[2] >> shift,
                0,0,0,0,0
            });
        }
    }
}
