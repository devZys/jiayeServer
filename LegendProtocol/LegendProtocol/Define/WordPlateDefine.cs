#if WORDPLATE
using System.Collections.Generic;

namespace LegendProtocol
{
    //常量定义
    public struct WordPlateConstValue
    {
        public const int WordPlateMaxPlayer = 3;       //字牌最大人数
        public const int WordPlateZhuangAmount = 20;    //字牌庄家牌数
        public const int WordPlateLeisureAmount = 19;   //字牌闲家牌数
    }
    //字牌房间属性
    public enum WordPlateHousePropertyType
    {
        EMHP_None = 0,                       //空
        EMHP_Famous = 1,                     //名堂
        EMHP_BigSmallHu = 1 << 1,            //大小字胡
        EMHP_BaoTing = 1 << 2,               //天胡报听
    }
    public class HouseActionNode
    {
        public ulong houseId;
        public byte actionId;
        public int parameter;
        public HouseActionNode() { }
        public HouseActionNode(ulong houseId, byte actionId, int parameter)
        {
            this.houseId = houseId;
            this.actionId = actionId;
            this.parameter = parameter;
        }
    }
    //发牌结构
    public class WordPlateHandTile
    {
        public ZhuangLeisureType zhuangLeisureType = ZhuangLeisureType.Leisure;
        public List<WordPlateTile> wordPlateTileList = new List<WordPlateTile>();
        public WordPlateHandTile() { }
        public WordPlateHandTile(ZhuangLeisureType zhuangLeisureType)
        {
            this.zhuangLeisureType = zhuangLeisureType;
        }
    }
    //胡牌牌组结构
    public class WordPlateHuMeld
    {
        public PlateMeldType meldType;
        public List<WordPlateTile> meldList = new List<WordPlateTile>();
        public int score;
        public bool bHuMeld;
        public WordPlateHuMeld(PlateMeldType meldType, List<WordPlateTile> meldList, int score, bool bHuMeld = false)
        {
            this.meldType = meldType;
            this.meldList.AddRange(meldList);
            this.score = score;
            this.bHuMeld = bHuMeld;
        }
    }
    public class WordPlateWinSorce
    {
        public int winBaseSorce;
        public int winAllSorce;
        public WordPlateWinSorce() { }
        public WordPlateWinSorce(int winBaseSorce, int winAllSorce)
        {
            this.winBaseSorce = winBaseSorce;
            this.winAllSorce = winAllSorce;
        }
    }
    public class WordPlateHuSorceMeld
    {
        public int maxSorce;
        public List<WordPlateMeld> huMeldList;
        public WordPlateHuSorceMeld(int maxSorce, List<WordPlateMeld> huMeldList)
        {
            this.maxSorce = maxSorce;
            this.huMeldList = huMeldList;
        }
    }
    public class WordPlateCount
    {
        public int count;
        public WordPlateTile wordPlateTile;
        public WordPlateCount(WordPlateTile wordPlateTile)
        {
            this.count = 0;
            this.wordPlateTile = wordPlateTile;
        }
    }
}
#endif
