using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace trurl
{
    public class InsufficientPrivilegeException : Exception
    {
        public InsufficientPrivilegeException() : base() { }

        public override string Message
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public string GetMessage(string command)
        {
            return string.Format("command '{0}' only permitted to admins", command);
        }
    }
}
