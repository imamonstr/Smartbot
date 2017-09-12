using System;
using System.Collections.Generic;
using System.Linq;
using SmartBot.Database;
using SmartBot.Plugins.API;

namespace SmartBot.Mulligan
{
    [Serializable]
    public class BigPriestMulligan : MulliganProfile
    {

        private static string _name = "Big Priest Mulligan v1.1";
        private string _log = "\r\n---"+ _name +"---\r\n";

        private List<Card.Cards> _choices;

        private readonly List<Card.Cards> _keep = new List<Card.Cards>();

        private readonly Dictionary<Defs, List<Card.Cards>> _defsDictionary = new Dictionary<Defs, List<Card.Cards>>();

        private enum Defs
        {
            Pull
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
            bool aggro = IsAggroClass(opponentClass);
            Define(Defs.Pull, Cards.Barnes, Cards.ShadowVisions);

            Keep("Always kept", Cards.Barnes);

            if (!aggro)
            {
                Keep("Keep Essence vs control", Cards.ShadowEssence);
            }

            if (_keep.Contains(Cards.Barnes) || _keep.Contains(Cards.ShadowEssence))
            {
                Keep("Keep Servitude with barnes or vs non aggro with Essence", Cards.EternalServitude, Cards.EternalServitude);
            }

            if (aggro)
            {
                Keep("Keep anti aggro cards vs aggro", Cards.ShadowWordPain, Cards.ShadowWordPain, Cards.SpiritLash, Cards.PotionofMadness);
                if (opponentClass == Card.CClass.SHAMAN)
                {
                    Keep("Keep Horror vs shaman", Cards.ShadowWordHorror);
                }
                else if(opponentClass == Card.CClass.PALADIN && _choices.Contains(Cards.ShadowWordHorror) && _choices.Contains(Cards.PintSizePotion)) 
                {
                    Keep("Keep Horror with PintSize vs paladin", Cards.PintSizePotion, Cards.ShadowWordHorror);
                }
            }
            else if(opponentClass == Card.CClass.DRUID)
            {
                Keep("Keep Pain vs druid to hedge against aggro", Cards.ShadowWordPain);
            }
            else
            {
                Keep("Keep a Visions vs control", Cards.ShadowVisions);
                if (Kept(Defs.Pull) > 0 && _keep.Contains(Cards.EternalServitude))
                {
                    Keep("Keep Thoughtsteal vs control when we already have the combo", Cards.Thoughtsteal, Cards.Thoughtsteal);
                }
            }

            PrintLog();
            return _keep;
        }

        private static bool IsAggroClass(Card.CClass cClass)
        {
            return cClass == Card.CClass.WARRIOR || cClass == Card.CClass.PALADIN || cClass == Card.CClass.SHAMAN || cClass == Card.CClass.HUNTER;
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