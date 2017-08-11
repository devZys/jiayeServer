using LegendProtocol;
using LegendServerLogicDefine;

namespace LegendServerLogic.Entity.Players
{
    public class CompetitionPlayer
    {
        public ulong summonerId;
        public string nickName;
        public string userId;
        public string ip;
        public UserSex sex;
        public int allIntegral;
        public int rank;
        public CompPlayerStatus status;
        public CompetitionPlayer() { }
        public CompetitionPlayer(ulong summonerId, string nickName, string userId, string ip, UserSex sex, int rank)
        {
            this.summonerId = summonerId;
            this.nickName = nickName;
            this.userId = userId;
            this.ip = ip;
            this.sex = sex;
            this.rank = rank;
            this.allIntegral = 0;
            this.status = CompPlayerStatus.ECPS_Apply;
        }
    }
}
