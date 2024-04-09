using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ContinuousLinq;
using GlowSequencer.Util;

namespace GlowSequencer.ViewModel
{

    public class SelectionProperties : Observable
    {

        private SequencerViewModel sequencer;
        private ObservableCollection<BlockViewModel> selectedBlocks;

        public bool IsActive { get { return selectedBlocks.Count > 0; } }

        public string TypeLabel { get { return AggregateGet(b => b.TypeLabel, "Mixed"); } }

        public MusicSegmentViewModel SegmentContext
        {
            get { return AggregateGet(b => b.SegmentContext); }
            set { AggregateSet(b => b.SegmentContext = value); }
        }

        public bool IsMusicSegmentModifiable
        {
            //get { return !selectedBlocks.OfType<GroupBlockViewModel>().Any(); }
            get { return true; }
        }

        public ReadOnlyContinuousCollection<TrackAffiliationData> TrackAffiliation { get; private set; }

        public TimeUnit StartTimeComplex { get { return AggregateTime(b => b.StartTime, (b, value) => b.StartTime = value, TimeUnit.WrapAbsolute); } }
        public TimeUnit EndTimeComplex { get { return AggregateTime(b => b.EndTime, (b, value) => b.EndTime = value, TimeUnit.WrapAbsolute); } }
        public TimeUnit DurationComplex { get { return AggregateTime(b => b.Duration, (b, value) => b.Duration = value, TimeUnit.Wrap); } }


        // Color blocks
        public Color Color
        {
            get { return AggregateGet<ColorBlockViewModel, Color>(b => b.Color, Colors.Transparent); }
            set { AggregateSet<ColorBlockViewModel>(b => b.Color = value); }
        }

        // Ramp blocks
        public Color StartColor
        {
            get { return AggregateGet<RampBlockViewModel, Color>(b => b.StartColor, Colors.Transparent); }
            set { AggregateSet<RampBlockViewModel>(b => b.StartColor = value); }
        }
        public Color EndColor
        {
            get { return AggregateGet<RampBlockViewModel, Color>(b => b.EndColor, Colors.Transparent); }
            set { AggregateSet<RampBlockViewModel>(b => b.EndColor = value); }
        }

        // Loop blocks
        public int? Repetitions
        {
            get { return AggregateGet<LoopBlockViewModel, int?>(b => b.Repetitions); }
            set { AggregateSet<LoopBlockViewModel>(b => b.Repetitions = value.Value); }
        }


        public SelectionProperties(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;

            selectedBlocks = sequencer.SelectedBlocks;
            selectedBlocks.CollectionChanged += selectedBlocks_CollectionChanged;

            TrackAffiliation = sequencer.Tracks.Select(t => new TrackAffiliationData(this, t));

            ForwardPropertyEvents(nameof(SegmentContext), this, () =>
            {
                if (!sequencer.SynchronizeActiveWithSelection || !IsMusicSegmentModifiable) return;
                var context = SegmentContext;
                if (context != null)
                    sequencer.ActiveMusicSegment = SegmentContext;
            });
        }

        private void selectedBlocks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
                // meh
                foreach (BlockViewModel item in sequencer.AllBlocks)
                    item.PropertyChanged -= item_PropertyChanged;
            else
            {
                if (e.OldItems != null)
                    foreach (BlockViewModel item in e.OldItems)
                        item.PropertyChanged -= item_PropertyChanged;

                if (e.NewItems != null)
                    foreach (BlockViewModel item in e.NewItems)
                        item.PropertyChanged += item_PropertyChanged;
            }

            Notify(nameof(IsActive));
            Notify(nameof(TypeLabel));
            Notify(nameof(SegmentContext));
            Notify(nameof(IsMusicSegmentModifiable));
            Notify(nameof(StartTimeComplex));
            Notify(nameof(EndTimeComplex));
            Notify(nameof(DurationComplex));
            Notify(nameof(Color));
            Notify(nameof(StartColor));
            Notify(nameof(EndColor));
        }

        private void item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(BlockViewModel.SegmentContext): Notify(nameof(SegmentContext)); Notify(nameof(StartTimeComplex)); Notify(nameof(EndTimeComplex)); Notify(nameof(DurationComplex)); break;

