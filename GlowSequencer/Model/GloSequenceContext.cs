using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Model
{
    public class GloSequenceContext
    {
        private const int TICKS_PER_SECOND = 100;

        private int _currentTicks = 0;
        private float _tickFractionsAcc = 0;
        //private float _currentTime = 0; // keep track of this independently to prevent rounding errors
        // note tat this stays unaffected by Advance(...)

        private float _currentTime { get { return CurrentTime; } set { } }

        private Track _track;
        private GloCommandContainer commandContainer;

        /// <summary>The track that is being exported.</summary>
        public Track Track { get { return _track; } }

        /// <summary>Exact time where we are currently at.</summary>
        //public float CurrentTime { get { return _currentTime; } }
        public float CurrentTime { get { return (_currentTicks + _tickFractionsAcc) / TICKS_PER_SECOND; } }


        public GloSequenceContext(Track track, GloCommandContainer container)
        {
            _track = track;
            commandContainer = container;
        }

        public void Append(IEnumerable<Block> blocks)
        {
            foreach (var block in blocks.OrderBy(b => b.StartTime))
            {
                float delayTime = block.StartTime - _currentTime;

                if (delayTime < 0)
                {
                    if (delayTime > (-1.0f / TICKS_PER_SECOND))
                        delayTime = 0;
                    else
                        throw new InvalidOperationException("blocks are overlapping at " + CurrentTime + " s");
                }

                commandContainer.Commands.Add(Advance(delayTime).AsCommand());

                IEnumerable<GloCommand> cmds = block.ToGloCommands(this);
                commandContainer.Commands.AddRange(cmds);

                _currentTime = block.GetEndTime();
            }
        }

        public GloSequenceContextDelay Advance(float delayTime)
        {
            float exactTicks = delayTime * TICKS_PER_SECOND;
            int delayTicks = (int)exactTicks;

            _tickFractionsAcc += exactTicks - delayTicks;

            int fractionBias = (int)_tickFractionsAcc;
            if (fractionBias > 0)
            {
                // a whole tick was accumulated by now, so we add it to the current delay and adjust the accumlator
                _tickFractionsAcc -= fractionBias;
                delayTicks += fractionBias;
            }

            _currentTicks += delayTicks;
            return new GloSequenceContextDelay(delayTicks);
        }

        public void Postprocess()
        {
            // - delete 0 length delays
            for (int i = 0; i < commandContainer.Commands.Count; i++)
            {
                GloCommand current = commandContainer.Commands[i];

                if (current is GloDelayCommand && ((GloDelayCommand)current).DelayTicks <= 0)
                {
                    if (((GloDelayCommand)current).DelayTicks < 0)
                        throw new Exception("a negative delay command managed to get into the postprocessing stage");

                    commandContainer.Commands.RemoveAt(i);
                    i--;
                }
            }


            // - merge subsequent color commands
            // - merge subsequent delay commands
            // - remove redundant color commands after a ramp
            bool modified;
            int passes = 0;
            do
            {
                modified = false;
                passes++;

                for (int i = 0; i < commandContainer.Commands.Count - 1; i++)
                {
                    GloCommand current = commandContainer.Commands[i];
                    GloCommand next = commandContainer.Commands[i + 1];

                    if (current is GloColorCommand && next is GloColorCommand)
                    {
                        // the first color is immediately overwritten, so it can be removed
                        commandContainer.Commands.RemoveAt(i);
                        i--;
                        modified = true;
                    }
                    else if (current is GloDelayCommand && next is GloDelayCommand)
                    {
                        // merge next onto current and remove next
                        ((GloDelayCommand)current).DelayTicks += ((GloDelayCommand)next).DelayTicks;

                        commandContainer.Commands.RemoveAt(i + 1);
                        i--;
                        modified = true;

                        // TODO insert comment stating the unmerged delays
                    }
                    else if (current is GloRampCommand && next is GloColorCommand && ((GloRampCommand)current).TargetColor == ((GloColorCommand)next).Color)
                    {
                        // color command is redundant, because the ramp already ended at that color
                        commandContainer.Commands.RemoveAt(i + 1);
                        i--;
                        modified = true;
                    }
                }
            } while (modified);

            System.Diagnostics.Debug.WriteLine("Postprocessing took {0} passes.", passes);
        }

    }


    public class GloSequenceContextDelay
    {
        private int _ticks;

        public int Ticks { get { return _ticks; } }

        internal GloSequenceContextDelay(int ticks)
        {
            _ticks = ticks;
        }

        public GloDelayCommand AsCommand()
        {
            return new GloDelayCommand(_ticks);
        }
    }
}
