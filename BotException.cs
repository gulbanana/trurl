using System;

namespace trurl;

abstract class BotException : Exception
{
    public abstract string GetMessage(string command);
}

class InsufficientPrivilegeException(string requiredPrivilege) : BotException
{
    public override string GetMessage(string command) => $"command !{command} requires {requiredPrivilege}";
}

class InvalidParametersException(int minParameters, int? maxParameters = null) : BotException
{
    public override string GetMessage(string command)
    {
        if (minParameters == 0)
        {
            return $"command !{command} takes no params - try !help {command}";
        }
        else if (!maxParameters.HasValue)
        {
            return $"command !{command} takes {minParameters} param(s) - try !help {command}";
        }
        else
        {
            return $"command !{command} takes {minParameters} to {maxParameters.Value} param(s) - try !help {command}";
        }
    }
}

class LimitsExceededException(string limit, int max) : BotException
{
    public override string GetMessage(string _) => $"exceeded permitted {limit} (max {max})";
}
