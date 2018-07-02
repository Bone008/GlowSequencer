using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GlowSequencer.View
{
    public class MusicSegmentCommands
    {
        public static readonly RoutedUICommand AddSegment = new RoutedUICommand("", "AddSegment", typeof(SequencerCommands));
        public static readonly RoutedUICommand DeleteSegment = new RoutedUICommand("", "DeleteSegment", typeof(SequencerCommands), new InputGestureCollection { new KeyGesture(Key.Delete) });
        public static readonly RoutedUICommand SetAsDefault = new RoutedUICommand("", "SetAsDefault", typeof(SequencerCommands), new InputGestureCollection { new KeyGesture(Key.Enter, ModifierKeys.Alt) });
        public static readonly RoutedUICommand MoveSegmentByTime = new RoutedUICommand("", "MoveSegmentByTime", typeof(SequencerCommands));
        public static readonly RoutedUICommand SetOriginToCursor = new RoutedUICommand("", "SetOriginToCursor", typeof(SequencerCommands));
        public static readonly RoutedUICommand SelectReferringBlocks = new RoutedUICommand("", "SelectReferringBlocks", typeof(SequencerCommands));
    }

    public partial class MusicSegmentsWindow
    {
        private void CommandBinding_CanExecuteAlways(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_CanExecuteIfNotReadOnly(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (e.Parameter is MusicSegmentViewModel segment && !segment.IsReadOnly);
        }

        private void CommandBinding_CanExecuteIfNotDefault(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (e.Parameter is MusicSegmentViewModel segment && !segment.IsDefault);
        }

        private void CommandBinding_CanExecuteIfReferringBlocks(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (e.Parameter is MusicSegmentViewModel segment && segment.ReferringBlocksDummies.Any());
        }

        private void CommandBinding_ExecuteAddSegment(object sender, ExecutedRoutedEventArgs e)
        {
            MusicSegmentViewModel newSegment = sequencer.AddMusicSegment();
            segmentsListBox.SelectedItem = newSegment;
        }

        private void CommandBinding_ExecuteDeleteSegment(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter == null)
                return;

            MusicSegmentViewModel segment = (MusicSegmentViewModel)e.Parameter;

            if (segment.ReferringBlocksDummies.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(this, segment.ReferringBlocksDummies.Count + " blocks are referring to '" + segment.Label + "' and will have their music segment reset." + Environment.NewLine + "Are you sure?", "Confirmation", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            sequencer.DeleteMusicSegment(segment);
        }


        private void CommandBinding_ExecuteSetAsDefault(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter == null)
                return;

            sequencer.SetMusicSegmentAsDefault((MusicSegmentViewModel)e.Parameter);
        }



        private void CommandBinding_ExecuteMoveSegmentByTime(object sender, ExecutedRoutedEventArgs e)
        {
            MusicSegmentViewModel segment = e.Parameter as MusicSegmentViewModel;
            if (segment == null)
                throw new ArgumentException("invalid parameter");

            var result = Mastermind.ShowPromptTimeSpan(this, "Enter time delta", TimeSpan.Zero);
            if (!result.Success)
                return; // cancelled by user

            segment.TimeOrigin += result.Value;
        }

        private void CommandBinding_ExecuteSetOriginToCursor(object sender, ExecutedRoutedEventArgs e)
        {
            MusicSegmentViewModel segment = e.Parameter as MusicSegmentViewModel;
            if (segment == null)
                throw new ArgumentException("invalid parameter");

            segment.TimeOrigin = TimeSpan.FromSeconds(sequencer.CursorPosition);
        }

        private void CommandBinding_ExecuteSelectReferringBlocks(object sender, ExecutedRoutedEventArgs e)
        {
            MusicSegmentViewModel segment = e.Parameter as MusicSegmentViewModel;
            if (segment == null)
                throw new ArgumentException("invalid parameter");

            var blocks = sequencer.AllBlocks.Where(block => block.SegmentContext == segment);
            sequencer.SelectBlocks(blocks, CompositionMode.None);
        }

    }
}
