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
        private const int WAVEFORM_PIXEL_INTERVAL = 2;
        private const int READ_BUFFER_SIZE = 4096;

        public static Task<Waveform> CreateWaveformAsync(ISeekableSampleProvider sampleProvider, float scaleInPixelsPerSecond,
                                                         double fromTime, double toTime,
                                                         CancellationToken cancellation = default(CancellationToken))
        {
            if (scaleInPixelsPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(scaleInPixelsPerSecond), scaleInPixelsPerSecond, "must be positive");
            if (fromTime < 0)
                throw new ArgumentOutOfRangeException(nameof(fromTime), fromTime, "must be non-negative");

            return Task.Run(() => CreateWaveform(sampleProvider, scaleInPixelsPerSecond, fromTime, toTime, cancellation), cancellation);
        }

        private static Waveform CreateWaveform(ISeekableSampleProvider sampleProvider, float scaleInPixelsPerSecond,
                                               double fromTime, double toTime,
                                               CancellationToken cancellation = default(CancellationToken))
        {
            var sw = new Stopwatch();
            sw.Start();

            int channels = sampleProvider.WaveFormat.Channels;
            float sampleRate = sampleProvider.WaveFormat.SampleRate;

            scaleInPixelsPerSecond /= WAVEFORM_PIXEL_INTERVAL;
            // It does not make sense to have more pixels than samples.
            if (scaleInPixelsPerSecond > sampleRate)
                scaleInPixelsPerSecond = sampleRate;

            List<float> minValues = new List<float>();
            List<float> maxValues = new List<float>();

            // the first sample to include in the waveform - aligned with the pixel interval to prevent jittering when scrolling
            long alignIntervalFactor = (long)Math.Round(sampleRate / scaleInPixelsPerSecond);
            long firstSample = (long)(fromTime * sampleRate) / alignIntervalFactor * alignIntervalFactor;
            long lastSample = (long)Math.Ceiling(toTime * sampleRate);

            sampleProvider.Seek((int)firstSample * channels);

            long c = firstSample; // global sample counter (for all channels)
            int lastX = 0; // current render position
            float currentMin = float.PositiveInfinity;
            float currentMax = float.NegativeInfinity;

            float[] buffer = new float[READ_BUFFER_SIZE];
            int numRead;
            do
            {
                cancellation.ThrowIfCancellationRequested();

                numRead = sampleProvider.Read(buffer, 0, READ_BUFFER_SIZE);
                for (int i = 0; i < numRead && c <= lastSample; i++)
                {
                    float renderPosition = (c - firstSample) / sampleRate * scaleInPixelsPerSecond;
                    int x = (int)renderPosition;
                    if (x > lastX)
                    {
                        // we advanced a pixel, add new values with aggregate
                        minValues.Add(currentMin);
                        maxValues.Add(currentMax);
                        lastX++;
                        // reset
                        currentMin = float.PositiveInfinity;
                        currentMax = float.NegativeInfinity;
                    }

                    // aggregate
                    float value = buffer[i];
                    currentMin = Math.Min(currentMin, value);
                    currentMax = Math.Max(currentMax, value);

                    // only count up total when we cycled through all channels
                    if ((i + 1) % channels == 0)
                        c++;
                }
            } while (numRead > 0 && c <= lastSample);

            double actualFromTime = (double)firstSample / sampleRate;
            double timePerSample = 1.0 / scaleInPixelsPerSecond;
            var wf = new Waveform(actualFromTime, timePerSample, minValues.ToArray(), maxValues.ToArray());

            sw.Stop();
            Debug.WriteLine($"generated waveform with {minValues.Count} data points for {toTime - fromTime} s in {sw.ElapsedMilliseconds} ms");

            return wf;
        }
    }
}
