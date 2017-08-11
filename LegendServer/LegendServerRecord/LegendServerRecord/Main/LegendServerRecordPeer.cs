using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Photon.SocketServer;
using PhotonHostRuntimeInterfaces;
using ExitGames.Concurrency.Fibers;
using SimpleJSON;
using LegendProtocol;
using LegendServerDB.Core;
using LegendServerDB.SafeCheck;
using LegendServer.Util;
using LegendServer.Database.Config;
using LegendServer.Database;

namespace LegendServerDB
{
    public class LegendServerDBPeer : PeerBase
    {
        public readonly IFiber sendMsgFiber;
        public readonly IFiber recvMsgFiber;
        public LegendServerDBApplication serverRoot;

        public LegendServerDBPeer(IRpcProtocol rpcProtocol, IPhotonPeer nativePeer, LegendServerDBApplication server)
               : base(rpcProtocol, nativePeer)
        {
            this.serverRoot = server;
            this.sendMsgFiber = new PoolFiber();
            this.sendMsgFiber.Start();
            this.recvMsgFiber = new PoolFiber();
            this.recvMsgFiber.Start();

            SessionManager.Instance.AddSession(this.ConnectionId, new Session(this));  
        }
        protected override void OnDisconnect(PhotonHostRuntimeInterfaces.DisconnectReason reasonCode, string reasonDetail)
        {
            serverRoot.MyLogger.Debug("断线代码： " + reasonCode);
            ServerCPU.Instance.PushHighPriorityCommand(() => SessionManager.Instance.OnSessionDisconnect(this.ConnectionId));
        }

        private bool ProcessMsg(OperationRequest operationRequest, SendParameters sendParameters)
        {
            try
            {
                if (operationRequest.Parameters.Count < 2) return false;

                //取得消息ID，如果非法消息过滤掉
                MsgID msgId = (MsgID)operationRequest.Parameters[0];
                if (!MsgFactory.IsValidMsg(msgId)) return false;

                //取得发送消息的召唤师，如果是非法召唤师则直接过滤掉
                Session player = SessionManager.Instance.GetSessionByPeerId(this.ConnectionId);
                if (player == null) return false;
                
                //安全检测模块无法通过则过滤掉
                SafeCheckMain safeCheckSystem = ModuleManager.Get<SafeCheckMain>();
                if (safeCheckSystem.CanProcessMsg(player) == false) return false;                

                //取得原始消息数据，如果空包则过滤掉
                byte[] msgByte = (byte[])operationRequest.Parameters[1];
                if (msgByte.Length <= 0) return false;
                
                //根据消息ID取已注册的消息组件
                MsgComponent msgComponent = MsgFactory.MsgComponents[msgId];

                //构建对应的消息结构
                Msg msg = msgComponent.MsgBuilder();

                //尝试解压缩并反序列化消息（如果配置为不压缩的则不进行压缩或者会针对特定类型的消息才压缩）
                CompressData.tryUncompressMsg(msgByte, msg);

                //处理消息
                if (serverRoot.OpenProfiler)
                {
                    //处理消息时附加性能统计
                    CodeElapseChecker.CountElapse("Msg:" + msg.ToString(), () => msgComponent.MsgHandle(this.ConnectionId, msg), serverRoot.ProfilerBound);
                }
                else
                {
                    //只处理消息不做性能统计
                    msgComponent.MsgHandle(this.ConnectionId, msg);
                }

                //响应至安全检测模块
                safeCheckSystem.OnPlayerProcessMsg(player);
            }
            catch (System.Exception)
            {
                return false;
            }

            return true;
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            switch (operationRequest.OperationCode)
            {
                case (byte)0:
                {
                    //进入中央处理器的逻辑单线程处理
                    ServerCPU.Instance.PushNormalCommand(() => ProcessMsg(operationRequest, sendParameters));
                }
                break;
                default:
                break;
            }
        }
    }
}
