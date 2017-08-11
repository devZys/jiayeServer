using System;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using NLog;
using LegendServer.Database.Log;
using System.Diagnostics;

namespace LegendServerRecord.Record
{
    public class RecordMain : Module
    {
        public RecordMsgProxy msg_proxy;
        private static Logger myLogger = LogManager.GetCurrentClassLogger();
        public bool isOpenDebug = false;
        public RecordMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new RecordMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            //是否开启调试日志
            SystemConfigDB logCfg = DBManager<SystemConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "openDebug");
            if (logCfg != null)
            {
                bool.TryParse(logCfg.value, out isOpenDebug);
            }
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.X2R_NotifyRecordLog, new MsgComponent(msg_proxy.OnNotifyRecordLog, typeof(NotifyRecordLog_X2R)));
            MsgFactory.Regist(MsgID.X2R_NotifyRecordLoginUser, new MsgComponent(msg_proxy.OnNotifyRecordLoginUser, typeof(NotifyRecordLoginUser_X2R)));
            MsgFactory.Regist(MsgID.X2R_NotifyRecordRoomCard, new MsgComponent(msg_proxy.OnNotifyRecordRoomCard, typeof(NotifyRecordRoomCard_X2R)));
            MsgFactory.Regist(MsgID.X2R_NotifyRecordBusinessUser, new MsgComponent(msg_proxy.OnNotifyRecordBusinessUser, typeof(NotifyRecordBusinessUser_X2R)));
        }
        public override void OnRegistTimer()
        {
        }
        public override void OnStart()
        {
        }
        public override void OnDestroy()
        {
        }
        public void RecordLog(LogType type, string context, StackFrame stackFrame, bool isSaveToDB = false)
        {
            if (type == LogType.Debug && !isOpenDebug) return;
            if (string.IsNullOrEmpty(context)) return;

            if (isSaveToDB)
            {
                SaveLogToDB(type, msg_proxy.root.ServerName, msg_proxy.root.ServerID, context, stackFrame);
            }
            else
            {
                ServerUtil.PrintLog(type, "【当前服务器名：" + msg_proxy.root.ServerName + " 服务器ID：" + msg_proxy.root.ServerID + "】===》" + " " + context, stackFrame);
            }
        }
        public void RecordLog(LogType type, Exception ex, StackFrame stackFrame, bool isSaveToDB = false)
        {
            if (type == LogType.Debug && !isOpenDebug) return;

            string log = "ExceptionMessage:" + ex.Message + (ex.InnerException != null ? ("InnerExceptionMessage:" + ex.InnerException.Message) : "");

            if (isSaveToDB)
            {
                SaveLogToDB(type, msg_proxy.root.ServerName, msg_proxy.root.ServerID, log, stackFrame);
            }
            else
            {
                ServerUtil.PrintLog(type, log, stackFrame);
            }
        }
        public void SaveLogToDB(LogType logType, string serverName, int serverID, string context, StackFrame stackFrame = null)
        {
            switch (logType)
            {
                case LogType.Info:
                    LogInfoDB logInfoDB = new LogInfoDB();
                    logInfoDB.logID = MyGuid.NewGuid(ServiceType.Record, (uint)msg_proxy.root.ServerID);
                    logInfoDB.serverName = serverName;
                    logInfoDB.serverID = serverID;
                    logInfoDB.context = context;
                    DBManager<LogInfoDB>.Instance.AddRecordToCache(logInfoDB, element => element.logID == logInfoDB.logID);
                    break;
                case LogType.Debug:
                    LogDebugDB logDebugDB = new LogDebugDB();
                    logDebugDB.logID = MyGuid.NewGuid(ServiceType.Record, (uint)msg_proxy.root.ServerID);
                    logDebugDB.serverName = serverName;
                    logDebugDB.serverID = serverID;
                    logDebugDB.context = context;
                    DBManager<LogDebugDB>.Instance.AddRecordToCache(logDebugDB, element => element.logID == logDebugDB.logID);
                    break;
                case LogType.Error:
                    LogErrorDB logErrorDB = new LogErrorDB();
                    logErrorDB.logID = MyGuid.NewGuid(ServiceType.Record, (uint)msg_proxy.root.ServerID);
                    logErrorDB.serverName = serverName;
                    logErrorDB.serverID = serverID;
                    if (stackFrame != null)
                    {
                        context = context + " [文件名：" + stackFrame.GetFileName() + " 文件行：" + stackFrame.GetFileLineNumber() + " 文件列：" + stackFrame.GetFileColumnNumber() + "]";
                    }
                    logErrorDB.context = context;
                    DBManager<LogErrorDB>.Instance.AddRecordToCache(logErrorDB, element => element.logID == logErrorDB.logID);
                    break;
                case LogType.Fatal:
                    LogFatalDB logFatalDB = new LogFatalDB();
                    logFatalDB.logID = MyGuid.NewGuid(ServiceType.Record, (uint)msg_proxy.root.ServerID);
                    logFatalDB.serverName = serverName;
                    logFatalDB.serverID = serverID;
                    if (stackFrame != null)
                    {
                        context = context + " [文件名：" + stackFrame.GetFileName() + " 文件行：" + stackFrame.GetFileLineNumber() + " 文件列：" + stackFrame.GetFileColumnNumber() + "]";
                    }
                    logFatalDB.context = context;
                    DBManager<LogFatalDB>.Instance.AddRecordToCache(logFatalDB, element => element.logID == logFatalDB.logID);
                    break;
                default:
                    break;
            }
        }
    }
}