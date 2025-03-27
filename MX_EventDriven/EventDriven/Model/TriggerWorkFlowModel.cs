using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace EventDriven.Model
{
    public abstract class aProperty
    {
        public bool Contains(string propertyName)
        {
            return this.GetType().GetProperty(propertyName) != null;
        }        
        public bool TryGet(string propertyName, out object value, out Type perpertyType)
        {
            PropertyInfo property = this.GetType().GetProperty(propertyName);
            perpertyType = property.PropertyType;
            if (property != null)
            {
                value = property.GetValue(this);
                return true;
            }
            value = null;
            return false;
        }
        public object Get(string propertyName, out Type perpertyType)
        {
            PropertyInfo property = this.GetType().GetProperty(propertyName);
            perpertyType = property.PropertyType;
            return property?.GetValue(this) ?? throw new ArgumentException($"Property {propertyName} not found.");
        }
        public void Set(string propertyName, object value)
        {
            PropertyInfo property = this.GetType().GetProperty(propertyName);
            if (property == null)
            {
                throw new ArgumentException($"Property {propertyName} not found.");
            }
            property.SetValue(this, value);
        }
    }
    public class TriggerWorkFlowModel :  aProperty
    {
        public GlobalVariable GlobalVariable { get; set; }
        public List<CarrierStorage> CarrierStorage { get; set; }
        public List<ButtonConfig> Buttons { get; set; }
    }

    public class ButtonConfig : aProperty
    {
        public string ButtonContent { get; set; }
        public List<Action> Actions { get; set; }
    }
    public class GlobalVariable : aProperty
    {
        public Materials Materials { get; set; }
        public int Monitor_Interval { get; set; }
        public int Action_Interval { get; set; }
        public int Hold_Time { get; set; }

    }
    public class Materials : aProperty
    {
        public List<CassettleFormat> CassettleFormat { get; set; }
    }
    public class CarrierStorage : aProperty
    {
        public string Name { get; set; }
        public Material Material { get; set; }
        public Initialize Initialize { get; set; }
        public Trigger Trigger { get; set; }
    }
    public class Material : aProperty
    {
        public string BindingMaterial { get; set; } //�qMaterial�����o���S�w��CassettleId
    }

    public class CassettleFormat : aProperty
    {
        public string CassettleId { get; set; }
        public bool ReadResult { get; set; }
        public List<Wafer> WaferList { get; set; }
    }

    public class Wafer : aProperty
    {
        public bool Existed { get; set; }
        public string WaferId { get; set; }
        public bool ReadResult { get; set; }
        public int WorkNumber { get; set; }
        public int SlotNo { get; set; }
    }

    public class Initialize : aProperty
    {
        public List<Action> InitialActions { get; set; }
    }

    public class Trigger : aProperty
    {
        public List<TriggerAction> TriggerActions { get; set; }
    }
    /// <summary>
    /// TriggerAction: Ĳ�o����s�P�ʧ@
    /// �|�]�tTriggerPoint�PActions�|�զbCondition�ŦX�ɰ���Ҧ�Actions
    /// </summary>
    public class TriggerAction : aProperty
    {
        public string Name { get; set; }
        public TriggerPoint TriggerPoint { get; set; }
        public List<Action> Actions { get; set; }
    }
    /// <summary>
    /// Ĳ�o����s�P����
    /// Type�i��|��OR, AND
    /// Conditions�i��|���h��
    /// OR: �u�n���@�ӱ���ŦX�NĲ�o
    /// AND: �Ҧ����󳣲ŦX�~Ĳ�o
    /// </summary>
    public class TriggerPoint : aProperty
    {
        public string Type { get; set; }
        public List<Condition> Conditions { get; set; }
    }
    /// <summary>
    /// Condition�|��¶��Action�զ�Ĳ�o������
    /// Action 
    /// - Monitor Address���ƭȱq�O���ܬ�ExceptedValue�ɹF��Condition => �A�Ω�Handshake, ���A�ܤƨ�Y�Ӽƭ�
    /// - Change Address���ƭ��ܤƮɹF��Condition�A����ExceptedValue�L�@�� => �A�Ω�Index, Alive
    /// </summary>
    public class Condition : aProperty
    {
        public string Action { get; set; }
        public int ExceptedValue { get; set; }
        public string Address { get; set; }
        public int LastValue { get; set; }
    }
    /// <summary>
    /// ��ڤW���檺�ʧ@
    /// ActionName: �ʧ@�W�٬��D�n���檺�欰
    /// - Write: �g�J�ƭȨ�Address
    /// Inputs: �ʧ@����J�Ѽ�
    /// Output: �ʧ@����X�ѼƼȮɨS���Ψ�]���S���w�q�S�O�����O
    /// </summary>
    public class Action : aProperty
    {        
        public string ActionName { get; set; }
        public Inputs Inputs { get; set; }
        public object Output { get; set; }
        public ExecuteCondition ExecuteCondition { get; set; }
        public List<Action> SubActions { get; set; }
        public List<Action> PostActions { get; set; }
    }
    /// <summary>
    /// Inputs: �ʧ@����J�Ѽ�
    /// Adress : �ʧ@���a�}
    /// Value : �ʧ@���ƭ�
    /// �o��̦@�Τ@�����OInputValue
    /// </summary>
    public class Inputs : aProperty
    {
        public string Address { get; set; }
        public InputValue[] Value { get; set; }
    }
    /// <summary>
    /// Type: �ƭȪ����A�������
    /// - KeyIn: ��ʿ�J
    /// - Action: �Ӧۥt�@�Ӱʧ@�����G
    /// Format: �ƭȪ��榡
    /// - Int: ���
    /// - String: �r��
    /// Content: �ƭȪ����e�|�ھ�Type�����P�Ӧ��Ҥ��P
    /// �䴩KeyIn�ɪ����̷�Format���榡��J
    /// �p�G�OAction�h�|��ActionName, Address
    /// ���OActionName: Read, Address: W1000
    /// </summary>
    public class InputValue : aProperty
    {
        public string Type { get; set; } // "KeyIn", "Global"
        // KeyIn related properties
        public string Format { get; set; } // "Int", "String" only For KeyIn
        public object Content { get; set; } //Specific value for KeyIn, Global variable name for Global
        // Action related properties
        public string ActionName { get; set; } // "Read", "Write", "SecHandShake"
        public string Address { get; set; } // "W1000"
        public int Lens { get; set; } // 1
        public string ElementUnit { get; set; } //"Bit", "Word"
        /// <summary>
        /// Only for loop Read use
        /// </summary>
        public bool Floating { get; set; }
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
    public class ExecuteCondition : aProperty
    {
        public string Type { get; set; } // "Equal"
        public string Format { get; set; } // "Int"
        public int Content { get; set; } //1
    }
    public class GlobalVariableHandler
    {
        public static string ReplaceIndexToContent(string content, int index) => content.Replace("Index", index.ToString());
        public static string ReplaceBindingMaterialToContent(TriggerWorkFlowModel workflow, string content)
        {
            string[] cArray = content.Split('.');
            if (cArray.Length == 0) return content;
            string Prefix = cArray[0] + ".";
            foreach (CarrierStorage cS in workflow.CarrierStorage)
            {
                if (cS.Name == cArray[0])
                {
                    content = content.Replace("BindingMaterial", cS.Material.BindingMaterial);
                    break;
                }
            }
            return content.StartsWith(Prefix) ? content.Substring(Prefix.Length) : content;
        }
    }
}
