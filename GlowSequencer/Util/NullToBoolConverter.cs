using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GlowSequencer.Util
{
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = parameter as string ?? "";

            if (param == "inverted")
                return value == null;
            else
                return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = parameter as string ?? "";

            if (!(value is bool))
                throw new InvalidOperationException("can only convert back booleans");

            bool v = (bool)value;

            // if uninverted and false || inverted and true --> set to null
            if (v == (param == "inverted"))
                return null;
            else
                return Binding.DoNothing;
        }
    }
}
