using Colossal.UI.Binding;
using QCommonLib;

namespace MoveIt.UI
{
    public class ButtonState : IJsonWritable
    {
        public string m_Id;
        public bool m_Enabled;
        public bool m_Active;

        private bool _Changed;

        public ButtonState(string id, bool enabled, bool active)
        {
            m_Id = id;
            m_Enabled = enabled;
            m_Active = active;

            _Changed = true;
        }

        public void Update(bool enabled, bool active)
        {
            if (m_Enabled == enabled && m_Active == active)
            {
                return;
            }

            m_Enabled = enabled;
            m_Active = active;

            _Changed = true;
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName("Id");
            writer.Write(m_Id);
            writer.PropertyName("IsEnabled");
            writer.Write(m_Enabled);
            writer.PropertyName("IsActive");
            writer.Write(m_Active);
            writer.TypeEnd();
        }

        public override string ToString()
        {
            return $"{m_Id} E:{m_Enabled}, A:{m_Active}";
        }

        public override bool Equals(object obj)
        {
            if (obj is not ButtonState) return false;

            if (_Changed)
            {
                _Changed = false;
                return false;
            }

            return true;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
