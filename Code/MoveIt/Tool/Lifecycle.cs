using Colossal.Serialization.Entities;
using Game;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Systems;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Instance = this;
            Enabled = false;
            
            m_OverlaySystem = World.GetOrCreateSystemManaged<MIT_OverlaySystem>();
            m_VanillaOverlaySystem = World.GetOrCreateSystemManaged<MIT_VanillaOverlaySystem>();
            m_RemoveOverriddenSystem = World.GetOrCreateSystemManaged<MIT_RemoveOverriddenSystem>();
            m_RenderSystem = World.GetOrCreateSystemManaged<MIT_RenderSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<MIT_UISystem>();

            _TerrainSystem = World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>();
            _RaycastSystem = base.World.GetOrCreateSystemManaged<Game.Common.RaycastSystem>();
            m_PhotoModeRenderSystem = World.GetOrCreateSystemManaged<Game.Rendering.PhotoModeRenderSystem>();

            HotkeyManager = new();
            ControlPointManager = new();
            Hover = new();

            m_ApplyAction = new Input.ApplyButton("Tool", "Apply");
            m_SecondaryAction = new Input.SecondaryButton("Tool", "Secondary Apply");

            CreationPhase = CreationPhases.None;
            ToolState = ToolStates.Default;
            ToolAction = ToolActions.None;

            m_MarqueeSelect = false;
            _IsManipulateMode = false;

            QKeyboard.Init();

            m_TempQuery = new EntityQueryBuilder(Allocator.Persistent)
                .WithAll<Temp, Game.Objects.Transform>()
                .WithNone<Game.Common.Owner>()
                .Build(EntityManager);

            m_ControlPointQuery = new EntityQueryBuilder(Allocator.Persistent)
                .WithAll<MIT_ControlPoint>()
                .Build(EntityManager);
        }

        protected override void OnStartRunning()
        {
            Log.Info("Tool.OnStartRunning()");
            base.OnStartRunning();

            MIT_ToolTipSystem.instance.EnableIfPopulated();
            m_RemoveOverriddenSystem.Start();
            m_VanillaOverlaySystem.Start();
            m_OverlaySystem.Start();
            m_RenderSystem.Start();
            m_ApplyAction.Enabled = true;
            m_SecondaryAction.Enabled = true;
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
            m_RemoveOverriddenSystem.End();
            MIT_ToolTipSystem.instance.Enabled = false;

            QLog.FlushBundle();
        }

        protected override void OnDestroy()
        {
            m_ControlPointQuery.Dispose();
            m_TempQuery.Dispose();
            base.OnDestroy();
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
        }

        public void RequestEnable()
        {
            if (m_ToolSystem.activeTool != this && m_ToolSystem.activeTool == m_DefaultToolSystem)
            {
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
                applyMode = ApplyMode.Clear;

                Selection.Refresh();
                Manipulation.Refresh();
                Hover.Refresh();
                ControlPointManager.Refresh();
            }
        }

        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
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
