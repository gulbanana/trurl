using System;

namespace trurl
{
    public class InsufficientPrivilegeException : Exception
    {
        private readonly string requiredPrivilege;

        public InsufficientPrivilegeException(string requiredPrivilege) => this.requiredPrivilege = requiredPrivilege;

        public string GetMessage(string command)
        {
            return string.Format("command '{0}' requires {1}", command, requiredPrivilege);
        }
    }
}
