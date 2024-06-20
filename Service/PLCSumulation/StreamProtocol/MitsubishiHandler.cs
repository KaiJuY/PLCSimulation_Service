using PLCSumulation.SocketServiceLab;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PLCSumulation.StreamProtocol
{
    public class MitsubishiHandler : ProtocolHandler
    {
        MitsubishiProtocolType MitProtocolType;
        public MitsubishiHandler(ISocketSetting socketSetting, MitsubishiProtocolType type) : base(socketSetting)
        {
           this.MitProtocolType = type;
        }

        protected override bool IsValidMessage(byte[] data)
        {
            try
            {
                IMitsubishiValidator validator = ValidatorFactory.GetValidator(MitProtocolType);
                return validator.IsLengthValid(data) && validator.IsMessageFormatValid(data);
            }
            catch (Exception)
            {

                throw;
            }
            return false;
        }

        protected override ParsedData ParseMessage(byte[] data)
        {
            // 解析 MyProtocol 訊息
            int messageType = BitConverter.ToInt32(data, 4);
            byte[] payload = new byte[data.Length - 8];
            Array.Copy(data, 8, payload, 0, payload.Length);

            return new ParsedData { MessageType = messageType, Payload = payload };
        }

        protected override void ProcessParsedData(ParsedData parsedData, EndPoint remoteEndPoint)
        {
            // 處理解析後的資料
            Console.WriteLine($"Received message type {parsedData.MessageType} from {remoteEndPoint}");
            // 進一步處理 parsedData.Payload
            SocketServer.Send(Encoding.ASCII.GetBytes("Recived From MitsubishiHandler"), remoteEndPoint);
        }
    }
    public enum MitsubishiProtocolType
    {
        McProtocol = 0,
        MxConponent = 1
    }
    public class ValidatorFactory
    {
        public static IMitsubishiValidator GetValidator(MitsubishiProtocolType mitsubishiProtocolType)
        {
            try
            {
                if ((mitsubishiProtocolType == MitsubishiProtocolType.MxConponent))
                    return new MxValidator();
            }
            catch (Exception)
            {

                throw;
            }
            return new NullValidator();
        }
    }
    public interface IMitsubishiValidator
    {
        byte[] CheckConnectedFormat { get; }
        byte[] DataFormat { get; }
        bool IsLengthValid(byte[] data);
        bool IsMessageFormatValid(byte[] data);

    }
    public class MxValidator : IMitsubishiValidator
    {
        public byte[] CheckConnectedFormat { get; }
        public byte[] DataFormat { get; }
        private byte OrderLower = 0x0A;
        private byte OrderUpper = 0x9F;
        public MxValidator()
        {          
            CheckConnectedFormat = new byte[] { 0x5A, 0x00, 0x00, 0xFF };
            DataFormat = new byte[] { 0x57, 0x00, 0x00, 0x00, 0x00, 0x11, 0x11, 0x07, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x00, 0xFE, 0x03, 0x00, 0x00, 0x1A };
        }
        public bool IsLengthValid(byte[] data)
        {
            return data.Length == 4 || data.Length >= 47;
        }
        public bool IsMessageFormatValid(byte[] data)
        {
            if(data.Length == 4)
            {
                for(int i = 0; i < CheckConnectedFormat.Length; i++)
                {
                    if (data[i] != CheckConnectedFormat[i]) return false;
                }
                return true;
            }
            else
            {
                for(int i = 0; i < DataFormat.Length; i++)
                {
                    switch(i)
                    {
                        case 3:
                            if (data[i] < OrderLower || data[i] > OrderUpper) return false;
                            break;
                        case 20:
                            break;
                        default:
                            if (data[i] != DataFormat[i]) return false;
                            break;
                    }
                }
                return true;
            }
        }
    }
    public class NullValidator : IMitsubishiValidator
    {
        public byte[] CheckConnectedFormat { get; }
        public byte[] DataFormat { get; }
        public bool IsLengthValid(byte[] data)
        {
            return false;
        }
        public bool IsMessageFormatValid(byte[] data)
        {
            return false;
        }
    }
    public interface IMitsubishiParser
    {
        void 
    }
    public enum eMitsubishiMessageType
    {
        ConnectCheck = 0,
        ReadBlock,
        WriteBlock,
        ReadDevice,
        WriteDevice,
    }
}
