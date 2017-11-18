using ContinuousLinq;
using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public class VisualizedTrackViewModel : Observable
    {
        internal readonly Track track;
        private Color _currentColor = Colors.Black;

        public Color CurrentColor { get { return _currentColor; } set { SetProperty(ref _currentColor, value); } }

        public VisualizedTrackViewModel(Track track)
        {
            this.track = track;
        }
    }

    public class VisualizationViewModel : Observable
    {
        private readonly SequencerViewModel sequencer;

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
    }
}