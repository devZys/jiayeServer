using System;
using LegendProtocol;
using LegendServer.Database.RoomCard;
using LegendServer.Database;
using System.Collections.Generic;

namespace LegendServerLogic.Actor.Summoner
{
    public class Summoner : ObjectBase
    {
        public ulong id;
        public string userId;
        public string nickName;
        public int proxyServerId;
        public int acServerId;
        public DateTime loginTime;
        public UserAuthority auth;
        public ulong houseId;
        public int competitionKey;
        public int roomCard;
        public UserSex sex;
        public string ip;
        public int allIntegral;
        public bool bFirstLogin;
        public bool bOpenHouse;
        public List<TicketsNode> ticketsList;
        public List<RecordBusinessNode> recordBusinessList;
        public DateTime dailySharingTime;
        public DateTime lastSendChatMsgTime;
        public DateTime lastSendFeedbackMsgTime;
        public bool bTicketsInfoFlag;
        public ulong belong;
        public SummonerStatus status;
        public override void Init(params object[] paramList)
        {
            if (paramList.Length < 19) return;

            this.id = (ulong)paramList[0];
            this.userId = (string)paramList[1];
            this.nickName = (string)paramList[2];
            this.auth = (UserAuthority)paramList[3];
            this.houseId = (ulong)paramList[4];
            this.loginTime = (DateTime)paramList[5];
            this.sex = (UserSex)paramList[6];
            this.belong = (ulong)paramList[7];
            this.ip = (string)paramList[8];
            this.allIntegral = (int)paramList[9];
            this.bFirstLogin = (bool)paramList[10];
            this.dailySharingTime = DateTime.Now;
            DateTime.TryParse((string)paramList[11], out this.dailySharingTime);
            this.bOpenHouse = (bool)paramList[12];
            this.recordBusinessList = (List<RecordBusinessNode>)paramList[13];
            this.ticketsList = (List<TicketsNode>)paramList[14];
            this.competitionKey = (int)paramList[15];
            this.proxyServerId = (int)paramList[16];
            this.acServerId = (int)paramList[17];
            this.roomCard = (int)paramList[18];

            lastSendChatMsgTime = new DateTime(DateTime.MinValue.Ticks);
            lastSendFeedbackMsgTime = new DateTime(DateTime.MinValue.Ticks);
            bTicketsInfoFlag = false;
        }
        public void AddAllIntegral(int addIntegral)
        {
            if (addIntegral == 0 || (allIntegral == int.MaxValue && addIntegral > 0) || (allIntegral == int.MinValue && addIntegral < 0))
            {
                return;
            }
            int sumIntegral = allIntegral + addIntegral;
            if (allIntegral > 0 && sumIntegral <= allIntegral && sumIntegral < addIntegral)
            {
                //正数加爆了
                ServerUtil.RecordLog(LogType.Error, "AddAllIntegral 正数加爆了!! allIntegral = " + allIntegral + ", addIntegral = " + addIntegral);
                allIntegral = int.MaxValue;
            }
            else if (allIntegral < 0 && sumIntegral >= allIntegral && sumIntegral > addIntegral)
            {
                //负数加爆了
                ServerUtil.RecordLog(LogType.Error, "AddAllIntegral 负数加爆了!! allIntegral = " + allIntegral + ", addIntegral = " + addIntegral);
                allIntegral = int.MinValue;
            }
            else
            {
                allIntegral += addIntegral;
            }
        }
        public void AddTicketsNode(TicketsNode ticketsNode)
        {
            if (ticketsNode != null && !ticketsList.Contains(ticketsNode))
            {
                ticketsList.Add(ticketsNode);
            }
        }
        public void DelTicketsNode(ulong ticketsOnlyId)
        {
            ticketsList.RemoveAll(element => element.id == ticketsOnlyId);
        }
    }
}
