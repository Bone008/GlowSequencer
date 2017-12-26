using GlowSequencer.Audio;
using GlowSequencer.Model;
using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public enum WaveformDisplayMode
    {
        Linear,
        Logarithmic
    }

    public class PlaybackViewModel : Observable
    {
        private static readonly TimeSpan CURSOR_UPDATE_INTERVAL = TimeSpan.FromMilliseconds(10);

        private readonly Timeline timelineModel;
        private readonly SequencerViewModel sequencer;
        private readonly System.Windows.Threading.DispatcherTimer cursorUpdateTimer;
        private readonly AudioPlayback audioPlayback = new AudioPlayback();
        private BufferedAudioFile audioFile = null;

        private bool inUpdateCursorPosition = false;
        private CancellationTokenSource renderWaveformCts = null;

        private Waveform _currentWaveform = null;
        private bool _isLoading = false;
        private float _musicVolume = 1.0f;

        public Waveform CurrentWaveform { get { return _currentWaveform; } set { SetProperty(ref _currentWaveform, value); } }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }
        public bool IsPlaying => audioPlayback.IsPlaying;

        public string MusicFileName { get { return timelineModel.MusicFileName; } private set { timelineModel.MusicFileName = value; Notify(); } }
        /// <summary>Total time in seconds of the loaded music file, or 0 if none is loaded.</summary>
        public float MusicDuration => audioFile != null ? audioFile.TimeLength : 0;
        public float MusicVolume { get { return _musicVolume; } set { SetProperty(ref _musicVolume, value); } }

        // LOW support this
        private float MusicTimeOffset => 0;

        public PlaybackViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;
            this.timelineModel = sequencer.GetModel();

            cursorUpdateTimer = new System.Windows.Threading.DispatcherTimer() { Interval = CURSOR_UPDATE_INTERVAL };
            cursorUpdateTimer.Tick += (_, __) => UpdateCursorPosition();

            ForwardPropertyEvents(nameof(MusicVolume), this, () => audioPlayback.Volume = LoudnessHelper.LoudnessFromVolume(MusicVolume));
            ForwardPropertyEvents(nameof(sequencer.CurrentViewLeftPositionTime), sequencer, InvalidateWaveform);
            // Note: Because CurrentViewLeftPositionTime depends on TimePixelScale and is always changed together with the right position,
            // it is enough to only depend on this one to reduce callback duplication.
            //ForwardPropertyEvents(nameof(sequencer.TimePixelScale), sequencer, InvalidateWaveform);
            //ForwardPropertyEvents(nameof(sequencer.CurrentViewRightPositionTime), sequencer, InvalidateWaveform);
            ForwardPropertyEvents(nameof(sequencer.CursorPosition), sequencer, OnCursorPositionChanged);
            audioPlayback.PlaybackStopped += OnPlaybackStopped;

            audioPlayback.Init(EmptySampleProvider.Singleton);
        }

        private void InvalidateWaveform()
        {
            // IsLoading is important because we may initially have started rendering
            // while the viewport width was unknown.
            if (CurrentWaveform != null || IsLoading)
                RenderWaveformAsync(true).Forget();
        }

        private void OnCursorPositionChanged()
        {
            if (!inUpdateCursorPosition && audioPlayback.IsInitialized && audioPlayback.IsPlaying)
            {
                audioPlayback.Seek(sequencer.CursorPosition - MusicTimeOffset);
            }
        }

        private void UpdateCursorPosition()
        {
            inUpdateCursorPosition = true;
            sequencer.CursorPosition = (float)audioPlayback.CurrentTime + MusicTimeOffset;
            inUpdateCursorPosition = false;

            // Stop when at end of timeline.
            if (audioPlayback.IsPlaying && sequencer.CursorPosition >= sequencer.TimelineLength)
                Stop();
        }

        private void OnPlaybackStopped(object sender, EventArgs e)
        {
            cursorUpdateTimer.Stop();
            UpdateCursorPosition();
            Notify(nameof(IsPlaying));
        }

        /// <summary>Starts playback at the given time in seconds.</summary>
        public bool PlayAt(float time)
        {
            sequencer.CursorPosition = time;
            return Play();
        }

        public bool Play()
        {
            if (!audioPlayback.IsInitialized) return false;
            if (audioPlayback.IsPlaying) return false;
            // Already at end of timeline?
            if (sequencer.CursorPosition >= sequencer.TimelineLength)
                return false;

            audioPlayback.Seek(sequencer.CursorPosition - MusicTimeOffset);
            audioPlayback.Play();
            cursorUpdateTimer.Start();
            Notify(nameof(IsPlaying));
            return true;
        }

        public bool Stop()
        {
            if (!audioPlayback.IsPlaying) return false;

            audioPlayback.Stop();
            // The rest will be updated by the PlaybackStopped callback.
            return true;
        }

        public void ClearFile()
        {
            Stop();
            audioPlayback.Init(EmptySampleProvider.Singleton);
            audioFile = null;
            CurrentWaveform = null;
            RenderWaveformAsync(false).Forget();

            MusicFileName = null;
        }

        public async Task LoadFileAsync(string fileName)
        {
            try { audioFile = new BufferedAudioFile(fileName); }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Could not load music file: " + Path.GetFileName(fileName) + Environment.NewLine + Environment.NewLine + e.Message, "Problem opening file",
                                               System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            MusicFileName = fileName;

            // TODO progress inidicator for file loading
            audioFile.LoadIntoMemoryAsync(null).Forget();
            audioPlayback.Init(audioFile.CreateStream(infinite: true));

            // Initialize our user-facing value from the playback device (which apparently gets its value from Windows).
            MusicVolume = LoudnessHelper.VolumeFromLoudness(audioPlayback.Volume);

            CurrentWaveform = null;
            await RenderWaveformAsync(false);

            Notify(nameof(MusicDuration));
        }

        private async Task RenderWaveformAsync(bool withDelay)
        {
            // Note: This function does not set CurrentWaveform to null to allow the old
            // one to stay visible until the new calculation is complete (when called by InvalidateWaveform).

            renderWaveformCts?.Cancel();
            renderWaveformCts?.Dispose();
            renderWaveformCts = new CancellationTokenSource();

            try
            {
                IsLoading = true;
                if (withDelay)
                    await Task.Delay(300, renderWaveformCts.Token);
                if (audioFile == null) // no longer available
                {
                    IsLoading = false;
                    return;
                }

                double viewLeft = sequencer.CurrentViewLeftPositionTime - MusicTimeOffset;
                double viewRight = sequencer.CurrentViewRightPositionTime - MusicTimeOffset;
                double padding = (viewRight - viewLeft) / 4; // use the width of one viewport on each side as padding
                double waveformLeft = Math.Max(0, viewLeft - padding);
                double waveformRight = viewRight + padding;
                
                Waveform result = await WaveformGenerator.CreateWaveformAsync(audioFile.CreateStream(),
                                                                              sequencer.TimePixelScale,
                                                                              waveformLeft,
                                                                              waveformRight,
                                                                              renderWaveformCts.Token);
                Debug.WriteLine($"Time per sample: {result.TimePerSample}, Sample count: {result.Minimums.Length}");

                CurrentWaveform = result;
                IsLoading = false;
            }
            catch (OperationCanceledException) { }
        }
    }
}
