using LegendProtocol;
using LegendServerLogic.Entity.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// 作为一个通用麻将玩家基类.(简单理解为一个牌张列表数据集,以及这些牌产生的逻辑)
namespace LegendServerLogic.Entity.Players
{
#if MAHJONG
    public class MahjongPlayer : Player
    {
        //庄闲
        public ZhuangLeisureType zhuangLeisureType;
        //玩家房间状态
        public MahjongPlayerStatus housePlayerStatus;
        //摆牌
        public CSStartDisplayType startDisplayType;
        // 是否听牌
        public bool m_bReadyHand;
        // 是否放弃胡牌
        public bool m_bGiveUpWin;
        //大胡自摸
        public int m_BigWinMyself;
        //小胡自摸（暗杠次数）
        public int m_SmallWinMyself;
        //大胡点炮
        public int m_BigWinFangBlast;
        //小胡点炮（放杠次数）
        public int m_SmallWinFangBlast;
        //大胡接炮
        public int m_BigWinJieBlast;
        //小胡接炮（明杠次数）
        public int m_SmallWinJieBlast;
        //新牌
        public MahjongTile newMahjongTile;
        //刚刚碰的牌
        public MahjongTile currPongTile;
        //是否胡七对
        public bool m_b7Pairs;
        //是否开启假将胡
        public bool m_bOpenFakeHu;
        //是否开启个性摆牌
        public bool m_bPersonalisePendulum;
        //无分杠的牌
        public List<int> noInKongTileList = new List<int>();
        //四喜摆牌列表
        public List<MahjongTile> displayFourTileList = new List<MahjongTile>();
        //标志有没有封顶
        public bool bIntegralCapped;

        // 发牌的张数.手牌列表
        private List<MahjongTile> m_handTileList = new List<MahjongTile>();
        // 已经吃碰杠的牌,不在手牌中了
        private List<Meld> m_displayTileList = new List<Meld>();
        // 出牌列表
        private List<MahjongTile> m_showTileList = new List<MahjongTile>();

        // 临时列表 用来返回的 不用在每个函数里new出来
        private List<MahjongTile> m_tempTileList = new List<MahjongTile>();
        // 起手胡列表
        private List<DisplayMahjongNode> m_displayMahjongList = new List<DisplayMahjongNode>();

        // 麻将逻辑策略
        private MahjongStrategyBase m_strategy;

        // 需要由服务器提供自己的手牌. 
        public MahjongPlayer() { }

        public MahjongPlayer(MahjongStrategyBase strategy)
        {
            index = 0;
            allIntegral = 0;
            lineType = LineType.OnLine;
            sex = UserSex.Shemale;
            zhuangLeisureType = ZhuangLeisureType.Leisure;
            housePlayerStatus = MahjongPlayerStatus.MahjongFree;
            startDisplayType = CSStartDisplayType.SDT_None;
            voteStatus = VoteStatus.FreeVote;
            m_strategy = strategy;
            m_bReadyHand = false;
            m_bGiveUpWin = false;
            m_b7Pairs = false;
            m_bOpenFakeHu = false;
            m_bPersonalisePendulum = false;
            bIntegralCapped = false;
            m_BigWinMyself = 0;
            m_SmallWinMyself = 0;
            m_BigWinFangBlast = 0;
            m_SmallWinFangBlast = 0;
            m_BigWinJieBlast = 0;
            m_SmallWinJieBlast = 0;
            newMahjongTile = null;
            currPongTile = null;
            longitude = 0.0f;
            latitude = 0.0f;
            bHosted = false;
            proxyServerId = 0;
        }

        private void _AddDisplayMeldRemoveHandTile(Meld meld)
        {
            m_displayTileList.Add(meld);

            for (int i = 0; i < meld.m_meldTileList.Count; ++i)
            {
                m_handTileList.Remove(meld.m_meldTileList[i]);
            }
        }
        public int GetHeadMahjongCount()
        {
            return m_handTileList.Count;
        }

        // 由策略自己进行排序
        public void SortTile()
        {
            m_handTileList.Sort(m_strategy.CompareTile);
        }

        //清理牌
        public void InitMahjongTileList()
        {
            m_handTileList.Clear();
            m_displayTileList.Clear();
            m_showTileList.Clear();
            newMahjongTile = null;
            currPongTile = null;
            noInKongTileList.Clear();
            displayFourTileList.Clear();
            m_displayMahjongList.Clear();
            startDisplayType = CSStartDisplayType.SDT_None;
            m_bReadyHand = false;
            m_bGiveUpWin = false;
            bIntegralCapped = false;
        }

