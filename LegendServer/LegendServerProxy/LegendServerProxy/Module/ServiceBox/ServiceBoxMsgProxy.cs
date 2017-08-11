using System;
using LegendProtocol;
using LegendServerProxy.Core;
using LegendServer.LocalConfig;
using System.Diagnostics;
using System.Text;
using LegendServer.Database.Config;
using LegendServer.Database;

namespace LegendServerProxy.ServiceBox
{
    public class ServiceBoxMsgProxy : ServerMsgProxy
    {
        private ServiceBoxMain main;

        public ServiceBoxMsgProxy(ServiceBoxMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestLoadblanceStatus(int peerId, bool inbound, object msg)
        {
            RequestLoadblanceStatus_B2P reqMsg_B2P = msg as RequestLoadblanceStatus_B2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestLoadblanceStatus_P2C reqMsg_P2C = new RequestLoadblanceStatus_P2C();
            reqMsg_P2C.userId = session.userId;
            reqMsg_P2C.serverType = reqMsg_B2P.serverType;
            SendCenterMsg(reqMsg_P2C);
        }
        public void OnReplyLoadblanceStatus(int peerId, bool inbound, object msg)
        {
            ReplyLoadblanceStatus_C2P replyMsg_C2P = msg as ReplyLoadblanceStatus_C2P;

            ReplyLoadblanceStatus_P2B replyMsg_P2B = new ReplyLoadblanceStatus_P2B();
            replyMsg_P2B.serverType = replyMsg_C2P.serverType;
            replyMsg_P2B.result.AddRange(replyMsg_C2P.result);
            SendClientMsg(replyMsg_C2P.userId, replyMsg_P2B);            
        }

        public void OnRequestTestCalcLogic(int peerId, bool inbound, object msg)
        {
            RequestTestCalcLogic_B2P reqMsg_B2P = msg as RequestTestCalcLogic_B2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestTestCalcLogic_P2L reqMsg_P2L = new RequestTestCalcLogic_P2L();
            reqMsg_P2L.userId = session.userId;
            reqMsg_P2L.param1 = reqMsg_B2P.param1;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }

        public void OnReplyTestCalcLogic(int peerId, bool inbound, object msg)
        {
            ReplyTestCalcLogic_L2P replyMsg_L2P = msg as ReplyTestCalcLogic_L2P;

            ReplyTestCalcLogic_P2B replyMsg_P2B = new ReplyTestCalcLogic_P2B();
            replyMsg_P2B.result = replyMsg_L2P.result;
            SendClientMsg(replyMsg_L2P.userId, replyMsg_P2B);
        }
        public void OnRequestTestDB(int peerId, bool inbound, object msg)
        {
            RequestTestDB_B2P reqMsg_B2P = msg as RequestTestDB_B2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestTestDB_P2L reqMsg_P2L = new RequestTestDB_P2L();
            reqMsg_P2L.userId = session.userId;
            reqMsg_P2L.operate = reqMsg_B2P.operate;
            reqMsg_P2L.strategy = reqMsg_B2P.strategy;
            reqMsg_P2L.loop = reqMsg_B2P.loop;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyTestDB(int peerId, bool inbound, object msg)
        {
            ReplyTestDB_L2P replyMsg_B2P = msg as ReplyTestDB_L2P;
            
            ReplyTestDB_P2B replyMsg_P2B = new ReplyTestDB_P2B();
            replyMsg_P2B.result = replyMsg_B2P.result;
            SendClientMsg(replyMsg_B2P.userId, replyMsg_P2B);
        }

        public void OnRequestTestDBCacheSync(int peerId, bool inbound, object msg)
        {
            RequestTestDBCacheSync_B2P reqMsg_B2P = msg as RequestTestDBCacheSync_B2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestTestDBCacheSync_P2L reqMsg_P2L = new RequestTestDBCacheSync_P2L();
            reqMsg_P2L.userId = session.userId;
            reqMsg_P2L.data = reqMsg_B2P.data;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyTestDBCacheSync(int peerId, bool inbound, object msg)
        {
            ReplyTestDBCacheSync_L2P replyMsg_B2P = msg as ReplyTestDBCacheSync_L2P;

            ReplyTestDBCacheSync_P2B replyMsg_P2B = new ReplyTestDBCacheSync_P2B();
            replyMsg_P2B.data = replyMsg_B2P.data;
            SendClientMsg(replyMsg_B2P.userId, replyMsg_P2B);
        }
        public void OnRequestGetDBCacheData(int peerId, bool inbound, object msg)
        {
            RequestGetDBCacheData_B2P reqMsg_B2P = msg as RequestGetDBCacheData_B2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestGetDBCacheData_P2L reqMsg_P2L = new RequestGetDBCacheData_P2L();
            reqMsg_P2L.userId = session.userId;
            reqMsg_P2L.dbServerId = reqMsg_B2P.dbServerId;
            reqMsg_P2L.guid = reqMsg_B2P.guid;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyGetDBCacheData(int peerId, bool inbound, object msg)
        {
            ReplyGetDBCacheData_L2P replyMsg_B2P = msg as ReplyGetDBCacheData_L2P;

            ReplyGetDBCacheData_P2B replyMsg_P2B = new ReplyGetDBCacheData_P2B();
            replyMsg_P2B.dbServerId = replyMsg_B2P.dbServerId;
            replyMsg_P2B.paramTotal = replyMsg_B2P.paramTotal;
            SendClientMsg(replyMsg_B2P.userId, replyMsg_P2B);
        }
        public void OnUpdateServerCfg(int peerId, bool inbound, object msg)
        {
            UpdateServerCfg_X2X msg_X2X = msg as UpdateServerCfg_X2X;

            if (msg_X2X.server == root.ServerName || msg_X2X.server == "all")
            {
                //先更新属于自己需要更新的
                UpdateConfig(msg_X2X.updateType);
            }

            //只有第一次前端请求的其他的才由中心服务器转发
            if (inbound)
            {
                SendCenterMsg(msg_X2X);
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
        public void OnRequestCreateHouse(int peerId, bool inbound, object msg)
        {
            if (!main.bOpenBoxAutoPlaying)
            {
                return;
            }
            RequestCreateHouse_B2P reqMsg_B2P = msg as RequestCreateHouse_B2P;
            RequestCreateHouse_P2L reqMsg_P2L = new RequestCreateHouse_P2L();

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            reqMsg_P2L.summonerId = session.summonerId;
            reqMsg_P2L.marketId = main.marketId;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyCreateHouse(ulong summonerId, int houseId)
        {
            ReplyCreateHouse_P2B replyMsg_P2B = new ReplyCreateHouse_P2B();
            replyMsg_P2B.houseId = houseId;
            SendClientMsgBySummonerId(summonerId, replyMsg_P2B, true);
        }
        public void OnRequestJoinHouse(int peerId, bool inbound, object msg)
        {
            if (!main.bOpenBoxAutoPlaying)
            {
                return;
            }
            RequestJoinHouse_B2P reqMsg_B2P = msg as RequestJoinHouse_B2P;
            RequestJoinHouse_P2L reqMsg_P2L = new RequestJoinHouse_P2L();

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            reqMsg_P2L.summonerId = session.summonerId;
            reqMsg_P2L.houseId = reqMsg_B2P.houseId;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyJoinHouse(ulong summonerId, bool bSuccess, int houseId)
        {
            ReplyJoinHouse_P2B replyMsg_P2B = new ReplyJoinHouse_P2B();
            replyMsg_P2B.houseId = houseId;
            replyMsg_P2B.bSuccess = bSuccess;
            SendClientMsgBySummonerId(summonerId, replyMsg_P2B, true);
        }
        public void OnRecvHouseEndSettlement(int peerId, bool inbound, object msg)
        {
            RecvHouseEndSettlement_L2P recvMsg_L2P = msg as RecvHouseEndSettlement_L2P;
            RecvHouseEndSettlement_P2B recvMsg_P2B = new RecvHouseEndSettlement_P2B();
            recvMsg_P2B.houseId = recvMsg_L2P.houseId;
            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2B, true);
        }
    }
}

