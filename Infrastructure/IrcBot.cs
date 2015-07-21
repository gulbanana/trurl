﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using IrcDotNet;
using System.Runtime.InteropServices;

namespace trurl
{
    // Provides core functionality for a single-server IRC bot - originally adapted from the multiclient framework in IrcDotNet samples.
    public abstract class BotBase
    {
        private const int clientQuitTimeout = 1000;

        // Regex for splitting space-separated list of command parts until first parameter that begins with '/'.
        private static readonly Regex commandPartsSplitRegex = new Regex("(?<! /.*) ", RegexOptions.None);

        // Dictionary of all chat command processors, keyed by name.
        private IDictionary<string, CommandProcessor> commands;

        // True if the read loop is currently active, false if ready to terminate.
        private bool isRunning;

        IrcClient client;

        public BotBase()
        {
            this.isRunning = false;
            this.commands = new Dictionary<string, CommandProcessor>(StringComparer.InvariantCultureIgnoreCase);
            InitializeCommands();
        }

        public virtual string QuitMessage
        {
            get { return "We want the Demon, you see, to extract from the dance of atoms only information that is genuine."; }
        }

        protected IDictionary<string, CommandProcessor> Commands
        {
            get { return this.commands; }
        }

        public void Run(string server, IrcRegistrationInfo registrationInfo, IEnumerable<string> channels)
        {
            this.isRunning = true;

            client = Connect(server, registrationInfo);
            client.Registered += (s, e) =>
            {
                foreach (var channel in channels)
                    Join(channel);
            };

            // Read commands from stdin until bot terminates.
            while (this.isRunning)
            {
                Console.WriteLine("bot running - type 'exit' or send !quit in irc to exit");
                var line = Console.ReadLine();
                if (line == null)
                    break;
                if (line.Length == 0)
                    continue;

                if (line.Equals("exit"))
                    this.isRunning = false;
                else
                    Console.WriteLine("unrecognised command (use 'exit' to quit)");
            }

            Disconnect(client);
        }

        public void Stop()
        {
            this.isRunning = false;
            Disconnect(client);
            Environment.Exit(0);
        }

        private IrcClient Connect(string server, IrcRegistrationInfo registrationInfo)
        {
            // Create new IRC client and connect to given server.
            var client = new IrcClient();
            client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            client.Connected += IrcClient_Connected;
            client.Disconnected += IrcClient_Disconnected;
            client.Registered += IrcClient_Registered;

            // Wait until connection has succeeded or timed out.
            using (var connectedEvent = new ManualResetEventSlim(false))
            {
                client.Connected += (sender2, e2) => connectedEvent.Set();
                client.Connect(server, false, registrationInfo);
                if (!connectedEvent.Wait(10000))
                {
                    client.Dispose();
                    Console.WriteLine("Connection to '{0}' timed out.", server);
                    return null;
                }
            }

            Console.WriteLine("Now connected to '{0}'.", server);

            // Add new client to collection.
            return client;
        }

        private void Disconnect(IrcClient client)
        {
            var serverName = client.ServerName;
            client.Quit(clientQuitTimeout, this.QuitMessage);
            client.Dispose();

            Console.WriteLine("Disconnected from '{0}'.", serverName);
        }

        protected void Join(string channel)
        {
            client.Channels.Join(channel);

            Console.WriteLine("Joined '{0}'.", channel);
        }

        protected void Leave(string channel)
        {
            client.Channels.Leave(channel);

            Console.WriteLine("Left '{0}'.", channel);
        }

        private bool ReadChatCommand(IrcClient client, IrcMessageEventArgs eventArgs)
        {
            // Check if given message represents chat command.
            var line = eventArgs.Text.TrimEnd(' ');
            if (line.Length > 1 && line.StartsWith("!"))
            {
                // Process command.
                var parts = commandPartsSplitRegex.Split(line.Substring(1)).Select(p => p.TrimStart('/')).ToArray();
                var command = parts.First();
                var parameters = parts.Skip(1).ToArray();
                ReadChatCommand(client, eventArgs.Source, eventArgs.Targets, command, parameters);
                return true;
            }
            return false;
        }

