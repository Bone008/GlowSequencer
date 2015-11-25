using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Model
{
    public abstract class GloCommand
    {
        public string Name { get; private set; }

        protected GloCommand(string name)
        {
            Name = name;
        }

        public virtual object[] GetArguments()
        {
            return new object[0];
        }

        public virtual IEnumerable<string> ToGloLines()
        {
            if(Name != null)
                yield return string.Join(", ", Enumerable.Repeat(Name, 1).Concat(GetArguments().Select(arg => arg.ToString())));
        }
    }

    public class GloCommandContainer : GloCommand
    {
        public string TerminatorName { get; private set; }
        public List<GloCommand> Commands { get; private set; }

        public GloCommandContainer(string name, string terminatorName)
            : base(name)
        {
            TerminatorName = terminatorName;
            Commands = new List<GloCommand>();
        }

        public override IEnumerable<string> ToGloLines()
        {
            foreach (string line in base.ToGloLines())
                yield return line;

            string prefix = (Name != null ? "    " : ""); // indent only if there is a start block
            foreach (GloCommand subCmd in Commands)
                foreach (string line in subCmd.ToGloLines())
                    yield return prefix + line;

            if(TerminatorName != null)
                yield return TerminatorName;
        }
    }

    public class GloLoopCommand : GloCommandContainer
    {
        public int Repetitions { get; set; }

        public GloLoopCommand(int repetitions)
            : base("L", "E")
        {
            Repetitions = repetitions;
        }

        public override object[] GetArguments()
        {
            return new object[] { Repetitions };
        }
    }

    public class GloDelayCommand : GloCommand
    {
        public int DelayTicks { get; set; }

        public GloDelayCommand(int delayTicks)
            : base("D")
        {
            DelayTicks = delayTicks;
        }

        public override object[] GetArguments()
        {
            return new object[] { DelayTicks };
        }
    }

    public class GloColorCommand : GloCommand
    {
        public GloColor Color { get; set; }

        public GloColorCommand(GloColor color)
            : base("C")
        {
            Color = color;
        }

        public override object[] GetArguments()
        {
            return new object[] { Color.r, Color.g, Color.b };
        }
    }

    public class GloRampCommand : GloCommand
    {
        public GloColor TargetColor { get; set; }
        public int DurationTicks { get; set; }


        public GloRampCommand(GloColor color, int durationTicks)
            : base("RAMP")
        {
            TargetColor = color;
            DurationTicks = durationTicks;
        }

        public override object[] GetArguments()
        {
            return new object[] { TargetColor.r, TargetColor.g, TargetColor.b, DurationTicks };
        }
    }

    // now handled by container start/end commands
    //public class GloEndCommand : GloCommand
    //{
    //    public GloEndCommand()
    //        : base("END")
    //    { }
    //}
}
