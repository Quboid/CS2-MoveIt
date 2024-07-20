using Colossal.UI.Binding;
using MoveIt.Tool;
using MoveIt.Systems.UIElements.Foldout;

namespace MoveIt.Systems.UIElements
{
    public abstract class FoldoutCBState : IJsonWritable
    {
        public readonly MIT _Tool = MIT.m_Instance;

        public bool m_IsOpen;
        public FOTitleCBState m_DDTitleCBState;
        public FOEntryCBState[] m_DDEntryCBStates;

        protected bool _Changed;

        public FoldoutCBState()
        {
            m_IsOpen = false;
            _Changed = true;
        }

        public void Update()
        {
            _Changed = m_IsOpen != _Tool.m_UISystem.m_isFiltersOpen;
            m_IsOpen = _Tool.m_UISystem.m_isFiltersOpen;
            m_DDTitleCBState.Update(true, false);
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);

            writer.PropertyName("IsOpen");
            writer.Write(m_IsOpen);

            writer.PropertyName("Title");
            writer.Write(m_DDTitleCBState);

            writer.PropertyName("Entries");
            writer.ArrayBegin(m_DDEntryCBStates.Length);
            for (int i = 0; i < m_DDEntryCBStates.Length; i++)
            {
                writer.Write(m_DDEntryCBStates[i]);
            }
            writer.ArrayEnd();

            writer.TypeEnd();
        }

        public override string ToString()
        {
            string msg = $"DropdownState {m_DDTitleCBState.m_Id} m_IsOpen:({m_IsOpen}):";
            return msg;
        }

        public override bool Equals(object obj)
        {
            if (obj is not FilterSectionStates) return false;

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
