using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LegendProtocol
{
    public delegate void MsgHandle(int peerId, bool inbound, object msg);
    public delegate void MQHandle(MQID id, byte[] msgBody);
    [Serializable]
    public class MsgComponent
    {
        public MsgComponent(MsgHandle handle, Type type)
        {
            this.type = type;
            this.handle = handle;
        }
        public Type type;
        public MsgHandle handle;
    }

    public class MsgFactory
    {
        static public Dictionary<MsgID, MsgComponent> allMsg = new Dictionary<MsgID, MsgComponent>();
        static public void Regist(MsgID msgId, MsgComponent msgComponent)
        {
            if (!allMsg.ContainsKey(msgId))
            {
                allMsg.Add(msgId, msgComponent);
            }
        }
        static public bool IsValidMsg(MsgID msgId)
        {
            return allMsg.ContainsKey(msgId);
        }
    }
    public class MQFactory
    {
        static public Dictionary<MQID, MQHandle> AllMQ = new Dictionary<MQID, MQHandle>();
        static public void Regist(MQID mqId, MQHandle mqHandle)
        {
            if (!AllMQ.ContainsKey(mqId))
            {
                AllMQ.Add(mqId, mqHandle);
            }
        }
        static public bool IsValidMQ(MQID mqId)
        {
            return AllMQ.ContainsKey(mqId);
        }
    }
}
