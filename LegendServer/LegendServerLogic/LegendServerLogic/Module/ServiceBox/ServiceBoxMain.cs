using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;

namespace LegendServerLogic.ServiceBox
{
    public class ServiceBoxMain : Module
    {
        public ServiceBoxMsgProxy msg_proxy;
        public bool bOpenBoxAutoPlaying = false;
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
            //是否开启盒子自动打牌
            SystemConfigDB systemConfig = DBManager<SystemConfigDB>.Instance.GetSingleRecordInCache(element => element.key == "openBoxAutoPlaying");
            if (systemConfig != null && !string.IsNullOrEmpty(systemConfig.value))
            {
                bool.TryParse(systemConfig.value, out bOpenBoxAutoPlaying);
            }
        }
        public override void OnRegistMsg()
        {            
            MsgFactory.Regist(MsgID.P2L_RequestTestCalcLogic, new MsgComponent(msg_proxy.OnRequestTestCalcLogic, typeof(RequestTestCalcLogic_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestTestDB, new MsgComponent(msg_proxy.OnRequestTestDB, typeof(RequestTestDB_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyTestDB, new MsgComponent(msg_proxy.OnReplyTestDB, typeof(ReplyTestDB_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestTestDBCacheSync, new MsgComponent(msg_proxy.OnRequestTestDBCacheSync, typeof(RequestTestDBCacheSync_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyTestDBCacheSync, new MsgComponent(msg_proxy.OnReplyTestDBCacheSync, typeof(ReplyTestDBCacheSync_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestGetDBCacheData, new MsgComponent(msg_proxy.OnRequestGetDBCacheData, typeof(RequestGetDBCacheData_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyGetDBCacheData, new MsgComponent(msg_proxy.OnReplyGetDBCacheData, typeof(ReplyGetDBCacheData_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestCreateHouse, new MsgComponent(msg_proxy.OnRequestCreateHouse, typeof(RequestCreateHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestJoinHouse, new MsgComponent(msg_proxy.OnRequestJoinHouse, typeof(RequestJoinHouse_P2L)));
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
        public void RecvHouseEndSettlement(ulong summonerId, int proxyServerId, int houseId)
        {
            msg_proxy.OnRecvHouseEndSettlement(summonerId, proxyServerId, houseId);
        }
    }

}