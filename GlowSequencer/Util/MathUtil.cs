using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Util
{
    public static class MathUtil
    {
        // see http://stackoverflow.com/questions/1082917/mod-of-negative-number-is-melting-my-brain for discussion on negative values for modulo

        public static int RealMod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public static float RealMod(float x, float m)
        {
            return (x % m + m) % m;
        }

        public static float? RealMod(float? x, float m)
        {
            return (x % m + m) % m;
        }

        public static double RealMod(double x, double m)
        {
            return (x % m + m) % m;
        }

        public static int FloorToInt(float x)
        {
            return (int)Math.Floor(x);
        }

        public static int? FloorToInt(float? x)
        {
            return x == null ? (int?)null : (int?)Math.Floor(x.Value);
        }

        public static int FloorToInt(double x)
        {
            return (int)Math.Floor(x);
        }

        public static int? FloorToInt(double? x)
        {
            return x == null ? (int?)null : (int?)Math.Floor(x.Value);
        }
    }
}
