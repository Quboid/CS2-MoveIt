﻿using Colossal.Serialization.Entities;
using Game;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Selection;
using QCommonLib;
using Unity.Entities;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        // Runs on first load
        protected override void OnCreate()
        {
            Log.Info($"Tool.OnCreate");
            base.OnCreate();
            m_Instance = this;
            Enabled = false;

            m_OverlaySystem = World.GetOrCreateSystemManaged<Overlays.MIT_OverlaySystem>();
            m_VanillaOverlaySystem = World.GetOrCreateSystemManaged<Systems.MIT_VanillaOverlaySystem>();
            m_RemoveOverriddenSystem = World.GetOrCreateSystemManaged<Systems.MIT_RemoveOverriddenSystem>();
            m_RenderSystem = World.GetOrCreateSystemManaged<Systems.MIT_RenderSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<Systems.MIT_UISystem>();
            m_PostToolSystem = World.GetOrCreateSystemManaged<Systems.MIT_PostToolSystem>();
            //m_HoverSystem = World.GetOrCreateSystemManaged<MIT_HoverSystem>();

            m_RaycastSystem = World.GetOrCreateSystemManaged<Game.Common.RaycastSystem>();

            m_ApplyAction = new Input.ApplyButton("Tool", "Apply");
            m_SecondaryAction = new Input.SecondaryButton("Tool", "Secondary Apply");

            QKeyboard.Init();

            m_TempQuery = SystemAPI.QueryBuilder()
                .WithAll<Temp, Game.Objects.Transform>()
                .WithNone<Game.Common.Owner>()
                .Build();

            m_ControlPointQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_ControlPoint>()
                .Build();
        }

        // Runs on every load, after OnCreate
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            Log.Info($"Tool.OnGamePreload(Purpose:{purpose}, GameMode:{mode})");
            base.OnGamePreload(purpose, mode);

            m_Instance = this;
            Enabled = false;

            ControlPointManager = new();
            HotkeyManager = new();
            Hover = new();
            Moveables = new();
            Queue = new();

            CreationPhase = CreationPhases.None;
            ToolState = ToolStates.Default;
            ToolAction = ToolActions.None;

            m_MarqueeSelect = false;
            m_IsManipulateMode = false;
            Selection ??= new SelectionNormal();

            m_RenderSystem.m_Widgets.Clear();

            m_OverlaySystem.DestroyAllEntities();
        }

        protected override void OnStartRunning()
        {
            Log.IsDebug = ExtraDebugLogging;
            Log.Info("Tool.OnStartRunning()");
            base.OnStartRunning();

            MIT_ToolTipSystem.instance.EnableIfPopulated();
            //m_HoverSystem.Start();
            m_RemoveOverriddenSystem.Start();
            m_PostToolSystem.Start();
            m_VanillaOverlaySystem.Start();
            m_OverlaySystem.Start();
            m_RenderSystem.Start();
            m_ApplyAction.Enabled = true;
            m_SecondaryAction.Enabled = true;
            m_SelectionDirty = true;
        }

        protected override void OnStopRunning()
        {
            Log.Info($"Tool.OnStopRunning");
            base.OnStopRunning();

            Hover.Clear();
            m_SecondaryAction.Enabled = false;
            m_ApplyAction.Enabled = false;
            m_RenderSystem.End();
            m_OverlaySystem.End();
            m_VanillaOverlaySystem.End();
            m_PostToolSystem.End();
            m_RemoveOverriddenSystem.End();
            //m_HoverSystem.End();
            MIT_ToolTipSystem.instance.Enabled = false;

            QLog.FlushBundle();
        }

        protected override void OnDestroy()
        {
            Log.Info("Tool.OnDestroy()");
            base.OnDestroy();
        }

        public void RequestEnable()
        {
            if (m_ToolSystem.activeTool != this)
            {
                _PreviousTool = m_ToolSystem.activeTool;
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
                applyMode = ApplyMode.Clear;

                Moveables.Refresh();
                Selection.Refresh();

                _UIHasFocusStep = 0;
            }
        }

        public void RequestDisable()
        {
            m_ToolSystem.activeTool = _PreviousTool ?? m_DefaultToolSystem;
        }

        public void RequestToggle()
        {
            if (m_ToolSystem.activeTool == this)
            {
                RequestDisable();
            }
            else
            {
                RequestEnable();
            }
        }
    }
}
