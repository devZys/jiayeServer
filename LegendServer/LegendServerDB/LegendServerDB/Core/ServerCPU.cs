using System;
using System.Collections.Concurrent;
using System.Threading;
using LegendProtocol;
using LegendServer.LocalConfig;
using LegendServer.Util;
using System.Diagnostics;
using RabbitMQ.Client.Events;
using LegendServerDB.Distributed;
using RabbitMQ.Client;
using LegendServer.Database.Config;
using LegendServer.Database;

namespace LegendServerDB.Core
{
    //服务器的中央处理器(双线程)
    class ServerCPU
    {
        public Thread workThread = null;
        public Thread daemonThread = null;
        public Thread dbSyncThread = null;
        public Thread mqThread = null;
        public Thread profilerThread = null;
        private static ServerCPU instance = null;
        private static object singletonLocker = new object();//单例双检锁
        private ConcurrentQueue<Action> commandQuene = new ConcurrentQueue<Action>();//普通逻辑指令
        private ConcurrentQueue<Action> dbSyncCommandQuenue = new ConcurrentQueue<Action>();//DB缓存同步指令
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

            dbSyncThread = new Thread(new ThreadStart(DBSyncThreadFunc));
            dbSyncThread.Start();

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

        public void InitMQ(LegendServerDBApplication main)
        {
            WebConfigDB gameName = DBManager<WebConfigDB>.Instance.GetSingleRecordInCache(element => element.configKey == "GameName");
            WebConfigDB mqHostName = DBManager<WebConfigDB>.Instance.GetSingleRecordInCache(element => element.configKey == "MQHostName");
            WebConfigDB mqUserName = DBManager<WebConfigDB>.Instance.GetSingleRecordInCache(element => element.configKey == "MQUserName");
            WebConfigDB mqUserPassword = DBManager<WebConfigDB>.Instance.GetSingleRecordInCache(element => element.configKey == "MQUserPassword");
            if (gameName == null || mqHostName == null || mqUserName == null || mqUserPassword == null)
            {
                ServerUtil.RecordLog(LogType.Fatal, "MQ配置信息有误！");
                return;
            }
            if (string.IsNullOrEmpty(gameName.configValue) || string.IsNullOrEmpty(mqHostName.configValue) || string.IsNullOrEmpty(mqUserName.configValue) || string.IsNullOrEmpty(mqUserPassword.configValue))
            {
                ServerUtil.RecordLog(LogType.Fatal, "MQ配置信息有误 有字符串值为空！");
                return;
            }
            GameType gameType = GameType.YPHNMJ;
            if (!Enum.TryParse(gameName.configValue, out gameType))
            {
                ServerUtil.RecordLog(LogType.Fatal, "MQ配置信息有误 获取类型出错！");
                return;
            }
            main.Game = gameType;
            MQReceiver.Instance.RegistMQ(main);
            MQHostInfo hostInfo = new MQHostInfo(gameType, mqHostName.configValue, mqUserName.configValue, mqUserPassword.configValue);
            MQSender.Instance.Init(hostInfo);
            StartMQThread(hostInfo);
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
        //守护线程处理
        private void DaemonThreadFunc()
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
        //DB缓存同步线程处理
        private void DBSyncThreadFunc()
        {
            while (true)
            {
                while (dbSyncCommandQuenue.Count > 0)
                {
                    Action command = null;
                    if (dbSyncCommandQuenue.TryDequeue(out command))
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
        public void StartMQThread(MQHostInfo mqHostInfo)
        {
            mqThread = new Thread(new ParameterizedThreadStart(MQThreadFunc));
            mqThread.Start(mqHostInfo);
        }
        private void MQThreadFunc(object obj)
        {
            try
            {
                var factory = new ConnectionFactory();

                MQHostInfo hostInfo = obj as MQHostInfo;

                factory.HostName = hostInfo.Name;
                factory.UserName = hostInfo.User;
                factory.Password = hostInfo.Password;

                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        var consumer = new QueueingBasicConsumer(channel);
                        string queueName = channel.QueueDeclare(hostInfo.Game.ToString() + ModuleManager.Get<DistributedMain>().msg_proxy.root.ServerID, true, false, false, null).QueueName;
                        foreach (MQID mqId in MQFactory.AllMQ.Keys)
                        {
                            string game_mqId = hostInfo.Game.ToString() + Enum.GetName(typeof(MQID), mqId);
                            channel.ExchangeDeclare(exchange: game_mqId, type: "fanout", durable: true, autoDelete: false, arguments: null);
                            channel.QueueBind(queue: queueName, exchange: game_mqId, routingKey: Enum.GetName(typeof(MQID), mqId));
                        }

                        channel.BasicConsume(queue: queueName, noAck: true, consumer: consumer);
                        while (true)
                        {
                            var eventArgs = consumer.Queue.Dequeue();
                            if (eventArgs == null) continue;
                            if (string.IsNullOrEmpty(eventArgs.RoutingKey)) continue;

                            int serverId = 0;
                            if (eventArgs.BasicProperties != null && !string.IsNullOrEmpty(eventArgs.BasicProperties.MessageId))
                            {
                                int.TryParse(eventArgs.BasicProperties.MessageId, out serverId);
                            }
                            if (serverId > 0 && ModuleManager.Get<DistributedMain>().msg_proxy.root.ServerID != serverId) continue;

                            MQReceiver.Instance.OnRecvMsg(eventArgs.RoutingKey, eventArgs.Body);

                            Thread.Sleep(1);
                        }

                        //或者如下用法：
                        /**
                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            MQReceiver.Instance.OnRecvMsg(ea.RoutingKey, ea.Body);
                        };
                        channel.BasicConsume(queue: game_mqId, noAck: true, consumer: consumer);
                        System.Threading.Thread.Sleep(-1);
                        */
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
            }
        }
        public void PushCommand(Action command)
        {
            commandQuene.Enqueue(command);
        }
        public void PushDBSyncCommand(Action command)
        {
            dbSyncCommandQuenue.Enqueue(command);
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
