using ContinuousLinq;
using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public class VisualizationViewModel : Observable
    {
        private readonly SequencerViewModel sequencer;

        //public double StageWidth => 300;
        //public double StageHeight => 200;
        public ReadOnlyContinuousCollection<VisualizedTrackViewModel> VisualizedTracks { get; private set; }

        public VisualizationViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;
            VisualizedTracks = sequencer.GetModel().Tracks.Select(t => new VisualizedTrackViewModel(t));

            ForwardPropertyEvents(nameof(sequencer.CursorPosition), sequencer, OnCursorPositionChanged, true);
        }

        private void OnCursorPositionChanged()
        {
            float now = sequencer.CursorPosition;
            List<Block> blockCandidates = ((IEnumerable<Block>)sequencer.GetModel().Blocks)
                .Where(b => b.IsTimeInOccupiedRange(now))
                .ToList();

            foreach (var vm in VisualizedTracks)
            {
                Block activeBlock = blockCandidates
                    .Where(b => b.Tracks.Contains(vm.track))
                    .LastOrDefault();

                if (activeBlock == null)
                    vm.CurrentColor = Colors.Black;
                else
                    vm.CurrentColor = activeBlock.GetColorAtTime(now, vm.track).ToViewColor();
            }
        }




        [Obsolete("only for designer", true)]
        public VisualizationViewModel()
        {
            sequencer = null;
            var tl = new Timeline();
            VisualizedTracks = new ObservableCollection<Track> {
                new Track(tl, "Track 01"),
                new Track(tl, "Track 02"),
                new Track(tl, "Track 03"),
                new Track(tl, "Track 04"),
                new Track(tl, "Track 05"),
                new Track(tl, "Track 06"),
            }.Select(t => new VisualizedTrackViewModel(t));
        }
    }
}