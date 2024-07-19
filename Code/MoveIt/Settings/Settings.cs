using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Settings
{
    [FileLocation(nameof(MoveIt))]
    [SettingsUITabOrder(tabMain, tabKeys)]
    [SettingsUIGroupOrder(groupGeneral, groupHotkeys)]
    [SettingsUIShowGroupName(groupGeneral, groupHotkeys)]
    [SettingsUIMouseAction(QInputSystem.MOUSE_APPLY,            ActionType.Button, false, false, new string[] { "MoveIt_Input" })]
    [SettingsUIMouseAction(QInputSystem.MOUSE_CANCEL,           ActionType.Button, false, false, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_MOVEDOWN,     ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_MOVEUP,       ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" } )]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_MOVEDOWN2,    ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_MOVEUP2,      ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    public class Settings : ModSetting
    {
        public const string tabMain = "tabMain";
        public const string tabKeys = "tabKeys";

        public const string groupGeneral = "groupGeneral";
        public const string groupHotkeys = "groupHotkeys";

        public Settings(IMod mod) : base(mod)
        { }

        // General options
        [SettingsUISection(tabMain, groupGeneral)]
        public bool InvertRotation { get; set; } = false;

        [SettingsUISection(tabMain, groupGeneral)]
        public bool ShowDebugPanel { get; set; } = false;

        [SettingsUISection(tabMain, groupGeneral)]
        public bool ExtraDebugLogging
        {
            get => _ExtraDebugLogging;
            set
            {
                _ExtraDebugLogging = value;
                if (MIT.Log is not null) MIT.Log.IsDebug = value;
            }
        }
        private bool _ExtraDebugLogging = true;

        [SettingsUISection(tabMain, groupGeneral)]
        [SettingsUIAdvanced]
        public bool HideMoveItIcon { get; set; } = false;

        [SettingsUISection(tabMain, groupGeneral)]
        [SettingsUIAdvanced]
        public bool ShowDebugLines { get; set; } = false;

        [SettingsUISection(tabMain, groupGeneral)]
        [SettingsUIButton]
        public bool SaveLogsToDesktopBtn
        {
            set => FileUtils.SaveLogsToDesktop();
        }

        [SettingsUISection(tabMain, groupGeneral)]
        [SettingsUIMultilineText]
        [SettingsUIHideByConditionAttribute(typeof(FileUtils), nameof(FileUtils.HideGooeeWarning))]
        public string GooeeWarning => string.Empty;

        [SettingsUISection(tabMain, groupGeneral)]
        [SettingsUIButton]
        [SettingsUIHideByConditionAttribute(typeof(FileUtils), nameof(FileUtils.HideGooeeWarning))]
        public bool OpenLocalModsFolderBtn
        {
            set => FileUtils.OpenLocalModsFolder();
        }

        [SettingsUISection(tabMain, groupGeneral)]
        [SettingsUIHidden]
        public bool HasShownMConflictPanel { get; set; } = false;

        // Hotkeys
        [SettingsUIKeyboardBinding(BindingKeyboard.M, Inputs.KEY_TOGGLETOOL)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_ToggleTool { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.M, Inputs.KEY_TOGGLEMARQUEE, ctrl:true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_ToggleMarquee { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.M, Inputs.KEY_TOGGLEMANIP, alt: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_ToggleManip { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Z, Inputs.KEY_UNDO, ctrl: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_Undo { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Z, Inputs.KEY_REDO, ctrl: true, shift: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_Redo { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.D, Inputs.KEY_DESELECTALL, alt: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_DeselectAll { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.PageDown, Inputs.KEY_MOVEDOWN)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_MoveDown { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Numpad3, Inputs.KEY_MOVEDOWN2)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_MoveDown2 { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.PageUp, Inputs.KEY_MOVEUP)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_MoveUp { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Numpad9, Inputs.KEY_MOVEUP2)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_MoveUp2 { get; set; }

        // Hidden test hotkeys
        //[SettingsUIKeyboardBinding(BindingKeyboard.L, Inputs.KEY_DEJANK, ctrl: true)]
        //[SettingsUISection(tabKeys, groupHotkeys)]
        //[SettingsUIAdvanced]
        //public ProxyBinding Key_Dejank { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.D, Inputs.KEY_DEBUGFREEZE, ctrl: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        [SettingsUIAdvanced]
        public ProxyBinding Key_DebugFreeze { get; set; }

        //[SettingsUIKeyboardBinding(BindingKeyboard.D, Inputs.KEY_DEBUGCLEAR, ctrl: true, shift: true)]
        //[SettingsUISection(tabKeys, groupHotkeys)]
        //[SettingsUIAdvanced]
        //public ProxyBinding Key_DebugClear { get; set; }

        // Mouse buttons
        [SettingsUIMouseBinding(QInputSystem.MOUSE_APPLY)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        [SettingsUIHidden]
        public ProxyBinding ApplyMimic { get; set; }

        [SettingsUIMouseBinding(QInputSystem.MOUSE_CANCEL)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        [SettingsUIHidden]
        public ProxyBinding CancelMimic { get; set; }


        public override void SetDefaults()
        { }
    }
}
