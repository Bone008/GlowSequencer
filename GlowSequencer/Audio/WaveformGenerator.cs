using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GlowSequencer.Audio
{
    public static class WaveformGenerator
    {
        private const int READ_BUFFER_SIZE = 4096;

        public static Task<Waveform> CreateWaveformAsync(ISampleProvider sampleProvider, float scaleInPixelsPerSecond, CancellationToken cancellation = default(CancellationToken))
        {
            return Task.Run(() => CreateWaveform(sampleProvider, scaleInPixelsPerSecond, cancellation));
        }

        private static Waveform CreateWaveform(ISampleProvider sampleProvider, float scaleInPixelsPerSecond, CancellationToken cancellation)
        {
            int channels = sampleProvider.WaveFormat.Channels;
            float sampleRate = sampleProvider.WaveFormat.SampleRate;

            int? estimatedValueCount = null;
            if (sampleProvider is Stream stream)
            {
                long estimatedSampleCount = stream.Length / (sampleProvider.WaveFormat.BitsPerSample >> 3);
                estimatedValueCount = (int)(estimatedSampleCount / sampleRate * scaleInPixelsPerSecond / channels);
            }

            Debug.WriteLine("estimated values: " + estimatedValueCount);
            List<float> minValues = new List<float>(estimatedValueCount ?? 0);
            List<float> maxValues = new List<float>(estimatedValueCount ?? 0);

            int c = 0; // global sample counter (for all channels)
            int lastX = 0; // current render position
            float currentMin = float.PositiveInfinity;
            float currentMax = float.NegativeInfinity;

            float[] buffer = new float[READ_BUFFER_SIZE];
            int numRead;
            do
            {
                cancellation.ThrowIfCancellationRequested();

                numRead = sampleProvider.Read(buffer, 0, READ_BUFFER_SIZE);
                for (int i = 0; i < numRead; i++)
                {
                    float renderPosition = c / sampleRate * scaleInPixelsPerSecond;
                    int x = (int)renderPosition;
                    if (x > lastX)
                    {
                        // we advanced a pixel, add new values with aggregate so far until we are at the current pixel again
                        while (x > lastX)
                        {
                            minValues.Add(currentMin);
                            maxValues.Add(currentMax);
                            lastX++;
                        }
                        // reset
                        currentMin = float.PositiveInfinity;
                        currentMax = float.NegativeInfinity;
                    }

                    // aggregate
                    float value = buffer[i];
                    currentMin = Math.Min(currentMin, value);
                    currentMax = Math.Max(currentMax, value);

                    // only count up total when we cycled through all channels
                    if ((i+1) % channels == 0)
                        c++;
                }
            } while (numRead > 0);

            var timePerSample = TimeSpan.FromSeconds(1.0 / scaleInPixelsPerSecond);
            return new Waveform(timePerSample, minValues.ToArray(), maxValues.ToArray());
        }
    }
}
