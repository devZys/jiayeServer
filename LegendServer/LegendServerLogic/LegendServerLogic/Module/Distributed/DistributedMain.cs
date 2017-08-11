using System.Collections.Concurrent;
using System.Linq;
using LegendProtocol;
using LegendServerLogic.Core;
using LegendServer.Util;
using LegendServer.LocalConfig;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System;
using LegendServerLogic.Actor.Summoner;
#if MAHJONG
using LegendServerLogic.Mahjong;
#elif RUNFAST
using LegendServerLogic.RunFast;
#elif WORDPLATE
using LegendServerLogic.WordPlate;
#endif

namespace LegendServerLogic.Distributed
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
        private ConcurrentQueue<PeerLink> connectedServerCheckerList = new ConcurrentQueue<PeerLink>();
        private ConcurrentQueue<Action> delayReconnectServerList = new ConcurrentQueue<Action>();
        static private int loadHouseCount = 0;
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
        public void AddConnectedServerChecker(PeerLink link)
        {
            if (TimerManager.Instance.Get(TimerId.InternalServerConnectedCheck) == null)
            {
                TimerManager.Instance.Regist(TimerId.InternalServerConnectedCheck, 0, 1000, int.MaxValue, OnInternalServerConnectedCheckTimer, null, null, null);
            }

            if (connectedServerCheckerList.FirstOrDefault(element => (element.toName == link.toName && element.toId == link.toId)) == null)
            {
                connectedServerCheckerList.Enqueue(link);
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
                if (session.serverName == "db")
                {
                    OnDBServerConnected(session.serverID);
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
        private void OnServiceStopKeyCheckTimer(object obj)
        {
            if (File.Exists(LocalConfigManager.Instance.CurrentPath + msg_proxy.root.ServerName + msg_proxy.root.ServerID))
            {
                File.Delete(LocalConfigManager.Instance.CurrentPath + msg_proxy.root.ServerName + msg_proxy.root.ServerID);

                LegendServerLogicApplication.StopService();
            }
        }
        private void OnUpdateActualCCUTimer(object obj)
        {
            int ccu = SummonerManager.Instance.GetSummonerCount();
            ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus(ccu);
        }
        public void SetLoadHouseCount(int count = 0)
        {
            loadHouseCount = count;
            ServerUtil.RecordLog(LogType.Info, "Logic 开始请求房间信息 Count = " + loadHouseCount);
            if (loadHouseCount == 0)
            {
                msg_proxy.root.OnDBLoadCompleted();
            }
        }
        public void DelLoadHouseCount()
        {
            loadHouseCount -= 1;
            ServerUtil.RecordLog(LogType.Info, "Logic 请求房间信息返回还剩 Count = " + loadHouseCount);
            if (loadHouseCount == 0)
            {
                msg_proxy.root.OnDBLoadCompleted();
#if RUNFAST
                ModuleManager.Get<RunFastMain>().CheckHouseOperateTimer();
#elif MAHJONG
                ModuleManager.Get<MahjongMain>().CheckHouseOperateTimer();
#elif WORDPLATE
                ModuleManager.Get<WordPlateMain>().CheckHouseOperateTimer();
#endif
            }
        }
        public void OnDBServerConnected(int dbServerID)
        {
#if MAHJONG
            ModuleManager.Get<MahjongMain>().OnRequestHouseInfo(dbServerID);
#elif RUNFAST
            ModuleManager.Get<RunFastMain>().OnRequestHouseInfo(dbServerID);
#elif WORDPLATE
            ModuleManager.Get<WordPlateMain>().OnRequestHouseInfo(dbServerID);
#endif
        }
    }

}