using System;
using System.Collections.Concurrent;
using System.Threading;
using LegendProtocol;
using LegendServer.LocalConfig;
using LegendServer.Util;
using System.Diagnostics;

namespace LegendServerProxy.Core
{
    //服务器的中央处理器(双线程)
    class ServerCPU
    {
        public Thread workThread = null;
        public Thread profilerThread = null;
        public Thread tokenCheckThread = null;
        private static ServerCPU instance = null;
        private static object singletonLocker = new object();//单例双检锁
        private ConcurrentQueue<Action> commandQuene = new ConcurrentQueue<Action>();//普通逻辑指令
        private ConcurrentQueue<Action> profilerCommandQuenue = new ConcurrentQueue<Action>();//性能分析指令
        private ConcurrentQueue<Action> tokenCheckCommandQuenue = new ConcurrentQueue<Action>();//token检测指令
        private int logicTimeFrame = 1;//逻辑祯ms
        public int ProfilerBound = 0;//超过此值则性能分析将入库
        public static ServerCPU Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new ServerCPU();
                        }
                    }
                }
                return instance;
            }
        }
        private ServerCPU()
        {
        }
        public void Init()
        {
            InitCfg();

            workThread = new Thread(new ThreadStart(WorkThreadFunc));
            workThread.Start();
        }
        public void InitCfg()
        {
            this.logicTimeFrame = 1;
            LocalConfigInfo config = LocalConfigManager.Instance.GetConfig("logicTimeFrame");
            if (config != null)
            {
                int.TryParse(config.value, out this.logicTimeFrame);
            }

            this.ProfilerBound = 0;
            config = LocalConfigManager.Instance.GetConfig("profilerBound");
            if (config != null)
            {
                int.TryParse(config.value, out this.ProfilerBound);
            }

            if (ProfilerBound > 0 && profilerThread == null)
            {
                profilerThread = new Thread(new ThreadStart(ProfilerThreadFunc));
                profilerThread.Start();

                CodeElapseChecker.Initialize();
            }
        }
        //工作线程处理
        private void WorkThreadFunc()
        {
            try
            {
                while (true)
                {
                    DateTime time = DateTime.Now;

                    while (commandQuene.Count > 0)
                    {
                        Action command = null;
                        if (commandQuene.TryDequeue(out command))
                        {
                            if (command != null)
                            {
                                command();
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(logicTimeFrame);

                    TimerManager.Instance.Update((int)((DateTime.Now - time).TotalMilliseconds));
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
            }
        }
        //性能统计线程处理
        private void ProfilerThreadFunc()
        {
            while (true)
            {
                while (profilerCommandQuenue.Count > 0)
                {
                    Action command = null;
                    if (profilerCommandQuenue.TryDequeue(out command))
                    {
                        if (command != null)
                        {
                            command();
                        }
                    }
                }
                System.Threading.Thread.Sleep(1);
            }
        }
        public void PushCommand(Action command)
        {
            commandQuene.Enqueue(command);
        }
        public void PushProfilerCommand(Action command)
        {
            profilerCommandQuenue.Enqueue(command);
        }
    }
}
