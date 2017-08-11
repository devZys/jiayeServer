#if MAHJONG
using System;
using System.Collections.Generic;

// 转转麻将的逻辑策略类，包括了所有牌型的基本操作.吃。碰。杠。胡。算番等。不同打发若有不同。继承此类重写即可.
// 作为一个单例.一种玩法有一个策略即可.
namespace LegendProtocol
{
    public class ZZMahjongSetTile : MahjongSetTileBase
    {
        public ZZMahjongSetTile()
        {
            mahjongType = MahjongType.ZhuanZhuanMahjong;
            Init();
        }

        /// <summary>
        /// 初始化转转麻将
        /// </summary>
        private void Init()
        {
            //删除转转麻将不用的牌(只要索筒万)
            m_allTileList.RemoveAll(element => !element.IsSuit());
        }
    }
    public class ZZMahjongStrategy : MahjongStrategyBase
    {
        public ZZMahjongStrategy()
        {
            mahjongType = MahjongType.ZhuanZhuanMahjong;
            Init();
        }

        /// <summary>
        /// 初始化转转麻将
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
            if (WinHandTileCheckToZZ(handTileList, meldList))
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
        private bool WinHandTileCheckToZZ(List<MahjongTile> handTileList, List<Meld> meldList)
        {
            if (handTileList.Count == 2 && meldList.Count == 4)
            {
                //单吊
                if (handTileList[0].Equal(handTileList[1]))
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
            return false;
        }
    }
}
#endif