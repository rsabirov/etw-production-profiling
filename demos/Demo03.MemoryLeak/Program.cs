using System;
using System.Collections.Generic;

namespace Demo03.MemoryLeak
{
    /// <summary>
    /// A simple program that continuously creates new arrays and emulates memory leak.
    /// </summary>
    class Program
    {
        private static readonly List<int[]> arrays = new List<int[]>();
        private static readonly Random Rand = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine("This application tests ETW events.");
            Console.WriteLine("To run with logging, use PerfView (search for 'PerfView Download' for download)");
            Console.WriteLine(@"Run PerfView with default setting and run this demo app for few minutes and after check GCStat report in PerfView.");
            Console.WriteLine(@"Also you can try to run PerfView with addition GC related provider.");

            Console.WriteLine("Press any key to exit...");

            while (!Console.KeyAvailable)
            {
                int size = Rand.Next(1024, 100000);
                int[] newArray = new int[size];

                arrays.Add(newArray);

                System.Threading.Thread.Sleep(10);
            }

            Console.WriteLine("Done, exiting");
        }
    }
}