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




        protected SequencerViewModel sequencer;
        protected Model.Block model;

        protected readonly string _typeLabel;

        protected BlockViewModel(SequencerViewModel sequencer, Model.Block model, string typeLabel)
        {
            this.sequencer = sequencer;
            this.model = model;
            this._typeLabel = typeLabel;

            //Tracks = model.Tracks.Select(g => new TrackViewModel(sequencer, g));

            ForwardPropertyEvents(nameof(model.StartTime), model, nameof(StartTime), nameof(EndTime), nameof(DisplayOffset));
            //ForwardPropertyEvents(nameof(model.StartTime), model, () => { foreach (var g in sequencer.Tracks) g.OnMaxBlockExtentChanged(); });

            ForwardPropertyEvents(nameof(model.Duration), model, nameof(Duration), nameof(EndTime), nameof(DisplayWidth));
            //ForwardPropertyEvents(nameof(model.Duration), model, () => { foreach (var g in sequencer.Tracks) g.OnMaxBlockExtentChanged(); });

            ForwardPropertyEvents(nameof(model.SegmentContext), model, nameof(SegmentContext), nameof(IsSegmentActive));
            //ForwardPropertyEvents(nameof(model.SegmentContext), model, () =>
            //{
            //    ForwardPropertyEvents("Bpm", model.SegmentContext, nameof(StartTimeComplex), nameof(EndTimeComplex), nameof(DurationComplex));
            //    ForwardPropertyEvents("BeatsPerBar", model.SegmentContext, nameof(StartTimeComplex), nameof(EndTimeComplex), nameof(DurationComplex));
            //    ForwardPropertyEvents("TimeOrigin", model.SegmentContext, nameof(StartTimeComplex), nameof(EndTimeComplex));
            //}, true);

            // track affiliation
            CollectionChangedEventManager.AddHandler(model.Tracks, (sender, e) => { Notify(nameof(DisplayTopOffset)); Notify(nameof(DisplayHeight)); Notify(nameof(DisplayClip)); });
            // tracks in general
            // moved to OnTracksCollectionChanged(), called by the sequencer, because when this view model is constructed, the "Tracks" collection may still be under construction
            //CollectionChangedEventManager.AddHandler(sequencer.Tracks, (sender, e) => { Notify(nameof(DisplayTopOffset)); Notify(nameof(DisplayHeight)); Notify(nameof(DisplayClip)); });

            // subscribe to sequencer
            ForwardPropertyEvents(nameof(sequencer.ActiveMusicSegment), sequencer, nameof(IsSegmentActive));
            ForwardPropertyEvents(nameof(sequencer.TimePixelScale), sequencer, nameof(DisplayOffset), nameof(DisplayWidth));
            
            sequencer.SelectedBlocks.CollectionChanged += (sender, e) =>
            {
                if ((e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Contains(this))
                    || (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.Contains(this))
                    || e.Action == NotifyCollectionChangedAction.Replace
                    || e.Action == NotifyCollectionChangedAction.Reset)
                {
                    Notify(nameof(IsSelected));
                }
            };
        }

        public string TypeLabel { get { return _typeLabel; } }


        public float StartTime { get { return model.StartTime; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.StartTime, value); } }
        public float Duration { get { return model.Duration; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.Duration, value); } }

        public float EndTime
        {
            get { return StartTime + Duration; }
            set { Duration = value - StartTime; }
        }

        public virtual float EndTimeOccupied
        {
            get { return EndTime; }
        }


        public virtual MusicSegmentViewModel SegmentContext
        {
            // note that null is no longer an expected value for SegmentContext
            get { return (model.SegmentContext == null ? null : sequencer.MusicSegments[model.SegmentContext.GetIndex()]); }
            set { sequencer.ActionManager.RecordSetProperty(model, m => m.SegmentContext, (value == null ? null : value.GetModel())); }
            //set { model.SegmentContext = (value == null ? null : value.GetModel()); }
        }


        public double DisplayOffset
        {
            get { return StartTime * sequencer.TimePixelScale; }
        }

        public double DisplayTopOffset
        {
            get { return model.Tracks.Min(t => t.GetIndex()) * TrackViewModel.DISPLAY_HEIGHT; }
        }

        public double DisplayWidth
        {
            get { return Duration * sequencer.TimePixelScale; }
        }

        public double DisplayHeight
        {
            get { return (model.Tracks.Max(t => t.GetIndex()) + 1) * TrackViewModel.DISPLAY_HEIGHT - DisplayTopOffset; }
        }

        public Geometry DisplayClip
        {
            get
            {
                int minIndex = model.Tracks.Min(t => t.GetIndex());
                var indices = Enumerable.OrderBy(model.Tracks, t => t.GetIndex()).Select(t => t.GetIndex() - minIndex).ToArray();

                Geometry geom = null;
                Action<int, int> makeRect = (from, to) =>
                    {
                        Rect r = new Rect(-10000, from * TrackViewModel.DISPLAY_HEIGHT, 20000, (to - from) * TrackViewModel.DISPLAY_HEIGHT);
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

        public bool IsSelected
        {
            get { return sequencer.SelectedBlocks.Contains(this); }
        }

        public virtual bool IsSegmentActive
        {
            get { return sequencer.ActiveMusicSegment == SegmentContext; }
        }

        public float TmpLoopOffset { get; set; }


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
            Notify(nameof(DisplayTopOffset));
            Notify(nameof(DisplayHeight));
            Notify(nameof(DisplayClip));
        }

        public void AddToTrack(TrackViewModel track)
        {
            if (!model.Tracks.Contains(track.GetModel()))
            {
                sequencer.ActionManager.RecordAdd(model.Tracks, track.GetModel());
                //model.Tracks.Add(track.GetModel());
            }
        }

        public void RemoveFromTrack(TrackViewModel track)
        {
            if (model.Tracks.Count > 1)
            {
                sequencer.ActionManager.RecordRemove(model.Tracks, track.GetModel());
                //model.Tracks.Remove(track.GetModel());
            }
        }

    }
}
