using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public class ReplaceColorViewModel : Observable
    {
        private SequencerViewModel main;

        public Color ColorToSearch { get; set; } // TODO observable
        public Color ColorToReplace { get; set; }

        public IEnumerable<Color> ColorChoices { get; private set; }

        public ReplaceColorViewModel(SequencerViewModel main)
        {
            this.main = main;

            IEnumerable<BlockViewModel> blocks = (main.SelectedBlocks.Any() ? main.SelectedBlocks : (IEnumerable<BlockViewModel>)main.AllBlocks);

            ColorChoices = _ColorsFromBlocks(blocks)
                            .Distinct()
                            .OrderByDescending(c => c.R + c.G + c.B)
                            .ToList();

            ColorToSearch = ColorChoices.FirstOrDefault();
            ColorToReplace = Colors.White;
        }

        public void Execute()
        {
            // TODO implement color replacement
        }

        private IEnumerable<Color> _ColorsFromBlocks(IEnumerable<BlockViewModel> blocks)
        {
            return blocks.OfType<ColorBlockViewModel>().Select(b => b.Color)
                        .Concat(blocks.OfType<RampBlockViewModel>().Select(b => b.StartColor))
                        .Concat(blocks.OfType<RampBlockViewModel>().Select(b => b.EndColor))
                        .Concat(blocks.OfType<GroupBlockViewModel>().SelectMany(b => _ColorsFromBlocks(b.Children)));
        }
    }
}
