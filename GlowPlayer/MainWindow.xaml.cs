using System;
using System.Collections.Generic;
using System.IO;
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

        private List<SequenceAnimator> runningAnimators = new List<SequenceAnimator>();

        private DispatcherTimer timer;
        private TimeSpan progress = TimeSpan.Zero;
        private DateTime lastTick;

        public MainWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += timer_Tick;
        }

        private void StartFromBeginning()
        {
            lastTick = DateTime.Now;
            progress = TimeSpan.Zero;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            progress += (now - lastTick);
            lastTick = now;

            timestampLabel.Content = progress.ToString();

            runningAnimators.ForEach(anim => anim.Tick(progress));
            runningAnimators.RemoveAll(anim => !anim.Running);

            if (runningAnimators.Count == 0)
                timer.Stop();
        }

        private void Button_Click(object sender, RoutedEventArgs _)
        {
            var diag = new Microsoft.Win32.OpenFileDialog();
            diag.DefaultExt = ".glo";
            diag.Filter = "Glo files (*.glo)|*.glo|All files|*.*";
            diag.FilterIndex = 1;
            diag.Multiselect = true;

            if (diag.ShowDialog(this) == true)
            {
                timer.Stop();
                runningAnimators.Clear();
                trackContainer.Items.Clear();

                foreach (string file in diag.FileNames)
                {
                    GloProgram prog;
                    try { prog = GloProgram.LoadFromFile(file); }
                    catch(FileFormatException e)
                    {
                        MessageBox.Show(this, "Could not load file '" + file + "':" + Environment.NewLine + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Canvas c = new Canvas();
                    c.Width = 50;
                    c.Height = 50;
                    c.Margin = new Thickness(10);
                    c.Background = new SolidColorBrush(Colors.Black);

                    Label l = new Label();
                    l.Content = new string(System.IO.Path.GetFileNameWithoutExtension(file).Reverse().Take(6).Reverse().ToArray());
                    l.Foreground = new SolidColorBrush(Colors.White);
                    c.Children.Add(l);

                    trackContainer.Items.Add(c);

                    runningAnimators.Add(new SequenceAnimator(c, prog));
                }

                StartFromBeginning();
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            lastTick = DateTime.Now;
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            StartFromBeginning();
        }
    }
}
