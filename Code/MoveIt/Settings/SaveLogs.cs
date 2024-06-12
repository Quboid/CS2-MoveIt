using Colossal.PSI.Environment;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.IO;
using System.IO.Compression;

namespace MoveIt.Settings
{
    internal class SaveLogs
    {
        internal static void ToDesktop()
        {
            string timestamp        = $"{DateTime.Now:yyyy-MM-dd_hh_mm_ss}";
            string logTime          = QLoggerBase.GetFormattedTimeNow();
            string pathDesktop      = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string archiveFile      = Path.Combine(pathDesktop, $"MoveIt_Logs_{timestamp}.zip");
            string pathAppData      = EnvPath.kUserDataPath;
            string pathLogs         = Path.Combine(pathAppData, "Logs");
            string[] rootFiles      = new string[2] { "Player.log", "Player-prev.log" };

            MIT.Log.Info($"Saving log files from {pathAppData} at {logTime}");

            try
            {
                if (!Directory.Exists(pathLogs))
                {
                    MIT.Log.Info($"Log folder {pathLogs} not found.");
                    return;
                }

                ZipFile.CreateFromDirectory(pathLogs, archiveFile, CompressionLevel.Optimal, true);
                using var archive = ZipFile.Open(archiveFile, ZipArchiveMode.Update);

                for (int i = 0; i < rootFiles.Length; i++)
                {
                    string logFile = Path.Combine(pathAppData, rootFiles[i]);
                    string tmpFile = Path.Combine(pathAppData, "MIT_Log.tmp");
                    if (File.Exists(logFile))
                    {
                        File.Copy(logFile, tmpFile);
                        archive.CreateEntryFromFile(tmpFile, rootFiles[i]);
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
