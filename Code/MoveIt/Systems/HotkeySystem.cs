using MoveIt.Actions;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Systems
{
    internal partial class MIT_HotkeySystem : QKey_System
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        public const string KEY_TOGGLETOOL      = "Key_ToggleTool";
        public const string KEY_TOGGLEMARQUEE   = "Key_ToggleMarquee";
        public const string KEY_TOGGLEMANIP     = "Key_ToggleManip";
        public const string KEY_UNDO            = "Key_Undo";
        public const string KEY_REDO            = "Key_Redo";
        public const string KEY_DESELECTALL     = "Key_DeselectAll";
        public const string KEY_DEBUGFREEZE     = "Key_DebugFreeze";
        public const string KEY_DEBUGCLEAR      = "Key_DebugClear";
        public const string KEY_DEJANK          = "Key_Dejank";

        public const string KEY_MOVEDOWN        = "Key_MoveDown";
        public const string KEY_MOVEUP          = "Key_MoveUp";

        protected override void OnCreate()
        {
            base.OnCreate();

            RegisterBinding(new(
                action: Mod.Settings.GetAction(KEY_TOGGLETOOL),
                context: QKey_Contexts.Always,
                trigger: DoToolToggle
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(KEY_TOGGLEMARQUEE),
                context: QKey_Contexts.ToolEnabled,
                trigger: _Tool.ToggleSelectionMode
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(KEY_TOGGLEMANIP),
                context: QKey_Contexts.ToolEnabled,
                trigger: _Tool.ToggleManipulationMode
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(KEY_UNDO),
                context: QKey_Contexts.ToolEnabled,
                trigger: DoUndo
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(KEY_REDO),
                context: QKey_Contexts.ToolEnabled,
                trigger: DoRedo
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(KEY_DESELECTALL),
                context: QKey_Contexts.ToolEnabled,
                trigger: DoDeselectAll
            ));

            //RegisterBinding(new(
            //    action: Mod.Settings.GetAction(KEY_DEBUGFREEZE),
            //    context: QKey_Contexts.ToolEnabled,
            //    trigger: DoDebugFreeze
            //));

            //RegisterBinding(new(
            //    action: Mod.Settings.GetAction(KEY_DEBUGCLEAR),
            //    context: QKey_Contexts.ToolEnabled,
            //    trigger: DoDebugClear
            //));

            //RegisterBinding(new(
            //    action: Mod.Settings.GetAction(KEY_DEJANK),
            //    context: QKey_Contexts.ToolEnabled,
            //    trigger: _Tool.DejankNodes
            //));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(KEY_MOVEDOWN),
                context: QKey_Contexts.ToolEnabled,
                trigger: null,
                isPassive: true
            ));

            RegisterBinding(new(
                action: Mod.Settings.GetAction(KEY_MOVEUP),
                context: QKey_Contexts.ToolEnabled,
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
