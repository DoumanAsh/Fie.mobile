using System;
using System.Diagnostics;

namespace Logging {
    public class Debug {
        [Conditional("DEBUG")]
        public static void log(string format, params object[] args) {
            Console.WriteLine(format, args);
        }
    }
}
