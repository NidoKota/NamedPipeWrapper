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
        public async Task OnDisposeTask() => await _onDisposeTaskSource.Task;

        private StreamReader _reader;
        private NamedPipeServerStream _pipeServer;
        private readonly TaskCompletionSource<int> _onDisposeTaskSource = new TaskCompletionSource<int>();

        private async Task ReadServerStart(string pipeName, Process process)
        {
            _pipeServer = new NamedPipeServerStream($"{pipeName}{process.Id}");

            await _pipeServer.WaitForConnectionAsync();

            _reader = new StreamReader(_pipeServer);

            _ = Monitoring()
                .ToString(); // Forget

            async Task Monitoring()
            {
                while (_pipeServer.IsConnected)
                {
                    var message = await _reader.ReadLineAsync();
                    if (_pipeServer.IsConnected) OnRead?.Invoke(message);
                }
                
                _onDisposeTaskSource.SetResult(0);
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
            catch (TimeoutException ex)
            {
                Utility.DebugLog($"TimeOut: {ex.Message}");
                Dispose();
            }
        }

        public async Task Write(string message)
        {
            await _writer.WriteLineAsync(message);
            await _writer.FlushAsync();
            Utility.DebugLog($"Send: {message} {DateTime.Now}");
        }

        public void Dispose()
        {
            _childProcess?.Dispose();
            _currentProcess?.Dispose();
            _reader?.Dispose();
            _pipeServer?.Dispose();
            _writer?.Dispose();
            _pipeClient?.Dispose();
        }
    }
}