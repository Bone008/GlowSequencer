using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace GlowSequencer.Util
{
    public class MoreVibrantColorConverter : IValueConverter
    {
        private const double MIN_BRIGHTNESS = 0.4;
        private const double DARK_BRIGHTNESS_RANGE = 0.1;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is Color)
            {
                Color c = (Color)value;

                // do not modify black
                if (c.R + c.G + c.B == 0)
                    return c;

                System.Drawing.Color otherColor = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
                double h, s, v;
                ColorUtil.ColorToHSV(otherColor, out h, out s, out v);

                // only modify if original color is bright enough
                if (v > MIN_BRIGHTNESS + DARK_BRIGHTNESS_RANGE)
                    return c;

                v = MIN_BRIGHTNESS + (v / (MIN_BRIGHTNESS + DARK_BRIGHTNESS_RANGE)) * DARK_BRIGHTNESS_RANGE;

                otherColor = ColorUtil.ColorFromHSV(h, s, v);
                // convert back to WPF color
                return new Color { R = otherColor.R, G = otherColor.G, B = otherColor.B, A = otherColor.A };

                //return new Color
                //{
                //    R = (byte)(c.R == 0 ? 0 : MIN_BRIGHTNESS + (c.R / 255.0f) * DARK_BRIGHTNESS_RANGE),
                //    G = (byte)(c.G == 0 ? 0 : MIN_BRIGHTNESS + (c.G / 255.0f) * DARK_BRIGHTNESS_RANGE),
                //    B = (byte)(c.B == 0 ? 0 : MIN_BRIGHTNESS + (c.B / 255.0f) * DARK_BRIGHTNESS_RANGE),
                //    A = c.A
                //};
            }

            throw new InvalidOperationException("Unsupported type: " + value.GetType().Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
