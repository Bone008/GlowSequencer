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

        public static int Clamp(int value, int min, int max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        public static float Clamp(float value, float min, float max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        /// <summary>Returns the smaller of two TimeSpan values.</summary>
        public static TimeSpan Min(TimeSpan x, TimeSpan y)
        {
            return (x < y ? x : y);
        }

        /// <summary>Returns the larger of two TimeSpan values.</summary>
        public static TimeSpan Max(TimeSpan x, TimeSpan y)
        {
            return (x > y ? x : y);
        }
    }
}
