using GlowSequencer.Audio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class PlaybackViewModel : Observable
    {
        private readonly SequencerViewModel sequencer;
        private readonly AudioPlayback audioPlayback = new AudioPlayback();

        private string _maximums = "";
        public string Maximums { get { return _maximums; } set { SetProperty(ref _maximums, value); } }

        public PlaybackViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;

            audioPlayback.MaximumCalculated += onMaximumCalculated;
        }

        private void onMaximumCalculated(object sender, MaxSampleEventArgs e)
        {
            Maximums += $" [{e.MinSample}, {e.MaxSample}]";
        }

        public void LoadFile(string fileName)
        {
            audioPlayback.Load(fileName);
            audioPlayback.Play();
        }
    }
}
