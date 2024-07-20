using Colossal.UI.Binding;
using Game.UI;
using MoveIt.Moveables;
using MoveIt.Overlays;
using MoveIt.Systems.UIElements;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using System.Linq;
using Game.Input;

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
        private ValueBinding<bool>          _ShowMConflict;
        private ValueBinding<string>        _RebindExistingMsg;
        private PanelState _DefaultState;
        private PanelState _PanelState;

        internal bool m_isFiltersOpen = false;

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
            AddBinding(_ShowMConflict               = new ValueBinding<bool>(Mod.MOD_UI, "MIT_ShowRebindConfirm", false));
            AddBinding(_RebindExistingMsg           = new ValueBinding<string>(Mod.MOD_UI, "MIT_RebindExistingMsg", "[Error]"));
            AddBinding(new TriggerBinding(Mod.MOD_UI, "MIT_EnableToggle", MIT_EnableToggle));
            AddBinding(new TriggerBinding<string>(Mod.MOD_UI, "MIT_PanelButtonPress", MIT_PanelButtonPress));
            AddBinding(new TriggerBinding<bool>(Mod.MOD_UI, "MIT_ShowRebindConfirm", MIT_ShowRebindConfirm));

            Enabled = true;
        }

        protected override void OnUpdate()
        {
            if (_Tool.Enabled) _PanelState.Update();
            _PanelStateBinding.Update(_PanelState);
            _ToolEnabledBinding.Update(_Tool.Enabled);
            _ShowDebugPanelBinding.Update(_Tool.ShowDebugPanel);
            _DebugPanelContentsBinding.Update(GenerateDebugPanelContents());
            _HideMoveItIcon.Update(_Tool.HideMoveItIcon);

            bool hasShownMConflictPanel = Mod.Settings.HasShownMConflictPanel;
            List<ProxyBinding> conflicts = GetActionKeyConflicts(Inputs.KEY_TOGGLETOOL);
            bool showMConflictPanel = !hasShownMConflictPanel && conflicts.Count > 0;
            _ShowMConflict.Update(showMConflictPanel);
            StringBuilder msg = new();
            if (showMConflictPanel)
            {
                msg.AppendFormat("Do you want the '**M**' key to open Move It?\nIt will be removed from:");
                foreach (ProxyBinding binding in conflicts)
                {
                    msg.AppendFormat("\n - {0}: **{1}**", binding.mapName, binding.actionName);
                }
            }
            _RebindExistingMsg.Update(msg.ToString());
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

                case "filtersTitle":
                    m_isFiltersOpen = !m_isFiltersOpen;

                    QLog.Debug($"UIButton: {buttonId} isOpen:{m_isFiltersOpen}");
                    break;

                default:
                    break;
            }
        }


        private string GenerateDebugPanelContents()
        {
            if (!_Tool.ShowDebugPanel) return string.Empty;
            if (_Tool.Queue is null || _Tool.Queue.Current is null) return string.Empty;

            _DrawQuery.CompleteDependency();

            StringBuilder sb = new();
            sb.AppendFormat("**{0}** Tool:**{1}**/**{2}**\n", _Tool.IsManipulating ? "Manip" : (_Tool.m_MarqueeSelect ? "Marquee" : "Single"), _Tool.ToolState, _Tool.ToolAction);
            sb.AppendFormat("Action:**{0}**{1} {2}\n", _Tool.Queue.Current, _Tool.Queue.HasCreationAction ? "*" : "", _Tool.Queue.GetQueueIndexes());
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
                _Tool.Moveables.Count, _Tool.Moveables.CountOf<MVControlPoint>(), _Tool.Selection.Count,
                _Tool.Selection.CountFull - _Tool.Selection.Count >= 0 ? _Tool.Selection.CountFull - _Tool.Selection.Count : "...");
            sb.AppendFormat("Overlays:**{0}** ({1} types), Util:**{2}**, CPs:**{3}**\n",
                GetOverlayCount(),
                GetOverlayTypeCount(),
                GetOverlayCount(OverlayTypes.SelectionCenter) + GetOverlayCount(OverlayTypes.Marquee),
                GetOverlayCount(OverlayTypes.MVControlPoint));
            return sb.ToString();
        }

        private void MIT_ShowRebindConfirm(bool doRebind)
        {
            if (doRebind)
            {
                var conflicts = GetActionKeyConflicts(Inputs.KEY_TOGGLETOOL);
                for (int i = 0; i < conflicts.Count; i++)
                {
                    ProxyBinding binding = conflicts[i];
                    binding.path = string.Empty;
                    InputManager.instance.SetBinding(binding, out _);
                }

                MIT.Log.Info($"Set {conflicts.Count} bindings to empty ({string.Join(",", conflicts)})");
            }
            else
            {
                ProxyAction toggleTool = Mod.Settings.GetAction(Inputs.KEY_TOGGLETOOL);
                ProxyBinding binding = toggleTool.bindings.First();
                binding.WithModifiers(new List<ProxyModifier>()
                {
                    new()
                    {
                        m_Name              = "modifier",
                        m_Component         = ActionComponent.Press,
                        m_Path              = "<Keyboard>/shift",
                    },
                });
                //binding.AddModifier(new()
                //{
                //    m_Name              = "modifier",
                //    m_IsProhibition     = false,
                //    m_Component         = ActionComponent.Press,
                //    m_Path              = "<Keyboard>/shift",
                //});
                InputManager.instance.SetBinding(binding, out _);
                Mod.Settings.Key_ToggleTool = binding;

                MIT.Log.Info($"Set ToggleTool to Shift+M");
                
                //QLog.Debug($"Binding Paths for ToggleTool and ToggleManip:" +
                //    $"\n{binding.path} {binding.modifiers.Count}:{string.Join(",", binding.modifiers)}" +
                //    $"\n{t2.path} {t2.modifiers.Count}:{string.Join(",", t2.modifiers)}" +
                //    $"\n+ {t2.modifiers.First().m_Name}, {t2.modifiers.First().m_Path}, {t2.modifiers.First().m_Component}, {t2.modifiers.First().m_IsProhibition}");
            }

            Mod.Settings.HasShownMConflictPanel = true;
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

        internal static List<ProxyBinding> GetActionKeyConflicts(string actionName)
        {
            List<ProxyBinding> results = new();
            var bindings = Mod.Settings.GetAction(actionName).bindings;
            foreach (var binding in bindings)
            {
                results.AddRange(binding.conflicts);
            }
            return results;
        }

        //internal static List<ProxyAction> GetMKeyConflictsX => InputManager.instance.actions.Where(a =>
        //    a.usedKeys.Count() == 1 &&
        //    a.usedKeys.Any(k => k.Equals("<Keyboard>/m")) &&
        //    a.bindings.Any(b =>
        //        b.modifiers.Count == 0 &&
        //        !b.m_MapName.Equals("Editor") &&
        //        !b.m_MapName.Equals("MoveIt.MoveIt.Mod")
        //        )
        //    ).ToList();


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
