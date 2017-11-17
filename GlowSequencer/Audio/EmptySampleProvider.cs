using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace GlowSequencer.Audio
{
    public sealed class EmptySampleProvider : ISeekableSampleProvider
    {
        public static readonly EmptySampleProvider Singleton = new EmptySampleProvider();

        public int Position => 0;
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        private EmptySampleProvider() { }

        /// <summary>Fills the buffer with all zeros.</summary>
        public int Read(float[] buffer, int offset, int count)
        {
            // Note that we cannot use Array.Clear, because buffer may actually be
            // a unioned byte array instead, and Array.Clear would be confused.
            for (int i = offset; i < offset + count; i++)
                buffer[i] = 0.0f;

            return count;
        }

        public void Seek(int position)
        {
            // noop
        }
    }
}
