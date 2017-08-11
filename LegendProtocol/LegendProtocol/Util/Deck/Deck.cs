#if RUNFAST
using System;
using System.Collections.Generic;
using System.Linq;

namespace LegendProtocol
{
    public abstract class Deck
    {
        private Dictionary<PokerGroupType, Func<List<Card>, List<Card>, bool>> checkDataHandles = new Dictionary<PokerGroupType, Func<List<Card>, List<Card>, bool>>();
        private Dictionary<PokerGroupType, Func<List<Card>, List<Card>, UnshieldedCard>> unshieldedDataHandles = new Dictionary<PokerGroupType, Func<List<Card>, List<Card>, UnshieldedCard>>();
        private Dictionary<PokerGroupType, Action<List<Card>, List<Card>, List<Rank>, List<PromptCard>>> promptDataHandles = new Dictionary<PokerGroupType, Action<List<Card>, List<Card>, List<Rank>, List<PromptCard>>>();
        //牌组
        static public List<Card> cardList = new List<Card>();
        public Deck()
        {
            InitDecks();
            //检查是否要的起
            RegistCheckDataHandle(PokerGroupType.DanZhang, CheckDanZhang);
            RegistCheckDataHandle(PokerGroupType.ZhaDan, CheckZhaDan);
            RegistCheckDataHandle(PokerGroupType.SanZhang, CheckSanZhang);
            RegistCheckDataHandle(PokerGroupType.DuiZi, CheckDuiZi);
            RegistCheckDataHandle(PokerGroupType.ShunZi, CheckShunZi);
            //获取不屏幕的牌
            RegistUnshieldedDataHandle(PokerGroupType.DanZhang, GetUnshieldedDanZhangCard);
            RegistUnshieldedDataHandle(PokerGroupType.ZhaDan, GetUnshieldedZhaDanCard);
            RegistUnshieldedDataHandle(PokerGroupType.SanZhang, GetUnshieldedSanZhangCard);
            RegistUnshieldedDataHandle(PokerGroupType.DuiZi, GetUnshieldedDuiZiCard);
            RegistUnshieldedDataHandle(PokerGroupType.ShunZi, GetUnshieldedShunZiCard);
            //获取提示的牌
            RegistPromptDataHandle(PokerGroupType.DanZhang, GetDanZhangPromptCard);
            RegistPromptDataHandle(PokerGroupType.ZhaDan, GetZhaDanPromptCard);
            RegistPromptDataHandle(PokerGroupType.SanZhang, GetSanZhangPromptCard);
            RegistPromptDataHandle(PokerGroupType.DuiZi, GetDuiZiPromptCard);
            RegistPromptDataHandle(PokerGroupType.ShunZi, GetShunZiPromptCard);
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
        public abstract bool IsShowPokersRules(List<Card> headCardList, List<Card> showCardList, List<Card> currentCardList, PokerGroupType pokerGroupType, int currentBureau, bool bSpadeThree, bool bIsMySelf = true);
        public abstract RulesReturn IsRules(List<Card> cardList);
        public abstract bool IsThreePokersRules(int threePokersCount, int headCardCount, int cardCount);
        private void RegistCheckDataHandle(PokerGroupType pokerType, Func<List<Card>, List<Card>, bool> handle)
        {
            if (!checkDataHandles.ContainsKey(pokerType))
            {
                checkDataHandles[pokerType] = handle;
            }
        }
        public Func<List<Card>, List<Card>, bool> GetCheckDataHandle(PokerGroupType pokerType)
        {
            if (checkDataHandles.ContainsKey(pokerType))
            {
                return checkDataHandles[pokerType];
            }
            return null;
        }
        private void RegistUnshieldedDataHandle(PokerGroupType pokerType, Func<List<Card>, List<Card>, UnshieldedCard> handle)
        {
            if (!unshieldedDataHandles.ContainsKey(pokerType))
            {
                unshieldedDataHandles[pokerType] = handle;
            }
        }
        public Func<List<Card>, List<Card>, UnshieldedCard> GetUnshieldedDataHandle(PokerGroupType pokerType)
        {
            if (unshieldedDataHandles.ContainsKey(pokerType))
            {
                return unshieldedDataHandles[pokerType];
            }
            return null;
        }
        private void RegistPromptDataHandle(PokerGroupType pokerType, Action<List<Card>, List<Card>, List<Rank>, List<PromptCard>> handle)
        {
            if (!promptDataHandles.ContainsKey(pokerType))
            {
                promptDataHandles[pokerType] = handle;
            }
        }
        public Action<List<Card>, List<Card>, List<Rank>, List<PromptCard>> GetPromptDataHandle(PokerGroupType pokerType)
        {
            if (promptDataHandles.ContainsKey(pokerType))
            {
                return promptDataHandles[pokerType];
            }
            return null;
        }
        //随机获得count张手牌
        public static List<Card> GetRandomByCount(List<Card> cardList, int count = 1)
        {
            List<Card> resultList = new List<Card>();
            for (int i = 0; i < count; ++i)
            {
                int index = MyRandom.NextPrecise(0, cardList.Count);
                Card card = cardList[index];
                resultList.Add(new Card { suit = card.suit, rank = card.rank });
                cardList.Remove(card);
            }
            //SortCardList(resultList);
            return resultList;
        }
        public void SortCardList(List<Card> cardList)
        {
            if (cardList.Count == 0)
            {
                return;
            }
            cardList.Sort((cardOne, cardTwo) => { return (cardOne.rank < cardTwo.rank) ? 1 : ((cardOne.rank > cardTwo.rank) ? -1 : ((cardOne.suit > cardTwo.suit) ? 1 : ((cardOne.suit < cardTwo.suit) ? -1 : 0))); });
        }
        public bool ExistsCardList(List<Card> headCardList, List<Card> showCardList)
        {
            List<Card> cardList = new List<Card>();
            cardList.AddRange(headCardList);
            foreach (Card card in showCardList)
            {
                Card result = cardList.Find(element => (element.suit == card.suit && element.rank == card.rank));
                if (result != null)
                {
                    cardList.Remove(result);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        public static bool IsSame(List<Card> cardList, int amount)
        {
            bool IsSame1 = false;
            bool IsSame2 = false;
            for (int i = 0; i < amount - 1; i++) //从大到小比较相邻牌是否相同
            {
                if (cardList[i].rank == cardList[i + 1].rank)
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
                if (cardList[i].rank == cardList[i - 1].rank)
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
            if (cardList.Count < PokerConstValue.PokerShunZi)
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
                if (cardList[i].rank == cardList[i + 1].rank && cardList[i].rank - 1 == cardList[i + 2].rank && cardList[i + 2].rank == cardList[i + 3].rank)
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
            SameThreeSort(cardList); //排序,把最长最大的飞机放前面
            List<Rank> sanZhangRankList = GetAllSanZhangCard(cardList);
            if (sanZhangRankList != null && sanZhangRankList.Count > 0)
            {
                returnNode.parameter = 1;
                for (int i = 1; i < sanZhangRankList.Count; ++i)
                {
                    if (sanZhangRankList[i] + 1 == sanZhangRankList[i - 1])
                    {
                        returnNode.parameter += 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (returnNode.parameter == 0)  //当牌组里面有三个时
            {
                returnNode.type = PokerGroupType.Error;
            }
            return returnNode;

        }
        private static void SameThreeSort(List<Card> cardList)
        {
            List<Rank> sanZhangRankList = GetMaxSanZhangCard(cardList);
            //把最长最大的飞机放前面
            List<Card> promptList = new List<Card>();
            for (int i = 0; i < sanZhangRankList.Count; ++i)
            {
                List<Card> threeCardList = cardList.FindAll(element => element.rank == sanZhangRankList[i]);
                if (threeCardList.Count >= PokerConstValue.PokerSanZhang)
                {
                    if (threeCardList.Count > PokerConstValue.PokerSanZhang)
                    {
                        threeCardList = threeCardList.Take(PokerConstValue.PokerSanZhang).ToList();
                    }
                    promptList.AddRange(threeCardList);
                    foreach (Card card in threeCardList)
                    {
                        cardList.Remove(cardList.Find(element => (element.suit == card.suit && element.rank == card.rank)));
                    }
                }
            }
            if (promptList.Count > 0)
            {
                if (cardList.Count > 0)
                {
                    promptList.AddRange(cardList);
                    cardList.Clear();
                }
                cardList.AddRange(promptList);
            }
        }
        private static List<Rank> GetZhaDanCard(List<Card> cardList)
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
        private static List<Rank> GetMaxSanZhangCard(List<Card> cardList)
        {
            if (cardList.Count < PokerConstValue.PokerSanZhang)
            {
                return new List<Rank>();
            }
            List<Rank> sanZhangRankList = GetAllSanZhangCard(cardList);
            if (sanZhangRankList.Count > 1)
            {
                List<Rank> rankList = new List<Rank>();
                List<Rank> sanZhangList = new List<Rank>();
                sanZhangList.Add(sanZhangRankList[0]);
                for (int i = 0; i < sanZhangRankList.Count - 1; ++i)
                {
                    if (sanZhangRankList[i] - 1 == sanZhangRankList[i + 1] && !sanZhangList.Contains(sanZhangRankList[i + 1]))
                    {
                        sanZhangList.Add(sanZhangRankList[i + 1]);
                    }
                    else
                    {
                        if (sanZhangList.Count > rankList.Count)
                        {
                            rankList.Clear();
                            rankList.AddRange(sanZhangList);
                        }
                        sanZhangList.Clear();
                        sanZhangList.Add(sanZhangRankList[i + 1]);
                    }
                }
                if (sanZhangList.Count > rankList.Count)
                {
                    rankList.Clear();
                    rankList.AddRange(sanZhangList);
                }
                return rankList;
            }
            return sanZhangRankList;
        }
        private static List<Rank> GetAllSanZhangCard(List<Card> cardList)
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
        private static List<Rank> GetSanZhangCard(List<Card> cardList)
        {
            if (cardList.Count < PokerConstValue.PokerSanZhang)
            {
                return new List<Rank>();
            }
            List<Rank> sanzhangList = GetMaxSanZhangCard(cardList);
            if (0 == cardList.Count % RunFastConstValue.RunFastThree)
            {
                int treeCount = cardList.Count / RunFastConstValue.RunFastThree;
                if (sanzhangList.Count > treeCount)
                {
                    return sanzhangList.Take(treeCount).ToList();
                }
            }
            return sanzhangList;
        }
        private static List<Rank> GetDuiZiCard(List<Card> cardList)
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
        private static List<Rank> GetShunZiCard(List<Card> cardList)
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
                        break;
                    }
                    shunRankList.Clear();
                    shunRankList.Add(danRankList[i + 1]);
                }
            }
            if (rankList.Count == 0 && shunRankList.Count >= PokerConstValue.PokerShunZi)
            {
                rankList.AddRange(shunRankList);
            }
            return rankList;
        }
        private static bool CheckRankCard(List<Rank> nextRankList, List<Rank> rankList)
        {
            if (rankList.Count == 1)
            {
                if (nextRankList.Exists(element => element > rankList[0]))
                {
                    return true;
                }
            }
            else
            {
                List<Rank> shunRankList = new List<Rank>();
                shunRankList.Add(nextRankList[0]);
                for (int i = 0; i < nextRankList.Count - 1; ++i)
                {
                    if (nextRankList[i] - 1 == nextRankList[i + 1] && !shunRankList.Contains(nextRankList[i + 1]))
                    {
                        shunRankList.Add(nextRankList[i + 1]);
                    }
                    else
                    {
                        if (shunRankList.Count >= rankList.Count)
                        {
                            //已经排序过，第一个就是最大的
                            if (shunRankList[0] > rankList[0])
                            {
                                return true;
                            }
                        }
                        shunRankList.Clear();
                        shunRankList.Add(nextRankList[i + 1]);
                    }
                }
                if (shunRankList.Count >= rankList.Count)
                {
                    if (shunRankList[0] > rankList[0])
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private static bool CheckDanZhang(List<Card> nextCardList, List<Card> cardList)
        {
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count != 1)
            {
                return false;
            }
            //炸弹肯定要的起
            List<Rank> zhaDanList = GetZhaDanCard(nextCardList);
            if (zhaDanList.Count > 0)
            {
                return true;
            }
            //牌都不够
            if (nextCardList.Count < cardList.Count)
            {
                return false;
            }
            if (!nextCardList.Exists(element => element.rank > cardList[0].rank))
            {
                return false;
            }
            return true;
        }
        private static bool CheckDuiZi(List<Card> nextCardList, List<Card> cardList)
        {
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count % PokerConstValue.PokerDuiZi != 0)
            {
                return false;
            }
            //炸弹肯定要的起
            List<Rank> zhaDanList = GetZhaDanCard(nextCardList);
            if (zhaDanList.Count > 0)
            {
                return true;
            }
            //牌都不够
            if (nextCardList.Count < cardList.Count)
            {
                return false;
            }
            List<Rank> nextDuiZiList = GetDuiZiCard(nextCardList);
            List<Rank> duiZiList = GetDuiZiCard(cardList);
            if (nextDuiZiList.Count > 0 && duiZiList.Count > 0 && nextDuiZiList.Count >= duiZiList.Count)
            {
                return CheckRankCard(nextDuiZiList, duiZiList);
            }
            return false;
        }
        private static bool CheckSanZhang(List<Card> nextCardList, List<Card> cardList)
        {
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count == 0)
            {
                return false;
            }
            //炸弹肯定要的起
            List<Rank> zhaDanList = GetZhaDanCard(nextCardList);
            if (zhaDanList.Count > 0)
            {
                return true;
            }
            //牌都不够
            //if (nextCardList.Count < cardList.Count)
            //{
            //    return false;
            //}
            List<Rank> nextSanZhangList = GetAllSanZhangCard(nextCardList);
            List<Rank> sanZhangList = GetSanZhangCard(cardList);
            if (nextSanZhangList.Count > 0 && sanZhangList.Count > 0 && nextSanZhangList.Count >= sanZhangList.Count)
            {
                return CheckRankCard(nextSanZhangList, sanZhangList);
            }
            return false;
        }
        private static bool CheckShunZi(List<Card> nextCardList, List<Card> cardList)
        {
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count < PokerConstValue.PokerShunZi)
            {
                return false;
            }
            //炸弹肯定要的起
            List<Rank> zhaDanList = GetZhaDanCard(nextCardList);
            if (zhaDanList.Count > 0)
            {
                return true;
            }
            //牌都不够
            if (nextCardList.Count < cardList.Count)
            {
                return false;
            }
            //最大的是Ace,不用打了（已经排序过，第一个就是最大的）
            Rank rank = cardList[0].rank;
            if (rank == Rank.Ace)
            {
                return false;
            }
            List<Rank> nextShunZiList = GetShunZiCard(nextCardList);
            if (nextShunZiList.Count > 0 && nextShunZiList.Count >= cardList.Count && nextShunZiList[0] > rank)
            {
                return true;
            }
            return false;
        }
        private static bool CheckZhaDan(List<Card> nextCardList, List<Card> cardList)
        {
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count % PokerConstValue.PokerZhaDan != 0 || cardList.Count > nextCardList.Count)
            {
                return false;
            }
            List<Rank> nextZhaDanList = GetZhaDanCard(nextCardList);
            List<Rank> zhaDanList = GetZhaDanCard(cardList);
            if (nextZhaDanList.Count > 0 && zhaDanList.Count > 0 && nextZhaDanList.Count >= zhaDanList.Count)
            {
                return CheckRankCard(nextZhaDanList, zhaDanList);
            }
            return false;
        }
        public bool CheckShowCardByType(List<Card> nextCardList, List<Card> cardList, PokerGroupType type)
        {
            Func<List<Card>, List<Card>, bool> handle = GetCheckDataHandle(type);
            if (handle != null)
            {
                return handle(nextCardList, cardList);
            }
            return false;
        }
        public bool CheckCurrentCardByType(List<Card> currentCardList, PokerGroupType currentType, List<Card> cardList, PokerGroupType type)
        {
            if (currentType == type)
            {
                if (currentCardList.Count != cardList.Count)
                {
                    if (currentType != PokerGroupType.SanZhang || currentCardList.Count < cardList.Count)
                    {
                        return false;
                    }
                    List<Rank> sanZhangList = GetSanZhangCard(cardList);
                    List<Rank> currentSanZhangList = GetSanZhangCard(currentCardList);
                    if (sanZhangList.Count != currentSanZhangList.Count && sanZhangList.Count < currentSanZhangList.Count)
                    {
                        return false;
                    }
                }
                if (cardList[0].rank > currentCardList[0].rank)
                {
                    return true;
                }
            }
            else
            {
                if (type == PokerGroupType.ZhaDan)
                {
                    return true;
                }
            }
            return false;
        }
        private static UnshieldedCard GetUnshieldedDanZhangCard(List<Card> nextCardList, List<Card> cardList)
        {
            UnshieldedCard unshieldedCard = new UnshieldedCard();
            unshieldedCard.type = PokerGroupType.DanZhang;
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count != 1)
            {
                return unshieldedCard;
            }
            //炸弹肯定不屏蔽
            List<Rank> zhaDanList = GetZhaDanCard(nextCardList);
            if (zhaDanList.Count > 0)
            {
                unshieldedCard.rankZhaDanList.AddRange(zhaDanList);
            }
            //牌都不够
            if (nextCardList.Count < cardList.Count)
            {
                return unshieldedCard;
            }
            List<Rank> danZhangList = new List<Rank>();
            for (int i = 0; i < nextCardList.Count; ++i)
            {
                if (!danZhangList.Contains(nextCardList[i].rank))
                {
                    danZhangList.Add(nextCardList[i].rank);
                }
            }
            List<Rank> reusltList = danZhangList.FindAll(element => element > cardList[0].rank);
            if (reusltList.Count > 0)
            {
                unshieldedCard.rankList.AddRange(reusltList);
            }
            return unshieldedCard;
        }
        private static UnshieldedCard GetUnshieldedDuiZiCard(List<Card> nextCardList, List<Card> cardList)
        {
            UnshieldedCard unshieldedCard = new UnshieldedCard();
            unshieldedCard.type = PokerGroupType.DuiZi;
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count % PokerConstValue.PokerDuiZi != 0)
            {
                return unshieldedCard;
            }
            //炸弹肯定不屏蔽
            List<Rank> zhaDanList = GetZhaDanCard(nextCardList);
            if (zhaDanList.Count > 0)
            {
                unshieldedCard.rankZhaDanList.AddRange(zhaDanList);
            }
            //牌都不够
            if (nextCardList.Count < cardList.Count)
            {
                return unshieldedCard;
            }
            List<Rank> reusltList = GetUnshieldedCard(GetDuiZiCard(nextCardList), GetDuiZiCard(cardList));
            if (reusltList.Count > 0)
            {
                unshieldedCard.rankList.AddRange(reusltList);
            }
            return unshieldedCard;
        }
        private static UnshieldedCard GetUnshieldedSanZhangCard(List<Card> nextCardList, List<Card> cardList)
        {
            UnshieldedCard unshieldedCard = new UnshieldedCard();
            unshieldedCard.type = PokerGroupType.SanZhang;
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count == 0)
            {
                return unshieldedCard;
            }
            //炸弹肯定不屏蔽
            List<Rank> zhaDanList = GetZhaDanCard(nextCardList);
            if (zhaDanList.Count > 0)
            {
                unshieldedCard.rankZhaDanList.AddRange(zhaDanList);
            }
            //牌都不够
            if (nextCardList.Count < cardList.Count)
            {
                return unshieldedCard;
            }
            List<Rank> reusltList = GetUnshieldedCard(GetAllSanZhangCard(nextCardList), GetSanZhangCard(cardList));
            if (reusltList.Count > 0)
            {
                unshieldedCard.rankList.AddRange(reusltList);
            }
            return unshieldedCard;
        }
        private static UnshieldedCard GetUnshieldedZhaDanCard(List<Card> nextCardList, List<Card> cardList)
        {
            UnshieldedCard unshieldedCard = new UnshieldedCard();
            unshieldedCard.type = PokerGroupType.ZhaDan;
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count % PokerConstValue.PokerZhaDan != 0 || cardList.Count > nextCardList.Count)
            {
                return unshieldedCard;
            }
            List<Rank> reusltList = GetUnshieldedCard(GetZhaDanCard(nextCardList), GetZhaDanCard(cardList));
            if (reusltList.Count > 0)
            {
                unshieldedCard.rankZhaDanList.AddRange(reusltList);
            }
            return unshieldedCard;
        }
        private static UnshieldedCard GetUnshieldedShunZiCard(List<Card> nextCardList, List<Card> cardList)
        {
            UnshieldedCard unshieldedCard = new UnshieldedCard();
            unshieldedCard.type = PokerGroupType.ShunZi;
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count < PokerConstValue.PokerShunZi)
            {
                return unshieldedCard;
            }
            //炸弹肯定不屏蔽
            List<Rank> zhaDanList = GetZhaDanCard(nextCardList);
            if (zhaDanList.Count > 0)
            {
                unshieldedCard.rankZhaDanList.AddRange(zhaDanList);
            }
            //牌都不够
            if (nextCardList.Count < cardList.Count)
            {
                return unshieldedCard;
            }
            //最大的是Ace,不用打了（已经排序过，第一个就是最大的）
            Rank rank = cardList[0].rank;
            if (rank == Rank.Ace)
            {
                return unshieldedCard;
            }
            List<Rank> nextShunZiList = GetShunZiCard(nextCardList.FindAll(element => element.rank > cardList[cardList.Count - 1].rank), cardList.Count);
            if (nextShunZiList == null || nextShunZiList.Count == 0 || cardList.Count > nextShunZiList.Count)
            {
                return unshieldedCard;
            }
            unshieldedCard.rankList.AddRange(nextShunZiList);
            return unshieldedCard;
        }
        private static List<Rank> GetUnshieldedCard(List<Rank> nextCardList, List<Rank> cardList)
        {
            List<Rank> resultRankList = new List<Rank>();
            if (nextCardList == null || cardList == null || nextCardList.Count == 0 || cardList.Count == 0 || cardList.Count > nextCardList.Count)
            {
                return resultRankList;
            }
            //取出比最小值大的rank 因为已经排序过，所有最后一个就是最小的rank
            List<Rank> nextRankList = nextCardList.FindAll(element => element > cardList[cardList.Count - 1]);
            if (nextRankList == null || nextRankList.Count == 0 || cardList.Count > nextCardList.Count)
            {
                return resultRankList;
            }
            if (cardList.Count == 1)
            {
                return nextRankList;
            }
            List<Rank> shunRankList = new List<Rank>();
            shunRankList.Add(nextRankList[0]);
            for (int i = 0; i < nextRankList.Count - 1; ++i)
            {
                if (nextRankList[i] - 1 == nextRankList[i + 1] && !shunRankList.Contains(nextRankList[i + 1]))
                {
                    shunRankList.Add(nextRankList[i + 1]);
                }
                else
                {
                    if (shunRankList.Count >= cardList.Count)
                    {
                        resultRankList.AddRange(shunRankList);
                    }
                    shunRankList.Clear();
                    shunRankList.Add(nextRankList[i + 1]);
                }
            }
            if (shunRankList.Count >= cardList.Count)
            {
                resultRankList.AddRange(shunRankList);
            }
            return resultRankList;
        }
        private static List<Rank> GetShunZiCard(List<Card> cardList, int showCardCount)
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
                    if (shunRankList.Count >= showCardCount)
                    {
                        rankList.AddRange(shunRankList);
                    }
                    shunRankList.Clear();
                    shunRankList.Add(danRankList[i + 1]);
                }
            }
            if (shunRankList.Count >= showCardCount)
            {
                rankList.AddRange(shunRankList);
            }
            return rankList;
        }
        /// <summary>
        /// 获取当前玩家不屏蔽的牌
        /// </summary>
        /// <param name="nextCardList"></param>     当前玩家的手牌
        /// <param name="cardList"></param>         上次出的牌
        /// <param name="type"></param>             上次出的牌的类型
        /// <returns></returns>
        public UnshieldedCard GetUnshieldedCardByType(List<Card> nextCardList, List<Card> cardList, PokerGroupType type)
        {
            Func<List<Card>, List<Card>, UnshieldedCard> handle = GetUnshieldedDataHandle(type);
            if (handle != null)
            {
                return handle(nextCardList, cardList);
            }
            return new UnshieldedCard();
        }
        private static void GetZhaDanPromptCard(List<Card> headCardList, List<Card> cardList, List<Rank> zhaDanRankList, List<PromptCard> promptCardList)
        {
            if (headCardList == null || cardList == null || zhaDanRankList == null || promptCardList == null)
            {
                return;
            }
            if (headCardList.Count <= PokerConstValue.PokerZhaDan || zhaDanRankList.Count == 0)
            {
                return;
            }
            foreach (Rank rank in zhaDanRankList)
            {
                List<Card> zhaDanCardList = headCardList.FindAll(elmenet => elmenet.rank == rank);
                if (zhaDanCardList.Count == PokerConstValue.PokerZhaDan)
                {
                    promptCardList.Add(new PromptCard(zhaDanCardList));
                }
            }
        }
        private static void GetDanZhangPromptCard(List<Card> headCardList, List<Card> cardList, List<Rank> danZhangRankList, List<PromptCard> promptCardList)
        {
            if (headCardList == null || cardList == null || danZhangRankList == null || promptCardList == null)
            {
                return;
            }
            if (headCardList.Count <= PokerConstValue.PokerDanZhang || cardList.Count != PokerConstValue.PokerDanZhang || danZhangRankList.Count == 0)
            {
                return;
            }
            foreach (Rank rank in danZhangRankList)
            {
                List<Card> danZhangCardList = headCardList.FindAll(elmenet => elmenet.rank == rank);
                if (danZhangCardList.Count >= PokerConstValue.PokerDanZhang)
                {
                    if (danZhangCardList.Count > PokerConstValue.PokerDanZhang)
                    {
                        danZhangCardList = danZhangCardList.Take(PokerConstValue.PokerDanZhang).ToList();
                    }
                    promptCardList.Add(new PromptCard(danZhangCardList));
                }
            }
        }
        private static void GetDuiZiPromptCard(List<Card> headCardList, List<Card> cardList, List<Rank> duiZiRankList, List<PromptCard> promptCardList)
        {
            if (headCardList == null || cardList == null || duiZiRankList == null || promptCardList == null)
            {
                return;
            }
            if (headCardList.Count <= cardList.Count || cardList.Count % PokerConstValue.PokerDuiZi != 0 || duiZiRankList.Count == 0)
            {
                return;
            }
            //获取手牌对子个数
            List<Rank> rankList = GetDuiZiCard(cardList);
            if (rankList.Count > duiZiRankList.Count)
            {
                return;
            }
            //获取提示组合
            List<Rank> resultList = new List<Rank>();
            bool bFlag = false;
            for (int i = 0; i < duiZiRankList.Count; ++i)
            {
                //初始化
                resultList.Clear();
                bFlag = false;
                resultList.Add(duiZiRankList[i]);
                for (int j = i + 1; j < rankList.Count + i; ++j)
                {
                    //不是连续的不能组合
                    if (j >= duiZiRankList.Count || duiZiRankList[j] + 1 != resultList[resultList.Count - 1])
                    {
                        bFlag = false;
                        break;
                    }
                    resultList.Add(duiZiRankList[j]);
                    bFlag = true;
                }
                //获取手牌中的组合
                if (!bFlag && rankList.Count > 1)
                {
                    continue;
                }
                List<Card> promptList = new List<Card>();
                foreach (Rank rank in resultList)
                {
                    List<Card> duiZiCardList = headCardList.FindAll(elmenet => elmenet.rank == rank);
                    if (duiZiCardList.Count >= PokerConstValue.PokerDuiZi)
                    {
                        if (duiZiCardList.Count > PokerConstValue.PokerDuiZi)
                        {
                            duiZiCardList = duiZiCardList.Take(PokerConstValue.PokerDuiZi).ToList();
                        }
                        promptList.AddRange(duiZiCardList);
                    }
                }
                if (promptList.Count / PokerConstValue.PokerDuiZi == rankList.Count)
                {
                    promptCardList.Add(new PromptCard(promptList));
                }
            }
        }
        private static void GetSanZhangPromptCard(List<Card> headCardList, List<Card> cardList, List<Rank> sanZhangRankList, List<PromptCard> promptCardList)
        {
            if (headCardList == null || cardList == null || sanZhangRankList == null || promptCardList == null)
            {
                return;
            }
            if (headCardList.Count <= cardList.Count || cardList.Count < PokerConstValue.PokerSanZhang || sanZhangRankList.Count == 0)
            {
                return;
            }
            //获取手牌三张个数
            List<Rank> rankList = GetSanZhangCard(cardList);
            if (rankList.Count > sanZhangRankList.Count)
            {
                return;
            }
            //获取提示组合
            List<Rank> resultList = new List<Rank>();
            bool bFlag = false;
            for (int i = 0; i < sanZhangRankList.Count; ++i)
            {
                //初始化
                resultList.Clear();
                bFlag = false;
                resultList.Add(sanZhangRankList[i]);
                for (int j = i + 1; j < rankList.Count + i; ++j)
                {
                    //不是连续的不能组合
                    if (j >= sanZhangRankList.Count || sanZhangRankList[j] + 1 != resultList[resultList.Count - 1])
                    {
                        bFlag = false;
                        break;
                    }
                    resultList.Add(sanZhangRankList[j]);
                    bFlag = true;
                }
                //获取手牌中的组合
                if (!bFlag && rankList.Count > 1)
                {
                    continue;
                }
                List<Card> promptList = new List<Card>();
                foreach (Rank rank in resultList)
                {
                    List<Card> duiZiCardList = headCardList.FindAll(elmenet => elmenet.rank == rank);
                    if (duiZiCardList.Count >= PokerConstValue.PokerSanZhang)
                    {
                        if (duiZiCardList.Count > PokerConstValue.PokerSanZhang)
                        {
                            duiZiCardList = duiZiCardList.Take(PokerConstValue.PokerSanZhang).ToList();
                        }
                        promptList.AddRange(duiZiCardList);
                    }
                }
                if (promptList.Count / PokerConstValue.PokerSanZhang == rankList.Count)
                {
                    promptCardList.Add(new PromptCard(promptList));
                }
            }
        }
        private static void GetShunZiPromptCard(List<Card> headCardList, List<Card> cardList, List<Rank> shunZiRankList, List<PromptCard> promptCardList)
        {
            if (headCardList == null || cardList == null || shunZiRankList == null || promptCardList == null)
            {
                return;
            }
            if (headCardList.Count <= cardList.Count || cardList.Count < PokerConstValue.PokerShunZi || shunZiRankList.Count < PokerConstValue.PokerShunZi)
            {
                return;
            }
            //获取提示组合
            List<Rank> resultList = new List<Rank>();
            bool bFlag = false;
            for (int i = 0; i < shunZiRankList.Count; ++i)
            {
                //初始化
                resultList.Clear();
                bFlag = false;
                resultList.Add(shunZiRankList[i]);
                for (int j = i + 1; j < cardList.Count + i; ++j)
                {
                    //不是连续的不能组合
                    if (j >= shunZiRankList.Count || shunZiRankList[j] + 1 != resultList[resultList.Count - 1])
                    {
                        bFlag = false;
                        break;
                    }
                    resultList.Add(shunZiRankList[j]);
                    bFlag = true;
                }
                //获取手牌中的组合
                if (!bFlag)
                {
                    continue;
                }
                List<Card> promptList = new List<Card>();
                foreach (Rank rank in resultList)
                {
                    List<Card> duiZiCardList = headCardList.FindAll(elmenet => elmenet.rank == rank);
                    if (duiZiCardList.Count >= PokerConstValue.PokerDanZhang)
                    {
                        if (duiZiCardList.Count > PokerConstValue.PokerDanZhang)
                        {
                            duiZiCardList = duiZiCardList.Take(PokerConstValue.PokerDanZhang).ToList();
                        }
                        promptList.AddRange(duiZiCardList);
                    }
                }
                if (promptList.Count == cardList.Count)
                {
                    promptCardList.Add(new PromptCard(promptList));
                }
            }
        }
        /// <summary>
        /// 获取玩家提示牌组合
        /// </summary>
        /// <param name="nextCardList"></param>     需要提示的玩家手牌
        /// <param name="cardList"></param>         上次出的牌
        /// <param name="type"></param>             上次出的牌的类型
        /// <param name="rankList"></param>         不屏蔽的牌
        /// <param name="promptCardList"></param>   用来放玩家提示牌组合列表
        public void GetPromptCardByType(List<Card> nextCardList, List<Card> cardList, PokerGroupType type, List<Rank> rankList, List<PromptCard> promptCardList)
        {
            Action<List<Card>, List<Card>, List<Rank>, List<PromptCard>> handle = GetPromptDataHandle(type);
            if (handle != null)
            {
                handle(nextCardList, cardList, rankList, promptCardList);
            }
        }
        /// <summary>
        /// 判断最后一把手牌有炸弹的时，能否丢掉
        /// </summary>
        /// <param name="showCardList"></param>
        /// <returns></returns>
        public bool IsShowFinallyPokersRules(List<Card> showCardList)
        {
            if (showCardList.Count <= PokerConstValue.PokerZhaDan)
            {
                return true;
            }
            List<Rank> zhaDanRankList = GetZhaDanCard(showCardList);
            if (zhaDanRankList != null && zhaDanRankList.Count > 0)
            {
                return false;
            }
            return true;
        }
    }
}
#endif
