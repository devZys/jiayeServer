using System.Collections.Generic;
using System.Linq;
using Photon.SocketServer;
using LegendProtocol;
using LegendServerProxy.Core;
using System.Collections.Concurrent;
using LegendServer.Util;
using LegendServerProxy.ServiceBox;

namespace LegendServerProxy
{
    public abstract class ServerMsgProxy
    {
        public LegendServerProxyApplication root;
        
        public ServerMsgProxy(object root)
        {
            this.root = root as LegendServerProxyApplication;
        }

        public void SendClientMsg(int peerId, object msg)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);
            if (session != null && session.peer != null)
            {
                MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                OperationResponse response = new OperationResponse(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                if (ServerCPU.Instance.ProfilerBound > 0)
                {
                    //处理消息时附加性能统计
                    CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => session.peer.SendOperationResponse(response, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                }
                else
                {
                    //只处理消息不做性能统计
                    session.peer.SendOperationResponse(response, new SendParameters());
                }
            }
        }
        public void SendClientMsg(string userId, object msg)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByUserId(userId);
            if (session != null && session.peer != null)
            {
                MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                OperationResponse response = new OperationResponse(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                if (ServerCPU.Instance.ProfilerBound > 0)
                {
                    //处理消息时附加性能统计
                    CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => session.peer.SendOperationResponse(response, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                }
                else
                {
                    //只处理消息不做性能统计
                    session.peer.SendOperationResponse(response, new SendParameters());
                }
            }
        }
        public void SendClientMsgBySummonerId(ulong summonerId, object msg, bool bSendBox = false)
        {
            if (!bSendBox && ModuleManager.Get<ServiceBoxMain>().bOpenBoxAutoPlaying)
            {
                //机器人盒子自动出牌模式不发消息
                return;
            }
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionBySummonerId(summonerId);
            if (session != null && session.peer != null)
            {
                MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                OperationResponse response = new OperationResponse(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });

                if (ServerCPU.Instance.ProfilerBound > 0)
                {
                    //发送消息时附加性能统计
                    CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => session.peer.SendOperationResponse(response, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                }
                else
                {
                    session.peer.SendOperationResponse(response, new SendParameters());
                }
            }
        }
        public void BroadCastClientMsg(object msg)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            ConcurrentDictionary<int, InboundClientSession> inboundSessions = SessionManager.Instance.GetInboundClientSessionCollection();
            IEnumerable<InboundClientPeer> result = inboundSessions.Values.Select(element => element.peer).Where(e => e != null);
            if (result.Count() > 0)
            {
                MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                EventData msgData = new EventData((byte)0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } }); 
                if (ServerCPU.Instance.ProfilerBound > 0)
                {
                    //处理消息时附加性能统计
                    CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => root.BroadCastEvent(msgData, result, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                }
                else
                {
                    //只处理消息不做性能统计
                    root.BroadCastEvent(msgData, result, new SendParameters());
                }
            }
        }
        public void SendLogicMsg(object msg, int serverID)
        {
            SendServerMsg("logic", serverID, true, msg);
        }
        public void SendWorldMsg(object msg, int serverID = 1)
        {
            SendServerMsg("world", serverID, true, msg);
        }
        public void SendCenterMsg(object msg, int serverID = 1)
        {
            SendServerMsg("center", serverID, false, msg);
        }
        public void SendRecordMsg(object msg, ActionType actionType, int serverID = 1)
        {
            SendServerMsg("record", serverID, false, msg, (byte)actionType);
        }
        public void SendACMsg(object msg, int serverID)
        {
            SendServerMsg("ac", serverID, true, msg);
        } 
        public void SendServerMsg(string serverName, int serverID, bool inbound, object msg, byte operationCode = 0)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            if (inbound)
            {
                InboundServerSession inboundSession = SessionManager.Instance.GetInboundServerSession(serverName, serverID);
                if (inboundSession != null && inboundSession.peer != null)
                {
                    MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                    OperationResponse response = new OperationResponse(operationCode, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                    if (ServerCPU.Instance.ProfilerBound > 0)
                    {
                        //处理消息时附加性能统计
                        CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => inboundSession.peer.SendOperationResponse(response, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                    }
                    else
                    {
                        //只处理消息不做性能统计
                        inboundSession.peer.SendOperationResponse(response, new SendParameters());
                    }
                }
            }
            else
            {
                OutboundSession outboundSession = SessionManager.Instance.GetOutboundSession(serverName, serverID);
                if (outboundSession != null && outboundSession.peer != null)
                {
                    MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                    OperationRequest request = new OperationRequest(operationCode, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                    if (ServerCPU.Instance.ProfilerBound > 0)
                    {
                        //处理消息时附加性能统计
                        CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => outboundSession.peer.SendOperationRequest(request, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                    }
                    else
                    {
                        //只处理消息不做性能统计
                        outboundSession.peer.SendOperationRequest(request, new SendParameters());
                    }
                }
            }
        }
    }

}
