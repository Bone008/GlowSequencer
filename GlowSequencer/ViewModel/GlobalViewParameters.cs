using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class GlobalViewParameters : Observable
    {
        private Xceed.Wpf.Toolkit.ColorMode _currentColorMode;

        public Xceed.Wpf.Toolkit.ColorMode CurrentColorMode { get { return _currentColorMode; } set { _currentColorMode = value; } }

        // TODO here the "synchronize units" toggle could be implemented neatly
    }
}
