using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace GlowSequencer.Util
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is TimeSpan)
            {
                TimeSpan ts = (TimeSpan)value;
                
                string sign = "";
                if (ts < TimeSpan.Zero)
                {
                    ts = ts.Negate();
                    sign = "-";
                }

                bool compactMode = (parameter != null && bool.Parse(parameter.ToString()));

                if(compactMode)
                    return sign + (ts.TotalMinutes >= 1 ? Math.Floor(ts.TotalMinutes) + ":" : "") + (ts.Seconds + (ts.Milliseconds / 1000.0)).ToString("0.###", CultureInfo.InvariantCulture);
                else
                    return sign + Math.Floor(ts.TotalMinutes).ToString("00") + ":" + ts.ToString("ss\\.fff");
            }

            throw new InvalidOperationException("Unsupported type: " + value.GetType().Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if(value is string)
            {
                string str = ((string)value).Replace(" ", "");
                Match m = Regex.Match(str, @"^(-)?(?:(\d+):)?(\d+(?:\.\d+)?)$"); // accepts 'mm:ss.fff', 'mm:ss', 'ss.fff' and 'ss' format, as well as negatives
                if (!m.Success)
                    return null;

                TimeSpan result = TimeSpan.FromSeconds(double.Parse(m.Groups[3].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));
                if (m.Groups[2].Success)
                    result += TimeSpan.FromMinutes(int.Parse(m.Groups[2].Value));

                if (m.Groups[1].Success)
                    result = result.Negate();

                return result;
            }

            throw new InvalidOperationException("Unsupported type: " + value.GetType().Name);
        }
    }
}
