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

        // unused as of yet
        public Color RenderedColor1 { get { return model.RenderedColor1.ToViewColor(); } }
        public Color RenderedColor2 { get { return model.RenderedColor2.ToViewColor(); } }

        public ColorBlockViewModel(SequencerViewModel sequencer, Model.ColorBlock model)
            : base(sequencer, model, "Color")
        {
            this.model = model;
            ForwardPropertyEvents(nameof(model.Color), model, nameof(Color));
            ForwardPropertyEvents(nameof(model.RenderedColor1), model, nameof(RenderedColor1));
            ForwardPropertyEvents(nameof(model.RenderedColor2), model, nameof(RenderedColor2));
        }

        public new Model.ColorBlock GetModel()
        {
            return model;
        }

    }
}
