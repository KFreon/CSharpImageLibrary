using CSharpImageLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using static CSharpImageLibrary.ImageFormats;

namespace UI_Project
{
    public class ImageEngineFormatDetailsToFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? 
                ImageEngineFormat.Unknown : 
                ((ImageEngineFormatDetails)value).SurfaceFormat;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            return new ImageEngineFormatDetails((ImageEngineFormat)value);
        }
    }
}
