using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public enum BrightModeType
    {
        Brighten,
        Darken
    }

    // Note that this is not yet observable, because it is only used for a MessageBox so far.
    public class BrightModeViewModel
    {
        /// <summary>Threshold of the brightest channel of a color. In bright mode, all darker colors will be brigthened to reach this threshold.</summary>
        private const int TOO_DARK_THRESHOLD = 20;
        /// <summary>Threshold of the brightest channel of a color. In dark mode, all brighter colors will be darkened to reach this threshold.</summary>
        private const int TOO_BRIGHT_THRESHOLD = 10;

        private readonly SequencerViewModel sequencer;
        private readonly BrightModeType type;

        public IEnumerable<BlockViewModel> AffectedBlocks => (sequencer.SelectedBlocks.Any() ? sequencer.SelectedBlocks : (IEnumerable<BlockViewModel>)sequencer.AllBlocks);
        public bool AffectsOnlySelection => sequencer.SelectedBlocks.Any();

        public BrightModeViewModel(SequencerViewModel sequencer, BrightModeType type)
        {
            this.sequencer = sequencer;
            this.type = type;
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
            int brightestValue = Math.Max(color.R, Math.Max(color.G, color.B));
            // Do not modify black or colors that are already bright/dark enough.
            if (brightestValue == 0)
                return color;
            if (type == BrightModeType.Brighten && brightestValue >= TOO_DARK_THRESHOLD)
                return color;
            if (type == BrightModeType.Darken && brightestValue <= TOO_BRIGHT_THRESHOLD)
                return color;

            int targetBrightness = (type == BrightModeType.Brighten ? TOO_DARK_THRESHOLD : TOO_BRIGHT_THRESHOLD);
            float factor = (float)targetBrightness / brightestValue;
            return GloColor.FromRGB(
                    (int)Math.Round(color.R * factor),
                    (int)Math.Round(color.G * factor),
                    (int)Math.Round(color.B * factor)
                ).ToViewColor();
        }
    }
}
