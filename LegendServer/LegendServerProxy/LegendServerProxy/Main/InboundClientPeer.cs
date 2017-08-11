using System;
using System.Collections.Generic;
using Photon.SocketServer;
using PhotonHostRuntimeInterfaces;
using LegendProtocol;
using LegendServerProxy.Core;
using LegendServer.Util;
using LegendServerProxy.SafeCheck;
using System.Diagnostics;

namespace LegendServerProxy
{
    //被动建立的连接（客户端玩家）
    public class InboundClientPeer : ClientPeer
    {
        public InboundClientPeer(InitRequest initRequest)
               : base(initRequest)
        {
            SessionManager.Instance.AddInboundClientSession(this.ConnectionId, ObjectPoolManager<InboundClientSession>.Instance.NewObject(this, initRequest.RemoteIP, initRequest.ApplicationId));
        }
        protected override void OnDisconnect(PhotonHostRuntimeInterfaces.DisconnectReason reasonCode, string reasonDetail)
        {
            int peerId = this.ConnectionId;
            ServerCPU.Instance.PushCommand(() => SessionManager.Instance.RemoveInboundClientSession(peerId, true, false)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                );
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            try
            {
                InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(this.ConnectionId);

                //安全检测无法通过则过滤掉
                if (CanProcessMsg(session) == false) return;

                switch (operationRequest.OperationCode)
                {
                    case (byte)0:
                        {
                            //进入中央处理器的逻辑单线程处理
                            ServerCPU.Instance.PushCommand(() => ProcessMsg(operationRequest.Parameters, sendParameters));

                            //响应会话的消息
                            OnClientSessionMsg(session);
                        }
                        break;
                    case (byte)1:
                        {
                            //ping包
                            if (operationRequest.Parameters.Count >= 1)
                            {
                                OperationResponse response = new OperationResponse(1, new Dictionary<byte, object> { { (byte)0, operationRequest.Parameters[0] } });
                                session.peer.SendOperationResponse(response, new SendParameters());
                            }

                            //响应会话的消息
                            OnClientSessionMsg(session);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }
        }

        private bool ProcessMsg(Dictionary<byte, object> operationParameters, SendParameters sendParameters)
        {
            try
            {
                if (operationParameters.Count < 3) return false;

                //取得消息ID，如果非法消息过滤掉
                MsgID msgId = (MsgID)operationParameters[0];
                if (!MsgFactory.IsValidMsg(msgId)) return false;

                //过滤掉非法会话
                InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(this.ConnectionId);
                if (session == null || session.status == SessionStatus.Disconnect) return false;

                //取得原始消息数据，如果空包则过滤掉
                byte[] msgByte = (byte[])operationParameters[1];

                //根据消息ID取已注册的消息组件
                MsgComponent msgComponent = MsgFactory.allMsg[msgId];

                //反序列化出消息
                object msg = Serializer.tryUncompressMsg(msgByte, (int)operationParameters[2] >= Util.needCompressSize, msgComponent.type);                

                //处理消息
                if (ServerCPU.Instance.ProfilerBound > 0)
                {
                    //处理消息时附加性能统计
                    CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => msgComponent.handle(this.ConnectionId, true, msg), ServerCPU.Instance.ProfilerBound, (int)operationParameters[2]);
                }
                else
                {
                    //只处理消息不做性能统计
                    msgComponent.handle(this.ConnectionId, true, msg);
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return false;
            }
            return true;
        }

        public bool CanProcessMsg(InboundClientSession session)
        {
            //掉线了怎么会处理消息呢
            if (session == null || session.status == SessionStatus.Disconnect) return false;

            //恶意攻击被锁定的时间内不处理消息
            if (session.lockedTimeByfrequentAttack > 0) return false;

            //非root系统用户在服务被暂停时不处理消息
            if (session.auth != UserAuthority.Root && SessionManager.Instance.ServicePause) return false;

            return true;
        }
        public void OnClientSessionMsg(InboundClientSession session)
        {
            //间隔时间内统计收到消息的个数
            session.procMsgCountByUnitInterval++;

            DateTime currentDateTime = DateTime.Now;

            if (session.lastProcMsgTimeForUnitInterval.Ticks <= DateTime.MinValue.Ticks)
            {
                //记录间隔时间内上次处理消息的时间
                session.lastProcMsgTimeForUnitInterval = currentDateTime;
            }

            if ((currentDateTime - session.lastProcMsgTimeForUnitInterval).TotalMilliseconds <= ModuleManager.Get<SafeCheckMain>().procMsgAttackCheckTimePeriod)
            {
                //如果间隔时间内超过指定的收到消息的个数限制则认为是恶意攻击，锁定一段时间不处理消息
                if (session.procMsgCountByUnitInterval > ModuleManager.Get<SafeCheckMain>().procMsgCountLimitAttackCheck)
                {
                    ServerUtil.RecordLog(LogType.Error, "玩家IP：" + session.ip + " 3秒内一共收到了：" + session.procMsgCountByUnitInterval + "个消息包");

                    session.lockedTimeByfrequentAttack = ModuleManager.Get<SafeCheckMain>().procMsgAttackCheckLockTime;

                    if (!TimerManager.Instance.Exist(TimerId.FrequentAttackCheck))
                    {
                        TimerManager.Instance.Regist(TimerId.FrequentAttackCheck, 0, 1000, int.MaxValue, ModuleManager.Get<SafeCheckMain>().OnFrequentAttackCheckTimer, SessionManager.Instance.GetInboundClientSessionCollection(), null, null);
                    }

                    session.procMsgCountByUnitInterval = 0;
                    session.lastProcMsgTimeForUnitInterval = new DateTime(DateTime.MinValue.Ticks);

                    session.frequentAttackWarningCount++;

                    ServerUtil.RecordLog(LogType.Error, "检查到玩家：" + session.userId + " IP：" + session.ip + " 恶意攻击，被锁定：" + session.frequentAttackWarningCount.ToString() + "次");

                    if (session.frequentAttackWarningCount >= ModuleManager.Get<SafeCheckMain>().procMsgAttackWarningCntLimit)
                    {
                        ServerUtil.RecordLog(LogType.Error, "检查到玩家：" + session.userId + " IP：" + session.ip + " 恶意攻击超过限定次数,即将被断开连接");

                        session.peer.Disconnect();
                        SessionManager.Instance.RemoveInboundClientSession(session.peer.ConnectionId, true, false);
                    }
                }
            }
            else
            {
                //间隔时间之外不管处理了多少条消息都是合法的
                session.procMsgCountByUnitInterval = 0;
                session.lastProcMsgTimeForUnitInterval = new DateTime(DateTime.MinValue.Ticks);
                session.lockedTimeByfrequentAttack = 0;
            }
        }
    }
}
