using Game.Tools;
using MoveIt.Managers;
using MoveIt.Selection;
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

    public enum InteractionFlags
    {
        None                = 0,
        Hovering            = 1,
        Selected            = 2,
        Moving              = 4,
        Deselect            = 8,
        Tool                = 16,
        Static              = 32,
        ParentHovering      = 64,
        ParentSelected      = 128,
        ParentManipulating  = 256,
    }

    public partial class MIT : ObjectToolBaseSystem
    {
        internal static MIT m_Instance;

        internal ControlPointManager ControlPointManager;
        internal InputManager InputManager;
        internal HoverManager Hover;
        internal MoveablesManager Moveables;
        internal QueueManager Queue;

        internal MIT_VanillaOverlaySystem m_VanillaOverlaySystem;
        internal MIT_RemoveOverriddenSystem m_RemoveOverriddenSystem;
        internal MIT_RenderSystem m_RenderSystem;
        internal MIT_UISystem m_UISystem;
        internal MIT_PostToolSystem m_PostToolSystem;
        internal MIT_HotkeySystem m_HotkeySystem;
        //internal MIT_HoverSystem m_HoverSystem;
        internal Overlays.MIT_OverlaySystem m_OverlaySystem;

        internal Game.Common.RaycastSystem m_RaycastSystem;

        public static QLoggerCO Log { get => _Log; }
        private readonly static QLoggerCO _Log = new(false, "", true);

        private ToolBaseSystem _PreviousTool = null;

        /// <summary>
        /// Check if the UI, including Move It panel, has focus (raycast hits it)
        /// If hit, is set to 3 but immediately decremented each frame so first frames after leaving UI doesn't register
        /// </summary>
        internal bool UIHasFocus => _UIHasFocusStep != 0;
        private short _UIHasFocusStep;

        internal JobHandle m_InputDeps;

        internal EntityQuery m_TempQuery;
        internal EntityQuery m_ControlPointQuery;

        // Options
        internal bool ShowDebugPanel        => Mod.Settings.ShowDebugPanel;
        internal bool HideMoveItIcon        => Mod.Settings.HideMoveItIcon;
        internal bool ExtraDebugLogging     => Mod.Settings.ExtraDebugLogging;
        internal int RotationDirection      => Mod.Settings.InvertRotation ? 1 : -1;

        /// <summary>
        /// Get the currently hovered entity, readonly. For other mods to access.
        /// Entity.Null if nothing hovered.
        /// </summary>
        public Entity HoveredEntity => Hover.Definition.m_Entity;

        /// <summary>
        /// Get a hashset of the currently selected entities, readonly. For other mods to access.
        /// Empty hashset if nothing selected.
        /// </summary>
        public HashSet<Entity> SelectedEntities => Selection.Definitions.Select(mvd => mvd.m_Entity).ToHashSet();

        /// <summary>
        /// Raycaster which only hits terrain
        /// </summary>
        internal RaycastTerrain m_RaycastTerrain;

        internal struct PrefabInfo
        {
            internal string m_Name;
            internal Entity m_Entity;
        }

        public ToolStates ToolState         { get; set; }
        public ToolActions ToolAction       { get; set; }
        public CreationPhases CreationPhase { get; set; }
        public ApplyMode BaseApplyMode      { get => applyMode; set => applyMode = value; }

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
        /// Screen position where rotation started
        /// </summary>
        internal float m_MouseStartX;
        /// <summary>
        /// The current mouse position on the terrain
        /// </summary>
        internal float3 m_PointerPos;

        // Selection modes
        internal Input.Marquee m_Marquee;
        internal bool m_MarqueeSelect;
        internal bool UseMarquee => m_MarqueeSelect && !m_IsManipulateMode;

        /// <summary>
        /// Is Manipulation Mode active, including quick-selection (player holding Alt)?
        /// </summary>
        public bool IsManipulating => m_IsManipulateMode || (QKeyboard.Alt && ToolState == ToolStates.Default);
        /// <summary>
        /// Is Manipulation Mode active, NOT including quick-selection?
        /// </summary>
        internal bool m_IsManipulateMode;

        // Selections
        public SelectionBase Selection { get => _Selection; set => _Selection = value; }
        private SelectionBase _Selection = null;
        public bool m_SelectionDirty = true;

        internal const float TERRAIN_UPDATE_MARGIN = 16f;
    }
}
