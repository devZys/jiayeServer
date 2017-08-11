using System.Collections.Generic;
using System.Linq;
using LegendProtocol;
using System;
using LegendServerCenter.Core;
using System.IO;
using LegendServer.LocalConfig;
using LegendServer.Util;
using System.Collections.Concurrent;
using LegendServer.Database;

namespace LegendServerCenter.Distributed
{
    public class DistributedMain : Module
    {
        public DistributedMsgProxy msg_proxy;
        public string PublicKey = "";
        public ServerInternalStatus ServerStatus = ServerInternalStatus.UnLoaded;
        public List<ServerInfo> AllDeployServers = new List<ServerInfo>();

        public DistributedMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new DistributedMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
            ServerCPU.Instance.InitCfg();
            ServerRegister.Instance.InitCfg();
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.X2C_RequestDBConfig, new MsgComponent(msg_proxy.OnRequestDBConfig, typeof(RequestDBConfig_X2C)));
            MsgFactory.Regist(MsgID.X2C_ServerLoadedNotify, new MsgComponent(msg_proxy.OnServerLoadedNotify, typeof(ServerLoadedNotify_X2C)));
            MsgFactory.Regist(MsgID.X2X_NotifyLoadBlanceStatus, new MsgComponent(msg_proxy.OnServerCCUNotify, typeof(NotifyLoadBlanceStatus_X2X)));
            MsgFactory.Regist(MsgID.D2C_NotifyVerifyPassed, new MsgComponent(msg_proxy.OnNotifyVerifyPassed, typeof(NotifyVerifyPassed_D2C)));
            MsgFactory.Regist(MsgID.X2X_UpdateServerCfg, new MsgComponent(msg_proxy.OnUpdateServerCfg, typeof(UpdateServerCfg_X2X)));
            MsgFactory.Regist(MsgID.P2C_RequestControlExternalService, new MsgComponent(msg_proxy.OnRequestControlExternalService, typeof(RequestControlExternalService_P2C)));
            MsgFactory.Regist(MsgID.X2X_RequestCloseCluster, new MsgComponent(msg_proxy.OnRequestCloseCluster, typeof(RequestCloseCluster_X2X)));
        }
        public override void OnRegistTimer()
        {
            TimerManager.Instance.Regist(TimerId.ServiceStopKeyCheck, 0, 10000, int.MaxValue, OnServiceStopKeyCheckTimer, null, null, null);
        }
        public override void OnStart()
        {
        }
        public override void OnDestroy()
        {
        }
        public string GetMyselfServerName()
        {
            return msg_proxy.root.ServerName;
        }
        public int GetMyselfServerId()
        {
            return msg_proxy.root.ServerID;
        }
        
        public void AddServer(string serverName, int serverId)
        {
            if (AllDeployServers.Exists(element => (element.name == serverName && element.id == serverId)))
            {
                ServerUtil.RecordLog(LogType.Error, "在serverdeploy数据表中配置了相同的服务！");
                return;
            }

            AllDeployServers.Add(new ServerInfo { name = serverName, id = serverId, status = ServerInternalStatus.UnLoaded, ccu = 0});
        }
        public bool ExistServer(string serverName, int serverId)
        {
            return AllDeployServers.Exists(element => element.name == serverName && element.id == serverId);
        }
        public List<ServerIndex> GetAllInboundServer(string serverName, int serverId)
        {
            ServerInfo serverInfo = AllDeployServers.Find(server => server.name == serverName && server.id == serverId);
            if (serverInfo == null)
            {
                ServerUtil.RecordLog(LogType.Error, "无效的服务器！");
                return null;
            }
            return serverInfo.allInboundServer;
        }
        public void SetServerStatus(string name, int id, ServerInternalStatus status)
        {
            ServerInfo server = AllDeployServers.Find(element => (element.name == name && element.id == id));
            if (server != null)
            {
                server.status = status;
                if (status == ServerInternalStatus.Loaded)
                {
                    int count = AllDeployServers.Count - AllDeployServers.Count(element => element.status == ServerInternalStatus.Loaded);                    
                    ServerUtil.RecordLog(LogType.Info, "服务器名：" + server.name + " 服务器ID：" + server.id + " 加载完毕！[仍待加载的服务器数：" + count + "]");
                    if (count <= 0)
                    {
                        ServerUtil.RecordLog(LogType.Info, "集群即将进入运行态，运行态下的日志请在Record服务器查收！");
                    }

                    OnServerLoaded();
                }
            }
        }
        public void UpdateLoadBlanceStatus(string name, int id, bool increase, int newCCU)
        {
            ServerInfo server = AllDeployServers.Find(element => (element.name == name && element.id == id));
            if (server != null)
            {
                if (newCCU == -1)
                {
                    //单个玩家的进入或者离开
                    if (increase)
                    {
                        if (server.ccu < int.MaxValue)
                        {
                            server.ccu++;
                        }
                    }
                    else
                    {
                        if (server.ccu > 0)
                        {
                            server.ccu--;
                        }
                    }
                }
                else
                {
                    //定时更新过来的真实CCU（纠正误差）
                    if (server.ccu != newCCU)
                    {
                        ServerUtil.RecordLog(LogType.Debug, server.id + " 号 " + server.name + " 服务器CCU出现的误差已纠正！定时更新过来的真实CCU是：" + newCCU + " Center里的CCU是：" + server.ccu + " 一定程度是因为玩家登陆到Center分配服务器后网络信号不佳造成在AC和Proxy并没有都建立网络连接！");
                        server.ccu = newCCU;
                    }
                }
            }
        }
        public void OnServerLoaded()
        {
            int loadedCount = AllDeployServers.Count(element => element.status == ServerInternalStatus.Loaded);
            if (loadedCount == AllDeployServers.Count)
            {
                AllDeployServers.ForEach(element => element.status = ServerInternalStatus.Running);
                ServerStatus = ServerInternalStatus.Running;

                ModuleManager.Start();

                msg_proxy.BroadCastServerRunning();

                ServerUtil.RecordLog(LogType.Info, "已加载完毕的服务器列表：");
                IEnumerable<IGrouping<string, ServerInfo>> serverList = from element in AllDeployServers group element by element.name;
                foreach (IGrouping<string, ServerInfo> element in serverList)
                {
                    foreach (ServerInfo server in element)
                    {
                        ServerUtil.RecordLog(LogType.Info, "服务器名: " + server.name + " ID号: " + server.id + " 状态：" + server.status.ToString());
                    }
                }
                ServerUtil.RecordLog(LogType.Info, "服务器集群进入运行态......");
            }
        }
        //获取某服务器的内部状态
        public ServerInternalStatus GetServerStatus(string server, int id)
        {
            ServerInfo serverInfo = AllDeployServers.Find(element => element.name == server && element.id == id);
            if (serverInfo == null) return ServerInternalStatus.UnLoaded;

            return serverInfo.status;
        }
        //获得负载最优的服务器
        public ServerInfo GetOptimalServer(string name)
        {
            if (AllDeployServers.Count <= 0) return null;            

            ServerInfo result = AllDeployServers.Where(server => server.name == name && server.status == ServerInternalStatus.Running).OrderBy(element => element.ccu).FirstOrDefault();
            return result;
        }
        //获得指定运行中的服务器
        public ServerInfo GetRunningServer(string name, int id)
        {
            ServerInfo result = AllDeployServers.Find(server => server.name == name && server.id == id && server.status == ServerInternalStatus.Running);
            return result;
        }
        //获得服务器信息
        public List<ServerInfo> GetServerInfoList(string name)
        {
            return AllDeployServers.FindAll(element => element.name == name);
        }
        //获得已经关闭的服务器ID列表
        public List<ServerInfo> GetClosedServerInfoList(string name)
        {
            return AllDeployServers.FindAll(element => element.name == name && element.status == ServerInternalStatus.Closed);
        }
        //获取所有服务数量
        public int GetServerCount(string name)
        {
            return AllDeployServers.Count(element => element.name == name);
        }
        //服务器重连
        public void OnServerReconnect(InboundSession session)
        {
            //通知目标重新进入运行态
            msg_proxy.NotifyServerRunningAgain(session);

            //最后将该重连的服务器在中心服务器中置为运行态
            SetServerStatus(session.serverName, session.serverID, ServerInternalStatus.Running);
        }
        private void OnServiceStopKeyCheckTimer(object obj)
        {
            if (File.Exists(LocalConfigManager.Instance.CurrentPath + msg_proxy.root.ServerName + msg_proxy.root.ServerID))
            {
                File.Delete(LocalConfigManager.Instance.CurrentPath + msg_proxy.root.ServerName + msg_proxy.root.ServerID);

                LegendServerCenterApplication.StopService();
            }
        }
    }

}