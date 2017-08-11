using System;
using System.Collections.Generic;
using Photon.SocketServer;
using LegendProtocol;
using LegendServerAC.Core;
using System.Diagnostics;
using LegendServer.Util;

namespace LegendServerAC
{
    public abstract class ServerMsgProxy
    {
        public LegendServerACApplication root;
        public ServerMsgProxy(object root)
        {
            this.root = root as LegendServerACApplication;
        }

        public void SendClientMsg(int peerId, object msg)
        {
            try
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
                        //发送消息时附加性能统计
                        CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => session.peer.SendOperationResponse(response, new SendParameters()), ServerCPU.Instance.ProfilerBound, originalSize);
                    }
                    else
                    {
                        session.peer.SendOperationResponse(response, new SendParameters());
                    }
                }
            }
            catch(Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }
        }
        public void SendDBMsg(object msg)
        {
            SendServerMsg("db", root.DBServerID, false, msg);
        }
        public void SendCenterMsg(object msg, int serverID = 1)
        {
            SendServerMsg("center", serverID, false, msg);
        }
        public void SendRecordMsg(object msg, ActionType actionType, int serverID = 1)
        {
            SendServerMsg("record", serverID, false, msg, (byte)actionType);
        }
        public void SendLogicMsg(object msg, int serverID)
        {
            SendServerMsg("logic", serverID, true, msg);
        }
        public void SendServerMsg(string serverName, int serverID, bool inbound, object msg, byte operationCode = 0)
        {
            try
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
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }
        }
    }

}
