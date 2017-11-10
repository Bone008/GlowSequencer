using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Audio
{
    public interface ISeekableSampleProvider : ISampleProvider
    {
        /// <summary>Gets the current sample position.</summary>
        int Position { get; }

        /// <summary>Sets the read position to the given sample.</summary>
        void Seek(int position);
    }
}
