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
    public class BufferedAudioFile : IDisposable
    {
        public const int READ_BLOCK_SIZE = 16 * 1024;

        private readonly string fileName;
        private readonly WaveFormat waveFormat;
        private AudioFileReader reader;

        private float[] data;
        private int currentLength = 0;
        private readonly object lengthLockObject = new object();

        // possibly throws an exception when the file could not be opened
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
        public ISeekableSampleProvider CreateStream()
        {
            return new BufferReader(this);
        }

        public void Dispose()
        {
            reader?.Dispose();
            reader = null;
        }

        private class BufferReader : ISeekableSampleProvider
        {
            private readonly BufferedAudioFile context;
            private long position = 0;

            public BufferReader(BufferedAudioFile context)
            {
                this.context = context;
            }

            public WaveFormat WaveFormat => context.waveFormat;

            public int Read(float[] buffer, int offset, int count)
            {
                if (position >= context.data.Length) return 0; // already at EOF

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
                            return 0; // EOF
                        else
                            Monitor.Wait(context.lengthLockObject);
                    }
                }

                count = (int)Math.Min(unreadSamples, count);
                Array.Copy(context.data, position, buffer, offset, count);
                position += count;
                return count;
            }

            public void Seek(long position)
            {
                if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
                this.position = position;
            }
        }
    }
}
