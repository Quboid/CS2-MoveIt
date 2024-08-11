using Colossal.IO.AssetDatabase.Internal;
using MoveIt.Moveables;
using MoveIt.Selection;
using System.Collections.Generic;
using Unity.Entities;

namespace MoveIt.Actions.Select
{
    internal class SelectMarqueeAction : SelectBase
    {
        public override string Name => "SelectMarqueeAction";

        /// <summary>
        /// The selected objects before any mid-marquee additions
        /// </summary>
        private readonly SelectionBase _MarqueeStart;

        /// <summary>
        /// Constructor for SelectMarqueeAction
        /// </summary>
        /// <param name="append">Should this be added to existing selection, or should new one be made?</param>
        internal SelectMarqueeAction(bool append) : base()
        {
            _IsAppend = append;

            if (_MIT.UseMarquee)
            {
                if (_IsAppend && _MIT.Selection is not null)
                {
                    _MarqueeStart = new SelectionNormal(_MIT.Selection);
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
                _MIT.Selection.Clear();
                _MIT.Moveables.Refresh();
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
            //    MIT.Log.Debug($"AddMarq ent:{marquee.m_Entities?.Count}, prev:{marquee.m_EntitiesPrev?.Count}{(removed.Count > 0 ? $"\n    {remove}" : "")}{(added.Count > 0 ? $"\n    {add}" : "")}");
            //}

            HashSet<MVDefinition> initialSelection = new(_MarqueeStart.Definitions);
            HashSet<MVDefinition> currentSelection = _MIT.Selection.Definitions;
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
                    _MIT.Selection.Remove(toRemoveDefs, true);
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
                    _MIT.Selection.Add(toAddDefs, true);
                }
            }

            if (!fast)
            {
                _MIT.Selection.UpdateFull();
            }
        }
    }
}
