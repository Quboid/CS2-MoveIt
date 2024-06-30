using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using MoveIt.Tool;
using UnityEngine.InputSystem;

namespace MoveIt.Settings
{
    [FileLocation(nameof(MoveIt))]
    [SettingsUITabOrder(tabMain, tabKeys)]
    [SettingsUIGroupOrder(groupGeneral, groupHotkeys)]
    [SettingsUIShowGroupName(groupGeneral, groupHotkeys)]
    [SettingsUIKeyboardActionAttribute(Systems.MIT_HotkeySystem.KEY_MOVEDOWN,   ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Keybind" })]
    [SettingsUIKeyboardActionAttribute(Systems.MIT_HotkeySystem.KEY_MOVEUP,     ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Keybind" } )]
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
        [SettingsUIKeyboardBinding(Key.M, Systems.MIT_HotkeySystem.KEY_TOGGLETOOL)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_ToggleTool { get; set; }

        [SettingsUIKeyboardBinding(Key.M, Systems.MIT_HotkeySystem.KEY_TOGGLEMARQUEE, ctrl:true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_ToggleMarquee { get; set; }

        [SettingsUIKeyboardBinding(Key.M, Systems.MIT_HotkeySystem.KEY_TOGGLEMANIP, alt: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_ToggleManip { get; set; }

        [SettingsUIKeyboardBinding(Key.Z, Systems.MIT_HotkeySystem.KEY_UNDO, ctrl: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_Undo { get; set; }

        [SettingsUIKeyboardBinding(Key.Z, Systems.MIT_HotkeySystem.KEY_REDO, ctrl: true, shift: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_Redo { get; set; }

        [SettingsUIKeyboardBinding(Key.D, Systems.MIT_HotkeySystem.KEY_DESELECTALL, alt: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_DeselectAll { get; set; }

        [SettingsUIKeyboardBinding(Key.PageDown, Systems.MIT_HotkeySystem.KEY_MOVEDOWN)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_MoveDown { get; set; }

        [SettingsUIKeyboardBinding(Key.PageUp, Systems.MIT_HotkeySystem.KEY_MOVEUP)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_MoveUp { get; set; }

        // Hidden test hotkeys
        //[SettingsUIKeyboardBinding(Key.L, Systems.MIT_HotkeySystem.KEY_DEJANK, ctrl: true)]
        //[SettingsUISection(tabKeys, groupHotkeys)]
        //[SettingsUIAdvanced]
        //public ProxyBinding Key_Dejank { get; set; }

        //[SettingsUIKeyboardBinding(Key.D, Systems.MIT_HotkeySystem.KEY_DEBUGFREEZE, ctrl: true)]
        //[SettingsUISection(tabKeys, groupHotkeys)]
        //[SettingsUIAdvanced]
        //public ProxyBinding Key_DebugFreeze { get; set; }

        //[SettingsUIKeyboardBinding(Key.D, Systems.MIT_HotkeySystem.KEY_DEBUGCLEAR, ctrl: true, shift: true)]
        //[SettingsUISection(tabKeys, groupHotkeys)]
        //[SettingsUIAdvanced]
        //public ProxyBinding Key_DebugClear { get; set; }


        public override void SetDefaults()
        { }
    }
}
