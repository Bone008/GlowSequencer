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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GlowPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GloColor _col = GloColor.FromRGB(0, 0, 0);

        private GloColor CurrentColor
        {
            get { return _col; }
            set
            {
                _col = value;
                //colorPanel.Background = new SolidColorBrush(Color.FromRgb((byte)_col.r, (byte)_col.g, (byte)_col.b));
            }
        }

        private DispatcherTimer timer;
        private IEnumerator<TimeSpan> programQueue = null;
        private DateTime nextExecutionTime;

        public MainWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += timer_Tick;

            colorPanel.Background = new SolidColorBrush(c2c(_col));
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fd = new Microsoft.Win32.OpenFileDialog();
            fd.DefaultExt = ".glo";
            fd.Filter = "Glo Files (*.glo)|*.glo|All files|*";
            bool? result = fd.ShowDialog(this);

            if (result.HasValue && result.Value)
            {
                try
                {
                    GloProgram prog = GloProgram.LoadFromFile(fd.FileName);
                    programQueue = RunProgram(prog).GetEnumerator();
                    nextExecutionTime = DateTime.Now;
                    timer.Start();
                }
                catch (System.IO.FileFormatException ex)
                {
                    MessageBox.Show("Error loading program: " + ex.Message);
                }
            }
        }


        private void timer_Tick(object sender, EventArgs e)
        {
            while (nextExecutionTime < DateTime.Now)
            {
                if (programQueue.MoveNext())
                {
                    nextExecutionTime += programQueue.Current;
                }
                else
                {
                    timer.Stop();
                    CurrentColor = GloColor.FromRGB(0, 0, 0);
                    break;
                }
            }
        }

        private IEnumerable<TimeSpan> RunProgram(GloProgram prog)
        {
            foreach (var elem in RunSequence(prog.Root.Commands))
                yield return elem;

        }
        private IEnumerable<TimeSpan> RunSequence(IEnumerable<GloCommand> seq)
        {
            foreach (GloCommand cmd in seq)
            {
                if (cmd is GloLoop)
                {
                    GloLoop loop = (GloLoop)cmd;
                    for (int i = 0; i < loop.Repetitions; i++)
                    {
                        foreach (var elem in RunSequence(loop.Commands))
                            yield return elem;
                    }
                }

                else if (cmd is GloDelayCommand)
                {
                    yield return ((GloDelayCommand)cmd).Delay;
                }

                else if (cmd is GloRampCommand)
                {
                    GloRampCommand ramp = (GloRampCommand)cmd;
                    GloColor startCol = CurrentColor;


                    ColorAnimation animation = new ColorAnimation();
                    animation.To = c2c(ramp.TargetColor);
                    animation.Duration = new Duration(ramp.Duration);
                    colorPanel.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);

                    CurrentColor = ramp.TargetColor;

                    /*double duration = ramp.Duration.TotalMilliseconds;
                    for (double t = 0; t < duration; t += 10)
                    {
                        CurrentColor = GloColor.Blend(startCol, ((GloRampCommand)cmd).TargetColor, t / duration);
                        yield return TimeSpan.FromMilliseconds(10);
                    }*/
                    yield return ramp.Duration;
                }

                else if (cmd is GloColorCommand)
                {
                    colorPanel.Background.SetCurrentValue(SolidColorBrush.ColorProperty, c2c(((GloColorCommand)cmd).Color));
                    //CurrentColor = ((GloColorCommand)cmd).Color;
                    yield return TimeSpan.Zero;
                }

                else
                {
                    MessageBox.Show("Unknown command: " + cmd.GetType().Name);
                }
            }
        }


        private Color c2c(GloColor _col)
        {
            return Color.FromRgb((byte)_col.r, (byte)_col.g, (byte)_col.b);
        }
    }
}
