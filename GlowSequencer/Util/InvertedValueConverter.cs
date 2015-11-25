using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace GlowSequencer.Util
{
    public class InvertedValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is bool)
                return !((bool)value);
            if (value is int)
                return -((int)value);
            if (value is float)
                return -((float)value);
            if (value is double)
                return -((double)value);

            throw new InvalidOperationException("Unsupported type: " + value.GetType().Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // inverting is symmetric
            return Convert(value, targetType, parameter, culture);
        }
    }
}
