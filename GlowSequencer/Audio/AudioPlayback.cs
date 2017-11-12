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
        private int lastKnownSeekSample = 0; // the value that was last sent to sampleProvider.Seek(...)

        /// <summary>Used to wait for a playback device to be actually stopped.</summary>
        private Action continuationOnStoppedHook = null;

        public event EventHandler PlaybackStopped;

        public bool IsInitialized => sampleProvider != null;
        public bool IsPlaying => playbackDevice?.PlaybackState == PlaybackState.Playing;

        public double CurrentTime
        {
            get
            {
                if (sampleProvider == null) return 0;
                int currentSamplePos = (playbackDevice != null ? lastKnownSeekSample + GetSamplePositionFromDevice() : sampleProvider.Position);
                return (double)currentSamplePos / sampleProvider.WaveFormat.SampleRate / sampleProvider.WaveFormat.Channels;
            }
        }

        public float Volume { get { return playbackDevice?.Volume ?? 1.0f; } set { if (playbackDevice != null) playbackDevice.Volume = value; } }

        public void Init(ISeekableSampleProvider sampleProvider)
        {
            Stop();
            EnsureDeviceCreated();

            this.sampleProvider = sampleProvider;
            lastKnownSeekSample = sampleProvider.Position;
            playbackDevice.Init(sampleProvider);
        }

        public void Clear()
        {
            Stop();
            this.sampleProvider = null;
            lastKnownSeekSample = 0;
            continuationOnStoppedHook = null;
        }

        private void EnsureDeviceCreated()
        {
            if (playbackDevice == null)
            {
                playbackDevice = new WaveOut { DesiredLatency = 200 };
                playbackDevice.PlaybackStopped += OnPlaybackStopped; ;

                // Hacky hack to read the volume from the system (should have been implemented by NAudio IMHO).
                playbackDevice.Volume = WaveOutHelper.GetWaveOutVolume(playbackDevice);
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            // If we are internally waiting for a stop to happen,
            // call the hook instead of forwarding the event (see Seek method).
            if (continuationOnStoppedHook != null)
            {
                continuationOnStoppedHook();
                continuationOnStoppedHook = null;
                return;
            }

            PlaybackStopped?.Invoke(sender, EventArgs.Empty);
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

            bool wasPlaying = IsPlaying;
            Action completeSeek = () =>
            {
            if (sampleProvider == null) return; // if Clear() was called in the meantime
                sampleProvider.Seek(lastKnownSeekSample = (int)(timeSeconds * sampleProvider.WaveFormat.SampleRate * sampleProvider.WaveFormat.Channels));
                if (wasPlaying) Play();
            };

            if (wasPlaying)
            {
                Stop();
                continuationOnStoppedHook = completeSeek;
            }
            else
            {
                completeSeek();
            }
        }

        /// <summary>Stops playback (but doesn't actually reset the CurrentTime).</summary>
        public void Stop()
        {
            if (playbackDevice != null)
            {
                // Because the position of the playback device will be reset by calling Stop,
                // we save how far we have played since the last seek in order to keep correct track
                // of CurrentTime after stopping.
                lastKnownSeekSample += GetSamplePositionFromDevice();
                playbackDevice.Stop();
            }
        }

        public void Dispose()
        {
            Stop();
            playbackDevice?.Dispose();
            playbackDevice = null;
        }

        private int GetSamplePositionFromDevice()
        {
            if (playbackDevice == null) return 0;
            return (int)(playbackDevice.GetPosition() / sizeof(float));
        }
    }
}
