using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using Connection;
using MasterwaiLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartBot.Database;
using SmartBot.Plugins.API;
using SmartBotStats;

namespace SmartBot.Plugins
{
    [Serializable]
    public class MasterwaiAccountSwitcherv3ConnectorData : PluginDataContainer
    {
        public MasterwaiAccountSwitcherv3ConnectorData()
        {
            Name = "MasterwaiAccountSwitcherv3Connector";
        }

        [DisplayName("Start MAS3 with Smartbot")]
        public bool StartMas { get; set; }
    }

    public class MasterwaiAccountSwitcherv3Connector : Plugin
    {
        private bool _switching;
        private bool _enabled;
        private bool _skipReroll;
        private bool _earlyConcede;
        private bool _gettingDecks;
        private bool _sleeping;
        private State _state = State.None;

        private static readonly string BnetConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Battle.net\\Battle.net.config");
        private static readonly string BnetConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Battle.net");

        private readonly CommandHandlerServer<Mas3CommandType> _commandHandlerServer = new CommandHandlerServer<Mas3CommandType>("MAS3PIPE");

        private void CommandRecieved(Mas3CommandType command, string[] args)
        {
            if (!_enabled) return;
            switch (command)
            {
                case Mas3CommandType.Configuration:
                    if (_sleeping)
                    {
                        StopSleep();
                        return;
                    }
                    ApplyConfig(SimpleSerializer.FromJason<Configuration>(args[0]));
                    break;
                case Mas3CommandType.RequestBotStrings:
                    _commandHandlerServer.SendCommand(Mas3CommandType.ResponseBotStrings, new[]{
                        SimpleSerializer.ToJason(Bot.GetProfiles()),
                        SimpleSerializer.ToJason(Bot.GetMulliganProfiles()),
                        SimpleSerializer.ToJason(Bot.GetDiscoverProfiles()),
                        SimpleSerializer.ToJason(Bot.GetArenaProfiles())
                        });
                    break;
                case Mas3CommandType.RequestDecks:
                    if (!_gettingDecks)
                    {
                        _gettingDecks = true;
                        Bot.RefreshDecks();
                        WaitNewDecks();
                    }
                    break;
                case Mas3CommandType.StartSleep:
                    StartSleep();
                    break;
                case Mas3CommandType.StartBot:
                    Bot.StartBot();
                    Bot.SuspendBot();
                    _commandHandlerServer.SendCommand(Mas3CommandType.EnterAccount, new[] { "" });
                    break;
                case Mas3CommandType.StopBot:
                    Bot.StopBot();
                    break;
            }
        }

