#if WORDPLATE
using System;
using System.Collections.Generic;

//字牌逻辑管理类.包括所有字牌类型的通用属性和通用逻辑.
namespace LegendProtocol
{
    public class WordPlateManager
    {
        private static WordPlateManager instance = null;
        private static object objLock = new object();
        // 当前玩法使用的麻将策略(可根据不同的字牌类型自定义更改,需要具体业务逻辑初始化和调用)
        private Dictionary<WordPlateType, WordPlateStrategyBase> mahjongStrategyDictionary = new Dictionary<WordPlateType, WordPlateStrategyBase>();

        private WordPlateManager() { }

        public static WordPlateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (objLock)
                    {
                        if (instance == null)
                        {
                            instance = new WordPlateManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }

        public void Init()
        {
            foreach (WordPlateType mahjongType in Enum.GetValues(typeof(WordPlateType)))
            {
                if (mahjongType == WordPlateType.WaiHuZiPlate)
                {
                    WhzWordPlateStrategy waiHuZiStrategy = new WhzWordPlateStrategy();
                    SetWordPlateStrategy(mahjongType, waiHuZiStrategy);
                }
            }
        }

        private void SetWordPlateStrategy(WordPlateType mahjongType, WordPlateStrategyBase strategy)
        {
            if (!mahjongStrategyDictionary.ContainsKey(mahjongType))
            {
                mahjongStrategyDictionary.Add(mahjongType, strategy);
            }
        }

        public WordPlateStrategyBase GetWordPlateStrategy(WordPlateType mahjongType)
        {
            if (mahjongStrategyDictionary.ContainsKey(mahjongType))
            {
                return mahjongStrategyDictionary[mahjongType];
            }
            return null;
        }

    }
}
#endif
