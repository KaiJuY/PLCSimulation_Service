﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOControlModule;
using IOControlModule.MitControlModule;
using System.Text.RegularExpressions;
using System.ComponentModel;
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
        public bool ReadListInt(string basedevice, string baseaddress, int count, out List<Int16> values)
        {
            values = new List<Int16>();

            for (int i = 0; i < count; i++)
            {
                if (!ReadInt(basedevice, baseaddress, out short value)) return false;
                values.Add(value);
                baseaddress = (Convert.ToInt32(baseaddress, 16) + 1).ToString("X4");
            }
            return true;
        }
        public bool ReadString(string device, string address, int lens, out string value)
        {
            string _value = string.Empty;
            ReadListInt(device, address, lens, out List<Int16> values);
            for (int j = 0; j < lens; j++)
            {
                string text = values[j].ToString("X4");
                text = text.Substring(2, 2) + text.Substring(0, 2);
                if (text.Contains("00"))
                {
                    text = text.Replace("00", "");
                }
                _value += text;
            }
            byte[] bytes = (from i in Enumerable.Range(0, _value.Length / 2)
                            select Convert.ToByte(_value.Substring(i * 2, 2), 16)).ToArray();
            value = Encoding.ASCII.GetString(bytes);
            return true;
        }
        /* Broken function
        public bool ReadString(string device, string address, int lens, out string value)
        {            
            _mitControlModule.ReadDataFromPLC(device, address, lens, out value);
            return true;
        }
        public bool ReadListInt(string device, string address, int count, out List<short> values)
        {
            values = new List<short>();

            return _mitControlModule.ReadDataFromPLC(device, address, count, out values);
        }
        */
        public bool WriteInt(string device, string address, short value) => _mitControlModule.WriteDataToPLC(device, address, value);
        public bool WriteListInt(string basedevice, string baseaddress, List<Int16> values)
        {
            foreach (var val in values)
            {
                if (!WriteInt(basedevice, baseaddress, val)) return false;
                baseaddress = (Convert.ToInt32(baseaddress, 16) + 1).ToString("X4");
            }
            return true;
        }
        /* Broken function
        public bool WriteListInt(string device, string address, List<short> values) => _mitControlModule.WriteDataToPLC(device, address, values);
        public bool WriteListInt(List<string> device, List<string> address, List<short> values) => _mitControlModule.WriteDataToPLC(device, address, values);
        */
        public bool WriteString(string device, string address, string value) => _mitControlModule.WriteDataToPLC(device, address, value);
        public bool PrimaryHandShake(string Pdevice, string Paddress, string Sdevice, string Saddress, int timeout) => _mitControlModule.PrimaryHandshake(Pdevice, Paddress, Sdevice, Saddress, timeout);
        public bool SecondaryHandShake(string Pdevice, string Paddress, string Sdevice, string Saddress, int timeout) => _mitControlModule.SecondaryHandshake(Pdevice, Paddress, Sdevice, Saddress, timeout);
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
