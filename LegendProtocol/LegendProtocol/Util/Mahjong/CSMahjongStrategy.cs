#if MAHJONG
using System.Linq;
using System.Collections.Generic;
using System;

// 长沙麻将的逻辑策略类，包括了所有牌型的基本操作.吃。碰。杠。胡。算番等。不同打发若有不同。继承此类重写即可.
// 作为一个单例.一种玩法有一个策略即可.
namespace LegendProtocol
{
    public class CSMahjongSetTile : MahjongSetTileBase
    {
        public CSMahjongSetTile()
        {
            mahjongType = MahjongType.ChangShaMahjong;
            Init();
        }   
        
        /// <summary>
        /// 初始化长沙麻将
        /// </summary>
        private void Init()
        {
            //删除长沙麻将不用的牌(只要索筒万)
            m_allTileList.RemoveAll(element => !element.IsSuit());
        }
    }
    public class CSMahjongStrategy : MahjongStrategyBase
    {
        private bool m_bIsUse258Pairs = true;
        public CSMahjongStrategy()
        {
            mahjongType = MahjongType.ChangShaMahjong;
            Init();
        }

        /// <summary>
        /// 初始化长沙麻将
        /// </summary>
        private void Init()
        {
            //可以吃
            m_bCanChow = true;
        }

        /// <summary>
        /// 如果是清一色.或者全求人.碰碰胡.可以乱将(每次手牌变更需要判定一次)
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="meldList"></param>
        /// <returns></returns>
        public void CheckUse258Pairs(List<MahjongTile> tileList, List<Meld> meldList)
        {
            m_bIsUse258Pairs = false;

            //碰碰胡
            if (!AnalyseBumperWin(tileList, meldList))
            {
                m_bIsUse258Pairs = true;
            }

            // 全球人
            if (meldList.Count == 4)
            {
                m_bIsUse258Pairs = false;
            }

            // 清一色.
            if (m_bIsUse258Pairs)
            {
                TileDescType targetDescType = tileList[0].GetDescType();
                if (!tileList.Exists(element => element.GetDescType() != targetDescType) && !meldList.Exists(element => element.GetMahjongTileByIndex(0).GetDescType() != targetDescType))
                {
                    m_bIsUse258Pairs = false;
                }
            }
        }

        /// <summary>
        /// 重写父类接口.确认是否乱将便可以.
        /// </summary>
        /// <param name="tilesList"></param>
        /// <param name="bTriplet"></param>
        /// <returns></returns>
        public override bool _DoAnalyseTils(List<MahjongTile> tilesList, bool bTriplet = false)
        {
            // 不需要258做将 直接用base的方式.
            if (!m_bIsUse258Pairs)
            {
                return base._DoAnalyseTils(tilesList, bTriplet);
            }

            Array.Clear(m_tempCountArray, 0, m_tempCountArray.Length);
            Array.Clear(m_currCountArray, 0, m_currCountArray.Length);

            if (tilesList.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < tilesList.Count; ++i)
            {
                // 单纯为了风牌和箭牌的索引不越界
                int index = (bTriplet && (int)tilesList[i].GetNumType() > MahjongConstValue.MAXINDEX) ? (int)tilesList[i].GetNumType() - MahjongConstValue.MAXINDEX : (int)tilesList[i].GetNumType();
                m_tempCountArray[index]++;
                m_currCountArray[index]++;
            }

            if (tilesList.Count % 3 == 2)
            {
                // 索引从1开始对应数字的牌的枚举(除去将牌之后判定剩余牌是否为3n) 需要258做将
                for (int i = 1; i < m_tempCountArray.Length; ++i)
                {
                    if (m_currCountArray[i] >= 2 && (i == 2 || i == 5 || i == 8))
                    {
                        m_currCountArray[i] -= 2;
                        if (_Is3DotN(bTriplet))
                        {
                            return true;
                        }
                        m_currCountArray[i] += 2;
                    }
                }
            }
            else if (tilesList.Count % 3 == 0)
            {
                if (_Is3DotN(bTriplet))
                {
                    return true;
                }
            }

            return false;
        }

        //碰碰胡
        private bool AnalyseBumperWin(List<MahjongTile> handTileList, List<Meld> meldList)
        {
            if (meldList.Exists(element => element.m_eMeldType == MeldType.EM_Sequence))
            {
                return false;
            }

            return base.AnalyseHandTile(handTileList, meldList, true, false, false, true);
        }

