using MoveIt.Moveables;
using MoveIt.Selection;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.Actions.Select
{
    internal class DeselectAllAction : Action
    {
        public override string Name => "DeselectAllAction";

        /// <summary>
        /// The Normal selected moveables' definitions when the action was created
        /// </summary>
        protected SelectionState _InitialStateNormal;
        /// <summary>
        /// The Manipulation selected moveables' definitions when the action was created
        /// </summary>
        protected SelectionState _InitialStateManip;

        public override void Do()
        {
            base.Do();

            List<MVDefinition> normal = m_IsManipulationMode ? GetModeSwitchList() : GetPreviousList();
            List<MVDefinition> manip = m_IsManipulationMode ? GetPreviousList() : GetModeSwitchList();

            DeselectAll(normal, manip);

            _InitialStateNormal = new(false, normal);
            _InitialStateManip = new(true, manip);
        }

        /// <summary>
        /// Calculate what needs deselected, what needs reselected, save to live selection
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            _MIT.Selection = _MIT.m_IsManipulateMode ? new SelectionManip(_InitialStateManip) : new SelectionNormal(_InitialStateNormal);
        }

        /// <summary>
        /// Calculate what needs deselected, what needs reselected, save to live selection
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            DeselectAll(_InitialStateNormal.Definitions, _InitialStateManip.Definitions);
        }

        private void DeselectAll(List<MVDefinition> normal, List<MVDefinition> manip)
        {
            Deselect(normal.Union(manip));

            // Set fresh action
            _MIT.Selection = _MIT.m_IsManipulateMode ? new SelectionManip() : new SelectionNormal();

            // Cleanup
            _MIT.Moveables.Clear();
        }

        private List<MVDefinition> GetModeSwitchList()
        {
            ModeSwitchAction modeSwitch = _MIT.Queue.GetPrevious<ModeSwitchAction>();
            List<MVDefinition> result = modeSwitch is null ? new() : modeSwitch.GetInitialSelectionStates();

            return result;
        }

        private List<MVDefinition> GetPreviousList()
        {
            Action previous = _MIT.Queue.PrevAction;
            List<MVDefinition> result = previous is null ? new() : previous.GetSelectionStates();

            return result;
        }
    }
}
