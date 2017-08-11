#if MAHJONG
// 每一张牌的数据 麻将牌基础数据.麻将牌。包括 筒 索 万 字 花.(字和花可以分得更加细)

namespace LegendProtocol
{
    public class MahjongTile
    {
        private TileBlurType m_eBlurType;
        private TileDescType m_eDescType;
        private TileNumType m_eNumType;

        public MahjongTile(TileBlurType blurType, TileDescType descType, TileNumType numType)
        {
            m_eBlurType = blurType;
            m_eDescType = descType;
            m_eNumType = numType;
        }
        public MahjongTile(MahjongTile mahjongTile)
        {
            m_eBlurType = mahjongTile.GetBlurType();
            m_eDescType = mahjongTile.GetDescType();
            m_eNumType = mahjongTile.GetNumType();
        }
        public MahjongTile(int mahjongNode)
        {
            int blurTyp = mahjongNode % 10;
            m_eBlurType = (TileBlurType)blurTyp;

            int mahjongNum = mahjongNode / 10;
            int descType = mahjongNum % 10;
            m_eDescType = (TileDescType)descType;

            int numType = mahjongNum / 10;
            m_eNumType = (TileNumType)numType;
        }
        public int GetMahjongNode()
        {
            return (int)m_eNumType * 100 + (int)m_eDescType * 10 + (int)m_eBlurType;
        }
        static public bool Equals(MahjongTile tileA, MahjongTile tileB)
        {
            return tileA.GetDescType() == tileB.GetDescType() && tileA.GetNumType() == tileB.GetNumType();
        }
        public void Init(TileBlurType blurType, TileDescType descType, TileNumType numType)
        {
            m_eBlurType = blurType;
            m_eDescType = descType;
            m_eNumType = numType;
        }
        public bool IsFlower()
        {
            return m_eBlurType == TileBlurType.ETB_Flower;
        }
        public bool IsHonor()
        {
            return m_eBlurType == TileBlurType.ETB_Honor;
        }
        public bool IsSuit()
        {
            return m_eBlurType == TileBlurType.ETB_Suit;
        }
        public bool IsDragon()
        {
            return m_eDescType == TileDescType.ETD_Dragon;
        }
        public bool IsWind()
        {
            return m_eDescType == TileDescType.ETD_Wind;
        }
        public bool IsDot()
        {
            return m_eDescType == TileDescType.ETD_Dot;
        }
        public bool IsBamboo()
        {
            return m_eDescType == TileDescType.ETD_Bamboo;
        }
        public bool IsCharacter()
        {
            return m_eDescType == TileDescType.ETD_Character;
        }
        // 某些癞子法需要对此进行判定.
        public bool IsRed()
        {
            return m_eNumType == TileNumType.ETN_Red;
        }
        //是否为将
        public bool IsPairs()
        {
            return m_eNumType == TileNumType.ETN_Two || m_eNumType == TileNumType.ETN_Five || m_eNumType == TileNumType.ETN_Eight;
        }
        // 是否为指定牌
        public bool Equal(MahjongTile tile)
        {
            return m_eDescType == tile.GetDescType() && m_eNumType == tile.GetNumType();
        }
        public TileBlurType GetBlurType()
        {
            return m_eBlurType;
        }

        public TileNumType GetNumType()
        {
            return m_eNumType;
        }

        public TileDescType GetDescType()
        {
            return m_eDescType;
        }
    }
}
#endif