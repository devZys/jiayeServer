using LegendProtocol;

namespace LegendServerProxy.Record
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
    }
}

