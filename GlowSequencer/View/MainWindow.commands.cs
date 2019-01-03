using GlowSequencer.Util;
using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace GlowSequencer.View
{
    public static class SequencerCommands
    {
        // Note: RoutedUICommand is equal to RoutedCommand + a Text property (which is currently not used, though).
        private static RoutedCommand Make(InputGestureCollection gestures = null, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (gestures == null) gestures = new InputGestureCollection();
            return new RoutedUICommand(name, name, typeof(SequencerCommands), gestures);
        }

        public static readonly RoutedCommand ExportGlo = Make(new InputGestureCollection { new KeyGesture(Key.E, ModifierKeys.Control) });
        public static readonly RoutedCommand ShowTransferWindow = Make(new InputGestureCollection { new KeyGesture(Key.Enter, ModifierKeys.Control) });

        public static readonly RoutedCommand ReplaceColor = Make(new InputGestureCollection { new KeyGesture(Key.R, ModifierKeys.Control) });
        public static readonly RoutedCommand BrightMode = Make();

        public static readonly RoutedCommand InsertBlock = Make();
        public static readonly RoutedCommand ConvertToType = Make();
        public static readonly RoutedCommand GroupBlocks = Make(new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control) });
        public static readonly RoutedCommand UngroupBlocks = Make(new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift) });
        public static readonly RoutedCommand MoveToFront = Make();
        public static readonly RoutedCommand MoveToBack = Make();
        public static readonly RoutedCommand SplitAtCursor = Make();

        public static readonly RoutedCommand SwapRampColors = Make();
        public static readonly RoutedCommand TrackAffiliationAll = Make();
        public static readonly RoutedCommand TrackAffiliationInvert = Make();

        public static readonly RoutedCommand AddTrack = Make(new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift) });
        public static readonly RoutedCommand RenameTrack = Make(new InputGestureCollection { new KeyGesture(Key.F2) });
        public static readonly RoutedCommand DuplicateTrack = Make();
        public static readonly RoutedCommand DeleteTrack = Make();
        public static readonly RoutedCommand SetTrackHeight = Make();

        public static readonly RoutedCommand MusicLoadFile = Make(new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift) });
        public static readonly RoutedCommand MusicClearFile = Make();
        public static readonly RoutedCommand MusicManageSegments = Make();

        public static readonly RoutedCommand AddNote = Make(new InputGestureCollection { new KeyGesture(Key.F8) });
        public static readonly RoutedCommand EditNote = Make();
        public static readonly RoutedCommand DeleteNote = Make();
        public static readonly RoutedCommand NavigateToNote = Make();

        public static readonly RoutedCommand PlayPause = Make(new InputGestureCollection { new KeyGesture(Key.Space) });
        public static readonly RoutedCommand ZoomIn = Make(new InputGestureCollection { new KeyGesture(Key.Add, ModifierKeys.Control), new KeyGesture(Key.OemPlus, ModifierKeys.Control) });
        public static readonly RoutedCommand ZoomOut = Make(new InputGestureCollection { new KeyGesture(Key.Subtract, ModifierKeys.Control), new KeyGesture(Key.OemMinus, ModifierKeys.Control) });
        public static readonly RoutedCommand CancelPipette = Make();

        public static readonly RoutedCommand About = Make();
    }

    public partial class MainWindow
    {
        private const string CLIPBOARD_BLOCKS_FORMAT = "glowsequencer.blocks";
        private static readonly string[] MUSIC_EXTENSIONS = { "mp3", "m4a", "wav", "wma", "aiff", "aac" };

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
            // Hacky fix: This is called for the first time before "main" is initialized ...
            e.CanExecute = main != null && (sequencer.SelectedBlocks.Count > 0);
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

        private void CommandBinding_CanExecuteIfNote(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (e.Parameter is NoteViewModel);
        }

        private void CommandBinding_CanExecuteConvertToType(object sender, CanExecuteRoutedEventArgs e)
        {
            if ((string)e.Parameter == "color")
                e.CanExecute = sequencer.CanConvertToColor;
            else if ((string)e.Parameter == "ramp")
                e.CanExecute = sequencer.CanConvertToRamp;
            else
                e.CanExecute = sequencer.CanConvertToAutoDeduced;
        }

        private void CommandBinding_CanExecuteIfMusicFile(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = sequencer.Playback.MusicFileName != null;
        }

        private void CommandBinding_CanExecuteIfPipette(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = sequencer.IsPipetteActive;
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
            diag.Filter = string.Format("Glow Sequence (*{0})|*{0}|Uncompressed Glow Sequence (*.xml)|*.xml", FileSerializer.EXTENSION_PROJECT);

            if (diag.ShowDialog(this).GetValueOrDefault(false))
            {
                bool compressed = (diag.FilterIndex != 2);
                main.SaveDocumentAs(diag.FileName, compressed);
                return true;
            }
            return false;
        }


        private void CommandBinding_ExecuteExportGlo(object sender, ExecutedRoutedEventArgs e)
        {
            var result = Mastermind.ShowPromptTimeSpan(this, "Export start time", TimeSpan.Zero);
            if (!result.Success)
                return;
            TimeSpan startTime = result.Value;

            string exportName = main.DocumentName;
            if (exportName.EndsWith(FileSerializer.EXTENSION_PROJECT, StringComparison.InvariantCultureIgnoreCase))
                exportName = exportName.Substring(0, exportName.Length - FileSerializer.EXTENSION_PROJECT.Length);

            var diag = new Microsoft.Win32.SaveFileDialog();
            diag.FileName = exportName + FileSerializer.EXTENSION_EXPORT;
            diag.AddExtension = true;
            diag.DefaultExt = FileSerializer.EXTENSION_EXPORT;
            diag.Filter = string.Format("Aerotech Ultimate Program (*{0})|*{0}", FileSerializer.EXTENSION_EXPORT);
            diag.FilterIndex = 0;

            if (diag.ShowDialog(this).GetValueOrDefault(false))
            {
                if (main.ExportProgram(diag.FileName, (float)startTime.TotalSeconds))
                    MessageBox.Show(this, "Done!");
            }
        }

        private void CommandBinding_ExecuteShowTransferWindow(object sender, ExecutedRoutedEventArgs e)
        {
            Mastermind.OpenTransferWindow(main);
        }


        private void CommandBinding_ExecuteUndo(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.ActionManager.Undo();
            sequencer.SanityCheckSelections();
        }
        private void CommandBinding_ExecuteRedo(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.ActionManager.Redo();
            sequencer.SanityCheckSelections();
        }

        private void CommandBinding_ExecuteReplaceColor(object sender, ExecutedRoutedEventArgs e)
        {
            ReplaceColorViewModel replaceColorVm;

            if (e.Parameter is Color)
            {
                sequencer.SelectBlock(null, CompositionMode.None);
                replaceColorVm = new ReplaceColorViewModel(sequencer) { ColorToSearch = (Color)e.Parameter };
            }
            else
                replaceColorVm = new ReplaceColorViewModel(sequencer);


            var win = new ReplaceColorWindow(replaceColorVm);
            win.Owner = this;
            win.ShowDialog();
        }

        private void CommandBinding_ExecuteBrightMode(object sender, ExecutedRoutedEventArgs e)
        {
            var brightModeVm = new BrightModeViewModel(sequencer);

            var result = MessageBox.Show(
                "Bright mode increases the brightness of all dark colors to allow rehearsing in bright environments. "
                + "Do not forget to undo the changes in the program or to save them to a separate file!"
                + Environment.NewLine + Environment.NewLine
                + (brightModeVm.AffectsOnlySelection
                    ? $"Only the {StringUtil.Pluralize(brightModeVm.AffectedBlocks.Count(), "selected block")} will be affected."
                    : "All blocks in the sequence will be affected."),
                "Bright mode",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information
            );

            if (result == MessageBoxResult.OK)
            {
                brightModeVm.Execute();
            }
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

        private void CommandBinding_ExecuteConvertToType(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.ConvertSelectedBlocksTo((string)e.Parameter);
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

            sequencer.SelectBlocks(pastedBlocks.Select(b => BlockViewModel.FromModel(sequencer, b)), CompositionMode.None);
            ScrollCursorIntoView(ScrollIntoViewMode.Center);
        }

        private void CommandBinding_ExecuteDelete(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.DeleteSelectedBlocks();
        }

        private void CommandBinding_ExecuteSplitAtCursor(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.SplitBlocksAtCursor();
        }

        private void CommandBinding_ExecuteAddTrack(object sender, ExecutedRoutedEventArgs e)
        {
            TrackViewModel afterTrack = (e.Parameter == null ? sequencer.SelectedTrack : (TrackViewModel)e.Parameter);
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

        private void CommandBinding_ExecuteSetTrackHeight(object sender, ExecutedRoutedEventArgs e)
        {
            var result = Mastermind.ShowPrompt(this, "Enter track height in pixels", globalParams.TrackDisplayHeight.ToString(), double.Parse,
                            value => value > 0 && !double.IsNaN(value));
            if (!result.Success)
                return;

            globalParams.TrackDisplayHeight = result.Value;
        }


        private void CommandBinding_ExecuteMusicLoadFile(object sender, ExecutedRoutedEventArgs e)
        {
            var extensions = MUSIC_EXTENSIONS.Select(ext => $"*.{ext}");
            var diag = new Microsoft.Win32.OpenFileDialog();
            diag.Title = "Open music file";
            diag.Filter = string.Format("Audio file ({0})|{1}|All files|*.*",
                string.Join(", ", extensions), string.Join(";", extensions));
            diag.FilterIndex = 0;

            if (sequencer.Playback.MusicFileName != null)
                diag.InitialDirectory = Path.GetDirectoryName(sequencer.Playback.MusicFileName);

            if (diag.ShowDialog(this) ?? false)
            {
                DoMusicLoadFile(diag.FileName);
            }
        }

        private void DoMusicLoadFile(string file)
        {
            sequencer.ActionManager.RecordAction(
                MakeMusicFileAction(file),
                MakeMusicFileAction(sequencer.Playback.MusicFileName) // old loaded file
            );
        }

        private void CommandBinding_ExecuteMusicClearFile(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.ActionManager.RecordAction(
                () => sequencer.Playback.ClearFile(),
                MakeMusicFileAction(sequencer.Playback.MusicFileName)
            );
        }

        // Note that this needs to be blocking to ensure quick undo/redo does not mess anything up.
        // The better solution would be to have the ActionManager support async.
        private Action MakeMusicFileAction(string fileName)
        {
            if (fileName == null) return () => sequencer.Playback.ClearFile();
            else return () => sequencer.Playback.LoadFileAsync(fileName).Forget();
        }


        private void CommandBinding_ExecuteMusicManageSegments(object sender, ExecutedRoutedEventArgs e)
        {
            Mastermind.OpenMusicSegmentsWindow();
        }

        private void CommandBinding_ExecuteAddNote(object sender, ExecutedRoutedEventArgs e)
        {
            var existingNoteVm = sequencer.Notes.Notes.FirstOrDefault(
                    noteVm => Math.Abs(noteVm.TimeSeconds - sequencer.CursorPosition) < Model.Block.MIN_DURATION_TECHNICAL_LIMIT * 0.5f);

            if (existingNoteVm != null)
                SequencerCommands.EditNote.Execute(existingNoteVm, sender as IInputElement);
            else
                sequencer.Notes.AddNoteAtCursor();
        }

        private void CommandBinding_ExecuteEditNote(object sender, ExecutedRoutedEventArgs e)
        {
            NoteViewModel noteVm = (NoteViewModel)e.Parameter;

            var win = new EditNoteWindow(noteVm);
            win.Owner = this;
            win.Show();
        }

        private void CommandBinding_ExecuteDeleteNote(object sender, ExecutedRoutedEventArgs e)
        {
            NoteViewModel noteVm = (NoteViewModel)e.Parameter;
            sequencer.Notes.DeleteNote(noteVm);
        }

        private void CommandBinding_ExecuteNavigateToNote(object sender, ExecutedRoutedEventArgs e)
        {
            NoteViewModel noteVm = (NoteViewModel)e.Parameter;
            sequencer.CursorPosition = noteVm.TimeSeconds;
            ScrollCursorIntoView(ScrollIntoViewMode.Center);
        }

        private void CommandBinding_ExecutePlayPause(object sender, ExecutedRoutedEventArgs e)
        {
            if (sequencer.Playback.IsPlaying)
                sequencer.Playback.Stop();
            else
                sequencer.Playback.Play();
        }

        private void CommandBinding_ExecuteZoomIn(object sender, ExecutedRoutedEventArgs e)
        {
            ChangeZoom(10);
        }

        private void CommandBinding_ExecuteZoomOut(object sender, ExecutedRoutedEventArgs e)
        {
            ChangeZoom(-10);
        }

        private void CommandBinding_ExecuteCancelPipette(object sender, ExecutedRoutedEventArgs e)
        {
            sequencer.PipetteTarget = null;
        }

        private void CommandBinding_ExecuteHelp(object sender, ExecutedRoutedEventArgs e)
        {
            Mastermind.OpenHelpWindow();
        }

        private void CommandBinding_ExecuteAbout(object sender, ExecutedRoutedEventArgs e)
        {
            Mastermind.OpenAboutWindow();
        }

    }

}
