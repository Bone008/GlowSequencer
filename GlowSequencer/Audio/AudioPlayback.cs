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

        public event EventHandler PlaybackStopped;

        public bool IsInitialized => sampleProvider != null;
        public bool IsPlaying => playbackDevice != null && playbackDevice.PlaybackState == PlaybackState.Playing;
        public double CurrentTime => (double)sampleProvider.Position / sampleProvider.WaveFormat.SampleRate / sampleProvider.WaveFormat.Channels;

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
            playbackDevice.PlaybackStopped += (sender, e) => PlaybackStopped?.Invoke(sender, EventArgs.Empty);
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

            sampleProvider.Seek((int)(timeSeconds * sampleProvider.WaveFormat.SampleRate * sampleProvider.WaveFormat.Channels));
        }

        /// <summary>Stops playback (but doesn't actually reset the CurrentTime).</summary>
        public void Stop()
        {
            playbackDevice?.Stop();
        }

        public void Dispose()
        {
            Stop();
            playbackDevice?.Dispose();
            playbackDevice = null;
        }
    }
}
