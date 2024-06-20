using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PLCSumulation.SocketServiceLab
{
    public interface ISocketService
    {
        event EventHandler<SocketDataEventArgs> DataReceived;
        void Send(byte[] data, EndPoint endPoint);
        void Start();
        Task StopAsync();
    }
    public class SocketDataEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
    }
    public class TcpSocketServer : ISocketService
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private ISocketSetting SocketSetting;
        private readonly ConcurrentDictionary<EndPoint, TcpClient> _clients;
        private readonly ConcurrentDictionary<TcpClient, object> _locks;
        public event EventHandler<SocketDataEventArgs> DataReceived;

        public TcpSocketServer(ISocketSetting socketSetting, EventHandler<SocketDataEventArgs> eventHandler)
        {
            this._clients = new ConcurrentDictionary<EndPoint, TcpClient>();
            this._locks = new ConcurrentDictionary<TcpClient, object>();
            this.SocketSetting = socketSetting;
            this.DataReceived += eventHandler;
        }
        public void Start()
        {
            _cts = new CancellationTokenSource();

            try
            {
                _listener = new TcpListener(SocketSetting.IP, SocketSetting.Port);
                _listener.Start(SocketSetting.BacklogSize);
                _listener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to start TCP server: {ex.Message}");
            }
        }
        public void Send(byte[] msg, EndPoint endPoint)
        {
            if (_clients.TryGetValue(endPoint, out TcpClient tcpClient))
            {
                var lockObj = _locks.GetOrAdd(tcpClient, new object());
                lock (lockObj)
                {
                    tcpClient.Client.Send(msg);
                }
            }
        }

        private void AcceptTcpClientCallback(IAsyncResult ar)
        {
            if (_listener == null) return;

            try
            {
                TcpClient client = _listener.EndAcceptTcpClient(ar);
                _clients.TryAdd(client.Client.RemoteEndPoint, client);
                ProcessTcpClient(client);
            }
            catch (ObjectDisposedException)
            {
                // Listener is already disposed, do nothing
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to accept TCP client: {ex.Message}");
            }
        }

        private async void ProcessTcpClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    //已經是await不需要再加上lock
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (bytesRead == 0) break;

                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, 0, data, 0, bytesRead);

                    DataReceived?.Invoke(this, new SocketDataEventArgs
                    {
                        Data = data,
                        RemoteEndPoint = client.Client.RemoteEndPoint
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Server is stopping, do nothing
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing TCP client: {ex.Message}");
            }
            finally
            {
                client.Close();
                _clients.TryRemove(client.Client.RemoteEndPoint, out TcpClient tcpClient);
            }
        }

        public async Task StopAsync()
        {
            _cts?.Cancel();

            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Failed to stop TCP server: {ex.Message}");
                }
            }

            await Task.Delay(100); // 等待所有連線關閉
        }
    }
    public class UdpSocketServer : ISocketService
    {
        private UdpClient _udpClient;
        private CancellationTokenSource _cts;
        private ISocketSetting SocketSetting;
        private bool ListenSpecialIP;
        public event EventHandler<SocketDataEventArgs> DataReceived;

        public UdpSocketServer(ISocketSetting socketSetting, EventHandler<SocketDataEventArgs> eventHandler, bool listenSpecialIP = false)
        {
            this.SocketSetting = socketSetting;
            this.DataReceived += eventHandler;
            this.ListenSpecialIP = listenSpecialIP;
        }
        public void Start()
        {
            _cts = new CancellationTokenSource();

            try
            {
                _udpClient = this.ListenSpecialIP ? new UdpClient(new IPEndPoint(SocketSetting.IP, SocketSetting.Port)) : new UdpClient(SocketSetting.Port); // 若 listenSpecialIP 為 true，則監聽特定 IP
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, SocketSetting.ReuseAddress);
                ReceiveAsync(_cts.Token);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to start UDP server: {ex.Message}");
            }
        }
        public void Send(byte[] data, EndPoint endPoint)
        {
            _udpClient.Send(data, data.Length, (endPoint as IPEndPoint));
        }

        private async void ReceiveAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync();
                    DataReceived?.Invoke(this, new SocketDataEventArgs
                    {
                        Data = result.Buffer,
                        RemoteEndPoint = result.RemoteEndPoint
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Server is stopping, do nothing
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving UDP data: {ex.Message}");
            }
        }

        public Task StopAsync()
        {
            _cts?.Cancel();

            if (_udpClient != null)
            {
                _udpClient.Close();
            }

            return Task.CompletedTask;
        }
    }
}
