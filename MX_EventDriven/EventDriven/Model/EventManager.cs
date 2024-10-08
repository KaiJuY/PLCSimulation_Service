using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDriven.Model;
using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;

namespace EventDriven.Services
{
    public class EventManager
    {
        private TriggerWorkFlowModel _workFlow;
        private Dictionary<string, TriggerBehavior> _registeredEvents;
        private string jsonFileName;
        public string JsonFileName
        {
            get { return jsonFileName; }
            set { jsonFileName = value; }
        }
        public EventManager()
        {
            _registeredEvents = new Dictionary<string, TriggerBehavior>();
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
        }
        public void UnregisterEvents()
        {
            _registeredEvents.Clear();
        }
        public void Monitor()
        {
            foreach (var trigger in _registeredEvents)
            {
                trigger.Value.CheckAndTrigger(); // 依次檢查並觸發事件
            }
        }
        private bool CheckCondition(Model.TriggerAction triggerAction)
        {
            bool result = false;

            if (triggerAction.TriggerPoint.Type == "OR")
            {
                foreach (var triggerPoint in triggerAction.TriggerPoint.Conditions)
                {
                    //任何一個實現就返回true
                }
            }
            else if (triggerAction.TriggerPoint.Type == "AND")
            {
                foreach (var triggerPoint in triggerAction.TriggerPoint.Conditions)
                {
                    //全部實現就返回true
                }
            }
            return result;
        }
        private void ExecuteActions(List<Model.Action> actions)
        {
            foreach (var action in actions)
            {
                //這邊要實現具體的動作執行邏輯最好是Async
            }
        }
        /*
        //public async Task ExecuteEventAsync(string eventName)
        //{
        //    if (_registeredEvents.TryGetValue(eventName, out var action))
        //    {
        //        await action();
        //    }
        //    else
        //    {
        //        throw new ArgumentException($"Event '{eventName}' is not registered.");
        //    }
        //}

        //private async Task ExecuteActionsAsync(List<Models.Action> actions)
        //{
        //    foreach (var action in actions)
        //    {
        //        await ExecuteActionAsync(action);
        //    }
        //}

        //private async Task ExecuteActionAsync(Models.Action action)
        //{
        //    // 這裡需要實現具體的動作執行邏輯
        //    // 例如，根據 action.ActionName 來決定執行什麼操作
        //    switch (action.ActionName)
        //    {
        //        case "Write":
        //            await WriteAsync(action.Inputs);
        //            break;
        //        case "Read":
        //            await ReadAsync(action.Inputs.Address.Content.ToString());
        //            break;
        //        default:
        //            throw new NotImplementedException($"Action '{action.ActionName}' is not implemented.");
        //    }
        //}

        //private async Task WriteAsync(Inputs inputs)
        //{
        //    string address = inputs.Address.Content.ToString();
        //    object value = inputs.Value.Content;

        //    if (inputs.Value.Type == "Action")
        //    {
        //        // 如果值是另一個動作的結果，先執行該動作
        //        var readAction = new Models.Action
        //        {
        //            ActionName = "Read",
        //            Inputs = new Inputs { Address = new InputValue { Content = inputs.Value.Content.ToString() } }
        //        };
        //        value = await ReadAsync(inputs.Value.Content.ToString());
        //    }

        //    // 這裡需要實現實際的寫入邏輯
        //    Console.WriteLine($"Writing value {value} to address {address}");
        //    // 實際的寫入操作可能需要調用特定的 API 或服務
        //}

        //private async Task<object> ReadAsync(string address)
        //{
        //    // 這裡需要實現實際的讀取邏輯
        //    Console.WriteLine($"Reading from address {address}");
        //    // 實際的讀取操作可能需要調用特定的 API 或服務
        //    return await Task.FromResult<object>(0); // 假設返回值
        //}
        */
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