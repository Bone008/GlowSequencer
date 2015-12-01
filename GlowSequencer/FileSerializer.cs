using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer
{
    public class FileSerializer
    {
        private const int TICKS_PER_SECOND = 100;

        public static Timeline LoadFromFile(string filename)
        {

            XDocument doc = XDocument.Load(filename);
            return Timeline.FromXML(doc.Root.Element("timeline"));
        }

        public static void SaveToFile(Timeline timeline, string filename)
        {
            XDocument doc = new XDocument();
            doc.Add(new XElement("sequence",
                new XElement("version", GetProgramVersion()),
                timeline.ToXML()
            ));
            doc.Save(filename);
        }


        public class PrimitiveBlock
        {
            public int startTime, endTime; // endTime is exclusive
            public GloColor startColor, endColor;


            public PrimitiveBlock(float tStart, float tEnd, GloColor colStart, GloColor colEnd)
            {
                startTime = ToTicks(tStart);
                endTime = ToTicks(tEnd);
                startColor = colStart;
                endColor = colEnd;
            }

            public GloColor ColorAt(int tick)
            {
                if (startColor == endColor)
                    return startColor;

                return GloColor.Blend(startColor, endColor, (tick - startTime) / (double)(endTime - startTime));
            }

            public static int ToTicks(float time)
            {
                return (int)Math.Round(time * 100);
            }
        }

        private struct Sample
        {
            public int ticks;
            public GloColor colBefore;
            public GloColor colAfter;
            public PrimitiveBlock blockBefore;
            public PrimitiveBlock blockAfter;
        }

        public static bool ExportGloFiles(Timeline timeline, string filenameBase, string filenameSuffix)
        {
            foreach (var track in timeline.Tracks)
            {
                // Algorithm "back-to-front rendering" := every block paints all affected samples with its data
                // Each sample stores "color up to this point" and "color from this point forward" along with the block that set the respective half.
                // After painting is complete, all samples that just pass through a single block are redundant.

                Sample[] samples = CollectSamples(track.Blocks);
                GloCommandContainer commandContainer = SamplesToCommands(samples);
                OptimizeCommands(commandContainer.Commands);

                // write to file
                string sanitizedTrackName = System.IO.Path.GetInvalidFileNameChars().Aggregate(track.Label, (current, c) => current.Replace(c.ToString(), "")).Replace(' ', '_');
                string file = filenameBase + sanitizedTrackName + filenameSuffix;
                WriteCommands(commandContainer, file);
            }


            // old algorithm for comparison
            //foreach (var track in timeline.Tracks)
            //{
            //    GloCommandContainer container = new GloCommandContainer(null, "END");
            //    try
            //    {
            //        var ctx = new GloSequenceContext(track, container);
            //        ctx.Append(track.Blocks);
            //        ctx.Postprocess();
            //    }
            //    catch (InvalidOperationException e)
            //    {
            //        // TO_DO showing the error as a message box from here breaks layer architecture
            //        System.Windows.MessageBox.Show("Error while exporting track '" + track.Label + "': " + e.Message);
            //        return false;
            //    }

            //    // write to file
            //    string sanitizedTrackName = System.IO.Path.GetInvalidFileNameChars().Aggregate(track.Label, (current, c) => current.Replace(c.ToString(), "")).Replace(' ', '_');
            //    string file = filenameBase + sanitizedTrackName + "_old" + filenameSuffix;
            //    WriteCommands(container, file);
            //}

            return true;
        }

        private static Sample[] CollectSamples(IEnumerable<Block> blocks)
        {
            List<PrimitiveBlock> allBlocks = blocks.SelectMany(b => b.BakePrimitive()).ToList();
            int length = allBlocks.Max(b => b.endTime);

            var samples = new Sample[length + 1];
            for (int i = 0; i < samples.Length; i++)
                samples[i].ticks = i;

            foreach (var primBlock in allBlocks)
            {
                samples[primBlock.startTime].colAfter = primBlock.startColor;
                samples[primBlock.startTime].blockAfter = primBlock;

                for (int tick = primBlock.startTime + 1; tick < primBlock.endTime; tick++)
                {
                    samples[tick].colAfter = primBlock.ColorAt(tick);
                    samples[tick].colBefore = primBlock.ColorAt(tick);
                    samples[tick].blockBefore = primBlock;
                    samples[tick].blockAfter = primBlock;
                }

                samples[primBlock.endTime].colBefore = primBlock.endColor;
                samples[primBlock.endTime].blockBefore = primBlock;
            }

            // samples where the block does not change are redundant
            samples = samples.Where(s => s.blockBefore != s.blockAfter).ToArray();
            
            // make absolutely sure all colors are in [0..255] range
            foreach (var s in samples)
            {
                s.colBefore.Normalize();
                s.colAfter.Normalize();
            }

            return samples;
        }

        private static GloCommandContainer SamplesToCommands(Sample[] samples)
        {
            GloCommandContainer container = new GloCommandContainer(null, "END");
            GloColor lastColor = GloColor.Black;
            int lastTick = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                int delay = samples[i].ticks - lastTick;
                if (delay > 0)
                    if (samples[i].colBefore == lastColor)
                        container.Commands.Add(new GloDelayCommand(delay));
                    else
                    {
                        container.Commands.Add(new GloRampCommand(samples[i].colBefore, delay));
                        lastColor = samples[i].colBefore;
                    }

                if (samples[i].colAfter != lastColor)
                {
                    container.Commands.Add(new GloColorCommand(samples[i].colAfter));
                    lastColor = samples[i].colAfter;
                }

                lastTick = samples[i].ticks;
            }

            return container;
        }

        private static void OptimizeCommands(List<GloCommand> commands)
        {
            // premise: the command list is still flat and does not contain any loops

            var sw = new Stopwatch();
            sw.Start();

            // optimize by detecting loops

            List<string> allLines = new List<string>(commands.Select(cmd => cmd.ToGloLines().Single())); // flat hierarchy ==> one line per command
            //List<string> lineBuffer = new List<string>(commands.Count / 2);
            //List<string> lineBuffer2 = new List<string>(commands.Count / 2);
            for (int loopPeriod = 2; loopPeriod <= commands.Count / 2; loopPeriod++)
            {
                for (int loopStart = 0; loopStart <= commands.Count - 2 * loopPeriod; loopStart++)
                {
                    //lineBuffer.Clear();
                    //lineBuffer.AddRange(Enumerable.Range(loopStart, loopPeriod).Select(i => allLines[i]));

                    int possibleRepetitions = (commands.Count - loopStart) / loopPeriod - 1;
                    int rep = 1;
                    while (rep <= possibleRepetitions)
                    {
                        //lineBuffer2.Clear();
                        //lineBuffer2.AddRange(Enumerable.Range(loopStart + loopPeriod * rep, loopPeriod).Select(i => allLines[i]));

                        int comparisonStart = loopStart + loopPeriod * rep;

                        if (RangeEqual(allLines, loopStart, comparisonStart, loopPeriod))
                            rep++;
                        else
                            break;
                    }

                    if (rep > 1)
                    {
                        Debug.WriteLine("found loop of period {0}, looping x{1}, starting at {2}", loopPeriod, rep, loopStart);
                        GloLoopCommand loop = new GloLoopCommand(rep);
                        loop.Commands.AddRange(Enumerable.Range(loopStart, loopPeriod).Select(i => commands[i]));

                        GloLoopCommand loop2 = null;

                        if (rep > 255)
                        {
                            int wrappedWhole = rep / 255;
                            int wrappedRemainder = rep % 255;

                            // after this: loop  --> multiples of 255
                            //             loop2 --> remainder [optional]

                            if (wrappedRemainder > 0)
                            {
                                loop2 = new GloLoopCommand(wrappedRemainder);
                                loop2.Commands.AddRange(loop.Commands);
                            }

                            loop.Repetitions = 255;
                            if (wrappedWhole > 1)
                            {
                                GloLoopCommand outerLoop = new GloLoopCommand(wrappedWhole);
                                outerLoop.Commands.Add(loop);
                                loop = outerLoop;
                            }
                        }

                        commands.RemoveRange(loopStart, rep * loopPeriod);
                        allLines.RemoveRange(loopStart, rep * loopPeriod);

                        commands.Insert(loopStart, loop);
                        allLines.Insert(loopStart, Guid.NewGuid().ToString()); // insert dummy line that won't match again to keep indices in sync
                        if (loop2 != null)
                        {
                            loopStart++;
                            commands.Insert(loopStart, loop2);
                            allLines.Insert(loopStart, Guid.NewGuid().ToString());
                        }
                    }
                }
            }

            sw.Stop();
            Debug.WriteLine("Loop optimization complete. Time: {0} ms", sw.ElapsedMilliseconds);

            // possible code optimization: extract Tuple<GloCommand, string> beforehand to minimize calls to ToGloLines()
        }

        private static void WriteCommands(GloCommandContainer container, string file)
        {
            // convert commands to their string representation
            var commandStrings = container.ToGloLines();

            var lines = Enumerable.Repeat("; Generated by Glow Sequencer version " + GetProgramVersion() + " at " + DateTime.Now + ".", 1).Concat(commandStrings);
            System.IO.File.WriteAllLines(file, lines, Encoding.ASCII);
        }

        private static bool RangeEqual<T>(IList<T> list, int startA, int startB, int count)
        {
            for (int i = 0; i < count; i++)
                if (!EqualityComparer<T>.Default.Equals(list[startA + i], list[startB + i]))
                    return false;

            return true;
        }

        //private static GloCommandContainer ExportCommands(Timeline timeline, Track track, IEnumerable<Block> blocks)
        //{
        //    GloCommandContainer allCommands = new GloCommandContainer("MAIN");
        //    int currentTicks = 0;
        //    float tickFractionsAcc = 0;

        //    float currentTime = 0;

        //    foreach (var block in blocks.SelectMany(b => FlattenBlock(b, track)))
        //    {
        //        float delayTime = block.StartTime - currentTime;
        //        currentTime = block.StartTime;

        //        if (delayTime < 0)
        //            throw new InvalidOperationException("blocks are overlapping at " + currentTime + " s");

        //        int gapTicks = DelayTicks(delayTime, ref currentTicks, ref tickFractionsAcc);
        //        if (gapTicks > 0)
        //            allCommands.Commands.Add(new GloDelayCommand(gapTicks));

        //        if (block is ColorBlock)
        //        {
        //            allCommands.Commands.Add(new GloColorCommand(((ColorBlock)block).Color));
        //            allCommands.Commands.Add(new GloDelayCommand(DelayTicks(block.Duration, ref currentTicks, ref tickFractionsAcc)));
        //            allCommands.Commands.Add(new GloColorCommand(GloColor.Black));

        //            currentTime = block.GetEndTime();
        //        }
        //        else if (block is RampBlock)
        //        {
        //            allCommands.Commands.Add(new GloColorCommand(((RampBlock)block).StartColor));
        //            allCommands.Commands.Add(new GloRampCommand(((RampBlock)block).EndColor, DelayTicks(block.Duration, ref currentTicks, ref tickFractionsAcc)));
        //            allCommands.Commands.Add(new GloColorCommand(GloColor.Black));

        //            currentTime = block.GetEndTime();
        //        }
        //        else
        //        {
        //            throw new NotImplementedException("unknown block type: " + block.GetType());
        //        }
        //    }

        //    return allCommands;
        //}

        //private static IEnumerable<Block> FlattenBlock(Block b, Track track)
        //{
        //    if (b is GroupBlock)
        //        return ((GroupBlock)b).Children.Where(child => child.Tracks.Contains(track)).SelectMany(child => FlattenBlock(child, track));
        //    else
        //        return Enumerable.Repeat(b, 1);
        //}


        //private static void PostprocessCommands(GloCommandContainer container)
        //{
        //    // - merge subsequent color commands
        //    // - merge subsequent delay commands
        //    for (int i = 0; i < container.Commands.Count - 1; i++)
        //    {
        //        GloCommand current = container.Commands[i];
        //        GloCommand next = container.Commands[i + 1];

        //        if (current is GloColorCommand && next is GloColorCommand)
        //        {
        //            // the first color is immediately overwritten, so it can be removed
        //            container.Commands.RemoveAt(i);
        //            i--;
        //        }
        //        else if (current is GloDelayCommand && next is GloDelayCommand)
        //        {
        //            // merge next onto current and remove next
        //            ((GloDelayCommand)current).DelayTicks += ((GloDelayCommand)next).DelayTicks;

        //            container.Commands.RemoveAt(i + 1);
        //            i--;

        //            // TO_DO insert comment stating the unmerged delays
        //        }
        //    }
        //}



        //public static void ExportGloFiles_Legacy(Timeline timeline, string filenameBase, string filenameSuffix)
        //{
        //    foreach (var track in timeline.Tracks)
        //    {
        //        GloCommandContainer allCommands = new GloCommandContainer(null, "END");
        //        int currentTicks = 0;
        //        float tickFractionsAcc = 0;

        //        float currentTime = 0;

        //        foreach (var block in track.Blocks.OrderBy(b => b.StartTime))
        //        {
        //            float delayTime = block.StartTime - currentTime;
        //            currentTime = block.StartTime;

        //            int gapTicks = DelayTicks(delayTime, ref currentTicks, ref tickFractionsAcc);
        //            if (gapTicks > 0)
        //                allCommands.Commands.Add(new GloDelayCommand(gapTicks));


        //            if (block is ColorBlock)
        //            {
        //                allCommands.Commands.Add(new GloColorCommand(((ColorBlock)block).Color));
        //                allCommands.Commands.Add(new GloDelayCommand(DelayTicks(block.Duration, ref currentTicks, ref tickFractionsAcc)));
        //                allCommands.Commands.Add(new GloColorCommand(GloColor.Black));

        //                currentTime = block.GetEndTime();
        //            }
        //            else if (block is RampBlock)
        //            {
        //                allCommands.Commands.Add(new GloColorCommand(((RampBlock)block).StartColor));
        //                allCommands.Commands.Add(new GloRampCommand(((RampBlock)block).EndColor, DelayTicks(block.Duration, ref currentTicks, ref tickFractionsAcc)));
        //                allCommands.Commands.Add(new GloColorCommand(GloColor.Black));

        //                currentTime = block.GetEndTime();
        //            }
        //        }


        //        // postprocess command structure:
        //        // - merge subsequent color commands
        //        // - merge subsequent delay commands
        //        for (int i = 0; i < allCommands.Commands.Count - 1; i++)
        //        {
        //            GloCommand current = allCommands.Commands[i];
        //            GloCommand next = allCommands.Commands[i + 1];

        //            if (current is GloColorCommand && next is GloColorCommand)
        //            {
        //                // the first color is immediately overwritten, so it can be removed
        //                allCommands.Commands.RemoveAt(i);
        //                i--;
        //            }
        //            else if (current is GloDelayCommand && next is GloDelayCommand)
        //            {
        //                // merge next onto current and remove next
        //                ((GloDelayCommand)current).DelayTicks += ((GloDelayCommand)next).DelayTicks;

        //                allCommands.Commands.RemoveAt(i + 1);
        //                i--;

        //                // TO_DO insert comment stating the unmerged delays
        //            }
        //        }

        //        /*allCommands.Commands.Add(new GloEndCommand()); make it compile */

        //        // convert commands to their string representation
        //        var commandStrings = allCommands.Commands.Select(cmd => string.Join(", ", Enumerable.Repeat(cmd.Name, 1).Concat(cmd.GetArguments().Select(arg => arg.ToString()))));

        //        var lines = Enumerable.Repeat("; Generated by Glow Sequencer version " + GetProgramVersion() + " at " + DateTime.Now + ".", 1).Concat(commandStrings);

        //        // write to file
        //        string sanitizedTrackName = System.IO.Path.GetInvalidFileNameChars().Aggregate(track.Label, (current, c) => current.Replace(c.ToString(), "")).Replace(' ', '_');
        //        string file = filenameBase + sanitizedTrackName + filenameSuffix;

        //        System.IO.File.WriteAllLines(file, lines, Encoding.ASCII);
        //    }
        //}

        //private static int DelayTicks(float delayTime, ref int currentTicks, ref float tickFractionsAcc)
        //{
        //    float exactTicks = delayTime * TICKS_PER_SECOND;
        //    int delayTicks = (int)exactTicks;

        //    tickFractionsAcc += exactTicks - delayTicks;

        //    int fractionBias = (int)tickFractionsAcc;
        //    if (fractionBias > 0)
        //    {
        //        // a whole tick was accumulated by now, so we add it to the current delay and adjust the accumlator
        //        tickFractionsAcc -= fractionBias;
        //        delayTicks += fractionBias;
        //    }

        //    currentTicks += delayTicks;

        //    return delayTicks;
        //}


        private static string GetProgramVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
