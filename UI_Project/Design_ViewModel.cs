using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI_Project
{
    public class Design_ViewModel : NewViewModel
    {
        public Design_ViewModel() : base()
        {
            LoadedImage = new CSharpImageLibrary.ImageEngineImage(Properties.Resources.DXT1_CodecTest);
        }
    }
}
