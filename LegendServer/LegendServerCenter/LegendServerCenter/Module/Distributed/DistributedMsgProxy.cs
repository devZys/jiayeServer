using System;
using LegendProtocol;
using LegendServerCenter.Core;
using LegendServer.LocalConfig;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LegendServerCenter.Distributed
{
    public class DistributedMsgProxy : ServerMsgProxy
    {
        private DistributedMain main;

        private List<int> acServerDbIdList = new List<int>();
        private List<int> acServerSelfIdList = new List<int>();
        private List<int> logicServerDbIdList = new List<int>();
        private List<int> logicServerSelfIdList = new List<int>();
        public DistributedMsgProxy(DistributedMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestDBConfig(int peerId, bool inbound, object msg)
        {
            RequestDBConfig_X2C reqMsg = msg as RequestDBConfig_X2C;

            ReplyDBConfig_C2X replyDBConfig = new ReplyDBConfig_C2X(); 

            DBConfigInfo info = root.GetDBConfig();
            replyDBConfig.address = info.address;
            replyDBConfig.port = info.port;
            replyDBConfig.database = info.database;
            replyDBConfig.user = info.user;
            replyDBConfig.password = info.password;
            replyDBConfig.logicCount = main.GetServerCount("logic");

            SendMsg(peerId, replyDBConfig);
        }
        public void OnServerLoadedNotify(int peerId, bool inbound, object msg)
        {
            ServerLoadedNotify_X2C msg_X2C = msg as ServerLoadedNotify_X2C;

            InboundSession mySelfSession = SessionManager.Instance.GetInboundSessionByPeerId(peerId);

            if (main.ExistServer(mySelfSession.serverName, mySelfSession.serverID))
            {
                ServerInternalStatus status = main.GetServerStatus(mySelfSession.serverName, mySelfSession.serverID);
                if (status == ServerInternalStatus.UnLoaded && main.ServerStatus != ServerInternalStatus.Running)
                {
                    //集群首次启动

                    //每次有服务器加载进来则将它的所有自身主动去连的出境服务器告诉Center，当所有服务器都启动完的时候，Center就可以反向计算每个目标出境服务器的入境服务器（即这个刚加载进来的服务器）
                    msg_X2C.allOutboundServer.ForEach(server =>
                    {
                        ServerInfo outboundServer = main.AllDeployServers.Find(element => element.name == server.name && element.id == server.id);
                        if (outboundServer != null && !outboundServer.allInboundServer.Exists(e => e.name == mySelfSession.serverName && e.id == mySelfSession.serverID))
                        {
                            outboundServer.allInboundServer.Add(new ServerIndex() { name = mySelfSession.serverName, id = mySelfSession.serverID });
                        }
                    });

                    main.SetServerStatus(mySelfSession.serverName, mySelfSession.serverID, ServerInternalStatus.Loaded);
                }
                else
                {
                    if (status == ServerInternalStatus.Closed && main.ServerStatus == ServerInternalStatus.Running)
                    {
                        //某个子服务器重连时
                        main.OnServerReconnect(mySelfSession);
                    }
                }
            }
            else
            {
                //新的服务器并入集群
                //暂时不做
                ServerUtil.RecordLog(LogType.Fatal, "没有配置过该服务器：" + mySelfSession.serverName + mySelfSession.serverID);
            }
        }

        public void OnRequestCloseCluster(int peerId, bool inbound, object msg)
        {
            NotifyCloseService_C2X notifyMsg = new NotifyCloseService_C2X();

            ServerUtil.RecordLog(LogType.Info, "集群被Root用户在后台Box工具中执行了关闭操作，即将断开所有子结点的通信！");

            //通知给所有子结点关闭服务
            BroadCastMsg(notifyMsg);

            //关闭自身
            LegendServerCenterApplication.StopService();
        }

        public void OnRequestControlExternalService(int peerId, bool inbound, object msg)
        {
            RequestControlExternalService_P2C reqMsg = msg as RequestControlExternalService_P2C;

            NotifyControlExternalService_C2X notifyMsg = new NotifyControlExternalService_C2X();
            notifyMsg.senderProxyServerId = reqMsg.senderProxyServerId;
            notifyMsg.senderBoxPeerId = reqMsg.senderBoxPeerId;
            notifyMsg.pause = reqMsg.pause;

            BroadCastMsg(notifyMsg, "ac");
            BroadCastMsg(notifyMsg, "proxy");
        }

        public void BroadCastServerRunning()
        {
            main.PublicKey = Guid.NewGuid().ToString();
            List<ServerInfo> acServerList = main.GetServerInfoList("ac");
            List<ServerInfo> logicServerList = main.GetServerInfoList("logic");
            List<ServerInfo> dbServerList = main.GetServerInfoList("db");

            ServerRunning_C2X msg = new ServerRunning_C2X();
            msg.publicKey = main.PublicKey;
            msg.joinWay = NodeJoinWay.ClusterStart;

            dbServerList.ForEach(element => msg.allDbServerId.Add(element.id));

            //给登陆认证服务器与逻辑服务器分配数据库缓存服务器
            acServerList.ForEach(element => 
            {
                ServerInfo db = main.GetOptimalServer("db");
                if (db != null)
                {
                    msg.acServerSelfIdList.Add(element.id);
                    msg.acServerDbIdList.Add(db.id);
                    db.ccu++;
                }
            });
            logicServerList.ForEach(element =>
            {
                ServerInfo db = main.GetOptimalServer("db");
                if (db != null)
                {
                    msg.logicServerSelfIdList.Add(element.id);
                    msg.logicServerDbIdList.Add(db.id);
                    db.ccu++;
                }
            });

            //备份分配好的服务器，待AC或者Logic宕机重连时有用
            acServerSelfIdList.Clear();
            acServerDbIdList.Clear();
            logicServerSelfIdList.Clear();
            logicServerDbIdList.Clear();
            acServerSelfIdList.AddRange(msg.acServerSelfIdList);
            acServerDbIdList.AddRange(msg.acServerDbIdList);
            logicServerSelfIdList.AddRange(msg.logicServerSelfIdList);
            logicServerDbIdList.AddRange(msg.logicServerDbIdList);

            //集群进入运行态，广播给所有节点
            foreach (InboundSession session in SessionManager.Instance.GetInboundSessionCollection().Values)
            {
                msg.allInboundServer.Clear();

                //获取该服务器的所有入境服务器
                List<ServerIndex> allInboundServer = main.GetAllInboundServer(session.serverName, session.serverID);
                if (allInboundServer != null && allInboundServer.Count > 0)
                {
                    msg.allInboundServer.AddRange(allInboundServer);
                }

                SendMsg(session.serverName, session.serverID, msg);
            }
        }
        public void NotifyServerRunningAgain(InboundSession session)
        {
            ServerRunning_C2X runningMsg_C2X = new ServerRunning_C2X();
            runningMsg_C2X.publicKey = main.PublicKey;     
            
            if (session.serverName == "ac" || session.serverName == "logic" || session.serverName == "world")
            {
                //将之前备份的DB集群分配信息发给刚连上的服务器（只有AC、Logic、World需要）
                List<ServerInfo> dbServerList = main.GetServerInfoList("db");
                dbServerList.ForEach(element => runningMsg_C2X.allDbServerId.Add(element.id));
                runningMsg_C2X.acServerSelfIdList.AddRange(acServerSelfIdList);
                runningMsg_C2X.acServerDbIdList.AddRange(acServerDbIdList);
                runningMsg_C2X.logicServerSelfIdList.AddRange(logicServerSelfIdList);
                runningMsg_C2X.logicServerDbIdList.AddRange(logicServerDbIdList);
            }

            runningMsg_C2X.joinWay = NodeJoinWay.HotUpdate;

            //获取该服务器的所有入境服务器，并通知他们主动连接我
            List<ServerIndex> allInboundServer = main.GetAllInboundServer(session.serverName, session.serverID);
            if (allInboundServer != null && allInboundServer.Count > 0)
            {
                runningMsg_C2X.allInboundServer.AddRange(allInboundServer);

                NotifyReconnectTargetServer_C2X reconectNotifyMsg_C2X = new NotifyReconnectTargetServer_C2X();
                reconectNotifyMsg_C2X.serverName = session.serverName;
                reconectNotifyMsg_C2X.serverId = session.serverID;
                allInboundServer.ForEach(element =>
                {
                    SendMsg(element.name, element.id, reconectNotifyMsg_C2X);
                });
            }

            //通知目标进入运行态
            SendMsg(session.peer.ConnectionId, runningMsg_C2X);
        }
        public void OnServerCCUNotify(int peerId, bool inbound, object msg)
        {
            NotifyLoadBlanceStatus_X2X fromMsg = msg as NotifyLoadBlanceStatus_X2X;
            main.UpdateLoadBlanceStatus(fromMsg.name, fromMsg.id, fromMsg.increase, fromMsg.newCCU);
        }
        public void OnNotifyVerifyPassed(int peerId, bool inbound, object msg)
        {
            NotifyVerifyPassed_D2C msg_D2C = msg as NotifyVerifyPassed_D2C;
            /**
                首先见：LegendServer拓扑架构图：http://www.cnblogs.com/legendstudio/p/4917617.html

                因为上次删档封测测试存在统计CCU时出现不准确的情况，需要做负载均衡的优化：

                每个类型的服务器可以多配置但是每个服务器在处理逻辑时是单线程的，但每个服务器就并行与Center交互的，不能由Proxy、AC、Logic在检测到玩家上线时将各自的CCU通知Center更新，因为当时大量玩家通过某个服务器涌入进来时CCU的更新消息还没到达Center的时候，Center在负载分配时则自然不再均衡，因为Center根据每个服务器的CCU来以及其他指标来分配最低CCU最优的服务器给玩家，而真实CCU都还没来的及更新过来就已来负载分配显然不合理。

                0、由玩家从AC登陆时Center统一累加AC、Proxy、Logic的CCU，其中Proxy和Logic提前累加CCU（第2、3点会纠正可能出现的误差），该步骤解决了大量玩家正常流程同时涌入进来负载不均衡的问题。
                1、每个AC和Proxy以及Logic在检测到玩家掉线时通知Center对各服务器的CCU减值
                2、每30秒将CCU通过AC和Proxy将当前Session里真实的在线状态的人数告知Center覆盖当前的统计，以纠正负载误差（比如只连接上AC时将Proxy和Logic的CCU也加上去了但玩家因为网络信号特殊情况或者逻辑异常并未在接下来连接上Proxy和进入Logic）。
                3、异地登陆时要将AC和Proxy中的所有旧的玩家的session断开（同时通知Center的CCU减减）。

                本思路不局限于LegendServer拓扑架构且仅供本人笔记记录与参考
            */
            ServerInfo proxy = main.GetOptimalServer("proxy");
            ServerInfo logic = msg_D2C.logicId > 0 ? main.GetRunningServer("logic", msg_D2C.logicId) : main.GetOptimalServer("logic");
            ServerInfo ac = main.GetRunningServer("ac", msg_D2C.acId);
            if (proxy != null && logic != null && ac != null)
            {
                TokenNotify_C2P notifyMsg = new TokenNotify_C2P();
                //token: userId|ip|时间|最优网关服务器|最优逻辑服务器" => "198777|192.168.123.125|2015-06-19 17:43:02|1"（本游戏不需要登陆第三方渠道平台，token关键信息是ip即可）
                notifyMsg.accessToken = msg_D2C.requesterUserId + "|" + msg_D2C.requesterIp + "|" + DateTime.Now.ToString() + "|" + logic.id;
                notifyMsg.acPeerId = msg_D2C.acPeerId;
                notifyMsg.acId = msg_D2C.acId;
                notifyMsg.auth = msg_D2C.auth;
                notifyMsg.summonerId = msg_D2C.summonerId;
                main.GetClosedServerInfoList("ac").ForEach(element => notifyMsg.closedACServerList.Add(element.id));                
                SendProxyMsg(notifyMsg, proxy.id);

                ServerUtil.RecordLog(LogType.Debug, "通过认证,分配最佳网关服ID:" + proxy.id + " 最佳逻辑服ID:" + logic.id);

                proxy.ccu++;
                logic.ccu++;
                ac.ccu++;
            }
            else
            {
                //没有可供负载的服务器了通知给玩家
                NotifyServerClosed_C2A notifyClosedMsg = new NotifyServerClosed_C2A();
                notifyClosedMsg.acPeerId = msg_D2C.acPeerId;
                SendACMsg(notifyClosedMsg, msg_D2C.acId);
            }
        }
        public void OnUpdateServerCfg(int peerId, bool inbound, object msg)
        {
            UpdateServerCfg_X2X msg_X2X = msg as UpdateServerCfg_X2X;

            InboundSession session = SessionManager.Instance.GetInboundSessionByPeerId(peerId);
            ServerIndex requester = new ServerIndex() { name = session.serverName, id = session.serverID };
            if (msg_X2X.server == "all")
            {
                //首先更新自己
                UpdateConfig(msg_X2X.updateType);

                //然后更新除了请求方外的其他所有服务器
                BroadCastMsgByExclude(msg_X2X, requester);
            }
            else
            {
                if (msg_X2X.server == "center")
                {
                    //更新自己
                    UpdateConfig(msg_X2X.updateType);
                }
                else
                {
                    //继续更新除了请求方外的其他类型服务器
                    BroadCastMsgByExclude(msg_X2X, msg_X2X.server, requester);
                }
            }
        }
        public void UpdateConfig(ServerCfgUpdateType updateType)
        {
            if (updateType == ServerCfgUpdateType.LocalConfig)
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
                if (updateType == ServerCfgUpdateType.DBConfig)
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

