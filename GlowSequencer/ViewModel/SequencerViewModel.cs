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
        private bool _enableSmartInsert = true;
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
        public PlaybackViewModel Playback{ get; private set; }

        public ReadOnlyContinuousCollection<MusicSegmentViewModel> MusicSegments { get; private set; }
        public ReadOnlyContinuousCollection<TrackViewModel> Tracks { get; private set; }
        public ReadOnlyContinuousCollection<BlockViewModel> AllBlocks { get; private set; }
        
        public TrackViewModel SelectedTrack { get { return _selectedTrack; } set { SetProperty(ref _selectedTrack, value); } }
        public ObservableCollection<BlockViewModel> SelectedBlocks { get; private set; }
        
        public float CursorPosition { get { return _cursorPosition; } set { SetProperty(ref _cursorPosition, Math.Max(0, value)); } }
        public TimeUnit CursorPositionComplex { get { return TimeUnit.WrapAbsolute(_cursorPosition, _activeMusicSegment.GetModel(), v => CursorPosition = v); } }

        public double CursorPixelPosition { get { return _cursorPosition * TimePixelScale; } set { CursorPosition = (float)(value / TimePixelScale); } }


        /// <summary>Horizontal zoom level in pixels per second.</summary>
        public float TimePixelScale { get { return _timePixelScale; } set { SetProperty(ref _timePixelScale, Math.Max(value, 1 / 60.0f)); } }

        public float CurrentViewLeftPositionTime => (float)_viewportLeftOffsetPx / TimePixelScale;
        public float CurrentViewRightPositionTime => (float)(_viewportLeftOffsetPx + _viewportWidthPx) / TimePixelScale;
        public TimeUnit CurrentViewLeftPositionComplex { get { return TimeUnit.WrapAbsolute(CurrentViewLeftPositionTime, _activeMusicSegment.GetModel()); } }
        public TimeUnit CurrentViewRightPositionComplex { get { return TimeUnit.WrapAbsolute(CurrentViewRightPositionTime, _activeMusicSegment.GetModel()); } }
        public double CurrentWinWidth { get { return _currentWinWidth; } set { SetProperty(ref _currentWinWidth, value); } }
        
        public double TimelineWidth
        {
            get
            {
                float maxTime = Math.Max(
                    AllBlocks.Max(b => (float?)b.EndTimeOccupied).GetValueOrDefault(0),
                    Playback.MusicDuration);
                double newWidth = Math.Max(CurrentWinWidth, maxTime * TimePixelScale + 200);
                double stepSize = CurrentWinWidth / 2;
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
        public bool EnableSmartInsert { get { return _enableSmartInsert; } set { SetProperty(ref _enableSmartInsert, value); } }

        // visibility flags for the ConvertToTrack command
        public bool CanConvertToColor => SelectedBlocks.Any(b => b is RampBlockViewModel);
        public bool CanConvertToRamp => SelectedBlocks.Any(b => b is ColorBlockViewModel);
        public bool CanConvertToAutoDeduced => CanConvertToColor ^ CanConvertToRamp; // if type has been left out, it needs to be deducible which one is intended
        public string ConvertAutoDeduceGestureText => (CanConvertToAutoDeduced ? "Ctrl+B" : "");

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
            Playback = new PlaybackViewModel(this);

            Action<BlockViewModel> fn_SubscribeToBlock = bvm => ForwardPropertyEvents("EndTime", bvm, nameof(TimelineWidth));
            AllBlocks.ToList().ForEach(fn_SubscribeToBlock);
            AllBlocks.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null) e.NewItems.Cast<BlockViewModel>().ToList().ForEach(fn_SubscribeToBlock);
                Notify(nameof(TimelineWidth));
            };

            ForwardPropertyEvents("CursorPosition", this, "CursorPixelPosition", "CursorPositionComplex");
            ForwardPropertyEvents("TimePixelScale", this, "CursorPixelPosition",
                                                          "CurrentViewLeftPositionTime", "CurrentViewRightPositionTime",
                                                          "CurrentViewLeftPositionComplex", "CurrentViewRightPositionComplex",
                                                          nameof(TimelineWidth), "GridInterval");
            ForwardPropertyEvents("ActiveMusicSegment", this, "CursorPositionComplex", "CurrentViewLeftPositionComplex", "CurrentViewRightPositionComplex", "GridInterval");

            ForwardPropertyEvents("CurrentWinWidth", this, nameof(TimelineWidth));
            ForwardPropertyEvents(nameof(Playback.MusicDuration), Playback, nameof(TimelineWidth));

            ForwardCollectionEvents(SelectedBlocks, nameof(CanConvertToColor), nameof(CanConvertToRamp), nameof(CanConvertToAutoDeduced), nameof(ConvertAutoDeduceGestureText));

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
            Notify("CurrentViewLeftPositionTime");
            Notify("CurrentViewRightPositionTime");
            Notify("CurrentViewLeftPositionComplex");
            Notify("CurrentViewRightPositionComplex");
        }

        /// <summary>
        /// Called after undo/redo to make sure that no deleted blocks are in the selection.
        /// </summary>
        public void SanityCheckSelectedBlocks()
        {
            // TODO: once we have a better ObservableCollection, use RemoveAll for better performance
            for (int i = SelectedBlocks.Count - 1; i >= 0; i--)
            {
                var block = SelectedBlocks[i].GetModel();
                if (!model.Blocks.Contains(block))
                    SelectedBlocks.RemoveAt(i);
            }
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
                        // make sel equal to newBlocks
                        for (int i = sel.Count - 1; i >= 0; i--)
                        {
                            if (!newBlocks.Contains(sel[i]))
                                sel.RemoveAt(i);
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
            // inherit color and tracks from previous block, if applicable
            GloColor prevColor = GloColor.White;
            Track[] prevTracks = { _selectedTrack.GetModel() };

            if (EnableSmartInsert)
            {
                Func<BlockViewModel, bool> fnIsBlockApplicable =
                    (bl => bl.StartTime < CursorPosition && (bl is ColorBlockViewModel || bl is RampBlockViewModel));

                var prevBlocks = ((IEnumerable<BlockViewModel>)_selectedTrack.Blocks).Where(fnIsBlockApplicable);
                if (prevBlocks.Any())
                {
                    BlockViewModel prevBlock = prevBlocks.MaxBy(bl => bl.EndTimeOccupied);

                    // inherit color
                    if (prevBlock is ColorBlockViewModel)
                        prevColor = ((ColorBlockViewModel)prevBlock).GetModel().Color;
                    else
                        prevColor = ((RampBlockViewModel)prevBlock).GetModel().EndColor;

                    // inherit tracks, but only if the last block on the selected track is also the last block on all other tracks of the block
                    bool lastOfAllTracks = prevBlock.GetModel().Tracks.All(t => t.Blocks
                                            .Select(bl => BlockViewModel.FromModel(this, bl))
                                            .Where(fnIsBlockApplicable)
                                            .MaxBy(bl => bl.EndTimeOccupied)
                                        == prevBlock);
                    if (lastOfAllTracks)
                        prevTracks = prevBlock.GetModel().Tracks.ToArray();
                }
            }

            Block b;
            switch (type)
            {
                case "color":
                    b = new ColorBlock(model, prevTracks)
                    {
                        Color = prevColor
                    };
                    break;
                case "ramp":
                    b = new RampBlock(model, prevTracks)
                    {
                        StartColor = prevColor,
                        EndColor = (prevColor == GloColor.White ? GloColor.Black : GloColor.White)
                    };
                    break;
                default:
                    throw new ArgumentException("unsupported block type " + type);
            }

            b.SegmentContext = ActiveMusicSegment.GetModel();
            b.StartTime = CursorPosition;
            b.Duration = GridInterval;

            ActionManager.RecordAdd(model.Blocks, b);
        }

        // type may be null, then the target will be auto-inferred based on currently selected blocks
        public void ConvertSelectedBlocksTo(string type)
        {
            bool rampsToColors = (type == "color");
            bool colorsToRamps = (type == "ramp");
            if (!rampsToColors && !colorsToRamps)
            {
                colorsToRamps = SelectedBlocks.Any(b => b is ColorBlockViewModel);
                rampsToColors = SelectedBlocks.Any(b => b is RampBlockViewModel);
                if (colorsToRamps && rampsToColors) throw new ArgumentException("type needs to be given when both color and ramp blocks are selected");
            }

            using (ActionManager.CreateTransaction())
            {
                for (int i = 0; i < SelectedBlocks.Count; i++)
                {
                    var block = SelectedBlocks[i].GetModel();
                    Block convertedBlock;
                    if (rampsToColors && block is RampBlock rampBlock)
                    {
                        convertedBlock = new ColorBlock(model, rampBlock.Tracks.ToArray())
                        {
                            Color = (rampBlock.StartColor == GloColor.Black ? rampBlock.EndColor : rampBlock.StartColor)
                        };
                    }
                    else if (colorsToRamps && block is ColorBlock colorBlock)
                    {
                        convertedBlock = new RampBlock(model, colorBlock.Tracks.ToArray())
                        {
                            StartColor = colorBlock.Color,
                            EndColor = (colorBlock.Color == GloColor.White ? GloColor.Black : GloColor.White)
                        };
                    }
                    else { continue; }

                    // copy generic properties
                    convertedBlock.StartTime = block.StartTime;
                    convertedBlock.Duration = block.Duration;
                    convertedBlock.SegmentContext = block.SegmentContext;

                    ActionManager.RecordReplace(model.Blocks, block, convertedBlock);
                    SelectedBlocks[i] = BlockViewModel.FromModel(this, convertedBlock);
                }
            }
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
                // create an independant copy of the block so transforming its time to local reference frame does not screw up undo
                Block newChild = Block.FromXML(model, b.GetModel().ToXML());

                group.AddChild(newChild, true);
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
                        // create an independant copy of the block so transforming its time back to global reference frame does not screw up undo
                        Block independantBlock = Block.FromXML(model, b.ToXML());

                        independantBlock.StartTime += group.StartTime;
                        ActionManager.RecordRemove(group.Children, b);
                        ActionManager.RecordAdd(model.Blocks, independantBlock);

                        SelectBlock(BlockViewModel.FromModel(this, independantBlock), CompositionMode.Additive);
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
