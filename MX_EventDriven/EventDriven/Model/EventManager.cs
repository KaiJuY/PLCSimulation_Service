using System;
using System.Collections.Generic;
using EventDriven.Model;
using System.Text.Json;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Net;
using IoModule.MitControlModule;
using System.Linq;

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
        public string TestRead()
        {
            if (!_iOContainer.ReadListInt("W", "3000", 5, out List<short> value)) return "Read Fail.";

            string result = string.Empty;
            foreach (var val in value)
            {
                result += val.ToString() + ", ";
            }
            return result;
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
        /// <summary>
        ///基礎 Write已實現會以Adress與Value寫入PLC
        ///其中Value會有多種輸入方式KeyIn, Action
        ///KeyIn會直接將Content帶入寫入的值
        ///Action目前只支援Read會將Address的值讀取出來並且依造指定的Lens讀取
        ///"Inputs": {
        ///		"Address": "W3000",
        ///		"Value": [
        ///			{
        ///				"Type": "KeyIn",
        ///				"Format": "Int",
        ///				"Content": 1
        ///          },
        ///			{
        ///				"Type": "KeyIn",
        ///				"Format": "String",
        ///				"Content": "MYGOD"
        ///			},
        ///          {
        ///	            "Type": "Action",
        ///	            "ActionName": "Read",
        ///              "Address": "W1024",
        ///              "Lens": 1
        ///          }
        ///		    ]
        ///},
        ///"Output": { }
        ///},
        ///以下為未實現的部分
        /// 針對KF的Case需要新增
        /// LD的出料邏輯Action
        /// ULD的入料邏輯Action
        /// LD-ULD的出入料邏輯Action
        /// 針對需要控制ROBOT的Case需要新增
        /// ROBOT的動作Action
        /// </summary>
        /// <param name="action"></param>
        /// <exception cref="Exception"></exception>
        private void ExecuteAction(Model.Action action)
        {
            if(action.ActionName != "Write") return;
            List<short> value = GetContent(action.Inputs.Value);
            if (!StringValidator.SplitAndValidateString(action.Inputs.Address, out string device, out string addr)) throw new Exception("Address Format Error.");
            _iOContainer.WriteListInt(device, addr, value);
        }
        /// <summary>
        /// 直接取得可以輸入的數據集合並轉換成List<short>方便寫入PLC
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private List<short> GetContent(InputValue[] inputs)
        {            
            List<short> content = new List<short>();
            foreach (InputValue input in inputs)
            {
                List<short> vals = null;
                switch (input.Type)
                {
                    case "KeyIn":
                        vals = DecodeContent(input.Content, input.Format);
                        break;
                    case "Action":
                        if (input.ActionName != "Read") throw new Exception("Action Not Support.");
                        if(! StringValidator.SplitAndValidateString(input.Address, out string device, out string address)) throw new Exception("Address Format Error.");
                        _iOContainer.ReadListInt(device, address, input.Lens, out vals);
                        break;
                    default:
                        throw new Exception("Type Not Support.");
                }
                if(vals == null) throw new Exception("Vals Not Exist.");
                content.AddRange(vals);
                //直接將數值轉換成指定格式
            }
            if (!content.Any()) throw new Exception("Content Not Exist.");
            return content;
        }
        private List<short> DecodeContent(object content, string format)
        {
            switch (format.ToLower())
            {
                case "string":
                    return MitUtility.getInstance().StringToASCII(content.ToString());
                case "int":
                    return new List<short>() { Convert.ToInt16(content.ToString()) };
                default:
                    throw new InvalidOperationException("Unsupported format: " + format);
            }
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