                case nameof(BlockViewModel.StartTime): Notify(nameof(StartTimeComplex)); break;
                case nameof(BlockViewModel.EndTime): Notify(nameof(EndTimeComplex)); break;
                case nameof(BlockViewModel.Duration): Notify(nameof(DurationComplex)); break;

                case nameof(ColorBlockViewModel.Color): Notify(nameof(Color)); break;
                case nameof(RampBlockViewModel.StartColor): Notify(nameof(StartColor)); break;
                case nameof(RampBlockViewModel.EndColor): Notify(nameof(EndColor)); break;

                case nameof(BlockViewModel.TrackNotificationPlaceholder):
                    foreach (TrackAffiliationData affiliation in TrackAffiliation)
                        affiliation.OnTrackBlocksChanged();
                    break;
            }
        }

        private T AggregateGet<B, T>(Func<B, T> selector, T def = default(T)) where B : BlockViewModel
        {
            T[] distinctValues = selectedBlocks.OfType<B>().Select(selector).Distinct().Take(2).ToArray();
            return (distinctValues.Length == 1 ? distinctValues[0] : def);
        }

        private T AggregateGet<T>(Func<BlockViewModel, T> selector, T def = default(T))
        {
            T[] distinctValues = selectedBlocks.Select(selector).Distinct().Take(2).ToArray();
            return (distinctValues.Length == 1 ? distinctValues[0] : def);
        }

        private void AggregateSet(Action<BlockViewModel> setter)
        {
            if (selectedBlocks.Count > 1)
            {
                using (sequencer.ActionManager.CreateTransaction())
                {
                    selectedBlocks.ForEach(setter);
                }
            }
            else selectedBlocks.ForEach(setter);
        }
        private void AggregateSet<B>(Action<B> setter) where B : BlockViewModel
        {
            if (selectedBlocks.Count > 1)
            {
                using (sequencer.ActionManager.CreateTransaction())
                {
                    selectedBlocks.OfType<B>().ForEach(setter);
                }
            }
            else selectedBlocks.OfType<B>().ForEach(setter);
        }

        private TimeUnit AggregateTime(Func<BlockViewModel, float> valueSelector, Action<BlockViewModel, float> valueSetter, Func<float?, Model.MusicSegment, Action<float>, TimeUnit> timeUnitFactory)
        {
            float[] values = selectedBlocks.Select(valueSelector).Distinct().Take(2).ToArray();
            Model.MusicSegment musicSegment = AggregateGet(b => b.GetModel().SegmentContext, sequencer.ActiveMusicSegment.GetModel());

            return timeUnitFactory((values.Length == 1 ? values[0] : (float?)null), musicSegment, v =>
            {
                foreach (BlockViewModel b in selectedBlocks)
                    valueSetter(b, v);
            });
        }




        public class TrackAffiliationData : Observable
        {
            private readonly SelectionProperties context;
            private readonly TrackViewModel _track;

            public TrackViewModel Track => _track;

            public bool? AffiliationState
            {
                get
                {
                    var flatBlocks = GetFlatBlocks();
                    if (!flatBlocks.Any(b => b.GetModel().Tracks.Contains(_track.GetModel())))
                        return false;
                    if (flatBlocks.All(b => b.GetModel().Tracks.Contains(_track.GetModel())))
                        return true;
                    return null;
                }
                set
                {
                    if (!value.HasValue)
                        return;
                    using (context.sequencer.ActionManager.CreateTransaction(false))
                    {
                        foreach (var b in GetFlatBlocks())
                        {
                            if (value.Value)
                                b.AddToTrack(_track);
                            else
                                b.RemoveFromTrack(_track);
                        }
                    }
                }
            }

            public TrackAffiliationData(SelectionProperties context, TrackViewModel track)
            {
                this.context = context;
                _track = track;

                ForwardCollectionEvents(context.selectedBlocks, nameof(AffiliationState));
            }

            internal void OnTrackBlocksChanged()
            {
                Notify(nameof(AffiliationState));
            }

            /// <summary>Returns the selected blocks, unpacking groups into a flat list.</summary>
            private IEnumerable<BlockViewModel> GetFlatBlocks()
            {
                return context.selectedBlocks
                    .SelectMany(b => b is GroupBlockViewModel group ? group.Children : Enumerable.Repeat(b, 1));
            }
        }
    }

}
