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
        private readonly SequencerViewModel sequencer;
        private readonly AudioPlayback audioPlayback = new AudioPlayback();
        private BufferedAudioFile audioFile = null;

        private CancellationTokenSource cts = null;

        private Waveform _currentWaveform = null;
        private bool _isLoading = false;

        public Waveform CurrentWaveform { get { return _currentWaveform; } set { SetProperty(ref _currentWaveform, value); } }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }

        public PlaybackViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;

            ForwardPropertyEvents(nameof(sequencer.TimePixelScale), sequencer, InvalidateWaveform);
            ForwardPropertyEvents(nameof(sequencer.CurrentViewLeftPositionTime), sequencer, InvalidateWaveform);
            ForwardPropertyEvents(nameof(sequencer.CurrentViewRightPositionTime), sequencer, InvalidateWaveform);
        }

        private void InvalidateWaveform()
        {
            if (CurrentWaveform != null)
                RenderWaveformAsync(true).Forget();
        }

        public void TogglePlaying()
        {
            // TODO
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

            audioFile.LoadIntoMemoryAsync(null).Forget();

            CurrentWaveform = null;
            IsLoading = true;
            await RenderWaveformAsync(false);
        }

        private async Task RenderWaveformAsync(bool withDelay)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();

            try
            {
                IsLoading = true;
                if (withDelay)
                    await Task.Delay(300, cts.Token);

                double viewLeft = sequencer.CurrentViewLeftPositionTime;
                double viewRight = sequencer.CurrentViewRightPositionTime;
                double padding = (viewRight - viewLeft) / 4; // use the width of one viewport on each side as padding
                double waveformLeft = Math.Max(0, viewLeft - padding);
                double waveformRight = viewRight + padding;

                Waveform result = await WaveformGenerator.CreateWaveformAsync(audioFile.CreateStream(),
                                                                              sequencer.TimePixelScale,
                                                                              waveformLeft,
                                                                              waveformRight,
                                                                              cts.Token);
                Debug.WriteLine($"Time per sample: {result.TimePerSample}, Sample count: {result.Minimums.Length}");

                CurrentWaveform = result;
                IsLoading = false;
            }
            catch (OperationCanceledException) { }
        }
    }
}
