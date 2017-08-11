using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LegendProtocol;
using RabbitMQ.Client;
using System.Diagnostics;
using LegendServer.Database.Summoner;
using LegendServer.Database;
using LegendServerDB.MainCity;

namespace LegendServerDB.Core
{
    public class MQReceiver
    {
        private static MQReceiver instance = null;
        private static object singletonLocker = new object();
        private LegendServerDBApplication main;
        public static MQReceiver Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new MQReceiver();
                        }
                    }
                }
                return instance;
            }
        }
        private MQReceiver()
        {
        }
        ~MQReceiver()
        {
        }
        public void RegistMQ(LegendServerDBApplication main)
        {
            this.main = main;
            MQFactory.Regist(MQID.NotifyChangeBindCode, OnNotifyChangeBindCode);//收到Web后台对邀请码绑定的修改通知
            MQFactory.Regist(MQID.NotifyBroadCastAnnouncement, OnNotifyBroadCastAnnouncement);//收到游戏广播公告（跑马灯）通知
            MQFactory.Regist(MQID.NotifyGameServerSendGoods, OnNotifyGameServerSendGoods);//通知游戏服务器发货
        }

        public void OnRecvMsg(string msgId, byte[] msgBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(msgId) || msgBytes == null || msgBytes.Length <= 0)
                {
                    ServerUtil.RecordLog(LogType.Error, "收到空MQ消息！", new StackTrace(new StackFrame(true)).GetFrame(0));
                    return;
                }
                MQID id = (MQID)Enum.Parse(typeof(MQID), msgId);
                if (!MQFactory.IsValidMQ(id))
                {
                    ServerUtil.RecordLog(LogType.Error, "收到无效MQ消息！", new StackTrace(new StackFrame(true)).GetFrame(0));
                    return;
                }
                //响应至该MQ消息的处理器
                MQFactory.AllMQ[id](id, msgBytes);
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return;
            }
        }
        public void OnNotifyChangeBindCode(MQID id, byte[] msgBytes)
        {
            try
            {
                string msgVal = Encoding.UTF8.GetString(msgBytes);
                string[] msgBody = msgVal.Split('&');
                if (msgBody.Length < 2)
                {
                    ServerUtil.RecordLog(LogType.Error, "收到ID为：" + Enum.GetName(typeof(MQID), id) + "的MQ消息，但内容格式有误！");
                    return;
                }
                ulong summonerId = 0;
                ulong.TryParse(msgBody[0], out summonerId);

                ulong inviteCode = 0;
                ulong.TryParse(msgBody[1], out inviteCode);

                //并入逻辑线程处理
                ServerCPU.Instance.PushCommand(() =>
                {
                    SummonerDB summoner = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(e => e.id == summonerId);
                    if (summoner != null)
                    {
                        summoner.belong = inviteCode;
                        DBManager<SummonerDB>.Instance.UpdateRecordInCache(summoner, e => e.id == summoner.id, true, false);
                    }
                });
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex);
                return;
            }
        }
        private void OnNotifyBroadCastAnnouncement(MQID id, byte[] msgBytes)
        {
            try
            {
                string msgVal = Encoding.UTF8.GetString(msgBytes);
                if (!string.IsNullOrEmpty(msgVal) && msgVal.Length > 0)
                {
                    //并入逻辑线程处理
                    ServerCPU.Instance.PushCommand(() =>
                    {
                        ModuleManager.Get<MainCityMain>().NotifyUpdateAnnouncement(msgVal);
                    });
                }      
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex);
                return;
            }
        }
        private void OnNotifyGameServerSendGoods(MQID id, byte[] msgBytes)
        {
            try
            {
                string msgVal = Encoding.UTF8.GetString(msgBytes);
                string[] msgBody = msgVal.Split('|');
                if (msgBody.Length < 4)
                {
                    ServerUtil.RecordLog(LogType.Error, "收到ID为：" + Enum.GetName(typeof(MQID), id) + "的MQ消息，但内容格式有误！");
                    return;
                }

                GameType gameType = 0;
                Enum.TryParse(msgBody[0], out gameType);
                if (main.Game != gameType)
                {
                    ServerUtil.RecordLog(LogType.Error, "收到ID为：" + Enum.GetName(typeof(MQID), id) + "的MQ消息，但游戏类型却是：" + gameType.ToString() + "！");
                    return;
                }

                ulong payOrderGuid = 0;
                ulong.TryParse(msgBody[1], out payOrderGuid);

                ulong summonerId = 0;
                ulong.TryParse(msgBody[2], out summonerId);

                int addRoomCardNum = 0;
                int.TryParse(msgBody[3], out addRoomCardNum);

                if (summonerId == 0 || addRoomCardNum == 0)
                {
                    ServerUtil.RecordLog(LogType.Error, "收到ID为：" + Enum.GetName(typeof(MQID), id) + "的MQ消息值有误！payOrderGuid = " + payOrderGuid + ", summonerId = " + summonerId + ", addRoomCardNum = " + addRoomCardNum);
                    return;
                }

                //这里往其他游戏服务器转发该玩家获得房卡的信息，最终通知给玩家
                ServerCPU.Instance.PushCommand(() =>
                {
                    SummonerDB summoner = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(e => e.id == summonerId);
                    if (summoner != null)
                    {
                        ModuleManager.Get<MainCityMain>().OnNotifyGameServerSendGoods(summonerId, addRoomCardNum);
                    }
                });

                if (payOrderGuid != 0)
                {
                    //然后再回复支付WEB系统结掉该订单
                    MQSender.Instance.Send(MQID.NotifyPaySystemFinishPayOrder, gameType.ToString() + "|" + payOrderGuid.ToString());
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex);
                return;
            }
        }
    }
}