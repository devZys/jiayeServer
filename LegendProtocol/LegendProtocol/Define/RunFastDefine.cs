#if RUNFAST
using System.Collections.Generic;
namespace LegendProtocol
{
    //牌组规则返回
    public class RulesReturn
    {
        public PokerGroupType type;
        public int parameter;
        public RulesReturn() { }
        public RulesReturn(PokerGroupType type, int parameter)
        {
            this.type = type;
            this.parameter = parameter;
        }
    }
    //跑得快房间属性
    public enum RunFastHousePropertyType
    {
        ERFHP_None = 0,                        //空
        ERFHP_SpadeThree = 1,                  //黑桃三
        ERFHP_StrongOff = 1 << 1,              //强关
        ERFHP_SurplusCardCount = 1 << 2,       //显示剩余牌数
    }
    //玩家不屏蔽的牌
    public class UnshieldedCard
    {
        public PokerGroupType type;
        public List<Rank> rankList;
        public List<Rank> rankZhaDanList;
        public UnshieldedCard()
        {
            rankList = new List<Rank>();
            rankZhaDanList = new List<Rank>();
        }
        public UnshieldedCard(PokerGroupType type, List<Rank> rankList, List<Rank> rankZhaDanList)
        {
            this.type = type;
            this.rankList = rankList;
            this.rankZhaDanList = rankZhaDanList;
        }
    }
    //提示牌
    public class PromptCard
    {
        public List<Card> cardList;
        public PromptCard()
        {
            cardList = new List<Card>();
        }
        public PromptCard(List<Card> cardList)
        {
            this.cardList = cardList;
        }
    }
    //常量定义
    public struct RunFastConstValue
    {
        public const int RunFastThreePlayer = 3;   //跑得快3人玩法
        public const int RunFastTwoPlayer = 2;   //跑得快2人玩法
        public const int RunFastThree = 5;        //跑得快标准3带2
        public const int RunFastThreeWing = 2;    //跑得快标准3带2
        public const int RunFastSixteen = 16;      //跑得快十六张牌
        public const int RunFastFifteen = 15;     //跑得快十五张牌
    }
}
#endif
