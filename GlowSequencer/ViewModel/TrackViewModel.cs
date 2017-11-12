using ContinuousLinq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{

    public class TrackViewModel : Observable
    {
        public const int DISPLAY_HEIGHT = 50;


        private SequencerViewModel sequencer;
        private Model.Track model;

        public string Label { get { return model.Label; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.Label, value); } }
        public bool IsSelected => sequencer.SelectedTrack == this;

        public ReadOnlyContinuousCollection<BlockViewModel> Blocks { get; private set; }

        public TrackViewModel(SequencerViewModel sequencer, Model.Track model)
        {
            this.sequencer = sequencer;
            this.model = model;

            ForwardPropertyEvents(nameof(model.Label), model, nameof(Label));
            ForwardPropertyEvents(nameof(sequencer.SelectedTrack), sequencer, nameof(IsSelected));

            Blocks = model.Blocks.Select(b => BlockViewModel.FromModel(sequencer, b));
        }

        public int GetIndex()
        {
            return model.GetIndex();
        }

        public Model.Track GetModel()
        {
            return model;
        }
    }
}
