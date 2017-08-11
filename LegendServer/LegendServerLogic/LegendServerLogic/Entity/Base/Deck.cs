#if RUNFAST
using LegendProtocol;
using LegendServer.Util;
using System;
using System.Collections.Generic;

namespace LegendServerLogic.Entity.Base
{
    public abstract class Deck
    {
        static private Dictionary<PokerGroupType, Func<List<Card>, List<Rank>>> dataHandles = new Dictionary<PokerGroupType, Func<List<Card>, List<Rank>>>();
        //牌组
        static public List<Card> cardList = new List<Card>();
        public Deck()
        {
            InitDecks();
            RegistDataHandle(PokerGroupType.ZhaDan, GetZhaDanCard);
            RegistDataHandle(PokerGroupType.SanZhang, GetSanZhangCard);
            RegistDataHandle(PokerGroupType.DuiZi, GetDuiZiCard);
            RegistDataHandle(PokerGroupType.ShunZi, GetShunZiCard);
        }
        //初始化牌组
        public static void InitDecks()
        {
            //牌
            for (int suit = (int)Suit.Spade; suit < (int)Suit.Joker; ++suit)
            {
                for (int rank = (int)Rank.Three; rank < (int)Rank.LittleJoker; ++rank)
                {
                    cardList.Add(new Card { suit = (Suit)suit, rank = (Rank)rank });
                }
            }
            cardList.Add(new Card { suit = Suit.Joker, rank = Rank.LittleJoker });
            cardList.Add(new Card { suit = Suit.Joker, rank = Rank.BigJoker });
        }
        static private void RegistDataHandle(PokerGroupType pokerType, Func<List<Card>, List<Rank>> handle)
        {
            if (!dataHandles.ContainsKey(pokerType))
            {
                dataHandles[pokerType] = handle;
            }
        }
        static public Func<List<Card>, List<Rank>> GetDataHandle(PokerGroupType pokerType)
        {
            if (dataHandles.ContainsKey(pokerType))
            {
                return dataHandles[pokerType];
            }
            return null;
        }
        //随机获得count张手牌
        public static List<Card> GetRandomByCount(List<Card> cardList, int count)
        {
            List<Card> resultList = new List<Card>();
            if (cardList.Count == count)
            {
                resultList.AddRange(cardList);
                cardList.Clear();
            }
            else {
                for (int i = 0; i < count; ++i)
                {
                    int index = MyRandom.Next(0, cardList.Count);
                    Card card = cardList[index];
                    resultList.Add(card);
                    cardList.Remove(card);
                }
            }
            return resultList;
        }
        public static void SortCardList(List<Card> cardList)
        {
            if(cardList.Count == 0)
            {
                return;
            }
            cardList.Sort((cardOne, cardTwo) => { return (cardOne.rank < cardTwo.rank) ? 1 : ((cardOne.rank > cardTwo.rank) ? -1 : ((cardOne.suit > cardTwo.suit) ? 1 : ((cardOne.suit < cardTwo.suit) ? -1 : 0))); });
        }
        public static bool IsSame(List<Card> cardList, int amount)
        {
            bool IsSame1 = false;
            bool IsSame2 = false;
            for (int i = 0; i < amount - 1; i++) //从大到小比较相邻牌是否相同
            {
                if (cardList[i] == cardList[i + 1])
                {
                    IsSame1 = true;
                }
                else
                {
                    IsSame1 = false;
                    break;
                }
            }
            for (int i = cardList.Count - 1; i > cardList.Count - amount; i--)  //从小到大比较相邻牌是否相同
            {
                if (cardList[i] == cardList[i - 1])
                {
                    IsSame2 = true;
                }
                else
                {
                    IsSame2 = false;
                    break;
                }
            }
            if (IsSame1 || IsSame2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //是否是顺子
        public static bool IsStraight(List<Card> cardList)
        {
            if(cardList.Count < PokerConstValue.PokerShunZi)
            {
                return false;
            }
            //不能包含2、小王、大王
            if (cardList.Exists(element => (element.rank == Rank.Deuce || element.suit == Suit.Joker)))
            {
                return false;
            }
            bool bIsStraight = false;
            for (int i = 0; i < cardList.Count - 1; i++)
            {
                if (cardList[i].rank - 1 == cardList[i + 1].rank)
                {
                    bIsStraight = true;
                }
                else
                {
                    bIsStraight = false;
                    break;
                }
            }
            return bIsStraight;
        }
        //是否是连对
        public static bool IsLinkPair(List<Card> cardList)
        {
            if (cardList.Count % 2 != 0)
            {
                return false;
            }
            //不能包含2、小王、大王
            if (cardList.Exists(element => (element.rank == Rank.Deuce || element.suit == Suit.Joker)))
            {
                return false;
            }
            bool bIsLinkPair = false;
            //首先比较是否都为对子，再比较第一个对子的点数-1是否等于第二个对子，最后检察最小的两个是否为对子（这里的for循环无法检测到最小的两个，所以需要拿出来单独检查）
            for (int i = 0; i < cardList.Count - 2;)  
            {
                if (cardList[i] == cardList[i + 1] && cardList[i].rank - 1 == cardList[i + 2].rank && cardList[i + 2] == cardList[i + 3])
                {
                    bIsLinkPair = true;
                }
                else
                {
                    bIsLinkPair = false;
                    break;
                }
                i += 2;
            }
            return bIsLinkPair;
        } 
        //判断三张牌方法为判断两两相邻的牌,如果两两相邻的牌相同,则count自加1.最后根据count的值判断牌的类型为多少个连续三张
        public static RulesReturn IsThreeLinkPokers(List<Card> cardList)
        {
            RulesReturn returnNode = new RulesReturn();
            returnNode.type = PokerGroupType.SanZhang;
            returnNode.parameter = 0;
            SameThreeSort(cardList); //排序,把飞机放在前面
            for (int i = 2; i < cardList.Count; i++)  //得到牌组中有几个飞机
            {
                if (cardList[i] == cardList[i - 1] && cardList[i] == cardList[i - 2])
                {
                    returnNode.parameter++;
                }
            }
            if (returnNode.parameter > 0)  //当牌组里面有三个时
            { 
                //当牌组为飞机时
                if (returnNode.parameter > 1) 
                {
                    //判断飞机之间的点数是否相差1
                    for (int i = 0; i < returnNode.parameter * 3 - 3; i += 3)
                    {
                        //2点不能当飞机出
                        if (!(cardList[i].rank != Rank.Deuce && cardList[i].rank - 1 == cardList[i + 3].rank)) 
                        {
                            returnNode.type = PokerGroupType.Error;
                            break;
                        }
                    }
                }
            }
            else
            {
                returnNode.type = PokerGroupType.Error;
            }
            return returnNode;

        }
        private static void SameThreeSort(List<Card> cardList)
        {
            Card fourRank = null;  //如果把4张当三张出并且带4张的另外一张,就需要特殊处理,这里记录出现这种情况的牌的点数.
            bool bFindedThree = false;  //已找到三张相同的牌
            List<Rank> tempRankList = new List<Rank>();  //记录三张相同的牌
            int count = 0; //记录在连续三张牌前面的翅膀的张数
            int four = 0; // 记录是否连续出现三三相同,如果出现这种情况则表明出现把4张牌(炸弹)当中的三张和其他牌配成飞机带翅膀,并且翅膀中有炸弹牌的点数.
            // 比如有如下牌组: 998887777666 玩家要出的牌实际上应该为 888777666带997,但是经过从大到小的排序后变成了998887777666 一不美观,二不容易比较.
            //直接从2开始循环,因为cardList[0],cardList[1]的引用已经存储在其他变量中,直接比较即可
            for (int i = 2; i < cardList.Count; ++i)
            {
                // 比较cardList[i]与cardList[i-1],cardList[i]与cardList[i-2]是否同时相等,如果相等则说明这是三张相同牌
                if (cardList[i].rank == cardList[i - 2].rank && cardList[i].rank == cardList[i - 1].rank)
                {
                    //默认的Four为0,所以第一次运行时这里为false,直接执行else
                    //一旦连续出现两个三三相等,就会执行这里的if
                    if (four >= 1)
                    {
                        fourRank = cardList[i]; //当找到四张牌时,记录下4张牌的点数
                        Card changePoker;
                        for (int k = i; k > 0; k--) //把四张牌中的一张移动到最前面.
                        {
                            changePoker = cardList[k];
                            cardList[k] = cardList[k - 1];
                            cardList[k - 1] = changePoker;
                        }
                        count++; //由于此时已经找到三张牌,下面为count赋值的程序不会执行,所以这里要手动+1
                    }
                    else
                    {
                        four++; //记录本次循环,因为本次循环找到了三三相等的牌,如果连续两次找到三三相等的牌则说明找到四张牌(炸弹)
                        tempRankList.Add(cardList[i].rank); //把本次循环的cardList[i]记录下来,即记录下三张牌的点数
                    }
                    bFindedThree = true; //标记已找到三张牌
                }
                else
                {
                    four = 0; //没有找到时,连续找到三张牌的标志Four归零
                    if (!bFindedThree) //只有没有找到三张牌时才让count增加.如果已经找到三张牌,则不再为count赋值.
                    {
                        count = i - 1;
                    }
                }
            } 
            //迭代所有的三张牌点数
            foreach (Rank tempRank in tempRankList)
            {
                Card changePoker;  //临时交换Poker
                //把所有的三张牌往前移动
                for (int i = 0; i < cardList.Count; i++)
                {
                    //当cardList[i]等于三张牌的点数时
                    if (cardList[i].rank == tempRank)
                    {
                        //由于上面已经把4张牌中的一张放到的最前面,这张牌也会与tempPoker相匹配所以这里进行处理
                        // 当第一次遇到四张牌的点数时,把记录四张牌的FourPoker赋值为null,并中断本次循环.由于FourPoker已经为Null,所以下次再次遇到四张牌的点数时会按照正常情况执行.
                        if (cardList[i] == fourRank)
                        {
                            fourRank = null;
                            continue;
                        }
                        changePoker = cardList[i - count];
                        cardList[i - count] = cardList[i];
                        cardList[i] = changePoker;
                    }
                }
            }
        }
        public static List<Rank> GetZhaDanCard(List<Card> cardList)
        {
            if (cardList.Count < PokerConstValue.PokerZhaDan)
            {
                return new List<Rank>();
            }
            List<Rank> rankList = new List<Rank>();
            for (int i = 3; i < cardList.Count; ++i)
            {
                if (cardList[i].rank == cardList[i - 3].rank && cardList[i].rank == cardList[i - 2].rank && cardList[i].rank == cardList[i - 1].rank)
                {
                    if (!rankList.Contains(cardList[i].rank))
                    {
                        rankList.Add(cardList[i].rank);
                    }
                }
            }
            return rankList;
        }
        public static List<Rank> GetSanZhangCard(List<Card> cardList)
        {
            if (cardList.Count < PokerConstValue.PokerSanZhang)
            {
                return new List<Rank>();
            }
            List<Rank> rankList = new List<Rank>();
            for (int i = 2; i < cardList.Count; ++i)
            {
                if (cardList[i].rank == cardList[i - 2].rank && cardList[i].rank == cardList[i - 1].rank)
                {
                    if (!rankList.Contains(cardList[i].rank))
                    {
                        rankList.Add(cardList[i].rank);
                    }
                }
            }
            return rankList;
        }
        public static List<Rank> GetDuiZiCard(List<Card> cardList)
        {
            if (cardList.Count < PokerConstValue.PokerDuiZi)
            {
                return new List<Rank>();
            }
            List<Rank> rankList = new List<Rank>();
            for (int i = 1; i < cardList.Count; ++i)
            {
                if (cardList[i].rank == cardList[i - 1].rank)
                {
                    if (!rankList.Contains(cardList[i].rank))
                    {
                        rankList.Add(cardList[i].rank);
                    }
                }
            }
            return rankList;
        }
        public static List<Rank> GetShunZiCard(List<Card> cardList)
        {
            if (cardList.Count < PokerConstValue.PokerShunZi)
            {
                return new List<Rank>();
            }
            List<Rank> danRankList = new List<Rank>();
            for (int i = 0; i < cardList.Count; ++i)
            {
                if (cardList[i].suit != Suit.Joker && cardList[i].rank != Rank.Deuce && !danRankList.Contains(cardList[i].rank))
                {
                    danRankList.Add(cardList[i].rank);
                }
            }
            if (danRankList.Count < PokerConstValue.PokerShunZi)
            {
                return new List<Rank>();
            }
            List<Rank> rankList = new List<Rank>();
            List<Rank> shunRankList = new List<Rank>();
            shunRankList.Add(danRankList[0]);
            for (int i = 0; i < danRankList.Count - 1; ++i)
            {
                if (danRankList[i] - 1 == danRankList[i + 1] && !shunRankList.Contains(danRankList[i + 1]))
                {
                    shunRankList.Add(danRankList[i + 1]);
                }
                else
                {
                    if (shunRankList.Count >= PokerConstValue.PokerShunZi)
                    {
                        rankList.AddRange(shunRankList);
                    }
                    shunRankList.Clear();
                    shunRankList.Add(danRankList[i + 1]);
                }
            }
            if (shunRankList.Count >= PokerConstValue.PokerShunZi)
            {
                rankList.AddRange(shunRankList);
            }
            return rankList;
        }
    }
}
#endif
