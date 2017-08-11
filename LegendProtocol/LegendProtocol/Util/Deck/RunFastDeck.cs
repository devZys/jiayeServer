#if RUNFAST
using System.Collections.Generic;
namespace LegendProtocol
{
    public static class MyRunFastDeck
    {
        private static readonly RunFastDeck runFastDeck = new RunFastDeck();

        public static RunFastDeck GetRunFastDeck()
        {
            return runFastDeck;
        }
    }
    public class RunFastDeck : Deck
    {
        public RunFastDeck()
        {
        }
        public void InitRunFastDecks(RunFastType type, List<List<Card>> runFastHandCardList)
        {
            List<Card> runFastCardList = new List<Card>();
            runFastCardList.AddRange(cardList);
            //不要王
            runFastCardList.RemoveAll(element => element.suit == Suit.Joker);
            //不要红桃2，草花2，方片2
            runFastCardList.RemoveAll(element => (element.suit != Suit.Spade && element.rank == Rank.Deuce));
            int count = 0;
            if (type == RunFastType.Sixteen)
            {
                //不要黑桃A
                runFastCardList.RemoveAll(element => (element.suit == Suit.Spade && element.rank == Rank.Ace));
                count = RunFastConstValue.RunFastSixteen;
            }
            else if (type == RunFastType.Fifteen)
            {
                //不要红桃A，草花A，方片A
                runFastCardList.RemoveAll(element => (element.suit != Suit.Spade && element.rank == Rank.Ace));
                //不要黑桃K
                runFastCardList.RemoveAll(element => (element.suit == Suit.Spade && element.rank == Rank.King));
                count = RunFastConstValue.RunFastFifteen;
            }
            else
            {
                return;
            }
            if (runFastCardList.Count % count != 0 || (runFastHandCardList.Count != RunFastConstValue.RunFastTwoPlayer && runFastHandCardList.Count != RunFastConstValue.RunFastThreePlayer))
            {
                return;
            }
            RankCardOne(runFastCardList, count, runFastHandCardList);
        }
        private void RandCardData(List<Card> runFastCardList)
        {
            int count = runFastCardList.Count;
            List<Card> randCardList = GetRandomByCount(runFastCardList, count);
            runFastCardList.Clear();
            runFastCardList.AddRange(randCardList);
        }
        private void RankCardOne(List<Card> runFastCardList, int count, List<List<Card>> runFastHandCardList)
        {
            //随机发牌
            for (int i = 0; i < runFastHandCardList.Count; ++i)
            {
                runFastHandCardList[i].Clear();
                runFastHandCardList[i].AddRange(GetRandomByCount(runFastCardList, count));
            }
        }
        private void RankCardTwo(List<Card> runFastCardList, int count, List<List<Card>> runFastHandCardList)
        {
            //随机发牌
            runFastHandCardList[0].Clear();
            runFastHandCardList[1].Clear();
            if (runFastHandCardList.Count == 3)
            {
                runFastHandCardList[2].Clear();
            }
            for (int i = 0; i < count; ++i)
            {
                runFastHandCardList[0].AddRange(GetRandomByCount(runFastCardList));
                runFastHandCardList[1].AddRange(GetRandomByCount(runFastCardList));
                if (runFastHandCardList.Count == 3)
                {
                    runFastHandCardList[2].AddRange(GetRandomByCount(runFastCardList));
                }
            }
        }
        private void RankCardConst(List<Card> runFastCardList, int count, List<List<Card>> runFastHandCardList)
        {
            //随机发牌
            for (int i = 0; i < runFastHandCardList.Count; ++i)
            {
                runFastHandCardList[i].Clear();
                if (i == 0)
                {
                    runFastHandCardList[i].AddRange(GetOneCardByCount(runFastCardList, count));
                }
                else if (i == 1)
                {
                    runFastHandCardList[i].AddRange(GetTwoCardByCount(runFastCardList, count));
                }
                else
                {
                    runFastHandCardList[i].AddRange(GetThreeCardByCount(runFastCardList, count));
                }
            }
        }
        public List<Card> GetOneCardByCount(List<Card> cardList, int count)
        {
            List<Card> resultList = new List<Card>();
            if (cardList.Count == count)
            {
                resultList.AddRange(cardList);
                cardList.Clear();
            }
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Ten });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Eight });
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Ten });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Eight });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Queen });
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Queen });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Six });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Queen });
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Nine });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Nine });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Jack });
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Six });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Jack });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Seven });
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Seven });
            if (count == 16)
            {
                resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Deuce });
            }
            foreach (Card card in resultList)
            {
                cardList.Remove(cardList.Find(element => (element.suit == card.suit && element.rank == card.rank)));
            }
            //SortCardList(resultList);
            return resultList;
        }
        public List<Card> GetTwoCardByCount(List<Card> cardList, int count)
        {
            List<Card> resultList = new List<Card>();
            if (cardList.Count == count)
            {
                resultList.AddRange(cardList);
                cardList.Clear();
            }
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Jack });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Ten });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.King });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Nine });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Jack });
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Eight });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Queen });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.King });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.King });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Four });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Six });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Seven });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Three });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Five });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Ace });
            if (count == 16)
            {
                resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Ace });
            }
            foreach (Card card in resultList)
            {
                cardList.Remove(cardList.Find(element => (element.suit == card.suit && element.rank == card.rank)));
            }
            //SortCardList(resultList);
            return resultList;
        }
        public List<Card> GetThreeCardByCount(List<Card> cardList, int count)
        {
            List<Card> resultList = new List<Card>();
            if (cardList.Count == count)
            {
                resultList.AddRange(cardList);
                cardList.Clear();
            }
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Three });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Three });
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Three });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Three });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Four });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Four });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Four });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Six });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Six });
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Eight });
            resultList.Add(new Card { suit = Suit.Diamond, rank = Rank.Nine });
            resultList.Add(new Card { suit = Suit.Club, rank = Rank.Nine });
            resultList.Add(new Card { suit = Suit.Heart, rank = Rank.Nine });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Nine });
            resultList.Add(new Card { suit = Suit.Spade, rank = Rank.Queen });
            if (count == 16)
            {
                resultList.Add(new Card { suit = Suit.Heart, rank = Rank.King });
            }
            foreach (Card card in resultList)
            {
                cardList.Remove(cardList.Find(element => (element.suit == card.suit && element.rank == card.rank)));
            }
            //SortCardList(resultList);
            return resultList;
        }
        /// <summary>
        /// 客户端用来判断出牌是否合规矩
        /// </summary>
        /// <param name="headCardList"></param>       玩家当前手牌
        /// <param name="showCardList"></param>       玩家当前选择要出的牌
        /// <param name="currentCardList"></param>    上一把某个玩家出的牌
        /// <param name="pokerGroupType"></param>     上一把某个玩家出牌的类型  
        /// <param name="currentBureau"></param>      当前牌局数  
        /// <param name="bSpadeThree"></param>        第一局是否强制出黑桃3
        /// <param name="bIsMySelf"></param>          上一把出牌的人是不是自己
        /// <returns></returns>
        public override bool IsShowPokersRules(List<Card> headCardList, List<Card> showCardList, List<Card> currentCardList, PokerGroupType pokerGroupType, int currentBureau, bool bSpadeThree, bool bIsMySelf = true)
        {
            if (headCardList == null || showCardList == null || currentCardList == null)
            {
                return false;
            }
            int headCardCount = headCardList.Count;
            int showCardCount = showCardList.Count;
            if (headCardCount == 0 || showCardCount == 0 || showCardCount > headCardCount)
            {
                return false;
            }
            if (!ExistsCardList(headCardList, showCardList))
            {
                return false;
            }
            RulesReturn rulesReturn = IsRules(showCardList);
            if (rulesReturn.type == PokerGroupType.Error || (rulesReturn.type == PokerGroupType.SanZhang && !IsThreePokersRules(rulesReturn.parameter, headCardCount, showCardCount)))
            {
                return false;
            }
            if (currentCardList.Count > 0 && pokerGroupType != PokerGroupType.None)
            {
                //不是第一把或者上一把出牌也不是自己就要判断出牌能不能够大于上次的牌
                if (!bIsMySelf && !CheckCurrentCardByType(currentCardList, pokerGroupType, showCardList, rulesReturn.type))
                {
                    return false;
                }
            }
            else
            {
                //第一局第一把要有黑桃三
                if (currentBureau == 1 && bSpadeThree && !showCardList.Exists(element => (element.suit == Suit.Spade && element.rank == Rank.Three)))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 判断三张出牌是否合规矩
        /// </summary>
        /// <param name="threePokersCount"></param>     选择出牌三张的个数
        /// <param name="headCardCount"></param>        手牌个数
        /// <param name="cardCount"></param>            选择出牌个数
        /// <returns></returns>
        public override bool IsThreePokersRules(int threePokersCount, int headCardCount, int cardCount)
        {
            if (threePokersCount <= 0)
            {
                return false;
            }
            //三个的个数跟出牌的数量合不合理
            int minPokersCount = threePokersCount * PokerConstValue.PokerSanZhang;
            int maxPokersCount = threePokersCount * RunFastConstValue.RunFastThree;
            if (cardCount < minPokersCount || cardCount > maxPokersCount)
            {
                return false;
            }
            //不是最后一手牌，不能不带2个翅膀
            if (headCardCount != cardCount && maxPokersCount != cardCount)
            {
                if (0 == cardCount % RunFastConstValue.RunFastThree)
                {
                    int treeCount = cardCount / RunFastConstValue.RunFastThree;
                    if (threePokersCount >= treeCount)
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
        /// <summary>
        /// 判断选择出牌的类型
        /// </summary>
        /// <param name="cardList"></param>     玩家选择出牌的牌
        /// <returns></returns>                 1.玩家选择出牌的牌的类型    2.玩家三张时候三张的个数
        public override RulesReturn IsRules(List<Card> cardList)
        {
            RulesReturn returnNode = new RulesReturn();
            returnNode.type = PokerGroupType.Error;
            returnNode.parameter = 0;
            //排序
            SortCardList(cardList);
            switch (cardList.Count)
            {
                case 1:
                    returnNode.type = PokerGroupType.DanZhang;
                    break;
                case 2:
                    if (IsSame(cardList, PokerConstValue.PokerDuiZi))
                    {
                        returnNode.type = PokerGroupType.DuiZi;
                    }
                    break;
                case 3:
                    if (IsSame(cardList, PokerConstValue.PokerSanZhang))
                    {
                        returnNode.type = PokerGroupType.SanZhang;
                        returnNode.parameter = 1;
                    }
                    break;
                case 4:
                    if (IsSame(cardList, PokerConstValue.PokerZhaDan))
                    {
                        returnNode.type = PokerGroupType.ZhaDan;
                        returnNode.parameter = 1;
                    }
                    else if (IsLinkPair(cardList))
                    {
                        returnNode.type = PokerGroupType.DuiZi;
                    }
                    else
                    {
                        returnNode = IsThreeLinkPokers(cardList);
                    }
                    break;
                case 5:
                case 7:
                case 9:
                case 11:
                    if (IsStraight(cardList))
                    {
                        returnNode.type = PokerGroupType.ShunZi;
                    }
                    else
                    {
                        returnNode = IsThreeLinkPokers(cardList);
                    }
                    break;
                case 6:
                case 8:
                case 10:
                case 12:
                    if (IsStraight(cardList))
                    {
                        returnNode.type = PokerGroupType.ShunZi;
                    }
                    else if (IsLinkPair(cardList))
                    {
                        returnNode.type = PokerGroupType.DuiZi;
                    }
                    else
                    {
                        returnNode = IsThreeLinkPokers(cardList);
                    }
                    break;
                case 13:
                case 15:
                    returnNode = IsThreeLinkPokers(cardList);
                    break;
                case 14:
                case 16:
                    if (IsLinkPair(cardList))
                    {
                        returnNode.type = PokerGroupType.DuiZi;
                    }
                    else
                    {
                        returnNode = IsThreeLinkPokers(cardList);
                    }
                    break;
                default:
                    break;
            }
            return returnNode;
        }
    }
}
#endif
