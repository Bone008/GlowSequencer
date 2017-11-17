using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Model
{
    // not implemented yet
    public class SubsequenceBlock : Block
    {
        // private Subsequence sequence

        public SubsequenceBlock(Timeline timeline, params Track[] tracks)
            : base(timeline, tracks)
        {
        }

        protected override GloColor GetColorAtLocalTimeCore(float localTime, Track track)
        {
            throw new NotImplementedException();
        }

        [Obsolete]
        public override IEnumerable<GloCommand> ToGloCommands(GloSequenceContext context)
        {
            throw new NotImplementedException();
        }
    }
}
