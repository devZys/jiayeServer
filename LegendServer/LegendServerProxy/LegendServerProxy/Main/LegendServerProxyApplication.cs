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
using Photon.SocketServer.ServerToServer;
using LegendServerProxy.Core;
using System.Net;
using LegendProtocol;
using LegendServerProxy.Distributed;
using LegendServer.LocalConfig;
using System.Diagnostics;

namespace LegendServerProxy
{
    public class LegendServerProxyApplication : ApplicationBase
    {
        public string ServerName = "";//服务器名称
        public int ServerID = 0;//服务器编号
#if MAHJONG
        public GameTypeName serverGameName = GameTypeName.Mahjong;
#elif  RUNFAST
        public GameTypeName serverGameName = GameTypeName.RunFast;
#elif  WORDPLATE
        public GameTypeName serverGameName = GameTypeName.WordPlate;
#endif

        protected override PeerBase CreatePeer(InitRequest initRequest)
        {
            if (initRequest.ApplicationId.Contains("client"))
            {
                if (ModuleManager.Get<DistributedMain>().ServerStatus != ServerInternalStatus.Running) return null;

                return new InboundClientPeer(initRequest);
            }
            else
            {
                return ServerRegister.Instance.TryRegistServer(initRequest);
            }
        }
        protected override void Setup()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            ServerInitialize.InitObjectPool();
            ServerInitialize.RegistPublicModule(this);

            if (LocalConfigManager.Instance.LoadLocalConfig() == false)
            {
                ServerUtil.RecordLog(LogType.Fatal, "proxy读取本地配置出错！！！", new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }

            LocalConfigInfo mySelfConfig = LocalConfigManager.Instance.GetConfig("MySelfServer");
            if (mySelfConfig == null || mySelfConfig.value == null)
            {
                ServerUtil.RecordLog(LogType.Fatal, "解析本地MySelfServer配置出错!");
                return;
            }
            string[] mySelfValue = null;
            mySelfValue = mySelfConfig.value.Split(':');
            if (mySelfValue.Length < 4)
            {
                ServerUtil.RecordLog(LogType.Fatal, "解析本地MySelfServer配置出错!");
                return;
            }

            ServerInitialize.RegistGameModule(this);
            ServerCPU.Instance.Init();

            ConnectToServer();
        }
        protected override void TearDown()
        {
            StopService();
        }
        public void OnDBLoadCompleted()
        {
            ModuleManager.RegistTimer();
        }
        private  void ConnectToServer()
        {
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

            List<LocalConfigInfo> targetConfigList = LocalConfigManager.Instance.GetAllConfig("CenterServer");
            List<LocalConfigInfo> targetConfigList1 = LocalConfigManager.Instance.GetAllConfig("ProxyServer");
            List<LocalConfigInfo> targetConfigList2 = LocalConfigManager.Instance.GetAllConfig("LogicServer");
            List<LocalConfigInfo> targetConfigList3 = LocalConfigManager.Instance.GetAllConfig("ACServer");
            List<LocalConfigInfo> targetConfigList4 = LocalConfigManager.Instance.GetAllConfig("DBServer");
            List<LocalConfigInfo> targetConfigList5 = LocalConfigManager.Instance.GetAllConfig("WorldServer");
            List<LocalConfigInfo> targetConfigList6 = LocalConfigManager.Instance.GetAllConfig("RecordServer");
            targetConfigList.AddRange(targetConfigList1);
            targetConfigList.AddRange(targetConfigList2);
            targetConfigList.AddRange(targetConfigList3);
            targetConfigList.AddRange(targetConfigList4);
            targetConfigList.AddRange(targetConfigList5);
            targetConfigList.AddRange(targetConfigList6);

            PeerLink firstPeerLink = null;

            foreach (LocalConfigInfo element in targetConfigList)
            {
                string[] targetValue = element.value.Split(':');
                if (targetValue.Length < 4)
                {
                    ServerUtil.RecordLog(LogType.Fatal, mySelfId + " 号" + mySelfName + "服务器解析" + element.key + "配置出错!", new StackTrace(new StackFrame(true)).GetFrame(0));
                    return;
                }

                string targetName = targetValue[0];
                string targetIp = targetValue[1];
                int targetPort = 0;
                int.TryParse(targetValue[2], out targetPort);
                int targetId = 0;
                int.TryParse(targetValue[3], out targetId);

                ModuleManager.Get<DistributedMain>().AllOutboundServer.Add(new ServerIndex() { name = targetName, id = targetId });

                PeerLink peerLink = new PeerLink(mySelfName, mySelfIP, mySelfPort, mySelfId, targetName, targetIp, targetPort, targetId);
                ModuleManager.Get<DistributedMain>().AddConnectedServerChecker(peerLink);

                if (firstPeerLink == null)
                {
                    firstPeerLink = peerLink;
                }
            }

            ServerName = mySelfName;
            ServerID = mySelfId;

            if (targetConfigList.Count <= 0)
            {
                OnServerLoaded();
            }
            else
            {
                new OutboundPeer(this, firstPeerLink).ConnectTcp(new IPEndPoint(IPAddress.Parse(firstPeerLink.toIp), firstPeerLink.toPort), mySelfConfig.value, null);
            }
        }
        public void OnServerLoaded()
        {
            ServerUtil.RecordLog(LogType.Info, ServerID + " 号 " + ServerName + " 启动完毕!");
        }
        static public void StopService()
        {
            ModuleManager.Get<DistributedMain>().ServerStatus = ServerInternalStatus.Closed;

            //中断拓扑结构中与之关联的所有结点
            SessionManager.Instance.DisconnectCenter();
            SessionManager.Instance.DisconnectAllPlayer();
            SessionManager.Instance.DisconnectAllLogic();
            SessionManager.Instance.DisconnectWorld();
            SessionManager.Instance.DisconnectAllAC();
            SessionManager.Instance.DisconnectRecord();

            ModuleManager.Destroy();
        }
        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            ServerUtil.RecordLog(LogType.Fatal, "服务器宕机了!", new StackTrace(new StackFrame(true)).GetFrame(0));
            ServerUtil.RecordLog(LogType.Fatal, (Exception)args.ExceptionObject, new StackTrace(new StackFrame(true)).GetFrame(0));

            StopService();
        }
    }
}
