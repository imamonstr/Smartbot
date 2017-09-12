using SmartBot.Plugins.API;
using System;
using System.Collections.Generic;
using System.Linq;
using MasterwaiLib;
using SmartBot.Database;
using SmartBotStats;
using MasterwaiHistory;

namespace SmartBot.Plugins
{
    [Serializable]
    public class MasterwaiHistoryData : PluginDataContainer
    {
        public MasterwaiHistoryData()
        {
            Name = "MasterwaiHistory";
        }
    }

    public class MasterwaiHistory : Plugin
    {
        private List<Card.Cards> _latestOffered;
        private List<Card.Cards> _latestKept;
        private IDisposable _disposable;

        public override void OnPluginCreated()
        {
            _disposable = Program.BootApi(8999);
        }

        public override void OnVictory()
        {
            SaveGame(true);
        }

        public override void OnDefeat()
        {
            SaveGame(false);
        }

        private void SaveGame(bool result)
        {
            var board = Bot.CurrentBoard;
            if (board == null) return;
            var played = board.EnemyGraveyard;
            played.AddRange(board.MinionEnemy.Select(x => x.Template.Id));
            played = played.Where(x => CardTemplate.LoadFromId(x).IsCollectible).ToList();

            var dic = new Dictionary<Card.Cards, int>();

            foreach (var card in played.ToArray())
            {
                if (dic.ContainsKey(card))
                {
                    if (dic[card] == 2)
                    {
                        played.Remove(card);
                    }
                    else
                    {
                        dic[card]++;
                    }
                }
                else
                {
                    dic[card] = 1;
                }
            }
            var mode = Bot.CurrentMode();
            var wild = mode == Bot.Mode.RankedWild || mode == Bot.Mode.UnrankedWild;
            var game = new Game(played: played, deck: board.Deck, offered: _latestOffered, kept: _latestKept, result: result, hero: board.FriendClass, opponnent: board.EnemyClass, mode: Bot.CurrentMode(), rank: Bot.GetPlayerDatas().GetRank(wild), server: GetCurrentServer(), account: Bot.GetCurrentAccount());
            global::MasterwaiHistory.MasterwaiHistory.AddGame(game);
        }

        private Server GetCurrentServer()
        {
            switch (Bot.CurrentRegion())
            {
                case BnetRegion.REGION_EU:
                    return Server.Europe;

                case BnetRegion.REGION_US:
                    return Server.Americas;

                case BnetRegion.REGION_KR:
                    return Server.Asia;

                case BnetRegion.REGION_CN:
                    return Server.China;

                default:
                    return Server.Invalid;
            }
        }


        public override void OnHandleMulligan(List<Card.Cards> choices, Card.CClass opponentClass, Card.CClass ownClass)
        {
            _latestOffered = choices.ToList();
        }

        public override void OnMulliganCardsReplaced(List<Card.Cards> replacedCards)
        {
            _latestKept = _latestOffered.ToList();
            foreach (Card.Cards replacedCard in replacedCards)
            {
                _latestKept.Remove(replacedCard);
            }

            var strName = "[HISTORY] Offered: ";
            var strRaw = "[HISTORY] Offered: ";
            foreach (Card.Cards cards in _latestOffered)
            {
                strName += CardTemplate.LoadFromId(cards).Name + ", ";
                strRaw += cards + ", ";
            }
            Bot.Log("[NAMES]" + strName);
            Bot.Log("[RAW]" + strRaw);
            Bot.Log("");

            strName = "[HISTORY] Replaced: ";
            strRaw = "[HISTORY] Replaced: ";
            foreach (Card.Cards cards in replacedCards)
            {
                strName += CardTemplate.LoadFromId(cards).Name + ", ";
                strRaw += cards + ", ";
            }
            Bot.Log("[NAMES]" + strName);
            Bot.Log("[RAW]" + strRaw);
            Bot.Log("");


            strName = "[HISTORY] Kept: ";
            strRaw = "[HISTORY] Kept: ";
            foreach (Card.Cards cards in _latestKept)
            {
                strName += CardTemplate.LoadFromId(cards).Name + ", ";
                strRaw += cards + ", ";
            }
            Bot.Log("[NAMES]" + strName);
            Bot.Log("[RAW]" + strRaw);
            Bot.Log("");
        }


        #region Disposing

        ~MasterwaiHistory()
        {
            _disposable.Dispose();
        }

        public override void Dispose()
        {
            _disposable.Dispose();
        }

        #endregion
    }
}