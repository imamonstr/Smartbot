using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using Combinatorics.Collections;
using MasterwaiMulliganLib;
using SmartBot.Database;
using SmartBot.Plugins.API;

namespace SmartBot.Mulligan
{
    [Serializable]
    public class MasterwaiArena : MulliganProfile
    {
        private const bool Debug = true;
        private const string DeckPath = "D:\\Smartbot\\Logs\\Masterwai\\DebugDecks\\ ROGUE 21-11-2017 -- 19.37.27.txt";
        private readonly string _logDirPath = Directory.GetCurrentDirectory() + @"\Logs\Masterwai\";
        private readonly string _logPath = Directory.GetCurrentDirectory() + @"\Logs\Masterwai\MasterwaiArenaMulligan.txt";
        private static readonly string DecksDirPath = "D:\\Smartbot" + "\\Logs\\Masterwai\\DebugDecks\\";

        private const string Divider = "======================================================";

        private string _log = "";

        private List<Card.Cards> _deck;
        private List<Card.Cards> _choices;
        private List<Card.Cards> _hand;
        private List<Card.Cards> _keep;
        private Card.CClass _opponentClass;
        private Card.CClass _ownClass;
        private bool _coin;

        private Dictionary<Card.Cards, Mcard> _cardInfo;
        private readonly IniManager _settingsManager = new IniManager(Directory.GetCurrentDirectory() + @"\MulliganProfiles\MMTierlists\TierlistSettings.ini");
        private IniManager _manager;

