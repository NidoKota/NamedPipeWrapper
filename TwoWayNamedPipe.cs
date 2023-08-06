using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace NamedPipeWrapper
{
    public class TwoWayNamedPipe : IDisposable
    {
        private readonly Process _childProcess;
        private readonly Process _currentProcess;

        public TwoWayNamedPipe(Process childProcess = null)
        {
            _childProcess = childProcess;
            _currentProcess = Process.GetCurrentProcess();
        }

        public async Task Connect()
        {
            await FirstConnect();
            await SecondConnect();

            IsValid = true;
        }

        private async Task FirstConnect()
        {
            if (_childProcess == null)
            {
                await ReadServerStart("TwoWayNamedPipeFirst", _currentProcess);
            }
            else
            {
                await WriteClientStart("TwoWayNamedPipeFirst", _childProcess);
            }
        }

        private async Task SecondConnect()
        {
            if (_childProcess == null)
            {
                await WriteClientStart("TwoWayNamedPipeSecond", _currentProcess);
            }
            else
            {
                await ReadServerStart("TwoWayNamedPipeSecond", _childProcess);
            }
        }

        public event Action<string> OnRead;
        public event Action OnDisconnect;
        public bool IsValid { get; private set; }

        private StreamReader _reader;
        private NamedPipeServerStream _pipeServer;
        private CancellationTokenSource _pipeServerCancelSource = new CancellationTokenSource();

        private async Task ReadServerStart(string pipeName, Process process)
        {
            _pipeServer = new NamedPipeServerStream($"{pipeName}{process.Id}");

            await _pipeServer.WaitForConnectionAsync();

            _reader = new StreamReader(_pipeServer);

            _ = Monitoring()
                .ToString(); // Forget

            async Task Monitoring()
            {
                try
                {
                    while (true)
                    {
                        var message = await _reader.ReadLineAsync().WithCancellation(_pipeServerCancelSource.Token);
                        _pipeServerCancelSource = new CancellationTokenSource();
                        
                        if (_pipeServer.IsConnected) OnRead?.Invoke(message);
                        else
                        {
                            OnDisconnectInternal();
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }
        }

        private StreamWriter _writer;
        private NamedPipeClientStream _pipeClient;

        private async Task WriteClientStart(string pipeName, Process process)
        {
            _pipeClient = new NamedPipeClientStream($"{pipeName}{process.Id}");
            try
            {
                await _pipeClient.ConnectAsync(5000);
                _writer = new StreamWriter(_pipeClient);
            }
            catch (TimeoutException)
            {
                Utility.DebugLog($"TimeOut");
                Dispose();
            }
        }

        public async Task Write(string message, CancellationToken token)
        {
            if (!IsValid) return;

            try
            {
                await _writer.WriteLineAsync(message).WithCancellation(token);
                await _writer.FlushAsync().WithCancellation(token);
            }
            catch (OperationCanceledException) { }

            Utility.DebugLog($"Send: {message} {DateTime.Now}");
        }
        
        private void OnDisconnectInternal()
        {
            IsValid = false;
            OnDisconnect?.Invoke();
        }

        public void Dispose()
        {
            IsValid = false;
            _pipeServerCancelSource.Cancel();
            
            _childProcess?.Dispose();
            _currentProcess?.Dispose();
            _reader?.Dispose();
            _pipeServer?.Dispose();
            _writer?.Dispose();
            _pipeClient?.Dispose();
        }
    }
}