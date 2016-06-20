using ContinuousLinq;
using GlowSequencer.Model;
using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public enum CompositionMode { None, Additive, Subtractive }


    public class SequencerViewModel : Observable
    {
        private Timeline model;

        private MusicSegmentViewModel _activeMusicSegment;
        private bool _synchronizeActiveWithSelection = true;
        private bool _fadeAwayOtherBlocks = true;
        private bool _adjustBlocksWithSegmentChanges = true;
        private TrackViewModel _selectedTrack = null;

        private float _cursorPosition = 0;
        private float _timePixelScale = 100;

        // data from the view
        private double _viewportLeftOffsetPx = 0;
        private double _viewportWidthPx = 0;
        private double _currentWinWidth = 1000;
        //private double _horizontalTimelineOffset = 0;

        private ISet<BlockViewModel> temporaryDeltaSelectedBlocks = new HashSet<BlockViewModel>();

        public GuiLabs.Undo.ActionManager ActionManager { get; private set; }

        public SelectionProperties SelectionData { get; private set; }

        public ReadOnlyContinuousCollection<MusicSegmentViewModel> MusicSegments { get; private set; }

        public ReadOnlyContinuousCollection<TrackViewModel> Tracks { get; private set; }

        public ReadOnlyContinuousCollection<BlockViewModel> AllBlocks { get; private set; }


        public TrackViewModel SelectedTrack { get { return _selectedTrack; } set { SetProperty(ref _selectedTrack, value); } }

        public ObservableCollection<BlockViewModel> SelectedBlocks { get; private set; }


        public float CursorPosition { get { return _cursorPosition; } set { SetProperty(ref _cursorPosition, Math.Max(0, value)); } }
        public TimeUnit CursorPositionComplex { get { return TimeUnit.WrapAbsolute(_cursorPosition, _activeMusicSegment.GetModel(), v => CursorPosition = v); } }

        public double CursorPixelPosition { get { return _cursorPosition * TimePixelScale; } set { CursorPosition = (float)(value / TimePixelScale); } }


        public TimeUnit CurrentViewLeftPositionComplex { get { return TimeUnit.WrapAbsolute((float)_viewportLeftOffsetPx / TimePixelScale, _activeMusicSegment.GetModel()); } }
        public TimeUnit CurrentViewRightPositionComplex { get { return TimeUnit.WrapAbsolute((float)(_viewportLeftOffsetPx + _viewportWidthPx) / TimePixelScale, _activeMusicSegment.GetModel()); } }
        public double CurrentWinWidth { get { return _currentWinWidth; } set { SetProperty(ref _currentWinWidth, value); } }


        public double TimelineWidth
        {
            get
            {
                double stepSize = CurrentWinWidth / 2;
                double newWidth = Math.Max(CurrentWinWidth, AllBlocks.Max(b => (float?)b.EndTimeOccupied).GetValueOrDefault(0) * TimePixelScale + 200);
                //if (newWidth > _lastKnownTimelineWidth || newWidth + 100 < _lastKnownTimelineWidth)
                //    _lastKnownTimelineWidth = Math.Round(newWidth, -2);
                //return _lastKnownTimelineWidth;
                return Math.Ceiling(newWidth / stepSize) * stepSize;
            }
        }

        /// <summary>
        /// Gets the current interval of the grid lines in seconds.
        /// </summary>
        public float GridInterval
        {
            get
            {
                const float minGridInterval = 25;
                double minDurationPx = Model.Block.MIN_DURATION_TECHNICAL_LIMIT * TimePixelScale;
                double min = Math.Max(minDurationPx, minGridInterval);

                double intervalPx = TimePixelScale / ActiveMusicSegment.GetBeatsPerSecond();

                if (intervalPx < min)
                {
                    // magnify into bars, 4 bars, 16 bars, etc. (when zooming out)
                    intervalPx *= ActiveMusicSegment.BeatsPerBar;
                    while (intervalPx < min)
                        intervalPx *= 4;
                }
                else
                {
                    // subdivide beats (when zooming in)
                    while (intervalPx / 2 > min)
                        intervalPx /= 2;
                }

                return (float)(intervalPx / TimePixelScale); // convert from pixels to seconds
            }
        }


        public MusicSegmentViewModel ActiveMusicSegment { get { return _activeMusicSegment; } set { SetProperty(ref _activeMusicSegment, value); } }
        public bool SynchronizeActiveWithSelection { get { return _synchronizeActiveWithSelection; } set { SetProperty(ref _synchronizeActiveWithSelection, value); } }
        public bool FadeAwayOtherBlocks { get { return _fadeAwayOtherBlocks; } set { SetProperty(ref _fadeAwayOtherBlocks, value); } }
        public bool AdjustBlocksWithSegmentChanges { get { return _adjustBlocksWithSegmentChanges; } set { SetProperty(ref _adjustBlocksWithSegmentChanges, value); } }



        /// <summary>Pixels per second.</summary>
        public float TimePixelScale { get { return _timePixelScale; } set { SetProperty(ref _timePixelScale, Math.Max(value, 1 / 60.0f)); } }

        public SequencerViewModel(Timeline model)
        {
            this.model = model;

            ActionManager = new GuiLabs.Undo.ActionManager();
            SelectedBlocks = new ObservableCollection<BlockViewModel>();
            SelectionData = new SelectionProperties(this);
            Tracks = model.Tracks.Select(g => new TrackViewModel(this, g));
            MusicSegments = model.MusicSegments.Select(seg => new MusicSegmentViewModel(this, seg));
            AllBlocks = model.Blocks.Select(b => BlockViewModel.FromModel(this, b));

            if (Tracks.Count > 0)
                SelectedTrack = Tracks[0];

            ActiveMusicSegment = MusicSegments[model.DefaultMusicSegment.GetIndex()];


            Action<BlockViewModel> fn_SubscribeToBlock = bvm => ForwardPropertyEvents("EndTime", bvm, "TimelineWidth");
            AllBlocks.ToList().ForEach(fn_SubscribeToBlock);
            AllBlocks.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null) e.NewItems.Cast<BlockViewModel>().ToList().ForEach(fn_SubscribeToBlock);
                Notify("TimelineWidth");
            };

            ForwardPropertyEvents("CursorPosition", this, "CursorPixelPosition", "CursorPositionComplex");
            ForwardPropertyEvents("TimePixelScale", this, "CursorPixelPosition", "CurrentViewLeftPositionComplex", "CurrentViewRightPositionComplex",
                                                          "TimelineWidth", "GridInterval");
            ForwardPropertyEvents("ActiveMusicSegment", this, "CursorPositionComplex", "CurrentViewLeftPositionComplex", "CurrentViewRightPositionComplex", "GridInterval");

            ForwardPropertyEvents("CurrentWinWidth", this, "TimelineWidth");

            Tracks.CollectionChanged += (_, e) =>
            {
                foreach (var b in AllBlocks)
                    b.OnTracksCollectionChanged();
            };
        }

        public void NotifyGridInterval()
        {
            Notify("GridInterval");
        }

        public float GetGridOffset()
        {
            return ActiveMusicSegment.TimeOriginSeconds;
        }

        /// <summary>
        /// Called by the view to inform the VM about the state of the blocks viewport.
        /// </summary>
        public void SetViewportState(double viewportOffsetPx, double viewportWidth)
        {
            _viewportLeftOffsetPx = viewportOffsetPx;
            _viewportWidthPx = viewportWidth;
            Notify("CurrentViewLeftPositionComplex");
            Notify("CurrentViewRightPositionComplex");
        }

        // ===== Commands =====


        public void SelectBlock(BlockViewModel block, CompositionMode compositionMode)
        {
            Debug.WriteLine("single select (mode: {0}, block: {1})", compositionMode, block);

            switch (compositionMode)
            {
                case CompositionMode.None:
                    SelectedBlocks.Clear();
                    if (block != null && !SelectedBlocks.Contains(block))
                        SelectedBlocks.Add(block);
                    break;

                case CompositionMode.Additive:
                    if (block != null && !SelectedBlocks.Contains(block))
                        SelectedBlocks.Add(block);
                    break;

                case CompositionMode.Subtractive:
                    if (block != null)
                        SelectedBlocks.Remove(block);
                    break;
            }
        }

        // select multiple blocks, but as an atomic operation (not over time)
        public void SelectBlocks(IEnumerable<BlockViewModel> collection, CompositionMode compositionMode)
        {
            SelectBlocksDelta(collection, compositionMode);
            ConfirmSelectionDelta();
        }

        public void SelectBlocksDelta(IEnumerable<BlockViewModel> collection, CompositionMode compositionMode)
        {
            Debug.WriteLine("multiselect (mode: {0}, blocks: {1})", compositionMode, collection.Count());

            var sel = SelectedBlocks;

            switch (compositionMode)
            {
                case CompositionMode.None:
                    ISet<BlockViewModel> newBlocks = new HashSet<BlockViewModel>(collection);
                    if (newBlocks.Count == 0)
                    {
                        sel.Clear();
                    }
                    else
                    {
                        foreach (BlockViewModel b in sel.ToArray())
                        {
                            if (!newBlocks.Contains(b))
                                sel.Remove(b);
                        }
                        foreach (BlockViewModel b in newBlocks)
                        {
                            if (!sel.Contains(b))
                                sel.Add(b);
                        }
                    }
                    break;

                case CompositionMode.Additive:
                    {
                        // delta set contains all blocks that are part of the additive selection
                        // calculate necessary changes
                        var toDeselect = temporaryDeltaSelectedBlocks.Except(collection).ToList();
                        var toSelect = collection.Except(temporaryDeltaSelectedBlocks).Except(sel).ToList();

                        foreach (BlockViewModel b in toDeselect)
                        {
                            sel.Remove(b);
                            temporaryDeltaSelectedBlocks.Remove(b);
                        }
                        foreach (BlockViewModel b in toSelect)
                        {
                            sel.Add(b);
                            temporaryDeltaSelectedBlocks.Add(b);
                        }
                    }
                    break;

                case CompositionMode.Subtractive:
                    {
                        // delta set contains all blocks that are part of the subtractive selection
                        // calculate necessary changes
                        var toDeselect = collection.Except(temporaryDeltaSelectedBlocks).Intersect(sel).ToList();
                        var toSelect = temporaryDeltaSelectedBlocks.Except(collection).ToList();

                        foreach (BlockViewModel b in toDeselect)
                        {
                            sel.Remove(b);
                            temporaryDeltaSelectedBlocks.Add(b);
                        }
                        foreach (BlockViewModel b in toSelect)
                        {
                            if (!sel.Contains(b)) sel.Add(b);
                            temporaryDeltaSelectedBlocks.Remove(b);
                        }
                    }
                    break;
            }
        }

        public void ConfirmSelectionDelta()
        {
            Debug.WriteLine("confirmed delta ({0} blocks)", temporaryDeltaSelectedBlocks.Count);
            temporaryDeltaSelectedBlocks.Clear();
        }

        public void SelectAllBlocks()
        {
            SelectedBlocks.Clear();
            foreach (BlockViewModel b in AllBlocks)
                SelectedBlocks.Add(b);
        }


        public void InsertBlock(string type)
        {
            GloColor prevColor = GloColor.White;

            var prevBlocks = ((IEnumerable<BlockViewModel>)_selectedTrack.Blocks).Where(bl => bl.StartTime < CursorPosition && (bl is ColorBlockViewModel || bl is RampBlockViewModel));
            if (prevBlocks.Any())
            {
                BlockViewModel prevBlock = prevBlocks.MaxBy(bl => bl.EndTimeOccupied);

                if (prevBlock is ColorBlockViewModel)
                    prevColor = ((ColorBlockViewModel)prevBlock).GetModel().Color;
                else
                    prevColor = ((RampBlockViewModel)prevBlock).GetModel().EndColor;
            }

            Block b;
            switch (type)
            {
                case "color":
                    b = new ColorBlock(model, _selectedTrack.GetModel())
                    {
                        Color = prevColor
                    };
                    break;
                case "ramp":
                    b = new RampBlock(model, _selectedTrack.GetModel())
                    {
                        StartColor = prevColor,
                        EndColor = (prevColor == GloColor.White ? GloColor.Black : GloColor.White)
                    };
                    break;
                default:
                    throw new NotImplementedException("insertion of block type " + type);
            }

            b.SegmentContext = ActiveMusicSegment.GetModel();
            b.StartTime = CursorPosition;
            b.Duration = GridInterval;

            ActionManager.RecordAction(new GuiLabs.Undo.AddItemAction<Block>(model.Blocks.Add, bl => model.Blocks.Remove(bl), b));
            //model.Blocks.Add(b);
        }

        public void DeleteSelectedBlocks()
        {
            using (ActionManager.CreateTransaction())
            {
                foreach (var b in SelectedBlocks)
                    ActionManager.RecordAction(new GuiLabs.Undo.AddItemAction<Block>(bl => model.Blocks.Remove(bl), model.Blocks.Add, b.GetModel()));
                //model.Blocks.Remove(b.GetModel());
                SelectedBlocks.Clear();
            }
        }

        public void GroupSelectedBlocks()
        {
            if (!SelectedBlocks.Any())
                return;

            var relatedSegments = Enumerable.Select(SelectedBlocks, b => b.SegmentContext).Distinct().ToList();
            MusicSegment segmentForGroup = (relatedSegments.Count == 1 ? relatedSegments.Single().GetModel() : model.MusicSegments[0]);

            var group = new LoopBlock(model);
            group.SegmentContext = segmentForGroup;
            group.StartTime = SelectedBlocks.Min(b => b.StartTime);
            foreach (var b in SelectedBlocks)
            {
                group.AddChild(b.GetModel(), true);
            }

            using (ActionManager.CreateTransaction())
            {
                DeleteSelectedBlocks();
                ActionManager.RecordAdd(model.Blocks, group);
            }

            SelectBlock(BlockViewModel.FromModel(this, group), CompositionMode.None);
        }

        public void UngroupSelectedBlocks()
        {
            using (ActionManager.CreateTransaction())
            {
                foreach (var groupVM in SelectedBlocks.OfType<GroupBlockViewModel>().ToArray())
                {
                    SelectedBlocks.Remove(groupVM);

                    GroupBlock group = (Model.GroupBlock)groupVM.GetModel();
                    ActionManager.RecordRemove(model.Blocks, group);

                    foreach (Block b in group.Children.ToArray())
                    {
                        b.StartTime += group.StartTime;
                        ActionManager.RecordRemove(group.Children, b);
                        ActionManager.RecordAdd(model.Blocks, b);

                        SelectBlock(BlockViewModel.FromModel(this, b), CompositionMode.Additive);
                    }
                }
            }
        }


        public void SelectRelativeTrack(int delta)
        {
            int newIndex = SelectedTrack.GetModel().GetIndex() + delta;
            if (newIndex >= 0 && newIndex < Tracks.Count)
                SelectedTrack = Tracks[newIndex];
        }

        public void AddTrack(TrackViewModel afterTrack = null)
        {
            Track newTrack = new Track(model, model.DeriveTrackLabel(Track.DEFAULT_BASE_LABEL));

            ActionManager.RecordAction(() =>
            {
                if (afterTrack != null)
                    model.Tracks.Insert(afterTrack.GetModel().GetIndex() + 1, newTrack);
                else
                    model.Tracks.Add(newTrack);
            },

            () => model.Tracks.Remove(newTrack));
        }

        public void DuplicateTrack(TrackViewModel trackVM)
        {
            Track oldTrack = trackVM.GetModel();
            Track newTrack = new Track(model, model.DeriveTrackLabel(trackVM.Label));
            int insertIndex = oldTrack.GetIndex() + 1;

            using (ActionManager.CreateTransaction(false))
            {
                ActionManager.RecordInsert(model.Tracks, insertIndex, newTrack);
                //model.Tracks.Insert(insertIndex, newTrack);

                foreach (var block in oldTrack.Blocks)
                    block.ExtendToTrack(oldTrack, newTrack, ActionManager);
                //ActionManager.RecordAdd(block.Tracks, newTrack);
                //block.Tracks.Add(newTrack);
            }
        }

        public void DeleteTrack(TrackViewModel trackVM)
        {
            if (Tracks.Count <= 1)
                return;

            Track track = trackVM.GetModel();

            using (ActionManager.CreateTransaction(false))
            {
                // remove all blocks from the track
                foreach (var block in track.Blocks.ToArray())
                {
                    if (block.Tracks.Count > 1)
                        block.RemoveFromTrack(track, ActionManager);
                    else
                        ActionManager.RecordRemove(model.Blocks, block);

                    //block.Tracks.Remove(track);
                    //if (block.Tracks.Count == 0)
                    //    ActionManager.RecordRemove(model.Blocks, block);
                    //model.Blocks.Remove(block);
                }

                // deselect
                if (SelectedTrack == trackVM)
                    SelectedTrack = Tracks[trackVM.GetIndex() + (trackVM.GetIndex() < Tracks.Count - 1 ? 1 : -1)];

                ActionManager.RecordRemove(model.Tracks, track);
                //model.Tracks.Remove(track);
            }
        }


        public MusicSegmentViewModel AddMusicSegment()
        {
            var newSegment = new MusicSegment(model) { Label = "Unnamed", Bpm = 120, BeatsPerBar = 4, TimeOrigin = 0 };
            model.MusicSegments.Add(newSegment);

            // should be logically equivalent to MusicSegments.Last()
            return MusicSegments.Single(segVm => segVm.GetModel() == newSegment);
        }

        public void DeleteMusicSegment(MusicSegmentViewModel segmentVM)
        {
            MusicSegment segment = segmentVM.GetModel();

            if (segment.GetIndex() == 0)
                return;

            foreach (var b in model.Blocks)
            {
                if (b.SegmentContext == segment)
                    b.SegmentContext = model.MusicSegments[0];
            }

            if (model.DefaultMusicSegment == segment)
                model.DefaultMusicSegment = model.MusicSegments[0];
            if (ActiveMusicSegment == segmentVM)
                ActiveMusicSegment = MusicSegments[model.DefaultMusicSegment.GetIndex()];

            model.MusicSegments.Remove(segment);
        }

        public void SetMusicSegmentAsDefault(MusicSegmentViewModel segmentVM)
        {
            model.DefaultMusicSegment = segmentVM.GetModel();
        }





        public Timeline GetModel()
        {
            return model;
        }
    }

}
