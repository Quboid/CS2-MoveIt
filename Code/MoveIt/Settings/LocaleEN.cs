using Colossal;
using Game.Settings;
using System.Collections.Generic;

namespace MoveIt.Settings
{
    public class LocaleEN : IDictionarySource
    {
        private readonly Settings m_Settings;

        public LocaleEN(Settings settings)
        {
            m_Settings = settings;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            
            return new Dictionary<string, string>
            {
                { m_Settings.GetSettingsLocaleID(), "Move It" },
                { m_Settings.GetOptionTabLocaleID(Settings.tabMain), "Move It Options" },

                { m_Settings.GetOptionGroupLocaleID(Settings.groupGeneral), $"General Options | {Mod.MOD_NAME} {Mod.Version}" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.InvertRotation)), "Invert Rotation" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.InvertRotation)), "Set rotation direction to vanilla CS2 relocate. If unticked, use CS1 Move It's direction." },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.ShowDebugPanel)), "Show Debug Panel" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.ShowDebugPanel)), "Show a panel with technical output to help track down problems." },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.HideMoveItIcon)), "Hide Move It Icon" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.HideMoveItIcon)), "If you're having issues with Move It crashing, try hiding the icon. Enable or disable Move It with the M key." },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.ExtraDebugLogging)), "Extended Debug Logging" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.ExtraDebugLogging)), "If you're having issues with Move It crashing, try hiding the icon. Enable or disable Move It with the M key." },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.SaveLogsToDesktopBtn)), "Save Logs To Desktop" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.SaveLogsToDesktopBtn)), "Save a zip file to your desktop with logs for the game and mods for submitting with bug reports. Note: includes your Windows username, Steam ID, system specifications, and timezone." },
            };
        }

        public void Unload()
        { }
    }
}
