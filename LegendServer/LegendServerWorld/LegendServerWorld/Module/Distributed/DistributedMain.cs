using System.Collections.Concurrent;
using LegendProtocol;
using System.Linq;
using LegendServerWorld.Core;
using LegendServer.Util;
using LegendServer.LocalConfig;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System;

namespace LegendServerWorld.Distributed
{
    public class DistributedMain : Module
    {
        public DistributedMsgProxy msg_proxy;
        public string PublicKey = "";
        public ServerInternalStatus ServerStatus = ServerInternalStatus.UnLoaded;
        public bool DBLoaded = false;
        public string CenterServerName = "";
        public int CenterServerID = 0;
        public List<ServerIndex> AllOutboundServer = new List<ServerIndex>();
        public List<ServerIndex> AllInboundServer = new List<ServerIndex>();
        public List<ServerIndex> AllWaitConnectInboundServer = new List<ServerIndex>();
        private ConcurrentQueue<PeerLink> connectedServerCheckerList = new ConcurrentQueue<PeerLink>();
        private ConcurrentQueue<Action> delayReconnectServerList = new ConcurrentQueue<Action>();
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
            MsgFactory.Regist(MsgID.C2X_NotifyCloseService, new MsgComponent(msg_proxy.OnNotifyCloseService, typeof(NotifyCloseService_C2X)));
            MsgFactory.Regist(MsgID.C2X_ReplyDBConfig, new MsgComponent(msg_proxy.OnReplyDBConfig, typeof(ReplyDBConfig_C2X)));
            MsgFactory.Regist(MsgID.C2X_ServerRunning, new MsgComponent(msg_proxy.OnReciveRunningNotify, typeof(ServerRunning_C2X)));
            MsgFactory.Regist(MsgID.X2X_UpdateServerCfg, new MsgComponent(msg_proxy.OnUpdateServerCfg, typeof(UpdateServerCfg_X2X)));
            MsgFactory.Regist(MsgID.C2X_NotifyReconnectTargetServer, new MsgComponent(msg_proxy.OnReconnectTargetServerNotify, typeof(NotifyReconnectTargetServer_C2X)));
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
        public void AddDelayReconnectServer(Action cmd)
        {
            if (cmd == null) return;

            if (TimerManager.Instance.Get(TimerId.DelayReconnectServerCheck) == null)
            {
                TimerManager.Instance.Regist(TimerId.DelayReconnectServerCheck, 1000, 1000, int.MaxValue, OnDelayReconnectServerCheckTimer, null, null, null);
            }

            if (delayReconnectServerList.FirstOrDefault(element => (element == cmd)) == null)
            {
                delayReconnectServerList.Enqueue(cmd);
            }
        }
        private void OnDelayReconnectServerCheckTimer(object obj)
        {
            Action cmd = null;
            delayReconnectServerList.TryDequeue(out cmd);

            if (cmd != null)
            {
                cmd();
            }
            if (delayReconnectServerList.Count <= 0)
            {
                TimerManager.Instance.Remove(TimerId.DelayReconnectServerCheck);
            }
        }
        public void AddConnectedServerChecker(PeerLink peerLink)
        {
            if (TimerManager.Instance.Get(TimerId.InternalServerConnectedCheck) == null)
            {
                TimerManager.Instance.Regist(TimerId.InternalServerConnectedCheck, 0, 100, int.MaxValue, OnInternalServerConnectedCheckTimer, null, null, null);
            }

            if (connectedServerCheckerList.FirstOrDefault(element => (element.toName == peerLink.toName && element.toId == peerLink.toId)) == null)
            {
                connectedServerCheckerList.Enqueue(peerLink);
            }
        }
        public void UpdateLoadBlanceStatus(string serverName, int serverID, bool increase)
        {
            if (ServerStatus == ServerInternalStatus.Running)
            {
                msg_proxy.NotifyLoadBlanceStatus(serverName, serverID, increase);
            }
        }
        public void OnInboundServerReconnectCheck(object obj)
        {
            //计时器每次检测已经连入的入境服务器
            List<ServerIndex> connectedServer = new List<ServerIndex>();
            AllWaitConnectInboundServer.ForEach(server =>
            {
                InboundSession inboundSession = SessionManager.Instance.GetInboundSession(server.name, server.id);
                if (inboundSession != null)
                {
                    connectedServer.Add(server);
                }
            });

            //将已连入的入境服务器从等待连接的列表里删除
            connectedServer.ForEach(server => AllWaitConnectInboundServer.Remove(server));

            //如果等待连接的入境服务器列表为空说明重连并入集群完成
            if (AllWaitConnectInboundServer.Count <= 0)
            {
                TimerManager.Instance.Remove(TimerId.InboundServerReconnectCheck);

                ModuleManager.Start();
                ServerStatus = ServerInternalStatus.Running;

                ServerUtil.RecordLog(LogType.Info, "热更新后自适应并入集群！");
            }
        }
        private void OnInternalServerConnectedCheckTimer(object obj)
        {
            if (connectedServerCheckerList.Count <= 0)
            {
                if (DBLoaded)
                {
                    TimerManager.Instance.Remove(TimerId.InternalServerConnectedCheck);

                    ServerStatus = ServerInternalStatus.Loaded;

                    msg_proxy.NotifyServerLoaded();
                    msg_proxy.root.OnServerLoaded();
                }
                return;
            }

            PeerLink peerLink = null;
            connectedServerCheckerList.TryPeek(out peerLink);
            if (peerLink == null) return;

            OutboundSession session = SessionManager.Instance.GetOutboundSession(peerLink.toName, peerLink.toId);
            if (session != null)
            {
                if (session.serverName == "center")
                {
                    if (!DBLoaded)
                    {
                        //Center服务器已连接则向其请求DB配置
                        msg_proxy.RequestDBConfig();
                    }
                }

                //移出已连接的
                PeerLink removePeerNode = null;
                connectedServerCheckerList.TryDequeue(out removePeerNode);

                //获取即将连接的
                PeerLink connectPeerLink = null;
                connectedServerCheckerList.TryPeek(out connectPeerLink);

                if (connectPeerLink != null)
                {
                    //获取自身配置
                    LocalConfigInfo mySelfServer = LocalConfigManager.Instance.GetConfig("MySelfServer");

                    //连接下一个目标服务器
                    new OutboundPeer(msg_proxy.root, connectPeerLink).ConnectTcp(new IPEndPoint(IPAddress.Parse(connectPeerLink.toIp), connectPeerLink.toPort), mySelfServer.value, null);
                }
            }
        }
        private void OnServiceStopKeyCheckTimer(object obj)
        {
            if (File.Exists(LocalConfigManager.Instance.CurrentPath + msg_proxy.root.ServerName + msg_proxy.root.ServerID))
            {
                File.Delete(LocalConfigManager.Instance.CurrentPath + msg_proxy.root.ServerName + msg_proxy.root.ServerID);

                LegendServerWorldApplication.StopService();
            }
        }
        public int GetLogicId()
        {
            List<InboundSession> sessions = SessionManager.Instance.GetAllInboundSession("logic");
            if (sessions.Count > 0)
            {
                int index = LegendServer.Util.MyRandom.NextPrecise(0, sessions.Count);
                return sessions[index].serverID;
            }
            return 0;
        }
    }
}