using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GlowSequencer.View
{
    public partial class MainWindow
    {
        private const int NOTE_DRAG_INITIAL_THRESHOLD = 10;

        private bool noteIsDragging = false;
        private Point noteDragStart = new Point();
        private bool noteDragNeedsToOvercomeThreshold = false;
        private float noteDragInitialTime = 0;
        
        // Also move cursor when clicking on header section of timeline, equivalent to clicking on waveform.
        private void notesCanvasClickableArea_MouseUp(object sender, MouseButtonEventArgs e) => waveform_MouseUp(sender, e);

        private void Note_QueryCursor(object sender, QueryCursorEventArgs e)
        {
            if (noteIsDragging)
                e.Cursor = Cursors.SizeAll;
        }

        private void Note_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var noteVm = (NoteViewModel)((FrameworkElement)sender).DataContext;

            // Double-click to edit.
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                SequencerCommands.EditNote.Execute(noteVm, sender as UIElement);
            }
            // Dragging.
            else if (e.ChangedButton == MouseButton.Right)
            {
                // Get to the actual list item, because for some reason capturing the mouse on the ContentPresenter
                // wrapper (ItemContainer) messes up the reported coordinates during MouseMove.
                var controlBlock = (FrameworkElement)VisualTreeHelper.GetChild((FrameworkElement)sender, 0);
                controlBlock.CaptureMouse();

                noteIsDragging = true;
                noteDragStart = e.GetPosition(notesContainer);
                noteDragNeedsToOvercomeThreshold = true;
                noteDragInitialTime = noteVm.TimeSeconds;
            }
        }

        private void Note_MouseMove(object sender, MouseEventArgs e)
        {
            if (noteIsDragging)
            {
                Vector delta = e.GetPosition(notesContainer) - noteDragStart;
                if (noteDragNeedsToOvercomeThreshold)
                {
                    if (Math.Abs(delta.X) < NOTE_DRAG_INITIAL_THRESHOLD)
                        return;
                    else
                        noteDragNeedsToOvercomeThreshold = false;
                }

                float deltaT = (float)(delta.X / sequencer.TimePixelScale);
                var noteVm = (NoteViewModel)((FrameworkElement)sender).DataContext;
                noteVm.TimeSeconds = SnapValue(noteDragInitialTime + deltaT);
            }
        }

        private void Note_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Note that this has to be in MouseUp, otherwise there is a quirk
                // when changing the scroll position leads to the mouse no longer being over the note
                // and the MouseUp of the waveform changing the cursor position again.
                var noteVm = (sender as FrameworkElement)?.DataContext as NoteViewModel;
                sequencer.CursorPosition = noteVm.TimeSeconds;
                ScrollCursorIntoView(ScrollIntoViewMode.Edge);
            }
            else if (noteIsDragging && e.ChangedButton == MouseButton.Right)
            {
                // Suppress context menu after drag.
                if (!noteDragNeedsToOvercomeThreshold)
                    e.Handled = true;

                var controlBlock = (FrameworkElement)VisualTreeHelper.GetChild((FrameworkElement)sender, 0);
                controlBlock.ReleaseMouseCapture();

                noteIsDragging = false;
                noteDragStart = new Point();
            }
        }
    }
}
