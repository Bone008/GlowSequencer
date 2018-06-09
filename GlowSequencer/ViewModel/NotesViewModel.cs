using ContinuousLinq;
using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class NotesViewModel : Observable
    {
        private readonly SequencerViewModel sequencer;

        // Note that the unsorted variant needs to be used by the main timeline visualization,
        // otherwise the item collection gets refreshed during dragging, resulting in broken
        // mouse capture and the DataContext of notes being set to {DisconnectedItem}.
        public ReadOnlyContinuousCollection<NoteViewModel> Notes { get; private set; }
        public ReadOnlyContinuousCollection<NoteViewModel> NotesSorted { get; private set; }

        public bool HasNotes => Notes.Count > 0;

        public NotesViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;
            Notes = sequencer.GetModel().Notes.Select(note => new NoteViewModel(sequencer, note));
            NotesSorted = Notes.OrderBy(note => note.TimeSeconds);

            ForwardCollectionEvents(Notes, nameof(HasNotes));
        }

        public void AddNoteAtCursor()
        {
            Note newNote = new Note { Time = sequencer.CursorPosition };
            sequencer.ActionManager.RecordAdd(sequencer.GetModel().Notes, newNote);
        }

        public void DeleteNote(NoteViewModel noteVm)
        {
            sequencer.ActionManager.RecordRemove(sequencer.GetModel().Notes, noteVm.GetModel());
        }
    }
}
