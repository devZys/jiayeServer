#if MAHJONG
using System;
using System.Collections.Generic;

//麻将基类.包括所有麻将类型的通用属性和通用逻辑.
namespace LegendProtocol
{
    public class MahjongManager
    {
        private static MahjongManager instance = null;
        private static object objLock = new object();

        private MahjongManager() { }

        public static MahjongManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (objLock)
                    {
                        if (instance == null)
                        {
                            instance = new MahjongManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }

        public void Init()
        {
        }

        public MahjongStrategyBase GetMahjongStrategy(MahjongType mahjongType)
        {
            if (mahjongType == MahjongType.ChangShaMahjong)
            {
                return new CSMahjongStrategy();
            }
            else if (mahjongType == MahjongType.ZhuanZhuanMahjong)
            {
                return new ZZMahjongStrategy();
            }
            else if (mahjongType == MahjongType.RedLaiZiMahjong)
            {
                return new RedMahjongStrategy();
            }

            return null;
        }
        public MahjongSetTileBase GetMahjongSetTile(MahjongType mahjongType)
        {
            if (mahjongType == MahjongType.ChangShaMahjong)
            {
                return new CSMahjongSetTile();
            }
            else if (mahjongType == MahjongType.ZhuanZhuanMahjong)
            {
                return new ZZMahjongSetTile();
            }
            else if (mahjongType == MahjongType.RedLaiZiMahjong)
            {
                return new RedMahjongSetTile();
            }

            return null;
        }

    }
    // 原始牌.用来做听牌遍历用
    public class MahjongOriginalManager
    {
        private static MahjongOriginalManager instance = null;
        private static object objLock = new object();

        public MahjongTile[] m_DragonTotalTiles = new MahjongTile[3];
        public MahjongTile[] m_WindTotalTiles = new MahjongTile[4];
        public MahjongTile[] m_DotTotalTiles = new MahjongTile[9];
        public MahjongTile[] m_BambooTotalTiles = new MahjongTile[9];
        public MahjongTile[] m_CharacterTotalTiles = new MahjongTile[9];
        private MahjongOriginalManager() { }
        public static MahjongOriginalManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (objLock)
                    {
                        if (instance == null)
                        {
                            instance = new MahjongOriginalManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }
        public void Init()
        {
            for (int i = 0; i < m_DragonTotalTiles.Length; ++i)
            {
                m_DragonTotalTiles[i] = new MahjongTile(TileBlurType.ETB_Honor, TileDescType.ETD_Dragon, (TileNumType)((int)TileNumType.ETN_Red + i));
            }

            for (int i = 0; i < m_WindTotalTiles.Length; ++i)
            {
                m_WindTotalTiles[i] = new MahjongTile(TileBlurType.ETB_Honor, TileDescType.ETD_Wind, (TileNumType)((int)TileNumType.ETN_East + i));
            }

            for (int i = 0; i < m_DotTotalTiles.Length; ++i)
            {
                m_DotTotalTiles[i] = new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, (TileNumType)((int)TileNumType.ETN_One + i));
            }

            for (int i = 0; i < m_BambooTotalTiles.Length; ++i)
            {
                m_BambooTotalTiles[i] = new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, (TileNumType)((int)TileNumType.ETN_One + i));
            }

            for (int i = 0; i < m_CharacterTotalTiles.Length; ++i)
            {
                m_CharacterTotalTiles[i] = new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, (TileNumType)((int)TileNumType.ETN_One + i));
            }
        }
    }
}
#endif


