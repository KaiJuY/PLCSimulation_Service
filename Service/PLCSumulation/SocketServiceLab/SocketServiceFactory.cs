using System;
using System.Collections.Generic;
using System.Text;

namespace PLCSumulation.SocketServiceLab
{
    public class SocketServiceFactory
    {
        public static ISocketService CreateSocketService(ISocketSetting socketSetting, EventHandler<SocketDataEventArgs> eventHandler)
        {
            return socketSetting.ProtocolType switch
            {
                eProtocolType.Tcp => new TcpSocketServer(socketSetting, eventHandler),
                eProtocolType.Udp => new UdpSocketServer(socketSetting, eventHandler),
                _ => throw new ArgumentException("Invalid Protocol Type"),
            };
        }
    }
}
