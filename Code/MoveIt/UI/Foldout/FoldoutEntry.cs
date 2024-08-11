using MoveIt.Tool;
using System;

namespace MoveIt.UI.Foldout
{
    /// <summary>
    /// Wrapper for a foldout menu entry's state object
    /// </summary>
    internal abstract record FoldoutEntry : IEquatable<FoldoutEntry>
    {
        protected readonly MIT _MIT = MIT.m_Instance;

        internal virtual bool Active
        {
            get => m_UIEntry.m_Active;
            set => m_UIEntry.m_Active = value;
        }

        internal virtual bool UI_Changed
        {
            get => m_UIEntry.Changed;
            set => m_UIEntry.Changed = value;
        }

        internal string m_Id;
        internal FOEntryStateBase m_UIEntry;

        internal FoldoutEntry(string id)
        {
            m_Id = id;
        }

        internal virtual FOEntryStateBase GetUIState()
        {
            return m_UIEntry;
        }

        internal bool Equal(FoldoutEntry other)
        {
            return m_Id.Equals(other.m_Id);
        }
    }
}
