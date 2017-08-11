namespace ShoYoo.Kon.GameBase.Debug
{
    using ShoYoo.Engine.Utils;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Linq;

    //public class ThreadMonitor
    //{
    //    private DateTime LastLoopTime = default(DateTime);
    //    private DateTime LastDeadLock = default(DateTime);
    //    Thread thread = null;
    //    bool Running = false;
    //    private int m_deadlock_ms = 0;
    //    private int m_check_ms = 0;

    //    public ThreadMonitor(int deadlock_ms = 3000, int check_ms = 1000)
    //    {
    //        m_deadlock_ms = deadlock_ms;
    //        m_check_ms = check_ms;
    //    }

    //    public void Start()
    //    {
    //        LastLoopTime = DateTime.Now;
    //        thread = Thread.CurrentThread;
    //        Thread t = new Thread(Monitor);
    //        Running = true;
    //        t.Start();
    //    }

    //    public void Stop()
    //    {
    //        Running = false;
    //    }

    //    public void Loop()
    //    {
    //        LastLoopTime = DateTime.Now;
    //    }

    //    public void Monitor()
    //    {
    //        while (Running)
    //        {
    //            DateTime now = DateTime.Now;
    //            if ((now - LastLoopTime).TotalMilliseconds > m_deadlock_ms && LastDeadLock != LastLoopTime)
    //            {
    //                LastDeadLock = LastLoopTime;
    //                Log.Error("");

    //                try
    //                {
    //                    thread.Suspend();
    //                    StackTrace stack = new StackTrace(thread, true);
    //                    Log.Error("Server deadlock" + Environment.NewLine + stack.ToString());
    //                }
    //                catch (Exception ex)
    //                {
    //                    Log.Error("Server deadlock, but can't get stack trace infomation.");
    //                }
    //                finally
    //                {
    //                    thread.Resume();
    //                }
    //            }
    //            Thread.Sleep(m_check_ms);
    //        }
    //    }
    //}

    public static class ThreadMonitor
    {
        public static int m_deadlock_ms = 3000;
        public static int m_check_ms = 1000;

        private static ConcurrentDictionary<Thread, MonitoredThread> MonitoredThreads
            = new ConcurrentDictionary<Thread, MonitoredThread>();

        private static Thread monitor_thread = null;
        private static bool Running = false;

        public static void Trace()
        {
            if (monitor_thread == null)
            {
                Start();

                return;
            }

            Thread current_thread = Thread.CurrentThread;
            MonitoredThread monitored_thread = null;
            if (!MonitoredThreads.TryGetValue(current_thread, out monitored_thread))
            {
                MonitoredThread mt = new MonitoredThread();
                mt.Thread = current_thread;
                mt.LastLoopTime = DateTime.Now;

                MonitoredThreads.AddOrUpdate(current_thread, mt, (t, old) => mt);

                return;
            }

            monitored_thread.LastLoopTime = DateTime.Now;
        }
        public static void Start()
        {
            if (!Running)
            {
                monitor_thread = new Thread(Monitor);
                Running = true;
                monitor_thread.Start();
            }
        }

        public static void Stop()
        {
            if (Running)
                Running = false;
        }


        private static void Monitor()
        {
            while (Running)
            {
                MonitoredThread[] monitored_threads = MonitoredThreads.Values.ToArray();

                foreach (MonitoredThread monitored_thread in monitored_threads)
                {
                    DateTime now = DateTime.Now;
                    if ((now - monitored_thread.LastLoopTime).TotalMilliseconds > m_deadlock_ms
                        && monitored_thread.LastDeadLock != monitored_thread.LastLoopTime)
                    {
                        monitored_thread.LastDeadLock = monitored_thread.LastLoopTime;

                        try
                        {
                            monitored_thread.Thread.Suspend();
                            StackTrace stack = new StackTrace(monitored_thread.Thread, true);
                            Log.Error("Server detected deadlock" + Environment.NewLine + stack.ToString().TrimEnd());
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Server detected deadlock, but can't get stack trace infomation.");
                        }
                        finally
                        {
                            monitored_thread.Thread.Resume();
                        }
                    }
                }
                Thread.Sleep(m_check_ms);
            }
        }
    }

    public class MonitoredThread
    {
        public Thread Thread = null;
        public DateTime LastLoopTime = default(DateTime);
        public DateTime LastDeadLock = default(DateTime);
    }
}
