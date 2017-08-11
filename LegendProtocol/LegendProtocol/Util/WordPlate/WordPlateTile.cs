#if WORDPLATE
// 每一张牌的数据 麻将牌基础数据.麻将牌。包括 筒 索 万 字 花.(字和花可以分得更加细)

using System.Collections.Generic;

namespace LegendProtocol
{
    public class WordPlateTile
    {
        private PlateDescType m_eDescType;
        private PlateNumType m_eNumType;

        public WordPlateTile(PlateDescType descType, PlateNumType numType)
        {
            m_eDescType = descType;
            m_eNumType = numType;
        }
        public WordPlateTile(WordPlateTile wordPlateTile)
        {
            m_eDescType = wordPlateTile.GetDescType();
            m_eNumType = wordPlateTile.GetNumType();
        }
        public WordPlateTile(int wordPlateNode)
        {
            int descType = wordPlateNode % 10;
            m_eDescType = (PlateDescType)descType;

            int numType = wordPlateNode / 10;
            m_eNumType = (PlateNumType)numType;
        }
        public int GetWordPlateNode()
        {
            return (int)m_eNumType * 10 + (int)m_eDescType;
        }
        static public bool Equals(WordPlateTile tileA, WordPlateTile tileB)
        {
            return tileA.GetDescType() == tileB.GetDescType() && tileA.GetNumType() == tileB.GetNumType();
        }
        public void Init(PlateDescType descType, PlateNumType numType)
        {
            m_eDescType = descType;
            m_eNumType = numType;
        }
        public bool IsBig()
        {
            return m_eDescType == PlateDescType.EPD_Big;
        }
        public bool IsSmall()
        {
            return m_eDescType == PlateDescType.EPD_Small;
        }
        public bool IsRed()
        {
            return m_eNumType == PlateNumType.EPN_Two || m_eNumType == PlateNumType.EPN_Seven || m_eNumType == PlateNumType.EPN_Ten;
        }
        // 是否为指定牌
        public bool Equal(WordPlateTile tile)
        {
            return m_eDescType == tile.GetDescType() && m_eNumType == tile.GetNumType();
        }
        public PlateNumType GetNumType()
        {
            return m_eNumType;
        }
        public PlateDescType GetDescType()
        {
            return m_eDescType;
        }
    }
    // 牌组(包括手牌中的和桌面上牌的牌组)
    public class WordPlateMeld
    {
        public PlateMeldType m_eMeldType;
        public List<WordPlateTile> m_meldTileList = new List<WordPlateTile>();
        public WordPlateMeld(List<WordPlateTile> tileList, PlateMeldType type)
        {
            m_meldTileList.AddRange(tileList);
            m_eMeldType = type;
        }

        // 顺子 碰牌
        public WordPlateMeld(WordPlateTile tile1, WordPlateTile tile2, WordPlateTile tile3, PlateMeldType type)
        {
            m_meldTileList.Add(tile1);
            m_meldTileList.Add(tile2);
            m_meldTileList.Add(tile3);
            m_eMeldType = type;
        }

        // 溜 飘
        public WordPlateMeld(WordPlateTile tile1, WordPlateTile tile2, WordPlateTile tile3, WordPlateTile tile4, PlateMeldType type)
        {
            m_meldTileList.Add(tile1);
            m_meldTileList.Add(tile2);
            m_meldTileList.Add(tile3);
            m_meldTileList.Add(tile4);
            m_eMeldType = type;
        }

        // 对子.将牌
        public WordPlateMeld(WordPlateTile tile1, WordPlateTile tile2, PlateMeldType type)
        {
            m_meldTileList.Add(tile1);
            m_meldTileList.Add(tile2);
            m_eMeldType = type;
        }
        // 对子.将牌
        public WordPlateMeld(WordPlateTile tile1, WordPlateTile tile2)
        {
            m_meldTileList.Add(tile1);
            m_meldTileList.Add(tile2);
            m_eMeldType = PlateMeldType.EPM_Pair;
        }

        public WordPlateTile GetWordPlateTileByIndex(int index)
        {
            if (index < m_meldTileList.Count)
            {
                return m_meldTileList[index];
            }

            return null;
        }
        public bool CheckWordPlateTile(WordPlateTile tile)
        {
            if (m_meldTileList.Count >= 1)
            {
                return m_meldTileList[0].Equal(tile);
            }

            return false;
        }
    }
}
#endif