        //设置手牌
        public void SetHandTileList(List<MahjongTile> handTileList)
        {
            InitMahjongTileList();
            m_handTileList.AddRange(handTileList);
            SortTile();
        }    

        //设置手牌
        public void SetHandTileList(List<int> handNodeList)
        {
            foreach (int mahjongNode in handNodeList)
            {
                m_handTileList.Add(new MahjongTile(mahjongNode));
            }
            SortTile();
        }

        //设置GL手牌
        public void GetPlayerHandTileList(List<int> mahjongList)
        {
            m_handTileList.ForEach(mahjongTile =>
            {
                mahjongList.Add(mahjongTile.GetMahjongNode());
            });
        }  
        //获取玩家最后一个手牌
        public int GetPlayerHandMahjongEndNode()
        {
            MahjongTile mahjongTile = m_handTileList.LastOrDefault();
            if (mahjongTile != null)
            {
                return mahjongTile.GetMahjongNode();
            }
            return 0;
        }
        //设置GL出牌
        public void SetShowTileList(List<int> showTileList)
        {
            foreach (int mahjongNode in showTileList)
            {
                m_showTileList.Add(new MahjongTile(mahjongNode));
            }
        }

        //设置GL出牌
        public void GetPlayerShowTileList(List<int> mahjongList)
        {
            foreach (MahjongTile mahjongTile in m_showTileList)
            {
                mahjongList.Add(mahjongTile.GetMahjongNode());
            }
        }
        
        public void SetMeldMahjongList(List<MeldNode> meldMahjongList)
        {
            foreach (MeldNode meldNode in meldMahjongList)
            {
                List<MahjongTile> meldTileList = new List<MahjongTile>();
                foreach (int mahjongNode in meldNode.meldTileList)
                {
                    meldTileList.Add(new MahjongTile(mahjongNode));
                }
                Meld meld = new Meld(meldTileList, meldNode.meldType);
                m_displayTileList.Add(meld);
            }
        }

        //设置GL显牌
        public void GetPlayerMeldList(List<MeldNode> displayMahjongList)
        {
            foreach(Meld meld in m_displayTileList)
            {
                MeldNode meldNode = new MeldNode();
                meldNode.meldType = meld.m_eMeldType;
                foreach (MahjongTile mahjongTile in meld.m_meldTileList)
                {
                    meldNode.meldTileList.Add(mahjongTile.GetMahjongNode());
                }
                displayMahjongList.Add(meldNode);
            }
        }
        public void GetPlayerFourTileList(List<int> mahjongList)
        {
            this.displayFourTileList.ForEach(fourTile =>
            {
                mahjongList.Add(fourTile.GetMahjongNode());
            });
        }
        public bool CheckFourTile(MahjongTile mahjongTile)
        {
            if (mahjongTile == null)
            {
                return false;
            }
            return displayFourTileList.Exists(element => element.Equal(mahjongTile));
        }
        //获取玩家杠的数量
        public int GetPlyerKongNum()
        {
            List<Meld> kongMeldList = m_displayTileList.FindAll(elemnet => (elemnet.m_eMeldType == MeldType.EM_ExposedKong || elemnet.m_eMeldType == MeldType.EM_ConcealedKong));
            if (kongMeldList != null)
            {
                return kongMeldList.Count;
            }
            return 0;
        }
        //获取玩家杠的数量
        public int GetPlyerKongNum(MahjongTile mahjongTile)
        {
            List<Meld> kongMeldList = m_displayTileList.FindAll(elemnet => ((elemnet.m_eMeldType == MeldType.EM_ExposedKong && elemnet.m_meldTileList.Count > 0 && !elemnet.m_meldTileList[0].Equal(mahjongTile)) || elemnet.m_eMeldType == MeldType.EM_ConcealedKong));
            if (kongMeldList != null)
            {
                return kongMeldList.Count;
            }
            return 0;
        }

        //增加出牌
        public void AddShowMahjong(int mahjongNode)
        {
            if (mahjongNode == 0)
            {
                return;
            }
            AddShowMahjong(new MahjongTile(mahjongNode));
        }
        public void AddShowMahjong(MahjongTile mahjongTile)
        {
            if (mahjongTile == null)
            {
                return;
            }
            m_showTileList.Add(mahjongTile);
        }
        public void AddShowMahjong(List<MahjongTile> mahjongList)
        {
            if (mahjongList == null)
            {
                return;
            }
            m_showTileList.AddRange(mahjongList);
        }

