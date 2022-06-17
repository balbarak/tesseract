using Balbarak.Tesseract.Terminals;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Balbarak.Tesseract
{
    internal partial class TerminalClient : IDisposable
    {

        private delegate bool ConsoleCtrlDelegate(ConsoleCtrlEvent CtrlType);

        private readonly ConcurrentQueue<string> _outputData = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _errorData = new ConcurrentQueue<string>();
        private readonly AsyncManualResetEvent _outputProcessEvent = new AsyncManualResetEvent();
        private readonly TimeSpan _sigintTimeout = TimeSpan.FromMilliseconds(7500);
        private readonly TimeSpan _sigtermTimeout = TimeSpan.FromMilliseconds(2500);
        private readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>();
        private readonly StringBuilder _output = new StringBuilder();
        private TaskCompletionSource<bool> _processExitedCompletionSource;

        private Encoding _outputEncoding = Encoding.UTF8;
        private bool _waitingOnStreams = false;
        private Stopwatch _stopWatch;
        private Process _proc;
        private int _streamReadCount = 0;
        private StreamWriter _inputWriter;
        private readonly ITraceWriter _trace;

        public event EventHandler<string> OnOutput;

        public TerminalClient()
        {

        }

        public TerminalClient(ITraceWriter trace)
        {
            _trace = trace;
        }

        public TerminalClient(ITraceWriter trace, Encoding encoding) : this(trace)
        {
            _outputEncoding = encoding;
        }

        public TerminalClient(ITraceWriter trace, Dictionary<string, string> environmentVariables) : this(trace)
        {
            _environmentVariables = environmentVariables;
        }

        public TerminalClient(ITraceWriter trace, Dictionary<string, string> environmentVariables, Encoding encoding) : this(trace, encoding)
        {
            _environmentVariables = environmentVariables;
        }

        public Task<int> ExcuteAsync(string workingDir, string fileName, string args, CancellationToken ctk = default)
        {
            return ExcuteInternalAsync(workingDir, fileName, args, cancellationToken: ctk);
        }

        public async Task<string> ExcuteAndReadOutputAsync(string workingDir, string fileName, string args, CancellationToken ctk = default)
        {
            _output.Clear();

            var exitCode = await ExcuteInternalAsync(
                workingDir,
                fileName,
                args,
                saveOutput: true,
                cancellationToken: ctk);

            return _output.ToString();
        }


        private async Task<int> ExcuteInternalAsync(
            string workingDir,
            string fileName,
            string args,
            ConcurrentQueue<string> inputs = null,
            bool killProcessOnCancel = true,
            bool saveOutput = false,
            CancellationToken cancellationToken = default)
        {
            InitProcess(workingDir, fileName, args, killProcessOnCancel);

            // Ensure we process STDOUT even the process exit event happen before we start read STDOUT stream.
            Interlocked.Increment(ref _streamReadCount);
            Interlocked.Increment(ref _streamReadCount);

            _stopWatch = Stopwatch.StartNew();

            _proc.Start();

            var readErrorTask = ReadStream(_proc.StandardError, _errorData);

            var readOutputTask = ReadStream(_proc.StandardOutput, _outputData);

            if (inputs != null)
            {
                var task = WriteStream(inputs, _proc.StandardInput, true);
            }
            else
            {
                // Close the input stream. This is done to prevent commands from blocking the build waiting for input from the user.
                _proc.StandardInput.Close();
            }

            using (var registration = cancellationToken.Register(async () => await CancelAndKillProcessTree(killProcessOnCancel)))
            {
                _trace?.Info($"Process started with process id {_proc.Id}, waiting for process exit.");

                while (true)
                {
                    Task outputSignal = _outputProcessEvent.WaitAsync();

                    var signaled = await Task.WhenAny(outputSignal, _processExitedCompletionSource.Task);

                    if (signaled == outputSignal)
                    {
                        ProcessOutput(saveOutput);
                    }
                    else
                    {
                        _stopWatch.Stop();
                        break;
                    }
                }

                ProcessOutput(saveOutput);

                _trace?.Info($"Finished process {_proc.Id} with exit code {_proc.ExitCode}, and elapsed time {_stopWatch.Elapsed}.");
            }

            return _proc.ExitCode;
        }

        private void InitProcess(string workingDir, string fileName, string args, bool killProccessOnCancel)
        {
            _processExitedCompletionSource = new TaskCompletionSource<bool>();

            _trace?.Info("Starting process:");
            _trace?.Info($"  File name: '{fileName}'");
            _trace?.Info($"  Arguments: '{args}'");
            _trace?.Info($"  Working directory: '{workingDir}'");
            _trace?.Info($"  Encoding web name: {_outputEncoding?.WebName} ; code page: '{_outputEncoding?.CodePage}'");
            _trace?.Info($"  Force kill process on cancellation: '{killProccessOnCancel}'");

            _proc = new Process();
            _proc.StartInfo.WorkingDirectory = workingDir;
            _proc.StartInfo.FileName = fileName;
            _proc.StartInfo.Arguments = args;
            _proc.StartInfo.UseShellExecute = false;
            _proc.StartInfo.RedirectStandardOutput = true;
            _proc.StartInfo.RedirectStandardError = true;
            _proc.StartInfo.RedirectStandardInput = true;

            if (_outputEncoding != null)
            {
                _proc.StartInfo.StandardOutputEncoding = _outputEncoding;
                _proc.StartInfo.StandardErrorEncoding = _outputEncoding;
            }

            foreach (var item in _environmentVariables)
            {
                _proc.StartInfo.EnvironmentVariables[item.Key] = item.Value;
            }

            _proc.EnableRaisingEvents = true;
            _proc.Exited += OnProcessExited;
        }

        private Task ReadStream(StreamReader reader, ConcurrentQueue<string> dataBuffer)
        {
            return Task.Run(() =>
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (line != null)
                    {
                        dataBuffer.Enqueue(line);
                        _outputProcessEvent.Set();

                    }
                }

                _trace?.Info("STDOUT/STDERR stream read finished.");

                if (Interlocked.Decrement(ref _streamReadCount) == 0 && _waitingOnStreams)
                {
                    _processExitedCompletionSource.TrySetResult(true);
                }
            });
        }

        private Task WriteStream(ConcurrentQueue<string> redirectStandardIn, StreamWriter standardIn, bool keepStandardInOpen)
        {
            return Task.Run(() =>
            {
                // Write the contents as UTF8 to handle all characters.
                var utf8Writer = new StreamWriter(standardIn.BaseStream, new UTF8Encoding(false));

                while (!_processExitedCompletionSource.Task.IsCompleted)
                {
                    string input = null;

                    bool success = redirectStandardIn.TryDequeue(out input);

                    if (input != null)
                    {
                        utf8Writer.WriteLine(input);
                        utf8Writer.Flush();

                        if (!keepStandardInOpen)
                        {
                            _trace?.Info("Close STDIN after the first redirect finished.");
                            standardIn.Close();
                            break;
                        }
                    }

                }

                _trace?.Info("STDIN stream write finished.");
            });
        }

        private Task WriteStream(StreamWriter standardIn)
        {
            return Task.Run(() =>
            {
                // Write the contents as UTF8 to handle all characters.
                _inputWriter = new StreamWriter(standardIn.BaseStream, new UTF8Encoding(false));

                while (!_processExitedCompletionSource.Task.IsCompleted)
                {

                }

                _trace?.Info("STDIN stream write finished.");
            });
        }

        private void ProcessOutput(bool saveOutput = false)
        {
            List<string> errorData = new List<string>();
            List<string> outputData = new List<string>();

            string errorLine;

            while (_errorData.TryDequeue(out errorLine))
            {
                errorData.Add(errorLine);
            }

            string outputLine;

            while (_outputData.TryDequeue(out outputLine))
            {
                outputData.Add(outputLine);
            }

            _outputProcessEvent.Reset();

            if (errorData.Any())
            {
                foreach (var item in errorData)
                {
                    if (item == null)
                        continue;

                    OnOutput?.Invoke(this, item);

                    if (saveOutput)
                        _output.AppendLine(item);
                }
            }

            if (outputData.Any())
            {
                foreach (var item in outputData)
                {
                    if (item == null)
                        continue;

                    OnOutput?.Invoke(this, item);

                    if (saveOutput)
                        _output.AppendLine(item);

                }
            }
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            _trace?.Info($"Exited process {_proc.Id} with exit code {_proc.ExitCode}");

            if (_streamReadCount != 0)
            {
                _waitingOnStreams = true;

                Task.Run(async () =>
                {
                    // Wait 5 seconds and then Cancel/Kill process tree
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    KillProcessTree();

                    _processExitedCompletionSource.TrySetResult(true);

                });
            }
            else
            {
                _processExitedCompletionSource.TrySetResult(true);
            }
        }

        private async Task CancelAndKillProcessTree(bool killProcessOnCancel)
        {
            if (!killProcessOnCancel)
            {
                bool sigint_succeed = await SendSIGINT(_sigintTimeout);
                if (sigint_succeed)
                {
                    _trace?.Info("Process cancelled successfully through Ctrl+C/SIGINT.");
                    return;
                }

                bool sigterm_succeed = await SendSIGTERM(_sigtermTimeout);
                if (sigterm_succeed)
                {
                    _trace?.Info("Process terminate successfully through Ctrl+Break/SIGTERM.");
                    return;
                }
            }

            _trace?.Info("Kill entire process tree since both cancel and terminate signal has been ignored by the target process.");

            KillProcessTree();
        }

        private void KillProcessTree()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsKillProcessTree();
            }
            else
            {
                NixKillProcessTree();
            }
        }

        private async Task<bool> SendSIGINT(TimeSpan timeout)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await SendCtrlSignal(ConsoleCtrlEvent.CTRL_C, timeout);
            }

            return await SendPosixSignal(PosixSignals.SIGINT, timeout);
        }

        private async Task<bool> SendSIGTERM(TimeSpan timeout)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await SendCtrlSignal(ConsoleCtrlEvent.CTRL_BREAK, timeout);
            }

            return await SendPosixSignal(PosixSignals.SIGTERM, timeout);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_proc != null)
                {
                    _proc.Dispose();
                    _proc = null;
                }
            }
        }
    }
}
