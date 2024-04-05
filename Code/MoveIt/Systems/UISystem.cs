using Colossal.UI.Binding;
using Game.UI;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Systems
{
    internal partial class MIT_UISystem : UISystemBase
    {
        protected readonly MIT _Tool = MIT.m_Instance;

        private ValueBinding<bool> _ToolEnabled;

        //private ValueBinding<bool> _ShowUI;

        protected override void OnCreate()
        {
            base.OnCreate();

            //AddBinding(_ShowUI = new(Mod.MOD_UI, "MIT_ShowUI", false));
            AddBinding(_ToolEnabled = new(Mod.MOD_UI, "MIT_ToolEnabled", false));
            AddBinding(new TriggerBinding(Mod.MOD_UI, "MIT_EnableToggle", MIT_EnableToggle));
        }

        protected override void OnUpdate()
        {
            //bool showUI = true;

            //if (_Tool.m_PhotoModeRenderSystem.Enabled)
            //{
            //    showUI = false;
            //}

            //_ShowUI.Update(showUI);
            _ToolEnabled.Update(_Tool.Enabled);
        }

        private void MIT_EnableToggle()
        {
            QLog.Debug($"MIT_EnableToggle");
            _Tool.RequestToggle();
        }
    }
}
