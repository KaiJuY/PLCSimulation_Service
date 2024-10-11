using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventDriven.Model
{
    public class TriggerWorkFlowModel
    {
        public Materials Materials { get; set; }
        public Initialize Initialize { get; set; }
        public Trigger Trigger { get; set; }
    }

    public class Materials
    {
        public List<CassettleFormat> CassettleFormat { get; set; }
    }

    public class CassettleFormat
    {
        public string CassettleId { get; set; }
        public bool ReadResult { get; set; }
        public List<WaferList> WaferList { get; set; }
    }

    public class WaferList
    {
        public string WaferId { get; set; }
        public bool ReadResult { get; set; }
    }

    public class Initialize
    {
        public List<object> InitialActions { get; set; }
    }

    public class Trigger
    {
        public int Interval { get; set; }
        public List<TriggerAction> TriggerActions { get; set; }
    }
    /// <summary>
    /// TriggerAction: 觸發條件群與動作
    /// 會包含TriggerPoint與Actions會試在Condition符合時執行所有Actions
    /// </summary>
    public class TriggerAction
    {
        public string Name { get; set; }
        public TriggerPoint TriggerPoint { get; set; }
        public List<Action> Actions { get; set; }
    }
    /// <summary>
    /// 觸發條件群與類型
    /// Type可能會有OR, AND
    /// Conditions可能會有多個
    /// OR: 只要有一個條件符合就觸發
    /// AND: 所有條件都符合才觸發
    /// </summary>
    public class TriggerPoint
    {
        public string Type { get; set; }
        public List<Condition> Conditions { get; set; }
    }
    /// <summary>
    /// Condition會圍繞著Action組成觸發的條件
    /// Action 
    /// - Monitor Address的數值從別的變為ExceptedValue時達到Condition => 適用於Handshake, 狀態變化到某個數值
    /// - Change Address的數值變化時達到Condition，此時ExceptedValue無作用 => 適用於Index, Alive
    /// </summary>
    public class Condition
    {
        public string Action { get; set; }
        public int ExceptedValue { get; set; }
        public string Address { get; set; }
        public int LastValue { get; set; }
    }
    /// <summary>
    /// 實際上執行的動作
    /// ActionName: 動作名稱為主要執行的行為
    /// - Write: 寫入數值到Address
    /// Inputs: 動作的輸入參數
    /// Output: 動作的輸出參數暫時沒有用到因此沒有定義特別的類別
    /// </summary>
    public class Action
    {
        public string ActionName { get; set; }
        public Inputs Inputs { get; set; }
        public object Output { get; set; }
    }
    /// <summary>
    /// Inputs: 動作的輸入參數
    /// Adress : 動作的地址
    /// Value : 動作的數值
    /// 這兩者共用一個類別InputValue
    /// </summary>
    public class Inputs
    {
        public string Address { get; set; }
        public InputValue[] Value { get; set; }
    }
    /// <summary>
    /// Type: 數值的型態分為兩種
    /// - KeyIn: 手動輸入
    /// - Action: 來自另一個動作的結果
    /// Format: 數值的格式
    /// - Int: 整數
    /// - String: 字串
    /// Content: 數值的內容會根據Type的不同而有所不同
    /// 支援KeyIn時直接依照Format的格式輸入
    /// 如果是Action則會有ActionName, Address
    /// 像是ActionName: Read, Address: W1000
    /// </summary>
    public class InputValue
    {
        public string Type { get; set; }
        // KeyIn related properties
        public string Format { get; set; }
        public object Content { get; set; }
        // Action related properties
        public string ActionName { get; set; }
        public string Address { get; set; }
        public int Lens { get; set; }
        // Custom serialization logic depending on Type
        public bool ShouldSerializeFormat()
        {
            return Type == "KeyIn";
        }
        public bool ShouldSerializeContent()
        {
            return Type == "KeyIn";
        }
        public bool ShouldSerializeActionName()
        {
            return Type == "Action";
        }
        public bool ShouldSerializeAddress()
        {
            return Type == "Action";
        }
        public bool ShouldSerializeLens()
        {
            return Type == "Action";
        }
    }
}