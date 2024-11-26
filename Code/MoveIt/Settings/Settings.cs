using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using MoveIt.Input;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Settings
{
    [FileLocation(nameof(MoveIt))]
    [SettingsUITabOrder(tabMain, tabKeys)]
    [SettingsUIGroupOrder(groupGeneral, groupHotkeys, groupMovement, groupToolbox)]
    [SettingsUIShowGroupName(groupGeneral, groupHotkeys, groupMovement, groupToolbox)]
    [SettingsUIMouseAction(QInputSystem.MOUSE_APPLY,    ActionType.Button, false, false, new string[] { "MoveIt_Input" })]
    [SettingsUIMouseAction(QInputSystem.MOUSE_CANCEL,   ActionType.Button, false, false, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_MOVEDOWN,             ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_MOVEUP,               ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_MOVEDOWN2,            ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_MOVEUP2,              ActionType.Button, false, false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_UNDO,                 ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_REDO,                 ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_DESELECTALL,          ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_DEBUGFREEZE,          ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_TOGGLEMARQUEE,        ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_TOGGLEMANIP,          ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_FILTERSTOGGLE,        ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_TB_TERRAINHEIGHT,     ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_TB_OBJECTHEIGHT,      ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_TB_OBJANGLEINDIV,     ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    [SettingsUIKeyboardActionAttribute(Inputs.KEY_TB_OBJANGLEGROUP,     ActionType.Button, true,  false, Mode.DigitalNormalized, new string[] { "MoveIt_Input" })]
    public class Settings : ModSetting
    {
        public const string tabMain = "tabMain";
        public const string tabKeys = "tabKeys";

        public const string groupGeneral    = "groupGeneral";
        public const string groupHotkeys    = "groupHotkeys";
        public const string groupMovement   = "groupMovement";
        public const string groupToolbox    = "groupToolbox";

        public Settings(IMod mod) : base(mod)
        { }

        // General options
        [SettingsUISection(tabMain, groupGeneral)]
        public bool InvertRotation { get; set; } = false;

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
        public bool ShowDebugPanel { get; set; } = false;

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
            // ReSharper disable once ValueParameterNotUsed
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
            // ReSharper disable once ValueParameterNotUsed
            set => FileUtils.OpenLocalModsFolder();
        }

        [SettingsUISection(tabMain, groupGeneral)]
        [SettingsUIHidden]
        public bool HasShownMConflictPanel { get; set; } = false;

        // Hotkeys
        #region groupHotkeys
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

        [SettingsUIKeyboardBinding(BindingKeyboard.F, Inputs.KEY_FILTERSTOGGLE, alt: true)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        public ProxyBinding Key_FiltersToggle { get; set; }
        #endregion

        #region groupMovement
        [SettingsUIKeyboardBinding(BindingKeyboard.PageDown, Inputs.KEY_MOVEDOWN)]
        [SettingsUISection(tabKeys, groupMovement)]
        public ProxyBinding Key_MoveDown { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Numpad3, Inputs.KEY_MOVEDOWN2)]
        [SettingsUISection(tabKeys, groupMovement)]
        public ProxyBinding Key_MoveDown2 { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.PageUp, Inputs.KEY_MOVEUP)]
        [SettingsUISection(tabKeys, groupMovement)]
        public ProxyBinding Key_MoveUp { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.Numpad9, Inputs.KEY_MOVEUP2)]
        [SettingsUISection(tabKeys, groupMovement)]
        public ProxyBinding Key_MoveUp2 { get; set; }
        #endregion

        #region Debug keys
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
        #endregion

        #region groupToolbox
        [SettingsUIKeyboardBinding(BindingKeyboard.G, Inputs.KEY_TB_TERRAINHEIGHT, ctrl: true)]
        [SettingsUISection(tabKeys, groupToolbox)]
        public ProxyBinding Key_TB_TerrainHeight { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.H, Inputs.KEY_TB_OBJECTHEIGHT, ctrl: true)]
        [SettingsUISection(tabKeys, groupToolbox)]
        public ProxyBinding Key_TB_ObjectHeight { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.A, Inputs.KEY_TB_OBJANGLEGROUP, alt: true)]
        [SettingsUISection(tabKeys, groupToolbox)]
        public ProxyBinding Key_TB_ObjAngleGroup { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.A, Inputs.KEY_TB_OBJANGLEINDIV, shift: true)]
        [SettingsUISection(tabKeys, groupToolbox)]
        public ProxyBinding Key_TB_ObjAngleIndiv { get; set; }
        #endregion

        #region Mouse imitators
        [SettingsUIMouseBinding(QInputSystem.MOUSE_APPLY)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        [SettingsUIBindingMimic(InputManager.kToolMap, "Apply")]
        [SettingsUIHidden]
        public ProxyBinding ApplyMimic { get; set; }

        [SettingsUIMouseBinding(QInputSystem.MOUSE_CANCEL)]
        [SettingsUISection(tabKeys, groupHotkeys)]
        [SettingsUIBindingMimic(InputManager.kToolMap, "Cancel")]
        [SettingsUIHidden]
        public ProxyBinding CancelMimic { get; set; }
        #endregion


        public override void SetDefaults()
        { }
    }
}
