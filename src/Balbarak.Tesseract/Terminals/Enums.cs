using System;
using System.Collections.Generic;
using System.Text;

namespace Balbarak.Tesseract.Terminals
{
    internal enum PosixSignals : int
    {
        SIGINT = 2,
        SIGTERM = 15
    }
    internal enum ConsoleCtrlEvent
    {
        CTRL_C = 0,
        CTRL_BREAK = 1
    }
    internal enum PROCESSINFOCLASS : int
    {
        ProcessBasicInformation = 0
    };
}
