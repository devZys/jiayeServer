using System.Collections.Generic;
using Photon.SocketServer;
using PhotonHostRuntimeInterfaces;
using Photon.SocketServer.ServerToServer;
using LegendProtocol;
using LegendServerRecord.Core;
using LegendServer.Util;
using System.Diagnostics;
using System.Net;
using LegendServer.LocalConfig;
using LegendServerRecord.Distributed;

namespace LegendServerRecord
{
    //主动建立的连接
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
                ServerCPU.Instance.PushCommand(()=>ModuleManager.Get<DistributedMain>().AddDelayReconnectServer(() => { if (isDisconnected) ConnectTcp(new IPEndPoint(IPAddress.Parse(toIp), toPort), LocalConfigManager.Instance.GetConfig("MySelfServer").value, null); }), ActionType.ServerInternal);
            }
            else
            {
                string toName = link.toName;
                int toId = link.toId;
                string reason = reasonCode.ToString() + ":" + reasonDetail;
                ServerCPU.Instance.PushCommand(() => SessionManager.Instance.RemoveOutboundSession(toName, toId, reason), ActionType.ServerInternal);
            }
        }      

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            //进入中央处理器的服务器内部行为线程中处理
            ServerCPU.Instance.PushCommand(() => ProcessMsg(operationRequest.Parameters, sendParameters), ActionType.ServerInternal);
        }

        protected override void OnOperationResponse(OperationResponse operationResponse, SendParameters sendParameters)
        {
            //进入中央处理器的服务器内部行为线程中处理
            ServerCPU.Instance.PushCommand(() => ProcessMsg(operationResponse.Parameters, sendParameters), ActionType.ServerInternal);
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
                msgComponent.handle(this.ConnectionId, false, msg);
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
            ServerCPU.Instance.PushCommand(() => SessionManager.Instance.AddOutboundSession(this.ConnectionId, new OutboundSession(this, link.toName, link.toId)), ActionType.ServerInternal);
        }

        protected override void OnConnectionFailed(int errorCode, string errorMessage)
        {
            ServerCPU.Instance.PushCommand(() => ConnectTcp(new IPEndPoint(IPAddress.Parse(link.toIp), link.toPort), LocalConfigManager.Instance.GetConfig("MySelfServer").value, null), ActionType.ServerInternal);
        }

        protected override void OnEvent(IEventData eventData, SendParameters sendParameters)
        {
        }
    }   
}
