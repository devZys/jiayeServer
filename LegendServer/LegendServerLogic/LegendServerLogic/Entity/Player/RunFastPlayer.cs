using LegendProtocol;
using LegendServerLogic.Entity.Base;
using System.Collections.Generic;
using System.Linq;

namespace LegendServerLogic.Entity.Players
{
#if RUNFAST
    public class RunFastPlayer : Player
    {
        //玩家房间状态
        public HousePlayerStatus housePlayerStatus;
        //炸弹积分
        public int bombIntegral;
        //胜局数
        public int winBureau;
        //负局数
        public int loseBureau;
        //是否强关
        public bool bStrongOff;
        //身上的牌组
        public List<Card> cardList = new List<Card>();
        public RunFastPlayer()
        {
            index = 0;
            voteStatus = VoteStatus.FreeVote;
            bombIntegral = 0;
            winBureau = 0;
            loseBureau = 0;
            lineType = LineType.OnLine;
            allIntegral = 0;
            sex = UserSex.Shemale;
            bStrongOff = false;
            longitude = 0.0f;
            latitude = 0.0f;
            bHosted = false;
            proxyServerId = 0;
        }
    }
#endif
}
