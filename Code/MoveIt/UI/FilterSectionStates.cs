using MoveIt.Searcher;
using MoveIt.UI.Foldout;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.UI
{
    public class FilterSectionState : FOSectionContainerState
    {
        public FilterSectionState() : base()
        {
            m_FOTitleState = new FOTitleState("filtersAll", false, new("filtersAll", true, true));
        }

        public override bool TogglePanelOpen()
        {
            IsPanelOpen = !IsPanelOpen;
            _MIT.Filtering.Active = IsPanelOpen;
            return IsPanelOpen;
        }

        internal void SetFilter(string key, bool active)
        {
            Filter f = GetFilter(key);
            f.Active = active;
            UpdateTitleCheckbox();
        }

        internal void ToggleFilter(string key)
        {
            Filter f = GetFilter(key);
            f.Active = !f.Active;
            UpdateTitleCheckbox();
        }

        internal void ToggleFilterOnly(string key)
        {
            foreach (FoldoutEntry f in m_Entries)
            {
                f.Active = f.m_Id.Equals(key);
            }
            UpdateTitleCheckbox();
        }

        private Filter GetFilter(string id)
        {
            if (m_Entries.Count(f => f.m_Id.Equals(id)) == 0)
            {
                throw new Exception($"Filter '{id}' not found in filters list");
            }
            return (Filter)m_Entries.First(f => f.m_Id.Equals(id));
        }

        private void UpdateTitleCheckbox()
        {
            bool active = true;
            foreach (FoldoutEntry f in m_Entries)
            {
                if (!f.Active)
                {
                    active = false;
                    break;
                }

            }
            m_FOTitleState.m_CheckboxState.m_Active = active;
        }

        internal void UI_ToggleAll(bool active)
        {
            m_FOTitleState.m_CheckboxState.m_Active = active;
            foreach (FoldoutEntry f in m_Entries)
            {
                f.Active = active;
            }
        }

        internal Filters GetMask()
        {
            Filters mask = Filters.ControlPoints;
            if (_MIT.Filtering.Active)
            {
                foreach (FoldoutEntry f in m_Entries)
                {
                    if (f.Active)
                    {
                        mask |= ((Filter)f).m_MaskBit;
                    }
                }
            }
            else
            {
                mask = Utils.FilterAll;
            }
            return mask;
        }

        public override bool Equals(object obj)
        {
            if (obj is not FilterSectionState) return false;
            if (_MIT is null || _MIT.Filtering is null) return true;

            if (Changed)
            {
                Changed = false;
                return false;
            }

            return true;
        }

        internal override List<FoldoutEntry> GetEntries()
        {
            return new()
            {
                new Filter("buildings", Filters.Buildings,  true),
                new Filter("plants",    Filters.Plants,     true),
                new Filter("decals",    Filters.Decals,     true),
                new Filter("props",     Filters.Props,      true),
                new Filter("surfaces",  Filters.Surfaces,   true),
                new Filter("nodes",     Filters.Nodes,      true),
                new Filter("segments",  Filters.Segments,   true),
            };
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