        public MasterwaiArena()
        {
            try
            {
                foreach (var info in Directory.GetFiles(DecksDirPath).Select(x => new FileInfo(x)).Where(x => x.LastWriteTimeUtc < DateTime.UtcNow - TimeSpan.FromDays(5)))
                {
                    try
                    {
                        info.Delete();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void AddLog(string s)
        {
            _log += "\r\n" + s;
        }

        private void PrintLog()
        {
            Console.WriteLine(_log);
            Bot.Log(_log);
            Directory.CreateDirectory(_logDirPath);
            File.AppendAllText(_logPath, _log);
            _log = "";
        }

        private void StartLog()
        {
            string tierlistVer = _manager.GetString("info", "version", "unknown");
            string tierlistName = _manager.GetString("info", "name", "Basic");

            AddLog(Divider);

            AddLog("MasterwaiMulligan");

            AddLog(Divider);

            AddLog("Tierlist");
            AddLog("Version: " + tierlistVer);
            AddLog("Name: " + tierlistName);

            AddLog(Divider);

            AddLog("Curve Importance: " + SimBoard.FourDropMod.ToString("F2"));
            AddLog("1 Drop Mod: " + SimBoard.OneDropMod.ToString("F2"));
            AddLog("2 Drop Mod: " + SimBoard.TwoDropMod.ToString("F2"));
            AddLog("3 Drop Mod: " + SimBoard.ThreeDropMod.ToString("F2"));
            AddLog("4 Drop Mod: " + SimBoard.FourDropMod.ToString("F2"));

            AddLog(Divider);

            AddLog("Match info:");
            AddLog("Class: " + _ownClass);
            AddLog("Opponent: " + _opponentClass);
            AddLog("Coin: " + _coin);

            AddLog(Divider);

            AddLog("Offered:");
            foreach (var card in _choices)
            {
                var cardTmp = CardTemplate.LoadFromId(card);
                AddLog("> " + cardTmp.Name);
            }

            AddLog(Divider);
        }

        public List<Card.Cards> HandleMulligan(List<Card.Cards> choices, Card.CClass opponentClass, Card.CClass ownClass)
        {
            GetDeck();

            var tierlistPath = Directory.GetCurrentDirectory() + @"\MulliganProfiles\MMTierlists\";

            if (Bot.CurrentMode() == Bot.Mode.Arena || Bot.CurrentMode() == Bot.Mode.Arena)
            {
                var arenaTierlist = _settingsManager.GetString("Decks", "Arena", "NoSetting");
                tierlistPath += arenaTierlist == "NoSetting" ? "SovietArena_TierList.ini" : arenaTierlist;
            }
            else
            {
                var rankedTierList = _settingsManager.GetString("Decks", Bot.CurrentDeck().Name, "NoSetting");
                tierlistPath += rankedTierList == "NoSetting" ? "SovietArena_TierList.ini" : rankedTierList;

            }

            _manager = new IniManager(tierlistPath);

            _keep = new List<Card.Cards>();
            _opponentClass = opponentClass;
            _ownClass = ownClass;
            _choices = choices;
            _hand = _choices.ToList();

            SimBoard.Opponnent = opponentClass;

            var curveImportance = ParseDouble(_manager.GetString("curve", "curveImportance", "0.5"));
            var fourDropMod = ParseDouble(_manager.GetString("curve", "fourDropMod", "1"));
            SimBoard.OneDropMod = ParseDouble(_manager.GetString("curve", "oneDropMod", "4")) / fourDropMod * curveImportance;
            SimBoard.TwoDropMod = ParseDouble(_manager.GetString("curve", "twoDropMod", "3")) / fourDropMod * curveImportance;
            SimBoard.ThreeDropMod = ParseDouble(_manager.GetString("curve", "threeDropMod", "2")) / fourDropMod * curveImportance;
            SimBoard.FourDropMod = curveImportance;

            _coin = _hand.Count >= 4;
            if (_coin)
            {
                _keep.Add(Card.Cards.GAME_005);
                _choices.Remove(Card.Cards.GAME_005);
                _deck.Add(Card.Cards.GAME_005);
            }

            _cardInfo = _manager.GetDict(_deck, ownClass);

            if (Debug)
            {
                AddLog(Divider);

                AddLog("Deck:");
                for (var i = 0; i < _deck.Count; i++)
                {
                    Card.Cards card = _deck[i];
                    try
                    {
                        AddLog("V: " + _cardInfo[card].CardPts.ToString().PadLeft(5) + " : " + CardTemplate.LoadFromId(card).Name);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                Directory.CreateDirectory(DecksDirPath);
                var now = DateTime.UtcNow;
                File.WriteAllText(String.Format("{0} \\ {1} {2}-{3}-{4} -- {5}.{6}.{7}.txt",
                    DecksDirPath, ownClass, now.Day, now.Month, now.Year, now.Hour, now.Minute, now.Second), SimpleSerializer.ToJason(_deck));
            }

            StartLog();

            string bestComboVal = "0000";
            double bestComboPts = double.MinValue;
            int bestCardCount = 0;

            var combos = new Variations<bool>(new List<bool> { true, false }, _choices.Count, GenerateOption.WithRepetition);

            var deck = _deck.ToList();
            _hand.ForEach(x => deck.Remove(x));

            foreach (var combo in combos)
            {
                var comboCards = new List<Card.Cards>();
                for (var i = 0; i < _choices.Count; i++)
                {
                    if (combo[i])
                    {
                        comboCards.Add(_choices[i]);
                    }
                }

                var comboStr = "";
                foreach (bool b in combo)
                {
                    comboStr += b ? 1 : 0;
                }

                int count = comboCards.Count;
                var startingHands = new Combinations<Card.Cards>(deck, _choices.Count - count).Select(x =>
                {
                    var ret = new List<Card.Cards>(x);
                    ret.AddRange(comboCards);
                    if (_coin) ret.Add(Card.Cards.GAME_005);
                    return ret;
                }).ToList();
                double combinationPts = (double)startingHands.Average(x => CurveSim(x));

                AddLog(String.Format("Combination: {0} -- Value: {1:###.###}", comboStr, combinationPts));

                if (bestComboPts < combinationPts || (Math.Abs(bestComboPts - combinationPts) < 0.01 && count > bestCardCount))
                {
                    bestCardCount = count;
                    bestComboPts = combinationPts;
                    bestComboVal = comboStr;
                }
            }

            #region EndLog

            AddLog(String.Format("Best Combination: {0} -- Value: {1:F3}", bestComboVal, bestComboPts));
            AddLog(Divider);

            AddLog("Finally keeping:");

            for (var i = _choices.Count - 1; i >= 0; i--)
            {
                if (bestComboVal[i] == '1')
                {
                    _keep.Add(_choices[i]);
                    var cardTmp = CardTemplate.LoadFromId(_choices[i]);
                    AddLog("> " + cardTmp.Name);
                }
            }

            if (_keep.Count == 0)
            {
                AddLog("Nothing");
            }

            AddLog(Divider);
            #endregion

            PrintLog();
            return _keep;
        }

        private void GetDeck()
        {
            _deck = Bot.CurrentDeck().Cards.Select(card => (Card.Cards)Enum.Parse(typeof(Card.Cards), card)).ToList();
        }

        private double CurveSim(List<Card.Cards> hand)
        {
            var mHand = hand.Select(x => _cardInfo[x]).ToList();
            var score = new SimBoard(mHand, hand).Eval();
            return score;
        }

        private static double ParseDouble(string str)
        {
            return double.Parse(str, CultureInfo.CreateSpecificCulture("en-US"));
        }
    }

    public static class SimpleSerializer
    {
        public static T FromJason<T>(string json)
        {
            var jsonSerializer = new DataContractJsonSerializer(typeof(T));

            using (Stream stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Position = 0;
                    return (T)jsonSerializer.ReadObject(stream);
                }
            }
        }

        public static string ToJason<T>(T obj)
        {
            using (var stream = new MemoryStream())
            {

                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, obj);
                stream.Position = 0;

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}