        private void ApplyConfig(Configuration configuration)
        {
            if (_state == State.None) return;

            //Do not accept further commands
            var enteringState = _state;
            _state = State.None;

            //If should switch
            if (Bot.GetCurrentAccount() != configuration.Login || GetCurrentServer() != configuration.Server)
            {
                //But isn't allowed to
                if (enteringState == State.FirstGame)
                {
                    _state = State.FirstGame;
                    ApplyConfig(new Configuration(Bot.GetCurrentAccount(), "", Bot.GetDecks().First(), Bot.Mode.Practice, Bot.GetMulliganProfiles().First(), Bot.GetProfiles().First()) { Concede = true, Server = GetCurrentServer() });
                    _commandHandlerServer.SendCommand(Mas3CommandType.StayOneGame, new[] { Bot.GetCurrentAccount(), GetCurrentServer().ToString() });
                    return;
                }

                //And is allowed to

                try
                {
                    var p = Process.GetProcesses().ToList().Find(x => x.ProcessName == "Battle.net");
                    if (p != null)
                    {
                        p.Kill();
                        p.WaitForExit(1500);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                ChangeLoginServer(configuration.Server);
                ChangePlayServer(configuration.Server);


                _switching = true;
                Bot.SwitchAccount(configuration.Login, configuration.Password);
                return;
            }

            //We are on the correct account

            //Reroll
            if (configuration.Reroll != null && enteringState != State.FirstGame)
            {
                if (!_skipReroll)
                {
                    if (Bot.CanCancelQuest() && Bot.GetQuests().Exists(x => x.Name == configuration.Reroll.Name))
                    {
                        var oldQuests = Bot.GetQuests();

                        Bot.CancelQuest(Bot.GetQuests().Find(x => x.Name == configuration.Reroll.Name));
                        _state = enteringState;
                        var t = new Timer(2000);
                        t.Elapsed += (sender, args) => WaitNewQuests(1, oldQuests);
                        t.AutoReset = false;
                        t.Start();
                        return;
                    }
                }
                else
                {
                    _skipReroll = false;
                }
            }

            //Apply settings
            _earlyConcede = configuration.Concede;
            Bot.ChangeDeck(configuration.Deck.Name);
            Bot.ChangeMode(configuration.Mode);
            Bot.ChangeMulligan(configuration.Mulligan);
            Bot.ChangeProfile(configuration.Profile);

            Bot.SetCloseHs(configuration.CloseHs);
            Bot.SetMaxArenaPayments(configuration.MaxArenaPayments);
            Bot.SetMaxWins(configuration.MaxWins);
            Bot.SetMaxHours(configuration.MaxHours);
            Bot.SetMaxLosses(configuration.MaxLosses);
            Bot.SetAutoConcede(configuration.AutoConcede);
            Bot.SetAutoConcedeAlternativeMode(configuration.AutoConcedeAlternativeMode);
            Bot.SetAutoConcedeMaxRank(configuration.AutoConcedeMaxRank);
            Bot.SetConcedeWhenLethal(configuration.ConcedeWhenLethal);
            Bot.SetCurrentArenaPayments(configuration.CurrentArenaPayments);
            Bot.SetMaxRank(configuration.MaxRank);
            Bot.SetMinRank(configuration.MinRank);

            //Go
            if (!Bot.IsBotRunning())
            {
                //Bot.Log("GO! -> StartBot");
                var t = new Timer(2000);
                t.Elapsed += (sender, args) => Bot.StartBot();
                t.AutoReset = false;
                t.Start();
            }
            else
            {
                //Bot.Log("GO! -> ResumeBot");
                Bot.ResumeBot();
            }
        }


        private void StartSleep()
        {
            Bot.Log("Going into sleep mode.");
            Bot.Log("STEP 1");
            if (_sleeping) return;
            Bot.Log("STEP 2");
            _sleeping = true;
            Bot.CloseHs();
            Bot.Log("STEP 3");
            Bot.StopRelogger();
            Bot.Log("STEP 4");
            Bot.SuspendBot();
            Bot.Log("STEP 5");
        }

        private void StopSleep()
        {
            if (!_sleeping) return;
            Bot.Log("Awakening from sleep mode.");
            _sleeping = false;
            _switching = true;
            Bot.ResumeBot();
            Bot.StartRelogger();
        }


        private void WaitNewQuests(int tries, List<Quest> oldQuests)
        {
            var newQuests = Bot.GetQuests();
            //Bot.Log("Timer: " + tries);
            if (!oldQuests.All(x => newQuests.Exists(y => y.Name == x.Name)) || tries >= 10)
            {
                if (tries >= 10) _skipReroll = true;

                //Bot.Log("Timer: New Quests");
                _commandHandlerServer.SendCommand(Mas3CommandType.RerollQuests, new[] { Bot.GetCurrentAccount(), GetCurrentServer().ToString(), SimpleSerializer.ToJason(newQuests) });
                return;
            }

            var t = new Timer(2000);
            t.Elapsed += (sender, args) => WaitNewQuests(tries + 1, oldQuests);
            t.AutoReset = false;
            t.Start();
        }

        private void WaitNewDecks()
        {
            var t = new Timer(5000);
            t.Elapsed += (sender, args) =>
            {
                _gettingDecks = false;
                _commandHandlerServer.SendCommand(Mas3CommandType.ResponseDecks, new[] { Bot.GetCurrentAccount(), GetCurrentServer().ToString(), SimpleSerializer.ToJason(Bot.GetDecks()) });
            };
            t.AutoReset = false;
            t.Start();
        }

        private void WaitDecklistUpdate()
        {
            var t = new Timer(2000);
            t.Elapsed += (sender, args) =>
            {
                //Bot.Log("[DeckUpdateFix] Checking Decks.");
                if (Bot.GetDecks().Count == 0)
                {
                    //Bot.Log("[DeckUpdateFix] Decks not Updated. Updating now.");
                    Bot.RefreshDecks();
                }
                else
                {
                    //Bot.Log("[DeckUpdateFix] Decks Updated.");
                    t.Stop();
                }
            };
            t.AutoReset = true;
            t.Start();
        }

        private void ChangePlayServer(Server server)
        {
            string sStr;
            switch (server)
            {
                case Server.Europe:
                    sStr = "EU";
                    break;
                case Server.Americas:
                    sStr = "US";
                    break;
                case Server.Asia:
                    sStr = "KR";
                    break;
                case Server.China:
                    sStr = "";
                    break;
                default:
                    sStr = "ERROR";
                    break;
            }

            var files = Directory.GetFiles(BnetConfigFolder).ToList();
            files.Remove(BnetConfig);

            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                JToken config = JToken.Parse(text);

                var wtcg = config.SelectToken("User.Client.PlayScreen.GameFamily.WTCG");
                if (wtcg != null)
                {
                    var latestRegion = wtcg["LastSelectedGameRegion"];

                    if (latestRegion != null)
                    {
                        wtcg["LastSelectedGameRegion"] = sStr;
                    }
                    else
                    {
                        ((JObject)wtcg).Add("LastSelectedGameRegion", sStr);
                    }
                }
                File.WriteAllText(file, config.ToString());
            }


        }

        private void ChangeLoginServer(Server server)
        {
            var text = File.ReadAllText(BnetConfig);
            JToken config = JToken.Parse(text);

            var services = config.SelectToken("*.Services");
            if (services == null) return;
            switch (server)
            {
                case Server.Europe:
                    services["LastLoginRegion"] = "EU";
                    services["LastLoginAddress"] = "eu.actual.battle.net";
                    services["LastLoginTassadar"] = "eu.battle.net" + (char)92 + "/login";
                    break;
                case Server.Americas:
                    services["LastLoginRegion"] = "US";
                    services["LastLoginAddress"] = "us.actual.battle.net";
                    services["LastLoginTassadar"] = "us.battle.net" + (char)92 + "/login";
                    break;
                case Server.Asia:
                    services["LastLoginRegion"] = "KR";
                    services["LastLoginAddress"] = "kr.actual.battle.net";
                    services["LastLoginTassadar"] = "kr.battle.net" + (char)92 + "/login";
                    break;
                case Server.China:
                    services["LastLoginRegion"] = "CN";
                    services["LastLoginAddress"] = "cn.actual.battle.net";
                    services["LastLoginTassadar"] = "www.battlenet.com.cn"+(char)92+"/login";


                    break;
            }
            File.WriteAllText(BnetConfig, config.ToString().Remove(config.ToString().IndexOf("\\/"), 1));
        }

        private Server GetCurrentServer()
        {

            switch (Bot.CurrentRegion())
            {
                case BnetRegion.REGION_CN:
                    return Server.China;
                case BnetRegion.REGION_EU:
                    return Server.Europe;
                case BnetRegion.REGION_KR:
                    return Server.Asia;
                case BnetRegion.REGION_US:
                    return Server.Americas;
                default:

                    var text = File.ReadAllText(Directory.GetFiles(BnetConfigFolder).First(x => x != "Battle.net.config"));
                    JToken config;
                    using (JsonTextReader reader = new JsonTextReader(new StringReader(text)))
                    {
                        config = JToken.ReadFrom(reader);
                    }
                    var server = config.SelectToken("User.Client.PlayScreen.GameFamily.WTCG.LastSelectedGameRegion");
                    if (server != null)
                    {
                        switch ((string)server)
                        {
                            case "EU":
                                return Server.Europe;
                            case "US":
                                return Server.Americas;
                            case "KR":
                                return Server.Asia;
                            case "CN":
                                return Server.China;
                            case "":
                                return Server.China;
                        }
                    }
                    return Server.Invalid;
            }

        }

        #region Events

        public override void OnPluginCreated()
        {
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Masterwai\\"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Masterwai\\");
            }

