using System.Collections.Generic;
using System.Collections.Concurrent;
using LegendProtocol;
using System.Linq;
using LegendServerDB.Core;
using LegendServer.Util;
using LegendServer.LocalConfig;
using System.Net;
using System.IO;
using System;
using LegendServer.Database.ServiceBox;
using LegendServer.Database.Summoner;
using System.Diagnostics;
#if MAHJONG
using LegendServer.Database.Mahjong;
#elif RUNFAST
using LegendServer.Database.RunFast;
#elif WORDPLATE
using LegendServer.Database.WordPlate;
#endif

namespace LegendServerDB.Distributed
{
    public class DistributedMain : Module
    {
        public DistributedMsgProxy msg_proxy;
        public string PublicKey = "";
        public ServerInternalStatus ServerStatus = ServerInternalStatus.UnLoaded;
        public bool DBLoaded = false;
        public int DBServerCount = 1;
        public List<ServerIndex> AllOutboundServer = new List<ServerIndex>();
        public List<ServerIndex> AllInboundServer = new List<ServerIndex>();
        public List<ServerIndex> AllWaitConnectInboundServer = new List<ServerIndex>();
        private ConcurrentQueue<PeerLink> connectedServerCheckerList = new ConcurrentQueue<PeerLink>();
        private ConcurrentQueue<Action> delayReconnectServerList = new ConcurrentQueue<Action>();
        private List<Action> runningCallBack = new List<Action>();
        public int LogicServerCount = 0;//拓扑结构中所有逻辑服数量
        public bool StartLogicCompleted = false;//开服逻辑是否完成
        public int CurrentExeLogic = 0;//当前执行的逻辑数量
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

        public void LoadDataBase(string address, string port, string database, string user, string password)
        {
            if (!DBLoaded)
            {
                string result = ServerInitialize.LoadDatabaseToCache(address, port, database, user, password);
                if (!String.IsNullOrEmpty(result))
                {
                    ServerUtil.RecordLog(LogType.Fatal, "DB 数据库加载至缓存出错," + result, new StackTrace(new StackFrame(true)).GetFrame(0));
                    return;
                }

                if (!RegistAllSyncTable())
                {
                    ServerUtil.RecordLog(LogType.Fatal, "DB 注册需要同步缓存的表时出错," + result, new StackTrace(new StackFrame(true)).GetFrame(0));
                    return;
                }

                DBLoaded = true;

                msg_proxy.root.OnDBLoadCompleted();
            }
        }

        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.C2X_NotifyCloseService, new MsgComponent(msg_proxy.OnNotifyCloseService, typeof(NotifyCloseService_C2X)));
            MsgFactory.Regist(MsgID.C2X_ReplyDBConfig, new MsgComponent(msg_proxy.OnReplyDBConfig, typeof(ReplyDBConfig_C2X)));
            MsgFactory.Regist(MsgID.C2X_ServerRunning, new MsgComponent(msg_proxy.OnReciveRunningNotify, typeof(ServerRunning_C2X)));
            MsgFactory.Regist(MsgID.D2D_SyncDBCache, new MsgComponent(msg_proxy.OnSyncDBCache, typeof(SyncDBCache_D2D)));
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
        public bool RegistAllSyncTable()
        {
            do
            {
                if (!DBCacheSync<ServiceBoxTestDB>.Instance.RegistSyncTable(new List<string> { "guid" })) break;
                if (!DBCacheSync<DBCacheSyncTestDB>.Instance.RegistSyncTable(new List<string> { "guid" })) break;
                if (!DBCacheSync<SummonerDB>.Instance.RegistSyncTable(new List<string> { "id" })) break;
#if MAHJONG
                if (!DBCacheSync<MahjongHouseDB>.Instance.RegistSyncTable(new List<string> { "houseId" })) break;
                if (!DBCacheSync<MahjongPlayerDB>.Instance.RegistSyncTable(new List<string> { "houseId", "summonerId" })) break;
                if (!DBCacheSync<MahjongBureauDB>.Instance.RegistSyncTable(new List<string> { "houseId", "bureau" })) break;
#elif RUNFAST
                if (!DBCacheSync<RunFastHouseDB>.Instance.RegistSyncTable(new List<string> { "houseId" })) break;
                if (!DBCacheSync<RunFastPlayerDB>.Instance.RegistSyncTable(new List<string> { "houseId", "summonerId" })) break;
                if (!DBCacheSync<RunFastBureauDB>.Instance.RegistSyncTable(new List<string> { "houseId", "bureau" })) break;
#elif WORDPLATE
                if (!DBCacheSync<WordPlateHouseDB>.Instance.RegistSyncTable(new List<string> { "houseId" })) break;
                if (!DBCacheSync<WordPlatePlayerDB>.Instance.RegistSyncTable(new List<string> { "houseId", "summonerId" })) break;
                if (!DBCacheSync<WordPlateBureauDB>.Instance.RegistSyncTable(new List<string> { "houseId", "bureau" })) break;
#endif
                return true;
            } while (false);

            return false;
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
        private void OnInternalServerConnectedCheckTimer(object obj)
        {
            if (connectedServerCheckerList.Count <= 0)
            {
                if (DBLoaded && StartLogicCompleted)
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
        //同步缓存给其他DB缓存服务器
        public void SyncCache<T>(T record, List<ulong> keyFieldValue, DataOperate operateType) where T : class
        {
            if (DBServerCount <= 1) return;
            if (!DBCacheSyncFacade.Instance.IsMustSync<T>()) return;

            SyncDBCache_D2D msg_D2D = new SyncDBCache_D2D();
            msg_D2D.keyFieldValues.AddRange(keyFieldValue);
            msg_D2D.operateType = operateType;
            msg_D2D.recordData = DBCacheSyncFacade.Instance.Serialize(record);
            msg_D2D.tableName = typeof(T).ToString();

            msg_proxy.BroadCastDBMsg(msg_D2D);
        }
        private void OnServiceStopKeyCheckTimer(object obj)
        {
            if (File.Exists(LocalConfigManager.Instance.CurrentPath + msg_proxy.root.ServerName + msg_proxy.root.ServerID))
            {
                File.Delete(LocalConfigManager.Instance.CurrentPath + msg_proxy.root.ServerName + msg_proxy.root.ServerID);

                LegendServerDBApplication.StopService();
            }
        }
        public void RegistLoadDBCompletedCallBack(Action handle)
        {
            runningCallBack.Add(handle);
        }
        public void ProcessLoadDBCompletedCallBack()
        {
            runningCallBack.ForEach(e => 
            {
                e();
            });
            runningCallBack.Clear();
        }
        public void AddCurrentExeLogic()
        {
            CurrentExeLogic++;
            if (CurrentExeLogic == LogicServerCount)
            {
                StartLogicCompleted = true;
            }
        }
    }
}