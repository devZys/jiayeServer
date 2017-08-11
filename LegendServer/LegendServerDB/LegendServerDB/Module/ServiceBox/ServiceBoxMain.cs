using System;
using LegendProtocol;

namespace LegendServerDB.ServiceBox
{
    public class ServiceBoxMain : Module
    {
        public ServiceBoxMsgProxy msg_proxy;
        public ServiceBoxMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new ServiceBoxMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.L2D_RequestTestDB, new MsgComponent(msg_proxy.OnRequestTestDB, typeof(RequestTestDB_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestTestDBCacheSync, new MsgComponent(msg_proxy.OnRequestTestDBCacheSync, typeof(RequestTestDBCacheSync_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestGetDBCacheData, new MsgComponent(msg_proxy.OnRequestGetDBCacheData, typeof(RequestGetDBCacheData_L2D)));
            MsgFactory.Regist(MsgID.C2X_NotifyShowRunningDBCache, new MsgComponent(msg_proxy.OnNotifyShowRunningDBCache, typeof(NotifyShowRunningDBCache_C2X)));
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

        public void ShowRunningCache(string dbInfo, DataOperate operate)
        {
            string dbCacheInstance = "";
            if (dbInfo.Contains("."))
            {
                dbCacheInstance = "缓存实例：" + dbInfo.Substring(dbInfo.LastIndexOf(".") + 1);
            }
            else
            {
                dbCacheInstance = "缓存实例：" + dbInfo;
            }
            switch (operate)
            {
                case DataOperate.Insert:
                    dbCacheInstance += " 正在Insert";
                    break;
                case DataOperate.Update:
                    dbCacheInstance += " 正在Update";
                    break;
                case DataOperate.Delete:
                    dbCacheInstance += " 正在Delete";
                    break;
                default:
                    dbCacheInstance = "";
                    return;
            }
            msg_proxy.SendShowRunningDBCache(dbCacheInstance);
        }
    }

}