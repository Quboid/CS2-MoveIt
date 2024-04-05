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

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.Marquee)), "Marquee Selection" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.Marquee)), "Select objects by click-and-dragging a selection box. Hot-key: Control+M" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.InvertRotation)), "Invert Rotation" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.InvertRotation)), "Set rotation direction to vanilla CS2 relocate. If unticked, use CS1 Move It's direction." },
            };
        }

        public void Unload()
        { }
    }
}
