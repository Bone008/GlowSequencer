using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Audio
{
    public sealed class Waveform
    {
        /// <summary>How much time each entry in the data arrays represents.</summary>
        public TimeSpan TimePerSample { get; private set; }
        public float[] Minimums { get; private set; }
        public float[] Maximums { get; private set; }

        public Waveform(TimeSpan timePerSample, float[] minimums, float[] maximums)
        {
            TimePerSample = timePerSample;
            Minimums = minimums;
            Maximums = maximums;
        }
    }
}
