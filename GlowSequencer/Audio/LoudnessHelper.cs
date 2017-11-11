using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Audio
{
    public static class LoudnessHelper
    {
        // See https://www.dr-lex.be/info-stuff/volumecontrols.html for an explanation of the nature of these formulas.
        // We assume 40 dB of dynamic range and cut-off at 0.
        private const float A = 0.01f;
        private const float B = 4.60517f; // b = ln(1/a)

        /// <summary>Properly scales a volume user input in the range [0, 1] to account for logarithmic loudness.</summary>
        public static float LoudnessFromVolume(float volume)
        {
            if (volume == 0) return 0;

            float scaleFactor = (float)(A * Math.Pow(Math.E, B * volume));
            if (scaleFactor > 1.0f)
                scaleFactor = 1.0f; // prevent rounding errors at the top

            return scaleFactor;
        }

        /// <summary>Inverse of LoudnessFromVolume.</summary>
        public static float VolumeFromLoudness(float loudness)
        {
            if (loudness == 0) return 0;

            float volume = (float)(Math.Log(loudness / A) / B);
            return MathUtil.Clamp(volume, 0f, 1f);
        }
    }
}