            _commandHandlerServer.Start(CommandRecieved);

            var t = new Timer(2500);
            t.Elapsed += (sender, args) =>
            {
                var conn = _commandHandlerServer.PipeServer.Client.IsConnected;
                if (conn == _enabled) return;
                Bot.Log(conn ? "Now in [SWITCHER MODE]" : "Now in [MANUAL MODE]");
                _enabled = conn;
                DataContainer.Enabled = _enabled;
            };
            t.AutoReset = true;
            t.Start();

            var data = (MasterwaiAccountSwitcherv3ConnectorData)DataContainer;
            if (data.StartMas && Process.GetProcessesByName("MasterwaiAccountSwitcherv3").Length == 0)
            {
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\MasterwaiAccountSwitcherv3.exe", "mini");
            }
            //foreach (var process in Process.GetProcesses())
            //{
            //    Bot.Log(process.ProcessName);
            //}
        }

        public override void OnGameEnd()
        {
            if (!_enabled) return;

            _earlyConcede = false;

            Bot.SuspendBot();
            _state = State.AwaitingCommand;

            if (Bot.CurrentMode() == Bot.Mode.Arena || Bot.CurrentMode() == Bot.Mode.ArenaAuto)
            {
                var t = new Timer(3000);
                t.Elapsed += (sender, args) => { GameEnd(true); };
                t.AutoReset = false;
                t.Start();
            }
            else
            {
                GameEnd(false);
            }
        }

