using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFileLogging
{
    internal readonly record struct LogMessageEntry(string Message, bool LogAsError = false);
}
