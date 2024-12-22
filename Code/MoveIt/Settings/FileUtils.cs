using Colossal.PSI.Environment;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.IO;
using System.IO.Compression;

namespace MoveIt.Settings
{
    public class FileUtils
    {
        public static string ModsFolder                 => Path.Combine(EnvPath.kUserDataPath, "Mods");
        public static string ModsDataFolder             => Path.Combine(EnvPath.kUserDataPath, "ModsData");
        public static bool GooeeModsFolderExists        => Directory.Exists(Path.Combine(ModsFolder, "Gooee"));
        public static bool GooeeModsDataFolderExists    => Directory.Exists(Path.Combine(ModsDataFolder, "Gooee"));
        public static bool GooeeBothFoldersExist        => GooeeModsFolderExists && GooeeModsDataFolderExists;
        public static bool HideGooeeWarning             => !GooeeModsFolderExists && !GooeeModsDataFolderExists;

        public static bool OpenLocalModsFolder()
        {
            if (GooeeModsFolderExists) Colossal.RemoteProcess.OpenFolder(ModsFolder);
            if (GooeeModsDataFolderExists) Colossal.RemoteProcess.OpenFolder(ModsDataFolder);
            return true;
        }

        internal static void SaveLogsToDesktop()
        {
            string timestamp        = $"{DateTime.Now:yyyy-MM-dd_HH_mm_ss}";
            string logTime          = QLoggerBase.GetFormattedTimeNow();
            string pathDesktop      = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string archiveFile      = Path.Combine(pathDesktop, $"MoveIt_Logs_{timestamp}.zip");
            string pathAppData      = EnvPath.kUserDataPath;
            string pathLogs         = Path.Combine(pathAppData, "Logs");
            string[] rootFiles      = new string[5] { "Player.log", "Player-prev.log", "MoveIt.coc", "Settings.coc", "UserState.coc" };

            MIT.Log.Info($"Saving log files from {pathAppData} at {logTime}");

            try
            {
                if (!Directory.Exists(pathLogs))
                {
                    MIT.Log.Info($"Log folder {pathLogs} not found.");
                    return;
                }

                ZipFile.CreateFromDirectory(pathLogs, archiveFile, CompressionLevel.Optimal, true);
                using ZipArchive archive = ZipFile.Open(archiveFile, ZipArchiveMode.Update);

                foreach (string file in rootFiles)
                {
                    string logFile = Path.Combine(pathAppData, file);
                    string tmpFile = Path.Combine(pathAppData, "MIT_Log.tmp");
                    if (File.Exists(logFile))
                    {
                        File.Copy(logFile, tmpFile);
                        archive.CreateEntryFromFile(tmpFile, file);
                        File.Delete(tmpFile);
                    }
                }
            }
            catch (Exception ex)
            {
                MIT.Log.Error(ex.Message);
            }
        }
    }
}