        // 返回胡牌类型
        public override CSSpecialWinType WinHandTileCheck(List<MahjongTile> handTileList, List<Meld> meldList, bool b7Pairs = false, bool bFakeHu = false)
        {
            //判断要不要将
            CheckUse258Pairs(handTileList, meldList);

            CSSpecialWinType winType = SpecialWinCheck(handTileList, meldList);
            if (winType != CSSpecialWinType.WT_None)
            {
                return winType;
            }
            if (base.AnalyseHandTile(handTileList, meldList))
            {
                return CSSpecialWinType.WT_PingWin;
            }
            else if (bFakeHu)
            {
                m_bIsUse258Pairs = false;
                if (base.AnalyseHandTile(handTileList, meldList))
                {
                    return CSSpecialWinType.WT_FakeHu;
                }
            }
            return CSSpecialWinType.WT_None;
        }

        /// <summary>
        /// 特殊胡牌听牌检测
        /// </summary>
        /// <param name="handTileList"></param>
        /// <returns></returns>
        public CSSpecialWinType SpecialWinCheck(List<MahjongTile> handTileList, List<Meld> meldList)
        {
            CSSpecialWinType eSWType = CSSpecialWinType.WT_None;

            if (handTileList.Count % 3 != 2)
            {
                return eSWType;
            }
            // 小七对
            bool b7Pairs = false;
            if (base.SpecialAnglyseTile(handTileList))
            {
                eSWType |= CSSpecialWinType.WT_7Pairs;
                b7Pairs = true;
                List<MahjongTile> _tempTileList = ConcealedKongCheck(handTileList);
                //  可以暗杠的列表
                if (_tempTileList.Count > 0)
                {
                    eSWType |= CSSpecialWinType.WT_Luxury7Pairs;
                    if (_tempTileList.Count > 1)
                    {
                        eSWType |= CSSpecialWinType.WT_DLuxury7Pairs;
                    }
                }
            }

            // 将将胡 清一色
            bool bAllEyes = true;
            bool bSingleType = true;
            TileDescType oldDescType = handTileList[0].GetDescType();
            if (handTileList.Exists(element => element.GetDescType() != oldDescType))
            {
                bSingleType = false;
            }
            if (handTileList.Exists(element => (element.GetNumType() != TileNumType.ETN_Two && element.GetNumType() != TileNumType.ETN_Five && element.GetNumType() != TileNumType.ETN_Eight)))
            {
                bAllEyes = false;
            }
            if (bSingleType || bAllEyes)
            {
                for (int i = 0; i < meldList.Count; ++i)
                {
                    if (bSingleType)
                    {
                        bSingleType = !meldList[i].m_meldTileList.Exists(element => element.GetDescType() != oldDescType);
                    }
                    
                    if (bAllEyes)
                    {
                        bAllEyes = !meldList[i].m_meldTileList.Exists(element => element.GetNumType() != TileNumType.ETN_Two && element.GetNumType() != TileNumType.ETN_Five && element.GetNumType() != TileNumType.ETN_Eight);
                    }

                    if (!bSingleType && !bAllEyes)
                    {
                        break;
                    }
                }
            }

            // 将将胡
            if (bAllEyes)
            {
                eSWType |= CSSpecialWinType.WT_AllEyes;
            }

            // 清一色
            if (bSingleType && (b7Pairs || base.AnalyseHandTile(handTileList, meldList)))
            {
                eSWType |= CSSpecialWinType.WT_SingleType;
            }

            // 全求人.
            if (meldList.Count == 4)
            {
                //if (!meldList.Exists(element => element.m_eMeldType == MeldType.EM_ConcealedKong) && handTileList.Count == 2 && (bAllEyes || handTileList[0].Equal(handTileList[1])))
                //if (handTileList.Count == 2 && (bAllEyes || handTileList[0].Equal(handTileList[1])))
                if (handTileList.Count == 2 && handTileList[0].Equal(handTileList[1]))
                {
                    eSWType |= CSSpecialWinType.WT_AllMelds;
                }
            }

            // 碰碰胡
            if (AnalyseBumperWin(handTileList, meldList))
            {
                eSWType |= CSSpecialWinType.WT_BumperWin;
            }

            return eSWType;
        }
        /// <summary>
        /// 听牌检测
        /// </summary>
        /// <param name="handTileList"></param>
        /// <returns></returns>
        public bool SpecialWinReadHandCheck(List<MahjongTile> handTileList, List<Meld> meldList, bool b7Pairs, bool bFakeHu)
        {
            if (handTileList.Count % 3 == 2)
            {
                return false;
            }
            List<MahjongTile> _ReadyHandTileList = new List<MahjongTile>();
            List<MahjongTile> _tempTileList = ConcealedKongCheck(handTileList);
            //筒
            for (int i = 0; i < MahjongOriginalManager.Instance.m_DotTotalTiles.Length; ++i)
            {
                // 如果这张牌可以暗杠.那这张牌的判定没有意义.
                if (_tempTileList.Count > 0 && _tempTileList.Exists(elmenet => elmenet.Equal(MahjongOriginalManager.Instance.m_DotTotalTiles[i])))
                {
                    continue;
                }
                MahjongTile tile = new MahjongTile(MahjongOriginalManager.Instance.m_DotTotalTiles[i]);
                handTileList.Add(tile);
                handTileList.Sort(CompareTile);
                if (CSSpecialWinType.WT_None != WinHandTileCheck(handTileList, meldList, b7Pairs, bFakeHu))
                {
                    _ReadyHandTileList.Add(tile);
                }
                handTileList.Remove(tile);
            }
            //条
            for (int i = 0; i < MahjongOriginalManager.Instance.m_BambooTotalTiles.Length; ++i)
            {
                // 如果这张牌可以暗杠.那这张牌的判定没有意义.
                if (_tempTileList.Count > 0 && _tempTileList.Exists(elmenet => elmenet.Equal(MahjongOriginalManager.Instance.m_BambooTotalTiles[i])))
                {
                    continue;
                }
                MahjongTile tile = new MahjongTile(MahjongOriginalManager.Instance.m_BambooTotalTiles[i]);
                handTileList.Add(tile);
                handTileList.Sort(CompareTile);
                if (CSSpecialWinType.WT_None != WinHandTileCheck(handTileList, meldList, b7Pairs, bFakeHu))
                {
                    _ReadyHandTileList.Add(tile);
                }
                handTileList.Remove(tile);
            }
            //万
            for (int i = 0; i < MahjongOriginalManager.Instance.m_CharacterTotalTiles.Length; ++i)
            {
                // 如果这张牌可以暗杠.那这张牌的判定没有意义.
                if (_tempTileList.Count > 0 && _tempTileList.Exists(elmenet => elmenet.Equal(MahjongOriginalManager.Instance.m_CharacterTotalTiles[i])))
                {
                    continue;
                }
                MahjongTile tile = new MahjongTile(MahjongOriginalManager.Instance.m_CharacterTotalTiles[i]);
                handTileList.Add(tile);
                handTileList.Sort(CompareTile);
                if (CSSpecialWinType.WT_None != WinHandTileCheck(handTileList, meldList, b7Pairs, bFakeHu))
                {
                    _ReadyHandTileList.Add(tile);
                }
                handTileList.Remove(tile);
            }
            if (_ReadyHandTileList.Count > 0)
            {
                return true;
            }
            return false;
        }
        // 手牌分析.刚抓到一张牌之后做手牌分析.也可为服务器发牌之后第一次数据分析.（闲家第一次只分析是否听牌 看麻将玩法是否有此需求.如没有听牌需求 不必要调用此.听牌判定比胡牌判定慢很多.）
        public override bool AnalyseHandTile(List<MahjongTile> handTileList, List<Meld> meldList, bool b7Pairs = false, bool bFakeHu = false, bool bReadHand = false, bool bTriplet = false)
        {
            if (handTileList.Count % 3 == 2 && bReadHand)
            {
                return false;
            }
            if (bReadHand)
            {
                return SpecialWinReadHandCheck(handTileList, meldList, b7Pairs, bFakeHu);
            }

            return (CSSpecialWinType.WT_None != WinHandTileCheck(handTileList, meldList, b7Pairs, bFakeHu));
        }
        //长沙麻将判断杠后能否听牌
        public bool KongCheckByCSMahjong(List<MahjongTile> handTileList, List<Meld> meldList, MahjongTile targetTile, int count, bool bFakeHu)
        {
            if (targetTile == null || (count != 1 && count != 3 && count != 4))
            {
                return false;
            }
            List<MahjongTile> headMahjongList = new List<MahjongTile>();
            headMahjongList.AddRange(handTileList);
            if (count != headMahjongList.RemoveAll(element => element.Equal(targetTile)))
            {
                return false;
            }
            List<Meld> meldMahjongList = new List<Meld>();
            meldMahjongList.AddRange(meldList);
            if (count == 1 || count == 3)
            {
                if (count == 1)
                {
                    Meld meld = ExposedKongCheck(targetTile, meldMahjongList);
                    if (meld != null)
                    {
                        meldMahjongList.Remove(meld);
                    }
                }
                meldMahjongList.Add(Kong(new MahjongTile(targetTile), new MahjongTile(targetTile), new MahjongTile(targetTile), targetTile, true));
            }
            else if (count == 4)
            {
                meldMahjongList.Add(Kong(new MahjongTile(targetTile), new MahjongTile(targetTile), new MahjongTile(targetTile), targetTile, false));
            }
            return AnalyseHandTile(headMahjongList, meldMahjongList, true, bFakeHu, true);
        }