        //增加手牌
        public void AddNewHeadMahjong(MahjongTile mahjongTile)
        {
            if (mahjongTile == null)
            {
                return;
            }
            m_handTileList.Add(mahjongTile);
            SortTile();
            //恢复胡牌
            if (m_bGiveUpWin)
            {
                m_bGiveUpWin = false;
            }
            newMahjongTile = mahjongTile;
        }

        //检查手牌  
        public bool CheckPlayerShowMahjong(int remainder = 2)
        {
            if (m_handTileList.Count % 3 == remainder)
            {
                return true;
            }
            return false;
        }
        public bool CheckPlayerMahjong(int mahjongNode)
        {
            if (mahjongNode == 0)
            {
                return false;
            }
            return CheckPlayerMahjong(new MahjongTile(mahjongNode));
        }
        public bool CheckPlayerMahjong(MahjongTile mahjongTile)
        {
            if (mahjongTile == null)
            {
                return false;
            }
            return m_handTileList.Exists(element => element.Equal(mahjongTile));
        }
        public bool CheckPlayerRedMahjong()
        {
            return m_handTileList.Exists(elmenet => elmenet.IsRed());
        }

        //删除出牌
        public void DelPlayerMahjong(int mahjongNode)
        {
            if (mahjongNode == 0)
            {
                return;
            }
            DelPlayerMahjong(new MahjongTile(mahjongNode));
        } 
        
        //删除出牌
        public void DelPlayerMahjong(MahjongTile mahjongTile)
        {
            if (mahjongTile == null)
            {
                return;
            }
            MahjongTile tile = m_handTileList.Find(element => element.Equal(mahjongTile));
            if (tile != null)
            {
                m_handTileList.Remove(tile);
            }
        }

        // 判定是否可以吃牌.
        public bool ChowCheck(int mahjongNode)
        {
            if (mahjongNode == 0)
            {
                return false;
            }
            return ChowCheck(new MahjongTile(mahjongNode));
        }

        // 判定是否可以吃牌.(上家打牌后判定) 并返回吃牌列表(客户端显示)
        public bool ChowCheck(MahjongTile targetTile)
        {
            if (targetTile == null)
            {
                return false;
            }
            return m_strategy.ChowCheck(targetTile, m_handTileList);
        }
        public bool CheckChowMahjong(List<int> chowMahong)
        {
            if (chowMahong == null || chowMahong.Count != 3)
            {
                return false;
            }
            List<MahjongTile> chowMahongList = new List<MahjongTile>();
            for(int i = 0; i < chowMahong.Count; ++i)
            {
                if (i != 1)
                {
                    chowMahongList.Add(new MahjongTile(chowMahong[i]));
                }
            }
            if (chowMahongList.Exists(element => !CheckPlayerMahjong(element)))
            {
                return false;
            }
            return m_strategy.ChowCheck(new MahjongTile(chowMahong[1]), chowMahongList);
        }
        public Meld Chow(List<int> mahjongNode)
        {
            if (mahjongNode == null || mahjongNode.Count != 3)
            {
                return null;
            }
            MahjongTile targetTile = new MahjongTile(mahjongNode[1]);
            MahjongTile suitTile1 = m_handTileList.Find(element => element.Equal(new MahjongTile(mahjongNode[0])));
            MahjongTile suitTile2 = m_handTileList.Find(element => element.Equal(new MahjongTile(mahjongNode[2])));

           return Chow(suitTile1, suitTile2, targetTile);
        }
        public Meld Chow(MahjongTile suitTile1, MahjongTile suitTile2, MahjongTile targetTile)
        {
            Meld meld = m_strategy.Chow(suitTile1, suitTile2, targetTile);

            if (meld != null)
            {
                m_displayTileList.Add(meld);
                m_handTileList.Remove(suitTile1);
                m_handTileList.Remove(suitTile2);
            }
            return meld;
        }
        public Meld Pong(List<int> mahjongNode)
        {
            if (mahjongNode == null || mahjongNode.Count != 1)
            {
                return null;
            }
            MahjongTile targetTile = new MahjongTile(mahjongNode[0]);
            List<MahjongTile> suitTileList = m_handTileList.FindAll(element => element.Equal(targetTile));
            if (suitTileList == null || suitTileList.Count < 2)
            {
                return null;
            }
            return Pong(suitTileList[0], suitTileList[1], targetTile);
        }
        public Meld Pong(MahjongTile suitTile1, MahjongTile suitTile2, MahjongTile targetTile)
        {
            Meld meld = m_strategy.Pong(suitTile1, suitTile2, targetTile);

            if (meld != null)
            {
                m_displayTileList.Add(meld);
                m_handTileList.Remove(suitTile1);
                m_handTileList.Remove(suitTile2);
            }
            return meld;
        }
        public Meld Kong(List<int> mahjongNode, bool bKongSelf)
        {
            if (mahjongNode == null || mahjongNode.Count == 0 || mahjongNode.Count > 2)
            {
                return null;
            }
            MahjongTile targetTile = new MahjongTile(mahjongNode[0]);
            if (bKongSelf)
            {
                Meld meld = ExposedKongCheck(targetTile);
                if (meld != null)
                {
                    meld.m_eMeldType = MeldType.EM_ExposedKong;
                    meld.m_meldTileList.Add(targetTile);
                    return meld;
                }
            }
            List<MahjongTile> suitTileList = m_handTileList.FindAll(element => element.Equal(targetTile));
            if (suitTileList == null || suitTileList.Count != 3)
            {
                return null;
            }
            return Kong(suitTileList[0], suitTileList[1], suitTileList[2], targetTile, bKongSelf);
        }
        public Meld Kong(MahjongTile suitTile1, MahjongTile suitTile2, MahjongTile suitTile3, MahjongTile targetTile, bool bKongSelf)
        {
            Meld meld = m_strategy.Kong(suitTile1, suitTile2, suitTile3, targetTile, !bKongSelf);

            if (meld != null)
            {
                m_displayTileList.Add(meld);
                m_handTileList.Remove(suitTile1);
                m_handTileList.Remove(suitTile2);
                m_handTileList.Remove(suitTile3);
            }
            return meld;
        }

