﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOControlModule;
using IOControlModule.MitControlModule;
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
        public bool WriteInt(string device, string address, short value) => MitControlModule.WriteDataToPLC(device, address, value);
        public bool PrimaryHandShake(string Pdevice, string Paddress, string Sdevice, string Saddress) => MitControlModule.PrimaryHandshake(Pdevice, Paddress, Sdevice, Saddress, 5.0);
    }
}
