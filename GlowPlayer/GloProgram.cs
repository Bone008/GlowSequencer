using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowPlayer
{
    public struct GloColor
    {
        public int r;
        public int g;
        public int b;

        private GloColor(int r, int g, int b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public static GloColor operator *(double a, GloColor c)
        {
            int r = (int)Math.Round(a * c.r);
            int g = (int)Math.Round(a * c.g);
            int b = (int)Math.Round(a * c.b);
            return GloColor.FromRGB(r, g, b);
        }

        public static GloColor operator+(GloColor c1, GloColor c2)
        {
            return GloColor.FromRGB(c1.r + c2.r, c1.g + c2.g, c1.b + c2.b);
        }

        public static GloColor FromRGB(int r, int g, int b)
        {
            return new GloColor(r, g, b);
        }
        public static GloColor Blend(GloColor c1, GloColor c2, double pct)
        {
            if (pct <= 0)
                return c1;
            if (pct >= 1)
                return c2;

            return (1 - pct) * c1 + pct * c2;
        }
    }

    class GloProgram
    {

        public GloCommandContainer Root { get; private set; }

        public GloProgram(GloCommandContainer root)
        {
            this.Root = root;
        }

        public static GloProgram LoadFromFile(string file)
        {
            Stack<GloCommandContainer> openContainers = new Stack<GloCommandContainer>();
            openContainers.Push(new GloCommandContainer("PROGRAM"));
            bool seal = false;

            int lineNum = 0;
            foreach (string line in File.ReadAllLines(file, Encoding.UTF8))
            {
                lineNum++;

                int commentIndex = line.IndexOf(';');
                string commandLine = (commentIndex >= 0 ? line.Substring(0, commentIndex) : line).Trim();
                if (commandLine == "")
                    continue;

                string[] tokens = commandLine.Split(',');
                string cmd = tokens[0];

                switch (cmd)
                {
                    case "L":
                        openContainers.Push(new GloLoop(GetIntArg(tokens, 1)));
                        break;
                    case "E":
                        if (!(openContainers.Peek() is GloLoop))
                            throw new FileFormatException("no open loop to close");

                        GloLoop finishedLoop = (GloLoop)openContainers.Pop();
                        openContainers.Peek().Commands.Add(finishedLoop);
                        break;

                    case "D":
                        openContainers.Peek().Commands.Add(new GloDelayCommand(GetTimeArg(tokens, 1)));
                        break;

                    case "C":
                        openContainers.Peek().Commands.Add(new GloColorCommand(GetColorArg(tokens, 1)));
                        break;

                    case "RAMP":
                        openContainers.Peek().Commands.Add(new GloRampCommand(GetColorArg(tokens, 1), GetTimeArg(tokens, 4)));
                        break;

                    case "END":
                        seal = true;
                        break;
                }
            }

            if (openContainers.Count > 1)
                throw new FileFormatException("program ended with unclosed section: " + openContainers.Peek().GetType().Name);
            if (!seal)
                throw new FileFormatException("program was not terminated with END");

            return new GloProgram(openContainers.Pop());
        }


        private static string GetStringArg(string[] tokens, int i)
        {
            if (tokens.Length <= i)
                new FileFormatException("not enough arguments for command");

            return tokens[i];
        }
        private static int GetIntArg(string[] tokens, int i)
        {
            int n;
            if (!int.TryParse(GetStringArg(tokens, i), out n))
                throw new FileFormatException("command expected number");

            return n;
        }

        private static GloColor GetColorArg(string[] tokens, int i)
        {
            GloColor col = new GloColor();
            col.r = GetIntArg(tokens, i + 0);
            col.g = GetIntArg(tokens, i + 1);
            col.b = GetIntArg(tokens, i + 2);

            return col;
        }

        private static TimeSpan GetTimeArg(string[] tokens, int i)
        {
            return TimeSpan.FromMilliseconds(GetIntArg(tokens, i) * 10);
        }

    }

    abstract class GloCommand
    {
        public string Name { get; private set; }

        protected GloCommand(string name)
        {
            Name = name;
        }
    }

    class GloCommandContainer : GloCommand
    {
        public List<GloCommand> Commands { get; private set; }

        public GloCommandContainer(string name) : base(name)
        {
            Commands = new List<GloCommand>();
        }
    }

    class GloLoop : GloCommandContainer
    {
        public int Repetitions { get; private set; }

        public GloLoop(int repetitions) : base("L")
        {
            Repetitions = repetitions;
        }
    }

    class GloDelayCommand : GloCommand
    {
        public TimeSpan Delay { get; private set; }

        public GloDelayCommand(TimeSpan delay) : base("D")
        {
            Delay = delay;
        }
    }

    class GloColorCommand : GloCommand
    {
        public GloColor Color { get; private set; }

        public GloColorCommand(GloColor color) : base("C")
        {
            Color = color;
        }
    }

    class GloRampCommand : GloCommand
    {
        public GloColor TargetColor { get; private set; }
        public TimeSpan Duration { get; private set; }


        public GloRampCommand(GloColor color, TimeSpan duration) : base("RAMP")
        {
            TargetColor = color;
            Duration = duration;
        }
    }

}
