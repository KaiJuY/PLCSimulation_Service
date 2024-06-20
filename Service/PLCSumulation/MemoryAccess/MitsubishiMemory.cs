using System;
using System.Collections.Generic;
using System.Text;

namespace PLCSumulation.MemoryAccess
{
    public class MitsubishiMemory : MemoryManager<ushort>
    {
        public override void InitMemoryStation()
        {
            //從Config讀檔案初始化
        }
    }
    public class MitsubishiDeviceConvert
    {
        private Dictionary<byte, string> CovertMap;
        public MitsubishiDeviceConvert()
        {
            CovertMap = new Dictionary<byte, string>()
            {
                {0xA8, "D"},
                {0xA0, "B"},
                {0xB0, "ZR"},
                {0xC4, "CS"},
                {0xC3, "CC"},
                {0xC5, "CN"},
                {0xA2, "DX"},
                {0xA3, "DY"},
                {0x93, "F"},
                {0x92, "L"},
                {0x90, "M"},
                {0xAF, "R"},
                {0xA1, "SB"},
                {0xA9, "SD"},
                {0x91, "SM"},
                {0xB5, "SW"},
                {0xC7, "STS"},
                {0xC6, "STC"},
                {0xC8, "STN"},
                {0xC1, "TS"},
                {0xC0, "TC"},
                {0xC2, "TN"},
                {0x94, "V"},
                {0xB4, "W"},
                {0x9C, "X"},
                {0x9D, "Y"},
                {0xCC, "Z" }
            };
        }
        public bool GetDeviceAddress(byte[] DeviceAddress, out string DeviceName, out ushort Address)
        {
            DeviceName = string.Empty;
            Address = 0;
            /*
             * DeviceAddress[0] is Device Type need to convert to Device Name
             * DeviceAddress[1] is Addtess Type??目前沒有使用似乎00是HEX並沒有轉換， b0是DEC轉換成HEX
             * DeviceAddress[2] is High Byte
             * DeviceAddress[3] is Low Byte
             * High Byte + Low Byte ex. 1021 =>  21 10
             */
            try
            {
                if (DeviceAddress.Length < 4) return false;
                if (!CovertMap.ContainsKey(DeviceAddress[0])) return false;
                DeviceName = CovertMap[DeviceAddress[0]];
                Address = (ushort)((DeviceAddress[3] << 8) | DeviceAddress[2]);
                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
