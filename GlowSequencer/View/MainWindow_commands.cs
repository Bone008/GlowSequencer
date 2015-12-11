using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace GlowSequencer.View
{
    public static class SequencerCommands
    {
        public static readonly RoutedUICommand ExportGlo = new RoutedUICommand("", "ExportGlo", typeof(SequencerCommands), new InputGestureCollection { new KeyGesture(Key.E, ModifierKeys.Control) });

        public static readonly RoutedCommand InsertBlock = new RoutedCommand();
        public static readonly RoutedUICommand GroupBlocks = new RoutedUICommand("", "GroupBlocks", typeof(SequencerCommands), new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control) });
        public static readonly RoutedUICommand UngroupBlocks = new RoutedUICommand("", "UngroupBlocks", typeof(SequencerCommands), new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift) });
        public static readonly RoutedCommand MoveToFront = new RoutedCommand();
        public static readonly RoutedCommand MoveToBack = new RoutedCommand();

        public static readonly RoutedCommand SwapRampColors = new RoutedCommand();
        public static readonly RoutedCommand TrackAffiliationAll = new RoutedCommand();
        public static readonly RoutedCommand TrackAffiliationInvert = new RoutedCommand();

        public static readonly RoutedCommand AddTrack = new RoutedCommand();
        public static readonly RoutedCommand RenameTrack = new RoutedUICommand("", "RenameTrack", typeof(SequencerCommands), new InputGestureCollection { new KeyGesture(Key.F2) });
        public static readonly RoutedCommand DuplicateTrack = new RoutedCommand();
        public static readonly RoutedCommand DeleteTrack = new RoutedCommand();

        public static readonly RoutedUICommand MusicManageSegments = new RoutedUICommand("", "MusicManageSegments", typeof(SequencerCommands));


        public static readonly RoutedCommand About = new RoutedCommand();
    }

    public partial class MainWindow
    {
        private const string CLIPBOARD_BLOCKS_FORMAT = "glowsequencer.blocks";

        static MainWindow()
        {
            ApplicationCommands.SaveAs.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        }

        private void CommandBinding_CanExecuteAlways(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_CanExecuteUndo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = sequencer.ActionManager.CanUndo;
        }

        private void CommandBinding_CanExecuteRedo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = sequencer.ActionManager.CanRedo;
        }

        private void CommandBinding_CanExecuteIfSelected(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (sequencer.SelectedBlocks.Count > 0);
        }

        private void CommandBinding_CanExecuteIfClipboard(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsData(CLIPBOARD_BLOCKS_FORMAT);
        }

        private void CommandBinding_CanExecuteIfGroupable(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !sequencer.SelectedBlocks.All(b => b is GroupBlockViewModel);
        }

        private void CommandBinding_CanExecuteIfUngroupable(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = sequencer.SelectedBlocks.Any() && sequencer.SelectedBlocks.All(b => b is GroupBlockViewModel);
        }

        private void CommandBinding_CanExecuteIfMoreThanOneTrack(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (sequencer.Tracks.Count > 1);
        }

        private void CommandBinding_CanExecuteIfDeterminateGradient(object sender, CanExecuteRoutedEventArgs e)
        {
            ViewModel.SelectionProperties props = (ViewModel.SelectionProperties)e.Parameter;
            e.CanExecute = (props.StartColor != System.Windows.Media.Colors.Transparent && props.EndColor != System.Windows.Media.Colors.Transparent);
        }

        private void CommandBinding_CanExecuteIfTrackAffiliationNotAll(object sender, CanExecuteRoutedEventArgs e)
        {
            ViewModel.SelectionProperties props = (ViewModel.SelectionProperties)e.Parameter;
            e.CanExecute = (sequencer.SelectedBlocks.Any()
                            && props.TrackAffiliation.Any(aff => aff.CanModify && !aff.AffiliationState.GetValueOrDefault(false)));
        }



        private bool ConfirmUnchanged()
        {
            var result = MessageBox.Show("Do you want to save the changes made to the current document?", "Unsaved Changes", MessageBoxButton.YesNoCancel);

            switch (result)
            {
                case MessageBoxResult.Cancel: return false; // do not do operation
                case MessageBoxResult.No: return true; // do operation without saving
                case MessageBoxResult.Yes: return TrySave(); // save, then do operation if successful
                default: throw new NotImplementedException("unexpected message box result");
            }
        }

        private void CommandBinding_ExecuteClose(object sender, ExecutedRoutedEventArgs e)
        {
            // unsaved confimration dialog will be handled by the Closing event handler
            this.Close();
        }

        private void CommandBinding_ExecuteNew(object sender, ExecutedRoutedEventArgs e)
        {
            if (main.IsDirty && !ConfirmUnchanged())
                return;

            main.OpenNewDocument();
        }

        private void CommandBinding_ExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var diag = new Microsoft.Win32.OpenFileDialog();
            diag.DefaultExt = FileSerializer.EXTENSION_PROJECT;
            diag.Filter = string.Format("Glow Sequence (*{0})|*{0}|All files|*.*", FileSerializer.EXTENSION_PROJECT);
            diag.FilterIndex = 0;

            if (diag.ShowDialog(this).GetValueOrDefault(false))
            {
                if (main.IsDirty && !ConfirmUnchanged())
                    return;

                main.OpenDocument(diag.FileName);
                //Model.Timeline timeline = FileSerializer.LoadFromFile(diag.FileName);
                //DataContext = sequencer = new SequencerViewModel(timeline) { CurrentWinWidth = sequencer.CurrentWinWidth };
            }
        }

        private void CommandBinding_ExecuteSave(object sender, ExecutedRoutedEventArgs e)
        {
            TrySave();
        }
        private void CommandBinding_ExecuteSaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            TrySaveAs();
        }

        private bool TrySave()
        {
            if (main.FilePath != null)
            {
                main.SaveDocument();
                return true;
            }
            else
                return TrySaveAs();
        }

        private bool TrySaveAs()
        {
            var diag = new Microsoft.Win32.SaveFileDialog();
            diag.FileName = main.DocumentName;
            diag.AddExtension = true;
            diag.DefaultExt = FileSerializer.EXTENSION_PROJECT;
            diag.Filter = string.Format("Glow Sequence (*{0})|*{0}", FileSerializer.EXTENSION_PROJECT);
            diag.FilterIndex = 0;

            if (diag.ShowDialog(this).GetValueOrDefault(false))
            {
                main.SaveDocumentAs(diag.FileName);
                return true;
            }
            return false;
        }


        private void CommandBinding_ExecuteExportGlo(object sender, ExecutedRoutedEventArgs e)
        {
            string exportName = main.DocumentName;
            if (exportName.EndsWith(FileSerializer.EXTENSION_EXPORT, StringComparison.InvariantCultureIgnoreCase))
                exportName = exportName.Substring(0, exportName.Length - FileSerializer.EXTENSION_EXPORT.Length);

            var diag = new Microsoft.Win32.SaveFileDialog();
            diag.FileName = exportName + FileSerializer.EXTENSION_EXPORT;
            diag.AddExtension = true;
            diag.DefaultExt = FileSerializer.EXTENSION_EXPORT;
            diag.Filter = string.Format("Aerotech Ultimate Program (*{0})|*{0}", FileSerializer.EXTENSION_EXPORT);
            diag.FilterIndex = 0;

            if (diag.ShowDialog(this).GetValueOrDefault(false))
            {
                if (main.ExportProgram(diag.FileName))
                    MessageBox.Show(this, "Done!");
            }
        }


        private void CommandBinding_ExecuteUndo(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.ActionManager.Undo();
        }
        private void CommandBinding_ExecuteRedo(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.ActionManager.Redo();
        }

        private void CommandBinding_ExecuteSelectAll(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.SelectAllBlocks();
        }


        private void CommandBinding_ExecuteInsertBlock(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.InsertBlock((string)e.Parameter);
        }

        private void CommandBinding_ExecuteGroupBlocks(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.GroupSelectedBlocks();
        }

        private void CommandBinding_ExecuteUngroupBlocks(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.UngroupSelectedBlocks();
        }


        // TODO MoveToBack/Front could be automated by automagically sorting blocks by StartTime
        // this could probably be done most efficiently by reinserting (.Move!) blocks when they are dragged around
        private void CommandBinding_ExecuteMoveToFront(object sender, ExecutedRoutedEventArgs e)
        {
            Model.Timeline t = sequencer.GetModel();
            var toMove = sequencer.SelectedBlocks.Select(bvm => bvm.GetModel()).ToList();

            using (sequencer.ActionManager.CreateTransaction())
            {
                foreach (var block in toMove)
                {
                    sequencer.ActionManager.RecordRemove(t.Blocks, block);
                    sequencer.ActionManager.RecordAdd(t.Blocks, block);
                }
            }
        }

        private void CommandBinding_ExecuteMoveToBack(object sender, ExecutedRoutedEventArgs e)
        {
            Model.Timeline t = sequencer.GetModel();
            var toMove = sequencer.SelectedBlocks.Select(bvm => bvm.GetModel()).ToList();

            using (sequencer.ActionManager.CreateTransaction())
            {
                for (int i = 0; i < toMove.Count; i++)
                {
                    sequencer.ActionManager.RecordRemove(t.Blocks, toMove[i]);
                    sequencer.ActionManager.RecordInsert(t.Blocks, i, toMove[i]);
                }
            }
        }



        private void CommandBinding_ExecuteSwapRampColors(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.SelectionProperties props = (ViewModel.SelectionProperties)e.Parameter;

            using (sequencer.ActionManager.CreateTransaction())
            {
                var tmp = props.StartColor;
                props.StartColor = props.EndColor;
                props.EndColor = tmp;
            }
        }

        private void CommandBinding_ExecutedTrackAffiliationAll(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.SelectionProperties props = (ViewModel.SelectionProperties)e.Parameter;
            using (sequencer.ActionManager.CreateTransaction(false))
            {
                foreach (var aff in props.TrackAffiliation)
                    aff.AffiliationState = true;
            }
        }

        private void CommandBinding_ExecutedTrackAffiliationInvert(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.SelectionProperties props = (ViewModel.SelectionProperties)e.Parameter;
            using (sequencer.ActionManager.CreateTransaction(false))
            {
                var toTrue = props.TrackAffiliation.Where(aff => aff.AffiliationState.HasValue && !aff.AffiliationState.Value).ToList();
                var toFalse = props.TrackAffiliation.Where(aff => aff.AffiliationState.HasValue && aff.AffiliationState.Value).ToList();

                foreach (var aff in toTrue) aff.AffiliationState = true;
                foreach (var aff in toFalse) aff.AffiliationState = false;
            }
        }



        private void CommandBinding_ExecuteCut(object sender, ExecutedRoutedEventArgs e)
        {
            if (sequencer.SelectedBlocks.Count == 0)
                return;
            CopyToClipboard();
            sequencer.DeleteSelectedBlocks();
        }

        private void CommandBinding_ExecuteCopy(object sender, ExecutedRoutedEventArgs e)
        {
            if (sequencer.SelectedBlocks.Count == 0)
                return;
            CopyToClipboard();
        }

        private void CopyToClipboard()
        {
            XElement clipboard = new XElement("clipboard");
            clipboard.Add(sequencer.SelectedBlocks.Select(b => b.GetModel().ToXML()));

            var trackRefs = clipboard.Descendants("track-reference");
            var indices = trackRefs.Select(elem => (int)elem);
            int imin = indices.Min();
            int imax = indices.Max();

            int trackBaseline = sequencer.SelectedTrack.GetIndex();
            //clipboard.SetAttributeValue("relative-to-track", trackBaseline);

            // clamp baseline between min and max tracks
            trackBaseline = Math.Min(trackBaseline, imax);
            trackBaseline = Math.Max(trackBaseline, imin);

            // shift tracks to indices relative to baseline (may be negative)
            foreach (XElement tref in trackRefs)
                tref.Value = (int)tref - trackBaseline + "";

            string clipboardXml = clipboard.ToString(SaveOptions.DisableFormatting);
            Clipboard.SetData(CLIPBOARD_BLOCKS_FORMAT, clipboardXml);
        }

        private void CommandBinding_ExecutePaste(object sender, ExecutedRoutedEventArgs e)
        {
            string clipboardXml = Clipboard.GetData(CLIPBOARD_BLOCKS_FORMAT) as string;
            if (clipboardXml == null)
                return;

            XElement clipboard = XElement.Parse(clipboardXml);
            var trackRefs = clipboard.Descendants("track-reference");
            var indices = trackRefs.Select(elem => (int)elem);
            int oldFrom = indices.Min();
            int oldTo = indices.Max();

            //int trackBaseline = (int)clipboard.Attribute("relative-to-track");
            int trackBaseline = sequencer.SelectedTrack.GetIndex();

            // stay within range of the tracks
            trackBaseline = Math.Max(-oldFrom, trackBaseline);
            trackBaseline = Math.Min(sequencer.GetModel().Tracks.Count - 1 - oldTo, trackBaseline);

            if (oldFrom + trackBaseline < 0)
            {
                MessageBox.Show(this, "There are not enough tracks to fit the contents of the clipboard!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // shift relative indices back to absolute ones
            foreach (XElement tref in trackRefs)
                tref.Value = (int)tref + trackBaseline + "";

            Model.Block[] pastedBlocks = clipboard.Elements("block").Select(blockElem => Model.Block.FromXML(sequencer.GetModel(), blockElem)).ToArray();


            float timeBaseline = pastedBlocks.Min(b => b.StartTime);
            float timeDelta = sequencer.CursorPosition - timeBaseline;

            using (sequencer.ActionManager.CreateTransaction())
            {
                foreach (var block in pastedBlocks)
                {
                    // adjust to cursor position
                    block.StartTime += timeDelta;

                    sequencer.ActionManager.RecordAdd(sequencer.GetModel().Blocks, block);
                    //sequencer.GetModel().Blocks.Add(block);
                }
            }

            sequencer.SelectBlocks(pastedBlocks.Select(b => BlockViewModel.FromModel(sequencer, b)), false);
        }

        private void CommandBinding_ExecuteDelete(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.DeleteSelectedBlocks();
        }


        private void CommandBinding_ExecuteAddTrack(object sender, ExecutedRoutedEventArgs e)
        {
            TrackViewModel afterTrack = (e.Parameter == null ? null : (TrackViewModel)e.Parameter);
            sequencer.AddTrack(afterTrack);
        }

        private void CommandBinding_ExecuteRenameTrack(object sender, ExecutedRoutedEventArgs e)
        {
            TrackViewModel track = (e.Parameter != null ? (TrackViewModel)e.Parameter : sequencer.SelectedTrack);

            PromptWindow prompt = new PromptWindow("Rename track");
            prompt.Owner = this;
            prompt.PromptText = track.Label;
            prompt.AllowEmpty = false;

            if (prompt.ShowDialog() == true)
            {
                track.Label = prompt.PromptText;
            }
        }

        private void CommandBinding_ExecuteDuplicateTrack(object sender, ExecutedRoutedEventArgs e)
        {
            TrackViewModel track = (TrackViewModel)e.Parameter;
            sequencer.DuplicateTrack(track);
        }

        private void CommandBinding_ExecuteDeleteTrack(object sender, ExecutedRoutedEventArgs e)
        {
            TrackViewModel track = (TrackViewModel)e.Parameter;

            int deletedBlocks = track.Blocks.Where(b => b.GetModel().Tracks.Count == 1).Count();
            if (deletedBlocks > 0)
            {
                var result = MessageBox.Show(this, "Are you sure that you want to delete '" + track.Label + "' and its " + deletedBlocks + " blocks?", "Confirmation", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            sequencer.DeleteTrack(track);
        }


        private void CommandBinding_ExecuteMusicManageSegments(object sender, ExecutedRoutedEventArgs e)
        {
            Mastermind.OpenMusicSegmentsWindow();
        }

        private void CommandBinding_ExecuteAbout(object sender, ExecutedRoutedEventArgs e)
        {
            Mastermind.OpenAboutWindow();
        }

    }

}
