using System;
using LegendProtocol;
using LegendServerWorld.Core;
using System.Diagnostics;
using LegendServer.LocalConfig;
using LegendServer.Util;
using LegendServer.Database.ServerDeploy;
using System.Net;
using LegendServer.Database;
using LegendServerWorld.UIDAlloc;

namespace LegendServerWorld.Distributed
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

            LegendServerWorldApplication.StopService();
        }
        public void RequestDBConfig()
        {
            RequestDBConfig_X2C msg = new RequestDBConfig_X2C();
            SendCenterMsg(msg);
        }
        public void OnReplyDBConfig(int peerId, bool inbound, object msg)
        {
            ReplyDBConfig_C2X replyDBConfig = msg as ReplyDBConfig_C2X;

            string result = ServerInitialize.LoadDatabaseToCache(replyDBConfig.address, replyDBConfig.port, replyDBConfig.database, replyDBConfig.user, replyDBConfig.password);
            if (!String.IsNullOrEmpty(result))
            {
                ServerUtil.RecordLog(LogType.Fatal, "World 数据库加载至缓存出错," + result, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }

            //生成房间Id池
            ModuleManager.Get<UIDAllocMain>().BuildRoomIDPool();

            main.DBLoaded = true;

            root.OnDBLoadCompleted();
        }
        public void NotifyServerLoaded()
        {
            ServerLoadedNotify_X2C msg = new ServerLoadedNotify_X2C();
            msg.allOutboundServer.AddRange(main.AllOutboundServer);
            SendMsg("center", 1, false, msg);
        }
        public void NotifyLoadBlanceStatus(string serverName, int serverID, bool increase)
        {
            NotifyLoadBlanceStatus_X2X notifyMsg = new NotifyLoadBlanceStatus_X2X();
            notifyMsg.name = serverName;
            notifyMsg.id = serverID;
            notifyMsg.increase = increase;
            SendCenterMsg(notifyMsg);
        }
        public void OnReciveRunningNotify(int peerId, bool inbound, object msg)
        {
            ServerRunning_C2X msg_C2X = msg as ServerRunning_C2X;
            main.PublicKey = msg_C2X.publicKey;

            //更新所有入境服务器
            main.AllInboundServer.Clear();
            main.AllInboundServer.AddRange(msg_C2X.allInboundServer);
            main.AllWaitConnectInboundServer.Clear();

            root.DBServerList.AddRange(msg_C2X.allDbServerId);

            //集群初次启动
            if (msg_C2X.joinWay == NodeJoinWay.ClusterStart)
            {
                ModuleManager.Start();
                main.ServerStatus = ServerInternalStatus.Running;
            }
            else
            {
                //热更新重启
                if (msg_C2X.joinWay == NodeJoinWay.HotUpdate)
                {
                    if (msg_C2X.allInboundServer.Count <= 0)
                    {
                        //本服务器没有入境服务器直接进行运行态
                        ModuleManager.Start();
                        main.ServerStatus = ServerInternalStatus.Running;

                        ServerUtil.RecordLog(LogType.Info, "热更新后自适应并入集群！");
                    }
                    else
                    {
                        //本服务器有入境服务器则开启动计时器等待入境服务器的连接，均连入了则进行运行态
                        if (TimerManager.Instance.Get(TimerId.InboundServerReconnectCheck) == null)
                        {
                            main.AllWaitConnectInboundServer.AddRange(msg_C2X.allInboundServer);

                            TimerManager.Instance.Regist(TimerId.InboundServerReconnectCheck, 0, 1000, int.MaxValue, main.OnInboundServerReconnectCheck, null, null, null);
                        }
                    }
                }
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

