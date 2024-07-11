#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using IrcDotNet;

namespace trurl;

// adapted from the multiclient framework in IrcDotNet samples.
abstract partial class BotBase
{
    private const int clientQuitTimeout = 1000;

    // Regex for splitting space-separated list of command parts until first parameter that begins with '/'.
    private static readonly Regex commandPartsSplitRegex = CommandPartsSplit();
    [GeneratedRegex("(?<! /.*) ", RegexOptions.None)]
    private static partial Regex CommandPartsSplit();

    private readonly bool ignoreEOF;
    protected readonly IReadOnlyDictionary<string, Command> commands;
    private bool isRunning;
    private IrcClient client;

    public BotBase(bool ignoreEOF)
    {
        this.ignoreEOF = ignoreEOF;
        isRunning = false;
        commands = InitializeCommands().ToDictionary(cmd => cmd.Name, cmd => cmd, StringComparer.CurrentCultureIgnoreCase);        
    }

    public virtual string QuitMessage => "Bot exited.";

    public void Run(string server, IrcRegistrationInfo registrationInfo, IEnumerable<string> channels)
    {
        isRunning = true;

        client = Connect(server, registrationInfo);
        client.Registered += (s, e) =>
        {
            foreach (var channel in channels)
            {
                Join(channel);
            }
        };

        // Read commands from stdin until bot terminates.
        while (isRunning)
        {
            Console.WriteLine("bot running - type 'exit' or send !quit in irc to exit");
            var line = Console.ReadLine();
            if (line == null)
            {
                Console.WriteLine("EOF");
                if (ignoreEOF)
                {
                    Thread.Sleep(Timeout.Infinite);
                }
                else
                {
                    break;
                }
            }

            if (line.Length == 0)
            {
                continue;
            }

            if (line.Equals("exit"))
            {
                isRunning = false;
            }
            else
            {
                Console.WriteLine("unrecognised command (use 'exit' to quit)");
            }
        }

        Disconnect(client);
    }

    public void Stop()
    {
        isRunning = false;
        Disconnect(client);
        Environment.Exit(0);
    }

    private IrcClient Connect(string server, IrcRegistrationInfo registrationInfo)
    {
        // Create new IRC client and connect to given server.
        var client = new StandardIrcClient();
        client.FloodPreventer = new IrcStandardFloodPreventer(4, 1000);
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
        var line = eventArgs.Text;
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

    private void ReadChatCommand(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string name, string[] parameters)
    {
        var defaultReplyTarget = GetDefaultReplyTarget(client, source, targets);

        if (commands.TryGetValue(name, out var command))
        {
            try
            {
                command.Processor(client, source, targets, name, parameters);
            }
            catch (BotException be)
            {
                client.LocalUser.SendNotice(defaultReplyTarget, be.GetMessage(name));
            }
            catch (Exception ex)
            {
                if (source is IIrcMessageTarget)
                {
                    client.LocalUser.SendNotice(defaultReplyTarget, $"Error processing '{name}' command: {ex.Message}");
                }
                Console.WriteLine(ex.ToString());
            }
        }
        else
        {
            if (source is IIrcMessageTarget && defaultReplyTarget.All(t => !t.Name.StartsWith("#")))
            {
                client.LocalUser.SendNotice(defaultReplyTarget, $"Command '{name}' not recognized.");
            }
        }
    }

    protected IList<IIrcMessageTarget> GetDefaultReplyTarget(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets)
    {
        if (targets.Contains(client.LocalUser) && source is IIrcMessageTarget)
        {
            return [(IIrcMessageTarget)source];
        }
        else
        {
            return targets;
        }
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
            {
                return;
            }
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
            {
                return;
            }
        }

        OnChannelMessageReceived(channel, e);
    }
    #endregion

    #region "Subclass API"
    protected abstract IEnumerable<Command> InitializeCommands();
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
}
