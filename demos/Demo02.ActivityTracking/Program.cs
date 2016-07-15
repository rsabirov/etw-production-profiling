using System;
using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Demo02.ActivityTracking
{
    //Microsoft sample, source: https://blogs.msdn.microsoft.com/vancem/2015/09/15/eventsource-activity-support-demo-code/

    /*******************************************************************************/
    /// <summary>
    /// Here is a EventSource that shows the use of .NET V4.6 feature of EventSource Activities.
    /// 
    /// All you need to use EventSource Activities is to name events with the 'Start' and
    /// 'Stop' suffixes, and place these events at the start and stop of the activity.  
    /// 
    /// EventSource will automatically assign an activity ID (in it path format (e.g. //1/1/33/3))
    /// and will mark all events caused by that activity with this ID (or a 'sub-id' that
    /// has a suffix which represents nested activities).  
    /// 
    /// To see this in action simply turn on this EventSource with PerfView like
    /// 
    ///     PerfView /OnlyProviders=*MyActivitySource collect 
    ///     
    /// If you use other collectors you must turn on the .NET Task Library provider 
    /// (Guid 2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5), Keyword TasksFlowActivityIds (0x80) 
    /// </summary>
    [EventSource(Name = "MyActivitySource")]
    public class MyActivitySource : EventSource
    {
        /// <summary>
        /// My one and only instance of MyActivitySource that can log events.  
        /// </summary>
        public static MyActivitySource Log = new MyActivitySource();

        /// <summary>
        /// Useful for simple, ad-hoc print style logging.  
        /// </summary>
        /// <param name="message"></param>
        public void DebugMessage(string message) { WriteEvent(1, message); }

        public void RequestStart(string url, int requestNum) { WriteEvent(2, url, requestNum); }
        public void RequestStop(bool success) { WriteEvent(3, success); }
        
        public void Phase1Start(string message) { WriteEvent(4, message); }
        public void Phase1Stop() { WriteEvent(5); }

        public void Phase2Start(string message) { WriteEvent(6, message); }
        public void Phase2Stop() { WriteEvent(7); }

        public void SecurityStart(string userName) { WriteEvent(8, userName); }
        public void SecurityStop() { WriteEvent(9); }

        // These will show what happens when in the common case where an error path causes you to loose a stop. 
        public void BadStart() { WriteEvent(10); }
        public void BadStop() { WriteEvent(11); }
    }

    /// <summary>
    /// A simple demo of using the new Activity feature of EventSource (Available in version 4.6 of the .NET runtime)
    /// You should turn on the EventSource (e.g. with PerfView /Providers=*Microsoft-Demos-Activities collect) to see 
    /// something useful.  Then look at the 'events' view and look at the ActivityIDs for the events.   You can also
    /// look at the 'StartStopTree view 
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            MyActivitySource.Log.DebugMessage("Starting Demo");

            Console.WriteLine("Starting Demo");
            Console.WriteLine("To collect events, run following command before running the demo: PerfView /Providers=*MyActivitySource collect");
            Run();
            MyActivitySource.Log.DebugMessage("Done Demo");
            Console.WriteLine("Done Demo");
        }

        /// <summary>
        /// Runs the server as a whole.   It just runs 20 requests concurrently.  
        /// </summary>
        public static void Run()
        {
            MyActivitySource.Log.DebugMessage("Service Starting");
            Parallel.For(0, 20, delegate (int i)
            {
                MyActivitySource.Log.DebugMessage("Processing Request");

                /// As with many services, we catch exception and go on to the next request. 
                try { ProcessRequest(i).Wait(); }
                catch (Exception) { }
            });
            MyActivitySource.Log.DebugMessage("Service Done");
        }

        /// <summary>
        /// Simulates the processing of a single request.   We happen to be async.  
        /// </summary>
        private async static Task<bool> ProcessRequest(int reqNum)
        {
            MyActivitySource.Log.RequestStart("SomeURL", reqNum);

            // Simulates disk I/O or database calls 
            await Task.Delay(random.Next(20, 30));

            //********************************************************************
            // Starting Phase 1 (a child activity)
            MyActivitySource.Log.Phase1Start("Starting Phase 1");

            // Simulates sub-work.  
            await DoSecurity();

            // Does more async work (we are probably hopping threads)
            HttpClient httpClient = new HttpClient();
            var bing = await httpClient.GetAsync("http://www.bing.com");

            MyActivitySource.Log.Phase1Stop();

            //********************************************************************
            // Bad Work:  simulates disk I/O or database calls 
            BadWork(reqNum);

            // More work (that causes thread hops)
            await Task.Delay(random.Next(20, 30));

            //********************************************************************
            // Starting Phase 2 (a sibling activity)
            MyActivitySource.Log.Phase2Start("Starting Phase 2");

            // Do a sub-activity
            await DoSecurity();

            // Do some more work 
            var ms = await httpClient.GetAsync("http://www.microsoft.com");

            MyActivitySource.Log.Phase2Stop();

            //********************************************************************
            bool success = bing.IsSuccessStatusCode && ms.IsSuccessStatusCode;

            // If convenient, put this in a try-catch so that under all conditions
            // there is a stop for every start.   However even if that is not true
            // The system can recover from it.  
            MyActivitySource.Log.RequestStop(success);
            return success;
        }

        /// <summary>
        /// A surrogate for some sub-activity.  Most web sites have a non-trivial security aspect
        /// so I called it 'DoSecurity'.   We make it async because it is likely
        /// to do I/O. 
        /// </summary>
        /// <returns></returns>
        private static async Task DoSecurity()
        {
            MyActivitySource.Log.SecurityStart("MyUser");

            MyActivitySource.Log.DebugMessage("This is some random place in the security code.");

            await Task.Delay(random.Next(20, 30));
            Thread.SpinWait(100000);
            MyActivitySource.Log.DebugMessage("After fetching security stuff.");

            MyActivitySource.Log.SecurityStop();
        }

        /// <summary>
        /// Bad work will throw an exception on request 5.  This will cause missing stops
        /// (since we did not put them in try-finally but this is OK in many cases.   
        /// </summary>
        private static void BadWork(int reqNum)
        {
            MyActivitySource.Log.BadStart();

            if (reqNum == 5)
            {
                MyActivitySource.Log.DebugMessage("Bad Work Exception");
                throw new NotImplementedException();
            }
            MyActivitySource.Log.BadStop();
        }

        private static readonly Random random = new Random(0);
    }
}