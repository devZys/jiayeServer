//                            _ooOoo_  
//                           o8888888o  
//                           88" . "88  
//                           (| -_- |)  
//                            O\ = /O  
//                        ____/`---'\____  
//                      .   ' \\| |// `.  
//                       / \\||| : |||// \  
//                     / _||||| -:- |||||- \  
//                       | | \\\ - /// | |  
//                     | \_| ''\---/'' | |  
//                      \ .-\__ `-` ___/-. /  
//                   ___`. .' /--.--\ `. . __  
//                ."" '< `.___\_<|>_/___.' >'"".  
//               | | : `- \`.;`\ _ /`;.`/ - ` : | |  
//                 \ \ `-. \_ __\ /__ _/ .-` / /  
//         ======`-.____`-.___\_____/___.-`____.-'======  
//                            `=---='  
//  
//         ............................................. 
//                  佛祖镇楼               永无BUG  



using System;
using System.Collections.Generic;
using Photon.SocketServer;
using LegendServerCenter.Core;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.LocalConfig;
using LegendServerCenter.Distributed;
using LegendServer.Database.ServerDeploy;
using System.Diagnostics;

namespace LegendServerCenter
{
    public class LegendServerCenterApplication : ApplicationBase
    {
        public string ServerName = "";//服务器名称
        public int ServerID = 0;//服务器编号
#if  MAHJONG
        public GameTypeName serverGameName = GameTypeName.Mahjong;
#elif RUNFAST
        public GameTypeName serverGameName = GameTypeName.RunFast;
#elif  WORDPLATE
        public GameTypeName serverGameName = GameTypeName.WordPlate;
#endif

        protected override PeerBase CreatePeer(InitRequest initRequest)
        {
            return ServerRegister.Instance.TryRegistServer(initRequest);
        }
        protected override void Setup()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            ServerInitialize.InitObjectPool();
            ServerInitialize.RegistAllModule(this);

            if (LocalConfigManager.Instance.LoadLocalConfig() == false)
            {
                ServerUtil.RecordLog(LogType.Fatal, "Center读取本地配置出错！！！", new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }

            DBConfigInfo dbCfgInfo = GetDBConfig();
            string result = ServerInitialize.LoadDatabaseToCache(dbCfgInfo.address, dbCfgInfo.port, dbCfgInfo.database, dbCfgInfo.user, dbCfgInfo.password);
            if (!String.IsNullOrEmpty(result))
            {
                ServerUtil.RecordLog(LogType.Fatal, "Center 数据库加载至缓存出错,错误信息:" + result, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }

            OnDBLoadCompleted();

            LocalConfigInfo mySelfConfig = LocalConfigManager.Instance.GetConfig("MySelfServer");
            if (mySelfConfig == null || mySelfConfig.value == null)
            {
                ServerUtil.RecordLog(LogType.Fatal, "解析本地MySelfServer配置出错!", new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }
            string[] mySelfValue = mySelfConfig.value.Split(':');
            if (mySelfValue.Length < 4)
            {
                ServerUtil.RecordLog(LogType.Fatal, "解析本地MySelfServer配置出错!", new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }

            string mySelfName = mySelfValue[0];
            string mySelfIP = mySelfValue[1];
            int mySelfPort = 0;
            int.TryParse(mySelfValue[2], out mySelfPort);
            int mySelfId = 0;
            int.TryParse(mySelfValue[3], out mySelfId);

            ServerName = mySelfName;
            ServerID = mySelfId;   

            ServerDeployConfigDB server = DBManager<ServerDeployConfigDB>.Instance.GetSingleRecordInCache(element => element.name == mySelfName && element.id == mySelfId && element.ip == mySelfIP);
            if (server == null)
            {
                ServerUtil.RecordLog(LogType.Fatal, this.ToString() + "本地MySelfServer配置与serverdeploy数据库配置不匹配!", new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }

            ServerCPU.Instance.Init();

            List<ServerDeployConfigDB> serverList = DBManager<ServerDeployConfigDB>.Instance.GetRecordsInCache();
            ServerUtil.RecordLog(LogType.Info, "整个集群一共需要加载 " + serverList.Count + " 个服务");

            serverList.ForEach(element => ModuleManager.Get<DistributedMain>().AddServer(element.name.Trim(), element.id));

            ModuleManager.Get<DistributedMain>().SetServerStatus(ServerName, ServerID, ServerInternalStatus.Loaded);
            ModuleManager.Get<DistributedMain>().ServerStatus = ServerInternalStatus.Loaded;

            OnServerLoaded();

        }
        protected override void TearDown()
        {
            StopService();
        }
        public void OnDBLoadCompleted()
        {
            ModuleManager.RegistTimer();
        }
        public void OnServerLoaded()
        {
            ServerUtil.RecordLog(LogType.Info, ServerID + " 号 " + ServerName + " 启动完毕!");
        }
        static public void StopService()
        {
            ModuleManager.Get<DistributedMain>().ServerStatus = ServerInternalStatus.Closed;

            ModuleManager.Destroy();
        }
        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            ServerUtil.RecordLog(LogType.Fatal, "中心服务器宕机了! [啊！这不是真的！]", new StackTrace(new StackFrame(true)).GetFrame(0));
            ServerUtil.RecordLog(LogType.Fatal, (Exception)args.ExceptionObject, new StackTrace(new StackFrame(true)).GetFrame(0));

            StopService();
        }
        public DBConfigInfo GetDBConfig()
        {
            LocalConfigInfo config = LocalConfigManager.Instance.GetConfig("database");
            string[] value = config.value.Split(':');

            DBConfigInfo cfg = new DBConfigInfo();
            cfg.address = value[0];
            if (value.Length == 4)
            {
                cfg.port = "3306";
                cfg.database = value[1];
                cfg.user = value[2];
                cfg.password = value[3];
            }
            else
            {
                cfg.port = value[1];
                cfg.database = value[2];
                cfg.user = value[3];
                cfg.password = value[4];
            }

            return cfg;
        }
    }
}
