using Colossal.IO.AssetDatabase.Internal;
using MoveIt.Moveables;
using MoveIt.Selection;
using System.Collections.Generic;
using Unity.Entities;

namespace MoveIt.Actions
{
    internal class SelectMarqueeAction : Action
    {
        public override string Name => "SelectMarqueeAction";

        /// <summary>
        /// The selected objects before any mid-marquee additions
        /// </summary>
        private readonly SelectionBase _MarqueeStart;

        private readonly bool _IsAppend;

        /// <summary>
        /// Constructor for SelectMarqueeAction
        /// </summary>
        /// <param name="append">Should this be added to existing selection, or should new one be made?</param>
        internal SelectMarqueeAction(bool append) : base()
        {
            _IsAppend = append;

            if (_Tool.UseMarquee)
            {
                if (_IsAppend && _Tool.Selection is not null)
                {
                    _MarqueeStart = new SelectionNormal(_Tool.Selection);
                }
                else
                {
                    _MarqueeStart = new SelectionNormal();
                }
            }
        }

        public override void Do()
        {
            base.Do();

            if (!_IsAppend)
            {
                _Tool.Selection.Clear();
                _Tool.Moveables.Refresh();
            }
        }

        public void AddMarqueeSelection(Input.Marquee marquee, bool fast)
        {
            //if (marquee is not null && marquee.m_Entities is not null && marquee.m_EntitiesPrev is not null && marquee.m_Entities.Count != marquee.m_EntitiesPrev.Count)
            //{
            //    HashSet<Entity> added = new(marquee.m_Entities);
            //    HashSet<Entity> removed = new(marquee.m_EntitiesPrev);
            //    added.ExceptWith(marquee.m_EntitiesPrev);
            //    removed.ExceptWith(marquee.m_Entities);
            //    string add = "   Add: ";
            //    string remove = "Remove: ";
            //    added.ForEach(e => add += $"{e.DX()}, ");
            //    removed.ForEach(e => remove += $"{e.DX()}, ");
            //    QLog.Debug($"AddMarq ent:{marquee.m_Entities?.Count}, prev:{marquee.m_EntitiesPrev?.Count}{(removed.Count > 0 ? $"\n    {remove}" : "")}{(added.Count > 0 ? $"\n    {add}" : "")}");
            //}

            HashSet<MVDefinition> initialSelection = new(_MarqueeStart.Definitions);
            HashSet<MVDefinition> currentSelection = _Tool.Selection.Definitions;
            HashSet<Entity> initialEntities = _MarqueeStart.Entities;

            HashSet<Entity> toRemove;
            if (marquee.m_EntitiesPrev is not null)
            {
                toRemove = new(marquee.m_EntitiesPrev);
                if (marquee.m_Entities is not null) toRemove.ExceptWith(marquee.m_Entities);
                toRemove.ExceptWith(initialEntities);

                if (toRemove.Count > 0)
                {
                    HashSet<MVDefinition> toRemoveDefs = new();
                    toRemove.ForEach(e => toRemoveDefs.Add(new(QTypes.GetEntityIdentity(e), e, false)));
                    _Tool.Selection.Remove(toRemoveDefs, true);
                }
            }

            HashSet<Entity> toAdd;
            if (marquee.m_Entities is not null)
            {
                toAdd = new(marquee.m_Entities);
                if (marquee.m_EntitiesPrev is not null) toAdd.ExceptWith(marquee.m_EntitiesPrev);
                toAdd.ExceptWith(initialEntities);

                if (toAdd.Count > 0)
                {
                    HashSet<MVDefinition> toAddDefs = new();
                    toAdd.ForEach(e => toAddDefs.Add(new(QTypes.GetEntityIdentity(e), e, false)));
                    _Tool.Selection.Add(toAddDefs, true);
                }
            }

            if (!fast)
            {
                _Tool.Selection.UpdateFull();
            }
        }

        /// <summary>
        /// Calculate what needs deselected, what needs reselected, save to live selection
        /// </summary>
        public override void Undo()
        {
            List<MVDefinition> fromSelection = _SelectionState.Definitions;
            List<MVDefinition> toSelection = _Tool.Queue.PrevAction.GetSelectionStates();
            ProcessSelectionChange(fromSelection, toSelection);
            base.Undo();
        }

        /// <summary>
        /// Calculate what needs deselected, what needs reselected, save to live selection
        /// </summary>
        public override void Redo()
        {
            List<MVDefinition> fromSelection = _Tool.Queue.PrevAction.GetSelectionStates();
            List<MVDefinition> toSelection = _SelectionState.Definitions;
            ProcessSelectionChange(fromSelection, toSelection);
            base.Redo();
        }
    }
}
