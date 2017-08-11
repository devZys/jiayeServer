using LegendProtocol;

namespace LegendServerDB.Record
{
    public class RecordMsgProxy : ServerMsgProxy
    {
        private RecordMain main;

        public RecordMsgProxy(RecordMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void NotifyRecordLog(LogType type, string context, bool isSaveToDB)
        {
            NotifyRecordLog_X2R notifyMsg = new NotifyRecordLog_X2R();
            notifyMsg.serverName = root.ServerName;
            notifyMsg.serverID = root.ServerID;
            notifyMsg.logType = type;
            notifyMsg.context = context;
            notifyMsg.isSaveToDB = isSaveToDB;

            SendRecordMsg(notifyMsg, ActionType.GameLog);
        }
        public void NotifyRecordLoginUser(RecordLoginType recordType)
        {
            NotifyRecordLoginUser_X2R notifyMsg = new NotifyRecordLoginUser_X2R();
            notifyMsg.recordType = (int)recordType;

            SendRecordMsg(notifyMsg, ActionType.GameLoginUser);
        }
        public void NotifyRecordRoomCard(ulong summonerId, RecordRoomCardType recordRoomCardType, int roomCard)
        {
            NotifyRecordRoomCard_X2R notifyMsg = new NotifyRecordRoomCard_X2R();
            notifyMsg.summonerId = summonerId;
            notifyMsg.recordType = (int)recordRoomCardType;
            notifyMsg.roomCard = roomCard;

            SendRecordMsg(notifyMsg, ActionType.GameRoomCard);
        }
        public void NotifyRecordBusinessUser(int summonerId, string lastTime, int recordType)
        {
            NotifyRecordBusinessUser_X2R notifyMsg = new NotifyRecordBusinessUser_X2R();
            notifyMsg.businessId = summonerId;
            notifyMsg.lastTime = lastTime;
            notifyMsg.recordType = recordType;

            SendRecordMsg(notifyMsg, ActionType.GameBusinessUser);
        }
    }
}

