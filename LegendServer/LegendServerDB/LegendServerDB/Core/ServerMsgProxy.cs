using System;
using System.Collections.Generic;
using Photon.SocketServer;
using LegendProtocol;
using LegendServerDB.Core;
using System.Diagnostics;
using LegendServer.Util;

namespace LegendServerDB
{
    public abstract class ServerMsgProxy
    {
        public LegendServerDBApplication root;
        
        public ServerMsgProxy(object root)
        {
            this.root = root as LegendServerDBApplication;
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
                else
                {
                    OutboundSession session = SessionManager.Instance.GetOutboundSessionByPeerId(peerId);
                    if (session != null && session.peer != null)
                    {
                        MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                        OperationRequest request = new OperationRequest(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                        if (ServerCPU.Instance.ProfilerBound > 0)
                        {
                            //发送消息时附加性能统计
                            CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => session.peer.SendOperationRequest(request, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                        }
                        else
                        {
                            session.peer.SendOperationRequest(request, new SendParameters());
                        }
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
        public void SendRecordMsg(object msg, ActionType actionType, int serverID = 1)
        {
            SendMsg("record", serverID, false, msg, (byte)actionType);
        }
        public void SendMsg(string serverName, int serverID, bool inbound, object msg, byte operationCode = 0)
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
                    OperationResponse response = new OperationResponse(operationCode, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                    if (ServerCPU.Instance.ProfilerBound > 0)
                    {
                        //发送消息时附加性能统计
                        CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => inboundSession.peer.SendOperationResponse(response, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                    }
                    else
                    {
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
                        //发送消息时附加性能统计
                        CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => outboundSession.peer.SendOperationRequest(request, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                    }
                    else
                    {
                        outboundSession.peer.SendOperationRequest(request, new SendParameters());
                    }
                }
            }
        }
        public void BroadCastDBMsg(object msg)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);

            foreach (InboundSession session in SessionManager.Instance.GetInboundSessionCollection().Values)
            {
                if (session != null && session.peer != null && session.serverName == "db" && session.serverID != root.ServerID)
                {
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

            foreach (OutboundSession session in SessionManager.Instance.GetOutboundSessionCollection().Values)
            {
                if (session != null && session.peer != null && session.serverName == "db" && session.serverID != root.ServerID)
                {
                    OperationRequest request = new OperationRequest(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
                    if (ServerCPU.Instance.ProfilerBound > 0)
                    {
                        //发送消息时附加性能统计
                        CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => session.peer.SendOperationRequest(request, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                    }
                    else
                    {
                        session.peer.SendOperationRequest(request, new SendParameters());
                    }
                }
            }
        }
        public void BroadCastLogicMsg(object msg)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);

            foreach (InboundSession session in SessionManager.Instance.GetInboundSessionCollection().Values)
            {
                if (session != null && session.peer != null && session.serverName == "logic" && root.AllLogicMappingMe.Exists(e => e == session.serverID))
                {
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
        }
    }

}
