using IrcDotNet;
using System.Collections.Generic;

namespace trurl;

delegate void CommandProcessor(IrcClient client, IIrcMessageSource source, IList<IIrcMessageTarget> targets, string command, IList<string> parameters);

record Command(string Name, CommandProcessor Processor, string[] HelpLines);