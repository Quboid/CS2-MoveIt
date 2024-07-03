using MoveIt.Moveables;
using MoveIt.Selection;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.Actions
{
    internal class SelectAction : Action
    {
        public override string Name => "SelectAction";

        /// <summary>
        /// Was the player in Manipulation Mode when action was created, including quick select (holding Alt)?
        /// </summary>
        internal readonly bool m_IsManipulating;

        private readonly bool _IsAppend;
        private readonly bool _IsForChild;

        public SelectAction() : base()
        {
            _IsAppend = false;
            _IsForChild = false;
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
        }

        public override void Do()
        {
            base.Do();

            _Tool.Selection = m_IsManipulating ? new SelectionManip(_Tool.Selection) : new SelectionNormal(_Tool.Selection);

            if (!_IsAppend)
            {
                if (m_IsManipulating && _IsForChild)
                {
                    // New Manip selection, selecting child object so keep existing parents
                    HashSet<MVDefinition> toRemove = new();
                    toRemove = _Tool.Selection.Definitions.Where(mvd => _Tool.Moveables.GetOrCreate(mvd).IsManipChild).ToHashSet();
                    _Tool.Selection.Remove(toRemove, false);
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
