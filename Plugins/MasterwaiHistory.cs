using SmartBot.Plugins.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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


        private void SaveGame(bool result)
        {
            var board = Bot.CurrentBoard;
            if (board == null) return;

            var played = board.EnemyGraveyard.ToList();
            played.AddRange(board.MinionEnemy.Select(x => x.Template.Id));
            played = played.Where(x => CardTemplate.LoadFromId(x).IsCollectible).ToList();

            var opponnentPlayed = board.FriendGraveyard.ToList();
            opponnentPlayed.AddRange(board.MinionFriend.Select(x => x.Template.Id));
            opponnentPlayed = opponnentPlayed.Where(x => CardTemplate.LoadFromId(x).IsCollectible).ToList();

            CleanCollection(played);
            CleanCollection(opponnentPlayed);
            
            var mode = Bot.CurrentMode();
            var wild = mode == Bot.Mode.RankedWild || mode == Bot.Mode.UnrankedWild;
            var game = new Game(mode: Bot.CurrentMode(),
                rank: Bot.GetPlayerDatas().GetRank(wild),
                server: GetCurrentServer(),
                account: Bot.GetCurrentAccount(),
                offered: _latestOffered,
                kept: _latestKept,
                result: result,
                played: played,
                deck: board.Deck,
                hero: board.FriendClass,
                opponnentPlayed: opponnentPlayed,
                opponnentHero: board.EnemyClass);
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

        private void CleanCollection<T>(List<T> list)
        {
            var dict = new Dictionary<T, int>();
            foreach (T t in list.ToArray())
            {
                if (dict.ContainsKey(t))
                {
                    if (dict[t] == 2)
                    {
                        list.Remove(t);
                    }
                    else
                    {
                        dict[t]++;
                    }
                }
                else
                {
                    dict[t] = 1;
                }
            }
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