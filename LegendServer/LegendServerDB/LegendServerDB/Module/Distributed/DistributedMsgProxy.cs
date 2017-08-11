using System;
using LegendProtocol;
using LegendServerDB.Core;
using System.Diagnostics;
using System.Linq;
using LegendServer.Database.ServiceBox;
using LegendServer.Database;
using LegendServer.Database.Profiler;
using LegendServer.Database.Summoner;
using LegendServer.Database.Config;
using System.Collections.Generic;
using LegendServer.LocalConfig;
using LegendServer.Database.ServerDeploy;
using System.Net;
using LegendServerDB.MainCity;
#if MAHJONG
using LegendServerDB.Mahjong;
#elif RUNFAST
using LegendServerDB.RunFast;
#elif WORDPLATE
using LegendServerDB.WordPlate;
#endif

namespace LegendServerDB.Distributed
{
    public class DistributedMsgProxy : ServerMsgProxy
    {
        private DistributedMain main;
        public DistributedMsgProxy(DistributedMain main)
            : base(main.root)
        {
            this.main = main;
        }

        public void OnNotifyCloseService(int peerId, bool inbound, object msg)
        {
            NotifyCloseService_C2X notifyMsg = msg as NotifyCloseService_C2X;

            LegendServerDBApplication.StopService();
        }
        public void RequestDBConfig()
        {
            RequestDBConfig_X2C msg = new RequestDBConfig_X2C();
            SendCenterMsg(msg);
        }
        public void OnReplyDBConfig(int peerId, bool inbound, object msg)
        {
            ReplyDBConfig_C2X replyDBConfig = msg as ReplyDBConfig_C2X;

            main.LogicServerCount = replyDBConfig.logicCount;
            main.LoadDataBase(replyDBConfig.address, replyDBConfig.port, replyDBConfig.database, replyDBConfig.user, replyDBConfig.password);
        }
        public void NotifyServerLoaded()
        {
            ServerLoadedNotify_X2C msg = new ServerLoadedNotify_X2C();
            msg.allOutboundServer.AddRange(main.AllOutboundServer);
            SendMsg("center", 1, false, msg);
        }
        public void OnReciveRunningNotify(int peerId, bool inbound, object msg)
        {
            ServerRunning_C2X msg_C2X = msg as ServerRunning_C2X;
            main.PublicKey = msg_C2X.publicKey;

            //更新所有入境服务器
            main.AllInboundServer.Clear();
            main.AllInboundServer.AddRange(msg_C2X.allInboundServer);
            main.AllWaitConnectInboundServer.Clear();

            //保存所有映射到我自己的逻辑服
            for (int index = 0; index < msg_C2X.logicServerDbIdList.Count; index++)
            {
                if (msg_C2X.logicServerDbIdList[index] == root.ServerID)
                {
                    root.AllLogicMappingMe.Add(msg_C2X.logicServerSelfIdList[index]);
                }
            }

            ModuleManager.Start();
            main.ServerStatus = ServerInternalStatus.Running;

            int inboundCount = SessionManager.Instance.GetInboundSessionCollection().Values.Count(element => element.serverName == "db");
            int outboundCount = SessionManager.Instance.GetOutboundSessionCollection().Values.Count(element => element.serverName == "db");
            main.DBServerCount = inboundCount + outboundCount + 1;

            if ((main.DBServerCount) != msg_C2X.allDbServerId.Count)
            {
                ServerUtil.RecordLog(LogType.Fatal, "CenterServer里收到的已注册的所有DBServer结点并没有彼此互连成功！");
                return;
            }
        }

        public void OnReconnectTargetServerNotify(int peerId, bool inbound, object msg)
        {
            NotifyReconnectTargetServer_C2X msg_C2X = msg as NotifyReconnectTargetServer_C2X;

            if (main.AllOutboundServer.Exists(server => server.name == msg_C2X.serverName && server.id == msg_C2X.serverId))
            {
                ServerDeployConfigDB mySelfDeplyCfg = DBManager<ServerDeployConfigDB>.Instance.GetSingleRecordInCache(element => element.name == root.ServerName && element.id == root.ServerID);
                if (mySelfDeplyCfg == null)
                {
                    ServerUtil.RecordLog(LogType.Fatal, "自身去重连其他服务器时发现自身不在集群拓扑结构计划中，请检查服务器重启进集群的流程！！");
                    return;
                }
                ServerDeployConfigDB targetDeplyCfg = DBManager<ServerDeployConfigDB>.Instance.GetSingleRecordInCache(element => element.name == msg_C2X.serverName && element.id == msg_C2X.serverId);
                if (targetDeplyCfg == null)
                {
                    ServerUtil.RecordLog(LogType.Fatal, "需要重连的服务器不在集群拓扑结构表中，请检查服务器重启进集群的流程！！");
                    return;
                }

                PeerLink peerLink = new PeerLink(mySelfDeplyCfg.name, mySelfDeplyCfg.ip, mySelfDeplyCfg.tcp_port, mySelfDeplyCfg.id, targetDeplyCfg.name, targetDeplyCfg.ip, targetDeplyCfg.tcp_port, targetDeplyCfg.id);
                LocalConfigInfo mySelfCfg = LocalConfigManager.Instance.GetConfig("MySelfServer");
                new OutboundPeer(root, peerLink).ConnectTcp(new IPEndPoint(IPAddress.Parse(peerLink.toIp), peerLink.toPort), mySelfCfg.value, null);
            }
            else
            {
                ServerUtil.RecordLog(LogType.Fatal, "需要重连的服务器不在集群拓扑结构计划中，请检查服务器重启进集群的流程！！");
                return;
            }
        }

        public void OnSyncDBCache(int peerId, bool inbound, object msg)
        {
            SyncDBCache_D2D msg_D2D = msg as SyncDBCache_D2D;

            //收到另一个DBServer的该同步消息后，在本地DB缓存同步机制里唯一需要调用的接口，tableName为数据表名，Handle调用参数分别为：被同步的记录数据、操作类型、表的关键字对应的值
            DBCacheSyncFacade.Instance.Handle(msg_D2D.tableName)(msg_D2D.recordData, msg_D2D.operateType, msg_D2D.keyFieldValues);
        }
        public void OnUpdateServerCfg(int peerId, bool inbound, object msg)
        {
            UpdateServerCfg_X2X msg_X2X = msg as UpdateServerCfg_X2X;

            if (msg_X2X.updateType == ServerCfgUpdateType.LocalConfig)
            {
                if (LocalConfigManager.Instance.LoadLocalConfig())
                {
                    ServerUtil.RecordLog(LogType.Info, "热更新本地配置成功！", new StackTrace(new StackFrame(true)).GetFrame(0));
                    return;
                }
                else
                {
                    ServerUtil.RecordLog(LogType.Fatal, "热更新本地配置出错！！！", new StackTrace(new StackFrame(true)).GetFrame(0));
                    return;
                }
            }
            else
            {
                if (msg_X2X.updateType == ServerCfgUpdateType.DBConfig)
                {
                    string result = ServerInitialize.LoadDBConfig();
                    if (String.IsNullOrEmpty(result))
                    {
                        ServerUtil.RecordLog(LogType.Info, "热更新数据库配置成功！", new StackTrace(new StackFrame(true)).GetFrame(0));
                    }
                    else
                    {
                        ServerUtil.RecordLog(LogType.Fatal, "热更新数据库配置出错！！！【" + result + "】", new StackTrace(new StackFrame(true)).GetFrame(0));
                    }
                }
            }
        }
    }
}

