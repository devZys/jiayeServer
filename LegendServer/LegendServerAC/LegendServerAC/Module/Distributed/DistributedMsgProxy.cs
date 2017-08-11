using System;
using LegendProtocol;
using LegendServerAC.Core;
using System.Diagnostics;
using LegendServer.Util;
using LegendServer.LocalConfig;
using System.Net;
using LegendServer.Database;
using LegendServer.Database.ServerDeploy;
using System.Collections.Generic;

namespace LegendServerAC.Distributed
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

            LegendServerACApplication.StopService();
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
                ServerUtil.RecordLog(LogType.Fatal, "AC 数据库加载至缓存出错," + result, new StackTrace(new StackFrame(true)).GetFrame(0));
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

            int index = msg_C2X.acServerSelfIdList.FindIndex(element => element == root.ServerID);
            if (index >= 0)
            {
                root.DBServerID = msg_C2X.acServerDbIdList[index];
            }
            else
            {
                root.DBServerID = msg_C2X.acServerDbIdList[MyRandom.Next(msg_C2X.acServerDbIdList.Count)];
            }

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
                    //本服务器没有入境服务器直接进行运行态
                    ModuleManager.Start();
                    main.ServerStatus = ServerInternalStatus.Running;

                    ServerUtil.RecordLog(LogType.Info, "热更新后自适应并入集群！");
                }
            }
        }

        public void OnNotifyControlExternalService(int peerId, bool inbound, object msg)
        {
            NotifyControlExternalService_C2X notifyMsg = msg as NotifyControlExternalService_C2X;

            SessionManager.Instance.ServicePause = notifyMsg.pause;
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
        public void OnUpdateServerCfg(int peerId, bool inbound, object msg)
        {
            UpdateServerCfg_X2X msg_X2X = msg as UpdateServerCfg_X2X;

            if (msg_X2X.updateType == ServerCfgUpdateType.LocalConfig)
            {
                if (LocalConfigManager.Instance.LoadLocalConfig())
                {
                    ServerUtil.RecordLog(LogType.Info, "热更新本地配置成功！", new StackTrace(new StackFrame(true)).GetFrame(0));
                }
                else
                {
                    ServerUtil.RecordLog(LogType.Fatal, "热更新本地配置出错！！！", new StackTrace(new StackFrame(true)).GetFrame(0));
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
        public void OnKickOldACPeer(int peerId, bool inbound, object msg)
        {
            KickOldACPeer_P2A msg_P2A = msg as KickOldACPeer_P2A;

            //世界服务器在收到异地登陆时不管分配到了相同还是不相同的认证服都要通过该玩家所在的网关通知旧的认证服断开旧连接
            //如果异地登陆了不同的认证服务器则只会是旧的认证服务器收到此消息

            //通过用户ID获取客户端玩家入境会话（按连接时间降序）
            List<InboundClientSession> result = SessionManager.Instance.GetAllInboundClientSessionByUserId(msg_P2A.userId);

            if (ModuleManager.Get<DistributedMain>().msg_proxy.root.ServerID == msg_P2A.newServerId)
            {
                //如果多处异地登陆到了同一个旧的认证服务器，只保留最后一个登陆的连接，其他全部剔除
                int removeCount = 0;
                for (int index = 1; index < result.Count; index++)
                {
                    InboundClientSession client = result[index];
                    if (client != null)
                    {
                        if (client.peer != null)
                        {
                            SessionManager.Instance.RemoveInboundClientSession(client.peer.ConnectionId);
                            client.peer.Disconnect();
                        }

                        client.status = SessionStatus.Disconnect;
                        ObjectPoolManager<InboundClientSession>.Instance.FreeObject(client);

                        ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus(false);
                        removeCount++;
                    }
                }
                if (removeCount > 0)
                {
                    ServerUtil.RecordLog(LogType.Debug, "异地登陆时旧的认证服务器收到通知将玩家：" + msg_P2A.userId + " 从该服务器全部剔除！数量为：" + removeCount);
                }
            }
            else
            {
                //如果异地登陆到了不同的认证服务器则只管把这个旧认证服务器上的连接全部剔除
                int removeCount = 0;
                result.ForEach(client => 
                {
                    if (client != null)
                    {
                        if (client.peer != null)
                        {
                            SessionManager.Instance.RemoveInboundClientSession(client.peer.ConnectionId);
                            client.peer.Disconnect();
                        }

                        client.status = SessionStatus.Disconnect;
                        ObjectPoolManager<InboundClientSession>.Instance.FreeObject(client);

                        ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus(false);
                        removeCount++;
                    }
                });
                if (removeCount > 0)
                {
                    ServerUtil.RecordLog(LogType.Debug, "异地登陆时旧的认证服务器收到通知将玩家：" + msg_P2A.userId + " 从该服务器全部剔除！数量为：" + removeCount);
                }
            }
        }
    }
}

