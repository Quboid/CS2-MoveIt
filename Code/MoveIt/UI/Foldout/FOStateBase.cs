using Colossal.UI.Binding;
using MoveIt.UI.Checkbox;

namespace MoveIt.UI.Foldout
{
    public abstract class FOStateBase : StateBase
    {
        public CheckboxState m_CheckboxState;
        public string m_RawId;

        public FOStateBase(string rawId, bool active, CheckboxState checkboxState = null) : base(rawId + "Row", true, active)
        {
            m_CheckboxState = checkboxState;
            m_RawId = rawId;
        }

        public void Update(bool enabled, bool active, bool CBenabled, bool CBactive)
        {
            if (m_CheckboxState is null)
            {
                throw new System.Exception($"Checkbox-specific update called but no checkbox is enabled");
            }

            if (m_Enabled == enabled && m_Active == active && m_CheckboxState.m_Enabled == CBenabled && m_CheckboxState.m_Active == CBactive)
            {
                return;
            }

            m_Enabled = enabled;
            m_Active = active;
            m_CheckboxState.Update(CBenabled, CBactive);
        }

        public override void WriteExtend(IJsonWriter writer)
        {
            writer.PropertyName("RawId");
            writer.Write(m_RawId);
            if (m_CheckboxState is not null)
            {
                writer.PropertyName("Checkbox");
                writer.Write(m_CheckboxState);
            }
            WriteExtendInner(writer);
        }

        public virtual void WriteExtendInner(IJsonWriter writer)
        { }

        public override string ToString()
        {
            if (m_CheckboxState is not null)
            {
                return $"{m_Id} E:{m_Enabled}, A:{m_Active}, CB:[{m_CheckboxState}]";
            }
            return $"{m_Id} E:{m_Enabled}, A:{m_Active}";
        }
    }
}
