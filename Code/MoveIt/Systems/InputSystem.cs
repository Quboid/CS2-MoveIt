using MoveIt.Actions;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Systems
{
    internal partial class MIT_InputSystem : QInputSystem
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        protected override void OnCreate()
        {
            base.OnCreate();

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_TOGGLETOOL),
                context: QInput_Contexts.Always,
                trigger: DoToolToggle
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_TOGGLEMARQUEE),
                context: QInput_Contexts.ToolEnabled,
                trigger: _Tool.ToggleSelectionMode
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_TOGGLEMANIP),
                context: QInput_Contexts.ToolEnabled,
                trigger: _Tool.ToggleManipulationMode
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_UNDO),
                context: QInput_Contexts.ToolEnabled,
                trigger: DoUndo
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_REDO),
                context: QInput_Contexts.ToolEnabled,
                trigger: DoRedo
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_DESELECTALL),
                context: QInput_Contexts.ToolEnabled,
                trigger: DoDeselectAll
            ));

            //RegisterBinding(new(
            //    action: Mod.Settings.GetAction(Inputs.KEY_DEBUGFREEZE),
            //    context: QKey_Contexts.ToolEnabled,
            //    trigger: DoDebugFreeze
            //));

            //RegisterBinding(new(
            //    action: Mod.Settings.GetAction(Inputs.KEY_DEBUGCLEAR),
            //    context: QKey_Contexts.ToolEnabled,
            //    trigger: DoDebugClear
            //));

            //RegisterBinding(new(
            //    action: Mod.Settings.GetAction(Inputs.KEY_DEJANK),
            //    context: QKey_Contexts.ToolEnabled,
            //    trigger: _Tool.DejankNodes
            //));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_MOVEDOWN),
                context: QInput_Contexts.ToolEnabled,
                trigger: null,
                isPassive: true
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_MOVEUP),
                context: QInput_Contexts.ToolEnabled,
                trigger: null,
                isPassive: true
            ));

            //DebugDumpAllBindings();
        }

        private void DoToolToggle()
        {
            //QLog.Debug($"Key: {KEY_TOGGLETOOL}");
            _Tool.RequestToggle();
        }

        private void DoUndo()
        {
            //QLog.Debug($"Key: {KEY_UNDO}");
            if (_Tool.ToolState == ToolStates.Default)
            {
                _Tool.ToolAction = ToolActions.Undo;
            }
        }

        private void DoRedo()
        {
            //QLog.Debug($"Key: {KEY_REDO}");
            if (_Tool.ToolState == ToolStates.Default)
            {
                _Tool.ToolAction = ToolActions.Redo;
            }
        }

        private void DoDeselectAll()
        {
            //QLog.Debug($"Key: {KEY_DESELECTALL}");
            _Tool.Queue.Push(new DeselectAllAction());
            _Tool.ToolAction = ToolActions.Do;
            //_Tool.Queue.Do();
        }

        //private void DoDebugFreeze()
        //{
        //    //QLog.Debug($"Key: {KEY_DEBUGFREEZE}");
        //    _Tool.m_OverlaySystem.m_DebugFreeze = !_Tool.m_OverlaySystem.m_DebugFreeze;
        //}

        //private void DoDebugClear()
        //{
        //    //QLog.Debug($"Key: {KEY_DEBUGCLEAR}");
        //    _Tool.m_RenderSystem.Clear();
        //}
    }
}
