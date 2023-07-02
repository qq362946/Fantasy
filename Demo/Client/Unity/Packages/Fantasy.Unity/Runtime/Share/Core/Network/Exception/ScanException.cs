using System;

namespace Fantasy.Core.Network
{
    public class ScanException : Exception
    {
        public ScanException() { }

        public ScanException(string msg) : base(msg) { }
    }
}