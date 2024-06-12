using Colossal.UI.Binding;
using Game.UI;
using MoveIt.Moveables;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System;
using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace MoveIt.Systems
{
    internal partial class MIT_UISystem : UISystemBase
    {
        protected readonly MIT _Tool = MIT.m_Instance;

        private ValueBinding<bool>          _ToolEnabledBinding;
        private ValueBinding<PanelState>    _PanelStateBinding;
        private ValueBinding<bool>          _ShowDebugPanelBinding;
        private ValueBinding<string>        _DebugPanelContentsBinding;
        private ValueBinding<bool>          _HideMoveItIcon;
        private PanelState _DefaultState;
        private PanelState _PanelState;

        private EntityQuery _DrawQuery;

        protected override void OnCreate()
        {
            _DrawQuery = SystemAPI.QueryBuilder()
                .WithAll<MIO_Type>()
                .WithNone<Game.Common.Deleted>()
                .Build();

            _DefaultState = new PanelState();
            _PanelState = new PanelState();

            base.OnCreate();
            AddBinding(_ToolEnabledBinding          = new ValueBinding<bool>(Mod.MOD_UI, "MIT_ToolEnabled", false));
            AddBinding(_PanelStateBinding           = new ValueBinding<PanelState>(Mod.MOD_UI, "MIT_PanelState", _DefaultState));
            AddBinding(_ShowDebugPanelBinding       = new ValueBinding<bool>(Mod.MOD_UI, "MIT_ShowDebugPanel", false));
            AddBinding(_DebugPanelContentsBinding   = new ValueBinding<string>(Mod.MOD_UI, "MIT_DebugPanelContents", "Hello World"));
            AddBinding(_HideMoveItIcon              = new ValueBinding<bool>(Mod.MOD_UI, "MIT_HideMoveItIcon", false));
            AddBinding(new TriggerBinding(Mod.MOD_UI, "MIT_EnableToggle", MIT_EnableToggle));
            AddBinding(new TriggerBinding<string>(Mod.MOD_UI, "MIT_PanelButtonPress", MIT_PanelButtonPress));
        }

        protected override void OnUpdate()
        {
            if (_Tool.Enabled) _PanelState.Update();
            _PanelStateBinding.Update(_PanelState);
            _ToolEnabledBinding.Update(_Tool.Enabled);
            _ShowDebugPanelBinding.Update(_Tool.ShowDebugPanel);
            _DebugPanelContentsBinding.Update(DebugPanelContents());
            _HideMoveItIcon.Update(_Tool.HideMoveItIcon);
        }

        private string DebugPanelContents()
        {
            if (!_Tool.ShowDebugPanel) return string.Empty;
            if (_Tool.Queue is null || _Tool.Queue.Current is null) return string.Empty;

            _DrawQuery.CompleteDependency();

            StringBuilder sb = new();
            sb.AppendFormat("**{0}** Tool:**{1}**/**{2}**\n", _Tool.IsManipulating ? "Manip" : (_Tool.m_MarqueeSelect ? "Marquee" : "Single"), _Tool.ToolState, _Tool.ToolAction);
            sb.AppendFormat("Action:**{0}** {1}\n", _Tool.Queue.Current, _Tool.Queue.GetQueueIndexes());
            if (_Tool.Hover.IsNull)
            {
                sb.AppendFormat("Nothing hovered\n");
            }
            else
            {
                string prefabName = QCommon.GetPrefabName(EntityManager, _Tool.Hover.Definition.m_Entity);
                if (prefabName.Length > 26) prefabName = prefabName.Substring(0, 24) + "...";
                sb.AppendFormat("**{0}** {1}\n", _Tool.Hover.Definition.m_Entity.DX(), prefabName);
            }
            sb.AppendFormat("**{0}**\n", _Tool.m_PointerPos.DX());
            sb.AppendFormat("MVs:**{0}** (CPs:{1}), Sel:**{2}** ({3})\n",
                _Tool.Moveables.Count, _Tool.Moveables.CountOf<MVControlPoint>(),
                _Tool.Selection.Count, _Tool.Selection.CountFull - _Tool.Selection.Count);
            sb.AppendFormat("Overlays:**{0}** ({1} types), Util:**{2}**, CPs:**{3}**\n",
                GetOverlayCount(),
                GetOverlayTypeCount(),
                GetOverlayCount(OverlayTypes.SelectionCenter) + GetOverlayCount(OverlayTypes.Marquee),
                GetOverlayCount(OverlayTypes.MVControlPoint));
            return sb.ToString();
        }

        /// <summary>
        /// Called from the UI
        /// </summary>
        private void MIT_EnableToggle()
        {
            _Tool.RequestToggle();
        }

        /// <summary>
        /// Called from the UI
        /// </summary>
        /// <param name="buttonId">The HTML element's ID</param>
        private void MIT_PanelButtonPress(string buttonId)
        {
            switch (buttonId)
            {
                case "undo":

                    if (_Tool.ToolState == ToolStates.Default)
                    {
                        _Tool.ToolAction = ToolActions.Undo;
                    }
                    break;

                case "redo":
                    if (_Tool.ToolState == ToolStates.Default)
                    {
                        _Tool.ToolAction = ToolActions.Redo;
                    }
                    break;

                case "single":
                    _Tool.SetSelectionMode(false);
                    break;

                case "marquee":
                    _Tool.SetSelectionMode(true);
                    break;

                case "manipulation":
                    _Tool.SetManipulationMode(true);
                    break;

                default:
                    break;
            }
        }



        internal int GetOverlayCount()
        {
            return _DrawQuery.CalculateEntityCount();
        }

        internal int GetOverlayCount(OverlayTypes t)
        {
            if (_DrawQuery.IsEmpty) return 0;

            int c = 0;
            var all = _DrawQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity e in all)
            {
                if (EntityManager.GetComponentData<MIO_Type>(e).m_Type == t) c++;
            }
            return c;
        }

        internal int GetOverlayTypeCount()
        {
            try
            {
                if (_DrawQuery.IsEmpty) return 0;

                HashSet<OverlayTypes> types = new();
                var all = _DrawQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity e in all)
                {
                    if (!EntityManager.Exists(e)) continue;

                    var t = EntityManager.GetComponentData<MIO_Type>(e).m_Type;
                    if (!types.Contains(t)) types.Add(t);
                }
                all.Dispose();
                return types.Count;
            }
            catch (Exception ex)
            {
                var all = _DrawQuery.ToEntityArray(Allocator.Temp);
                string msg = "";
                foreach (Entity e in all)
                {
                    string exists = "N";
                    try
                    {
                        if (EntityManager.Exists(e)) { exists = "Y"; }
                    }
                    catch { exists = "X"; }
                    msg += $"{e.D()}-{exists},  ";
                }
                MIT.Log.Warning($"GetOverlayTypeCount failed (entities:{_DrawQuery.CalculateEntityCount()})\n{_Tool.Moveables.DebugFull()}\n{msg}\n{ex}");
                all.Dispose();
                return -1;
            }
        }

        internal string DebugDrawQuery()
        {
            string msg = $"Overlays:{_DrawQuery.CalculateEntityCount()}\n";
            var all = _DrawQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity olay in all)
            {
                var t = _Tool.EntityManager.GetComponentData<MIO_Type>(olay).m_Type;
                msg += $"  [{olay.D()}-{t}]";
            }
            return msg + $"\n{_Tool.Moveables.DebugFull()}";
        }
    }
}
