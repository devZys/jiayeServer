using System;
using LegendProtocol;
using LegendServerLogicDefine;
using System.Collections.Generic;
#if MAHJONG
using LegendServerLogic.Mahjong;
#elif RUNFAST
using LegendServerLogic.RunFast;
#elif WORDPLATE
using LegendServerLogic.WordPlate;
#endif

namespace LegendServerLogic.UIDAlloc
{
    public class UIDAllocMsgProxy : ServerMsgProxy
    {
        private UIDAllocMain main;
        private Dictionary<ulong, PreRegistRoomInfo> preRegistRoomInfo = new Dictionary<ulong, PreRegistRoomInfo>();

        public UIDAllocMsgProxy(UIDAllocMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void RequestUID(UIDType uidType, ulong summonerId, PreRegistRoomInfo roomInfo)
        {
            if (roomInfo == null) return;

            preRegistRoomInfo[summonerId] = roomInfo;

            RequestUID_X2X reqMsg = new RequestUID_X2X();
            reqMsg.uidType = UIDType.RoomID;
            reqMsg.summonerId = summonerId;
            SendWorldMsg(reqMsg);
        }
        public void OnReplyUID(int peerId, bool inbound, object msg)
        {
            ReplyUID_X2X replyMsg = msg as ReplyUID_X2X;
            switch (replyMsg.uidType)
            {
                case UIDType.RoomID:
                    OnNewRoomID(replyMsg);
                    break;
                default:
                    break;
            }
        }
        private void OnNewRoomID(ReplyUID_X2X replyUIDMsg)
        {
            PreRegistRoomInfo registInfo = null;
            if (preRegistRoomInfo.TryGetValue(replyUIDMsg.summonerId, out registInfo))
            {
                bool bCreate = false;
#if MAHJONG
                PreMahjongRoomInfo mahjongRoomInfo = registInfo as PreMahjongRoomInfo;
                bCreate = ModuleManager.Get<MahjongMain>().msg_proxy.OnCreateMahjongHouse(replyUIDMsg.summonerId, (int)replyUIDMsg.uid, mahjongRoomInfo);
#elif RUNFAST
                PreRunFastRoomInfo runFastRoomInfo = registInfo as PreRunFastRoomInfo;
                bCreate = ModuleManager.Get<RunFastMain>().msg_proxy.OnCreateRunFastHouse(replyUIDMsg.summonerId, (int)replyUIDMsg.uid, runFastRoomInfo);
#elif WORDPLATE
                PreWordPlateRoomInfo wordPlateRoomInfo = registInfo as PreWordPlateRoomInfo;
                bCreate = ModuleManager.Get<WordPlateMain>().msg_proxy.OnCreateWordPlateHouse(replyUIDMsg.summonerId, (int)replyUIDMsg.uid, wordPlateRoomInfo);
#endif
                //具体创建房间
                if (!bCreate)
                {
                    //如果创建失败则通知回收UID
                    NotifyRecycleUID(replyUIDMsg.uidType, replyUIDMsg.uid);
                    return;
                }

                //创建完成
                preRegistRoomInfo.Remove(replyUIDMsg.summonerId);
            }
        }
        public void NotifyRecycleUID(UIDType uidType, ulong uid)
        {
            if (uid <= 0)
            {
                return;
            }
            NotifyRecycleUID_X2X notifyMsg = new NotifyRecycleUID_X2X();
            notifyMsg.uidType = uidType;
            notifyMsg.uid = uid;
            SendWorldMsg(notifyMsg);
        }
    }
}

