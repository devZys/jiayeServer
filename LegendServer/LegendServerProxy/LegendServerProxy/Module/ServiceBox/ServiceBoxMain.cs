using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;

namespace LegendServerProxy.ServiceBox
{
    public class ServiceBoxMain : Module
    {
        public ServiceBoxMsgProxy msg_proxy;
        public int marketId = 10000;
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
            MsgFactory.Regist(MsgID.B2P_RequestLoadblanceStatus, new MsgComponent(msg_proxy.OnRequestLoadblanceStatus, typeof(RequestLoadblanceStatus_B2P)));
            MsgFactory.Regist(MsgID.C2P_ReplyLoadblanceStatus, new MsgComponent(msg_proxy.OnReplyLoadblanceStatus, typeof(ReplyLoadblanceStatus_C2P)));
            MsgFactory.Regist(MsgID.B2P_RequestTestCalcLogic, new MsgComponent(msg_proxy.OnRequestTestCalcLogic, typeof(RequestTestCalcLogic_B2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyTestCalcLogic, new MsgComponent(msg_proxy.OnReplyTestCalcLogic, typeof(ReplyTestCalcLogic_L2P)));
            MsgFactory.Regist(MsgID.B2P_RequestTestDB, new MsgComponent(msg_proxy.OnRequestTestDB, typeof(RequestTestDB_B2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyTestDB, new MsgComponent(msg_proxy.OnReplyTestDB, typeof(ReplyTestDB_L2P)));
            MsgFactory.Regist(MsgID.B2P_RequestTestDBCacheSync, new MsgComponent(msg_proxy.OnRequestTestDBCacheSync, typeof(RequestTestDBCacheSync_B2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyTestDBCacheSync, new MsgComponent(msg_proxy.OnReplyTestDBCacheSync, typeof(ReplyTestDBCacheSync_L2P)));
            MsgFactory.Regist(MsgID.B2P_RequestGetDBCacheData, new MsgComponent(msg_proxy.OnRequestGetDBCacheData, typeof(RequestGetDBCacheData_B2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyGetDBCacheData, new MsgComponent(msg_proxy.OnReplyGetDBCacheData, typeof(ReplyGetDBCacheData_L2P)));
            MsgFactory.Regist(MsgID.X2X_UpdateServerCfg, new MsgComponent(msg_proxy.OnUpdateServerCfg, typeof(UpdateServerCfg_X2X)));
            MsgFactory.Regist(MsgID.B2P_RequestCreateHouse, new MsgComponent(msg_proxy.OnRequestCreateHouse, typeof(RequestCreateHouse_B2P)));
            MsgFactory.Regist(MsgID.B2P_RequestJoinHouse, new MsgComponent(msg_proxy.OnRequestJoinHouse, typeof(RequestJoinHouse_B2P)));
            MsgFactory.Regist(MsgID.L2P_RecvHouseEndSettlement, new MsgComponent(msg_proxy.OnRecvHouseEndSettlement, typeof(RecvHouseEndSettlement_L2P)));
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
        public void ReplyCreateHouse(int marketId, ulong summonerId, int houseId)
        {
            if (marketId == this.marketId)
            {
                msg_proxy.OnReplyCreateHouse(summonerId, houseId);
            }
        }
        public void ReplyJoinHouse(ulong summonerId, bool bSuccess, int houseId)
        {
            msg_proxy.OnReplyJoinHouse(summonerId, bSuccess, houseId);
        }
    }

}