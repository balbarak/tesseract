using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Balbarak.Tesseract.Terminals
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProccessData
    {
        public long ExitStatus;
        public long PebBaseAddress;
        public long AffinityMask;
        public long BasePriority;
        public long UniqueProcessId;
        public long InheritedFromUniqueProcessId;
    };
}


