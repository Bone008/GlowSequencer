using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public class RampBlockViewModel : BlockViewModel
    {
        protected new Model.RampBlock model;

        public Color StartColor
        {
            get { return model.StartColor.ToViewColor(); }
            set { model.StartColor = value.ToGloColor(); }
        }
        public Color EndColor
        {
            get { return model.EndColor.ToViewColor(); }
            set { model.EndColor = value.ToGloColor(); }
        }

        public RampBlockViewModel(SequencerViewModel sequencer, Model.RampBlock model)
            : base(sequencer, model, "Ramp")
        {
            this.model = model;
            ForwardPropertyEvents("StartColor", model, "StartColor");
            ForwardPropertyEvents("EndColor", model, "EndColor");
        }


        public new Model.RampBlock GetModel()
        {
            return model;
        }

    }
}
