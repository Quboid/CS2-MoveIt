using MoveIt.Managers;
using MoveIt.UI.Foldout;
using System;
using System.Collections.Generic;

namespace MoveIt.UI
{
    /// <summary>
    /// Wrapper for an entry's state in the Toolbox foldout menu's main entry list
    /// </summary>
    internal record ToolEntry : FoldoutEntry
    {
        internal Type m_ActionType;

        internal ToolEntry(string id, Type action) : base(id)
        {
            m_ActionType = action;
            m_UIEntry = new FOEntryState(id, false);
        }

        internal override FOEntryStateBase GetUIState()
        {
            if (_MIT.ToolboxManager is not null && _MIT.ToolboxManager.IsActive(m_Id) && _MIT.MITState == Tool.MITStates.ToolActive)
            {
                if (!m_UIEntry.m_Active)
                {
                    m_UIEntry.m_Active = true;
                    UI_Changed = true;
                }
                return m_UIEntry;
            }

            if (m_UIEntry.m_Active)
            {
                m_UIEntry.m_Active = false;
                UI_Changed = true;
            }
            return m_UIEntry;
        }
    }

    public class ToolboxSectionState : FOSectionContainerState
    {
        public ToolboxSectionState() : base()
        {
            m_FOTitleState = new FOTitleState("toolboxTitle", true);
        }

        internal override List<FoldoutEntry> GetEntries()
        {
            return ToolboxManager.GetUIEntries();
        }
    }
}
