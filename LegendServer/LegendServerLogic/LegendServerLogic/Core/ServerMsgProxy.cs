using System;
using System.Collections.Generic;
using Photon.SocketServer;
using LegendProtocol;
using LegendServerLogic.Core;
using System.Diagnostics;
using LegendServer.Util;

namespace LegendServerLogic
{
    public abstract class ServerMsgProxy
    {
        public LegendServerLogicApplication root;
        
        public ServerMsgProxy(object root)
        {
            this.root = root as LegendServerLogicApplication;
        }

        public void SendMsg(int peerId, object msg)
        {
            try
            {
                int originalSize = 0;
                byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
                if (msgByte == null) return;

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
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }           
        }
        public void SendMsg(string serverName, int serverID, object msg, byte operationCode = 0)
        {
            try
            {
                int originalSize = 0;
                byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
                if (msgByte == null) return;

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
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }
        }
        public void SendDBMsg(object msg, int serverID = 0)
        {
            SendMsg("db", serverID > 0 ? serverID : root.DBServerID, msg);
        }
        public void SendCenterMsg(object msg, int serverID = 1)
        {
            SendMsg("center", serverID, msg);
        }
        public void SendWorldMsg(object msg, int serverID = 1)
        {
            SendMsg("world", serverID, msg);
        }
        public void SendProxyMsg(object msg, int serverID)
        {
            SendMsg("proxy", serverID, msg);
        }
        public void SendLogicMsg(object msg, int serverID)
        {
            SendMsg("logic", serverID, msg);
        }
        public void SendRecordMsg(object msg, ActionType actionType, int serverID = 1)
        {
            SendMsg("record", serverID, msg, (byte)actionType);
        }
    }

}
