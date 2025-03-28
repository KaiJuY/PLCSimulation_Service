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
using System.Windows.Documents;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EventDriven.Services
{
    public class EventManager : INotifyPropertyChanged
    {
        private static IOContainer _iOContainer;
        private static TriggerWorkFlowModel _workFlow;
        private Dictionary<string, TriggerBehavior> _registeredEvents;
        private bool _isMonitoring;
        private string _lastTriggeredActionName;
        private readonly object _registeredEventsLock = new object(); // 鎖定物件
        public event PropertyChangedEventHandler PropertyChanged;
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
        public string LastTriggeredActionName
        {
            get { return _lastTriggeredActionName; }
            set
            {
                if (_lastTriggeredActionName != value)
                {
                    _lastTriggeredActionName = value;
                    OnPropertyChanged();
                }
            }
        }
        public IOContainer IOContainer
        {
            get { return _iOContainer; }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        public void DoInitialActions() => _workFlow.CarrierStorage.ForEach(Cs => ExecuteActions(Cs.Initialize.InitialActions));
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
                        _lastTriggeredActionName = triggerAction.Name; // 記錄觸發的 Action Name
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
                    if (DoCondition(triggerPoint))
                    {
                        LastTriggeredActionName= triggerAction.Name;
                        return true;
                    }
                }
            }
            else if (triggerAction.TriggerPoint.Type == "AND")
            {
                foreach (var triggerPoint in triggerAction.TriggerPoint.Conditions)
                {
                    if (!DoCondition(triggerPoint)) return false;
                }
                LastTriggeredActionName = triggerAction.Name;
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
                    case "PriHandShake":
                        return new ExecutePriHandshake();
                    case "Index":
                        return new ExecuteIndex();
                    case "LoopAction":
                        return new ExecuteLoopAction();
                    case "Hold":
                        return new ExecuteHold();
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
                return _iOContainer.WriteListInt(device, addr, value);                
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
                            vals = DecodeContent(input.Content, input.Format, input.Lens);                            
                            break;
                        case "Action":
                            if (input.ActionName != "Read") throw new Exception("Action Not Support.");
                            if (!StringValidator.SplitAndValidateString(input.Address, out string device, out string address)) throw new Exception("Address Format Error.");
                            _iOContainer.ReadListInt(device, address, input.Lens, out vals);
                            if(input.ElementUnit == "Bit") vals = ConvertIntegerToBitList(vals).Select(b => b > 0 ? (short)1 : (short)0).ToList();
                            break;
                        case "GlobalVariable":
                            object output = GetGlobalVariable(input.Content.ToString(), out Type type, input.ElementUnit);
                            string formatName = string.Empty;
                            if (type != null) formatName = type == typeof(List<int>) ? "IntList" : type.Name;
                            vals = DecodeContent(output, formatName);
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
            private List<short> DecodeContent(object content, string format, int lens = 0)
            {
                switch (format.ToLower())
                {
                    case "string":
                        return MitUtility.getInstance().StringToASCII(content.ToString());
                    case "int":
                        //repeat content for lens times
                        return Enumerable.Repeat(Convert.ToInt16(content.ToString()), Math.Max(1, lens)).ToList();    
                    case "intlist":
                        return ((List<int>)content).Select(i => i > short.MaxValue ? (short)(i - 65536) : (short)i).ToList();
                    default:
                        throw new InvalidOperationException("Unsupported format: " + format);
                }
            }
            private object GetGlobalVariable(string content, out Type type, string elementUnit = "")
            {
                content = GlobalVariableHandler.ReplaceBindingMaterialToContent(_workFlow, content);
                string[] cArray = content.Split('.');
                object result = _workFlow.GlobalVariable;
                type = null;
                for (int i = 0; i < cArray.Length; i++)
                {
                    if (result == null) throw new Exception("Global Variable Not Exist.");

                    if (result is List<CassettleFormat> cassettleList) //for CassettleList
                    {
                        CassettleFormat cassettle = cassettleList.FirstOrDefault(c => c.CassettleId == cArray[i]);
                        if (cassettle == null)
                            throw new Exception($"Cassettle with ID 'Cassettle1' not found.");

                        result = cassettle;
                        type = result.GetType();
                    }
                    else if (result is List<Wafer> waferList) //for WaferList
                    {
                        if (int.TryParse(cArray[i], out int index))
                        {
                            if (index < 0 || index >= waferList.Count)
                                throw new Exception($"Index {index} out of range for list with {waferList.Count} items.");
                            result = waferList[index];
                            type = result?.GetType();
                        }
                        else if (cArray[i] == "All")
                        {
                            if (cArray.Length - 1 <= i)
                                throw new Exception($"Current key {cArray[i]} must have property.");
                            List<object> resultList = new List<object>();
                            foreach (Wafer wafer in waferList)
                            {
                                if (!wafer.TryGet(cArray[i + 1], out result, out type))
                                {
                                    throw new Exception($"Current key {cArray[i]} can't found from GV.");
                                }

                                if (elementUnit == "Bit")
                                {
                                    // If the result is a bool, add it to the list
                                    if (result is bool boolResult)
                                    {
                                        resultList.Add(boolResult);
                                    }
                                }
                                else
                                {
                                    throw new Exception($"In All {elementUnit} Not Support.");
                                }
                            }
                            result = ConvertBoolListToInteger(resultList.Cast<bool>().ToList());
                            type = result?.GetType();                            
                            return result; //got result here not aProperty object
                        }
                    }
                    else if (result is aProperty propertyObj) //normal case
                    {
                        if (!propertyObj.TryGet(cArray[i], out result, out type))
                        {
                            throw new Exception($"Current key {cArray[i]} can't found from GV.");
                        }
                    }
                    else
                    {
                        throw new Exception($"Current key {cArray[i]} not aProperty.");
                    }
                }
                return result;
            }
            /// <summary>
            /// Boolean list to integer list, make 16 bits as a word
            /// 1011 => 11
            /// </summary>
            /// <param name="boolList"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException"></exception>
            private List<int> ConvertBoolListToInteger(List<bool> boolList)
            {
                if (boolList == null || boolList.Count == 0)
                {
                    throw new ArgumentException("Input boolean list cannot be null or empty.");
                }                
                List<int> resultList = new List<int>();
                int result = 0;
                for (int i = 0; i < boolList.Count; i++)
                {
                    byte shift = (byte)(i % 16); //A Word is 16 bits
                    result = (result << 1) | (boolList[i] ? 1 : 0);
                    if(shift == 15 || i == boolList.Count - 1)
                    {
                        resultList.Add(result);//Add the converted integer at last bit of the word
                        result = 0;
                    }
                }
                return resultList;
            }
            /// <summary>
            /// ConverWordstoBitList, mkae 1 word to 16 bits
            /// 11 => [1, 1, 0, 1]
            /// </summary>
            /// <param name="wordValues"></param>
            /// <returns></returns>
            private List<int> ConvertIntegerToBitList(List<short> intValues)
            {
                List<int> bitList = new List<int>();
                foreach (short word in intValues)
                {
                    for (int i = 0; i < 16; i++) // short is 16 bits
                    {
                        bitList.Add(((word >> i) & 1) == 1 ? 1 : 0); // Extract each bit
                    }
                }
                return bitList;
            }
        }
        public class ExecutePriHandshake : IExeAction
        {
            public bool Execute(Model.Action action)
            {
                if (action.Inputs.Value.Length != 1) throw new Exception("For Secondary Handshake, Value should be 1.");
                string value = GetContent(action.Inputs.Value);//item1 : Address
                if (!StringValidator.SplitAndValidateString(value, out string Sdevice, out string Saddr)) throw new Exception("Address Format Error."); //相當於Passive的
                if (!StringValidator.SplitAndValidateString(action.Inputs.Address, out string Pdevice, out string Paddr)) throw new Exception("Address Format Error."); //相當於Active的
                return _iOContainer.PrimaryHandShake(Pdevice, Paddr, Sdevice, Saddr);
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
        public class ExecuteHold : IExeAction
        {
            public bool Execute(Model.Action action)
            {
                SpinWait.SpinUntil(() => false, _workFlow.GlobalVariable.Hold_Time);
                return true;
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
                // Loop through each element and execute LoopActions if condition is me
                for (_loopIndex = 0; _loopIndex < _loopElements.Count; _loopIndex++)
                {
                    if (CheckLoopCondition(_loopElements[_loopIndex], action.ExecuteCondition)) // Check condition for current element
                    {
                        if(!ExecuteLoopActions(action.SubActions))
                        {
                            return false;
                        }
                    }
                }
                return ExecutePostActions(action.PostActions);
            }
            /// <summary>
            /// Support Action Read and KeyIn for now
            /// </summary>
            /// <param name="inputValue"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            private bool CheckInputValue(InputValue inputValue)
            {                
                switch(inputValue.Type)
                {
                    case "Action":
                        return FillElementValueForAction(inputValue);
                    case "KeyIn":
                        return FillElementValueForKeyIn(inputValue);
                    default:
                        throw new Exception("LoopAction Input Value Type Not Support.");
                }
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
            ///<summary>
            /// Only support Read Action for now
            /// </summary>
            /// <param name="inputValue">Pass in Argument from JSON</param>
            /// <returns>Execute success or not</returns>
            /// <exception cref="Exception">Not support Argument or execute failed.</exception>
            private bool FillElementValueForAction(InputValue inputValue)
            {                
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
                switch (inputValue.ElementUnit)
                {
                    case "Bit":
                        _loopElements = ConvertWordsToBitList(ReadValue);
                        break;
                    case "Word":
                        _loopElements = ReadValue.SelectMany(v => BitConverter.GetBytes(v)).Select(b => (int)b).ToList();
                        break;
                    default:
                        throw new Exception("LoopAction Read ElementUnit Not Support.");
                }
                return true;
            }
            /// <summary>
            /// Fill loop elements from KeyIn content and only support IntList format
            /// </summary>
            /// <param name="inputValue"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            private bool FillElementValueForKeyIn(InputValue inputValue)
            {
                if (inputValue.Type != "KeyIn")
                {
                    throw new Exception("LoopAction Input Value should be KeyIn.");
                }
                if (inputValue.Format != "IntList")
                {
                    throw new Exception("LoopAction KeyIn Format should be Int.");
                }
                if (inputValue.Content == null)
                {
                    throw new Exception("LoopAction KeyIn Content should not be null.");
                }
                //have to check the content is int array or not
                if (!(inputValue.Content is JsonElement jsonElement))
                {
                    throw new Exception("LoopAction KeyIn Content should be an array.");
                }
                if (jsonElement.ValueKind != JsonValueKind.Array)
                {
                    throw new Exception("LoopAction KeyIn Content should be an array.");
                }
                _loopElements = new List<int>();
                foreach (var element in jsonElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Number)
                    {
                        throw new Exception("LoopAction KeyIn Content should be an array of numbers.");
                    }
                    _loopElements.Add(element.GetInt32());
                }              
                return true;
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
            private bool ExecutePostActions(List<Model.Action> postActions)
            {
                if (postActions == null) return false;

                foreach (var Action in postActions)
                {
                    if (!ExecuteFactory.GetExeAction(Action.ActionName).Execute(Action))
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
                if(LocalAction.ActionName == "PriHandShake") throw new Exception("LoopAction Not Support HandShake.");
                int ShiftMutiValueInOneAction = 0;
                foreach (InputValue inputValue in LocalAction.Inputs.Value)
                {
                    if(!ConvertLoopFormatToSTD(inputValue, ShiftMutiValueInOneAction)) throw new Exception("Convert Loop Format Error.");
                    ShiftMutiValueInOneAction += inputValue.Lens;
                }
                return ExecuteFactory.GetExeAction(LocalAction.ActionName).Execute(LocalAction); // Execute the action (using existing ExecuteAction logic)
            }
            private bool ConvertLoopFormatToSTD(InputValue inputValue, int shiftMutiValInOneAct)
            {
                if (inputValue.Type == "GlobalVariable")
                    return GlobbalTypeConvert(inputValue);
                if (inputValue.Type == "Action" && inputValue.ActionName == "Read")
                    return ReadTypeConvert(inputValue, shiftMutiValInOneAct);
                if(inputValue.Type == "KeyIn" && inputValue.Format == "Int") //Current only support Int because if using other type shift will be wrong
                    return true;
                return false;
            }
            private bool GlobbalTypeConvert(InputValue inputValue)
            {
                inputValue.Content = GlobalVariableHandler.ReplaceIndexToContent(inputValue.Content.ToString(), _loopIndex);                
                return true;
            }
            private bool ReadTypeConvert(InputValue inputValue, int shiftMutiValInOneAct)
            {
                if (!StringValidator.SplitAndValidateString(inputValue.Address, out string DeviceName, out string Address)) return false;
                if(inputValue.Floating) inputValue.Address = DeviceName + (Convert.ToInt32(Address, 16) + shiftMutiValInOneAct + _loopIndex * inputValue.Lens).ToString("X4");                
                return true;
            }
            
            private List<int> ConvertWordsToBitList(List<short> wordValues)
            {
                List<int> bitList = new List<int>();
                foreach (short word in wordValues)
                {
                    for (int i = 0; i < 16; i++) // short is 16 bits
                    {
                        bitList.Add(((word << 1) & 1) == 1 ? 1 : 0); // Extract each bit
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
