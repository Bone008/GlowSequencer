using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class GlobalViewParameters : Observable
    {
        private bool _enableSnapping = true;
        private WaveformDisplayMode _currentWaveformDisplayMode = WaveformDisplayMode.Linear;
        private Xceed.Wpf.Toolkit.ColorMode _currentColorMode;


        public bool EnableSnapping { get { return _enableSnapping; } set { SetProperty(ref _enableSnapping, value); } }

        public WaveformDisplayMode WaveformDisplayMode { get { return _currentWaveformDisplayMode; } set { SetProperty(ref _currentWaveformDisplayMode, value); } }
        public bool WaveformDisplayModeIsLinear { get { return WaveformDisplayMode == WaveformDisplayMode.Linear; } set { WaveformDisplayMode = WaveformDisplayMode.Linear; } }
        public bool WaveformDisplayModeIsLogarithmic { get { return WaveformDisplayMode == WaveformDisplayMode.Logarithmic; } set { WaveformDisplayMode = WaveformDisplayMode.Logarithmic; } }

        /// <summary>Canvas or Palette mode for color pickers</summary>
        public Xceed.Wpf.Toolkit.ColorMode CurrentColorMode { get { return _currentColorMode; } set { _currentColorMode = value; } }

        // TODO here the "synchronize units" toggle could be implemented neatly

        public GlobalViewParameters()
        {
            ForwardPropertyEvents(nameof(WaveformDisplayMode), this, nameof(WaveformDisplayModeIsLinear), nameof(WaveformDisplayModeIsLogarithmic));
        }
    }
}
