using System.Collections.Generic;
using Colossal.UI.Binding;

namespace MoveIt.UI.Foldout
{
    public abstract class FOSectionContainerState : FOSectionContainerStateBase
    {
        protected FOTitleState m_FOTitleState;

        protected FOSectionContainerState()
        {
            _Changed = true;
            m_Entries = GetEntriesFromAbstract();
        }

        private List<FoldoutEntry> GetEntriesFromAbstract()
            => GetEntries();

        internal override bool Changed
        {
            get
            {
                if (HaveAnyEntriesChanged)
                {
                    return true;
                }
                if (m_FOTitleState.Changed)
                {
                    return true;
                }
                return _Changed;
            }
            set
            {
                m_Entries.ForEach(f => f.UI_Changed = value);
                m_FOTitleState.Changed = value;
                _Changed = value;
            }
        }

        public override void Update()
        {
            m_FOTitleState.Update(true, true);
            _Changed = true;
        }

        /// <summary>
        /// Gets array of FOEntryState (Foldout Entry state) data
        /// </summary>
        //internal FOEntryState[] GetEntriesStates()
        //{
        //    FOEntryState[] entries = new FOEntryState[m_Entries.Count];

        //    int i = 0;
        //    foreach (FoldoutEntry f in m_Entries)
        //    {
        //        entries[i++] = f.GetUIState();
        //    }

        //    return entries;
        //}

        public override void WriteExtend(IJsonWriter writer)
        {
            writer.PropertyName("Title");
            writer.Write(m_FOTitleState);
        }

        public override string ToString()
        {
            return $"FoldoutState {m_FOTitleState.m_Id} m_IsOpen:({IsPanelOpen}):";
        }


        internal override string DebugSectionStates()
        {
            string msg = $"{GetType().FullName} {m_Entries.Count}";
            msg += $"\nTitle: {m_FOTitleState}";
            for (int i = 0; i < m_Entries.Count; i++)
            {
                msg += $"\n    {i}: {m_Entries[i].m_UIEntry}";
            }
            return msg;
        }
    }
}
