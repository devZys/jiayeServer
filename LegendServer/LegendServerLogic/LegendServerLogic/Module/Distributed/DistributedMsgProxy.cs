using System;
using LegendProtocol;
using LegendServerLogic.Core;
using System.Diagnostics;
using LegendServer.LocalConfig;
using LegendServer.Util;
using LegendServer.Database.ServerDeploy;
using System.Net;
using LegendServer.Database;

namespace LegendServerLogic.Distributed
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

            LegendServerLogicApplication.StopService();
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
                ServerUtil.RecordLog(LogType.Fatal, "Logic 数据库加载至缓存出错," + result, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }

            ModuleManager.RegistTimer();
        }
        public void NotifyServerLoaded()
        {
            ServerLoadedNotify_X2C msg = new ServerLoadedNotify_X2C();
            msg.allOutboundServer.AddRange(main.AllOutboundServer);
            SendMsg("center", 1, msg);
        }
        public void OnReciveRunningNotify(int peerId, bool inbound, object msg)
        {
            ServerRunning_C2X msg_C2X = msg as ServerRunning_C2X;
            main.PublicKey = msg_C2X.publicKey;

            int index = msg_C2X.logicServerSelfIdList.FindIndex(element => element == root.ServerID);
            if (index >= 0)
            {
                root.DBServerID = msg_C2X.logicServerDbIdList[index];
            }
            else
            {
                root.DBServerID = msg_C2X.logicServerDbIdList[MyRandom.Next(msg_C2X.logicServerDbIdList.Count)];
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

