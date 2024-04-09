using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ContinuousLinq;
using System.Collections.Specialized;
using System.Diagnostics;

namespace GlowSequencer.ViewModel
{

    public abstract class BlockViewModel : Observable
    {
        // Bad: this causes a hard dependency on WPF view. Not sure how to elegantly inject a reference into this otherwise, though :(
        private static readonly GlobalViewParameters globalParams = (GlobalViewParameters)Application.Current.FindResource("vm_Global");

        // TODO replace this with ConditionalWeakTable or other weak dictionary
        private static Dictionary<Model.Block, BlockViewModel> s_viewModelCache = new Dictionary<Model.Block, BlockViewModel>();

        public static BlockViewModel FromModel(SequencerViewModel sequencer, Model.Block model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            BlockViewModel vm;
            if (!s_viewModelCache.TryGetValue(model, out vm))
            {
                if (model is Model.ColorBlock)
                    vm = new ColorBlockViewModel(sequencer, (Model.ColorBlock)model);
                else if (model is Model.RampBlock)
                    vm = new RampBlockViewModel(sequencer, (Model.RampBlock)model);
                else if (model is Model.LoopBlock)
                    vm = new LoopBlockViewModel(sequencer, (Model.LoopBlock)model);
                //else if (model is Model.GroupBlock)
                //    vm = new GroupBlockViewModel(sequencer, (Model.GroupBlock)model);

                else
                    throw new NotImplementedException("unknown block type: " + model.GetType().Name);

                s_viewModelCache[model] = vm;

            }

            return vm;
        }




        protected readonly SequencerViewModel sequencer;
        protected readonly Model.Block model;

        protected readonly string _typeLabel;

        protected BlockViewModel(SequencerViewModel sequencer, Model.Block model, string typeLabel)
        {
            this.sequencer = sequencer;
            this.model = model;
            this._typeLabel = typeLabel;

            ForwardPropertyEvents(nameof(model.StartTime), model, nameof(StartTime), nameof(EndTime), nameof(EndTimeOccupied), nameof(DisplayOffset));
            ForwardPropertyEvents(nameof(model.Duration), model, nameof(Duration), nameof(EndTime), nameof(EndTimeOccupied), nameof(DisplayWidth));

            ForwardPropertyEvents(nameof(model.SegmentContext), model, nameof(SegmentContext), nameof(IsSegmentActive));
            // track affiliation
            ForwardCollectionEvents(model.Tracks, nameof(DisplayTopOffset), nameof(DisplayHeight), nameof(DisplayClip));
            // tracks in general
            // moved to OnTracksCollectionChanged(), called by the sequencer, because when this view model is constructed, the "Tracks" collection may still be under construction
            //CollectionChangedEventManager.AddHandler(sequencer.Tracks, (sender, e) => { Notify(nameof(DisplayTopOffset)); Notify(nameof(DisplayHeight)); Notify(nameof(DisplayClip)); });

            // subscribe to sequencer
            ForwardPropertyEvents(nameof(sequencer.ActiveMusicSegment), sequencer, nameof(IsSegmentActive));
            ForwardPropertyEvents(nameof(sequencer.TimePixelScale), sequencer, nameof(DisplayOffset), nameof(DisplayWidth));
            ForwardCollectionEvents(sequencer.SelectedBlocks, (sender, e) =>
            {
                if ((e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Contains(this))
                    || (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.Contains(this))
                    || e.Action == NotifyCollectionChangedAction.Replace
                    || e.Action == NotifyCollectionChangedAction.Reset)
                {
                    Notify(nameof(IsSelected));
                }
            });

            // subscribe to track height changes
            if (globalParams != null)
                ForwardPropertyEvents(nameof(globalParams.TrackDisplayHeight), globalParams, NotifyTrackRelatedProperties);
        }

        public string TypeLabel => _typeLabel;

        public float StartTime { get { return model.StartTime; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.StartTime, value); } }
        public float Duration { get { return model.Duration; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.Duration, value); } }

        public float EndTime
        {
            get { return StartTime + Duration; }
            set { Duration = value - StartTime; }
        }
        public float EndTimeOccupied => model.GetEndTimeOccupied();

        public virtual MusicSegmentViewModel SegmentContext
        {
            // note that null is no longer an expected value for SegmentContext
            get { return (model.SegmentContext == null ? null : sequencer.MusicSegments[model.SegmentContext.GetIndex()]); }
            set { sequencer.ActionManager.RecordSetProperty(model, m => m.SegmentContext, (value == null ? null : value.GetModel())); }
            //set { model.SegmentContext = (value == null ? null : value.GetModel()); }
        }

        public double DisplayOffset => StartTime * sequencer.TimePixelScale;
        public double DisplayTopOffset => model.Tracks.Min(t => t.GetIndex()) * globalParams.TrackDisplayHeight;
        public double DisplayWidth => Duration * sequencer.TimePixelScale;
        public double DisplayHeight => (model.Tracks.Max(t => t.GetIndex()) + 1) * globalParams.TrackDisplayHeight - DisplayTopOffset;

        public Geometry DisplayClip
        {
            get
            {
                int minIndex = model.Tracks.Min(t => t.GetIndex());
                var indices = Enumerable.OrderBy(model.Tracks, t => t.GetIndex()).Select(t => t.GetIndex() - minIndex).ToArray();

                Geometry geom = null;
                Action<int, int> makeRect = (from, to) =>
                    {
                        Rect r = new Rect(-10000, from * globalParams.TrackDisplayHeight, 20000, (to - from) * globalParams.TrackDisplayHeight);
                        if (geom == null) geom = new RectangleGeometry(r);
                        else geom = Geometry.Combine(geom, new RectangleGeometry(r), GeometryCombineMode.Union, null);
                    };

                int prev = 0, start = 0;
                for (int c = 0; c < indices.Length; c++)
                {
                    int index = indices[c];
                    if (index > prev + 1)
                    {
                        makeRect(start, prev + 1);
                        start = index;
                    }
                    prev = index;
                }

                // all tracks are a continuous piece --> no need to clip at all
                if (geom == null)
                    return null;

                makeRect(start, indices[indices.Length - 1] + 1);
                return geom;
            }
        }

        public bool IsSelected => sequencer.SelectedBlocks.Contains(this);
        public virtual bool IsSegmentActive => sequencer.ActiveMusicSegment == SegmentContext;

        public Model.Block GetModel()
        {
            return model;
        }

        public virtual void ScaleDuration(float factor)
        {
            Duration *= factor;
        }

        public virtual void OnTracksCollectionChanged()
        {
            NotifyTrackRelatedProperties();
        }

        private void NotifyTrackRelatedProperties()
        {
            Notify(nameof(DisplayTopOffset));
            Notify(nameof(DisplayHeight));
            Notify(nameof(DisplayClip));
        }

        public void AddToTrack(TrackViewModel track)
        {
            Debug.Assert(this is not GroupBlockViewModel);
            if (!model.Tracks.Contains(track.GetModel()))
            {
                sequencer.ActionManager.RecordAdd(model.Tracks, track.GetModel());
                //model.Tracks.Add(track.GetModel());
            }
        }

        public void RemoveFromTrack(TrackViewModel track)
        {
            Debug.Assert(this is not GroupBlockViewModel);
            if (model.Tracks.Count > 1 && model.Tracks.Contains(track.GetModel()))
            {
                sequencer.ActionManager.RecordRemove(model.Tracks, track.GetModel());
                //model.Tracks.Remove(track.GetModel());
            }
        }

    }
}
