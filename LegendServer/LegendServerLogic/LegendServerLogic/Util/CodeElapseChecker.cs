using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using LegendServer.Database;
using LegendServerLogic.Core;
using LegendServer.Database.Profiler;
using LegendProtocol;
using LegendServerLogic.Distributed;

namespace LegendServer.Util
{
    //使用方法见本文档末尾
    public sealed class CodeElapseChecker
    {
        public static void Initialize()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Time("", 1, () => { });
        }

        public static void Time(string name, Action action, int iterationCnt = 1)
        {
            Time(name, iterationCnt, action);
        }

        public static void CountElapse(string name, Action action, int bound, int msgSize, int iterationCnt = 1)
        {
            if (String.IsNullOrEmpty(name)) return;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            int[] gcCounts = new int[GC.MaxGeneration + 1];
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                gcCounts[i] = GC.CollectionCount(i);
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            long cycleCount = GetCycleCount();
            for (int i = 0; i < iterationCnt; i++) action();
            long cpuCycles = GetCycleCount() - cycleCount;
            watch.Stop();

            string gcGen = "";
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                int count = GC.CollectionCount(i) - gcCounts[i];
                gcGen = gcGen + "GenIndex:" + i.ToString() + "," + "Count:" + count.ToString();
                gcGen = gcGen + ";";
            }
            gcGen = gcGen.Substring(0, gcGen.LastIndexOf(';'));

            if (watch.ElapsedMilliseconds >= bound)
            {
                if (!DBManager<ProfilerDB>.Instance.ExistModule())
                {
                    return;
                }
                string serverName = ModuleManager.Get<DistributedMain>().GetMyselfServerName();
                int serverID = ModuleManager.Get<DistributedMain>().GetMyselfServerId();
                ProfilerDB record = DBManager<ProfilerDB>.Instance.GetSingleRecordInCache(element => (element.serverName == serverName && element.serverID == serverID && element.name == name));
                if (record == null)
                {
                    ProfilerDB newRecord = new ProfilerDB();
                    newRecord.serverName = serverName;
                    newRecord.serverID = serverID;
                    newRecord.name = name;
                    newRecord.timeElapsed = watch.ElapsedMilliseconds;
                    newRecord.cpuCycles = cpuCycles;
                    newRecord.gcGeneration = gcGen;
                    newRecord.callCount++;
                    newRecord.msgSize = msgSize;

                    DBManager<ProfilerDB>.Instance.AddRecordToCache(newRecord, element => (element.serverName == serverName && element.serverID == serverID && element.name == name));
                }
                else
                {
                    if (watch.ElapsedMilliseconds > record.timeElapsed)
                    {
                        record.timeElapsed = watch.ElapsedMilliseconds;
                        record.cpuCycles = cpuCycles;
                        record.gcGeneration = gcGen;
                    }
                    if (msgSize > record.msgSize)
                    {
                        record.msgSize = msgSize;
                    }
                    record.callCount++;
                }
            }
        }

        public static void Time(string name, int iteration, Action action)
        {
            if (String.IsNullOrEmpty(name)) return;

            // 1.
            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(name);

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
            long cycleCount = GetCycleCount();
            for (int i = 0; i < iteration; i++) action();
            long cpuCycles = GetCycleCount() - cycleCount;
            watch.Stop();

            // 4.
            Console.ForegroundColor = currentForeColor;
            Console.WriteLine("\tTime Elapsed:\t" + watch.ElapsedMilliseconds.ToString("N0") + "ms");
            Console.WriteLine("\tCPU Cycles:\t" + cpuCycles.ToString("N0"));

            // 5.
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                int count = GC.CollectionCount(i) - gcCounts[i];
                Console.WriteLine("\tGen " + i + ": \t\t" + count);
            }

            Console.WriteLine();

        }

        private static long GetCycleCount()
        {
            //ulong cycleCount = 0;
            //QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
            //return cycleCount;
            return GetCurrentThreadTimes();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetThreadTimes(IntPtr hThread, out long lpCreationTime,
           out long lpExitTime, out long lpKernelTime, out long lpUserTime);

        private static long GetCurrentThreadTimes()
        {
            long l;
            long kernelTime, userTimer;
            GetThreadTimes(GetCurrentThread(), out l, out l, out kernelTime,
               out userTimer);
            return kernelTime + userTimer;
        }


        //[DllImport("kernel32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool QueryThreadCycleTime(IntPtr threadHandle, ref ulong cycleTime);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();

    }
}

