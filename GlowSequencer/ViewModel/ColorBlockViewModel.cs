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
            set { model.Color = value.ToGloColor(); }
        }

        public Color RenderedColor1 { get { return model.RenderedColor1.ToViewColor(); } }
        public Color RenderedColor2 { get { return model.RenderedColor2.ToViewColor(); } }

        public ColorBlockViewModel(SequencerViewModel sequencer, Model.ColorBlock model)
            : base(sequencer, model, "Color")
        {
            this.model = model;
            ForwardPropertyEvents("Color", model, "Color");
            ForwardPropertyEvents("RenderedColor1", model, "RenderedColor1");
            ForwardPropertyEvents("RenderedColor2", model, "RenderedColor2");
        }

        public new Model.ColorBlock GetModel()
        {
            return model;
        }

    }
}
