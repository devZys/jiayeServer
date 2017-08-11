#if WORDPLATE
using LegendProtocol;
using LegendServerLogic.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;

// 作为一个通用字牌玩家基类.(简单理解为一个牌张列表数据集,以及这些牌产生的逻辑)

namespace LegendServerLogic.Entity.Players
{
    public class WordPlatePlayer : Player
    {
        //庄闲
        public ZhuangLeisureType zhuangLeisureType;
        //玩家状态
        public WordPlatePlayerStatus housePlayerStatus;
        //是否死手
        public bool m_bDeadHand;
        //是否放弃胡牌
        public bool m_bGiveUpWin;
        //报听
        public bool m_bBaoTing;
        //胡牌次数
        public int m_nWinAmount;
        //胡牌总息数
        public int m_nAllWinScore;
        //基础胡牌积分
        public int baseScore;
        //有没有名堂
        public bool bFamous;

        //上一次歪的牌
        public WordPlateTile lastOperatTile;
        //本次操作是否是桌面上的牌
        public bool bOperatMeld;
        // 发牌的张数.手牌列表
        private List<WordPlateTile> m_handTileList = new List<WordPlateTile>();
        // 已经吃碰杠的牌,不在手牌中了
        private List<WordPlateMeld> m_displayTileList = new List<WordPlateMeld>();
        // 出牌列表
        private List<WordPlateTile> m_showTileList = new List<WordPlateTile>();
        //吃臭牌
        private List<WordPlateTile> m_passChowTileList = new List<WordPlateTile>();
        //碰臭牌
        private List<WordPlateTile> m_passPongTileList = new List<WordPlateTile>();
        //歪臭牌
        //private List<WordPlateTile> m_passWaiTileList = new List<WordPlateTile>();

        // 临时列表 用来返回的 不用在每个函数里new出来
        private List<WordPlateTile> m_tempTileList = new List<WordPlateTile>();
        // 胡牌后的组合系列
        private List<WordPlateMeld> m_tempMeldList = new List<WordPlateMeld>();

        // 字牌逻辑策略
        private WordPlateStrategyBase m_strategy;

        // 需要由服务器提供自己的手牌. 
        public WordPlatePlayer() { }

        public WordPlatePlayer(WordPlateStrategyBase strategy)
        {
            index = 0;
            allIntegral = 0;
            lineType = LineType.OnLine;
            sex = UserSex.Shemale;
            zhuangLeisureType = ZhuangLeisureType.Leisure;
            housePlayerStatus = WordPlatePlayerStatus.WordPlateFree;
            voteStatus = VoteStatus.FreeVote;
            m_strategy = strategy;
            m_bDeadHand = false;
            m_bGiveUpWin = false;
            bOperatMeld = false;
            m_bBaoTing = true;
            m_nWinAmount = 0;
            m_nAllWinScore = 0;
            baseScore = 0;
            bFamous = false;
            longitude = 0.0f;
            latitude = 0.0f;
            bHosted = false;
            lastOperatTile = null;
        }

        public int GetHeadWordPlateCount()
        {
            return m_handTileList.Count;
        }

