using System.Collections.Generic;
using Photon.SocketServer;
using PhotonHostRuntimeInterfaces;
using LegendProtocol;
using LegendServerLogic.Core;
using LegendServer.Util;
using Photon.SocketServer.ServerToServer;
using System.Diagnostics;
using System.Net;
using LegendServer.LocalConfig;
using LegendServerLogic.Distributed;

namespace LegendServerLogic
{
    public class OutboundPeer : OutboundS2SPeer
    {
        private ApplicationBase application;
        private PeerLink link;
        public OutboundPeer(ApplicationBase application, PeerLink link)
            : base(application)
        {
            this.application = application;
            this.link = link;
        }
        protected override void OnDisconnect(PhotonHostRuntimeInterfaces.DisconnectReason reasonCode, string reasonDetail)
        {
            if (SessionManager.Instance.GetOutboundSessionByPeerId(this.ConnectionId) == null)
            {
                string toIp = link.toIp;
                int toPort = link.toPort;
                bool isDisconnected = (this.ConnectionState == ConnectionState.Disconnected);
                ServerCPU.Instance.PushCommand(()=>ModuleManager.Get<DistributedMain>().AddDelayReconnectServer(() => { if (isDisconnected) ConnectTcp(new IPEndPoint(IPAddress.Parse(toIp), toPort), LocalConfigManager.Instance.GetConfig("MySelfServer").value, null); }));
            }
            else
            {
                string toName = link.toName;
                int toId = link.toId;
                string reason = reasonCode.ToString() + ":" + reasonDetail;
                ServerCPU.Instance.PushCommand(() => SessionManager.Instance.RemoveOutboundSession(toName, toId, reason));
            }
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            switch (operationRequest.OperationCode)
            {
                case (byte)0:
                    {
                        //进入中央处理器的逻辑单线程处理
                        ServerCPU.Instance.PushCommand(() => ProcessMsg(operationRequest.Parameters, sendParameters));
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void OnOperationResponse(OperationResponse operationResponse, SendParameters sendParameters)
        {
            switch (operationResponse.OperationCode)
            {
                case (byte)0:
                    {
                        //进入中央处理器的逻辑单线程处理
                        ServerCPU.Instance.PushCommand(() => ProcessMsg(operationResponse.Parameters, sendParameters));
                    }
                    break;
                default:
                    break;
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
                OutboundSession session = SessionManager.Instance.GetOutboundSessionByPeerId(this.ConnectionId);
                if (session == null) return false;

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
                    CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => msgComponent.handle(this.ConnectionId, false, msg), ServerCPU.Instance.ProfilerBound, (int)operationParameters[2]);
                }
                else
                {
                    //只处理消息不做性能统计
                    msgComponent.handle(this.ConnectionId, false, msg);
                }
            }
            catch (System.Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return false;
            }

            return true;
        }

        protected override void OnConnectionEstablished(object responseObject)
        {
            ServerCPU.Instance.PushCommand(() => SessionManager.Instance.AddOutboundSession(this.ConnectionId, new OutboundSession(this, link.toName, link.toId)));
        }

        protected override void OnConnectionFailed(int errorCode, string errorMessage)
        {
            ServerCPU.Instance.PushCommand(() => ConnectTcp(new IPEndPoint(IPAddress.Parse(link.toIp), link.toPort), LocalConfigManager.Instance.GetConfig("MySelfServer").value, null));
        }

        protected override void OnEvent(IEventData eventData, SendParameters sendParameters)
        {
        }
    }
}
