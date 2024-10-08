using System;
using System.Collections.Generic;
using EventDriven.Model;
using System.Text.Json;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Net;

namespace EventDriven.Services
{
    public class EventManager
    {
        private IOContainer _iOContainer;
        private TriggerWorkFlowModel _workFlow;
        private Dictionary<string, TriggerBehavior> _registeredEvents;
        private bool _isMonitoring;
        public bool IsMonitoring
        {
            get { return _isMonitoring; }
            set { _isMonitoring = value; }
        }
        private string jsonFileName;
        public string JsonFileName
        {
            get { return jsonFileName; }
            set { jsonFileName = value; }
        }
        public EventManager()
        {
            _registeredEvents = new Dictionary<string, TriggerBehavior>();
            _iOContainer = new IOContainer();
        }
        public bool LinkToPLC()
        {
            try
            {
                if(!_iOContainer.IsConnected())
                    _iOContainer.Connect();
                return _iOContainer.IsConnected();
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool LoadWorkFlow()
        {
            try
            {
                string json = File.ReadAllText(JsonFileName);
                _workFlow = JsonSerializer.Deserialize<TriggerWorkFlowModel>(json);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void RegisterEvents()
        {
            foreach (var triggerAction in _workFlow.Trigger.TriggerActions)
            {
                _registeredEvents[triggerAction.Name] = new TriggerBehavior
                {
                    Name = triggerAction.Name,
                    Condition = () => CheckCondition(triggerAction),
                    Action = () => ExecuteActions(triggerAction.Actions)
                };
                _registeredEvents[triggerAction.Name].Triggered += (sender, args) =>
                {
                    _registeredEvents[triggerAction.Name].Action.Invoke();
                };
            }
            IsMonitoring = true;
        }
        public void UnregisterEvents()
        {
            IsMonitoring = false;
            _registeredEvents.Clear();
        }
        public void Monitor()
        {
            while(IsMonitoring)
            {
                foreach (var trigger in _registeredEvents)
                {
                    trigger.Value.CheckAndTrigger(); // 依次檢查並觸發事件
                }
                SpinWait.SpinUntil(() => false, _workFlow.Trigger.Interval); // 每秒檢查一次
            }
        }
        public bool Test()
        {
            return _iOContainer.PrimaryHandShake("W", "1000", "W", "2000");
        }
        public void TestReset()
        {
            _iOContainer.WriteInt("W", "1000", 0);
        }
        /// <summary>
        /// Type可以改為策略模式實作目前這邊先簡單寫就好
        /// </summary>
        /// <param name="triggerAction"></param>
        /// <returns></returns>
        private bool CheckCondition(Model.TriggerAction triggerAction)
        {
            bool result = false;

            if (triggerAction.TriggerPoint.Type == "OR")
            {
                foreach (var triggerPoint in triggerAction.TriggerPoint.Conditions)
                {
                    if (DoCondition(triggerPoint)) return true;
                }
            }
            else if (triggerAction.TriggerPoint.Type == "AND")
            {
                foreach (var triggerPoint in triggerAction.TriggerPoint.Conditions)
                {
                    if (!DoCondition(triggerPoint)) return false;
                }
                return true;
            }
            return result;
        }
        private void ExecuteActions(List<Model.Action> actions)
        {
            foreach (var action in actions)
            {
                ExecuteAction(action);
                SpinWait.SpinUntil(() => false, 500);
            }
        }
        private void ExecuteAction(Model.Action action)
        {
            if(action.ActionName != "Write") return;
            var value = GetContent(action.Inputs.Value);
            var address = GetContent(action.Inputs.Address);
            if (!StringValidator.SplitAndValidateString(address.ToString(), out string device, out string addr)) throw new Exception("Address Format Error.");
            _iOContainer.WriteInt(device, addr, Convert.ToInt16(value));
        }
        private object GetContent(InputValue input)
        {
            object val = null;
            switch (input.Type)
            {
                case "KeyIn":
                    val = input.Content;
                    break;
                case "Action":
                    if(!input.Content.TryGetProperty("ActionName", out JsonElement actionName)) throw new Exception("Action Not Exist.");
                    if(!input.Content.TryGetProperty("Address", out JsonElement Jaddress)) throw new Exception("Address Not Exist.");
                    if (actionName.GetString() != "Read") throw new Exception("Action Not Support.");
                    if(! StringValidator.SplitAndValidateString(Jaddress.GetString(), out string device, out string address)) throw new Exception("Address Format Error.");
                    _iOContainer.ReadInt(device, address, out short value);
                    val = value;
                    break;
                default:
                    throw new Exception("Type Not Support.");
            }
            if(val == null) throw new Exception("Val Not Exist.");
            //直接將數值轉換成指定格式
            switch (input.Format.ToLower())
            {
                case "string":
                    return val.ToString();
                case "int":
                    if (int.TryParse(val.ToString(), out int intValue))
                    {
                        return intValue;
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unsupported format: " + input.Format);
            }
            throw new Exception("Val Not Exist.");
        }
        private bool DoCondition(Condition cond)
        {
            try
            {
                if (!StringValidator.SplitAndValidateString(cond.Address, out string DeviceName, out string Address)) return false;
                if(!_iOContainer.ReadInt(DeviceName, Address, out short currentvalue)) return false;
                if(cond.LastValue == currentvalue) return false;
                cond.LastValue = currentvalue;
                switch (cond.Action)
                {
                    case "Monitor"://需要到ExceptionValue且與上次不同
                        if (cond.ExceptedValue != currentvalue) return false;
                        break;
                    case "Change"://與上次不同
                        break;
                    default:
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }        
    }
}
public class TriggerBehavior
{
    public string Name { get; set; }
    public Func<bool> Condition { get; set; }
    public System.Action Action { get; set; }
    public event EventHandler Triggered;

    public void CheckAndTrigger()
    {
        if (Condition.Invoke()) // 如果條件滿足
        {
            OnTriggered(EventArgs.Empty); // 觸發事件
        }
    }

    protected virtual void OnTriggered(EventArgs e)
    {
        Triggered?.Invoke(this, e); // 使用事件觸發對應行為
    }
}

