using LegendProtocol;
using System;

namespace LegendServerLogic.Entity.Base
{
    public abstract class Player
    {
        public int index;
        //玩家Id
        public string userId;
        //昵称
        public string nickName;
        //
        public ulong summonerId;
        //性别
        public UserSex sex;
        //ip
        public string ip;
        //地址经度
        public float longitude;
        //地址纬度
        public float latitude;
        //在线状态
        public LineType lineType;
        //投票状态
        public VoteStatus voteStatus;
        //总积分
        public int allIntegral;
        //是否托管
        public bool bHosted;
        //映射网关Id
        public int proxyServerId;
        public Player() { }
    }
    public class PlayerInfo
    {
        public ulong summonerId;
        public string nickName;
        public string userId;
        public string ip;
        public UserSex sex;
        public PlayerInfo() { }
        public PlayerInfo(ulong summonerId, string nickName, string userId, string ip, UserSex sex)
        {
            this.summonerId = summonerId;
            this.nickName = nickName;
            this.userId = userId;
            this.ip = ip;
            this.sex = sex;
        }
    }
}
