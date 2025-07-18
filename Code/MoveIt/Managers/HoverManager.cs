using Colossal.Mathematics;
using Game.Tools;
using MoveIt.Moveables;
using MoveIt.Overlays.DebugOverlays;
using MoveIt.Tool;
using QCommonLib;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace MoveIt.Managers
{
    public record HoverHolder
    {
        protected readonly MIT _MIT = MIT.m_Instance;

        public bool IsForChild => _IsForChild;
        private readonly bool _IsForChild;

        public HoverHolder(bool isForManipChild)
        {
            _IsForChild = isForManipChild;
        }

        /// <summary>
        /// The hovered object, or default MVDefinition if none
        /// </summary>
        public MVDefinition Definition
        {
            get => _Definition;
            set => _Definition = value;
        }
        private MVDefinition _Definition;

        /// <summary>
        /// The hovered object, or last valid object if none
        /// </summary>
        public MVDefinition LastValid
        {
            get => _HoveredHit;
            set => _HoveredHit = value;
        }
        private MVDefinition _HoveredHit = new(true);

        /// <summary>
        /// The hovered object when the pointer button was pressed
        /// </summary>
        public MVDefinition OnPress
        {
            get => _HoveredOnPress;
            set => _HoveredOnPress = value;
        }
        private MVDefinition _HoveredOnPress = new(true);

        /// <summary>
        /// Moveable object for _Hovered, if present
        /// </summary>
        public Moveable MV
        {
            get => _HoveredMoveable;
            set => _HoveredMoveable = value;
        }
        private Moveable _HoveredMoveable;

        /// <summary>
        /// Set hovered to nothing
        /// </summary>
        internal void Clear()
        {
            if (MV is not null)
            {
                Unset();
            }
            Definition  = new(true);
            LastValid   = new(true);
            OnPress     = new(true);
        }

        internal void Set(MVDefinition to)
        {
            MV          = _MIT.Moveables.GetOrCreate<Moveable>(to);
            Definition  = to;
            LastValid   = to;
            MV.OnHover();
        }

        internal void Unset()
        {
            Moveable mv         = MV;
            MVDefinition mvd    = Definition;
            Definition          = new(true);
            MV                  = null;

            if (_MIT.Moveables.RemoveIfUnused(mvd))
            {
                // Unhover remaining children
                mv?.OnUnhoverChildren();
            }
            else
            {
                mv?.OnUnhover();
            }
        }

        public bool IsNull          => Definition.m_Entity.Equals(Entity.Null) && Definition.m_Parent.Equals(Entity.Null);
        public bool IsSelected      => !IsNull && _MIT.Selection.Has(Definition, false);
        public bool IsManipulatable => !IsNull && MV.IsManipulatable;

        public override string ToString()
        {
            return _Definition.ToString();
        }
    }

    public class HoverManager : MIT_Manager
    {
        //private readonly MIT_HoverSystem _HoverSystem;

        // public HoverManager()
        // {
        //     Normal = new(false);
        //     Child = new(true);
        //     //_HoverSystem = _MIT.World.GetOrCreateSystemManaged<MIT_HoverSystem>();
        // }

        public HoverHolder Normal { get; set; } = new(false);
        public HoverHolder Child { get; set; } = new(true);

        /// <summary>
        /// Get the HoverHolder of whatever hovered object is "top", i.e. the most relevant
        /// </summary>
        public HoverHolder TopHovered
        {
            get
            {
                //QLog.Bundle("HOV_ISNULL", $"ChildNull:{Child.Definition.m_Entity.Equals(Entity.Null)},  ParentNull:{Child.Definition.m_Parent.Equals(Entity.Null)},  isNull:{Child.Definition.m_Entity.Equals(Entity.Null) && Child.Definition.m_Parent.Equals(Entity.Null)}");
                if (!Child.IsNull) return Child;
                return Normal;
            }
        }

        /// <summary>
        /// Get the MVDefinition of whatever pressed object is "top", i.e. the most relevant
        /// </summary>
        public MVDefinition TopPressed
        {
            get
            {
                //QLog.Bundle("PRS_ISNULL", $"ChildNull:{Child.OnPress.m_Entity.Equals(Entity.Null)},  ParentNull:{Child.OnPress.m_Parent.Equals(Entity.Null)},  isNull:{Child.OnPress.m_Entity.Equals(Entity.Null) && Child.OnPress.m_Parent.Equals(Entity.Null)}");
                if (!Child.OnPress.IsNull) return Child.OnPress;
                return Normal.OnPress;
            }
        }

        /// <summary>
        /// Is the passed MVDefinition currently being hovered?
        /// </summary>
        /// <param name="mvd">The object definition to check</param>
        /// <returns>True if either Normal or Child hovered matches</returns>
        public bool Is(MVDefinition mvd)
        {
            if (mvd.Equals(Normal.Definition)) return true;
            if (mvd.Equals(Child.Definition)) return true;

            if (IsChildOf(Normal.MV, mvd)) return true;
            if (IsChildOf(Child.MV, mvd)) return true;
            return false;
        }

        private static bool IsChildOf(Moveable hovered, MVDefinition mvd)
            => hovered is not null && hovered.GetAllChildren().Any(childDef => childDef.Equals(mvd));

        /// <summary>
        /// Refresh the hovered object on tool activation
        /// </summary>
        internal void Refresh()
        { }

        /// <summary>
        /// Set hovered to nothing
        /// </summary>
        internal void Clear()
        {
            Normal.Clear();
            Child.Clear();
        }

        internal void Process(ToolRaycastSystem toolRaycastSystem)
        {
            if (_MIT.MITState == MITStates.DrawingSelection)
            {
                Normal.Unset();
                Child.Unset();
                return;
            }

            using Searcher.Searcher searcher = new(_MIT.Filtering.GetMask(), _MIT.IsManipulating, _MIT.m_PointerPos);

            NativeArray<Game.Common.RaycastResult> vanillaNetworkResults = _MIT.m_RaycastSystem.GetResult(toolRaycastSystem);
            NativeArray<Game.Common.RaycastResult> vanillaSurfaceResults = _MIT.m_RaycastSurface.GetResults();
            // Ray ray = Camera.main.ScreenPointToRay(Game.Input.InputManager.instance.mousePosition);
            searcher.SearchRay(ToolRaycastSystem.CalculateRaycastLine(Camera.main), vanillaNetworkResults, vanillaSurfaceResults);
            vanillaNetworkResults.Dispose();
            vanillaSurfaceResults.Dispose();

            ProcessMode(searcher, Normal);

            if (_MIT.IsManipulating)
            {
                ProcessMode(searcher, Child);
            }
            else
            {
                if (!Child.IsNull)
                {
                    Child.Clear();
                }
            }
        }

        private void ProcessMode(Searcher.Searcher searcher, HoverHolder holder)
        {
            MVDefinition to = new(true);
            if (searcher.Count > 0)
            {
                int i = 0;
                bool found = false;
                do
                {
                    Entity e = searcher.m_Results[i++].m_Entity;
                    Identity id = QTypes.GetEntityIdentity(e);
                    bool objIsManip = QTypes.IsManipulationPredict(id, _MIT.IsManipulating);

                    if (objIsManip != _MIT.IsManipulating) continue;

                    if (_MIT.IsManipulating)
                    {
                        if (QTypes.IsManipChildPredict(id, _MIT.IsManipulating) != holder.IsForChild) continue;
                    }

                    Entity parent = Entity.Null;
                    var parentId = Identity.None;
                    short parentKey = -1;
                    if (id == Identity.ControlPoint)
                    {
                        var component = _MIT.EntityManager.GetComponentData<Components.MIT_ControlPoint>(e);
                        parent = component.m_Parent;
                        parentId = component.m_ParentId;
                        parentKey = component.m_ParentKey;
                    }
                    to = new(id, e, objIsManip, QTypes.IsManagedPredict(id), parent, parentId, parentKey);
                    found = true;
                }
                while (!found && i < searcher.m_Results.Length);
            }

            if (holder.Definition.Equals(to)) return;

            if (_MIT.MITState == MITStates.ApplyButtonHeld || _MIT.MITState == MITStates.SecondaryButtonHeld) return;

            if (!holder.Definition.m_Entity.Equals(Entity.Null))
            {
                holder.Unset();
            }

            holder.Definition = new(true);

            if (_MIT.IsValid(to))
            {
                holder.Set(to);
            }
        }

        internal void SetToolFlagEnabled()
        {
            if (_MIT.MITState != MITStates.ToolActive) return;

            if (Normal.MV is not null)
            {
                Normal.MV.m_Overlay.AddFlag(InteractionFlags.ToolHover);
                foreach (Moveable mv in Normal.MV.GetChildMoveablesForOverlays<Moveable>())
                {
                    mv.m_Overlay.AddFlag(InteractionFlags.ToolParentHover);
                }
            }

            if (Child.MV is not null)
            {
                Child.MV.m_Overlay.AddFlag(InteractionFlags.ToolHover);
                foreach (Moveable mv in Child.MV.GetChildMoveablesForOverlays<Moveable>())
                {
                    mv.m_Overlay.AddFlag(InteractionFlags.ToolParentHover);
                }
            }
        }

        internal void SetToolFlagDisabled()
        {
            if (_MIT.MITState == MITStates.ToolActive) return;

            if (Normal.MV is not null)
            {
                Normal.MV.m_Overlay.RemoveFlag(InteractionFlags.ToolHover);
                foreach (Moveable mv in Normal.MV.GetChildMoveablesForOverlays<Moveable>())
                {
                    mv.m_Overlay.RemoveFlag(InteractionFlags.ToolParentHover);
                }
            }

            if (Child.MV is not null)
            {
                Child.MV.m_Overlay.RemoveFlag(InteractionFlags.ToolHover);
                foreach (Moveable mv in Child.MV.GetChildMoveablesForOverlays<Moveable>())
                {
                    mv.m_Overlay.RemoveFlag(InteractionFlags.ToolParentHover);
                }
            }
        }


        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append("Normal: ");
            if (Normal.Definition.IsNull)
            {
                sb.Append("Nothing");
            }
            else
            {
                sb.AppendFormat("{0}-{1}", Normal.Definition.m_Entity.DX(true), (Normal.MV.IsManipulatable ? (Normal.MV.IsManipChild ? "C" : "P") : "N"));
            }
            if (!Normal.LastValid.m_Entity.Equals(Entity.Null))
            {
                sb.AppendFormat(", Valid:{0}", Normal.LastValid.m_Entity.DX(true));
            }
            if (!Normal.OnPress.m_Entity.Equals(Entity.Null))
            {
                sb.AppendFormat(", Press:{0}", Normal.OnPress.m_Entity.DX(true));
            }

            sb.Append(" - Child: ");
            if (Child.Definition.IsNull)
            {
                sb.Append("Nothing");
            }
            else
            {
                sb.AppendFormat("{0}-{1}", Child.Definition.m_Entity.DX(true), (Child.MV.IsManipulatable ? (Child.MV.IsManipChild ? "C" : "P") : "N"));
            }
            if (!Child.LastValid.m_Entity.Equals(Entity.Null))
            {
                sb.AppendFormat(", Valid:{0}", Child.LastValid.m_Entity.DX(true));
            }
            if (!Child.OnPress.m_Entity.Equals(Entity.Null))
            {
                sb.AppendFormat(", Press:{0}", Child.OnPress.m_Entity.DX(true));
            }
            return sb.ToString();
        }


        public static void DebugRaycastLine()
        {
            Line3.Segment line = ToolRaycastSystem.CalculateRaycastLine(Camera.main);
            DebugLine.Factory(line, 0, new(0.2f, 0.8f, 0f, 0.5f), 7, 1);
        }

        public static string DebugVanillaResults(NativeArray<Game.Common.RaycastResult> networkResults, NativeArray<Game.Common.RaycastResult> surfaceResults)
        {
            var msg = $"networkResults:{networkResults.Length}, surfaceResults:{surfaceResults.Length}";
            foreach (Game.Common.RaycastResult result in networkResults)
            {
                msg += $"\n    NET: {result.m_Owner.DX()} - {result.m_Hit.m_HitEntity.DX()} - {result.m_Hit.m_HitPosition.D()} - {result.m_Hit.m_NormalizedDistance}";
            }
            foreach (Game.Common.RaycastResult result in surfaceResults)
            {
                msg += $"\n    SUR: {result.m_Owner.DX()} - {result.m_Hit.m_HitEntity.DX()} - {result.m_Hit.m_HitPosition.D()} - {result.m_Hit.m_NormalizedDistance}";
            }
            return msg;
        }

        public static void DebugDumpVanillaResults(NativeArray<Game.Common.RaycastResult> networkResults, NativeArray<Game.Common.RaycastResult> surfaceResults, string bundle = "", string prefix = "")
        {
            string msg = prefix + DebugVanillaResults(networkResults, surfaceResults);
            if (bundle.Equals(string.Empty))
            {
                MIT.Log.Debug(msg);
            }
            else
            {
                MIT.Log.Bundle(bundle, msg);
            }
        }
    }
}
