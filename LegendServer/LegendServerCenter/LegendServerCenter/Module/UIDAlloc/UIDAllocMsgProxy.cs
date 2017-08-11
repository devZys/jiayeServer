using System;
using LegendProtocol;

namespace LegendServerCenter.UIDAlloc
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

            ReplyUID_X2X replyMsg = new ReplyUID_X2X();
            replyMsg.userId = reqMsg.userId;
            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.uidType = reqMsg.uidType;

            switch (reqMsg.uidType)
            {
                case UIDType.SummoerID:
                    replyMsg.result = ResultCode.OK;
                    replyMsg.uid = main.NewSummonerId();
                    break;
                default:
                    replyMsg.result = ResultCode.Wrong;
                    replyMsg.uid = 0;
                    break;
            }
            SendMsg(peerId, replyMsg);
        }
    }
}

