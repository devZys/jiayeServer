using System;
using LegendProtocol;
using LegendServerWorld.Core;
using LegendServerWorldDefine;

namespace LegendServerWorld.UIDAlloc
{
    public class UIDAllocMsgProxy : ServerMsgProxy
    {
        private UIDAllocMain main;

        public UIDAllocMsgProxy(UIDAllocMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestUID(int peerId, bool inbound, object msg)
        {
            RequestUID_X2X reqMsg = msg as RequestUID_X2X;

            int serverId = 0;
            if (inbound)
            {
                InboundSession inboundSession = SessionManager.Instance.GetInboundSessionByPeerId(peerId);
                serverId = inboundSession.serverID;
            }
            else
            {
                OutboundSession outboundSession = SessionManager.Instance.GetOutboundSessionByPeerId(peerId);
                serverId = outboundSession.serverID;
            }

            ReplyUID_X2X replyMsg = new ReplyUID_X2X();
            replyMsg.userId = reqMsg.userId;
            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.uidType = reqMsg.uidType;

            switch (reqMsg.uidType)
            {
                case UIDType.RoomID:
                    replyMsg.result = ResultCode.OK;
                    replyMsg.uid = main.GetHouseCardId(serverId);
                    break;
                default:
                    replyMsg.result = ResultCode.Wrong;
                    replyMsg.uid = 0;
                    break;
            }
            SendMsg(peerId, inbound, replyMsg);
        }
        public void OnRequestHouseBelong(int peerId, bool inbound, object msg)
        {
            RequestHouseBelong_L2W reqMsg = msg as RequestHouseBelong_L2W;

            Summoner sender = SummonerManager.Instance.GetSummoner(reqMsg.summonerId);
            if (sender == null) return;

            ReplyHouseBelong_W2L replyMsg = new ReplyHouseBelong_W2L();
            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.houseId = reqMsg.houseId;
            replyMsg.logicId = main.HouseIdToLogicId.ContainsKey(reqMsg.houseId) ? main.HouseIdToLogicId[reqMsg.houseId] : 0;
            SendMsg(peerId, inbound, replyMsg);

            if (replyMsg.logicId > 0)
            {
                //变更新的逻辑服，因为旧逻辑服即将迁移该Summoner
                sender.logicId = replyMsg.logicId;
            }
        }

        public void OnNotifyRecycleUID(int peerId, bool inbound, object msg)
        {
            NotifyRecycleUID_X2X notifyMsg = msg as NotifyRecycleUID_X2X;

            switch (notifyMsg.uidType)
            {
                case UIDType.RoomID:
                    //回收房间ID
                    main.RevertHouseCardId((int)notifyMsg.uid);
                    break;
                default:
                    break;
            }
        }
    }
}

