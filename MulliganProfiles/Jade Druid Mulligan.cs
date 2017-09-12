using System;
using System.Collections.Generic;
using System.Linq;
using SmartBot.Database;
using SmartBot.Plugins.API;

namespace SmartBot.Mulligan
{
    [Serializable]
    public class JadeDruidMulligan : MulliganProfile
    {

        private static string _name = "Jade Druid Mulligan v1.1";
        private string _log = "\r\n---"+ _name +"---\r\n";

        private List<Card.Cards> _choices;

        private readonly List<Card.Cards> _keep = new List<Card.Cards>();

        private readonly Dictionary<Defs, List<Card.Cards>> _defsDictionary = new Dictionary<Defs, List<Card.Cards>>();

        private enum Defs
        {
            Ramp
        }

        public List<Card.Cards> HandleMulligan(List<Card.Cards> choices, Card.CClass opponentClass, Card.CClass ownClass)
        {
            _choices = choices;
            bool coin = _choices.Contains(Card.Cards.GAME_005);
            if (coin)
            {
                _choices.Remove(Card.Cards.GAME_005);
                _keep.Add(Card.Cards.GAME_005);
            }

            Define(Defs.Ramp, Cards.Innervate, Cards.WildGrowth, Cards.JadeBlossom);

            Keep("Always kept", Cards.JadeIdol);

            if (IsAggroClass(opponentClass))
            {
                Keep("Keep anti-aggro cards vs aggro", Cards.Wrath, Cards.Wrath, Cards.Swipe, Cards.MindControlTech);
            }

            var ramp = _keep.Contains(Cards.JadeIdol) || _keep.Contains(Cards.Wrath) || !coin ? 2 : 3;

            PrioKeep("Keep ramp", ramp, Cards.Innervate, Cards.WildGrowth, Cards.JadeBlossom, Cards.WildGrowth, Cards.JadeBlossom);

            if (Kept(Defs.Ramp) > 0)
            {
                if (_keep.Contains(Cards.Wrath) && _choices.Contains(Cards.FandralStaghelm))
                {
                    Keep("Keep Fandral with Wrath and ramp", Cards.FandralStaghelm);
                }
                else
                {
                    PrioKeep("Keep 4 drop with ramp", 1,  Cards.JadeSpirit, Cards.MireKeeper, Cards.FandralStaghelm);
                }

                if (IsAggroClass(opponentClass))
                {
                    Keep("Keep Plague with ramp vs aggro", Cards.SpreadingPlague);
                }

                if (Kept(Defs.Ramp) > 1 && !IsAggroClass(opponentClass))
                {
                    Keep("Keep draw with alot of ramp", Cards.Nourish);
                }
            }

            PrintLog();
            return _keep;
        }

        private static bool IsAggroClass(Card.CClass cClass)
        {
            return cClass == Card.CClass.WARRIOR || cClass == Card.CClass.PALADIN || cClass == Card.CClass.DRUID || cClass == Card.CClass.SHAMAN;
        }

        private bool IsPirateClass(Card.CClass cClass)
        {
            return cClass == Card.CClass.DRUID || cClass == Card.CClass.WARRIOR || cClass == Card.CClass.ROGUE || cClass == Card.CClass.SHAMAN;
        }

        /// <summary>
        /// Method to fill in his _keep list
        /// </summary>
        /// <param name="reason">Why?</param>
        /// <param name="cards">List of cards he wants to add</param>
        private void Keep(string reason, params Card.Cards[] cards)
        {
            var count = true;
            string str = "Keep: ";
            foreach (var card in cards)
            {
                if (_choices.Contains(card))
                {
                    str += CardTemplate.LoadFromId(card).Name + ",";
                    _choices.Remove(card);
                    _keep.Add(card);
                    count = false;
                }
            }
            if (count) return;
            str = str.Remove(str.Length - 1);
            str += "\r\nBecause: " + reason;
            AddLog(str);
        }

        /// <summary>
        /// Defines a list of cards as a certain type on card
        /// </summary>
        /// <param name="type">The type of card that you want to define these cards as</param>
        /// <param name="cards">List of cards you want to define as the given type</param>
        private void Define(Defs type, params Card.Cards[] cards)
        {
            _defsDictionary[type] = cards.ToList();
        }

        /// <summary>
        /// Returns the numbers of cards of the given type that you have kept
        /// </summary>
        /// <param name="type">The type of card you want to look for</param>
        private int Kept(Defs type)
        {
            return _keep.Count(x => _defsDictionary[type].Contains(x));
        }

        /// <summary>
        /// Returns the numbers of cards of the given type that you have given as a choice
        /// </summary>
        /// <param name="type">The type of card you want to look for</param>
        private int HasChoice(Defs type)
        {
            return _choices.Count(x => _defsDictionary[type].Contains(x));
        }

        private void PrioKeep(string reason, int numb, params Card.Cards[] cards)
        {
            if (numb > cards.Length) numb = cards.Length;

            var toKeep = new List<Card.Cards>();

            int count = 0;
            int index = 0;

            while (count < numb && index < cards.Length)
            {
                var card = cards[index];
                index++;
                if (_choices.Contains(card))
                {
                    count++;
                    toKeep.Add(card);
                }
            }

            Keep(reason, toKeep.ToArray());
        }

        private void AddLog(string s)
        {
            _log += "\r\n" + s;
        }

        private void PrintLog()
        {
            Bot.Log(_log+ "\r\n\r\n---"+ _name + "---");
            _log = "\r\n---"+ _name +"---\r\n";
        }
    }
}