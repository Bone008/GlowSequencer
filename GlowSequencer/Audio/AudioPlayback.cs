using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Audio
{
    /// <summary>Encapsulates control over playback and positioning.</summary>
    public class AudioPlayback : IDisposable
    {
        private static readonly WaveFormat DEFAULT_WAVE_FORMAT = new WaveFormat();

        private WaveOut playbackDevice = null;
        private ISeekableSampleProvider sampleProvider = null;
        private long deviceSamplePositionBase = 0; // the value that GetPosition() returned as we started seeking
        private int lastKnownSeekSample = 0; // the value that was last sent to sampleProvider.Seek(...)

        /// <summary>Used to wait for a playback device to be actually stopped.</summary>
        private Action continuationOnStoppedHook = null;

        public event EventHandler PlaybackStopped;

        public bool IsInitialized => sampleProvider != null;
        public bool IsPlaying => playbackDevice?.PlaybackState == PlaybackState.Playing;

        /// <summary>Returns the audio's current playback position in seconds.</summary>
        public double CurrentTime
        {
            get
            {
                int currentSamplePos = playbackDevice != null
                    ? lastKnownSeekSample + GetSamplePositionFromDevice()
                    : (sampleProvider?.Position ?? 0);
                return currentSamplePos / GetTotalSamplesPerSecond();
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

        // Note: Actually no longer used because instead of disabling playback with no music,
        // we load an EmptySampleProvider instead.
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
                playbackDevice.PlaybackStopped += OnPlaybackStopped;

                // Old hack to read the volume from the system (no longer needed as of NAudio 2.x).
                //playbackDevice.Volume = WaveOutHelper.GetWaveOutVolume(playbackDevice);
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
            if (wasPlaying)
            {
                Stop();
                continuationOnStoppedHook = () => FinishSeek(timeSeconds, wasPlaying);
            }
            else
            {
                FinishSeek(timeSeconds, wasPlaying);
            }
        }

        private void FinishSeek(double timeSeconds, bool wasPlaying)
        {
            // if Clear() was called in the meantime
            if (sampleProvider == null)
                return;

            UpdateSamplePositionBase();
            lastKnownSeekSample = (int)(timeSeconds * GetTotalSamplesPerSecond());
            sampleProvider.Seek(lastKnownSeekSample);
            if (wasPlaying)
                Play();
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
                UpdateSamplePositionBase();
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
            if (playbackDevice == null)
                return 0;
            long position = playbackDevice.GetPosition() - deviceSamplePositionBase;
            return (int)(position / sizeof(float));
        }

        /// <summary>
        /// Workaround for some NAudio bug triggered when the default output device is changed in Windows.
        /// Even though this is now officially supported: https://github.com/naudio/NAudio/pull/172
        /// 
        /// </summary>
        private void UpdateSamplePositionBase()
        {
            if (playbackDevice != null)
                deviceSamplePositionBase = playbackDevice.GetPosition();
        }

        private double GetTotalSamplesPerSecond()
        {
            // I don't really understand why, but if the playback device's wave format changes
            // after switching the output device in Windows, we have to do all conversions based on
            // its new format, EVEN THOUGH the sample provider's wave format didn't change o.O
            var waveFormat = playbackDevice?.OutputWaveFormat
                ?? sampleProvider?.WaveFormat
                ?? DEFAULT_WAVE_FORMAT;
            return waveFormat.SampleRate * waveFormat.Channels;
        }
    }
}
