using System;
using System.Collections.Concurrent;
using System.Threading;
using LegendProtocol;
using LegendServer.LocalConfig;
using LegendServer.Util;
using System.Diagnostics;

namespace LegendServerWorld.Core
{
    //服务器的中央处理器(双线程)
    class ServerCPU
    {
        public Thread workThread = null;
        public Thread daemonThread = null;
        public Thread profilerThread = null;
        private static ServerCPU instance = null;
        private static object singletonLocker = new object();//单例双检锁
        private ConcurrentQueue<Action> commandQuene = new ConcurrentQueue<Action>();//普通逻辑指令
        private ConcurrentQueue<Action> dbUpdateCommandQuene = new ConcurrentQueue<Action>();//守护线程的数据库更新指令（仅仅是推送操作指令，真正执行时数据可能已经被主线程更新过）
        private ConcurrentQueue<Action> profilerCommandQuenue = new ConcurrentQueue<Action>();//性能分析指令
        private int logicTimeFrame = 1;//逻辑祯ms
        private int dbCacheUpdateInterval = 1;//数据缓存的入库频率ms
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

            daemonThread = new Thread(new ThreadStart(DaemonThreadFunc));
            daemonThread.Start();
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

            this.dbCacheUpdateInterval = 1;
            config = LocalConfigManager.Instance.GetConfig("dbCacheUpdateInterval");
            if (config != null)
            {
                int.TryParse(config.value, out this.dbCacheUpdateInterval);
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
                ServerUtil.RecordLog(LogType.Error, ex);
            }
        }
        //守护线程处理
        private void DaemonThreadFunc()
        {
            try
            {
                while (true)
                {
                    while (dbUpdateCommandQuene.Count > 0)
                    {
                        Action command = null;
                        if (dbUpdateCommandQuene.TryDequeue(out command))
                        {
                            if (command != null)
                            {
                                command();
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(dbCacheUpdateInterval);
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex);
            }
        }
        //性能统计线程处理
        private void ProfilerThreadFunc()
        {
            try
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
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex);
            }
        }
        public void PushCommand(Action command)
        {
            commandQuene.Enqueue(command);
        }
        public void PushDBUpdateCommand(Action command)
        {
            dbUpdateCommandQuene.Enqueue(command);
        }
        public void PushProfilerCommand(Action command)
        {
            profilerCommandQuenue.Enqueue(command);
        }
    }
}
