using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ContinuousLinq;

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
            get { return !selectedBlocks.OfType<GroupBlockViewModel>().Any(); }
        }

        public ReadOnlyContinuousCollection<TrackAffiliationData> TrackAffiliation { get { return sequencer.Tracks.Select(t => new TrackAffiliationData(this, t)); } }

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

            ForwardPropertyEvents("SegmentContext", this, () =>
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

            Notify("IsActive");
            Notify("TypeLabel");
            Notify("SegmentContext");
            Notify("IsMusicSegmentModifiable");
            Notify("TrackAffiliation");
            Notify("StartTimeComplex");
            Notify("EndTimeComplex");
            Notify("DurationComplex");
            Notify("Color");
            Notify("StartColor");
            Notify("EndColor");
        }

        private void item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "SegmentContext": Notify("SegmentContext"); Notify("StartTimeComplex"); Notify("EndTimeComplex"); Notify("DurationComplex"); break;

                case "StartTime": Notify("StartTimeComplex"); break;
                case "EndTime": Notify("EndTimeComplex"); break;
                case "Duration": Notify("DurationComplex"); break;

                case "Color": Notify("Color"); break;
                case "StartColor": Notify("StartColor"); break;
                case "EndColor": Notify("EndColor"); break;
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
            foreach (BlockViewModel b in selectedBlocks)
                setter(b);
        }
        private void AggregateSet<B>(Action<B> setter) where B : BlockViewModel
        {
            foreach (B b in selectedBlocks.OfType<B>())
                setter(b);
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
            private SelectionProperties context;
            private TrackViewModel _track;

            public TrackViewModel Track { get { return _track; } }

            public bool? AffiliationState
            {
                get
                {
                    return (context.selectedBlocks.Any(b => b.GetModel().Tracks.Contains(_track.GetModel())) ?
                      (context.selectedBlocks.All(b => b.GetModel().Tracks.Contains(_track.GetModel())) ? true : (bool?)null) :
                      false);
                }
                set
                {
                    if (!value.HasValue)
                        return;

                    foreach (var b in context.selectedBlocks)
                        if (value.Value) b.AddToTrack(_track);
                        else b.RemoveFromTrack(_track);
                }
            }

            public bool CanModify
            {
                get { return !context.selectedBlocks.OfType<GroupBlockViewModel>().Any(); }
            }

            public TrackAffiliationData(SelectionProperties context, TrackViewModel track)
            {
                this.context = context;
                _track = track;

                CollectionChangedEventManager.AddHandler(track.Blocks, (sender, e) => Notify("AffiliationState"));
            }
        }
    }

}
