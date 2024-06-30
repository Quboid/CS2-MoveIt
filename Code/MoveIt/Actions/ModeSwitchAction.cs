using MoveIt.Moveables;
using MoveIt.Selection;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;

namespace MoveIt.Actions
{
    internal class ModeSwitchAction : Action
    {
        public override string Name => "ModeSwitch";

        /// <summary>
        /// The selected moveables' definitions when the action was created
        /// </summary>
        protected SelectionState _InitialSelectionState;

        public ModeSwitchAction()
        {
            _InitialSelectionState = SelectionState.SelectionToState(_Tool.m_IsManipulateMode, _Tool.Selection.Definitions);
        }

        /// <summary>
        /// Toggle Manipulation Mode
        /// Maintain the selection for this mode by searching back in the queue for the last switch and use it's initial (pre-mode switch) selection
        /// If no last switch found, selection is empty.
        /// </summary>
        public override void Do()
        {
            base.Do();
            List<MVDefinition> fromSelection = _InitialSelectionState.Definitions;
            ModeSwitchAction prev = _Tool.Queue.GetPrevious<ModeSwitchAction>();// GetPrecedingModeSwitchActionFromQueue(false);
            List<MVDefinition> toSelection = prev is null ? new() : prev.GetInitialSelectionStates();
            ToggleMode(fromSelection, toSelection, !m_IsManipulationMode);
            //MIT.Log.Debug($"MSA.Do is:{_Tool.m_IsManipulateMode} |{prev}| from:{fromSelection.Count}, to:{toSelection.Count}");
        }

        /// <summary>
        /// This action will be restored to the current one
        /// Runs directly after Archive(), before Undo() and Redo(), not on a newly Pushed action
        /// </summary>
        /// <param name="toolAction">The tool action at the time of unarchiving</param>
        /// <param name="idx">This action's queue index</param>
        public override void Unarchive(ToolActions toolAction, int idx)
        {
            _SelectionState.CleanDefinitions();
            _InitialSelectionState.CleanDefinitions();
            //MIT.Log.Debug($"MSA.Unarchive {idx}:{_Tool.Queue.Current.Name} ToolAction:{toolAction}");
        }

        /// <summary>
        /// Calculate what needs deselected, what needs reselected, save to live selection
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            List<MVDefinition> fromSelection = _SelectionState.Definitions;
            List<MVDefinition> toSelection = _InitialSelectionState.Definitions;
            ToggleMode(fromSelection, toSelection, m_IsManipulationMode);
        }

        /// <summary>
        /// Calculate what needs deselected, what needs reselected, save to live selection
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            List<MVDefinition> fromSelection = _InitialSelectionState.Definitions;
            List<MVDefinition> toSelection = _SelectionState.Definitions;
            ToggleMode(fromSelection, toSelection, !m_IsManipulationMode);
        }

        private void ToggleMode(List<MVDefinition> fromSelection, List<MVDefinition> toSelection, bool willBeManipulating)
        {
            _Tool.m_IsManipulateMode = willBeManipulating;

            ProcessModeSelectionChange(fromSelection, toSelection);

            _Tool.Moveables.UpdateAllControlPoints();
            _Tool.Moveables.UpdateAllOverlays();
            _Tool.Selection.CalculateCenter();

            _Tool.SetModesTooltip();
        }

        private void ProcessModeSelectionChange(List<MVDefinition> fromSelection, List<MVDefinition> toSelection)
        {
            SelectionState newSelectionStates = new(_Tool.m_IsManipulateMode, toSelection);

            MIT.Log.Debug($"ModeSwitchAction.ProcessSelectionChange" +
                $"\n FromSelection: {MIT.DebugDefinitions(fromSelection)}" +
                $"\n   ToSelection: {MIT.DebugDefinitions(toSelection)}" +
                $"\n         Final: {MIT.DebugDefinitions(newSelectionStates.Definitions)}");

            try
            {
                _Tool.Selection = _Tool.m_IsManipulateMode ? new SelectionManip(newSelectionStates) : new SelectionNormal(newSelectionStates);
                _Tool.Selection.Refresh();
            }
            catch (System.Exception ex)
            {
                MIT.Log.Error($"Failed ProcessSelectionChange toSel:{toSelection.Count}, newSelStates:{newSelectionStates.Count}\n" + ex);

                _Tool.Selection = _Tool.m_IsManipulateMode ? new SelectionManip() : new SelectionNormal();
                _Tool.Selection.Refresh();
            }
        }

        /// <summary>
        /// Get this action's select states without modification
        /// </summary>
        public virtual List<MVDefinition> GetInitialSelectionStates() => _InitialSelectionState.Definitions;
    }
}
