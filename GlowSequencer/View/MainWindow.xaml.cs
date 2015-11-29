using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GlowSequencer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum BlockDragMode
        {
            None, Block, Start, End
        }
        private class DraggedBlockData
        {
            public BlockViewModel block;
            public float initialStartTime;
            public float initialDuration;
        }

        private const int DRAG_START_END_PIXEl_WINDOW = 6;
        private const int DRAG_START_END_PIXEl_WINDOW_TOUCH = 12;
        private const double TIMELINE_TRACK_HEIGHT = 70;


        private MainViewModel main;
        private SequencerViewModel sequencer { get { return main.CurrentDocument; } }

        private Point? selectionDragStart = null;
        private bool selectionIsDragging = false;
        private bool selectionIsAdditiveDrag = false;

        private BlockDragMode dragMode = BlockDragMode.None;
        private Point dragStart = new Point();
        private List<DraggedBlockData> draggedBlocks = null;


        public MainWindow()
        {
            InitializeComponent();
            main = (MainViewModel)DataContext;

            sequencer.SetViewportState(trackBlocksScroller.HorizontalOffset, trackBlocksScroller.ActualWidth);
        }

        private void trackLabelsScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            trackBlocksScroller.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private void trackBlocksScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            trackLabelsScroller.ScrollToVerticalOffset(e.VerticalOffset);

            if (e.HorizontalChange != 0)
            {
                timelineGrid.GridOffset = e.HorizontalOffset;
                sequencer.SetViewportState(trackBlocksScroller.HorizontalOffset, trackBlocksScroller.ActualWidth);
            }
        }

        private void timelineTrack_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TrackViewModel track = ((FrameworkElement)sender).DataContext as TrackViewModel;
            sequencer.SelectedTrack = track;
        }

        private void timelineTrackLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TrackViewModel track = ((FrameworkElement)sender).DataContext as TrackViewModel;

            // double click --> rename
            if (e.ClickCount == 2)
                SequencerCommands.RenameTrack.Execute(track, (FrameworkElement)sender);
        }




        private void timelineBlock_QueryCursor(object sender, QueryCursorEventArgs e)
        {
            switch (dragMode)
            {
                case BlockDragMode.Block:
                    e.Cursor = Cursors.SizeAll;
                    break;
                case BlockDragMode.Start:
                case BlockDragMode.End:
                    e.Cursor = Cursors.SizeWE;
                    break;

                case BlockDragMode.None:
                    FrameworkElement control = (FrameworkElement)sender;
                    BlockViewModel block = (BlockViewModel)control.DataContext;

                    FrameworkElement controlBlock = (FrameworkElement)VisualTreeHelper.GetChild(control, 0);
                    var localMouse = e.GetPosition(controlBlock);


                    // if the block start/end is already being dragged or if the mouse is at the left or right edge
                    if (localMouse.X < DRAG_START_END_PIXEl_WINDOW ||
                        (localMouse.X > controlBlock.ActualWidth - DRAG_START_END_PIXEl_WINDOW && localMouse.X < controlBlock.ActualWidth + DRAG_START_END_PIXEl_WINDOW))
                        e.Cursor = Cursors.SizeWE;

                    break;
            }
        }

        private void timelineBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right)
                return;

            FrameworkElement control = (FrameworkElement)sender;
            FrameworkElement controlBlock = (FrameworkElement)VisualTreeHelper.GetChild(control, 0);
            BlockViewModel block = (control.DataContext as BlockViewModel);

            var localMouse = e.GetPosition(controlBlock);

            BlockDragMode mode;
            if (e.RightButton == MouseButtonState.Pressed)
                mode = BlockDragMode.Block;
            else if (localMouse.X > controlBlock.ActualWidth - DRAG_START_END_PIXEl_WINDOW && localMouse.X < controlBlock.ActualWidth + DRAG_START_END_PIXEl_WINDOW)
                mode = BlockDragMode.End;
            else if (localMouse.X < DRAG_START_END_PIXEl_WINDOW)
                mode = BlockDragMode.Start;
            else
                mode = BlockDragMode.None;

            bool additive = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            if (mode == BlockDragMode.None)
            {
                IEnumerable<BlockViewModel> toSelect;
                if (e.ClickCount == 2 && block is ColorBlockViewModel)
                    toSelect = sequencer.AllBlocks.OfType<ColorBlockViewModel>().Where(other => other.Color == ((ColorBlockViewModel)block).Color);
                else if (e.ClickCount == 2 && block is RampBlockViewModel)
                    toSelect = sequencer.AllBlocks.OfType<RampBlockViewModel>().Where(other => other.StartColor == ((RampBlockViewModel)block).StartColor
                                                                                               && other.EndColor == ((RampBlockViewModel)block).EndColor);
                else
                    toSelect = Enumerable.Repeat(block, 1);

                sequencer.SelectBlocks(toSelect, additive);
            }
            else
            {
                if (!block.IsSelected)
                    sequencer.SelectBlock(block, additive);

                // record initial information
                dragMode = mode;
                dragStart = e.GetPosition(timeline); // always relative to timeline
                draggedBlocks = sequencer.SelectedBlocks.Select(b => new DraggedBlockData { block = b, initialDuration = b.Duration, initialStartTime = b.StartTime }).ToList();

                controlBlock.CaptureMouse();
                e.Handled = true;
            }

            //control.Focus();
        }

        private void timelineBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right)
                return;

            if (dragMode != BlockDragMode.None)
            {
                FrameworkElement control = (FrameworkElement)sender;
                FrameworkElement controlBlock = (FrameworkElement)VisualTreeHelper.GetChild(control, 0);
                //BlockViewModel block = (BlockViewModel)control.DataContext;

                // suppress context menu
                if (!dragStart.Equals(e.GetPosition(timeline)))
                    e.Handled = true;

                // this would be the place to record the undo/redo action (1 for all blocks)

                // reset drag state
                dragMode = BlockDragMode.None;
                dragStart = new Point();
                draggedBlocks = null;

                controlBlock.ReleaseMouseCapture();
            }
        }

        private void timelineBlock_MouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement control = (FrameworkElement)sender;
            FrameworkElement controlBlock = (FrameworkElement)VisualTreeHelper.GetChild(control, 0);
            BlockViewModel block = (BlockViewModel)control.DataContext;

            if (dragMode == BlockDragMode.None)
                return;
            if (!controlBlock.IsMouseCaptured) // the dragging was started by manipulation events (i guess)
                return;

            Vector delta = e.GetPosition(timeline) - dragStart;
            HandleDrag(block, delta.X);
        }


        private void timelineBlock_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            FrameworkElement control = (FrameworkElement)sender;
            FrameworkElement controlBlock = (FrameworkElement)VisualTreeHelper.GetChild(control, 0);
            BlockViewModel block = (control.DataContext as BlockViewModel);

            if (!block.IsSelected)
                sequencer.SelectBlock(block, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));

            var localPos = e.Manipulators.First().GetPosition(controlBlock);

            BlockDragMode mode;
            if (localPos.X > controlBlock.ActualWidth - DRAG_START_END_PIXEl_WINDOW_TOUCH && localPos.X < controlBlock.ActualWidth + DRAG_START_END_PIXEl_WINDOW_TOUCH)
                mode = BlockDragMode.End;
            else if (localPos.X < DRAG_START_END_PIXEl_WINDOW_TOUCH)
                mode = BlockDragMode.Start;
            else
                mode = BlockDragMode.Block;

            // record initial information
            dragMode = mode;
            dragStart = e.Manipulators.First().GetPosition(timeline); // always relative to timeline
            draggedBlocks = sequencer.SelectedBlocks.Select(b => new DraggedBlockData { block = b, initialDuration = b.Duration, initialStartTime = b.StartTime }).ToList();


            e.Mode = ManipulationModes.Translate;
            e.Handled = true;
            // TODO [low] handling ManipulationStarting prevents double tap (to select similar) from working with touch, but not handling it stops firing ManipulationCompleted (because of PanningMode of the scroller)
        }

        private void timelineBlock_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (dragMode == BlockDragMode.None)
                return;

            BlockViewModel block = ((sender as FrameworkElement).DataContext as BlockViewModel);

            // this would be the place to record the undo/redo action (1 for all blocks)

            // reset drag state
            dragMode = BlockDragMode.None;
            dragStart = new Point();
            draggedBlocks = null;

            e.Handled = true;
        }

        private void timelineBlock_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (dragMode == BlockDragMode.None)
                return;

            BlockViewModel block = ((sender as FrameworkElement).DataContext as BlockViewModel);
            // don't use CumulativeManipulation, that gets messed up when the block changes position
            HandleDrag(block, (e.Manipulators.First().GetPosition(timeline) - dragStart).X);

            e.Handled = true;
        }

        private int c = 1000;
        private void HandleDrag(BlockViewModel principal, double deltaPx)
        {
            float deltaT = (float)(deltaPx / sequencer.TimePixelScale);

            if (deltaT == 0)
                return;

            Debug.WriteLine(c++ + ": moving by " + deltaPx);

            DraggedBlockData principalData = draggedBlocks.Single(db => db.block == principal);

            // adjust delta according to constraints
            // - grid snapping on the principal
            // - repect min duration on all blocks
            // - non-negative start time on the earliest block
            float minDurationDelta, minStartTime, snappedStartTime;
            switch (dragMode)
            {
                case BlockDragMode.Start:
                    snappedStartTime = SnapValue(principalData.initialStartTime + deltaT);
                    deltaT = snappedStartTime - principalData.initialStartTime;

                    minDurationDelta = -draggedBlocks.Min(b => b.initialDuration - b.block.GetModel().GetMinDuration());
                    if (-deltaT < minDurationDelta)
                        deltaT = -minDurationDelta;

                    minStartTime = draggedBlocks.Min(b => b.initialStartTime);
                    if (deltaT < -minStartTime)
                        deltaT = -minStartTime;
                    break;

                case BlockDragMode.End:
                    float snappedEndTime = SnapValue(principalData.initialStartTime + principalData.initialDuration + deltaT);
                    deltaT = snappedEndTime - principalData.initialStartTime - principalData.initialDuration;

                    minDurationDelta = -draggedBlocks.Min(b => b.initialDuration - b.block.GetModel().GetMinDuration());
                    if (deltaT < minDurationDelta)
                        deltaT = minDurationDelta;
                    break;
                case BlockDragMode.Block:
                    snappedStartTime = SnapValue(principalData.initialStartTime + deltaT);
                    deltaT = snappedStartTime - principalData.initialStartTime;

                    minStartTime = draggedBlocks.Min(b => b.initialStartTime);
                    if (deltaT < -minStartTime)
                        deltaT = -minStartTime;
                    break;
            }


            using (sequencer.ActionManager.CreateTransaction())
            {
                // apply delta to all dragged blocks
                switch (dragMode)
                {
                    case BlockDragMode.Start:
                        foreach (var b in draggedBlocks)
                        {
                            b.block.StartTime = b.initialStartTime + deltaT;
                            b.block.Duration = b.initialDuration - deltaT;
                        }
                        break;

                    case BlockDragMode.End:
                        foreach (var b in draggedBlocks)
                        {
                            b.block.Duration = b.initialDuration + deltaT;
                        }
                        break;
                    case BlockDragMode.Block:
                        foreach (var b in draggedBlocks)
                        {
                            b.block.StartTime = b.initialStartTime + deltaT;
                        }
                        break;
                }
            }
        }

        private float SnapValue(float v)
        {
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                float interval = sequencer.GridInterval;
                float offset = sequencer.GetGridOffset();
                return (float)Math.Round((v - offset) / interval) * interval + offset;
            }
            else
                return v;
        }



        private void timeline_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            int trackIndex = (int)Math.Floor(e.GetPosition(timeline).Y / TrackViewModel.DISPLAY_HEIGHT);
            if (trackIndex >= 0 && trackIndex < sequencer.Tracks.Count)
                sequencer.SelectedTrack = sequencer.Tracks[trackIndex];
        }

        private void timeline_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right)
                return;

            sequencer.CursorPosition = SnapValue((float)e.GetPosition(timeline).X / sequencer.TimePixelScale);

            var originalDC = ((FrameworkElement)e.OriginalSource).DataContext;
            if (originalDC == null || !(originalDC is BlockViewModel))
            {
                sequencer.SelectBlock(null, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
            }

            selectionDragStart = e.GetPosition(timeline);
            timeline.CaptureMouse();
            //Debug.WriteLine("d: set start point");
        }

        private void timeline_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectionDragStart == null)
                return; // TODO why?

            if (selectionIsDragging)
            {
                Point p1 = e.GetPosition(timeline);
                Point p2 = selectionDragStart.Value;
                Rect r = new Rect(p1, p2);

                dragSelectionRect.Visibility = Visibility.Visible;
                Canvas.SetLeft(dragSelectionRect, r.Left);
                Canvas.SetTop(dragSelectionRect, r.Top);
                dragSelectionRect.Width = r.Width;
                dragSelectionRect.Height = r.Height;

                MultiSelectBlocks(r);
                e.Handled = true;
            }
            else if (selectionDragStart != null)
            {
                Vector delta = e.GetPosition(timeline) - selectionDragStart.Value;
                if (delta.LengthSquared > 10 * 10)
                {
                    selectionIsDragging = true;
                    selectionIsAdditiveDrag = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                    e.Handled = true;
                }
            }
        }

        private void timeline_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // for some reason this has to be above the if statement below
            // ReleaseMouseCapture() apparently instantly calls the MouseMove handler or sth
            if (selectionDragStart != null)
            {
                selectionDragStart = null;
                timeline.ReleaseMouseCapture();
            }
            if (selectionIsDragging)
            {
                selectionIsDragging = false;
                dragSelectionRect.Visibility = Visibility.Hidden;

                if (selectionIsAdditiveDrag)
                {
                    sequencer.ConfirmAdditiveSelection();
                    selectionIsAdditiveDrag = false;
                }
            }
        }

        private void timeline_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double mousePosPixels = e.GetPosition(timeline).X;
                double mousePosSeconds = mousePosPixels / sequencer.TimePixelScale;
                double offsetFromEdgePixels = e.GetPosition(trackBlocksScroller).X;

                double currLog = Math.Log(sequencer.TimePixelScale, 1.04f);
                sequencer.TimePixelScale = (float)Math.Pow(1.04f, currLog + e.Delta * 0.1f);

                trackBlocksScroller.ScrollToHorizontalOffset(mousePosSeconds * sequencer.TimePixelScale - offsetFromEdgePixels);

                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                trackBlocksScroller.ScrollToHorizontalOffset(trackBlocksScroller.HorizontalOffset + e.Delta);
            }
        }


        private void MultiSelectBlocks(Rect r)
        {
            double timeRangeStart = r.Left / sequencer.TimePixelScale;
            double timeRangeEnd = r.Right / sequencer.TimePixelScale;

            int trackRangeStart = (int)Math.Floor(r.Top / TIMELINE_TRACK_HEIGHT); // inclusive
            int trackRangeEnd = (int)Math.Ceiling(r.Bottom / TIMELINE_TRACK_HEIGHT); // exclusive

            sequencer.SelectBlocks(sequencer.AllBlocks
                .Where(b => timeRangeStart <= b.EndTimeOccupied && timeRangeEnd >= b.StartTime
                            && trackRangeStart <= b.GetModel().Tracks.Max(t => t.GetIndex()) && trackRangeEnd > b.GetModel().Tracks.Min(t => t.GetIndex())
                ),
                selectionIsAdditiveDrag
            );

            //Debug.WriteLine("[{0} - {1} | {2:0.00} - {3:0.00}]", trackRangeStart, trackRangeEnd, timeRangeStart, timeRangeEnd);
        }


        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is TextBox || e.OriginalSource is ComboBox || e.OriginalSource is CheckBox || e.OriginalSource is Slider || e.OriginalSource is MenuItem)
                return;

            switch (e.Key)
            {
                case Key.Home:
                    trackBlocksScroller.ScrollToLeftEnd();
                    sequencer.CursorPosition = 0;
                    e.Handled = true;
                    break;
                case Key.End:
                    trackBlocksScroller.ScrollToRightEnd();
                    sequencer.CursorPosition = sequencer.AllBlocks.Max(b => (float?)b.EndTimeOccupied).GetValueOrDefault(0);
                    e.Handled = true;
                    break;
                case Key.Left:
                    sequencer.CursorPosition -= (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? 2 / sequencer.TimePixelScale : sequencer.GridInterval);
                    e.Handled = true;
                    break;
                case Key.Right:
                    sequencer.CursorPosition += (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? 2 / sequencer.TimePixelScale : sequencer.GridInterval);
                    e.Handled = true;
                    break;
                case Key.Up:
                    sequencer.SelectRelativeTrack(-1);
                    e.Handled = true;
                    break;
                case Key.Down:
                    sequencer.SelectRelativeTrack(+1);
                    e.Handled = true;
                    break;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                sequencer.CurrentWinWidth = ActualWidth;
                sequencer.SetViewportState(trackBlocksScroller.HorizontalOffset, trackBlocksScroller.ActualWidth);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (main.IsDirty && !ConfirmUnchanged())
                e.Cancel = true;
        }

        private void statusBarTimeValue_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // at some point, double clicking the readonly values could convert them into a text field to adjust the view/cursor position by entering values;
            // the feature was postponed because it was deemed low priority; this is the start of its implementation
#if DEBUG
            // find first binding of a descendant
            DependencyObject dep = (DependencyObject)e.Source;
            BindingExpression binding = null;
            do
            {
                if(VisualTreeHelper.GetChildrenCount(dep) < 1)
                {
                    MessageBox.Show("Internal error: Could not locate property to modify!");
                    return;
                }
                dep = VisualTreeHelper.GetChild(dep, 0);
                if (dep is FrameworkElement)
                {
                    var fe = (FrameworkElement)dep;
                    binding = fe.GetBindingExpression(TextBlock.TextProperty) ?? fe.GetBindingExpression(Run.TextProperty);
                }
            } while (binding == null);

            MessageBox.Show(binding.ResolvedSourcePropertyName + " = " + binding.ResolvedSource);
#endif
        }

    }
}
