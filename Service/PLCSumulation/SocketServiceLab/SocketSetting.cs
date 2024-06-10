using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PLCSumulation.SocketServiceLab
{
    public enum eProtocolType
    {
        Tcp,
        Udp
    }

    public interface ISocketSetting
    {
        IPAddress IP { get; set; }
        int Port { get; set; }
        eProtocolType ProtocolType { get; set; }
        int BacklogSize { get; set; }
        bool ReuseAddress { get; set; }
    }
    public class SocketSetting : ISocketSetting
    {
        public IPAddress IP { get; set; }
        public int Port { get; set; }
        public eProtocolType ProtocolType { get; set; }
        public int BacklogSize { get; set; }
        public bool ReuseAddress { get; set; }

        public SocketSetting(string iPAddress, int port, string protocolType, int backlogSize = 100, bool reuseAddress = true)
        {
            this.IP = ConvertToIPAddress(iPAddress);
            this.ProtocolType = ConvertToProtocolType(protocolType);
            this.Port = port;
            this.BacklogSize = backlogSize;
            this.ReuseAddress = reuseAddress;
        }
        private IPAddress ConvertToIPAddress(string iPAddress)
        {
            try
            {
                if (IPAddress.TryParse(iPAddress, out IPAddress iP))
                {
                    return iP;
                }
                else
                {
                    throw new ArgumentException("Invalid IP Address");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        private eProtocolType ConvertToProtocolType(string protocolType)
        {
            try
            {
                if (Enum.TryParse(protocolType, out eProtocolType pT))
                {
                    return pT;
                }
                else
                {
                    throw new ArgumentException("Invalid Protocol Type");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