        private void GameEnd(bool arena, bool arenaFailed = false)
        {
            var arenaTicket = arena ? (!(Statistics.ArenaLosses == 3 || Statistics.ArenaWins == 12)).ToString() : "";

            if (arenaFailed) arenaTicket = false.ToString();

            int gold = Statistics.Gold;

            bool reached = Bot.IsGoldCapReached();
            int wildRank = Bot.GetPlayerDatas().GetRank(true);
            int standardRank = Bot.GetPlayerDatas().GetRank(false);
            List<Quest> quests = Bot.GetQuests();
            
            var info = new[]
            {
                Bot.GetCurrentAccount(), GetCurrentServer().ToString(), gold.ToString(), wildRank.ToString(), standardRank.ToString(), SimpleSerializer.ToJason(quests), arenaTicket, reached.ToString()
            };
            _commandHandlerServer.SendCommand(Mas3CommandType.EndGameData, info);
        }

        public override void OnDecklistUpdate()
        {
            if (!_enabled || _gettingDecks || !_switching) return;

            var decks = Bot.GetDecks();
            if (decks.Count == 0 || GetCurrentServer() == Server.Invalid)
            {
                var t = new Timer(2000);
                t.Elapsed += (sender, args) => OnDecklistUpdate();
                t.AutoReset = false;
                t.Start();
                return;
            }

            //Entering account
            _switching = false;
            Bot.ChangeMode(Bot.GetDecks().First().Type == Deck.DeckType.Standard ? Bot.Mode.UnrankedStandard : Bot.Mode.UnrankedWild);
            Bot.ChangeDeck(Bot.GetDecks().First().Name);
            Bot.SuspendBot();
            _state = State.FirstGame;
            _commandHandlerServer.SendCommand(Mas3CommandType.EnterAccount, new[] { "" });
        }

        public override void OnStarted()
        {
            if (!_enabled) return;

            Bot.SuspendBot();
            _state = State.FirstGame;
            _commandHandlerServer.SendCommand(Mas3CommandType.EnterAccount, new[] { "" });
        }

        public override void OnGameBegin()
        {
            if (!_enabled || !_earlyConcede) return;
            _earlyConcede = false;
            Bot.Concede();
        }

        public override void OnArenaTicketPurchaseFailed()
        {
            if (!_enabled) return;

            Bot.SuspendBot();
            _state = State.AwaitingCommand;

            GameEnd(false, true);
        }

        public override void OnInjection()
        {
            WaitDecklistUpdate();
            base.OnInjection();
        }
        #endregion

        #region Disposing

        ~MasterwaiAccountSwitcherv3Connector()
        {
            _commandHandlerServer.PipeServer.Close();
        }

        public override void Dispose()
        {
            _commandHandlerServer.PipeServer.Close();
        }

        #endregion
    }

    public enum State
    {
        None,
        FirstGame,
        AwaitingCommand
    }
}