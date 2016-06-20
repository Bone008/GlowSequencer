using ContinuousLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public abstract class GroupBlockViewModel : BlockViewModel
    {
        protected new Model.GroupBlock model;

        private ReadOnlyContinuousCollection<BlockViewModel> _children;

        public ReadOnlyContinuousCollection<BlockViewModel> Children { get { return _children; } }

        public override MusicSegmentViewModel SegmentContext
        {
            get { return base.SegmentContext; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                using (sequencer.ActionManager.CreateTransaction())
                {
                    sequencer.ActionManager.RecordSetProperty(model, m => m.SegmentContext, value.GetModel());
                    model.SegmentContext = value.GetModel();
                    foreach (var child in model.Children)
                        sequencer.ActionManager.RecordSetProperty(child, m => m.SegmentContext, value.GetModel());
                }
            }
        }

        public override bool IsSegmentActive { get { return true; } }

        public GroupBlockViewModel(SequencerViewModel sequencer, Model.GroupBlock model)
            : this(sequencer, model, "Group")
        { }

        protected GroupBlockViewModel(SequencerViewModel sequencer, Model.GroupBlock model, string typeLabel)
            : base(sequencer, model, typeLabel)
        {
            this.model = model;
            _children = model.Children.Select(b => BlockViewModel.FromModel(sequencer, b));
        }

        public override void ScaleDuration(float factor)
        {
            foreach (var child in _children)
            {
                child.StartTime *= factor;
                child.ScaleDuration(factor);
            }
            base.ScaleDuration(factor);
        }

        public override void OnTracksCollectionChanged()
        {
            base.OnTracksCollectionChanged();
            foreach (var child in _children)
                child.OnTracksCollectionChanged();
        }

    }
}
