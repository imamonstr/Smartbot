using System;
using System.Collections.Generic;
using System.Linq;
using SmartBot.Plugins.API;
//a

namespace SmartBotProfiles
{
    [Serializable]
    public class MidrangeDefault : Profile
    {
        private const Card.Cards SteadyShot = Card.Cards.DS1h_292;
        private const Card.Cards Shapeshift = Card.Cards.CS2_017;
        private const Card.Cards LifeTap = Card.Cards.CS2_056;
        private const Card.Cards Fireblast = Card.Cards.CS2_034;
        private const Card.Cards Reinforce = Card.Cards.CS2_101;
        private const Card.Cards ArmorUp = Card.Cards.CS2_102;
        private const Card.Cards LesserHeal = Card.Cards.CS1h_001;
        private const Card.Cards TotemicCall = Card.Cards.CS2_049;
        private const Card.Cards DaggerMastery = Card.Cards.CS2_083b;

        private readonly Dictionary<Card.Cards, int> _heroPowersPriorityTable = new Dictionary<Card.Cards, int>
        {
            {LifeTap, 8},
            {Fireblast, 7},
            {DaggerMastery, 6},
            {Shapeshift, 5},
            {LesserHeal, 4},
            {SteadyShot, 3},
            {Reinforce, 2},
            {TotemicCall, 1},
            {ArmorUp, 0}
        };

        public ProfileParameters GetParameters(Board board)
        {
			var p = new ProfileParameters(BaseProfile.Default);

            double inHand = board.Hand.Sum(x => x.CurrentCost);
            inHand = inHand < 1 ? 1 : inHand;
            double value = (board.ManaAvailable + board.MaxMana) / inHand;

            p.DiscoverSimulationValueThresholdPercent = (float)((value > 1 ? Math.Sqrt(value) : value) * 15) + 3;

            return p;
        }

        public Card.Cards SirFinleyChoice(List<Card.Cards> choices)
        {
            var filteredTable = _heroPowersPriorityTable.Where(x => choices.Contains(x.Key)).ToList();
            return filteredTable.First(x => x.Value == filteredTable.Max(y => y.Value)).Key;
        }

        public Card.Cards KazakusChoice(List<Card.Cards> choices)
        {
            return choices[1];
        }
    }
}