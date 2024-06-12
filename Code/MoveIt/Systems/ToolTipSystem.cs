using Game.Tools;
using Game.UI.Tooltip;
using QCommonLib;

namespace MoveIt
{
    public partial class MIT_ToolTipSystem : TooltipSystemBase
    {
        private ToolSystem _ToolSystem;
        private StringTooltip _Tooltip;
        private Tool.MIT _Tool;

        public static MIT_ToolTipSystem instance;

        private float _TTL;
        private string _Text;

        internal void EnableIfPopulated()
        {
            UpdateForTTL();

            Enabled = _Text != string.Empty;
        }

        public void Set(string msg, float expires = 0)
        {
            _Text = msg;
            _TTL = (expires > 0) ? UnityEngine.Time.time + expires : 0f;
            Enabled = !msg.Equals(string.Empty);
        }

        private void UpdateForTTL()
        {
            if (_TTL > 0f && UnityEngine.Time.time > _TTL)
            {
                _Text = string.Empty;
                _TTL = 0f;
                Enabled = false;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
            _Tool = base.World.GetOrCreateSystemManaged<Tool.MIT>();
            _Tooltip = new StringTooltip
            {
                path = "QTesting_Main"
            };
            _TTL = 0f;
            _Text = string.Empty;
            instance = this;
        }

        protected override void OnUpdate()
        {
            UpdateForTTL();
            if (_ToolSystem.activeTool != _Tool || !Enabled)
            {
                return;
            }

            _Tooltip.value = _Text;
            AddMouseTooltip(_Tooltip);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public MIT_ToolTipSystem()
        { }
    }
}
