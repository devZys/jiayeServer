using LegendProtocol;

namespace LegendServerLogicDefine
{
    public enum CompetitionStatus
    {
        ECS_None,   //空
        ECS_Apply,  //申请状态
        ECS_Begin,  //开始比赛
        ECS_Game,   //正在比赛
        ECS_Wait,   //正在结算
        ECS_End,    //结束比赛
    }
    public class ComPlayerIntegral
    {
        public ulong summonerId;
        public int integral;
        public ComPlayerIntegral() { }
        public ComPlayerIntegral(ulong summonerId, int integral)
        {
            this.summonerId = summonerId;
            this.integral = integral;
        }
    }
    public class PreRegistRoomInfo
    {
        public int maxBureau;
        public int businessId;
        public PreRegistRoomInfo() { }
    }
#if MAHJONG
    public class PreMahjongRoomInfo : PreRegistRoomInfo
    {
        public MahjongType mahjongType;
        public int maxPlayerNum;
        public int housePropertyType;
        public int catchBird;
        public int flutter;
        public PreMahjongRoomInfo(int maxPlayerNum, int maxBureau, MahjongType mahjongType, int housePropertyType, int catchBird, int flutter, int businessId)
        {
            this.maxPlayerNum = maxPlayerNum;
            this.maxBureau = maxBureau;
            this.mahjongType = mahjongType;
            this.housePropertyType = housePropertyType;
            this.catchBird = catchBird;
            this.flutter = flutter;
            this.businessId = businessId;
        }
    }
#elif RUNFAST
    public class PreRunFastRoomInfo : PreRegistRoomInfo
    {
        public RunFastType runFastType;
        public int maxPlayerNum;
        public int housePropertyType;
        public PreRunFastRoomInfo(int maxPlayerNum, int maxBureau, RunFastType runFastType, int housePropertyType, int businessId)
        {
            this.maxPlayerNum = maxPlayerNum;
            this.maxBureau = maxBureau;
            this.runFastType = runFastType;
            this.housePropertyType = housePropertyType;
            this.businessId = businessId;
        }
    }
#elif WORDPLATE
    public class PreWordPlateRoomInfo : PreRegistRoomInfo
    {
        public WordPlateType wordPlateType;
        public int maxPlayerNum;
        public int housePropertyType;
        public int maxWinScore;
        public int baseWinScore;
        public PreWordPlateRoomInfo(int maxPlayerNum, int maxBureau, WordPlateType wordPlateType, int housePropertyType, int maxWinScore, int baseWinScore, int businessId)
        {
            this.maxPlayerNum = maxPlayerNum;
            this.maxBureau = maxBureau;
            this.wordPlateType = wordPlateType;
            this.housePropertyType = housePropertyType;
            this.maxWinScore = maxWinScore;
            this.baseWinScore = baseWinScore;
            this.businessId = businessId;
        }
    }
#endif
}
