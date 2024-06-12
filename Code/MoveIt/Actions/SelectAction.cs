using Colossal.IO.AssetDatabase.Internal;
using MoveIt.Moveables;
using MoveIt.Selection;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace MoveIt.Actions
{
    internal class SelectAction : Action
    {
        public override string Name => "SelectAction";

        /// <summary>
        /// Was the player in Manipulation Mode when action was created, including quick select (holding Alt)?
        /// </summary>
        internal readonly bool m_IsManipulating;
        /// <summary>
        /// The selected objects before any mid-marquee additions
        /// </summary>
        private SelectionBase _MarqueeStart;

        private readonly bool _IsAppend;
        private readonly bool _IsForChild;

        public SelectAction() : base()
        {
            _IsAppend = false;
            _IsForChild = false;

            //HashSet<MVDefinition> oldDefinitions = new();
            //if (_Tool.Selection is not null)
            //{
            //    oldDefinitions = _Tool.Selection.Definitions;
            //}

            //_Tool.Selection = m_IsManipulationMode ? new SelectionManip() : new SelectionNormal();
            //Deselect(oldDefinitions);
            //_Tool.Moveables.Refresh();
        }

        /// <summary>
        /// Constructor for SelectAction
        /// </summary>
        /// <param name="isManipulating">Is the player in Manipulation mode at the time of creation?</param>
        /// <param name="append">Should this be added to existing selection, or should new one be made?</param>
        /// <param name="isForChild">Is the object being added a manipulatable child?</param>
        internal SelectAction(bool isManipulating, bool append, bool isForChild = false) : base()
        {
            m_IsManipulating = isManipulating;
            _IsAppend = append;
            _IsForChild = isForChild;

            //_Tool.Selection = isManipulating ? new SelectionManip(_Tool.Selection) : new SelectionNormal(_Tool.Selection);

        }

        public override void Do()
        {
            base.Do();

            //QLog.Bundle("DO", $"{_OldSelection.Count},{_NewSelection.Count} " + Selection.DebugSelection());
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

            _Tool.Selection = m_IsManipulating ? new SelectionManip(_Tool.Selection) : new SelectionNormal(_Tool.Selection);

            //HashSet<MVDefinition> oldDefinitions = new();
            //if (_Tool.Selection is not null)
            //{
            //    oldDefinitions = _Tool.Selection.Definitions;
            //}

            if (!_IsAppend)
            {
                if (m_IsManipulating && _IsForChild)
                {
                    // New Manip selection, selecting child object so keep existing parents
                    HashSet<MVDefinition> toRemove = new();
                    toRemove = _Tool.Selection.Definitions.Where(mvd => _Tool.Moveables.GetOrCreate(mvd).IsChild).ToHashSet();
                    _Tool.Selection.Remove(toRemove);
                    Deselect(toRemove);
                }
                else
                {
                    // New selection, wipe everything
                    _Tool.Selection.Clear();
                    _Tool.Moveables.Refresh();
                }
            }
        }

        public void AddHovered(bool append)
        {
            if (_Tool.Hover.IsManipulatable != _Tool.IsManipulating) return;
            _Tool.Selection.ProcessAdd(_Tool.Hover.Definition, append);
        }

        public void AddMarqueeSelection(Input.Marquee marquee)
        {
            HashSet<MVDefinition> definitions = new(_MarqueeStart.Definitions);
            HashSet<MVDefinition> toAdd = new();
            HashSet<Entity> latest = new();
            definitions.ForEach(mvd => latest.Add(mvd.m_Entity));
            latest.UnionWith(marquee.m_Entities);

            foreach (Entity e in latest)
            {
                MVDefinition mvd = new(QTypes.GetEntityIdentity(e), e, false);
                if (!_Tool.Selection.Has(mvd))
                {
                    toAdd.Add(mvd);
                }
            }
            _Tool.Selection.Add(toAdd);

            HashSet<MVDefinition> currentSelection = _Tool.Selection.Definitions;
            foreach (MVDefinition mvd in currentSelection)
            {
                if (!latest.Contains(mvd.m_Entity))
                {
                    _Tool.Selection.Remove(mvd);
                }
            }

            HashSet<Entity> sanity = new();
            currentSelection.ForEach(mvd => sanity.Add(mvd.m_Entity));
            sanity.IntersectWith(latest);
            if (sanity.Count != _Tool.Selection.Count)
            {
                MIT.Log.Error($"UNEQUAL on marquee selection update. Sanity:{sanity.Count}, SewSel:{currentSelection.Count}");
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

        private void ProcessSelectionChange(List<MVDefinition> fromSelection, List<MVDefinition> toSelection)
        {
            IEnumerable<MVDefinition> deselected = fromSelection.Except(toSelection);
            IEnumerable<MVDefinition> reselected = toSelection.Except(fromSelection);

            SelectionState newSelectionStates = new(_Tool.m_IsManipulateMode, toSelection);

            MIT.Log.Debug($"SelectAction.ProcessSelectionChange" +
                $"\n FromSelection: {MIT.DebugDefinitions(fromSelection)}" +
                $"\n   ToSelection: {MIT.DebugDefinitions(toSelection)}" +
                $"\n      Deselect: {MIT.DebugDefinitions(deselected)}" +
                $"\n      Reselect: {MIT.DebugDefinitions(reselected)}" +
                $"\n         Final: {MIT.DebugDefinitions(newSelectionStates.Definitions)}");

            _Tool.Selection = m_IsManipulating ? new SelectionManip(newSelectionStates) : new SelectionNormal(newSelectionStates);
            _Tool.Selection.Refresh();

            deselected.ForEach(mvd => _Tool.Moveables.GetOrCreate(mvd).OnDeselect());
            reselected.ForEach(mvd => _Tool.Moveables.GetOrCreate(mvd).OnSelect());
        }
    }
}
