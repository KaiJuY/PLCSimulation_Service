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
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace EventDriven.Services
{
    public class EventManager
    {
        private static IOContainer _iOContainer;
        private static TriggerWorkFlowModel _workFlow;
        private Dictionary<string, TriggerBehavior> _registeredEvents;
        private bool _isMonitoring;
        private readonly object _registeredEventsLock = new object(); // 鎖定物件
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
        public IOContainer IOContainer
        {
            get { return _iOContainer; }
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
            foreach (var Cs in _workFlow.CarrierStorage)
            {
                foreach(var triggerAction in Cs.Trigger.TriggerActions)
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
            }
            IsMonitoring = true;
        }
        public void UnregisterEvents()
        {
            IsMonitoring = false;
            lock (_registeredEventsLock)
            {
                _registeredEvents.Clear();
            }
        }
        public void Monitor()
        {
            List<TriggerBehavior> triggers;
            lock (_registeredEventsLock)
            {
                triggers = new List<TriggerBehavior>(_registeredEvents.Values);
            }
            while (IsMonitoring)
            {
                foreach (var trigger in triggers)
                {
                    trigger.CheckAndTrigger(); // 依次檢查並觸發事件
                }
                SpinWait.SpinUntil(() => false, _workFlow.GlobalVariable.Monitor_Interval); // 每秒檢查一次
            }
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
                SpinWait.SpinUntil(() => false, _workFlow.GlobalVariable.Action_Interval);
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
        private void ExecuteAction(Model.Action action)=> ExecuteFactory.GetExeAction(action.ActionName).Execute(action);
        private bool DoCondition(Condition cond)
        {
            try
            {
                if (!StringValidator.SplitAndValidateString(cond.Address, out string DeviceName, out string Address)) return false;
                if (!_iOContainer.ReadInt(DeviceName, Address, out short currentvalue)) return false;
                if (cond.LastValue == currentvalue && cond.Action != "Specific") return false;
                cond.LastValue = currentvalue;
                switch (cond.Action)
                {
                    case "Monitor"://需要到ExceptionValue且與上次不同
                    case "Specific"://特定值
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
        public class ExecuteFactory
        {
            public static IExeAction GetExeAction(string actionName)
            {
                switch (actionName)
                {
                    case "Write":
                        return new ExecuteWrite();
                    case "SecHandShake":
                        return new ExecuteSecHandshake();
                    case "Index":
                        return new ExecuteIndex();
                    case "LoopAction":
                        return new ExecuteLoopAction();
                    default:
                        return new NullExeAction();
                }
            }
        }
        public interface IExeAction
        {
            bool Execute(Model.Action action);
        }
        public class ExecuteWrite: IExeAction
        {
            public bool Execute(Model.Action action)
            {
                List<Int16> value = new List<Int16>(GetContent(action.Inputs.Value));
                if (!StringValidator.SplitAndValidateString(action.Inputs.Address, out string device, out string addr)) throw new Exception("Address Format Error.");
                foreach (var val in value)
                {
                    if (!_iOContainer.WriteInt(device, addr, val)) return false;
                    addr = (Convert.ToInt32(addr, 16) + 1).ToString("X4");
                }
                return true;
                //return _iOContainer.WriteListInt(device, addr, value); //理論上這個效率比較好但不知道為什麼有異常
            }
            /// <summary>
            /// For Action Write
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
                            if (!StringValidator.SplitAndValidateString(input.Address, out string device, out string address)) throw new Exception("Address Format Error.");
                            _iOContainer.ReadListInt(device, address, input.Lens, out vals);
                            break;
                        case "GlobalVariable":
                            //todo
                            break;
                        default:
                            throw new Exception("Type Not Support.");
                    }
                    if (vals == null) throw new Exception("Vals Not Exist.");
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
        }
        public class ExecuteSecHandshake : IExeAction
        {
            public bool Execute(Model.Action action)
            {
                if (action.Inputs.Value.Length != 1) throw new Exception("For Secondary Handshake, Value should be 1.");
                string value = GetContent(action.Inputs.Value);//item1 : Address
                if (!StringValidator.SplitAndValidateString(value, out string Pdevice, out string Paddr)) throw new Exception("Address Format Error."); //相當於Active的
                if (!StringValidator.SplitAndValidateString(action.Inputs.Address, out string Sdevice, out string Saddr)) throw new Exception("Address Format Error."); //相當於Passive的
                return _iOContainer.SecondaryHandShake(Pdevice, Paddr, Sdevice, Saddr);
            }
            /// <summary>
            /// Secondary Handshake only support KeyIn
            /// </summary>
            /// <param name="inputs"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            private string GetContent(InputValue[] inputs)
            {
                string content = string.Empty;
                foreach (InputValue input in inputs)
                {
                    switch (input.Type)
                    {
                        case "KeyIn":
                            content = DecodeContent(input.Content, input.Format);
                            break;
                        default:
                            throw new Exception("Type Not Support.");
                    }
                    if (content == string.Empty) throw new Exception("Vals Not Exist.");
                }
                return content;
            }
            /// <summary>
            /// Secondary Handshake only support string
            /// </summary>
            /// <param name="content"></param>
            /// <param name="format"></param>
            /// <returns></returns>
            /// <exception cref="InvalidOperationException"></exception>
            private string DecodeContent(object content, string format)
            {
                switch (format.ToLower())
                {
                    case "string":
                        return content.ToString();
                    default:
                        throw new InvalidOperationException("Unsupported format: " + format);
                }
            }
        }
        public class ExecuteIndex : IExeAction
        {
            public bool Execute(Model.Action action)
            {
                //無視Value的內容僅做Address++
                if (!StringValidator.SplitAndValidateString(action.Inputs.Address, out string Sdevice, out string Saddr)) throw new Exception("Address Format Error."); //相當於Passive的
                if(!_iOContainer.ReadInt(Sdevice, Saddr, out short value)) throw new Exception("Read Fail.");
                return _iOContainer.WriteInt(Sdevice, Saddr, ++value);
            }
        }        
        public class ExecuteLoopAction : IExeAction
        {
            private List<int> _loopElements; // Store loop elements (bits)
            private int _loopIndex = 0; // Current loop index            

            public bool Execute(Model.Action action)
            {
                if (action.Inputs.Value == null || action.Inputs.Value.Length != 1) return false; // Only support one InputValue for LoopAction
                InputValue inputValue = action.Inputs.Value[0];
                if (!CheckInputValue(action.Inputs.Value[0])) return false;
                // Loop through each element and execute LoopActions if condition is met
                for (_loopIndex = 0; _loopIndex < _loopElements.Count; _loopIndex++)
                {
                    if (CheckLoopCondition(_loopElements[_loopIndex], action.Inputs.ExecuteCondition)) // Check condition for current element
                    {
                        if(!ExecuteLoopActions(action.Inputs.SubActions))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            private bool CheckInputValue(InputValue inputValue)
            {                
                //Only support Read Action for now
                if (inputValue.Type != "Action" || inputValue.ActionName != "Read")
                {
                    throw new Exception("LoopAction Input Value should be an Action of type Read.");
                }

                if (!StringValidator.SplitAndValidateString(inputValue.Address, out string device, out string address))
                {
                    throw new Exception("LoopAction Read Address Format Error.");
                }

                int lens = Convert.ToInt32(inputValue.Lens);
                if (lens <= 0)
                {
                    throw new Exception("LoopAction Read Lens should be greater than 0.");
                }

                if (!_iOContainer.ReadListInt(device, address, lens, out List<short> ReadValue))
                {
                    throw new Exception("LoopAction Read PLC data failed.");
                }
                switch(inputValue.ElementUnit)
                {
                    case "Bit":
                        _loopElements = ConvertWordsToBitList(ReadValue);
                        break;
                    default:
                        throw new Exception("LoopAction Read ElementUnit Not Support.");
                }                
                return true;
            }
            private bool CheckLoopCondition(int elementValue, ExecuteCondition condition)
            {
                if (condition == null) return true; // If no condition, always execute

                switch (condition.Type)
                {
                    case "Equals":
                        if (condition.Format == "Int")
                        {
                            return elementValue == Convert.ToInt32(condition.Content);
                        }
                        break;
                    // Add more condition types if needed (e.g., "NotEquals", "GreaterThan", etc.)
                    default:
                        throw new Exception($"Unsupported ExecuteCondition Type: {condition.Type}");
                }
                return false;
            }


            private bool ExecuteLoopActions(List<Model.Action> loopActions)
            {
                if (loopActions == null) return false;

                foreach (var loopAction in loopActions)
                {
                    if(!ExecuteActionWithLoopContext(loopAction))
                    {
                        return false;
                    }
                    SpinWait.SpinUntil(() => false, _workFlow.GlobalVariable.Action_Interval); // Action Interval
                }
                return true;
            }            
            private bool ExecuteActionWithLoopContext(Model.Action action)
            {
                Model.Action LocalAction = JsonSerializer.Deserialize<Model.Action>(JsonSerializer.Serialize(action));
                //We don't want to modify the original action
                foreach (InputValue inputValue in LocalAction.Inputs.Value)
                {
                    if(!ConvertLoopFormatToSTD(inputValue)) throw new Exception("Convert Loop Format Error.");
                }
                return ExecuteFactory.GetExeAction(LocalAction.ActionName).Execute(LocalAction); // Execute the action (using existing ExecuteAction logic)
            }
            private bool ConvertLoopFormatToSTD(InputValue inputValue)
            {
                if (inputValue.Type == "GlobalVariable")
                    return GlobbalTypeConvert(inputValue);
                if (inputValue.Type == "Action" && inputValue.ActionName == "Read")
                    return ReadTypeConvert(inputValue);
                return false;
            }
            private bool GlobbalTypeConvert(InputValue inputValue)
            {
                inputValue.Content = ReplaceBindingMaterialToContent(inputValue.Content.ToString());
                inputValue.Content = ReplaceIndexToContent(inputValue.Content.ToString());
                //Final Format should be like "Materials.CassettleFormat.BindingMaterial.WaferList.Index.WaferId"
                return true;
            }
            private bool ReadTypeConvert(InputValue inputValue)
            {
                inputValue.Address = (Convert.ToInt32(inputValue.Address, 16) + _loopIndex * inputValue.Lens).ToString("X4");
                return true;
            }
            private string ReplaceIndexToContent(string content) => content.Replace("Index", _loopIndex.ToString());
            private string ReplaceBindingMaterialToContent(string content)
            {
                string[] cArray = content.Split('.');
                if (cArray.Length == 0) return content;
                string Prefix = cArray[0] + ".";
                foreach (CarrierStorage cS in _workFlow.CarrierStorage)
                {
                    if(cS.Name == cArray[0])
                    {
                        content.Replace("BindingMaterial", cS.Material.BindingMaterial);
                        break;
                    }
                }
                return content.StartsWith(Prefix) ? content.Substring(Prefix.Length) : content;
            }
            private List<int> ConvertWordsToBitList(List<short> wordValues)
            {
                List<int> bitList = new List<int>();
                foreach (short word in wordValues)
                {
                    for (int i = 0; i < 16; i++) // short is 16 bits
                    {
                        bitList.Add(((word >> i) & 1) == 1 ? 1 : 0); // Extract each bit
                    }
                }
                return bitList;
            }
        }
        public class NullExeAction : IExeAction
        {
            public bool Execute(Model.Action action)
            {
                throw new Exception("Action Not Support.");
            }
        }
        public class WorkTransfering : AInputValue
        {
            public WorkTransfering(Model.Action action) : base(action)
            {
            }
            public override List<Tuple<ushort, object>> GetOffetContent(object content)
            {
                throw new NotImplementedException();
            }
        }
        public class VCRRead : AInputValue
        {
            public VCRRead(Model.Action action) : base(action)
            {
            }
            public override List<Tuple<ushort, object>> GetOffetContent(object content)
            {
                throw new NotImplementedException();
            }
        }
        public class CassetteState : AInputValue
        {
            public CassetteState(Model.Action action) : base(action)
            {
            }
            public override List<Tuple<ushort, object>> GetOffetContent(object content)
            {
                throw new NotImplementedException();
            }
        }
    }
    public interface IActionInput
    {        
        /// <summary>
        /// Return the offsetaddress and object value
        /// expection value is short and List<short>
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        List<Tuple<ushort, object>> GetOffetContent(object content);
    }
    public abstract class AInputValue : IActionInput
    {
        public AInputValue(Model.Action action)
        {
            if (!StringValidator.SplitAndValidateString(action.Inputs.Address, out string BaseDevice, out string BaseAddress)) throw new Exception("Address Format Error."); //相當於Passive的
        }
        public string BaseDevice { get; set; }
        public string BaseAddress { get; set; }
        public abstract List<Tuple<ushort, object>> GetOffetContent(object content);
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
}
