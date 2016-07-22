using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectrumGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            const int numValues = 5;
            const int numSaturation = 5;
            const int numHues = 30;


            var combinations = from sat in Enumerable.Range(1, numSaturation).Select(i => i * 1.0 / numSaturation)
                               from hue in Enumerable.Range(0, numHues).Select(i => i * 360.0 / numHues)
                               from value in Enumerable.Range(1, numValues).Select(i => i * 1.0 / numValues)
                               select Tuple.Create(sat, hue, value);

            combinations.ToList().ForEach(c => Console.Write(c.Item1+"|"+c.Item2+"|"+c.Item3 + "\t"));

            Console.WriteLine();
            Console.WriteLine();

            var result2 = from tpl in combinations
                          let sat = tpl.Item1
                          let hue = tpl.Item2
                          let value = tpl.Item3
                          select ColorUtil.ColorFromHSV(hue, sat, value);

            result2.Select(c => c.ToArgb().ToString("x").Substring(2)).ToList().ForEach(c => Console.Write(c + "\t"));

            Console.ReadKey(true);

            Console.WriteLine();

            combinations.ToList().ForEach(c => { Console.Write("                               \r"); Console.Write(c.Item1 + "|" + c.Item2 + "|" + c.Item3 + "\r"); System.Threading.Thread.Sleep(50); });
        }
    }
}
