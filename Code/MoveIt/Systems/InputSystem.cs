using MoveIt.Actions;
using MoveIt.Actions.Select;
using MoveIt.Actions.Toolbox;
using MoveIt.Input;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Systems
{
    internal partial class MIT_InputSystem : QInputSystem
    {
        protected static readonly MIT _MIT = MIT.m_Instance;

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
                trigger: _MIT.ToggleSelectionMode
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_TOGGLEMANIP),
                context: QInput_Contexts.ToolEnabled,
                trigger: _MIT.ToggleManipulationMode
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

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_FILTERSTOGGLE),
                context: QInput_Contexts.ToolEnabled,
                trigger: DoFiltersToggle
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_DEBUGFREEZE),
                context: QInput_Contexts.ToolEnabled,
                trigger: DoDebugFreeze
            ));

            //RegisterBinding(new(
            //    action: Mod.Settings.GetAction(Inputs.KEY_DEBUGCLEAR),
            //    context: QInput_Contexts.ToolEnabled,
            //    trigger: DoDebugClear
            //));

            //RegisterBinding(new(
            //    action: Mod.Settings.GetAction(Inputs.KEY_DEJANK),
            //    context: QInput_Contexts.ToolEnabled,
            //    trigger: _MIT.DejankNodes
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

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_MOVEDOWN2),
                context: QInput_Contexts.ToolEnabled,
                trigger: null,
                isPassive: true
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(Inputs.KEY_MOVEUP2),
                context: QInput_Contexts.ToolEnabled,
                trigger: null,
                isPassive: true
            ));

            // Toolbox

            foreach (ToolBoxTool tool in Managers.ToolboxManager.ToolList)
            {
                RegisterBinding(new(
                    action: Mod.Settings.GetAction(tool.m_Hotkey),
                    context: QInput_Contexts.ToolEnabled,
                    trigger: () => ToolboxActivate(tool.m_Id)
                ));
            }

            //DebugDumpAllBindings();
        }

        private static void DoToolToggle()
        {
            //MIT.Log.Debug($"Key: {Inputs.KEY_TOGGLETOOL}");
            _MIT.RequestToggle();
        }

        private static void DoUndo()
        {
            //MIT.Log.Debug($"Key: {Inputs.KEY_UNDO}");
            if (_MIT.MITState == MITStates.Default)
            {
                Action.Phase = Phases.Undo;
            }
        }

        private static void DoRedo()
        {
            //MIT.Log.Debug($"Key: {Inputs.KEY_REDO}");
            if (_MIT.MITState == MITStates.Default)
            {
                Action.Phase = Phases.Redo;
            }
        }
        
        private static void DoDeselectAll()
        {
            //MIT.Log.Debug($"Key: {Inputs.KEY_DESELECTALL}");
            _MIT.Queue.Push(new DeselectAllAction());
            Action.Phase = Phases.Do;
        }

        private static void DoFiltersToggle()
        {
            //MIT.Log.Debug($"Key: {Inputs.KEY_FILTERSTOGGLE}");
            _MIT.m_UISystem.ToggleFiltersPanel();
        }

        private static void DoDebugFreeze()
        {
            _MIT.m_OverlaySystem.DebugFreeze = !_MIT.m_OverlaySystem.DebugFreeze;
            MIT.Log.Debug($"Key: {Inputs.KEY_DEBUGFREEZE} ({_MIT.m_OverlaySystem.DebugFreeze})");
        }

        private static void ToolboxActivate(string id)
        {
            _MIT.ToolboxManager.Activate(id);
        }

        //private void DoDebugClear()
        //{
        //    //MIT.Log.Debug($"Key: {Inputs.KEY_DEBUGCLEAR}");
        //    _MIT.m_RenderSystem.Clear();
        //}
    }
}