        // 判定是否可以碰或者杠(任何人打牌后判定).
        public MeldType PongKongCheck(int mahjongNode)
        {
            if (mahjongNode == 0)
            {
                return MeldType.EM_None;
            }
            return PongKongCheck(new MahjongTile(mahjongNode));
        }
        public MeldType PongKongCheck(MahjongTile targetTile)
        {
            if (targetTile == null)
            {
                return MeldType.EM_None;
            }
            return m_strategy.PongKongCheck(targetTile, m_handTileList);
        }   
        
        // 判定自己是否可以明杠
        public Meld ExposedKongCheck(int mahjongNode)
        {
            if (mahjongNode == 0 || !CheckPlayerMahjong(mahjongNode))
            {
                return null;
            }
            return ExposedKongCheck(new MahjongTile(mahjongNode));
        }
        public Meld ExposedKongCheck(MahjongTile targetTile)
        {
            return m_strategy.ExposedKongCheck(targetTile, m_displayTileList);
        }
        public Meld ConcealedKong(MahjongTile suitTile1, MahjongTile suitTile2, MahjongTile suitTile3, MahjongTile targetTile)
        {
            Meld meld = m_strategy.Kong(suitTile1, suitTile2, suitTile3, targetTile, false);

            if (meld != null)
            {
                _AddDisplayMeldRemoveHandTile(meld);
            }
            return meld;
        }

