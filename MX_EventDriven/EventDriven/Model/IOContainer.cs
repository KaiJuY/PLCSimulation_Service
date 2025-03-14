﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOControlModule;
using IOControlModule.MitControlModule;
using System.Text.RegularExpressions;
namespace EventDriven.Model
{
    public  class IOContainer
    {
        private IMitControlModule _mitControlModule;
        public IOContainer()
        {
            //_mitControlModule = new MxControlModule("QCPU", "192.168.31.100");
            //MitControlModule = new MxControlModule("SIM", "127.0.0.1");
            //MitControlModule = new McControlModule("127.0.0.1", 7500);
        }

        public void CreateMxControlModule(string cpuType, string ipAddress)
        {
            _mitControlModule = new MxControlModule(cpuType, ipAddress);
        }

        public void CreateMcControlModule(string ipAddress, int port)
        {
            _mitControlModule = new McControlModule(ipAddress, port);
        }

        public void Connect() => _mitControlModule.Connect();
        public bool IsConnected() => _mitControlModule.IsConnected();
        public bool ReadInt(string device, string address, out short value)
        {
            value = 0;
            return _mitControlModule.ReadDataFromPLC(device, address, out value);
        }
        public bool ReadListInt(string device, string address, int count, out List<short> values)
        {
            values = new List<short>();
            return _mitControlModule.ReadDataFromPLC(device, address, count, out values);
        }
        public bool WriteInt(string device, string address, short value) => _mitControlModule.WriteDataToPLC(device, address, value);
        public bool WriteListInt(string device, string address, List<short> values) => _mitControlModule.WriteDataToPLC(device, address, values);
        public bool WriteListInt(List<string> device, List<string> address, List<short> values) => _mitControlModule.WriteDataToPLC(device, address, values);
        public bool WriteString(string device, string address, string value) => _mitControlModule.WriteDataToPLC(device, address, value);
        public bool PrimaryHandShake(string Pdevice, string Paddress, string Sdevice, string Saddress) => _mitControlModule.PrimaryHandshake(Pdevice, Paddress, Sdevice, Saddress, 5.0);
        public bool SecondaryHandShake(string Pdevice, string Paddress, string Sdevice, string Saddress) => _mitControlModule.SecondaryHandshake(Pdevice, Paddress, Sdevice, Saddress, 5.0);
    }
}
public class StringValidator
{
    public static bool SplitAndValidateString(string input, out string Device, out string Address)
    {
        Device = Address = string.Empty;

        if (string.IsNullOrEmpty(input) || input.Length < 2 || input.Length > 6)
            return false;

        // 嘗試分割字符串
        int oLength = input.Length >= 2 && char.IsLetter(input[1]) && !IsHexChar(input[1]) ? 2 : 1;
        Device = input.Substring(0, oLength);
        Address = input.Substring(oLength);

        // 驗證 O 部分
        if (!Regex.IsMatch(Device, @"^[G-Z]{1,2}$"))
            return false;

        // 驗證 X 部分
        if (!Regex.IsMatch(Address, @"^[0-9A-F]{1,4}$"))
            return false;

        return true;
    }

    private static bool IsHexChar(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F');
    }
}
