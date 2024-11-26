using MoveIt.UI.Foldout;

namespace MoveIt.Searcher
{
    /// <summary>
    /// Wrapper for an entry's state in the Filter foldout menu's main entry list
    /// </summary>
    internal record Filter : FoldoutEntry
    {
        internal Filter(string id, Filters bit, bool active) : base(id)
        {
            m_MaskBit = bit;
            m_UIEntry = new FOEntryState(id, false, new(id, true, active));
        }

        internal override bool Active
        {
            get => m_UIEntry.m_CheckboxState.m_Active;
            set
            {
                UI_Changed = m_UIEntry.m_CheckboxState.m_Active != value;
                m_UIEntry.m_CheckboxState.m_Active = value;
            }
        }

        internal override bool UI_Changed
        {
            get => m_UIEntry.m_CheckboxState.Changed;
            set => m_UIEntry.m_CheckboxState.Changed = value;
        }

        internal readonly Filters m_MaskBit;

        internal bool Equal(Filter other)
        {
            return m_Id.Equals(other.m_Id);
        }
    }
}
