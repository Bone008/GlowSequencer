using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public interface IPipetteColorTarget
    {
        Color TargetColor { get; set; }
    }
}
