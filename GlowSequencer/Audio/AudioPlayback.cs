using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Audio
{
    public class AudioPlayback : IDisposable
    {
        private WaveOut playbackDevice = null;
        private ISeekableSampleProvider sampleProvider = null;

        public bool IsInitialized => sampleProvider != null;
        public bool IsPlaying => playbackDevice != null && playbackDevice.PlaybackState == PlaybackState.Playing;
        public double CurrentTime => (double)sampleProvider.Position / sampleProvider.WaveFormat.SampleRate;

        public void Init(ISeekableSampleProvider sampleProvider)
        {
            Stop();
            EnsureDeviceCreated();

            this.sampleProvider = sampleProvider;
            playbackDevice.Init(sampleProvider);
        }

        private void EnsureDeviceCreated()
        {
            if (playbackDevice == null)
            {
                CreateDevice();
            }
        }

        private void CreateDevice()
        {
            playbackDevice = new WaveOut { DesiredLatency = 200 };
        }

        public void Play()
        {
            if (sampleProvider == null)
                throw new InvalidOperationException("not initialized");

            if (playbackDevice != null && playbackDevice.PlaybackState != PlaybackState.Playing)
            {
                playbackDevice.Play();
            }
        }

        public void Pause()
        {
            playbackDevice?.Pause();
        }

        public void Seek(double timeSeconds)
        {
            if (sampleProvider == null)
                throw new InvalidOperationException("not initialized");

            sampleProvider.Seek((long)(timeSeconds * sampleProvider.WaveFormat.SampleRate));
        }

        public void Stop()
        {
            playbackDevice?.Stop();
            if (sampleProvider != null)
            {
                sampleProvider.Seek(0);
            }
        }

        public void Dispose()
        {
            Stop();
            playbackDevice?.Dispose();
            playbackDevice = null;
        }
    }
}
