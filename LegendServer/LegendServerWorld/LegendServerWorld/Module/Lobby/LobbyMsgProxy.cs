using System;
using LegendProtocol;
using LegendServerWorldDefine;
using LegendServerWorld.Distributed;
using LegendServerWorld.Core;

namespace LegendServerWorld.Lobby
{
    public class LobbyMsgProxy : ServerMsgProxy
    {
        private LobbyMain main;

        public LobbyMsgProxy(LobbyMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnEnterWorld(int peerId, bool inbound, object msg)
        {
            EnterWorld_L2W reqMsg = msg as EnterWorld_L2W;
            if (string.IsNullOrEmpty(reqMsg.userId) || reqMsg.proxyServerId <= 0 || reqMsg.logicServerId <= 0) return;

            OutboundSession proxySession = SessionManager.Instance.GetOutboundSession("proxy", reqMsg.proxyServerId);
            if (proxySession == null) return;

            Summoner summoner = SummonerManager.Instance.GetSummoner(reqMsg.id);
            if (summoner == null)
            {
                //网关CCU累加
                proxySession.ccu++;

                SummonerManager.Instance.AddSummoner(reqMsg.id, ObjectPoolManager<Summoner>.Instance.NewObject(reqMsg.id, reqMsg.userId, reqMsg.nickName, reqMsg.loginTime, reqMsg.acServerId, reqMsg.proxyServerId, reqMsg.logicServerId));
                ServerUtil.RecordLog(LogType.Debug, "玩家：" + reqMsg.userId + " 进入World！[普通登陆]");
            }
            else
            {
                summoner.userId = reqMsg.userId;
                summoner.nickName = reqMsg.nickName;
                DateTime.TryParse(reqMsg.loginTime, out summoner.loginTime);

                if (summoner.proxyId != reqMsg.proxyServerId)
                {
                    //新网关的CCU累加
                    proxySession.ccu++;
                    OutboundSession oldProxySession = SessionManager.Instance.GetOutboundSession("proxy", summoner.proxyId);
                    if (oldProxySession != null)
                    {
                        //旧网关CCU递减
                        oldProxySession.ccu--;
                    }

                    //不同网关异地登陆发给旧网关踢除他
                    KickLogout_W2P logoutMsg = new KickLogout_W2P();
                    logoutMsg.userId = summoner.userId;
                    logoutMsg.isPlaceOtherLogin = true;
                    logoutMsg.logicServerId = reqMsg.logicServerId;
                    SendProxyMsg(logoutMsg, summoner.proxyId);

                    //更新网关
                    summoner.proxyId = reqMsg.proxyServerId;
                    ServerUtil.RecordLog(LogType.Debug, "玩家：" + reqMsg.userId + " 进入World！[不同网关的异地登陆]");
                }
                else
                {
                    //相同网关的异地登陆
                    if (summoner.logicId == reqMsg.logicServerId)
                    {
                        //相同的逻辑服ID，属于异地登陆时的剔除操作，需要在center中减少上一次逻辑服CCU，因为proxy中相同网关判断两个逻辑服ID一致时不会通知逻辑服减少CCU，相同网关且不同的逻辑服ID，proxy会通知旧逻辑服减少CCU
                        ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus("logic", summoner.logicId, false);
                    }
                    ServerUtil.RecordLog(LogType.Debug, "玩家：" + reqMsg.userId + " 进入World！[相同网关的异地登陆]");
                }
                summoner.logicId = reqMsg.logicServerId;

                //世界服务器在收到异地登陆时不管分配到了相同还是不相同的认证服都要通过该玩家所在的网关通知认证服断开旧连接（同时也会减少旧认证服务器的CCU）
                KickOldACPeer_W2P kickOldACPeerMsg = new KickOldACPeer_W2P();
                kickOldACPeerMsg.userId = summoner.userId;
                kickOldACPeerMsg.oldACServerId = summoner.acId;
                kickOldACPeerMsg.newACServerId = reqMsg.acServerId;
                SendProxyMsg(kickOldACPeerMsg, summoner.proxyId);
                ServerUtil.RecordLog(LogType.Debug, "玩家：" + reqMsg.userId + "异地登陆不管分配到了相同还是不相同的认证服都要通过该玩家所在的网关通知旧的认证服断开旧连接（同时也会减少旧认证服务器的CCU）！");

                if (summoner.acId != reqMsg.acServerId)
                {
                    //更新认证服
                    summoner.acId = reqMsg.acServerId;
                }
            }
        }

        public void OnLeaveWorld(int peerId, bool inbound, object msg)
        {
            LeaveWorld_L2W reqMsg = msg as LeaveWorld_L2W;

            Summoner summoner = SummonerManager.Instance.RemoveSummoner(reqMsg.summonerId);
            if (summoner != null)
            {
                OutboundSession proxySession = SessionManager.Instance.GetOutboundSession("proxy", summoner.proxyId);
                if (proxySession != null)
                {
                    //网关CCU递减
                    proxySession.ccu--;
                }
                ObjectPoolManager<Summoner>.Instance.FreeObject(summoner);

                ServerUtil.RecordLog(LogType.Debug, "玩家：" + summoner.userId + " 离开World！");
            }
        }
        public void OnNotifyGameServerSendGoods(int peerId, bool inbound, object msg)
        {
            NotifyGameServerSendGoods_X2X notifyMsg_D2W = msg as NotifyGameServerSendGoods_X2X;

            Summoner summoner = SummonerManager.Instance.RemoveSummoner(notifyMsg_D2W.summonerId);
            if (summoner != null)
            {
                NotifyGameServerSendGoods_X2X notifyMsg_W2L = new NotifyGameServerSendGoods_X2X();
                notifyMsg_W2L.summonerId = notifyMsg_D2W.summonerId;
                notifyMsg_W2L.addRoomCardNum = notifyMsg_D2W.addRoomCardNum;
                SendLogicMsg(notifyMsg_W2L, summoner.logicId);
            }
        }
    }
}

