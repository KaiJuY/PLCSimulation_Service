using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EventDriven.Model
{
    public class TriggerWorkFlowModel
    {
        public GlobalVariable GlobalVariable { get; set; }
        public List<CarrierStorage> CarrierStorage { get; set; }
        public List<ButtonConfig> Buttons { get; set; }
    }

    public class ButtonConfig
    {
        public string ButtonContent { get; set; }
        public List<Action> Actions { get; set; }
    }
    public class GlobalVariable
    {
        public Materials Materials { get; set; }
        public int Monitor_Interval { get; set; }
        public int Action_Interval { get; set; }

    }
    public class Materials
    {
        public List<CassettleFormat> CassettleFormat { get; set; }
    }
    public class CarrierStorage
    {
        public string Name { get; set; }
        public Material Material { get; set; }
        public Initialize Initialize { get; set; }
        public Trigger Trigger { get; set; }
    }
    public class Material
    {
        public string BindingMaterial { get; set; } //�qMaterial�����o���S�w��CassettleId
    }

    public class CassettleFormat
    {
        public string CassettleId { get; set; }
        public bool ReadResult { get; set; }
        public List<Wafer> WaferList { get; set; }
    }

    public class Wafer
    {
        public bool Existed { get; set; }
        public string WaferId { get; set; }
        public bool ReadResult { get; set; }
        public int WorkNumber { get; set; }
    }

    public class Initialize
    {
        public List<object> InitialActions { get; set; }
    }

    public class Trigger
    {
        public List<TriggerAction> TriggerActions { get; set; }
    }
    /// <summary>
    /// TriggerAction: Ĳ�o����s�P�ʧ@
    /// �|�]�tTriggerPoint�PActions�|�զbCondition�ŦX�ɰ���Ҧ�Actions
    /// </summary>
    public class TriggerAction
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
    public class TriggerPoint
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
    public class Condition
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
    public class Action
    {
        public string ActionName { get; set; }
        public Inputs Inputs { get; set; }
        public object Output { get; set; }
    }
    /// <summary>
    /// Inputs: �ʧ@����J�Ѽ�
    /// Adress : �ʧ@���a�}
    /// Value : �ʧ@���ƭ�
    /// �o��̦@�Τ@�����OInputValue
    /// </summary>
    public class Inputs
    {
        public string Address { get; set; }
        public InputValue[] Value { get; set; }
        public ExecuteCondition ExecuteCondition { get; set; }
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
    public class InputValue
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
    public class ExecuteCondition
    {
        public string Type { get; set; } // "Equal"
        public string Format { get; set; } // "Int"
        public object Content { get; set; } //1
    }
}
