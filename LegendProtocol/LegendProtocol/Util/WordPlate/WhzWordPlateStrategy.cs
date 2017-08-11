#if WORDPLATE
using System;
using System.Collections.Generic;
using System.Linq;

// 麻将的逻辑策略类.基类，包括了所有牌型的基本操作.吃。碰。杠。胡。算番等。不同打发若有不同。继承此类重写即可.
// 作为一个单例.一种玩法有一个策略即可.
namespace LegendProtocol
{
    public class WhzWordPlateStrategy : WordPlateStrategyBase
    {
        public WhzWordPlateStrategy()
        {
            wordPlateType = WordPlateType.WaiHuZiPlate;
        }

        /// <summary>
        /// 吃牌
        /// </summary>
        /// <param name="plateTile1"></param> 手牌1
        /// <param name="plateTile2"></param> 手牌2
        /// <param name="targetTile"></param> 上家打出来的牌
        /// <returns></returns> // 可以不判定.看需求.一般到此时已经确认这三张牌互为吃牌关系
        public override WordPlateMeld Chow(WordPlateTile plateTile1, WordPlateTile plateTile2, WordPlateTile targetTile)
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

                if (((int)m_tempNumTypeList[0] + (int)m_tempNumTypeList[2] == 2 * (int)m_tempNumTypeList[1]) ||
                    (m_tempNumTypeList[0] == PlateNumType.EPN_Two && m_tempNumTypeList[1] == PlateNumType.EPN_Seven && m_tempNumTypeList[2] == PlateNumType.EPN_Ten))
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
        public override bool ChowCheck(WordPlateTile targetTile, List<WordPlateTile> handTile, List<WordPlateTile> passChowTileList = null)
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
            // 列表中是否有2-7-10
            bool hasMinusEight = false;
            bool hasMinusFive = false;
            bool hasMinusThree = false;
            bool hasPlusEight = false;
            bool hasPlusFive = false;
            bool hasPlusThree = false;

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
                        case -8:
                            hasMinusEight = true;
                            break;
                        case -5:
                            hasMinusFive = true;
                            break;
                        case -3:
                            hasMinusThree = true;
                            break;
                        case 3:
                            hasPlusThree = true;
                            break;
                        case 5:
                            hasPlusFive = true;
                            break;
                        case 8:
                            hasPlusEight = true;
                            break;
                        default:
                            break;
                    }
                }
            });

            // 满足3条件之一 可以吃.
            if (hasMinusTwo && hasMinusOne)
            {
                if (passChowTileList == null || passChowTileList.Count == 0)
                {
                    return true;
                }
                int minusThreeNum = (int)numType + 3;
                if (minusThreeNum > (int)PlateNumType.EPN_Ten)
                {
                    return true;
                }
                else if (!passChowTileList.Exists(element => (element.GetDescType() == descType && element.GetNumType() == (PlateNumType)minusThreeNum)))
                {
                    return true;
                }
            }
            if (hasPlusOne && hasPlusTwo)
            {
                if (passChowTileList == null || passChowTileList.Count == 0)
                {
                    return true;
                }
                int plusThreeNum = (int)numType - 3;
                if (plusThreeNum <= (int)PlateNumType.EPN_None)
                {
                    return true;
                }
                else if (!passChowTileList.Exists(element => (element.GetDescType() == descType && element.GetNumType() == (PlateNumType)plusThreeNum)))
                {
                    return true;
                }
            }
            if ((hasMinusOne && hasPlusOne) || (targetTile.IsRed() && ((hasMinusEight && hasMinusFive) || (hasMinusThree && hasPlusFive) || (hasPlusEight && hasPlusThree))))
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// 计算明牌积分（客户端用）
        /// </summary>
        /// <param name="meldList"></param>
        /// <returns></returns>
        public override int GetWordPlateMeldScore(List<WordPlateMeld> meldList)
        {
            int score = 0;
            meldList.ForEach(meld =>
            {
                if (meld.m_eMeldType == PlateMeldType.EPM_Wai || meld.m_eMeldType == PlateMeldType.EPM_Slip)
                {
                    //歪 溜 单歪
                    score += 4;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pong || meld.m_eMeldType == PlateMeldType.EPM_Flutter)
                {
                    //碰 飘 单碰
                    score += 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Sequence && !meld.m_meldTileList.Exists(element => !element.IsRed()))
                {
                    //顺子 2-7-10
                    score += 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Triplet)
                {
                    //坎
                    score += 3;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pair && meld.m_meldTileList.Count == 2 &&
                (meld.m_meldTileList[0].Equal(meld.m_meldTileList[1]) || !meld.m_meldTileList.Exists(element => !element.IsRed())))
                {
                    //对子或者红牌
                    score += 1;
                }
            });

            return score;
        }
        /// <summary>
        /// 计算明牌积分（自己用）
        /// </summary>
        /// <param name="meldList"></param>
        /// <returns></returns>
        protected int GetWordPlateMeldScore(List<WordPlateMeld> showMeldList, List<WordPlateMeld> huMeldList, int baseScore, bool bFamous)
        {
            int score = 0;
            int redZuCount = 0;
            int redMeldCount = 0;
            showMeldList.ForEach(meld =>
            {
                if (meld.m_eMeldType == PlateMeldType.EPM_Wai || meld.m_eMeldType == PlateMeldType.EPM_Slip)
                {
                    //歪 溜
                    score += 4;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pong || meld.m_eMeldType == PlateMeldType.EPM_Flutter)
                {
                    //碰 飘
                    score += 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Sequence && !meld.m_meldTileList.Exists(element => !element.IsRed()))
                {
                    //吃 2-7-10
                    score += 1;
                }
                //红牌个数
                if (bFamous)
                {
                    int _redPlateCount = meld.m_meldTileList.FindAll(element => element.IsRed()).Count;
                    if (_redPlateCount > 0)
                    {
                        if (_redPlateCount == meld.m_meldTileList.Count)
                        {
                            redZuCount += 1;
                        }
                        else
                        {
                            redMeldCount += 1;
                        }
                    }
                }
            });
            huMeldList.ForEach(meld =>
            {
                if (meld.m_eMeldType == PlateMeldType.EPM_Wai)
                {
                    //歪 单歪
                    score += 4;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pong)
                {
                    //碰 单碰
                    score += 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Sequence && !meld.m_meldTileList.Exists(element => !element.IsRed()))
                {
                    //顺子 2-7-10
                    score += 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Triplet)
                {
                    //坎
                    score += 3;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pair && meld.m_meldTileList.Count == 2 &&
                (meld.m_meldTileList[0].Equal(meld.m_meldTileList[1]) || !meld.m_meldTileList.Exists(element => !element.IsRed())))
                {
                    //对子或者红牌
                    score += 1;
                }
                //红牌个数
                if (bFamous)
                {
                    int _redPlateCount = meld.m_meldTileList.FindAll(element => element.IsRed()).Count;
                    if (_redPlateCount > 0)
                    {
                        if (_redPlateCount == meld.m_meldTileList.Count)
                        {
                            redZuCount += 1;
                        }
                        else
                        {
                            redMeldCount += 1;
                        }
                    }
                }
            });
            //息不够
            if (score < baseScore)
            {
                return 0;
            }
            //名堂有优先获取权
            int fan = 1;
            if (bFamous)
            {
                if ((redZuCount == 1 && redMeldCount == 0) || (redZuCount == 1 && redMeldCount == 1) || (redZuCount == 2 && redMeldCount == 0))
                {
                    //单漂  //印  //双漂
                    fan = 2;         
                }
                else if ((redZuCount + redMeldCount) == (showMeldList.Count + huMeldList.Count))
                {
                    //花胡子  
                    fan = 64;
                }
            }
            return score * fan;
        }
        /// <summary>
        /// 计算胡牌牌组的积分和番类型
        /// </summary>
        /// <param name="meldList"></param>
        /// <param name="wordPlateFanList"></param>
        /// <returns></returns>
        public override WordPlateWinSorce GetWordPlateMeldScoreAndFan(List<WordPlateMeld> meldList, List<WordPlateFanNode> wordPlateFanList, bool bFamous, bool bBigSmallHu, List<WordPlateTile> noHandWordPlateList)
        {
            int baseScore = 0;
            int redPlateCount = 0;
            int redZuCount = 0;
            int redMeldCount = 0;
            int smallPlateCount = 0;
            int bigPlateCount = 0;
            int neiYuanCount = 0;
            int waiYuanCount = 0;
            bool bPengPengHu = true;
            bool bHangHangXi = true;
            m_wordPlateCountList.Clear();
            //开始统计
            meldList.ForEach(meld =>
            {
                if (meld.m_eMeldType == PlateMeldType.EPM_Wai)
                {
                    //歪 单歪
                    baseScore += 4;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Slip)
                {
                    //溜
                    baseScore += 4;
                    neiYuanCount += 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pong)
                {
                    //碰 单碰
                    baseScore += 1;
                    if (meld.m_meldTileList.Count > 0 && !noHandWordPlateList.Exists(e => e.Equal(meld.m_meldTileList[0])))
                    {
                        noHandWordPlateList.Add(meld.m_meldTileList[0]);
                    }
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Flutter)
                {
                    //飘
                    baseScore += 1;
                    waiYuanCount += 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Sequence)
                {
                    //顺子
                    if (!meld.m_meldTileList.Exists(element => !element.IsRed()))
                    {
                        //2-7-10
                        baseScore += 1;
                    }
                    else
                    {
                        bHangHangXi = false;
                    }
                    bPengPengHu = false;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Triplet)
                {
                    //坎
                    baseScore += 3;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pair && meld.m_meldTileList.Count == 2)
                {
                    if (!meld.m_meldTileList.Exists(element => !element.IsRed()) || meld.m_meldTileList[0].Equal(meld.m_meldTileList[1]))
                    {

                        //对子或者红牌
                        baseScore += 1;
                    }
                    else
                    {
                        bHangHangXi = false;
                    }
                    if (!meld.m_meldTileList[0].Equal(meld.m_meldTileList[1]))
                    {
                        bPengPengHu = false;
                    }
                }
                else
                {
                    bPengPengHu = false;
                    bHangHangXi = false;
                }
                //红牌个数
                int _redPlateCount = meld.m_meldTileList.FindAll(element => element.IsRed()).Count;
                if (_redPlateCount > 0)
                {
                    redPlateCount += _redPlateCount;
                    if (bFamous)
                    {
                        if (_redPlateCount == meld.m_meldTileList.Count)
                        {
                            redZuCount += 1;
                        }
                        else
                        {
                            redMeldCount += 1;
                        }
                    }
                }
                //大小胡
                if (bBigSmallHu && meld.m_meldTileList.Count > 0)
                {
                    if (meld.m_meldTileList[0].GetDescType() == PlateDescType.EPD_Big)
                    {
                        bigPlateCount += meld.m_meldTileList.Count;
                    }
                    else
                    {
                        smallPlateCount += meld.m_meldTileList.Count;
                    }
                }
                if (meld.m_eMeldType != PlateMeldType.EPM_Slip && meld.m_eMeldType != PlateMeldType.EPM_Flutter)
                {
                    meld.m_meldTileList.ForEach(tile =>
                    {
                        WordPlateCount wordPlateCount = m_wordPlateCountList.Find(e => e.wordPlateTile.Equal(tile));
                        if (wordPlateCount == null)
                        {
                            wordPlateCount = new WordPlateCount(tile);
                            m_wordPlateCountList.Add(wordPlateCount);
                        }
                        wordPlateCount.count += 1;
                    });
                }
            });
            //计算内元 外元
            List<WordPlateCount> fourWordPlateList = m_wordPlateCountList.FindAll(e => e.count >= 4);
            if (fourWordPlateList.Count > 0)
            {
                fourWordPlateList.ForEach(fourTile =>
                {
                    if (noHandWordPlateList.Exists(element => element.Equal(fourTile.wordPlateTile)))
                    {
                        //组成第4个的牌不属于自己就是外元
                        waiYuanCount += 1;
                    }
                    else
                    {
                        neiYuanCount += 1;
                    }
                });
            }
            //算番 内元
            int allFanCount = 1;
            wordPlateFanList.ForEach(element =>
            {
                allFanCount *= element.fanCount;
            });
            if (neiYuanCount > 0)
            {
                int _neiYuanFan = (int)Math.Pow(4, neiYuanCount);//4 * neiYuanCount;
                wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_NeiYuan, fanCount = _neiYuanFan });
                allFanCount *= _neiYuanFan;
            }
            //外元
            if (waiYuanCount > 0)
            {
                int _waiYuanFan = (int)Math.Pow(2, waiYuanCount);//2 * waiYuanCount;
                wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_WaiYuan, fanCount = _waiYuanFan });
                allFanCount *= _waiYuanFan;
            }
            //对子胡
            if (bPengPengHu)
            {
                if (bHangHangXi)
                {
                    bHangHangXi = false;
                }
                wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_PengPengHu, fanCount = 8 });
                allFanCount *= 8;
            }
            //行行息
            if (bHangHangXi)
            {
                wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_HangHangXi, fanCount = 4 });
                allFanCount *= 4;
            }
            //红个数
            if (redPlateCount == 0)
            {
                //黑子胡
                wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_BlackPlateHu, fanCount = 8 });
                allFanCount *= 8;
            }
            else if (redPlateCount == 1)
            {
                //一点红
                wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_OneRedPlate, fanCount = 4 });
                allFanCount *= 4;
            }
            else if (redPlateCount >= 10)
            {
                //火火翻
                int _fanCount = (int)Math.Pow(2, redPlateCount - 9);
                wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_RedPlateHu, fanCount = _fanCount });
                allFanCount *= _fanCount;
            }
            //大小胡
            if (bBigSmallHu)
            {
                //大字胡
                if (smallPlateCount == 0)
                {
                    wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_BigPlateHu, fanCount = 8 });
                    allFanCount *= 8;
                }
                //小字胡
                else if (bigPlateCount == 0)
                {
                    wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_SmallPlateHu, fanCount = 8 });
                    allFanCount *= 8;
                }
            }
            //名堂
            if (bFamous)
            {
                if (redZuCount == 1 && redMeldCount == 0)
                {
                    //单漂
                    wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_DanPiaoHu, fanCount = 2 });
                    allFanCount *= 2;
                }
                else if (redZuCount == 1 && redMeldCount == 1)
                {
                    //印
                    wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_YinHu, fanCount = 2 });
                    allFanCount *= 2;
                }
                else if (redZuCount == 2 && redMeldCount == 0)
                {
                    //双漂
                    wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_DoublePiaoHu, fanCount = 2 });
                    allFanCount *= 2;
                }
                else if ((redZuCount + redMeldCount) == meldList.Count)
                {
                    //花胡子
                    wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_HuaRedHu, fanCount = 64 });
                    allFanCount *= 64;
                }
            }

            return new WordPlateWinSorce(baseScore, baseScore * allFanCount);
        }

        // 手牌分析.刚抓到一张牌之后做手牌分析.也可为服务器发牌之后第一次数据分析.
        public override bool AnalyseHandTile(List<WordPlateTile> handTileList, List<WordPlateMeld> meldList, int baseScore, bool bFamous, WordPlateTile huTile = null, bool bMySelf = false)
        {
            // 前提.手牌必须是补花和排序之后的 否者无法快速分析牌型

            // 胡牌逻辑需要对数据进行如下统计:
            // 1.每种牌型的数量统计出来(看是否满足3n+2 或者 3n 或者 2)
            // 2.对于有3n+2的牌型.判定去掉将判定是否符合3N的组合。

            // 统计数量.
            if (!AnalyseHandTile(handTileList))
            {
                return false;
            }

            // 判定胡牌.
            if (handTileList.Count % 3 == 2)
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
                List<WordPlateMeld> _meldList = new List<WordPlateMeld>();
                List<List<WordPlateMeld>> wordPlateMeldList = new List<List<WordPlateMeld>>();
                bool bBig = false;
                if (m_BigTileList.Count % 3 == 0)
                {
                    bBig = GetHandWordPlateToMeld(m_BigTileList, _meldList, huTile, bMySelf);
                }
                else
                {
                    bBig = GetWordPlateMeldToPair(m_BigTileList, wordPlateMeldList, huTile, bMySelf);
                }
                bool bSmall = false;
                if (m_SmallTileList.Count % 3 == 0)
                {
                    bSmall = GetHandWordPlateToMeld(m_SmallTileList, _meldList, huTile, bMySelf);
                }
                else
                {
                    bSmall = GetWordPlateMeldToPair(m_SmallTileList, wordPlateMeldList, huTile, bMySelf);
                }
                //同时通过才能进行下一步判断
                if (bBig && bSmall)
                {
                    List<WordPlateHuSorceMeld> wordPlateHuSorceMeldList = new List<WordPlateHuSorceMeld>();
                    wordPlateMeldList.ForEach(eMeldList =>
                    {
                        List<WordPlateMeld> huMeldList = new List<WordPlateMeld>();
                        huMeldList.AddRange(_meldList);
                        huMeldList.AddRange(eMeldList);
                        int score = GetWordPlateMeldScore(meldList, huMeldList, baseScore, bFamous);
                        if (score > 0 && score >= baseScore)
                        {
                            wordPlateHuSorceMeldList.Add(new WordPlateHuSorceMeld(score, huMeldList));
                        }
                    });
                    if (wordPlateHuSorceMeldList.Count > 0)
                    {
                        bool bMax = false;
                        if (wordPlateHuSorceMeldList.Count >= 1)
                        {
                            int maxSorce = wordPlateHuSorceMeldList.Max(e => e.maxSorce);
                            WordPlateHuSorceMeld sorceMeld = wordPlateHuSorceMeldList.FirstOrDefault(e => e.maxSorce == maxSorce);
                            if (sorceMeld != null)
                            {
                                meldList.AddRange(sorceMeld.huMeldList);
                                bMax = true;
                            }
                        }
                        if (!bMax)
                        {
                            meldList.AddRange(wordPlateHuSorceMeldList[0].huMeldList);
                        }
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
        public override bool GetHandWordPlateToMeld(List<WordPlateTile> tilesList, List<WordPlateMeld> meldList, WordPlateTile huTile, bool bMySelf)
        {
            if (tilesList.Count == 0)
            {
                return true;
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

            return GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf);
        }
        public bool GetWordPlateMeldTo3Dot(PlateDescType descType, List<WordPlateMeld> meldList, WordPlateTile huTile, bool bMySelf)
        {
            for (int i = 1; i < m_currCountArray.Length; ++i)
            {
                if (m_currCountArray[i] >= 3)
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
                        else if (m_tempCountArray[i] == 3)
                        {
                            //3个才取碰 4个优先取坎
                            meldType = PlateMeldType.EPM_Pong;
                        }
                    }
                    WordPlateMeld meld = new WordPlateMeld(newTile, newTile, newTile, meldType);
                    meldList.Add(meld);
                    if (GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf))
                    {
                        return true;
                    }
                    meldList.Remove(meld);
                    m_currCountArray[i] += 3;
                }
                if (m_currCountArray[i] > 0)
                {
                    // 顺.
                    if (i == 2 && m_currCountArray[7] > 0 && m_currCountArray[10] > 0)
                    {
                        m_currCountArray[i] -= 1;
                        m_currCountArray[7] -= 1;
                        m_currCountArray[10] -= 1;
                        WordPlateMeld meld = new WordPlateMeld(new WordPlateTile(descType, (PlateNumType)10), new WordPlateTile(descType, (PlateNumType)7), new WordPlateTile(descType, (PlateNumType)2), PlateMeldType.EPM_Sequence);
                        meldList.Add(meld);
                        if (GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf))
                        {
                            return true;
                        }
                        meldList.Remove(meld);
                        m_currCountArray[i] += 1;
                        m_currCountArray[7] += 1;
                        m_currCountArray[10] += 1;
                    }
                    if (i < m_currCountArray.Length - 2 && m_currCountArray[i + 1] > 0 && m_currCountArray[i + 2] > 0)
                    {
                        m_currCountArray[i] -= 1;
                        m_currCountArray[i + 1] -= 1;
                        m_currCountArray[i + 2] -= 1;
                        WordPlateMeld meld = new WordPlateMeld(new WordPlateTile(descType, (PlateNumType)i + 2), new WordPlateTile(descType, (PlateNumType)i + 1), new WordPlateTile(descType, (PlateNumType)i), PlateMeldType.EPM_Sequence);
                        meldList.Add(meld);
                        if (GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf))
                        {
                            return true;
                        }
                        else
                        {
                            meldList.Remove(meld);
                            m_currCountArray[i] += 1;
                            m_currCountArray[i + 1] += 1;
                            m_currCountArray[i + 2] += 1;
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
        protected bool GetWordPlateMeldToPair(List<WordPlateTile> tilesList, List<List<WordPlateMeld>> wordPlateMeldList, WordPlateTile huTile, bool bMySelf)
        {
            if (tilesList.Count == 0)
            {
                return true;
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
            //判断胡牌类型
            for (int i = 1; i < m_tempCountArray.Length; ++i)
            {
                if (m_tempCountArray[i] > 0)
                {
                    if (m_tempCountArray[i] >= 2)
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
                        List<WordPlateMeld> meldList = new List<WordPlateMeld>();
                        meldList.Add(meld);
                        if (GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf))
                        {
                            m_tempCountArray.CopyTo(m_currCountArray, 0);
                            wordPlateMeldList.Add(meldList);
                        }
                        else
                        {
                            meldList.Remove(meld);
                            m_currCountArray[i] += 2;
                        }
                    }
                    if (m_tempCountArray[i] >= 1)
                    {
                        if (i == 2 && m_tempCountArray[7] > 0)
                        {
                            m_currCountArray[i] -= 1;
                            m_currCountArray[7] -= 1;
                            WordPlateMeld meld = new WordPlateMeld(new WordPlateTile(descType, (PlateNumType)7), new WordPlateTile(descType, (PlateNumType)2));
                            List<WordPlateMeld> meldList = new List<WordPlateMeld>();
                            meldList.Add(meld);
                            if (GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf))
                            {
                                m_tempCountArray.CopyTo(m_currCountArray, 0);
                                wordPlateMeldList.Add(meldList);
                            }
                            else
                            {
                                meldList.Remove(meld);
                                m_currCountArray[i] += 1;
                                m_currCountArray[7] += 1;
                            }
                        }
                        if (i == 2 && m_tempCountArray[10] > 0)
                        {
                            m_currCountArray[i] -= 1;
                            m_currCountArray[10] -= 1;
                            WordPlateMeld meld = new WordPlateMeld(new WordPlateTile(descType, (PlateNumType)10), new WordPlateTile(descType, (PlateNumType)i));
                            List<WordPlateMeld> meldList = new List<WordPlateMeld>();
                            meldList.Add(meld);
                            if (GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf))
                            {
                                m_tempCountArray.CopyTo(m_currCountArray, 0);
                                wordPlateMeldList.Add(meldList);
                            }
                            else
                            {
                                meldList.Remove(meld);
                                m_currCountArray[i] += 1;
                                m_currCountArray[10] += 1;
                            }
                        }
                        if (i == 7 && m_tempCountArray[10] > 0)
                        {
                            m_currCountArray[i] -= 1;
                            m_currCountArray[10] -= 1;
                            WordPlateMeld meld = new WordPlateMeld(new WordPlateTile(descType, (PlateNumType)10), new WordPlateTile(descType, (PlateNumType)i));
                            List<WordPlateMeld> meldList = new List<WordPlateMeld>();
                            meldList.Add(meld);
                            if (GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf))
                            {
                                m_tempCountArray.CopyTo(m_currCountArray, 0);
                                wordPlateMeldList.Add(meldList);
                            }
                            else
                            {
                                meldList.Remove(meld);
                                m_currCountArray[i] += 1;
                                m_currCountArray[10] += 1;
                            }
                        }
                        if (i < m_tempCountArray.Length - 1 && m_tempCountArray[i + 1] > 0)
                        {
                            m_currCountArray[i] -= 1;
                            m_currCountArray[i + 1] -= 1;
                            WordPlateMeld meld = new WordPlateMeld(new WordPlateTile(descType, (PlateNumType)i + 1), new WordPlateTile(descType, (PlateNumType)i));
                            List<WordPlateMeld> meldList = new List<WordPlateMeld>();
                            meldList.Add(meld);
                            if (GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf))
                            {
                                m_tempCountArray.CopyTo(m_currCountArray, 0);
                                wordPlateMeldList.Add(meldList);
                            }
                            else
                            {
                                meldList.Remove(meld);
                                m_currCountArray[i] += 1;
                                m_currCountArray[i + 1] += 1;
                            }
                        }
                        if (i < m_tempCountArray.Length - 2 && m_tempCountArray[i + 2] > 0)
                        {
                            m_currCountArray[i] -= 1;
                            m_currCountArray[i + 2] -= 1;
                            WordPlateMeld meld = new WordPlateMeld(new WordPlateTile(descType, (PlateNumType)i + 2), new WordPlateTile(descType, (PlateNumType)i));
                            List<WordPlateMeld> meldList = new List<WordPlateMeld>();
                            meldList.Add(meld);
                            if (GetWordPlateMeldTo3Dot(descType, meldList, huTile, bMySelf))
                            {
                                m_tempCountArray.CopyTo(m_currCountArray, 0);
                                wordPlateMeldList.Add(meldList);
                            }
                            else
                            {
                                meldList.Remove(meld);
                                m_currCountArray[i] += 1;
                                m_currCountArray[i + 2] += 1;
                            }
                        }
                    }
                }
            }
            if (wordPlateMeldList.Count > 0)
            {
                return true;
            }
            return false;
        }
        protected override List<List<WordPlateTile>> GetTileMeld(PlateDescType descType)
        {
            List<List<WordPlateTile>> _tilesList = new List<List<WordPlateTile>>();
            //先获取4个
            for (int i = 1; i < m_currCountArray.Length; ++i)
            {
                if (m_currCountArray[i] == 4)
                {
                    List<WordPlateTile> tempTileList = new List<WordPlateTile>();
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    _tilesList.Add(tempTileList);
                    m_currCountArray[i] -= 4;
                }
            }
            //再获取三个
            for (int i = 1; i < m_currCountArray.Length; ++i)
            {
                if (m_currCountArray[i] == 3)
                {
                    List<WordPlateTile> tempTileList = new List<WordPlateTile>();
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    _tilesList.Add(tempTileList);
                    m_currCountArray[i] -= 3;
                }
            }
            //再获取二个
            for (int i = 1; i < m_currCountArray.Length; ++i)
            {
                if (m_currCountArray[i] == 2)
                {
                    List<WordPlateTile> tempTileList = new List<WordPlateTile>();
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    _tilesList.Add(tempTileList);
                    m_currCountArray[i] -= 2;
                }
            }
            //再获取顺子(优先取2-7-10)
            if (m_currCountArray[2] > 0 && m_currCountArray[7] > 0 && m_currCountArray[10] > 0)
            {
                List<WordPlateTile> tempTileList = new List<WordPlateTile>();
                tempTileList.Add(new WordPlateTile(descType, (PlateNumType)10));
                tempTileList.Add(new WordPlateTile(descType, (PlateNumType)7));
                tempTileList.Add(new WordPlateTile(descType, (PlateNumType)2));
                _tilesList.Add(tempTileList);
                m_currCountArray[2] -= 1;
                m_currCountArray[7] -= 1;
                m_currCountArray[10] -= 1;
            }
            for (int i = 1; i < m_currCountArray.Length; ++i)
            {
                if (m_currCountArray[i] > 0 && i < m_currCountArray.Length - 2 && m_currCountArray[i + 1] > 0 && m_currCountArray[i + 2] > 0)
                {
                    List<WordPlateTile> tempTileList = new List<WordPlateTile>();
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i + 2));
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i + 1));
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    _tilesList.Add(tempTileList);
                    m_currCountArray[i] -= 1;
                    m_currCountArray[i + 1] -= 1;
                    m_currCountArray[i + 2] -= 1;
                }
            }
            //再取两个关联的
            for (int i = 1; i < m_currCountArray.Length; ++i)
            {
                if (m_currCountArray[i] > 0)
                {
                    int oneIndex = 0;
                    int twoIndex = 0;
                    if (i == 2 && (m_currCountArray[7] > 0 || m_currCountArray[10] > 0))
                    {
                        oneIndex = 7;
                        twoIndex = 10;
                    }
                    else if (i == 7 && m_currCountArray[10] > 0)
                    {
                        oneIndex = 10;
                    }
                    else if (i < m_currCountArray.Length - 1 && m_currCountArray[i + 1] > 0)
                    {
                        oneIndex = i + 1;
                    }
                    else if (i < m_currCountArray.Length - 2 && m_currCountArray[i + 2] > 0)
                    {
                        twoIndex = i + 2;
                    }
                    else
                    {
                        continue;
                    }
                    List<WordPlateTile> tempTileList = new List<WordPlateTile>();
                    if (m_currCountArray[twoIndex] > 0)
                    {
                        tempTileList.Add(new WordPlateTile(descType, (PlateNumType)twoIndex));
                        m_currCountArray[twoIndex] -= 1;
                    }
                    if (m_currCountArray[oneIndex] > 0)
                    {
                        tempTileList.Add(new WordPlateTile(descType, (PlateNumType)oneIndex));
                        m_currCountArray[oneIndex] -= 1;
                    }
                    tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    _tilesList.Add(tempTileList);
                    m_currCountArray[i] -= 1;
                }
            }
            //多余的(单牌叠加在一起，不会超过4张)
            List<WordPlateTile> _tempTileList = new List<WordPlateTile>();
            for (int i = 1; i < m_currCountArray.Length; ++i)
            {
                if (m_currCountArray[i] > 0)
                {
                    _tempTileList.Add(new WordPlateTile(descType, (PlateNumType)i));
                    m_currCountArray[i] -= 1;
                }
            }
            if (_tempTileList.Count > 0)
            {
                _tilesList.Add(_tempTileList);
            }
            return _tilesList;
        }
        /// <summary>
        /// 获取胡牌接口
        /// </summary>
        /// <param name="handTileList"></param> 当前手牌
        /// <param name="meldList"></param>     当前操作牌组
        /// <param name="baseScore"></param>    当前胡牌基础分
        /// <param name="bOperatHu"></param>    当前是否为操作之后胡牌（歪 碰 吃之后胡牌）
        /// <param name="huTile"></param>       当前胡牌的牌
        /// <param name="bMySelf"></param>      是否是自己摸牌
        /// <returns></returns>
        public override List<WordPlateHuMeld> GetHuMeld(List<WordPlateTile> handTileList, List<WordPlateMeld> meldList, int baseScore, bool bFamous, bool bOperatHu = false, WordPlateTile huTile = null, bool bMySelf = false)
        {
            List<WordPlateHuMeld> huMeldList = new List<WordPlateHuMeld>();
            List<WordPlateTile> _handTileList = new List<WordPlateTile>();
            _handTileList.AddRange(handTileList);
            if (huTile != null && _handTileList.Count % 3 == 1)
            {
                _handTileList.Add(huTile);
            }
            if (_handTileList.Count % 3 != 2)
            {
                return huMeldList;
            }
            _handTileList.Sort(CompareTile);
            // 统计数量.
            if (!AnalyseHandTile(_handTileList))
            {
                return huMeldList;
            }
            //统计pairs
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
                return huMeldList;
            }
            //Step2:确认所有牌组是否都符合3*n + 2 或者 3×n的组合
            List<WordPlateMeld> _meldList = new List<WordPlateMeld>();
            List<List<WordPlateMeld>> wordPlateMeldList = new List<List<WordPlateMeld>>();
            bool bBig = false;
            if (m_BigTileList.Count % 3 == 0)
            {
                bBig = GetHandWordPlateToMeld(m_BigTileList, _meldList, huTile, bMySelf);
            }
            else
            {
                bBig = GetWordPlateMeldToPair(m_BigTileList, wordPlateMeldList, huTile, bMySelf);
            }
            bool bSmall = false;
            if (m_SmallTileList.Count % 3 == 0)
            {
                bSmall = GetHandWordPlateToMeld(m_SmallTileList, _meldList, huTile, bMySelf);
            }
            else
            {
                bSmall = GetWordPlateMeldToPair(m_SmallTileList, wordPlateMeldList, huTile, bMySelf);
            }
            if (!bBig || !bSmall)
            {
                return huMeldList;
            }
            List<WordPlateHuSorceMeld> wordPlateHuSorceMeldList = new List<WordPlateHuSorceMeld>();
            wordPlateMeldList.ForEach(eMeldList =>
            {
                List<WordPlateMeld> _huMeldList = new List<WordPlateMeld>();
                _huMeldList.AddRange(_meldList);
                _huMeldList.AddRange(eMeldList);
                int score = GetWordPlateMeldScore(meldList, _huMeldList, baseScore, bFamous);
                if (score > 0 && score >= baseScore)
                {
                    wordPlateHuSorceMeldList.Add(new WordPlateHuSorceMeld(score, _huMeldList));
                }
            });
            if (wordPlateHuSorceMeldList.Count == 0)
            {
                return huMeldList;
            }
            bool bMax = false;
            List<WordPlateMeld> handMeldList = new List<WordPlateMeld>();
            if (wordPlateHuSorceMeldList.Count >= 1)
            {
                int maxSorce = wordPlateHuSorceMeldList.Max(e => e.maxSorce);
                WordPlateHuSorceMeld sorceMeld = wordPlateHuSorceMeldList.FirstOrDefault(e => e.maxSorce == maxSorce);
                if (sorceMeld != null)
                {
                    handMeldList.AddRange(sorceMeld.huMeldList);
                    bMax = true;
                }
            }
            if (!bMax)
            {
                handMeldList.AddRange(wordPlateHuSorceMeldList[0].huMeldList);
            }
            //统计操作牌
            bool bhuFlag = false;
            int meldCount = 0;
            meldList.ForEach(meld =>
            {
                int score = 0;
                bool bHuMeld = false;
                PlateMeldType meldType = meld.m_eMeldType;
                if (meld.m_eMeldType == PlateMeldType.EPM_Wai || meld.m_eMeldType == PlateMeldType.EPM_Slip)
                {
                    //歪 溜
                    score = 4;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pong || meld.m_eMeldType == PlateMeldType.EPM_Flutter)
                {
                    //碰 飘
                    score = 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Sequence)
                {
                    meldType = PlateMeldType.EPM_Chow;
                    if (!meld.m_meldTileList.Exists(element => !element.IsRed()))
                    {
                        //吃 2-7-10
                        score = 1;
                    }
                }
                meldCount++;
                if (bOperatHu && meldCount == meldList.Count && !bhuFlag && huTile != null && meld.m_meldTileList.Exists(element => element.Equal(huTile)))
                {
                    //先歪 碰 吃后再点胡（肯定是最后一个牌组是胡牌牌组）
                    bhuFlag = true;
                    bHuMeld = true;
                }
                huMeldList.Add(new WordPlateHuMeld(meldType, meld.m_meldTileList, score, bHuMeld));
            });
            //统计手牌
            if (!bhuFlag && huTile != null)
            {
                //排序
                handMeldList.Sort(CompareTileByRevMeld);
            }
            handMeldList.ForEach(meld =>
            {
                int score = 0;
                bool bHuMeld = false;
                if (meld.m_eMeldType == PlateMeldType.EPM_Wai)
                {
                    //歪
                    score = 4;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pong)
                {
                    //碰
                    score = 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Sequence && !meld.m_meldTileList.Exists(element => !element.IsRed()))
                {
                    //顺子 2-7-10
                    score = 1;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Triplet)
                {
                    //坎
                    score = 3;
                }
                else if (meld.m_eMeldType == PlateMeldType.EPM_Pair && meld.m_meldTileList.Count == 2 &&
                (meld.m_meldTileList[0].Equal(meld.m_meldTileList[1]) || !meld.m_meldTileList.Exists(element => !element.IsRed())))
                {
                    //对子或者红牌
                    score = 1;
                }
                if (!bOperatHu && !bhuFlag && huTile != null && meld.m_eMeldType != PlateMeldType.EPM_Triplet && meld.m_meldTileList.Exists(element => element.Equal(huTile)))
                {
                    bhuFlag = true;
                    bHuMeld = true;
                }
                huMeldList.Add(new WordPlateHuMeld(meld.m_eMeldType, meld.m_meldTileList, score, bHuMeld));
            });

            return huMeldList;
        }
    }
}
#endif