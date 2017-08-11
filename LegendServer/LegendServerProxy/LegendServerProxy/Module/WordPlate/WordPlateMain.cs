#if WORDPLATE
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;

namespace LegendServerProxy.WordPlate
{
    public class WordPlateMain : Module
    {
        public WordPlateMsgProxy msg_proxy;
        public WordPlateMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new WordPlateMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.T2P_RequestCreateWordPlateHouse, new MsgComponent(msg_proxy.OnReqCreateWordPlateHouse, typeof(RequestCreateWordPlateHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyCreateWordPlateHouse, new MsgComponent(msg_proxy.OnReplyCreateWordPlateHouse, typeof(ReplyCreateWordPlateHouse_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestJoinWordPlateHouse, new MsgComponent(msg_proxy.OnReqJoinWordPlateHouse, typeof(RequestJoinWordPlateHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyJoinWordPlateHouse, new MsgComponent(msg_proxy.OnReplyJoinWordPlateHouse, typeof(ReplyJoinWordPlateHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvJoinWordPlateHouse, new MsgComponent(msg_proxy.OnRecvJoinWordPlateHouse, typeof(RecvJoinWordPlateHouse_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestQuitWordPlateHouse, new MsgComponent(msg_proxy.OnReqQuitWordPlateHouse, typeof(RequestQuitWordPlateHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyQuitWordPlateHouse, new MsgComponent(msg_proxy.OnReplyQuitWordPlateHouse, typeof(ReplyQuitWordPlateHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvQuitWordPlateHouse, new MsgComponent(msg_proxy.OnRecvQuitWordPlateHouse, typeof(RecvQuitWordPlateHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvLeaveWordPlateHouse, new MsgComponent(msg_proxy.OnRecvLeaveWordPlateHouse, typeof(RecvLeaveWordPlateHouse_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestWordPlateHouseVote, new MsgComponent(msg_proxy.OnReqWordPlateHouseVote, typeof(RequestWordPlateHouseVote_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyWordPlateHouseVote, new MsgComponent(msg_proxy.OnReplyWordPlateHouseVote, typeof(ReplyWordPlateHouseVote_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvWordPlateHouseVote, new MsgComponent(msg_proxy.OnRecvWordPlateHouseVote, typeof(RecvWordPlateHouseVote_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestReadyWordPlateHouse, new MsgComponent(msg_proxy.OnReqReadyWordPlateHouse, typeof(RequestReadyWordPlateHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyReadyWordPlateHouse, new MsgComponent(msg_proxy.OnReplyReadyWordPlateHouse, typeof(ReplyReadyWordPlateHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvReadyWordPlateHouse, new MsgComponent(msg_proxy.OnRecvReadyWordPlateHouse, typeof(RecvReadyWordPlateHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvBeginWordPlate, new MsgComponent(msg_proxy.OnRecvBeginWordPlate, typeof(RecvBeginWordPlate_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestWordPlateHouseInfo, new MsgComponent(msg_proxy.OnReqWordPlateHouseInfo, typeof(RequestWordPlateHouseInfo_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyWordPlateHouseInfo, new MsgComponent(msg_proxy.OnReplyWordPlateHouseInfo, typeof(ReplyWordPlateHouseInfo_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestShowWordPlate, new MsgComponent(msg_proxy.OnReqShowWordPlate, typeof(RequestShowWordPlate_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyShowWordPlate, new MsgComponent(msg_proxy.OnReplyShowWordPlate, typeof(ReplyShowWordPlate_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvShowWordPlate, new MsgComponent(msg_proxy.OnRecvShowWordPlate, typeof(RecvShowWordPlate_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestOperatWordPlate, new MsgComponent(msg_proxy.OnReqOperatWordPlate, typeof(RequestOperatWordPlate_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyOperatWordPlate, new MsgComponent(msg_proxy.OnReplyOperatWordPlate, typeof(ReplyOperatWordPlate_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvOperatWordPlate, new MsgComponent(msg_proxy.OnRecvOperatWordPlate, typeof(RecvOperatWordPlate_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvPlayerWinWordPlate, new MsgComponent(msg_proxy.OnRecvPlayerWinWordPlate, typeof(RecvPlayerWinWordPlate_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvSettlementWordPlate, new MsgComponent(msg_proxy.OnRecvSettlementWordPlate, typeof(RecvSettlementWordPlate_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvEndSettlementWordPlate, new MsgComponent(msg_proxy.OnRecvEndSettlementWordPlate, typeof(RecvEndSettlementWordPlate_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvPlayerPassChowWordPlate, new MsgComponent(msg_proxy.OnRecvPlayerPassChowWordPlate, typeof(RecvPlayerPassChowWordPlate_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestWordPlateOverallRecord, new MsgComponent(msg_proxy.OnReqWordPlateOverallRecord, typeof(RequestWordPlateOverallRecord_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyWordPlateOverallRecord, new MsgComponent(msg_proxy.OnReplyWordPlateOverallRecord, typeof(ReplyWordPlateOverallRecord_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestWordPlateBureauRecord, new MsgComponent(msg_proxy.OnReqWordPlateBureauRecord, typeof(RequestWordPlateBureauRecord_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyWordPlateBureauRecord, new MsgComponent(msg_proxy.OnReplyWordPlateBureauRecord, typeof(ReplyWordPlateBureauRecord_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestWordPlateBureauPlayback, new MsgComponent(msg_proxy.OnReqWordPlateBureauPlayback, typeof(RequestWordPlateBureauPlayback_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyWordPlateBureauPlayback, new MsgComponent(msg_proxy.OnReplyWordPlateBureauPlayback, typeof(ReplyWordPlateBureauPlayback_L2P)));
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
    }
}
#endif