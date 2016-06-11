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

        public ReadOnlyContinuousCollection<BlockViewModel> Blocks { get; private set; }
        //public IEnumerable<BlockViewModel> Blocks { get; private set; }

        public bool IsSelected
        {
            get { return sequencer.SelectedTrack == this; }
        }

        public TrackViewModel(SequencerViewModel sequencer, Model.Track model)
        {
            this.sequencer = sequencer;
            this.model = model;

            ForwardPropertyEvents("Label", model, "Label");
            ForwardPropertyEvents("SelectedTrack", sequencer, "IsSelected");

            Blocks = model.Blocks.Select(b => BlockViewModel.FromModel(sequencer, b));
            //Blocks.CollectionChanged += (sender, e) => OnMaxBlockExtentChanged();

            ForwardPropertyEvents("TimePixelScale", sequencer, "MaxBlockExtent");

            //Blocks = sequencer.Blocks.Where(b => b.Tracks.Contains(this));
        }


        //public void OnTimescaleChanged()
        //{
        //    OnMaxBlockExtentChanged();
        //}

        //public void OnMaxBlockExtentChanged()
        //{
        //    float v = MaxBlockExtent;
        //    if (_lastPublishedBlockExtent != v)
        //    {
        //        _lastPublishedBlockExtent = v;
        //        Notify("MaxBlockExtent");
        //    }
        //}

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
