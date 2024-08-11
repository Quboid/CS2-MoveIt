using Colossal.UI.Binding;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.UI.Foldout
{
    public abstract class FOSectionContainerStateBase : IJsonWritable
    {
        protected readonly MIT _MIT = MIT.m_Instance;

        internal List<FoldoutEntry> m_Entries;

        internal virtual bool IsPanelOpen
        {
            get => _IsPanelOpen;
            set
            {
                if (_IsPanelOpen == value) return;
                _Changed = true;
                _IsPanelOpen = value;
            }
        }
        protected bool _IsPanelOpen = false;

        internal virtual bool Changed
        {
            get
            {
                if (HaveAnyEntriesChanged)
                {
                    return true;
                }
                return _Changed;
            }
            set
            {
                m_Entries.ForEach(f => f.UI_Changed = value);
                _Changed = value;
            }
        }
        protected bool _Changed;

        internal bool HaveAnyEntriesChanged => m_Entries.Count(f => f.UI_Changed) > 0;

        public virtual bool TogglePanelOpen()
        {
            IsPanelOpen = !IsPanelOpen;
            return IsPanelOpen;
        }

        public virtual void Update()
        {
            _Changed = true;
        }


        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);

            writer.PropertyName("IsOpen");
            writer.Write(IsPanelOpen);

            WriteExtend(writer);

            writer.PropertyName("Entries");
            writer.ArrayBegin(m_Entries.Count);
            for (int i = 0; i < m_Entries.Count; i++)
            {
                writer.Write(m_Entries[i].m_UIEntry);
            }
            writer.ArrayEnd();

            writer.TypeEnd();
        }

        public virtual void WriteExtend(IJsonWriter writer)
        { }

        public override string ToString()
        {
            return $"FoldoutSectionStateBase m_IsOpen:({IsPanelOpen}):";
        }

        public override bool Equals(object obj)
        {
            if (!obj.GetType().Equals(GetType())) return false;

            if (_Changed)
            {
                _Changed = false;
                return false;
            }

            return true;
        }

        public override int GetHashCode() => base.GetHashCode();

        internal abstract List<FoldoutEntry> GetEntries();


        internal virtual string DebugSectionStates()
        {
            string msg = $"{GetType().FullName} {m_Entries.Count}";
            for (int i = 0; i < m_Entries.Count; i++)
            {
                msg += $"\n    {i}: {m_Entries[i].m_UIEntry}";
            }
            return msg;
        }

        internal void DebugDumpSectionStates(string prefix = "")
        {
            MIT.Log.Debug(prefix + DebugSectionStates());
        }
    }
}