        // 由策略自己进行排序
        public void SortTile()
        {
            m_handTileList.Sort(m_strategy.CompareTile);
        }
        //清理牌
        public void InitWordPlateTileList()
        {
            m_handTileList.Clear();
            m_displayTileList.Clear();
            m_showTileList.Clear();
            m_passChowTileList.Clear();
            m_passPongTileList.Clear();
            m_bDeadHand = false;
            m_bGiveUpWin = false;
            bOperatMeld = false;
            m_bBaoTing = true;
            lastOperatTile = null;
        }
        //设置手牌
        public void SetHandTileList(List<WordPlateTile> handTileList)
        {
            InitWordPlateTileList();
            m_handTileList.AddRange(handTileList);
            SortTile();
        }    
        //设置GL手牌
        public void GetPlayerHandTileList(List<int> wordPlateList)
        {
            m_handTileList.ForEach(wordPlateTile =>
            {
                wordPlateList.Add(wordPlateTile.GetWordPlateNode());
            });
        }  
        //获取玩家最后一个手牌
        public int GetPlayerHandWordPlateEndNode()
        {
            WordPlateTile wordPlateTile = m_handTileList.LastOrDefault();
            if (wordPlateTile != null)
            {
                return wordPlateTile.GetWordPlateNode();
            }
            return 0;
        }
        //设置GL出牌
        public void GetPlayerShowTileList(List<int> wordPlateList)
        {
            foreach (WordPlateTile wordPlateTile in m_showTileList)
            {
                wordPlateList.Add(wordPlateTile.GetWordPlateNode());
            }
        }
        //设置GL显牌
        public void GetPlayerWordPlateMeldList(List<PlateMeldNode> displayWordPlateList)
        {
            foreach(WordPlateMeld meld in m_displayTileList)
            {
                PlateMeldNode meldNode = new PlateMeldNode();
                meldNode.meldType = meld.m_eMeldType;
                foreach (WordPlateTile wordPlateTile in meld.m_meldTileList)
                {
                    meldNode.meldTileList.Add(wordPlateTile.GetWordPlateNode());
                }
                displayWordPlateList.Add(meldNode);
            }
        }        
        //设置GL吃臭牌
        public void GetPlayerPassChowTileList(List<int> passChowTileList)
        {
            foreach (WordPlateTile wordPlateTile in m_passChowTileList)
            {
                passChowTileList.Add(wordPlateTile.GetWordPlateNode());
            }
        }
        public void AddShowWordPlate(WordPlateTile wordPlateTile)
        {
            if (wordPlateTile == null)
            {
                return;
            }
            m_showTileList.Add(wordPlateTile);
        }
        //检查手牌  
        public bool CheckPlayerShowWordPlate(int remainder = 2)
        {
            if (m_handTileList.Count % 3 == remainder)
            {
                return true;
            }
            return false;
        }
        public bool CheckPlayerWordPlate(int wordPlateNode)
        {
            if (wordPlateNode == 0)
            {
                return false;
            }
            return CheckPlayerWordPlate(new WordPlateTile(wordPlateNode));
        }
        public bool CheckPlayerWordPlate(WordPlateTile wordPlateTile)
        {
            if (wordPlateTile == null)
            {
                return false;
            }
            return m_handTileList.Exists(element => element.Equal(wordPlateTile));
        }
        //删除出牌
        public void DelPlayerWordPlate(int wordPlateNode)
        {
            if (wordPlateNode == 0)
            {
                return;
            }
            DelPlayerWordPlate(new WordPlateTile(wordPlateNode));
        }         
        //删除出牌
        public void DelPlayerWordPlate(WordPlateTile wordPlateTile, bool bBaoTing = false)
        {
            if (wordPlateTile == null)
            {
                return;
            }
            if (m_bBaoTing && bBaoTing && m_displayTileList.Count > 0)
            {
                m_bBaoTing = false;
            }
            WordPlateTile tile = m_handTileList.Find(element => element.Equal(wordPlateTile));
            if (tile != null)
            {
                m_handTileList.Remove(tile);
            }
        }
        //设置报听胡
        public void SetBaoTingHu()
        {
            if (m_bBaoTing && (m_displayTileList.Exists(element => element.m_eMeldType == PlateMeldType.EPM_Sequence) ||
                m_displayTileList.Exists(element => element.m_eMeldType == PlateMeldType.EPM_Pong) || m_displayTileList.Exists(element => element.m_eMeldType == PlateMeldType.EPM_Wai)))
            {
                m_bBaoTing = false;
            }
        }
        // 判定是否可以吃牌.
        public bool ChowCheck(int wordPlateNode)
        {
            if (wordPlateNode == 0)
            {
                return false;
            }
            return ChowCheck(new WordPlateTile(wordPlateNode));
        }
        // 判定是否可以吃牌.(上家打牌后判定)
        public bool ChowCheck(WordPlateTile targetTile)
        {
            if (targetTile == null || CheckPassChowTile(targetTile))
            {
                return false;
            }
            return m_strategy.ChowCheck(targetTile, m_handTileList, m_passChowTileList);
        }
        public bool CheckChowWordPlate(List<int> chowWordPlateList)
        {
            if (chowWordPlateList == null || chowWordPlateList.Count != 3)
            {
                return false;
            }
            List<WordPlateTile> _chowWordPlateList = new List<WordPlateTile>();
            for(int i = 1; i < chowWordPlateList.Count; ++i)
            {
                _chowWordPlateList.Add(new WordPlateTile(chowWordPlateList[i]));
            }
            if (_chowWordPlateList.Exists(element => !CheckPlayerWordPlate(element)))
            {
                return false;
            }
            return m_strategy.ChowCheck(new WordPlateTile(chowWordPlateList[0]), _chowWordPlateList, m_passChowTileList);
        }
        public WordPlateMeld Chow(List<int> wordPlateNode)
        {
            if (wordPlateNode == null || wordPlateNode.Count != 3)
            {
                return null;
            }
            WordPlateTile targetTile = new WordPlateTile(wordPlateNode[0]);
            WordPlateTile suitTile1 = m_handTileList.Find(element => element.Equal(new WordPlateTile(wordPlateNode[1])));
            WordPlateTile suitTile2 = m_handTileList.Find(element => element.Equal(new WordPlateTile(wordPlateNode[2])));
            bOperatMeld = false;
            return Chow(suitTile1, suitTile2, targetTile);
        }
        public WordPlateMeld Chow(WordPlateTile suitTile1, WordPlateTile suitTile2, WordPlateTile targetTile)
        {
            WordPlateMeld meld = m_strategy.Chow(suitTile1, suitTile2, targetTile);

            if (meld != null)
            {
                m_displayTileList.Add(meld);
                m_handTileList.Remove(suitTile1);
                m_handTileList.Remove(suitTile2);
            }
            return meld;
        }
        public WordPlateMeld PongWai(List<int> wordPlateNode, WordPlateOperatType operatType)
        {
            if (wordPlateNode == null || wordPlateNode.Count != 1 || (operatType != WordPlateOperatType.EWPO_Pong && operatType != WordPlateOperatType.EWPO_Wai))
            {
                return null;
            }
            WordPlateTile targetTile = new WordPlateTile(wordPlateNode[0]);
            List<WordPlateTile> suitTileList = m_handTileList.FindAll(element => element.Equal(targetTile));
            if (suitTileList == null || suitTileList.Count < 2)
            {
                return null;
            }
            bOperatMeld = false;
            return PongWai(suitTileList[0], suitTileList[1], targetTile, operatType == WordPlateOperatType.EWPO_Wai);
        }
        public WordPlateMeld PongWai(WordPlateTile suitTile1, WordPlateTile suitTile2, WordPlateTile targetTile, bool bWai)
        {
            WordPlateMeld meld = m_strategy.PongWai(suitTile1, suitTile2, targetTile, bWai);

            if (meld != null)
            {
                m_displayTileList.Add(meld);
                m_handTileList.Remove(suitTile1);
                m_handTileList.Remove(suitTile2);
            }
            return meld;
        }
        public WordPlateMeld Flutter(List<int> wordPlateNode, bool bMySelf)
        {
            if (wordPlateNode == null || wordPlateNode.Count == 0 || wordPlateNode.Count > 2)
            {
                return null;
            }
            WordPlateTile targetTile = new WordPlateTile(wordPlateNode[0]);
            if (bMySelf)
            {
                WordPlateMeld meld = FlutterSlipCheck(targetTile);
                if (meld != null && meld.m_eMeldType == PlateMeldType.EPM_Pong)
                {
                    meld.m_eMeldType = PlateMeldType.EPM_Flutter;
                    meld.m_meldTileList.Add(targetTile);
                    bOperatMeld = true;
                    return meld;
                }
            }
            List<WordPlateTile> suitTileList = m_handTileList.FindAll(element => element.Equal(targetTile));
            if (suitTileList == null || suitTileList.Count != 3)
            {
                return null;
            }
            bOperatMeld = false;
            return FlutterSlip(suitTileList[0], suitTileList[1], suitTileList[2], targetTile, false);
        }
        public WordPlateMeld Slip(List<int> wordPlateNode)
        {
            if (wordPlateNode == null || wordPlateNode.Count == 0 || wordPlateNode.Count > 2)
            {
                return null;
            }
            WordPlateTile targetTile = new WordPlateTile(wordPlateNode[0]);
            List<WordPlateTile> suitTileList = m_handTileList.FindAll(element => element.Equal(targetTile));
            if (suitTileList == null || suitTileList.Count != 3)
            {
                WordPlateMeld meld = FlutterSlipCheck(targetTile);
                if (meld != null && meld.m_eMeldType == PlateMeldType.EPM_Wai)
                {
                    meld.m_eMeldType = PlateMeldType.EPM_Slip;
                    meld.m_meldTileList.Add(targetTile);
                    bOperatMeld = true;
                    return meld;
                }
                return null;
            }
            bOperatMeld = false;
            return FlutterSlip(suitTileList[0], suitTileList[1], suitTileList[2], targetTile);
        }
        public WordPlateMeld FlutterSlip(WordPlateTile suitTile1, WordPlateTile suitTile2, WordPlateTile suitTile3, WordPlateTile targetTile, bool bSlip = true)
        {
            WordPlateMeld meld = m_strategy.FlutterSlip(suitTile1, suitTile2, suitTile3, targetTile, bSlip);

            if (meld != null)
            {
                m_displayTileList.Add(meld);
                m_handTileList.Remove(suitTile1);
                m_handTileList.Remove(suitTile2);
                m_handTileList.Remove(suitTile3);
            }
            return meld;
        }
        public WordPlateMeld Slip(WordPlateTile suitTile1, WordPlateTile suitTile2, WordPlateTile suitTile3, WordPlateTile suitTile4)
        {
            WordPlateMeld meld = m_strategy.FlutterSlip(suitTile1, suitTile2, suitTile3, suitTile4);

            if (meld != null)
            {
                _AddDisplayMeldRemoveHandTile(meld);
            }
            bOperatMeld = false;
            return meld;
        }
        private void _AddDisplayMeldRemoveHandTile(WordPlateMeld meld)
        {
            m_displayTileList.Add(meld);

            for (int i = 0; i < meld.m_meldTileList.Count; ++i)
            {
                m_handTileList.Remove(meld.m_meldTileList[i]);
            }
        }
        // 判定是否可以碰或者飘 溜(任何人打牌后判定).
        public PlateMeldType PongSlipCheck(int wordPlateNode)
        {
            if (wordPlateNode == 0)
            {
                return PlateMeldType.EPM_None;
            }
            return PongSlipCheck(new WordPlateTile(wordPlateNode));
        }
        public PlateMeldType PongSlipCheck(WordPlateTile targetTile)
        {
            if (targetTile == null)
            {
                return PlateMeldType.EPM_None;
            }
            return m_strategy.PongSlipCheck(targetTile, m_handTileList);
        }   
        
