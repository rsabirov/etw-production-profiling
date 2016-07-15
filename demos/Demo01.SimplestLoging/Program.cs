using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Demo01.SimplestLoging
{
    class Program
    {
        /// <summary>
        /// A simple program that creates a new ETW event source and generates a few (strongly typed) events.
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("This application tests ETW events.");
            Console.WriteLine("To run with logging, use PerfView (search for 'PerfView Download' for download)");
            Console.WriteLine(@"PerfView /OnlyProviders=*MinimalEventSource,*MyCompany run Demo01.SimplestLoging.exe");

            // If you wish to catch errors during construction you need to do it explicitly By default
            // Exceptions are not thrown.  Thus in debug code at least you should be checking for failure.
            // If you have a good place to log the error, you should do that. 
#if DEBUG
            if (MinimalEventSource.Log.ConstructionException != null)
                throw MinimalEventSource.Log.ConstructionException;
#endif
            MinimalEventSource.Log.Load(10, "MyFile");

            MinimalEventSource.Log.Load(11, "AnotherFile");
            MinimalEventSource.Log.SendEnums(MyColor.Blue, MyFlags.Flag2 | MyFlags.Flag3);
            MinimalEventSource.Log.Message("This is a message.");
            MinimalEventSource.Log.SetOther(true, 123456789);

            // Show how fast we can log, and also demonstrate that no object are allocated
            const int eventsCount = 10000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
                MinimalEventSource.Log.HighFreq(i);
            sw.Stop();
            Console.WriteLine("{0} events took {1:f3} ms to log.", eventsCount, sw.Elapsed.TotalMilliseconds);

            Console.WriteLine("Hit return to exit");
            Console.ReadLine();
        }
    }

    internal sealed class MinimalEventSource : EventSource
    {
        public static MinimalEventSource Log = new MinimalEventSource();

        public void Load(long imageBase, string name)
        {
            WriteEvent(1, imageBase, name);
        }

        public void Unload(long imageBase)
        {
            WriteEvent(2, imageBase);
        }

        public void SetGuid(Guid MyGuid)
        {
            WriteEvent(3, MyGuid);
        }

        public void SendEnums(MyColor color, MyFlags flags)
        {
            WriteEvent(4, (int)color, (int)flags);
        }    // Cast enums to int for efficient logging.  

        public void Message(string message)
        {
            WriteEvent(5, message);
        }

        public void SetOther(bool flag, int myInt)
        {
            WriteEvent(6, flag, myInt);
        }

        public void HighFreq(int value)
        {
            if (IsEnabled())
                WriteEvent(7, value);
        }
    }

    internal enum MyColor
    {
        Red,
        Blue,
        Green,
    }

    [Flags]
    internal enum MyFlags
    {
        Flag1 = 1,
        Flag2 = 2,
        Flag3 = 4,
    }
}