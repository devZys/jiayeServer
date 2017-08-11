#if MAHJONG
using System;
using System.Collections.Generic;

// 转转麻将的逻辑策略类，包括了所有牌型的基本操作.吃。碰。杠。胡。算番等。不同打发若有不同。继承此类重写即可.
// 作为一个单例.一种玩法有一个策略即可.
namespace LegendProtocol
{
    public class RedMahjongSetTile : MahjongSetTileBase
    {
        public RedMahjongSetTile()
        {
            mahjongType = MahjongType.RedLaiZiMahjong;
            Init();
        }

        /// <summary>
        /// 初始化红中麻将
        /// </summary>
        private void Init()
        {
            //删除红中麻将不用的牌(只要索筒万红中)
            m_allTileList.RemoveAll(element => !element.IsSuit() && !element.IsRed());
        }
    }
    public class RedMahjongStrategy : MahjongStrategyBase
    {
        public RedMahjongStrategy()
        {
            mahjongType = MahjongType.RedLaiZiMahjong;
            Init();
        }

        /// <summary>
        /// 初始化红中麻将
        /// </summary>
        private void Init()
        {
            //可以吃
            m_bCanChow = false;
        }

        // 返回胡牌类型
        public override CSSpecialWinType WinHandTileCheck(List<MahjongTile> handTileList, List<Meld> meldList, bool b7Pairs = false, bool bFakeHu = false)
        {
            if (b7Pairs && SpecialAnglyseTile(handTileList))
            {
                return CSSpecialWinType.WT_7Pairs;
            }
            if (WinHandTileCheckToRed(handTileList, meldList))
            {
                return CSSpecialWinType.WT_PingWin;
            }
            return CSSpecialWinType.WT_None;
        }

        // 手牌分析.刚抓到一张牌之后做手牌分析.也可为服务器发牌之后第一次数据分析.（闲家第一次只分析是否听牌 看麻将玩法是否有此需求.如没有听牌需求 不必要调用此.听牌判定比胡牌判定慢很多.）
        public override bool AnalyseHandTile(List<MahjongTile> handTileList, List<Meld> meldList, bool b7Pires = false, bool bFakeHu = false, bool bReadHand = false, bool bTriplet = false)
        {
            if (handTileList.Count % 3 != 2 || bReadHand)
            {
                //牌数不对
                return false;
            }

            return (CSSpecialWinType.WT_None != WinHandTileCheck(handTileList, meldList, b7Pires));
        }
        private bool WinHandTileCheckToRed(List<MahjongTile> handTileList, List<Meld> meldList)
        {
            if (handTileList.Count == 2 && meldList.Count == 4)
            {
                //单吊
                if (handTileList[0].Equal(handTileList[1]) || handTileList.Exists(elmenet => elmenet.IsRed()))
                {
                    //相同或者有癞子
                    return true;
                }
            }
            //没有癞子
            if (!handTileList.Exists(elmenet => elmenet.IsRed()))
            {
                return base.AnalyseHandTile(handTileList, meldList);
            }
            else
            {
                List<MahjongTile> _DragonTileList = new List<MahjongTile>();
                List<MahjongTile> _WindTileList = new List<MahjongTile>();
                List<MahjongTile> _DotTileList = new List<MahjongTile>();
                List<MahjongTile> _BambooTileList = new List<MahjongTile>();
                List<MahjongTile> _CharacterTileList = new List<MahjongTile>();

                for (int i = 0; i < handTileList.Count; ++i)
                {
                    MahjongTile tile = handTileList[i];
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
                            return false;
                    }
                }

                int curRedNum = _DragonTileList.Count;
                //万
                m_NeedMinRedNum = MahjongConstValue.MAXREDNUM;
                GetNeedRedNumToBe3Dot(_CharacterTileList, 0);
                int characterNeedNum = m_NeedMinRedNum;
                //条
                m_NeedMinRedNum = MahjongConstValue.MAXREDNUM;
                GetNeedRedNumToBe3Dot(_BambooTileList, 0);
                int bambooNeedNum = m_NeedMinRedNum;
                //筒
                m_NeedMinRedNum = MahjongConstValue.MAXREDNUM;
                GetNeedRedNumToBe3Dot(_DotTileList, 0);
                int dotNeedNum = m_NeedMinRedNum;
                //将在万里面
                int needRedAll = bambooNeedNum + dotNeedNum;
                if (needRedAll <= curRedNum)
                {
                    int hasRedNum = curRedNum - needRedAll;
                    if (_DoAnalyseTils(_CharacterTileList, hasRedNum))
                    {
                        return true;
                    }
                }
                //将在条里面
                needRedAll = characterNeedNum + dotNeedNum;
                if (needRedAll <= curRedNum)
                {
                    int hasRedNum = curRedNum - needRedAll;
                    if (_DoAnalyseTils(_BambooTileList, hasRedNum))
                    {
                        return true;
                    }
                }
                //将在筒里面
                needRedAll = characterNeedNum + bambooNeedNum;
                if (needRedAll <= curRedNum)
                {
                    int hasRedNum = curRedNum - needRedAll;
                    if (_DoAnalyseTils(_DotTileList, hasRedNum))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        //判断七小对
        public override bool SpecialAnglyseTile(List<MahjongTile> handTileList)
        {
            if (!handTileList.Exists(elmenet => elmenet.IsRed()))
            {
                return base.SpecialAnglyseTile(handTileList);
            }

            if (handTileList.Count == MahjongConstValue.MahjongZhuangAmount)
            {
                int count = 1;
                int danZhangCount = 0;
                List<MahjongTile> _handTileList = new List<MahjongTile>();
                _handTileList.AddRange(handTileList);
                int redCount = _handTileList.RemoveAll(element => element.IsRed());
                TileDescType oldDescType = _handTileList[0].GetDescType();
                TileNumType oldNumType = _handTileList[0].GetNumType();

                for (int i = 1; i < _handTileList.Count; ++i)
                {
                    TileDescType currDescType = _handTileList[i].GetDescType();
                    TileNumType currNumType = _handTileList[i].GetNumType();
                    if (oldDescType == currDescType && oldNumType == currNumType)
                    {
                        count++;
                    }
                    else if (count % 2 == 0)
                    {
                        oldDescType = currDescType;
                        oldNumType = currNumType;
                        count = 1;
                    }
                    else
                    {
                        oldDescType = currDescType;
                        oldNumType = currNumType;
                        //记录单张个数
                        danZhangCount += 1;
                    }
                    if (danZhangCount > redCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        // 比较排序函数(顺序为. 红中、万、筒、索、风、箭、花)
        public override int CompareTile(MahjongTile a, MahjongTile b)
        {
            if (!a.Equal(b) && (a.IsRed() || b.IsRed()))
            {
                if (a.IsRed())
                {
                    return -1;
                }
                else if (b.IsRed())
                {
                    return 1;
                }
            }
            TileDescType eDescTypeA = a.GetDescType();
            TileDescType eDescTypeB = b.GetDescType();

            // 如果类型不一样按照类型排序
            if (eDescTypeA != eDescTypeB)
            {
                if ((int)eDescTypeA < (int)eDescTypeB)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }

            // 如果类型一样的话 按NumType排序
            TileNumType eNumTypeA = a.GetNumType();
            TileNumType eNumTypeB = b.GetNumType();

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
    }
}
#endif