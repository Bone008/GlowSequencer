using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public class ColorBlockViewModel : BlockViewModel
    {
        protected new Model.ColorBlock model;

        public Color Color
        {
            get { return model.Color.ToViewColor(); }
            set { sequencer.ActionManager.RecordSetProperty(model, m => m.Color, value.ToGloColor()); }
        }

        public ColorBlockViewModel(SequencerViewModel sequencer, Model.ColorBlock model)
            : base(sequencer, model, "Color")
        {
            this.model = model;
            ForwardPropertyEvents(nameof(model.Color), model, nameof(Color));
        }

        public new Model.ColorBlock GetModel()
        {
            return model;
        }

    }
}
