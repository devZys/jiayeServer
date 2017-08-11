using Photon.SocketServer;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.ServerDeploy;
using System.Diagnostics;
using LegendServerWorld.Distributed;
using LegendServer.LocalConfig;

namespace LegendServerWorld.Core
{
    //服务器注册者
    public class ServerRegister
    {
        private static object singletonLocker = new object();//单例双检锁
        private static object registerLocker = new object();//操作锁

        private static ServerRegister instance = null;
        bool ServerUnLoadAllowEstablishSocket = false;
        public static ServerRegister Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new ServerRegister();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }
        public ServerRegister() { }
        public void Init() { }
        public void InitCfg()
        {
            this.ServerUnLoadAllowEstablishSocket = false;
            LocalConfigInfo config = LocalConfigManager.Instance.GetConfig("serverUnLoadAllowEstablishSocket");
            if (config != null)
            {
                bool.TryParse(config.value, out this.ServerUnLoadAllowEstablishSocket);
            }
        }

        //尝试注册被动连接进来的服务器
        public InboundPeer TryRegistServer(InitRequest initRequest)
        {
            lock (registerLocker)
            {
                string[] peerValue = initRequest.ApplicationId.Split(':');
                if (peerValue.Length < 4)
                {
                    ServerUtil.RecordLog(LogType.Fatal, "解析peer信息出错!", new StackTrace(new StackFrame(true)).GetFrame(0));
                    return null;
                }

                string fromName = peerValue[0];
                int fromId = 0;
                int.TryParse(peerValue[3], out fromId);

                if (ModuleManager.Get<DistributedMain>().ServerStatus == ServerInternalStatus.UnLoaded)
                {
                    return ServerUnLoadAllowEstablishSocket ? new InboundPeer(initRequest, fromName, fromId) : null;
                }

                string fromIp = peerValue[1];
                int fromPort = 0;
                int.TryParse(peerValue[2], out fromPort);

                if (!initRequest.RemoteIP.Equals(fromIp))
                {
                    ServerUtil.RecordLog(LogType.Fatal, "第 " + fromId + "号" + fromName + "服务器的的本地配置IP与其自IP不符!", new StackTrace(new StackFrame(true)).GetFrame(0));
                    return null;
                }

                ServerDeployConfigDB serverDeploy = DBManager<ServerDeployConfigDB>.Instance.GetSingleRecordInCache(element => (element.name == fromName && element.id == fromId));
                if (serverDeploy == null || !serverDeploy.ip.Equals(fromIp))
                {
                    ServerUtil.RecordLog(LogType.Fatal, "第 " + fromId + "号" + fromName + "服务器是非法连接,请检查serverdeploy表!", new StackTrace(new StackFrame(true)).GetFrame(0));
                    return null;
                }

                return new InboundPeer(initRequest, fromName, fromId);
            }
        }
    }
}
