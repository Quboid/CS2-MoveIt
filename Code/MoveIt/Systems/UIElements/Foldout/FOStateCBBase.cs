using Colossal.UI.Binding;
using MoveIt.Systems.UIElements.Checkbox;

namespace MoveIt.Systems.UIElements.Foldout
{
    public abstract class FOStateCBBase : StateBase
    {
        public CheckboxState m_CheckboxState;

        public FOStateCBBase(string id, bool enabled, bool active, CheckboxState checkboxState) : base(id, enabled, active)
        {
            m_CheckboxState = checkboxState;
        }

        public void Update(bool enabled, bool active, CheckboxState checkboxState)
        {
            if (m_Enabled == enabled && m_Active == active && checkboxState.Equals(m_CheckboxState))
            {
                return;
            }

            m_Enabled = enabled;
            m_Active = active;
            m_CheckboxState = checkboxState;
            _Changed = true;
        }

        public override void WriteExtend(IJsonWriter writer)
        {
            writer.PropertyName("Checkbox");
            writer.Write(m_CheckboxState);
        }

        public override string ToString()
        {
            return $"{m_Id} E:{m_Enabled}, A:{m_Active}, CB:[{m_CheckboxState}]";
        }
    }
}
