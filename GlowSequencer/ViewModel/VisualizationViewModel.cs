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
        private bool _isEnabled = true;

        public bool IsEnabled { get { return _isEnabled; } set { SetProperty(ref _isEnabled, value); } }

        //public double StageWidth => 300;
        //public double StageHeight => 200;
        public ReadOnlyContinuousCollection<VisualizedTrackViewModel> VisualizedTracks { get; private set; }

        public VisualizationViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;
            VisualizedTracks = sequencer.GetModel().Tracks.Select(t => new VisualizedTrackViewModel(t));

            ForwardPropertyEvents(nameof(sequencer.CursorPosition), sequencer, OnCursorPositionChanged, true);
            ForwardPropertyEvents(nameof(IsEnabled), this, OnIsEnabledChanged);
        }

        private void OnIsEnabledChanged()
        {
            if (IsEnabled)
            {
                OnCursorPositionChanged();
            }
            else
            {
                foreach(var vm in VisualizedTracks)
                {
                    vm.CurrentColor = Colors.Black;
                }
            }
        }

        private void OnCursorPositionChanged()
        {
            if (!IsEnabled)
                return;

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