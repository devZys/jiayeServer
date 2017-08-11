using System;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using NLog;
using System.Diagnostics;
using FluentNHibernate.Testing.Values;
using System.Collections.Generic;

namespace LegendServerLogic.Record
{
    public class RecordMain : Module
    {
        public RecordMsgProxy msg_proxy;
        private static Logger myLogger = LogManager.GetCurrentClassLogger();
        private bool isOpenDebug = false;
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

            if (stackFrame != null && (type == LogType.Error || type == LogType.Fatal))
            {
                context = context + " [文件名：" + stackFrame.GetFileName() + " 文件行：" + stackFrame.GetFileLineNumber() + " 文件列：" + stackFrame.GetFileColumnNumber() + "]";
            }

            msg_proxy.NotifyRecordLog(type, context, isSaveToDB);
        }
        public void RecordLog(LogType type, Exception ex, StackFrame stackFrame, bool isSaveToDB = false)
        {
            if (type == LogType.Debug && !isOpenDebug) return;

            string log = "ExceptionMessage:" + ex.Message + (ex.InnerException != null ? ("InnerExceptionMessage:" + ex.InnerException.Message) : "");

            if (stackFrame != null && (type == LogType.Error || type == LogType.Fatal))
            {
                log = log + " [文件名：" + stackFrame.GetFileName() + " 文件行：" + stackFrame.GetFileLineNumber() + " 文件列：" + stackFrame.GetFileColumnNumber() + "]";
            }

            msg_proxy.NotifyRecordLog(type, log, isSaveToDB);
        }
    }
}