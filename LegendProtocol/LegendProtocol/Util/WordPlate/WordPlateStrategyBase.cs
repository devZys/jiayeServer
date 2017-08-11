#if WORDPLATE
using System;
using System.Collections.Generic;

// 麻将的逻辑策略类.基类，包括了所有牌型的基本操作.吃。碰。杠。胡。算番等。不同打发若有不同。继承此类重写即可.
// 作为一个单例.一种玩法有一个策略即可.
namespace LegendProtocol
{
    public class WordPlateStrategyBase
    {
        //所有的牌
        protected List<WordPlateTile> m_allTileList = new List<WordPlateTile>();
        //临时变量
        protected List<PlateNumType> m_tempNumTypeList = new List<PlateNumType>();
        protected List<WordPlateTile> m_tempTileList = new List<WordPlateTile>();
        protected List<WordPlateCount> m_wordPlateCountList = new List<WordPlateCount>();

        protected int[] m_tempCountArray = new int[11];
        protected int[] m_currCountArray = new int[11];
        protected int[] m_defualtCount = new int[11] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // 听牌列表.
        protected List<WordPlateTile> m_ReadyHandTileList = new List<WordPlateTile>();

        // 分析牌型的时候的牌统计。
        protected List<WordPlateTile> m_BigTileList = new List<WordPlateTile>();
        protected List<WordPlateTile> m_SmallTileList = new List<WordPlateTile>();

        // 原始牌.用来做听牌遍历用.
        protected WordPlateTile[] m_BigTotalTiles = new WordPlateTile[10];
        protected WordPlateTile[] m_SmallTotalTiles = new WordPlateTile[10];

