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
        {
            Marquee = MIT.m_Instance.m_MarqueeSelect;
        }

        [SettingsUISection(tabMain, groupGeneral)]
        public bool InvertRotation { get => InvertRotationHack; set { InvertRotationHack = value; InvertRotationHackInv = !value; } }
        [SettingsUISection(tabMain, groupGeneral)]
        public bool Marquee
        {
            get => MIT.m_Instance.m_MarqueeSelect;
            set => MIT.m_Instance.m_MarqueeSelect = value;
        }
        [SettingsUIHidden]
        public bool InvertRotationHack { get; set; } = false;
        [SettingsUIHidden]
        public bool InvertRotationHackInv { get; set; } = false;

        public override void SetDefaults()
        { }
    }
}
