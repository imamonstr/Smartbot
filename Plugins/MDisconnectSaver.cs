using SmartBot.Plugins.API;
using System;

namespace SmartBot.Plugins
{
    [Serializable]
    public class MDisconnectSaverData : PluginDataContainer
    {
        public MDisconnectSaverData()
        {
            Name = "MDisconnectSaver";
            LatencyThreshold = 600;
        }

        public int LatencyThreshold { get; set; }
    }

    public class MDisconnectSaver : Plugin
    {
        private const string Divider = "======================================================";
        private const string Header = Divider + "\r\n[MDisconnectSaver]";
        private DateTime _latestLag = DateTime.MinValue;
        private DateTime _latestLog = DateTime.MaxValue;
        private int _tickCounter;
        private bool _stopped;

        public override void OnPluginCreated()
        {
            Debug.OnLogReceived += OnLog;
        }

        private void OnLog(string str)
        {
            _latestLog = DateTime.UtcNow;
            if (str.Contains("30seconds ago"))
            {
                Lag();
            }
        }

        public override void OnTick()
        {
            _tickCounter++;
            if (_tickCounter < 4) return;
            _tickCounter = 0;
            if (Bot.GetAverageLatency() >= Data().LatencyThreshold || Bot.GetAverageLatency() == 0)
            {
                Lag();
            }
            if (!_stopped && Bot.IsBotRunning() && _latestLog < DateTime.UtcNow - TimeSpan.FromMinutes(5))
            {
                Restart();
            }
            if (_stopped && _latestLag < DateTime.UtcNow - TimeSpan.FromMinutes(15))
            {
                Bot.Log("\r\n" + Header + "\r\nLast lag was more than 15 minutes ago. Resuming.\r\n" + Divider);
                Bot.StartRelogger();
                Bot.StartBot();
                _stopped = false;
            }
        }

        private void Lag()
        {
            if (Bot.IsBotRunning())
            {
                if (!_stopped)
                {
                    Bot.Log("\r\n" + Header + "\r\nDetected lag. Stopping after game and waiting.\r\n" + Divider);
                }
                Bot.Finish();
                _stopped = true;
            }
            _latestLag = DateTime.UtcNow;
        }

        private void Restart()
        {
            Bot.Log("\r\n" + Header + "\r\nDetected timeout. Restarting Hearthstone.\r\n" + Divider);
            Bot.CloseHs();
            Bot.StartRelogger();
        }

        private MDisconnectSaverData Data()
        {
            return (MDisconnectSaverData)DataContainer;
        }
    }
}