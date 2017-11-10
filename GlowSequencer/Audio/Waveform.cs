using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Audio
{
    public sealed class Waveform
    {
        /// <summary>At which time the data arrays start, in seconds.</summary>
        public double TimeOffset { get; private set; }
        /// <summary>How much time each entry in the data arrays represents, in seconds.</summary>
        public double TimePerSample { get; private set; }
        public float[] Minimums { get; private set; }
        public float[] Maximums { get; private set; }

        public Waveform(double timeOffset, double timePerSample, float[] minimums, float[] maximums)
        {
            TimeOffset = timeOffset;
            TimePerSample = timePerSample;
            Minimums = minimums;
            Maximums = maximums;
        }
    }
}
