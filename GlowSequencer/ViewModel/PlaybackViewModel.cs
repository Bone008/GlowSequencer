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
        private const int WAVEFORM_PIXEL_INTERVAL = 3;

        private readonly SequencerViewModel sequencer;
        private readonly AudioPlayback audioPlayback = new AudioPlayback();

        private CancellationTokenSource cts = null;

        private Waveform _currentWaveform = null;
        private bool _isLoading = false;

        public Waveform CurrentWaveform { get { return _currentWaveform; } set { SetProperty(ref _currentWaveform, value); } }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }

        public PlaybackViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;

            ForwardPropertyEvents(nameof(sequencer.TimePixelScale), sequencer, () =>
            {
                if(CurrentWaveform != null)
                    RenderWaveformAsync(true).Forget();
            });
        }

        public async Task LoadFileAsync(string fileName)
        {
            CurrentWaveform = null;
            IsLoading = true;

            if (audioPlayback.IsPlaying)
            {
                audioPlayback.Stop();
                await Task.Delay(100); // without this, the audio playing thread apparently still tries to read from the disposed stream
            }
            audioPlayback.Load(fileName);

            if (audioPlayback.Stream != null)
            {
                await RenderWaveformAsync(false);
            }
        }

        private async Task RenderWaveformAsync(bool withDelay)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();

            if (withDelay)
                await Task.Delay(300, cts.Token);

            IsLoading = true;
            audioPlayback.Stop(); // reset, note that this needs to be separate from playback
            Waveform result = await WaveformGenerator.CreateWaveformAsync(audioPlayback.Stream, sequencer.TimePixelScale / WAVEFORM_PIXEL_INTERVAL, cts.Token);
            Debug.WriteLine($"Time per sample: {result.TimePerSample}, Sample count: {result.Minimums.Length}");

            CurrentWaveform = result;
            IsLoading = false;
        }
    }
}
