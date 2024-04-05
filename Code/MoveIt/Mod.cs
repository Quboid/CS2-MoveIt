using Game;
using MoveIt.Systems;
using System.Reflection;

namespace MoveIt
{
    public class Mod : Game.Modding.IMod
    {
        public const string MOD_NAME = "Move It";
        public const string MOD_UI = "MoveIt";

        public static Colossal.Logging.ILog log = Colossal.Logging.LogManager.GetLogger($"{nameof(MoveIt)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Settings.Settings Settings;

        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        public void OnLoad(UpdateSystem updateSystem)
        {
            // Comment out this to avoid debug log being created
            QCommonLib.QLog.Init(true);
            updateSystem.UpdateAt<Tool.MIT>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateBefore<MIT_RemoveOverriddenSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateBefore<MIT_VanillaOverlaySystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<MIT_RenderSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<MIT_OverlaySystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<MIT_UISystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAt<MIT_ToolTipSystem>(SystemUpdatePhase.UITooltip);

            //log.Info(nameof(OnLoad));

            //if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            //    log.Info($"Current mod asset at {asset.path}");

            Settings = new Settings.Settings(this);
            Settings.RegisterInOptionsUI();
            Game.SceneFlow.GameManager.instance.localizationManager.AddSource("en-US", new Settings.LocaleEN(Settings));
            Colossal.IO.AssetDatabase.AssetDatabase.global.LoadSettings(nameof(MoveIt), Settings, new Settings.Settings(this));
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }
    }
}
