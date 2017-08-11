using System.Collections.Concurrent;
using System.Linq;
using LegendProtocol;
using LegendServerAC.Core;
using LegendServer.Util;
using System.Net;
using LegendServer.LocalConfig;
using System.IO;
using System.Collections.Generic;
using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace LegendServerAC.Distributed
{
    public class DistributedMain : Module
    {
        public DistributedMsgProxy msg_proxy;
        public string PublicKey = "";
        public bool DBLoaded = false;//是否已加载数据库
        public ServerInternalStatus ServerStatus = ServerInternalStatus.UnLoaded;
        public string CenterServerName = "";
        public int CenterServerID = 0;
        public List<ServerIndex> AllOutboundServer = new List<ServerIndex>();
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
            MsgFactory.Regist(MsgID.C2X_NotifyControlExternalService, new MsgComponent(msg_proxy.OnNotifyControlExternalService, typeof(NotifyControlExternalService_C2X)));
            MsgFactory.Regist(MsgID.P2A_KickOldACPeer, new MsgComponent(msg_proxy.OnKickOldACPeer, typeof(KickOldACPeer_P2A)));
        }
        public override void OnRegistTimer()
        {
            TimerManager.Instance.Regist(TimerId.ServiceStopKeyCheck, 0, 10000, int.MaxValue, OnServiceStopKeyCheckTimer, null, null, null);
            TimerManager.Instance.Regist(TimerId.UpdateActualCCU, 0, 30000, int.MaxValue, OnUpdateActualCCUTimer, null, null, null);
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
        public void UpdateLoadBlanceStatus(bool increase)
        {
            if (ServerStatus == ServerInternalStatus.Running)
            {
                msg_proxy.NotifyLoadBlanceStatus(increase);
            }
        }

        public void UpdateLoadBlanceStatus(int newCCU)
        {
            if (ServerStatus == ServerInternalStatus.Running)
            {
                msg_proxy.NotifyLoadBlanceStatus(newCCU);
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
                    CenterServerName = session.serverName;
                    CenterServerID = session.serverID;

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

                LegendServerACApplication.StopService();
            }
        }
        private void OnUpdateActualCCUTimer(object obj)
        {
            int ccu = SessionManager.Instance.GetActualCCU();
            ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus(ccu);
        }
    }
}