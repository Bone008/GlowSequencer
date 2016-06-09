using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer
{
    public static class CompatibilityHooks
    {
        /// <summary>
        /// Conversion for 1.3.0 -> later.
        /// Because group blocks now both inerhit and control the music segment value of their children, their value should be fixed up on load.
        /// </summary>
        public static void GroupBlockFromXML(GroupBlock block)
        {
            // only when the group block was loaded with the "<none>" segment
            if (block.SegmentContext.IsReadOnly)
            {
                var relatedSegments = Enumerable.Select(block.Children, b => b.SegmentContext).Distinct().ToList();
                // only when all child blocks are in the same segment
                if (relatedSegments.Count == 1)
                    block.SegmentContext = relatedSegments.Single();
            }
        }
    }
}