        private void ReadChatCommand(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, string[] parameters)
        {
            var defaultReplyTarget = GetDefaultReplyTarget(client, source, targets);

            CommandProcessor processor;
            if (this.commands.TryGetValue(command, out processor))
            {
                try
                {
                    processor(client, source, targets, command, parameters);
                }
                catch (InvalidCommandParametersException icpe)
                {
                    client.LocalUser.SendNotice(defaultReplyTarget, icpe.GetMessage(command));
                }
                catch (InsufficientPrivilegeException ipe)
                {
                    client.LocalUser.SendNotice(defaultReplyTarget, ipe.GetMessage(command));
                }
                catch (Exception ex)
                {
                    if (source is IIrcMessageTarget)
                    {
                        client.LocalUser.SendNotice(defaultReplyTarget, string.Format("Error processing '{0}' command: {1}", command, ex.Message));
                    }
                }
            }
            else
            {
                if (source is IIrcMessageTarget)
                {
                    client.LocalUser.SendNotice(defaultReplyTarget, string.Format("Command '{0}' not recognized.", command));
                }
            }
        }

        protected IList<IIrcMessageTarget> GetDefaultReplyTarget(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets)
        {
            if (targets.Contains(client.LocalUser) && source is IIrcMessageTarget)
                return new[] { (IIrcMessageTarget)source };
            else
                return targets;
        }


        #region IRC Client Event Handlers

        private void IrcClient_Connected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            OnClientConnect(client);
        }

        private void IrcClient_Disconnected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            OnClientDisconnect(client);
        }

        private void IrcClient_Registered(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            client.LocalUser.NoticeReceived += IrcClient_LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += IrcClient_LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += IrcClient_LocalUser_JoinedChannel;
            client.LocalUser.LeftChannel += IrcClient_LocalUser_LeftChannel;

            Console.Beep();

            OnClientRegistered(client);
        }

        private void IrcClient_LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            OnLocalUserNoticeReceived(localUser, e);
        }

        private void IrcClient_LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            if (e.Source is IrcUser)
            {
                // Read message and process if it is chat command.
                if (ReadChatCommand(localUser.Client, e))
                    return;
            }

            OnLocalUserMessageReceived(localUser, e);
        }

        private void IrcClient_LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined += IrcClient_Channel_UserJoined;
            e.Channel.UserLeft += IrcClient_Channel_UserLeft;
            e.Channel.MessageReceived += IrcClient_Channel_MessageReceived;
            e.Channel.NoticeReceived += IrcClient_Channel_NoticeReceived;

            OnLocalUserJoinedChannel(localUser, e);
        }

        private void IrcClient_LocalUser_LeftChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined -= IrcClient_Channel_UserJoined;
            e.Channel.UserLeft -= IrcClient_Channel_UserLeft;
            e.Channel.MessageReceived -= IrcClient_Channel_MessageReceived;
            e.Channel.NoticeReceived -= IrcClient_Channel_NoticeReceived;

            OnLocalUserJoinedChannel(localUser, e);
        }

        private void IrcClient_Channel_UserLeft(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;

            OnChannelUserJoined(channel, e);
        }

        private void IrcClient_Channel_UserJoined(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;

            OnChannelUserLeft(channel, e);
        }

        private void IrcClient_Channel_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;

            OnChannelNoticeReceived(channel, e);
        }

        private void IrcClient_Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;

            if (e.Source is IrcUser)
            {
                // Read message and process if it is chat command.
                if (ReadChatCommand(channel.Client, e))
                    return;
            }

            OnChannelMessageReceived(channel, e);
        }

        #endregion

        #region "Subclass API"
        protected abstract void InitializeCommands();

        protected virtual void OnClientConnect(IrcClient client) { }
        protected virtual void OnClientDisconnect(IrcClient client) { }
        protected virtual void OnClientRegistered(IrcClient client) { }
        protected virtual void OnLocalUserJoinedChannel(IrcLocalUser localUser, IrcChannelEventArgs e) { }
        protected virtual void OnLocalUserLeftChannel(IrcLocalUser localUser, IrcChannelEventArgs e) { }
        protected virtual void OnLocalUserNoticeReceived(IrcLocalUser localUser, IrcMessageEventArgs e) { }
        protected virtual void OnLocalUserMessageReceived(IrcLocalUser localUser, IrcMessageEventArgs e) { }
        protected virtual void OnChannelUserJoined(IrcChannel channel, IrcChannelUserEventArgs e) { }
        protected virtual void OnChannelUserLeft(IrcChannel channel, IrcChannelUserEventArgs e) { }
        protected virtual void OnChannelNoticeReceived(IrcChannel channel, IrcMessageEventArgs e) { }
        protected virtual void OnChannelMessageReceived(IrcChannel channel, IrcMessageEventArgs e) { }
        #endregion

        protected delegate void CommandProcessor(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters);
    }
}
