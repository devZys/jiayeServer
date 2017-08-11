#if MAHJONG
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;

namespace LegendServerProxy.Mahjong
{
    public class MahjongMain : Module
    {
        public MahjongMsgProxy msg_proxy;
        public MahjongMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new MahjongMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.T2P_RequestCreateMahjongHouse, new MsgComponent(msg_proxy.OnReqCreateMahjongHouse, typeof(RequestCreateMahjongHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyCreateMahjongHouse, new MsgComponent(msg_proxy.OnReplyCreateMahjongHouse, typeof(ReplyCreateMahjongHouse_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestJoinMahjongHouse, new MsgComponent(msg_proxy.OnReqJoinMahjongHouse, typeof(RequestJoinMahjongHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyJoinMahjongHouse, new MsgComponent(msg_proxy.OnReplyJoinMahjongHouse, typeof(ReplyJoinMahjongHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvJoinMahjongHouse, new MsgComponent(msg_proxy.OnRecvJoinMahjongHouse, typeof(RecvJoinMahjongHouse_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestQuitMahjongHouse, new MsgComponent(msg_proxy.OnReqQuitMahjongHouse, typeof(RequestQuitMahjongHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyQuitMahjongHouse, new MsgComponent(msg_proxy.OnReplyQuitMahjongHouse, typeof(ReplyQuitMahjongHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvQuitMahjongHouse, new MsgComponent(msg_proxy.OnRecvQuitMahjongHouse, typeof(RecvQuitMahjongHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvLeaveMahjongHouse, new MsgComponent(msg_proxy.OnRecvLeaveMahjongHouse, typeof(RecvLeaveMahjongHouse_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMahjongHouseVote, new MsgComponent(msg_proxy.OnReqMahjongHouseVote, typeof(RequestMahjongHouseVote_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMahjongHouseVote, new MsgComponent(msg_proxy.OnReplyMahjongHouseVote, typeof(ReplyMahjongHouseVote_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvMahjongHouseVote, new MsgComponent(msg_proxy.OnRecvMahjongHouseVote, typeof(RecvMahjongHouseVote_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestReadyMahjongHouse, new MsgComponent(msg_proxy.OnReqReadyMahjongHouse, typeof(RequestReadyMahjongHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyReadyMahjongHouse, new MsgComponent(msg_proxy.OnReplyReadyMahjongHouse, typeof(ReplyReadyMahjongHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvReadyMahjongHouse, new MsgComponent(msg_proxy.OnRecvReadyMahjongHouse, typeof(RecvReadyMahjongHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvBeginMahjong, new MsgComponent(msg_proxy.OnRecvBeginMahjong, typeof(RecvBeginMahjong_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMahjongPendulum, new MsgComponent(msg_proxy.OnReqMahjongPendulum, typeof(RequestMahjongPendulum_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMahjongPendulum, new MsgComponent(msg_proxy.OnReplyMahjongPendulum, typeof(ReplyMahjongPendulum_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvMahjongPendulum, new MsgComponent(msg_proxy.OnRecvMahjongPendulum, typeof(RecvMahjongPendulum_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMahjongPendulumDice, new MsgComponent(msg_proxy.OnReqMahjongPendulumDice, typeof(RequestMahjongPendulumDice_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMahjongPendulumDice, new MsgComponent(msg_proxy.OnReplyMahjongPendulumDice, typeof(ReplyMahjongPendulumDice_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvMahjongPendulumDice, new MsgComponent(msg_proxy.OnRecvMahjongPendulumDice, typeof(RecvMahjongPendulumDice_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvMahjongEndPendulum, new MsgComponent(msg_proxy.OnRecvMahjongEndPendulum, typeof(RecvMahjongEndPendulum_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMahjongHouseInfo, new MsgComponent(msg_proxy.OnReqMahjongHouseInfo, typeof(RequestMahjongHouseInfo_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMahjongHouseInfo, new MsgComponent(msg_proxy.OnReplyMahjongHouseInfo, typeof(ReplyMahjongHouseInfo_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestShowMahjong, new MsgComponent(msg_proxy.OnReqShowMahjong, typeof(RequestShowMahjong_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyShowMahjong, new MsgComponent(msg_proxy.OnReplyShowMahjong, typeof(ReplyShowMahjong_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvShowMahjong, new MsgComponent(msg_proxy.OnRecvShowMahjong, typeof(RecvShowMahjong_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvGiveOffMahjong, new MsgComponent(msg_proxy.OnRecvGiveOffMahjong, typeof(RecvGiveOffMahjong_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestOperatMahjong, new MsgComponent(msg_proxy.OnReqOperatMahjong, typeof(RequestOperatMahjong_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyOperatMahjong, new MsgComponent(msg_proxy.OnReplyOperatMahjong, typeof(ReplyOperatMahjong_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvOperatMahjong, new MsgComponent(msg_proxy.OnRecvOperatMahjong, typeof(RecvOperatMahjong_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvPlayerWinMahjong, new MsgComponent(msg_proxy.OnRecvPlayerWinMahjong, typeof(RecvPlayerWinMahjong_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvKongMahjong, new MsgComponent(msg_proxy.OnRecvKongMahjong, typeof(RecvKongMahjong_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvSettlementMahjong, new MsgComponent(msg_proxy.OnRecvSettlementMahjong, typeof(RecvSettlementMahjong_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvEndSettlementMahjong, new MsgComponent(msg_proxy.OnRecvEndSettlementMahjong, typeof(RecvEndSettlementMahjong_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvSelectSeabedMahjong, new MsgComponent(msg_proxy.OnRecvSelectSeabedMahjong, typeof(RecvSelectSeabedMahjong_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestPlayerSelectSeabed, new MsgComponent(msg_proxy.OnReqPlayerSelectSeabed, typeof(RequestPlayerSelectSeabed_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyPlayerSelectSeabed, new MsgComponent(msg_proxy.OnReplyPlayerSelectSeabed, typeof(ReplyPlayerSelectSeabed_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvPlayerSelectSeabed, new MsgComponent(msg_proxy.OnRecvPlayerSelectSeabed, typeof(RecvPlayerSelectSeabed_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMahjongMidwayPendulum, new MsgComponent(msg_proxy.OnReqMahjongMidwayPendulum, typeof(RequestMahjongMidwayPendulum_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMahjongMidwayPendulum, new MsgComponent(msg_proxy.OnReplyMahjongMidwayPendulum, typeof(ReplyMahjongMidwayPendulum_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvMahjongMidwayPendulum, new MsgComponent(msg_proxy.OnRecvMahjongMidwayPendulum, typeof(RecvMahjongMidwayPendulum_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMahjongMidwayPendulumDice, new MsgComponent(msg_proxy.OnReqMahjongMidwayPendulumDice, typeof(RequestMahjongMidwayPendulumDice_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMahjongMidwayPendulumDice, new MsgComponent(msg_proxy.OnReplyMahjongMidwayPendulumDice, typeof(ReplyMahjongMidwayPendulumDice_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvMahjongMidwayPendulumDice, new MsgComponent(msg_proxy.OnRecvMahjongMidwayPendulumDice, typeof(RecvMahjongMidwayPendulumDice_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMahjongOverallRecord, new MsgComponent(msg_proxy.OnReqMahjongOverallRecord, typeof(RequestMahjongOverallRecord_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMahjongOverallRecord, new MsgComponent(msg_proxy.OnReplyMahjongOverallRecord, typeof(ReplyMahjongOverallRecord_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMahjongBureauRecord, new MsgComponent(msg_proxy.OnReqMahjongBureauRecord, typeof(RequestMahjongBureauRecord_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMahjongBureauRecord, new MsgComponent(msg_proxy.OnReplyMahjongBureauRecord, typeof(ReplyMahjongBureauRecord_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMahjongBureauPlayback, new MsgComponent(msg_proxy.OnReqMahjongBureauPlayback, typeof(RequestMahjongBureauPlayback_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMahjongBureauPlayback, new MsgComponent(msg_proxy.OnReplyMahjongBureauPlayback, typeof(ReplyMahjongBureauPlayback_L2P)));
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