        //麻将类型
        public WordPlateType wordPlateType = WordPlateType.WordPlateNone; 
        public WordPlateStrategyBase()
        {
            for (int i = 0; i < m_BigTotalTiles.Length; ++i)
            {
                m_BigTotalTiles[i] = new WordPlateTile(PlateDescType.EPD_Big, (PlateNumType)((int)PlateNumType.EPN_One + i));
            }

            for (int i = 0; i < m_SmallTotalTiles.Length; ++i)
            {
                m_SmallTotalTiles[i] = new WordPlateTile(PlateDescType.EPD_Small, (PlateNumType)((int)PlateNumType.EPN_One + i));
            }
            InitWordPlateTile();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void InitWordPlateTile()
        {
            //初始化牌
            m_allTileList.Clear();
            for(int i = 0; i < 4; ++i)
            {
                for(int tileNum = (int)PlateNumType.EPN_One; tileNum <= (int)PlateNumType.EPN_Ten; ++tileNum)
                {
                    for (int tileDesc = (int)PlateDescType.EPD_Small; tileDesc <= (int)PlateDescType.EPD_Big; ++tileDesc)
                    {
                        m_allTileList.Add(new WordPlateTile((PlateDescType)tileDesc, (PlateNumType)tileNum));
                    }
                }
            }
        }

        protected virtual bool IsExistInTileList(WordPlateTile tile, List<WordPlateTile> tileList)
        {
            return tileList.Exists(element => element.Equal(tile));
        }

        /// <summary>
        /// 发牌
        /// </summary>
        /// <param name="wordPlateList"></param>        //玩家手中的牌
        /// <param name="remainWordPlateList"></param>  //剩余的牌
        public virtual void InitWordPlateTile(Dictionary<int, WordPlateHandTile> wordPlateList, List<WordPlateTile> remainWordPlateList)
        {
            List<WordPlateTile> _wordPlateTileList = new List<WordPlateTile>();
            _wordPlateTileList.AddRange(m_allTileList);
            for (int i = 0; i < wordPlateList.Count; ++i)
            {
                int count = WordPlateConstValue.WordPlateLeisureAmount;
                if (wordPlateList[i].zhuangLeisureType == ZhuangLeisureType.Zhuang)
                {
                    count = WordPlateConstValue.WordPlateZhuangAmount;
                }
                wordPlateList[i].wordPlateTileList.AddRange(GetWordPlateTileByCount(_wordPlateTileList, count));
                //wordPlateList[i].wordPlateTileList.AddRange(GetWordPlateTile(_wordPlateTileList, count, i));
            }
            remainWordPlateList.AddRange(_wordPlateTileList);
        }

        private List<WordPlateTile> GetWordPlateTileByCount(List<WordPlateTile> wordPlateList, int count)
        {
            List<WordPlateTile> resultList = new List<WordPlateTile>();
            for (int i = 0; i < count; ++i)
            {
                int index = MyRandom.NextPrecise(0, wordPlateList.Count);
                WordPlateTile tile = wordPlateList[index];
                resultList.Add(new WordPlateTile(tile));
                wordPlateList.Remove(tile);
            }

            return resultList;
        }
        private List<WordPlateTile> GetWordPlateTile(List<WordPlateTile> wordPlateList, int count, int index)
        {
            switch (index)
            {
                case 0:
                    return GetWordPlateTileOne(wordPlateList, count);
                case 1:
                    return GetWordPlateTileTwo(wordPlateList, count);
                case 2:
                    return GetWordPlateTileThree(wordPlateList, count);
                default:
                    break;
            }
            return new List<WordPlateTile>();
        }
        private List<WordPlateTile> GetWordPlateTileOne(List<WordPlateTile> wordPlateList, int count)
        {
            List<WordPlateTile> resultList = new List<WordPlateTile>();

            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Two));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Seven));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Ten));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Four));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Five));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Three));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Eight));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Eight));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Nine));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Nine));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Nine));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Eight));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Ten));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Seven));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Seven));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Four));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Five));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Two));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Two));
            if (count == 20)
            {
                resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Six));
            }
            foreach (WordPlateTile wordPlateTile in resultList)
            {
                wordPlateList.Remove(wordPlateList.Find(element => element.Equal(wordPlateTile)));
            }
            return resultList;
        }
        private List<WordPlateTile> GetWordPlateTileTwo(List<WordPlateTile> wordPlateList, int count)
        {
            List<WordPlateTile> resultList = new List<WordPlateTile>();

            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Ten));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Two));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Seven));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Three));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Four));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Five));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Nine));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Nine));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Nine));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Five));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Five));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Five));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Two));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Four));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Three));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Seven));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Eight));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Nine));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Ten));
            if (count == 20)
            {
                resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Ten));
            }
            foreach (WordPlateTile wordPlateTile in resultList)
            {
                wordPlateList.Remove(wordPlateList.Find(element => element.Equal(wordPlateTile)));
            }
            return resultList;
        }
        private List<WordPlateTile> GetWordPlateTileThree(List<WordPlateTile> wordPlateList, int count)
        {
            List<WordPlateTile> resultList = new List<WordPlateTile>();

            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Two));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Two));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Seven));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_One));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Two));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Three));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Three));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Three));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Six));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Eight));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_One));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_One));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Ten));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Six));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Six));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Six));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_One));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_One));
            resultList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_One));
            if (count == 20)
            {
                resultList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_One));
            }
            foreach (WordPlateTile wordPlateTile in resultList)
            {
                wordPlateList.Remove(wordPlateList.Find(element => element.Equal(wordPlateTile)));
            }
            return resultList;
        }

        /// <summary>
        /// 吃牌
        /// </summary>
        /// <param name="plateTile1"></param> 手牌1
        /// <param name="plateTile2"></param> 手牌2
        /// <param name="targetTile"></param> 上家打出来的牌
        /// <returns></returns> // 可以不判定.看需求.一般到此时已经确认这三张牌互为吃牌关系
        public virtual WordPlateMeld Chow(WordPlateTile plateTile1, WordPlateTile plateTile2, WordPlateTile targetTile)
        {
            // 确定三张牌的花色一致
            PlateDescType targetType = targetTile.GetDescType();

            if (plateTile1.GetDescType() == targetType && plateTile2.GetDescType() == targetType)
            {
                m_tempNumTypeList.Clear();
                m_tempNumTypeList.Add(plateTile1.GetNumType());
                m_tempNumTypeList.Add(plateTile2.GetNumType());
                m_tempNumTypeList.Add(targetTile.GetNumType());
                m_tempNumTypeList.Sort();

                if ((int)m_tempNumTypeList[0] + (int)m_tempNumTypeList[2] == 2 * (int)m_tempNumTypeList[1])
                {
                    //吃牌放前面
                    return new WordPlateMeld(targetTile, plateTile1, plateTile2, PlateMeldType.EPM_Sequence);
                }
            }

            return null;
        }

        /// <summary>
        /// 判定是否可以吃牌
        /// </summary>
        /// <param name="targetTile"></param> 目标牌
        /// <param name="handTile"></param> 自己的手牌
        /// <returns></returns>
        public virtual bool ChowCheck(WordPlateTile targetTile, List<WordPlateTile> handTile, List<WordPlateTile> passChowTileList = null)
        {
            if (passChowTileList != null && passChowTileList.Exists(element => element.Equal(targetTile)))
            {
                return false;
            }
            PlateDescType descType = targetTile.GetDescType();
            PlateNumType numType = targetTile.GetNumType();
            // 列表中是否有正负2的差牌
            bool hasMinusTwo = false;
            bool hasMinusOne = false;
            bool hasPlusTwo = false;
            bool hasPlusOne = false;

            // 满足一下条件可以吃, 1 同色. (2 索引-2，-1 . 3 索引 -1, 1. 4 索引 1, 2)
            handTile.ForEach(tile =>
            {
                if (tile.GetDescType() == descType)
                {
                    int diff = numType - tile.GetNumType();
                    switch (diff)
                    {
                        case -2:
                            hasMinusTwo = true;
                            break;
                        case -1:
                            hasMinusOne = true;
                            break;
                        case 1:
                            hasPlusOne = true;
                            break;
                        case 2:
                            hasPlusTwo = true;
                            break;
                        default:
                            break;
                    }
                }
            });

            // 满足3条件之一 可以吃.
            if ((hasMinusTwo && hasMinusOne) || (hasMinusOne && hasPlusOne) || (hasPlusOne && hasPlusTwo))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 碰（歪）牌
        /// </summary>
        /// <param name="plateTile1"></param> 手牌1
        /// <param name="plateTile2"></param> 手牌2
        /// <param name="targetTile"></param> 要操作的牌
        /// <returns></returns>
        public virtual WordPlateMeld PongWai(WordPlateTile plateTile1, WordPlateTile plateTile2, WordPlateTile targetTile, bool bWai = false)
        {
            // 确定三张牌的花色一致
            PlateDescType targetDescType = targetTile.GetDescType();

            if (plateTile1.GetDescType() == targetDescType && plateTile2.GetDescType() == targetDescType)
            {
                PlateNumType targetNumType = targetTile.GetNumType();

                if (plateTile1.GetNumType() == targetNumType && plateTile2.GetNumType() == targetNumType)
                {
                    return new WordPlateMeld(plateTile1, plateTile2, targetTile, bWai ? PlateMeldType.EPM_Wai : PlateMeldType.EPM_Pong);
                }
            }

            return null;
        }

        /// <summary>
        /// 判定目标牌是否可以碰歪溜飘.
        /// </summary>
        /// <param name="targetTile"></param>   别人打的牌.
        /// <param name="handTile"></param>     手牌列表
        /// <returns></returns> 这里注意一下可溜必可飘碰.不返回两种类型了
        public virtual PlateMeldType PongSlipCheck(WordPlateTile targetTile, List<WordPlateTile> handTile)
        {
            int count = 0;

            handTile.ForEach(tile =>
            {
                if (tile.Equal(targetTile))
                {
                    count++;
                }
            });

            if (count == 2)
            {
                return PlateMeldType.EPM_Pong;
            }
            else if (count == 3)
            {
                return PlateMeldType.EPM_Flutter;
            }
            else if (count == 4)
            {
                return PlateMeldType.EPM_Slip;
            }

            return PlateMeldType.EPM_None;
        }

        /// <summary>
        /// 判定是否可以内飘 溜(碰为内飘， 歪为溜)
        /// </summary>
        /// <param name="targetTile"></param>   选择要操作的牌.
        /// <param name="displayTileList"></param>     已经操作的牌
        /// <returns></returns>
        public virtual WordPlateMeld FlutterSlipCheck(WordPlateTile targetTile, List<WordPlateMeld> displayTileList)
        {
            return displayTileList.Find(element => (element.m_eMeldType == PlateMeldType.EPM_Pong || element.m_eMeldType == PlateMeldType.EPM_Wai) && element.m_meldTileList.Exists(tile => tile.Equal(targetTile)));
        }

        /// <summary>
        /// 判断是否存在死手
        /// </summary>
        /// <param name="targetTile"></param>
        /// <param name="displayTileList"></param>
        /// <returns></returns>
        public virtual bool DeadHandMeldCheck(WordPlateTile targetTile, List<WordPlateMeld> displayTileList)
        {
            foreach(WordPlateMeld meld in displayTileList)
            {
                if (meld.m_eMeldType == PlateMeldType.EPM_Pong || meld.m_eMeldType == PlateMeldType.EPM_Sequence)
                {
                    if (meld.CheckWordPlateTile(targetTile))
                    {
                        //吃过碰过的牌打出去死手
                        return true;
                    }
                    if (meld.m_eMeldType == PlateMeldType.EPM_Sequence && meld.m_meldTileList.Count == 3)
                    {
                        m_tempTileList.Clear();
                        m_tempTileList.Add(meld.m_meldTileList[1]);
                        m_tempTileList.Add(meld.m_meldTileList[2]);
                        if (ChowCheck(targetTile, m_tempTileList))
                        {
                            //打出去的牌能被再吃
                            return true;
                        }
                    }
                }
            }

            return false;
        } 
        /// <summary>
        /// 判断是否存在吃了这个牌
        /// </summary>
        /// <param name="targetTile"></param>
        /// <param name="displayTileList"></param>
        /// <returns></returns>
        public virtual bool ChowMeldCheck(WordPlateTile targetTile, List<WordPlateMeld> displayTileList)
        {
            return displayTileList.Exists(element => element.m_eMeldType == PlateMeldType.EPM_Sequence && element.CheckWordPlateTile(targetTile));
        }

        /// <summary>
        /// 飘 溜牌
        /// </summary>
        /// <param name="suitTile1"></param> 手牌1
        /// <param name="suitTile2"></param> 手牌2
        /// <param name="suitTile3"></param> 手牌3
        /// <param name="targetTile"></param> 目标牌.
        /// <returns></returns>  注意:::吃碰歪漂溜的判定可以去掉.一般来说 判定了可以吃碰歪漂溜了之后。下面的条件都已经满足了.没必要重复判定.
        public virtual WordPlateMeld FlutterSlip(WordPlateTile suitTile1, WordPlateTile suitTile2, WordPlateTile suitTile3, WordPlateTile targetTile, bool bSlip = true)
        {
            // 确定四张牌的花色一致
            PlateDescType targetDescType = targetTile.GetDescType();

            if (suitTile1.GetDescType() == targetDescType && suitTile2.GetDescType() == targetDescType && suitTile3.GetDescType() == targetDescType)
            {
                PlateNumType targetNumType = targetTile.GetNumType();

                if (suitTile1.GetNumType() == targetNumType && suitTile2.GetNumType() == targetNumType && suitTile3.GetNumType() == targetNumType)
                {
                    return new WordPlateMeld(suitTile1, suitTile2, suitTile3, targetTile, bSlip ? PlateMeldType.EPM_Slip : PlateMeldType.EPM_Flutter);
                }
            }

            return null;
        }


        // 判定列表牌中能暗杠的牌.(一个列表)
        public virtual void ConcealedKongCheck(List<WordPlateTile> tileList, int targetCount = 4)
        {
            m_tempNumTypeList.Clear();
            m_tempTileList.Clear();

            if (tileList.Count == 0)
            {
                return;
            }

            int count = 1;
            WordPlateTile newTile = tileList[0];
            // 建立在TileList已经排序好的基础上.
            for (int i = 1; i < tileList.Count; ++i)
            {
                if (newTile.Equal(tileList[i]))
                {
                    count++;

                    if (count == targetCount)
                    {
                        m_tempNumTypeList.Add(tileList[i].GetNumType());
                        m_tempTileList.Add(tileList[i]);
                    }
                    else if (count > targetCount)
                    {
                        m_tempNumTypeList.Remove(tileList[i].GetNumType());
                        m_tempTileList.RemoveAll(element => element.Equal(tileList[i]));
                    }
                }
                else
                {
                    count = 1;
                    newTile = tileList[i];
                }
            }
        }
        protected virtual void ResetCountArrray()
        {
            m_defualtCount.CopyTo(m_tempCountArray, 0);
            m_defualtCount.CopyTo(m_currCountArray, 0);
        }
        public bool AnalyseHandTile(List<WordPlateTile> handTileList)
        {
            // 统计数量.
            m_BigTileList.Clear();
            m_SmallTileList.Clear();

            for (int i = 0; i < handTileList.Count; ++i)
            {
                WordPlateTile tile = handTileList[i];
                switch (tile.GetDescType())
                {
                    case PlateDescType.EPD_Big:
                        m_BigTileList.Add(tile);
                        break;
                    case PlateDescType.EPD_Small:
                        m_SmallTileList.Add(tile);
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        // 手牌分析.刚抓到一张牌之后做手牌分析.也可为服务器发牌之后第一次数据分析.
        public virtual bool AnalyseHandTile(List<WordPlateTile> handTileList, List<WordPlateMeld> meldList, int baseScore, bool bFamous, WordPlateTile huTile = null, bool bMySelf = false)
        {
            // 前提.手牌必须是补花和排序之后的 否者无法快速分析牌型

            // 胡牌逻辑需要对数据进行如下统计:
            // 1.判定是需要判定胡牌还是听牌.
            // 2.每种牌型的数量统计出来(看是否满足3n+2 或者 3n 或者 2)
            // 3.对于有3n+2的牌型.判定去掉将判定是否符合3N的组合。

            // 统计数量.
            if (!AnalyseHandTile(handTileList))
            {
                return false;
            }

            // Setp1:区分是要胡牌还是要听牌.
            int nHandTileCount = handTileList.Count;
            bool bCheckWin = false;
            if (nHandTileCount % 3 == 2)
            {
                bCheckWin = true;
            }
            else if (nHandTileCount % 3 == 1)
            {
            }
            else
            {
                return false;
            }

            // 判定胡牌.
            if (bCheckWin)
            {
                int nPairsCount = 0;
                if (m_BigTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                if (m_SmallTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }

                // 做将的牌型多于1则不需要判定了 不能胡
                if (nPairsCount != 1)
                {
                    return false;
                }

                //Step2:确认所有牌组是否都符合3*n + 2 或者 3×n的组合
                if (GetHandWordPlateToMeld(m_BigTileList, meldList, huTile, bMySelf) && GetHandWordPlateToMeld(m_SmallTileList, meldList, huTile, bMySelf))
                {
                    return true;
                }

                return false;
            }            
            return false;
        }
        /// <summary>
        /// 获取手牌组合类型
        /// </summary>
        /// <param name="tilesList"></param>
        /// <param name="meldList"></param>
        /// <param name="huTile"></param>
        /// <param name="bMySelf"></param>
        /// <returns></returns>
        public virtual bool GetHandWordPlateToMeld(List<WordPlateTile> tilesList, List<WordPlateMeld> meldList, WordPlateTile huTile, bool bMySelf)
        {
            if (tilesList.Count == 0)
            {
                return false;
            }

            ResetCountArrray();

            PlateDescType descType = tilesList[0].GetDescType();
            for (int i = 0; i < tilesList.Count; ++i)
            {
                // 统计牌数
                int index = (int)tilesList[i].GetNumType();
                m_tempCountArray[index]++;
                m_currCountArray[index]++;
            }

            // 先获取坎
            for (int i = 1; i < m_tempCountArray.Length; ++i)
            {
                if (m_tempCountArray[i] >= 3)
                {
                    m_currCountArray[i] -= 3;
                    PlateMeldType meldType = PlateMeldType.EPM_Triplet;
                    WordPlateTile newTile = new WordPlateTile(descType, (PlateNumType)i);
                    if (huTile != null && huTile.Equal(newTile))
                    {
                        if (bMySelf)
                        {
                            meldType = PlateMeldType.EPM_Wai;
                        }
                        else
                        {
                            meldType = PlateMeldType.EPM_Pong;
                        }
                    }
                    WordPlateMeld meld = new WordPlateMeld(newTile, newTile, newTile, meldType);
                    meldList.Add(meld);
                }
            }
            //再获取顺子
            if (tilesList.Count % 3 == 0)
            {
                return GetWordPlateMeldTo3Dot(descType, meldList);
            }
            else if (tilesList.Count % 3 == 2)
            {
                // 索引从1开始对应数字的牌的枚举(除去将牌之后判定剩余牌是否为3n)
                for (int i = 1; i < m_tempCountArray.Length; ++i)
                {
                    if (m_currCountArray[i] >= 2)
                    {
                        m_currCountArray[i] -= 2;
                        PlateMeldType meldType = PlateMeldType.EPM_Pair;
                        WordPlateTile newTile = new WordPlateTile(descType, (PlateNumType)i);
                        if (huTile != null && huTile.Equal(newTile))
                        {
                            if (bMySelf)
                            {
                                meldType = PlateMeldType.EPM_Wai;
                            }
                            else
                            {
                                meldType = PlateMeldType.EPM_Pong;
                            }
                        }
                        WordPlateMeld meld = new WordPlateMeld(newTile, newTile, meldType);
                        meldList.Add(meld);
                        if (GetWordPlateMeldTo3Dot(descType, meldList))
                        {
                            return true;
                        }
                        meldList.Remove(meld);
                        m_currCountArray[i] += 2;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 获取顺子组合
        /// </summary>
        /// <param name="descType"></param>
        /// <param name="meldList"></param>
        /// <returns></returns>
        public virtual bool GetWordPlateMeldTo3Dot(PlateDescType descType, List<WordPlateMeld> meldList)
        {
            for (int i = 1; i < m_currCountArray.Length; ++i)
            {
                if (m_currCountArray[i] > 0)
                {
                    // 顺.
                    if (i < m_currCountArray.Length - 2 && m_currCountArray[i + 1] > 0 && m_currCountArray[i + 2] > 0)
                    {
                        m_currCountArray[i] -= 1;
                        m_currCountArray[i + 1] -= 1;
                        m_currCountArray[i + 2] -= 1;
                        WordPlateMeld meld = new WordPlateMeld(new WordPlateTile(descType, (PlateNumType)i), new WordPlateTile(descType, (PlateNumType)i + 1), new WordPlateTile(descType, (PlateNumType)i + 2), PlateMeldType.EPM_Sequence);
                        meldList.Add(meld);
                        if (GetWordPlateMeldTo3Dot(descType, meldList))
                        {
                            return true;
                        }
                        else
                        {
                            m_currCountArray[i] += 1;
                            m_currCountArray[i + 1] += 1;
                            m_currCountArray[i + 2] += 1;
                            meldList.Add(meld);
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // 判定所有牌之后确认每个Count现在都为0的时候为胡牌.
            return true;
        }
        /// <summary>
        /// 计算明牌积分
        /// </summary>
        /// <param name="meldList"></param>
        /// <returns></returns>
        public virtual int GetWordPlateMeldScore(List<WordPlateMeld> meldList)
        {
            return 0;
        }
        /// <summary>
        /// 计算胡牌牌组的积分和番类型
        /// </summary>
        /// <param name="meldList"></param>
        /// <param name="wordPlateFanList"></param>
        /// <returns></returns>
        public virtual WordPlateWinSorce GetWordPlateMeldScoreAndFan(List<WordPlateMeld> meldList, List<WordPlateFanNode> wordPlateFanList, bool bFamous, bool bBigSmallHu, List<WordPlateTile> noHandWordPlateList)
        {
            return null;
        }
        // 比较排序函数(顺序为. 小大)
        public virtual int CompareTile(WordPlateTile a, WordPlateTile b)
        {
            PlateDescType eDescTypeA = a.GetDescType();
            PlateDescType eDescTypeB = b.GetDescType();

            // 如果类型不一样按照类型排序
            if (eDescTypeA != eDescTypeB)
            {
                if ((int)eDescTypeA > (int)eDescTypeB)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }

            // 如果类型一样的话 按NumType排序
            PlateNumType eNumTypeA = a.GetNumType();
            PlateNumType eNumTypeB = b.GetNumType();

            if ((int)eNumTypeA > (int)eNumTypeB)
            {
                return 1;
            }
            else if ((int)eNumTypeA == (int)eNumTypeB)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        // 比较排序函数(顺序为. 大小)
        public virtual int CompareTileByRevMeld(WordPlateMeld a, WordPlateMeld b)
        {
            if ((int)a.m_eMeldType < (int)b.m_eMeldType)
            {
                return 1;
            }
            else if ((int)a.m_eMeldType == (int)b.m_eMeldType)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        // 比较排序函数(顺序为. 小大)
        public virtual int CompareTileByMeld(WordPlateMeld a, WordPlateMeld b)
        {
            if ((int)a.m_eMeldType < (int)b.m_eMeldType)
            {
                return -1;
            }
            else if ((int)a.m_eMeldType == (int)b.m_eMeldType)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        public virtual List<List<WordPlateTile>> GetHandMeld(List<WordPlateTile> handTileList)
        {
            List<List<WordPlateTile>> allTilesList = new List<List<WordPlateTile>>();
            if (handTileList == null || handTileList.Count == 0)
            {
                return allTilesList;
            }
            //排序
            //handTileList.Sort(CompareTile);
            // 统计数量.
            if (!AnalyseHandTile(handTileList))
            {
                return allTilesList;
            }
            if (m_SmallTileList.Count > 0)
            {
                allTilesList.AddRange(GetTileMeld(m_SmallTileList));
            }
            if (m_BigTileList.Count > 0)
            {
                allTilesList.AddRange(GetTileMeld(m_BigTileList));
            }
            return allTilesList;
        } 
        protected virtual List<List<WordPlateTile>> GetTileMeld(List<WordPlateTile> tileList)
        {
            if (tileList.Count == 0)
            {
                return new List<List<WordPlateTile>>();
            }

            ResetCountArrray();

            PlateDescType descType = tileList[0].GetDescType();
            for (int i = 0; i < tileList.Count; ++i)
            {
                // 统计牌数
                int index = (int)tileList[i].GetNumType();
                m_currCountArray[index]++;
            }
            ;
            return GetTileMeld(descType);
        }
        protected virtual List<List<WordPlateTile>> GetTileMeld(PlateDescType descType)
        {
            return new List<List<WordPlateTile>>();
        }
        public virtual List<WordPlateHuMeld> GetHuMeld(List<WordPlateTile> handTileList, List<WordPlateMeld> meldList, int baseScore, bool bFamous, bool bOperatHu = false, WordPlateTile huTile = null, bool bMySelf = false)
        {
            return new List<WordPlateHuMeld>();
        }
        public bool IsWaiHu(List<WordPlateMeld> displayTileList, List<WordPlateMeld> meldList, WordPlateTile huTile)
        {
            if (huTile == null)
            {
                return false;
            }
            List<WordPlateMeld> handMeldList = new List<WordPlateMeld>();
            handMeldList.AddRange(meldList);
            displayTileList.ForEach(meld =>
            {
                handMeldList.Remove(meld);
            });
            //排序
            handMeldList.Sort(CompareTileByRevMeld);
            foreach (WordPlateMeld meld in handMeldList)
            {
                if (meld.m_eMeldType == PlateMeldType.EPM_Wai && meld.m_meldTileList.Exists(element => element.Equal(huTile)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif