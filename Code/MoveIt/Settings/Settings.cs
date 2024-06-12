using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using MoveIt.Tool;

namespace MoveIt.Settings
{
    [FileLocation(nameof(MoveIt))]
    [SettingsUITabOrder(tabMain)]
    [SettingsUIGroupOrder(groupGeneral)]
    [SettingsUIShowGroupName(groupGeneral)]
    public class Settings : ModSetting
    {
        public const string tabMain = "Move It";

        public const string groupGeneral = "General Settings";

        public Settings(IMod mod) : base(mod)
        { }

        [SettingsUISection(tabMain, groupGeneral)]
        public bool InvertRotation { get => InvertRotationHack; set { InvertRotationHack = value; InvertRotationHackInv = !value; } }
        [SettingsUIHidden]
        public bool InvertRotationHack { get; set; } = false;
        [SettingsUIHidden]
        public bool InvertRotationHackInv { get; set; } = false;

        [SettingsUISection(tabMain, groupGeneral)]
        public bool ShowDebugPanel { get; set; } = false;

        [SettingsUISection(tabMain, groupGeneral)]
        public bool HideMoveItIcon { get; set; } = false;

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
        [SettingsUIButton]
        public bool SaveLogsToDesktopBtn
        {
            set => SaveLogs.ToDesktop();
        }

        public override void SetDefaults()
        { }
    }
}
