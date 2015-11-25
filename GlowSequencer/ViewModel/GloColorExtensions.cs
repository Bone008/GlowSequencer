using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public static class GloColorExtensions
    {
        public static System.Windows.Media.Color ToViewColor(this GloColor c)
        {
            return System.Windows.Media.Color.FromRgb(
                (byte)c.r,
                (byte)c.g,
                (byte)c.b
             );
        }

        public static GloColor ToGloColor(this System.Windows.Media.Color c)
        {
            return GloColor.FromRGB(c.R, c.G, c.B);
        }
    }
}
