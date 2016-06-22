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
        private SequencerViewModel sequencer;

        private Color _colorToSearch;
        private Color _colorToReplace;

        public Color ColorToSearch { get { return _colorToSearch; } set { SetProperty(ref _colorToSearch, value); } }
        public Color ColorToReplace { get { return _colorToReplace; } set { SetProperty(ref _colorToReplace, value); } }

        public ObservableCollection<Xceed.Wpf.Toolkit.ColorItem> ColorChoices { get; private set; }

        public ReplaceColorViewModel(SequencerViewModel sequencer)
        {
            this.sequencer = sequencer;

            var colorItems = from color in _ColorsFromBlocks(GetConsideredBlocks())
                             group color by color into g
                             //orderby g.Key.R + g.Key.G + g.Key.B descending
                             select new Xceed.Wpf.Toolkit.ColorItem(g.Key, string.Format("R: {0}; G: {1}; B: {2} - Usages: {3}", g.Key.R, g.Key.G, g.Key.B, g.Count()));

            ColorChoices = new ObservableCollection<Xceed.Wpf.Toolkit.ColorItem>(colorItems);
            ColorToSearch = (ColorChoices.Any() ? ColorChoices.First().Color.Value : Colors.White);
            ColorToReplace = Colors.White;
        }

        public void Execute()
        {
            using (sequencer.ActionManager.CreateTransaction())
            {
                _ExecuteForBlocks(GetConsideredBlocks());
            }
        }

        private void _ExecuteForBlocks(IEnumerable<BlockViewModel> blocks)
        {
            foreach (BlockViewModel block in blocks)
            {
                if (block is ColorBlockViewModel && ((ColorBlockViewModel)block).Color == _colorToSearch)
                    ((ColorBlockViewModel)block).Color = _colorToReplace;
                else if (block is RampBlockViewModel)
                {
                    if (((RampBlockViewModel)block).StartColor == _colorToSearch)
                        ((RampBlockViewModel)block).StartColor = _colorToReplace;
                    if (((RampBlockViewModel)block).EndColor == _colorToSearch)
                        ((RampBlockViewModel)block).EndColor = _colorToReplace;
                }
                else if (block is GroupBlockViewModel)
                    _ExecuteForBlocks(((GroupBlockViewModel)block).Children);
            }
        }

        private IEnumerable<Color> _ColorsFromBlocks(IEnumerable<BlockViewModel> blocks)
        {
            return blocks.OfType<ColorBlockViewModel>().Select(b => b.Color)
                        .Concat(blocks.OfType<RampBlockViewModel>().Select(b => b.StartColor))
                        .Concat(blocks.OfType<RampBlockViewModel>().Select(b => b.EndColor))
                        .Concat(blocks.OfType<GroupBlockViewModel>().SelectMany(b => _ColorsFromBlocks(b.Children)));
        }

        private IEnumerable<BlockViewModel> GetConsideredBlocks()
        {
            return (sequencer.SelectedBlocks.Any() ? sequencer.SelectedBlocks : (IEnumerable<BlockViewModel>)sequencer.AllBlocks);
        }
    }
}
