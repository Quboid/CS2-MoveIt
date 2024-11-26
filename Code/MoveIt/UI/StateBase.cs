using Colossal.UI.Binding;

namespace MoveIt.UI
{
    public abstract class StateBase : IJsonWritable
    {
        public readonly string m_Id;
        public bool m_Enabled;
        public bool m_Active;

        public bool Changed
        {
            get => _Changed;
            set => _Changed = value;
        }
        protected bool _Changed;

        protected StateBase(string id, bool enabled, bool active)
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
            WriteExtend(writer);
            writer.TypeEnd();
        }

        protected virtual void WriteExtend(IJsonWriter writer)
        { }

        public override string ToString()
        {
            return $"{m_Id} E:{m_Enabled}, A:{m_Active}";
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj.GetType() != GetType()) return false;

            if (_Changed)
            {
                _Changed = false;
                return false;
            }

            return true;
        }

        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        public override int GetHashCode() => base.GetHashCode();
    }
}
