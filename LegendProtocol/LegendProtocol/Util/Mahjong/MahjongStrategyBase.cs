#if MAHJONG
using System;
using System.Collections.Generic;

// 麻将的逻辑策略类.基类，包括了所有牌型的基本操作.吃。碰。杠。胡。算番等。不同打发若有不同。继承此类重写即可.
// 作为一个单例.一种玩法有一个策略即可.
namespace LegendProtocol
{
    //起手胡摆牌枚举
    public enum CSStartDisplayType
    {
        SDT_None = 0,                       //空
        SDT_ThreeSameOne = 1,               //三同   通过三个不同色同数的对子来判定.
        SDT_ThreeSameTwo = 1 << 1,          //三同   通过三个不同色同数的对子来判定.
        SDT_SteadilyHighOne = 1 << 2,       //节节高 通过三个同色相连的对子来判定.
        SDT_SteadilyHighTwo = 1 << 3,       //节节高 通过三个同色相连的对子来判定.
        SDT_DoubleTripletOne = 1 << 4,      //六顺   通过两个刻来判定.
        SDT_DoubleTripletTwo = 1 << 5,      //六顺   通过两个刻来判定.
        SDT_ConcealedKongOne = 1 << 6,      //四喜   通过暗杠判定
        SDT_ConcealedKongTwo = 1 << 7,      //四喜   通过暗杠判定
        SDT_ConcealedKongThree = 1 << 8,    //四喜   通过暗杠判定
        SDT_MissOneType = 1 << 9,           //缺一色
        SDT_NoPairs = 1 << 10,              //无将
        SDT_AFlower = 1 << 11,              //一枝花
        SDT_MidwayConcealedKong = 1 << 12,  //中途四喜
    }
    //胡牌枚举
    public enum CSSpecialWinType
    {
        WT_None = 0,                    //空
        WT_PingWin = 1,                 //平胡
        WT_7Pairs = 1 << 1,             //小七对
        WT_AllEyes = 1 << 2,            //将将胡
        WT_SingleType = 1 << 3,         //清一色
        WT_AllMelds = 1 << 4,           //全求人
        WT_KongWin = 1 << 5,            //杠上开花
        WT_GrabKongWin = 1 << 6,        //抢杠胡
        WT_SeabedWin = 1 << 7,          //海底胡
        WT_BumperWin = 1 << 8,          //碰碰胡
        WT_Luxury7Pairs = 1 << 9,       //豪华小七对
        WT_DLuxury7Pairs = 1 << 10,     //双豪华小七对
        WT_FrontClear = 1 << 11,        //门前清     
        WT_FakeHu = 1 << 12,            //假将胡      
    }
    //发牌类
    public class MahjongSetTileBase
    {
        //所有的牌
        protected List<MahjongTile> m_allTileList = new List<MahjongTile>();
        //麻将类型
        public MahjongType mahjongType = MahjongType.MahjongNone;
        public MahjongSetTileBase()
        {
            InitMahjongTile();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void InitMahjongTile()
        {
            //初始化牌
            m_allTileList.Clear();
            for (int i = 0; i < 4; ++i)
            {
                for (int tileNum = (int)TileNumType.ETN_One; tileNum <= (int)TileNumType.ETN_White; ++tileNum)
                {
                    if (tileNum <= (int)TileNumType.ETN_Nine)
                    {
                        for (int tileDesc = (int)TileDescType.ETD_Dot; tileDesc <= (int)TileDescType.ETD_Character; ++tileDesc)
                        {
                            m_allTileList.Add(new MahjongTile(TileBlurType.ETB_Suit, (TileDescType)tileDesc, (TileNumType)tileNum));
                        }
                    }
                    else if (tileNum <= (int)TileNumType.ETN_North)
                    {
                        m_allTileList.Add(new MahjongTile(TileBlurType.ETB_Honor, TileDescType.ETD_Wind, (TileNumType)tileNum));
                    }
                    else
                    {
                        m_allTileList.Add(new MahjongTile(TileBlurType.ETB_Honor, TileDescType.ETD_Dragon, (TileNumType)tileNum));
                    }
                }
            }
        }
        /// <summary>
        /// 发牌
        /// </summary>
        /// <param name="playerList"></param>
        /// <param name="remainMahjongList"></param>
        /// <param name="bRedLaizi"></param>
        public void InitMahjongTile(Dictionary<int, MahjongHandTile> mahjongList, List<MahjongTile> remainMahjongList)
        {
            List<MahjongTile> _mahjongTileList = new List<MahjongTile>();
            _mahjongTileList.AddRange(m_allTileList);
            for (int i = 0; i < mahjongList.Count; ++i)
            {
                int count = MahjongConstValue.MahjongLeisureAmount;
                if (mahjongList[i].zhuangLeisureType == ZhuangLeisureType.Zhuang)
                {
                    count = MahjongConstValue.MahjongZhuangAmount;
                }
                mahjongList[i].mahjongTileList.AddRange(GetMahjongTileByCount(_mahjongTileList, count));
                //mahjongList[i].mahjongTileList.AddRange(GetMahjongTile(_mahjongTileList, count, i));
            }
            remainMahjongList.AddRange(_mahjongTileList);
        }

        private List<MahjongTile> GetMahjongTileByCount(List<MahjongTile> mahjongList, int count)
        {
            List<MahjongTile> resultList = new List<MahjongTile>();
            for (int i = 0; i < count; ++i)
            {
                int index = MyRandom.NextPrecise(0, mahjongList.Count);
                MahjongTile tile = mahjongList[index];
                resultList.Add(new MahjongTile(tile));
                mahjongList.Remove(tile);
            }

            return resultList;
        }
        private List<MahjongTile> GetMahjongTile(List<MahjongTile> mahjongList, int count, int index)
        {
            switch (index)
            {
                case 0:
                    return GetMahjongTileOne(mahjongList, count);
                case 1:
                    return GetMahjongTileTwo(mahjongList, count);
                case 2:
                    return GetMahjongTileThree(mahjongList, count);
                case 3:
                    return GetMahjongTileFour(mahjongList, count);
                default:
                    break;
            }
            return new List<MahjongTile>();
        }
        private List<MahjongTile> GetMahjongTileOne(List<MahjongTile> mahjongList, int count)
        {
            List<MahjongTile> resultList = new List<MahjongTile>();

            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Six));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Six));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Eight));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Eight));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Eight));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Six));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Six));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Eight));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Eight));
            if (count == 14)
            {
                resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Five));
            }
            foreach (MahjongTile mahjongTile in resultList)
            {
                mahjongList.Remove(mahjongList.Find(element => element.Equal(mahjongTile)));
            }
            return resultList;
        }
        private List<MahjongTile> GetMahjongTileTwo(List<MahjongTile> mahjongList, int count)
        {
            List<MahjongTile> resultList = new List<MahjongTile>();

            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Two));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Three));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Three));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Four));
            if (count == 14)
            {
                resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Eight));
            }
            foreach (MahjongTile mahjongTile in resultList)
            {
                mahjongList.Remove(mahjongList.Find(element => element.Equal(mahjongTile)));
            }
            return resultList;
        }
        private List<MahjongTile> GetMahjongTileThree(List<MahjongTile> mahjongList, int count)
        {
            List<MahjongTile> resultList = new List<MahjongTile>();

            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Two));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Two));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Two));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Five));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Five));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Nine));
            if (mahjongType == MahjongType.RedLaiZiMahjong)
            {
                resultList.Add(new MahjongTile(TileBlurType.ETB_Honor, TileDescType.ETD_Dragon, TileNumType.ETN_Red));
            }
            else
            {
                resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Nine));
            }
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Three));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Three));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Three));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Two));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Two));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Two));
            if (count == 14)
            {
                resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Nine));
            }
            foreach (MahjongTile mahjongTile in resultList)
            {
                mahjongList.Remove(mahjongList.Find(element => element.Equal(mahjongTile)));
            }
            return resultList;
        }
        private List<MahjongTile> GetMahjongTileFour(List<MahjongTile> mahjongList, int count)
        {
            List<MahjongTile> resultList = new List<MahjongTile>();

            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Two));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Two));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Two));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Six));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Three));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Three));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Eight));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Nine));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Five));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Five));
            if (count == 14)
            {
                resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Three));
            }
            foreach (MahjongTile mahjongTile in resultList)
            {
                mahjongList.Remove(mahjongList.Find(element => element.Equal(mahjongTile)));
            }
            return resultList;
        }
    }
    //策略类
    public class MahjongStrategyBase
    {
        // 对于多种打牌方式目前 只有不能吃的玩法.
        protected bool m_bCanChow = true;

        protected int[] m_tempCountArray = new int[10];
        protected int[] m_currCountArray = new int[10];
        //需要的最小癞子数
        protected int m_NeedMinRedNum = 0;
        protected int Min(int one, int two) { return (one) < (two) ? (one) : (two); }

        //麻将类型
        public MahjongType mahjongType = MahjongType.MahjongNone; 
        public MahjongStrategyBase()
        {
        }
        
        protected virtual bool IsExistInTileList(MahjongTile tile, List<MahjongTile> tileList)
        {
            return tileList.Exists(element => element.Equal(tile));
        }
        
        /// <summary>
        /// 获取是否允许吃牌
        /// </summary>
        /// <returns></returns>
        public bool GetCanChow()
        {
            return m_bCanChow;
        }

        /// <summary>
        /// 吃牌
        /// </summary>
        /// <param name="suitTile1"></param> 手牌1
        /// <param name="suitTile2"></param> 手牌2
        /// <param name="targetTile"></param> 上家打出来的牌
        /// <returns></returns> // 可以不判定.看需求.一般到此时已经确认这三张牌互为吃牌关系
        public virtual Meld Chow(MahjongTile suitTile1, MahjongTile suitTile2, MahjongTile targetTile)
        {
            // 规则不能吃. 传入的牌不是花色配牌.返回空
            if (!m_bCanChow || !suitTile1.IsSuit() || !suitTile2.IsSuit() || !targetTile.IsSuit())
            {
                return null;
            }

            // 确定三张牌的花色一致
            TileDescType targetType = targetTile.GetDescType();

            if (suitTile1.GetDescType() == targetType && suitTile2.GetDescType() == targetType)
            {
                List<TileNumType> _tempNumTypeList = new List<TileNumType>();
                _tempNumTypeList.Add(suitTile1.GetNumType());
                _tempNumTypeList.Add(suitTile2.GetNumType());
                _tempNumTypeList.Add(targetTile.GetNumType());
                _tempNumTypeList.Sort();

                if ((int)_tempNumTypeList[0] + (int)_tempNumTypeList[2] == 2 * (int)_tempNumTypeList[1])
                {
                    //吃牌放中间
                    return new Meld(suitTile1, targetTile, suitTile2, MeldType.EM_Sequence);
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
        public virtual bool ChowCheck(MahjongTile targetTile, List<MahjongTile> handTile)
        {
            // 不可吃.牌不是花色配牌
            if (!m_bCanChow || !targetTile.IsSuit())
            {
                return false;
            }

            TileDescType descType = targetTile.GetDescType();
            TileNumType numType = targetTile.GetNumType();
            // 列表中是否有正负2的差牌
            bool hasMinusTwo = false;
            bool hasMinusOne = false;
            bool hasPlusTwo = false;
            bool hasPlusOne = false;

            // 满足一下条件可以吃, 1 同色. (2 索引-2，-1 . 3 索引 -1, 1. 4 索引 1, 2)
            for (int i = 0; i < handTile.Count; ++i)
            {
                MahjongTile tile = handTile[i];
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
            }

            // 满足3条件之一 可以吃.
            if ((hasMinusTwo && hasMinusOne) || (hasMinusOne && hasPlusOne) || (hasPlusOne && hasPlusTwo))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 碰牌
        /// </summary>
        /// <param name="suitTile1"></param> 手牌1
        /// <param name="suitTile2"></param> 手牌2
        /// <param name="targetTile"></param> 别人打出来的牌
        /// <returns></returns>
        public virtual Meld Pong(MahjongTile suitTile1, MahjongTile suitTile2, MahjongTile targetTile)
        {
            // 传入的牌不是花色配牌.返回空
            if (!suitTile1.IsSuit() || !suitTile2.IsSuit() || !targetTile.IsSuit())
            {
                return null;
            }

            // 确定三张牌的花色一致
            TileDescType targetDescType = targetTile.GetDescType();

            if (suitTile1.GetDescType() == targetDescType && suitTile2.GetDescType() == targetDescType)
            {
                TileNumType targetNumType = targetTile.GetNumType();

                if (suitTile1.GetNumType() == targetNumType && suitTile2.GetNumType() == targetNumType)
                {
                    return new Meld(suitTile1, suitTile2, targetTile, MeldType.EM_Triplet);
                }
            }

            return null;
        }

        /// <summary>
        /// 判定目标牌是否可以碰杠.
        /// </summary>
        /// <param name="targetTile"></param>   别人打的牌.
        /// <param name="handTile"></param>     手牌列表
        /// <returns></returns> 这里注意一下可扛必可碰.不返回两种类型了
        public virtual MeldType PongKongCheck(MahjongTile targetTile, List<MahjongTile> handTile)
        {
            //传入的牌不是花色配牌
            if (!targetTile.IsSuit())
            {
                return MeldType.EM_None;
            }

            int count = 0;

            for (int i = 0; i < handTile.Count; ++i)
            {
                MahjongTile tile = handTile[i];
                if (tile.Equal(targetTile))
                {
                    count++;
                }
            }

            if (count == 2)
            {
                return MeldType.EM_Triplet;
            }
            else if (count == 3)
            {
                return MeldType.EM_ExposedKong;
            }
            else if (count == 4)
            {
                return MeldType.EM_ConcealedKong;
            }

            return MeldType.EM_None;
        }

        /// <summary>
        /// 判定是否可以明杠
        /// </summary>
        /// <param name="targetTile"></param>   选择要杠的牌.
        /// <param name="displayTileList"></param>     已经吃碰杠的牌
        /// <returns></returns>
        public virtual Meld ExposedKongCheck(MahjongTile targetTile, List<Meld> displayTileList)
        {
            return displayTileList.Find(element => element.m_eMeldType == MeldType.EM_Triplet && element.m_meldTileList.Exists(tile => tile.Equal(targetTile)));
        }

        // 补杠(通常有两种.一种是抓进来那张牌才能补杠.一种是所有手牌都可以进行补杠) 如果是抓进来那张牌才能补杠.HandList里给一张牌就可以.
        public virtual bool SupplementExposedKongCheck(List<Meld> dispalyMeldList, List<MahjongTile> handList)
        {
            // 这个列表可以做成 out的传参方式返回.怎么方便怎么用.
            List<MahjongTile> _tempTileList = new List<MahjongTile>();

            for (int i = 0; i < dispalyMeldList.Count; ++i)
            {
                if (dispalyMeldList[i].m_eMeldType == MeldType.EM_Triplet)
                {
                    MahjongTile tile = dispalyMeldList[i].GetMahjongTileByIndex(0);
                    if (IsExistInTileList(tile, handList))
                    {
                        _tempTileList.Add(tile);
                    }
                }
            }

            if (_tempTileList.Count > 0)
            {
                return true;
            }

            return false;
        }

        // 判定列表牌中能暗杠的牌.(一个列表)
        public virtual List<MahjongTile> ConcealedKongCheck(List<MahjongTile> tileList, int targetCount = 4)
        {
            List<MahjongTile> _tempTileList = new List<MahjongTile>();

            if (tileList.Count == 0)
            {
                return _tempTileList;
            }

            int count = 1;
            MahjongTile newTile = new MahjongTile(tileList[0]);
            // 建立在TileList已经排序好的基础上.
            for (int i = 1; i < tileList.Count; ++i)
            {
                if (newTile.Equal(tileList[i]))
                {
                    count++;

                    if (count == targetCount)
                    {
                        _tempTileList.Add(tileList[i]);
                    }
                    else if (count > targetCount)
                    {
                        _tempTileList.RemoveAll(element => element.Equal(tileList[i]));
                    }
                }
                else
                {
                    count = 1;
                    newTile = new MahjongTile(tileList[i]);
                }
            }

            return _tempTileList;
        }

        /// <summary>
        /// 扛牌
        /// </summary>
        /// <param name="suitTile1"></param> 手牌1
        /// <param name="suitTile2"></param> 手牌2
        /// <param name="suitTile3"></param> 手牌3
        /// <param name="targetTile"></param> 目标牌.
        /// <returns></returns>  注意:::吃碰杠的判定可以去掉.一般来说 判定了可以吃碰杠了之后。下面的条件都已经满足了.没必要重复判定.
        public virtual Meld Kong(MahjongTile suitTile1, MahjongTile suitTile2, MahjongTile suitTile3, MahjongTile targetTile, bool bExposedKong)
        {
            // 传入的牌不是花色配牌.返回空
            if (!suitTile1.IsSuit() || !suitTile2.IsSuit() || !suitTile3.IsSuit() || !targetTile.IsSuit())
            {
                return null;
            }
            // 确定四张牌的花色一致
            TileDescType targetDescType = targetTile.GetDescType();

            if (suitTile1.GetDescType() == targetDescType && suitTile2.GetDescType() == targetDescType && suitTile3.GetDescType() == targetDescType)
            {
                TileNumType targetNumType = targetTile.GetNumType();

                if (suitTile1.GetNumType() == targetNumType && suitTile2.GetNumType() == targetNumType && suitTile3.GetNumType() == targetNumType)
                {
                    return new Meld(suitTile1, suitTile2, suitTile3, targetTile, bExposedKong ? MeldType.EM_ExposedKong : MeldType.EM_ConcealedKong);
                }
            }

            return null;
        }

        // 判定一种手牌是否为3n+2或者3n
        public virtual bool _DoAnalyseTils(List<MahjongTile> tilesList, bool bTriplet = false)
        {
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
                // 索引从1开始对应数字的牌的枚举(除去将牌之后判定剩余牌是否为3n)
                for (int i = 1; i < m_tempCountArray.Length; ++i)
                {
                    if (m_currCountArray[i] >= 2)
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
            //else
            //{
            //    _ReadyHandCheck(bTriplet);

            //    if(m_tempReadyHandTileNumList.Count != 0)
            //    {
            //        return true;
            //    }
            //}

            return false;
        }

        //// 这个判定针对3n+1的 也就是需要听牌的部分.
        //public virtual void _ReadyHandCheck(bool bTriplet)
        //{
        //    for (int i = 1; i < m_currCountArray.Length; ++i)
        //    {
        //        // 先去掉刻.判定
        //        if (m_currCountArray[i] >= 3)
        //        {
        //            m_currCountArray[i] -= 3;
        //            _ReadyHandCheck(bTriplet);
        //            m_currCountArray[i] += 3;
        //        }

        //        if (m_currCountArray[i] > 0)
        //        {
        //            // 只判定刻.
        //            if (bTriplet)
        //            {
        //                // 大于2张.
        //                if (m_currCountArray[i] >= 2)
        //                {
        //                    m_currCountArray[i] -= 2;
        //                    m_tempReadyHandPairsNumList.Add(i);
        //                    _ReadyHandCheck(bTriplet);
        //                    m_tempReadyHandPairsNumList.Remove(i);
        //                    m_currCountArray[i] += 2;
        //                }
        //                else if (m_currCountArray[i] == 1)
        //                {
        //                    m_currCountArray[i] -= 1;
        //                    //除去这个单调将牌 剩余牌是否为3n 并且去顶这个牌在原牌型中不是4个.
        //                    if (m_tempCountArray[i] != 4 && m_tempReadyHandPairsNumList.Count == 0 && _Is3DotN(bTriplet))
        //                    {
        //                        m_tempReadyHandTileNumList.Add(i);
        //                    }
        //                    m_currCountArray[i] += 1;
        //                    return; // 剩下单张 不管是否听牌. 没必要继续遍历了.
        //                }
        //            }
        //            else
        //            {
        //                // 再去掉将牌.
        //                if (m_currCountArray[i] >= 2)
        //                {
        //                    m_currCountArray[i] -= 2;
        //                    m_tempReadyHandPairsNumList.Add(i);
        //                    _ReadyHandCheck(bTriplet);
        //                    m_tempReadyHandPairsNumList.Remove(i);
        //                    m_currCountArray[i] += 2;
        //                }

        //                // 去掉顺.
        //                if (i <= MAXINDEX - 2 && m_currCountArray[i + 1] > 0 && m_currCountArray[i + 2] > 0)
        //                {
        //                    m_currCountArray[i] -= 1;
        //                    m_currCountArray[i + 1] -= 1;
        //                    m_currCountArray[i + 2] -= 1;
        //                    _ReadyHandCheck(bTriplet);
        //                    m_currCountArray[i] += 1;
        //                    m_currCountArray[i + 1] += 1;
        //                    m_currCountArray[i + 2] += 1;
        //                }
        //                else if(i <= MAXINDEX - 1 && m_currCountArray[i + 1] > 0)
        //                {
        //                    m_currCountArray[i] -= 1;
        //                    m_currCountArray[i + 1] -= 1;
        //                    if (m_tempReadyHandPairsNumList.Count == 1 && _Is3DotN(bTriplet))
        //                    {
        //                        if(i > 1)
        //                        {
        //                            m_tempReadyHandTileNumList.Add(i - 1);
        //                        }

        //                        if(i < MAXINDEX - 1)
        //                        {
        //                            m_tempReadyHandTileNumList.Add(i + 2);
        //                        }
        //                    }
        //                    m_currCountArray[i] += 1;
        //                    m_currCountArray[i + 1] += 1;
        //                }
        //                else if (i <= MAXINDEX - 2 && m_currCountArray[i + 2] > 0)
        //                {
        //                    m_currCountArray[i] -= 1;
        //                    m_currCountArray[i + 2] -= 1;
        //                    if (m_tempReadyHandPairsNumList.Count == 1 && _Is3DotN(bTriplet))
        //                    {
        //                        m_tempReadyHandTileNumList.Add(i + 1);
        //                    }
        //                    m_currCountArray[i] += 1;
        //                    m_currCountArray[i + 2] += 1;
        //                }
        //                else
        //                {
        //                    //最后的判定 把此牌拿去做单调将.
        //                    m_currCountArray[i] -= 1;
        //                    if (m_tempCountArray[i] != 4 && m_tempReadyHandPairsNumList.Count == 0 && _Is3DotN(bTriplet))
        //                    {
        //                        m_tempReadyHandTileNumList.Add(i);
        //                    }
        //                    m_currCountArray[i] += 1;
        //                    // 这是最后一步.如果到此处了.说明此牌型已经判定完毕了. 不管是否听牌.都已经结束了.
        //                    return;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            //这里说明此张的数量为0。也就是说。当这张牌为最后一张的时候.有可能为对倒牌型听牌。
        //            if (i == MAXINDEX && m_tempReadyHandPairsNumList.Count == 2 && m_tempReadyHandTileNumList[0] != m_tempReadyHandTileNumList[1])
        //            {
        //                m_tempReadyHandTileNumList.Add(m_tempReadyHandTileNumList[0]);
        //                m_tempReadyHandTileNumList.Add(m_tempReadyHandTileNumList[1]);
        //            }
        //        }

        //    }//for
        //}

        // 除去将之后是否手牌剩下了3n.
        public virtual bool _Is3DotN(bool bTriplet)
        {
            for (int i = 1; i < m_currCountArray.Length; ++i)
            {
                //刻.
                if (m_currCountArray[i] >= 3)
                {
                    m_currCountArray[i] -= 3;
                    if (_Is3DotN(bTriplet))
                    {
                        return true;
                    }
                    m_currCountArray[i] += 3;
                }
                else if (m_currCountArray[i] > 0)
                {
                    // 只判定刻.
                    if (bTriplet)
                    {
                        return false;
                    }

                    // 顺.
                    if (i <= MahjongConstValue.MAXINDEX - 2 && m_currCountArray[i + 1] > 0 && m_currCountArray[i + 2] > 0)
                    {
                        m_currCountArray[i] -= 1;
                        m_currCountArray[i + 1] -= 1;
                        m_currCountArray[i + 2] -= 1;
                        if (_Is3DotN(bTriplet))
                        {
                            return true;
                        }
                        else
                        {
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
        //判断某一类型能否成为3n + 2
        public virtual bool _DoAnalyseTils(List<MahjongTile> mahjongTileList, int hasRedNum)
        {
            int mahjongCount = mahjongTileList.Count;
            if (mahjongCount <= 0)
            {
                if (hasRedNum >= 2)
                {
                    return true;
                }
                return false;
            }
            for (int i = 0; i < mahjongCount; ++i)
            {
                //如果是最后一张牌
                if ((i + 1) == mahjongCount)
                {
                    if (hasRedNum > 0)
                    {
                        hasRedNum = hasRedNum - 1;
                        MahjongTile mahjongTile = mahjongTileList[i];
                        mahjongTileList.Remove(mahjongTile);
                        m_NeedMinRedNum = MahjongConstValue.MAXREDNUM;
                        GetNeedRedNumToBe3Dot(mahjongTileList, 0);
                        if (m_NeedMinRedNum <= hasRedNum)
                        {
                            return true;
                        }
                        hasRedNum = hasRedNum + 1;
                        mahjongTileList.Add(mahjongTile);
                        mahjongTileList.Sort(CompareTile);
                    }
                }
                else
                {
                    if ((i + 2) == mahjongCount || mahjongTileList[i].GetNumType() != mahjongTileList[i + 2].GetNumType())
                    {
                        if (mahjongTileList[i].Equal(mahjongTileList[i + 1]))
                        {
                            MahjongTile mahjongOne = mahjongTileList[i];
                            MahjongTile mahjongTwo = mahjongTileList[i + 1];
                            mahjongTileList.Remove(mahjongOne);
                            mahjongTileList.Remove(mahjongTwo);
                            m_NeedMinRedNum = MahjongConstValue.MAXREDNUM;
                            GetNeedRedNumToBe3Dot(mahjongTileList, 0);
                            if (m_NeedMinRedNum <= hasRedNum)
                            {
                                return true;
                            }
                            mahjongTileList.Add(mahjongTwo);
                            mahjongTileList.Add(mahjongOne);
                            mahjongTileList.Sort(CompareTile);
                        }
                    }
                    if (hasRedNum > 0 && mahjongTileList[i].GetNumType() != mahjongTileList[i + 1].GetNumType())
                    {
                        hasRedNum = hasRedNum - 1;
                        MahjongTile mahjongTile = mahjongTileList[i];
                        mahjongTileList.Remove(mahjongTile);
                        m_NeedMinRedNum = MahjongConstValue.MAXREDNUM;
                        GetNeedRedNumToBe3Dot(mahjongTileList, 0);
                        if (m_NeedMinRedNum <= hasRedNum)
                        {
                            return true;
                        }
                        hasRedNum = hasRedNum + 1;
                        mahjongTileList.Add(mahjongTile);
                        mahjongTileList.Sort(CompareTile);
                    }
                }
            }
            return false;
        }
        //判断输入的牌是否可以组成一组(一组包括刻或者顺子)
        public virtual bool _Is3DotN(MahjongTile tileOne, MahjongTile tileTwo, MahjongTile tileThree)
        {
            if (tileOne.GetBlurType() != tileTwo.GetBlurType() || tileTwo.GetBlurType() != tileThree.GetBlurType())
            {
                return false;
            }
            if (tileOne.GetNumType() == tileTwo.GetNumType() && tileTwo.GetNumType() == tileThree.GetNumType())
            {
                //刻
                return true;
            }
            if (!tileOne.IsSuit())
            {
                //不是配牌不能顺
                return false;
            }
            if ((tileOne.GetNumType() + 1) == tileTwo.GetNumType() && (tileOne.GetNumType() + 2) == tileThree.GetNumType())
            {
                return true;
            }
            return false;
        }
        //判断某一类型组成一组需要最小癞子数(一组包括刻或者顺子)
        public virtual void GetNeedRedNumToBe3Dot(List<MahjongTile> mahjongTileList, int needNum)
        {
            if (m_NeedMinRedNum == 0) return;
            if (needNum >= m_NeedMinRedNum) return;

            int vSize = mahjongTileList.Count;
            if (vSize == 0)
            {
                m_NeedMinRedNum = Min(needNum, m_NeedMinRedNum);
            }
            else if (vSize == 1)
            {
                m_NeedMinRedNum = Min(needNum + 2, m_NeedMinRedNum);
            }
            else if (vSize == 2)
            {
                MahjongTile tileOne = mahjongTileList[0];
                MahjongTile tileTwo = mahjongTileList[1];
                if (tileTwo.IsSuit())
                {
                    if (tileTwo.GetNumType() - tileOne.GetNumType() < 3)
                    {
                        m_NeedMinRedNum = Min(needNum + 1, m_NeedMinRedNum);
                    }
                }
                else
                {
                    if (tileTwo.Equal(tileOne))
                    {
                        m_NeedMinRedNum = Min(needNum + 1, m_NeedMinRedNum);
                    }
                }
            }
            else
            {
                MahjongTile tileOne = mahjongTileList[0];
                MahjongTile tileTwo = mahjongTileList[1];
                MahjongTile tileThree = mahjongTileList[2];
                //第一个自己一扑
                if (needNum + 2 < m_NeedMinRedNum)
                {
                    mahjongTileList.Remove(tileOne);
                    GetNeedRedNumToBe3Dot(mahjongTileList, needNum + 2);
                    mahjongTileList.Add(tileOne);
                    mahjongTileList.Sort(CompareTile);
                }

                //第一个跟其它的一个一扑
                if (needNum + 1 < m_NeedMinRedNum)
                {
                    if (tileTwo.IsSuit())
                    {
                        for (int i = 1; i < mahjongTileList.Count; ++i)
                        {
                            if (needNum + 1 >= m_NeedMinRedNum) break;
                            tileTwo = mahjongTileList[i];
                            //455567这里可结合的可能为 45 46 否则是45 45 45 46
                            //如果当前的value不等于下一个value则和下一个结合避免重复
                            if (i + 1 != mahjongTileList.Count)
                            {
                                tileThree = mahjongTileList[i + 1];
                                if (tileThree.GetNumType() == tileTwo.GetNumType()) continue; ;
                            }

                            if (tileTwo.GetNumType() - tileOne.GetNumType() < 3)
                            {
                                mahjongTileList.Remove(tileOne);
                                mahjongTileList.Remove(tileTwo);
                                GetNeedRedNumToBe3Dot(mahjongTileList, needNum + 1);
                                mahjongTileList.Add(tileTwo);
                                mahjongTileList.Add(tileOne);
                                mahjongTileList.Sort(CompareTile);
                            }
                            else break;
                        }
                    }
                    else
                    {
                        if (tileTwo.Equal(tileOne))
                        {
                            mahjongTileList.Remove(tileOne);
                            mahjongTileList.Remove(tileTwo);
                            GetNeedRedNumToBe3Dot(mahjongTileList, needNum + 1);
                            mahjongTileList.Add(tileTwo);
                            mahjongTileList.Add(tileOne);
                            mahjongTileList.Sort(CompareTile);
                        }
                    }
                }
                //第一个和其它两个一扑
                //后面间隔两张张不跟前面一张相同222234 
                //可能性为222 234
                for (int i = 1; i < mahjongTileList.Count; ++i)
                {
                    if (needNum >= m_NeedMinRedNum) break;
                    tileTwo = mahjongTileList[i];
                    if (i + 2 < mahjongTileList.Count)
                    {
                        if (mahjongTileList[i + 2].GetNumType() == tileTwo.GetNumType()) continue;
                    }
                    for (int j = i + 1; j < mahjongTileList.Count; ++j)
                    {
                        if (needNum >= m_NeedMinRedNum) break;
                        tileThree = mahjongTileList[j];
                        if (j + 1 < mahjongTileList.Count)
                        {
                            if (tileThree.GetNumType() == mahjongTileList[j + 1].GetNumType()) continue;
                        }
                        tileOne = mahjongTileList[0];
                        if (_Is3DotN(tileOne, tileTwo, tileThree))
                        {
                            mahjongTileList.Remove(tileOne);
                            mahjongTileList.Remove(tileTwo);
                            mahjongTileList.Remove(tileThree);
                            GetNeedRedNumToBe3Dot(mahjongTileList, needNum);
                            mahjongTileList.Add(tileThree);
                            mahjongTileList.Add(tileTwo);
                            mahjongTileList.Add(tileOne);
                            mahjongTileList.Sort(CompareTile);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 特殊牌型分析.每一种玩法都有可能含有特殊牌型的胡牌放式 如:小七对 十三幺等等.这里基类做最普通的小七对.具体玩法再扩充此方法
        /// </summary>
        /// <returns></returns>
        public virtual bool SpecialAnglyseTile(List<MahjongTile> handTileList)
        {
            if (handTileList.Count == MahjongConstValue.MahjongZhuangAmount || handTileList.Count == MahjongConstValue.MahjongLeisureAmount)
            {
                bool hasSingle = false;

                int count = 1;
                TileDescType oldDescType = handTileList[0].GetDescType();
                TileNumType oldNumType = handTileList[0].GetNumType();
                //List<MahjongTile> _ReadyHandTileList = new List<MahjongTile>();

                for (int i = 1; i < handTileList.Count; ++i)
                {
                    TileDescType currDescType = handTileList[i].GetDescType();
                    TileNumType currNumType = handTileList[i].GetNumType();
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
                        // 14张 发现单牌就认为没法胡了
                        if (handTileList.Count == MahjongConstValue.MahjongZhuangAmount)
                        {
                            return false;
                        }
                        else if (!hasSingle)//13张先标记单调的牌
                        {
                            hasSingle = true;
                            //_ReadyHandTileList.Add(handTileList[i]);
                        }
                        else
                        {
                            //如果已经标记完了单调的牌 再出现单张.则没有听牌.
                            //把之前加到听牌列表的最后一张牌移除.
                            //_ReadyHandTileList.RemoveAt(_ReadyHandTileList.Count - 1);
                            return false;
                        }
                    }
                }

                // 遍历完毕还没有出现return除去 说明听牌或者胡牌
                return true;
            }

            return false;
        }

        // 返回胡牌类型
        public virtual CSSpecialWinType WinHandTileCheck(List<MahjongTile> handTileList, List<Meld> meldList, bool b7Pairs = false, bool bFakeHu = false)
        {
            if (AnalyseHandTile(handTileList, meldList))
            {
                return CSSpecialWinType.WT_PingWin;
            }
            return CSSpecialWinType.WT_None;
        }

        //public bool AnalyseHandTile(List<MahjongTile> handTileList)
        //{
        //    // 统计数量.
        //    m_DragonTileList.Clear();
        //    m_WindTileList.Clear();
        //    m_DotTileList.Clear();
        //    m_BambooTileList.Clear();
        //    m_CharacterTileList.Clear();

        //    for (int i = 0; i < handTileList.Count; ++i)
        //    {
        //        MahjongTile tile = handTileList[i];
        //        switch (tile.GetDescType())
        //        {
        //            case TileDescType.ETD_Dragon:
        //                m_DragonTileList.Add(tile);
        //                break;
        //            case TileDescType.ETD_Wind:
        //                m_WindTileList.Add(tile);
        //                break;
        //            case TileDescType.ETD_Dot:
        //                m_DotTileList.Add(tile);
        //                break;
        //            case TileDescType.ETD_Bamboo:
        //                m_BambooTileList.Add(tile);
        //                break;
        //            case TileDescType.ETD_Character:
        //                m_CharacterTileList.Add(tile);
        //                break;
        //            default:
        //                return false;
        //        }
        //    }
        //    return true;
        //}

        // 手牌分析.刚抓到一张牌之后做手牌分析.也可为服务器发牌之后第一次数据分析.（闲家第一次只分析是否听牌 看麻将玩法是否有此需求.如没有听牌需求 不必要调用此.听牌判定比胡牌判定慢很多.）
        public virtual bool AnalyseHandTile(List<MahjongTile> handTileList, List<Meld> meldList, bool b7Pires = false, bool bFakeHu = false, bool bReadHand = false, bool bTriplet = false)
        {
            // 前提.手牌必须是补花和排序之后的 否者无法快速分析牌型
            // 注意:这里不分析特殊胡牌牌型.比如小七对.将将胡.十三幺.国士无双等牌型.

            // 胡牌逻辑需要对数据进行如下统计:
            // 1.判定是需要判定胡牌还是听牌.
            // 2.每种牌型的数量统计出来(看是否满足3n+2 或者 3n 或者 2)
            // 3.对于有3n+2的牌型.判定去掉将判定是否符合3N的组合。

            // 统计数量.
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
                if (_DragonTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                if (_WindTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                if (_DotTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                if (_BambooTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                if (_CharacterTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }

                // 做将的牌型多于1则不需要判定了 不能胡
                if (nPairsCount != 1)
                {
                    return false;
                }

                //Step2:确认所有牌组是否都符合3*n + 2 或者 3×n的组合
                if (_DoAnalyseTils(_DragonTileList, true) && _DoAnalyseTils(_WindTileList, true) && _DoAnalyseTils(_DotTileList, bTriplet)
                    && _DoAnalyseTils(_BambooTileList, bTriplet) && _DoAnalyseTils(_CharacterTileList, bTriplet))
                {
                    return true;
                }

                return false;
            }
            else if (bReadHand)
            {
                // 判定听牌.
                List<MahjongTile> _ReadyHandTileList = new List<MahjongTile>();

                int nPairsCount = 0;
                int nSingleCount = 0;
                if (_DragonTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                else if (_DragonTileList.Count % 3 == 1)
                {
                    nSingleCount++;
                }

                if (_WindTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                else if (_WindTileList.Count % 3 == 1)
                {
                    nSingleCount++;
                }

                if (_DotTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                else if (_DotTileList.Count % 3 == 1)
                {
                    nSingleCount++;
                }

                if (_BambooTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                else if (_BambooTileList.Count % 3 == 1)
                {
                    nSingleCount++;
                }

                if (_CharacterTileList.Count % 3 == 2)
                {
                    nPairsCount++;
                }
                else if (_CharacterTileList.Count % 3 == 1)
                {
                    nSingleCount++;
                }

                // 剩下两种牌数量的时候可能听牌.  条件1为对倒或者坎张牌型. 条件2为单调将或者坎张牌型.
                if ((nPairsCount == 2 && nSingleCount == 0) || (nSingleCount == 1 && nPairsCount == 0))
                {
                    return false;
                }

                #region Foreach 所有牌
                // 遍历所有的牌 如果能胡牌 则为听牌. 暂时没有找到更优办法.(癞子玩法不能如此判定)
                if (_DragonTileList.Count > 0)
                {
                    List<MahjongTile> _tempTileList = ConcealedKongCheck(_DragonTileList);

                    for (int i = 0; i < MahjongOriginalManager.Instance.m_DragonTotalTiles.Length; ++i)
                    {
                        // 如果这张牌可以暗杠.那这张牌的判定没有意义.
                        if (_tempTileList.Count > 0 && _tempTileList.Exists(elmenet => elmenet.Equal(MahjongOriginalManager.Instance.m_DragonTotalTiles[i])))
                        {
                            continue;
                        }

                        MahjongTile tile = new MahjongTile(MahjongOriginalManager.Instance.m_DragonTotalTiles[i]);
                        handTileList.Add(tile);
                        handTileList.Sort(CompareTile);
                        if (AnalyseHandTile(handTileList, meldList, b7Pires, bFakeHu, false, bTriplet))
                        {
                            _ReadyHandTileList.Add(tile);
                        }
                        handTileList.Remove(tile);
                    }
                }

                if (_WindTileList.Count > 0)
                {
                    List<MahjongTile> _tempTileList = ConcealedKongCheck(_WindTileList);

                    for (int i = 0; i < MahjongOriginalManager.Instance.m_WindTotalTiles.Length; ++i)
                    {
                        if (_tempTileList.Count > 0 && _tempTileList.Exists(elmenet => elmenet.Equal(MahjongOriginalManager.Instance.m_WindTotalTiles[i])))
                        {
                            continue;
                        }

                        MahjongTile tile = new MahjongTile(MahjongOriginalManager.Instance.m_WindTotalTiles[i]);
                        handTileList.Add(tile);
                        handTileList.Sort(CompareTile);
                        if (AnalyseHandTile(handTileList, meldList, b7Pires, bFakeHu, false, bTriplet))
                        {
                            _ReadyHandTileList.Add(tile);
                        }
                        handTileList.Remove(tile);
                    }
                }

                if (_DotTileList.Count > 0)
                {
                    List<MahjongTile> _tempTileList = ConcealedKongCheck(_DotTileList);

                    for (int i = 0; i < MahjongOriginalManager.Instance.m_DotTotalTiles.Length; ++i)
                    {
                        if (_tempTileList.Count > 0 && _tempTileList.Exists(elmenet => elmenet.Equal(MahjongOriginalManager.Instance.m_DotTotalTiles[i])))
                        {
                            continue;
                        }

                        MahjongTile tile = new MahjongTile(MahjongOriginalManager.Instance.m_DotTotalTiles[i]);
                        handTileList.Add(tile);
                        handTileList.Sort(CompareTile);
                        if (AnalyseHandTile(handTileList, meldList, b7Pires, bFakeHu, false, bTriplet))
                        {
                            _ReadyHandTileList.Add(tile);
                        }
                        handTileList.Remove(tile);
                    }
                }

                if (_BambooTileList.Count > 0)
                {
                    List<MahjongTile> _tempTileList = ConcealedKongCheck(_BambooTileList);

                    for (int i = 0; i < MahjongOriginalManager.Instance.m_BambooTotalTiles.Length; ++i)
                    {
                        if (_tempTileList.Count > 0 && _tempTileList.Exists(elmenet => elmenet.Equal(MahjongOriginalManager.Instance.m_BambooTotalTiles[i])))
                        {
                            continue;
                        }

                        MahjongTile tile = new MahjongTile(MahjongOriginalManager.Instance.m_BambooTotalTiles[i]);
                        handTileList.Add(tile);
                        handTileList.Sort(CompareTile);
                        if (AnalyseHandTile(handTileList, meldList, b7Pires, bFakeHu, false, bTriplet))
                        {
                            _ReadyHandTileList.Add(tile);
                        }
                        handTileList.Remove(tile);
                    }
                }

                if (_CharacterTileList.Count > 0)
                {
                    List<MahjongTile> _tempTileList = ConcealedKongCheck(_CharacterTileList);

                    for (int i = 0; i < MahjongOriginalManager.Instance.m_CharacterTotalTiles.Length; ++i)
                    {
                        if (_tempTileList.Count > 0 && _tempTileList.Exists(elmenet => elmenet.Equal(MahjongOriginalManager.Instance.m_CharacterTotalTiles[i])))
                        {
                            continue;
                        }

                        MahjongTile tile = MahjongOriginalManager.Instance.m_CharacterTotalTiles[i];
                        handTileList.Add(tile);
                        handTileList.Sort(CompareTile);
                        if (AnalyseHandTile(handTileList, meldList, b7Pires, bFakeHu, false, bTriplet))
                        {
                            _ReadyHandTileList.Add(tile);
                        }
                        handTileList.Remove(tile);
                    }
                }

                if (_ReadyHandTileList.Count > 0)
                {
                    return true;
                }

                return false;
                #endregion

                #region 逻辑判定不遍历所有牌
                // Step2:把牌移除之后判定是否为3n如何是.则移除的牌为单调.
                // Step3:把>=2的牌做将剩下的牌判定是否为3n.
                // 有时间再写 -.-  ....
                #endregion

            }
            return false;
        }

        // 比较排序函数(顺序为. 万、筒、索、风、箭、花)
        public virtual int CompareTile(MahjongTile a, MahjongTile b)
        {
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