        // 获取目标牌列表(和碰杠函数配合获取指定的牌 用来客户端显示)  
        public List<MahjongTile> GetTileList(int mahjongNode)
        {
            if (mahjongNode == 0)
            {
                return null;
            }
            return GetTileList(new MahjongTile(mahjongNode));
        }
        public List<MahjongTile> GetTileList(MahjongTile targetTile)
        {
            m_tempTileList.Clear();

            for (int i = 0; i < m_handTileList.Count; ++i)
            {
                MahjongTile tile = m_handTileList[i];
                if (tile.Equal(targetTile))
                {
                    m_tempTileList.Add(tile);
                }
            }

            return m_tempTileList;
        }
        //长沙麻将判断是否可以杠
        public bool KongCheckByCSMahjong(int mahjongNode, int count)
        {
            if (mahjongNode == 0 || m_strategy.mahjongType != MahjongType.ChangShaMahjong)
            {
                return false;
            }
            CSMahjongStrategy csStrategy = m_strategy as CSMahjongStrategy;
            return csStrategy.KongCheckByCSMahjong(m_handTileList, m_displayTileList, new MahjongTile(mahjongNode), count, m_bOpenFakeHu);
        }
        public bool KongCheckByCSMahjong(MahjongTile mahjongTile, int count)
        {
            if (mahjongTile == null || m_strategy.mahjongType != MahjongType.ChangShaMahjong)
            {
                return false;
            }
            CSMahjongStrategy csStrategy = m_strategy as CSMahjongStrategy;
            return csStrategy.KongCheckByCSMahjong(m_handTileList, m_displayTileList, mahjongTile, count, m_bOpenFakeHu);
        }
        //胡牌判断    
        public CSSpecialWinType WinHandTileCheck(bool bFakeHu = false)
        {
            return WinHandTileCheck(m_handTileList, bFakeHu);
        }     
        //胡牌判断
        public bool WinHandTileCheck(List<int> mahjongList, bool bFakeHu)
        {
            if (mahjongList == null || mahjongList.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < mahjongList.Count; ++i)
            {
                if (CSSpecialWinType.WT_None == WinHandTileCheck(mahjongList[i], bFakeHu))
                {
                    return false;
                }
            }
            return true;
        }
        //胡牌判断
        public CSSpecialWinType WinHandTileCheck(int mahjongNode, bool bFakeHu)
        {
            if (mahjongNode == 0)
            {
                return CSSpecialWinType.WT_None;
            }
            return WinHandTileCheck(new MahjongTile(mahjongNode), bFakeHu);
        }
        //胡牌判断
        public CSSpecialWinType WinHandTileCheck(MahjongTile targetTile, bool bFakeHu)
        {
            if (targetTile == null)
            {
                return CSSpecialWinType.WT_None;
            }
            List<MahjongTile> checkTileList = new List<MahjongTile>();
            checkTileList.AddRange(m_handTileList);
            checkTileList.Add(targetTile);
            //排序
            checkTileList.Sort(m_strategy.CompareTile);
            return WinHandTileCheck(checkTileList, bFakeHu);
        }  
        //胡牌判断 
        private CSSpecialWinType WinHandTileCheck(List<MahjongTile> headTileList, bool bFakeHu)
        {
            return m_strategy.WinHandTileCheck(headTileList, m_displayTileList, m_b7Pairs, bFakeHu);
        }
        // 分析摆牌
        public CSStartDisplayType StartDisplay()
        {
            if (m_strategy.mahjongType == MahjongType.ChangShaMahjong)
            {
                CSMahjongStrategy cs_strategy = m_strategy as CSMahjongStrategy;
                return cs_strategy.StartDisplay(m_handTileList, m_displayMahjongList, m_bPersonalisePendulum);
            }
            return CSStartDisplayType.SDT_None;
        }
        // 获取摆牌
        public void GetStartDisplayMahjong(CSStartDisplayType displayType, List<int> mahjongList)
        {
            if (m_strategy.mahjongType != MahjongType.ChangShaMahjong || displayType == CSStartDisplayType.SDT_None)
            {
                return;
            }
            if (displayType == CSStartDisplayType.SDT_MissOneType || displayType == CSStartDisplayType.SDT_NoPairs || displayType == CSStartDisplayType.SDT_AFlower)
            {
                GetPlayerHandTileList(mahjongList);
            }
            else
            {
                DisplayMahjongNode displayNode = m_displayMahjongList.Find(element => element.startDisplayType == displayType);
                if (displayNode != null && displayNode.mahjongTileList.Count >= 0)
                {
                    displayNode.mahjongTileList.ForEach(mahjongTile =>
                    {
                        mahjongList.Add(mahjongTile.GetMahjongNode());
                    });
                    if (m_bPersonalisePendulum && displayType == CSStartDisplayType.SDT_ConcealedKongOne)
                    {
                        displayFourTileList.Add(displayNode.mahjongTileList[0]);
                    }
                }
            }
        }
        //胡牌判断 
        public CSSpecialWinType WinHandTileCheckOne(List<MahjongTile> headTileList, List<Meld> meldList)
        {
            //排序
            MahjongStrategyBase strategy = MahjongManager.Instance.GetMahjongStrategy(MahjongType.RedLaiZiMahjong);
            headTileList.Sort(strategy.CompareTile);
            CSSpecialWinType winType = CSSpecialWinType.WT_None;
            int timeWorld = Environment.TickCount;
            winType = strategy.WinHandTileCheck(headTileList, meldList, m_b7Pairs);
            int ElapsedMilliseconds = Environment.TickCount - timeWorld;
            return winType;
        }
    }
#endif
}
