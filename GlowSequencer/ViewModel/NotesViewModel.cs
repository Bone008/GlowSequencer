using ContinuousLinq;
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

        public ReadOnlyContinuousCollection<NoteViewModel> Notes { get; private set; }

        public NotesViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;
            Notes = sequencer.GetModel().Notes.Select(note => new NoteViewModel(sequencer, note));
        }
    }
}
