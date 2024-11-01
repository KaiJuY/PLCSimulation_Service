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
        IMitControlModule MitControlModule;
        public IOContainer()
        {
            //MitControlModule = new MxControlModule("QCPU", "192.168.31.100");
            //MitControlModule = new MxControlModule("SIM", "127.0.0.1");
            MitControlModule = new McControlModule("127.0.0.1", 7500);
        }
        public void Connect() => MitControlModule.Connect();
        public bool IsConnected() => MitControlModule.IsConnected();
        public bool ReadInt(string device, string address, out short value)
        {
            value = 0;
            return MitControlModule.ReadDataFromPLC(device, address, out value);
        }
        public bool ReadListInt(string device, string address, int count, out List<short> values)
        {
            values = new List<short>();
            return MitControlModule.ReadDataFromPLC(device, address, count, out values);
        }
        public bool WriteInt(string device, string address, short value) => MitControlModule.WriteDataToPLC(device, address, value);
        public bool WriteListInt(string device, string address, List<short> values) => MitControlModule.WriteDataToPLC(device, address, values);
        public bool WriteString(string device, string address, string value) => MitControlModule.WriteDataToPLC(device, address, value);
        public bool PrimaryHandShake(string Pdevice, string Paddress, string Sdevice, string Saddress) => MitControlModule.PrimaryHandshake(Pdevice, Paddress, Sdevice, Saddress, 5.0);
        public bool SecondaryHandShake(string Pdevice, string Paddress, string Sdevice, string Saddress) => MitControlModule.SecondaryHandshake(Pdevice, Paddress, Sdevice, Saddress, 5.0);
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
