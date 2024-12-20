﻿using Game;
using MoveIt.Systems;
using QCommonLib;
using System.Reflection;

namespace MoveIt
{
    public class Mod : Game.Modding.IMod
    {
        public const string MOD_NAME = "Move It";
        public const string MOD_UI = "MoveIt";

#if IS_DEBUG
        public const bool IS_BETA = true;
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
#else
        public const bool IS_BETA = false;
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
#endif

        public static Settings.Settings Settings;

        public void OnLoad(UpdateSystem updateSystem)
        {
            if (IS_BETA) QLog.Init(true);

            Settings = new Settings.Settings(this);
            Settings.RegisterKeyBindings();
            Settings.RegisterInOptionsUI();
            Game.SceneFlow.GameManager.instance.localizationManager.AddSource("en-US", new Settings.LocaleEN(Settings));
            Colossal.IO.AssetDatabase.AssetDatabase.global.LoadSettings(nameof(MoveIt), Settings, new Settings.Settings(this));

            //updateSystem.UpdateAt<MIT_HoverSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<Tool.MIT>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<MIT_InputSystem>(SystemUpdatePhase.PreTool);
            updateSystem.UpdateAt<MIT_PostToolSystem>(SystemUpdatePhase.PostTool);
            updateSystem.UpdateBefore<MIT_RemoveOverriddenSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateBefore<MIT_VanillaOverlaySystem>(SystemUpdatePhase.Rendering);
            //updateSystem.UpdateAt<MIT_RenderSystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<Overlays.MIT_OverlaySystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<MIT_UISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<MIT_ToolTipSystem>(SystemUpdatePhase.UITooltip);
        }

        public void OnDispose()
        {
            Tool.MIT.Log?.Info(nameof(OnDispose));
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }
    }
}
