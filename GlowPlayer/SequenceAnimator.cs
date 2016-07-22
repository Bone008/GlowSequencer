using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GlowPlayer
{
    class SequenceAnimator
    {
        private Canvas colorPanel;

        private IEnumerator<TimeSpan> programQueue = null;
        private DateTime nextExecutionTime;
        private TimeSpan cursor = TimeSpan.Zero;
        private GloColor currentColor = GloColor.FromRGB(0, 0, 0);

        public bool Running { get; private set; }

        public SequenceAnimator(Canvas colorPanel, GloProgram program)
        {
            this.colorPanel = colorPanel;
            Running = true;

            programQueue = RunProgram(program).GetEnumerator();
            nextExecutionTime = DateTime.Now;
        }

        public void Tick(TimeSpan progress)
        {
            //while (nextExecutionTime < DateTime.Now)
            while (cursor <= progress)
            {
                if (programQueue.MoveNext())
                {
                    nextExecutionTime += programQueue.Current;
                    cursor += programQueue.Current;
                }
                else
                {
                    Running = false;
                    currentColor = GloColor.FromRGB(0, 0, 0);
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
                    GloColor startCol = currentColor;


                    ColorAnimation animation = new ColorAnimation();
                    animation.To = c2c(ramp.TargetColor);
                    animation.Duration = new Duration(ramp.Duration);
                    colorPanel.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);

                    currentColor = ramp.TargetColor;

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
