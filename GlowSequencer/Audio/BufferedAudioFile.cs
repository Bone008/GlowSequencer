using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace GlowSequencer.Audio
{
    /// <summary>
    /// This class reads an audio file into memory and allows multiple readers to access any part of it.
    /// After calling the constructor, LoadIntoMemoryAsync should be called, which automatically closes
    /// the file resource once it is finished.
    /// </summary>
    public class BufferedAudioFile
    {
        public const int READ_BLOCK_SIZE = 16 * 1024;

        private readonly string fileName;
        private readonly WaveFormat waveFormat;
        private AudioFileReader reader;

        private float[] data;
        private int currentLength = 0;
        private readonly object lengthLockObject = new object();

        /// <summary>Gets the total amount of audio samples in this file.</summary>
        public int SampleLength => data.Length;
        /// <summary>Gets the total duration of this file.</summary>
        public float TimeLength => (float)data.Length / waveFormat.SampleRate / waveFormat.Channels;

        /// <summary>Opens an audio file and reads metadata. Throws an exception when the file could not be opened.</summary>
        public BufferedAudioFile(string fileName)
        {
            this.fileName = fileName;
            reader = new AudioFileReader(fileName);
            data = new float[reader.Length / (reader.WaveFormat.BitsPerSample >> 3)];
            waveFormat = reader.WaveFormat;
        }

        public Task LoadIntoMemoryAsync(IProgress<float> progress)
        {
            return Task.Run(() =>
            {
                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    int numRead;
                    do
                    {
                        if (reader == null) break; // we were disposed

                        numRead = reader.Read(data, currentLength, READ_BLOCK_SIZE);
                        lock (lengthLockObject)
                        {
                            currentLength += numRead;
                            Monitor.PulseAll(lengthLockObject);
                        }
                        progress?.Report(currentLength / (float)data.Length);
                    } while (numRead > 0);
                }
                finally
                {
                    // Close file.
                    reader?.Dispose();
                    reader = null;

                    sw.Stop();
                    Debug.WriteLine($"loaded {currentLength} samples into memory in {sw.ElapsedMilliseconds} ms");
                }
            });
        }

        /// <summary>Creates a sample provider that starts reading from the beginning of the buffer data.</summary>
        /// <param name="infinite">if true, the sample provider will never stop reading and return 0 samples instead</param>
        public ISeekableSampleProvider CreateStream(bool infinite = false)
        {
            return new BufferReader(this, infinite);
        }

        private class BufferReader : ISeekableSampleProvider
        {
            private readonly BufferedAudioFile context;
            private readonly bool infinite;
            // Note: cannot reasonably use long here because for some reason Buffer.BlockCopy uses ints.
            // So the maximum supported sample count across all channels is in theory 2^29.
            private int position = 0;

            public BufferReader(BufferedAudioFile context, bool infinite)
            {
                this.context = context;
                this.infinite = infinite;
            }

            public WaveFormat WaveFormat => context.waveFormat;
            public int Position => position;

            private int ReadEOF(float[] buffer, int offset, int count)
            {
                if (infinite)
                {
                    // Note that we cannot use Array.Clear, because buffer may actually be
                    // a unioned byte array instead, and Array.Clear would be confused.
                    for (int i = offset; i < offset + count; i++)
                        buffer[i] = 0;
                    return count;
                }
                else return 0;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                // already at EOF?
                if (position >= context.data.Length) return ReadEOF(buffer, offset, count);

                long unreadSamples;
                lock (context.lengthLockObject)
                {
                    while (true)
                    {
                        int availableSamples = context.currentLength;
                        unreadSamples = availableSamples - position;
                        if (unreadSamples > 0)
                            break; // data available
                        else if (availableSamples >= context.data.Length)
                            return ReadEOF(buffer, offset, count);
                        else
                            Monitor.Wait(context.lengthLockObject);
                    }
                }

                count = (int)Math.Min(unreadSamples, count);
                // copy count samples from context.data[position] to buffer[offset]
                Buffer.BlockCopy(context.data, position * sizeof(float), buffer, offset * sizeof(float), count * sizeof(float));
                //Array.Copy(context.data, position, buffer, offset, count);
                position += count;
                return count;
            }

            public void Seek(int position)
            {
                if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
                this.position = position;
            }
        }
    }
}
