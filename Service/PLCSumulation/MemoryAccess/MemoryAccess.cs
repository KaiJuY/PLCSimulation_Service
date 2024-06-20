using System;
using System.Collections.Generic;
using System.Text;

namespace PLCSumulation.MemoryAccess
{
    public abstract class  MemoryDevice<T>
    {
        private T[] Value { get; set; }
        private object _lock = new object();
        public MemoryDevice(int size)
        {
            this.Value = new T[size];
        }
        public virtual bool Read(int address, out T val)
        {
            try
            {
                val = default(T);
                if (!CheckAddressValid(address)) return false;
                lock (_lock)
                {
                    val = Value[address];
                }
                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public virtual bool Write(int address, T value)
        {
            try
            {
                if (!CheckAddressValid(address)) return false;
                lock (_lock)
                {
                    Value[address] = value;
                }
                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public virtual bool CheckAddressValid(int address)
        {
            try
            {
                if (address > Value.Length) return false;
            }
            catch (Exception)
            {

                throw;
            }
            return true;
        }
    }
    public abstract class MemoryManager<T>
    {
        private Dictionary<string, MemoryDevice<T>> MemoryStation;
        public MemoryManager() 
        {
            MemoryStation = new Dictionary<string, MemoryDevice<T>>();
            InitMemoryStation();
        }
        public abstract void InitMemoryStation();
        public virtual bool Read(string deviceName, ushort address, out T val)
        {
            try
            {
                val = default(T);
                if (!MemoryStation.ContainsKey(deviceName)) return false;
                return MemoryStation[deviceName].Read(address, out val);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public virtual bool Write(string deviceName, ushort address, T value)
        {
            try
            {
                if (!MemoryStation.ContainsKey(deviceName)) return false;
                return MemoryStation[deviceName].Write(address, value);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
