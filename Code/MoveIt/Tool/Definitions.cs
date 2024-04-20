using Game.Tools;
using MoveIt.Managers;
using MoveIt.Systems;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Tool
{
    public enum CreationPhases
    {
        None,
        Cleanup,
        Positioning,
        Create,
        Finalize,
    }

    public enum ToolStates
    {
        Default,
        ApplyButtonHeld,
        SecondaryButtonHeld,
        DrawingSelection,
        ToolActive
    }

    public enum ToolActions
    {
        None,
        Do,
        Undo,
        Redo
    }

    internal enum SelectionGridModes
    {
        Unprocessed,
        Ignore,
        Add,
        Remove
    }

    public partial class MIT : ObjectToolBaseSystem
    {
        internal static MIT m_Instance;

        internal ControlPointManager ControlPointManager;
        internal HotkeyManager HotkeyManager;
        internal HoverManager Hover;

        internal MIT_OverlaySystem m_OverlaySystem;
        internal MIT_VanillaOverlaySystem m_VanillaOverlaySystem;
        internal MIT_RemoveOverriddenSystem m_RemoveOverriddenSystem;
        internal MIT_RenderSystem m_RenderSystem;
        internal MIT_UISystem m_UISystem;

        public static QLoggerCO Log { get => _Log; }
        private readonly static QLoggerCO _Log = new(true, "", true);

        private Game.Simulation.TerrainSystem _TerrainSystem;
        private Game.Common.RaycastSystem _RaycastSystem;
        internal Game.Rendering.PhotoModeRenderSystem m_PhotoModeRenderSystem;

        internal JobHandle m_InputDeps;

        internal Input.InputButton m_ApplyAction;
        internal Input.InputButton m_SecondaryAction;

        internal EntityQuery m_TempQuery;
        internal EntityQuery m_ControlPointQuery;

        internal int RotationDirection => Mod.Settings.InvertRotation ? 1 : -1;
        internal Input.Marquee m_Marquee;

        internal bool m_MarqueeSelect;
        public bool Manipulating => _IsManipulateMode;
        private bool _IsManipulateMode;
        internal bool UseMarquee => m_MarqueeSelect && !_IsManipulateMode;

        /// <summary>
        /// Raycaster which only hits terrain
        /// </summary>
        internal RaycastTerrain m_RaycastTerrain;

        internal struct PrefabInfo
        {
            internal string m_Name;
            internal Entity m_Entity;
        }

        public ToolStates ToolState { get; set; }

        public ToolActions ToolAction { get; set; }

        public CreationPhases CreationPhase { get; set; }

        public ApplyMode BaseApplyMode { get => applyMode; set => applyMode = value; }

        /// <summary>
        /// Where the current drag started, relative to selection center
        /// </summary>
        internal float3 m_DragPointerOffsetFromSelection;
        /// <summary>
        /// World position of where the current drag started, absolute
        /// </summary>
        internal float3 m_ClickPositionAbs;
        /// <summary>
        /// Where sensitivity was last toggled, absolute
        /// </summary>
        // internal float3 m_sensitivityTogglePosAbs;

        /// <summary>
        /// Get the currently hovered entity, readonly. For other mods to access.
        /// Entity.Null if nothing hovered.
        /// </summary>
        public Entity HoveredEntity => Hover.Entity;

        /// <summary>
        /// Get a hashset of the currently selected entities, readonly. For other mods to access.
        /// Empty hashset if nothing selected.
        /// </summary>
        public HashSet<Entity> SelectedEntities => Selection.Entities.ToHashSet();

        /// <summary>
        /// Screen position where rotation started
        /// </summary>
        internal float m_MouseStartX;
        /// <summary>
        /// The current mouse position on the terrain
        /// </summary>
        internal float3 m_PointerPos;

        public Selection.Main Selection = new();
        public Selection.Manipulating Manipulation = new();
        public Selection.Base ActiveSelection => _IsManipulateMode ? Manipulation : Selection;
    }
}
