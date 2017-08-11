using System;
using System.Collections.Generic;
using Photon.SocketServer;
using LegendProtocol;
using LegendServerRecord.Core;
using System.Diagnostics;

namespace LegendServerRecord
{
    public abstract class ServerMsgProxy
    {
        public LegendServerRecordApplication root;
        
        public ServerMsgProxy(object root)
        {
            this.root = root as LegendServerRecordApplication;
        }

        public void SendMsg(int peerId, bool inbound, object msg)
        {
            try
            {
                int originalSize = 0;
                byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
                if (msgByte == null) return;

                if (inbound)
                {
                    InboundSession session = SessionManager.Instance.GetInboundSessionByPeerId(peerId);
                    if (session != null && session.peer != null)
                    {
                        MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                        OperationResponse response = new OperationResponse(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                        session.peer.SendOperationResponse(response, new SendParameters());
                    }
                }
                else
                {
                    OutboundSession session = SessionManager.Instance.GetOutboundSessionByPeerId(peerId);
                    if (session != null && session.peer != null)
                    {
                        MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                        OperationRequest request = new OperationRequest(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                        session.peer.SendOperationRequest(request, new SendParameters());
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }
        }
        public void SendACMsg(object msg, int serverID)
        {
            SendMsg("ac", serverID, true, msg);
        }
        public void SendLogicMsg(object msg, int serverID)
        {
            SendMsg("logic", serverID, true, msg);
        }
        public void SendCenterMsg(object msg, int serverID = 1)
        {
            SendMsg("center", serverID, false, msg);
        }
        public void SendWorldMsg(object msg, int serverID = 1)
        {
            SendMsg("world", serverID, true, msg);
        }
        public void SendMsg(string serverName, int serverID, bool inbound, object msg)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            if (inbound)
            {
                InboundSession inboundSession = SessionManager.Instance.GetInboundSession(serverName, serverID);
                if (inboundSession != null && inboundSession.peer != null)
                {
                    MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                    OperationResponse response = new OperationResponse(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                    inboundSession.peer.SendOperationResponse(response, new SendParameters());
                }
            }
            else
            {
                OutboundSession outboundSession = SessionManager.Instance.GetOutboundSession(serverName, serverID);
                if (outboundSession != null && outboundSession.peer != null)
                {
                    MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                    OperationRequest request = new OperationRequest(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                    outboundSession.peer.SendOperationRequest(request, new SendParameters());
                }
            }
        }
    }

}
