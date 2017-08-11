using System;
using System.Collections.Concurrent;
using System.Threading;
using LegendProtocol;
using LegendServer.LocalConfig;
using LegendServerRecord.Record;
using LegendServer.Util;

namespace LegendServerRecord.Core
{
    //服务器的中央处理器(双线程)
    class ServerCPU
    {
        public ConcurrentDictionary<int, Thread> workThread = new ConcurrentDictionary<int, Thread>();
        private ConcurrentDictionary<int, ConcurrentQueue<Action>> workQuene = new ConcurrentDictionary<int, ConcurrentQueue<Action>>();
        private static ServerCPU instance = null;
        private static object singletonLocker = new object();//单例双检锁
        private int logicTimeFrame = 1;//逻辑祯ms
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

            for (int index = 0; index < (int)ActionType.Count; index++)
            {
                int srcIndex = index;
                workQuene[index] = new ConcurrentQueue<Action>();
                workThread[index] = new Thread(new ThreadStart(()=> 
                {
                    while (true)
                    {
                        DateTime time = DateTime.Now;
                        while (workQuene[srcIndex].Count > 0)
                        {
                            Action command = null;
                            if (workQuene[srcIndex].TryDequeue(out command))
                            {
                                if (command != null)
                                {
                                    command();
                                }
                            }
                        }

                        System.Threading.Thread.Sleep(logicTimeFrame);

                        if (srcIndex == 0)
                        {
                            TimerManager.Instance.Update((int)((DateTime.Now - time).TotalMilliseconds));
                        }
                    }
                }));

                workThread[index].Start();
            }
        }
        

        public void InitCfg()
        {
            this.logicTimeFrame = 1;
            LocalConfigInfo config = LocalConfigManager.Instance.GetConfig("logicTimeFrame");
            if (config != null)
            {
                int.TryParse(config.value, out this.logicTimeFrame);
            }
        }

        public void PushCommand(Action command, ActionType type)
        {
            int index = (int)type;
            if (index < 0 || index >= (int)ActionType.Count)
            {
                ServerUtil.RecordLog(LogType.Error, "PushCommand时指定的行为记录类型有误，不能超过ActionType.Count或者小于0");
                return;
            }

            workQuene[index].Enqueue(command);
        }
    }
}
