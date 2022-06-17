using System;
using System.Collections.Generic;
using System.Text;

namespace Balbarak.Tesseract.Terminals
{
    internal class ProcessTermination
    {
        public ProcessTermination(int pid, bool expanded)
        {
            Pid = pid;
            ChildPidExpanded = expanded;
        }

        public int Pid { get; }
        public bool ChildPidExpanded { get; }
    }
}
