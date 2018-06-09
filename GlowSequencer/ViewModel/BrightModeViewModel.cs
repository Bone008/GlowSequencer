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
    public class BrightModeViewModel
    {
        /// <summary>Threshold of the brightest channel of a color. All darker colors will be brigthened to reach this threshold.</summary>
        private const int BRIGHTNESS_THRESHOLD = 100;

        private readonly SequencerViewModel sequencer;

        public IEnumerable<BlockViewModel> AffectedBlocks => (sequencer.SelectedBlocks.Any() ? sequencer.SelectedBlocks : (IEnumerable<BlockViewModel>)sequencer.AllBlocks);
        public bool AffectsOnlySelection => sequencer.SelectedBlocks.Any();

        public BrightModeViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;
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
                    colorBlock.Color = BrightenColor(colorBlock.Color);
                }
                else if (block is RampBlockViewModel rampBlock)
                {
                    rampBlock.StartColor = BrightenColor(rampBlock.StartColor);
                    rampBlock.EndColor = BrightenColor(rampBlock.EndColor);
                }
                else if (block is GroupBlockViewModel groupBlock)
                    _ExecuteForBlocks(groupBlock.Children);
            }
        }

        private Color BrightenColor(Color color)
        {
            int brightestValue = Math.Max(color.R, Math.Max(color.G, color.B));
            // Do not modify black or colors that are already bright enough.
            if (brightestValue == 0 || brightestValue >= BRIGHTNESS_THRESHOLD)
                return color;

            float factor = (float)BRIGHTNESS_THRESHOLD / brightestValue;
            return GloColor.FromRGB(
                    (int)Math.Round(color.R * factor),
                    (int)Math.Round(color.G * factor),
                    (int)Math.Round(color.B * factor)
                ).ToViewColor();
        }
    }
}
