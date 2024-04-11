using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    // Note that this is not yet observable, because it is only used for a MessageBox so far.
    // This is used for the "original" bright mode that modifies the Timeline, whereas the "new"
    // bright mode happens at export time and is invoked by TransferDirectlyController.
    public class BrightModeViewModel
    {
        private readonly SequencerViewModel sequencer;
        private readonly ColorTransformMode mode;

        public IEnumerable<BlockViewModel> AffectedBlocks => (sequencer.SelectedBlocks.Any() ? sequencer.SelectedBlocks : (IEnumerable<BlockViewModel>)sequencer.AllBlocks);
        public bool AffectsOnlySelection => sequencer.SelectedBlocks.Any();

        public BrightModeViewModel(SequencerViewModel sequencer, ColorTransformMode mode)
        {
            this.sequencer = sequencer;
            this.mode = mode;
        }

        public void Execute()
        {
            using (sequencer.ActionManager.CreateTransaction())
            {
                _ExecuteForBlocks(AffectedBlocks);
            }
        }

        private void _ExecuteForBlocks(IEnumerable<BlockViewModel> blocks)
        {
            foreach (BlockViewModel block in blocks)
            {
                if (block is ColorBlockViewModel colorBlock)
                {
                    colorBlock.Color = AdjustColor(colorBlock.Color);
                }
                else if (block is RampBlockViewModel rampBlock)
                {
                    rampBlock.StartColor = AdjustColor(rampBlock.StartColor);
                    rampBlock.EndColor = AdjustColor(rampBlock.EndColor);
                }
                else if (block is GroupBlockViewModel groupBlock)
                    _ExecuteForBlocks(groupBlock.Children);
            }
        }

        private Color AdjustColor(Color color)
        {
            return GloColor.TransformToMode(color.ToGloColor(), mode).ToViewColor();
        }
    }
}
