using System;

namespace MoveIt.Actions.Toolbox
{
    internal class TerrainHeight : ToolBoxTool
    {
        internal TerrainHeight() : base(typeof(AlignTerrainHeight))
        {
            m_Id        = "terrainHeight";
            m_UI        = new("terrainHeight", typeof(AlignTerrainHeight));
            m_Hotkey    = Inputs.KEY_TB_TERRAINHEIGHT;
            m_Filters   = Searcher.Utils.FilterAll;
            m_Settings  = new() {
                RequiresSelection   = true,
                FireOnCreation      = true,
                UseOverlay          = false,
            };
        }
    }

    internal class ObjectHeight : ToolBoxTool
    {
        internal ObjectHeight() : base(typeof(AlignObjectHeight))
        {
            m_Id        = "objectHeight";
            m_UI        = new("objectHeight", typeof(AlignObjectHeight));
            m_Hotkey    = Inputs.KEY_TB_OBJECTHEIGHT;
            m_Filters   = Searcher.Utils.FilterAll;
            m_Settings  = new()
            {
                RequiresSelection   = true,
                FireOnCreation      = false,
                UseOverlay          = true,
            };
        }
    }

    internal class RotateAtCentre : ToolBoxTool
    {
        internal RotateAtCentre() : base(typeof(AlignRotateAtCentre))
        {
            m_Id        = "rotateAtCentre";
            m_UI        = new("rotateAtCentre", typeof(AlignRotateAtCentre));
            m_Hotkey    = Inputs.KEY_TB_OBJANGLEGROUP;
            m_Filters   = Searcher.Utils.FilterAll & ~(Searcher.Filters.Nodes | Searcher.Filters.ControlPoints);
            m_Settings  = new()
            {
                RequiresSelection   = true,
                FireOnCreation      = false,
                UseOverlay          = true,
            };
        }
    }

    internal class RotateInPlace : ToolBoxTool
    {
        internal RotateInPlace() : base(typeof(AlignRotateInPlace))
        {
            m_Id        = "rotateInPlace";
            m_UI        = new("rotateInPlace", typeof(AlignRotateInPlace));
            m_Hotkey    = Inputs.KEY_TB_OBJANGLEINDIV;
            m_Filters   = Searcher.Utils.FilterAll & ~(Searcher.Filters.Nodes | Searcher.Filters.ControlPoints);
            m_Settings  = new()
            {
                RequiresSelection   = true,
                FireOnCreation      = false,
                UseOverlay          = true,
            };
        }
    }

    internal abstract class ToolBoxTool
    {
        /// <summary>
        /// The Id name, as called by the UI
        /// </summary>
        internal string m_Id;
        /// <summary>
        /// This tool's UI entry in the toolbox
        /// </summary>
        internal UI.ToolEntry m_UI;
        /// <summary>
        /// What Action does this tool create?
        /// </summary>
        internal Type m_ActionType;
        /// <summary>
        /// This tool's keyboard hotkey tag
        /// </summary>
        internal string m_Hotkey;
        /// <summary>
        /// What filters to apply to the raycast
        /// </summary>
        internal Searcher.Filters m_Filters;
        /// <summary>
        /// This tool's settings
        /// </summary>
        internal Settings m_Settings;

        /// <summary>
        /// A Toolbox tool
        /// </summary>
        /// <param name="id">The Id name, as called by the UI</param>
        /// <param name="actionType">What Action does this tool create?</param>
        /// <param name="hotkey">This tool's keyboard hotkey tag</param>
        /// <param name="filters">What filters to apply to the raycast</param>
        /// <param name="settings">This tool's settings</param>
        internal ToolBoxTool(Type actionType)
        {
            if (!typeof(Transform.TransformToolbox).IsAssignableFrom(actionType))
            {
                throw new Exception($"Error: actionType <{actionType.FullName}> is not <TransformToolbox>");
            }

            m_ActionType = actionType;
        }


        internal record Settings
        {
            /// <summary>
            /// Does this tool require an existing selection to start?
            /// </summary>
            internal bool RequiresSelection;
            /// <summary>
            /// Does this tool immediately fire it's Action.Do?
            /// </summary>
            internal bool FireOnCreation;
            /// <summary>
            /// Does this tool require use tool hover overlay?
            /// </summary>
            internal bool UseOverlay;
        }
    }
}