        /// <summary>
        /// 起手需要判定的牌型
        /// </summary>
        public CSStartDisplayType StartDisplay(List<MahjongTile> handTile, List<DisplayMahjongNode> displayMahjongList, bool bPersonalisePendulum)
        {
            if (handTile.Count == 0)
            {
                return CSStartDisplayType.SDT_None;
            }
            displayMahjongList.Clear();

            if (bPersonalisePendulum)
            {
                //个性摆牌
                return PersonaliseStartDisplay(handTile, displayMahjongList);
            }

            return StartDisplay(handTile, displayMahjongList);
        }
        /// <summary>
        /// 起手需要判定的牌型.1.缺一色.2.四喜.3.无将.4.六顺
        /// </summary>
        public CSStartDisplayType StartDisplay(List<MahjongTile> handTile, List<DisplayMahjongNode> displayMahjongList)
        {
            CSStartDisplayType eSDType = CSStartDisplayType.SDT_None;

            List<MahjongTile> _DragonTileList = new List<MahjongTile>();
            List<MahjongTile> _WindTileList = new List<MahjongTile>();
            List<MahjongTile> _DotTileList = new List<MahjongTile>();
            List<MahjongTile> _BambooTileList = new List<MahjongTile>();
            List<MahjongTile> _CharacterTileList = new List<MahjongTile>();

            for (int i = 0; i < handTile.Count; ++i)
            {
                MahjongTile tile = handTile[i];
                switch (tile.GetDescType())
                {
                    case TileDescType.ETD_Dragon:
                        _DragonTileList.Add(tile);
                        break;
                    case TileDescType.ETD_Wind:
                        _WindTileList.Add(tile);
                        break;
                    case TileDescType.ETD_Dot:
                        _DotTileList.Add(tile);
                        break;
                    case TileDescType.ETD_Bamboo:
                        _BambooTileList.Add(tile);
                        break;
                    case TileDescType.ETD_Character:
                        _CharacterTileList.Add(tile);
                        break;
                    default:
                        return eSDType;
                }
            }

            int missCount = 0;

            if (_DotTileList.Count == 0)
            {
                missCount++;
            }

            if (_BambooTileList.Count == 0)
            {
                missCount++;
            }

            if (_CharacterTileList.Count == 0)
            {
                missCount++;
            }

            if (missCount >= 1)
            {
                eSDType |= CSStartDisplayType.SDT_MissOneType;
            }

            bool bHas258 = false;

            for (int i = 0; i < handTile.Count; ++i)
            {
                TileNumType numType = handTile[i].GetNumType();
                if (numType == TileNumType.ETN_Two || numType == TileNumType.ETN_Five || numType == TileNumType.ETN_Eight)
                {
                    bHas258 = true;
                    break;
                }
            }

            if (!bHas258)
            {
                eSDType |= CSStartDisplayType.SDT_NoPairs;
            }

            List<MahjongTile> _fourTileList = ConcealedKongCheck(handTile);
            //  添加到可以暗杠的列表
            if (_fourTileList.Count > 0)
            {
                if (_fourTileList.Count >= 1)
                {
                    eSDType |= CSStartDisplayType.SDT_ConcealedKongOne;
                    DisplayMahjongNode newNode = new DisplayMahjongNode(CSStartDisplayType.SDT_ConcealedKongOne);
                    newNode.mahjongTileList.Add(_fourTileList[0]);
                    newNode.mahjongTileList.Add(_fourTileList[0]);
                    newNode.mahjongTileList.Add(_fourTileList[0]);
                    newNode.mahjongTileList.Add(_fourTileList[0]);
                    displayMahjongList.Add(newNode);
                }
                if (_fourTileList.Count >= 2)
                {
                    eSDType |= CSStartDisplayType.SDT_ConcealedKongTwo;
                    DisplayMahjongNode newNode = new DisplayMahjongNode(CSStartDisplayType.SDT_ConcealedKongTwo);
                    newNode.mahjongTileList.Add(_fourTileList[1]);
                    newNode.mahjongTileList.Add(_fourTileList[1]);
                    newNode.mahjongTileList.Add(_fourTileList[1]);
                    newNode.mahjongTileList.Add(_fourTileList[1]);
                    displayMahjongList.Add(newNode);
                }
                if (_fourTileList.Count == 3)
                {
                    eSDType |= CSStartDisplayType.SDT_ConcealedKongThree;
                    DisplayMahjongNode newNode = new DisplayMahjongNode(CSStartDisplayType.SDT_ConcealedKongThree);
                    newNode.mahjongTileList.Add(_fourTileList[2]);
                    newNode.mahjongTileList.Add(_fourTileList[2]);
                    newNode.mahjongTileList.Add(_fourTileList[2]);
                    newNode.mahjongTileList.Add(_fourTileList[2]);
                    displayMahjongList.Add(newNode);
                }
            }

            List<MahjongTile> _threeTileList = ConcealedKongCheck(handTile, 3);
            //  添加到可以明杠的列表
            if (_threeTileList.Count > 0)
            {
                if (_threeTileList.Count >= 2)
                {
                    eSDType |= CSStartDisplayType.SDT_DoubleTripletOne;
                    DisplayMahjongNode newNode = new DisplayMahjongNode(CSStartDisplayType.SDT_DoubleTripletOne);
                    newNode.mahjongTileList.Add(_threeTileList[0]);
                    newNode.mahjongTileList.Add(_threeTileList[0]);
                    newNode.mahjongTileList.Add(_threeTileList[0]);
                    newNode.mahjongTileList.Add(_threeTileList[1]);
                    newNode.mahjongTileList.Add(_threeTileList[1]);
                    newNode.mahjongTileList.Add(_threeTileList[1]);
                    displayMahjongList.Add(newNode);
                }
                if (_threeTileList.Count == 4)
                {
                    eSDType |= CSStartDisplayType.SDT_DoubleTripletTwo;
                    DisplayMahjongNode newNode = new DisplayMahjongNode(CSStartDisplayType.SDT_DoubleTripletTwo);
                    newNode.mahjongTileList.Add(_threeTileList[2]);
                    newNode.mahjongTileList.Add(_threeTileList[2]);
                    newNode.mahjongTileList.Add(_threeTileList[2]);
                    newNode.mahjongTileList.Add(_threeTileList[3]);
                    newNode.mahjongTileList.Add(_threeTileList[3]);
                    newNode.mahjongTileList.Add(_threeTileList[3]);
                    displayMahjongList.Add(newNode);
                }
            }

            return eSDType;
        }
        /// <summary>
        /// 起手需要判定的牌型.1.缺一色.2.一枝花.3.无将.4.三同.5.节节高.6.六顺.7.四喜
        /// </summary>
        public CSStartDisplayType PersonaliseStartDisplay(List<MahjongTile> handTile, List<DisplayMahjongNode> displayMahjongList)
        {
            CSStartDisplayType eSDType = CSStartDisplayType.SDT_None;

            List<MahjongCount> _MahjongCountList = new List<MahjongCount>();
            // 统计数量.
            List<MahjongTile> _DotTileList = new List<MahjongTile>();
            List<MahjongTile> _BambooTileList = new List<MahjongTile>();
            List<MahjongTile> _CharacterTileList = new List<MahjongTile>();
            int count = 1;
            int pairsCount = 0;
            MahjongTile newTile = handTile[0];
            for (int i = 0; i < handTile.Count; ++i)
            {
                switch (handTile[i].GetDescType())
                {
                    case TileDescType.ETD_Dot:
                        _DotTileList.Add(handTile[i]);
                        break;
                    case TileDescType.ETD_Bamboo:
                        _BambooTileList.Add(handTile[i]);
                        break;
                    case TileDescType.ETD_Character:
                        _CharacterTileList.Add(handTile[i]);
                        break;
                    default:
                        break;
                }
                if (handTile[i].IsPairs())
                {
                    pairsCount += 1;
                }
                if (i > 0)
                {
                    if (newTile.Equal(handTile[i]))
                    {
                        count++;
                    }
                    else
                    {
                        if (count >= 2)
                        {
                            _MahjongCountList.Add(new MahjongCount(newTile, count));
                        }
                        count = 1;
                        newTile = handTile[i];
                    }
                }
            }
            if (count >= 2)
            {
                _MahjongCountList.Add(new MahjongCount(newTile, count));
            }
            int missCount = 0;
            int fiveDotCount = 0;
            if (_DotTileList.Count == 0)
            {
                missCount++;
            }
            else
            {
                fiveDotCount = _DotTileList.Count(element => element.GetNumType() == TileNumType.ETN_Five);
            }
            int fiveBambooCount = 0;
            if (_BambooTileList.Count == 0)
            {
                missCount++;
            }
            else
            {
                fiveBambooCount = _BambooTileList.Count(element => element.GetNumType() == TileNumType.ETN_Five);
            }
            int fiveCharacterCount = 0;
            if (_CharacterTileList.Count == 0)
            {
                missCount++;
            }
            else
            {
                fiveCharacterCount = _CharacterTileList.Count(element => element.GetNumType() == TileNumType.ETN_Five);
            }
            //起手胡
            if (missCount >= 1)
            {
                eSDType |= CSStartDisplayType.SDT_MissOneType;
            }
            if (pairsCount == 0)
            {
                eSDType |= CSStartDisplayType.SDT_NoPairs;
            }
            else if ((_DotTileList.Count == 1 && fiveDotCount == 1) || (_BambooTileList.Count == 1 && fiveBambooCount == 1) || (_CharacterTileList.Count == 1 && fiveCharacterCount == 1) ||
                (pairsCount == 1 && (fiveDotCount == 1 || fiveBambooCount == 1 || fiveCharacterCount == 1)))
            {
                eSDType |= CSStartDisplayType.SDT_AFlower;
            }
            //三同
            if (missCount == 0 && _MahjongCountList.Count >= 3)
            {
                eSDType = ThreeSame(eSDType, displayMahjongList, _MahjongCountList);
            }
            //节节高
            if (_MahjongCountList.Count >= 3)
            {
                eSDType = SteadilyHigh(eSDType, displayMahjongList, _MahjongCountList);
            }
            //六六顺
            if (_MahjongCountList.Count >= 2)
            {
                eSDType = DoubleTriplet(eSDType, displayMahjongList, _MahjongCountList);
            }
            //四喜
            MahjongCount fourMahjong = _MahjongCountList.Find(element => element.count == 4);
            if (fourMahjong != null)
            {
                eSDType |= CSStartDisplayType.SDT_ConcealedKongOne;
                DisplayMahjongNode newNode = new DisplayMahjongNode(CSStartDisplayType.SDT_ConcealedKongOne);
                newNode.mahjongTileList.Add(fourMahjong.mahjongTile);
                newNode.mahjongTileList.Add(fourMahjong.mahjongTile);
                newNode.mahjongTileList.Add(fourMahjong.mahjongTile);
                newNode.mahjongTileList.Add(fourMahjong.mahjongTile);
                displayMahjongList.Add(newNode);
            }

            return eSDType;
        }
        private CSStartDisplayType ThreeSame(CSStartDisplayType eSDType, List<DisplayMahjongNode> displayMahjongList, List<MahjongCount> mahjongCountList)
        {
            Array.Clear(m_tempCountArray, 0, m_tempCountArray.Length);
            Array.Clear(m_currCountArray, 0, m_currCountArray.Length);

            mahjongCountList.ForEach(mahjongCount =>
            {
                if (mahjongCount.mahjongTile.GetNumType() <= TileNumType.ETN_Nine)
                {
                    int index = (int)mahjongCount.mahjongTile.GetNumType();
                    m_tempCountArray[index]++;
                    m_currCountArray[index]++;
                }
            });
            int tempCount = 1;
            for (int i = 1; i < m_tempCountArray.Length; ++i)
            {
                if (m_tempCountArray[i] == 3)
                {
                    CSStartDisplayType _tempSDType = CSStartDisplayType.SDT_None;
                    if (tempCount == 1)
                    {
                        _tempSDType = CSStartDisplayType.SDT_ThreeSameOne;
                    }
                    else if (tempCount == 2)
                    {
                        _tempSDType = CSStartDisplayType.SDT_ThreeSameTwo;
                    }
                    if (_tempSDType != CSStartDisplayType.SDT_None)
                    {
                        eSDType |= _tempSDType;
                        DisplayMahjongNode newNode = new DisplayMahjongNode(_tempSDType);
                        newNode.mahjongTileList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, (TileNumType)i));
                        newNode.mahjongTileList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, (TileNumType)i));
                        newNode.mahjongTileList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, (TileNumType)i));
                        newNode.mahjongTileList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, (TileNumType)i));
                        newNode.mahjongTileList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, (TileNumType)i));
                        newNode.mahjongTileList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, (TileNumType)i));
                        displayMahjongList.Add(newNode);
                        mahjongCountList.RemoveAll(element => element.mahjongTile.GetNumType() == (TileNumType)i);
                    }
                    tempCount++;
                }
            }

            return eSDType;
        }
        private CSStartDisplayType SteadilyHigh(CSStartDisplayType eSDType, List<DisplayMahjongNode> displayMahjongList, List<MahjongCount> mahjongCountList)
        {
            List<MahjongCount> twoMahjongCountList = mahjongCountList.FindAll(element => element.count == 2);
            if (twoMahjongCountList == null)
            {
                return eSDType;
            }
            List<MahjongTile> twoMahjongList = GetSteadilyHighMahjong(twoMahjongCountList);
            if (twoMahjongList.Count == 6)
            {
                return SetSteadilyHigh(eSDType, twoMahjongList, displayMahjongList, mahjongCountList);
            }
            List<MahjongCount> threeMahjongCountList = mahjongCountList.FindAll(element => element.count <= 3);
            if (threeMahjongCountList == null)
            {
                return eSDType;
            }
            List<MahjongTile> threeMahjongList = GetSteadilyHighMahjong(threeMahjongCountList);
            if (threeMahjongList.Count == 6)
            {
                return SetSteadilyHigh(eSDType, threeMahjongList, displayMahjongList, mahjongCountList);
            }
            List<MahjongTile> allMahjongList = GetSteadilyHighMahjong(mahjongCountList);
            if (allMahjongList.Count > threeMahjongList.Count && allMahjongList.Count > twoMahjongList.Count)
            {
                return SetSteadilyHigh(eSDType, allMahjongList, displayMahjongList, mahjongCountList);
            }
            else if (threeMahjongList.Count > twoMahjongList.Count)
            {
                return SetSteadilyHigh(eSDType, threeMahjongList, displayMahjongList, mahjongCountList);
            }
            else if (twoMahjongList.Count > 0)
            {
                return SetSteadilyHigh(eSDType, twoMahjongList, displayMahjongList, mahjongCountList);
            }
            return eSDType;
        }
        private List<MahjongTile> GetSteadilyHighMahjong(List<MahjongCount> mahjongCountList)
        {
            List<MahjongTile> twoMahjongList = new List<MahjongTile>();
            if (mahjongCountList.Count >= 3)
            {
                for (int i = 0; i < mahjongCountList.Count - 2; ++i)
                {
                    if (mahjongCountList[i].mahjongTile.GetDescType() == mahjongCountList[i + 1].mahjongTile.GetDescType() && mahjongCountList[i].mahjongTile.GetDescType() == mahjongCountList[i + 2].mahjongTile.GetDescType() &&
                       (mahjongCountList[i].mahjongTile.GetNumType() + 1) == mahjongCountList[i + 1].mahjongTile.GetNumType() && (mahjongCountList[i].mahjongTile.GetNumType() + 2) == mahjongCountList[i + 2].mahjongTile.GetNumType())
                    {
                        twoMahjongList.Add(mahjongCountList[i].mahjongTile);
                        twoMahjongList.Add(mahjongCountList[i + 1].mahjongTile);
                        twoMahjongList.Add(mahjongCountList[i + 2].mahjongTile);
                        i += 2;
                    }
                }
            }
            return twoMahjongList;
        }
        private CSStartDisplayType SetSteadilyHigh(CSStartDisplayType eSDType, List<MahjongTile> mahjongList, List<DisplayMahjongNode> displayMahjongList, List<MahjongCount> mahjongCountList)
        {
            if (mahjongList.Count == 0 || mahjongList.Count % 3 != 0)
            {
                return eSDType;
            }
            for (int i = 0; i < mahjongList.Count - 2; ++i)
            {
                CSStartDisplayType _tempSDType = CSStartDisplayType.SDT_None;
                if (i == 0)
                {
                    _tempSDType = CSStartDisplayType.SDT_SteadilyHighOne;
                }
                else if (i == 3)
                {
                    _tempSDType = CSStartDisplayType.SDT_SteadilyHighTwo;
                }
                if (_tempSDType != CSStartDisplayType.SDT_None)
                {
                    eSDType |= _tempSDType;
                    DisplayMahjongNode newNode = new DisplayMahjongNode(_tempSDType);
                    newNode.mahjongTileList.Add(mahjongList[i]);
                    newNode.mahjongTileList.Add(mahjongList[i]);
                    mahjongCountList.RemoveAll(element => element.mahjongTile.Equal(mahjongList[i]));
                    newNode.mahjongTileList.Add(mahjongList[i + 1]);
                    newNode.mahjongTileList.Add(mahjongList[i + 1]);
                    mahjongCountList.RemoveAll(element => element.mahjongTile.Equal(mahjongList[i + 1]));
                    newNode.mahjongTileList.Add(mahjongList[i + 2]);
                    newNode.mahjongTileList.Add(mahjongList[i + 2]);
                    mahjongCountList.RemoveAll(element => element.mahjongTile.Equal(mahjongList[i + 2]));
                    displayMahjongList.Add(newNode);
                    i += 2;
                }
            }
            return eSDType;
        }
        private CSStartDisplayType DoubleTriplet(CSStartDisplayType eSDType, List<DisplayMahjongNode> displayMahjongList, List<MahjongCount> mahjongCountList)
        {
            List<MahjongCount> allMahjongCountList = mahjongCountList.FindAll(element => element.count >= 3);
            if (allMahjongCountList == null || allMahjongCountList.Count < 2)
            {
                return eSDType;
            }
            if (allMahjongCountList.Count == 4)
            {
                return SetDoubleTriplet(eSDType, allMahjongCountList, displayMahjongList, mahjongCountList);
            }
            allMahjongCountList.Sort((nodeOne, nodeTwo) => { return (nodeOne.count >= nodeTwo.count) ? 1 : -1; });
            return SetDoubleTriplet(eSDType, allMahjongCountList, displayMahjongList, mahjongCountList);
        }
        private CSStartDisplayType SetDoubleTriplet(CSStartDisplayType eSDType, List<MahjongCount> allMahjongCountList, List<DisplayMahjongNode> displayMahjongList, List<MahjongCount> mahjongCountList)
        {
            if (allMahjongCountList != null && allMahjongCountList.Count >= 2)
            {
                if (allMahjongCountList.Count >= 2)
                {
                    eSDType |= CSStartDisplayType.SDT_DoubleTripletOne;
                    DisplayMahjongNode newNode = new DisplayMahjongNode(CSStartDisplayType.SDT_DoubleTripletOne);
                    newNode.mahjongTileList.Add(allMahjongCountList[0].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[0].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[0].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[1].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[1].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[1].mahjongTile);
                    displayMahjongList.Add(newNode);
                    mahjongCountList.Remove(allMahjongCountList[0]);
                    mahjongCountList.Remove(allMahjongCountList[1]);
                }
                if (allMahjongCountList.Count == 4)
                {
                    eSDType |= CSStartDisplayType.SDT_DoubleTripletTwo;
                    DisplayMahjongNode newNode = new DisplayMahjongNode(CSStartDisplayType.SDT_DoubleTripletTwo);
                    newNode.mahjongTileList.Add(allMahjongCountList[2].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[2].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[2].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[3].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[3].mahjongTile);
                    newNode.mahjongTileList.Add(allMahjongCountList[3].mahjongTile);
                    displayMahjongList.Add(newNode);
                    mahjongCountList.Remove(allMahjongCountList[2]);
                    mahjongCountList.Remove(allMahjongCountList[3]);
                }
            }
            return eSDType;
        }
    }
}
#endif