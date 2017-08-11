using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LegendProtocol;
using RabbitMQ.Client;
using System.Diagnostics;

namespace LegendServerDB.Core
{
    public class MQSender
    {
        private MQHostInfo hostInfo;
        private static MQSender instance = null;
        private static object singletonLocker = new object();
        public static MQSender Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new MQSender();
                        }
                    }
                }
                return instance;
            }
        }
        private MQSender()
        {
        }
        ~MQSender()
        {
        }
        public void Init(MQHostInfo hostInfo)
        {
            this.hostInfo = hostInfo;
        }
        public bool Send(MQID id, string context, int serverId = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(context)) return false;

                var factory = new ConnectionFactory();
                factory.HostName = hostInfo.Name;
                factory.UserName = hostInfo.User;
                factory.Password = hostInfo.Password;

                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.ExchangeDeclare(exchange: hostInfo.Game.ToString() + Enum.GetName(typeof(MQID), id), type: "fanout", durable: true, autoDelete: false, arguments: null);

                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;
                        properties.MessageId = serverId.ToString();
                                                
                        channel.BasicPublish(exchange: hostInfo.Game.ToString() + Enum.GetName(typeof(MQID), id), routingKey: Enum.GetName(typeof(MQID), id), basicProperties: properties, body: Encoding.UTF8.GetBytes(context));
                    }
                }
                return true;
            }                
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return false;
            }
        }
        public void Send(MQID id, byte[] context, int serverId = 0)
        {
            try
            {
                if (context == null || context.Length <= 0) return;

                var factory = new ConnectionFactory();
                factory.HostName = hostInfo.Name;
                factory.UserName = hostInfo.User;
                factory.Password = hostInfo.Password;

                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.ExchangeDeclare(exchange: hostInfo.Game.ToString() + Enum.GetName(typeof(MQID), id), type: "fanout", durable: true, autoDelete: false, arguments: null);

                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;
                        properties.MessageId = serverId.ToString();
                        channel.BasicPublish(exchange: hostInfo.Game.ToString() + Enum.GetName(typeof(MQID), id), routingKey: Enum.GetName(typeof(MQID), id), basicProperties: properties, body: context);
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