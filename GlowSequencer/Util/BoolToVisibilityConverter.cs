using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GlowSequencer.Util
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // interpret null as false
            if (value == null)
                value = false;

            if (!(value is bool))
                return null;

            string param = parameter as string ?? "";

            Visibility invisible = param.Contains("nocollapse") ? Visibility.Hidden : Visibility.Collapsed;

            bool b = (bool)value;
            if (param.Contains("inverted"))
                b = !b;

            return b ? Visibility.Visible : invisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
