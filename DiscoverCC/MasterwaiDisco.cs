using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using SmartBot.Database;
using SmartBot.Discover;
using SmartBot.Plugins.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Discover
{
    public class Mtd : DiscoverPickHandler
    {
        private string _log = LogHead;
        private const string Divider = "======================================================";
        private const string LogHead = "\r\n" + Divider + "\r\n[-MasterwaiDisco-]";

        public delegate int EvalMethod(Board board);
        private static readonly JsonSerializerSettings S = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter { AllowIntegerValues = false } }
        };
        public static readonly Tierlist TierList = JsonConvert.DeserializeObject<Tierlist>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "DiscoverCC\\ParsedTierlist.json"), S);
        private static readonly Dictionary<string, DeckLogic> CardDictionarys = new Dictionary<string, DeckLogic>
        {
            {"Universal", new Universal()}
        };


        public Card.Cards HandlePickDecision(Card.Cards originCard, List<Card.Cards> choices, Board board)
        {
            //Logic for special discovers
            if (originCard == Cards.ArchThiefRafaam)
            {
                Card.Cards mirrorOfDoom = Card.Cards.LOEA16_5;
                Card.Cards timepieceOfHorror = Card.Cards.LOEA16_4;
                Card.Cards lanternOfPower = Card.Cards.LOEA16_3;

                //Insert Rafaam logic
                AddLog("ArchThiefRafamm special case");
                PrintLog();
                return mirrorOfDoom;
            }

            try
            {
                choices = choices.OrderByDescending(x => GetValue(x, board)).ToList();
                var choice = choices.First();
                PrintLog();
                return choice;
            }
            catch (InvalidOperationException)
            {
                AddLog("Card not found in Tierlist. Falling back to Smartbot default Discover.");
                PrintLog();
                return Card.Cards.CRED_01;
            }
        }

        private int GetValue(Card.Cards card, Board board)
        {
            AddLog("Name: " + CardTemplate.LoadFromId(card).Name);
            
            var deck = "Universal"; //Next level deck identification I know. Thank you.
            AddLog("Deck identified: " + deck);

            var specificDeckLogic = CardDictionarys.ContainsKey(deck) ? CardDictionarys[deck] : CardDictionarys["Universal"];
            DeckLogic deckLogic;
            
            if (!specificDeckLogic.CardValues.ContainsKey(card))
            {
                AddLog("Deckspecific logic: " + "No.");
                deckLogic = CardDictionarys["Universal"];
            }
            else
            {
                AddLog("Deckspecific logic: " + "Yes.");
                deckLogic = specificDeckLogic;
            }

            int value;
            if (deckLogic.CardValues.ContainsKey(card))
            {
                AddLog("Custom logic: " + "Yes.");
                value = deckLogic.CardValues[card](board);
            }
            else
            {
                AddLog("Custom logic: " + "No.");
                value = TierList.GetCardValue(card, board.FriendClass);
            }

            value = specificDeckLogic.Modifiers(card, board, value);

            AddLog("Card value: " + value);
            AddLog("");
            return value;
        }


        //Add line to logstring for current discover
        private void AddLog(string s)
        {
            _log += "\r\n" + s;
        }

        //Print log for current discover and reset string to header
        private void PrintLog()
        {
            Bot.Log(_log + "\r\n[-MasterwaiDisco-]\r\n" + Divider);
            _log = LogHead;
        }
    }

    public class Universal : DeckLogic
    {
        public Universal()
        {
            CardValues = new Dictionary<Card.Cards, Mtd.EvalMethod>
            {
                #region Reno style cards
            
                {Cards.Kazakus, KazakusValue},
                {Cards.KrultheUnshackled, KrultheUnshackledValue},
                {Cards.RazatheChained, RazatheChainedValue},
                {Cards.InkmasterSolia, InkmasterSoliaValue},

                #endregion

                #region TriClass

                {Cards.GrimestreetSmuggler, GrimestreetSmuggler},
                {Cards.GrimestreetInformant, GrimestreetInformant},
                {Cards.DonHanCho, DonHanCho},

                {Cards.JadeSpirit, JadeSpirit},
                {Cards.LotusAgents, LotusAgents},
                {Cards.AyaBlackpaw, AyaBlackpaw},

                {Cards.KabalChemist, KabalChemist},
                {Cards.KabalCourier, KabalCourier},

                #endregion

                #region Cthun

                {Cards.CThun, CThun},
                {Cards.BeckonerofEvil, BeckonerofEvil},
                {Cards.TwilightElder, TwilightElder},
                {Cards.CThunsChosen, CThunsChosen},
                {Cards.DarkArakkoa, DarkArakkoa},
                {Cards.SkeramCultist, SkeramCultist},
                {Cards.HoodedAcolyte, HoodedAcolyte},
                {Cards.DiscipleofCThun, DiscipleofCThun},
                {Cards.BladeofCThun, BladeofCThun},
                {Cards.TwilightGeomancer, TwilightGeomancer},
                {Cards.Doomcaller, Doomcaller},
                {Cards.CrazedWorshipper, CrazedWorshipper},
                {Cards.CultSorcerer, CultSorcerer},
                {Cards.UsherofSouls, UsherofSouls},
                {Cards.KlaxxiAmberWeaver, KlaxxiAmberWeaver},
                {Cards.AncientShieldbearer, AncientShieldbearer},
                {Cards.TwilightDarkmender, TwilightDarkmender},
                {Cards.TwinEmperorVeklor, TwinEmperorVeklor},

                #endregion

                #region Banned

                #region Priest

                {Cards.InnerFire, InnerFire},
                {Cards.MindBlast, MindBlast},
                {Cards.Lightwell, Lightwell},
                {Cards.Purify, Purify},
                {Cards.AwakentheMakers, AwakentheMakers},

                #endregion

                #endregion

                #region Priest

                {Cards.PowerWordShield, PowerWordShield },

                #endregion

                #region Secret Synergy

                {Cards.Arcanologist, Arcanologist}


                #endregion

            };
        }

        #region Eval Methods

        #region Priest

        private static int PowerWordShield(Board board)
        {
            return 110;
        }

        #endregion

        #region Banned

        #region Banned Priest

        private static int AwakentheMakers(Board board)
        {
            return 50;
        }

        private static int Purify(Board board)
        {
            var syn = new List<Card.Cards>
            {
                Cards.AncientWatcher,
                Cards.HumongousRazorleaf,
                Cards.EerieStatue
            };

            if (syn.Any(x => Helpers.CardAvailable(board, x) > 0))
            {
                return 100;
            }

            if (
                syn.Any(
                    x =>
                        board.Hand.Exists(y => y.Template.Id == x) ||
                        board.MinionFriend.Exists(z => z.Template.Id == x)))
            {
                return 130;
            }

            return 50;
        }

        private static int Lightwell(Board board)
        {
            return 45;
        }

        private static int MindBlast(Board board)
        {
            return (int)((30 - board.HeroEnemy.CurrentHealth) * 6.5);
        }

        private static int InnerFire(Board board)
        {
            return 50;
        }

        #endregion

        #endregion

        #region Reno Style Cards

        private static int KazakusValue(Board board)
        {
            return Helpers.HasDuplicates(board.Deck) ? 57 : 150;
        }

        private static int InkmasterSoliaValue(Board board)
        {
            return Helpers.HasDuplicates(board.Deck) ? 70 : 130;
        }

        private static int RazatheChainedValue(Board board)
        {
            return Helpers.HasDuplicates(board.Deck) ? 95 : 150;
        }

        private static int KrultheUnshackledValue(Board board)
        {
            return Helpers.HasDuplicates(board.Deck) ? 90 : Mtd.TierList.GetCardValue(Cards.RazatheChained, board.FriendClass);
        }

        #endregion

        #region TriClass

        #region TriClass Goons

        private static int GrimestreetSmuggler(Board board)
        {
            switch (board.FriendClass)
            {
                case Card.CClass.PALADIN:
                    return 96;
                case Card.CClass.HUNTER:
                    return 96;
                case Card.CClass.WARRIOR:
                    return 96;
                default:
                    return 96;
            }
        }

        private static int GrimestreetInformant(Board board)
        {
            switch (board.FriendClass)
            {
                case Card.CClass.PALADIN:
                    return 121;
                case Card.CClass.HUNTER:
                    return 121;
                case Card.CClass.WARRIOR:
                    return 121;
                default:
                    return 121;
            }
        }

        private static int DonHanCho(Board board)
        {
            switch (board.FriendClass)
            {
                case Card.CClass.PALADIN:
                    return 114;
                case Card.CClass.HUNTER:
                    return 114;
                case Card.CClass.WARRIOR:
                    return 114;
                default:
                    return 114;
            }
        }

        #endregion

        #region TriClass Jade

        private static int JadeSpirit(Board board)
        {
            switch (board.FriendClass)
            {
                case Card.CClass.DRUID:
                    return 76;
                case Card.CClass.ROGUE:
                    return 79;
                case Card.CClass.SHAMAN:
                    return 79;
                default:
                    return 60;
            }
        }

        private static int LotusAgents(Board board)
        {
            switch (board.FriendClass)
            {
                case Card.CClass.DRUID:
                    return 111;
                case Card.CClass.ROGUE:
                    return 111;
                case Card.CClass.SHAMAN:
                    return 111;
                default:
                    return 111;
            }
        }

        private static int AyaBlackpaw(Board board)
        {
            switch (board.FriendClass)
            {
                case Card.CClass.DRUID:
                    return 103;
                case Card.CClass.ROGUE:
                    return 107;
                case Card.CClass.SHAMAN:
                    return 107;
                default:
                    return 90;
            }
        }

        #endregion

        #region TriClass Kabal

        private static int KabalChemist(Board board)
        {
            switch (board.FriendClass)
            {
                case Card.CClass.MAGE:
                    return 99;
                case Card.CClass.PRIEST:
                    return 98;
                case Card.CClass.WARLOCK:
                    return 99;
                default:
                    return 99;
            }
        }

        private static int KabalCourier(Board board)
        {
            switch (board.FriendClass)
            {
                case Card.CClass.MAGE:
                    return 114;
                case Card.CClass.PRIEST:
                    return 110;
                case Card.CClass.WARLOCK:
                    return 103;
                default:
                    return 105;
            }
        }

        #endregion

        #endregion

        #region CThun

        private static int CThun(Board board)
        {
            return board.CthunAttack * 10;
        }

        private static int BeckonerofEvil(Board board)
        {
            var v = Mtd.TierList.GetCardValue(Cards.RiverCrocolisk, board.FriendClass);
            return Helpers.HasCThun(board) ? v + 10 : v;
        }

        private static int TwilightElder(Board board)
        {
            var v = Mtd.TierList.GetCardValue(Cards.AuctionmasterBeardo, board.FriendClass);
            return Helpers.HasCThun(board) ? v + 7 : v - 3;
        }

        private static int CThunsChosen(Board board)
        {
            var v = Mtd.TierList.GetCardValue(Cards.SilvermoonGuardian, board.FriendClass);
            return Helpers.HasCThun(board) ? v + 20 : v + 10;

        }

        private static int DarkArakkoa(Board board)
        {
            return Helpers.HasCThun(board) ? 150 : 135;
        }

        private static int SkeramCultist(Board board)
        {
            return Helpers.HasCThun(board) ? 105 : 95;
        }

        private static int HoodedAcolyte(Board board)
        {
            return Helpers.HasCThun(board) ? 115 : 105;
        }

        private static int DiscipleofCThun(Board board)
        {
            return Helpers.HasCThun(board) ? 100 : 90;
        }

        private static int BladeofCThun(Board board)
        {
            return Helpers.HasCThun(board) ? 120 : 95;
        }

        private static int TwilightGeomancer(Board board)
        {
            return Helpers.HasCThun(board) ? 85 : 60;
        }

        private static int Doomcaller(Board board)
        {
            return Helpers.HasCThun(board) ? 120 : 85;
        }

        private static int CrazedWorshipper(Board board)
        {
            var v = Mtd.TierList.GetCardValue(Cards.FenCreeper, board.FriendClass);
            return Helpers.HasCThun(board) ? v + 12 : v;
        }

        private static int CultSorcerer(Board board)
        {
            var v = Mtd.TierList.GetCardValue(Cards.BloodfenRaptor, board.FriendClass);
            return Helpers.HasCThun(board) ? v + 15 : v + 7;
        }

        private static int UsherofSouls(Board board)
        {
            var v = Mtd.TierList.GetCardValue(Cards.PitFighter, board.FriendClass);
            return Helpers.HasCThun(board) ? v + 8 : v;
        }

        private static int AncientShieldbearer(Board board)
        {
            var v = 85;

            if (Helpers.HasCThun(board))
            {
                if (board.CthunAttack >= 10)
                {
                    return v + 10 + (30 - board.HeroFriend.CurrentHealth) * 2;
                }
                return v + 5 + (30 - board.HeroFriend.CurrentHealth);
            }
            return v;
        }

        private static int TwilightDarkmender(Board board)
        {
            var v = 100;

            if (Helpers.HasCThun(board))
            {
                if (board.CthunAttack >= 10)
                {
                    return v + (30 - board.HeroFriend.CurrentHealth) * 2;
                }
                return v + (30 - board.HeroFriend.CurrentHealth);
            }
            return v;
        }

        private static int KlaxxiAmberWeaver(Board board)
        {
            var v = Mtd.TierList.GetCardValue(Cards.ChillwindYeti, board.FriendClass);

            if (Helpers.HasCThun(board))
            {
                if (board.CthunAttack >= 10)
                {
                    return v + 50;
                }
                return v + 15;
            }
            return v;
        }

        private static int TwinEmperorVeklor(Board board)
        {
            var v = 60;

            if (Helpers.HasCThun(board))
            {
                if (board.CthunAttack >= 10)
                {
                    return v + 100;
                }
                return v + 60;
            }
            return v;
        }

        #endregion

        #region Secret Synergy

        private static int Arcanologist(Board board)
        {
            var v = Mtd.TierList.GetCardValue(Cards.Arcanologist, board.FriendClass);
            return board.Deck.Any(x => CardTemplate.LoadFromId(x).IsSecret) ? v : Mtd.TierList.GetCardValue(Cards.RiverCrocolisk, Card.CClass.NONE);
        }

        #endregion

        #endregion

        public override int Modifiers(Card.Cards card, Board board, int value)
        {
            var inHand = board.Hand.Sum(x => x.CurrentCost);
            inHand = inHand < 1 ? 1 : inHand;
            double handValue = (double)(board.ManaAvailable + board.MaxMana) / inHand;

            switch (CardTemplate.LoadFromId(card).Cost)
            {
                case 1:
                    return (int)(value - handValue * 35);
                case 2:
                    return (int)(value - handValue * 30);
                case 3:
                    return (int)(value - handValue * 25);
            }
            return value;
        }
    }

    public abstract class DeckLogic
    {
        public Dictionary<Card.Cards, Mtd.EvalMethod> CardValues;

        public virtual int Modifiers(Card.Cards card, Board board, int value)
        {
            return value;
        }
    }

    public static class Helpers
    {
        #region Helper Methods

        public static bool HasDuplicates(List<Card.Cards> deck)
        {
            bool duplicates = false;
            foreach (var card in deck)
            {
                if (deck.Count(x => x == card) > 1)
                {
                    duplicates = true;
                }
            }
            return duplicates;
        }

        public static bool HasCThun(Board board)
        {
            return CardAvailable(board, Cards.CThun) > 0 ||
                   (board.FriendGraveyard.Contains(Cards.CThun) && CardAvailable(board, Cards.Doomcaller) > 0);
        }

        public static int CardAvailable(Board board, Card.Cards card)
        {
            var numb = board.Deck.Count(x => x == card);
            numb -= board.FriendGraveyard.Count(x => x == card);
            numb -= board.MinionFriend.Count(x => x.Template.Id == card);

            return numb;
        }

        #endregion
    }

    #region Tierlist Classes

    [DataContract]
    public class Tierlist
    {
        [DataMember] public List<ArenaCardScore> Cards;

        //Get value of certain card in certain class
        public int GetCardValue(Card.Cards card, Card.CClass friendlyClass)
        {
            //Look for value in our class if the card is neutral or from our class. Otherwise look for value in the class that the card belongs to.
            var cclass = CardTemplate.LoadFromId(card).Class == Card.CClass.NONE ? friendlyClass : CardTemplate.LoadFromId(card).Class;

            try
            {
                //If there is no specific value for the requested class, use neutral value.
                return (int)Cards.Find(x => x.Id == card).Scores[cclass] == 0 ? (int)Cards.Find(x => x.Id == card).Scores[Card.CClass.NONE] : (int)Cards.Find(x => x.Id == card).Scores[cclass];
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("The card " + card + " could not be found in the Tierlist.", e);
            }
        }
    }

    [DataContract]
    public class ArenaCardScore
    {
        [DataMember] public Card.Cards Id;

        [DataMember]
        public Dictionary<Card.CClass, double> Scores = new Dictionary<Card.CClass, double>
        {
            {Card.CClass.DRUID, 0}, {Card.CClass.HUNTER, 0}, {Card.CClass.MAGE, 0}, {Card.CClass.PALADIN, 0}, {Card.CClass.PRIEST, 0}, {Card.CClass.ROGUE, 0}, {Card.CClass.WARLOCK, 0}, {Card.CClass.WARRIOR, 0}, {Card.CClass.SHAMAN, 0}, {Card.CClass.NONE, 0}
        };
    }

    #endregion
}