        // 判定自己是否可以补飘 溜
        public WordPlateMeld FlutterSlipCheck(int wordPlateNode, bool bMyHand = true)
        {
            if (wordPlateNode == 0)
            {
                return null;
            }
            if (bMyHand && !CheckPlayerWordPlate(wordPlateNode))
            {
                return null;
            }
            return FlutterSlipCheck(new WordPlateTile(wordPlateNode));
        }
        public WordPlateMeld FlutterSlipCheck(WordPlateTile targetTile)
        {
            return m_strategy.FlutterSlipCheck(targetTile, m_displayTileList);
        }
        //判断这个牌吃了没有
        public bool ChowMeldCheck(WordPlateTile targetTile)
        {
            return m_strategy.ChowMeldCheck(targetTile, m_displayTileList);
        }
        //判断是否存在死手
        public bool DeadHandMeldCheck(WordPlateTile targetTile)
        {
            if (m_bDeadHand)
            {
                return true;
            }
            return m_strategy.DeadHandMeldCheck(targetTile, m_displayTileList);
        }
        public void SetShowPassTile(WordPlateTile targetTile)
        {
            if (targetTile == null)
            {
                return;
            }
            //吃臭
            SetPassChowTile(targetTile);
            //碰臭
            if (!m_passChowTileList.Exists(element => element.Equal(targetTile)))
            {
                List<WordPlateTile> _targetTileList = m_handTileList.FindAll(element => element.Equal(targetTile));
                if (_targetTileList != null && _targetTileList.Count == 2)
                {
                    m_passPongTileList.Add(targetTile);
                }
            }
        }
        //设置碰臭牌
        public void SetPassPongTile(WordPlateTile targetTile)
        {
            if (targetTile != null && !CheckPassPongTile(targetTile))
            {
                m_passPongTileList.Add(targetTile);
            }
        }
        //检测碰臭牌   
        public bool CheckPassPongTile(WordPlateTile targetTile)
        {
            if (targetTile != null)
            {
                if (m_passPongTileList.Exists(element => element.Equal(targetTile)))
                {
                    return true;
                }
            }
            return false;
        }
        //设置吃臭牌
        public bool SetPassChowTile(WordPlateTile targetTile)
        {
            if (targetTile != null && !m_passChowTileList.Exists(element => element.Equal(targetTile)))
            {
                m_passChowTileList.Add(targetTile);
                return true;
            }
            return false;
        }
        //检测吃臭牌   
        private bool CheckPassChowTile(WordPlateTile targetTile)
        {
            if (targetTile != null)
            {
                return m_passChowTileList.Exists(element => element.Equal(targetTile));
            }
            return false;
        }
        // 获取目标牌列表 
        public List<WordPlateTile> GetTileList(int wordPlateNode)
        {
            if (wordPlateNode == 0)
            {
                return null;
            }
            return GetTileList(new WordPlateTile(wordPlateNode));
        }
        public List<WordPlateTile> GetTileList(WordPlateTile targetTile)
        {
            m_tempTileList.Clear();

            for (int i = 0; i < m_handTileList.Count; ++i)
            {
                WordPlateTile tile = m_handTileList[i];
                if (tile.Equal(targetTile))
                {
                    m_tempTileList.Add(tile);
                }
            }

            return m_tempTileList;
        }
        //胡牌判断    
        public bool WinHandTileCheck()
        {
            return WinHandTileCheck(m_handTileList);
        }     
        //胡牌判断
        public bool WinHandTileCheck(List<int> wordPlateList, bool bMyself = false)
        {
            if (wordPlateList == null || wordPlateList.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < wordPlateList.Count; ++i)
            {
                if (!WinHandTileCheck(wordPlateList[i], bMyself))
                {
                    return false;
                }
            }
            return true;
        }
        //胡牌判断
        public bool WinHandTileCheck(int wordPlateNode, bool bMyself = false)
        {
            if (wordPlateNode == 0)
            {
                return false;
            }
            return WinHandTileCheck(new WordPlateTile(wordPlateNode), bMyself);
        }
        //胡牌判断
        public bool WinHandTileCheck(WordPlateTile targetTile, bool bMyself = false)
        {
            if (targetTile == null)
            {
                return false;
            }
            List<WordPlateTile> checkTileList = new List<WordPlateTile>();
            checkTileList.AddRange(m_handTileList);
            checkTileList.Add(targetTile);
            //排序
            checkTileList.Sort(m_strategy.CompareTile);
            return WinHandTileCheck(checkTileList, targetTile, bMyself);
        }  
        //胡牌判断 
        private bool WinHandTileCheck(List<WordPlateTile> headTileList, WordPlateTile huTile = null, bool bMyself = false)
        {
            m_tempMeldList.Clear();
            m_tempMeldList.AddRange(m_displayTileList);
            return m_strategy.AnalyseHandTile(headTileList, m_tempMeldList, baseScore, bFamous, huTile, bMyself);
        }
        //判断胡牌牌组
        public bool CheckHuPlateMeld()
        {
            return m_tempMeldList.Count == 0;
        }
        public bool IsWaiHu(WordPlateTile huTile, bool bMySelf)
        {
            if (lastOperatTile != null || huTile == null)
            {
                //操作后再胡牌 不用加入（因为加入也是null）
                return true;
            }
            if (!bMySelf)
            {
                //不是自己摸得牌肯定不是歪
                return false;
            }
            return m_strategy.IsWaiHu(m_displayTileList, m_tempMeldList, huTile);
        }
        //获取积分和番
        public WordPlateWinSorce GetWordPlateMeldScoreAndFan(List<WordPlateFanNode> wordPlateFanList, WordPlateTile huTile, bool bFamous, bool bBigSmallHu, bool bBaoTing)
        {
            if (bBaoTing && m_bBaoTing)
            {
                wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_BaoTingHu, fanCount = 4 });
            }
            //手牌中不属于自己的牌
            List<WordPlateTile> noHandWordPlateList = new List<WordPlateTile>();
            m_displayTileList.ForEach(element =>
            {
                if (element.m_eMeldType == PlateMeldType.EPM_Sequence && element.m_meldTileList.Count == 3)
                {
                    noHandWordPlateList.Add(element.m_meldTileList[0]);
                }
            });
            if (huTile != null)
            {
                noHandWordPlateList.Add(huTile);
            }
            return m_strategy.GetWordPlateMeldScoreAndFan(m_tempMeldList, wordPlateFanList, bFamous, bBigSmallHu, noHandWordPlateList);
        }
    }
}
#endif