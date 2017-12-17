using System;
using System.ComponentModel;
using System.Timers;
using SmartBot.Plugins.API;

namespace SmartBot.Plugins
{
    [Serializable]
    public class MasterwaiEmotesData : PluginDataContainer
    {
        public MasterwaiEmotesData()
        {
            Name = "MasterwaiEmotes";
            AvReactive = 0;
            AvMaxAnswers = 0;
            AvTurns = 0;
        }

        [DisplayName("How many emotes will be answered on\r\naverage until squelch. Range: 4 - 8")]
        public int AvMaxAnswers { get; set; }
        [DisplayName("Chance to answer an emote on\r\naverage. Range: 30 - 80")]
        public int AvReactive { get; set; }
        [DisplayName("How many turns on average until\r\nchanging behavior. Range: 3 - 7")]
        public int AvTurns { get; set; }

    }

    public class MasterwaiEmotes : Plugin
    {
        private static readonly Random Rng = new Random();

        private int _turnsMax;
        private int _squelchMax;
        private int _reactiveMax;

        private int _turns;
        private int _reactive;
        private int _squelch;
        private bool _squelched;

        private bool _disabled;

        public override void OnGameBegin()
        {
            CheckValues();
            base.OnGameBegin();
        }

        public override void OnStarted()
        {
            CheckValues();
            base.OnStarted();
        }

        private void CheckValues()
        {
            var c = (MasterwaiEmotesData)DataContainer;

            if (c.AvMaxAnswers < 4 || c.AvMaxAnswers > 8 ||
                c.AvReactive < 30 || c.AvReactive > 80 ||
                c.AvTurns < 3 || c.AvTurns > 7)
            {
                _disabled = true;
                Bot.Log("[MasterwaiEmotes] Values out of Range. Plugin disabled");
            }
            else
            {
                Bot.Log("[MasterwaiEmotes] Working fine!");

                _squelchMax = (200 / c.AvMaxAnswers) - 5;
                _turnsMax = (2 * c.AvTurns) - 1;
                _reactiveMax = (2 * c.AvReactive);
                SetValues();
            }
        }

        private void SetValues()
        {
            if(_disabled) return;
            _squelched = false;
            _squelch = GetRandom(5, _squelchMax);
            _reactive = GetRandom(0, _reactiveMax);
            _turns = GetRandom(1, _turnsMax);
        }

        public override void OnTurnEnd()
        {
            _turns -= 1;
            if (_turns <= 0)
            {
                SetValues();
            }
            base.OnTurnEnd();
        }

        public override void OnReceivedEmote(Bot.EmoteType emoteType)
        {
            if (!_squelched)
            {
                if (GetRandom(1, 100) <= _reactive)
                {
                    if (Bot.CurrentBoard.TurnCount <= 2 && GetRandom(1, 100) <= 50)
                    {
                        SendEmote(Bot.EmoteType.Greetings);
                    }
                    else
                    {
                        SendEmote((Bot.EmoteType)GetRandom(0, 5));
                    }
                }
                _squelched = GetRandom(0, 100) <= _squelch;
            }
            base.OnReceivedEmote(emoteType);
        }

        private static int GetRandom(int min, int max)
        {
            return Rng.Next(min, max + 1);
        }

        private static void SendEmote(Bot.EmoteType type)
        {
            var t = new Timer(GetRandom(300, 1500));
            t.Elapsed += (sender, args) =>
            {
                Bot.SendEmote(type);
            };
            t.AutoReset = false;
            t.Start();
        }
    }
}