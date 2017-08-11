using System;
using System.Collections.Generic;
using Photon.SocketServer;
using LegendProtocol;
using LegendServerCenter.Core;
using System.Diagnostics;
using LegendServer.Util;

namespace LegendServerCenter
{
    public abstract class ServerMsgProxy
    {
        public LegendServerCenterApplication root;
        public ServerMsgProxy(object root)
        {
            this.root = root as LegendServerCenterApplication;
        }

        public void SendMsg(int peerId, object msg)
        {
            try
            {
                int originalSize = 0;
                byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
                if (msgByte == null) return;                

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
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }
        }
        public void SendProxyMsg(object msg, int serverID)
        {
            SendMsg("proxy", serverID, msg);
        }
        public void SendACMsg(object msg, int serverID)
        {
            SendMsg("ac", serverID, msg);
        }
        public void SendRecordMsg(object msg, ActionType actionType, int serverID = 1)
        {
            SendMsg("record", serverID, msg, (byte)actionType);
        }
        public void SendMsg(string serverName, int serverID, object msg, byte operationCode = 0)
        {
            try
            {
                int originalSize = 0;
                byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
                if (msgByte == null) return;

                InboundSession session = SessionManager.Instance.GetInboundSession(serverName, serverID);
                if (session != null && session.peer != null)
                {
                    MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
                    OperationResponse response = new OperationResponse(operationCode, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });
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
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }     
        }
        public void BroadCastMsg(object msg)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
            OperationResponse response = new OperationResponse(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });

            foreach (InboundSession session in SessionManager.Instance.GetInboundSessionCollection().Values)
            {
                if (session != null && session.peer != null)
                {
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
        //广播消息（排除excludeServer里的服务器）
        public void BroadCastMsgByExclude(object msg, ServerIndex excludeServer)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
            OperationResponse response = new OperationResponse(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });

            foreach (InboundSession session in SessionManager.Instance.GetInboundSessionCollection().Values)
            {
                if (session != null && session.peer != null)
                {
                    if (excludeServer.name != session.serverName)
                    {
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
                    else
                    {
                        if (session.serverID != excludeServer.id)
                        {
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

        //广播消息（排除excludeServer里的服务器，且要求类型是指定类型服务器）
        public void BroadCastMsgByExclude(object msg, string serverName, ServerIndex excludeServer)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
            OperationResponse response = new OperationResponse(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });

            foreach (InboundSession session in SessionManager.Instance.GetInboundSessionCollection().Values)
            {
                if (session != null && session.peer != null && session.serverName == serverName)
                {
                    if (excludeServer.name != session.serverName)
                    {
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
                    else
                    {
                        if (session.serverID != excludeServer.id)
                        {
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
        //广播消息（要求类型是指定类型服务器）
        public void BroadCastMsg(object msg, string serverName)
        {
            int originalSize = 0;
            byte[] msgByte = Serializer.tryCompressMsg(msg, out originalSize);
            if (msgByte == null) return;

            MsgID msgId = (MsgID)msg.GetType().GetProperty("msgId").GetValue(msg);
            OperationResponse response = new OperationResponse(0, new Dictionary<byte, object> { { (byte)0, msgId }, { (byte)1, msgByte }, { (byte)2, originalSize } });

            foreach (InboundSession session in SessionManager.Instance.GetInboundSessionCollection().Values)
            {
                if (session != null && session.peer != null && session.serverName == serverName)
                {
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
