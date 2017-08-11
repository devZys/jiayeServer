using System;
using LegendProtocol;
using LegendServerProxy.Core;
using System.Diagnostics;
using LegendServer.LocalConfig;
using LegendServer.Util;
using LegendServer.Database.ServerDeploy;
using System.Net;
using LegendServer.Database;
using System.Collections.Generic;
using LegendServer.Database.Profiler;

namespace LegendServerProxy.Distributed
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

            LegendServerProxyApplication.StopService();
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
                ServerUtil.RecordLog(LogType.Fatal, "proxy 数据库加载至缓存出错," + result, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }            

            main.DBLoaded = true;

            root.OnDBLoadCompleted();
        }
        public void NotifyServerLoaded()
        {
            ServerLoadedNotify_X2C msg = new ServerLoadedNotify_X2C();
            msg.allOutboundServer.AddRange(main.AllOutboundServer);
            SendCenterMsg(msg);
        }
        public void OnReciveRunningNotify(int peerId, bool inbound, object msg)
        {
            ServerRunning_C2X msg_C2X = msg as ServerRunning_C2X;
            main.PublicKey = msg_C2X.publicKey;

            //更新所有入境服务器
            main.AllInboundServer.Clear();
            main.AllInboundServer.AddRange(msg_C2X.allInboundServer);
            main.AllWaitConnectInboundServer.Clear();

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
        public void OnKickOldACPeer(int peerId, bool inbound, object msg)
        {
            KickOldACPeer_W2P msg_W2P = msg as KickOldACPeer_W2P;

            //世界服务器在收到异地登陆时不管分配到了相同还是不相同的认证服都要通过该玩家所在的网关通知旧的认证服断开旧连接
            KickOldACPeer_P2A msg_P2A = new KickOldACPeer_P2A();
            msg_P2A.userId = msg_W2P.userId;
            msg_P2A.newServerId = msg_W2P.newACServerId;
            SendACMsg(msg_P2A, msg_W2P.oldACServerId);
        }

        public void OnRequestCloseCluster(int peerId, bool inbound, object msg)
        {
            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);
            if (session.auth != UserAuthority.Root) return;

            //交给中心服务器去广播
            SendCenterMsg(msg);
        }

        public void OnRequestControlExternalService(int peerId, bool inbound, object msg)
        {
            RequestControlExternalService_B2P reqMsg_B2P = msg as RequestControlExternalService_B2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);
            if (session.auth != UserAuthority.Root) return;

            RequestControlExternalService_P2C reqMsg_P2C = new RequestControlExternalService_P2C();
            reqMsg_P2C.senderProxyServerId = root.ServerID;
            reqMsg_P2C.senderBoxPeerId = peerId;
            reqMsg_P2C.pause = reqMsg_B2P.pause;
            SendCenterMsg(reqMsg_P2C);
        }

        public void OnNotifyControlExternalService(int peerId, bool inbound, object msg)
        {
            NotifyControlExternalService_C2X notifyMsg = msg as NotifyControlExternalService_C2X;

            SessionManager.Instance.ServicePause = notifyMsg.pause;

            if (notifyMsg.senderProxyServerId == root.ServerID)
            {
                InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(notifyMsg.senderBoxPeerId);
                if (session != null && session.peer != null && session.status == SessionStatus.Connected)
                {
                    ReplyControlExternalService_P2B replyMsg = new ReplyControlExternalService_P2B();
                    replyMsg.pause = notifyMsg.pause;
                    SendClientMsg(notifyMsg.senderBoxPeerId, replyMsg);
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
        public void NotifyLoadBlanceStatus(bool increase)
        {
            NotifyLoadBlanceStatus_X2X notifyMsg = new NotifyLoadBlanceStatus_X2X();
            notifyMsg.name = root.ServerName;
            notifyMsg.id = root.ServerID;
            notifyMsg.increase = increase;
            notifyMsg.newCCU = -1;
            SendCenterMsg(notifyMsg);
        }
        public void NotifyLoadBlanceStatus(int newCCU)
        {
            NotifyLoadBlanceStatus_X2X notifyMsg = new NotifyLoadBlanceStatus_X2X();
            notifyMsg.name = root.ServerName;
            notifyMsg.id = root.ServerID;
            notifyMsg.newCCU = newCCU;
            SendCenterMsg(notifyMsg);
        }
    }
}

