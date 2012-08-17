using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace Lab.Util
{
    public class CodeTimer
    {
        public static void Initialize()
        {
           
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Time("", 1, () => { });
        }

        public static void Time(string name, int iteration, Action action,
                                                          Action<long,ulong,IEnumerable<int>> result=null,
                                                           Action<Exception> exceptionLog=null)
        {
            if (String.IsNullOrEmpty(name)) return;

            // 1.
            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            
            if (result == null)
            {
                Console.WriteLine(name);
                result = (time, cpu, gens) =>
                {
                    Console.ForegroundColor = currentForeColor;
                    Console.WriteLine("\tTime Elapsed:\t" + time.ToString("N0") + "ms");
                    Console.WriteLine("\tCPU Cycles:\t" + cpu.ToString("N0"));
                    var i=0;
                    foreach (var item in gens)
                    {
                        Console.WriteLine("\tGen " + i + ": \t\t" + item);
                        i++;
                    }
                    Console.WriteLine();
                };
            }
            if (exceptionLog == null)
            {
                exceptionLog = ex => Console.WriteLine(ex.Message);
            }

            // 2.
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            int[] gcCounts = new int[GC.MaxGeneration + 1];
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                gcCounts[i] = GC.CollectionCount(i);
            }

            // 3.
            Stopwatch watch = new Stopwatch();
            watch.Start();
            ulong cycleCount = GetCycleCount();
            for (int i = 0; i < iteration; i++)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exceptionLog(ex);
                }
            }
            ulong cpuCycles = GetCycleCount() - cycleCount;
            watch.Stop();

            var gensTemp=Enumerable.Range(0, GC.MaxGeneration + 1).Select(i => GC.CollectionCount(i) - gcCounts[i]);
            result(watch.ElapsedMilliseconds, cpuCycles, gensTemp);
        }

        private static ulong GetCycleCount()
        {
            ulong cycleCount = 0;
            QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
            return cycleCount;
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryThreadCycleTime(IntPtr threadHandle, ref ulong cycleTime);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();
    }
}
