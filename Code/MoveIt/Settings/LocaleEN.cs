using Colossal;
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
            var strings = new Dictionary<string, string>
            {
                { m_Settings.GetSettingsLocaleID(), Mod.MOD_NAME },
                { m_Settings.GetBindingMapLocaleID(), Mod.MOD_NAME },

                // General options
                { m_Settings.GetOptionTabLocaleID(Settings.tabMain), "Options" },
                { m_Settings.GetOptionGroupLocaleID(Settings.groupGeneral), $"General Options | {Mod.MOD_NAME} {Mod.Version}" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.InvertRotation)), "Invert Rotation" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.InvertRotation)), "Set rotation direction to vanilla CS2 relocate. If unticked, use CS1 Move It's direction." },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.ShowDebugPanel)), "Show Debug Panel" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.ShowDebugPanel)), "Show a panel with technical output to help track down problems." },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.ExtraDebugLogging)), "Extended Debug Logging" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.ExtraDebugLogging)), "Save extra data to the log files to help with debugging. Recommended for now." },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.HideMoveItIcon)), "Hide Move It Icon" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.HideMoveItIcon)), "If you're having issues with Move It crashing, try hiding the icon. Enable or disable Move It with the M key." },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.ShowDebugLines)), "Show Debug Lines" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.ShowDebugLines)), "Show debugging lines to highlight certain Move It processes." },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.SaveLogsToDesktopBtn)), "Save Logs To Desktop" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.SaveLogsToDesktopBtn)), "Save a zip file to your desktop with logs for the game and mods for submitting with bug reports. Note: includes your Windows username, Steam ID, system specifications, and timezone." },

                // Hotkeys
                { m_Settings.GetOptionTabLocaleID(Settings.tabKeys), "Hotkeys" },
                { m_Settings.GetOptionGroupLocaleID(Settings.groupHotkeys), $"Hotkeys Options | {Mod.MOD_NAME} {Mod.Version}" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_ToggleTool)), "Open Move It" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_ToggleTool)), "Enable or disable Move It" },
                { m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_ToggleTool)), "\"Open Move It\"" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_ToggleMarquee)), "Toggle Marquee Mode" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_ToggleMarquee)), "Switch between the Marquee Select and Single Select modes." },
                { m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_ToggleMarquee)), "\"Toggle Marquee\"" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_ToggleManip)), "Toggle Manipulation Mode" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_ToggleManip)), "Switch between Manipulation Mode and Normal Mode." },
                { m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_ToggleManip)), "\"Toggle Manipulation\"" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_Undo)), "Undo Action" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_Undo)), "Undo the previous Move It action." },
                { m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_Undo)), "\"Undo Action\"" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_Redo)), "Redo Action" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_Redo)), "Redo an action after undoing it." },
                { m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_Redo)), "\"Redo Action\"" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_DeselectAll)), "Deselect All Objects" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_DeselectAll)), "Deselect all selected objects." },
                { m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_DeselectAll)), "\"Deselect All\"" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_MoveDown)), "Move Objects Down" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_MoveDown)), "Move selected objects down. Each press is 1/8 of a metre. Shift+Press for 1m increments, or Control+Press for 1/64m increments." },
                { m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_MoveDown)), "\"Move Down\"" },

                { m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_MoveUp)), "Move Objects Up" },
                { m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_MoveUp)), "Move selected objects up. Each press is 1/8 of a metre. Shift+Press for 1m increments, or Control+Press for 1/64m increments." },
                { m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_MoveUp)), "\"Move Up\"" },

                //{ m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_Dejank)), "Debug: Dejank Test" },
                //{ m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_Dejank)), "Ignore this, it's just here in case you need to rebind it." },
                //{ m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_Dejank)), "\"Dejank Test\"" },

                //{ m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_DebugFreeze)), "Debug: Freeze Lines" },
                //{ m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_DebugFreeze)), "Ignore this, it's just here in case you need to rebind it." },
                //{ m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_DebugFreeze)), "\"Freeze Lines\"" },

                //{ m_Settings.GetOptionLabelLocaleID(nameof(Settings.Key_DebugClear)), "Debug: Clear Lines" },
                //{ m_Settings.GetOptionDescLocaleID(nameof(Settings.Key_DebugClear)), "Ignore this, it's just here in case you need to rebind it." },
                //{ m_Settings.GetBindingKeyLocaleID(nameof(Settings.Key_DebugClear)), "\"Clear Lines\"" },
            };

            #region Gooee Warning
            string gooeeLabel = m_Settings.GetOptionLabelLocaleID(nameof(Settings.GooeeWarning));
            string gooeeBtn = m_Settings.GetOptionLabelLocaleID(nameof(Settings.OpenLocalModsFolderBtn));
            string gooeeDesc = m_Settings.GetOptionDescLocaleID(nameof(Settings.OpenLocalModsFolderBtn));

            strings[gooeeLabel] =
                "\n**Warning: Gooee Detected**\n\n" +
                "Gooee is an abandoned mod dependency that **will** cause problems with Move It and other mods, and should be removed. However after removal parts of it may remain in your local ";

            if (FileUtils.GooeeBothFoldersExist)
            {
                strings[gooeeLabel] += 
                    "Mods and ModsData folders which must be cleaned up.\n\n" +
                    "1. Close the game.\n 2. Remove Gooee if it exists.\n 3. Delete the Gooee folder in your local Mods folder.\n 4. Delete the Gooee folder in your local ModsData folder.";
                strings[gooeeBtn] = "Open Local Mods and ModsData Folders";
                strings[gooeeDesc] = "Open the local Mods and ModsData folders - you should close the game before deleting the Gooee folders.";
            }
            else
            {
                if (FileUtils.GooeeModsFolderExists)
                {
                    strings[gooeeLabel] +=
                        "Mods folder which must be cleaned up.\n\n" +
                        "1. Close the game.\n 2. Remove Gooee if it exists.\n 3. Delete the Gooee folder in your local Mods folder.";
                    strings[gooeeBtn] = "Open Local Mods Folder";
                    strings[gooeeDesc] = "Open the local Mods folder - you should close the game before deleting the Gooee folder.";
                }
                else
                {
                    strings[gooeeLabel] +=
                        "ModsData folder which must be cleaned up.\n\n" +
                        "1. Close the game.\n 2. Remove Gooee if it exists.\n 3. Delete the Gooee folder in your local ModsData folder.";
                    strings[gooeeBtn] = "Open Local ModsData Folders";
                    strings[gooeeDesc] = "Open the local ModsData folder - you should close the game before deleting the Gooee folder.";
                }
            }
            #endregion

            return strings;
        }

        public void Unload()
        { }
    }
}
