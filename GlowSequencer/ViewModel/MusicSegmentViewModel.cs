using ContinuousLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class MusicSegmentViewModel : Observable
    {

        private SequencerViewModel _sequencer;
        private Model.MusicSegment _model;

        public string Label { get { return _model.Label; } set { _model.Label = value; } }
        public float Bpm { get { return _model.Bpm; } set { _model.Bpm = value; } }
        public int BeatsPerBar { get { return _model.BeatsPerBar; } set { _model.BeatsPerBar = value; } }

        public float TimeOriginSeconds { get { return _model.TimeOrigin; } set { _model.TimeOrigin = value; } }
        public TimeSpan TimeOrigin { get { return TimeSpan.FromSeconds(_model.TimeOrigin); } set { _model.TimeOrigin = (float)value.TotalSeconds; } }

        public bool IsReadOnly { get { return _model.IsReadOnly; } }
        public bool IsDefault { get { return _model.IsDefault; } }

        public ReadOnlyContinuousCollection<object> ReferringBlocksDummies { get; private set; }


        public MusicSegmentViewModel(SequencerViewModel sequencer, Model.MusicSegment model)
        {
            _sequencer = sequencer;
            _model = model;

            ReferringBlocksDummies = sequencer.GetModel().Blocks.Where(b => b.SegmentContext == model).Select(b => (object)null);

            ForwardPropertyEvents("Label", model, "Label");
            ForwardPropertyEvents("Bpm", model, "Bpm");
            ForwardPropertyEvents("BeatsPerBar", model, "BeatsPerBar");
            ForwardPropertyEvents("TimeOrigin", model, "TimeOrigin", "TimeOriginSeconds");
            ForwardPropertyEvents("IsReadOnly", model, "IsReadOnly");
            ForwardPropertyEvents("IsDefault", model, "IsDefault");

            ForwardPropertyEvents("Bpm", this, CheckSequencerActiveUpdate);
            ForwardPropertyEvents("BeatsPerBar", this, CheckSequencerActiveUpdate);
            ForwardPropertyEvents("TimeOrigin", this, CheckSequencerActiveUpdate);
        }

        private void CheckSequencerActiveUpdate()
        {
            if(_sequencer.ActiveMusicSegment == this)
            {
                // dirty way to trigger handler for HorizontalTimelineData
                _sequencer.ActiveMusicSegment = this;
            }
        }

        public float GetBeatsPerSecond()
        {
            return _model.GetBeatsPerSecond();
        }

        public int GetIndex()
        {
            return _model.GetIndex();
        }

        public Model.MusicSegment GetModel()
        {
            return _model;
        }
    }
}
