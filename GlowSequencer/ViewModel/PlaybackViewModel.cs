using GlowSequencer.Audio;
using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class PlaybackViewModel : Observable
    {
        private static readonly TimeSpan CURSOR_UPDATE_INTERVAL = TimeSpan.FromMilliseconds(10);

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

        /// <summary>Total time in seconds of the loaded music file, or 0 if none is loaded.</summary>
        public float MusicDuration => audioFile != null ? audioFile.TimeLength : 0;
        public float MusicVolume { get { return _musicVolume; } set { SetProperty(ref _musicVolume, value); } }

        // TODO support this
        private float MusicTimeOffset => 0;

        public PlaybackViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;

            cursorUpdateTimer = new System.Windows.Threading.DispatcherTimer() { Interval = CURSOR_UPDATE_INTERVAL };
            cursorUpdateTimer.Tick += (_, __) => UpdateCursorPosition();

            ForwardPropertyEvents(nameof(MusicVolume), this, () => audioPlayback.Volume = ScaleFactorFromUserVolume(MusicVolume));
            ForwardPropertyEvents(nameof(sequencer.TimePixelScale), sequencer, InvalidateWaveform);
            ForwardPropertyEvents(nameof(sequencer.CurrentViewLeftPositionTime), sequencer, InvalidateWaveform);
            ForwardPropertyEvents(nameof(sequencer.CurrentViewRightPositionTime), sequencer, InvalidateWaveform);
            ForwardPropertyEvents(nameof(sequencer.CursorPosition), sequencer, OnCursorPositionChanged);
            audioPlayback.PlaybackStopped += (_, __) => { cursorUpdateTimer.Stop(); UpdateCursorPosition(); };
        }

        private void InvalidateWaveform()
        {
            if (CurrentWaveform != null)
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
        }

        public void TogglePlaying()
        {
            // TODO how to play when there is no music track?

            if (!audioPlayback.IsInitialized)
                return;
            if (audioPlayback.IsPlaying)
            {
                audioPlayback.Stop();
                UpdateCursorPosition();
                cursorUpdateTimer.Stop();
            }
            else
            {
                audioPlayback.Seek(sequencer.CursorPosition - MusicTimeOffset);
                audioPlayback.Play();
                cursorUpdateTimer.Start();
            }
        }

        public async Task LoadFileAsync(string fileName)
        {
            try { audioFile = new BufferedAudioFile(fileName); }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message, "Problem opening file",
                                               System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // TODO progress inidicator for file loading
            audioFile.LoadIntoMemoryAsync(null).Forget();
            audioPlayback.Init(audioFile.CreateStream());

            // Initialize our user-facing value from the playback device (which apparently gets its value from Windows).
            MusicVolume = UserVolumeFromScaleFactor(audioPlayback.Volume);

            CurrentWaveform = null;
            await RenderWaveformAsync(false);

            Notify(nameof(MusicDuration));
        }

        private async Task RenderWaveformAsync(bool withDelay)
        {
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


        // See https://www.dr-lex.be/info-stuff/volumecontrols.html for an explanation of the nature of these formulas.
        // We assume 40 dB of dynamic range and cut-off at 0.
        private const float sc_a = 0.01f;
        private const float sc_b = 4.60517f; // b = ln(1/a)

        /// <summary>Properly scales a volume user input in the range [0, 1] to account for logarithmic loudness.</summary>
        private static float ScaleFactorFromUserVolume(float volume)
        {
            if (volume == 0) return 0;

            float scaleFactor = (float)(sc_a * Math.Pow(Math.E, sc_b * volume));
            if (scaleFactor > 1.0f)
                scaleFactor = 1.0f; // prevent rounding errors at the top

            return scaleFactor;
        }

        /// <summary>Inverse of ScaleFactorFromUserVolume.</summary>
        private static float UserVolumeFromScaleFactor(float scaleFactor)
        {
            if (scaleFactor == 0) return 0;

            float volume = (float)(Math.Log(scaleFactor / sc_a) / sc_b);
            return MathUtil.Clamp(volume, 0f, 1f);
        }
    }
}
