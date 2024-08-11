using Colossal.UI.Binding;
using Game.Input;
using Game.UI;
using MoveIt.Moveables;
using MoveIt.Overlays;
using MoveIt.Tool;
using MoveIt.UI;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace MoveIt.Systems
{
    internal partial class MIT_UISystem : UISystemBase
    {
        protected readonly MIT _MIT = MIT.m_Instance;

        private ValueBinding<bool>          _ToolEnabledBinding;
        private ValueBinding<PanelState>    _PanelStateBinding;
        private ValueBinding<bool>          _ShowDebugPanelBinding;
        private ValueBinding<string>        _DebugPanelContentsBinding;
        private ValueBinding<bool>          _HideMoveItIcon;
        private ValueBinding<bool>          _ShowMConflict;
        private ValueBinding<string>        _RebindExistingMsg;
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
            AddBinding(_ToolEnabledBinding          = new ValueBinding<bool>(Mod.MOD_UI,        "MIT_ToolEnabled", false));
            AddBinding(_PanelStateBinding           = new ValueBinding<PanelState>(Mod.MOD_UI,  "MIT_PanelState", _DefaultState));
            AddBinding(_ShowDebugPanelBinding       = new ValueBinding<bool>(Mod.MOD_UI,        "MIT_ShowDebugPanel", false));
            AddBinding(_DebugPanelContentsBinding   = new ValueBinding<string>(Mod.MOD_UI,      "MIT_DebugPanelContents", "Hello World"));
            AddBinding(_HideMoveItIcon              = new ValueBinding<bool>(Mod.MOD_UI,        "MIT_HideMoveItIcon", false));
            AddBinding(_ShowMConflict               = new ValueBinding<bool>(Mod.MOD_UI,        "MIT_ShowRebindConfirm", false));
            AddBinding(_RebindExistingMsg           = new ValueBinding<string>(Mod.MOD_UI,      "MIT_RebindExistingMsg", "[Error]"));
            AddBinding(new TriggerBinding(Mod.MOD_UI, "MIT_EnableToggle", MIT_EnableToggle));
            AddBinding(new TriggerBinding<string, string>(Mod.MOD_UI, "MIT_PanelButtonPress", MIT_PanelButtonPress));
            AddBinding(new TriggerBinding<string, string, bool>(Mod.MOD_UI, "MIT_PanelCheckboxChange", MIT_PanelCheckboxChange));
            AddBinding(new TriggerBinding<string, string, int>(Mod.MOD_UI, "MIT_PanelLabelMouseUp", MIT_PanelLabelMouseUp));
            AddBinding(new TriggerBinding<bool>(Mod.MOD_UI, "MIT_ShowRebindConfirm", MIT_ShowRebindConfirm));

            Enabled = true;
        }

        protected override void OnUpdate()
        {
            if (_MIT.Enabled) _PanelState.Update();
            _PanelStateBinding.Update(_PanelState);
            _ToolEnabledBinding.Update(_MIT.Enabled);
            _ShowDebugPanelBinding.Update(_MIT.ShowDebugPanel);
            _DebugPanelContentsBinding.Update(GenerateDebugPanelContents());
            _HideMoveItIcon.Update(_MIT.HideMoveItIcon);

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
            _MIT.RequestToggle();
        }

        /// <summary>
        /// Open or close the Filters panel, enabling or disabling filters
        /// </summary>
        /// <returns>Is the panel now open?</returns>
        internal bool ToggleFiltersPanel()
            => _PanelState.m_FilterSection.TogglePanelOpen();

        /// <summary>
        /// Called from the UI
        /// </summary>
        /// <param name="section">The UI section</param>
        /// <param name="buttonId">The HTML element's ID</param>
        private void MIT_PanelButtonPress(string section, string buttonId)
        {
            if (section.Equals("toprow"))
            {
                switch (buttonId)
                {
                    case "undo":

                        if (_MIT.MITState == MITStates.Default)
                        {
                            _MIT.MITAction = MITActions.Undo;
                        }
                        break;

                    case "redo":
                        if (_MIT.MITState == MITStates.Default)
                        {
                            _MIT.MITAction = MITActions.Redo;
                        }
                        break;

                    case "single":
                        _MIT.SetSelectionMode(false);
                        break;

                    case "marquee":
                        _MIT.SetSelectionMode(true);
                        break;

                    case "manipulation":
                        _MIT.SetManipulationMode(true);
                        break;

                    default:
                        MIT.Log.Debug($"UIButton: {buttonId}");
                        break;
                }
                return;
            }
        }

        /// <summary>
        /// Called from the UI
        /// </summary>
        /// <param name="section">The UI section</param>
        /// <param name="buttonId">The HTML element's ID</param>
        /// <param name="value">The checkbox's value</param>
        private void MIT_PanelCheckboxChange(string section, string buttonId, bool value)
        {
            if (section.Equals("filters"))
            {
                if (buttonId.Equals("filtersAll"))
                {
                    _PanelState.m_FilterSection.UI_ToggleAll(value);
                    return;
                }

                _PanelState.m_FilterSection.SetFilter(buttonId, value);
                return;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Called from the UI
        /// </summary>
        /// <param name="section">The UI section</param>
        /// <param name="id">The HTML element's ID</param>
        /// <param name="button">The pressed mouse button</param>
        private void MIT_PanelLabelMouseUp(string section, string id, int button)
        {
            if (button == 0)
            {
                if (section.Equals("filters"))
                {
                    if (id.Equals("filtersAll"))
                    {
                        _PanelState.m_FilterSection.TogglePanelOpen();
                        return;
                    }

                    _PanelState.m_FilterSection.ToggleFilter(id);
                    return;
                }

                if (section.Equals("toolbox"))
                {
                    if (id == "toolboxTitle")
                    {
                        _PanelState.m_ToolboxSection.TogglePanelOpen();
                    }
                    else
                    {
                        _MIT.ToolboxManager.Activate(id);
                    }
                    return;
                }

                return;
            }

            if (button == 2)
            {
                if (section.Equals("filters"))
                {
                    if (id.Equals("filtersAll")) return;

                    _PanelState.m_FilterSection.ToggleFilterOnly(id);
                    return;
                }
                return;
            }
        }

        private string GenerateDebugPanelContents()
        {
            if (!_MIT.ShowDebugPanel) return string.Empty;
            if (_MIT.Queue is null || _MIT.Queue.Current is null) return string.Empty;

            _DrawQuery.CompleteDependency();

            StringBuilder sb = new();
            sb.AppendFormat("**{0}** Tool:**{1}**", _MIT.IsManipulating ? "Manip" : (_MIT.m_MarqueeSelect ? "Marquee" : "Single"), _MIT.MITState);
            if (_MIT.MITState == MITStates.ToolActive)
            {
                sb.AppendFormat("/P:**{0}**", _MIT.ToolboxManager.Phase);
            }
            if (_MIT.MITAction != MITActions.None)
            {
                sb.AppendFormat("/TA:**{0}**", _MIT.MITAction);
            }
            sb.AppendFormat("\nAction:**{0}**{1} {2}\n", _MIT.Queue.Current, _MIT.Queue.HasCreationAction ? "*" : "", _MIT.Queue.UI_GetQueueIndexes());
            if (_MIT.Hover.Normal.IsNull && _MIT.Hover.Child.IsNull)
            {
                sb.AppendFormat("Nothing hovered\n");
            }
            else
            {
                if (!_MIT.Hover.Normal.IsNull)
                {
                    string prefabName = QCommon.GetPrefabName(EntityManager, _MIT.Hover.Normal.Definition.m_Entity);
                    int len = _MIT.Hover.Child.IsNull ? 20 : 26;
                    if (prefabName.Length > len) prefabName = prefabName.Substring(0, len - 2) + "...";
                    sb.AppendFormat("**{0}** {1}", _MIT.Hover.Normal.Definition.m_Entity.DX(), prefabName);
                    if (!_MIT.Hover.Child.IsNull)
                    {
                        sb.AppendFormat(" ({0})", _MIT.Hover.Child.Definition.m_Entity.DX());
                    }
                    sb.Append("\n");
                }
                else
                {
                    sb.AppendFormat("Child: **{0}**\n", _MIT.Hover.Child.Definition.m_Entity.DX());
                }
            }
            sb.AppendFormat("**{0}**\n", _MIT.m_PointerPos.DX());
            sb.AppendFormat("MVs:**{0}** (CPs:{1}), Sel:**{2}** ({3})\n",
                _MIT.Moveables.Count, _MIT.Moveables.CountOf<MVControlPoint>(), _MIT.Selection.Count,
                _MIT.Selection.CountFull - _MIT.Selection.Count >= 0 ? _MIT.Selection.CountFull - _MIT.Selection.Count : "...");
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
                InputManager.instance.SetBinding(binding, out _);
                Mod.Settings.Key_ToggleTool = binding;

                MIT.Log.Info($"Set ToggleTool to Shift+M");

                //MIT.Log.Debug($"Binding Paths for ToggleTool and ToggleManip:" +
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
                MIT.Log.Warning($"GetOverlayTypeCount failed (entities:{_DrawQuery.CalculateEntityCount()})\n{_MIT.Moveables.DebugFull()}\n{msg}\n{ex}");
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

        internal FilterSectionState GetFilterState()
        {
            return _PanelState.m_FilterSection;
        }


        internal string DebugDrawQuery()
        {
            string msg = $"Overlays:{_DrawQuery.CalculateEntityCount()}\n";
            var all = _DrawQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity olay in all)
            {
                var t = _MIT.EntityManager.GetComponentData<MIO_Type>(olay).m_Type;
                msg += $"  [{olay.D()}-{t}]";
            }
            return msg + $"\n{_MIT.Moveables.DebugFull()}";
        }
    }
}
