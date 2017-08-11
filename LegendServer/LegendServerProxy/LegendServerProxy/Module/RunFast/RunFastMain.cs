#if RUNFAST
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServerProxy.Core;
using LegendServerProxy.Distributed;
using System;

namespace LegendServerProxy.RunFast
{
    public class RunFastMain : Module
    {
        public RunFastMsgProxy msg_proxy;
        public RunFastMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new RunFastMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.T2P_RequestCreateRunFastHouse, new MsgComponent(msg_proxy.OnReqCreateRunFastHouse, typeof(RequestCreateRunFastHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyCreateRunFastHouse, new MsgComponent(msg_proxy.OnReplyCreateRunFastHouse, typeof(ReplyCreateRunFastHouse_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestJoinRunFastHouse, new MsgComponent(msg_proxy.OnReqJoinRunFastHouse, typeof(RequestJoinRunFastHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyJoinRunFastHouse, new MsgComponent(msg_proxy.OnReplyJoinRunFastHouse, typeof(ReplyJoinRunFastHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvJoinRunFastHouse, new MsgComponent(msg_proxy.OnRecvJoinRunFastHouse, typeof(RecvJoinRunFastHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvBeginRunFast, new MsgComponent(msg_proxy.OnRecvBeginRunFast, typeof(RecvBeginRunFast_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestShowRunFastCard, new MsgComponent(msg_proxy.OnReqShowRunFastCard, typeof(RequestShowRunFastCard_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyShowRunFastCard, new MsgComponent(msg_proxy.OnReplyShowRunFastCard, typeof(ReplyShowRunFastCard_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvShowRunFastCard, new MsgComponent(msg_proxy.OnRecvShowRunFastCard, typeof(RecvShowRunFastCard_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestPassRunFastCard, new MsgComponent(msg_proxy.OnReqPassRunFastCard, typeof(RequestPassRunFastCard_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyPassRunFastCard, new MsgComponent(msg_proxy.OnReplyPassRunFastCard, typeof(ReplyPassRunFastCard_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvPassRunFastCard, new MsgComponent(msg_proxy.OnRecvPassRunFastCard, typeof(RecvPassRunFastCard_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestQuitRunFastHouse, new MsgComponent(msg_proxy.OnReqQuitRunFastHouse, typeof(RequestQuitRunFastHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyQuitRunFastHouse, new MsgComponent(msg_proxy.OnReplyQuitRunFastHouse, typeof(ReplyQuitRunFastHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvQuitRunFastHouse, new MsgComponent(msg_proxy.OnRecvQuitRunFastHouse, typeof(RecvQuitRunFastHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvLeaveRunFastHouse, new MsgComponent(msg_proxy.OnRecvLeaveRunFastHouse, typeof(RecvLeaveRunFastHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvZhaDanIntegral, new MsgComponent(msg_proxy.OnRecvZhaDanIntegral, typeof(RecvZhaDanIntegral_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvSettlementRunFast, new MsgComponent(msg_proxy.OnRecvSettlementRunFast, typeof(RecvSettlementRunFast_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvEndSettlementRunFast, new MsgComponent(msg_proxy.OnRecvEndSettlementRunFast, typeof(RecvEndSettlementRunFast_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestReadyRunFastHouse, new MsgComponent(msg_proxy.OnReqReadyRunFastHouse, typeof(RequestReadyRunFastHouse_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyReadyRunFastHouse, new MsgComponent(msg_proxy.OnReplyReadyRunFastHouse, typeof(ReplyReadyRunFastHouse_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvReadyRunFastHouse, new MsgComponent(msg_proxy.OnRecvReadyRunFastHouse, typeof(RecvReadyRunFastHouse_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestRunFastHouseInfo, new MsgComponent(msg_proxy.OnReqRunFastHouseInfo, typeof(RequestRunFastHouseInfo_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyRunFastHouseInfo, new MsgComponent(msg_proxy.OnReplyRunFastHouseInfo, typeof(ReplyRunFastHouseInfo_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestDissolveHouseVote, new MsgComponent(msg_proxy.OnReqDissolveHouseVote, typeof(RequestDissolveHouseVote_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyDissolveHouseVote, new MsgComponent(msg_proxy.OnReplyDissolveHouseVote, typeof(ReplyDissolveHouseVote_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvDissolveHouseVote, new MsgComponent(msg_proxy.OnRecvDissolveHouseVote, typeof(RecvDissolveHouseVote_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestRunFastOverallRecord, new MsgComponent(msg_proxy.OnReqRunFastOverallRecord, typeof(RequestRunFastOverallRecord_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyRunFastOverallRecord, new MsgComponent(msg_proxy.OnReplyRunFastOverallRecord, typeof(ReplyRunFastOverallRecord_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestRunFastBureauRecord, new MsgComponent(msg_proxy.OnReqRunFastBureauRecord, typeof(RequestRunFastBureauRecord_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyRunFastBureauRecord, new MsgComponent(msg_proxy.OnReplyRunFastBureauRecord, typeof(ReplyRunFastBureauRecord_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestRunFastBureauPlayback, new MsgComponent(msg_proxy.OnReqRunFastBureauPlayback, typeof(RequestRunFastBureauPlayback_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyRunFastBureauPlayback, new MsgComponent(msg_proxy.OnReplyRunFastBureauPlayback, typeof(ReplyRunFastBureauPlayback_L2P)));
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