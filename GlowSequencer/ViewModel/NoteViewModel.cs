using System;
using GlowSequencer.Model;
using GlowSequencer.Util;
using System.Globalization;

namespace GlowSequencer.ViewModel
{
    public class NoteViewModel : Observable
    {
        private readonly SequencerViewModel sequencer;
        private readonly Note model;

        public string Label { get { return model.Label; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.Label, value); } }
        public string Description { get { return model.Description; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.Description, value); } }
        public float TimeSeconds { get { return model.Time; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.Time, value); } }

        public string LabelOrFormattedTime => Label ?? TimeSpanToStringConverter.Convert(TimeSpan.FromSeconds(TimeSeconds));
        public double DisplayOffset => TimeSeconds * sequencer.TimePixelScale;

        public NoteViewModel(SequencerViewModel sequencer, Note model)
        {
            this.sequencer = sequencer;
            this.model = model;

            ForwardPropertyEvents(nameof(model.Label), model, nameof(Label), nameof(LabelOrFormattedTime));
            ForwardPropertyEvents(nameof(model.Description), model, nameof(Description));
            ForwardPropertyEvents(nameof(model.Time), model, nameof(TimeSeconds), nameof(DisplayOffset), nameof(LabelOrFormattedTime));
            ForwardPropertyEvents(nameof(sequencer.TimePixelScale), sequencer, nameof(DisplayOffset));
        }

        public Note GetModel()
        {
            return model;
        }
    }
}