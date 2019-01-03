using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GlowSequencer.View
{
    /// <summary>
    /// Interaction logic for PipetteButton.xaml
    /// </summary>
    public partial class PipetteButton : UserControl, IPipetteColorTarget
    {
        public static readonly DependencyProperty ActivationContextProperty =
            DependencyProperty.Register("ActivationContext", typeof(IPipetteColorTarget), typeof(PipetteButton),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ActivationContextChanged));
        public static readonly DependencyProperty TargetColorProperty =
            DependencyProperty.Register("TargetColor", typeof(Color), typeof(PipetteButton),
                new FrameworkPropertyMetadata(Colors.Transparent, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private static void ActivationContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Update the check status of the button through code. Data binding IsChecked would be more elegant,
            // but there is no trivial way since a UserControl is not Observable.
            var self = (PipetteButton)d;
            self.pipetteButton.IsChecked = (e.NewValue == self);
        }

        /// <summary>Should be linked to SequencerViewModel.PipetteTarget, this control will assign this appropriately when
        /// the pipette is enabled or disabled.</summary>
        public IPipetteColorTarget ActivationContext
        {
            get { return (IPipetteColorTarget)GetValue(ActivationContextProperty); }
            set { SetValue(ActivationContextProperty, value); }
        }

        /// <summary>The control updates this value with the picked color. It should be bound to the property that this pipette should modify.</summary>
        public Color TargetColor
        {
            get { return (Color)GetValue(TargetColorProperty); }
            set { SetValue(TargetColorProperty, value); }
        }
        
        public PipetteButton()
        {
            InitializeComponent();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ActivationContext = this;
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ActivationContext == this)
            {
                ActivationContext = null;
            }
        }
    }
}
