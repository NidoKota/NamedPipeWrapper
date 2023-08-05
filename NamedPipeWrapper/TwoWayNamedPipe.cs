using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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

            Connect();
        }

        private async void Connect()
        {
            await FirstConnect();
            await SecondConnect();
        }

        private async Task FirstConnect()
        {
            if (_childProcess == null)
            {
                await ReadServerStart("TwoWayNamedPipeFirst");
            }
            else
            {
                await WriteClientStart("TwoWayNamedPipeFirst");
            }
        }

        private async Task SecondConnect()
        {
            if (_childProcess == null)
            {
                await WriteClientStart("TwoWayNamedPipeSecond");
            }
            else
            {
                await ReadServerStart("TwoWayNamedPipeSecond");
            }
        }

        public event Action<string> OnRead;

        private StreamReader _reader;
        private NamedPipeServerStream _pipeServer;

        private async Task ReadServerStart(string pipeName)
        {
            _pipeServer = new NamedPipeServerStream($"{pipeName}{_childProcess.Id}");

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
            }
        }

        private StreamWriter _writer;
        private NamedPipeClientStream _pipeClient;

        private async Task WriteClientStart(string pipeName)
        {
            _pipeClient = new NamedPipeClientStream($"{pipeName}{_childProcess.Id}");
            try
            {
                await _pipeClient.ConnectAsync(5000);
                _writer = new StreamWriter(_pipeClient);
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"TimeOut: {ex.Message}");
                Dispose();
            }
        }

        public async Task Write(string message)
        {
            await _writer.WriteLineAsync(message);
            await _writer.FlushAsync();
            Console.WriteLine($"Send: {message} {DateTime.Now}");
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