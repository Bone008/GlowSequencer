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

        private List<Block> cachedOrderedBlocks = null;
        private float lastCacheTime = -1;
        private int lastCacheIndex = 0;

        public ReadOnlyContinuousCollection<VisualizedTrackViewModel> VisualizedTracks { get; private set; }

        public VisualizationViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;

            VisualizedTracks = sequencer.GetModel().Tracks.Select(t => new VisualizedTrackViewModel(t));

            ForwardPropertyEvents(nameof(sequencer.Playback.IsPlaying), sequencer.Playback, OnIsPlayingChanged);
            ForwardPropertyEvents(nameof(sequencer.CursorPosition), sequencer, OnCursorPositionChanged, true);
        }

        private void OnIsPlayingChanged()
        {
            // Make sure we have the cached lookup available when playback is started.
            if (sequencer.Playback.IsPlaying)
                cachedOrderedBlocks = Enumerable.OrderBy(sequencer.GetModel().Blocks, b => b.StartTime).ToList();
            else
                cachedOrderedBlocks = null;

            lastCacheTime = -1;
            lastCacheIndex = 0;
        }

        private void OnCursorPositionChanged()
        {
            List<Block> orderedBlocks;
            if (cachedOrderedBlocks == null)
                // FIXME this breaks overlapping blocks sometimes!
                orderedBlocks = Enumerable.OrderBy(sequencer.GetModel().Blocks, b => b.StartTime).ToList();
            else
                orderedBlocks = cachedOrderedBlocks;

            float now = sequencer.CursorPosition;
            foreach (var vm in VisualizedTracks)
            {
                // TODO optimize
                Block activeBlock = orderedBlocks
                    .Where(b => b.IsTimeInOccupiedRange(now))
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