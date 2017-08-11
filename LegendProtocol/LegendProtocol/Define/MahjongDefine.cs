#if MAHJONG
using System.Collections.Generic;

namespace LegendProtocol
{
    //常量定义
    public struct MahjongConstValue
    {
        public const int MahjongFourPlayer = 4;   //麻将4人玩法
        public const int MahjongThreePlayer = 3;   //麻将3人玩法
        public const int MahjongZhuangAmount = 14;   //麻将庄家牌数
        public const int MahjongLeisureAmount = 13;   //麻将闲家牌数
        public const int MAXINDEX = 9;              //最大索引值
        public const int MAXREDNUM = 4;             //最大癞子数
    }
    //麻将房间属性
    public enum MahjongHousePropertyType
    {
        EMHP_None = 0,                       //空
        EMHP_ZhuangLeisure = 1,              //庄闲
        EMHP_EatHu = 1 << 1,                 //放炮胡
        EMHP_BirdDoubleIntegral = 1 << 2,    //中鸟翻倍
        EMHP_BirdAddIntegral = 1 << 3,       //中鸟加分
        EMHP_Hu7Pairs = 1 << 4,              //胡七对
        EMHP_GrabKongHu = 1 << 5,            //抢杠胡
        EMHP_Jin = 1 << 6,                   //筋
        EMHP_FakeHu = 1 << 7,                //假将胡
        EMHP_DoubleKong = 1 << 8,            //开杠个数翻倍
        EMHP_DoubleSeabed = 1 << 9,          //海底个数翻倍
        EMHP_HuZhuang = 1 << 10,             //胡牌为庄
        EMHP_IntegralCapped = 1 << 11,       //积分封顶
        EMHP_PersonalisePendulum = 1 << 12,  //个性化摆牌
    }
    public class MahjongHandTile
    {
        public ZhuangLeisureType zhuangLeisureType = ZhuangLeisureType.Leisure;
        public List<MahjongTile> mahjongTileList = new List<MahjongTile>();
        public MahjongHandTile() { }
        public MahjongHandTile(ZhuangLeisureType zhuangLeisureType)
        {
            this.zhuangLeisureType = zhuangLeisureType;
        }
    }
    public class MahjongCount
    {
        public MahjongTile mahjongTile;
        public int count;
        public MahjongCount() { }
        public MahjongCount(MahjongTile mahjongTile, int count)
        {
            this.mahjongTile = mahjongTile;
            this.count = count;
        }
    }
    public class DisplayMahjongNode
    {
        public CSStartDisplayType startDisplayType;
        public List<MahjongTile> mahjongTileList = new List<MahjongTile>();
        public DisplayMahjongNode() { }
        public DisplayMahjongNode(CSStartDisplayType startDisplayType)
        {
            this.startDisplayType = startDisplayType;
        }
    }
    // 牌组(包括手牌中的和桌面上牌的牌组)
    public class Meld
    {
        public MeldType m_eMeldType;
        public List<MahjongTile> m_meldTileList = new List<MahjongTile>();
        public Meld(List<MahjongTile> tileList, MeldType type)
        {
            m_meldTileList.AddRange(tileList);
            m_eMeldType = type;
        }

        // 顺子 碰牌
        public Meld(MahjongTile tile1, MahjongTile tile2, MahjongTile tile3, MeldType type)
        {
            m_meldTileList.Add(tile1);
            m_meldTileList.Add(tile2);
            m_meldTileList.Add(tile3);
            m_eMeldType = type;
        }

        // 杠
        public Meld(MahjongTile tile1, MahjongTile tile2, MahjongTile tile3, MahjongTile tile4, MeldType type)
        {
            m_meldTileList.Add(tile1);
            m_meldTileList.Add(tile2);
            m_meldTileList.Add(tile3);
            m_meldTileList.Add(tile4);
            m_eMeldType = type;
        }

        // 对子.将牌
        public Meld(MahjongTile tile1, MahjongTile tile2)
        {
            m_meldTileList.Add(tile1);
            m_meldTileList.Add(tile2);
            m_eMeldType = MeldType.EM_Pair;
        }

        public MahjongTile GetMahjongTileByIndex(int index)
        {
            if (index < m_meldTileList.Count)
            {
                return m_meldTileList[index];
            }

            return null;
        }
    }
}
#endif
