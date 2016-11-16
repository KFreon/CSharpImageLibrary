using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpImageLibrary
{
    public static class GPU
    {
        public static bool IsGPUAvailable { get; private set; }

        static GPU()
        {
            IsGPUAvailable = false;
        }
    }
}
